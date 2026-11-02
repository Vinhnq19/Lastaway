# 06 — Level Editor (xếp map)

Cập nhật **03/11/2026**. Scene `Assets/_JungleMaze/Scenes/LevelEditor.unity`, chạy bằng **Play** trong Unity Editor.

## 1. Mở

1. Lần đầu (hoặc sau khi đổi catalog): menu **Jungle Maze → Level Editor → Build Level Editor Scene**. Lệnh này sinh lại `Art/LevelEditor/LevelEditorCatalog.asset` và scene, chạy lại nhiều lần không tạo trùng.
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

**Đưa map vào game:** **Lưu thành…** đè lên `Backend/Shared/GameData/map_demo.json`, rồi chạy **Jungle Maze → Phase 2 → Sync Runtime Data** để client nhận file mới và hash. Server cũng đọc file này nên phải deploy lại server.

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
