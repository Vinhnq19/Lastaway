# Hộp thoại không có mock + nút nhấn K2 (unity, 07/11)

Lỗi Huy báo: (1) hộp "Cài đặt máy chủ" vỡ ô chọn, thiếu nhãn, khoảng trống lớn; (2) nhấn nút K2 (nút X đỏ) đế đổi hình nhấn nhưng icon/chữ không dịch xuống. Bậc VERIFIED trong Editor (Game view 1920x1080, luồng mock + JWT giả).

## Gốc lỗi ô chọn
`projects/lastaway/art/inbox/T-020/meta.json` (art-ux) ghi `border_LRTB` của 8 sprite theo thứ tự trái, TRÊN, phải, dưới thay vì trái, phải, trên, dưới. Ô chọn bị nhập với cạnh trên 68 px trên ô cao 56 px nên 9-slice chồng (hai vạch ngang, ô vuông lệch). Sửa phía Unity: bảng `BorderFix` trong `Tools/Editor/UiKit/UiKit.cs` khi nhập; art-ux nên sửa meta.json rồi bỏ bảng.

| Sprite | meta.json | Đúng (L, R, T, B) |
|---|---|---|
| kit_dropdown_closed | 19, 19, 68, 19 | 19, 68, 19, 19 |
| kit_dropdown_open | 19, 19, 67, 19 | 19, 67, 19, 19 |
| kit_progress_track | 12, 5, 12, 5 | 12, 12, 5, 5 |
| kit_progress_fill | 3, 2, 3, 2 | 3, 3, 2, 2 |
| kit_slider_track | 7, 3, 7, 3 | 7, 7, 3, 3 |
| kit_slider_fill | 2, 1, 2, 1 | 2, 2, 1, 1 |
| kit_scroll_track | 4, 5, 4, 5 | 4, 4, 5, 5 |
| kit_tag_neutral | 8, 6, 8, 6 | 8, 8, 6, 6 |

## Đã sửa
- Hộp Cài đặt máy chủ (làn A, `LobbyUiPrefabBuild.cs`): ô chọn `kit_dropdown_closed/open` + mũi tên `kit_arrow_down` trong ô phải 68 px, danh sách `kit_row_*`, thanh cuộn kit; nhãn cỡ 20 trên mỗi ô (Kết nối, Máy chủ, Cổng, URL API); ô nhập `kit_input_normal/focus`; panel co theo nội dung (820x860 -> 820x720), Lưu endpoint và Âm thanh & điều khiển một hàng. Logic endpoint của Vinh không đổi.
- Rà mọi hộp thoại/màn không có mock (Lịch sử, Bảng xếp hạng, Đăng xuất, Notice, Connecting...): không còn ô Sliced nào nhỏ hơn tổng border. Cổng máy (script `slicegate.cs` của unity, chạy trên LobbyUi, MatchOverlay, Hud, SpectatorRow, HudPickupToast, HudEffectChip, HudSlot): 251 ô Sliced đang hiện, 0 lỗi (trước sửa: 1 lỗi, chính là ô chọn).
- Nút nhấn K2 (unity, chỗ dùng chung): `KitButtonFace` khi nhấn dịch cả nội dung (icon + chữ) xuống bằng đế = 11 % cao nút (7,04 px ở nút M 64; 5,7 px ở nút X 52), thả ra trả lại, tắt thì không dịch. `UiKit.FaceAll` gắn cơ chế này cho mọi nút có sprite `kit_btn_*`, `kit_close_*`, `kit_iconbtn_*` (gồm nút X, nút chỉ icon), gọi ở cuối builder LobbyUi, MatchOverlay, HudPanels.

## Ảnh
| File | Nội dung |
|---|---|
| `so-sanh-cai-dat-may-chu.png` | trước/sau, mở từ sảnh và từ màn đăng nhập |
| `so-sanh-lich-su-bxh-dang-xuat.png` | trước/sau Lịch sử, Bảng xếp hạng, Đăng xuất |
| `nut-thuong-va-dang-nhan.png` | nút X và nút M "Lưu endpoint": bình thường và đang nhấn (phóng to) |
| `truoc/`, `sau/` | ảnh gốc 1920x1080 |

## Test
| Bộ | Kết quả |
|---|---|
| EditMode | 990/1002 (12 đỏ có sẵn sau merge: 6 test map, 5 ArtAudioLevelTests, 1 OfflineWorldSessionTests audio) |
| PlayMode | 175/176 (đỏ DepthSorting có sẵn) |

Thêm: `Tests/PlayMode/KitButtonFaceTests.cs` (mọi nút kit trong LobbyUi, MatchOverlay, Hud có `KitButtonFace`; nhấn dịch đúng đế, thả trả, tắt đứng yên; đế 7 px ở nút 64), `LobbySkinATests.ServerSettings_FieldLabels_KitDropdownWithArrow`, `LobbySkinATests.SlicedImages_NeverSmallerThanTheirBorders`.

## Ghi nhận, không sửa
Ô chọn skin "Nhân vật" ở sảnh (`Scripts/Presentation/Player/HeroSkinPicker.cs`, vùng Hào, vào từ merge main) chen vào bên trái menu, nút "<" ">" không theo skin A.

## Bỏ BorderFix (07/11)
art-ux đã sửa 8 border trong meta.json về L, R, T, B; bảng `BorderFix` trong UiKit đã xóa, border nhập lại từ meta giống hệt bản sửa tay. Cổng 9-slice 251 ô, 0 lỗi; PlayMode 175/176. Ảnh `sau/cai-dat-may-chu-meta-sua.png`.
