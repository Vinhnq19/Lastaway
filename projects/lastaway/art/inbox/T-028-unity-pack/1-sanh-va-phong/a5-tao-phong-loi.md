# a5-tao-phong-loi (T-024)

Ảnh mock: `T-021/a/a5-tao-phong-loi.png` (1920x1080). Border 9-slice là của sprite @1x (LRTB); @2x nhân đôi.

## Khung, icon, FX, nền

| Phần tử mock | File nguồn | Cỡ vẽ 1080p | Border 9-slice LRTB | @1x / @2x | Số lần | Ghi chú |
|---|---|---:|---|---|---:|---|
| nền | `files/bg/nen-gach-tile.png` | 510x280 | không (không cắt) | 1x | 1 | ô nền gạch lát 510x280 lát kín 1920x1080, phủ tối alpha 0.62 + vignette (gradient chạy lúc render) |
| logo | `Assets/_JungleMaze/Art/UI/Menu/Logo.png (crop 76,68,952,184)` | 760x101 | không (không cắt) | - | 1 | logo pixel LASTAWAY, đã có trong Assets |
| khung/thành phần kit | `T-020/kit_toast.png` | 404x96 | [22, 22, 22, 22] | có / có | 1 |  |
| khung/thành phần kit | `T-020/kit_iconbtn_round_normal.png` | 48x48 | không (không cắt) | có / có | 1 | nút tròn kaki làm nền icon |
| icon | `T-020/kit_ico_person.png` | 28x28 | không (không cắt) | có / có | 1 | icon ink tối (đặt lên nền kaki) |
| icon | `T-020/kit_icon_check.png` | 28x22 | không (không cắt) | có / có | 1 |  |
| khung/thành phần kit | `T-020/kit_dim.png` | 1920x1080 | không (không cắt) | có / có | 1 | lớp phủ tối toàn màn: kéo giãn full màn, tint alpha 205/255 (màu gốc #0A0A08) |
| khung/thành phần kit | `T-020/kit_panel_popup.png` | 820x660 | [47, 47, 88, 47] | có / có | 1 |  |
| khung/thành phần kit | `T-020/kit_close_normal.png` | 52x52 | không (không cắt) | có / có | 1 | nút đóng |
| icon | `T-020/kit_icon_cross_red.png` | 26x26 | không (không cắt) | có / có | 1 | X đỏ #94281F trên nút đóng |
| khung/thành phần kit | `T-020/kit_input_normal.png` | 700x56 | [19, 19, 19, 19] | có / có | 1 |  |
| khung/thành phần kit | `T-020/kit_slider_track.png` | 700x16 | [7, 3, 7, 3] | có / có | 1 |  |
| khung/thành phần kit | `T-020/kit_slider_fill.png` | 700x10 | [2, 1, 2, 1] | có / có | 1 |  |
| khung/thành phần kit | `T-020/kit_knob.png` | 31x32 | không (không cắt) | có / có | 1 |  |
| icon | `T-020/kit_icon_cross.png` | 22x22 | không (không cắt) | có / có | 1 |  |
| khung/thành phần kit | `T-020/kit_dialog_footer.png` | 726x96 | [8, 8, 8, 8] | có / có | 1 |  |

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
| secondary | normal | 250x64 | 30 | Hủy | @cross_ink |
| primary | disabled | 250x64 | 30 | Tạo phòng | kit_ico_plus |

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
| Tạo phòng | 40 | Chakra Petch Bold | #FFDC6E |
| Tên phòng (1 đến 48 ký tự) | 20 | Be Vietnam Pro Bold | #D6DAD6 |
| Leo rank tối nay | 24 | Be Vietnam Pro Bold | #EDF2EE |
| Sức chứa | 20 | Be Vietnam Pro Bold | #D6DAD6 |
| 10 người | 24 | Be Vietnam Pro Bold | #FFDC6E |
| 1 | 20 | Be Vietnam Pro Bold | #D6DAD6 |
| 5 | 20 | Be Vietnam Pro Bold | #D6DAD6 |
| 10 | 20 | Be Vietnam Pro Bold | #D6DAD6 |
| Cần tối thiểu 6 người để bắt đầu trận. | 20 | Be Vietnam Pro Bold | #D6DAD6 |
| Còn 1/2 phòng bổ sung. Phòng trống sau 10 phút sẽ bị thu hồi | 20 | Be Vietnam Pro Bold | #D6DAD6 |
| Đã đủ 2 phòng bổ sung. Xóa một phòng rồi thử lại. | 20 | Be Vietnam Pro Bold | #FF8A78 |
| Hủy | 30 | Be Vietnam Pro Bold | #1F1A10 |
| Tạo phòng | 30 | Be Vietnam Pro Bold | #1F1A10 |