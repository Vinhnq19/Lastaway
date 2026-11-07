# Phối hợp lastaway

Các bên không nhắn trực tiếp được; file này là hộp thư. Chỉ **nối** mục mới cuối "Nhật ký", không sửa hay xóa mục của bên khác.
Mỗi mục: `[YYYY-MM-DD HH:mm] TỪ -> ĐẾN: nội dung`.

## Quy ước
1. **Khóa Unity:** chỉ một bên chạy Unity (Editor, batchmode, build, player) tại một thời điểm. Trước khi chạy ghi `UNITY_LOCK: <bên>` ở dòng đầu "Trạng thái"; xong ghi `UNITY_LOCK: none`. Các bộ test chạy tuần tự. Huy đang mở Editor thì chờ, không giành. Giữ/trả Editor cũng ghi ở `docs/Agent_Coordination.vi.md` (luật repo).
2. **Sở hữu file:** bên nào đang sửa file nào ghi ở "Đang sửa". Không sửa file bên khác đang giữ. Bảng quyền đầy đủ: `docs/CONTRACT_UI_V1.md` mục 4.
3. **Source:** module-engineer viết code, không chạy Unity; Integrator (`unity`) chạy builder, compile, test, chụp ảnh.
4. **Tài liệu chung** (`BACKLOG`, `DECISIONS`, `so-hien-hanh`): do CPO cập nhật; bên khác gửi nội dung qua báo cáo.
5. **Sắp hết hạn mức:** ghi mục `HANDOFF:` theo khuôn bên dưới.
6. Báo cáo chỉ ghi điều đã chạy thật, kèm đường dẫn log hoặc file evidence.

## Trạng thái
UNITY_LOCK: none
Contract hiện hành: UI v1 (`docs/CONTRACT_UI_V1.md`), khóa 2026-11-07
Build mới nhất: chưa build player trong đợt này

## Đang sửa
| Bên / người | File hoặc vùng | Từ lúc | Theo việc |
|---|---|---|---|
| unity (Integrator) | `Tools/Editor/UiKit/**`, `Scripts/Presentation/UI/Kit/**`, asmdef, prefab sinh ra, `Art/UI/Kit/**`, font Chakra Petch SDF, lỗi "lòi UI" ở Tạo phòng/Phòng chờ (tái hiện) | 2026-11-07 13:50 | T-022..T-025 |
| làn A (module-engineer) | LobbyUi builder, Lobby views, UiArt, test Lobby/UiArt | 2026-11-07 14:10 | T-024 rồi T-025 |
| làn B (module-engineer) | HudFeedback builder, Hud builder, Hud views, test Hud | 2026-11-07 14:10 | T-022 |
| làn C (module-engineer) | MatchOverlay builder, MatchOverlay views, test MatchUi | 2026-11-07 14:10 | T-023 |

## Cần art-ux
<!-- Mỗi dòng: màn | phần tử | đang giữ chỗ bằng | cần gì (cỡ 1080p, trạng thái). art-ux đọc mục này khi làm T-028. -->
| Màn | Phần tử | Giữ chỗ | Cần |
|---|---|---|---|
| B4 túi đồ | icon đạn | đã có: `icon_ammo` (T-028, CPO duyệt) | xong |
| B4 túi đồ | icon "đồ ở gần / dưới đất" | đã có: `icon_ground` (T-028, CPO duyệt) | xong |
| B9, B6, B7, B5 | fx_* bản nét cũ (chưa kaki) | `T-021/b/assets/fx_*` đã nhập | bản gen lại nếu Huy duyệt (man-hinh.md "Chưa dựng lại") |
| B5 bản đồ lớn | icon vòng trắng ở chú giải | `kit_icon_dot` | icon vòng (làn B) |
| B3 spectator | vũ khí của mục tiêu | đang là chữ | bảng tên vũ khí sang icon (làn C) |
| B2 bị loại | nguyên nhân Bo thu | `kit_ico_clock` trên đĩa (mock `icon_zone-timer` 60 px) | xác nhận icon (làn C) |
| A2, A4, A6 | icon nút Luyện tập offline, Lưu endpoint, Xem online, Điền đủ | `kit_ico_play`, `kit_ico_check`, `kit_ico_list`, `kit_ico_people` | duyệt icon tạm (làn A) |

## HANDOFF
<!-- Mỗi lần bàn giao một khối, mới nhất ở cuối. Bên nhận ghi "Đã nhận" vào Nhật ký. -->

## Nhật ký
[2026-11-07 13:50] unity -> mọi làn: mở đợt T-022..T-025 (CPO, Huy 07/11 "triển khai làm đồng loạt luôn"). Đã nhập 114 sprite kit + 15 fx vào `Art/UI/Kit/`, tạo `ChakraPetch-Bold SDF.asset` (232 ký tự, 0 thiếu so với `hud_charset.txt`), viết `UiKit.cs`, `KitButtonFace.cs`, `KitButtonLabel.cs`; compile 0 lỗi; ảnh tự kiểm nút S/M/L 3 loại + tắt, panel popup, chữ 96/40/24/20: `projects/lastaway/qa/T-024/nen-kit-tu-kiem.png`. Bậc INTEGRATED cho phần nền. UNITY_LOCK: unity.
[2026-11-07 14:01] unity -> làn A, CPO: tái hiện lỗi "lòi UI" (mock HappyPath + JWT giả trong bộ nhớ, không gọi API thật): ở Phòng chờ `RoomMapPanel.cs` (Vinh, gắn lúc chạy ở `Bootstrap.Lobby.cs:142`) tự dựng khung thẻ map, nút "+ Bot", "Điền đủ 6", "Xóa" bằng màu phẳng ngoài prefab và mock; Backdrop tắt ở Phòng chờ/Đang tải/Kết quả (`LobbyUiPresenter.cs:116`) nên lộ nền phía sau; chip tài khoản đè modal Vào phòng. Ảnh: `projects/lastaway/qa/T-024/loi-lori-ui/`. Giao làn A sửa phần hình, giữ logic map và bot. Chưa tái hiện với server thật (cần API + GameServer local).
[2026-11-07 14:05] unity -> mọi làn: đã nhập gói T-028 (CPO duyệt): `icon_ammo`, `icon_ground`, `kit_badge_key` (border 14), nền `bg_floor_tile` (Tiled) vào `Art/UI/Kit/`; thêm `UiKit.Floor`; compile 0 lỗi. Bảng ánh xạ từng mock: `projects/lastaway/art/inbox/T-028-unity-pack/`. UNITY_LOCK: unity.
[2026-11-07 14:23] unity -> mọi làn: nhận bàn giao WRITTEN của A (T-024, T-025), B (T-022), C (T-023). Compile toàn project 0 lỗi. Đã nhập T-028 đợt 2 (kit_killfeed_panel, kit_map_bg, map_grid, map_ring_zone, map_ring_safe). Đã dựng LobbyUi, MatchOverlay, SpectatorRow, Hud, HudFeedback, HudInventory, HudBigMap. Đang chạy EditMode + PlayMode toàn bộ. UNITY_LOCK: unity.
[2026-11-07 14:48] unity -> CPO, mọi làn: gộp vòng 2 xong. Compile 0 lỗi; EditMode 962/968, PlayMode 160/161 (đỏ có sẵn: 6 test map, DepthSorting); cổng cỡ chữ Unity 395 chữ, 1 báo nhầm (ô số đếm rỗng HudSlot). Đã dựng LobbyUi, MatchOverlay, SpectatorRow, Hud, HudFeedback, HudInventory, HudBigMap; vignette 2 lớp. Bằng chứng `projects/lastaway/qa/T-022..T-025/`. Bậc VERIFIED trong Editor cho T-022, T-023, T-024, T-025. UNITY_LOCK: none.
[2026-11-07 14:55] CPO -> unity (ghi lại): T-028 đợt 3 đã duyệt: kit_ico_practice, kit_ico_save, kit_ico_globe, kit_ico_fill (thay icon tạm Luyện tập offline, Lưu endpoint, Xem online, Điền đủ), icon_zone_cause (thẻ Bo thu B2), icon vũ khí spectator theo `T-028-unity-pack/weapons-icons.md` (CPO ghi `files/items/`, file thật nằm ở gốc gói). Vignette chốt 2 lớp. Làm cùng vòng sửa sau kết quả qa; chưa dùng Editor khi qa đang giữ khóa.
[2026-11-07 15:13] unity -> mọi làn: mở R3 theo qa-result (T-022 TRẢ LẠI; T-023, T-024, T-025 ĐẠT, sửa lỗi Vừa/Nhẹ). Đã nhập T-028 đợt 3 (143 file kit). Lớp tối modal: project Linear color space làm alpha 0,6 chỉ tối như 0,38 trong mock (đo thế giới 114 -> 73 Unity, 114 -> 53 mock); `UiKit.Colors.Dim` lên 0,8. Giao: B (vòng bo nét 4 px, minimap dùng chung sprite vòng, chú giải 4 hàng, toast khung kit + tên Việt), A (chip, v0.2, tick API, 4 icon mới, sàn Mất kết nối, chữ trùng), C (icon Bo thu, icon vũ khí, hạng/thẻ thống kê theo mock). UNITY_LOCK: unity.
[2026-11-07 15:31] unity -> CPO: R3 gộp xong. Compile 0 lỗi; EditMode 966/972, PlayMode 171/172 (đỏ có sẵn); cổng cỡ chữ 399 chữ, 1 báo nhầm cũ. Integrator sửa thêm: comment nuốt lệnh ghi số lượng ở `PickupToastView.cs` (toast luôn ×2), ô nhãn Đang dùng 340 px. Bằng chứng `projects/lastaway/qa/T-02x/r3/`. UNITY_LOCK: none.
[2026-11-07 15:34] unity -> CPO: R3b xong: `EliminationText.Title` = "BỊ LOẠI" (CPO quyết), dựng lại MatchOverlay; EditMode 966/972, PlayMode 171/172 (đỏ có sẵn). UNITY_LOCK: none.
