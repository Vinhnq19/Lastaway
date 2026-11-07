# T-024 - Sảnh, đăng nhập, vào/tạo phòng, phòng của tôi, phòng chờ, thắng/thua sang skin A (unity, 07/11)

Bậc: **VERIFIED trong Editor** (compile sạch, EditMode và PlayMode toàn bộ, Game view 1920x1080 chạy luồng UI-02 bằng mock HappyPath + JWT giả trong bộ nhớ, không gọi API thật). Chưa build player, chưa PLAYTESTED, chưa chạy với API + GameServer thật.

Đợt chung T-022..T-025: contract `docs/CONTRACT_UI_V1.md`, phối hợp `docs/COORDINATION.md`. Code làn A (module-engineer), gộp và kiểm bởi unity.

## Đã làm
| Phần | Nội dung |
|---|---|
| Nền chung (unity) | `Tools/Editor/UiKit/UiKit.cs` (+ asmdef): nhập 114 sprite kit T-020, 15 fx T-021, gói T-028 (2 đợt) vào `Art/UI/Kit/`; font `ChakraPetch-Bold SDF` (232 ký tự, 0 thiếu); helper thang chữ 96/40/30/24/20, nút K2, panel, Dim, Floor. Runtime `Scripts/Presentation/UI/Kit/` (`KitButtonFace`, `KitButtonLabel`, `UiLayerStack`). Ảnh tự kiểm `nen-kit-tu-kiem.png` |
| LobbyUi | `LobbyUiPrefabBuild.cs` viết lại phần hình bằng UiKit theo px mock + bảng T-028; view chỉ thêm field hình; presenter chỉ phần gán hình (Backdrop theo `HasFloor`, đếm ngược reconnect, decor kết quả) |
| Lỗi "lòi UI" (Huy báo) | Tái hiện: `RoomMapPanel.cs` (Vinh, gắn lúc chạy ở `Bootstrap.Lobby.cs:142`) tự dựng thẻ map, "+ Bot", "Điền đủ 6", "Xóa" bằng màu phẳng; Backdrop tắt ở Phòng chờ/Đang tải/Kết quả nên lộ nền game phía sau; chip tài khoản đè modal. Sửa: phần tử map/bot dựng trong prefab bằng kit, `RoomMapPanel` dùng ref prefab (logic map, bot giữ nguyên); nền gạch ở Phòng chờ, Đang tải, Kết quả; modal giữa màn có Dim kín màn. Ảnh trước: `loi-lori-ui/` |
| UiArt | `UiArtAssetBuild` không còn ghi đè sprite kit và prefab feedback |

## Ảnh (mock trái, Unity phải)
| File | Màn |
|---|---|
| `so-sanh-1-sanh-vao-phong-phong-cua-toi.png` | A2 sảnh, A3 vào phòng (Dim), A4 phòng của tôi |
| `so-sanh-2-phong-cho-thang.png` | A6 phòng chờ, B6 thắng |
| `r2/*.png` | ảnh gốc 1920x1080 vòng 2 |

## Lệch mock có chủ ý
- A5 tạo phòng là hàng cuối màn Phòng của tôi (luồng hiện có), không phải popup.
- Ô mã A3 là một ô nhập (mã mặc định có thể dài hơn 6 ký tự).
- A6: trạng thái là câu 40 px ("Bắt đầu sau 4 s") thay số lớn 96 vì presenter chỉ trả một câu; nút Bắt đầu cạnh Sẵn sàng; header không có nút quay lại.
- Kết quả: khi `showKills` tắt chỉ còn thẻ "Sống sót: mm:ss" căn giữa (mock có 2 thẻ chỉ số); thêm nút "Xem online" (luồng có sẵn).
- Nút "Luyện tập offline" ở sảnh (luồng có sẵn, không có trong mock).

## Test
| Bộ | Kết quả | Ghi chú |
|---|---|---|
| EditMode | 962/968 | 6 đỏ là test map có sẵn (LevelDocumentTests x4, Phase2Tests, OnlineWorldIntegrationTests) |
| PlayMode | 160/161 | Đỏ `DepthSortingPlayTests` (vùng Hero, có sẵn) |
| Cổng cỡ chữ Unity | 395 chữ, 1 báo | ô số đếm rỗng của `HudSlot` (lúc chạy là "×n", thuộc ngoại lệ số trên ô) |
| Console | 0 lỗi code | 1 lỗi `Curl error 60: Cert verify failed` khi màn Đăng nhập gọi API thật (mạng/chứng chỉ, không phải code đợt này) |

Test làn A thêm: `Tests/PlayMode/Lobby/LobbySkinATests.cs` (7). Sửa: `LobbyLayoutTests.FigmaBlocks_ButtonsAndText_NoOverflow`, `UiArtIntegrationTests.PickupRow_ReusedIdsAndUnknownReset` (icon 56 px theo B9).

## Cần art-ux / Huy
- Icon tạm cần duyệt: Luyện tập offline `kit_ico_play`, Lưu endpoint `kit_ico_check`, Xem online `kit_ico_list`, Điền đủ `kit_ico_people`.
- Nền chưa có vignette gradient (bang-tong.md nói chạy lúc render).

## R3 (unity, 07/11)
Ảnh `r3/so-sanh-r3.png`. Chip tài khoản không cắt chữ (nới theo chữ, mép phải 1884); nhãn phiên bản "v" + `Application.version` (project đang 1.0, mock ghi v0.2); dấu tick `kit_icon_check` chỉ khi API sẵn sàng; 4 icon đã duyệt (`kit_ico_practice`, `kit_ico_save`, `kit_ico_globe`, `kit_ico_fill`). Lớp tối modal đậm hơn (alpha 0,8, lý do Linear color space, xem T-023).
