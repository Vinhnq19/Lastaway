# Số hiện hành - Lastaway

Số liệu cân bằng gốc nằm trong GDD (`Docs/[GDD] - Lastaway.xlsx`, các sheet Hero, Weapons, Equipments, Obstacles, MATCH DESIGN DATA, ITEM LIST). Bảng dưới chỉ ghi số Huy chốt khác GDD hoặc chưa có trong GDD.

| Tham số | Giá trị | Ngày | Nguồn |
|---|---|---|---|
| Kích thước Vitals và Quick bar | Giảm 30% so với HUD v2 (24/10) | 2026-11-06 | Huy, chat |
| Minimap | Hiện bản đồ thật, chưa thêm chức năng (thay bằng dòng 07/11) | 2026-11-06 | Huy, chat |
| Minimap chức năng (Giai đoạn 3) | Phương án M của `de-xuat/minimap-chuc-nang.md`: minimap 2 mức (toàn map 150 cell theo map thật, gần 30 cell theo Hero; Huy giữ 2 mức 08/11 sau chặng 1), đổi bằng cuộn chuột khi trỏ trên minimap (Huy 08/11) và phím N; M vẫn mở bản đồ lớn; bản đồ lớn texture 1024, chặn tấn công khi mở, vẫn đi được; 1 waypoint cá nhân (click trái bản đồ lớn đặt, chuột phải xóa, tự xóa khi Hero vào 1,5 cell); lớp địch hoãn tới khi FOW-01 của Quang Huy có interface (08/11); không ping kiểu PUBG, không đồng đội; 0 gói mạng mới. Số 30 cell, 1,5 cell, 2,0 s do designer đặt, để config, chỉnh sau cổng Huy chơi | 2026-11-08 | Huy, chat ("Chốt gói M") |
| Chỉ số HUD | Icon kèm số nhỏ, bỏ nhãn chữ; khi bằng 0 icon vẫn sáng và hiện "0" (07/11) | 2026-11-06 | Huy, chat |
| Quick bar | Số phím ở góc dưới phải trong ô; hàng ô căn giữa dọc; không có tab đạn | 2026-11-07 | Huy, chat |
| Viền panel HUD | 24 px (1080p) mọi panel, cùng cỡ hoa văn góc | 2026-11-07 | Huy, chat |
| HUD bố cục | Theo ảnh ref `concept-toan-man-huy-07-11.png`, cỡ và vị trí panel đo ra 1920x1080 (art-ux ghi trong `T-015/asset-list.md`); thay bản B (Vitals 311x172, Quick bar 592x136) | 2026-11-07 | Huy, chat |
| Cỡ nút UI | S cao 48, M cao 64, L cao 80 (1080p); nút K2 đế khoảng 7 px; hộp thoại một nút căn giữa rộng 320 | 2026-11-07 | Huy, chat |
| Chữ trong màn gameplay | Hạn chế; dùng icon vật phẩm, icon trạng thái, hình UX. Chỉ giữ tên người chơi, số, phím tắt, tên vật phẩm khi nhặt (toast icon + tên + số); ngoài ra tên vật phẩm ở tooltip; icon vật phẩm to hơn, cỡ chờ duyệt mock | 2026-11-07 | Huy, chat |
| Thang cỡ chữ | 5 bậc (px ở 1080p): 96 số lớn, 40 tiêu đề và nút L, 30 thân lớn và nút M, 24 thân và nút S, 20 chú thích; sàn tuyệt đối 20, chữ gameplay sàn 24 (số phím, số trên ô được 20) | 2026-11-07 | CPO, Huy giao duyệt |
| Font | Hai tầng: Chakra Petch Bold (OFL, Google Fonts) cho bậc 96 và 40 (tiêu đề, số lớn); Be Vietnam Pro Bold cho bậc 30, 24, 20 (thân, nút, chú thích, số HUD). Thay Denk One | 2026-11-07 | Huy, chat (chọn Chakra Petch Bold) |
