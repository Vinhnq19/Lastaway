# T-028 gói asset cho Unity (T-022 đến T-025)

Bảng ánh xạ từng phần tử mock T-021 sang file PNG, cho vai unity. Chỉ đọc; không sửa `Assets/`, không chạy Unity, chưa commit.

## Quy ước
- Cỡ, toạ độ trong bảng là **1080p (1920x1080)**. Unity (CanvasScaler tham chiếu 1280x720, match width) quy đổi chia 1,5.
- Border 9-slice là LRTB của sprite @1x (px ảnh gốc); sprite @2x có border gấp đôi. Sprite có border `không` thì không cắt (icon, ô vật phẩm).
- Chữ không phải asset: bậc 96 và 40 Chakra Petch Bold, bậc 30, 24, 20 và mọi chữ nút Be Vietnam Pro Bold, viền chữ max(2, em/12) px #0A0C0C (xem `T-020/ui-kit.md` mục 13). Mỗi bảng màn có mục Chữ liệt kê chữ, cỡ, font, màu.
- Nút: sprite `kit_btn_<primary|secondary|danger>_<normal|hover|pressed|disabled>` 9-slice [22,22,28,28]; chữ tối #1F1A10, icon trái cao bằng cỡ chữ, gap 10 px, căn giữa mặt nút (trừ đế); cỡ nút S 48 (chữ 24), M 64 (30), L 80 (40).
- Không còn phần tử "vẽ bằng mã" trong 36 bảng: lớp phủ tối toàn màn dùng `kit_dim` (kéo giãn, tint alpha); kill feed, nhãn phím, nền/lưới/vòng bo bản đồ lớn dùng asset gen đợt 2 (`files/new/`). Bản đồ thật vẽ lúc chạy trên nền `kit_map_bg`.
- Nguồn file: kit ở `files/kit/` (bản sao `T-020/`, kèm `meta.json`), FX ở `files/fx/` (nét cũ, Huy giữ đỏ cho cung sát thương và vignette), icon vật phẩm ở `files/items/` (bản sao từ `Assets/_JungleMaze/Art/UI/Icons/`, đã có trong Assets, dùng đường dẫn gốc), nền gạch `files/bg/`, asset gen mới `files/new/`.
- Bảng tổng và chỗ dùng: `bang-tong.md`. Thư mục `src/` có script dựng lại (`map_screens.py`, `build_pack.py`, `import_gen.py`) và prompt gen.

## Thứ tự và nhóm (mỗi file `.md` là một mock)

| Nhóm | Task | Mock |
|---|---|---|
| `1-sanh-va-phong/` | T-024 (làm trước) | a1 khởi động, a2 sảnh chính, a3 vào phòng (+ lỗi, đang kết nối), a3b đăng nhập (+ lỗi), a4 phòng của tôi (+ trống, lỗi), a4 xóa phòng, a5 tạo phòng (+ lỗi), a6 phòng chờ (chờ đủ người, đếm ngược) |
| `2-hud-balo-ban-do/` | T-022 | b9 phản hồi HUD (+ máu thấp), b4 túi đồ (+ tooltip, trống), b5 bản đồ lớn |
| `3-esc-spectator/` | T-023 | b1 ESC cài đặt (+ xác nhận thoát), b3 spectator (+ chuyển, hết người), b2 bị loại (+ bo thu, hết người) |
| `4-loading-mat-ket-noi-ket-qua/` | T-025 | a7 đang tải (+ lỗi), b8 mất kết nối (thử lại, thất bại, hết hạn), b6 thắng, b7 thua |

## Asset gen mới (qua cổng duyệt của CPO)
| File | Dùng cho | Cỡ |
|---|---|---|
| `files/new/icon_ammo.png` (+ @2x) | dòng Đạn cột Trang bị ở B4 (mock dùng ô giữ chỗ) | 64x64, vẽ 72 |
| `files/new/icon_ground.png` (+ @2x) | tiêu đề cột trái "đồ ở gần" ở B4 (mock dùng `kit_ico_list` tạm) | 64x64, vẽ 30 trên nút tròn |
| `files/new/kit_badge_key.png` (+ @2x) | nhãn số phím quick bar (mock vẽ ô bo 30x30 bằng mã) | 48x48, border [14,14,14,14], vẽ 30x30 |

| `files/new/kit_killfeed_panel.png` (+ @2x) | panel kill feed | 192x64, border [16,16,16,16], vẽ rộng theo chữ |
| `files/new/kit_map_bg.png` (+ @2x) | nền khung bản đồ lớn | 256x256, border [24,24,24,24], vẽ 840x840 |
| `files/new/map_grid.png` (+ @2x) | lưới 10x10 trên bản đồ (v2: gen 1 ô, script lát 10x10 ô 83 px, lề 5 px khớp vùng trong `kit_map_bg`) | 840x840, nền trong suốt |
| `files/new/map_ring_zone.png` (+ @2x) | vòng bo hiện tại (vàng cam; v2 nét ~4 px ở cỡ vẽ 680) | 512x512, vẽ 680x680 |
| `files/new/map_ring_safe.png` (+ @2x) | vòng bo tiếp theo (trắng) | 256x256, vẽ 180x180 |
| `files/new/legend_ring_next_dashed.png` (+ @2x) | chú giải vòng bo kế tiếp (vòng trắng nét đứt 12 đoạn, nền trong suốt) | 32x32 |
Trang duyệt đợt 2: `files/new/duyet-dot2.png` (asset mới đặt trong mock ở cỡ 100%).

### Đợt 3 (unity báo thiếu icon, mục "Cần art-ux" của `docs/COORDINATION.md`)
| File | Dùng cho | Cỡ |
|---|---|---|
| `files/new/kit_ico_practice.png` (+ @2x) | nút Luyện tập offline (bia tập bắn có mũi tên) | 48x48 ink tối |
| `files/new/kit_ico_save.png` (+ @2x) | nút Lưu endpoint (đĩa mềm) | 48x48 ink tối |
| `files/new/kit_ico_globe.png` (+ @2x) | nút Xem online (quả địa cầu) | 48x48 ink tối |
| `files/new/kit_ico_fill.png` (+ @2x) | nút Điền đủ (nhóm 3 người và dấu cộng) | 48x48 ink tối |
| `files/new/icon_zone_cause.png` (+ @2x) | nguyên nhân bị loại Bo thu (thẻ `kit_card_cause`; v2: khói bo đỏ lấn vào, nhân vật nhỏ ngoài vòng) | 64x64 màu, vẽ 60 |
Icon vũ khí cho mục tiêu spectator: không gen, đủ 18 loại có sẵn trong Assets, xem `weapons-icons.md`. Trang duyệt đợt 3: `files/new/duyet-dot3.png`.

## Chưa có file riêng, giữ nguyên
- FX và emblem (hit marker, cung sát thương, mũi tên nguy hiểm, chip hiệu ứng, vignette, huy hiệu Thắng/Thua, marker bản đồ, icon mất kết nối): gen GPT nét cũ, Huy giữ (07/11).
- Logo và nền menu: giữ như cũ (logo `Assets/_JungleMaze/Art/UI/Menu/Logo.png`).
- Mục "Cần art-ux" của file phối hợp đợt unity chưa có khi làm gói này; có thì báo thêm.

## Lưu ý dọn file
`files/items/` còn 18 bản sao không có đuôi `.png` (trùng nội dung với bản `.png` cùng tên, do lần chạy đầu đặt tên thiếu đuôi). Bỏ qua chúng; xóa bằng tay khi tiện (kiểm tra xóa tự động đã chặn lệnh `rm` của phiên này).
