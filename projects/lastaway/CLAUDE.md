# Lastaway - ngữ cảnh sản phẩm

Game Unity multiplayer battle royale top-down 10 người (client Unity + GameServer .NET 8 LiteNetLib tick 30Hz + API ASP.NET Core/MongoDB). Nhịp: **Chất lượng**. Giai đoạn sản phẩm: 3 (Unity slice) đang chạy, nhập vào studio ngày 06/11/2026.

## Nguồn sự thật (đọc ở đây, không chép sang projects/)

| Cần | File |
|---|---|
| Ticket, số liệu, PIC | `Docs/[GDD] - Lastaway.xlsx`, đọc bằng `python -I Tools/read_gdd.py "PROJECT TRACKING" --id <ID>` |
| Spec, plan, nghiệm thu từng ticket | `Docs/Tickets/<ID>.md` (trạng thái thật nằm ở đây, GDD hay chậm hơn) |
| Kiến trúc client | `Docs/Client_Base_Plan.vi.md`, `Docs/Client_Developer_Guide.vi.md` |
| Backend, protocol | `Docs/Network_Architecture.vi.md` |
| UX/UI | `Docs/UI_Wireframes.vi.md`, Figma page `Lastaway v2 - Redesign` |
| Lịch sử điều phối trước 06/11 | `Docs/CPO_Runbook.vi.md` |
| Bản đồ code | `graphify-out/` (xem `AGENTS.md`) |

## Ranh giới đội người

- Đội AI chỉ build làn **V.Huy** (UI, Audio, Art icon, Telemetry). Vùng của Hào (Hero, map, balance), Vinh (`Backend/`), Vũ (netcode client), Quang Huy (ITM, ENV, MAP) không sửa.
- Việc backend chỉ khi Vinh đồng ý; lúc đó vai `unity` gọi subagent `backend-dev`.
- Không push, không deploy, không dùng AWS.
- Unity Editor: một bên dùng mỗi lúc. Kết thúc task phải trả camera Follow Player (luật trong `AGENTS.md`).
- Commit kèm đường dẫn, tiếng Anh, không ghi AI (quy ước repo).

## Ánh xạ vai cũ sang vai studio

| Vai cũ (`.claude/agents/`) | Vai studio |
|---|---|
| game-designer | `designer` |
| tech-lead, unity-dev | `unity` (plan + build, module-engineer khi cần) |
| backend-dev | subagent của `unity`, chỉ khi Vinh đồng ý |
| code-reviewer, qa-tester | `qa` |
| (art, UX) | `art-ux` |
