# Backlog - Lastaway

Nguồn sự thật duy nhất về việc cần làm. CPO sở hữu cấu trúc; mọi vai cập nhật trạng thái task của mình.

Trạng thái: `todo` · `doing` · `review` · `done` · `blocked` (thêm `loại`, `gộp vào T-###`). Ước lượng: S (một lượt ngắn) · M (một lượt đầy) · L (phải chia nhỏ).

Mã task lớn nhất đã cấp: **T-025**. Task mới lấy số tiếp theo.

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
| T-015 | lastaway: tách skin A đá khắc thành asset UI dùng được (khung 9-slice, ô quick bar trống/chọn, nền thanh, khung minimap, Match status, killfeed) và asset list | art-ux | T-014 | M | done | Sprite PNG nền trong suốt, đúng cỡ spec HUD B ở 1080p, có border 9-slice ghi trong asset list; ảnh dựng thử HUD B bằng asset khớp concept A; qa chấm chéo. Huy duyệt lượt 2 ngày 06/11, icon kim loại ấm; qa ĐẠT 06/11 (0 Nặng, 6 Nhẹ, chưa xem trong Unity). Lượt 3 (07/11): giảm chi tiết, viền nhẹ, thêm cách tách UI khỏi gameplay để Huy chọn. Lượt 4-r11 (07/11): mọi asset gen bằng GPT theo concept toàn màn, cỡ panel đo từ concept, viền 24 px đồng bộ, Quick bar bỏ tab đạn, Vitals tách hàng bằng đậm nhạt. Huy duyệt 07/11; qa chấm trong Unity ở T-016 |
| T-016 | lastaway: gắn skin A vào Hud.prefab (HUD B) | unity | T-013, T-015 | M | done | Game view 1920x1080 khớp dung-thu-hud-A.png T-015; rect theo T-015/asset-list.md mục 3 (thay HUD B); test HUD xanh; qa ĐẠT; ảnh chứng minh số giáp hiện khi có giáp; camera Follow Player. qa ĐẠT 07/11 (VERIFIED Editor, chưa PLAYTESTED; 0 Nặng, 7 Nhẹ trong qa/T-016/qa-result.md). Killfeed chưa có object: task riêng khi Huy cần |
| T-017 | lastaway: spec minimap Giai đoạn 3 (địch trong vùng nhìn, vùng địa hình, bo, đồng đội, phóng to thu nhỏ) và dòng ticket PROJECT TRACKING | designer | - | S | blocked | `projects/lastaway/de-xuat/minimap-chuc-nang.md` có luật bằng ví dụ số cho từng lớp, nguồn dữ liệu, phụ thuộc FOW-01, phím zoom PC, câu hỏi cho Huy; Huy chốt. Huy hoãn 07/11 ("chưa làm vội") |
| T-018 | lastaway: dựng minimap Giai đoạn 3 theo spec T-017 | unity | T-016, T-017 | M | todo | Game view 1920x1080 đủ các lớp đã chốt; test xanh; qa ĐẠT |
| T-019 | lastaway: icon giáp, mũ, Adrenaline khi bằng 0 sáng và hiện "0" theo ref | unity | T-016 | S | done | Game view 1920x1080 đầu trận (giáp 0, mũ 0) khớp dung-thu-hud-A.png; ca có giáp vẫn đúng; test HUD xanh; qa ĐẠT. qa ĐẠT 07/11 (VERIFIED Editor, 0 Nặng) |
| T-020 | lastaway: UI kit skin A (nút đủ trạng thái, panel S/M/L, popup, tab, slider, toggle, ô nhập, dòng danh sách, thanh cuộn, badge, toast; token màu, chữ) gen bằng GPT | art-ux | T-015 | M | done | Mỗi thành phần đủ trạng thái, viền 24 px và hoa văn góc như HUD; 9-slice ghi meta; chữ >= 4,5:1; trang so sánh Huy duyệt. Huy duyệt 07/11 (kèm màu ngữ nghĩa bản sửa) |
| T-021 | lastaway: mock 1920x1080 mọi màn (sảnh, tìm/tạo phòng, phòng chờ, loading, ESC, spectator, balo, bản đồ lớn, thắng/thua, mất kết nối) bằng UI kit, theo wireframe và GDD; sơ đồ luồng | art-ux | T-020 | M | doing | Mỗi màn có mock đặt cạnh wireframe; nội dung đúng GDD (10 người, 4+4 khe); Huy duyệt từng màn |
| T-022 | lastaway: skin A trong Unity cho phản hồi HUD (sát thương, nguy hiểm, nhặt đồ, effect, phím), balo/nhặt đồ, bản đồ lớn | unity | T-021 | M | todo | Game view 1920x1080 khớp mock T-021; test xanh; qa ĐẠT |
| T-023 | lastaway: UI-03 trong Unity: menu ESC cài đặt và spectator skin A | unity | T-021 | M | todo | Theo UI-03.md và mock T-021; ESC không dừng simulation; test xanh; qa ĐẠT |
| T-024 | lastaway: đổi skin LobbyUi và MatchOverlay sang UI kit (chỉ hình, không đổi logic) | unity | T-021 | M | todo | Vinh đồng ý 07/11 (qua Huy). Game view khớp mock; luồng UI-02 hiện có không vỡ; qa ĐẠT |
| T-025 | lastaway: màn còn thiếu của UI-02 (loading, mất kết nối/reconnect) và chuyển cảnh | unity | T-024 | M | todo | Vinh đồng ý 07/11 (qua Huy). Theo UI-02.md và mock T-021; chạy bằng mock state; qa ĐẠT |

## Icebox

| ID | Task | Agent | Phụ thuộc | Ước lượng | Trạng thái | Tiêu chí hoàn thành |
|---|---|---|---|---|---|---|
