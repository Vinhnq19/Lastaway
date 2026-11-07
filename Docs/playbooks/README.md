# Playbooks - năng lực của từng vai

Mỗi agent đọc playbook của mình trước khi làm việc. Playbook là nơi đội tích lũy năng lực:
checklist, phương pháp, câu hỏi phải tự trả lời, và **Lỗi hay gặp** rút từ retro.

Cấu trúc mỗi playbook:
1. **Checklist trước khi bắt đầu**
2. **Phương pháp** - cách làm việc chuẩn của vai trò
3. **Câu hỏi phải tự trả lời trước khi báo xong**
4. **Lỗi hay gặp** - "Triệu chứng → Nguyên nhân → Cách tránh", kèm ngày thêm. Mục này chỉ lớn lên qua `/retro`.

Quy tắc: prompt agent trong `.claude/agents/` giữ ngắn (vai trò, nguyên tắc). Kiến thức làm việc nằm ở đây.
