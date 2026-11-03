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

AWS đã chuẩn bị bind mount mặc định `/opt/junglemaze/data/maps`; đợt này chưa deploy AWS. Khi triển khai sau, chuyển gói vào thư mục staging riêng trên EC2, validate/publish bằng container cùng phiên bản rồi reload nội bộ. Giữ volume Mongo, secret và map cũ. Trước khi đổi sang client mới phải deploy server gameplay protocol **2**; handshake tài khoản vẫn **v4**.

Kiểm hai PC: chạy client cùng bản trên hai máy, trỏ cùng API HTTPS/server LAN hoặc WSS, dùng hai tài khoản. Đổi map và đối chiếu preview, hash trong lobby, cover/spawn/vật thể trong trận; thử rời lúc countdown, lỗi tải rồi retry, kết thúc/chơi lại. Hai process cùng PC chỉ xác minh hai client, chưa nghiệm thu hai PC vật lý.

Cập nhật **03/11/2026**. Scene `Assets/_JungleMaze/Scenes/LevelEditor.unity`, chạy bằng **Play** trong Unity Editor.

## 1. Mở

1. Chỉ khi chưa có scene/catalog (lệnh này ghi lại các asset; giữ bản sửa thủ công trước khi chạy): menu **Jungle Maze → Level Editor → Build Level Editor Scene**. Lệnh này sinh lại `Art/LevelEditor/LevelEditorCatalog.asset` và scene, chạy lại nhiều lần không tạo trùng.
2. **Jungle Maze → Level Editor → Open Level Editor Scene**, rồi bấm **Play**.

Scene này **không** tạo Bootstrap của game (không menu kết nối, HUD hay network).

## 2. Giao diện

```
┌ Mới · Mở… · Mở map game · Lưu · Lưu thành… · Hoàn tác · Làm lại · Kiểm tra · Snap · Viền ─── ● tên file ┐
│ BẢNG VẬT THỂ   │                                                        │ MAP: Rộng/Cao → Tạo ground   │
│ Che chắn       │            map: Tilemap ground W×H                     │ Phiên bản, ground tile       │
│ Vật thể game   │            vật thể đặt tự do                           │ ĐANG CHỌN: scale/xoay/lật    │
│ Trang trí      │            P1/P2.. = cặp portal, S1..S10 = spawn       │ KIỂM TRA THEO LUẬT GAME      │
│ Spawn          │                                                        │ FILE TRONG Levels/ · phím tắt│
└ chế độ · toạ độ con trỏ · kích thước map · thông báo ──────────────────────────────────────────────────┘
```

## 3. Thao tác

| Thao tác | Kết quả |
|---|---|
| Click một vật thể ở cột trái | Vào chế độ **Đặt** với vật thể đó (click lại để thôi) |
| **Chuột trái** (chế độ Đặt) | Đặt tại con trỏ, vị trí tự do, không theo cell. Bóng xem trước **đỏ** khi game sẽ không cho phép (đè vật khác, spawn bị chặn) |
| **Chuột trái** (chế độ Chọn) | Chọn vật thể trên cùng, giữ để kéo |
| **Chuột phải** | Xóa vật thể dưới con trỏ |
| **Lăn chuột** | Tăng/giảm kích thước (×1.1 mỗi nấc, 0.25–8) của bút hoặc vật đang chọn |
| **Shift** | Lật ngang (scale x = -1) bút hoặc vật đang chọn |
| **R** | Xoay 90° (footprint đổi rộng↔cao; ví dụ Tường thành nằm ngang) |
| **Alt + kéo chuột trái** | Di chuyển camera (giữ chuột giữa hoặc WASD cũng được) |
| **Alt + lăn chuột**, `+`/`-` | Phóng to / thu nhỏ quanh con trỏ |
| **F** | Xem toàn map |
| Q / Esc | Về chế độ Chọn |
| Del / Backspace | Xóa vật đang chọn |
| Mũi tên | Dịch vật đang chọn 0.1 |
| G | Bật/tắt snap 0.5 |
| H | Bật/tắt viền footprint |
| Ctrl+Z / Ctrl+Y (Ctrl+Shift+Z) | Hoàn tác / làm lại (200 bước) |
| Ctrl+S | Lưu |
| Ctrl+D | Nhân bản vật đang chọn (giữ scale/xoay/lật) |

> Trong Play mode, Unity Editor có thể bắt Ctrl+Z/Ctrl+S trước Game view. Nếu phím tắt không ăn, dùng nút trên thanh trên.

Ground: nhập **Rộng/Cao** rồi bấm **Tạo ground**. Tilemap 1 ô = 1 world unit, đúng như map trong game. Có hai tile: `Ground96`, `Cell48`.

## 4. Vật thể (theo GDD sheet Obstacles)

| Nhóm | Vật thể | Size gốc | Ghi ra JSON |
|---|---|---|---|
| Che chắn | Cột đá cổ đại, Tường thành, Tảng đá lớn, Tượng cổ | 1×2, 1×4, 4×4, 3×3 | `obstacles` (name = AncientPillar, CityWall, BoulderRock, AncientStatue) |
| Vật thể game | Rương gỗ, Chum gốm, Tiếp tế, Bụi rậm, Cửa đá, Portal, Đầm lầy, Lava, Ổ gai | 1, 1, 2, 1.5, 1×2, 1, 4, 3, 1 | `worldObjects` (kind = Chest, Pot, Supply, Bush, Door, Portal, Swamp, Lava, Poison) |
| Trang trí | Sông, Thảm hoa / nấm | 4×1, 1×1 | `decorations`: **game chưa hỗ trợ, bỏ qua** |
| Spawn | Điểm spawn | Ø 0.8 | `spawnPoints` |

Portal tự ghép cặp theo thứ tự đặt (1-1, 2-2, ...). Đổi số cặp ở cột phải.

Số liệu và sprite nằm trong `LevelEditorCatalog.asset`. Bản catalog này lấy từ GDD local ngày 25/10. Nếu sheet online đổi size, sửa `LevelEditorBuild.BuildCatalog` rồi build lại.

## 5. File JSON

Định dạng **giống hệt `Backend/Shared/GameData/map_demo.json`**. Client (`WorldConfigReader`) và GameServer (`MatchRuntime.World`) đọc thẳng được qua `AuthoredMapDocument`. Field phụ của editor nằm trong từng vật thể (`scale`, `rotation`, `flipX`), cộng thêm `decorations` và `editor.groundTile`. Game bỏ qua các field này.

- **Mở map game**: mở thẳng `map_demo.json` để sửa.
- **Lưu / Lưu thành…**: mặc định vào `Assets/_JungleMaze/Levels/`. Danh sách file ở cột phải, click để mở.
- **Kiểm tra**: chạy đúng đường game đọc map (`AuthoredMapDocument.Build`). Bắt buộc map **vuông**, gốc (0,0), **đúng 10 spawn** cách nhau ≥ 6, nằm trong bo đầu, không bị chặn, đi tới được nhau. Không vật thể nào đè nhau, portal đủ cặp. Hợp lệ thì hiện hash của map.
- Lưu không bắt buộc hợp lệ (cho phép lưu bản dở). Thanh trạng thái báo map đã dùng được cho game hay chưa.

**Đưa map vào phòng mạng:** dùng quy trình **Export Published Map → validate → publish → reload** ở đầu tài liệu; không ghi đè `map_demo.json`, không cần build lại server/client khi chỉ thay dữ liệu được hỗ trợ. **Đổi map mặc định offline đóng gói:** lưu vào `Backend/Shared/GameData/map_demo.json`, chạy **Jungle Maze → Phase 2 → Sync Runtime Data**, rồi build lại client. Thao tác offline này không thay lựa chọn map của phòng mạng.

## 6. Mã nguồn

| File | Vai trò |
|---|---|
| `Scripts/LevelEditor/LevelDocument.cs` | Model + đọc/ghi JSON + kiểm tra (C# thuần, test được) |
| `Scripts/LevelEditor/LevelHistory.cs` | Undo/redo bằng ảnh chụp JSON |
| `Scripts/LevelEditor/LevelCatalog.cs` | ScriptableObject danh sách vật thể, ground tile |
| `Scripts/LevelEditor/LevelEditorController*.cs` | Input/camera, hiển thị (Tilemap, sprite, viền, ghost), giao diện IMGUI + file |
| `Tools/Editor/LevelEditor/LevelEditorBuild.cs` | Sinh catalog + scene |
| `Tests/EditMode/LevelEditor/LevelDocumentTests.cs` | Mở `map_demo.json` → lưu lại → **cùng hash game**; xoay/scale/lật; cặp portal; luật kiểm tra |

Assembly `JungleMaze.LevelEditor` chỉ phụ thuộc `JungleMaze.World` và Newtonsoft, không đụng vào gameplay.
