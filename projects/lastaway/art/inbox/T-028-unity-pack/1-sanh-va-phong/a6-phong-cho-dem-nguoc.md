# a6-phong-cho-dem-nguoc (T-024)

Ảnh mock: `T-021/a/a6-phong-cho-dem-nguoc.png` (1920x1080). Border 9-slice là của sprite @1x (LRTB); @2x nhân đôi.

## Khung, icon, FX, nền

| Phần tử mock | File nguồn | Cỡ vẽ 1080p | Border 9-slice LRTB | @1x / @2x | Số lần | Ghi chú |
|---|---|---:|---|---|---:|---|
| nền | `files/bg/nen-gach-tile.png` | 510x280 | không (không cắt) | 1x | 1 | ô nền gạch lát 510x280 lát kín 1920x1080, phủ tối alpha 0.7 + vignette (gradient chạy lúc render) |
| khung/thành phần kit | `T-020/kit_screen_header.png` | 1800x104 | [36, 36, 12, 12] | có / có | 1 | thanh tiêu đề màn |
| khung/thành phần kit | `T-020/kit_iconbtn_square_normal.png` | 64x64 | không (không cắt) | có / có | 1 | nút quay lại |
| icon | `T-020/kit_ico_arrow_left.png` | 40x40 | không (không cắt) | có / có | 1 | icon ink tối trên nút quay lại |
| khung/thành phần kit | `T-020/kit_slot_player_notready.png` | 440x96 | [100, 40, 24, 24] | có / có | 7 |  |
| khung/thành phần kit | `T-020/kit_card_room_full.png` | 440x96 | [24, 24, 24, 24] | có / có | 3 |  |
| khung/thành phần kit | `T-020/kit_panel_m.png` | 830x520 | [47, 47, 47, 47] | có / có | 1 |  |
| khung/thành phần kit | `T-020/kit_iconbtn_round_normal.png` | 46x46 | không (không cắt) | có / có | 1 | nút tròn kaki làm nền icon |
| icon | `T-020/kit_ico_person.png` | 26x26 | không (không cắt) | có / có | 1 | icon ink tối (đặt lên nền kaki) |

## Nút

Sprite `T-020/kit_btn_<loại>_<trạng thái>.png` (+ `@2x`), 9-slice [22,22,28,28]; chữ Be Vietnam Pro Bold, icon trái cao bằng cỡ chữ, gap 10 px, căn giữa mặt nút (trừ đế).

| Loại | Trạng thái | Cỡ nút | Cỡ chữ | Chữ | Icon |
|---|---|---:|---:|---|---|
| secondary | normal | 130x56 | 24 | Chép | kit_ico_copy |
| secondary | normal | 320x72 | 30 | Rời phòng | @cross_red |
| secondary | normal | 420x72 | 30 | Sẵn sàng (tùy chọn) | kit_ico_check |

## Chữ (không phải asset: render bằng font)

Thang 5 bậc; bậc 96 và 40 dùng Chakra Petch Bold, còn lại Be Vietnam Pro Bold (chữ nút luôn Be Vietnam Pro). Viền chữ max(2, em/12) px màu #0A0C0C.

| Chữ | Cỡ | Font | Màu |
|---|---:|---|---|
| Phòng chờ | 40 | Chakra Petch Bold | #FFDC6E |
| Mã phòng 4X7K2P | 24 | Be Vietnam Pro Bold | #EDF2EE |
| Chép | 24 | Be Vietnam Pro Bold | #1F1A10 |
| Huy (chủ phòng) | 24 | Be Vietnam Pro Bold | #EDF2EE |
| V.Huy | 24 | Be Vietnam Pro Bold | #EDF2EE |
| Hào | 24 | Be Vietnam Pro Bold | #EDF2EE |
| Vinh | 24 | Be Vietnam Pro Bold | #EDF2EE |
| Vũ | 24 | Be Vietnam Pro Bold | #EDF2EE |
| Quang Huy | 24 | Be Vietnam Pro Bold | #EDF2EE |
| Khoa | 24 | Be Vietnam Pro Bold | #EDF2EE |
| Chỗ trống | 24 | Be Vietnam Pro Bold | #D6DAD6 |
| Bắt đầu sau | 24 | Be Vietnam Pro Bold | #EDF2EE |
| 12 | 96 | Chakra Petch Bold | #FFDC6E |
| giây | 30 | Be Vietnam Pro Bold | #EDF2EE |
| 7/10 người trong phòng | 24 | Be Vietnam Pro Bold | #EDF2EE |
| Tối thiểu 6 người để bắt đầu | 24 | Be Vietnam Pro Bold | #D6DAD6 |
| Bản đồ: Đảo rừng 50x50 ô | 24 | Be Vietnam Pro Bold | #D6DAD6 |
| Trận tự bắt đầu sau 15 giây khi đủ 6 người. Thiếu người thì  | 24 | Be Vietnam Pro Bold | #D6DAD6 |
| Rời phòng | 30 | Be Vietnam Pro Bold | #1F1A10 |
| Sẵn sàng (tùy chọn) | 30 | Be Vietnam Pro Bold | #1F1A10 |