# Asset list - skin A đá khắc (T-015)

Skin: đá khắc (Huy chọn 06/11, `DECISIONS.md`), nguồn thẩm mỹ `art/inbox/T-014/01-da-khac/`. Sprite PNG RGBA, nền trong suốt, **vẽ ở cỡ 1080p thật** (khớp rect đo ở `qa/T-013/do-rect-1920x1080.txt`); bản `@2x` gấp đôi để dùng khi cần nét hơn. Chưa qua cổng qa, chưa vào `Assets/`, chưa gắn prefab.

## 1. Quy tắc import (cho unity, T-016)

- Canvas HUD có reference 1280x720, nên 1080p = 1,5 lần. Sprite `@1x` vẽ theo pixel 1080p, vậy: **dùng `@1x` với Pixels Per Unit 150**, hoặc **`@2x` với PPU 300**. Khi đó 63 px = 42 u (ô quick bar) và border 32 px = 21 u, đúng như thiết kế. Để PPU 100 thì mọi thứ phình 1,5 lần.
- Image Type = **Sliced** cho mọi khung/thanh; Slot dùng Simple (kích thước cố định) hoặc Sliced đều được.
- Filter **Bilinear**, không mipmap, nén None (nét vẽ là tranh pixel dựng từ ảnh GPT, không phải pixel-grid thật; Point sẽ răng cưa khi canvas scale khác 1,5).
- Border Unity nhập theo thứ tự **Left, Bottom, Right, Top**; bảng dưới ghi **L, R, T, B** theo yêu cầu. Giá trị là px của chính file đó (cột "@2x" là border của file `@2x`).
- Tint: `Image.color` để **trắng** với mọi sprite trừ `hud_bar_fill_white` (tint đỏ `HudPalette.Red` hoặc vàng `HudPalette.Yellow`).

## 2. Danh sách sprite

| File | Dùng ở (object trong `Hud.prefab`) | Cỡ @1x (px) | Border L,R,T,B @1x | Border @2x | Ghi chú |
|---|---|---|---|---|---|
| `hud_panel_vitals.png` | `Vitals` (thay nền Surface + `ArtBevel`/`ArtEdge`) | 311 x 173 | 32,32,32,32 | 65,65,65,65 | Khung đá, góc có họa tiết vàng và rêu. Nội dung chừa lề: ngang >= 34 px, dọc >= 14 px |
| `hud_panel_quickbar.png` | `QuickBar` (nền) | 592 x 126 | 25,25,23,23 | 50,50,47,47 | Khung mỏng dài. Lề ngang >= 25 px; Slot1 hiện cách mép trái 15 px nên chồng 10 px lên góc họa tiết: chấp nhận (ô vẽ đè lên) hoặc dịch 10 px |
| `hud_panel_match.png` | `MatchStatus` (nền) | 240 x 89 | 27,27,27,27 | 54,54,54,54 | Lề ngang >= 27 px; icon và số căn giữa dọc |
| `hud_minimap_frame.png` | `Minimap` (đặt **trên** `MapArea`, cùng rect) | 240 x 240 | 32,32,32,32 | 65,65,65,65 | **Giữa trong suốt** (lỗ), bản đồ hiện xuyên qua. Viền đá che 8 px mép bản đồ |
| `hud_minimap_overlay.png` | `Minimap`, lớp **giữa** `MapArea` (terrain) và các vòng bo/marker, bên dưới `hud_minimap_frame` | 240 x 240 | 0,0,0,0 (Simple, kéo giãn) | 0,0,0,0 | N3: lớp phủ tối `#0C1417`, giữa 50%, viền 24 px lên 70% (vignette). Phải nằm **dưới** vòng bo; đặt trên vòng bo thì làm tối cả vòng và không tăng tương phản |
| `hud_killfeed_plate.png` | Killfeed (chưa có object, chừa chỗ 24,24; 171 x 90 u) | 256 x 135 | 38,38,38,38 | 76,76,76,76 | Nền đá 60% trong suốt, chỉ làm dịu nền chữ; có thể bỏ nếu chữ đủ đọc |
| `hud_slot_empty.png` | `QuickBar/Slots/WeaponGroup/Slot1..4`, `ConsumableGroup/Slot5..8` (trạng thái Empty/Default) | 63 x 63 | 18,18,18,18 | 37,37,37,37 | Dựng bằng script (viền đá, lõm tối), **không gạch đứt**; thay họa tiết nét đứt mà qa đã ghi |
| `hud_slot_selected.png` | cùng Slot, trạng thái Selected | 63 x 63 | 18,18,18,18 | 37,37,37,37 | Viền vàng + ánh sáng nhẹ bên trong. Thay `Amber` viền chọn cũ |
| `hud_bar_track_hp.png` | `Vitals/HpBar/Track` | 164 x 21 | 21,21,8,8 | 42,42,15,15 | Rãnh thanh máu. Fill đặt lùi vào 3 px mỗi cạnh |
| `hud_bar_track_thin.png` | `Vitals/AdrenalineRow/Bar` (Track) | 164 x 12 | 12,12,4,4 | 24,24,9,9 | Rãnh thanh Adrenaline (mỏng). Fill lùi 2 px |
| `hud_bar_fill_white.png` | `HpBar/Fill`, `AdrenalineRow/Bar` Fill (và `Trail` nếu cần) | 64 x 16 | 6,6,5,5 | 13,13,11,11 | Trắng, tint bằng `Image.color`: Red cho máu, Yellow cho Adrenaline, Trail dùng Red 35% như cũ |
| `hud_ammo_plate.png` | `QuickBar/StripArea` (tùy chọn) | 220 x 36 | 31,31,11,11 | 62,62,22,22 | Tấm đá nhỏ đặt sau số đạn/ tên vũ khí. T-013 hiện không dùng thanh đạn riêng nên có thể không cần |

## 3. Icon trạng thái

Giữ nguyên bộ `art/inbox/hud-icons/png/` (T-012), **không đổi màu, không thêm viền**: màu theo `HudPalette` đã đủ tương phản trên nền khung tối (`#23282A`). Chưa đo tương phản bằng script: NOT VERIFIED.

## 4. Nguồn từng sprite

| Sprite | Cách làm |
|---|---|
| 5 khung (vitals, quickbar, match, minimap, killfeed) | GPT (Codex) vẽ tấm mẫu `src/sheet-panels.png` trên nền magenta phẳng, tham chiếu ảnh concept `T-014/01-da-khac/bo-thu.png`. Script Pillow tách nền (chroma key + khử viền hồng), cắt từng khung, rồi dựng 9-slice về đúng cỡ 1080p (góc giữ nguyên tỉ lệ, cạnh giữa kéo giãn), sau đó làm nét nhẹ |
| 2 ô quick bar | Dựng bằng script Pillow (viền đá, lõm tối, vàng cho Selected); màu lấy từ concept A. Bản GPT (`src/sheet-slots.png`) bị mất chi tiết ở 63 px và tạo viền đứt nên bỏ |
| 3 thanh (track hp, thin, fill) và ammo plate | GPT vẽ `src/sheet-slots.png`, tách và dựng 9-slice như trên |
| Prompt | `src/sheet-panels.prompt.txt`, `src/sheet-slots.prompt.txt`; log trong `src/` |
| Script | Pillow, `src/tools/` (`gen_t015` soạn prompt, `extract_t015` tách nền, `build_t015` xuất sprite, `dungthu_t015` ghép thử); chạy bằng `python` thường (Pillow nằm ở user site, không dùng `-I`) |

## 5. Dựng thử

`dung-thu-hud-A.png`: ghép sprite lên ảnh Unity `qa/T-013/01-dau-tran-1920x1080.png` theo đúng rect HUD B (Vitals 36,872; QuickBar 664,918; MatchStatus 1644,288; Minimap 1644,36; ô 63 px tại x 678,8 và 971,3). Panel cũ được che bằng một mảng world lấy từ vùng lân cận (chỉ để dựng thử), nội dung (icon, thanh, số, chữ) vẽ lại bằng icon PNG T-012 và font Be Vietnam Pro Bold của project. Đây là mock, không phải ảnh chụp Unity.

## 6. Giả định và chưa kiểm

- Mức nét: khung là ảnh GPT thu nhỏ khoảng 0,36 lần nên rim đá chỉ dày khoảng 8 px ở 1080p, mỏng hơn concept T-014 (HUD concept lớn hơn HUD B thật ~1,2 lần). Muốn dày hơn cần tăng border và lề nội dung, tức đổi layout T-013.
- Minimap: lỗ trong suốt phụ thuộc MapArea vẽ bản đồ thật (T-013 đã nối); chưa kiểm viền che đúng.
- Chưa có cảnh bo thu, hiệu ứng máu thấp, hiệu ứng nhặt đồ với skin A.
- Tất cả: chưa qua qa chấm chéo, chưa xem trong Unity: NOT VERIFIED. Thư mục `debug/` chứa ảnh phóng to dùng soi, có thể xóa.

## 7. Xử lý 3 lỗi Nhẹ của qa T-013 (`qa/T-013/qa-result.md`)

| Lỗi | Xử lý | Đo (script, 06/11/2026) |
|---|---|---|
| N1 ô trống rối | `hud_slot_empty`: nền lõm phẳng `#232A2A`, viền đá liền, không nét đứt, không ô xám (xem mục 2). `hud_slot_selected` thêm viền vàng + glow | Nhìn trong `dung-thu-hud-A.png`: 8 ô sạch |
| N2 số phím 1-8 (10 u) khó đọc | Giữ cỡ 10 u. Chữ **kem `#F1E7C8`**, **viền đen dày 2 px `#0A0C0C`** (stroke), đặt trong lõm ô ở góc trên trái, cách mép 7 px. Dùng `TMP_Text` outline 0,2 và màu `#F1E7C8`; font giữ như hiện có | Kem trên nền lõm `#232A2A`: **12,07:1** (cũ `#A6BBB7`: 7,39:1, nhưng chìm vào họa tiết cũ); kem trên viền đá `#706C5A`: 4,46:1 (chữ có viền đen nên không phụ thuộc) |
| N3 minimap tương phản thấp | (a) `hud_minimap_frame` viền đá liền che mép map; (b) `hud_minimap_overlay` tối map **dưới** vòng bo (nên ghép bằng cách đặt lớp này giữa terrain và vòng bo; nếu vòng bo cùng một RenderTexture với terrain thì nhân màu terrain khoảng 0,5 thay vì lớp phủ); alpha giữa 50% | Tương phản vòng vàng `#E7D611` / nền map TB `#727234`: **3,36 -> 6,87:1**; vòng đỏ `#F80F03`: **1,21 -> 2,48:1** (vòng đỏ vẫn dưới 3:1: đề xuất viền đen 1 px quanh vòng đỏ, việc của unity vì vòng do world vẽ). Phủ lên cả vòng: vàng 3,23:1, đỏ 1,10:1, tức tệ hơn |

Đo trên vùng 200x200 px trong minimap, ảnh `qa/T-013/01-dau-tran-1920x1080.png`. `dung-thu-hud-A.png` đã dựng với overlay nằm dưới vòng bo (vòng và marker không bị phủ) và số phím mới.
