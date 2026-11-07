# T-028 bảng tổng file trong `files/`

Toạ độ và cỡ trong các bảng màn là **1080p (1920x1080)**. Unity (CanvasScaler tham chiếu 1280x720, match width) quy đổi chia 1,5. Border 9-slice là LRTB của sprite @1x (px ảnh gốc); sprite @2x có border gấp đôi.

## Kit T-020 và T-026 (`files/kit/`, meta ở `files/kit/meta.json`)

| File | Cỡ gốc @1x | Border LRTB | @2x | Dùng ở màn |
|---|---:|---|---|---|
| `kit_btn_danger_disabled.png` | 240x64 | [22, 22, 28, 28] | có | - |
| `kit_btn_danger_hover.png` | 240x64 | [22, 22, 28, 28] | có | - |
| `kit_btn_danger_normal.png` | 240x64 | [22, 22, 28, 28] | có | a4-xoa-phong |
| `kit_btn_danger_pressed.png` | 240x64 | [22, 22, 28, 28] | có | - |
| `kit_btn_primary_disabled.png` | 240x64 | [22, 22, 28, 28] | có | a3-vao-phong-dang-ket-noi, a4-phong-cua-toi, a4-xoa-phong, a5-tao-phong-loi, b2-bi-loai-het-nguoi |
| `kit_btn_primary_hover.png` | 240x64 | [22, 22, 28, 28] | có | - |
| `kit_btn_primary_normal.png` | 240x64 | [22, 22, 28, 28] | có | a2-sanh-chinh, a3-vao-phong, a3-vao-phong-dang-ket-noi, a3-vao-phong-loi, a3b-dang-nhap, a3b-dang-nhap-loi ... |
| `kit_btn_primary_pressed.png` | 240x64 | [22, 22, 28, 28] | có | - |
| `kit_btn_secondary_disabled.png` | 240x64 | [22, 22, 28, 28] | có | - |
| `kit_btn_secondary_hover.png` | 240x64 | [22, 22, 28, 28] | có | - |
| `kit_btn_secondary_normal.png` | 240x64 | [22, 22, 28, 28] | có | a2-sanh-chinh, a3-vao-phong, a3-vao-phong-dang-ket-noi, a3-vao-phong-loi, a3b-dang-nhap, a3b-dang-nhap-loi ... |
| `kit_btn_secondary_pressed.png` | 240x64 | [22, 22, 28, 28] | có | - |
| `kit_card_cause.png` | 768x150 | [110, 28, 28, 28] | có | b2-bi-loai, b2-bi-loai-bo, b2-bi-loai-het-nguoi, b7-thua |
| `kit_card_room_full.png` | 615x120 | [24, 24, 24, 24] | có | a4-phong-cua-toi, a4-xoa-phong, a6-phong-cho-cho-du-nguoi, a6-phong-cho-dem-nguoc |
| `kit_card_room_normal.png` | 619x120 | [24, 24, 24, 24] | có | a4-phong-cua-toi, a4-phong-cua-toi-trong, a4-xoa-phong |
| `kit_card_room_selected.png` | 615x120 | [24, 24, 24, 24] | có | a4-phong-cua-toi, a4-xoa-phong, b3-spectator, b3-spectator-chuyen, b3-spectator-het |
| `kit_card_stat.png` | 278x130 | [28, 28, 28, 28] | có | b2-bi-loai, b2-bi-loai-bo, b2-bi-loai-het-nguoi, b3-spectator, b3-spectator-chuyen, b3-spectator-het ... |
| `kit_close_normal.png` | 52x52 | không | có | a3-vao-phong, a3-vao-phong-dang-ket-noi, a3-vao-phong-loi, a4-xoa-phong, a5-tao-phong, a5-tao-phong-loi ... |
| `kit_codebox_active.png` | 69x78 | không | có | a3-vao-phong |
| `kit_codebox_error.png` | 69x78 | không | có | a3-vao-phong-loi |
| `kit_codebox_filled.png` | 69x78 | không | có | a3-vao-phong, a3-vao-phong-dang-ket-noi |
| `kit_dialog_footer.png` | 1041x96 | [8, 8, 8, 8] | có | a3-vao-phong, a3-vao-phong-dang-ket-noi, a3-vao-phong-loi, a4-xoa-phong, a5-tao-phong, a5-tao-phong-loi ... |
| `kit_dim.png` | 64x64 | không | có | a3-vao-phong, a3-vao-phong-dang-ket-noi, a3-vao-phong-loi, a4-xoa-phong, a5-tao-phong, a5-tao-phong-loi ... |
| `kit_divider.png` | 1069x14 | [10, 10, 2, 2] | có | b1-esc-cai-dat, b1-esc-xac-nhan-thoat, b4-tui-do, b4-tui-do-tooltip, b4-tui-do-trong |
| `kit_ico_arrow_left.png` | 48x48 | không | có | a4-phong-cua-toi, a4-phong-cua-toi-loi, a4-phong-cua-toi-trong, a4-xoa-phong, a6-phong-cho-cho-du-nguoi, a6-phong-cho-dem-nguoc ... |
| `kit_ico_arrow_right.png` | 48x48 | không | có | b3-spectator-chuyen, b5-ban-do-lon |
| `kit_ico_bag.png` | 48x48 | không | có | b4-tui-do, b4-tui-do-tooltip, b4-tui-do-trong |
| `kit_ico_check.png` | 48x48 | không | có | a6-phong-cho-cho-du-nguoi, a6-phong-cho-dem-nguoc |
| `kit_ico_clock.png` | 48x48 | không | có | a2-sanh-chinh, a3-vao-phong, a3-vao-phong-dang-ket-noi, a3-vao-phong-loi, a5-tao-phong, a5-tao-phong-loi ... |
| `kit_ico_copy.png` | 48x48 | không | có | a4-phong-cua-toi, a4-xoa-phong, a6-phong-cho-cho-du-nguoi, a6-phong-cho-dem-nguoc |
| `kit_ico_door.png` | 48x48 | không | có | a2-sanh-chinh, a3-vao-phong, a3-vao-phong-dang-ket-noi, a3-vao-phong-loi, a5-tao-phong, a5-tao-phong-loi |
| `kit_ico_eye.png` | 48x48 | không | có | b2-bi-loai, b2-bi-loai-bo, b2-bi-loai-het-nguoi, b3-spectator, b3-spectator-chuyen, b3-spectator-het |
| `kit_ico_gear.png` | 48x48 | không | có | a2-sanh-chinh, a3-vao-phong, a3-vao-phong-dang-ket-noi, a3-vao-phong-loi, a5-tao-phong, a5-tao-phong-loi |
| `kit_ico_home.png` | 48x48 | không | có | a7-dang-tai-loi, b2-bi-loai, b2-bi-loai-bo, b2-bi-loai-het-nguoi, b6-thang, b7-thua ... |
| `kit_ico_key.png` | 48x48 | không | có | a3b-dang-nhap, a3b-dang-nhap-loi |
| `kit_ico_list.png` | 48x48 | không | có | b4-tui-do, b4-tui-do-tooltip, b4-tui-do-trong |
| `kit_ico_logout.png` | 48x48 | không | có | a2-sanh-chinh, a3-vao-phong, a3-vao-phong-dang-ket-noi, a3-vao-phong-loi, a5-tao-phong, a5-tao-phong-loi |
| `kit_ico_music.png` | 48x48 | không | có | b1-esc-cai-dat, b1-esc-xac-nhan-thoat |
| `kit_ico_people.png` | 48x48 | không | có | b3-spectator, b3-spectator-chuyen, b3-spectator-het |
| `kit_ico_person.png` | 48x48 | không | có | a2-sanh-chinh, a3-vao-phong, a3-vao-phong-dang-ket-noi, a3-vao-phong-loi, a5-tao-phong, a5-tao-phong-loi ... |
| `kit_ico_play.png` | 48x48 | không | có | a2-sanh-chinh, a3-vao-phong, a3-vao-phong-dang-ket-noi, a3-vao-phong-loi, a4-phong-cua-toi, a4-phong-cua-toi-trong ... |
| `kit_ico_plus.png` | 48x48 | không | có | a2-sanh-chinh, a3-vao-phong, a3-vao-phong-dang-ket-noi, a3-vao-phong-loi, a4-phong-cua-toi, a4-phong-cua-toi-loi ... |
| `kit_ico_refresh.png` | 48x48 | không | có | a4-phong-cua-toi-loi, b1-esc-cai-dat, b1-esc-xac-nhan-thoat, b3-spectator-het, b6-thang, b7-thua ... |
| `kit_ico_server.png` | 48x48 | không | có | a3b-dang-nhap, a3b-dang-nhap-loi |
| `kit_ico_skull.png` | 48x48 | không | có | b2-bi-loai, b2-bi-loai-het-nguoi, b3-spectator, b3-spectator-chuyen, b3-spectator-het, b7-thua |
| `kit_ico_sound.png` | 48x48 | không | có | b1-esc-cai-dat, b1-esc-xac-nhan-thoat |
| `kit_ico_trash.png` | 48x48 | không | có | a4-phong-cua-toi, a4-xoa-phong |
| `kit_ico_trophy.png` | 48x48 | không | có | a2-sanh-chinh, a3-vao-phong, a3-vao-phong-dang-ket-noi, a3-vao-phong-loi, a5-tao-phong, a5-tao-phong-loi ... |
| `kit_ico_vibrate.png` | 48x48 | không | có | b1-esc-cai-dat, b1-esc-xac-nhan-thoat |
| `kit_ico_wifi.png` | 48x48 | không | có | a1-khoi-dong |
| `kit_icon_check.png` | 40x32 | không | có | a1-khoi-dong, a2-sanh-chinh, a3-vao-phong, a3-vao-phong-dang-ket-noi, a3-vao-phong-loi, a5-tao-phong ... |
| `kit_icon_cross.png` | 32x32 | không | có | a3-vao-phong-loi, a3b-dang-nhap-loi, a4-phong-cua-toi-loi, a5-tao-phong-loi, a7-dang-tai-loi, b8-mat-ket-noi-that-bai |
| `kit_icon_cross_ink.png` | 32x32 | không | có | a3-vao-phong, a3-vao-phong-dang-ket-noi, a3-vao-phong-loi, a4-xoa-phong, a5-tao-phong, a5-tao-phong-loi |
| `kit_icon_cross_red.png` | 32x32 | không | có | a3-vao-phong, a3-vao-phong-dang-ket-noi, a3-vao-phong-loi, a4-xoa-phong, a5-tao-phong, a5-tao-phong-loi ... |
| `kit_icon_dot.png` | 32x32 | không | có | a7-dang-tai |
| `kit_icon_warn.png` | 36x32 | không | có | b5-ban-do-lon, b9-phan-hoi-hud, b9-phan-hoi-mau-thap |
| `kit_iconbtn_round_normal.png` | 64x64 | không | có | a1-khoi-dong, a2-sanh-chinh, a3-vao-phong, a3-vao-phong-dang-ket-noi, a3-vao-phong-loi, a5-tao-phong ... |
| `kit_iconbtn_square_normal.png` | 64x64 | không | có | a4-phong-cua-toi, a4-phong-cua-toi-loi, a4-phong-cua-toi-trong, a4-xoa-phong, a6-phong-cho-cho-du-nguoi, a6-phong-cho-dem-nguoc ... |
| `kit_input_error.png` | 360x56 | [19, 19, 19, 19] | có | a3b-dang-nhap-loi |
| `kit_input_normal.png` | 360x56 | [19, 19, 19, 19] | có | a3-vao-phong, a3-vao-phong-dang-ket-noi, a3-vao-phong-loi, a3b-dang-nhap, a5-tao-phong, a5-tao-phong-loi |
| `kit_keycap_active.png` | 50x52 | [14, 14, 14, 14] | có | b4-tui-do, b4-tui-do-tooltip, b9-phan-hoi-hud, b9-phan-hoi-mau-thap |
| `kit_keycap_normal.png` | 50x52 | [14, 14, 14, 14] | có | a7-dang-tai, a7-dang-tai-loi, b3-spectator, b3-spectator-chuyen, b3-spectator-het, b4-tui-do ... |
| `kit_knob.png` | 31x32 | không | có | a5-tao-phong, a5-tao-phong-loi, b1-esc-cai-dat, b1-esc-xac-nhan-thoat |
| `kit_panel_l.png` | 1000x640 | [47, 47, 47, 47] | có | b4-tui-do, b4-tui-do-tooltip, b4-tui-do-trong, b5-ban-do-lon |
| `kit_panel_m.png` | 640x400 | [47, 47, 47, 47] | có | a3b-dang-nhap, a3b-dang-nhap-loi, a6-phong-cho-cho-du-nguoi, a6-phong-cho-dem-nguoc, b2-bi-loai, b2-bi-loai-bo ... |
| `kit_panel_popup.png` | 720x480 | [47, 47, 88, 47] | có | a3-vao-phong, a3-vao-phong-dang-ket-noi, a3-vao-phong-loi, a4-xoa-phong, a5-tao-phong, a5-tao-phong-loi ... |
| `kit_panel_s.png` | 360x240 | [47, 47, 47, 47] | có | a7-dang-tai, a7-dang-tai-loi, b4-tui-do, b4-tui-do-tooltip, b4-tui-do-trong, b5-ban-do-lon |
| `kit_progress_fill.png` | 470x16 | [3, 2, 3, 2] | có | a1-khoi-dong, a3-vao-phong-dang-ket-noi, a7-dang-tai, b3-spectator, b3-spectator-chuyen, b3-spectator-het ... |
| `kit_progress_track.png` | 480x28 | [12, 5, 12, 5] | có | a1-khoi-dong, a3-vao-phong-dang-ket-noi, a7-dang-tai, b3-spectator, b3-spectator-chuyen, b3-spectator-het ... |
| `kit_row_alt.png` | 600x48 | [12, 2, 2, 2] | có | b3-spectator, b3-spectator-chuyen, b6-thang, b7-thua, b9-phan-hoi-hud, b9-phan-hoi-mau-thap |
| `kit_row_normal.png` | 600x48 | [12, 2, 2, 2] | có | b3-spectator, b3-spectator-chuyen, b4-tui-do, b4-tui-do-tooltip, b4-tui-do-trong, b6-thang ... |
| `kit_row_selected.png` | 600x48 | [12, 2, 2, 2] | có | b3-spectator, b3-spectator-chuyen, b3-spectator-het, b4-tui-do, b4-tui-do-tooltip, b6-thang ... |
| `kit_screen_header.png` | 1210x104 | [36, 36, 12, 12] | có | a4-phong-cua-toi, a4-phong-cua-toi-loi, a4-phong-cua-toi-trong, a4-xoa-phong, a6-phong-cho-cho-du-nguoi, a6-phong-cho-dem-nguoc |
| `kit_slider_fill.png` | 320x10 | [2, 1, 2, 1] | có | a5-tao-phong, a5-tao-phong-loi, b1-esc-cai-dat, b1-esc-xac-nhan-thoat |
| `kit_slider_track.png` | 320x16 | [7, 3, 7, 3] | có | a5-tao-phong, a5-tao-phong-loi, b1-esc-cai-dat, b1-esc-xac-nhan-thoat |
| `kit_slot_item_empty.png` | 139x140 | không | có | b4-tui-do, b4-tui-do-tooltip, b4-tui-do-trong |
| `kit_slot_item_normal.png` | 139x140 | không | có | b4-tui-do, b4-tui-do-tooltip, b4-tui-do-trong |
| `kit_slot_item_selected.png` | 139x140 | không | có | b4-tui-do, b4-tui-do-tooltip, b4-tui-do-trong |
| `kit_slot_player_notready.png` | 371x96 | [100, 40, 24, 24] | có | a6-phong-cho-cho-du-nguoi, a6-phong-cho-dem-nguoc |
| `kit_tab_off.png` | 160x64 | [19, 19, 19, 1] | có | a3b-dang-nhap, a3b-dang-nhap-loi |
| `kit_tab_on.png` | 160x64 | [19, 19, 19, 1] | có | a3b-dang-nhap, a3b-dang-nhap-loi |
| `kit_tag_neutral.png` | 110x36 | [8, 6, 8, 6] | có | a4-phong-cua-toi, a4-phong-cua-toi-trong, a4-xoa-phong, b9-phan-hoi-hud, b9-phan-hoi-mau-thap |
| `kit_tip_item.png` | 387x230 | [30, 30, 30, 30] | có | b4-tui-do-tooltip |
| `kit_toast.png` | 360x64 | [22, 22, 22, 22] | có | a2-sanh-chinh, a3-vao-phong, a3-vao-phong-dang-ket-noi, a3-vao-phong-loi, a4-phong-cua-toi-loi, a5-tao-phong ... |
| `kit_toggle_off.png` | 114x48 | không | có | b1-esc-cai-dat, b1-esc-xac-nhan-thoat |
| `kit_toggle_on.png` | 114x48 | không | có | b1-esc-cai-dat, b1-esc-xac-nhan-thoat |

## FX và emblem (`files/fx/`, gen GPT nét cũ, Huy giữ)

| File | Cỡ gốc | Dùng ở màn |
|---|---:|---|
| `fx_damage_arc.png` | 118x392 | b9-phan-hoi-hud, b9-phan-hoi-mau-thap |
| `fx_danger_arrow.png` | 206x289 | b9-phan-hoi-hud, b9-phan-hoi-mau-thap |
| `fx_emblem_lose.png` | 314x309 | b7-thua |
| `fx_emblem_win.png` | 378x353 | b6-thang |
| `fx_hitmarker.png` | 216x206 | b9-phan-hoi-hud, b9-phan-hoi-mau-thap |
| `fx_icon_disconnect.png` | 234x200 | b8-mat-ket-noi, b8-mat-ket-noi-het-han, b8-mat-ket-noi-that-bai |
| `fx_map_drop.png` | 224x286 | b5-ban-do-lon |
| `fx_map_mark.png` | 104x153 | b5-ban-do-lon |
| `fx_map_player.png` | 124x168 | b5-ban-do-lon |

## Icon vật phẩm (`files/items/`, bản sao từ `Assets/_JungleMaze/Art/`, đường dẫn gốc ghi cạnh)

| File | Nguồn trong Assets | Dùng ở màn |
|---|---|---|
| `icon_alive.png` | `Assets/_JungleMaze/Art/Hud/StatusIcons/icon_alive.png` | b3-spectator, b3-spectator-chuyen, b3-spectator-het |
| `icon_hp.png` | `Assets/_JungleMaze/Art/Hud/StatusIcons/icon_hp.png` | b3-spectator, b3-spectator-chuyen, b3-spectator-het |
| `icon_kill.png` | `Assets/_JungleMaze/Art/Hud/StatusIcons/icon_kill.png` | b2-bi-loai, b2-bi-loai-het-nguoi, b6-thang, b7-thua |
| `icon_zone-timer.png` | `Assets/_JungleMaze/Art/Hud/StatusIcons/icon_zone-timer.png` | b2-bi-loai, b2-bi-loai-bo, b2-bi-loai-het-nguoi, b5-ban-do-lon, b6-thang, b7-thua ... |
| `Invisible.png` | `Assets/_JungleMaze/Art/UI/Icons/Effects/Invisible.png` | b9-phan-hoi-hud, b9-phan-hoi-mau-thap |
| `Poison.png` | `Assets/_JungleMaze/Art/UI/Icons/Effects/Poison.png` | b9-phan-hoi-hud, b9-phan-hoi-mau-thap |
| `Slow.png` | `Assets/_JungleMaze/Art/UI/Icons/Effects/Slow.png` | b9-phan-hoi-hud, b9-phan-hoi-mau-thap |
| `ADRENALINE.png` | `Assets/_JungleMaze/Art/UI/Icons/Equipments/ADRENALINE.png` | b4-tui-do, b4-tui-do-tooltip, b4-tui-do-trong |
| `ARMOR.png` | `Assets/_JungleMaze/Art/UI/Icons/Equipments/ARMOR.png` | b4-tui-do, b4-tui-do-tooltip, b4-tui-do-trong, b9-phan-hoi-hud, b9-phan-hoi-mau-thap |
| `ELIXIR.png` | `Assets/_JungleMaze/Art/UI/Icons/Equipments/ELIXIR.png` | b4-tui-do, b4-tui-do-tooltip, b4-tui-do-trong |
| `GOD_LEAF.png` | `Assets/_JungleMaze/Art/UI/Icons/Equipments/GOD_LEAF.png` | b4-tui-do, b4-tui-do-tooltip, b4-tui-do-trong, b9-phan-hoi-hud, b9-phan-hoi-mau-thap |
| `UNICORN_BLOOD.png` | `Assets/_JungleMaze/Art/UI/Icons/Equipments/UNICORN_BLOOD.png` | b4-tui-do, b4-tui-do-tooltip, b4-tui-do-trong |
| `WOODEN_HELMET.png` | `Assets/_JungleMaze/Art/UI/Icons/Equipments/WOODEN_HELMET.png` | b4-tui-do, b4-tui-do-tooltip, b4-tui-do-trong |
| `CROSSBOW.png` | `Assets/_JungleMaze/Art/UI/Icons/Weapons/CROSSBOW.png` | b1-esc-cai-dat, b1-esc-xac-nhan-thoat, b2-bi-loai, b2-bi-loai-bo, b2-bi-loai-het-nguoi, b3-spectator ... |
| `HAND.png` | `Assets/_JungleMaze/Art/UI/Icons/Weapons/HAND.png` | b4-tui-do, b4-tui-do-tooltip, b4-tui-do-trong |
| `RUBBER_GUN.png` | `Assets/_JungleMaze/Art/UI/Icons/Weapons/RUBBER_GUN.png` | b1-esc-cai-dat, b1-esc-xac-nhan-thoat, b2-bi-loai, b2-bi-loai-bo, b2-bi-loai-het-nguoi, b4-tui-do ... |
| `SHOTGUN.png` | `Assets/_JungleMaze/Art/UI/Icons/Weapons/SHOTGUN.png` | b1-esc-cai-dat, b1-esc-xac-nhan-thoat, b2-bi-loai, b2-bi-loai-bo, b2-bi-loai-het-nguoi, b4-tui-do ... |
| `IconMouse.png` | `Assets/_JungleMaze/Art/UI/MatchOverlay/IconMouse.png` | b5-ban-do-lon |

## Nền (`files/bg/`)

| File | Ghi chú |
|---|---|
| `nen-gach-tile.png` (510x280) | ô nền gạch lát kín màn 1920x1080 (cắt từ ảnh HUD concept T-015); phủ tối alpha 0,62 đến 0,7 và vignette gradient chạy lúc render |

## Asset gen mới T-028 (`files/new/`, qua cổng duyệt của CPO)

| File | Cỡ @1x | Border LRTB | @2x | Nguồn |
|---|---:|---|---|---|
| `icon_ammo.png` | 64x64 | không | có | gen GPT qua codex-img.sh, prompt `src/gen/ammo.prompt.src.txt`, ref nét: kit T-020 + icon Assets |
| `icon_ground.png` | 64x64 | không | có | gen GPT qua codex-img.sh, prompt `src/gen/ground.prompt.src.txt`, ref nét: kit T-020 + icon Assets |
| `kit_badge_key.png` | 48x48 | [14, 14, 14, 14] | có | gen GPT qua codex-img.sh, prompt `src/gen/badge-key.prompt.src.txt`, ref nét: kit T-020 + icon Assets |
| `kit_killfeed_panel.png` | 192x64 | [16, 16, 16, 16] | có | gen GPT qua codex-img.sh, prompt `src/gen/killfeed.prompt.src.txt`; panel kill feed (Image #14120E nền, kéo giãn theo số dòng) |
| `kit_map_bg.png` | 256x256 | [24, 24, 24, 24] | có | gen GPT qua codex-img.sh, prompt `src/gen/mapbg.prompt.src.txt`; nền khung bản đồ lớn |
| `map_grid.png` | 840x840 | không | có | gen GPT 1 ô lưới (prompt `src/gen/gridtile.prompt.src.txt`: chỉ hai nét mảnh trên và trái, độ tương phản thấp), script lát 10x10 ô 83 px vào vùng trong của `kit_map_bg` (lề 5 px mỗi cạnh, mép phải và dưới do khung đóng); lưới 840x840, nền trong suốt |
| `map_ring_zone.png` | 512x512 | không | có | gen GPT qua codex-img.sh, prompt `src/gen/ringzone.prompt.src.txt`; vòng bo hiện tại (nét vàng cam), vẽ 680x680 |
| `map_ring_safe.png` | 256x256 | không | có | gen GPT qua codex-img.sh, prompt `src/gen/ringsafe.prompt.src.txt`; vòng bo tiếp theo, vùng an toàn (nét trắng), vẽ 180x180; cỡ 32x32 làm chú giải |
| `kit_ico_practice.png` | 48x48 | không | có | gen GPT qua codex-img.sh, prompt `src/gen/practice.prompt.src.txt`; icon mục tối cho nút Luyện tập offline (bia tập bắn có mũi tên) |
| `kit_ico_save.png` | 48x48 | không | có | gen GPT qua codex-img.sh, prompt `src/gen/save.prompt.src.txt`; icon mục tối cho nút Lưu endpoint (đĩa mềm) |
| `kit_ico_globe.png` | 48x48 | không | có | gen GPT qua codex-img.sh, prompt `src/gen/globe.prompt.src.txt`; icon mục tối cho nút Xem online (quả địa cầu) |
| `kit_ico_fill.png` | 48x48 | không | có | gen GPT qua codex-img.sh, prompt `src/gen/fill.prompt.src.txt`; icon mục tối cho nút Điền đủ (nhóm 3 người và dấu cộng) |
| `icon_zone_cause.png` | 64x64 | không | có | gen GPT qua codex-img.sh, prompt `src/gen/zonecause.prompt.src.txt`; icon màu nguyên nhân chết Bo thu v2 (khói bo đỏ lấn vào nền đá, nhân vật nhỏ ở ngoài vòng; lấp khoảng 90% ô) |