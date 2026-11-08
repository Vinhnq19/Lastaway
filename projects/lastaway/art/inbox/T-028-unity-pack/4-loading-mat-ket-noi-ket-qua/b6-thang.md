# b6-thang (T-025)

Ảnh mock: `T-021/b/b6-thang.png` (1920x1080). Border 9-slice là của sprite @1x (LRTB); @2x nhân đôi.

## Khung, icon, FX, nền

| Phần tử mock | File nguồn | Cỡ vẽ 1080p | Border 9-slice LRTB | @1x / @2x | Số lần | Ghi chú |
|---|---|---:|---|---|---:|---|
| nền | `files/bg/nen-gach-tile.png` | 510x280 | không (không cắt) | 1x | 1 | ô nền gạch lát 510x280 lát kín 1920x1080, phủ tối alpha 0.62 + vignette (gradient chạy lúc render) |
| khung/thành phần kit | `T-020/kit_panel_m.png` | 820x820 | [47, 47, 47, 47] | có / có | 1 |  |
| khung/thành phần kit | `T-020/kit_iconbtn_round_normal.png` | 56x56 | không (không cắt) | có / có | 1 | nút tròn kaki làm nền icon |
| icon | `T-020/kit_ico_trophy.png` | 36x36 | không (không cắt) | có / có | 1 | icon ink tối (đặt lên nền kaki) |
| khung/thành phần kit | `T-020/kit_row_selected.png` | 720x60 | [12, 2, 2, 2] | có / có | 1 |  |
| icon vật phẩm | `Assets/_JungleMaze/Art/Hud/StatusIcons/icon_kill.png` | 32x32 | không (không cắt) | 64x64 / không | 10 | icon vật phẩm có sẵn trong Assets, khóa mock 'kill' |
| khung/thành phần kit | `T-020/kit_row_alt.png` | 720x60 | [12, 2, 2, 2] | có / có | 5 |  |
| khung/thành phần kit | `T-020/kit_row_normal.png` | 720x60 | [12, 2, 2, 2] | có / có | 4 |  |
| FX/emblem | `T-021/b/assets/fx_emblem_win.png` | 246x230 | không (không cắt) | có / không | 1 | FX/emblem gen GPT (nét cũ, Huy giữ) |
| khung/thành phần kit | `T-020/kit_card_stat.png` | 330x130 | [28, 28, 28, 28] | có / có | 2 |  |
| icon vật phẩm | `Assets/_JungleMaze/Art/Hud/StatusIcons/icon_kill.png` | 56x56 | không (không cắt) | 64x64 / không | 1 | icon vật phẩm có sẵn trong Assets, khóa mock 'kill' |
| icon vật phẩm | `Assets/_JungleMaze/Art/Hud/StatusIcons/icon_zone-timer.png` | 56x56 | không (không cắt) | 64x64 / không | 1 | icon vật phẩm có sẵn trong Assets, khóa mock 'zone' |

## Nút

Sprite `T-020/kit_btn_<loại>_<trạng thái>.png` (+ `@2x`), 9-slice [22,22,28,28]; chữ Be Vietnam Pro Bold, icon trái cao bằng cỡ chữ, gap 10 px, căn giữa mặt nút (trừ đế).

| Loại | Trạng thái | Cỡ nút | Cỡ chữ | Chữ | Icon |
|---|---|---:|---:|---|---|
| secondary | normal | 330x72 | 30 | Về sảnh | kit_ico_home |
| primary | normal | 330x72 | 30 | Chơi lại | kit_ico_refresh |

## Chữ (không phải asset: render bằng font)

Thang 5 bậc; bậc 96 và 40 dùng Chakra Petch Bold, còn lại Be Vietnam Pro Bold (chữ nút luôn Be Vietnam Pro). Viền chữ max(2, em/12) px màu #0A0C0C.

| Chữ | Cỡ | Font | Màu |
|---|---:|---|---|
| Xếp hạng | 30 | Be Vietnam Pro Bold | #FFDC6E |
| #1 | 24 | Be Vietnam Pro Bold | #FFDC6E |
| Huy | 24 | Be Vietnam Pro Bold | #EDF2EE |
| 3 | 24 | Be Vietnam Pro Bold | #D6DAD6 |
| #2 | 24 | Be Vietnam Pro Bold | #D6DAD6 |
| Hào | 24 | Be Vietnam Pro Bold | #EDF2EE |
| 4 | 24 | Be Vietnam Pro Bold | #D6DAD6 |
| #3 | 24 | Be Vietnam Pro Bold | #D6DAD6 |
| Vinh | 24 | Be Vietnam Pro Bold | #EDF2EE |
| 1 | 24 | Be Vietnam Pro Bold | #D6DAD6 |
| #4 | 24 | Be Vietnam Pro Bold | #D6DAD6 |
| V.Huy | 24 | Be Vietnam Pro Bold | #EDF2EE |
| 2 | 24 | Be Vietnam Pro Bold | #D6DAD6 |
| #5 | 24 | Be Vietnam Pro Bold | #D6DAD6 |
| Vũ | 24 | Be Vietnam Pro Bold | #EDF2EE |
| 0 | 24 | Be Vietnam Pro Bold | #D6DAD6 |
| #6 | 24 | Be Vietnam Pro Bold | #D6DAD6 |
| Quang Huy | 24 | Be Vietnam Pro Bold | #EDF2EE |
| #7 | 24 | Be Vietnam Pro Bold | #D6DAD6 |
| Khoa | 24 | Be Vietnam Pro Bold | #EDF2EE |
| #8 | 24 | Be Vietnam Pro Bold | #D6DAD6 |
| Khánh | 24 | Be Vietnam Pro Bold | #EDF2EE |
| #9 | 24 | Be Vietnam Pro Bold | #D6DAD6 |
| Minh | 24 | Be Vietnam Pro Bold | #EDF2EE |
| #10 | 24 | Be Vietnam Pro Bold | #D6DAD6 |
| Long | 24 | Be Vietnam Pro Bold | #EDF2EE |
| CHIẾN THẮNG | 96 | Chakra Petch Bold | #FFDC6E |
| #1 / 10 | 40 | Chakra Petch Bold | #EDF2EE |
| 3 | 40 | Chakra Petch Bold | #EDF2EE |
| 07:00 | 40 | Chakra Petch Bold | #EDF2EE |
| Về sảnh | 30 | Be Vietnam Pro Bold | #1F1A10 |
| Chơi lại | 30 | Be Vietnam Pro Bold | #1F1A10 |