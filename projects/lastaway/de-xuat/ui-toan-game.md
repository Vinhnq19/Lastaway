# Đề xuất đợt - lastaway: sản xuất toàn bộ UI/UX theo phong cách skin A

Trạng thái: Huy chọn L ngày 07/11. Đang chặng 1 (H1, T-020). Vinh đồng ý 07/11 cho H5, H6.
Điều kiện bắt đầu: HUD skin A đã VERIFIED (T-016, commit 93c02caf); T-019 đang làm.
Ước lượng là phiên làm việc của đội AI, sai số khoảng ±50%.

## 1. Hiện trạng

| Màn | Nguồn wireframe | Ticket GDD (PIC) | Trong Unity |
|---|---|---|---|
| HUD trong trận | `3:2` | UI-01 (V.Huy) | Có, skin A xong |
| Phản hồi HUD: sát thương, nguy hiểm, nhặt đồ, effect, phím | WF-11, 12, 13 | UI-01 phần 2 (V.Huy) | Có prefab, chưa skin A |
| Balo, nhặt đồ | `3:3` | ITM-01, ART-04 (V.Huy) | Chưa rõ |
| Bản đồ lớn (M) | `7:186` | UI-01 mở rộng | Chưa |
| Cài đặt ESC | `7:337` | UI-03 (V.Huy) | Chưa |
| Xem sau khi bị loại | `7:304` | UI-03 (V.Huy) | Có MatchOverlay, chưa skin A |
| Khởi động, sảnh chính | `13:2`, `7:2` | UI-02 (Vinh) | Có LobbyUi |
| Tìm phòng, tạo phòng, phòng chờ | `8:7`, `8:124`, `8:223` | UI-02 (Vinh) | Có LobbyUi |
| Đang tải trận | `13:3` | UI-02 (Vinh) | Chưa rõ |
| Thắng, thua | `3:4`, `3:5` | UI-02 (Vinh) | Có trong MatchOverlay |
| Mất kết nối, reconnect | chưa có frame | UI-02 (Vinh) | Chưa |
| Bị hạ gục, máy bay nhảy dù | `7:268`, `7:103` | Không có trong GDD | Ngoài phạm vi |

Kết luận ngắn: skin A mới phủ HUD. Màn menu và lobby (UI-02) đã có code do Vinh và Huy làm, PIC là Vinh; đội AI chỉ build làn V.Huy.

## 2. Hạng mục

| Mã | Hạng mục | Vai | Phiên | Phụ thuộc |
|---|---|---|---|---|
| H1 | UI kit skin A: nút (thường, nhấn, tắt, nguy hiểm), panel cỡ S/M/L, popup, tab, slider, toggle, ô nhập, dòng danh sách, thanh cuộn, badge, toast; token màu và chữ. Gen bằng GPT | art-ux | 2 | T-015 |
| H2 | Mock toàn màn mọi màn ở mục 1 (trừ ngoài phạm vi) bằng UI kit, theo wireframe và GDD; luồng chuyển màn | art-ux | 2 | H1 |
| H3 | Skin A cho phản hồi HUD, balo và nhặt đồ, bản đồ lớn | art-ux, unity | 2 | H1, H2 |
| H4 | UI-03 trong Unity: ESC cài đặt, spectator skin A | unity | 2 | H1, H2 |
| H5 | Đổi skin LobbyUi và MatchOverlay sang UI kit (chỉ đổi hình, không đổi logic) | unity | 2 | H1, H2, Vinh đồng ý |
| H6 | Màn còn thiếu của UI-02: loading, mất kết nối; chuyển cảnh | unity | 2 | H5, Vinh đồng ý |

## 3. Phương án

### S - Bộ thiết kế: H1 + H2
Ra UI kit và mock mọi màn để Huy duyệt hướng. Không đụng Unity.
Nghiệm thu: mỗi thành phần kit có đủ trạng thái, cùng viền 24 px và hoa văn góc; mỗi màn có mock 1920x1080 đặt cạnh wireframe; tương phản chữ >= 4,5:1.
Đánh đổi: chưa có gì chạy trong game.

### M - Bộ thiết kế + làn V.Huy trong Unity: H1 đến H4 (khuyến nghị)
Thêm phần thuộc làn V.Huy vào game: phản hồi HUD, balo, bản đồ lớn, ESC, spectator.
Nghiệm thu: Game view 1920x1080 khớp mock từng màn; test HUD và UI xanh; qa ĐẠT từng màn.
Đánh đổi: menu, lobby, kết quả vẫn skin cũ tới khi Vinh đồng ý.

### L - Toàn game: H1 đến H6
Thêm đổi skin menu, lobby, kết quả và làm màn còn thiếu của UI-02.
Rủi ro: UI-02 do Vinh làm PIC; sửa prefab LobbyUi dễ đụng code của Vinh. Cần Vinh đồng ý trước H5.

## 4. So sánh

| | S | M | L |
|---|---|---|---|
| Phiên làm | khoảng 4 | khoảng 8 | khoảng 12 |
| Chạy trong game | Không | HUD, balo, map, ESC, spectator | Toàn bộ |
| Cần Vinh | Không | Không | Có |
| Rủi ro chính | Mock đẹp nhưng khó dựng | Hai phong cách song song một thời gian | Đụng làn Vinh |

## 5. Chặng và cổng Huy duyệt

| Chặng | Hạng mục | Cổng Huy |
|---|---|---|
| 1 | H1 | Duyệt UI kit (trang so sánh) |
| 2 | H2 | Duyệt mock từng màn |
| 3 | H3, H4 | Xem ảnh Game view từng màn |
| 4 | H5, H6 (chỉ L) | Xem luồng menu tới kết quả |

Chặng sau chỉ mở khi Huy duyệt chặng trước.
