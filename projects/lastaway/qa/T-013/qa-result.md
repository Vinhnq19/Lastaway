# QA T-013 - HUD bản B trong Hud.prefab (lastaway)

Kết luận: **ĐẠT** (có giới hạn nêu ở mục "Giới hạn", 4 lỗi Nhẹ có đích xử lý)
QA: qa · Ngày: 2026-11-06 · Build/commit: working tree chưa commit trên HEAD `627125f7` (bằng chứng unity chạy 11:24-11:33, `Hud.prefab` sửa 11:14), Unity 6000.3.15f1, scene `Game` offline
Vòng 1 (10:xx, chỉ có ảnh Dev): NOT VERIFIED. Vòng 2 (file này): dùng `bang-chung-2/` của unity (luật chỉ unity chạy Editor, DECISIONS 06/11), qa tự đếm và soi lại.

## Môi trường
| Mục | Trạng thái |
|---|---|
| Editor | unity chạy qua MCP; qa không có kênh Editor, chỉ đọc JSON, ảnh, txt |
| Online có bot (Docker/AWS) | Không có: NOT VERIFIED |
| 1280x720, 1920x1080, 2560x1440 | Có ảnh + số đo từng khối |

## Theo tiêu chí brief
| # | Tiêu chí | Kết quả | Bằng chứng (qa tự kiểm) |
|---|---|---|---|
| 1a | Console 0 đỏ | **Đạt** | `console-trong-play.json`: 0 Error, 1 Warning (WebSocket gói MCP, ngoài HUD); `console-sau-recompile.json`: 0 entry |
| 1b | EditMode | **Đạt theo định nghĩa brief**: 953/959. qa đếm lại JSON: Passed 953, Failed 6 = `DecorationsAreSavedButIgnoredByTheGame`, `OpeningAndSavingGameMapKeepsTheSameGameHash`, `PlainGameMapWithoutEditorFieldsInfersScaleFromCatalogSize`, `ValidationUsesGameRules`, `PackagedMapHashMustMatchServer`, `NormalWorldUsesAuthoredTeamMapWithFixedInteractivePlacements`: đều test map/world của Vũ/Vinh/Quang Huy, không có ca HUD | `test-EditMode.json` |
| 1c | PlayMode | **Đạt**: 134/135. qa đếm lại: 1 đỏ `HeroWithInterpolatedRigidbodySortsByFinalFramePose` (vùng Hero). 2 test mới `LayoutB_VitalsIcons_DimUntilTheRowHasANumber`, `LayoutB_Minimap_DrawsTheMapWithoutPlayers` Passed, cùng `Layout_FourBlocksMatchLayoutB_At1280x720`, `Layout_ChildElementsMatchLayoutB` | `test-PlayMode.json` |
| 1c' | DepthSorting đỏ từ trước T-013 | **Đạt (gián tiếp)**: lần chạy 10:53 trước khi sửa (133 ca) đỏ đúng ca này; file test và `Presentation/Player`, `Presentation/Rendering` không nằm trong diff T-013. Chưa chạy baseline bằng stash (luật cấm) | `test-PlayMode-truoc-T013.json`, `baseline-depthsorting.md` |
| 2a | Rect 4 khối so spec 1920x1080 | **Đạt**: Vitals 310,5x172,5, Quick bar 592,5x126, Minimap 240x240, Match status 240x88,5, lệch <= 0,3 u so spec mục 2 | `do-phan-giai-1920x1080.txt`, `do-rect-1920x1080.txt` |
| 2b | % che | **Đạt**: 9,81% toàn màn, 17,18% góc dưới trái (qa đếm lại pixel trên `03-do-che-nen-magenta.png`), thấp hơn B 11,65% và v2 20,84% | ảnh 03 |
| 2c | Minimap map thật, vòng bo, người chơi không lộ | **Đạt**: vùng chụp (0,0)-(150,150) trùng đúng map 150x150 ô, RT 256; ảnh thấy địa hình, tường, vòng bo đỏ/vàng, marker bản thân; ca có `PlayerView` dựng tạm: tổng màu vùng tâm lệch 0 (bị ẩn), sprite đối chứng lệch -276 (hiện) | `minimap-vung-chup.txt`, `minimap-an-nguoi-choi.txt`, `ca-bo-dang-thu.png` |
| 3a | 1280x720, 2560x1440 | **Đạt**: qa đo lề từ txt: 1280 hệ số 1 (lề 24 px), 2560 hệ số 2 (lề 48 px); Vitals trái/dưới, Minimap và Match status phải/trên đúng lề; Quick bar cách đều 442,5/442,5 và 885/885; mọi khối `inside=True`. Soi ảnh: không tràn, không chồng PickupFeed, DangerBanner, EffectsRow cách Vitals | `do-phan-giai-*.png/.txt` |
| 3b | Chữ không tràn | **Đạt**: 41 chữ đang hiện, `no overflow` | `chu-tran.txt` |
| 3c | Máu giảm | **Đạt**: HP 22 (<= 25%) màu đỏ, thanh co theo; sát thương thật qua `ApplyDamage(78)` | `ca-hp-thap.png`, `o-toi-vitals-phong-to.png` |
| 3d | Có giáp/mũ (số hiện) | **Đạt một phần**: mũ 30 hiện số, icon sáng (nhặt thật). Giáp: ảnh `ca-mac-giap-mu.png` **không có số giáp** (icon giáp mờ), khác lời khai "đọc được 50"; Dev ghi trận offline ghi đè giáp ngay sau. Số giáp chỉ có bằng test PlayMode (chung `HudGaugeRowView` với mũ, nên rủi ro thấp). Giới hạn | `ca-mac-giap-mu.png` |
| 3e | Adrenaline chạy | **Đạt**: icon sáng, thanh vàng, "5s" đọc được (12 u) | `ca-adrenaline-dang-chay.png` (đặt thẳng `EffectStateStore.Apply`, giới hạn) |
| 3f | Nhặt đồ vào quick bar, đổi ô | **Đạt**: nhặt gậy vào ô 1; chọn ô 1 hiện viền Amber, trước đó không viền | `ca-doi-o-1-chua-cam.png`, `ca-doi-o-2-cam-o-1.png` |
| 3g | Bo thu | **Đạt**: bán kính 37,6 -> 36; vòng đỏ (hiện tại) và vàng (kế) thu nhỏ trên world và minimap | `ca-bo-dang-thu.png` |

## Bốn điểm CPO soi so với spec
| Điểm | Kết luận |
|---|---|
| Giáp/mũ = 0 không số | **Đạt** (spec mục 3: số ẩn, icon mờ). Mờ bằng alpha 0,35 thay vì token Muted: tương đương |
| Adrenaline không số khi rỗng | **Đạt** (spec mục 3) |
| Ô trống quick bar họa tiết đứt dày | **Lệch mock, không trái chữ spec**. Xác nhận bằng ảnh 3 độ phân giải: khung đứt + ô xám của skin hiện có, nhìn rối; số phím 10 u gần không đọc được trên họa tiết. Mức Nhẹ, đích T-014/art-ux |
| Minimap phóng sát tâm | **Đạt**: minimap là toàn map 150x150; nội dung dồn giữa vì khu đô thị nằm giữa, rìa là cỏ. Độ tương phản map thấp (nền xanh ô-liu nhạt), đọc được vòng bo nhưng chi tiết mờ: Nhẹ |
| Ô tối cạnh icon giáp (qa tự phát hiện vòng 1) | **Đạt, không phải lỗi**: ảnh phóng to cạnh nhau: nền world có ô tối, nền magenta không có; là đá lát nhìn xuyên Surface alpha 0,92 |

## Năm điểm Dev tự lệch spec
Giữ đánh giá vòng 1: chấp nhận cả 5; CPO cũng đã chấp nhận. Riêng vòng bo đỏ/vàng thay Purple: CPO ghi DECISIONS, Huy xem lúc playtest. Spec nên cập nhật: đồng hồ bo x 106, icon ô 22 u.

## Giới hạn (không chặn ĐẠT, theo CPO)
- Giáp và Adrenaline đặt thẳng vào store HUD (không qua luật offline); số giáp chưa thấy bằng mắt trong ảnh.
- Minimap với người chơi khác: dùng `PlayerView` dựng tạm; chưa thử online có bot.
- Bằng chứng là bản working tree chưa commit; commit khác nội dung thì phải kiểm lại.
- PlayMode/EditMode do unity chạy, qa chỉ đọc JSON (không tự chạy).
- Chưa build player, chưa PLAYTESTED (chờ Huy chơi). Bậc: **VERIFIED trong Editor**.

## Lỗi Nhẹ (đề xuất, không chặn)
| # | Lỗi | Mức | Đích |
|---|---|---|---|
| N1 | Ô trống quick bar: khung đứt + ô xám làm rối mắt; đề xuất nền phẳng Track khi trống | Nhẹ | T-014 (art-ux) |
| N2 | Số phím 1-8 (10 u) gần không đọc được trên họa tiết ô | Nhẹ | T-014; trong ngoại lệ 10 u đã duyệt nhưng nên tăng tương phản |
| N3 | Minimap tương phản thấp, chi tiết map mờ | Nhẹ | T-014 hoặc unity chỉnh màu RT |
| N4 | Ảnh `ca-mac-giap-mu.png` không chứng minh số giáp; thêm ca ảnh giáp có số khi có đường nhặt giáp thật | Nhẹ | unity, khi có giáp rơi |

Bug mở: không. Nhãn "Bare Hands" khi cầm gậy: vùng Hào, theo brief không tính.
