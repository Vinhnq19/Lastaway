> Cập nhật01/11/2026: LAN/online bắt buộc account-ticket v4; Guest cũ chỉ đọc lịch sử. Chạy trận/account/history theo [hướng dẫn Phase 2](Phase2_Match_Guest_Zone.vi.md); các ví dụ foundation cũ chỉ dùng để kiểm thử module riêng.

# Hướng dẫn dùng Client Base cho người làm gameplay

Cập nhật **01/11/2026**. Tài liệu dành cho người mới vào làm gameplay trên Unity client. Đọc mục 1–3 trước khi viết code; mục 4 là các công thức làm việc hằng ngày. Lý do thiết kế nằm ở [Client_Base_Plan.vi.md](Client_Base_Plan.vi.md); wire protocol và server nằm ở [Network_Architecture.vi.md](Network_Architecture.vi.md).

## 1. Chạy thử trong 5 phút

1. **Offline:** mở Boot, Play → **Luyện tập offline** từ Login/Home, hoặc mở trực tiếp Game trong Editor. Không bật Docker/API; một người chơi đủ vòng bo/HP → kết quả luyện tập → replay/menu. Không ghi lịch sử online.
2. **Chơi mạng:** setup stack API/Mongo/GameServer và secret theo [Phase2, mục1](Phase2_Match_Guest_Zone.vi.md#1-khởi-chạy-backend-local). Chọn profile `phase2-local`, đăng ký/đăng nhập trên menu rồi tạo/vào phòng bằng mã và tên. Không dùng RoomCode để bỏ bước account. LAN/VPN/AWS cần API HTTPS; HTTP chỉ loopback Local.
3. Server `main` cần6 người, lobby15s/loading30s. Profile `test` chỉ dành Local2 người/timer tăng tốc; không dùng public. Chỉ bắt đầu từ Boot khi kiểm full flow mạng.
4. Click **Game View**, WASD/mũi tên để đi và chuột ngắm. Client gửi input; online nhận vị trí/HP/bo từ server. Offline dùng HeroController và OfflineMatchSession, không chạy cùng hai mô phỏng.
5. Chuột trái attack, R reload, F nhặt món đang trỏ,1–4 chọn vũ khí,5–8 dùng đồ, Backspace bỏ vũ khí. Scene dùng world/item/combat service đã ghép; trạng thái nghiệm thu từng bước và giới hạn đọc trong [báo cáo hiện hành](Reports/Phase2_Local/report.vi.md). Không dùng công cụ projectile foundation cũ để chứng minh PvP đã hoàn tất.
6. F3 bật số đo; ESC mở menu và không dừng timer. Build Windows hiện giữ6000.3.15f1, có native quit BLOCKED; file evidence PASS chưa đủ nếu exit khác0.

Nếu Editor không được focus mà game đứng hình, bật **Project Settings → Player → Run In Background**.

Chi tiết phạm vi và bằng chứng năm task nền tảng: [Unity_Foundation_Acceptance.vi.md](Unity_Foundation_Acceptance.vi.md). Prediction/reconciliation nhân vật của mình (MOV-02) và interpolation người chơi khác (MOV-03): xem [Docs/hvu](hvu/README.vi.md).

## 2. Đổi endpoint không cần build lại

Mặc định khi mở game bình thường là **Online · AWS** (`phase2-aws`); endpoint Local/LAN đã lưu không tự được nạp lại. Chọn lại môi trường trong menu mỗi phiên, hoặc chỉ định `-profile phase2-local` / `-clientConfig <file>` khi chạy test. Xem hướng dẫn từng môi trường tại mục0.1 của [Phase2](Phase2_Match_Guest_Zone.vi.md).


Client đọc JSON cùng schema với `Backend/NetSmokeClient/client.*.json`. Chọn file bằng `-clientConfig`, nếu không có thì dùng `-profile`, nếu không có cả hai thì dùng `phase2-aws`. Sau khi đọc file, áp biến môi trường rồi override endpoint từ dòng lệnh:

| Nguồn | Khi nào dùng |
| --- | --- |
| `StreamingAssets/JungleMaze/client.<profile>.json` (`phase2-local`, `phase2-lan`, `phase2-vpn`, `phase2-selfhost`, `phase2-aws`) | AWS là mặc định khi mở thường. Sửa trực tiếp trong thư mục `<Game>_Data/StreamingAssets` của bản build |
| `persistentDataPath/client.json` | Chỉ nạp khi chỉ định bằng `-clientConfig <đường dẫn>`. Tự tạo khi bấm **Lưu endpoint** hoặc khi vào phòng với host/port đã sửa trên menu. Windows: `%USERPROFILE%\AppData\LocalLow\DefaultCompany\Lastaway\client.json` |
| Biến môi trường `NET_MODE`, `NET_GAME_SERVER_HOST`, `NET_GAME_SERVER_PORT`, `NET_API_BASE_URL` | Cùng tên với NetSmokeClient |
| Dòng lệnh `-clientConfig <path>`, `-profile lan`, `-mode LAN`, `-host 192.168.1.20`, `-port 7777`, `-name P2` | Mở nhiều client trên một máy để test |

Dữ liệu từ bản cũ có thể nằm trong thư mục %USERPROFILE%\AppData\LocalLow\DefaultCompany\Jungle_Maze_Survival; ứng dụng không tự chuyển dữ liệu sang thư mục mới.

Key lạ, thiếu key bắt buộc, host có `http://`, hay `transport` ngoài `Udp|Wss|Auto` đều bị từ chối. Unity Windows/Editor có WebSocket và Auto; `webSocketUrl` dùng WSS từ xa, WS chỉ loopback Local. Env thêm `NET_TRANSPORT`, `NET_WS_URL`, `NET_ROOM_CODE`; CLI thêm `-transport`, `-webSocketUrl`. Auto chỉ fallback khi lỗi kết nối trước join, không đổi khi phòng đầy/mã sai. Dùng các profile `phase2-*` và [hướng dẫn Phase 2](Phase2_Match_Guest_Zone.vi.md#6-chọn-môi-trường-online) cho gameplay authoritative; ví dụ foundation ở trên chỉ dành cho module cũ.

## 3. Kiến trúc: code của bạn đặt ở đâu

Client theo **Layered Architecture + quy tắc phụ thuộc của Clean Architecture**. Mỗi lớp là một asmdef, nên tham chiếu sai chiều sẽ không compile.

| Lớp (thư mục / assembly) | Đặt gì vào đây | Không được |
| --- | --- | --- |
| `Domain` · `JungleMaze.Domain` | Model, luật game, C# thuần (`PlayerModel`, `PlayerRegistry`, `PlayerIntent`) | Dùng `UnityEngine`, packet, LiteNetLib |
| `Application` · `JungleMaze.Application` (namespace `JungleMaze.App`) | Game state, use case, service điều phối, **port interface** (`INetworkClient`, `IPacketSender`, `IPacketReceiver`, `IPoolService`, `ISceneLoader`...), `PooledPrefabFactory` | Gọi LiteNetLib/file/HTTP trực tiếp |
| `Presentation` · `JungleMaze.Presentation` | MonoBehaviour hiển thị, view/presenter UI (MVP), đọc input, VFX | Tham chiếu Infrastructure hoặc LiteNetLib |
| `Infrastructure` · `JungleMaze.Infrastructure` | Implement port: `NetworkClient`, config, `AuthApiClient`, `PoolService`, `SceneLoader`, `TickRunner` | Tham chiếu Presentation |
| `Core` · `JungleMaze.Core` | Công cụ dùng chung: DI, `EventBus`, `StateMachine`, `ObjectPool`, `IFactory`, `ITickable`, `ILog` | Biết về game |
| `Backend/Shared/Protocol` · `JungleMaze.Protocol` | Packet dùng chung server/client | Dùng Unity API |
| `Bootstrap` · `JungleMaze.Bootstrap` | `Bootstrap.cs`, nơi duy nhất tạo và nối object | Chứa logic game |

Namespace của lớp Application là `JungleMaze.App`, không phải `JungleMaze.Application`, để tránh che mất `UnityEngine.Application`. Tương tự, thư mục input của Presentation là `Controls`, không phải `Input`.

Khi không chắc code thuộc lớp nào: code vẽ hoặc đọc input bằng Unity thì vào **Presentation**; code điều phối luồng hoặc nghe packet thì vào **Application**; luật thuần thì vào **Domain**; code chạm mạng, file hay HTTP thì vào **Infrastructure**.

## 4. Công thức làm việc hằng ngày

### 4.1. Thêm một service

```csharp
// Application/Inventory/InventoryService.cs
public sealed class InventoryService : IInitializable, IDisposable
{
    private readonly IEventBus _bus;
    public InventoryService(IEventBus bus) => _bus = bus;   // constructor injection
    public void Initialize() { /* subscribe */ }
    public void Dispose() { /* unsubscribe */ }
}
```

Đăng ký **một dòng** trong `Bootstrap.Compose()`: `_registry.Register(new InventoryService(bus));`. Nếu service cần chạy mỗi frame, implement `ITickable` và gọi `tickRunner.Add(service)`. `Initialize` chạy theo thứ tự đăng ký, `Dispose` chạy theo thứ tự ngược lại.

### 4.2. MonoBehaviour cần dependency

```csharp
public sealed class HealthBar : MonoBehaviour
{
    private IEventBus _bus;
    [Inject] public void Construct(IEventBus bus, PlayerRegistry players) { _bus = bus; /* ... */ }
}
```

Có ba trường hợp:

- **Đặt sẵn trong scene:** Bootstrap inject khi scene load, tức là sau `Awake`/`OnEnable` và trước `Start`. Hãy dùng dependency trong `Start` hoặc ngay trong `Construct`.
- **Lấy từ pool:** được inject trước `Awake`, vì instance được tạo dưới một root đang inactive.
- **Không dùng** `FindObjectOfType`, singleton `Instance` mới, hay tự gọi `registry.Resolve` trong code gameplay.

### 4.3. Nghe và phát sự kiện (Observer)

```csharp
public readonly struct PlayerDied { public readonly byte NetId; public PlayerDied(byte id) => NetId = id; }

private Action<PlayerDied> _onDied;                  // giữ delegate để Unsubscribe đúng instance
void OnEnable()  { _onDied ??= OnDied; _bus.Subscribe(_onDied); }
void OnDisable() { _bus.Unsubscribe(_onDied); }
_bus.Publish(new PlayerDied(3));                     // event là struct: không cấp phát
```

Event có sẵn: `NetworkStateChanged`, `JoinedRoom`, `AppScreenChanged`, `ConnectionError`, `PlayerStateUpdated`, `PlayerRemoved`.

### 4.4. Spawn đạn/VFX (Pool + Factory)

```csharp
[Inject] public void Construct(IPoolService pool) =>
    _bullets = new PooledPrefabFactory<Bullet>(pool, bulletPrefab, prewarm: 32);

var bullet = _bullets.Create(new SpawnPose(muzzle.position, muzzle.rotation));
pool.Despawn(bullet, 2f);                            // tự trả về pool sau 2s, không cần coroutine
```

Component trên prefab implement `IPoolable` để reset trạng thái trong `OnSpawned`/`OnDespawned`. Không dùng `Instantiate`/`Destroy` trong vòng gameplay. Despawn hai lần sẽ bị báo lỗi. Ví dụ: `Presentation/Player/RemotePlayerSpawner.cs`; đạn và VFX thật trong scene nghiệm thu dùng `Presentation/World/LocalWeaponProbe.cs` + `CoverProbeProjectile.cs`.

### 4.5. Gửi và nhận packet gameplay

```csharp
var input = new PlayerInputPacket { Tick = tick, InputSeq = seq };
input.SetMove(x, y);
_sender.Send(ref input, PacketDelivery.Unreliable);  // false nếu chưa Joined

_receiver.Subscribe<PlayerStatePacket>(OnState);     // main thread, nhiều subscriber
```

Bình thường bạn không cần gửi input trực tiếp: `PlayerInputService` đã gửi 30Hz khi đang InMatch. Presentation chỉ cần gọi `SetIntent(...)`, xem `PlayerInputReader`. Tương tự, view đọc `PlayerModel` qua `WorldSyncService`, không đọc packet.

**Thêm một loại packet mới:**

1. Tạo `struct` implement `INetSerializable` trong `Backend/Shared/Protocol/Gameplay/`, chỉ dùng kiểu cố định kích thước và cú pháp C# 9.
2. Thêm test round-trip và test `Is.Not.AllocatingGCMemory` theo mẫu `Tests/EditMode/Protocol/PacketRegistryTests.cs`.
3. Chạy `dotnet build Backend/JungleBackend.sln`: server compile cùng file này.
4. Báo người làm server để xử lý packet mới.

### 4.6. Thêm màn hình hoặc luồng (State)

State cấp ứng dụng nằm trong `Application/States/GameStates.cs`. Kế thừa `GameState`, override `Enter`/`Exit`/`Tick` và hai hàm `OnNetworkStateChanged`/`OnGameEvent` nếu cần, rồi tạo state trong constructor của `GameFlow`. Load scene qua `ISceneLoader`, không gọi `SceneManager` trực tiếp. AI hoặc nhân vật dùng lại `Core.States.StateMachine`.

### 4.7. Đẩy dữ liệu lên HUD (UI-01)

HUD đọc ba port trong `Application/Hud/State/`; chỉ tính cách hiển thị, không quyết định gameplay. Gameplay ghi qua port ghi tương ứng (inject bằng `[Inject]` hoặc constructor):

```csharp
_hud.SetHp(72);                                // IHudStateWriter: HP, giáp/mũ, vũ khí cầm, đạn, bo, còn sống, pose
_hud.SetAmmo(inMagazine: 4, reserve: 30);
_hud.SetAliveCount(alive: 7, total: 8);        // total = số người lúc vào trận
_inventory.SetConsumableSlot(0, "GOD_LEAF", 6); // IInventoryStateWriter: 4 khe vũ khí, 4 khe dược, khe đang chọn
_effects.Apply(EffectKind.Adrenaline, 8f, 8f);  // IEffectStateWriter: store tự đếm ngược và tự xoá khi hết hạn
```

- Ghi trên main thread. Store chỉ phát `Changed` khi giá trị đổi, và tự `Reset` khi mất kết nối.
- Ai ghi gì: WPN-02 `SetHp`, ITM-01 khe/giáp/mũ/`SetHeldWeapon`, WPN-01 `SetAmmo`, Game-10 `Apply`, ZON-01 `SetZoneCountdown` (ít nhất 1 lần/giây, HUD không tự đếm bo), SRV-03 `SetAliveCount`, ART-04 thay `NullItemIconProvider` trong Bootstrap. Hiệu ứng mới: thêm giá trị vào `EffectKind`, không đổi port.
- Thử không cần GameServer: mở `Scenes/Dev/HudPreview.unity`, Play, bấm **F4**. Panel F4 và `Mock*State` chỉ có trong Editor/Development build.
- Art HUD hiện là bản tạm: màu, sprite, font chỉ gán trên `Prefabs/UI/Hud.prefab` và `HudPalette`. Thay art thì sửa prefab rồi chạy context menu "Apply palette" trên `HudRoot`, không sửa code.

**Phản hồi HUD — UI-01 phần 2 (đã nghiệm thu 25/10):**

```csharp
_hud.NotifyPickup("RUBBER_GUN", 1);                  // Chỉ thông báo; ITM-01 tự ghi số lượng/khe riêng.
_hud.NotifyHitDealt(HitTargetKind.Hero);             // Other không hiện hit marker.
_hud.NotifyDamageTaken(true, 225f);                 // 0=Đông, 90=Bắc; false nếu nguồn không cho biết hướng.
_hud.SetDanger(123, DangerKind.FusedBomb, 1.5f, true, 7f, 7f); // Vị trí nguồn theo cell thế giới, không phải dx/dy.
_hud.ClearDanger(123);                             // Nguồn gỡ sớm: HUD ẩn ở frame kế tiếp.
_inventory.BeginUse(4, "GOD_LEAF", 0.5f, 0.5f, true); // Khe toàn cục 0..7; dược phẩm 4..7, giây từ balance JSON.
_inventory.CancelUse("GOD_LEAF");                  // Hoặc CompleteUse khi gameplay xác nhận xong.
_effects.Apply(EffectKind.Poison, 3f, 3f);           // Slow không thời lượng: Apply(Slow, 0f, 0f).
```

- `IHudStateWriter` và `IInventoryStateWriter` chứa các hàm mới; ba port đọc không phụ thuộc nguồn network/gameplay. Không phát pickup để tự trừ/tăng túi, không gửi damage feedback để tự đổi HP.
- `HudStateStore`, `InventoryStateStore`, `EffectStateStore` được Bootstrap tick trước scenario; HUD đọc trong LateUpdate. Dùng đồ về 0 vẫn chờ nguồn `CompleteUse`/`CancelUse`; HUD không tự kết luận gameplay đã xong. Id cũ khi đã đổi món bị bỏ qua.
- Bomb dùng id ổn định do nguồn cấp; bẫy không đồng hồ dùng `DangerKind.Trap`, làm mới ít nhất mỗi 4 giây nếu còn hiện. Nguồn quyết định tầm nhìn/LOS và quyền tiết lộ hướng; UI không suy vị trí đối thủ.
- **F5** là panel phản hồi Editor/Development, có từng loại sự kiện và preset Dồn/Giao tranh; F4 vẫn là HUD lõi. `HudFeedbackTuning` trên prefab chỉnh thời gian/giới hạn; `HudEffectCatalog.asset` chỉnh nhãn/icon hiệu ứng. Icon vật phẩm thật vẫn thuộc ART-04.
- QA đo 0 B trong Tick đồng bộ của store/presenter/view sau warm-up; chưa bao gồm deferred Canvas rebuild, toàn frame hay Release player. Xem báo cáo `Docs/Reports/UI-01-P2_2026-10-25/`.

### 4.8. Phát VFX và seam visibility (VFX-01)

VFX-01 vẽ phản hồi trên thân (nháy, số sát thương, hạt trúng đòn, quầng trạng thái), vòng cảnh báo ngòi nổ và rung màn hình. Nổ, khói, bẫy, dây leo, chuông do `ThrowableView` (WPN-03) vẽ; vùng bo do `ZoneOverlayView` vẽ. Mọi quyết định nằm ở `VfxDirector` (Application), hình nằm ở `VfxStage` (Presentation, pool CLI-03). Muốn có hiệu ứng thì **publish một event struct lên `IEventBus`**, không gọi view:

```csharp
_bus.Publish(new HitConfirmedEvent(eventKey, sourceNetId, targetNetId, damage, x, y));          // WPN-02: damage > 0
_bus.Publish(new ExplosionEvent(key, "BANANA", ExplosionPhase.Fuse, x, y, 3, 3, fuseSeconds, owner)); // vòng ngòi nổ
_bus.Publish(new ExplosionEvent(key, "BANANA", ExplosionPhase.Detonate, x, y, 3, 3, 0, owner));      // gỡ vòng + rung nếu gần
_bus.Publish(new StatusEvent(key, netId, StatusKind.Stun, true, seconds, remaining));          // Game-10: chỉ cho NetId ≠ mình
```

- `EventKey` phải ổn định khi gói gửi lại (tick + nguồn + đích + seq, hoặc id sự kiện server); cùng khoá trong 5 s chỉ phát một lần. Khoá 0 = không khử trùng.
- Đồ ném (WPN-03) đã nối sẵn qua `ThrowableVfxSource` (nghe `IThrowableEventSource`); trạng thái của chính mình đã nối qua `LocalEffectStatusSource` (đọc `IEffectState`). Không publish lại các event này.
- Hit, nổ, trạng thái không bao giờ được dự đoán. Chỉ ra đòn của mình (`AttackPredicted`) được dự đoán, đối soát theo `RequestSeq`.
- **FOW-01:** hiện thực `IVisibilityPolicy.IsVisible(x, y, netId)` (`netId = 255` khi hỏi theo vị trí) và đăng ký vào registry dưới `IVisibilityPolicy`, trước hoặc sau `ComposeVfx` đều được. VFX không đăng ký mặc định vào registry; khi chưa có policy thì `DeferredVisibilityPolicy` cho hiện tất cả và tự chuyển sang policy của FOW-01 khi nó xuất hiện. Director hỏi lại mọi hiệu ứng đang sống mỗi tick; người chơi local luôn hiện.
- Số trình bày (0,16 s nháy, 0,7 s số, rung ≤ 0,15 cell, ngân sách 64) nằm trong `Art/Vfx/Resources/VfxRuntimeSet.asset`; prefab/catalog dựng lại bằng menu **Jungle Maze → VFX → Build VFX-01 Assets**.
- Thử không cần GameServer: mở `Scenes/Dev/VfxPreview.unity`, Play, panel **F10** tự mở (hit, quầng, ném giả, lặp khoá, rollback, đổi phòng, ẩn mục tiêu, tải 10 nguồn). Ở scene Game, F10 vẫn bắn được event giả lên trận thật (Editor/Development).

## 5. Kiểm thử

- **Window → General → Test Runner → EditMode → Run All:** assembly `JungleMaze.Tests.EditMode`; kết quả 23/10 là **125/125 PASS**, gồm DI, EventBus, StateMachine, pool, config, serializer 0 GC và `NetworkClient` với `FakeGameServer` trên loopback. Sau UI-01 lõi và merge main (24/10) là **193/193 PASS**. Sau build UI-01 phần 2, QA độc lập ngày 25/10 đạt **247/247 PASS**, gồm test feedback trong `Tests/EditMode/Hud/Feedback/`. Đây là kết quả test, không thay nghiệm thu G3.
- **PlayMode → Run All:** assembly `JungleMaze.Tests.PlayMode`; các test nền tảng chạy Unity Physics2D, config sau Awake, aim, camera, map, projectile và pooling. Sau UI-01 lõi là **20/20 PASS**; QA phần 2 ngày 25/10 đạt **37/37 PASS**, thêm 17 `HudFeedbackAcceptanceTests` và giữ hồi quy lõi. Các test này không cần EC2/API/MongoDB. Bằng chứng mới ở `Docs/Reports/UI-01-P2_2026-10-25/`.
- Phía server: `Backend/Tests/run_all.cmd` (cần Docker).
- Đo RTT để nghiệm thu: bật F3, hoặc xem dòng `[client-summary]` trong log khi mất kết nối, cùng format với NetSmokeClient.

Số đo ngày 20/10/2026 trên một PC, GameServer chạy bằng `dotnet run` qua loopback:

| Phép đo | Kết quả |
| --- | --- |
| Unity client | p95 ≈ 63ms (lần đo chỉ trong Editor), p95 ≈ 40ms (Play Mode) |
| NetSmokeClient, cùng server | p95 = 63ms |
| RTT riêng của transport LiteNetLib | ≈ 0ms |

Độ trễ chủ yếu nằm ở vòng xử lý của server, không phải ở client.

## 6. Kết nối Phase 2 và ranh giới tiếp theo

Luồng chạy trận nằm trong [hướng dẫn Phase 2](Phase2_Match_Guest_Zone.vi.md). Dùng profile `phase2-local` (protocol v4) cho account-ticket và resume. Ticket được lấy qua API trước transport; không gửi JWT/Guest credential dài hạn qua UDP. Server main cần 6 người, profile test riêng cần 2. Giữ 10 người/phòng, 5 phòng/tổng 50 slot.

| Module | Trách nhiệm hiện tại |
| --- | --- |
| `MatchSession` | Hello/loading, MatchId, input 30Hz, snapshot/ack, own NetId từ welcome, HP/bo/kết quả/replay; bỏ packet trận cũ |
| `NetworkClient` | UDP/WSS/Auto v4, hai channel; attempt/ticket trước join, một slot khi fallback, resume bằng proof + vé mới cùng account. Codec cũ chỉ cho fixture |
| `MatchDemoView` | Góc nhìn toàn map, roster/timer/HP/bo/result/history; tắt local controller và local weapon probe khi chơi mạng |
| `ClientRuntimeData` | Kiểm manifest/hash với server và geometry scene trước LoadingAck |
| `GuestHistoryClient` | Account ticket/history qua HTTPS hoặc loopback; DPAPI chỉ còn cho hồ sơ Guest cũ. Guest không lấy vé chơi mới |
| GameServer | C# authoritative movement/collision, lifecycle, 5 đợt bo, HealthService và kết quả; API/DB không chặn tick |

Unity Windows/Editor đã có WSS/Auto. Prediction/reconciliation (MOV-02) và interpolation (MOV-03) đã có, xem [Docs/hvu](hvu/README.vi.md). Combat, inventory, hazard, HUD đầy đủ và killcam không được coi hoàn tất bởi demo. Sprite/nhãn hiện tại dùng để kiểm mạng. `PlayerInputService` cũ không được tick cùng `MatchSession`; không bật lại HeroController trong demo mạng. Dùng khung scenario/assertion/driver ở mục8 của hướng dẫn Phase2 để kiểm feature mới, không sửa state server từ client để tạo kết quả PASS.

## 7. Sửa dữ liệu và map để kiểm thử

- Dữ liệu: `Assets/StreamingAssets/JungleMaze/balance/match.json`, `weapons.json`, `equipments.json`. Provider đọc một lần khi Bootstrap khởi tạo: **Stop Play → sửa/lưu JSON → Play lại → join**. Không có hot reload hoặc tự đồng bộ Google Sheets.
- Ví dụ trong `weapons.json`, tìm `RUBBER_GUN`: đổi `projectileSpeed` từ 18 thành 36, giữ `range: 6`; đạn thử bay nhanh gấp đôi nhưng vẫn chỉ đi tối đa 6 cell. Đổi `fireRatePerSecond` sẽ đổi nhịp bắn thử; không cần sửa code. Khôi phục dữ liệu sau khi thử.
- Trong scene `Game`, chọn **Local weapon validation → LocalWeaponProbe → Weapon Id** để chọn một ID thuộc nhóm `Ranged`. Prefab đạn/VFX ở `Assets/_JungleMaze/Prefabs/Diagnostics/`; tắt component này khi không cần công cụ thử.
- `MapGridView` đọc `mapSize` và `cellSize` từ match config: mặc định **50×50 cell, 48 pixel/cell, 1 Unity unit/cell**. Nền dùng một tiled renderer; bốn collider biên được tạo lúc Start. Giữ transform `MapGrid` tại gốc, scale 1.
- Bốn prefab trong `Prefabs/Obstacles/` là Cột 1×2, Tường 1×4, Đá 4×4, Tượng 3×3; root scale quy định footprint, BoxCollider2D không trigger, layer `HardCover`. Sprite 48 PPU hiển thị đúng kích thước collider.
- Đạn thử dùng `CoverQuery.Sweep` quét cả bước di chuyển thay vì chỉ kiểm tra điểm cuối, nên không xuyên cover khi tốc độ cao. FOW/LOS sau này tái sử dụng query/collider này; task hiện tại chưa dựng shader/FOW.

### World integration respects the authored map

`world_settings.json` defaults to `layoutMode: "Authored"`. World services read the team's existing `map_demo.json`; they preserve its cover bounds and spawn coordinates. `WorldSceneView` never disables existing map objects and only creates views/colliders for interactive world objects and floor items. An empty placement list stays empty: it does not fill the Game scene with procedural containers, hazards or portals.

The map owner can export optional `worldObjects` in the same map document using `WorldObjectDefinition` (`kind`, `bounds.minX/minY/maxX/maxY`, and `pairId` for portals). World runtime assigns instance IDs and validates placements without moving them. This export is separate from the renderer/prefabs; do not modify scene cover geometry in an item or environment adapter.

Procedural layouts remain available in the explicit WorldDemo and backend fixtures. Backend fixtures set `WORLD_LAYOUT_MODE=Prototype`; normal team matches must not set this override. The existing map JSON and Game scene are not rewritten by world initialization.
