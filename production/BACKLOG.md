# Backlog - Lastaway

Nguồn sự thật duy nhất về việc cần làm. CPO sở hữu cấu trúc; mọi vai cập nhật trạng thái task của mình.

Trạng thái: `todo` · `doing` · `review` · `done` · `blocked` (thêm `loại`, `gộp vào T-###`). Ước lượng: S (một lượt ngắn) · M (một lượt đầy) · L (phải chia nhỏ).

Mã task lớn nhất đã cấp: **T-016**. Task mới lấy số tiếp theo.

Kiểm cấu trúc sau mỗi lần sửa: `node scripts/backlog-check.mjs`.

## Đợt hiện tại - lastaway: làn V.Huy còn mở (nhập 2026-11-06)

| ID | Task | Agent | Phụ thuộc | Ước lượng | Trạng thái | Tiêu chí hoàn thành |
|---|---|---|---|---|---|---|
| T-001 | lastaway TEL-01: chốt G1 spec `Docs/Tickets/TEL-01.md` (Huy hoặc CPO), rồi unity lập plan và build | unity | - | L | todo | G1 ghi trong ticket; build qua review chéo và qa ĐẠT theo AC mục 3 của ticket |
| T-002 | lastaway ART-05 F4: dọn thư mục `Placeholder/` âm thanh sau F1 | unity | - | S | todo | Không còn file Placeholder bị tham chiếu; EditMode xanh; mục F4 trong `Docs/Tickets/ART-05.md` ghi đóng |
| T-003 | lastaway: audit lại Phase 2 trên main hiện tại, thay bản 01/11 | qa | - | M | todo | Bảng 22 task GĐ2 ĐẠT/chưa đạt có bằng chứng ở `Docs/Reports/Phase2_Audit_2026-11-<dd>.vi.md` |
| T-004 | lastaway UI-02: một nguồn phase/screen, offline không bị Canvas Login che, bám luật account 05/11 | unity | T-003 | L | todo | Boot -> StartOffline vào được Game; Login -> phòng -> trận -> kết quả -> menu chạy được; designer cập nhật spec trước; qa ĐẠT |
| T-005 | lastaway ZON-01: đề xuất bảng luật bo một nguồn (bán kính, damage theo phase, thời lượng), so code r25 với GDD r36 | designer | - | S | todo | `projects/lastaway/de-xuat/luat-bo.md`; Huy chốt rồi mới lan sang code |
| T-006 | lastaway: soạn gọn 26 câu luật lõi GDD v0.3 chờ Huy (`game-designer-team/projects/lastaway/gdd/v0.3-nhap/cho-lead-quyet.md`) | designer | - | M | todo | Một trang câu hỏi kèm phương án nghiêng về; câu Huy chốt vào `so-hien-hanh.md` |
| T-007 | lastaway: nhận bàn giao Figma `oeZgisxdiR80hRqbikUwPb` page `116:3` làm nguồn UX/UI, lập bản đồ frame sang màn hình và ticket | art-ux | - | M | done | `projects/lastaway/ux/figma-ban-do.md` liệt kê đủ frame của page `116:3` (node id, tên, màn hình, ticket, trạng thái so với build); ghi chỗ lệch với `Docs/UI_Wireframes.vi.md`. Nghiệm thu 06/11: 36/36 frame; thêm `figma-feedback.md` 5 comment (Vũ) |
| T-008 | lastaway: nối Unity MCP vào Editor của `D:\Lastaway`, sửa hết lỗi đỏ trong Console | unity | - | M | done | Console 0 lỗi đỏ sau compile và sau một lần vào Play; EditMode xanh; ghi danh sách lỗi, nguyên nhân, cách sửa trong báo cáo. Nghiệm thu 06/11: Console 0 đỏ; EditMode 953/959, 6 đỏ do map_demo.json mới (Vũ, b153b6ed) ngoài vùng V.Huy, chuyển PIC Vũ/Vinh; FPT.prefab thiếu nested prefab chuyển Hào |
| T-009 | lastaway: đề xuất HUD tối giản theo quyết định 06/11 (Vitals, Quick bar -30%; icon + số; minimap bản đồ thật), benchmark nhiều game cùng thể loại, chọn bộ icon thư viện | art-ux | - | M | done | `projects/lastaway/de-xuat/hud-toi-gian.md` có bảng benchmark >= 4 game, bộ icon kèm nguồn và giấy phép, mock HUD 1920x1080 đo vùng che; Huy duyệt. Huy chọn B 06/11 |
| T-010 | lastaway: chụp màn gameplay hiện tại 1920x1080 làm đầu vào cho GPT | unity | - | S | done | >= 4 ảnh trong `projects/lastaway/art/brief/gameplay-hien-tai/` (spawn, giao tranh, loot/túi đồ, bo thu hẹp), camera Follow Player, có `index.md` ghi commit và cách chụp. Nghiệm thu 06/11: 5 ảnh; thiếu cảnh có địch vì offline không có bot, CPO chấp nhận |
| T-011 | lastaway: GPT sáng tạo nhiều bản màn gameplay từ ảnh T-010, dựng trang so sánh để Huy chọn | art-ux | T-010 | M | done | >= 4 hướng, mỗi hướng HUD + world cùng khung; trang so sánh cạnh ảnh gốc; ảnh ở `art/inbox/`; Huy chọn một bản. 06/11 Huy không follow hướng world nào, giữ style gameplay; chuyển sang T-014 |
| T-012 | lastaway: spec dựng HUD bản B cho unity (rect, neo, cỡ chữ số, icon PNG xuất từ SVG game-icons.net) và mục Credits ghi công CC BY | art-ux | T-009 | S | done | `projects/lastaway/ux/hud-B-spec.md` đủ số cho từng khối; icon PNG trong `projects/lastaway/art/inbox/hud-icons/png/`; Quick bar không thừa khoảng trống dưới. Huy duyệt 2 ngoại lệ 06/11 |
| T-013 | lastaway: dựng HUD bản B trong prefab HUD theo spec T-012 | unity | T-012, T-010 | M | done | Game view 1920x1080 khớp mock B (ảnh so sánh); EditMode xanh phần HUD; camera Follow Player; qa ĐẠT. qa ĐẠT 06/11 (VERIFIED Editor, chưa PLAYTESTED); lỗi Nhẹ N1-N3 chuyển T-015; số giáp chưa thấy trên ảnh, kiểm ở T-016 |
| T-014 | lastaway: GPT gen skin UI (HUD B, menu) hợp màu và style gameplay hiện tại, world giữ nguyên | art-ux | T-012 | M | done | >= 4 bản skin UI vẽ lên ảnh gameplay T-010 (world không đổi), đúng bố cục HUD B; trang so sánh; Huy chọn một bản. Huy chọn A đá khắc 06/11 |
| T-015 | lastaway: tách skin A đá khắc thành asset UI dùng được (khung 9-slice, ô quick bar trống/chọn, nền thanh, khung minimap, Match status, killfeed) và asset list | art-ux | T-014 | M | done | Sprite PNG nền trong suốt, đúng cỡ spec HUD B ở 1080p, có border 9-slice ghi trong asset list; ảnh dựng thử HUD B bằng asset khớp concept A; qa chấm chéo. Huy duyệt lượt 2 ngày 06/11, icon kim loại ấm; qa ĐẠT 06/11 (0 Nặng, 6 Nhẹ, chưa xem trong Unity) |
| T-016 | lastaway: gắn skin A vào Hud.prefab (HUD B) | unity | T-013, T-015 | M | todo | Game view 1920x1080 khớp concept A; rect HUD B không đổi; test HUD xanh; qa ĐẠT; ảnh chứng minh số giáp hiện khi có giáp |

## Icebox

| ID | Task | Agent | Phụ thuộc | Ước lượng | Trạng thái | Tiêu chí hoàn thành |
|---|---|---|---|---|---|---|
