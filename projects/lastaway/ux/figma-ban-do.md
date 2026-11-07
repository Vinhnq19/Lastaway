# Bản đồ Figma Lastaway - page `116:3`

File: `oeZgisxdiR80hRqbikUwPb` ("Lastaways"), page `116:3` "Lastaway - Màn hình trong game". Chủ sở hữu từ 06/11/2026: art-ux (T-007). Chỉ đọc, chưa ghi gì vào Figma.

## 1. Bản chất page và cách đếm

- Đếm bằng `get_metadata` nodeId `116:3`, lọc con trực tiếp của page: **36 frame** 1920x1080 (`01_` đến `36_`, lưới 4 cột, 10 hàng) và 10 text nhãn hàng `Row · A` đến `Row · J` (`135:2` đến `135:11`). Không có node loại khác. Số dòng bảng 3 = 36.
- Mỗi frame là **bản chụp hierarchy Unity** (gốc `LobbyUi(Clone)`, `Hud(Clone)` + `MatchOverlay(Clone)`), không phải thiết kế vẽ tay. Nguồn: audit 31/10 (`Docs/Research/Art/01_Visual_Audit.vi.md`), build `5e0227e8`/`2eff290c`. Hệ quả: Figma 116:3 là "ảnh chụp build ngày 31/10", không phải đích thiết kế; đích bố cục vẫn là v2 (`39:2`) trong `Docs/UI_Wireframes.vi.md`.
- Frame HUD 17-25 là lớp UI trên nền phẳng, không có world, nhân vật, VFX.
- Ảnh build tương ứng: `Docs/Figma/Screens_2026-10-31/` (34 ảnh, `index.txt`) và `Docs/UX/Round2_Captures/` (5 ảnh: 01, 03, 04, 05, 06). Figma có 36 frame = 34 ảnh Unity trừ 3 (`01_main-menu`, `02_join-form`, `06_login`, bị bản vòng 2 thay) cộng 5 ảnh vòng 2 (R2 01, 03, 04, 05, 06).

## 2. Cách đọc cột "Trạng thái so với build"

So frame (build 31/10) với build hiện tại (HEAD `88a70cad`), bằng **git và code, không mở Unity** (ràng buộc T-007). Ba mức:

| Mức | Nghĩa |
|---|---|
| Khớp | Không có commit nào sau 31/10 đổi phần UI này, hoặc đổi nhưng không thấy trên frame |
| Lệch | Có commit sau 31/10 đổi diện mạo hoặc hành vi frame; frame cũ hơn build. Kèm commit làm bằng chứng |
| Chưa làm | Frame vẽ thứ build chưa có |

Lệch chung (áp lên mọi frame có nút chính / chữ phụ / số), chép vào dòng frame bằng mã L1-L4:

| Mã | Lệch | Bằng chứng |
|---|---|---|
| L1 | Màu mặt nút chính `3B8D7B` thành `2E7263`; thêm `ButtonDanger.png` cho hành động phá hủy; chữ Faint `77908A` thành `859E98` (tương phản 4,5:1) | `52913c30`; `Assets/_JungleMaze/Art/UI/Menu/ButtonPrimary.png`, `ButtonDanger.png`, `Hud/HudPalette.cs`; `Tests/EditMode/Hud/HudPaletteContrastTests.cs` |
| L2 | Mọi modal có scrim; nút Đăng xuất / Xóa / Rời trận dùng kiểu Danger | `ad43bb77`; `Tools/Editor/Lobby/LobbyUiPrefabBuild.cs`, `Tools/Editor/MatchOverlay/MatchOverlayPrefabBuild.cs`; ảnh `Docs/Research/Art/captures/before|after/modal_*.png` |
| L3 | Số trong lobby và overlay dùng font Be Vietnam Pro (`HudTextBold`) thay font pixel | `ad43bb77`; ảnh `captures/after/hud_numbers.png` |
| L4 | Luồng lobby, phiên tài khoản, luật phòng, ResultModel đổi sau chụp | `8c69c0a7`; `Application/Lobby/LobbyFlow.cs`, `ResultModel.cs`, `UI/Lobby/LobbyUiPresenter.cs`. Hiệu ứng nhìn thấy chưa kiểm vì không mở Unity: **NOT VERIFIED** |

## 3. Bảng frame (36 dòng)

Cột "WF cũ" là node tương ứng trong `Docs/UI_Wireframes.vi.md` (v1 hoặc v2) nếu có.

| # | Node | Tên frame | Màn hình | Ticket | Ảnh build 31/10 | WF cũ | Trạng thái so với build | Ghi chú lệch / bằng chứng |
|---|---|---|---|---|---|---|---|---|
| 1 | `121:285` | 01_login | Đăng nhập | UI-02 (vòng 2) | R2 `01_login.png`, build `06_login.png` | không có | Lệch | L1. T-004: Canvas Login che Game khi offline (UI-02 mục 8) |
| 2 | `121:355` | 02_register-validation | Đăng ký, lỗi kiểm tra | UI-02 | `07_register-validation.png` | không có | Lệch | L1 |
| 3 | `121:2` | 03_home | Home (Vào phòng, Tạo phòng, Lịch sử, BXH, Cài đặt, Thoát, thẻ tài khoản) | UI-02 | R2 `03_home.png` | v2 `42:88` (Menu chính, khác bố cục) | Lệch | L1, L4. `MainMenuView.cs` đổi ở `8c69c0a7` |
| 4 | `133:31` | 04_logout-confirm | Hộp xác nhận đăng xuất | UI-02 | R2 `04_logout-confirm.png` | không có | Lệch | L2 (V14 đã sửa: nút Danger, có scrim) |
| 5 | `133:195` | 05_settings-server | Cài đặt máy chủ (host/port/API) | UI-02 | R2 `05_settings-server.png` | v2 `42:137` (bảng endpoint nằm trên menu) | Khớp | Chưa có commit đổi `ServerSettingsView`; V16 (panel đè logo) vẫn mở |
| 6 | `121:106` | 06_join-modal | Modal vào phòng | UI-02 | R2 `06_join-modal.png`, build `02_join-form.png` | v2 `42:137` | Lệch | L2 |
| 7 | `121:157` | 07_join-invalid-code | Vào phòng, mã sai ký tự | UI-02 | `03_join-form-invalid-code.png` | v2 `42:137` | Lệch | L2; luật mã đổi ở `Domain/Lobby/RoomCodeRules.cs` (`8c69c0a7`) |
| 8 | `121:210` | 08_join-room-full | Vào phòng bị từ chối, phòng đầy | UI-02 | `04_join-rejected-room-full.png` | không có | Lệch | L2. AC7/AC8 chưa có `RoomNotFound`, `MatchInProgress` (UI-02 mục 8) |
| 9 | `121:263` | 09_connecting | Đang kết nối | UI-02 | `05_connecting.png` | không có | Khớp | V13 vẫn mở: chỉ chữ tĩnh, chưa có chỉ báo tiến trình |
| 10 | `121:427` | 10_room-manager | Quản lý phòng, danh sách + tạo | UI-02 | `08_room-manager.png` | không có | Lệch | L1, L4 |
| 11 | `121:598` | 11_room-delete-confirm | Xác nhận xóa phòng | UI-02 | `09_room-delete-confirm.png` | không có | Lệch | L2: `ad43bb77` đổi thành modal giữa màn có scrim, câu hỏi nêu tên phòng (`LobbyUiPresenter.cs`). Sửa V1. Ảnh sau: `captures/after/modal_room_delete.png` |
| 12 | `121:798` | 12_room-create-limit | Quản lý phòng, lỗi giới hạn tạo | UI-02 | `10_room-create-limit.png` | không có | Lệch | L1, L4 (luật phòng) |
| 13 | `121:1054` | 13_lobby-waiting | Phòng chờ, chờ đủ người | UI-02, SRV-03 | `12_lobby-waiting.png` | v2 `42:182`, v1 `8:223` | Lệch | L3, L4. V8 vẫn mở: nền phẳng, mất rừng pixel. Commit `a84466dd` đặt lobby 5 s |
| 14 | `121:968` | 14_lobby-countdown | Phòng chờ, đếm ngược | UI-02, SRV-03 | `11_lobby-countdown.png` | v2 `42:182` (15 s khi đủ 6) | Lệch | L3, L4; `a84466dd` đổi lobby còn 5 s, WF v2 ghi 15 s |
| 15 | `121:1140` | 15_loading | Đang tải (hướng dẫn điều khiển) | UI-02 | `13_loading.png` | v2 `42:269` | Lệch | L3 (V7 "Còn" đọc thành "0òn" đã đổi font) |
| 16 | `121:1171` | 16_loading-error | Đang tải, lỗi | UI-02 | `14_loading-error.png` | không có | Khớp | |
| 17 | `123:38` | 17_hud-in-match | HUD trong trận | UI-01 | `26_hud-in-match.png` | v2 `41:2`, `41:180` | Lệch | L1 (Faint), L3. V9 nhãn vật phẩm bị cắt và lẫn tiếng Anh; V10 icon vật phẩm và minimap còn placeholder (`HudItemIconCatalog.cs`, `MinimapView.cs`) chưa kiểm lại |
| 18 | `123:1133` | 18_hud-pickup-toast | HUD, thông báo nhặt đồ | UI-01 phần 2 (`UI-01-P2.md`) | `27_hud-pickup-toast.png` | v2 `48:239` | Khớp | Cùng L1 với frame 17 |
| 19 | `123:2291` | 19_hud-low-hp-zone | HUD, máu thấp, bo sắp thu | UI-01 | `28_hud-low-hp-zone.png` | v2 `48:239` | Khớp | Cùng L1 |
| 20 | `123:3449` | 20_esc-menu | Menu ESC (cài đặt) | UI-03 | `29_esc-menu.png` | v1 `7:337` (5 tab, nút Lưu) | Lệch | L1. V17 nhãn xuống 2 dòng vẫn mở. Frame ít tab hơn WF v1: UI-03 mục "Mâu thuẫn" M1 |
| 21 | `125:2` | 21_leave-confirm | Xác nhận thoát trận | UI-03 | `30_leave-confirm.png` | không có | Lệch | L2: scrim `0B1114` alpha 0,72 và hộp đặc (`MatchOverlayPrefabBuild.cs`, `ad43bb77`). Sửa V3. Ảnh sau: `captures/after/modal_leave_match.png` |
| 22 | `125:1575` | 22_eliminated | Bị loại | UI-03 | `31_eliminated.png` | v2 `43:283` | Khớp | Frame chỉ có hạng, ai hạ, vũ khí, khoảng cách, số hạ gục; v2 có thêm "Sát thương 214" và máu còn lại của kẻ hạ, GDD không có (UI-03 M4) |
| 23 | `120:2` | 23_killcam | Killcam | UI-03 | `33_killcam.png` | không có | Khớp | V15: lưới debug + 2 chấm. `killcamDuration` 5 s ở trạng thái "Baseline cũ - chưa duyệt" (UI-03) |
| 24 | `120:1221` | 24_killcam-unavailable | Thông báo killcam không khả dụng | UI-03 | `34_killcam-unavailable-notice.png` | không có | Khớp | |
| 25 | `125:2777` | 25_spectator | Theo dõi (Spectator) | UI-03 | `32_spectator.png` | v1 `7:304` (có "đồng đội", Báo cáo, Khóa) | Lệch | `HudSpectatorMask.cs` (`5e0227e8`, 31/10) tắt Canvas HUD khi Spectator nên V2 (hai thẻ máu chồng nhau) đã sửa trong code; frame còn cảnh cũ. Chưa chụp lại: **NOT VERIFIED** |
| 26 | `121:1192` | 26_results-victory | Kết quả, chiến thắng | UI-02 | `15_results-victory.png` | v2 `43:445` | Lệch | L3, L4 (`ResultModel.cs`). V6 chưa dàn dựng thắng/thua; V7 số "5" giống "S" đã đổi font |
| 27 | `121:1368` | 27_results-defeat | Kết quả, thua (Hạng #3) | UI-02 | `17_results-defeat.png` | v2 `43:445`, v1 `3:5` | Lệch | L3, L4 |
| 28 | `121:1280` | 28_results-replay-waiting | Kết quả, chờ chơi lại | UI-02 | `16_results-replay-waiting.png` | không có | Lệch | L3, L4. V18 nhãn "Về menu"/"Về sảnh"/"Quay lại" không thống nhất |
| 29 | `121:1456` | 29_reconnecting | Mất kết nối, đang kết nối lại | UI-02 | `18_reconnecting.png` | v2 `43:131` | Lệch | L3 (V7 "Còn" thành "0òn") |
| 30 | `121:1478` | 30_reconnect-failed | Mất kết nối, không kết nối lại được | UI-02 | `19_reconnect-failed.png` | không có | Khớp | |
| 31 | `121:1499` | 31_history | Lịch sử, có dữ liệu | UI-02 (NET-02) | `20_history.png` | không có | Khớp | V12: một chuỗi chữ nhỏ dồn mọi cột. Cần NET-02 (UI-02 mục 8) |
| 32 | `121:1532` | 32_history-empty | Lịch sử, trống | UI-02 | `21_history-empty.png` | không có | Khớp | V11 panel trống lớn |
| 33 | `121:1557` | 33_history-unavailable | Lịch sử, tạm ngừng | UI-02 | `22_history-unavailable.png` | không có | Khớp | V11 |
| 34 | `121:1595` | 34_leaderboard | BXH trận, có dữ liệu | UI-02 | `23_leaderboard.png` | không có | Khớp | |
| 35 | `121:1645` | 35_leaderboard-empty | BXH trận, trống | UI-02 | `24_leaderboard-empty.png` | không có | Khớp | V11 |
| 36 | `123:2` | 36_leaderboard-unavailable | BXH trận, tạm ngừng | UI-02 | `25_leaderboard-unavailable.png` | không có | Khớp | V11 |

Tổng: Khớp 15 (trừ L1 chung), Lệch 21, Chưa làm 0. Nhưng L1 (đổi màu nút/Faint) áp lên gần mọi frame có nút chính hoặc chữ phụ, nên "Khớp" ở đây nghĩa là không có lệch riêng ngoài L1. Muốn khớp thật phải **chụp lại toàn bộ 36 frame từ build hiện tại**; việc này cần Unity Editor nên ngoài giới hạn T-007, đề xuất ở mục 6.

Cách kiểm "lệch" là git và code. Chưa so ảnh frame với ảnh build hiện tại; mức đó **NOT VERIFIED**.

## 4. Chỗ page `116:3` khác `Docs/UI_Wireframes.vi.md`

| Khía cạnh | `UI_Wireframes.vi.md` | Page `116:3` |
|---|---|---|
| Tên file Figma | `Codefest-2027` | Hiện là "Lastaways" (cùng key `oeZgisxdiR80hRqbikUwPb`); tài liệu cũ cần cập nhật tên |
| Bản chất | Thiết kế vẽ tay: Page 1 (v1, 15 frame 1280x720) và `Lastaway v2 - Redesign` (`39:2`, 10 frame + HUD phản hồi) | Ảnh chụp Unity, 36 frame 1920x1080, đã vào Figma thành lớp (hàng A-J) |
| Style guide, component, variables | Có (`39:3`, `Button`, `Slot`, `PlayerSlot`, `KillfeedRow`, `KeyCap`); chưa có variables | Không có component hay style guide; chỉ lớp sao chép từ prefab |
| Phạm vi màn | HUD, Menu chính, Vào phòng, Phòng chờ, Đang tải, Mất kết nối, Bị loại, Kết quả, Cài đặt v1, Theo dõi v1, Balo, Bản đồ lớn | Thêm: Đăng nhập, Đăng ký, Home, đăng xuất, cài đặt máy chủ, quản lý phòng (xóa, giới hạn), lịch sử, bảng xếp hạng, killcam, ESC, xác nhận thoát |
| Luồng vào | Guest: tên + mã phòng (vòng 1) | Đăng nhập trước, rồi Home (UI-02 vòng 2, 31/10): bỏ Guest |
| Mất kết nối | Có frame v2 `43:131` | Có `29_reconnecting`, `30_reconnect-failed` |
| Lobby | 15 s khi đủ 6 | Cùng số trên frame; build hiện 5 s (`a84466dd`) |
| Bị bỏ trên 116:3 | HUD feedback `48:239`/`48:461`, balo `3:3`, bản đồ lớn `7:186`, bay/nhảy dù, bị hạ gục chờ cứu | Không vẽ lại |
| HUD phản hồi | Frame thiết kế riêng | Gộp vào HUD capture `18_hud-pickup-toast` |

Hệ quả: nguồn UX đích gồm **hai lớp**. v2 (`39:2`) là bố cục và hành vi đã duyệt cho HUD, Menu, Lobby, Loading, Kết quả; `116:3` là hiện trạng build và là nơi gắn node vòng 2 mà UI-02 đã trích (`121:2`, `133:31`, `133:195`). Wireframe cũ chưa ghi `116:3`, đề xuất cập nhật ở mục 6; không sửa `Docs/UI_Wireframes.vi.md` theo giới hạn T-007.

## 5. Frame và màn chưa có ticket

- Mọi 36 frame đã gắn UI-01, UI-02 hoặc UI-03. Không frame mồ côi.
- Frame gắn ticket nhưng phụ thuộc việc chưa xong: 31-36 (lịch sử, BXH) cần NET-02; 01-04, 06 cần T-004; 08 cần Vinh/Vũ thêm `DisconnectReason` (AC7, AC8).
- Màn có trong WF cũ nhưng **chưa có frame trên 116:3 và chưa ai sở hữu**: Balo `3:3` (ITM-01, ART-04), Bản đồ lớn `7:186` (UI-01 "mở rộng sau"), killfeed (UI-01 ghi "chưa có ticket sở hữu" ở WF cũ), cài đặt đủ tab `7:337` (UI-03 M1: chưa quyết).
- Màn mà HUD cần nhưng cả hai nơi đều chưa vẽ: Settings đầy đủ (UI-03), Theo dõi bản v2 (UI-03).

## 6. Việc đề xuất, chưa làm (chờ CPO)

| Việc | Cần | Ghi |
|---|---|---|
| Chụp lại 36 frame từ build hiện tại, thay cũ | Unity Editor (unity) | Ngoài giới hạn T-007; ghi `projects/lastaway/de-xuat/chup-lai-figma-116-3.md` |
| Cập nhật tên file và thêm `116:3` vào `Docs/UI_Wireframes.vi.md` | Quyền sửa tài liệu | Cùng đề xuất trên |
| Thống nhất cách đánh dấu frame đã lỗi thời (nhãn "Build 31/10") | Quyền ghi Figma | Hiện chỉ đọc |
