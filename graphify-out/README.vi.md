# Bản đồ dự án Lastaway

- Mở `GRAPH_TREE.html` để duyệt thư mục, file và symbol. Assets gom theo thư mục.
- Mở `graph.html` để xem cụm và liên kết; tìm theo tên module. Vì trên 5.000 nút, giao diện tổng quan gom theo community.
- `GRAPH_REPORT.md`: thống kê, hub, quan hệ và giới hạn trích xuất.
- `graph.json`: dữ liệu tra cứu cho agent; `health.json`: chẩn đoán chất lượng.

Tra sơ đồ trước để tìm module, rồi đọc code thật trước khi sửa. Sơ đồ không chứng minh gameplay đã chạy đúng, không thay thế test và không tự hiểu mọi lời gọi reflection/DI.

## Cập nhật

Sau khi sửa code/config, chạy từ project root:

```powershell
$mapPython = Get-Content graphify-out/.graphify_python
& $mapPython -X utf8 Tools/update_project_map.py
```

Công cụ so hash nội dung: không đổi code/config thì không ghi lại bản đồ. AST dùng cache; không gọi API LLM. Các file bị xóa/đổi tên cũng thay đổi tập hash và làm bản đồ được dựng lại. Hook `post-commit` đã cài trên checkout này, chỉ gọi cùng công cụ; không chạy watcher nền. Hook không tự chia sẻ sang máy khác qua Git. AGENTS.md yêu cầu agent cập nhật sau khi sửa code, kể cả chưa commit.

Nếu tài liệu kiến trúc đổi, cần trích xuất lại fragment `architecture-semantic.json` trong lượt cập nhật code kế tiếp. Hash tài liệu không khớp thì công cụ bỏ các khái niệm cũ và báo cảnh báo, không âm thầm dùng chúng.

Máy khác cần Python có `graphifyy`, rồi tạo lại `graphify-out/.graphify_python` chứa đường dẫn interpreter ở máy đó. Graph được lập từ working tree; commit trong báo cáo là mốc tham khảo, hash nội dung mới quyết định có cập nhật hay không.

## Giới hạn đã biết

Bốn file UniTask có vùng cú pháp/preprocessor bộ AST không đọc hết; 18 file không có symbol được bộ trích xuất hỗ trợ (gồm top-level Backend/Api/Program.cs và PowerShell). Cây vẫn có các file đó. Config/asmdef/Docker có nút file nhưng không hứa đầy đủ call graph.

Graphify báo endpoint ngoài/không resolve và gộp nhiều quan hệ cùng cặp nút trong DiGraph; xem số thực trong health.json. Đây là giới hạn của công cụ, không phải lỗi C# của Unity. Metadata assets được gom ở cấp thư mục; không phân tích từng sprite/audio/PDF/XLSX. Số token của semantic agent là tự báo, không phải đo billing; không tuyên bố mức tiết kiệm.
