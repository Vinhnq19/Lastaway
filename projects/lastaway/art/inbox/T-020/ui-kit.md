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
