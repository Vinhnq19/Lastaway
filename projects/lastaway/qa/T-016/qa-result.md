# QA T-016 - Hud.prefab gắn skin A r11 (lastaway)

Kết luận: **ĐẠT** (0 lỗi Nặng, 7 lỗi Nhẹ; bậc VERIFIED trong Editor, chưa build player, chưa PLAYTESTED)
QA: qa · Ngày: 2026-11-07 · Nền: asset commit `f892b29b`; phần Unity (prefab, code, test) chưa commit, chấm trên working tree. Gồm chấm asset T-015 r11 trong Unity (CPO quyết 07/11, không chấm file riêng).
Cách chấm: đọc ảnh `r11/` và tự đo lại từ pixel; đọc `Hud.prefab`; tự chạy lại EditMode và PlayMode qua UnityMCP (Editor đang rảnh, đã trả).

## Theo tiêu chí brief
| # | Tiêu chí | Kết quả | Bằng chứng (qa tự đo) |
|---|---|---|---|
| 1a | Rect 4 khối 1920x1080 so spec mục 3 | **Đạt** | Ảnh nền magenta `06`, tách thành phần liên thông: Vitals (30, 857) 422x206; Quick bar (668, 950) 593x112; Minimap + Match gộp (1577, 30) 318x410 = 316 + khe 4 + 90. Trùng spec từng pixel |
| 1b | Viền panel đều 24 px | **Đạt (bằng mắt)** | 3 ảnh `so-sanh-*` phóng 2x: rim, hoa văn góc, rêu cùng cỡ ở 4 khối; không đo số (script quét viền trên ảnh cho kết quả không tin được vì đường bóng tối sát viền) |
| 1c | Quick bar không tab; số phím góc dưới phải trong ô | **Đạt** | Không còn tab đạn; 8 số 1-8 nằm góc dưới phải mỗi ô, kể cả ô có icon (`05`) |
| 1d | Vitals 3 dải đậm nhạt | **Đạt** | Dải trên tối, giữa sáng một bậc, dưới tối; không đường kẻ |
| 1e | Minimap cắt đúng lỗ 269x268, căn tâm | **Đạt** | `Hud.prefab`: `MapArea` stretch, sizeDelta (-32,667; -32) u trên khối 212 x 210,67 u = 269 x 268 px; vị trí x -0,333 u = -0,5 px, đúng tâm lỗ (158,5; 158) theo `meta.json` (hole 24..292, 24..291). Ảnh phóng không thấy khe hở giữa lỗ và khung |
| 2 | Ca động | **Đạt** | Có giáp/mũ (`02`: "50", "30", icon sáng); HP thấp (`03`: "22" đỏ, thanh co); bo thu (`04`: bán kính 70,8 về 54, 00:06, vòng đỏ và vàng); ô có đồ và ô chọn (`05`: gậy, chuối, ô 1 khung vàng); Adrenaline (`05`: thanh vàng, "7s"). Bo thu chỉ co 75 -> 70,8 trong ảnh nên khác biệt giữa các ảnh nhỏ, nhưng vòng đỏ/vàng và đồng hồ đổi đúng |
| 3a | Tương phản chữ >= 4,5 (đo trên ảnh Unity thật, pixel sáng nhất / nền trung vị) | **Đạt, 1 ngoại lệ** | "100": 12,75; giáp "50": 9,17; mũ "30": 9,20; "1/1": 11,51; đồng hồ bo: 10,86; số phím 1-8 (cạnh nền ô): 7,2 đến 11,7 (viền tối 2 px); **HP thấp "22" đỏ `E5484D`: 3,73** (xem N-RED) |
| 3b | Tương phản icon mảng chính >= 3 | **Đạt khi có số; mờ khi trống** | Tim 3,87; giáp 3,70 và mũ 3,76 (khi mặc); sét 7,63 (chạy) và 3,41 (trống). Giáp/mũ khi trống (icon mờ, theo thiết kế): 1,98 và 2,01, dưới 3 (xem N-DIM) |
| 4a | EditMode | **Đạt, đúng báo cáo** | Tự chạy: 953/959. 6 đỏ đều là test map/world: `DecorationsAreSavedButIgnoredByTheGame`, `OpeningAndSavingGameMapKeepsTheSameGameHash`, `PlainGameMapWithoutEditorFieldsInfersScaleFromCatalogSize`, `ValidationUsesGameRules` (LevelEditor), `PackagedMapHashMustMatchServer` (Protocol), `NormalWorldUsesAuthoredTeamMapWithFixedInteractivePlacements` (World). Không có ca HUD |
| 4b | PlayMode | **Đạt, đúng báo cáo** | Tự chạy: 135/136. 1 đỏ `DepthSortingPlayTests.HeroWithInterpolatedRigidbodySortsByFinalFramePose` ("Hero đã thực sự di chuyển", 10,104 không vượt 10,2): vùng Hero, đã đỏ ở lần chạy T-013 trước khi sửa HUD. Các ca HUD (`HudAcceptanceTests`, layout, `SkinA_...`) xanh |
| 4c | Console | **Đạt** | 0 Error sau 2 lần chạy test |

Lưu ý bằng chứng: `test-EditMode.json`, `test-PlayMode.json`, `console-errors.json` ở gốc `qa/T-016/` lưu lúc 14:02-14:03 ngày 06/11, **trước** ảnh r11 (18:10-18:13). Chúng không chứng minh r11; số r11 do qa tự chạy lại ở trên (cùng kết quả).

## Bốn lệch unity tự quyết, và killfeed
| Lệch | Đánh giá |
|---|---|
| "1/1" cỡ 20 thay vì 22 (tránh "10/10" đè vạch chia) | **Chấp nhận.** Nhìn trong ảnh vẫn khớp mock; lý do thật (trận 10 người) |
| Viền số phím khoảng 2 px thay vì 3 | **Chấp nhận.** Số đọc 7,2 đến 11,7:1; sửa chỉ khi dựng lại atlas |
| Icon giáp/mũ/sét mờ khi trống và ẩn số "0" (mock hiện icon sáng cùng số "0") | **Lệch ref nhìn thấy ngay từ giây đầu trận** (N-DIM). Là hành vi T-013 từ trước (spec G-4), không sai kỹ thuật; nhưng Huy ghi "làm giống hệt ảnh ref". Cần Huy chọn, không chặn |
| Vạch chia Match (2 px, `BEB496`) và dải "Đang dùng" hiện ngay trên Quick bar | **Vạch chia: chấp nhận** (có trong concept, sprite không vẽ). **Dải "Đang dùng": chưa thấy trong ảnh nào**, chỉ có khi đang dùng vật phẩm; NOT VERIFIED bằng mắt |
| Killfeed chưa có object | **Ghi nhận.** Sprite `hud_killfeed_plate` có sẵn nhưng chưa dùng; mock `dung-thu-hud-A.png` có killfeed, bản Unity không có. Không thuộc phạm vi T-016 theo index; cần quyết định task riêng |

## Lỗi
Nặng: **không**.

| # | Lỗi | Mức | Đích | File |
|---|---|---|---|---|
| N-RED | Số HP thấp màu `E5484D` trên nền Vitals chỉ 3,73:1, dưới ngưỡng 4,5 của brief. Đạt 3:1 của số lớn (30 px bold) nên đúng quy tắc asset-list ("số lớn 3"), nhưng không đạt mức CPO đặt | Nhẹ | unity hoặc CPO: nới token đỏ cho chữ (sáng hơn `E5484D`, ví dụ khoảng `F26A6F`) nếu muốn 4,5 | `03-hp-thap.png`, `HudPalette.cs` (token Red) |
| N-DIM | Giáp/mũ trống mờ còn 1,98-2,01:1 và không hiện "0"; ref hiện sáng + "0". Lệch ref ngay đầu trận | Nhẹ | Huy chọn qua CPO: giữ (spec G-4) hoặc theo ref (icon sáng, số "0") | `01-dau-tran-1920x1080.png`, `VitalsView.cs`, `HudGaugeRowView` |
| N-MARK | Marker người chơi trên minimap nhỏ (khoảng 10 px) và màu kem trên nền olive, khó thấy hơn chấm sáng có glow của mock; nền minimap thật tối và lẫn lưới nên chi tiết map vẫn mờ (đã nêu ở T-015 N-MAP). Chỉ soi bằng mắt, chưa đo | Nhẹ | unity: phóng marker hoặc thêm viền tối, xem ở lượt sau | `so-sanh-minimap-match.png`, `MinimapMapCapture.cs` |
| N-EDGE | Lề màn không đều theo concept: trái Vitals 30 px, phải minimap/Match 25 px, trên 30 px, dưới Vitals 17 px, dưới Quick bar 18 px. Gần mép dưới hơn các cạnh khác; cần soi trên thiết bị có vùng an toàn nếu game ra mobile | Nhẹ | CPO ghi nhận; không đổi (số lấy từ concept Huy) | `06-chi-hud-nen-magenta.png` |
| N-COVER | HUD che 13,47% màn (T-013: 9,81%; v2: 20,84%). Tăng do cỡ khối theo concept, đã được Huy chốt (DECISIONS 07/11) | Nhẹ (thông tin) | không | ảnh `06` |
| N-STALE | Tệp bằng chứng test ở gốc `qa/T-016/` cũ hơn r11 (xem trên) dễ gây hiểu nhầm | Nhẹ | unity thay bằng JSON chạy lại hoặc ghi chú trong `index.md` | `qa/T-016/test-*.json` |
| N-DELIV | Phần Unity (Hud.prefab, 6 file code HUD, test, builder `HudSkinABuild`, `.mat`, thư mục `Art/Hud/SkinA/`) chưa commit; đóng đợt cần commit đủ, kèm `.meta` | Nhẹ | CPO khi commit | `git status` |

## Giới hạn (NOT VERIFIED)
- Chưa build player, chưa PLAYTESTED; chỉ Game view 1920x1080 trận Luyện tập offline do unity chụp. qa không chụp lại ảnh mới (chụp lại sẽ khóa Editor lâu); đọc ảnh r11 và đo pixel.
- Giáp và Adrenaline trong ca động là đặt/loot offline (luật như T-013), không qua server; minimap với nhiều người chơi và online có bot chưa thử.
- Chưa thử 1280x720 và 2560x1440 với layout r11 (rect đặt theo px 1080p chia 1,5; test layout 1280x720 xanh trong PlayMode).
- Dải "Đang dùng" trên Quick bar chưa thấy bằng mắt.
- Số đo tương phản: pixel sáng nhất của chữ (top 5%) so với nền trung vị trong hộp; hộp đặt tay quanh chữ, sai số vài phần trăm. Không thay đổi kết luận trừ N-RED (3,73 cách 4,5 đủ xa).
- Phần Editor: đã thoát (không Play Mode), `EditorSettings.asset` bị chạy PlayMode đổi `m_EnterPlayModeOptions` 0 -> 1 và đã `git checkout` về 0; camera không đụng.
