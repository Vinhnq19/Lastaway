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

## 2026-11-06 - Giữ màu và style gameplay, chỉ làm lại UI
- **Quyết định:** Không đổi art world: giữ nguyên màu và style gameplay hiện tại (ảnh T-010). Chỉ sửa UI: GPT gen các bản UI hợp với gameplay hiện có để Huy chọn. 4 hướng world của T-011 không follow.
- **Người quyết:** Huy (06/11, nguyên văn: "Giữ nguyên Màu style Gameplay thứ cần sửa là UI nên cần Gen ảnh UI sao cho hợp Gameplay ý")
- **Lý do:** World đã ổn; feedback Vũ #4 ("như html chưa có css") nhắm vào UI.
- **Hệ quả:** T-011 đóng (concept world lưu tham khảo). T-014: GPT gen skin UI trên ảnh gameplay thật, giữ bố cục HUD B.

## 2026-11-06 - Chấp nhận 5 lệch spec HUD B của Dev; kênh Editor cho qa
- **Quyết định:** Chấp nhận 5 lệch T-013: đồng hồ bo dời trái 10 u (chống cắt chữ); icon trong ô 22 u; vòng bo trên minimap đỏ/vàng giống world thay Purple; icon import Bilinear; chữ ngoài 2 ngoại lệ giữ 13 u. qa không lái Editor: unity chạy test và chụp bằng chứng theo danh sách của qa, qa đọc rồi kết luận.
- **Người quyết:** CPO (qa đánh giá chấp nhận được)
- **Lý do:** Lệch nhỏ, có lý do kỹ thuật; giữ luật chỉ unity chạy Unity.
- **Hệ quả:** `hud-B-spec.md` coi các điểm này là đúng. Huy muốn khác thì sửa ở đây.

## 2026-11-06 - Chọn skin UI A đá khắc
- **Quyết định:** Skin UI theo bản A đá khắc của T-014 (`projects/lastaway/art/inbox/T-014/01-da-khac/`): khung đá xám khắc góc hoa văn, viền rêu, hợp world đá rêu olive. World giữ nguyên.
- **Người quyết:** Huy ("A đá khắc", 06/11)
- **Lý do:** Hợp world nhất.
- **Hệ quả:** T-015 (art-ux: asset list, sprite 9-slice), T-016 (unity: gắn skin lên HUD B).

## 2026-11-06 - Làm lại asset skin A theo chất đá concept
- **Quyết định:** Bản T-015 lượt 1 (khung mảnh 8 px, lòng panel xanh đen) không đạt chất đá. Làm lại: lòng panel vân đá xám như concept A, khung dày hơn (khoảng 12-14 px ở 1080p), nội dung lùi vào trong; rect ngoài HUD B (T-013) giữ nguyên. Giữ phần đạt: ô quick bar phẳng, khung minimap + overlay, số phím kem viền đen.
- **Người quyết:** Huy ("Làm lại", 06/11), theo đề xuất CPO
- **Lý do:** Huy chọn A vì chất đá khắc; bản tách mất chất đó.
- **Hệ quả:** T-015 làm lại lượt 2; T-016 chờ.

## 2026-11-06 - Duyệt T-015 lượt 2, đổi icon sang kim loại ấm
- **Quyết định:** Duyệt bộ asset lượt 2 (khung đá dày 13-14 px, góc khắc rêu, lòng vân đá; ô quick bar 60 px, gap 3, nhóm 12; lề nội dung Vitals 40/20, Quick bar 40/18, Match 32). Icon trạng thái đổi từ xanh/đỏ vector sang tông kim loại ấm như concept A; icon tim thêm viền tối (tương phản hiện 2,98:1).
- **Người quyết:** Huy ("Duyệt, đổi icon", 06/11)
- **Lý do:** Lượt 2 đạt chất đá; icon là chỗ lệch concept còn lại rõ nhất.
- **Hệ quả:** art-ux đổi icon trong T-015 rồi qa chấm chéo; T-016 bắt đầu sau khi qa ĐẠT.
