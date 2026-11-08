# b3-spectator-chuyen (T-023)

Ảnh mock: `T-021/b/b3-spectator-chuyen.png` (1920x1080). Border 9-slice là của sprite @1x (LRTB); @2x nhân đôi.

## Khung, icon, FX, nền

| Phần tử mock | File nguồn | Cỡ vẽ 1080p | Border 9-slice LRTB | @1x / @2x | Số lần | Ghi chú |
|---|---|---:|---|---|---:|---|
| nền | `files/bg/nen-gach-tile.png` | 510x280 | không (không cắt) | 1x | 1 | ô nền gạch lát 510x280 lát kín 1920x1080, phủ tối alpha 0.45 + vignette (gradient chạy lúc render) |
| khung/thành phần kit | `T-020/kit_card_room_selected.png` | 600x120 | [24, 24, 24, 24] | có / có | 1 |  |
| khung/thành phần kit | `T-020/kit_iconbtn_round_normal.png` | 50x50 | không (không cắt) | có / có | 1 | nút tròn kaki làm nền icon |
| icon | `T-020/kit_ico_eye.png` | 30x30 | không (không cắt) | có / có | 1 | icon ink tối (đặt lên nền kaki) |
| icon vật phẩm | `Assets/_JungleMaze/Art/Hud/StatusIcons/icon_alive.png` | 36x36 | không (không cắt) | 64x64 / không | 1 | icon vật phẩm có sẵn trong Assets, khóa mock 'alive' |
| khung/thành phần kit | `T-020/kit_iconbtn_round_normal.png` | 46x46 | không (không cắt) | có / có | 1 | nút tròn kaki làm nền icon |
| icon | `T-020/kit_ico_trophy.png` | 26x26 | không (không cắt) | có / có | 1 | icon ink tối (đặt lên nền kaki) |
| khung/thành phần kit | `T-020/kit_row_selected.png` | 300x48 | [12, 2, 2, 2] | có / có | 1 |  |
| khung/thành phần kit | `T-020/kit_row_alt.png` | 300x48 | [12, 2, 2, 2] | có / có | 4 |  |
| khung/thành phần kit | `T-020/kit_row_normal.png` | 300x48 | [12, 2, 2, 2] | có / có | 4 |  |
| khung/thành phần kit | `T-020/kit_card_stat.png` | 460x180 | [28, 28, 28, 28] | có / có | 1 |  |
| icon vật phẩm | `Assets/_JungleMaze/Art/UI/Icons/Weapons/CROSSBOW.png` | 44x44 | không (không cắt) | 64x64 / không | 1 | icon vật phẩm có sẵn trong Assets, khóa mock 'Nỏ' |
| icon vật phẩm | `Assets/_JungleMaze/Art/Hud/StatusIcons/icon_hp.png` | 36x36 | không (không cắt) | 64x64 / không | 1 | icon vật phẩm có sẵn trong Assets, khóa mock 'hp' |
| khung/thành phần kit | `T-020/kit_progress_track.png` | 250x24 | [12, 5, 12, 5] | có / có | 1 |  |
| khung/thành phần kit | `T-020/kit_progress_fill.png` | 205x16 | [3, 2, 3, 2] | có / có | 1 |  |
| khung/thành phần kit | `T-020/kit_keycap_normal.png` | 52x52 | [14, 14, 14, 14] | có / có | 3 |  |
| khung/thành phần kit | `T-020/kit_iconbtn_round_normal.png` | 48x48 | không (không cắt) | có / có | 2 | nút tròn kaki làm nền icon |
| icon | `T-020/kit_ico_people.png` | 28x28 | không (không cắt) | có / có | 1 | icon ink tối (đặt lên nền kaki) |
| icon | `T-020/kit_ico_skull.png` | 28x28 | không (không cắt) | có / có | 1 | icon ink tối (đặt lên nền kaki) |
| khung/thành phần kit | `T-020/kit_iconbtn_square_normal.png` | 64x64 | không (không cắt) | có / có | 1 |  |
| khung/thành phần kit | `T-020/kit_toast.png` | 440x70 | [22, 22, 22, 22] | có / có | 1 |  |
| khung/thành phần kit | `T-020/kit_iconbtn_round_normal.png` | 42x42 | không (không cắt) | có / có | 3 | nút tròn kaki làm nền icon |
| icon | `T-020/kit_ico_skull.png` | 22x22 | không (không cắt) | có / có | 1 | icon ink tối (đặt lên nền kaki) |
| icon | `T-020/kit_ico_arrow_right.png` | 22x22 | không (không cắt) | có / có | 1 | icon ink tối (đặt lên nền kaki) |
| icon | `T-020/kit_ico_eye.png` | 22x22 | không (không cắt) | có / có | 1 | icon ink tối (đặt lên nền kaki) |

## Chữ (không phải asset: render bằng font)

Thang 5 bậc; bậc 96 và 40 dùng Chakra Petch Bold, còn lại Be Vietnam Pro Bold (chữ nút luôn Be Vietnam Pro). Viền chữ max(2, em/12) px màu #0A0C0C.

| Chữ | Cỡ | Font | Màu |
|---|---:|---|---|
| Tuan | 30 | Be Vietnam Pro Bold | #FFDC6E |
| 9/10 | 24 | Be Vietnam Pro Bold | #EDF2EE |
| #4 | 24 | Be Vietnam Pro Bold | #EDF2EE |
| Tuan | 24 | Be Vietnam Pro Bold | #FFDC6E |
| Hào | 24 | Be Vietnam Pro Bold | #EDF2EE |
| Vinh | 24 | Be Vietnam Pro Bold | #EDF2EE |
| Vũ | 24 | Be Vietnam Pro Bold | #EDF2EE |
| Quang Huy | 24 | Be Vietnam Pro Bold | #EDF2EE |
| Khoa | 24 | Be Vietnam Pro Bold | #EDF2EE |
| Khánh | 24 | Be Vietnam Pro Bold | #EDF2EE |
| Minh | 24 | Be Vietnam Pro Bold | #EDF2EE |
| Long | 24 | Be Vietnam Pro Bold | #EDF2EE |
| Tuan | 30 | Be Vietnam Pro Bold | #EDF2EE |
| 86 | 24 | Be Vietnam Pro Bold | #EDF2EE |
| A | 24 | Be Vietnam Pro Bold | #1F1A10 |
| D | 24 | Be Vietnam Pro Bold | #1F1A10 |
| E | 24 | Be Vietnam Pro Bold | #1F1A10 |
| Vinh | 24 | Be Vietnam Pro Bold | #D6DAD6 |
| Tuan | 24 | Be Vietnam Pro Bold | #EDF2EE |

## Asset mới T-028 đợt 3 dùng cho màn này

| Chỗ | File | Cỡ vẽ 1080p |
|---|---|---:|
| vũ khí của mục tiêu spectator (cạnh tên trong thẻ, 44x44) | icon vũ khí Assets, đủ 18 loại: xem `weapons-icons.md`, `files/items/<TÊN>.png` | 44x44 |