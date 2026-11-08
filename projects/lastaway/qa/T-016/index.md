# T-016 - Skin A đá khắc trên HUD B (unity, 2026-11-06)

## Lượt r11 (07/11, bản Huy duyệt, commit f892b29b) - bằng chứng ở `r11/`

Bậc: **VERIFIED trong Editor** (compile sạch, EditMode và PlayMode chạy, Game view 1920x1080 trận Luyện tập offline). Chưa build player, chưa PLAYTESTED. Art đọc thẳng từ `projects/lastaway/art/inbox` (cây làm việc trùng commit f892b29b).

| Phần | Đã làm |
|---|---|
| Rect khối | Theo `asset-list.md` mục 3 (px 1080p chia 1,5): Vitals (30, 857) 422x206, Quick bar (668, 950) 593x112, Minimap (1577, 30) 318x316, Match (1577, 350) 318x90. Thay rect HUD B |
| Khung | 4 khung vẽ Simple đúng cỡ sprite (mục 1); Vitals 3 dải nằm trong sprite |
| Quick bar | Tab đạn và tên vũ khí ẩn (`StripArea` inactive, code đạn vẫn chạy). Ô 54 px, bước 60, nhóm ở x 50 và 310, y 29. Icon 40 px giữa ô. Số phím 16 px Be Vietnam Pro Bold, vàng `FFDC6E` (token `HudPalette.Gold`), neo dưới phải tại (50, 50), viền tối `0C1417` |
| Vitals | Icon theo tâm mục 3 (hộp = canvas PNG). Thanh HP: rãnh 224x32 tại (91, 26), fill lùi 6 px, màu `C83A41` (token `HpFill`, mục 1). Adrenaline: rãnh 242x24 tại (95, 146), fill lùi 5 px. Số 30/28 px bold |
| Minimap | `MapArea` = lỗ khung đọc từ `meta.json` "hole" (269x268 tại 24, 24); RenderTexture và overlay giãn theo lỗ; `RectMask2D` padding 0; marker chiếu theo cạnh 269 px |
| Match | Icon tâm (71, 45) và (185, 45); "1/1" trái x 97; vạch chia x 155 rộng 2 px (không có trong sprite, unity thêm, màu `BEB496` lấy từ mock); đồng hồ trái x 211, kem |
| Khác | Hàng hiệu ứng dời lên y 157 u (8 u trên Vitals). Dải "Đang dùng" hiện ngay trên Quick bar (trong panel không còn chỗ) |
| Builder | `HudLayoutBBuild.Apply` + `HudSkinABuild` chạy lại sạch; material `HudTextBold SDF - Key Outline.mat` tự dựng lại mỗi lần |

| Ảnh (`r11/`) | Ca | Đọc từ HUD lúc chụp |
|---|---|---|
| `01-dau-tran-1920x1080.png` | Đầu trận | HP 100, giáp/mũ trống, 01:18 |
| `02-mac-giap-mu.png` | Có giáp và mũ (loot offline thật, như lượt trước) | giáp "50", mũ "30" |
| `03-hp-thap.png` | HP <= 25% | HP "22" màu `E5484D` |
| `04-bo-dang-thu.png` | Bo đang thu | bán kính 70,8 (75 về 54), 00:06 |
| `05-cam-vu-khi-adrenaline.png` | Ô chọn có đồ + Adrenaline | ô 1 khung vàng, gậy và chuối, số phím đọc được trên icon, "7s" |
| `06-chi-hud-nen-magenta.png` | Chỉ HUD | đo % che |
| `so-sanh-mock-va-unity.png` | Trái mock `dung-thu-hud-A.png`, phải Unity ảnh 01 | |
| `so-sanh-vitals.png`, `so-sanh-quickbar.png`, `so-sanh-minimap-match.png` | Trên mock, dưới Unity, phóng 2x | |

| Số đo | Kết quả |
|---|---|
| Lệch vị trí so mock | Số "100" và "1/1" trùng pixel (x 357-407, y 887-908); viền trên 4 khung lệch > 40 mức sáng ở 3,4-10,3% điểm (Vitals 10,3% do nền ảnh QA cũ của mock lộ qua góc vát) |
| % che màn (ảnh 06) | 13,47% (lượt trước 9,82%, khối to hơn theo concept) |
| Chữ tràn | 0 sau sửa: "×n" từng bị TMP cắt mất vì hộp thấp hơn dòng chữ; "10/10" và "00:48" đã đo |
| PlayMode | 135/136, đỏ `DepthSortingPlayTests.HeroWithInterpolatedRigidbodySortsByFinalFramePose` (vùng Hero, chập chờn, như trước) |
| EditMode | 953/959, 6 đỏ là test map có sẵn |
| Console | 0 lỗi sau dựng và chụp |

Test sửa theo r11: `HudLayoutB.cs` (mọi rect theo mục 3, `MapHole`, `MinMargin`), `HudAcceptanceTests` (lề, tab đạn ẩn, lỗ minimap, marker theo `SizePixels`, font số bold, màu Gold và HpFill, viền số phím, khung Simple), `HudFigmaV2Feedback` (Effects, Use), `Art04IconSweepTests` (số phím và ×n nằm trên icon có chủ đích, kiểm viền thay vì không chạm), `UiArtIntegrationTests` (icon ô 40 px, so rect overlay có sai số float).

Lệch spec (unity tự quyết, đổi được):
- Số "1/1" 20 px thay vì 22: trận 10 người "10/10" ở 22 px rộng 62 px, đè vạch chia x 155. Luật HUD cấm autosize.
- Viền số phím khoảng 2 px, không đạt 3 px: atlas `HudTextBold SDF` padding 6 ở 48 pt; muốn 3 px phải dựng lại atlas padding >= 9.
- Số lượng "×n" 13 u ở góc trên phải ô, có viền tối như số phím (mục 3 không ghi).
- Giáp, mũ, Adrenaline khi trống: icon mờ, không hiện số "0" (giữ hành vi T-013; mock hiện "0").
- Đồng hồ hiện "01:18" (định dạng mm:ss của game), mock "1:21".
- Lưới mờ trên minimap còn (HUD B); bản đồ là terrain thật, không phải ảnh đảo của mock.

Thấy, không sửa (vùng khác): ô vàng trên đầu Hero sau khi cầm gậy (ảnh 05), như lượt trước.

---

## Lượt trước (06/11, art 4a735e4d) - ảnh ở thư mục gốc T-016

Bậc: **VERIFIED trong Editor** (compile sạch, EditMode và PlayMode chạy, Game view 1920x1080 trong trận Luyện tập offline). Chưa build player, chưa PLAYTESTED.

Art dùng đúng bản **commit 4a735e4d** (qa ĐẠT). Lúc làm, art-ux đang ghi đè sprite T-015 và icon `hud-icons/png` trong cây làm việc (13:51-13:52, kiểu khác hẳn). Builder đọc art qua `HudLayoutBBuild.ArtInbox`; lần dựng này trỏ vào bản `git archive 4a735e4d`. md5 sprite và icon trong `Assets/` khớp commit.

## Đã làm
| Phần | Nội dung |
|---|---|
| Builder | `Tools/Editor/Hud/HudSkinABuild.cs` (mới), gọi từ `HudLayoutBBuild.Apply` (menu `Jungle Maze/HUD/Apply layout B + skin A (T-013, T-016)`). Chạy lại cho ra cùng kết quả |
| Sprite | 10 sprite `@2x` vào `Art/Hud/SkinA/`, PPU 300, border theo `meta.json` (thứ tự Unity L, B, R, T), Bilinear, không mipmap, không nén. Không nhập `hud_killfeed_plate` (chưa có object killfeed) và `hud_ammo_plate` (tùy chọn) |
| Khung | Vitals, Quick bar, Match: con `Skin` (Sliced, màu trắng) vẽ đầu tiên, nền cũ tắt; `ArtEdge`/`ArtBevel` tắt ở 4 khối và 8 ô. Rect ngoài không đổi |
| Icon | 8 icon kim loại ấm thay icon cũ trong `Art/Hud/StatusIcons/`, màu theo PNG (`Image.color` trắng) |
| Lề | Theo `asset-list.md` mục 3 (px 1080p chia 1,5): Vitals 40/20, Quick bar 40/18, Match 32 |
| Ô quick bar | 60 px (40 u), khe 3, nhóm cách 12; sprite `hud_slot_empty`/`hud_slot_selected` vẽ không tint (`QuickbarSlotView.skinSprites`). Ô trống không còn ô vuông placeholder (N1). Số phím màu Text `EDF2EE` |
| Thanh | Rect thanh là vùng fill; rãnh đá `TrackSkin` lùi ra 3 px (HP) và 2 px (Adrenaline). Fill dùng `hud_bar_fill_white` tint đỏ/vàng như cũ |
| Minimap | Lớp: map (terrain) -> `Overlay` -> `Rings` -> lưới, marker -> `Frame` đá trên cùng. Vòng bo chụp vào RenderTexture riêng nền trong suốt (`MinimapMapCapture.ringImage`), nên overlay chỉ làm tối terrain. Viền tối `0C1417` quanh vòng bằng `Outline` 1 u. `RectMask2D` cắt map và vòng 6 u trong mép để vòng không lòi ra ngoài khung. Nền vuông cũ tắt (lộ ở góc vát) |

## Ảnh (Game view 1920x1080, scene `Game`, trận Luyện tập offline)
| File | Ca | Cách tạo | Đọc từ HUD lúc chụp |
|---|---|---|---|
| `01-dau-tran-1920x1080.png` | Đầu trận | Vào Play | HP 100, giáp/mũ trống, bo 75 -> 54, 01:23 |
| `02-mac-giap-mu.png` | **Có giáp và mũ (nợ T-013)** | Luật loot offline thật: `Roll(Chest)` ra `WOODEN_ARMOR` (seed 6) và `WOODEN_HELMET` (seed 11), `SpawnLoot` cạnh Hero rồi `Pickup` | giáp "50", mũ "30", icon sáng |
| `03-hp-thap.png` | HP <= 25% | `OfflineMatchSession.ApplyDamage(78)` | HP "22" màu `E5484D` |
| `04-bo-dang-thu.png` | Bo đang thu | Tăng tốc x4 tới lúc gần thu, về x1 | bán kính 33,05 -> 30,74 (giữa 36 và 21), 00:04 |
| `05-cam-vu-khi-adrenaline.png` | Ô chọn + Adrenaline | Loot thật `SAHUR_BAT` + `BANANA`, `SelectWeapon(0)`; Adrenaline đặt vào store HUD | ô 1 khung vàng, gậy và chuối hiện, thanh vàng |
| `06-chi-hud-nen-magenta.png` | Chỉ HUD | Camera không vẽ world, nền magenta | dùng đo % che |
| `so-sanh-mock-A-va-unity.png` | Trái: mock `dung-thu-hud-A.png` (bản 4a735e4d), phải: ảnh 01 | | |
| `so-sanh-vitals.png`, `so-sanh-quickbar.png`, `so-sanh-minimap-match.png` | Cùng vùng, trái mock, phải Unity, phóng to | | |

## Số đo
| Số | Kết quả |
|---|---|
| Rect 4 khối | Không đổi: test `Layout_FourBlocksMatchLayoutB_At1280x720` đạt (dung sai 2 u) |
| Rect con | `Layout_ChildElementsMatchLayoutB` đạt với hằng số mới `HudLayoutB.cs` (lề skin A) |
| % che màn (ảnh 06) | 9,82% toàn màn, 17,25% góc dưới trái (T-013: 9,81% và 17,18%) |
| Chữ tràn | 42 chữ đang hiện; chỉ đồng hồ "00:48" rộng 41,1 u trong khung 40 u, đã nới khung về 42 u (cạnh phải giữ nguyên) |

Tương phản vòng bo trên minimap (đo pixel ảnh 1080p trong lòng khung; "sát viền" = điểm tối nhất trong 2 px quanh nét, "nền" = trung bình 5 px ra ngoài):

| Vòng | Viền tối | Sát viền p10 / median | Nền p10 / median |
|---|---|---|---|
| Đỏ (bo hiện tại) | 1 px (0,67 u) | 2,58-2,74 / 4,57-4,69 | 1,13-1,27 / 1,77-1,84 |
| Đỏ | **1,5 px (1 u), đã chọn** | **3,00 / 5,05** | 1,33 / 2,20 |
| Vàng (bo kế) | 1,5 px | 7,95 / 15,02 | 3,29 / 6,32 |

Chọn 1,5 px vì 1 px chưa đạt 3:1 ở p10. Nền terrain sau overlay vẫn gần màu đỏ (1,3-2,2:1); chữ đọc nhờ viền tối, không nhờ nền.

N-FILL: thanh Adrenaline fill cao 6,67 u, border trên + dưới của `hud_bar_fill_white` ở 300 PPU là 7,33 u. Đặt `pixelsPerUnitMultiplier` 1,2 cho fill thanh mảnh (border còn 6,1 u) để Unity không bóp đầu thanh. Thanh HP fill cao 12 u, giữ 1.

## Test
| Bộ | Kết quả | Ghi chú |
|---|---|---|
| PlayMode | 135/136 | Đỏ: `DepthSortingPlayTests.HeroWithInterpolatedRigidbodySortsByFinalFramePose` (vùng Hero, đỏ sẵn từ T-013; có lần chạy xanh, ca này chập chờn) |
| EditMode | 953/959 | 6 đỏ là test map của Vũ/Vinh, như T-008 và T-013 |
| Console | 0 lỗi do HUD | Sau chạy test có 1 lỗi `[Pool] BulletPrefab bị Despawn hai lần` (`console-errors.json`), từ test combat, không phải HUD |

Test đổi (vì lề skin A): `HudLayoutB.cs` (hằng số con theo px/1,5), `HudFigmaV2Feedback.Use` (dải dùng đồ trong lề mới), `HudAcceptanceTests.Palette_MatchesStyleGuide` (ô chọn là sprite vàng, không tint). Test mới: `SkinA_StoneFrames_MinimapOverlayUnderRings_FrameOnTop` (khung đá, nền cũ và ArtEdge tắt, ô trống không placeholder, thứ tự lớp minimap, RT vòng bo trong suốt ngoài nét, layer và hiển thị của LineRenderer trả lại sau khi chụp).

## Lệch spec (unity tự quyết)
- Viền tối vòng bo 1,5 px thay vì 1 px (lý do: số đo trên).
- Khung đồng hồ bo bắt đầu x 145 px thay vì 148 (chữ "00:48" 41,1 u).
- Dải tên vũ khí cao 22 px (14,7 u) theo asset-list; phím R thu về 14,7 u cho vừa. Nhãn dùng đồ 136 u, gợi ý 119 u để "Đang dùng God Leaf" và "Di chuyển để hủy" không bị cắt.
- Icon trong ô 22 u tại (9, 16) như T-013 (không đè số phím, số lượng).
- Số phím màu Text `EDF2EE`, không viền đen 2 px như asset-list N2 (viền TMP cần material riêng; tương phản đã 11:1).
- Overlay minimap dùng y nguyên sprite; terrain trong Unity sáng hơn mock một chút vì mock ghép từ ảnh chụp cũ.

## Thấy khi chạy, không sửa (vùng khác)
- Nhãn vũ khí vẫn "Bare Hands" khi cầm gậy (vùng Hào, như T-013).
- Sau khi cầm gậy, trên đầu nhân vật có một ô vàng trong world (ảnh 04, 05; không có ở 02, 03). Không thuộc HUD.
- Adrenaline đặt thẳng vào store HUD dừng ở "1s" và không tự hết (không có nguồn hiệu ứng thật trong offline). Chỉ là cách dựng ca, không phải lỗi skin.

## Cập nhật 07/11 (INTEGRATED, theo tin CPO)
| Việc | Đã làm |
|---|---|
| Đồng hồ bo bỏ Purple | Token mới `HudPalette.Cream` `F1E7C8` (hud-B-spec mục 7-8) cho số đồng hồ bo và số phím ô. Đổi màu: sửa `palette.Cream` trên `HudRoot` của `Hud.prefab` (builder chỉ điền khi trống) hoặc `HudPalette.Default` |
| Chạy lại khi art đổi | `HudSkinABuild` đọc border từ `meta.json`: `border_LRTB_2x` (định dạng lượt 2) hoặc `border_LRTB` @1x nhân 2 (định dạng mới trong inbox); `null` hoặc không có mục thì border 0. Border nào không vừa rect thì tự tăng `pixelsPerUnitMultiplier` (thanh Adrenaline nay 1,10). Kiểm đủ 10 PNG trước khi chép, thiếu thì không chép gì. Sau import so border sprite với `meta.json`, lệch thì dừng |
| Thử chạy lại | Đã chạy với art inbox hiện tại (sạch, Console 0 lỗi) rồi dựng lại bằng bản 4a735e4d. `Hud.prefab` và `Assets/` đang là bản 4a735e4d (md5 khớp) |
| Test | PlayMode 135/136, EditMode 953/959, đỏ có sẵn như trên. `Palette_MatchesStyleGuide` nay kiểm số đồng hồ bo và số phím là Cream |

Ảnh ở trên chụp trước cập nhật này (số đồng hồ bo còn tím, số phím trắng). Chưa chụp lại, theo CPO: bằng chứng cuối chờ skin A lượt 3.

## Chưa làm
- Killfeed (`hud_killfeed_plate`): HUD chưa có object killfeed.
- Icon HP và màu đồng hồ bo: giữ bản 4a735e4d, thay sprite được (chỉ thay PNG rồi chạy lại builder).
- Build player, PLAYTESTED.
