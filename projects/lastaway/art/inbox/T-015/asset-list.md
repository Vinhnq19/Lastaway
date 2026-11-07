# Asset list - skin A đá khắc (T-015, lượt 5: sprite GPT, cỡ và vị trí đo từ concept của Huy, 07/11/2026)

Huy chốt "làm giống hệt ảnh ref": bảng mục 4 là **spec mới cho unity**, thay rect HUD B. Concept: `projects/lastaway/art/brief/T-015/concept-toan-man-huy-07-11.png` (1672x941, quy 1920x1080 nhân 1,1483). Ảnh phóng Quick bar 2x có 1 ô chứa icon: `quickbar-2x.png`. Ảnh soi lệch: `so-sanh-concept.png` (concept trên, dựng thử dưới), `chong-concept-50.png` (chồng 50%). Dựng thử: `dung-thu-hud-A.png` (mock trên ảnh QA T-013, chưa xem trong Unity). Bản cũ: `lop2/`, `lan3/` (loại), `src/tools/build_t015_v4.py` (loại, vẽ bằng code).

## 0. Nguồn và quy trình (luật 07/11: asset UI do GPT vẽ)

GPT qua Codex CLI (`codex-img.sh -e medium`), nền magenta `#FF00FF`.

| Sheet | Ảnh GPT | Prompt | Vòng |
|---|---|---|---|
| Khung **(đang dùng, trừ Quick bar và Minimap)** | `src/v4/sheet-frames-r3.png` | `src/v4/sheet-frames-r3.prompt.src.txt` (sửa từ r2, ref: `ref-r3.png` crop concept + concept toàn màn) | r1 (`sheet-frames.png`), r2 (lòng tối, `-r2`), r3 (cỡ + rim olive + rêu 4 góc, `-r3`): 3 vòng, r3 dùng luôn |
| Vitals 3 dải đậm nhạt | `src/v4/sheet-vit-r11.png` (sửa từ `-r9` = sửa sheet r7) | `src/v4/sheet-vit-r9.prompt.src.txt` (bỏ khung hàng, 3 dải) và `sheet-vit-r11.prompt.src.txt` (dải giữa tối hơn, hex rõ) | 4 vòng: r8 bỏ (GPT vẽ lại cả khung, góc to), r9 dải quá sáng, r10 không đổi, r11 dùng; dựng bằng `src/tools/build_vit.py` (k = 0,653 như r7) |
| Minimap viền mỏng | `src/v4/sheet-mm-r5.png` | `src/v4/sheet-mm-r5.prompt.src.txt` | 1 vòng; dựng bằng `src/tools/build_mm.py` (đo border 4 cạnh) |
| Quick bar không tab | `src/v4/sheet-qb-r4.png` | `src/v4/sheet-qb-r4.prompt.src.txt` | 1 vòng; dựng bằng `src/tools/build_qb.py` |
| Ô và thanh | `src/v4/sheet-slots.png` | `src/v4/sheet-slots.prompt.src.txt` | 1 vòng (giữ từ lượt 4) |
| Icon | `src/v4/sheet-icons.png` | `src/v4/sheet-icons.prompt.src.txt` | 1 vòng (giữ từ lượt 4) |

Script chỉ tách nền, cắt, 9-slice, đổi cỡ, đo: `src/tools/build_v6.py` (khung, ô, thanh), `build_icons_v5.py` (icon, canvas mới), `contrast_v6.py`, `dungthu_v6.py` (dựng thử, so sánh, chồng). Ngoại lệ không phải GPT: `hud_bar_fill_white`, `hud_minimap_overlay` (mặt nạ phẳng, giữ từ lượt trước).

## 1. Import (unity)

- Canvas reference 1280x720, 1080p = 1,5 lần: **`@1x` PPU 150 hoặc `@2x` PPU 300**. Border Unity theo thứ tự Left, Bottom, Right, Top. Filter Bilinear, không mipmap, nén None.
- Image Type: **Simple cho mọi khung** (Vitals có hàng ô lõm; các khung khác đúng cỡ concept). Sliced chỉ khi đổi cỡ, border trong `meta.json`.
- `Image.color` trắng; `hud_bar_fill_white` tint `C83A41` (HP) hoặc `F5C542` (Adrenaline).

## 2. Sprite (tên file giữ nguyên; cỡ px @1080p, @2x gấp đôi)

| File | Cỡ | Ghi chú |
|---|---|---|
| `hud_panel_vitals` | 422x206 | **không còn khung từng hàng, không đường kẻ**; lòng phẳng 3 dải đậm nhạt (Huy 07/11): dải trên y 24 đến 72 tối, dải giữa y 72 đến 132 sáng hơn một bậc, dải dưới y 132 đến 182 tối. Rim 24, góc như r7, border meta 33 |
| `hud_panel_quickbar` | 593x112 | không tab đạn, **rim 24 px**, border meta 30 (Simple) |
| `hud_panel_match` | 318x90 | **rim 24 px**, cao 90 (Huy 07/11), border meta 31 |
| `hud_minimap_frame` | 318x316 | **viền mỏng**: border đo L 24, R 25, T 24, B 24 (@1080p; @2x gấp đôi); lỗ trong suốt 269x268 tại x 24 đến 292, y 24 đến 291, tâm lỗ (158; 157,5), lệch tâm khung <= 1 px; hoa văn góc nhỏ, rêu đều 4 góc. Simple |
| `hud_killfeed_plate` | 270x130 | chỉ nền tối phẳng, alpha 60%, không khung |
| `hud_slot_empty`, `hud_slot_selected` | 54x54 | |
| `hud_bar_track_hp` | 236x32 | |
| `hud_bar_track_thin` | 242x24 | viền vàng (Adrenaline) |
| `hud_ammo_plate` | 276x19 | **đã bỏ** (không còn thanh đạn, unity không dùng) |
| `hud_bar_fill_white` | 64x16 | tint trong code |
| `hud_minimap_overlay` | 240x240 | giữ từ lượt 2, co theo lỗ 269x268 (khi unity gắn) |

Icon (`hud-icons/png/`, canvas và hình lớn nhất @1x): hp 44 (38), armor 50 (46), helmet và helmet-alt 46 (43), adrenaline 44 (39), alive 40 (37), zone-timer 40 (37), kill 32 (28).

## 3. Spec vị trí đo từ concept (px 1920x1080; gốc = góc trên trái sprite tương ứng)

Rect đặt trên màn:

| Khối | x | y | w | h |
|---|---:|---:|---:|---:|
| Vitals | 30 | 857 | 422 | 206 |
| Quick bar | 668 | 950 | 593 | 112 |
| Minimap | 1577 | 30 | 318 | 316 |
| Match status | 1577 | 350 | 318 | **90** (đáy y 440) |
| Killfeed (nền) | 16 | 44 | 270 | 130 |

Nội dung (tâm hoặc góc trái như ghi):

| Khối | Phần tử | Vị trí | Cỡ |
|---|---|---|---|
| Vitals | icon tim | tâm (64,42) | 38x33 |
| | thanh HP (rãnh) | x 91, y 26 | **224x32**; số "100" căn phải x 380, tâm y 41; font 30 |
| | icon giáp / số | tâm (65,100) / trái x 103, tâm y 101 | 46x37; font 28 |
| | icon mũ / số | tâm (245,100) / trái x 280, tâm y 101 | 36x43; font 28 |
| | icon sét | tâm (65,158) | 29x39 |
| | thanh Adrenaline | x 95, y 146 | 242x24; số "0" tâm x 366, y 158, font 28 |
| Quick bar | ô 1 đến 4 | x = 50 + 60 i, **y 29** (căn giữa dọc thân 112: lề trên 29, dưới 29) | 54x54 |
| | ô 5 đến 8 | x = 310 + 60 (i-4), y 29 | 54x54 |
| | số phím | **trong ô, góc dưới phải**: neo phải-dưới tại (x ô + 50, y ô + 50) | font 16 bold, vàng `FFDC6E`, viền tối 3 px |
| | icon vật phẩm trong ô | tâm ô (27,27) | tối đa 40x40 |
| | icon đạn, thanh đạn, tab | **đã bỏ** (`icon_ammo` và thanh đạn không còn dùng) | |
| Match | icon người | tâm (71,45) | 30x37 |
| | "1/1" | trái x 97, tâm y 45 | font 22 |
| | vạch chia | x 155, y 28 đến 62 | rộng 2 |
| | icon đồng hồ cát | tâm (185,45) | 29x37 |
| | "1:21" | trái x 211, tâm y 45 | font 22, kem `F1E7C8`; nội dung gói trong x 56 đến 262, tránh khối hoa văn góc (rộng khoảng 40 px mỗi bên) |
| Minimap | hộp bản đồ (lỗ khung) | trong sprite x 24, y 24 | 269x268; ảnh đảo căn theo tâm lỗ, nước hai bên đều; tâm vòng bo (157,8; 157,6) lệch tâm lỗ <= 0,3 px (`minimap-2x-tam.png`: kẻ tâm khung tím, tâm lỗ xanh, tâm vòng bo đỏ) |
| Killfeed | 3 dòng | trái x 37, tâm y 53, 88, 123 (cách 35) | font 19, dòng 1 trắng, hai dòng sau xám nhạt |

Font: Be Vietnam Pro Bold (như dựng thử); concept gần font bold không chân, cỡ đo theo chiều cao chữ số.

## 4. Bảng lệch so với `ux/hud-B-spec.md` (unity sửa ở T-016)

Mọi rect, vị trí, cỡ chữ ở mục 3 thay spec HUD B. Thêm: số phím trong góc dưới phải ô; **không có tab đạn, không có `icon_ammo`/thanh đạn** (Huy 07/11); bỏ tên vũ khí; số đồng hồ bo kem; Image Type Simple.

## 5. Tương phản (`contrast.md`, đo p99 mặt đá lòng panel)

Chữ: HP, giáp, mũ 11,15:1; số 1/1 11,52:1; số bo kem 10,57:1; số phím vàng trong góc ô 8,48:1 trên ô trống và ô chọn (có viền tối 3 px nên đọc được cả trên ô có icon). Icon (ngưỡng 3:1 kèm viền đen): mảng chính tim đỏ 3,25, giáp 4,29 (be sáng 6,17), mũ 4,38 (mảng phụ 3,51), sét 6,18 đến 8,93, người 6,60, đồng hồ cát 6,64 đến 11,87. Mảng bóng sẫm của tim đỏ 2,41 dưới 3:1, nằm trong viền đen, không gen lại.

## 6. Còn lệch concept (điểm đã biết)

| Điểm | Lệch |
|---|---|
| Rim đá | GPT vẽ rim sáng và rêu rậm hơn concept ở vài góc; vân gạch rim đều hơn |
| Quick bar | theo quyết định Huy 07/11: không tab, số phím trong góc ô, ô căn giữa (khác concept ảnh) |
| Hàng ô lõm Vitals | viền hàng mảnh hơn concept |
| Minimap | viền mỏng theo Huy 07/11 (24 px), hoa văn góc nhỏ, đơn giản hơn concept |
| Mock | còn vệt nền cũ của ảnh QA (đường dọc nhỏ cạnh Quick bar); nội dung bản đồ minimap lấy từ concept |
| Chưa kiểm | chưa xem trong Unity, chưa qua qa |

## 7. Bảng rim, cỡ khối góc, khoảng hở (đo bằng script trên sprite @1x, px 1080p)

Nguồn: lượt này GPT gen lại sheet 3 khung với yêu cầu khối góc nhỏ (`src/v4/sheet-panels-r7.png`, prompt `sheet-panels-r7.prompt.src.txt`, ref `ref-corner-mm.png`; tổng 2 vòng cho 3 khung này, r6 bị trả vì góc to). Script dựng: `src/tools/build_panels.py` (hệ số co theo rim đo trong ảnh nguồn). Đo: `rim_measure.py` (rim2) và `corner_measure.py`.

**Rim** (rim2 + 3,5 px rãnh tối trong cùng, hiệu chuẩn trên minimap: rim2 20,5 ứng với border 24):

| Panel | Cỡ | Trái | Phải | Trên | Dưới | Trung bình |
|---|---|---:|---:|---:|---:|---:|
| Minimap | 318x316 | 24 | 25 | 24 | 24 | 24,3 |
| Quick bar | 593x112 | 23,5 | 22,5 | 26,5 | 26,5 | 24,8 |
| Match | 318x90 | 24,5 | 24,5 | 25,5 | 24,5 | 24,8 |
| Vitals | 422x206 | 28,5 | 27,5 | 24,5 | 26,5 | 26,8 |

Lệch trung bình giữa các panel tối đa 2,5 px (Vitals, đã cố ý nới 13% để khối góc đủ lớn); Vitals trái/phải đo cao vì đường viền hàng ô lõm (x 22 đến 23) nằm sát rim và được tính vào.

**Khối góc** (đo theo đường chéo từ góc ngoài tới lòng tối: minimap 35 đến 37; cỡ khối = chéo chia căn 2):

| Panel | Chéo TL / TR / BL / BR | Cỡ khối (px) |
|---|---|---:|
| Minimap | 35 / 35 / 37 / 37 | 25,5 |
| Quick bar | 35 / 35 / 36 / 36 | 25,1 |
| Match | 33 / 33 / 33 / 33 | 23,3 |
| Vitals | 33 / 33 / 35 / 35 | 24,0 |

Lệch lớn nhất 2,2 px (Match so với minimap). Vitals và Match nhỏ hơn một chút vì tỉ lệ khối/rim của GPT khác nhau theo từng khung; chưa ép bằng tay.

**Khoảng hở nội dung tới khối góc** (tính từ toạ độ bố cục, khối góc 26 px, chưa đo bằng pixel; hở = khoảng cách ngang hoặc dọc nhỏ hơn):

| Panel | Cặp gần nhất | Hở (px) |
|---|---|---:|
| Quick bar | ô 1 (trái x 50) - góc trái (x tới 26) | 24 |
| Quick bar | ô 8 (phải x 544) - góc phải (từ x 567) | 23 |
| Match | icon người (trái x 56) - góc trái | 30 |
| Match | "1:21" (phải x khoảng 261) - góc phải (từ x 292) | 31 |
| Vitals | "100" (phải x 380) - góc phải (từ x 396) | 16 |
| Vitals | icon tim (trái x 45) - góc trái | 19 |

Tất cả >= 16 px, đạt yêu cầu >= 6 px. Match: hai góc trên (y tới 26) và dưới (từ y 64) tách nhau 38 px; nội dung nằm giữa, tâm y 45. Vòng này không còn khối góc đè ô hay số.

**Dải đậm nhạt Vitals** (L = độ sáng tương đối, đo cột giữa): dải trên 0,023, dải giữa 0,052, dải dưới 0,023. Chênh giữa hai dải kề 0,029 (mục tiêu gợi ý >= 0,03; hơi dưới, nhìn thấy rõ ở `vitals-2x.png`). Tâm dải: 48, 102, 157; vị trí nội dung giữ như r7 (tâm 42, 100, 158). Chữ trên dải sáng nhất: 8,96:1 (số giáp, mũ). Icon trên dải sáng: giáp 3,45 (mảng chính), mũ 3,52, mảng giáp phụ 4,96; mảng nâu sẫm của mũ 2,82 và đỏ sẫm của tim 2,71 dưới 3:1 (bóng trong viền đen). Ảnh phóng 2x: `vitals-2x.png`. Ảnh so viền: `4-panel-vien-2x.png`.
