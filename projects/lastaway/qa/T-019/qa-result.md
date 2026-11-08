# QA T-019 - Giáp, mũ, Adrenaline bằng 0 hiện "0" và icon sáng theo ref (lastaway)

Kết luận: **ĐẠT** (0 lỗi Nặng, 1 lỗi Nhẹ; bậc VERIFIED trong Editor, chưa build player, chưa PLAYTESTED)
QA: qa · Ngày: 2026-11-07 · Nền: HEAD `93c02caf` + 4 file Unity chưa commit (`HudGaugeRowView.cs`, `HudPresenter.cs`, `HudPresenterTests.cs`, `HudAcceptanceTests.cs`). Nguồn: N-DIM của T-016, Huy 07/11 "theo ref".

## Theo tiêu chí brief
| # | Tiêu chí | Kết quả | Bằng chứng (qa tự đo) |
|---|---|---|---|
| 1 | Đầu trận 0/0/0 khớp mock: số "0" cùng font, màu, vị trí; icon không mờ | **Đạt** | Ảnh `01` so `dung-thu-hud-A.png` trên cùng tọa độ 1080p: hộp bao số "0" giáp (134-150, 947-967) mock và (135-150, 947-967) Unity; mũ (311-327) và (312-327), cùng y 947-967; màu trung bình pixel sáng (232, 236, 232) trùng; số điểm sáng 178 và 182 (cùng cỡ chữ). Lệch <= 1 px. Số "0" Adrenaline cùng màu (231, 234, 227 so 232, 236, 230) và cùng y 1004-1030. Ảnh ghép `so-sanh-vitals-mock-0-giap.png`: nhìn cạnh nhau không phân biệt được |
| 2 | Có giáp/mũ và Adrenaline > 0 vẫn đúng | **Đạt** | `02`: giáp "50", mũ "30"; `03`: "8s" vàng, thanh vàng; test xanh (dưới) |
| 3 | Tương phản icon >= 3:1 mảng chính (ảnh Unity) | **Đạt** | Đầu trận (trống): tim 3,87; giáp 3,70; mũ 3,76; sét 7,63. Trước T-019 ở T-016: giáp 1,98, mũ 2,01, sét 3,41 (mờ). Đo cùng phương pháp (trung vị mảng sáng so nền hàng) |
| 4 | Test HUD chỉ còn ca đỏ có sẵn | **Đạt** | qa tự chạy qua UnityMCP: EditMode 953/959, 6 đỏ cùng tên như T-016 (4 `LevelDocumentTests`, `Phase2Tests.PackagedMapHashMustMatchServer`, `OnlineWorldIntegrationTests.NormalWorldUsesAuthoredTeamMapWithFixedInteractivePlacements`); PlayMode 135/136, đỏ duy nhất `DepthSortingPlayTests.HeroWithInterpolatedRigidbodySortsByFinalFramePose` (vùng Hero, đỏ từ trước). Console 0 Error |
| 5 | Không có test nào bị xóa assertion vô lý | **Đạt** | Đọc `git diff` 4 file: 6 assertion đổi từ "ẩn số/icon mờ" sang hành vi mới, không dòng assert nào bị xóa mà không thay. `HudPresenterTests`: `IsNull` -> `AreEqual("0")` kèm kiểm tone Normal; vòng chờ Adrenaline hết đổi điều kiện `!= null` -> `!= "0"`. `HudAcceptanceTests`: `IsFalse(enabled)` -> `"0"` + màu `palette.Text` + icon trắng; test `LayoutB_VitalsIcons_DimUntilTheRowHasANumber` đổi tên thành `Vitals_EmptyRowsReadZeroWithFullIcon_ThenTheValue` và mở rộng: 0, mặc mũ 30, cởi ra về 0 |

## Đọc mã (qa không sửa)
- `HudGaugeRowView`: bỏ `DimIconAlpha` và dòng đặt alpha; icon giữ màu trắng nướng sẵn. Không còn nơi nào khác đổi alpha icon (đã grep trong diff).
- `HudPresenter`: giáp/mũ không mặc đọc "0" tone Normal; Adrenaline hết hoặc chưa có đọc "0" Normal; đang chạy giữ "Ns" vàng; hiệu ứng không đếm giờ vẫn không số (giữ hành vi cũ). Nhánh mới `active != _adrenalineShownActive` bảo đảm "0" hiện đúng lúc hiệu ứng vừa hết.

## Lỗi
Nặng: **không**.

| # | Lỗi | Mức | Đích | File |
|---|---|---|---|---|
| N-LABEL | Nhãn hàng (`Giáp`, `Mũ`) vẫn đặt tone Faint khi trống (code và test còn kiểm). Nhãn bị ẩn trong HUD B nên không nhìn thấy; chỉ lưu ý nếu sau này hiện lại nhãn | Nhẹ (thông tin) | không | `HudPresenter.cs` |

## Giới hạn (NOT VERIFIED)
- Chỉ ảnh Game view 1920x1080 do unity chụp (3 ảnh); qa không chụp lại. Ca Adrenaline vẫn đặt vào store HUD (như T-013); hết giờ thật chỉ được test EditMode kiểm (`expired: 0`), chưa có ảnh khoảnh khắc "8s" về "0".
- Chưa build player, chưa PLAYTESTED, chưa thử 1280x720 và 2560x1440.
- Phần Unity chưa commit; đóng đợt phải commit 4 file này.
- qa chạy PlayMode nên `EditorSettings.asset` bị đổi `m_EnterPlayModeOptions` 0 -> 1; đã `git checkout` về 0. Editor không ở Play Mode.
