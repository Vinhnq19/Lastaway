# T-023 - ESC cài đặt, xác nhận thoát, bị loại, spectator sang skin A (unity, 07/11)

Bậc: **VERIFIED trong Editor** (Play trận Luyện tập offline + mock UI-03 cho bị loại/spectator, Game view 1920x1080). Chưa PLAYTESTED.

## Đã làm
- `MatchOverlayPrefabBuild.cs` viết lại phần hình bằng UiKit theo px mock + bảng T-028: popup cài đặt (slider, toggle kit, chân hộp, nút Mặc định/Thoát trận/Tiếp tục), hộp xác nhận thoát, nút X đóng (bấm hộ Tiếp tục/Ở lại), thẻ nguyên nhân và thẻ thống kê bị loại (thiếu dữ liệu thì ẩn thẻ), spectator (thẻ trên, danh sách tối đa 9, thẻ mục tiêu, keycap A D E). Killcam giữ bố cục, chữ lên thang (sàn 24).
- ESC đóng một lớp (CPO chốt 07/11): `MatchOverlayInput` gọi `UiLayerStack.TryCloseTop()` trước; túi đồ/bản đồ lớn đang mở thì ESC đóng panel trên cùng, ESC tiếp theo mới mở menu. Kiểm trong Play: mở túi rồi bản đồ, lần 1 đóng bản đồ, lần 2 đóng túi, lần 3 không còn lớp (ảnh `../T-022/r2/esc-*.png`).
- Presenter không sửa.

## Ảnh
| File | Màn |
|---|---|
| `so-sanh-esc-bi-loai-spectator.png` | B1 cài đặt, B1 xác nhận thoát, B2 bị loại, B3 spectator |
| `r2/*.png` | ảnh gốc 1920x1080 (gồm `b3-chuyen` đổi mục tiêu) |

## Test
Cùng lượt với T-024 (EditMode 962/968, PlayMode 160/161, đỏ có sẵn). Thêm: `Tests/PlayMode/MatchUi/MatchOverlaySkinATests.cs` (6), `Tests/EditMode/MatchUi/MatchOverlayEscLayerTests.cs` (4). Sửa assertion hình: `MatchOverlayArtTests` (3), `MatchOverlayLayoutTests` (4).

## Lệch mock (chữ do tầng Application sinh, không sửa)
Tiêu đề "Bạn đã bị loại" (mock "BỊ LOẠI"); thẻ thống kê có nhãn "Hạ gục: 2", "Sống sót: 05:12" (mock chỉ số); hạng một chuỗi "#4 / 10"; spectator "Còn sống 9/10", không có cúp/hạng; nút B2 cao 64 (mock 72, kit không có cỡ 72); toast rộng 640 cho dòng killcam; thêm dòng "Nhạc nền đang tắt", "Chưa thoát được" 24 px.

## Cần art-ux
Icon vũ khí của mục tiêu spectator (đang là chữ); icon nguyên nhân Bo thu (đang `kit_ico_clock` trên đĩa).

## R3 (unity, 07/11)
Ảnh `r3/so-sanh-r3.png`. T23-1 lớp tối: Dim có sẵn nhưng project dùng Linear color space nên alpha 0,6 chỉ tối như 0,38 trong mock (thế giới 114 -> 73 Unity, 114 -> 53 mock); `UiKit.Colors.Dim` lên 0,8, nay tối như mock ở Cài đặt và Xác nhận thoát. Bo thu dùng `icon_zone_cause`; icon vũ khí ở thẻ nguyên nhân (Nỏ) và thẻ mục tiêu spectator (ẩn khi nguồn không gửi vũ khí: nguồn thật hiện gửi null); hạng tách "#4" 96 và "/ 10" 40 nhạt; thẻ thống kê chỉ còn số. Tiêu đề vẫn "Bạn đã bị loại" (hằng `EliminationText.Title` ở Application): cần CPO quyết đổi hằng hay bỏ.

## R3b (unity, 07/11)
Tiêu đề B2 đổi thành "BỊ LOẠI" theo mock (CPO quyết; hằng `EliminationText.Title` ở Application, test dùng hằng nên không phải sửa chuỗi). EditMode 966/972, PlayMode 171/172 (đỏ có sẵn).
