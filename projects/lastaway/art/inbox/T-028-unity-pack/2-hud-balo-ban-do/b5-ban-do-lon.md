# b5-ban-do-lon (T-022)

Ảnh mock: `T-021/b/b5-ban-do-lon.png` (1920x1080). Border 9-slice là của sprite @1x (LRTB); @2x nhân đôi.

## Khung, icon, FX, nền

| Phần tử mock | File nguồn | Cỡ vẽ 1080p | Border 9-slice LRTB | @1x / @2x | Số lần | Ghi chú |
|---|---|---:|---|---|---:|---|
| khung/thành phần kit | `files/new/kit_map_bg.png` | 840x840 | [24, 24, 24, 24] | có / có | 1 | nền khung bản đồ lớn (bản đồ thật vẽ lúc chạy trên nền này) |
| khung/thành phần kit | `files/new/map_grid.png` | 840x840 | không (không cắt) | có / có | 1 | lưới 10x10 phủ lên bản đồ, nền trong suốt (giữ chỗ trong mock) |
| khung/thành phần kit | `files/new/map_ring_zone.png` | 680x680 | không (không cắt) | có / có | 1 | vòng bo hiện tại, tâm (530,530) |
| khung/thành phần kit | `files/new/map_ring_safe.png` | 180x180 | không (không cắt) | có / có | 1 | vòng bo tiếp theo (an toàn), tâm (570,480) |
| khung/thành phần kit | `files/new/legend_ring_next_dashed.png` | 32x32 | không (không cắt) | có / có | 1 | chú giải vòng bo kế tiếp: vòng trắng nét đứt (Huy chốt 07/11, theo kiểu main) |
| khung/thành phần kit | `files/new/kit_killfeed_panel.png` | 270x154 | [16, 16, 16, 16] | có / có | 1 | panel kill feed, rộng thêm theo chữ (tên + icon vũ khí 32 + tên, 24 px, dòng cách 44), alpha ~0,98 |
| khung/thành phần kit | `files/new/kit_badge_key.png` | 30x30 | [14, 14, 14, 14] | có / có | 8 | nhãn số phím quick bar (1), số 20 px vàng #FFDC6E đè lên |
| nền HUD | `HUD trong trận (Hud.prefab T-016)` | 1920x1080 | không (không cắt) | - | 1 | nền HUD đã có trong Unity (T-016); mock chỉ dựng lại để thấy lớp phủ |
| icon vật phẩm | `Assets/_JungleMaze/Art/UI/Icons/Weapons/CROSSBOW.png` | 32x32 | không (không cắt) | 64x64 / không | 1 | icon vật phẩm có sẵn trong Assets, khóa mock 'Nỏ' |
| icon vật phẩm | `Assets/_JungleMaze/Art/UI/Icons/Weapons/SHOTGUN.png` | 32x32 | không (không cắt) | 64x64 / không | 1 | icon vật phẩm có sẵn trong Assets, khóa mock 'Shotgun' |
| icon vật phẩm | `Assets/_JungleMaze/Art/UI/Icons/Weapons/RUBBER_GUN.png` | 32x32 | không (không cắt) | 64x64 / không | 1 | icon vật phẩm có sẵn trong Assets, khóa mock 'Nỏ cao su' |
| khung/thành phần kit | `T-020/kit_dim.png` | 1920x1080 | không (không cắt) | có / có | 1 | lớp phủ tối toàn màn: kéo giãn full màn, tint alpha 215/255 (màu gốc #0A0A08) |
| khung/thành phần kit | `T-020/kit_panel_l.png` | 1090x1000 | [47, 47, 47, 47] | có / có | 1 |  |
| FX/emblem | `T-021/b/assets/fx_map_player.png` | 49x66 | không (không cắt) | có / không | 1 | FX/emblem gen GPT (nét cũ, Huy giữ) |
| FX/emblem | `T-021/b/assets/fx_map_mark.png` | 48x70 | không (không cắt) | có / không | 1 | FX/emblem gen GPT (nét cũ, Huy giữ) |
| FX/emblem | `T-021/b/assets/fx_map_drop.png` | 70x90 | không (không cắt) | có / không | 1 | FX/emblem gen GPT (nét cũ, Huy giữ) |
| khung/thành phần kit | `T-020/kit_panel_s.png` | 700x150 | [47, 47, 47, 47] | có / có | 1 |  |
| icon | `T-020/kit_icon_warn.png` | 50x44 | không (không cắt) | có / có | 1 |  |
| icon vật phẩm | `Assets/_JungleMaze/Art/Hud/StatusIcons/icon_zone-timer.png` | 56x56 | không (không cắt) | 64x64 / không | 1 | icon vật phẩm có sẵn trong Assets, khóa mock 'zone' |
| FX/emblem | `T-021/b/assets/fx_map_player.png` | 38x52 | không (không cắt) | có / không | 2 | FX/emblem gen GPT (nét cũ, Huy giữ) |
| khung/thành phần kit | `T-020/kit_panel_s.png` | 700x130 | [47, 47, 47, 47] | có / có | 1 |  |
| icon | `T-020/kit_icon_warn.png` | 45x40 | không (không cắt) | có / có | 1 |  |
| FX/emblem | `T-021/b/assets/fx_map_drop.png` | 41x52 | không (không cắt) | có / không | 1 | FX/emblem gen GPT (nét cũ, Huy giữ) |
| FX/emblem | `T-021/b/assets/fx_map_mark.png` | 35x52 | không (không cắt) | có / không | 2 | FX/emblem gen GPT (nét cũ, Huy giữ) |
| khung/thành phần kit | `T-020/kit_panel_s.png` | 700x360 | [47, 47, 47, 47] | có / có | 1 |  |
| icon vật phẩm | `Assets/_JungleMaze/Art/UI/MatchOverlay/IconMouse.png` | 52x52 | không (không cắt) | 64x64 / không | 3 | icon vật phẩm có sẵn trong Assets, khóa mock 'mouse' |
| khung/thành phần kit | `T-020/kit_iconbtn_round_normal.png` | 46x46 | không (không cắt) | có / có | 4 | nút tròn kaki làm nền icon |
| icon | `T-020/kit_ico_arrow_right.png` | 26x26 | không (không cắt) | có / có | 4 | icon ink tối (đặt lên nền kaki) |
| khung/thành phần kit | `T-020/kit_iconbtn_round_normal.png` | 50x50 | không (không cắt) | có / có | 3 | nút tròn kaki làm nền icon |
| icon | `T-020/kit_ico_plus.png` | 30x30 | không (không cắt) | có / có | 1 | icon ink tối (đặt lên nền kaki) |
| icon | `T-020/kit_ico_arrow_left.png` | 30x30 | không (không cắt) | có / có | 1 | icon ink tối (đặt lên nền kaki) |
| icon | `T-020/kit_ico_arrow_right.png` | 30x30 | không (không cắt) | có / có | 1 | icon ink tối (đặt lên nền kaki) |
| khung/thành phần kit | `T-020/kit_keycap_normal.png` | 52x52 | [14, 14, 14, 14] | có / có | 1 |  |
| icon | `T-020/kit_icon_cross_red.png` | 36x36 | không (không cắt) | có / có | 1 |  |

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
| 3 | 40 | Chakra Petch Bold | #FFDC6E |
| 01:24 | 40 | Chakra Petch Bold | #EDF2EE |
| 420 | 40 | Chakra Petch Bold | #EDF2EE |
| M | 24 | Be Vietnam Pro Bold | #1F1A10 |