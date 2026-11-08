# a1-khoi-dong (T-024)

Ảnh mock: `T-021/a/a1-khoi-dong.png` (1920x1080). Border 9-slice là của sprite @1x (LRTB); @2x nhân đôi.

## Khung, icon, FX, nền

| Phần tử mock | File nguồn | Cỡ vẽ 1080p | Border 9-slice LRTB | @1x / @2x | Số lần | Ghi chú |
|---|---|---:|---|---|---:|---|
| nền | `files/bg/nen-gach-tile.png` | 510x280 | không (không cắt) | 1x | 1 | ô nền gạch lát 510x280 lát kín 1920x1080, phủ tối alpha 0.62 + vignette (gradient chạy lúc render) |
| logo | `Assets/_JungleMaze/Art/UI/Menu/Logo.png (crop 76,68,952,184)` | 820x109 | không (không cắt) | - | 1 | logo pixel LASTAWAY, đã có trong Assets |
| khung/thành phần kit | `T-020/kit_progress_track.png` | 480x28 | [12, 5, 12, 5] | có / có | 1 |  |
| khung/thành phần kit | `T-020/kit_progress_fill.png` | 230x16 | [3, 2, 3, 2] | có / có | 1 |  |
| icon | `T-020/kit_icon_check.png` | 28x22 | không (không cắt) | có / có | 1 |  |
| khung/thành phần kit | `T-020/kit_iconbtn_round_normal.png` | 42x42 | không (không cắt) | có / có | 1 | nút tròn kaki làm nền icon |
| icon | `T-020/kit_ico_wifi.png` | 22x22 | không (không cắt) | có / có | 1 | icon ink tối (đặt lên nền kaki) |

## Chữ (không phải asset: render bằng font)

Thang 5 bậc; bậc 96 và 40 dùng Chakra Petch Bold, còn lại Be Vietnam Pro Bold (chữ nút luôn Be Vietnam Pro). Viền chữ max(2, em/12) px màu #0A0C0C.

| Chữ | Cỡ | Font | Màu |
|---|---:|---|---|
| Đang kết nối máy chủ... | 24 | Be Vietnam Pro Bold | #EDF2EE |
| Kiểm tra phiên đăng nhập | 20 | Be Vietnam Pro Bold | #EDF2EE |
| Kết nối máy chủ | 20 | Be Vietnam Pro Bold | #EDF2EE |
| Máy chủ: Local 127.0.0.1:7777 | 20 | Be Vietnam Pro Bold | #D6DAD6 |
| v0.2 | 20 | Be Vietnam Pro Bold | #D6DAD6 |