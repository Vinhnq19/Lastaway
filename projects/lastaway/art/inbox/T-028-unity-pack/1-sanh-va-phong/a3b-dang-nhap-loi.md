# a3b-dang-nhap-loi (T-024)

Ảnh mock: `T-021/a/a3b-dang-nhap-loi.png` (1920x1080). Border 9-slice là của sprite @1x (LRTB); @2x nhân đôi.

## Khung, icon, FX, nền

| Phần tử mock | File nguồn | Cỡ vẽ 1080p | Border 9-slice LRTB | @1x / @2x | Số lần | Ghi chú |
|---|---|---:|---|---|---:|---|
| nền | `files/bg/nen-gach-tile.png` | 510x280 | không (không cắt) | 1x | 1 | ô nền gạch lát 510x280 lát kín 1920x1080, phủ tối alpha 0.62 + vignette (gradient chạy lúc render) |
| logo | `Assets/_JungleMaze/Art/UI/Menu/Logo.png (crop 76,68,952,184)` | 700x93 | không (không cắt) | - | 1 | logo pixel LASTAWAY, đã có trong Assets |
| khung/thành phần kit | `T-020/kit_panel_m.png` | 700x650 | [47, 47, 47, 47] | có / có | 1 |  |
| khung/thành phần kit | `T-020/kit_tab_on.png` | 280x64 | [19, 19, 19, 1] | có / có | 1 |  |
| khung/thành phần kit | `T-020/kit_tab_off.png` | 280x64 | [19, 19, 19, 1] | có / có | 1 |  |
| khung/thành phần kit | `T-020/kit_input_error.png` | 580x56 | [19, 19, 19, 19] | có / có | 2 |  |
| icon | `T-020/kit_icon_cross.png` | 22x22 | không (không cắt) | có / có | 1 |  |

## Nút

Sprite `T-020/kit_btn_<loại>_<trạng thái>.png` (+ `@2x`), 9-slice [22,22,28,28]; chữ Be Vietnam Pro Bold, icon trái cao bằng cỡ chữ, gap 10 px, căn giữa mặt nút (trừ đế).

| Loại | Trạng thái | Cỡ nút | Cỡ chữ | Chữ | Icon |
|---|---|---:|---:|---|---|
| primary | normal | 580x64 | 30 | Đăng nhập | kit_ico_key |
| secondary | normal | 580x56 | 24 | Máy chủ (nâng cao) | kit_ico_server |

## Chữ (không phải asset: render bằng font)

Thang 5 bậc; bậc 96 và 40 dùng Chakra Petch Bold, còn lại Be Vietnam Pro Bold (chữ nút luôn Be Vietnam Pro). Viền chữ max(2, em/12) px màu #0A0C0C.

| Chữ | Cỡ | Font | Màu |
|---|---:|---|---|
| Đăng nhập | 40 | Chakra Petch Bold | #FFDC6E |
| Đăng nhập | 24 | Be Vietnam Pro Bold | #1F1A10 |
| Đăng ký | 24 | Be Vietnam Pro Bold | #EDF2EE |
| Tên đăng nhập | 20 | Be Vietnam Pro Bold | #D6DAD6 |
| huycv | 24 | Be Vietnam Pro Bold | #EDF2EE |
| Mật khẩu | 20 | Be Vietnam Pro Bold | #D6DAD6 |
| •••••••• | 24 | Be Vietnam Pro Bold | #EDF2EE |
| Sai tên đăng nhập hoặc mật khẩu. | 20 | Be Vietnam Pro Bold | #FF8A78 |
| Đăng nhập | 30 | Be Vietnam Pro Bold | #1F1A10 |
| Máy chủ (nâng cao) | 24 | Be Vietnam Pro Bold | #1F1A10 |