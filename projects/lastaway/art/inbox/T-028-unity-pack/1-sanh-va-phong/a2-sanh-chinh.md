# a2-sanh-chinh (T-024)

Ảnh mock: `T-021/a/a2-sanh-chinh.png` (1920x1080). Border 9-slice là của sprite @1x (LRTB); @2x nhân đôi.

## Khung, icon, FX, nền

| Phần tử mock | File nguồn | Cỡ vẽ 1080p | Border 9-slice LRTB | @1x / @2x | Số lần | Ghi chú |
|---|---|---:|---|---|---:|---|
| nền | `files/bg/nen-gach-tile.png` | 510x280 | không (không cắt) | 1x | 1 | ô nền gạch lát 510x280 lát kín 1920x1080, phủ tối alpha 0.62 + vignette (gradient chạy lúc render) |
| logo | `Assets/_JungleMaze/Art/UI/Menu/Logo.png (crop 76,68,952,184)` | 760x101 | không (không cắt) | - | 1 | logo pixel LASTAWAY, đã có trong Assets |
| khung/thành phần kit | `T-020/kit_toast.png` | 404x96 | [22, 22, 22, 22] | có / có | 1 |  |
| khung/thành phần kit | `T-020/kit_iconbtn_round_normal.png` | 48x48 | không (không cắt) | có / có | 1 | nút tròn kaki làm nền icon |
| icon | `T-020/kit_ico_person.png` | 28x28 | không (không cắt) | có / có | 1 | icon ink tối (đặt lên nền kaki) |
| icon | `T-020/kit_icon_check.png` | 28x22 | không (không cắt) | có / có | 1 |  |

## Nút

Sprite `T-020/kit_btn_<loại>_<trạng thái>.png` (+ `@2x`), 9-slice [22,22,28,28]; chữ Be Vietnam Pro Bold, icon trái cao bằng cỡ chữ, gap 10 px, căn giữa mặt nút (trừ đế).

| Loại | Trạng thái | Cỡ nút | Cỡ chữ | Chữ | Icon |
|---|---|---:|---:|---|---|
| secondary | normal | 180x48 | 24 | Đăng xuất | kit_ico_logout |
| primary | normal | 420x80 | 40 | Vào phòng | kit_ico_play |
| secondary | normal | 420x80 | 40 | Tạo phòng | kit_ico_plus |
| secondary | normal | 420x80 | 40 | Lịch sử | kit_ico_clock |
| secondary | normal | 420x80 | 40 | Bảng xếp hạng | kit_ico_trophy |
| secondary | normal | 420x80 | 40 | Cài đặt | kit_ico_gear |
| secondary | normal | 420x80 | 40 | Thoát | kit_ico_door |

## Chữ (không phải asset: render bằng font)

Thang 5 bậc; bậc 96 và 40 dùng Chakra Petch Bold, còn lại Be Vietnam Pro Bold (chữ nút luôn Be Vietnam Pro). Viền chữ max(2, em/12) px màu #0A0C0C.

| Chữ | Cỡ | Font | Màu |
|---|---:|---|---|
| Đã đăng nhập | 20 | Be Vietnam Pro Bold | #D6DAD6 |
| huycv | 30 | Be Vietnam Pro Bold | #FFDC6E |
| Đăng xuất | 24 | Be Vietnam Pro Bold | #1F1A10 |
| Vào phòng | 40 | Be Vietnam Pro Bold | #1F1A10 |
| Tạo phòng | 40 | Be Vietnam Pro Bold | #1F1A10 |
| Lịch sử | 40 | Be Vietnam Pro Bold | #1F1A10 |
| Bảng xếp hạng | 40 | Be Vietnam Pro Bold | #1F1A10 |
| Cài đặt | 40 | Be Vietnam Pro Bold | #1F1A10 |
| Thoát | 40 | Be Vietnam Pro Bold | #1F1A10 |
| API sẵn sàng | 20 | Be Vietnam Pro Bold | #EDF2EE |
| v0.2 | 20 | Be Vietnam Pro Bold | #D6DAD6 |

## Asset mới T-028 đợt 3 dùng cho màn này

| Chỗ | File | Cỡ vẽ 1080p |
|---|---|---:|
| nút Luyện tập offline / Lưu endpoint / Xem online / Điền đủ (unity thêm, mock chưa có) | `files/new/kit_ico_practice`, `kit_ico_save`, `kit_ico_globe`, `kit_ico_fill` (+ @2x), icon mục tối 48x48 cao bằng cỡ chữ | cao 24 (nút S) hoặc 30 (nút M) |