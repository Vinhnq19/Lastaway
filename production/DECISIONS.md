# Nhật ký quyết định - Lastaway

Mỗi quyết định một mục. Ghi ngay khi quyết, không ghi hồi tố. Khi ghi quyết định mới, grep quyết định cũ cùng chủ đề và nhắc chéo.

Khuôn một mục:

```
## YYYY-MM-DD - <Tiêu đề quyết định>
- **Quyết định:** ...
- **Người quyết:** Huy | CPO | <vai>
- **Lý do:** ...
- **Hệ quả:** file/quy trình bị ảnh hưởng, việc tiếp theo.
```

## 2026-11-06 - Dựng bộ máy studio
- **Quyết định:** Dựng studio ba tầng (Huy chỉ trao đổi với CPO; CPO tạo phiên vai designer, level, unity, art-ux, qa; phiên vai gọi subagent). AI ngoài: GPT (qua Codex CLI) làm ảnh.
- **Người quyết:** Huy
- **Lý do:** Dựng bằng skill `istudio`.
- **Hệ quả:** `CLAUDE.md`, `.claude/`, `docs/`, `production/STUDIO_CONFIG.md`.

## 2026-11-06 - Thay bộ máy cũ của repo bằng istudio, nhập Lastaway
- **Quyết định:** Repo đã có bộ máy riêng (skill `/cpo`, 6 agent, 4 phiên `Lastaway · ...`). Huy chọn thay bằng istudio. Lastaway thành sản phẩm đầu (`projects/lastaway/`). Tri thức xuyên dự án trỏ tới `game-designer-team/docs/knowledge`. 6 agent cũ giữ trong `.claude/agents/` làm tham chiếu và subagent (`backend-dev` khi Vinh đồng ý).
- **Người quyết:** Huy
- **Lý do:** Trả lời câu hỏi khi chạy `/istudio`.
- **Hệ quả:** `Docs/Team_Workflow.vi.md`, `Docs/CPO_Runbook.vi.md`, `.claude/skills/cpo/SKILL.md` trỏ sang `CLAUDE.md`; T-001, T-002 trong BACKLOG. Bốn phiên cũ chờ Huy lưu trữ.

## 2026-11-07 - Commit ít
- **Quyết định:** CPO chỉ commit khi một task đóng (qa ĐẠT hoặc Huy duyệt), gộp mọi thay đổi DECISIONS, BACKLOG, so-hien-hanh, brief và asset của task đó vào một commit. Không commit riêng cho ghi quyết định, đổi trạng thái hay lượt làm lại chưa duyệt.
- **Người quyết:** Huy (07/11, "tôi muốn commit ít thôi")
- **Lý do:** 06-07/11 có khoảng 20 commit nhỏ, khó đọc lịch sử.
- **Hệ quả:** Thay dòng "CPO commit theo mốc khi đã kiểm" trong `CLAUDE.md` theo nghĩa này.

## 2026-11-07 - Đường tắt việc nhỏ cho thành viên (Huy chuyển yêu cầu của thành viên)

- Thành viên nhắn: skill Claude trong repo chung làm việc dev nhỏ quá nặng (lập kế hoạch, test, QA, chia dev).
- Sửa: `.claude/skills/cpo/SKILL.md` thêm mục "Việc nhỏ" ở đầu bước phân loại: tối đa khoảng 3 file, không đổi protocol, schema hay contract, không đụng vùng người khác thì làm thẳng, chỉ kiểm compile. `CLAUDE.md`: người dùng không phải Huy thì không áp dụng phần CPO và studio.
