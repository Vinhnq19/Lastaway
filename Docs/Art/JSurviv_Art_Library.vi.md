# Thư viện art JSurviv và kế hoạch thay thế

Toàn bộ art của JSurviv (20.264 file) đã được chuyển từ `Assets/Imported/JSurviv` sang **`Assets/_JungleMaze/Art/JSurviv/`**, chia theo chủ đề. Việc chuyển dùng `git mv` kèm `.meta` nên **GUID giữ nguyên**: prefab, scene, tile palette, animator cũ của JSurviv và `WorldVisualSet.asset` vẫn trỏ đúng, không cần gán lại.

`Assets/Imported/JSurviv` giờ chỉ còn code và phần phụ thuộc: `Scripts`, `Prefabs`, `Plugins`, `Configs`, `Resources`, `Settings`, `Epic Toon FX` (gói Asset Store), `TextMesh Pro`.

## Cấu trúc thư mục

| Thư mục | Nội dung | Số file |
| --- | --- | ---: |
| `Characters/Heroes/Sprites` | 11 hero (`Hero_1…Hero_14`), mỗi hero có Idle/Run/Attack/Hand (`.aseprite` + `Sheet/*.png`) | |
| `Characters/Heroes/Template` | Template hero gốc để vẽ hero mới | |
| `Characters/Heroes/Animations`, `Animators` | Clip `Player.*` (Idle, Attack, Die, Hold…), `hvu_Player/Hero_N/{idle,move,attack}`; `Player.controller`, `hvuPlayer.controller` | 246 (cả Characters) |
| `Characters/Bots` | `bot_1…10`, `bot_default`, tay bot | 31 |
| `Enemies/Sprites` | Bộ quái 2025: Hồn ma, Báo đốm, Anaconda, Tê giác, Fairy, Người Đá, Rồng JS, Thổ dân, Tinh Linh | 28 |
| `Enemies/SpritesMisc` | Dog, Ninja Lead, Other, Parent | 22 |
| `Enemies/Animations`, `Animators` | Clip + controller cho từng quái (ANACONDA, DRAGONJS, GOLEM…) | 27 |
| `Weapons/Sprites` | 18 vũ khí 2025 (tên folder như bản gốc) + `37. Đạn sỏi` (đạn nỏ) + sheet nổ thiên thạch | 60 |
| `Weapons/Projectiles` | Clip/controller đạn: BowProjectile, WaterProjectile | 6 |
| `Equipments/Armor` | Giáp gai, Giáp phù thủy, Mũ gỗ, Mũ phù thủy | 5 |
| `Equipments/Consumables` | Gậy thần, La bàn, Lá thần mộc, Lông vũ phượng hoàng, Máu kỳ lân, Nước mắt tinh linh, Thần dược, Tiên dược sự sống, Vảy đuôi tiên cá | 9 |
| `Equipments/Animations`, `Animators` | helmet, pot | 4 |
| `World/Obstacles/Sprites` | Bộ vật cản rừng 2025: rương, bẫy, cây, đá, giếng, tượng, sông (2.203 tile), tàn tích (2.468 tile), hoa dại, cầu, lava, cổng dịch chuyển, cửa đá, đầm lầy… | 5.928 |
| `World/Obstacles/Animations`, `Animators` | Hoa hồng dại, cây phát quang, lava, cổng, đuốc, cờ JS… | 46 |
| `World/Tiles` | `TileMap_dat` (Đất có cỏ, Nền đá, NenDat), `hvu_tileset` (stone_floor, stone_wall, statue) | 159 |
| `World/Palettes` | Tile palette rừng: `TileMap1`, `Ground.prefab`, `Enemies.prefab`, bush/grass/tree/stone… | 197 |
| `UI` | Sprite lobby, ranking, gameplay, timeout; animation icon | 88 |
| `Audio` | 21 file âm thanh gốc (cả 2 file zip) | 21 |
| `Fonts` | 9 font + TMP SDF | 19 |
| `Misc` | File rời: `dragonEgg.aseprite`, `JS TRANSPARENT PNG.png`, 2 prefab gốc… | 6 |
| `Legacy2024/` | Bộ đô thị 2024: `Maps` (Đường phố, Trong nhà, Sân thượng, FPT), `Props` (Art_New: xe, nội thất, biển báo), `Obstacles`, `Palettes`, `Weapons` (súng nước, dép lào…), `Equipments` (vest, nồi, mũ cối, bim bim…) | 13.393 |

Vũ khí và trang bị 2024 được nhận diện theo tên folder có số thứ tự (`01. Dao`, `02. Nồi`…), và vì không có trong `balance/weapons.json` hay `equipments.json`. Riêng `37. Đạn sỏi` giữ ở bộ 2025 vì `BowProjectile.prefab` (nỏ) dùng nó.

## Hiện trạng: game đang dùng gì

| Hạng mục | Đang vẽ bằng | Nơi chọn sprite |
| --- | --- | --- |
| Hero (local) | `Art/Foundation/Hero48.png`, không animation | `Prefabs/Hero.prefab` |
| Người chơi khác | Sprite UI mặc định, tô màu theo NetId, xoay theo hướng ngắm | `Prefabs/PlayerView.prefab`, `PlayerView.cs` |
| Vũ khí | Không vẽ vũ khí cầm tay; đòn chém lấy sprite đầu tiên của prefab JSurviv | `CombatWeaponPresenter.cs`, `MeleeSwingVisual.prefab` |
| Đạn | Mọi vũ khí bắn xa dùng chung `Cell48.png` | `CoverProbeProjectile.prefab` |
| Nền map | `Ground96.png` lát một renderer | `MapGridView.cs` |
| Tường biên | Texture sinh trong code | `MapGridView.cs` |
| 4 vật cản tĩnh | `Cell48.png` tô màu | `Prefabs/Obstacles/{AncientPillar,AncientStatue,BoulderRock,CityWall}` |
| World object (kind 1–13) | Đã dùng art JSurviv | `Art/World/Resources/WorldVisualSet.asset` |
| Item rơi trên đất (kind 100) | Ô vuông trắng tô vàng | `WorldObjectView.cs` |
| Quái | Chưa có trong client | — |
| VFX | Chưa có (chỉ tint flash, hit marker) | — |
| Icon HUD | Bản đã chuẩn hóa 32px trong `Art/UI/Icons` (ART-04) | `HudItemIconCatalog.asset` |

## Đã thay (03/11/2026)

| Hạng mục | Art mới | Ghi chú |
| --- | --- | --- |
| Nền map | `Art/World/Terrain/GroundGrass48.png` (bản sao `TileMap_dat/NenDat/tilemap_grass.png`, 48 px = 1 ô, lát liền) | `MapGridView.groundSprite` |
| Tường biên | `Art/World/Terrain/WallBlock16.png` (khối đá cắt từ `Nền đá/stone_wall_2.png`) | `MapGridView.wallSprite`, lát tile |
| Pillar (1) | `obsidan/obsidian.aseprite` | |
| Wall (2) | `WallBlock16` lát theo footprint | |
| Rock (3) | `10. Đá lớn/bigstone.aseprite` | |
| Statue (4) | `22. Tượng/statue.aseprite` | cao tối đa 1.6 lần footprint |
| Chest (5) | `01. Rương/chest.aseprite` (Frame_0) | |
| Bush (7) | `19. Bụi cây/bush1.aseprite` | |
| Swamp (8) | `Đầm lầy/swamp2.aseprite` | |
| 4 vật cản tĩnh trong Game | như kind 1–4 | `WorldDemoBuild.ApplyStaticMapVisuals` |
| Item rơi trên đất (100) | Icon HUD đã chuẩn hóa theo item id | qua `IItemIconProvider`, không dùng thẳng art JSurviv |
| Hero (local + remote) | `HeroVisualSet.asset`: 9 hero (`Hero_14` mặc định, bỏ `Hero_1`/Teemo), idle + move × back/side/front | `HeroSpriteAnimator` trên Body; người chơi khác nhận hero theo NetId |

**Sửa lỗi méo:** trước đây mọi vật thể bị scale X/Y riêng để nhét vừa khung va chạm (tượng 64×128 ép vào 3×3, cột lấy nhầm slice 450×450 rồi ép 1×2). Giờ `WorldObjectView.Fit` và `ApplyCoverVisualIfMatching` luôn scale đều:
- Vật đứng (kind 1–7, 12, 13): rộng bằng footprint, chân đặt ở đáy footprint, cao tối đa `MaxRise` = 1.6 footprint.
- Mảng phẳng (kind 8–11) và item: nằm gọn trong footprint, căn giữa.
- Tường: lát tile 1:1.
- Root của vật cản tĩnh bị scale lệch (1×4), nên visual con được bù lại scale đó.

**Body hero** đứng thẳng, chọn hướng theo góc ngắm của root (root vẫn xoay như cũ cho gameplay/test), lật trái/phải với hướng side, đổi idle/move theo tốc độ root.

**Dựng lại:** menu **Jungle Maze → Phase 2 → Build World Visual Assets**, **Apply Static Map Visuals**, **Jungle Maze → Art → Build Hero Visuals**. Nguồn art của từng kind nằm trong `WorldDemoBuild.JungleArt`.

**Kiểm chứng:** EditMode 919 test (918 pass; `RoomMapTests.BothPublishedMapsActivate…` fail sẵn, chỉ đọc `Backend/Shared/GameData` + `StreamingAssets`, không liên quan), PlayMode 133/133 pass, chạy Game offline và chụp ảnh đối chiếu trước/sau.

### Tấn công và vũ khí cầm tay

- **Animation tấn công:** `HeroVisualSet` có thêm loop `attack` back/side/front. `HeroSpriteAnimator.PlayAttack()` chạy một lần theo hướng hiện tại, rồi quay về idle/move. `CombatWeaponPresenter` gọi hàm này khi local dự đoán đòn (`AttackPredicted`) và khi server xác nhận đòn của người khác (`RemoteAttackConfirmed`).
- **Vũ khí cầm tay:**
  - `HeldWeaponCatalog.asset` gồm 17 vũ khí. Sprite lấy theo CSV ART-04. Góc xoay, điểm cầm và độ dài nằm trong `HeroVisualBuild.Tuning`. Kích thước tính theo vùng có nội dung thật của sprite, không theo canvas.
  - `HeldWeaponView` (child `Weapon` của Hero/PlayerView) đặt vũ khí ở tay theo hướng ngắm, lật khi ngắm sang trái, nằm sau body khi quay lưng, ẩn khi tay không hoặc khi chết.
  - Hero local theo `IHudState.HeldWeapon`. Người chơi khác hiện vũ khí của đòn đánh gần nhất, vì snapshot không có vũ khí đang cầm.
- **Khi đánh:** đòn cận chiến ẩn vũ khí cầm tay, vì `MeleeSwingVisual` vẽ vũ khí quét theo cung, giờ cũng dùng sprite đã chuẩn hóa. Đòn bắn hoặc ném làm vũ khí giật lùi 0.12 giây.
- **Kiểm chứng:**
  - EditMode 929 test: 928 pass, `RoomMapTests` fail sẵn.
  - PlayMode 133 test: 132 pass. `MenuOpen_DoesNotPause` fail do thời gian khi editor không được focus, chạy lại thì pass.
  - Preview 17 vũ khí × 4 góc ngắm. Playtest có kích một đòn thật qua `OfflineWorldSession.TryAttack`.

### Đạn riêng theo vũ khí

| Vũ khí | Đạn | Nguồn JSurviv |
| --- | --- | --- |
| RUBBER_GUN | Viên sỏi, 7 frame lặp, dài 0.28 | `Prefabs/Projectiles/BowProjectile` (`37. Đạn sỏi`) |
| SCEPTER | Quả cầu phép, dài 0.30 | `ScepterProjectile` (`quyen_truong_dan`) |
| CROSSBOW | Mũi tên, bù −45° (sprite vẽ chéo), dài 0.50 | `Arrow` (`mui_ten`) |
| SHOTGUN | Mỗi viên là tracer cam; một vệt nổ hình nón 6 frame ở nòng, dài 1.4, nén còn 0.24 s | `ShotgunProjectile` (JSurviv vẽ cả chùm đạn thành một hình nón, nên dùng làm hiệu ứng nòng) |

- `ProjectileArtCatalog.asset` (menu **Jungle Maze → Art → Build Projectile Art**) chuẩn hóa kích thước theo vùng nội dung lớn nhất trong mọi frame. Tool cũng tạo `Prefabs/Combat/ShotFlashVisual.prefab` và gắn cả hai vào `CombatWeaponPresenter` trong Game.
- `CoverProbeProjectile` giờ luôn xoay theo hướng bay. Trước đây tracer 0.18×0.08 luôn nằm ngang dù bắn hướng nào. `SetLook/ResetLook` chỉ đổi hình, không đổi sweep va chạm. Pool trả về hình tracer gốc khi despawn.
- Kiểm chứng: EditMode 935 test, 934 pass (`RoomMapTests` fail sẵn). PlayMode 133 test, 132 pass: `Switch20In5s…` fail do thời gian khi editor không focus, chạy lại pass. Ảnh playtest bắn đủ 4 vũ khí qua `SpawnAttackVisual`.

### Vũ khí đang cầm của người chơi khác

- **Protocol:** server ghi mã vũ khí đang cầm vào 4 bit cao (4–7) đang trống của `PlayerStatePacket.Flags`. Kích thước gói (19 byte) và `MatchProtocol.Version` (2) giữ nguyên. Các giá trị: 0 = không rõ (server cũ), 1..10 = `CombatWeaponId` + 1, 15 = đang cầm item không có combat id (đồ ném, đồ đặc biệt). API: `SetHeldWeapon`, `SetHeldOther`, `HeldCode`, `TryGetHeldWeapon`.
- **Server:** `MatchRuntime.WriteHeldWeapon` chỉ ghép mã vào gói lúc gửi; `entity.Flags` dùng cho replay/killfeed không bị dính bit. Có world map thì lấy từ inventory (`World.TryGetHeld`), không có thì lấy từ `CombatService.HeldWeapon`.
- **Client:** `PlayerSnapshot.HeldCode` → `PlayerView` gọi `HeldWeaponView.ShowServerCode` mỗi khi mã đổi. Mã 0 thì giữ cách cũ, tức hiện vũ khí của đòn đánh gần nhất, nên client mới chơi với server cũ vẫn ổn. Client cũ bỏ qua 4 bit này.
- **Kiểm chứng:**
  - `GameplayNetworkChecks` 18/18 qua UDP/WS thật, có 2 check mới: người khác thấy attacker tay không, rồi thấy RubberGun sau khi trang bị.
  - Các bộ check còn lại (Combat, Match, MatchResult, World, WorldNetwork, Inventory, Network, DualTransport, MapCatalog, Telemetry) đều pass. `NetworkChecks` có 1 lần fail ở check nhịp 30 Hz khi máy đang chạy test Unity song song, chạy lại 144/144 pass.
  - Unity EditMode 941 test: 940 pass (`RoomMapTests` fail sẵn). PlayMode 133/133 pass.
- **Deploy:** server trên AWS phải deploy lại thì người chơi online mới thấy vũ khí của nhau. Trước khi deploy, client mới vẫn chơi bình thường với server cũ.

**Còn lại:**
- Đồ ném và đồ đặc biệt (chuối, bom khói, chuông, gậy sahur…) chỉ có mã chung 15, nên người khác thấy tay trống. Muốn hiện đúng thì cần thêm bảng item id ổn định dùng chung, hoặc thêm 1 byte (sẽ phải nâng `MatchProtocol.Version`).
- World object chưa Y-sort với hero. `map_demo` có rương nằm trong footprint của tượng nên vẽ đè lên tượng.
- Pot, Lava, Portal, Door, Poison, Supply vẫn dùng art cũ.

## Kế hoạch thay thế

Ưu tiên theo mức độ người chơi nhìn thấy. Mỗi bước nên làm thành một ticket riêng, đi qua review và QA.

### Bước 1. Map và vật cản

1. **Nền**: thay `Ground96.png` bằng tile `World/Tiles/TileMap_dat/Đất có cỏ` (`tilemap_grass_*`) hoặc `Nền đá`. Ngắn hạn thì gán `groundSprite`. Dài hạn nên chuyển `MapGridView` sang Tilemap để trộn nhiều loại tile.
2. **Tường biên**: dùng `World/Obstacles/Sprites/ResourceObstacle2025/TileIndestructible` hoặc `hvu_tileset/stone_wall`.
3. **World object đang dùng art 2024**: đổi sang bản rừng 2025 trong `WorldVisualSet.asset`.

| Kind | Hiện tại (Legacy2024 hoặc palette) | Đề xuất 2025 |
| --- | --- | --- |
| 1 Pillar | `Legacy2024/Obstacles/Tàn tích bỏ hoang/abandoned.png` | `World/Obstacles/Sprites/tàn tích bỏ hoang` hoặc `tháp chuông` |
| 2 Wall | như trên | `TileIndestructible` / `hvu_tileset/stone_wall` |
| 3 Rock | `Legacy2024/Obstacles/05. Đá to hơn` | `10. Đá lớn/bigstone.png` |
| 4 Statue | `Legacy2024/Obstacles/19. Tượng đài` | `22. Tượng/statue.png` |
| 5 Chest | `World/Palettes/RPG Chests.png` | `01. Rương/chest.png` |
| 6 Pot | `Legacy2024/Obstacles/13. Bình hoa` | Giữ, hoặc thay bằng `Rương bỏ hoang` nếu GDD cho phép |
| 7 Bush | `World/Palettes/busht.png` | `19. Bụi cây/bush1.png` |
| 8 Swamp | `Hồ nước` | `Đầm lầy/swamp2.png` |
| 12 Door | `Legacy2024/.../stone_door.png` | `Cửa đá/stone_door.png` (bản 2025) |

4. **4 prefab vật cản tĩnh** (`AncientPillar`, `AncientStatue`, `BoulderRock`, `CityWall`): gắn sprite giống kind 1–4 ở trên, chỉnh collider theo kích thước sprite mới.
5. **Script cần sửa đường dẫn khi đổi**: `Tools/Editor/World/WorldDemoBuild.cs` đang gọi trực tiếp các sprite `Legacy2024/Obstacles/...`.

### Bước 2. Player

1. Chọn hero mặc định (ví dụ `Hero_2`). Dựng prefab body mới cho cả `Hero.prefab` và `PlayerView.prefab` để local và remote nhìn giống nhau.
2. Animation: dùng clip `Characters/Heroes/Animations/hvu_Player/Hero_N/{idle,move,attack}` với `hvuPlayer.controller`. Hiện không script nào điều khiển `Animator`, nên cần thêm presenter đọc trạng thái di chuyển/tấn công (từ MOV-02/MOV-03 và combat) để set parameter.
3. Hướng nhìn: `PlayerView` đang xoay cả transform theo góc ngắm. Với sprite nhân vật cần đổi sang lật trái/phải (flipX) cho body, chỉ xoay phần tay/vũ khí.
4. Nhiều hero và chọn hero: chỉ làm khi GDD có hero id và protocol gửi hero id. Khi đó tạo catalog `HeroId → AnimatorOverrideController`.

### Bước 3. Vũ khí và đạn

1. **Vũ khí cầm tay**: thêm child "Hand/Weapon" vào body, lấy sprite từ `Weapons/Sprites/<tên>` theo `CombatWeaponId`. Nên làm một catalog riêng (giống `HudItemIconCatalog`) thay cho cách lấy "sprite đầu tiên trong prefab JSurviv".
2. **Đạn**: mỗi vũ khí bắn xa có sprite riêng: nỏ dùng `37. Đạn sỏi` + `BowProjectile.controller`, shotgun dùng `Shotgun`, ná dùng sprite trong `Ná cao su`. Thay `CoverProbeProjectile` (prefab diagnostic) bằng prefab đạn thật.
3. **Ném**: chuối, bom khói, thiên thạch, crystal có sheet nổ (`BananaExplosingSheet.mat`, `MeteoExplosing.mat`, `meteorite_explosion-Sheet.png`).
4. **Item rơi trên đất (kind 100)**: hiển thị icon theo item id. Có thể dùng lại `HudItemIconCatalog` hoặc sprite gốc trong `Weapons/Sprites` và `Equipments/*`.

### Bước 4. VFX

- Hit, nổ, khói: lấy từ `Imported/JSurviv/Epic Toon FX` (gói Asset Store, chưa được move). Kiểm tra license trước khi dùng cho bản phát hành.

### Bước 5. Quái

- Client chưa có quái. Khi GDD/server có (ví dụ rồng gắn với `DragonFlyDurationSeconds`), dùng `Enemies/Sprites` + `Enemies/Animators`.

### Bước 6. UI, âm thanh, font

- Icon HUD đã có bản chuẩn hóa riêng (ART-04). Test AC17 cấm HUD dùng thẳng art JSurviv, và giờ chặn cả `Art/JSurviv/`.
- Âm thanh đã có bộ riêng (ART-05); `Art/JSurviv/Audio` chỉ là nguồn tham khảo. Test `NothingFromImported` cũng chặn cả thư mục mới.
- Font JSurviv chỉ để tham khảo; HUD dùng Be Vietnam Pro và Pixelify Sans.

## Lưu ý khi thay

- **PPU lệch nhau**: hero JSurviv dùng PPU 8 với frame 48px, vật cản 16, art Jungle Maze 96. Cần thống nhất 1 ô map = bao nhiêu pixel trước khi gắn, nếu không nhân vật sẽ to hoặc nhỏ bất thường.
- **Aseprite**: project đã có `com.unity.2d.aseprite` 3.0.1, file `.aseprite` import được trực tiếp. Sửa art thì sửa file `.aseprite`, không sửa sheet `.png` xuất ra.
- **Không sửa tại chỗ khi cần chuẩn hóa**: nếu cần cắt, đổi palette hay đổi kích thước, tạo bản sao trong thư mục art của Jungle Maze, như ART-04 đã làm với icon. Giữ `Art/JSurviv` làm nguồn gốc.
- **Bản quyền / IP**: `Hero_1` tên "Teemo" và font `BeaufortforLOL` liên quan League of Legends. `Legacy2024` có nhận diện Codefest/FPT. Cần xác nhận quyền dùng trước khi phát hành.
- **Legacy2024 vẫn đang được dùng** ở kind 1, 2, 3, 4, 6, 12 và `WorldDemoBuild.cs`. Đừng xóa thư mục này trước khi làm xong Bước 1.

## Đã cập nhật cùng lần move

- `Tools/Editor/World/WorldDemoBuild.cs`, `Art/World/Resources/WorldVisualSet.asset` (chuỗi `Source`): đường dẫn mới.
- `Tools/Audio/*.py`, `Tools/Audio/README.md`, `Tests/EditMode/Audio/ArtAudioManifestTests.cs`: trỏ tới `Art/JSurviv/Audio`.
- `Docs/Tickets/ART-04_icon_inventory.csv`, `Docs/Art/Production_A/Items/{Provenance.csv,Normalization.json}`, `Docs/Tickets/ART-05.md`, `Docs/Tickets/AUD-01_proposal.vi.md`: đường dẫn nguồn mới. Đã kiểm tra 46 + 33 đường dẫn trong CSV đều tồn tại.
- `Tests/EditMode/Art04/Art04IconInventoryTests.cs` (AC17) và `Tests/EditMode/Audio/ArtAudioCatalogTests.cs` (AC9): chặn thêm `Assets/_JungleMaze/Art/JSurviv/`.
- Report QA cũ trong `Docs/Reports` giữ nguyên đường dẫn lúc chạy.
