# Cấu hình studio

Mức studio. Sản phẩm muốn khác thì ghi đè ở `projects/<slug>/so-hien-hanh.md`.

| Mục | Giá trị | Ngày | Ai chốt |
|---|---|---|---|
| AI ngoài | GPT (qua Codex CLI) làm ảnh | 2026-11-06 | Huy |
| Ngưỡng dừng | 90% hạn mức tuần | 2026-11-06 | Mặc định, chờ Huy xác nhận |
| Song song | Tối đa 3 phiên vai chạy cùng lúc; mỗi phiên vai tối đa 3 subagent | 2026-11-06 | Theo kiến trúc |
| Cổng dev server đang bận | (chưa đặt) | 2026-11-06 | Huy điền |

## AI ngoài

| Việc | Công cụ mặc định | Ghi chú |
|---|---|---|
| Ảnh (icon, sprite, nền, UI) | GPT qua Codex CLI | |
| Video (trailer, ads, clip) | Không làm video, chỉ quay màn hình build | Quay màn hình build vẫn do qa/art-ux tự làm |

Cầu nối `art-bridge` kiểm công cụ trước task AI ngoài đầu tiên (GPT: `bash ~/.claude/skills/gpt-bridge/scripts/find-codex.sh --check`; Gemini: `gemini --version`); không chạy được thì báo `BLOCKED: AI ngoài`, không tự cài.

Đường đi asset: `projects/<slug>/art/brief/` -> `art-bridge` -> `art/inbox/` (gitignore) -> cổng duyệt -> `art/final/`. Chi tiết ở `KIEN-TRUC-STUDIO.md` mục 3.2 và `docs/playbooks/art-ux.md`.
