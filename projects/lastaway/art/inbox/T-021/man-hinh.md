# T-021 Mock mọi màn bằng UI kit T-020 (skin A) và HUD r11

Trạng thái: **đợt A và đợt B đã dựng lại trên kit K2 + P0 + P1 (07/11, sau T-026)**, chưa Unity, chưa qa. Bản mock cũ (nút viền olive) ở `a/_cu/` và `b/_cu/`. Cổng máy: `src/tools/check_center.py` (căn giữa nút, `a/center.md`, `b/center.md`) và đo tương phản từ pixel (`a/contrast.md`, `b/contrast.md`). Mock 1920x1080 trong `a/` và `b/`, ảnh cạnh wireframe `*-canh-wireframe.png`, sơ đồ luồng `luong.png`. Script: `src/tools/` (`mock.py`, `screens_a.py`, `screens_b.py`, `compare.py`, `luong.py`, `extract_fx.py`): chỉ đặt sprite GPT lên nền, chữ Be Vietnam Pro Bold; không vẽ tay đồ họa. Asset ngoài kit của đợt B gen bằng GPT: phản hồi HUD, huy hiệu, marker (`T-020/ui-kit.md` mục 8, file `b/assets/`, nét cũ chưa gen lại theo kaki, chờ Huy); thẻ thống kê, thẻ nguyên nhân, ô vật phẩm, thẻ chi tiết, đường chia (P1, `ui-kit.md` mục 12).

Nguồn: `Docs/UI_Wireframes.vi.md`, `Docs/Tickets/UI-02.md`, `UI-03.md`, GDD (10 người, 4 khe vũ khí + 4 khe dược, Adrenaline, Lobby 15 s không có Ready, `disconnectGrace` 15 s, `killcamDuration` 5 s mặc định tắt). GDD thắng wireframe khi lệch. Đã chốt (Huy, 07/11): **bỏ chế độ Khách, bắt đăng nhập rồi mới vào phòng**; mock Sảnh khách chuyển vào `a/_bo/`.

## Đợt A: luồng vào trận

| Màn | Mock (trạng thái) | Wireframe gốc | Ticket | Thành phần kit |
|---|---|---|---|---|
| A1 Khởi động | `a1-khoi-dong` | 13:2 (frame trống) | UI-02 | logo, `kit_progress_*`, `kit_icon_check/dot` |
| A2 Sảnh chính | `a2-sanh-chinh` (đã đăng nhập) | Figma vòng 2 `03_home`; v2 `42:88` | UI-02 | `kit_btn_primary/secondary`, `kit_toast` làm thẻ tài khoản |
| A3 Vào phòng | `a3-vao-phong`, `-loi` (phòng đầy), `-dang-ket-noi` | `06_join-modal`; v2 `42:137` | UI-02, CLI-01 | `kit_panel_popup`, `kit_input_*`, ô mã `hud_slot_*`, `kit_progress_*` |
| A3b Đăng nhập | `a3b-dang-nhap`, `-loi` | `01_login` | UI-02, NET-01 | `kit_panel_m`, `kit_tab_*`, `kit_input_*` |
| A4 Phòng của tôi | `a4-phong-cua-toi`, `-trong`, `-loi`, `a4-xoa-phong` | 8:7 (cũ) | UI-02, NET-01 | `kit_panel_l`, `kit_row_*`, `kit_btn_danger` (chỉ hộp xác nhận) |
| A5 Tạo phòng | `a5-tao-phong`, `-loi` | 8:124 (cũ) | UI-02, NET-01 | `kit_panel_popup`, `kit_slider_*`, `kit_btn_primary` |
| A6 Phòng chờ | `a6-phong-cho-cho-du-nguoi`, `-dem-nguoc` | v2 `42:182` | UI-02, SRV-03 | `kit_panel_l/s`, `kit_row_*`, `kit_tag_neutral` |
| A7 Đang tải | `a7-dang-tai`, `-loi` | v2 `42:269`; 13:3 (trống) | UI-02 | `kit_progress_*`, `kit_panel_s`, `kit_toast` |

## Đợt B: trong và sau trận

Nền "trong trận" là ảnh dựng thử HUD r11 (`T-015/dung-thu-hud-A.png`), phủ tối khi có modal. Ba màn bị loại / spectator / kết quả dùng nền sàn đá vì UI-02 vòng 2 tắt cả Canvas HUD khi Killcam hoặc Spectator.

| Màn | Mock (trạng thái) | Wireframe gốc | Ticket | Thành phần |
|---|---|---|---|---|
| B1 ESC cài đặt | `b1-esc-cai-dat`, `b1-esc-xac-nhan-thoat` | v1 `7:337` (5 tab, bỏ theo UI-03 M1) | UI-03 | `kit_panel_popup`, `kit_slider_*`, `kit_knob`, `kit_toggle_*`; Thoát trận = nút phụ + icon X + chữ `FF8A78`; hộp xác nhận Thoát / Ở lại |
| B2 Bị loại | `b2-bi-loai` (bị hạ), `-bo` (bị bo, ẩn dòng Hạ gục), `-het-nguoi` (nút Theo dõi tắt kèm lý do) | v2 `43:283` | UI-03 | `kit_panel_m/s`, chữ số lớn, icon X |
| B3 Spectator | `b3-spectator`, `-chuyen` (toast "X đã bị loại"), `-het` (trận kết thúc) | v1 `7:304` | UI-03 | `kit_panel_s`, `kit_row_*`, `kit_progress_*`, phím A D E |
| B4 Túi đồ | `b4-tui-do`, `-trong` (không có đồ gần) | `3:3` (kiểu PUBG, bỏ phụ kiện súng) | ITM-01, ART-04 | `kit_panel_l/s`, `kit_row_*`, `hud_slot_*`, `kit_toast` |
| B5 Bản đồ lớn (M) | `b5-ban-do-lon` | `7:186` | UI-01, mở rộng | `kit_panel_l/s`, `fx_map_*`, nền bản đồ giữ chỗ |
| B6 Thắng | `b6-thang` | v2 `43:445` | UI-02 | `fx_emblem_win`, `kit_panel_m/s`, `kit_row_*`, tiêu đề vàng |
| B7 Thua | `b7-thua` | v1 `3:5`; v2 `43:445` | UI-02 | `fx_emblem_lose`, tiêu đề "HẠNG #n" màu chính, nguyên nhân |
| B8 Mất kết nối | `b8-mat-ket-noi` (đếm 15 s), `-that-bai`, `-het-han` (đã bị loại, chỉ Về sảnh) | v2 `43:131` | UI-02 | `kit_panel_m`, `fx_icon_disconnect`, `kit_progress_*` |
| B9 Phản hồi HUD | `b9-phan-hoi-hud`, `-mau-thap` (viền máu) | v2 `48:461` | UI-01 phần 5.1 | `fx_hitmarker`, `fx_damage_arc`, `fx_danger_arrow`, `fx_chip_*`, `fx_vignette`, `kit_toast`, `kit_row_alt`, `kit_tag_neutral` |

Vị trí phản hồi HUD (1920x1080, = tọa độ Figma 1280x720 nhân 1,5): cảnh báo bom/bẫy tâm trên (735,36) 452x60; hiệu ứng đang chịu (24,700) tối đa 6 chip 116 px; "Đang dùng" thay dải vũ khí (570,858) 780x48; nhặt đồ (1672,800) rộng 210, tối đa 3 dòng + "+n món khác"; gợi ý "F Nhặt" giữa phải; hit marker quanh tâm ngắm; cung nhận sát thương trên vòng quanh nhân vật; mũi tên nguy hiểm ngoài màn hình kèm số giây.

Quy tắc màu (T-020 bản sửa): nút chính vàng; đỏ đầy chỉ ở hộp xác nhận Xóa phòng; thoát trận và rời phòng là nút phụ + icon X + chữ đỏ nhạt; xanh lá và hổ phách chỉ ở icon; toast và tag nền trung tính; đỏ san hô chỉ ở cung sát thương, mũi tên nguy hiểm, vignette (kèm chữ hoặc số). Chữ trên lòng tối >= 10:1 (TXT, MUTED, GOLD), trên dòng chọn >= 6,3:1, chữ đỏ nhạt 6,41 (lòng) và 4,65 (nút phụ), chữ trực tiếp trên nền sàn tối đo thấp nhất 4,88 (có viền chữ 2 px). Đo bằng tay trên mock B (vài điểm), chưa chạy script đo toàn bộ.

## Ghi chú trạng thái từng màn

| Màn | Trạng thái | Hành vi |
|---|---|---|
| A1 | đang kết nối | có JWT còn hạn thì vào thẳng Sảnh, không thì Đăng nhập |
| A3 | nhập dở, lỗi, kết nối | lỗi theo lý do join (`RoomFull`, `InvalidRoomCode`, `RoomNotFound`, `ServerFull`, `MatchInProgress`) |
| A4 | có dữ liệu, rỗng, lỗi API | chỉ xóa được phòng của mình khi trống |
| A6 | chờ đủ người, đếm ngược | đếm theo server (`PhaseEndTick`); nút Sẵn sàng là luật chưa duyệt |
| A7 | tải, lỗi | tối đa 30 s; lỗi có Về sảnh |
| B1 | mở, xác nhận thoát | không dừng trận, ESC đóng một lớp một lần; giá trị áp ngay, không có nút Lưu |
| B2 | bị hạ, bị bo, hết người | dòng thiếu dữ liệu thì ẩn (không hiện 0); Về sảnh không xác nhận; Theo dõi tắt khi không còn ai |
| B3 | bình thường, đổi mục tiêu, hết người | tự chuyển khi mục tiêu bị loại; danh sách chỉ người còn sống, tối đa 9; hết người thì trao Kết quả |
| B4 | có đồ, trống | ô trống ghi "Trống"; giáp, mũ, đạn tự nhặt |
| B5 | mở | M đóng; nền và vòng bo vẽ lúc chạy |
| B6, B7 | kết quả | đồng hạng hiện đúng số hạng máy chủ gửi; Chơi lại về Phòng chờ, Về sảnh về Sảnh |
| B8 | kết nối lại, thất bại, hết hạn | `disconnectGrace` 15 s; hết hạn thì bị loại, luôn có Về sảnh |
| B9 | bình thường, máu thấp | vignette vẽ dưới lớp HUD |

## Lệch wireframe, đã xử lý theo GDD hoặc ticket

| Wireframe | Mock |
|---|---|
| B8 v2 ghi "Rời trận sau 18s" | 15 s (`disconnectGrace`) |
| B2, B6 có "Sát thương", "Đồ đã nhặt", "Hào chỉ còn 36 máu" | bỏ, GDD và protocol không có (UI-03 Q10) |
| B1 v1 có 5 tab, phím tắt, Lưu | một panel, áp ngay (UI-03 M1, Q8, Q14) |
| B3 v1 có "đội", Báo cáo, Khóa Q | "Còn sống n/N", bỏ Báo cáo và Khóa (UI-03 M3, M4) |
| B4 phụ kiện súng, Nước tăng lực, Bom khói | 4 khe vũ khí + 4 khe dược, trang bị tự nhặt |
| B5 lưới chữ-số A-H, "đội" | bỏ lưới tọa độ, "điểm đánh dấu của bạn" |

## Câu hỏi còn mở

| # | Câu hỏi | Mặc định đã dùng |
|---|---|---|
| Q1 | Guest | **Đã chốt (Huy, 07/11): bỏ Khách, bắt đăng nhập** |
| Q2 | "Tìm phòng" không có backend danh sách công khai | Màn "Phòng của tôi" |
| Q3 | Nút Sẵn sàng ở Phòng chờ | Có hiện, ghi tùy chọn, tắt mặc định |
| Q4 | Tạo phòng: bỏ chọn bản đồ, chế độ, luật, mật khẩu | Bỏ hết |
| Q5 | Avatar người chơi | Vòng tròn rỗng giữ chỗ |
| Q6 | Logo | Dùng `Logo.png` hiện có |
| Q7 | Wireframe 13:2, 13:3 v1 là frame trống | Theo v2 và UI-02 |
| Q8 | Timeout Loading 30 s, mẹo | Từ Network_Architecture và Figma v2 |
| Q9 | Túi đồ: thao tác vứt (kéo ra ngoài) và dung lượng không có trong GDD ITM-01 | Mock ghi "đề xuất, chưa có trong GDD", không có thanh dung lượng |
| Q10 | Icon vật phẩm trong túi đồ và Quick bar | Ô để trống đến khi ART-04 có icon |

## Dựng lại đợt B trên kit mới (07/11)

| Mục | Kết quả |
|---|---|
| Mock | 18 mock `b/*.png`, nút K2 icon + chữ tối, nút đóng X đỏ #94281F, chân hộp thoại, header, thẻ thống kê, thẻ nguyên nhân bị loại, ô vật phẩm lớn (thường, chọn, trống), thẻ chi tiết vật phẩm (dữ liệu thật từ ITEM LIST: loại và nguồn rơi, không bịa chỉ số), đường chia mục, keycap |
| Cổng căn giữa | `check_center.py b`: 267 ca (252 họ nút + 15 nút trong mock B), 0 FAIL, lệch tối đa 0,5 px; `a`: 278 ca, 0 FAIL |
| Tương phản chữ | `b/contrast.md`: 366 dòng chữ, 0 dòng dưới 4,5:1 (đo từ pixel nền, chưa trừ viền chữ 2 px). Lần đầu 4 dòng lỗi ở B9 (chữ "8s" đỏ và "Nhặt Shotgun" trên nền sàn sáng), đã đặt lên nhãn/toast |
| Chưa dựng lại | phản hồi HUD, huy hiệu Thắng/Thua, marker bản đồ vẫn là sprite GPT cũ ở `b/assets/` (nét mịn kiểu cũ, hơi khác kaki); cung sát thương và vignette dùng đỏ: **ngoại lệ luật màu, chờ Huy duyệt** |

Câu hỏi mở đợt B, áp mặc định: túi đồ có thao tác vứt (ghi "đề xuất", chưa có trong GDD); ô vật phẩm trống chờ icon ART-04; nền bản đồ lớn là giữ chỗ (vẽ lúc chạy).

## Luật chữ trong màn gameplay (Huy 07/11) - áp lên đợt B

Chỉ giữ chữ cho tên người chơi, con số, phím trên keycap; nhãn, tiêu đề, toast, ô trống chuyển sang icon. Tên vật phẩm chỉ ở tooltip (mock mới `b/b4-tui-do-tooltip.png`). B1, B2, B6, B7, B8 giữ nút icon + chữ, chữ rút ngắn.

Số dòng chữ (đếm từ `b/contrast.json`, gồm cả tên người chơi và số):

| Mock | Trước | Sau |
|---|---:|---:|
| B3 spectator | 20 | 17 (9 là tên trong danh sách) |
| B4 túi đồ | 50 | 19 |
| B5 bản đồ lớn | 18 | 4 |
| B9 phản hồi HUD | 21 | 15 (mọi dòng còn lại là số hoặc phím) |

Nguồn icon vật phẩm (có sẵn trong `Assets/_JungleMaze/Art/`, không gen): Shotgun, Nỏ, Nỏ cao su, Tay không = `UI/Icons/Weapons/{SHOTGUN,CROSSBOW,RUBBER_GUN,HAND}`; Lá thần, Máu kỳ lân, Tăng lực, Mũ gỗ, Giáp, Adrenaline = `UI/Icons/Equipments/{GOD_LEAF,UNICORN_BLOOD,ELIXIR,WOODEN_HELMET,ARMOR,ADRENALINE}`; hiệu ứng độc, chậm, tàng hình = `UI/Icons/Effects/{Poison,Slow,Invisible}`; hạ gục, máu, còn sống, bo thu = `Hud/StatusIcons/icon_{kill,hp,alive,zone-timer}`; chuột = `UI/MatchOverlay/IconMouse`.

Thiếu icon (ô giữ chổ có viền trong mock): đạn (ô "Đạn" ở cột Trang bị B4). "Giáp gai" tạm dùng `ARMOR` vì không có icon riêng. Cần xác nhận icon điều khiển bản đồ B5 (chuột, cộng, mũi tên) với Huy.

Bỏ khỏi mock vì thành chữ: ghi chú "Kéo vật ra ngoài để vứt (đề xuất)", "F hoặc chuột phải nhặt nhanh", tiếng bước chân (toast B4, thuộc HUD).

## Icon vật phẩm to hơn, toast nhặt có tên (Huy 07/11, chờ duyệt cỡ)

Ngoại lệ chữ: toast nhặt đồ gồm icon + tên + số lượng. Các luật hạn chế chữ khác giữ nguyên. Ảnh so sánh: `b/so-sanh-icon.png` (trên = cũ, dưới = mới).

| Vị trí (1080p, cạnh icon) | Cũ | Đề xuất mới |
|---|---:|---:|
| Hàng "dưới đất" B4 | 40 px | 56 px (hàng cao 66) |
| Ô vũ khí, dược phẩm B4 (ô 130x140) | 76 px | 104 px |
| Trang bị B4 | 56 px | 72 px |
| Toast nhặt (kèm tên) B9 | 50 px | 72 px |
| Danh sách nhặt (kèm tên, số lượng) B9 | 40 px | 56 px (hàng cao 68) |
| Toast đang dùng vật phẩm B9 | 36 px | 52 px |

Quick bar: nằm trong ảnh HUD gốc (`T-015/dung-thu-hud-A.png`), mock không vẽ icon vật phẩm lên đó. Đề xuất icon trong ô quick bar chiếm khoảng 80% cạnh ô (ô hiện khoảng 56 px thì icon khoảng 44 px), cần Dev/art-ux sửa Hud.prefab khi duyệt. Icon nguồn 64x64 nên 104 px là phóng 1,6 lần (LANCZOS); nếu muốn nét hơn cần ART-04 cấp bản lớn.

## Dựng lại trên thang chữ và font mới (T-027, 07/11)

Toàn bộ 17 mock A và 19 mock B dựng lại bằng `mock.py` đã nối `thang.py`: 5 bậc 96/40/30/24/20, Chakra Petch Bold cho 96 và 40 (không phải nút; Huy chốt 07/11 thay Denk One, bản Denk One ở `a/_cu/denk-one/`, `b/_cu/denk-one/`), Be Vietnam Pro Bold cho phần còn lại và mọi chữ nút (S 24, M 30, L 40). Bản trước thang nằm ở `a/_cu/truoc-T027/`, `b/_cu/truoc-T027/`. Ghi chú mock ("Minh họa: ...") không đo, không tính vào cổng.

| Cổng | A | B |
|---|---|---|
| Cỡ chữ (`do_co_chu.py --gate`) | 303 dòng, 0 lỗi | 287 dòng, 0 lỗi |
| Tương phản 4,5:1 | 0 dòng dưới | 0 dòng dưới |
| Căn giữa nút | 278 ca, 0 FAIL, max 0,5 px | 266 ca, 0 FAIL, max 0,5 px |
| Chữ nút đúng bậc | 85/85 | 23/23 |

Đã sửa vì chữ to hơn: nút Đăng xuất (x1688, 180x48) ở chip tài khoản; bố cục màn Thắng và Thua (tiêu đề 96 px, thẻ nguyên nhân và thẻ thống kê dịch xuống).

### Sửa 2 lỗi sau duyệt CPO (T-027, 07/11)

- **Ảnh nền HUD dùng chung (B1 đến B9):** `screens_b.hudbg()` xóa chữ in sẵn của `T-015/dung-thu-hud-A.png` (kill feed, 100, giáp, mũ, adrenaline, 1/1, 1:21, số phím quick bar) rồi vẽ lại bằng text, nên cổng cỡ chữ đo được. Kill feed: tên + icon vũ khí + tên, 24 px, panel nới theo chữ. Số HUD 30 px (máu, giáp, mũ, adrenaline) và 24 px (1/1, 1:21). Số phím quick bar 20 px trên nhãn tròn (ngoại lệ số phím).
- **B4 cột phải:** dòng "4/90" (đạn của Nỏ) dùng ô giữ chỗ viền kit `kit_slot_item_empty` thay ô trắng rỗng. **Thiếu icon: đạn** (chờ ART-04). Icon tiêu đề cột trái đổi từ bản đồ sang `kit_ico_list` (đồ ở gần); **thiếu icon riêng "đồ ở gần / dưới đất"** (chờ ART-04).

| Cổng sau sửa | A | B |
|---|---|---|
| Cỡ chữ (`do_co_chu.py --gate`) | 303 dòng, 0 lỗi | 567 dòng (gồm chữ HUD nền), 0 lỗi |
| Tương phản 4,5:1 | 0 dòng dưới | 0 dòng dưới |
| Căn giữa nút | 278 ca, 0 FAIL, max 0,5 px | 266 ca, 0 FAIL, max 0,5 px |
| Chữ nút đúng bậc | 85/85 | 23/23 |

### Đổi font chính sang Chakra Petch Bold (T-027 FONT 3, 07/11)

Bậc 96 và 40 đã render lại bằng Chakra Petch Bold (rộng hơn Denk One). Kiểm tràn trên 24 mock có chữ font chính: rộng nhất CHIẾN THẮNG 622 px (cột trái 960 px), HẠNG #4 411 px, "Chưa kết nối lại được" 406 px trong hộp 760 px, "Phòng của tôi" 261 px; không có chỗ tràn khung hay tràn nút (nút L dùng Be Vietnam Pro nên không đổi).

| Cổng | A | B |
|---|---|---|
| Cỡ chữ (`do_co_chu.py --gate`, gồm font đúng tầng) | 303 dòng, 0 lỗi | 567 dòng, 0 lỗi |
| Tương phản 4,5:1 | 0 dòng dưới | 0 dòng dưới |
| Căn giữa nút 1 px | 278 ca, 0 FAIL, max 0,5 px | 266 ca, 0 FAIL, max 0,5 px |
| Chữ nút đúng bậc | 85/85 | 23/23 |
