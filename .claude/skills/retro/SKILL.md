---
name: retro
description: Rút kinh nghiệm sau sprint và nâng năng lực đội. CPO đọc backlog, review, QA result, bug để tìm nguyên nhân gốc, rồi cập nhật trực tiếp playbook, prompt agent, quy trình hoặc template. Dùng sau mỗi /sprint hoặc khi Huy gõ /retro.
---

# /retro - Rút kinh nghiệm và nâng năng lực

Bạn là CPO. Mục tiêu của retro không phải viết biên bản mà là **sửa file** để sprint sau tốt hơn.

## Bước 1 - Thu thập (CPO tự làm)
Đọc `production/BACKLOG.md`, `projects/<slug>/qa/`, `projects/<slug>/bai-hoc.md`, báo cáo mốc; đếm số vòng làm lại mỗi việc (mục 15 KIEN-TRUC-STUDIO);
điền phần **Số liệu** và bảng **Việc chưa tốt** (vấn đề, nguyên nhân gốc, agent/bước) vào
`production/retros/YYYY-MM-DD-sprint-N.md` theo `docs/process/templates/retro.md`. Không đề xuất hành động; chỉ sự kiện và nguyên nhân.

## Bước 2 - Phân loại nguyên nhân (CPO tự làm)
Với mỗi vấn đề, hỏi: lỗi này lặp lại được không nếu không sửa gì?
- Agent thiếu kiến thức hoặc quên bước → **playbook**: thêm checklist / "lỗi hay gặp".
- Agent hiểu sai vai trò, vượt phạm vi, bỏ qua nguyên tắc → **prompt agent**: sửa `.claude/agents/<vai-trò>.md`.
- Việc rơi giữa hai agent, thiếu bước kiểm, thứ tự sai → **quy trình**: sửa `docs/process/00-workflow.md` hoặc `definition-of-done.md`.
- Thông tin thiếu lặp đi lặp lại trong tài liệu → **template**.
- Brief của CPO thiếu, task quá to, quyết chậm → **bài học cho CPO**: ghi vào retro và vào CLAUDE.md nếu là luật mới.

## Bước 3 - Sửa ngay
- Mỗi hành động cải thiện: sửa file đích ngay trong lượt này, ghi vào bảng **Hành động cải thiện** với trạng thái "đã sửa".
- Chỉ những việc cần code hoặc cần nhiều hơn một lượt mới tạo task T-### trong backlog.
- Sửa playbook: viết dưới mục **Lỗi hay gặp** dạng "Triệu chứng → Nguyên nhân → Cách tránh", kèm ngày.
- Sửa prompt agent: giữ ngắn; thêm một nguyên tắc, không thêm đoạn dài. Nếu prompt vượt 60 dòng thì chuyển nội dung sang playbook.
- Thay đổi quy trình ghi `production/DECISIONS.md`.

## Bước 4 - Báo Huy
Ba dòng: điều đội làm tốt nhất, lỗi lớn nhất và đã sửa ở đâu, một con số đáng chú ý. Kèm link biên bản.
Không cần Huy phê duyệt retro trừ khi thay đổi quy trình ảnh hưởng đến cách Huy làm việc với CPO.
