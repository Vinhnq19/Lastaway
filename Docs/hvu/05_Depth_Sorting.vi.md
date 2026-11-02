# 05 — Depth Sorting (Y-sort) và Occlusion

Cập nhật **03/11/2026**, Unity **6000.3.15f1**. Nhánh `hvu-feat/depth-sorting-occlusion`.

Mục tiêu: nhân vật đi **lên** thì vẽ **sau** vật thể, đi **xuống** thì vẽ **trước**. Chỉ entity vừa di chuyển mới được tính lại; entity đứng yên không tốn gì mỗi frame.

## 1. Kiến trúc

```
 HeroController.LateUpdate ─┐   (vị trí đổi?)            DepthSortingSystem (1 instance, trên [Bootstrap])
 PlayerView.LateUpdate ─────┼──► DepthSortable.MarkDirty ──► dirty queue (List + cờ IsQueued, không trùng)
 OnEnable (đăng ký lần đầu) ┘                                   │
                                                                ▼ LateUpdate, DefaultExecutionOrder(10000)
                                       order = -round(SortPoint.y × 100) → SortingGroup.sortingOrder (chỉ khi đổi)
```

| Thành phần | File | Vai trò |
|---|---|---|
| `DepthSortable` | `Scripts/Presentation/Rendering/DepthSortable.cs` | Ở root entity. Có `SortPoint` (trống = root), `SortingGroup` (tự lấy). Không có Update. `[Inject]` nhận system; OnEnable đăng ký, OnDisable hủy đăng ký và rút khỏi queue. |
| `DepthSortingSystem` | `Scripts/Presentation/Rendering/DepthSortingSystem.cs` | Danh sách đã đăng ký (`HashSet`) + dirty queue. Xử lý một lần trong LateUpdate chạy **sau mọi script khác**, nên đọc vị trí cuối của frame (sau Rigidbody2D interpolation, sau PlayerView, sau teleport). `precision` mặc định 100, kẹp vào khoảng 16-bit. |
| `SortingLayers` | `Scripts/Presentation/Rendering/SortingLayers.cs` | Tên layer dùng trong code. |
| Bootstrap | `Scripts/Bootstrap/Bootstrap.cs` | `AddComponent<DepthSortingSystem>()` cạnh `TickRunner`, đăng ký vào DI. Không có service locator tĩnh. |
| Editor setup | `Tools/Editor/World/DepthSortingSetup.cs` | Menu **Jungle Maze → Rendering → Apply Depth Sorting Setup**. Chạy lại không tạo trùng. |

Vì sao không dùng `TickRunner`: nó chạy trong `Update`, trước khi `PlayerView` đặt vị trí (LateUpdate). Vì sao không để mỗi entity tự tính trong LateUpdate: brief cấm, và entity tĩnh sẽ tốn công vô ích.

Movement không phụ thuộc chặt vào rendering: `HeroController`/`PlayerView` chỉ `TryGetComponent<DepthSortable>` trong Awake. Không có component thì chạy y như cũ.

## 2. Sorting Layer

Thứ tự (sau → trước): `Ground` < `Weapon` < `Enemy` < **`WorldDynamic`** < `Default` < **`Foreground`** < `VFX`.
`Ground`, `Weapon`, `Enemy`, `VFX` có sẵn (từ JSurviv), không đổi. Tool chỉ **thêm** `WorldDynamic` (ngay trước Default) và `Foreground` (ngay sau Default).

| Layer | Nội dung hiện tại |
|---|---|
| `Ground` | Nền `MapGridView` (order -100), hazard phẳng swamp/lava/poison (order -1) |
| `WorldDynamic` | Hero, remote player, 4 obstacle (pillar, wall, statue, rock): Y-sort |
| `Default` | Nội dung cũ chưa Y-sort, **giữ nguyên hành vi**: projectile, melee swing, item rơi, chest/pot/bush/portal/door/supply, tường biên |
| `Foreground` | Dành cho tán cây, mái nhà cần che player (chưa có asset nào) |

`WorldDynamic` đặt **dưới** `Default` có chủ ý: mọi thứ cũ trước đây vẽ trên hoặc ngang Hero (order ≥ 0) vẫn vẽ trên Hero, nên không có regression.

**Ánh sáng URP 2D:** `Light2D` chỉ chiếu các layer trong *Target Sorting Layers*. Sprite ở layer không được chiếu sẽ **đen xì**. Tool setup tự thêm `WorldDynamic` và `Foreground` vào mọi Light2D đang chiếu `Default` trong `Assets/_JungleMaze/Scenes` (Boot, Game). Khi thêm Light2D mới hoặc layer mới, chạy lại menu hoặc tick layer trong Inspector của light.

## 3. Thêm Depth Sorting cho entity mới

1. Ở **root** prefab: thêm `DepthSortable`. `SortingGroup` được tự thêm. Đặt `SortingGroup.Sorting Layer = WorldDynamic`.
2. Đặt `SortPoint`: tạo child ở điểm chạm đất rồi kéo vào field. Không dùng tâm sprite, không đổi pivot sprite.

   | Entity | SortPoint |
   |---|---|
   | Player/NPC/Enemy **xoay theo hướng ngắm** (như Hero) | để trống (root = tâm thân); child lệch tâm sẽ xoay theo |
   | NPC/Enemy không xoay | giữa hai chân / điểm chạm đất |
   | Tree | gốc thân |
   | Rock, Building | đáy hoặc footprint |
3. Nếu entity **di chuyển**, script di chuyển gọi `depthSortable.MarkDirty()` **sau** khi đặt vị trí trong frame (LateUpdate là chắc nhất). Gọi mỗi frame cũng được: trùng lặp bị loại, order không đổi thì không ghi. Mẫu: `HeroController.LateUpdate`, `PlayerView.NotifyDepth`.
4. Entity **tĩnh**: không cần làm gì thêm. Lần đăng ký đầu (OnEnable sau inject) tự xếp một lần.
5. Object tạo bằng `new GameObject` + `AddComponent` không đi qua DI: gọi `depthSortable.Construct(system)`. Object trong scene hoặc từ `PoolService` đã được inject sẵn.
6. Prefab dự án do tool cấu hình: thêm đường dẫn vào `DepthSortingSetup` rồi chạy menu, không sửa tay.

Thân và vũ khí cầm tay để làm **con** của root: `SortingGroup` giữ chúng thành một khối, thứ tự bên trong theo `sortingOrder` của từng renderer con.

## 4. Occlusion (che khuất một phần)

```
Tree                      (không SortingGroup, không DepthSortable)
├── Trunk                 DepthSortable + SortingGroup(WorldDynamic), SortPoint = gốc
│   └── SpriteRenderer
└── Canopy                SpriteRenderer, Sorting Layer = Foreground  ← anh em với Trunk, KHÔNG là con
```

- Player Y-sort với `Trunk` như mọi entity.
- `Canopy` ở `Foreground` nên luôn che player, độc lập với order của trunk.
- **Sai**: đặt `SortingGroup` ở `Tree` root. Canopy sẽ bị nhốt chung group và chịu order của trunk.
- Building: tách thân (WorldDynamic, Y-sort theo đáy) và mái (Foreground) theo cùng cách.
- Chưa làm fade/shader khi player đứng dưới tán: brief không yêu cầu.

## 5. Tilemap

Project chưa dùng Tilemap. Nền là **một** SpriteRenderer tiled (`MapGridView`) ở layer `Ground`, không tham gia Y-sort. Khi thêm Tilemap: ground tilemap để `Ground`, Mode = Chunk; chỉ dùng `Individual` cho tilemap vật thể đứng cần sort từng tile (đặt `WorldDynamic`). Không đưa cả bản đồ vào `DepthSortingSystem`.

## 6. Kiểm thử

Tự động (chạy bằng Unity Test Runner):

| Tiêu chí brief | Test |
|---|---|
| Công thức, precision, kẹp 16-bit | EditMode `DepthSortingOrderTests` (3) |
| 1, 2. Đi lên vẽ sau / đi xuống vẽ trước trunk | `PlayerWalkingUpGoesBehindTrunkAndDownComesInFront` |
| 3. Đi chéo | `DiagonalMovementSortsByYOnly` |
| 4. NPC/Enemy giao nhau | `NpcAndEnemySwapOrderWhenTheyCross` |
| 5. Canopy che player độc lập | `CanopyIsOutsideTrunkGroupAndStaysInForeground` |
| 6. Body + Weapon cùng nhóm | `BodyAndWeaponStayGroupedUnderOneOrder` |
| 7. Entity đứng yên không sort lại | `StaticEntitySortsOnceOnRegisterThenStaysIdle` |
| 8. MarkDirty nhiều lần chỉ xử lý 1 lần | `RepeatedMarkDirtyIsQueuedOnceAndUnchangedOrderIsNotWritten`, `RegisterIsIdempotent` |
| 9. Disable/Destroy không lỗi | `DisabledEntityLeavesQueueAndIgnoresMarkDirtyUntilReEnabled`, `DestroyedEntityIsDroppedWithoutErrors` |
| 10. Rigidbody2D interpolation | `HeroWithInterpolatedRigidbodySortsByFinalFramePose` (30 frame, order khớp pose hiển thị) |
| Không cấp phát GC | `SteadyStateSortingDoesNotAllocate` |
| SortPoint thay cho tâm sprite | `SortPointOverridesSpriteCentre` |

Tất cả nằm trong PlayMode `DepthSortingPlayTests` (13).

Kết quả ngày 03/11/2026:
- EditMode **869/870**. Fail duy nhất `Phase2Tests.PackagedMapHashMustMatchServer` là lỗi có sẵn trên `main`: manifest runtime data lỗi thời.
- PlayMode **133/133**.

Thử tay trong scene `Game` (Play trực tiếp): 1 system, 5 entity đăng ký (Hero + 4 obstacle). Hero ở y=23.3, trên chân BoulderRock (y=23): order -2330 < -2300, vẽ sau đá. Ở y=21.5: -2150, vẽ trước. Hero đứng yên thì queue rỗng.

> Play trực tiếp scene `Game` hiện lỗi `Dữ liệu trận bị thay đổi; đóng gói lại client.` ngay trong `Bootstrap.Compose`. Đây là lỗi có sẵn, không do feature này. Bootstrap dừng nên không inject gì, cả HUD lẫn Y-sort. Chạy **Jungle Maze → Phase 2 → Sync Runtime Data** trước khi thử.

**Test scene thủ công** (chưa tạo file, mô tả để dựng khi có asset): Ground (MapGridView hoặc Tilemap ở `Ground`), Hero prefab, 2 `PlayerView` làm NPC/Enemy, một Tree theo cấu trúc mục 4, một BoulderRock. Đi vòng quanh gốc cây và tảng đá theo 4 hướng và đường chéo. Bật Frame Debugger để xem thứ tự vẽ.

## 7. Giới hạn còn lại

- Chest/pot/bush/portal/door/supply (`WorldObjectView`, tạo runtime) **chưa** Y-sort, vẫn luôn vẽ trên Hero như trước. Muốn Y-sort chest/pot/door/supply thì thêm `SortingGroup` + `DepthSortable` khi tạo view và truyền system từ `WorldSceneView` (đã được inject).
- Với art hiện tại, sprite obstacle vừa khít collider nên Hero hiếm khi chồng lên obstacle. Hiệu ứng thấy rõ khi có sprite "cao" (cây, tượng có chiều cao) vượt khỏi footprint.
- Scene `WorldDemo` không dùng Hero prefab (tự vẽ hero bằng renderer copy từ `Body`), nên vẫn sort như cũ.
- Remote player lấy SortPoint ở tâm thân (vì xoay theo hướng ngắm). Nếu sau này có sprite nhân vật không xoay, đặt SortPoint ở chân.
