# b9-phan-hoi-hud (T-022)

Ảnh mock: `T-021/b/b9-phan-hoi-hud.png` (1920x1080). Border 9-slice là của sprite @1x (LRTB); @2x nhân đôi.

## Khung, icon, FX, nền

| Phần tử mock | File nguồn | Cỡ vẽ 1080p | Border 9-slice LRTB | @1x / @2x | Số lần | Ghi chú |
|---|---|---:|---|---|---:|---|
| khung/thành phần kit | `files/new/kit_killfeed_panel.png` | 270x154 | [16, 16, 16, 16] | có / có | 1 | panel kill feed, rộng thêm theo chữ (tên + icon vũ khí 32 + tên, 24 px, dòng cách 44), alpha ~0,98 |
| khung/thành phần kit | `files/new/kit_badge_key.png` | 30x30 | [14, 14, 14, 14] | có / có | 8 | nhãn số phím quick bar (1), số 20 px vàng #FFDC6E đè lên |
| nền HUD | `HUD trong trận (Hud.prefab T-016)` | 1920x1080 | không (không cắt) | - | 1 | nền HUD đã có trong Unity (T-016); mock chỉ dựng lại để thấy lớp phủ |
| icon vật phẩm | `Assets/_JungleMaze/Art/UI/Icons/Weapons/CROSSBOW.png` | 32x32 | không (không cắt) | 64x64 / không | 1 | icon vật phẩm có sẵn trong Assets, khóa mock 'Nỏ' |
| icon vật phẩm | `Assets/_JungleMaze/Art/UI/Icons/Weapons/SHOTGUN.png` | 32x32 | không (không cắt) | 64x64 / không | 1 | icon vật phẩm có sẵn trong Assets, khóa mock 'Shotgun' |
| icon vật phẩm | `Assets/_JungleMaze/Art/UI/Icons/Weapons/RUBBER_GUN.png` | 32x32 | không (không cắt) | 64x64 / không | 1 | icon vật phẩm có sẵn trong Assets, khóa mock 'Nỏ cao su' |
| FX/emblem | `T-021/b/assets/fx_hitmarker.png` | 101x96 | không (không cắt) | có / không | 1 | FX/emblem gen GPT (nét cũ, Huy giữ) |
| FX/emblem | `T-021/b/assets/fx_damage_arc.png` | 60x200 | không (không cắt) | có / không | 1 | FX/emblem gen GPT (nét cũ, Huy giữ) |
| FX/emblem | `T-021/b/assets/fx_danger_arrow.png` | 64x90 | không (không cắt) | có / không | 1 | FX/emblem gen GPT (nét cũ, Huy giữ) |
| icon vật phẩm | `Assets/_JungleMaze/Art/Hud/StatusIcons/icon_zone-timer.png` | 34x34 | không (không cắt) | 64x64 / không | 1 | icon vật phẩm có sẵn trong Assets, khóa mock 'zone' |
| khung/thành phần kit | `T-020/kit_tag_neutral.png` | 70x36 | [8, 6, 8, 6] | có / có | 1 |  |
| khung/thành phần kit | `T-020/kit_toast.png` | 340x60 | [22, 22, 22, 22] | có / có | 1 |  |
| icon | `T-020/kit_icon_warn.png` | 29x26 | không (không cắt) | có / có | 1 |  |
| icon vật phẩm | `Assets/_JungleMaze/Art/Hud/StatusIcons/icon_zone-timer.png` | 32x32 | không (không cắt) | 64x64 / không | 1 | icon vật phẩm có sẵn trong Assets, khóa mock 'zone' |
| khung/thành phần kit | `T-020/kit_iconbtn_round_normal.png` | 40x40 | không (không cắt) | có / có | 1 | nút tròn kaki làm nền icon |
| icon | `T-020/kit_ico_arrow_left.png` | 20x20 | không (không cắt) | có / có | 1 | icon ink tối (đặt lên nền kaki) |
| icon vật phẩm | `Assets/_JungleMaze/Art/UI/Icons/Effects/Poison.png` | 76x76 | không (không cắt) | 64x64 / không | 1 | icon vật phẩm có sẵn trong Assets, khóa mock 'poison' |
| khung/thành phần kit | `T-020/kit_tag_neutral.png` | 60x36 | [8, 6, 8, 6] | có / có | 3 |  |
| icon vật phẩm | `Assets/_JungleMaze/Art/UI/Icons/Effects/Slow.png` | 76x76 | không (không cắt) | 64x64 / không | 1 | icon vật phẩm có sẵn trong Assets, khóa mock 'slow' |
| icon vật phẩm | `Assets/_JungleMaze/Art/UI/Icons/Effects/Invisible.png` | 76x76 | không (không cắt) | 64x64 / không | 1 | icon vật phẩm có sẵn trong Assets, khóa mock 'stealth' |
| khung/thành phần kit | `T-020/kit_toast.png` | 620x64 | [22, 22, 22, 22] | có / có | 1 |  |
| icon vật phẩm | `Assets/_JungleMaze/Art/UI/Icons/Equipments/GOD_LEAF.png` | 52x52 | không (không cắt) | 64x64 / không | 1 | icon vật phẩm có sẵn trong Assets, khóa mock 'Lá thần' |
| khung/thành phần kit | `T-020/kit_progress_track.png` | 360x24 | [12, 5, 12, 5] | có / có | 1 |  |
| khung/thành phần kit | `T-020/kit_progress_fill.png` | 190x16 | [3, 2, 3, 2] | có / có | 1 |  |
| khung/thành phần kit | `T-020/kit_toast.png` | 308x92 | [22, 22, 22, 22] | có / có | 1 |  |
| khung/thành phần kit | `T-020/kit_keycap_active.png` | 52x52 | [14, 14, 14, 14] | có / có | 1 |  |
| icon vật phẩm | `Assets/_JungleMaze/Art/UI/Icons/Weapons/SHOTGUN.png` | 72x72 | không (không cắt) | 64x64 / không | 1 | icon vật phẩm có sẵn trong Assets, khóa mock 'Shotgun' |
| khung/thành phần kit | `T-020/kit_row_alt.png` | 300x68 | [12, 2, 2, 2] | có / có | 3 |  |
| icon vật phẩm | `Assets/_JungleMaze/Art/UI/Icons/Equipments/ARMOR.png` | 56x56 | không (không cắt) | 64x64 / không | 1 | icon vật phẩm có sẵn trong Assets, khóa mock 'Giáp gai' |
| icon vật phẩm | `Assets/_JungleMaze/Art/UI/Icons/Equipments/GOD_LEAF.png` | 56x56 | không (không cắt) | 64x64 / không | 1 | icon vật phẩm có sẵn trong Assets, khóa mock 'Lá thần' |
| icon vật phẩm | `Assets/_JungleMaze/Art/UI/Icons/Weapons/SHOTGUN.png` | 56x56 | không (không cắt) | 64x64 / không | 1 | icon vật phẩm có sẵn trong Assets, khóa mock 'Shotgun' |
| khung/thành phần kit | `T-020/kit_tag_neutral.png` | 90x36 | [8, 6, 8, 6] | có / có | 1 |  |

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
| 8 | 24 | Be Vietnam Pro Bold | #EDF2EE |
| 4 | 24 | Be Vietnam Pro Bold | #D6DAD6 |
| 3 | 24 | Be Vietnam Pro Bold | #EDF2EE |
| 5 | 24 | Be Vietnam Pro Bold | #EDF2EE |
| 9 | 24 | Be Vietnam Pro Bold | #EDF2EE |
| 1,2 | 24 | Be Vietnam Pro Bold | #FFDC6E |
| F | 24 | Be Vietnam Pro Bold | #1F1A10 |
| Shotgun | 24 | Be Vietnam Pro Bold | #EDF2EE |
| + | 24 | Be Vietnam Pro Bold | #EDF2EE |
| Giáp gai | 24 | Be Vietnam Pro Bold | #EDF2EE |
| ×1 | 24 | Be Vietnam Pro Bold | #D6DAD6 |
| Lá thần | 24 | Be Vietnam Pro Bold | #EDF2EE |
| ×2 | 24 | Be Vietnam Pro Bold | #D6DAD6 |
| +2 | 24 | Be Vietnam Pro Bold | #EDF2EE |