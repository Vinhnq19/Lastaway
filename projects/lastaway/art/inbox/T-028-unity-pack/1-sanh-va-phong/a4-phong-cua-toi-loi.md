# a4-phong-cua-toi-loi (T-024)

Ảnh mock: `T-021/a/a4-phong-cua-toi-loi.png` (1920x1080). Border 9-slice là của sprite @1x (LRTB); @2x nhân đôi.

## Khung, icon, FX, nền

| Phần tử mock | File nguồn | Cỡ vẽ 1080p | Border 9-slice LRTB | @1x / @2x | Số lần | Ghi chú |
|---|---|---:|---|---|---:|---|
| nền | `files/bg/nen-gach-tile.png` | 510x280 | không (không cắt) | 1x | 1 | ô nền gạch lát 510x280 lát kín 1920x1080, phủ tối alpha 0.7 + vignette (gradient chạy lúc render) |
| khung/thành phần kit | `T-020/kit_screen_header.png` | 1800x104 | [36, 36, 12, 12] | có / có | 1 | thanh tiêu đề màn |
| khung/thành phần kit | `T-020/kit_iconbtn_square_normal.png` | 64x64 | không (không cắt) | có / có | 1 | nút quay lại |
| icon | `T-020/kit_ico_arrow_left.png` | 40x40 | không (không cắt) | có / có | 1 | icon ink tối trên nút quay lại |
| khung/thành phần kit | `T-020/kit_toast.png` | 1680x72 | [22, 22, 22, 22] | có / có | 1 |  |
| icon | `T-020/kit_icon_cross.png` | 28x28 | không (không cắt) | có / có | 1 |  |

## Nút

Sprite `T-020/kit_btn_<loại>_<trạng thái>.png` (+ `@2x`), 9-slice [22,22,28,28]; chữ Be Vietnam Pro Bold, icon trái cao bằng cỡ chữ, gap 10 px, căn giữa mặt nút (trừ đế).

| Loại | Trạng thái | Cỡ nút | Cỡ chữ | Chữ | Icon |
|---|---|---:|---:|---|---|
| secondary | normal | 220x52 | 24 | Thử lại | kit_ico_refresh |
| primary | normal | 280x72 | 30 | Tạo phòng | kit_ico_plus |

## Chữ (không phải asset: render bằng font)

Thang 5 bậc; bậc 96 và 40 dùng Chakra Petch Bold, còn lại Be Vietnam Pro Bold (chữ nút luôn Be Vietnam Pro). Viền chữ max(2, em/12) px màu #0A0C0C.

| Chữ | Cỡ | Font | Màu |
|---|---:|---|---|
| Phòng của tôi | 40 | Chakra Petch Bold | #FFDC6E |
| 2 phòng bổ sung tối đa | 24 | Be Vietnam Pro Bold | #D6DAD6 |
| Không tải được danh sách phòng. Kiểm tra máy chủ rồi thử lại | 24 | Be Vietnam Pro Bold | #EDF2EE |
| Thử lại | 24 | Be Vietnam Pro Bold | #1F1A10 |
| Tạo phòng | 30 | Be Vietnam Pro Bold | #1F1A10 |