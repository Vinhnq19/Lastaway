# a4-phong-cua-toi (T-024)

Ảnh mock: `T-021/a/a4-phong-cua-toi.png` (1920x1080). Border 9-slice là của sprite @1x (LRTB); @2x nhân đôi.

## Khung, icon, FX, nền

| Phần tử mock | File nguồn | Cỡ vẽ 1080p | Border 9-slice LRTB | @1x / @2x | Số lần | Ghi chú |
|---|---|---:|---|---|---:|---|
| nền | `files/bg/nen-gach-tile.png` | 510x280 | không (không cắt) | 1x | 1 | ô nền gạch lát 510x280 lát kín 1920x1080, phủ tối alpha 0.7 + vignette (gradient chạy lúc render) |
| khung/thành phần kit | `T-020/kit_screen_header.png` | 1800x104 | [36, 36, 12, 12] | có / có | 1 | thanh tiêu đề màn |
| khung/thành phần kit | `T-020/kit_iconbtn_square_normal.png` | 64x64 | không (không cắt) | có / có | 1 | nút quay lại |
| icon | `T-020/kit_ico_arrow_left.png` | 40x40 | không (không cắt) | có / có | 1 | icon ink tối trên nút quay lại |
| khung/thành phần kit | `T-020/kit_card_room_normal.png` | 1680x120 | [24, 24, 24, 24] | có / có | 1 |  |
| khung/thành phần kit | `T-020/kit_tag_neutral.png` | 140x40 | [8, 6, 8, 6] | có / có | 3 |  |
| khung/thành phần kit | `T-020/kit_card_room_selected.png` | 1680x120 | [24, 24, 24, 24] | có / có | 1 |  |
| khung/thành phần kit | `T-020/kit_card_room_full.png` | 1680x120 | [24, 24, 24, 24] | có / có | 1 |  |

## Nút

Sprite `T-020/kit_btn_<loại>_<trạng thái>.png` (+ `@2x`), 9-slice [22,22,28,28]; chữ Be Vietnam Pro Bold, icon trái cao bằng cỡ chữ, gap 10 px, căn giữa mặt nút (trừ đế).

| Loại | Trạng thái | Cỡ nút | Cỡ chữ | Chữ | Icon |
|---|---|---:|---:|---|---|
| primary | normal | 110x52 | 24 | Vào | kit_ico_play |
| secondary | normal | 190x52 | 24 | Sao chép | kit_ico_copy |
| secondary | normal | 110x52 | 24 | Xóa | kit_ico_trash |
| primary | disabled | 110x52 | 24 | Vào | kit_ico_play |
| primary | normal | 280x72 | 30 | Tạo phòng | kit_ico_plus |

## Chữ (không phải asset: render bằng font)

Thang 5 bậc; bậc 96 và 40 dùng Chakra Petch Bold, còn lại Be Vietnam Pro Bold (chữ nút luôn Be Vietnam Pro). Viền chữ max(2, em/12) px màu #0A0C0C.

| Chữ | Cỡ | Font | Màu |
|---|---:|---|---|
| Phòng của tôi | 40 | Chakra Petch Bold | #FFDC6E |
| 2 phòng bổ sung tối đa | 24 | Be Vietnam Pro Bold | #D6DAD6 |
| Phòng mặc định | 30 | Be Vietnam Pro Bold | #EDF2EE |
| Mặc định | 20 | Be Vietnam Pro Bold | #FFFFFF |
| 6/10 | 24 | Be Vietnam Pro Bold | #EDF2EE |
| mã do chủ phòng gửi | 20 | Be Vietnam Pro Bold | #D6DAD6 |
| Vào | 24 | Be Vietnam Pro Bold | #1F1A10 |
| Leo rank tối nay | 30 | Be Vietnam Pro Bold | #EDF2EE |
| Của bạn | 20 | Be Vietnam Pro Bold | #FFFFFF |
| 3/10 | 24 | Be Vietnam Pro Bold | #EDF2EE |
| 4X7K2P | 24 | Be Vietnam Pro Bold | #FFDC6E |
| Sao chép | 24 | Be Vietnam Pro Bold | #1F1A10 |
| Xóa | 24 | Be Vietnam Pro Bold | #1F1A10 |
| Luyện tập | 30 | Be Vietnam Pro Bold | #EDF2EE |
| 10/10 đầy | 24 | Be Vietnam Pro Bold | #EDF2EE |
| 9HM3TQ | 24 | Be Vietnam Pro Bold | #FFDC6E |
| Tạo phòng | 30 | Be Vietnam Pro Bold | #1F1A10 |

## Asset mới T-028 đợt 3 dùng cho màn này

| Chỗ | File | Cỡ vẽ 1080p |
|---|---|---:|
| nút Luyện tập offline / Lưu endpoint / Xem online / Điền đủ (unity thêm, mock chưa có) | `files/new/kit_ico_practice`, `kit_ico_save`, `kit_ico_globe`, `kit_ico_fill` (+ @2x), icon mục tối 48x48 cao bằng cỡ chữ | cao 24 (nút S) hoặc 30 (nút M) |