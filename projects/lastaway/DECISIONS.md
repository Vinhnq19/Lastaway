# Quyết định - Lastaway

Quyết định trước 06/11/2026 nằm ở `Docs/CPO_Runbook.vi.md` mục 4 và mục quyết định trong từng `Docs/Tickets/<ID>.md`.

## 2026-11-06 - Nhập Lastaway vào studio istudio
- **Quyết định:** Lastaway là sản phẩm đầu của studio. Thay bộ 4 phiên thường trực (`Lastaway · CPO`, `Thiết kế & Kế hoạch`, `Unity Dev`, `Review & QA`) bằng phiên `CPO` và 5 phiên vai istudio. Skill `/cpo` cũ và 6 agent cũ giữ làm tham chiếu, không còn là luồng chính.
- **Người quyết:** Huy
- **Lý do:** Huy chọn "Thay bằng istudio" khi chạy `/istudio`.
- **Hệ quả:** `Docs/Team_Workflow.vi.md` mục 2.1, `Docs/CPO_Runbook.vi.md`, `.claude/skills/cpo/SKILL.md` trỏ sang `CLAUDE.md`. Ticket mở chuyển vào `production/BACKLOG.md`.

## Quyết định cũ cần nhớ khi giao việc (từ bàn giao 06/11/2026)

| Ngày | Quyết định | Nguồn |
|---|---|---|
| 22/10 | Tên Lastaway, slug `lastaway`; tên miền kỹ thuật `junglemaze.duckdns.org` giữ nguyên | `game-designer-team/projects/lastaway/DECISIONS.md` |
| 27/10 | GDD v0.3 trên Sheet; 5 luật lõi (thắng thua, bo và HP_LOSS, túi đồ, trần khống chế, súng) vẫn là Đề xuất, 26 câu chờ Huy | `game-designer-team/projects/lastaway/gdd/v0.3-nhap/cho-lead-quyet.md` |
| 27/10 | Quyền duyệt G1/G3 thay Huy của CPO cũ thay bằng bảng Thẩm quyền trong `CLAUDE.md` | `Docs/CPO_Runbook.vi.md` mục 1 |
| 27/10 | `EditorSettings.asset` hoàn về 0 sau PlayMode; không commit `TimeManager.asset`, `.codex/config.toml` | Runbook mục 3 |
| 05/11 | Account bắt buộc; Guest cũ chỉ giữ lịch sử; mất API thì không join mới, Local luyện tập vẫn chạy | `Docs/Network_Architecture.vi.md` mục 4 |

Khoảng trống chặn QA-01 theo PIC người (MAP-02, Game-10, DRG-01, ZON-01, ART-02, WPN-03): `production/reports/2026-11-06-ban-giao-cpo-cu.md` mục 5.
