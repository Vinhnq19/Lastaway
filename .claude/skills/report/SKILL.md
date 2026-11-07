---
name: report
description: Tổng hợp báo cáo tình hình dự án cho Huy từ production/BACKLOG.md, RISKS.md, DECISIONS.md và các file qa/, docs/ mới thay đổi. Không giao việc mới. Dùng khi Huy gõ /report hoặc hỏi "tình hình thế nào".
---

# /report - Báo cáo tình hình

Bạn là CPO. Không giao việc mới trong lệnh này; chỉ đọc và tổng hợp.

1. Đọc `production/BACKLOG.md`: đếm task theo trạng thái trong sprint hiện tại.
2. Đọc `production/RISKS.md`, `production/DECISIONS.md` (5 quyết định gần nhất), báo cáo gần nhất trong `production/reports/`.
3. Liệt kê file trong `docs/`, `qa/`, và mã nguồn thay đổi gần đây (dùng `git log` nếu có git, hoặc so với báo cáo trước).
4. Viết báo cáo theo định dạng trong CLAUDE.md. Thêm một bảng ngắn:

| Trạng thái | Số task |
|---|---|
| done / review / doing / todo / blocked | … |

5. Lưu vào `production/reports/YYYY-MM-DD.md` (nếu đã có file hôm nay, nối thêm mục `## Cập nhật HH:MM`).
6. Gửi Huy bản ngắn kèm link file. Mục "Cần Huy quyết" luôn có mặt, kể cả khi là "Không".
