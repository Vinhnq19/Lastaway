# Asset list - skin A đá khắc (T-015, lượt 2)

Skin: đá khắc (Huy chọn 06/11), thẩm mỹ gốc `art/inbox/T-014/01-da-khac/`. Lượt 2 làm lại theo yêu cầu CPO: khung đá dày, góc khắc hoa văn có rêu, lòng panel là mặt đá có vân (không phẳng). Sprite PNG RGBA nền trong suốt, **vẽ ở cỡ 1080p thật**, khớp rect đo ở `qa/T-013/do-rect-1920x1080.txt`; bản `@2x` gấp đôi. **Rect ngoài HUD B không đổi.** Chưa qua qa, chưa vào `Assets/`, chưa gắn prefab.

## 1. Quy tắc import (unity, T-016)

- Canvas HUD reference 1280x720 nên 1080p = 1,5 lần. Sprite `@1x` vẽ theo pixel 1080p: **dùng `@1x` với Pixels Per Unit 150, hoặc `@2x` với PPU 300**. Khi đó 60 px = 40 u (ô quick bar) và border 42 px = 28 u, đúng thiết kế. PPU 100 làm mọi thứ phình 1,5 lần.
- Image Type **Sliced** cho khung và thanh; Slot Simple hoặc Sliced đều được. Fill Center bật.
- Filter **Bilinear**, không mipmap, nén None.
- Border Unity theo thứ tự **Left, Bottom, Right, Top**; bảng ghi **L, R, T, B**. Giá trị tính theo px của file đó; cột "@2x" là border của file `@2x`.
- `Image.color` để **trắng** với mọi sprite, riêng `hud_bar_fill_white` tint đỏ `HudPalette.Red` hoặc vàng `HudPalette.Yellow`.
- Tắt `ArtEdge` và `ArtBevel` cũ của `Vitals`, `QuickBar`, `Minimap`, `MatchStatus` (khung mới thay thế).

## 2. Danh sách sprite

| File | Dùng ở (`Hud.prefab`) | Cỡ @1x (px) | Border L,R,T,B | Border @2x | Ghi chú |
|---|---|---|---|---|---|
| `hud_panel_vitals.png` | `Vitals` (nền) | 311 x 173 | 42,42,42,42 | 85,85,85,85 | Khung đá khối xếp, viền trong mảnh, góc chamfer + hoa văn khắc + rêu; lòng đá xám có vân nứt |
| `hud_panel_quickbar.png` | `QuickBar` (nền) | 592 x 126 | 44,44,42,42 | 88,88,84,84 | Như trên, dạng dài |
| `hud_panel_match.png` | `MatchStatus` (nền) | 240 x 89 | 31,31,30,30 | 63,63,59,59 | Như trên, dạng nhỏ |
| `hud_minimap_frame.png` | `Minimap`, **trên** `MapArea` và **trên** `hud_minimap_overlay` | 240 x 240 | 45,45,45,45 | 90,90,90,90 | Giữa trong suốt (lỗ). Khung dày che khoảng 18 px mép bản đồ |
| `hud_minimap_overlay.png` | `Minimap`, **giữa terrain và vòng bo** | 240 x 240 | 0,0,0,0 | - | N3: tối `#0C1417`, giữa 50% viền 70%. Nằm dưới vòng bo; đặt trên vòng thì làm tối cả vòng |
| `hud_killfeed_plate.png` | Killfeed (chưa có object; 24,24; 171 x 90 u) | 256 x 135 | 21,21,21,21 | 42,42,42,42 | Tấm đá viền mảnh, 72% đục để world hơi lộ; bỏ được nếu chữ đủ đọc |
| `hud_slot_empty.png` | `QuickBar/Slots/WeaponGroup/Slot1..4`, `ConsumableGroup/Slot5..8` | **60 x 60** | 18,18,18,18 | 36,36,36,36 | Viền đá liền, lòng đá vân tối, không nét đứt (N1). **Nhỏ hơn T-013 (63 px): xem mục 4** |
| `hud_slot_selected.png` | cùng Slot, trạng thái chọn | 60 x 60 | 18,18,18,18 | 36,36,36,36 | Viền vàng, glow vàng nhẹ trong lòng |
| `hud_bar_track_hp.png` | `Vitals/HpBar/Track` | 164 x 21 | 21,21,8,8 | 42,42,15,15 | Rãnh thanh máu; fill lùi 3 px |
| `hud_bar_track_thin.png` | `Vitals/AdrenalineRow/Bar` (Track) | 164 x 12 | 12,12,4,4 | 24,24,9,9 | Rãnh Adrenaline; fill lùi 2 px |
| `hud_bar_fill_white.png` | `HpBar/Fill`, `AdrenalineRow/Bar` Fill | 64 x 16 | 6,6,5,5 | 13,13,11,11 | Trắng, tint đỏ hoặc vàng; `Trail` đỏ 35% như cũ |
| `hud_ammo_plate.png` | `QuickBar/StripArea` (tùy chọn) | 220 x 36 | 31,31,11,11 | 62,62,22,22 | T-013 không dùng thanh đạn riêng nên có thể bỏ |

Khung đá: vòng đá ngoài khoảng 13 đến 14 px, cộng viền trong và đường viền tối nên cả viền chiếm khoảng 16 đến 19 px ở 1080p (Vitals 18, Quick bar 19, Match 15, Minimap 19). Hoa văn góc rộng khoảng 40 px (Vitals, Quick bar, Minimap) và 32 px (Match).

## 3. Lề nội dung mới (padding, không đổi rect ngoài)

Để không đè lên hoa văn góc. Giá trị là px ở 1080p tính từ mép ngoài sprite; đổi sang u chia 1,5.

| Khối | Lề trái và phải | Lề trên và dưới | Bố trí đề nghị (px, gốc là góc trái trên khối) |
|---|---|---|---|
| Vitals (311 x 173) | **40** | **20** | Hàng 1: icon 36 tại x 40, y 20; thanh máu x 84, rộng 104, cao 24; số HP căn phải tới x 271. Hàng 2 (y 64): icon giáp x 40, số x 84; icon mũ x 150, số x 194. Hàng 3 (y 108): icon 32 tại x 42; thanh Adrenaline x 84, rộng 104, cao 14; số căn phải tới x 271. Nội dung từ y 20 đến 140 trong 173 |
| Quick bar (592 x 126) | **40** | **18** trên, 20 dưới | Dải tên vũ khí y 18 đến 40, bắt đầu x 40. Hàng ô **60 px** tại y 45; ô 1 tại x 41; khe 3 px; hai nhóm cách 12 px (nhóm 2 bắt đầu x 302); tổng 510 px trong vùng 512. Số phím tại x +6, y +1 mỗi ô, 15 px |
| Match status (240 x 89) | **32** | căn giữa dọc | Icon 24 tại x 32 và x 118; số x 62 và x 148 |
| Minimap (240 x 240) | 0 (MapArea giữ nguyên rect) | 0 | Khung phủ lên map; phần map nhìn thấy còn khoảng 204 x 204 |
| Killfeed (256 x 135) | 16 | 14 | Ba dòng chữ 17 px, cách nhau 32 px, bắt đầu y 24; dòng 2 và 3 mờ |

## 4. Thay đổi so với T-013 unity cần biết (nội dung trong khối, không đổi rect ngoài)

| Thay đổi | Từ (T-013) | Sang | Lý do |
|---|---|---|---|
| Ô quick bar | 63 px, khe 6, nhóm cách 22 | **60 px**, khe 3, nhóm cách 12 | Lề trái 40 px để Slot1 không đè hoa văn góc; tổng 510 trong 512 |
| Dải tên vũ khí | y 12 đến 45 | y 18 đến 40, x từ 40 | Chừa viền trên 18 px |
| Vitals | lề 15 | 40 ngang, 20 dọc; thanh rộng 104 | Chừa hoa văn góc |
| Match status | icon x 15 | x 32 | Chừa hoa văn góc |
| Minimap | khung phẳng | khung đá dày 18 px | Phần nhìn thấy bản đồ nhỏ hơn 18 px mỗi cạnh |

Rect ngoài giữ y nguyên: Vitals 311 x 173, Quick bar 592 x 126, Match 240 x 89, Minimap 240 x 240.

## 4b. Icon trạng thái kim loại ấm (T-015 bổ sung, 06/11/2026, thay icon vector xanh/đỏ)

Hình game-icons.net giữ nguyên, chỉ đổi chất: gradient kim loại sáng trên tối dưới, vạch sáng 1 px cạnh trên-trái, viền tối `141010` (1 px ở @1x, 2 px ở @2x). Kích thước file, pivot và vị trí trong lề mục 3 không đổi. Icon Adrenaline, icon Match status dùng chung bộ này.

**File thay đổi (cùng tên, ghi đè), thư mục `projects/lastaway/art/inbox/hud-icons/png/`:** `icon_hp`, `icon_armor`, `icon_helmet`, `icon_helmet-alt`, `icon_adrenaline`, `icon_alive`, `icon_kill`, `icon_zone-timer`, mỗi cái có `.png` và `@2x.png`. Bản vector gốc giữ ở `hud-icons/png-vector-goc/`. Màu sáng/tối từng icon ở `hud-icons/metal.json`. Script: `src/tools/recolor_icons_t015.py` (chạy lại luôn từ bản gốc).

| Icon | Chất | Màu sáng trên / tối dưới | Phân biệt |
|---|---|---|---|
| hp | đồng đỏ | `FFAA84` / `EE8F68` | cam hồng ấm, hình tim |
| armor | sắt cũ | `C3CBCC` / `8E999C` | xám gần như không bão hòa, hình giáp ngực |
| helmet, helmet-alt | đồng thau | `EFC463` / `D3A04A` | vàng, hình mũ |
| adrenaline | vàng sáng | `FFE08A` / `E8B945` | vàng sáng nhất, hình tia chớp |
| alive, kill | bạc kem | `F2ECD8` / `C9C2AA` | gần trắng |
| zone-timer | bạc tím | `D8CBF8` / `B6A5E3` | tím nhạt, khớp số `A274FF` |

Tương phản đo trên pixel lõi icon (bỏ viền tối), nền là điểm sáng p99 của mặt đá, ngưỡng 4,5:1 cho cả điểm tối nhất (p5):

| Icon | L trung bình | L p5 | cr trung bình ở p99 | cr p5 ở p99 | cr p5 ở p95 | Kết quả |
|---|---:|---:|---:|---:|---:|---|
| hp | 0,447 | 0,401 | 5,51 | 5,01 | 5,67 | Đạt |
| armor | 0,459 | 0,392 | 5,64 | 4,90 | 5,55 | Đạt |
| helmet | 0,495 | 0,419 | 6,04 | 5,20 | 5,89 | Đạt |
| adrenaline | 0,678 | 0,590 | 8,07 | 7,10 | 8,05 | Đạt |
| alive | 0,673 | 0,571 | 8,39 | 7,20 | 7,94 | Đạt |
| zone-timer | 0,531 | 0,434 | 6,74 | 5,62 | 6,19 | Đạt |

HP / giáp / mũ đọc trong 1 giây nhờ khác tông và khác độ bão hòa: HP cam hồng (L 0,45), giáp xám trung tính (L 0,46), mũ vàng (L 0,50); độ sáng gần nhau nên khác biệt chính là tông màu cộng hình khối, không phải độ sáng. Đã xem trên `dung-thu-hud-A.png`. Lưu ý: icon vốn chạm mép canvas (adrenaline, kill, zone-timer, helmet-alt), nên viền tối bị cắt đúng ở mép đó; không đổi kích thước canvas để unity khỏi chỉnh rect. Thanh máu vẫn đỏ bão hòa `E5484D` (tint trong code), chưa đụng.

## 5. Bảng đo tương phản (script `src/tools/contrast_t015.py`, output đầy đủ ở `contrast.md`, 06/11/2026)

Nền lấy từ chính sprite (điểm sáng p95 và p99 của mặt đá trong lòng, trường hợp xấu nhất); màu chữ và icon theo `HudPalette`. Ngưỡng: chữ thường 4,5:1; số lớn và icon 3:1. Lòng đá được hạ độ sáng (hệ số 0,70 trên RGB) so với concept để đạt ngưỡng, nên tối hơn concept một chút.

| Khối | Nền: median / p95 / p99 (độ sáng) | Màu chữ hoặc icon | cr ở p95 | cr ở p99 | Ngưỡng | Kết quả |
|---|---|---|---:|---:|---:|---|
| Vitals | 0,025 / 0,030 / 0,040 | số HP 24 px trắng `EDF2EE` | 11,64 | 10,28 | 3,0 | Đạt |
| Vitals | cùng | số giáp/mũ, chữ phụ `A6BBB7` | 6,54 | 5,77 | 4,5 | Đạt |
| Vitals | cùng | icon máu, giáp, mũ, Adrenaline | xem mục 4b | xem mục 4b | 4,5 | Đã thay bằng icon kim loại ấm (06/11/2026), đo trên pixel thật |
| Quick bar | 0,025 / 0,029 / 0,040 | tên vũ khí `EDF2EE` | 11,68 | 10,31 | 4,5 | Đạt |
| Quick bar | cùng | số phím kem `F1E7C8` (viền đen 2 px) | 10,72 | 9,46 | 4,5 | Đạt |
| Match status | 0,025 / 0,028 / 0,036 | số `EDF2EE` và icon người | 11,85 | 10,75 | 4,5 | Đạt |
| Match status | cùng | số đồng hồ bo `A274FF` (chữ) | 4,14 | 3,76 | 3,0 | Đạt. Icon đồng hồ cát và icon người: mục 4b |
| Killfeed (72% trên world) | 0,055 / 0,064 / 0,077 | dòng 1 `EDF2EE` | 8,11 | 7,30 | 4,5 | Đạt |
| Killfeed | cùng | dòng 2-3 `D6DAD6` | 6,50 | 5,85 | 4,5 | Đạt |

## 6. Nguồn từng sprite

| Sprite | Cách làm |
|---|---|
| 5 khung (vitals, quickbar, match, minimap, killfeed) | GPT (Codex) vẽ tấm `src/v2/sheet-panels2a.png` trên nền magenta phẳng, tham chiếu `T-014/01-da-khac/spawn.png` (hai lần chạy, chọn bản a vì viền dày hơn; bản b ở `src/v2/sheet-panels2b.png`). Script Pillow tách nền (chroma key, khử viền hồng), dựng 9-slice về đúng cỡ 1080p, hạ sáng lòng đá, làm nét nhẹ |
| 2 ô quick bar | Script Pillow: viền đá, lòng lấy vân từ lòng panel hạ sáng; viền vàng cho trạng thái chọn |
| Thanh (track hp, thin, fill) và ammo plate | GPT vẽ `src/sheet-slots.png` (lượt 1), tách và dựng 9-slice |
| Overlay minimap | Script Pillow (vignette) |
| Prompt | `src/v2/sheet-panels2.prompt.src.txt` và `*.prompt.txt` (đầy đủ), `src/sheet-slots.prompt.txt` |
| Script | `src/tools/`: `build_t015_v2.py` (sprite), `dungthu_t015.py` (ghép thử), `contrast_t015.py` (đo), `compare_t015.py` (so concept). Chạy bằng `python` thường (Pillow ở user site, không dùng `-I`) |

## 7. Lỗi Nhẹ của qa T-013

| Lỗi | Xử lý |
|---|---|
| N1 ô trống rối | Ô phẳng viền liền, lòng đá vân tối, không nét đứt, không ô xám |
| N2 số phím 10 u | Giữ cỡ; chữ kem `#F1E7C8` + viền đen 2 px; cr 10,72:1 (mục 5). Dùng `TMP_Text` outline 0,2 |
| N3 minimap tương phản thấp | Khung đá dày + `hud_minimap_overlay` dưới vòng bo. Đo ở lượt 1 (ảnh `qa/T-013/01-dau-tran-1920x1080.png`, vùng 200 x 200 px trong minimap): vòng vàng/nền map 3,36:1 lên 6,87:1; vòng đỏ 1,21:1 lên 2,48:1. Vòng đỏ vẫn dưới 3:1: đề xuất viền đen 1 px quanh vòng đỏ (unity). Nếu vòng bo cùng RenderTexture với terrain thì nhân màu terrain khoảng 0,5 thay vì dùng overlay |

## 8. Dựng thử và so concept

- `dung-thu-hud-A.png`: ghép sprite lên `qa/T-013/01-dau-tran-1920x1080.png` theo đúng rect HUD B, nội dung theo lề mục 3, icon PNG T-012, font Be Vietnam Pro Bold. Panel cũ che bằng mảng world lân cận (chỉ để dựng thử). Là mock, không phải ảnh chụp Unity.
- `so-sanh-concept.png`: concept A (T-014, thu về 1920x1080) bên trái, bản dựng thử bên phải, cùng tỉ lệ: toàn màn, Vitals, Quick bar, Minimap + Match.

## 9. Khác concept và chưa kiểm

- Khác concept A: lòng đá tối hơn (để đạt tương phản) và không có đường chia hàng khắc giữa các hàng của Vitals (concept có). Vòng đá ngoài 13 đến 14 px như yêu cầu, cả viền 16 đến 19 px nếu tính viền trong.
- Chưa qua qa chấm chéo, chưa xem trong Unity: NOT VERIFIED. Chưa có cảnh bo thu, máu thấp, nhặt đồ với skin A.
- `src/v2` và thư mục `debug/` chứa ảnh phụ, xóa được.
