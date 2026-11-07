# Feedback comment Figma - file `oeZgisxdiR80hRqbikUwPb`

Đọc ngày 06/11/2026 (art-ux, T-007 bước 1), chỉ đọc, không trả lời, không resolve, không viết comment.

## 1. Cách đọc và số đếm

- Nguồn: `GET /api/file/oeZgisxdiR80hRqbikUwPb/comments` gọi từ tab Browser đã đăng nhập (HTTP 200, `error:false`). Không dùng token Figma.
- Kết quả: **5 bản ghi = 4 thread + 1 reply**. Cả 5 `resolved_at = null`, `deleted_at = null`: **tất cả đang mở**, không có comment đã resolve. Một tác giả duy nhất (user_id `1551988494137517834`; avatar trên ghim hiển thị "Vũ"). API không trả tên người dùng, chưa xác nhận tên đầy đủ.
- Phạm vi: 3 comment trên frame `17_hud-in-match` (`123:38`), 1 reply, 1 comment cấp page `116:3`. **Không có comment nào trên 35 frame còn lại.**
- Thời gian trong bảng là giờ UTC theo API (06/11/2026).
- Vị trí ghim: `node_offset` là toạ độ trong frame 1920x1080 (tâm màn 960x540).

## 2. Bảng comment (5 dòng)

| # | Comment id | Node / frame | Vị trí ghim | Người viết | Ngày giờ (UTC) | Nội dung gốc | Trạng thái | Việc phải sửa trong wireframe | Ưu tiên |
|---|---|---|---|---|---|---|---|---|---|
| 1 | `1997336478` | `123:38` 17_hud-in-match | (282, 841): khối Vitals góc dưới trái | Vũ (ghim) | 06/11 01:56 | "quá nhiều chữ và quá nhiều loại thanh => player rối, các thanh nhìn giống nhau khó nhìn chỉ số, thay = icon" | Mở | Vitals: bỏ nhãn chữ (HP, giáp, mũ, Adrenaline), thay bằng icon; phân biệt các thanh bằng hình dạng/icon chứ không chỉ màu; giảm số loại thanh. Sửa `41:2` v2 | Cao |
| 2 | `1997337043` | `123:38` 17_hud-in-match | (1736, 173): Minimap góc phải trên | Vũ (ghim) | 06/11 01:56 | "minimap nhưng chưa có gì ngoài màu đen thẳm, ko hiện bản đồ lên thêm chức năng" | Mở | Minimap phải hiển thị nền bản đồ thật (địa hình, tường, vòng bo, vị trí người chơi); wireframe cần vẽ minimap có nội dung. "Thêm chức năng" chưa rõ (xem mục 4) | Trung bình |
| 3 | `1997338362` | `123:38` 17_hud-in-match | (925, 437): giữa màn | Vũ (ghim) | 06/11 01:58 | "bố cục UI nhìn rất chắn tầm nhìn, tham khảo những con cùng thể thoại làm sao cho tối giản UI, UI bị to so với game pc. Sẽ ntn nếu enemy ở góc dưới và bị UI máu kia che mất" | Mở | Thu nhỏ và tối giản toàn HUD; kiểm tình huống enemy ở góc dưới trái bị Vitals che; đối chiếu HUD game cùng thể loại (top-down BR). Sửa bố cục `41:2`, `41:180` | Cao |
| 4 | `1997339165` | `116:3` (cấp page, ngoài frame) | (33, -121) | Vũ (ghim) | 06/11 01:59 | "sửa art - hiện tại nhìn như html chưa có css" | Mở | Không phải sửa wireframe mà là art: toàn bộ UI còn phẳng như chưa style. Gắn với hướng art A (`Docs/Art/UI_Asset_Packs.vi.md`) và audit V6, V8, V10 | Cao |
| 5 | `1997343331` | `123:38` (reply của #1) | thuộc thread #1 | Vũ (ghim) | 06/11 02:03 | "đặc biệt với game cường độ cao người chơi ít khi liếc tới chỉ số để xem chi tiết nên cần tối giản và dễ nhìn nhanh" | Mở | Lý do cho #1: nhìn nhanh được, không đọc chữ. Tiêu chí kiểm: nhận ra mức máu trong khoảng nhìn thoáng (<= 1 s) không cần đọc số | Cao (cùng #1) |

## 3. Việc sửa gom theo màn hình

| Màn | Việc | Từ comment |
|---|---|---|
| HUD trong trận (`17_hud-in-match`, nền tảng của `18`, `19`) | Vitals: icon thay chữ, thanh phân biệt được bằng hình dạng, ít loại thanh | #1, #5 |
| HUD | Giảm kích thước và độ che: Vitals 296x164 và Quick bar 564x130 (v2, canvas 1280x720) nhân 1,5 khi lên 1080p, to so với PC | #3 |
| HUD | Kiểm tình huống enemy góc dưới bị Vitals che; vẽ lại hoặc thu nhỏ khối | #3 |
| HUD, Minimap | Nội dung minimap thật thay vì khung đen | #2 |
| Toàn bộ 36 frame | Art: đã sơn màu phẳng, cần hướng A đổi màu và asset; xem audit V6, V8, V10 | #4 |
| 35 frame còn lại | Không có feedback | - |

Ảnh hưởng chéo: sửa HUD v2 chạm UI-01 (đã Done, bố cục Huy duyệt 24/10) và `UI-01-P2`; đổi bố cục HUD là đổi hướng so với chốt 24/10, cần Huy xác nhận trước khi lan sang ticket (luật chống làm lại số 4). Chưa sửa gì.

## 4. Comment mơ hồ cần Huy nói rõ

| # | Điểm mơ hồ | Phương án nghiêng về |
|---|---|---|
| 1 | #3 "UI bị to": thu nhỏ bao nhiêu? | Giảm khoảng 30% kích thước Vitals và Quick bar (ví dụ Vitals còn ~207x115 trên canvas 1280x720), kiểm mức che bằng vùng giữa sạch |
| 2 | #3 "tham khảo những con cùng thể thoại": game nào làm chuẩn? | surviv.io (HUD tối giản, đã ghi ở audit mục 6); Huy bổ sung nếu có game khác |
| 3 | #2 "ko hiện bản đồ lên thêm chức năng": là "chưa hiện bản đồ, thêm chức năng", hay "không cần minimap"? | Hiểu là minimap cần hiện bản đồ; chưa thêm chức năng nào cho tới khi Huy nói |
| 4 | #1 "thay = icon": thay hết chữ, hay giữ số bên cạnh icon? | Icon + số nhỏ cho máu, giáp, Adrenaline; bỏ nhãn chữ |
| 5 | #4 "sửa art": ưu tiên màn nào trước? | HUD trước (90% thời gian nhìn), sau đó menu |
