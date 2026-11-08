# UI kit skin A (T-020, chặng 1) - nét HUD r11

Nguồn: GPT qua Codex (`codex-img.sh -e medium`), nền magenta `#FF00FF`, ref: `src/ref-hud.png` (dung-thu-hud-A) + `src/ref-frames.png` (sheet khung r7). Script chỉ tách nền, cắt, 9-slice, đổi cỡ, đo: `src/tools/build_kit.py` (dựng sprite), `compose_kit.py` (trang so sánh, màn thử). Không dựng tay.

Ra: sprite `kit_*.png` và `@2x` (1080p), `meta.json` (size, border LRTB), `ui-kit-sheet.png` (nền gameplay), `ui-kit-sheet-dark.png` (nền tối), `man-thu-cai-dat-1920x1080.png` (popup Cài đặt dựng bằng kit), `src/contrast_kit.md`, `src/build_log.json`.

## 1. GPT: sheet, prompt, vòng

| Sheet | Ảnh | Prompt | Vòng | Ra |
|---|---|---|---|---|
| Nút | `src/sheet-buttons.png` | `src/sheet-buttons.prompt.src.txt` | 1 | 12 nút (3 loại x 4 trạng thái) |
| Panel (vòng đầu, bỏ vì góc to) | `src/sheet-panels.png` | `src/sheet-panels.prompt.src.txt` | 1 | không dùng nữa |
| Panel vòng sửa 1, 2 | `src/sheet-panels2.png`, `src/sheet-panels3.png` | `src/sheet-panels2.prompt.src.txt`, `sheet-panels3.prompt.src.txt` (ref: `ref-minimap.png`, `ref-corner-mm.png` phóng 2x) | 2 | S, M, L từ `panels3` (S dùng chung cho M, L); `panels2` không dùng |
| Popup (thanh tiêu đề) | `src/sheet-popup2.png` (sửa từ panel S: `ref-panel-s.png`) | `src/sheet-popup2.prompt.src.txt` | 1 | popup dùng bản này (các bản `sheet-popup.png` và popup trong `panels2`, `panels3` bỏ) |
| Tab 3 trạng thái | `src/sheet-tabs.png` | `src/sheet-tabs.prompt.src.txt` | 1 | tab không chọn, hover, chọn |
| Control nhỏ | `src/sheet-small.png` | `src/sheet-small.prompt.src.txt` | 1 | icon nút tròn, vuông x 4 trạng thái, toggle, checkbox, badge, knob, mũi tên |
| Ô nhập, tab, hàng, toast, tooltip | `src/sheet-fields.png` | `src/sheet-fields.prompt.src.txt` | 1 | ô nhập x3, dropdown x2, tab x2, hàng x3, toast, tooltip |
| Thanh | `src/sheet-bars.png` | `src/sheet-bars.prompt.src.txt` | 1 | loading, slider, scrollbar, nền tối mờ |

Lần chạy đầu của sheet fields và bars bị mất vì lệnh nền bị cắt; chạy lại, không tính vòng.

## 2. Thành phần, cỡ, 9-slice (px 1080p; @2x gấp đôi)

Border theo thứ tự trái, phải, trên, dưới (`meta.json`, `border_LRTB`). Rim = độ dày viền nhìn thấy, đo bằng script (panel: `rim_measure.rim2` + 3,5; thành phần lòng sáng: `rim_color` x hệ số co).

| Thành phần | Sprite | Cỡ | Border | Rim đo | Trạng thái |
|---|---|---|---|---|---|
| Nút chính (vàng) | `kit_btn_primary_{normal,hover,pressed,disabled}` | 240x64 | 18 | khoảng 10 | 4 |
| Nút phụ (đá tối) | `kit_btn_secondary_*` | 240x64 | 18 | khoảng 10 | 4 |
| Nút nguy hiểm (đỏ) | `kit_btn_danger_*` | 240x64 | 18 | khoảng 10 | 4 |
| Icon nút tròn, vuông | `kit_iconbtn_round_*`, `kit_iconbtn_square_*` | 64x64 | không slice | khoảng 9 | 4 mỗi loại |
| Toggle | `kit_toggle_off/on` | 114x48 | không slice | | tắt, bật |
| Checkbox | `kit_checkbox_off/on` | 39x40 | không slice | | tắt, bật |
| Badge, knob, mũi tên | `kit_badge` 24, `kit_knob` 31x32, `kit_arrow_down` 16x10 | | không slice | | 1 |
| Panel S, M, L | `kit_panel_s/m/l` | 360x240, 640x400, 1000x640 | 47 (bốn cạnh) | 23,5 đến 24,5 | 1; dựng bằng 9-slice thật từ nguồn S, 4 góc chép nguyên pixel |
| Popup + thanh tiêu đề | `kit_panel_popup` | 720x480 (co giãn được) | 47, 47, **88 trên**, 47 | 23,5 đến 24,5 | nền = 9-slice của S (4 góc chép nguyên pixel); thanh tiêu đề cao 62 px lấy từ nguồn GPT popup2, nằm trong border trên |
| Ô nhập | `kit_input_{normal,focus,error}` | 360x56 | 19 | khoảng 11 | 3 |
| Dropdown | `kit_dropdown_{closed,open}` + `kit_row_*` | 360x56 | 19, 19 (phải 68), 19 | khoảng 11 | 2 (đóng; mở: header viền vàng + **danh sách** ghép từ `kit_row_normal`, `kit_row_alt`, `kit_row_selected` đặt 360x48 ngay dưới header, xem `ui-kit-sheet.png`) |
| Tab | `kit_tab_{off,hover,on}` | 160x56 | 13, 13, 13, 1 | khoảng 8 | 3: không chọn, **hover** (nền sáng hơn, vạch vàng mảnh trong rim trên), chọn (viền vàng, nền sáng) |
| Dòng danh sách | `kit_row_{normal,alt,selected}` | 600x48 | 12, 2, 2, 2 | không viền | thường, xen kẽ (sáng hơn), chọn (vạch vàng trái) |
| Toast | `kit_toast` | 360x64 | 22 | khoảng 12 | 1 |
| Tooltip | `kit_tooltip` | 240x80 | 22, 22, 22, 35 (có mũi nhọn dưới) | khoảng 16 | 1 |
| Thanh loading | `kit_progress_track` 480x28 (border 12,12,5,5), `kit_progress_fill` 470x16 | | | khoảng 7 | rãnh + thanh vàng |
| Slider | `kit_slider_track` 320x16 (7,7,3,3), `kit_slider_fill` 320x10, `kit_knob` | | | khoảng 4 | |
| Scrollbar | `kit_scroll_track` 14x320, `kit_scroll_thumb` 14x96 (dọc) | | border 4 | | |
| Nền tối mờ | `kit_dim` 64x64 phẳng `#1F1D18` | | | | dùng alpha 0,6 phía sau popup |

Image Type: Sliced cho mọi thành phần có border; Simple cho icon nút, toggle, checkbox, badge, knob, mũi tên; Tiled không cần. `@1x` PPU 150 hoặc `@2x` PPU 300 như HUD.

## 3. Luật phong cách đối chiếu

| Luật (DECISIONS 07/11) | Kết quả |
|---|---|
| Rim 24 px với panel cỡ HUD | S, M, L, popup rim 22,5 đến 25,5 (rim2 + 3,5). Thành phần nhỏ hạ tỉ lệ: nút 10, ô nhập 11, toast 12, thanh 4 đến 7 |
| Hoa văn góc cùng cỡ HUD (khoảng 25 px) | **Đạt:** M, L, popup chép nguyên 4 góc từ panel S (cap 50 px nguồn = 47 px đầu ra, phủ cả khối góc và rêu). Tự kiểm bằng script `build_kit.py panels`: diff lớn nhất của 4 góc 40x40 so với S = 0 ở @1x và @2x cho M, L; popup = 0 ở hai góc dưới, hai góc trên chỉ khác ở pixel lòng tối thay bằng màu thanh tiêu đề (số liệu `src/corner_diff.json`). Khối góc S đo chéo/căn 2: 23,3 đến 25,5 px. Nút chỉ có 2 khối nhỏ ở hai góc chéo |
| Rim olive, rêu | Panel có rêu 4 góc; nút và ô nhập rêu tối thiểu (1 đến 2 góc) |
| Lòng đá tối, tách hàng bằng đậm nhạt không kẻ đường | Popup: thanh tiêu đề sáng hơn thân bằng đậm nhạt; hàng danh sách thường, xen kẽ, chọn chỉ khác độ sáng (chọn có vạch vàng trái 1 chỗ) |

## 4. Token

Màu (hex):

| Token | Giá trị | Dùng |
|---|---|---|
| `surface-dark` | `2B2821` đến `38342B` | lòng panel, ô nhập |
| `surface-mid` | `413C30` | dải giữa Vitals, hàng xen kẽ |
| `surface-light` | `4A4538` | thanh tiêu đề popup, tab chọn, hàng chọn |
| `rim` | olive tan (trong sprite) | |
| `gold` | `FFDC6E` | focus, chọn, tiêu đề, số phím |
| `text` | `EDF2EE` | chữ chính |
| `text-muted` | `D6DAD6` | chữ phụ |
| `danger` | `C0392B` (viền, nút `7A2A26`) | viền lỗi, nút nguy hiểm |
| `danger-text` | `FF8A78` | chữ cảnh báo trên lòng tối (không đặt trên thanh tiêu đề) |
| `hp` | `C83A41` | thanh HP |
| `outline` | `0A0C0C` | viền chữ 2 px trên nút và nền biến thiên |

Chữ (Be Vietnam Pro Bold, px 1080p): tiêu đề popup 40, tiêu đề mục 30, thân 24, phụ 20, chú thích 18. Chữ nút 26.

Khoảng cách: lưới 8 px (4 px cho khoảng sát). Lề trong panel tính từ mép ngoài: rim 24 + 16 đến 36. Giãn hàng form 56 đến 64. Khoảng giữa nút 24.

## 5. Tương phản (`src/contrast_kit.md`, p95 L của nền)

| Nền | L p95 | text | text-muted | gold | danger E05A4F |
|---|---:|---:|---:|---:|---:|
| panel M (lòng) | 0,029 | 11,67 | 9,35 | 9,89 | 3,62 |
| popup thân | 0,033 | 11,15 | 8,94 | 9,45 | 3,45 |
| popup thanh tiêu đề | 0,094 | 6,44 | 5,16 | 5,46 | 2,00 |
| ô nhập thường, focus, lỗi | 0,027 đến 0,028 | 11,83 đến 12,00 | 9,49 đến 9,62 | 10,03 đến 10,17 | 3,67 đến 3,72 |
| dropdown đóng | 0,029 | 11,76 | 9,43 | 9,97 | 3,65 |
| tab chọn | 0,071 | 7,67 | 6,15 | 6,51 | 2,38 |
| tab không chọn | 0,028 | 11,83 | 9,49 | 10,03 | 3,67 |
| hàng thường, xen kẽ, chọn | 0,023, 0,038, 0,070 | 12,70, 10,53, 7,73 | 10,18, 8,44, 6,20 | 10,77, 8,93, 6,56 | 3,94, 3,26, 2,40 |
| toast, tooltip | 0,029 | 11,67, 11,71 | 9,36, 9,39 | 9,90, 9,93 | 3,62, 3,63 |

Chữ `text`, `text-muted`, `gold` đạt >= 4,5:1 trên mọi nền kit. `danger E05A4F` không đạt (2,00 đến 3,94), nên token chữ cảnh báo là `danger-text FF8A78` (5,5:1 trên lòng tối, không dùng trên thanh tiêu đề sáng hơn). Chữ nút (nền nút đo p95 L): trắng `FFFFFF` + viền tối 2 px; thô: chính thường 5,11, nhấn 7,33, tắt 10,34; phụ 7,75 đến 11,84; nguy hiểm 6,91 đến 11,56. **Nút chính hover không đạt thô: chữ trắng 3,96, chữ tối 4,08** (nền hover vàng sáng hơn dự kiến); trên mock chữ có viền tối 2 px đọc được, nhưng chưa đạt 4,5 thô. Nút tắt là trạng thái không cần đạt.

### Cỡ khối góc và kiểm góc giống hệt

Khối góc S (`corner_measure.py`, chéo / căn 2, px 1080p): TL 24,0; TR 23,3; BL 25,5; BR 25,5 (HUD minimap chuẩn 24,7 đến 26,2). M, L, popup chép nguyên góc từ S nên cùng cỡ.

Diff pixel lớn nhất (max abs trên kênh RGBA) của góc 40x40 so với `kit_panel_s`, đo trên file trong thư mục:

| Sprite | TL | TR | BL | BR |
|---|---:|---:|---:|---:|
| `kit_panel_m` @1x và @2x | 0 | 0 | 0 | 0 |
| `kit_panel_l` @1x và @2x | 0 | 0 | 0 | 0 |
| `kit_panel_popup` @1x | 0 | 15 | 0 | 0 |
| `kit_panel_popup` @2x | 21 | 96 | 0 | 0 |

Popup: hai góc dưới giống hệt S (diff 0). Hai góc trên chỉ khác ở các pixel lòng tối của S bên dưới ô góc, được thay bằng màu thanh tiêu đề (để không lộ ô vuông tối dưới ô góc); ô góc, rêu, rim giữ nguyên. Lần trước báo "30x30 giống hệt" nhưng cap chỉ 31 px nguồn (30 px đầu ra) nên rêu và ô góc lan ra vùng giãn và lệch; nay cap 50 phủ hết (CPO đo 40x40 trước đây ra lệch vì lý do này). Unity chỉ cần 9-slice một sprite S với border 47 cho cả M, L; file M, L, popup trong thư mục khớp với cách đó (dùng cho mock T-021).

## 6. Còn lệch và chưa kiểm

- Nút chính hover chưa đạt 4,5 thô (3,96:1): chữ có viền tối 2 px (Huy/CPO chấp nhận).
- Dropdown mở: danh sách là ghép các dòng `kit_row_*`, chưa có khung riêng cho danh sách.
- Slider knob dùng chung `kit_knob`; thanh slider trong mock đặt thủ công (không phải component).
- Chưa dựng thử trong Unity, chưa qua qa. Mock các màn (T-021) chưa làm, chờ Huy duyệt kit.

## 7. Màu ngữ nghĩa (cập nhật 07/11)

Quy tắc và nguồn: `projects/lastaway/de-xuat/mau-ngu-nghia.md`. Tóm tắt: nút chính giữ vàng kim, đỏ đầy nút chỉ trong hộp xác nhận, xanh lá và hổ phách chỉ ở icon nhỏ (`kit_icon_{check,cross,warn,dot}`), toast và tag nền trung tính, mọi nghĩa kèm icon hoặc chữ. Các sprite bản lạm dụng (`kit_btn_success_*`, `kit_toast_{success,error,warning}`, `kit_tag_{success,error,warning}`, `kit_badge_{success,error,warning,neutral}`) đã chuyển sang `bo-lam-dung/` và xóa khỏi `meta.json`; `kit_input_success` giữ. Sheet GPT bổ sung: `src/sheet-sem1.png`, `sheet-sem2.png` (bản lạm dụng, không dùng), `sheet-sem3.png` (icon giảm bão hòa, đang dùng).

## 8. Thành phần ngoài kit, thêm ở T-021 đợt B (07/11)

Gen bằng GPT (codex-img, ref `src/ref-hud.png`, nền magenta, tách bằng `T-015/src/tools/extract_t015.py` qua `T-021/src/tools/extract_fx.py`), file trong `art/inbox/T-021/b/assets/`, prompt và sheet gốc trong `T-021/src/gen/`. Mỗi sheet 1 vòng, chưa vòng sửa.

| File | Dùng ở | Cỡ gốc px | Ghi chú màu |
|---|---|---|---|
| `fx_hitmarker` | phản hồi HUD, tâm ngắm | 216x206 | trắng, viền tối |
| `fx_damage_arc` | hướng nhận sát thương, trên vòng quanh nhân vật | 118x392 | đỏ san hô `E0594A`, diện tích nhỏ |
| `fx_danger_arrow` | bom/bẫy ngoài màn hình | 206x289 | đỏ san hô, kèm số giây bằng chữ |
| `fx_chip_poison/slow/stealth` | hiệu ứng đang chịu (tối đa 6) | 305x319 | khung đá trung tính, màu chỉ ở glyph (lục `86B36B`, hổ phách, xám xanh); giây ghi bằng chữ dưới glyph |
| `fx_vignette` | viền máu thấp | 1920x1080 | đỏ sẫm, alpha 38 %, chấm dither; vẽ dưới lớp HUD |
| `fx_emblem_win`, `fx_emblem_lose` | màn Thắng, Thua | 378x353, 314x309 | vàng, đá xám không đỏ |
| `fx_map_player`, `fx_map_mark`, `fx_map_drop` | bản đồ lớn | 124x168, 104x153, 224x286 | đỏ san hô, lục nhạt, hồng nhạt; nghĩa luôn kèm chú thích chữ |
| `fx_icon_disconnect`, `fx_icon_eye`, `fx_icon_skull` | mất kết nối, spectator, số hạ gục | 234x200, 205x121, 108x109 | kem, viền tối |

Chưa có: icon vật phẩm (ART-04, mock túi đồ để ô trống), avatar người chơi, nền bản đồ và vòng bo (vẽ lúc chạy, mock chỉ giữ chỗ).

## 9. Họ nút K2 (T-026, Huy duyệt 07/11): thay nút cũ

Nút nổi khối (mặt + đế mỏng ~7 px ở cao 64, lún khi nhấn), màu theo đá viền khung: nút phụ kaki (mặt #BFA26C đo nửa trên, viền mẫu #AA9360 đến #B09A66), nút chính vàng dịu (#DDAF5D), nút phá hủy đỏ dịu (#D5786E, chỉ hộp xác nhận). Viền olive và rêu chỉ của khung, nút không có. Lịch sử chọn: so sánh v1 đến v4 trong `art/inbox/T-026/` (so-sanh-nut*.png). Gen bằng GPT (ref: sheet P2 xin 8 + crop viền của Huy), prompt và sheet gốc `T-026/src/gen4/`, `gen5/`.

| Sprite (mỗi cái có @2x) | Cỡ @1x | 9-slice LRTB | Ghi chú |
|---|---|---|---|
| `kit_btn_{primary,secondary,danger}_{normal,hover,pressed,disabled}` | 240x64 | 22,22,28,28 | thay bản cũ; nhấn: mặt dịch xuống, phần trên canvas trong suốt; tắt: phẳng xám, đế 1 px. Cỡ S 48 và L 80 co đều theo cao, kéo ngang bằng 3-slice (đã kiểm 96/240/420) |
| `kit_iconbtn_{round,square}_{normal,hover,pressed,disabled}` | 64x64 | không | thay bản cũ, chưa có icon bên trong |
| `kit_tab_{off,hover,on}` | 160x64 | 19,19,19,1 | đáy phẳng gắn vào panel; sửa lần 2 (07/11): tắt thấp và tối (cao ~43), hover vừa (~49), chọn cao nhất (64), sáng hẳn, thanh vàng dịu 6 px trên mép, hai cạnh chạy thẳng xuống đáy để liền với panel; khác nhau cả về chiều cao, không chỉ màu. Canvas đổi từ 56 sang 64 |
| `kit_close_{normal,hover,pressed,disabled}` (mới) | 52x52 | không | nút đóng hộp thoại, kaki, không đỏ; icon X đặt lên trên (`kit_icon_cross_dark`) |
| `kit_icon_cross_dark` (mới) | 32x32 | không | X đỏ đậm #55140F cho Thoát trận, Rời phòng và nút đóng; `kit_icon_cross_ink` (mới, cùng cỡ) là X mực tối #1F1A10 (viền sáng #4A4030), CHỐT (CPO, 07/11) là icon của nút đóng (luật màu: nút đóng không đỏ); gen bằng GPT (`T-026/src/gen5/nut-x-ink.png`), bản tô lại bằng script lúc đầu giữ ở `_bo/kit_icon_cross_ink_script*.png` (trái luật, chỉ dùng tạm); X đỏ đậm chỉ cho Thoát trận, Rời phòng; `kit_icon_cross` coral cũ giữ cho dòng lỗi trên nền tối |

Luật chữ trên nút: chữ tối #1F1A10 trên chính, phụ, phá hủy (tệ nhất thường, hover, nhấn: chính >= 7,8:1; phụ >= 6,5; phá hủy >= 4,55); Thoát trận, Rời phòng là nút phụ + icon X và chữ đỏ đậm #55140F (>= 5,25:1); nút tắt chữ sáng #EBEBE6 trên xám (~3:1, không bắt buộc). Chữ trên nút tối dùng không viền; chữ sáng giữ viền tối 2 px.

Bản cũ (viền olive + rêu) chép vào `_bo/` (24 + 16 + 6 file kèm @2x) và `_bo/meta.before-T026.json`. Chưa dựng trong Unity. `ui-kit-sheet.png`, `man-thu-*.png` và mock T-021 vẫn là ảnh dựng bằng nút cũ, cần dựng lại; sheet họ nút mới: `T-026/ho-nut-k2.png`. Còn thiếu (chờ Huy duyệt danh sách P0): keycap, nút thứ ba, nút chỉ chữ, nút quay lại, bộ icon chức năng.

Căn chữ trên nút (sửa 07/11): chữ căn giữa theo MẶT nút, không tính đế. Ô chữ trong Unity chừa padding dưới bằng độ dày đế (khoảng 5 px ở cao 48, 7 ở 64, 9 ở 80; đế = khoảng 11 % chiều cao). Nút nhấn: mặt hạ xuống bằng phần đế đã mất, chữ đi theo mặt.

## 10. P0 mở rộng kit (T-026, Huy duyệt danh sách 07/11)

Gen GPT (3 sheet, ref: sheet K2 + crop viền Huy + crop HUD; prompt và sheet gốc `T-026/src/gen6/`), nhập bằng `T-026/src/import_p0.py`, sheet xem: `T-026/p0-sheet.png`.

| Sprite (có @2x) | Cỡ @1x | 9-slice LRTB | Dùng ở |
|---|---|---|---|
| `kit_dialog_footer` (T02) | 1041x96 | 8,8,8,8 | chân hộp thoại: kéo ngang, đặt ở đáy thân popup (cách viền 47 px); nút 64 cao cách mép trên chân 16 px; đường chia khắc ở mép trên chân |
| `kit_screen_header` (T03) | 1210x104 | 36,36,12,12 | header màn toàn phần: kéo ngang, nút quay lại (`kit_iconbtn_square` + `kit_ico_arrow_left`) ở trái, tiêu đề giữa, phụ đề phải |
| `kit_card_room_{normal,selected,full}` (T05) | 619x120 | 24,24,24,24 | thẻ phòng: viền kaki mảnh 4 px, chọn = viền vàng dịu, đầy = tối và nhạt |
| `kit_slot_player_{empty,notready,ready}` (T06) | 371x96 | 100,40,24,24 | ô người chơi: vòng avatar giữ chỗ bên trái (không kéo), ready = viền vàng dịu + dấu tích lục nhỏ. Rỗng và chưa sẵn sàng khác nhau ít; phân biệt thêm bằng chữ "Chỗ trống" nhạt |
| `kit_codebox_{empty,active,filled,error}` (T07) | 66x78 | không | ô mã phòng 6 ký tự; lỗi = viền đỏ dịu, luôn kèm dòng lỗi có icon X |
| `kit_keycap_{normal,active,disabled}` (T08) | 48x52 | 14,14,14,14 | keycap: chữ tối #1F1A10, rộng thay theo chữ (WASD, ESC) |
| `kit_ico_{gear,map,bag,copy,refresh,person,clock,wifi,sound,music,vibrate,eye,skull,arrow_left,arrow_right,trophy}` (T04) | 48x48 | không | 16 icon mực tối: chỉ đặt trên mặt sáng (nút kaki, vàng), icon trên nền tối phải lót nút tròn kaki phía sau |

Gộp X mực tối `kit_icon_cross_ink` đã có (không gen lại trong sheet icon). Chưa dựng trong Unity.

## 11. Luật nút: icon + chữ, căn giữa thị giác, X đỏ (T-026, 07/11)

**Đồng bộ:** mọi nút có chữ đều có icon bên trái (trừ tab, keycap, nút chỉ chữ). Bảng gán (nguồn: `T-021/src/tools/mock.py` hằng `ICONS`):

| Nút | Icon | Nút | Icon |
|---|---|---|---|
| Vào phòng, Vào | `kit_ico_play` | Sao chép, Chép | `kit_ico_copy` |
| Tạo phòng | `kit_ico_plus` | Thử lại | `kit_ico_refresh` |
| Lịch sử | `kit_ico_clock` | Đăng nhập | `kit_ico_key` |
| Bảng xếp hạng | `kit_ico_trophy` | Máy chủ (nâng cao) | `kit_ico_server` |
| Cài đặt | `kit_ico_gear` | Sẵn sàng | `kit_ico_check` |
| Thoát (game) | `kit_ico_door` (khối cửa đặc, hơi thô) | Về trang chủ | `kit_ico_home` |
| Đăng xuất | `kit_ico_logout` | Hủy | `kit_icon_cross_ink` (X mực tối) |
| Xóa, Xóa phòng | `kit_ico_trash` | Thoát trận, Rời phòng | `kit_icon_cross_red` (X đỏ #94281F), chữ tối #1F1A10 |

Thêm 11 icon mới (play, plus, logout, door, trash, check, key, home, server, list, people) và `kit_icon_cross_red` (X đỏ #94281F, viền sáng #C9564A) bằng GPT (`T-026/src/gen7/`, ref là bộ 16 icon, nét đậm hơn 10 đến 12 px). Nút đóng hộp thoại (`kit_close_*`) dùng X đỏ này (đổi so với quyết định trước dùng X mực tối, theo DECISIONS 07/11). `kit_ico_list`, `kit_ico_people` chưa dùng ở mock A. Bộ 16 icon cũ giữ nguyên (không gen lại): nét đủ dày ở cỡ 22 đến 30 px.

**Cỡ icon (to hơn trước ~20 %):** chiều cao mực của icon = cỡ chữ của nút; khoảng cách icon đến chữ 10 px; cả nhóm (icon + chữ) căn giữa nút theo chiều ngang.

| Cỡ nút | Cao | Cỡ chữ | Cao icon (mực) | Đế (px) = 11 % cao |
|---|---:|---:|---:|---:|
| S | 48 | 22 | 22 | 5 |
| M | 64 | 26 | 26 | 7 |
| L | 80 | 30 | 30 | 9 |

Tham chiếu "cao xấp xỉ chiều cao chữ hoa + dấu": chữ hoa của Be Vietnam Pro cao khoảng 0,72 cỡ chữ, chữ hoa có dấu khoảng 0,95 cỡ chữ; icon 1,0 cỡ chữ là xấp xỉ đó.

**Căn giữa thị giác (spec cho Unity):** tâm hộp mực (icon + dải hàng đậm của chữ, không tính dấu và nét xuống) trùng tâm MẶT nút, không tính đế. Mặt nút = cao nút trừ đế; đế: thường và hover 11 % cao, nhấn 4 % (mặt hạ xuống, chữ đi theo), tắt 0. Unity: padding dưới của ô chữ và icon = đế (S 5, M 7, L 9) cộng 0 ở trên; căn dọc giữa ô còn lại. Nút nhấn dùng sprite nhấn với canvas giữ cỡ, phần trên trong suốt, nên padding dưới nhấn = 4 % cao (S 2, M 3, L 3). Chữ canh theo chiều cao chữ hoa (neo baseline = tâm mặt + nửa chiều cao chữ hoa), không theo hộp chữ của font.

**Cổng máy:** `T-021/src/tools/check_center.py` vẽ lại từng nút trên nền phẳng, tách mực bằng so ảnh, đo tâm mực so với tâm mặt, FAIL nếu lệch > 1 px. Kết quả cuối: 278 ca (họ nút S/M/L × 3 loại × 4 trạng thái × 7 chữ/icon = 252, nút thật trong mock A không trùng = 26), 0 FAIL, lệch lớn nhất 0,5 px; báo cáo `T-021/a/center.md`. Thử âm: cố ý lệch 3 px thì cổng báo 2,52 px FAIL. Giới hạn: tâm mặt dùng chính hằng đế nói trên (không đo ngược từ sprite vì đế trên mặt đá có vân đo tự động không ổn định), nên cổng bắt lỗi đặt chữ chứ không bắt sprite có đế lệch.

**Tham chiếu icon trên mạng (chỉ xem tên và mô tả trong kết quả tìm kiếm, KHÔNG tải, không mua, chưa xem ảnh nên chưa dùng để chỉnh nét):**
- Free Pixel UI Button Icon Set, 21 Fantasy Menu Buttons 32x32: https://pixelexplosive.itch.io/free-pixel-ui-button-icon-set-21-fantasy-menu-buttons-32x32
- Fantasy UI (25 icon, ô và nút 16x16): https://deja-view.itch.io/fantasy-ui
- Medieval UI Buttons Pack 16x16: https://isaque8.itch.io/medievaluibuttons
- Essential UI Icon Pack (38+ icon 16x16): https://pxelcoder.itch.io/essential-ui-icon-pack
- Fantasy RPG UI Icons (30 icon): https://guildmastercreative.itch.io/fantasy-rpg-ui-icons
- Stonebase UI Pack (viền đá, pixel): https://itchabop.itch.io/stonebase-ui-pack
- Unity Asset Store, Classic Fantasy RPG UI Kit (không phải pixel): https://marketplace.unity.com/packages/2d/gui/classic-fantasy-rpg-ui-kit-193841
Kết luận từ tham chiếu: mới là danh sách tên gói và cỡ (16x16, 32x32), chưa đủ để kết luận bộ icon hiện tại quá mảnh hay quá nhỏ; quyết định giữ bộ 16 icon cũ dựa trên mắt ở cỡ 22 đến 30 px. Chưa dựng trong Unity.

## 12. P1 cho đợt B (T-021 B, 07/11)

Gen GPT 1 sheet (`T-026/src/gen8/p1.png`, ref: P0 + K2 + crop viền Huy), nhập bằng `T-026/src/import_p1.py`.

| Sprite (có @2x) | Cỡ @1x | 9-slice LRTB | Dùng ở |
|---|---|---|---|
| `kit_card_stat` | 278x130 | 28,28,28,28 | thẻ thống kê (nhãn nhỏ + số lớn): Bị loại, Thắng, Thua, thẻ mục tiêu spectator |
| `kit_card_cause` | 768x150 | 110,28,28,28 | thẻ nguyên nhân bị loại: ô vuông lõm bên trái để icon (sọ), chữ ở phải; không kéo ô icon |
| `kit_slot_item_{normal,selected,empty,drag}` | 139x140 | không | ô vật phẩm lớn trong túi đồ; icon vật phẩm chờ ART-04 |
| `kit_tip_item` | 387x230 | 30,30,30,30 | thẻ chi tiết vật phẩm khi rê chuột (có mũi tên nhỏ bên trái; Unity chọn lật trái/phải theo vị trí) |
| `kit_divider` | 1069x14 | 10,10,2,2 | đường chia mục trong hộp thoại và màn toàn phần |

## 13. Chữ (T-027, CPO chốt 07/11; thay mọi cỡ chữ ghi ở các mục trên)

Thang 5 bậc, px cỡ font ở 1080p. Lớp áp thang nằm ở `T-021/src/tools/thang.py` (gọi từ `mock.py`; `LASTAWAY_THANG=0` để dựng lại bản trước thang).

| Bậc | px | Font | Đậm | Line-height | Dùng cho |
|---|---:|---|---|---:|---|
| H1 Số lớn | 96 | Chakra Petch | Bold | 1,05 | hạng, đếm ngược, CHIẾN THẮNG |
| H2 Tiêu đề | 40 | Chakra Petch | Bold | 1,2 | tiêu đề màn, panel, popup; số thống kê lớn |
| B1 Thân lớn | 30 | Be Vietnam Pro | Bold | 1,3 | nút M; tên nổi bật; số HUD chính (máu) |
| B2 Thân | 24 | Be Vietnam Pro | Bold | 1,35 | nút S; thân; danh sách; toast; tooltip; số HUD phụ; sàn chữ gameplay |
| C Chú thích | 20 | Be Vietnam Pro | Bold | 1,4 | chú thích, nhãn phụ, số trên ô, số phím; sàn tuyệt đối |

- **Chữ nút theo cỡ nút:** S 48 = 24, M 64 = 30, L 80 = 40; mọi chữ nút (kể cả L) dùng Be Vietnam Pro. Cỡ nút lệch (52, 56, 72) tính theo cỡ nút gần nhất (hòa thì xuống). Icon cạnh chữ cao bằng cỡ chữ.
- **Sàn:** không chữ nào dưới 20 px; gameplay (HUD, túi đồ, toast, spectator, bản đồ lớn) không dưới 24 px, riêng số phím và số trên ô được 20 px.
- **Viền chữ:** `max(2, round(em/12))` px, màu #0A0C0C; ưu tiên chữ nằm trên backplate (toast, tag, row, thẻ). Cổng tương phản đo ở p10 và p90 độ sáng nền, 4,5:1.
- **Dấu chồng:** Ắ Ặ Ề Ộ Ữ cao hơn chữ hoa khoảng 0,44 em; line-height một dòng không dưới 1,3, khối nhiều dòng 1,5.
- **Font chính (Huy chốt 07/11, thay Denk One):** Chakra Petch Bold cho bậc 96 và 40, tải từ Google Fonts (`ofl/chakrapetch`, OFL 1.1, bản gốc ở `T-027/font-ngoai/chakrapetch/`). Giấy phép: `Assets/_JungleMaze/Art/JSurviv/Fonts/ChakraPetch-OFL.txt` (cạnh `ChakraPetch-Bold.ttf`; dòng copyright của font: Copyright 2018 The Chakra Petch Project Authors). Denk One (`DenkOne.ttf`, `DenkOne-OFL.txt`, `DenkOne SDF.asset`) còn trong repo, không dùng. File meta do Unity sinh khi import. Be Vietnam Pro: `Assets/_JungleMaze/Art/Fonts/BeVietnamPro-OFL.txt`.
- **Cổng máy:** `T-027/src/do_co_chu.py --gate` (cỡ thuộc thang, sàn, gameplay 24, font đúng tầng); `T-021/src/tools/check_center.py` (căn giữa nút và chữ nút đúng bậc); tương phản `a|b/contrast.md`.
- **Chưa áp:** Unity prefab (Hud, LobbyUi, MatchOverlay) vẫn theo cỡ cũ: 206/353 thành phần chữ chưa đúng thang; việc của vai unity (m_fontSize x 1,5 vào bậc, Chakra Petch Bold cho 40 và 96).
