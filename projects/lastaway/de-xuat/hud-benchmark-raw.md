# HUD benchmark thô: 2D top-down battle royale / shooter

Ngày truy cập: 2026-11-06. Mục đích: đầu vào cho đề xuất HUD tối giản của Lastaway (PC 1920x1080, 10 người). File thô, chưa phải đề xuất.

## Giới hạn dữ liệu (đọc trước)

- Fandom wiki (surviv.io, ZombsRoyale, Super Animal Royale, Brawl Stars) trả HTTP 402 với WebFetch, không đọc được trang. Chỉ có snippet từ WebSearch.
- Công cụ không xem được ảnh chụp màn hình từ URL, nên mọi kích thước "% màn hình" chỉ tính được khi có số từ mã nguồn. Không có số nào ước từ ảnh.
- Chỉ Suroi có số đo thật (CSS mã nguồn mở). Các game còn lại chỉ có mô tả chữ hoặc tên element DOM, vị trí và cỡ ghi "không đo được".
- Đủ dữ liệu: 1 game (Suroi). Một phần: 4 game (surviv.io/survev, ZombsRoyale, Super Animal Royale, Brawl Stars). Bỏ: Bullet Echo (không có nguồn nào mô tả HUD, xem cuối file).
- Khuyến nghị: designer hoặc art-ux tự chụp 1 frame mỗi game ở 1920x1080 rồi đo, trước khi chốt số.

## Sự thật có nguồn

### Bảng so sánh

| Game | (1) Bố cục HUD | (2) Kích thước khối chính | (3) Máu / giáp / đạn | (4) Minimap | (5) Điểm đáng học | (6) Nguồn |
|---|---|---|---|---|---|---|
| Suroi (clone surviv.io, mã nguồn mở) | Trái trên: minimap + gas timer. Phải trên: kill counter (icon + số lớn), kill leader. Giữa trên: scope. Giữa dưới: thanh máu (có lớp shield chồng), thanh adrenaline ngay trên, số đạn trong băng / dự trữ trên nữa. Phải dưới: 2 ô vũ khí. Trái dưới: vật phẩm hồi máu. Dưới (25% và 75% ngang): equipment, perk | Xem bảng số đo Suroi bên dưới. Đo từ CSS, không phải ảnh | Máu: thanh ngang, shield là thanh chồng cùng khung, không thấy nhãn chữ trong CSS (chưa xác minh có nhãn hay không). Adrenaline: thanh mỏng riêng. Đạn: số chữ lớn (clip) + số nhỏ (dự trữ) ở giữa dưới. Vũ khí: ô có icon 85x85px | Trái trên, 220x220px + viền 10px (mặc định); 135x135px + viền 5px (màn lớn). Có nút đóng minimap (`#btn-close-minimap`), nên không bắt buộc luôn hiện. Nội dung: chưa xác minh | Cụm sinh tồn (máu + adrenaline + đạn) xếp dọc ở giữa dưới, mắt không rời tâm. Thu nhỏ HUD bằng breakpoint "large screens" (minimap 220 xuống 135, thanh máu 330 xuống 230). Số người còn sống không có selector riêng, chỉ có counter ở góc phải trên | CSS: https://raw.githubusercontent.com/HasangerGames/suroi/master/client/src/scss/pages/client/game.scss ; repo: https://github.com/HasangerGames/suroi ; thư mục: https://github.com/HasangerGames/suroi/tree/master/client/src/scss/pages/client |
| surviv.io / survev.io (bản clone đang chạy) | Có `#ui-top-left` (FPS, ping, mini info), `#ui-map-expand-desktop` / `#ui-map-minimize` / `#ui-map-info`, `#ui-top-center-scopes`, `#ui-right-center`, `#ui-leaderboard-wrapper`, `#ui-killfeed-wrapper`, `#ui-kill-leader-wrapper`, `#ui-bottom-center-right`, `#ui-weapon-container`. Vị trí tuyệt đối: không đo được | Không đo được. Mod cho phép scale HUD 70-100% (chỉ là tham số mod, không phải mặc định) | Máu: thanh có element `#ui-health-actual`, có `#ui-health-counter` (số, mod thêm % text). Boost/adrenaline: `#ui-boost-counter` gồm các thanh, mod gán nhãn "AD". Đạn: `#ui-current-clip`, `#ui-equipped-ammo-wrapper`. Vũ khí: `.ui-weapon-switch` + `.ui-weapon-name`, viền ô đổi màu theo loại đạn (ví dụ vàng cho 9mm) | Có nút thu nhỏ / mở rộng map. Cỡ: không đo được. Luôn hiện: chưa xác minh | Người chơi chủ động mod thêm: số % máu, số đạn chạy theo con trỏ chuột (font 25px), kill counter chuyển sang trái vì đè lên ô hồi máu / đạn trên màn nhỏ. Bài học: thông tin sống còn nên ở gần tâm nhìn, và góc dưới dễ xung đột | https://greasyfork.org/en/scripts/509087-survev-io-ui-mod ; https://greasyfork.org/en/scripts/509087-survev-io-ui-mod/code ; https://greasyfork.org/scripts/519982-alguien-client-survev-io-client/code ; https://en.wikipedia.org/wiki/Surviv.io (chỉ cơ chế, không có HUD) |
| ZombsRoyale.io | Một nguồn không chính thức ghi: minimap góc trái trên, thanh máu + shield giữa dưới, số đạn gần joystick phải (bản mobile). Độ tin cậy thấp | Không đo được | Hai thanh riêng: health 100, shield 100 (shield bắt đầu 0, hồi bằng potion). Đạn chia 4 loại (nhỏ, vừa, lớn, shotgun). Nhãn chữ: chưa xác minh | Có (một nguồn thấp tin cậy nói góc trái trên). Cỡ: không đo được. Luôn hiện: chưa xác minh | Máu + shield tách hai thanh dễ đọc, số liệu round (100/100). Ít dữ liệu hơn để học | https://zombsroyaleio.fandom.com/wiki/How_to_play_guide_for_ZombsRoyale.io (chỉ qua snippet, trang trả 402) ; https://zombsroyaleio.fandom.com/wiki/Consumables?oldid=3112 (snippet) |
| Super Animal Royale | Góc trái trên: bộ đếm số người còn sống và "ai giết ai" (killfeed). Phần còn lại: chưa xác minh | Không đo được. Một review cũ nói HUD "nhỏ đến bực" và lúc đó không phóng to được (chưa xác minh bản hiện tại) | Máu: thanh. Giáp: tối đa 3 vạch bảo vệ theo tier (nhãn level 1/2/3). Hồi máu bằng Health Juice (Q) làm đầy thanh từ từ; băng keo sửa giáp. Đạn: băng + dự trữ (ví dụ 30 trong súng, 60 dự trữ). Vị trí và hình dạng: chưa xác minh | Có mở map toàn màn (chọn điểm nhảy). Minimap góc: chưa xác minh | Giáp dạng vạch rời (3 tier) đọc nhanh hơn số. Review phàn nàn HUD quá nhỏ: bài học tránh cỡ chữ nhỏ ở 1080p | https://gamingtrend.com/impressions/attack-of-the-adorable-woodland-creatures-super-animal-royale-impressions ; https://giantbomb.com/super-animal-royale/3030-70232/user-reviews/2200-31070/ ; https://www.thexboxhub.com/super-animal-royale-review/ (chỉ qua snippet) |
| Brawl Stars (Showdown) | Thanh máu và thanh đạn gắn trên đầu nhân vật (world-space), không nằm ở góc. Super là nút vàng trên control cảm ứng, có vòng xoay quanh nhân vật khi sẵn sàng. Bố cục góc / số người còn sống / minimap: chưa xác minh | Không đo được | Máu: thanh trên đầu, tự hồi 13% max sau 3 giây không đánh / bị đánh. Đạn: thanh chia đoạn dưới thanh máu, mỗi đoạn một phát (thường 3). Không có nhãn chữ | Showdown 10 người, vòng độc xanh co vào. Minimap: chưa xác minh (không nguồn nào nhắc) | Đặt máu + đạn trên đầu nhân vật giữ mắt ở nhân vật, khỏi cần HUD góc. Đạn dạng đoạn rời, không số. Lưu ý đây là game mobile, đổi sang PC cần thử | https://www.pocketgamer.com/brawl-stars/brawl-stars-cheats-and-tips-everything-you-need-to-know-about-showdown-mode (Showdown 10 người, vòng độc) ; https://brawlstars.fandom.com/wiki/Power_Cards (snippet, mô tả thanh máu / đạn) ; https://www.allclash.com/?p=87432 (snippet) |

### Số đo Suroi (từ CSS, quy đổi sang 1920x1080)

Nguồn số: https://raw.githubusercontent.com/HasangerGames/suroi/master/client/src/scss/pages/client/game.scss, truy cập 2026-11-06. Đơn vị gốc là px DOM, tính % bằng px / 1920 và px / 1080. Giá trị "màn lớn" là media query `$screen_large`; ngưỡng px của biến này không đọc được (file biến không lấy được), nên chưa biết ở 1920x1080 áp mặc định hay "màn lớn". Quy đổi mặc định để tham khảo; không tính viền trừ khi ghi.

| Khối | Vị trí | Mặc định (px) | % ngang / dọc (mặc định) | Màn lớn (px) | % ngang / dọc (màn lớn) |
|---|---|---|---|---|---|
| Thanh máu | giữa dưới, bottom 10 (lớn 5) | 330x50, viền 8 | 17,2% / 4,6% | 230x35 | 12,0% / 3,2% |
| Thanh adrenaline | giữa dưới, bottom 65 (lớn 42) | 330x20 | 17,2% / 1,9% | 230x16 | 12,0% / 1,5% |
| Minimap | trái trên, top/left 10 (lớn 5) | 220x220, viền 10 | 11,5% / 20,4% (kể viền 12,5% / 22,2%) | 135x135, viền 5 | 7,0% / 12,5% (kể viền 7,6% / 13,4%) |
| Ô vũ khí (mỗi ô) | phải dưới, bottom 10 / right 10 (lớn 5) | 230x85 | 12,0% / 7,9% | 120x45 | 6,3% / 4,2% |
| Số đạn trong băng | giữa dưới, bottom 91 (lớn 60) | 100x50, font xx-large | 5,2% / 4,6% | 75x37, font 22px | 3,9% / 3,4% |
| Số đạn dự trữ | cạnh số trên | 60x30 | 3,1% / 2,8% | 45x22 | 2,3% / 2,0% |
| Kill counter (chữ) | phải trên, top/right 10 (lớn 5) | font 50px, icon 32px | n/a | font 22px, icon 16px | n/a |
| Gas timer | cạnh minimap | 80x30 | 4,2% / 2,8% | 60 rộng, font 14px | 3,1% / n/a |
| Ô vật phẩm hồi máu | trái dưới, bottom 50, left 10 | ô 110x110 | 5,7% / 10,2% | ô 75 | 3,9% / 6,9% |
| Equipment (mũ, giáp...) | dưới, left 25% | ô 55x78 | 2,9% / 7,2% | chưa xác minh | chưa xác minh |

Ghi chú: Suroi có DOM nên có thể co theo cửa sổ trình duyệt; tỷ lệ trên chỉ đúng nếu cửa sổ là 1920x1080 và không scale thêm.

## Sự thật có nguồn: cơ chế phụ trợ (ít dữ liệu HUD)

| Điều | Giá trị | Nguồn |
|---|---|---|
| Suroi changelog nhắc "teammate UI chồng với gas timer" và thanh máu animation bị kẹt đã sửa | v0.22.1 | https://as.suroi.io/changelog |
| Suroi adrenaline: hồi máu theo tốc độ tuyến tính | v0.22.1 | https://as.suroi.io/changelog |
| Showdown Brawl Stars: 10 người, vòng độc co vào | PocketGamer | https://www.pocketgamer.com/brawl-stars/brawl-stars-cheats-and-tips-everything-you-need-to-know-about-showdown-mode |

## Chưa xác minh

- Vị trí và kích thước HUD mặc định của surviv.io gốc (chỉ biết tên element, không có CSS). Cách xác minh: mở survev.io, DevTools, lấy computed style ở 1920x1080.
- Có nhãn chữ trên thanh máu / giáp / adrenaline ở Suroi, surviv.io, ZombsRoyale (chỉ biết mod gán nhãn "AD" ở survev, không phải mặc định).
- Layout đầy đủ và cỡ minimap của Super Animal Royale, ZombsRoyale, Brawl Stars Showdown.
- Minimap có luôn hiện ở surviv.io hay không (có nút thu nhỏ / mở rộng).
- Giáp / mũ ở Suroi, surviv.io hiển thị thế nào trong HUD (surviv.io có vest, helmet nhưng nguồn đọc được không nói cách hiện; Suroi có `#equipment-container` nhưng chưa biết hình dạng).
- Ngưỡng `$screen_large` của Suroi.
- Số người còn sống: Suroi không có selector riêng trong CSS; vị trí thật chưa xác minh.

## Game bị bỏ

| Game | Lý do |
|---|---|
| Bullet Echo | Hai lượt tìm, không nguồn nào mô tả HUD (chỉ cơ chế đèn pin, hồi sinh đồng đội). Không đủ dữ liệu, bỏ. |
| Minion Masters, Hyper Scape, Fortnite | Brief đã loại (không top-down). |

## Báo cáo

- Đã làm: tìm và đọc nguồn cho 6 game; 1 game đủ dữ liệu (Suroi, số thật từ CSS), 4 game có một phần (surviv.io/survev, ZombsRoyale, Super Animal Royale, Brawl Stars), 1 bỏ (Bullet Echo).
- Điểm yếu lớn: Fandom bị chặn (402), không xem được ảnh. Chưa có số đo từ ảnh cho game nào ngoài Suroi. Chưa đạt "5 game đủ dữ liệu" như brief.
- 5 quy tắc rút ra (có nguồn Suroi / survev / SAR / Brawl Stars): 1) Cụm sinh tồn (máu, adrenaline, đạn) xếp dọc giữa dưới, rộng khoảng 12-17% màn. 2) Minimap góc trái trên, 7-12% rộng, có nút thu gọn. 3) Vũ khí góc phải dưới, ô 6-12% rộng, thông tin bằng icon + viền màu theo loại đạn. 4) Giáp dạng vạch rời theo tier (SAR), không dùng số nhỏ. 5) Tránh chữ nhỏ và xung đột góc (SAR bị chê HUD nhỏ, survev mod phải dời kill counter khỏi ô hồi máu).
- File: D:\Lastaway\projects\lastaway\de-xuat\hud-benchmark-raw.md
- Đề xuất cho designer: tự chụp frame 1920x1080 của survev.io và Suroi (chơi trực tiếp, đo bằng DevTools) để bổ sung số surviv.io gốc; coi các hàng ZombsRoyale / Brawl Stars là tham khảo yếu.
