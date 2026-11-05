# 06 — Level Editor (xếp map)

## Map phát hành cho phòng mạng

Hai map đầu tiên: **Di tích cổ** (`di-tich-co`, mặc định) và **Bãi đá** (`bai-da`). Map online lấy từ catalog server; Local offline dùng map mặc định đóng gói sẵn và không cần API.

1. **Tạo/sửa:** mở Level Editor, Play, mở JSON map và chỉnh bố cục. Map phát hành hiện phải 50×50, đúng 10 spawn an toàn cách nhau tối thiểu 6 cell, portal đủ cặp, không chồng vật cản. Bấm **Kiểm tra**. Lưu bản nguồn bằng **Lưu thành…**; không sửa file theo hash đang phát hành.
2. **Xuất:** menu **Jungle Maze → Level Editor → Export Published Map**. Nhập ID ổn định, tên, mô tả, version mới; chọn mặc định nếu cần. Khi Level Editor đang Play, công cụ dùng bản đồ đang mở; ngoài Play, chọn JSON. Bấm **Kiểm tra và xuất map + preview vào catalog**. Preview là ảnh sơ đồ sinh từ đúng geometry/placements/spawn. Scene và catalog asset của Level Editor được giữ nguyên.
3. **Danh sách map:** nguồn ở `Backend/Shared/GameData/Maps/catalog.json`. Chỉ các mục trong `maps` được thấy trong phòng. Bỏ một mục để ẩn map; giữ ít nhất một mục, `defaultMapId` phải trỏ mục còn lại. Không cần xóa dữ liệu phiên bản cũ. Đổi JSON thì phải xuất lại để cập nhật hash; không sửa hash bằng tay.
4. **Validate/publish Local:** mở terminal tại `Backend`, chạy:

   ```powershell
   dotnet run --project GameServer -- --maps validate Shared/GameData/Maps
   dotnet run --project GameServer -- --maps publish Shared/GameData/Maps data/maps
   docker compose exec game-server dotnet GameServer.dll --maps reload
   ```

   Lệnh publish kiểm toàn bộ gói trước, chép payload bất biến rồi thay `catalog.json` bằng rename nguyên tử. Generation được tăng; gói lỗi không thay catalog active. Reload dùng `ROOM_CONTROL_TOKEN` trong container, không đưa token vào lệnh hoặc mở cổng 7780. `MAP_CATALOG_PATH` đổi thư mục host (mặc định `Backend/data/maps`); thư mục này nằm ngoài image. Lần chạy đầu thư mục trống được seed từ hai map đóng gói. Không chạy publish đồng thời từ nhiều cửa sổ.
5. **Chọn/chạy:** đăng nhập → tạo hoặc vào phòng → xem thẻ map. Trưởng phòng chọn; mỗi client tải JSON/preview qua API và xác minh hash. Có lỗi thì **Thử tải map lại**. Nút **Bắt đầu** chỉ bật khi đủ người và tất cả cùng sẵn sàng. Người vào/rời countdown làm hủy countdown. Kết thúc trận trở lại phòng, không tự bắt đầu lượt tiếp.
6. **Cập nhật:** giữ ID, tăng version, xuất/publish/reload lại. Trận đang chạy giữ map cũ; phòng chờ/lượt sau lấy bản mới. Nếu map bị gỡ, phòng chờ chuyển map mặc định. Chỉ JSON/PNG được tải; thêm loại vật thể hoặc assets chưa được client hỗ trợ cần build client mới.
7. **Rollback:** các payload cũ được giữ; mỗi lần publish lưu manifest trước vào `data/maps/catalog-history/<generation>.json`. Tạo thư mục nguồn rollback riêng, chép các thư mục map theo ID/hash cần phục hồi từ `data/maps`, chép manifest cũ thành `catalog.json` trong thư mục nguồn đó. Chạy `--maps validate <nguồn-rollback>`, `--maps publish <nguồn-rollback> data/maps`, rồi reload như trên. Công cụ tạo generation mới cho dữ liệu cũ; không chép đè catalog đang chạy trực tiếp.

AWS release `maps-hvu-20261103` dùng bind mount `/opt/junglemaze/data/maps` và gameplay protocol **2**; handshake tài khoản vẫn **v4**. Khi cập nhật map, chuyển gói vào thư mục staging riêng trên EC2, validate/publish bằng container cùng phiên bản rồi reload nội bộ. Giữ volume Mongo, secret và map cũ. Client cũ cần thay bằng bản hỗ trợ gameplay protocol 2.

Kiểm hai PC: chạy client cùng bản trên hai máy, trỏ cùng API HTTPS/server LAN hoặc WSS, dùng hai tài khoản. Đổi map và đối chiếu preview, hash trong lobby, cover/spawn/vật thể trong trận; thử rời lúc countdown, lỗi tải rồi retry, kết thúc/chơi lại. Hai process cùng PC chỉ xác minh hai client, chưa nghiệm thu hai PC vật lý.

Cập nhật **03/11/2026**. Scene `Assets/_JungleMaze/Scenes/LevelEditor.unity`, chạy bằng **Play** trong Unity Editor. Art Cainos 1.2.3 nằm riêng tại `Art/LevelEditor/Cainos/`.

## 1. Mở

1. Chỉ khi chưa có scene/catalog (lệnh này ghi lại các asset; giữ bản sửa thủ công trước khi chạy): menu **Jungle Maze → Level Editor → Build Level Editor Scene**. Lệnh này sinh lại `Art/LevelEditor/LevelEditorCatalog.asset` và scene, chạy lại nhiều lần không tạo trùng.
2. **Jungle Maze → Level Editor → Open Level Editor Scene**, rồi bấm **Play**.

Scene này **không** tạo Bootstrap của game (không menu kết nối, HUD hay network). Có sẵn map mẫu nhỏ `Levels/demo_cainos.json` để thử công cụ; map này chưa đạt kích thước phát hành 50×50. Khi xuất map theo ô, công cụ đọc footprint/collider chân của `LevelEditorCatalog.asset`; không thay bằng khối 1×1 mặc định.

## 2. Giao diện

```
┌ Mới · Mở… · Mở map game · Lưu · Lưu thành… · Hoàn tác · Làm lại · Kiểm tra · Viền · Lưới ─ ● tên file ┐
│ Chọn (Q) · Đường đá (P) │                                              │ MAP: Rộng/Cao → Tạo ground     │
│ Che chắn                │   ground cỏ theo seed, đường đá,              │ Phiên bản                      │
│ Vật thể game            │   viền đá + tường gạch mép nam                │ Seed [__] Áp dụng · Ngẫu nhiên │
│ Trang trí               │   P1/P2.. = cặp portal, S1..S10 = spawn       │ Cỏ rải ───●──                  │
│ Spawn                   │                                              │ ĐANG CHỌN · KIỂM TRA · FILE    │
└ chế độ · ô dưới con trỏ · kích thước map · seed · thông báo ────────────────────────────────────────────┘
```

## 3. Thao tác

Mọi vật **đặt theo ô**: không có vị trí lẻ, không phóng to/thu nhỏ hay xoay vật.

| Thao tác | Kết quả |
|---|---|
| Click một vật thể ở cột trái | Vào chế độ **Đặt** với vật thể đó (click lại để thôi) |
| **Chuột trái** (chế độ Đặt) | Đặt vào ô dưới con trỏ (con trỏ ở giữa cạnh đáy footprint). Bóng xem trước **đỏ** khi ô đã có vật hoặc ra ngoài map |
| **Chuột trái** (chế độ Chọn) | Chọn vật thể trên cùng, giữ để kéo theo ô |
| **Chuột phải** | Xóa vật thể dưới con trỏ |
| **P** / nút **Đường đá** | Cọ đường đá: giữ chuột trái để tô, giữ chuột phải để xoá. Mỗi lượt kéo = một bước hoàn tác |
| **Shift** | Lật ngang bút hoặc vật đang chọn (collider chân lật theo) |
| **Lăn chuột**, `+`/`-` | Phóng to / thu nhỏ quanh con trỏ |
| **Alt + kéo chuột trái** | Di chuyển camera (giữ chuột giữa hoặc WASD cũng được) |
| **F** | Xem toàn map |
| Q / Esc | Về chế độ Chọn |
| Del / Backspace | Xóa vật đang chọn |
| Mũi tên | Dịch vật đang chọn 1 ô |
| H | Bật/tắt viền footprint và **collider chân** (ô đỏ nhỏ) |
| L | Bật/tắt lưới (1 ô = 1 unit, nét đậm mỗi 5 ô; zoom xa thì tự ẩn nét mảnh) |
| Ctrl+Z / Ctrl+Y (Ctrl+Shift+Z) | Hoàn tác / làm lại (200 bước) |
| Ctrl+S | Lưu |
| Ctrl+D | Nhân bản vật đang chọn (giữ lật) |

> Trong Play mode, Unity Editor có thể bắt Ctrl+Z/Ctrl+S trước Game view. Nếu phím tắt không ăn, dùng nút trên thanh trên.

**Ground:** nhập **Rộng/Cao** (và seed) rồi bấm **Tạo ground**. 1 ô = 1 world unit = 32 px Cainos.
- **Seed** quyết định: tile cỏ (cỏ trơn, ngọn cỏ, cụm hoa/sỏi), viên đá lẻ ở viền đường đá, cửa sổ trên tường mép, và mẫu hình bụi/bình. **Cùng seed + cùng map = cùng kết quả.** Kết quả của mỗi ô chỉ phụ thuộc seed, toạ độ ô và các ô đường kề nó, nên sửa chỗ này không làm cỏ chỗ khác đổi theo.
- **Cỏ rải**: mật độ sprite cỏ nhỏ rải trên ô trống. Ô có vật, spawn hoặc đường đá thì không có cỏ.
- **Đường đá**: ô được tô lát kín đá. Ô cỏ sát đường có viên đá lẻ quay về phía đường, nên viền tự nối và vỡ tự nhiên (đo theo 4 viên 16 px của tileset).
- **Tường mép**: viền đá mảnh quanh mép trên/trái/phải, và 2 hàng tường (gờ đá + gạch) ngay dưới mép nam. Chỉ là hình, nằm ngoài vùng chơi.

## 4. Vật thể

| Nhóm | Vật thể | Ghi ra JSON |
|---|---|---|
| Che chắn | Cây 1–3, Tượng đá, Cột đá (+gãy), Cột rune thấp/cao/gãy, Bia mộ 1–3, Khối đá, Đèn đá, Giếng, Ghế đá ngang/dọc Đ/T, Quan tài ngang/dọc, Thùng tròn, Thùng gỗ nhỏ, Đá 1–2, Tảng đá lớn, Cổng đá 1–2 | `obstacles`: **một phần tử cho mỗi collider chân** (game coi là hard cover `Wall`) |
| Vật thể game | Rương, Bình gốm (3 mẫu), Thùng tiếp tế, Bụi rậm (5 mẫu), Cửa gỗ, Portal (bệ rune 3×2) | `worldObjects`, bounds = cả footprint ô |
| Trang trí (đi xuyên) | Đá vụn, Gạch vụn, Cỏ, Hoa, Biển báo Đ/T | `decorations`: game bỏ qua |
| Spawn | Điểm spawn | `spawnPoints` (tâm ô) |

- **Collider chân** lấy từ collider của prefab Cainos (gốc cây ~0.28×0.19, đế tượng, chân đá...). Thân/tán chỉ là hình. Collider bắt đầu cao hơn 0.3 (xà ngang cổng đá) bị bỏ, nên đi qua được giữa cổng.
- Footprint ô của vật cản lấy từ collider (làm tròn). Vật thể game làm tròn theo ô (rương/bình/bụi/cửa 1×1, portal 3×2).
- Đầm lầy, lava, ổ gai đã bỏ khỏi bảng vì Cainos không có art. File cũ có các vật này vẫn mở được (hiện thành khối giữ đúng bounds).
- Portal tự ghép cặp theo thứ tự đặt (1-1, 2-2, ...). Đổi số cặp ở cột phải.
- **Phá được** (cột phải, mục ĐANG CHỌN của một vật che chắn): bật để vật đó trong game có **HP** riêng, **nháy sáng** khi trúng đòn (shader `JungleMaze/Sprite Hit Flash`) và **rơi loot** khi vỡ theo bảng **Loot** chọn (Bình / Rương / Tiếp tế). Vật phá được không còn là hard cover: ghi vào `worldObjects` với `kind` = bảng loot, `hp`, `art` = tên vật, bounds = cả footprint (dễ đánh trúng). Rương/bình/thùng tiếp tế cũng chỉnh được HP. HP 0 = HP mặc định của loại (`world_settings.json`). Trên map, vật phá được có nhãn "Vỡ <hp>"; Ctrl+D nhân bản giữ nguyên thiết lập. Hash map chỉ đổi khi có `hp` khác 0, nên map cũ giữ nguyên hash.

> **Trong trận** `LevelArtView` (Game scene, `Scripts/Presentation/World/LevelArtView.cs`) đọc khối `editor` của `map_demo.json` và vẽ giống Level Editor: ground cỏ theo seed, đường đá, viền/tường mép, cỏ rải, hình Cainos của cover/decor (Y-sort theo chân, material Unlit như editor). Nó hiện khi chơi offline, hoặc khi phòng mạng chọn đúng map có cùng hash với `map_demo.json`; map khác giữ cách vẽ cũ. Vật thể game (rương, bình, bụi, portal, cửa, hazard) vẫn dùng art game theo trạng thái. Đổi `map_demo.json` thì phải dựng lại collider `Obstacles` trong `Game.unity` cho khớp từng collider chân (tên = type), nếu không màn Game báo lệch vật cản.

## 5. File JSON

Định dạng **giống hệt `Backend/Shared/GameData/map_demo.json`**. Client (`WorldConfigReader`) và GameServer (`MatchRuntime.World`) đọc thẳng được qua `AuthoredMapDocument`. Phần riêng của editor nằm trong `editor`: `seed`, `decor`, `path` (các ô đường đá) và `placements` (vật theo ô: type, ô, lật, cặp portal). Khi mở lại, editor đọc `editor.placements`. Game bỏ qua toàn bộ khối này.

- **Mở map game**: mở `map_demo.json`. Vật của map cũ **lệch ô** (viền tím) giữ nguyên chỗ, chỉ vào ô khi bạn kéo hoặc dịch nó. Không tự snap hàng loạt, vì snap có thể làm map hỏng luật (spawn quá gần, vật chồng nhau).
- **Lưu / Lưu thành…**: mặc định vào `Assets/_JungleMaze/Levels/`. Danh sách file ở cột phải, click để mở.
- **Kiểm tra**: chạy đúng đường game đọc map (`AuthoredMapDocument.Build`). Bắt buộc map **vuông**, gốc (0,0), **đúng 10 spawn** cách nhau ≥ 6, nằm trong bo đầu, không bị chặn, đi tới được nhau. Không vật thể nào đè nhau, portal đủ cặp. Hợp lệ thì hiện hash của map.
- Lưu không bắt buộc hợp lệ (cho phép lưu bản dở). Thanh trạng thái báo map đã dùng được cho game hay chưa.

**Đưa map vào phòng mạng:** dùng quy trình **Export Published Map → validate → publish → reload** ở đầu tài liệu; không ghi đè `map_demo.json`, không cần build lại server/client khi chỉ thay dữ liệu được hỗ trợ. **Đổi map mặc định offline đóng gói:** lưu vào `Backend/Shared/GameData/map_demo.json`, chạy **Jungle Maze → Phase 2 → Sync Runtime Data**, rồi build lại client. Thao tác offline này không thay lựa chọn map của phòng mạng.

## 6. Mã nguồn

| File | Vai trò |
|---|---|
| `Scripts/LevelEditor/LevelDocument.cs` | Model theo ô, collider chân, đọc/ghi JSON, kiểm tra (C# thuần, test được) |
| `Scripts/LevelEditor/LevelGround.cs` | Cỏ theo seed, auto-tile đường đá, tường mép, cỏ rải (C# thuần, test được) |
| `Scripts/LevelEditor/LevelHistory.cs` | Undo/redo bằng ảnh chụp JSON |
| `Scripts/LevelEditor/LevelCatalog.cs` | ScriptableObject: vật thể (footprint, collider, mẫu hình), lưới tile cỏ/tường, sprite cỏ rải |
| `Scripts/LevelEditor/LevelEditorController*.cs` | Input/camera, hiển thị (Tilemap, sprite con của prefab, bóng, viền, ghost, lưới), giao diện IMGUI + file |
| `Tools/Editor/LevelEditor/LevelEditorBuild.cs` | Đọc prefab Cainos → catalog, sinh scene |
| `Tests/EditMode/LevelEditor/LevelDocumentTests.cs` | Mở `map_demo.json` → lưu lại → **cùng hash game**; cặp portal; luật kiểm tra |
| `Tests/EditMode/LevelEditor/LevelGridTests.cs` | Collider chân → obstacles, lật, khối editor, chiếm ô, map theo ô qua Validate |
| `Tests/EditMode/LevelEditor/LevelGroundTests.cs` | Tất định theo seed, cụm hoa, đường đá tự nối, tường 2 hàng |

Assembly `JungleMaze.LevelEditor` chỉ phụ thuộc `JungleMaze.World` và Newtonsoft, không đụng vào gameplay.

> Project còn một bản Cainos **1.0.1 cũ** ở `Imported/JSurviv/.../Cainos` (PPU 64, team đã chỉnh). Bản đó giữ nguyên. **Không** import file `.unitypackage` Cainos bằng Assets → Import Package: gói này chứa cả `ProjectSettings` và sẽ ghi đè tags/layers/sorting layers/Graphics của project.
