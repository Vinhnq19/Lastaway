# Level Editor — chuyển sang bộ art Cainos, đặt theo ô, decor theo seed

Ngày: 2026-11-03 · Phạm vi: chỉ tool Level Editor (scene `LevelEditor`) · Ảnh tham chiếu: `Scene Overview.png` của bộ Cainos.

## 1. Mục đích

Tool xếp map hiện vẽ vật thể bằng sprite tạm, đặt ở vị trí thực bất kỳ, scale/xoay tự do. Map xếp ra trông lộn xộn và không giống game top-down thật. Mục tiêu là xếp được map trông như ảnh overview Cainos: nền cỏ có cỏ và hoa rải tự nhiên, đường đá lát, vật cản đúng art, mép map có tường gạch dày. Mọi thứ đặt theo ô.

Map xuất ra vẫn là file game/server đang đọc (`AuthoredMapDocument`), và nút "Kiểm tra" vẫn chạy đúng đường game build map.

## 2. Mục tiêu (user yêu cầu)

1. Import bộ **Pixel Art Top Down - Basic v1.2.3** (Cainos, `C:\Users\HVU\Downloads\Pixel Art Top Down - Basic v1.2.3\…unitypackage`) vào project. Bỏ toàn bộ vật cản (cover) cũ, thay bằng vật cản Cainos.
2. Collider của vật cản chỉ là **phần chân** (gốc cây, đế tượng, chân đá…), không phủ hết sprite. Phần sprite còn lại (tán cây, thân tượng) chỉ là hình.
3. Bấm "Tạo ground" thì **cỏ/hoa decor sinh theo seed**. Có ô nhập seed trên giao diện. Cùng seed + cùng map thì ra đúng cùng kết quả.
4. Có thể trang trí bằng **đường đá** lát trên ground và một số vật decor khác. Tất cả đi xuyên qua được.
5. Khi tạo ground thì có **lớp tường gạch dày dưới chân ground** (mép nam) như ảnh overview.
6. ~~Terrain 4×4 tự nối + thang~~: **bỏ** (user đã bỏ terrain; thang chỉ để nối terrain nên bỏ theo).
7. **Mọi thứ đặt theo ô**, không đặt tự do nữa.

## 3. Không làm (non-goals)

- Terrain nhiều tầng, thang, luật khác tầng.
- **Hiển thị art Cainos trong trận.** Đợt này game vẫn vẽ như cũ. Hệ quả đã biết: trong trận, vật cản Cainos hiện là khối cover cỡ collider chân (rất nhỏ so với hình). Phần vẽ map trong trận là việc riêng sau.
- Sửa server, protocol hay `AuthoredMapDocument`.
- **Làm lại map đang chạy** (`map_demo.json` ở StreamingAssets và Backend/Shared/GameData): giữ nguyên (user chốt). Map mới xếp bằng tool lưu trong `Levels/`.
- Tileset "Stone Ground" (sân lát phiến lớn). Có thể thêm cọ thứ hai sau.

## 4. Không được xảy ra (anti-goals)

- File xuất ra mà game/server không đọc được, hoặc "Kiểm tra" báo hợp lệ trong khi game build lỗi.
- Decor/đường đá lọt vào `obstacles` hoặc `worldObjects` (thành vật chặn trong trận).
- Cùng seed + cùng map mà decor ra khác (khi mở lại file, hoặc khi sửa một chỗ khác trên map).
- Hỏng file cũ: mở `map_demo.json` gốc vẫn phải được (vật thể không còn trong catalog vẫn hiện và giữ nguyên bounds).

## 5. Ràng buộc đã biết

- Unity 6000.3.15f1, URP 2D. Bộ Cainos có patch `Patch - URP 2D Lit - Unity 6.3.unitypackage`, phải import kèm.
- Sprite Cainos có **PPU 32**, 1 tile 32 px = 1 world unit = 1 ô map, khớp Tilemap hiện có.
- Game coi mọi `obstacles[].name` lạ là `WorldObjectKind.Wall` (hard cover). Mọi loại hard cover có hành vi như nhau, nên đặt tên mới thoải mái, không cần đổi shared code.
- Validate của game cấm **mọi** object chồng nhau (cover và world object) và yêu cầu 10 spawn thông nhau theo lưới ô với bán kính hero 0.4.
- Prefab Cainos có sẵn collider ở chân: ví dụ Tree 01 có `BoxCollider2D` 0.27×0.19 ở gốc, Statue có `PolygonCollider2D`. Vài prefab có nhiều collider (cổng đá Struct Gate = hai chân cột).
- Prefab Cainos gồm nhiều sprite con: thân, tán, bóng (bóng dùng shader `SH Sprite Shadow`), và dùng sorting layer "Layer 1/2" của Cainos mà project không có.

## 6. Thiết kế (đã chốt với user)

### 6.1 Đặt theo ô
- Vị trí mọi vật = **ô nguyên** (cellX, cellY). Mỗi loại có **footprint chiếm ô** (số nguyên, ví dụ cây 1×1 tại gốc, giếng 3×2). Một ô chỉ chứa một vật chặn.
- Bỏ scale bằng lăn chuột, bỏ xoay R, bỏ toggle snap 0.5. Vật có nhiều hướng thì là nhiều mục riêng trong catalog: ghế đá E/S/W, quan tài H/V, biển báo E/W.
- Spawn đặt ở tâm ô.
- Kéo/dịch bằng mũi tên đi theo bước 1 ô.

### 6.2 Collider chân
- Collider xuất ra JSON lấy từ collider của prefab Cainos (AABB của từng Collider2D, theo pivot prefab), làm tròn tới 1/32. Collider phải nằm gọn trong footprint ô.
- Prefab nhiều collider → nhiều obstacle trong JSON.
- Vật không có collider trong prefab thì catalog nhập collider tay.

### 6.3 Decor cỏ theo seed
- Ô nhập **Seed** (số nguyên) + nút "Ngẫu nhiên". Lưu seed vào file.
- Nền: mỗi ô chọn một biến thể tile cỏ Cainos (cỏ trơn, cỏ có ngọn, hoa, sỏi). Hoa và sỏi mọc thành cụm tự nhiên, không rải đều.
- Kết quả của mỗi ô chỉ phụ thuộc (seed, x, y), không phụ thuộc thứ tự duyệt, nên sửa map không làm cỏ ở chỗ khác đổi theo.
- Có thể thêm lớp sprite cỏ nhỏ (Grass 01–15, Flower 01) rải theo seed, tránh ô có vật chặn, đường đá và spawn.

### 6.4 Đường đá và decor đi xuyên
- Công cụ **Đường đá**: kéo chuột trái để tô ô, chuột phải để xoá.
- Ô đá dùng các mảnh đá lát trong `TX Tileset Grass` (mỗi tile 32 px gồm 4 viên 16 px). Ô có đá lát kín. Ô cỏ sát đường đá được rải viên đá lẻ ở phía giáp đường, chọn theo seed, nên viền đường tự nối và trông vỡ vụn tự nhiên như ảnh overview.
- Đường đá và decor props (đá nhỏ Stone 02–07, gạch vụn Brick 01–03, cỏ, hoa) **đi xuyên** được. Chúng ghi vào phần editor / `decorations`, không bao giờ vào `obstacles`/`worldObjects`.

### 6.5 Tường mép map
- Sau khi tạo ground: viền đá mảnh quanh mép T/B/Đ (tile viền của `TX Tileset Wall`) và **mặt tường gạch dày 2 ô ngay dưới mép nam** (y < 0). Cửa sổ gạch xen theo seed.
- Chỉ là hình, nằm ngoài vùng chơi, không ghi vào JSON ngoài seed.

### 6.6 Lưới
- Giữ lưới ô đã thêm (phím L / ô "Lưới").

## 7. Catalog vật thể (quyết định của Claude, user duyệt khi đọc)

| Nhóm | Mục | Ghi chú |
|---|---|---|
| Vật cản (cover) | Tree 01–03, Statue 01, Pillar 01–02, Rune Pillar X2/X3/Broken, Gravestone 01–03, Stone Cube, Stone Lantern, Well, Stone Bench E/S/W, Stone Coffin H/V, Barrel, Crate 02, Stone 05–06, Stone 07 (tảng lớn), Struct Gate 01–02 | Collider chân từ prefab |
| Vật thể game | Chest → Cainos Chest · Pot → Pot 01–03 · Bush → Bush 01–06 · Door → Wooden Gate | **Bounds = footprint ô nguyên** (user chốt "làm tròn theo ô"): rương/bình 1×1; bụi 1×1 hoặc 2×2 tuỳ cỡ art (bụi là vùng nấp nên phủ cả bụi); cửa 2×1 hoặc 1×2 tuỳ hướng art. Kích thước GDD cũ (bụi 1.5×1.5, cửa 1×2) không còn ràng buộc |
| Vật thể game đổi art (user: "đổi sang hoặc bỏ đi") | Supply → Crate 01 · Portal → Altar 01 (bệ rune) | Crate 01 và Altar không còn trong nhóm cover. Bounds theo ô |
| Bỏ khỏi catalog | Swamp, Lava, Poison | Cainos không có art. Không còn sprite cũ nào trong tool. File cũ có các vật này vẫn mở được (hiện thành khối giữ bounds) |
| Decor đi xuyên | Stone 01–04, Brick 01–03, Grass 01–15, Flower 01, Road Sign E/W | Ghi vào `decorations` |
| Spawn | Spawn | Như cũ |

## 8. Định dạng file

- Giữ nguyên các mảng game đọc: `mapBounds`, `obstacles` (một phần tử cho mỗi collider), `spawnPoints`, `worldObjects`, `decorations`.
- Khối `editor` thêm: `seed`, danh sách `placements` theo ô (type, cellX, cellY, flipX) làm nguồn sự thật khi editor mở lại file, và `path` (các ô đường đá).
- File không có `editor.placements` (ví dụ `map_demo.json` cũ): đọc từ các mảng game như hiện nay. **Quyết định của Claude:** vật lệch ô (spawn toạ độ lẻ, 211 world object, cover cũ) giữ nguyên bounds gốc, đánh dấu "lệch ô", không tự snap khi mở (snap hàng loạt có thể làm Validate hỏng: spawn quá gần, chồng nhau). Vật chỉ snap vào ô khi user kéo/dịch nó. Vì vậy một placement trong `editor.placements` có hai dạng: theo ô (cellX, cellY) hoặc bounds thô (minX..maxY) cho vật lệch ô. Vật không có trong catalog hiện thành khối giữ đúng bounds.

## 9. Để builder tự quyết

- Cách vẽ prefab trong editor: instance prefab rồi gỡ collider/script, hoặc tự dựng từ sprite con; xử lý material bóng; sắp thứ tự vẽ theo Y.
- Thuật toán nhiễu cho cụm hoa, mật độ, bảng ánh xạ 4 góc → sprite đá lát (phải đo từ pixel thật, không đoán bằng mắt).
- Footprint ô cụ thể của từng mục.
- Cách lưu `path` gọn (danh sách ô hoặc RLE).
- Test EditMode: tính tất định của decor theo seed, ánh xạ auto-tile đường đá, xuất/đọc lại file, Validate vẫn qua.
