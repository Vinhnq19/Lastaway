# a7-dang-tai-loi (T-025)

Ảnh mock: `T-021/a/a7-dang-tai-loi.png` (1920x1080). Border 9-slice là của sprite @1x (LRTB); @2x nhân đôi.

## Khung, icon, FX, nền

| Phần tử mock | File nguồn | Cỡ vẽ 1080p | Border 9-slice LRTB | @1x / @2x | Số lần | Ghi chú |
|---|---|---:|---|---|---:|---|
| nền | `files/bg/nen-gach-tile.png` | 510x280 | không (không cắt) | 1x | 1 | ô nền gạch lát 510x280 lát kín 1920x1080, phủ tối alpha 0.7 + vignette (gradient chạy lúc render) |
| logo | `Assets/_JungleMaze/Art/UI/Menu/Logo.png (crop 76,68,952,184)` | 440x58 | không (không cắt) | - | 1 | logo pixel LASTAWAY, đã có trong Assets |
| khung/thành phần kit | `T-020/kit_panel_s.png` | 560x300 | [47, 47, 47, 47] | có / có | 1 |  |
| icon | `T-020/kit_icon_cross.png` | 30x30 | không (không cắt) | có / có | 1 |  |
| khung/thành phần kit | `T-020/kit_panel_s.png` | 960x200 | [47, 47, 47, 47] | có / có | 1 |  |
| khung/thành phần kit | `T-020/kit_keycap_normal.png` | 107x52 | [14, 14, 14, 14] | có / có | 1 |  |
| khung/thành phần kit | `T-020/kit_keycap_normal.png` | 105x52 | [14, 14, 14, 14] | có / có | 1 |  |
| khung/thành phần kit | `T-020/kit_keycap_normal.png` | 52x52 | [14, 14, 14, 14] | có / có | 2 |  |
| khung/thành phần kit | `T-020/kit_keycap_normal.png` | 80x52 | [14, 14, 14, 14] | có / có | 1 |  |
| khung/thành phần kit | `T-020/kit_toast.png` | 1000x64 | [22, 22, 22, 22] | có / có | 1 |  |

## Nút

Sprite `T-020/kit_btn_<loại>_<trạng thái>.png` (+ `@2x`), 9-slice [22,22,28,28]; chữ Be Vietnam Pro Bold, icon trái cao bằng cỡ chữ, gap 10 px, căn giữa mặt nút (trừ đế).

| Loại | Trạng thái | Cỡ nút | Cỡ chữ | Chữ | Icon |
|---|---|---:|---:|---|---|
| primary | normal | 460x56 | 24 | Về trang chủ | kit_ico_home |

## Chữ (không phải asset: render bằng font)

Thang 5 bậc; bậc 96 và 40 dùng Chakra Petch Bold, còn lại Be Vietnam Pro Bold (chữ nút luôn Be Vietnam Pro). Viền chữ max(2, em/12) px màu #0A0C0C.

| Chữ | Cỡ | Font | Màu |
|---|---:|---|---|
| Đảo rừng · 50x50 ô · 10 người | 30 | Be Vietnam Pro Bold | #EDF2EE |
| Không vào được trận | 30 | Be Vietnam Pro Bold | #EDF2EE |
| Dữ liệu bản đồ không khớp với máy chủ. | 24 | Be Vietnam Pro Bold | #EDF2EE |
| Lý do: phiên bản dữ liệu khác nhau (ví dụ). | 20 | Be Vietnam Pro Bold | #D6DAD6 |
| Về trang chủ | 24 | Be Vietnam Pro Bold | #1F1A10 |
| Điều khiển | 20 | Be Vietnam Pro Bold | #FFDC6E |
| WASD | 24 | Be Vietnam Pro Bold | #1F1A10 |
| Di chuyển | 20 | Be Vietnam Pro Bold | #D6DAD6 |
| Chuột | 24 | Be Vietnam Pro Bold | #1F1A10 |
| Ngắm, bắn | 20 | Be Vietnam Pro Bold | #D6DAD6 |
| F | 24 | Be Vietnam Pro Bold | #1F1A10 |
| Nhặt | 20 | Be Vietnam Pro Bold | #D6DAD6 |
| M | 24 | Be Vietnam Pro Bold | #1F1A10 |
| Bản đồ | 20 | Be Vietnam Pro Bold | #D6DAD6 |
| ESC | 24 | Be Vietnam Pro Bold | #1F1A10 |
| Cài đặt | 20 | Be Vietnam Pro Bold | #D6DAD6 |
| MẸO | 20 | Be Vietnam Pro Bold | #FFDC6E |
| Bụi rậm che bạn khỏi tầm nhìn, nhưng bắn sẽ lộ vị trí ngay l | 24 | Be Vietnam Pro Bold | #EDF2EE |