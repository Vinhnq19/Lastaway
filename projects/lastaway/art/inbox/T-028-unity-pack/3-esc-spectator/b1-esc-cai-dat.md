# b1-esc-cai-dat (T-023)

Ảnh mock: `T-021/b/b1-esc-cai-dat.png` (1920x1080). Border 9-slice là của sprite @1x (LRTB); @2x nhân đôi.

## Khung, icon, FX, nền

| Phần tử mock | File nguồn | Cỡ vẽ 1080p | Border 9-slice LRTB | @1x / @2x | Số lần | Ghi chú |
|---|---|---:|---|---|---:|---|
| khung/thành phần kit | `files/new/kit_killfeed_panel.png` | 270x154 | [16, 16, 16, 16] | có / có | 1 | panel kill feed, rộng thêm theo chữ (tên + icon vũ khí 32 + tên, 24 px, dòng cách 44), alpha ~0,98 |
| khung/thành phần kit | `files/new/kit_badge_key.png` | 30x30 | [14, 14, 14, 14] | có / có | 8 | nhãn số phím quick bar (1), số 20 px vàng #FFDC6E đè lên |
| nền HUD | `HUD trong trận (Hud.prefab T-016)` | 1920x1080 | không (không cắt) | - | 1 | nền HUD đã có trong Unity (T-016); mock chỉ dựng lại để thấy lớp phủ; phủ tối alpha 150 |
| icon vật phẩm | `Assets/_JungleMaze/Art/UI/Icons/Weapons/CROSSBOW.png` | 32x32 | không (không cắt) | 64x64 / không | 1 | icon vật phẩm có sẵn trong Assets, khóa mock 'Nỏ' |
| icon vật phẩm | `Assets/_JungleMaze/Art/UI/Icons/Weapons/SHOTGUN.png` | 32x32 | không (không cắt) | 64x64 / không | 1 | icon vật phẩm có sẵn trong Assets, khóa mock 'Shotgun' |
| icon vật phẩm | `Assets/_JungleMaze/Art/UI/Icons/Weapons/RUBBER_GUN.png` | 32x32 | không (không cắt) | 64x64 / không | 1 | icon vật phẩm có sẵn trong Assets, khóa mock 'Nỏ cao su' |
| khung/thành phần kit | `T-020/kit_dim.png` | 1920x1080 | không (không cắt) | có / có | 1 | lớp phủ tối toàn màn: kéo giãn full màn, tint alpha 150/255 (màu gốc #0A0A08) |
| khung/thành phần kit | `T-020/kit_panel_popup.png` | 820x700 | [47, 47, 88, 47] | có / có | 1 |  |
| khung/thành phần kit | `T-020/kit_close_normal.png` | 52x52 | không (không cắt) | có / có | 1 | nút đóng |
| icon | `T-020/kit_icon_cross_red.png` | 26x26 | không (không cắt) | có / có | 1 | X đỏ #94281F trên nút đóng |
| khung/thành phần kit | `T-020/kit_iconbtn_round_normal.png` | 44x44 | không (không cắt) | có / có | 5 | nút tròn kaki làm nền icon |
| icon | `T-020/kit_ico_sound.png` | 24x24 | không (không cắt) | có / có | 2 | icon ink tối (đặt lên nền kaki) |
| khung/thành phần kit | `T-020/kit_slider_track.png` | 270x16 | [7, 3, 7, 3] | có / có | 4 |  |
| khung/thành phần kit | `T-020/kit_slider_fill.png` | 216x10 | [2, 1, 2, 1] | có / có | 1 |  |
| khung/thành phần kit | `T-020/kit_knob.png` | 31x32 | không (không cắt) | có / có | 4 |  |
| khung/thành phần kit | `T-020/kit_slider_fill.png` | 189x10 | [2, 1, 2, 1] | có / có | 1 |  |
| icon | `T-020/kit_ico_music.png` | 24x24 | không (không cắt) | có / có | 2 | icon ink tối (đặt lên nền kaki) |
| khung/thành phần kit | `T-020/kit_slider_fill.png` | 135x10 | [2, 1, 2, 1] | có / có | 1 |  |
| khung/thành phần kit | `T-020/kit_divider.png` | 680x14 | [10, 10, 2, 2] | có / có | 1 |  |
| khung/thành phần kit | `T-020/kit_toggle_off.png` | 114x48 | không (không cắt) | có / có | 1 |  |
| khung/thành phần kit | `T-020/kit_slider_fill.png` | 89x10 | [2, 1, 2, 1] | có / có | 1 |  |
| icon | `T-020/kit_ico_vibrate.png` | 24x24 | không (không cắt) | có / có | 1 | icon ink tối (đặt lên nền kaki) |
| khung/thành phần kit | `T-020/kit_toggle_on.png` | 114x48 | không (không cắt) | có / có | 1 |  |
| khung/thành phần kit | `T-020/kit_dialog_footer.png` | 726x96 | [8, 8, 8, 8] | có / có | 1 |  |

## Nút

Sprite `T-020/kit_btn_<loại>_<trạng thái>.png` (+ `@2x`), 9-slice [22,22,28,28]; chữ Be Vietnam Pro Bold, icon trái cao bằng cỡ chữ, gap 10 px, căn giữa mặt nút (trừ đế).

| Loại | Trạng thái | Cỡ nút | Cỡ chữ | Chữ | Icon |
|---|---|---:|---:|---|---|
| secondary | normal | 200x64 | 30 | Mặc định | kit_ico_refresh |
| secondary | normal | 240x64 | 30 | Thoát trận | @cross_red |
| primary | normal | 220x64 | 30 | Tiếp tục | kit_ico_play |

## Chữ (không phải asset: render bằng font)

Thang 5 bậc; bậc 96 và 40 dùng Chakra Petch Bold, còn lại Be Vietnam Pro Bold (chữ nút luôn Be Vietnam Pro). Viền chữ max(2, em/12) px màu #0A0C0C.

| Chữ | Cỡ | Font | Màu |
|---|---:|---|---|
| Hào | 24 | Be Vietnam Pro Bold | #EDF2EE |
| Linh | 24 | Be Vietnam Pro Bold | #EDF2EE |
| Vinh | 24 | Be Vietnam Pro Bold | #EDF2EE |
| Mai | 24 | Be Vietnam Pro Bold | #EDF2EE |
| Vũ | 24 | Be Vietnam Pro Bold | #EDF2EE |
| Sơn | 24 | Be Vietnam Pro Bold | #EDF2EE |
| 100 | 30 | Be Vietnam Pro Bold | #EDF2EE |
| 0 | 30 | Be Vietnam Pro Bold | #EDF2EE |
| 1/1 | 24 | Be Vietnam Pro Bold | #EDF2EE |
| 1:21 | 24 | Be Vietnam Pro Bold | #EDF2EE |
| 1 | 20 | Be Vietnam Pro Bold | #FFDC6E |
| 2 | 20 | Be Vietnam Pro Bold | #FFDC6E |
| 3 | 20 | Be Vietnam Pro Bold | #FFDC6E |
| 4 | 20 | Be Vietnam Pro Bold | #FFDC6E |
| 5 | 20 | Be Vietnam Pro Bold | #FFDC6E |
| 6 | 20 | Be Vietnam Pro Bold | #FFDC6E |
| 7 | 20 | Be Vietnam Pro Bold | #FFDC6E |
| 8 | 20 | Be Vietnam Pro Bold | #FFDC6E |
| Cài đặt | 40 | Chakra Petch Bold | #FFDC6E |
| Âm lượng | 24 | Be Vietnam Pro Bold | #EDF2EE |
| 80% | 24 | Be Vietnam Pro Bold | #D6DAD6 |
| Hiệu ứng | 24 | Be Vietnam Pro Bold | #EDF2EE |
| 70% | 24 | Be Vietnam Pro Bold | #D6DAD6 |
| Nhạc nền | 24 | Be Vietnam Pro Bold | #EDF2EE |
| 50% | 24 | Be Vietnam Pro Bold | #D6DAD6 |
| Nhạc | 24 | Be Vietnam Pro Bold | #EDF2EE |
| Chuột | 24 | Be Vietnam Pro Bold | #EDF2EE |
| 1,0 | 24 | Be Vietnam Pro Bold | #D6DAD6 |
| Rung | 24 | Be Vietnam Pro Bold | #EDF2EE |
| Mặc định | 30 | Be Vietnam Pro Bold | #1F1A10 |
| Thoát trận | 30 | Be Vietnam Pro Bold | #1F1A10 |
| Tiếp tục | 30 | Be Vietnam Pro Bold | #1F1A10 |