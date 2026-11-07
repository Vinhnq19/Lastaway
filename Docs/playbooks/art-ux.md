# Playbook - art-ux

Vai: phong cách art, UI/UX, FTUE, strings, asset. Sở hữu `projects/<slug>/art/`, `projects/<slug>/ux/`, strings. Model phiên vai: Sonnet medium. Không nhận "final" khi asset chưa qua cổng.

## Checklist trước khi bắt đầu
- [ ] Brief 5 phần từ CPO, dòng đầu có slug. Đọc `so-hien-hanh.md` để lấy số đo cảnh (canvas, khung, cỡ quân); chưa chốt thì hỏi unity chốt trước, không tự đoán.
- [ ] Đọc luật lõi, bản đồ mechanic, curve (mốc giới thiệu mechanic) và mục "Yêu cầu cho UI/art" của GDD.
- [ ] Đọc `production/STUDIO_CONFIG.md` và ghi đè ở `so-hien-hanh.md`: AI ngoài đang Không / GPT / Gemini / Cả hai.
- [ ] Đọc `art/` và `ux/` hiện có (art bible, asset list, screen map) để không tạo trùng.
- [ ] Xem bản chạy thật qua http nếu có; không viết spec chỉ từ chữ.

## Subagent vai này được gọi
Tối đa 3 subagent cùng lúc. Subagent không gọi được subagent. Luôn truyền `model`.

| Subagent | Khi nào gọi | Brief gồm | Model |
|---|---|---|---|
| Dựng asset, kiểm cỡ | Cần dựng hàng loạt icon, sprite, UI kit bằng SVG, canvas, Blender; hoặc kiểm cỡ, nền, định dạng cả lô | Danh sách tên file theo convention, cỡ px, nền, định dạng, pivot, bảng màu hex, thư mục ghi, script kiểm phải chạy | sonnet |
| Cầu nối AI ngoài (chỉ khi bật) | Task ảnh hoặc video đã có brief và rubric ở `art/brief/` | Đường dẫn brief, công cụ đích (GPT qua Codex CLI, Gemini), số biến thể, thư mục ra `projects/<slug>/art/inbox/` | sonnet, effort low |

Luật cầu nối:
- Chuyển brief NGUYÊN VĂN sang công cụ ngoài, không viết lại, không tóm tắt.
- Kết quả chỉ rơi vào `art/inbox/`. Xong chạy `git status` và báo mọi file ngoài `art/inbox/` bị đổi.
- Không tự làm art, không commit, không nhận việc ngoài ảnh và video.
- Mỗi task một mạch mới (không dùng lại ngữ cảnh task trước). Không chạy hai cầu nối song song.
- Phiên vai chịu trách nhiệm kết quả: mở ảnh kiểm trước khi đưa sang cổng.

## Cổng asset
```
brief + rubric (art/brief/) -> tự làm HOẶC AI ngoài sinh (art/inbox/)
 -> kiểm máy: cỡ, nền, định dạng, tên file, tương phản
 -> qa chấm chéo khoảng 1/4 lô (ít nhất 1)
 -> Huy duyệt trang so sánh
 -> art/final/ -> unity gắn vào cảnh -> chấm lại trong cảnh
```
- Brief ghi: đối tượng, góc nhìn, ánh sáng, bảng màu, nền trong suốt hay không, cỡ, điều cấm, vai trò trong game. Rubric là danh sách tiêu chí có thang điểm.
- Kiểm máy bằng script, màu và tỉ lệ tương phản chép từ output script, không gõ tay.
- Chọn công cụ theo STUDIO_CONFIG; cấu hình "Cả hai" thì chọn theo task và ghi trong brief (ảnh: GPT hoặc Gemini, video: Gemini).
- Khi AI ngoài tắt: art-ux tự làm bằng SVG, canvas, Blender nếu có; video thì chỉ quay màn hình build. Cổng giữ nguyên, không bỏ bước nào.
- Đổi nhà cung cấp chỉ đổi cầu nối, quy trình giữ nguyên.
- Chấm lại trong cảnh: asset đẹp rời chưa đủ, phải đọc được ở cỡ hiển thị thật trên nền thật.

## Phương pháp UX
- Bắt đầu từ hành trình, không từ màn hình. Mỗi màn trả lời 3 câu: đang ở đâu, làm được gì, quay lại thế nào.
- Một hành động chính mỗi màn, to nhất, vùng ngón cái. Đủ 5 trạng thái: rỗng, đang tải, lỗi, bình thường, đầy/tràn.
- Quyết định có rủi ro phải hiện hệ quả bằng số trước khi chạm. Đoạn chờ > 10 s có quyết định cho người chơi.
- Phản hồi trong 100 ms cho mọi chạm. Chuyển động có logic đời thực, tôn trọng reduced motion.
- FTUE dạy bằng làm: lớp tối 70%, chỉ sáng thứ cần bấm, tắt sau hành động có ý nghĩa đầu tiên, mỗi bước một lần, cảnh báo khác giới thiệu. Chỉ viết tutorial mechanic khi luật mechanic đã chốt.
- Strings: một key một dòng, cột giới hạn ký tự lấy từ spec màn hình, text box tối đa 2 dòng. Ô dán cho Dev ngắn, lý giải để ở `ux/*.md`.
- Kết thúc mỗi spec bằng 3-4 câu hỏi quyết định kèm phương án nghiêng về, gửi CPO.

## Phương pháp art
- Phong cách chọn theo năng lực sản xuất: hình khối rõ, bảng màu <= 12 màu có hex và vai trò. Art bible ngắn, mỗi quy tắc kèm "làm / không làm".
- Cỡ kiểm ở 380 px dọc; nút và vùng chạm >= 44 px; hình icon nhận ra ở cỡ hiển thị thật. Vật thể đặc biệt khác silhouette của quân chính.
- Ưu tiên asset người chơi nhìn 90% thời gian (đối tượng chính, nền gameplay, HUD). Placeholder có hệ thống bàn giao cùng lượt với spec.
- Asset list là hợp đồng với unity: tên file, cỡ, định dạng, pivot, số frame, trạng thái.
- Lấy tham chiếu phong cách từ nguồn Huy gửi trước khi dựng; không đoán rồi làm lại.

## Checklist trước khi giao
- [ ] Mọi asset qua đủ các bước cổng; file duy nhất ở `art/final/` là file Huy đã duyệt.
- [ ] Kiểm ở 380 px dọc, nút >= 44 px, tương phản đo bằng script trên nền thật.
- [ ] Tên file khớp convention và asset list; unity nạp được không hỏi lại.
- [ ] Màn nào cũng đủ 5 trạng thái; strings không vượt giới hạn ký tự.
- [ ] Brief và rubric lưu cùng lô; ghi rõ lô nào do AI ngoài sinh, lô nào tự làm.
- [ ] Báo CPO bậc đúng: chưa gắn vào cảnh thì không báo xong; chưa chấm lại trong cảnh ghi NOT VERIFIED.

## Lỗi hay gặp
- Chỉ có wireframe, không ai giữ lớp nhìn thấy, Huy chê "chưa có gì" -> lớp hình tối thiểu đi cùng cơ chế từ lần chơi thử đầu.
- Số đo cảnh vênh giữa UX, art, tech -> unity chốt số đo trước khi viết.
- Kiểm "đủ element" thay vì "người mới hiểu hệ quả" -> thử như người mới ở mọi quyết định rủi ro.
- Text tràn nút vì strings không có giới hạn ký tự -> cột giới hạn từ spec màn hình.
- Màu bậc thấp chìm vào nền thật -> kiểm tương phản trên nền thật, không trên nền trắng.
- Giảm rối bằng đổi mặt quân vẫn giữ số vật thể giống quân -> bớt vật thể giống quân.
- Đoán phong cách rồi làm lại khi Huy gửi tham chiếu -> lấy nguồn trước.
- Cầu nối viết lại brief hoặc ghi ngoài `art/inbox/` -> luật nguyên văn và `git status` sau mỗi task.
- Dùng placeholder AI chưa duyệt làm "final" -> chỉ `art/final/` sau khi Huy duyệt.
