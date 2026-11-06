# UI kit: thành phần cấu tạo, phân loại theo ngữ cảnh, thứ bậc nút (T-026, 07/11)

Trạng thái: đề xuất, chờ Huy duyệt danh sách thiếu. Chưa gen asset mới. Ảnh nhìn nhanh: `art/inbox/T-026/thieu-gi.png`. Nguồn dữ liệu bảng đối chiếu: `art/inbox/T-026/src/thieu.py`. Lý do làm: Huy thấy kit T-020 "mỗi cái khung", thiếu thẻ và thiếu phân loại nút (nút trong hộp thoại khác nút ngoài màn). Kit T-020 hiện là bộ sprite khung và nút, chưa phải hệ thành phần theo ngữ cảnh.

## 1. Nghiên cứu (trang mở được trong phiên này, 07/11)

| # | Nguồn | Nhận định rút ra (diễn đạt lại) | Dùng cho |
|---|---|---|---|
| 1 | Style guide UI của một game có menu, panel, hộp thoại, https://mfgoes.github.io/redshiftlabs/moon/UI_STYLE_GUIDE.html | Có hẳn bảng thứ bậc nút: chính (một nút, nổi bằng màu chứ không bằng cỡ), phụ (Lưu, Cài đặt, Quay lại), phá hủy (đặt dưới đường chia, không trộn với nút khác), chính trong panel (một hành động, full width), ghost cho việc ít quan trọng, nút icon không nền cho thao tác từng dòng. Menu trong game chia nhóm thành phần theo ngữ cảnh và cấm dùng kiểu nút của ngữ cảnh này ở ngữ cảnh khác (nút to chỉ ở menu chính, không vào panel chi tiết). Phân biệt hộp thoại (modal, che game) với panel gắn thanh dưới (không modal, game vẫn chạy). Có ba loại khung: panel hộp thoại, panel trong game, thẻ nhỏ chèn (inset) với bán kính riêng | luật thứ bậc nút, biến thể theo ngữ cảnh, thẻ nhỏ viền mảnh |
| 2 | Carbon, button usage, https://carbondesignsystem.com/components/button/usage/ | Năm loại nút: chính, phụ, thứ ba (tertiary), ghost, danger. Mỗi trang chỉ một nút chính; hộp thoại không có hai nút nhấn mạnh cao; không trộn cỡ nút trong một nhóm; nút icon chỉ khi icon quen thuộc và phải có tooltip; danger không được ở dạng chỉ-icon, phải có nhãn chữ; ghost hợp làm Hủy trong hộp thoại; nút trong hộp thoại có thể tràn sát mép hộp (cỡ XL) | số nút chính, danger luôn có chữ, nút icon có tooltip, không trộn cỡ |
| 3 | Kiến trúc UI cho game Unity, https://www.gamedeveloper.com/programming/a-ui-system-architecture-and-workflow-for-unity | Chia Screen thành Panel (cùng tồn tại với UI khác, không có lịch sử: HUD, thanh trạng thái) và Dialog (tiêu điểm chính, có lịch sử, chỉ cái trên cùng tương tác được); Widget là mảnh dùng lại trong nhiều Screen (ví dụ một dòng thành tích). Toast không được nhắc | bốn ngữ cảnh: màn toàn phần, hộp thoại, HUD, nổi; "thẻ" là Widget |
| 4 | Roblox UI/UX design, https://create.roblox.com/docs/production/game-design/ui-ux-design | Nút đóng hộp thoại là dấu X đặt một chỗ duy nhất, góc trên phải, nhất quán; chỉ hiện nút liên quan ngữ cảnh; nút quan trọng to hoặc sáng, nút ít quan trọng nhạt; quyết định hình thức nào dùng chỗ nào phải nhất quán (chỉ số đổi màu giống nhau ở chữ, icon, thanh; nút không dùng được luôn xám); nhóm gần nhau thì ngụ ý liên quan | nút đóng, nhất quán trạng thái, nhóm theo gần |
| 5 | GameAnalytics, UI hierarchy, https://www.gameanalytics.com/blog/how-to-make-your-game-ui-shine-and-increase-conversions | Chỉ một phần tử to hơn các phần tử quanh nó; phần tử cùng cỡ và thẳng hàng đọc như ngang quyền; hành động chính một màu riêng, đỏ chỉ để hủy hoặc bỏ; để khoảng trống quanh nút chính, không đặt nút cạnh tranh gần nó; bo góc, bảng màu, vát cạnh, đổ bóng phải giống nhau ở mọi thông báo và nút | thứ bậc theo màu và cỡ, khoảng trống quanh nút chính |
| 6 | 6 ví dụ UI game đáng học, https://www.gamedeveloper.com/audio/6-examples-of-ui-design-that-every-game-developer-should-study | Clash Royale: điều khiển đặt ngay màn chính, tab vuốt để điều hướng nhanh. Half-Life 2: HUD tối thiểu, phần tử chỉ hiện khi cần. Hearthstone: trạng thái đọc rõ, mỗi thao tác có phản hồi. Overwatch: thông tin đặt ở chỗ mắt đang nhìn (gần tâm ngắm). Dead Space: UI nằm trong thế giới | HUD tối giản, phản hồi cho mọi thao tác |
| 7 | So sánh UI battle royale (Fortnite, Apex, Warzone, PUBG), https://caidenk.itch.io/a1-skills-development-caiden/devlog/816648/user-interface-in-games | Inventory battle royale mở bằng một lần bấm phím, vật phẩm hiện bằng ảnh để nhận ra nhanh, không phủ kín màn và thường trong mờ để còn thấy người khác. Bài chỉ nói ở mức thể loại, không tả HUD hay lời nhắc nhặt đồ cụ thể từng game | túi đồ bán trong suốt, không che hết màn |
| 8 | Hệ inventory của một engine, https://docs.punyhuman.com/arcinventory/1.0/concepts/ui/ | Thành phần inventory gồm Item Slot (ô), Item Card nhỏ (ảnh khoảng 32x32 trong ô hoặc khi kéo) và Item Card lớn (chi tiết khi rê chuột); kéo thả là hành vi của ô, không phải widget riêng. Tooltip và context menu không có trong hệ này | thẻ vật phẩm nhỏ và lớn |

Không mở được nội dung: Game UI Database (https://www.gameuidatabase.com/, lỗi 403), AllOut game UI kit (https://docs.allout.game/ui/uidoc-game-ui-kit.md, 404), bài Medium về HUD và inventory (403). Chưa tìm được nguồn mở được mô tả riêng "thẻ người chơi" của game battle royale; thẻ người chơi trong đề xuất dưới đây suy từ PlayerSlot trong Figma v2 và wireframe, không từ nguồn ngoài. Nguồn 7 và 6 là phân tích UI game cụ thể nhưng ở mức tổng quát; chưa có teardown từng khung hình một game battle royale mở được.

## 2. Phân loại thành phần theo ngữ cảnh

Bốn ngữ cảnh (nguồn 3): **màn toàn phần** (Panel, không modal), **hộp thoại** (Dialog, modal), **HUD trong trận** (Panel, không che game), **nổi** (toast, tooltip, context menu, không chiếm tiêu điểm). Mỗi loại thành phần có biến thể theo ngữ cảnh, không dùng chung một sprite cho cả bốn.

| Loại | Màn toàn phần | Hộp thoại | HUD trong trận | Nổi |
|---|---|---|---|---|
| Nút | nút menu L (420 rộng), nút hành động M, nút quay lại ở header | nút M ở chân hộp thoại (phụ trái, chính phải), nút đóng X ở header | không có nút bấm (HUD không tương tác chuột) | không |
| Thẻ | thẻ phòng, ô người chơi, thẻ thống kê, thẻ kết quả, ô vật phẩm lớn | thẻ nhỏ viền mảnh trong thân hộp thoại (thống kê, dòng thông tin) | ô slot Quick bar, thẻ mục tiêu spectator | thẻ chi tiết vật phẩm khi rê chuột |
| Khung | panel M, L; header màn | popup có thanh tiêu đề + chân | panel HUD r11 (Vitals, Quick bar, Minimap, Match) | toast, tooltip |
| Thanh | tiến trình tải, scrollbar | slider, tiến trình kết nối | HP, giáp, Adrenaline, thanh dùng đồ | không |
| Ô nhập | ô nhập, dropdown, ô mã phòng | ô nhập, ô mã phòng, toggle, checkbox | không | không |
| Chỉ báo | tag, badge, chấm mạng | icon tích, X, cảnh báo ở dòng lỗi | chip hiệu ứng, hit marker, cung sát thương, vignette, cảnh báo bom | toast có icon |
| Điều hướng | tab, header + quay lại, đường chia mục | đường chia trước chân hộp thoại | không | không |
| Dòng | dòng danh sách, dòng xếp hạng | dòng chọn trong dropdown | dòng killfeed, dòng nhặt đồ | không |

## 3. Luật thứ bậc nút (đề xuất; cỡ nút là tham số nhìn thấy nên Huy duyệt)

| Cấp | Dùng cho | Hình thức | Số lượng | Có trong kit |
|---|---|---|---|---|
| 1 Chính | hành động tích cực chính (Vào phòng, Tạo, Lưu, Tiếp tục, Chơi lại) | vàng kim, chữ trắng viền tối | một nút mỗi màn hoặc mỗi hộp thoại (nguồn 1, 2) | có (`kit_btn_primary_*`) |
| 2 Phụ | hành động phụ, bổ trợ (Hủy, Về sảnh, Mặc định, Sao chép mã) | nền đá tối, rim olive | nhiều, cạnh nút chính | có (`kit_btn_secondary_*`) |
| 3 Thứ ba | hành động ít quan trọng đứng cạnh phụ (Chi tiết, Xem luật) | viền mảnh, nền trong | nhiều | thiếu (T09) |
| 4 Chỉ chữ | liên kết, hành động rất nhẹ (Quên mật khẩu, Thử lại trong dòng lỗi) | chữ vàng nhạt, gạch chân khi hover, không nền | nhiều | thiếu sprite, chỉ cần spec (T09) |
| 5 Icon | thao tác quen thuộc không cần nhãn (cài đặt, sao chép, làm mới) | nút vuông hoặc tròn có icon; luôn có tooltip (nguồn 2) | nhiều | có khung (`kit_iconbtn_*`), thiếu bộ icon (T04) |
| 6 Đóng | đóng hộp thoại | vuông xám đá + X, góc trên phải, một chỗ duy nhất (nguồn 4), không đỏ theo luật màu ngữ nghĩa | một mỗi hộp thoại | thiếu (T01) |
| 7 Quay lại | về màn trước trong màn toàn phần | nút icon mũi tên trái ở header, góc trên trái | một mỗi màn có màn trước | thiếu (T03, T04) |
| 8 Phá hủy | không hoàn tác (Xóa phòng) | đỏ đầy, chỉ trong hộp xác nhận, luôn kèm chữ, không bao giờ chỉ icon (nguồn 2) | một mỗi hộp xác nhận | có (`kit_btn_danger_*`) |

Quy ước kèm theo:

| Mục | Luật |
|---|---|
| Cỡ | S cao 48 (trong thẻ, dòng, chân hộp thoại hẹp), M cao 64 (hành động trong màn và hộp thoại), L cao 80 (nút menu chính ở Sảnh, rộng 420). Không trộn cỡ trong một nhóm nút (nguồn 2). Nút icon: 48 và 64. Kit hiện chỉ có nguồn một cỡ 240x64 co dãn 9-slice; cần kiểm S và L không vỡ viền (việc của art-ux, không gen) |
| Nút kèm icon | icon bên trái chữ (khác Carbon đặt phải, vì Thoát trận và Rời phòng cần icon X dẫn đầu theo luật màu); icon cao bằng chữ, cách chữ 10 px; Thoát trận, Rời phòng = nút phụ + icon X + chữ `FF8A78` |
| Vị trí trong hộp thoại | nút đóng X góc trên phải của header; chân hộp thoại: đường chia, rồi hàng nút; hai nút thì chia đôi hàng, phụ trái và chính phải; một nút thì căn giữa rộng 320 (khác Carbon căn phải; game dùng căn giữa cho cân với panel đá); hộp xác nhận phá hủy: Hủy (phụ) trái, Xóa (phá hủy) phải, nút phá hủy không phải nút mặc định focus |
| Vị trí trong màn toàn phần | nút quay lại ở header trái; một nút chính ở dưới phải hoặc giữa khối nội dung; nút phụ cùng hàng, bên trái nút chính |
| Trạng thái | thường, hover, nhấn, tắt (đã có 4); tắt chỉ xám, nơi người chơi cần biết lý do thì ghi chữ lý do cạnh nút (đã áp ở B2 "Theo dõi người khác"). Mọi nút cùng cấp giữ cùng cỡ chữ |
| Khoảng trống | chừa quanh nút chính, không đặt nút cạnh tranh sát nó (nguồn 5) |

## 4. Thẻ (chỉ những thẻ màn thật cần, đối chiếu wireframe và GDD)

| Thẻ | Nội dung (theo GDD và wireframe) | Màn | Trạng thái | Ghi chú |
|---|---|---|---|---|
| Thẻ phòng | tên phòng, mã 6 ký tự (không hiện cho phòng mặc định), số người n/10, nhãn Mặc định hoặc Của bạn, nút Vào, Sao chép mã, Xóa | A4 | thường, được chọn, đầy (nút Vào tắt) | thay dòng danh sách phẳng; không có luật chơi, bản đồ, ping vì backend không trả (Q2 đợt A) |
| Ô người chơi (PlayerSlot) | khung avatar giữ chỗ, tên, nhãn chủ phòng | A6, B3 | rỗng, chưa sẵn sàng, sẵn sàng | Figma v2 có 3 trạng thái; Sẵn sàng là luật chưa duyệt (Q3 đợt A); avatar chưa có art (Q5) |
| Dòng hạng | hạng, tên, số hạ gục, đánh dấu bản thân | B6, B7 | thường, bản thân | dựng được từ `kit_row_*` (đã có); đồng hạng hiện đúng số hạng server gửi |
| Thẻ thống kê | nhãn, số lớn (Hạ gục, Sống sót) | B2, B6, B7 | có, ẩn khi thiếu dữ liệu | GDD và protocol chỉ có hạng, hạ gục, thời gian sống; bỏ Sát thương, Đồ đã nhặt (UI-03 Q10) |
| Thẻ nguyên nhân bị loại | một dòng: bị ai hạ, vũ khí, khoảng cách; hoặc bị bo; hoặc mất kết nối | B2 | 3 loại nguyên nhân + ẩn dòng thiếu | |
| Ô vật phẩm | ảnh vật phẩm, số lượng, phím tắt 1 đến 8 | HUD Quick bar, B4 | thường, chọn, rỗng, đang kéo | 4 khe vũ khí + 4 khe dược; icon vật phẩm chờ ART-04 |
| Thẻ chi tiết vật phẩm | tên, loại, chỉ số theo sheet Weapons hoặc Equipments | B4 (rê chuột) | nổi | **GDD không có độ hiếm**: ITEM LIST chỉ có loại Weapon (18), Armor (6), Consumable (11), nguồn nhận, nguồn tiêu. Không làm thẻ theo độ hiếm; nếu sau này GDD thêm thì thêm biến thể màu viền |
| Thẻ thống kê trong hộp thoại | như thẻ thống kê nhưng viền mảnh 2 px | B8 | | cần viền mảnh vì panel S viền 47 px quá dày cho khối nhỏ |
| Thẻ tài khoản | tên tài khoản, nút Đăng xuất | A2 | | đang dùng toast; P2 |

## 5. Đối chiếu: kit T-020 và HUD T-015 với màn T-021

Ưu tiên: **P0** = chặn dựng đợt A (luồng vào trận) và hộp thoại; **P1** = đợt B và hoàn thiện; **P2** = sau, hoặc GDD chưa cần.

| | Số thành phần |
|---|---:|
| Đã có trong kit T-020 và HUD T-015 | 19 |
| Thiếu P0 (chặn dựng đợt A và hộp thoại) | 8 |
| Thiếu P1 (đợt B và hoàn thiện) | 10 |
| Thiếu P2 (sau, hoặc chưa cần theo GDD) | 4 |
| Tổng thiếu | 22 |

| Mã | Nhóm | Thành phần | Kit T-020 đã có | Thiếu | Ngữ cảnh | Màn T-021 cần | Ưu tiên |
|---|---|---|---|---|---|---|---|
| C01 | Nút | Nút chính (4 trạng thái) | có | 240x64, 9-slice | | mọi màn | |
| C02 | Nút | Nút phụ (4 trạng thái) | có | - | | mọi màn | |
| C03 | Nút | Nút phá hủy (4 trạng thái) | có | chỉ trong hộp xác nhận | | A4 xác nhận xóa | |
| C04 | Nút | Nút icon tròn và vuông | có | 64 px, chưa có icon bên trong | | A6, B1 | |
| C05 | Điều hướng | Tab (thường, hover, chọn) | có | - | | A3b | |
| C06 | Ô | Ô nhập (thường, focus, lỗi, thành công) | có | - | | A3, A3b, A5 | |
| C07 | Ô | Dropdown (đóng, mở) | có | - | | A5, B1 | |
| C08 | Ô | Toggle và checkbox | có | - | | B1 | |
| C09 | Thanh | Slider và knob | có | - | | A5, B1 | |
| C10 | Thanh | Thanh tiến trình | có | - | | A1, A3, A7, B8, B9 | |
| C11 | Thanh | Scrollbar | có | - | | A4, B4 | |
| C12 | Dòng | Dòng danh sách (thường, xen kẽ, chọn) | có | - | | A4, A6, B3, B6 | |
| C13 | Khung | Panel S, M, L | có | viền 47 px | | mọi màn | |
| C14 | Hộp thoại | Popup có thanh tiêu đề | có | chưa có nút đóng, chưa có chân | | A3, A5, B1 | |
| C15 | Nổi | Toast và tooltip | có | - | | A2, A7, B3, B9 | |
| C16 | Chỉ báo | Tag trung tính và badge | có | - | | A4, A6, B9 | |
| C17 | Chỉ báo | Icon trạng thái (tích, X, cảnh báo, chấm) | có | - | | mọi màn | |
| C18 | Khung | Lớp tối nền modal | có | - | | mọi modal | |
| C19 | HUD | Vitals, Quick bar, Minimap, Match, Killfeed, ô slot | có | T-015 r11 | | HUD, B9 | |
| T01 | Nút | Nút đóng (X) góc hộp thoại | không | vuông xám đá + X, không đỏ; một vị trí duy nhất góc trên phải | hộp thoại | A3, A3b, A4, A5, B1, B8 | P0 |
| T02 | Hộp thoại | Chân hộp thoại (đường chia + vùng nút) | không | nút phụ trái, chính phải; hộp xác nhận cỡ nhỏ dựng từ cùng khung | hộp thoại | A3, A4, A5, B1, B8 | P0 |
| T03 | Điều hướng | Header màn toàn phần (tiêu đề + quay lại) | không | kèm nút quay lại (mũi tên trái) | màn toàn phần | A3b, A4, B4, B5 | P0 |
| T04 | Nút | Bộ icon chức năng (khoảng 16) | không | cài đặt, bản đồ, túi, sao chép, làm mới, người, đồng hồ, wifi, âm thanh, nhạc, rung, mắt, sọ, mũi tên trái/phải, đóng | mọi ngữ cảnh | A2, A4, B1, B3, B8 | P0 |
| T05 | Thẻ | Thẻ phòng (tên, mã, 6/10, trạng thái, nút Vào) | không | thay dòng danh sách phẳng | màn toàn phần | A4 | P0 |
| T06 | Thẻ | Ô người chơi (rỗng, chưa sẵn sàng, sẵn sàng) kèm khung avatar | không | PlayerSlot của Figma v2; avatar vẫn giữ chỗ (Q5) | màn toàn phần | A6, B3, B6, B7 | P0 |
| T07 | Ô | Ô mã phòng 6 ký tự (rỗng, đang nhập, đầy, lỗi) | không | đang mượn hud_slot của T-015 | hộp thoại | A3 | P0 |
| T08 | Nút | Keycap (phím) và nhãn phím | không | đang mượn iconbtn vuông kéo dãn | mọi ngữ cảnh | A7, B3, B5, B9 | P0 |
| T09 | Nút | Nút thứ ba (viền mảnh, nền trong) và nút chỉ chữ | không | nút chỉ chữ có thể không cần sprite, chỉ spec màu và gạch chân | mọi ngữ cảnh | A2, A4, B1 | P1 |
| T10 | Thẻ | Thẻ thống kê nhỏ (nhãn + số lớn) | không | panel S viền 47 px quá dày cho khối nhỏ | màn toàn phần | B2, B6, B7 | P1 |
| T11 | Thẻ | Thẻ nguyên nhân bị loại / kết quả (tiêu đề + dòng) | không | - | màn toàn phần | B2, B7 | P1 |
| T12 | Thẻ | Ô vật phẩm lớn (túi đồ): thường, chọn, rỗng, đang kéo | không | icon vật phẩm chờ ART-04; GDD không có độ hiếm, loại vật phẩm = Weapon, Armor, Consumable | màn toàn phần | B4 | P1 |
| T13 | Nổi | Thẻ chi tiết vật phẩm khi rê chuột (biến thể lớn của tooltip) | không | tooltip nhỏ đã có | nổi | B4 | P1 |
| T14 | Điều hướng | Đường chia mục và tiêu đề mục | không | - | hộp thoại, màn toàn phần | B1, B4, B5 | P1 |
| T15 | Chỉ báo | Phản hồi HUD: hit marker, cung sát thương, mũi tên nguy hiểm, chip hiệu ứng, vignette | không | đã gen thử ở T-021 (b/assets), chưa duyệt, chưa vào kit | HUD | B9 | P1 |
| T16 | Chỉ báo | Huy hiệu Thắng, Thua và marker bản đồ | không | đã gen thử ở T-021, chưa duyệt | màn toàn phần | B5, B6, B7 | P1 |
| T17 | HUD | Dải cảnh báo bom/bẫy, dòng nhặt đồ, nhắc phím F, thanh đang dùng đồ | không | đang dựng từ toast, row, progress: có thể chỉ cần spec, không sprite mới | HUD | B9 | P1 |
| T18 | Khung | Nền màn menu riêng và logo theo skin đá | không | đang dùng gạch sàn của ảnh dựng thử HUD và Logo.png pixel kem (Q6) | màn toàn phần | A1 đến A7, B6, B7 | P1 |
| T19 | Chỉ báo | Chỉ báo mạng (ping 3 mức) | không | - | màn toàn phần, HUD | A1, B8 | P2 |
| T20 | Nổi | Menu chuột phải (context menu) | không | chưa cần; chỉ ghi để khỏi quên | nổi | không màn nào trong GDD | P2 |
| T21 | Thanh | Spinner xoay (đang chờ không biết thời lượng) | không | đang dùng thanh tiến trình | hộp thoại | A3, B8 | P2 |
| T22 | Thẻ | Thẻ tài khoản có avatar | không | đang dùng toast | màn toàn phần | A2 | P2 |

Ghi chú: các mã C01 đến C19 là thành phần đã có (cột "Thiếu" ở đó ghi chú về giới hạn hiện tại). T15 và T16 đã có bản gen thử ở T-021 đợt B (`art/inbox/T-021/b/assets/`, ghi ở `T-020/ui-kit.md` mục 8), chưa được duyệt và chưa nhập vào kit; T-021 đợt B bị dừng theo T-026.

## 6. Việc đề xuất sau khi Huy duyệt (chưa làm)

| Bước | Nội dung | Ai |
|---|---|---|
| 1 | Huy duyệt danh sách thiếu và ưu tiên, duyệt cỡ nút S 48, M 64, L 80 | Huy |
| 2 | Gen P0 bằng GPT qua art-bridge: nút đóng, chân hộp thoại, header màn, bộ icon (một sheet), thẻ phòng, ô người chơi, ô mã phòng, keycap; cùng nét skin A, cùng luật màu ngữ nghĩa | art-ux |
| 3 | Kiểm 9-slice nút ở S, M, L và popup có chân; cập nhật `meta.json`, `ui-kit.md` | art-ux |
| 4 | P1 sau khi P0 qua, mock lại đợt A trên kit mới trước, đợt B sau | art-ux |

## 7. Câu hỏi cho Huy

| # | Câu hỏi | Mặc định |
|---|---|---|
| 1 | Duyệt thứ bậc nút 8 cấp và cỡ S 48, M 64, L 80? | Duyệt; cỡ là tham số nhìn thấy nên chờ Huy |
| 2 | Hộp thoại một nút căn giữa (khác Carbon căn phải)? | Căn giữa, rộng 320 |
| 3 | GDD không có độ hiếm vật phẩm; thẻ vật phẩm chỉ phân loại Weapon, Armor, Consumable? | Đúng, không có độ hiếm |
| 4 | P0 gồm 8 thành phần có đủ chưa, hay muốn đẩy P1 nào lên P0? | Giữ như bảng |
| 5 | Đợt A mock lại trên kit mới sau khi gen P0, hay giữ mock cũ và chỉ dựng Unity từ kit mới? | Mock lại đợt A (nhanh, 18 màn dựng bằng script) |
