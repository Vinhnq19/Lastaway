# b8-mat-ket-noi-het-han (T-025)

Ảnh mock: `T-021/b/b8-mat-ket-noi-het-han.png` (1920x1080). Border 9-slice là của sprite @1x (LRTB); @2x nhân đôi.

## Khung, icon, FX, nền

| Phần tử mock | File nguồn | Cỡ vẽ 1080p | Border 9-slice LRTB | @1x / @2x | Số lần | Ghi chú |
|---|---|---:|---|---|---:|---|
| khung/thành phần kit | `files/new/kit_killfeed_panel.png` | 270x154 | [16, 16, 16, 16] | có / có | 1 | panel kill feed, rộng thêm theo chữ (tên + icon vũ khí 32 + tên, 24 px, dòng cách 44), alpha ~0,98 |
| khung/thành phần kit | `files/new/kit_badge_key.png` | 30x30 | [14, 14, 14, 14] | có / có | 8 | nhãn số phím quick bar (1), số 20 px vàng #FFDC6E đè lên |
| nền HUD | `HUD trong trận (Hud.prefab T-016)` | 1920x1080 | không (không cắt) | - | 1 | nền HUD đã có trong Unity (T-016); mock chỉ dựng lại để thấy lớp phủ; phủ tối alpha 120 |
| icon vật phẩm | `Assets/_JungleMaze/Art/UI/Icons/Weapons/CROSSBOW.png` | 32x32 | không (không cắt) | 64x64 / không | 1 | icon vật phẩm có sẵn trong Assets, khóa mock 'Nỏ' |
| icon vật phẩm | `Assets/_JungleMaze/Art/UI/Icons/Weapons/SHOTGUN.png` | 32x32 | không (không cắt) | 64x64 / không | 1 | icon vật phẩm có sẵn trong Assets, khóa mock 'Shotgun' |
| icon vật phẩm | `Assets/_JungleMaze/Art/UI/Icons/Weapons/RUBBER_GUN.png` | 32x32 | không (không cắt) | 64x64 / không | 1 | icon vật phẩm có sẵn trong Assets, khóa mock 'Nỏ cao su' |
| khung/thành phần kit | `T-020/kit_dim.png` | 1920x1080 | không (không cắt) | có / có | 2 | lớp phủ tối toàn màn: kéo giãn full màn, tint alpha 120/255 (màu gốc #0A0A08) |
| khung/thành phần kit | `T-020/kit_panel_m.png` | 760x420 | [47, 47, 47, 47] | có / có | 1 |  |
| FX/emblem | `T-021/b/assets/fx_icon_disconnect.png` | 75x64 | không (không cắt) | có / không | 1 | FX/emblem gen GPT (nét cũ, Huy giữ) |

## Nút

Sprite `T-020/kit_btn_<loại>_<trạng thái>.png` (+ `@2x`), 9-slice [22,22,28,28]; chữ Be Vietnam Pro Bold, icon trái cao bằng cỡ chữ, gap 10 px, căn giữa mặt nút (trừ đế).

| Loại | Trạng thái | Cỡ nút | Cỡ chữ | Chữ | Icon |
|---|---|---:|---:|---|---|
| primary | normal | 320x64 | 30 | Về sảnh | kit_ico_home |

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
| Đã mất kết nối | 40 | Chakra Petch Bold | #EDF2EE |
| Hạng #4 / 10 | 30 | Be Vietnam Pro Bold | #EDF2EE |
| Về sảnh | 30 | Be Vietnam Pro Bold | #1F1A10 |