# T-027 Đề xuất thang cỡ chữ (chưa chốt, chờ Huy chọn)

Trạng thái: đề xuất. Chưa sửa kit, mock đã duyệt hay Unity. Ngày 2026-11-07. Mọi cỡ là **px cỡ font ở 1080p** (Be Vietnam Pro Bold), PC ngồi bàn.

## 1. Kết luận

Nghiêng về **phương án gọn 5 bậc: 96 / 40 / 30 / 24 / 20**. Sàn tuyệt đối **20 px**; chữ trong màn gameplay **không dưới 24 px**, ngoại lệ số phím và số trên ô 20 px. Lý do: hiện có 28 cỡ khác nhau, 5 bậc đủ phủ mọi vai trò đang dùng và dễ cài thành cổng máy; 7 bậc chỉ hơn ở chỗ tách nút M (28) khỏi tiêu đề panel (32).

## 2. Hiện trạng (đo bằng `art/inbox/T-027/src/do_co_chu.py`, bảng đầy đủ ở `art/inbox/T-027/hien-trang.md`)

| Chỉ số | Giá trị |
|---|---|
| Số cỡ font khác nhau | 28 |
| Tổng dòng chữ đo | 943 (mock A 303, mock B 287, Unity prefab 353; đã loại ghi chú mock "Minh họa: ...", không phải UI) |
| Dưới 18,52 px (không đạt XAG PC 18 px body height) | 72 dòng (cỡ 18: 45 ở A, 25 ở B, 2 ở Unity) |
| 18,52 đến dưới 20 px | 47 dòng (toàn bộ là Unity 19,5 px: Hud.prefab, LobbyUi, MatchOverlay) |
| Dưới 24 px | 412 dòng (44%): A 150/303, B 125/287, Unity 137/353 |

- Be Vietnam Pro Bold: chiều cao chữ hoa = 0,74 em; thân chữ từ đỉnh d đến đáy g = 0,972 em; dấu chồng (Ắ) cao hơn chữ hoa 0,44 em. XAG 18 px body height tương đương em tối thiểu 18,52 px; làm tròn sàn thiết kế lên 20 px để dư.
- Unity: CanvasScaler tham chiếu 1280x720, hệ số 1,5 sang 1080p. Cỡ 13 trong prefab = 19,5 px thật: nhãn Adrenaline, Giáp, Mũ, Di chuyển để hủy, toast HUD. Đây là chỗ "nhỏ quá không đọc được" thật; mock A, B phần lớn đã từ 22 px trở lên.
- Ảnh HUD concept (T-015, đã dùng làm nền cho Hud.prefab) có chữ in sẵn: kill feed ước 18 px, số phím quick bar ước 14 px (ước lượng từ ảnh, không đo bằng script).

## 3. Cơ sở (chi tiết và URL ở `art/inbox/T-027/nghien-cuu.md`)

| Nguồn | Con số @1080p | Nhắm tới |
|---|---|---|
| XAG 101 (Microsoft, đã tự đọc lại) | PC/VR tối thiểu 18 px; Console 26 px; đo theo body height từ đỉnh ascender đến đáy descender; chữ trong icon và keycap cũng phải đạt; cho scale tới 200% | PC ngồi bàn / TV |
| XAG 101, giãn dòng | Khối nhiều dòng line spacing >= 1,5 | chung |
| XAG 102 | Tương phản 4,5:1 chữ thường, 3:1 chữ lớn, đo ở vùng nền xấu nhất | chung |
| Google Fonts | Ascender tiếng Việt lấy theo Ắ (U+1EAE), LineGap = 0; line height tổng 120 đến 130% em | tiếng Việt |
| Amazon 10-foot, IGDA GASIG | 28 px và 32 px | TV, không áp cho PC |

Chưa xác minh: số px chữ HUD của Fortnite, Apex, PUBG, Warzone (không có số công bố; chưa đo từ ảnh chụp, xem mục 8); line-height từng bậc của M3.

## 4. Hai phương án

### 4a. Gọn 5 bậc (đề xuất)

| Bậc | px | Đậm | Line-height | Dùng cho |
|---|---:|---|---:|---|
| H1 Số lớn | 96 | Bold | 1,05 | Hạng, đếm ngược, CHIẾN THẮNG |
| H2 Tiêu đề | 40 | Bold | 1,2 | Tiêu đề màn, panel, popup; nút L; số thống kê lớn |
| B1 Thân lớn | 30 | Bold | 1,3 | Nút M; tên người chơi nổi bật; số HUD chính (máu) |
| B2 Thân | 24 | Bold | 1,35 | Nút S; chữ thân; dòng danh sách; toast; tooltip; số HUD phụ; **sàn chữ gameplay** |
| C Chú thích | 20 | Bold | 1,4 | Chú thích, nhãn phụ, số trên ô, chữ trên keycap; **sàn tuyệt đối** |

### 4b. Đủ 7 bậc

| Bậc | px | Đậm | Line-height | Dùng cho |
|---|---:|---|---:|---|
| D Số lớn | 96 | Bold | 1,05 | Hạng, đếm ngược, CHIẾN THẮNG |
| N Số kết quả | 64 | Bold | 1,1 | Số thống kê kết quả, #hạng / 10 |
| H1 Tiêu đề màn | 40 | Bold | 1,2 | Tiêu đề màn, popup |
| H2 Tiêu đề panel | 32 | Bold | 1,25 | Tiêu đề panel; nút L |
| L Thân lớn | 28 | Bold | 1,3 | Nút M; tên nổi bật; số HUD chính; tên trong tooltip |
| B Thân | 24 | Bold | 1,35 | Nút S; thân; danh sách; toast; số HUD phụ; sàn chữ gameplay |
| C Chú thích | 20 | Bold | 1,4 | Chú thích, nhãn phụ, số trên ô, keycap; sàn tuyệt đối |

## 5. Luật chung cho cả hai phương án

| Luật | Nội dung |
|---|---|
| Sàn | Không chữ nào dưới 20 px em (body height 19,4 px, đạt XAG PC 18 px). Gameplay (HUD, túi đồ, toast, spectator) không dưới 24 px; riêng số phím và số trên ô được 20 px |
| Đậm | Bold cho mọi bậc; Medium (đã có `BeVietnamPro-Medium.ttf`) chỉ cho khối từ 2 dòng trở lên (tooltip, mô tả) |
| Line-height | Một dòng không dưới 1,3 em (dấu chồng Ắ Ặ Ề cao thêm 0,44 em); khối nhiều dòng 1,5 theo XAG 101 |
| Chữ trên nền biến thiên | Ưu tiên backplate (toast, tag, row, thẻ của kit). Không có backplate thì viền chữ `max(2, round(em/12))` px, màu #0A0C0C (hiện luôn 2 px). Viền không giúp chữ vượt sàn: XAG không tính pixel viền tương phản dưới 2:1 vào chiều cao |
| Tương phản | Cổng tương phản đo ở p10 và p90 độ sáng nền, 4,5:1 (3:1 cho chữ từ 40 px) |
| Icon cạnh chữ | Giữ luật nút: chiều cao ink icon = cỡ font |
| Gán bậc khi dựng | Cỡ chữ phải là một bậc của thang; cổng `do_co_chu.py --gate` sẽ báo cỡ ngoài thang và cỡ dưới sàn |

**Chữ trên nút theo bậc của cỡ nút, không theo cỡ chữ cũ** (cỡ nút gần nhất trong S 48, M 64, L 80; nút 72 cao tính là M):

| Cỡ nút | 5 bậc | 7 bậc |
|---|---:|---:|
| S 48 | 24 | 24 |
| M 64 | 30 | 28 |
| L 80 | 40 | 32 |

Mọi chữ khác: đổi sang bậc gần nhất (hòa thì lên), sau đó nâng gameplay lên 24 nếu còn dưới. 5 bậc: 18 đến 21 thành 20; 22 đến 27 thành 24 (27 hòa thì lên 30); 28 đến 34 thành 30; 36 đến 57 thành 40; 72 trở lên thành 96. 7 bậc dùng cùng quy tắc.

## 6. Demo v2 (thư mục `art/inbox/T-027/`, mỗi loại có bản `-5` và `-7`)

| Cần xem | File |
|---|---|
| Trang thang chữ (có bậc chữ nút S, M, L) | `thang-5-bac.png`, `thang-7-bac.png` |
| Trước/sau HUD trong trận, đã vẽ lại chữ in sẵn: kill feed (panel nới theo chữ), số máu 100, giáp, mũ, adrenaline, 1/1, 1:21, số quick bar | `hud-truoc-sau-5.png`, `hud-truoc-sau-7.png` |
| Trước/sau B4 túi đồ, phòng chờ, ESC (mỗi nửa 1920x1080) | `b4-`, `a6-`, `esc-truoc-sau-5.png` và `-7.png` |
| Crop 100%, trái trước, phải sau (chữ nhỏ nhất mỗi màn, kill feed, số quick bar, số máu, 1/1 và 1:21) | `crop-100-5.png`, `crop-100-7.png` |
| Độ lộn xộn: hộp chữ tô màu theo cỡ (HUD, phòng chờ) + biểu đồ cột số dòng theo cỡ, trước và sau | `loan-co-5.png`, `loan-co-7.png` |

Cách dựng: render lại đúng mock đã duyệt bằng font qua script (`src/demo.py`), gán mọi cỡ vào bậc; chữ nút theo cỡ nút. Ghi chú mock ("Minh họa: ...") không vẽ, không đo. Chữ in sẵn trong ảnh HUD là cỡ ước lượng.

| Số đo | 5 bậc | 7 bậc |
|---|---:|---:|
| Số cỡ khác nhau, trước thành sau (A + B + Unity, 943 dòng) | 28 thành 5 | 28 thành 7 |
| Dòng dưới 20 px, trước thành sau | 119 thành 0 | 119 thành 0 |
| Dòng dưới 24 px, trước thành sau (sau: chỉ còn chú thích ngoài gameplay ở 20) | 412 thành 101 | 412 thành 101 |
| HUD trong trận: số cỡ khác nhau, trước thành sau | 10 thành 3 | xem `loan-co-7.png` |
| Phòng chờ: số cỡ khác nhau, trước thành sau | 7 thành 4 | xem `loan-co-7.png` |

Cổng trên bản "sau" (chạy trên toàn bộ 36 mock A và B):

| Cổng | 5 bậc | 7 bậc |
|---|---|---|
| Tương phản 4,5:1 (590 dòng chữ) | 0 dòng dưới | 0 dòng dưới |
| Căn giữa nút (40 ca nút khác nhau) | 0 FAIL, lệch tối đa 0,5 px | 0 FAIL, lệch tối đa 0,5 px |
| Chữ nút đúng bậc của cỡ nút | 0 nút sai | 0 nút sai |

Nhận xét: ở mock A và B chênh trước/sau nhỏ vì phần lớn đã từ 22 px; khác rõ nhất ở chữ in sẵn của ảnh HUD, chú thích 18 và 20 px, và toàn bộ Unity (19,5 thành 24 hoặc 20). Chưa chụp được màn Unity thật; việc đó thuộc vai unity.

## 7. Việc phải làm nếu Huy chọn (chưa làm)

| Việc | Ai |
|---|---|
| Sửa cỡ trong Hud.prefab, LobbyUi, MatchOverlay (m_fontSize x 1,5 vào bậc; 47 thành phần 19,5 px) | unity |
| Vẽ lại kill feed và số quick bar trong ảnh HUD gốc hoặc chuyển sang text Unity | art-ux + unity |
| Dựng lại mock A, B theo bậc; chạy cổng cỡ chữ, tương phản, căn giữa | art-ux |
| Đặt SAN_SAN = 20 và bật kiểm bậc hợp lệ trong `do_co_chu.py`, ghi DECISIONS | art-ux |

## 8. Chưa làm, chưa xác minh

- Đo chữ HUD và menu của 2 đến 3 game battle royale từ ảnh chụp 1080p: chưa làm. Không có số công bố; ảnh chụp phải tải về máy nên cần Huy cho phép trước (nguồn ảnh, số lượng).
- Chưa kiểm trên Unity thật (chữ TMP đổi cỡ có tràn khung nút không), chưa chơi thử ở màn 24 inch. Cần làm sau khi Huy chọn.
- XAG khuyến nghị người chơi tự scale chữ đến 200%; chưa có trong thiết kế.
- Cỡ chữ in sẵn trong ảnh HUD là ước lượng từ ảnh.
- Bậc 96 và 64 chưa kiểm với ảnh thực tế ngoài mock kết quả.

## 9. Câu hỏi mở

1. Chọn 5 bậc (nghiêng về) hay 7 bậc?
2. Chữ gameplay sàn 24 px (số phím, số trên ô 20 px) có đủ lớn, hay muốn 26 px?
3. Có cho phép tải ảnh chụp 1080p của 2 đến 3 game battle royale về `art/inbox/T-027/ref/` để đo không (nghiêng về: Fortnite và Apex, 2 ảnh mỗi game, chỉ dùng để đo)?

## 10. Cập nhật 2026-11-07: font chính

Huy chốt bằng lời: **Chakra Petch Bold** (Google Fonts, OFL 1.1) cho bậc 96 và 40, thay Denk One; Be Vietnam Pro Bold cho 30, 24, 20 và mọi chữ nút (kể cả nút L). Thang 5 bậc 96/40/30/24/20 giữ nguyên. Lọc font, so sánh 4 font và nhận xét: `art/inbox/T-027/font-loc.md`, `font-so-sanh.png`, `font-so-sanh-2.png`. Phần mục 4 đến 6 ở trên không nhắc font chính và vẫn đúng; demo cuối: `thang-5-bac.png`, `*-truoc-sau-5.png`, `crop-100-5.png`, `loan-co-5.png` (bản Denk One ở `art/inbox/T-027/_cu/denk-one/`; các trang `-7` là phương án đã bỏ).
