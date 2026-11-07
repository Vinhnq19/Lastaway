# b4-tui-do (T-022)

Ảnh mock: `T-021/b/b4-tui-do.png` (1920x1080). Border 9-slice là của sprite @1x (LRTB); @2x nhân đôi.

## Khung, icon, FX, nền

| Phần tử mock | File nguồn | Cỡ vẽ 1080p | Border 9-slice LRTB | @1x / @2x | Số lần | Ghi chú |
|---|---|---:|---|---|---:|---|
| khung/thành phần kit | `files/new/kit_killfeed_panel.png` | 270x154 | [16, 16, 16, 16] | có / có | 1 | panel kill feed, rộng thêm theo chữ (tên + icon vũ khí 32 + tên, 24 px, dòng cách 44), alpha ~0,98 |
| khung/thành phần kit | `files/new/kit_badge_key.png` | 30x30 | [14, 14, 14, 14] | có / có | 8 | nhãn số phím quick bar (1), số 20 px vàng #FFDC6E đè lên |
| nền HUD | `HUD trong trận (Hud.prefab T-016)` | 1920x1080 | không (không cắt) | - | 1 | nền HUD đã có trong Unity (T-016); mock chỉ dựng lại để thấy lớp phủ |
| icon vật phẩm | `Assets/_JungleMaze/Art/UI/Icons/Weapons/CROSSBOW.png` | 32x32 | không (không cắt) | 64x64 / không | 1 | icon vật phẩm có sẵn trong Assets, khóa mock 'Nỏ' |
| icon vật phẩm | `Assets/_JungleMaze/Art/UI/Icons/Weapons/SHOTGUN.png` | 32x32 | không (không cắt) | 64x64 / không | 1 | icon vật phẩm có sẵn trong Assets, khóa mock 'Shotgun' |
| icon vật phẩm | `Assets/_JungleMaze/Art/UI/Icons/Weapons/RUBBER_GUN.png` | 32x32 | không (không cắt) | 64x64 / không | 1 | icon vật phẩm có sẵn trong Assets, khóa mock 'Nỏ cao su' |
| khung/thành phần kit | `T-020/kit_dim.png` | 1920x1080 | không (không cắt) | có / có | 1 | lớp phủ tối toàn màn: kéo giãn full màn, tint alpha 200/255 (màu gốc #0A0A08) |
| khung/thành phần kit | `T-020/kit_panel_l.png` | 1660x860 | [47, 47, 47, 47] | có / có | 1 |  |
| khung/thành phần kit | `T-020/kit_iconbtn_round_normal.png` | 64x64 | không (không cắt) | có / có | 1 | nút tròn kaki làm nền icon |
| icon | `T-020/kit_ico_bag.png` | 44x44 | không (không cắt) | có / có | 1 | icon ink tối (đặt lên nền kaki) |
| khung/thành phần kit | `T-020/kit_keycap_normal.png` | 70x52 | [14, 14, 14, 14] | có / có | 1 |  |
| khung/thành phần kit | `T-020/kit_divider.png` | 1460x14 | [10, 10, 2, 2] | có / có | 1 |  |
| khung/thành phần kit | `T-020/kit_panel_s.png` | 440x590 | [47, 47, 47, 47] | có / có | 1 |  |
| khung/thành phần kit | `T-020/kit_iconbtn_round_normal.png` | 50x50 | không (không cắt) | có / có | 1 | nút tròn kaki làm nền icon |
| icon | `T-020/kit_ico_list.png` | 30x30 | không (không cắt) | có / có | 1 | icon ink tối (đặt lên nền kaki) |
| khung/thành phần kit | `T-020/kit_row_selected.png` | 370x66 | [12, 2, 2, 2] | có / có | 1 |  |
| icon vật phẩm | `Assets/_JungleMaze/Art/UI/Icons/Weapons/SHOTGUN.png` | 56x56 | không (không cắt) | 64x64 / không | 1 | icon vật phẩm có sẵn trong Assets, khóa mock 'Shotgun' |
| khung/thành phần kit | `T-020/kit_keycap_active.png` | 44x52 | [14, 14, 14, 14] | có / có | 1 |  |
| khung/thành phần kit | `T-020/kit_row_normal.png` | 370x66 | [12, 2, 2, 2] | có / có | 3 |  |
| icon vật phẩm | `Assets/_JungleMaze/Art/UI/Icons/Equipments/GOD_LEAF.png` | 56x56 | không (không cắt) | 64x64 / không | 1 | icon vật phẩm có sẵn trong Assets, khóa mock 'Lá thần' |
| icon vật phẩm | `Assets/_JungleMaze/Art/UI/Icons/Equipments/UNICORN_BLOOD.png` | 56x56 | không (không cắt) | 64x64 / không | 1 | icon vật phẩm có sẵn trong Assets, khóa mock 'Máu kỳ lân' |
| icon vật phẩm | `Assets/_JungleMaze/Art/UI/Icons/Weapons/RUBBER_GUN.png` | 56x56 | không (không cắt) | 64x64 / không | 1 | icon vật phẩm có sẵn trong Assets, khóa mock 'Nỏ cao su' |
| khung/thành phần kit | `T-020/kit_panel_s.png` | 680x590 | [47, 47, 47, 47] | có / có | 1 |  |
| icon vật phẩm | `Assets/_JungleMaze/Art/UI/Icons/Weapons/CROSSBOW.png` | 44x44 | không (không cắt) | 64x64 / không | 1 | icon vật phẩm có sẵn trong Assets, khóa mock 'Nỏ' |
| khung/thành phần kit | `T-020/kit_slot_item_normal.png` | 130x140 | không (không cắt) | có / có | 5 |  |
| icon vật phẩm | `Assets/_JungleMaze/Art/UI/Icons/Weapons/HAND.png` | 104x104 | không (không cắt) | 64x64 / không | 1 | icon vật phẩm có sẵn trong Assets, khóa mock 'Tay không' |
| khung/thành phần kit | `T-020/kit_slot_item_selected.png` | 130x140 | không (không cắt) | có / có | 1 |  |
| icon vật phẩm | `Assets/_JungleMaze/Art/UI/Icons/Weapons/CROSSBOW.png` | 104x104 | không (không cắt) | 64x64 / không | 1 | icon vật phẩm có sẵn trong Assets, khóa mock 'Nỏ' |
| icon vật phẩm | `Assets/_JungleMaze/Art/UI/Icons/Weapons/RUBBER_GUN.png` | 104x104 | không (không cắt) | 64x64 / không | 1 | icon vật phẩm có sẵn trong Assets, khóa mock 'Nỏ cao su' |
| khung/thành phần kit | `T-020/kit_slot_item_empty.png` | 130x140 | không (không cắt) | có / có | 2 |  |
| icon vật phẩm | `Assets/_JungleMaze/Art/UI/Icons/Equipments/GOD_LEAF.png` | 44x44 | không (không cắt) | 64x64 / không | 1 | icon vật phẩm có sẵn trong Assets, khóa mock 'Lá thần' |
| icon vật phẩm | `Assets/_JungleMaze/Art/UI/Icons/Equipments/GOD_LEAF.png` | 104x104 | không (không cắt) | 64x64 / không | 1 | icon vật phẩm có sẵn trong Assets, khóa mock 'Lá thần' |
| icon vật phẩm | `Assets/_JungleMaze/Art/UI/Icons/Equipments/UNICORN_BLOOD.png` | 104x104 | không (không cắt) | 64x64 / không | 1 | icon vật phẩm có sẵn trong Assets, khóa mock 'Máu kỳ lân' |
| icon vật phẩm | `Assets/_JungleMaze/Art/UI/Icons/Equipments/ELIXIR.png` | 104x104 | không (không cắt) | 64x64 / không | 1 | icon vật phẩm có sẵn trong Assets, khóa mock 'Tăng lực' |
| khung/thành phần kit | `T-020/kit_panel_s.png` | 390x590 | [47, 47, 47, 47] | có / có | 1 |  |
| icon vật phẩm | `Assets/_JungleMaze/Art/UI/Icons/Equipments/ARMOR.png` | 44x44 | không (không cắt) | 64x64 / không | 1 | icon vật phẩm có sẵn trong Assets, khóa mock 'Giáp gai' |
| khung/thành phần kit | `T-020/kit_row_normal.png` | 320x88 | [12, 2, 2, 2] | có / có | 4 |  |
| icon vật phẩm | `Assets/_JungleMaze/Art/UI/Icons/Equipments/WOODEN_HELMET.png` | 72x72 | không (không cắt) | 64x64 / không | 1 | icon vật phẩm có sẵn trong Assets, khóa mock 'Mũ gỗ' |
| icon vật phẩm | `Assets/_JungleMaze/Art/UI/Icons/Equipments/ARMOR.png` | 72x72 | không (không cắt) | 64x64 / không | 1 | icon vật phẩm có sẵn trong Assets, khóa mock 'Giáp gai' |
| icon vật phẩm | `Assets/_JungleMaze/Art/UI/Icons/Equipments/ADRENALINE.png` | 72x72 | không (không cắt) | 64x64 / không | 1 | icon vật phẩm có sẵn trong Assets, khóa mock 'Adrenaline' |
| khung/thành phần kit | `T-020/kit_slot_item_empty.png` | 72x72 | không (không cắt) | có / có | 1 | ô giữ chỗ trong mock; dùng `files/new/icon_ammo.png` (64x64, gen T-028) cho đạn |

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
| Tab | 24 | Be Vietnam Pro Bold | #1F1A10 |
| F | 24 | Be Vietnam Pro Bold | #1F1A10 |
| ×2 | 24 | Be Vietnam Pro Bold | #D6DAD6 |
| ×1 | 24 | Be Vietnam Pro Bold | #D6DAD6 |
| 1 | 24 | Be Vietnam Pro Bold | #D6DAD6 |
| 2 | 24 | Be Vietnam Pro Bold | #D6DAD6 |
| 3 | 24 | Be Vietnam Pro Bold | #D6DAD6 |
| 4 | 24 | Be Vietnam Pro Bold | #D6DAD6 |
| 5 | 24 | Be Vietnam Pro Bold | #D6DAD6 |
| ×6 | 24 | Be Vietnam Pro Bold | #EDF2EE |
| 6 | 24 | Be Vietnam Pro Bold | #D6DAD6 |
| ×1 | 24 | Be Vietnam Pro Bold | #EDF2EE |
| 7 | 24 | Be Vietnam Pro Bold | #D6DAD6 |
| 8 | 24 | Be Vietnam Pro Bold | #D6DAD6 |
| ×4 | 24 | Be Vietnam Pro Bold | #EDF2EE |
| 18 | 30 | Be Vietnam Pro Bold | #EDF2EE |
| 48 | 30 | Be Vietnam Pro Bold | #EDF2EE |
| 55 | 30 | Be Vietnam Pro Bold | #EDF2EE |
| 4/90 | 30 | Be Vietnam Pro Bold | #EDF2EE |

## Asset mới T-028 dùng cho màn này

| Chỗ trong mock | File mới | Cỡ vẽ 1080p |
|---|---|---:|
| dòng Đạn cột Trang bị (ô giữ chỗ 4/90) | `files/new/icon_ammo.png` (+ @2x) | 72x72 |
| tiêu đề cột trái đồ ở gần (mock dùng `kit_ico_list` tạm) | `files/new/icon_ground.png` (+ @2x) | 30x30 trên nút tròn |