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

## 2026-11-06 - Hướng sửa HUD và art theo comment Figma của Vũ
- **Quyết định:** (1) Thu nhỏ Vitals và Quick bar 30%. (2) Tham khảo nhiều game cùng thể loại top-down battle royale, không chỉ một game. (3) Minimap hiện bản đồ thật, chưa thêm chức năng. (4) Icon kèm số nhỏ, bỏ nhãn chữ; icon lấy từ thư viện icon có giấy phép rõ. (5) Art: chụp màn hình gameplay hiện tại, gửi GPT tự sáng tạo nhiều bản màn gameplay để Huy chọn bản follow.
- **Người quyết:** Huy (bằng lời, 06/11)
- **Lý do:** Feedback 5 comment trên Figma (`projects/lastaway/ux/figma-feedback.md`).
- **Hệ quả:** Đổi bố cục HUD v2 đã duyệt 24/10 (UI-01). T-009 (đề xuất HUD tối giản), T-010 (chụp gameplay), T-011 (GPT sáng tạo màn gameplay). Chưa lan sang ticket, prefab cho tới khi Huy chọn bản.

## 2026-11-06 - Chọn HUD tối giản bản B
- **Quyết định:** HUD theo bản B trong `projects/lastaway/de-xuat/hud-toi-gian.md`: Vitals 311x172, Quick bar 592x136 (giảm 30% mỗi chiều), minimap và khối Match status giảm thêm 20%, icon kèm số thay chữ (cả Match status). Che 11,95% màn (v2: 20,84%).
- **Người quyết:** Huy (gõ "B", 06/11)
- **Lý do:** Che ít nhất; minimap v2 to hơn game cùng thể loại.
- **Hệ quả:** Giả định "30% mỗi chiều" và "Match status dùng icon" coi là chốt theo mock B; Huy đổi thì sửa ở đây. T-012 (art-ux: spec + icon PNG), T-013 (unity: dựng HUD B). Skin art chờ T-011.

## 2026-11-06 - Duyệt 2 ngoại lệ HUD B
- **Quyết định:** (1) Cỡ chữ HUD B dưới ngưỡng 13 u của UI-01: 12 u (Adrenaline, đạn dự trữ, tên vũ khí), 10 u (nhãn phím). (2) Bỏ tên món giáp/mũ và hai thanh độ bền, chỉ còn icon + số. Kèm giả định theo mock B: ẩn "/100", Match status dùng icon.
- **Người quyết:** Huy ("Duyệt cả hai", 06/11)
- **Lý do:** Giảm 30% không giữ được 13 u; tối giản theo feedback Vũ.
- **Hệ quả:** `projects/lastaway/ux/hud-B-spec.md` là spec chốt cho T-013.
