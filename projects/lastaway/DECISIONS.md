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

## 2026-11-06 - Icon HP tim biểu tượng, đồng hồ bo màu kem
- **Quyết định:** (1) Icon HP đổi từ tim giải phẫu (`heart-organ`) sang tim biểu tượng hai thùy như concept A, vẫn tông kim loại ấm có viền tối. (2) Đồng hồ bo (icon cát và số) bỏ token Purple, dùng kem như concept A.
- **Người quyết:** Huy ("Đổi cả hai", 06/11), theo lỗi Nhẹ N-ICON-1, N-ICON-2 của qa T-015
- **Lý do:** Tim giải phẫu ở cỡ thật đọc giống bàn tay; tím lệch concept và lệch vòng bo đỏ/vàng trên world.
- **Hệ quả:** art-ux thay `icon_hp*`, `icon_zone-timer*`, sửa `hud-B-spec.md` mục 7-8 và credits; unity gắn vào T-016.

## 2026-11-07 - Minimap có chức năng (Giai đoạn 3)
- **Quyết định:** Thay quyết định 06/11 "minimap chưa thêm chức năng". Minimap Giai đoạn 3: hiện địch khi vào vùng Hero thấy, hiện vùng địa hình, hiện bo, hiện đồng đội, phóng to thu nhỏ.
- **Người quyết:** Huy (07/11, dòng ticket "Giai đoạn 3 | 7/11/2026 | UI | Minimap")
- **Lý do:** Không ghi.
- **Hệ quả:** T-017 (designer: spec + dòng ticket), T-018 (unity: dựng, sau T-016). Phụ thuộc FOW-01 (Quang Huy, Not Yet) cho "vùng Hero thấy". GDD hiện là đấu đơn 10 người, chưa có đồng đội: mục này chờ Huy làm rõ.

## 2026-11-07 - Hoãn minimap; skin A lượt 3: bớt chi tiết, viền nhẹ, tách UI khỏi gameplay
- **Quyết định:** (1) Minimap Giai đoạn 3 chưa làm (T-017, T-018 hoãn). (2) Huy thích skin A hiện tại nhưng UI hơi chi tiết và viền đậm: giảm mức chi tiết (hoa văn, rêu, vân, shading icon), làm viền nhẹ hơn. (3) UI cần thêm yếu tố tách bạch với gameplay; art-ux đưa vài cách trên ảnh gameplay thật để Huy chọn.
- **Người quyết:** Huy (07/11, nguyên văn: "chưa làm vội. UI Oke và tôi thích rồi nhưng chúng hơi chi tiết, và viền khá đậm hãy giảm mức độ chi tiết đi và xử lý để viền nó ko bị đậm quá. Ngoài ra UI cần thêm gì đó để tách bạch với phần Gameplay")
- **Lý do:** UI chi tiết và cùng chất đá rêu với world nên dễ lẫn vào gameplay.
- **Hệ quả:** T-015 mở lại lượt 3. T-016 dựng tiếp cấu trúc với sprite hiện tại, sprite sẽ thay khi lượt 3 được duyệt. Giả định: "chưa làm vội" là trả lời câu hỏi minimap, áp cho cả T-017 và T-018.

## 2026-11-07 - Mục tiêu lượt 3: giống ảnh concept Huy gửi
- **Quyết định:** Lượt 3 bám sát ảnh `projects/lastaway/art/brief/T-015/muc-tieu-huy-07-11.png` (cắt từ concept A T-014): khung đá mảnh, lòng đá xám nâu sáng hơn, vạch khắc chia hàng trong Vitals, icon pixel đơn giản (tim đỏ, áo giáp, mũ, tia sét), quick bar có thanh đạn dạng tab phía trên và số phím kem nằm dưới ô.
- **Người quyết:** Huy (07/11, "tôi muốn nó giống được với Conpect này")
- **Lý do:** Không ghi.
- **Cập nhật 07/11:** Huy gửi concept toàn màn `projects/lastaway/art/brief/T-015/concept-toan-man-huy-07-11.png` (1672x941), nói "Gen ảnh lại theo concept này": asset UI gen lại bằng GPT từ concept này (Vitals, Quick bar, minimap, Match status, killfeed, icon), không dựng tay bằng script nữa; script chỉ dùng tách nền, cắt 9-slice, đo.
- **Hệ quả:** Thay mục tiêu "2-3 cách tách UI" bằng một bản bám ảnh này.

## 2026-11-07 - Mọi asset UI phải gen bằng AI ngoài
- **Quyết định:** Mọi asset UI của Lastaway (khung, panel, ô, nút, thanh, icon, nền, menu) gen bằng GPT qua art-bridge. Không dựng tay bằng script hay vector. Script chỉ để tách nền, cắt 9-slice, đổi cỡ, đo.
- **Người quyết:** Huy (07/11, "Mọi Asset UI cần phải Gen ảnh")
- **Hệ quả:** Áp cho T-015 và mọi task UI sau. Icon game-icons.net (T-012) thay dần bằng icon gen khi làm lại. `projects/lastaway/CLAUDE.md` ghi luật này. Icon HP heart-beats và icon kim loại ấm không còn là đích; theo icon trong ảnh. Vị trí số phím (dưới ô) và tab đạn lệch spec HUD B: art-ux ghi lệch, unity sửa ở T-016.

## 2026-11-07 - T-015 lượt 4: trả một vòng
- **Quyết định:** Lượt 4 (sprite GPT) gần concept nhưng lòng panel be sáng, kém tách khỏi sàn; icon trung tính 1,31-4,23:1. Gen lại lòng panel tối như concept, killfeed bỏ khung đá (chỉ nền tối mờ), khung minimap dày và đậm hơn như concept.
- **Người quyết:** Huy (07/11, "Trả một vòng"), theo đề xuất CPO.
- **Hệ quả:** Art-ux sửa bằng GPT, giữ tên file và rect HUD B. Icon đạt >= 3:1 trên lòng mới là đủ (viền đen giữ); số vẫn >= 4,5:1.

## 2026-11-07 - T-015: làm giống hệt ảnh ref
- **Quyết định:** Bộ HUD skin A phải giống ảnh `art/brief/T-015/concept-toan-man-huy-07-11.png` ở mọi điểm còn lệch: rim olive và rêu dày 4 góc, khung minimap chỉ hoa văn góc, cỡ số (HP, giáp, phím, bo), tỉ lệ thanh HP và Adrenaline, tab đạn. Cỡ và vị trí panel lấy theo concept (đo ra 1920x1080), thay cỡ bản B nếu khác.
- **Người quyết:** Huy (07/11, "Tôi chỉ cần làm giống ảnh Ref").
- **Giả định CPO:** "giống ảnh Ref" gồm cả cỡ panel, nên dòng "HUD bố cục" trong so-hien-hanh đổi theo số art-ux đo từ concept. Huy muốn giữ cỡ bản B thì nói lại.

## 2026-11-07 - Quick bar: số phím trong góc ô, ô giữa thanh, bỏ tab đạn
- **Quyết định:** Số phím đặt ở góc dưới bên phải trong mỗi ô (không còn nằm dưới ô). Hàng ô căn giữa theo chiều dọc của thanh. Bỏ tab đạn (thanh nhỏ phía trên Quick bar).
- **Người quyết:** Huy (07/11, "Đặt số vào góc dưới bên phải của ô... Các ô đưa xuống giữa. bỏ thanh nhỏ bên trên thanh to này đi").
- **Hệ quả:** Khung Quick bar gen lại bằng GPT không có tab; rect cao giảm theo. Chỗ hiện đạn hiện chưa có (giả định CPO: đạn chưa cần trên HUD lúc này; Huy muốn đặt đạn ở đâu thì nói). Unity ở T-016 bỏ phần tab đạn.

## 2026-11-07 - Viền minimap mỏng lại
- **Quyết định:** Khung minimap giảm độ dày viền từ 40 px xuống khoảng 24 px (1080p), giữ rect ngoài 318x316, lòng bản đồ to ra. Hoa văn góc nhỏ theo, rêu cân hai bên.
- **Người quyết:** Huy (07/11, "viền của minimap đang khá là dày"). Số 24 px là CPO chọn; Huy muốn mỏng hơn hoặc dày hơn thì nói.
- **Ghi chú:** Đo bản r3: lỗ khung lệch 1 px ngang, 3 px dọc; vòng bo và lâu đài lệch tâm <= 2 px. Cảm giác lệch trái do ảnh đảo trong mock (dải nước bên phải rộng hơn) và rêu, hoa văn góc không đối xứng. Bản đồ thật trong Unity là RenderTexture căn theo tâm.

## 2026-11-07 - Đồng bộ viền mọi panel HUD: 24 px
- **Quyết định:** Mọi panel HUD (Vitals, Quick bar, minimap, Match status) dùng chung một độ dày viền 24 px (1080p) và một cỡ hoa văn góc, như khung minimap đã duyệt. Match status cao lên khoảng 90 px để đủ chỗ chữ.
- **Người quyết:** Huy (07/11, "tôi cần mọi phần hộp thoại sẽ đồng bộ được viền với nhau"; chọn phương án 24 px CPO đưa).
- **Đo trước khi sửa (1080p):** Vitals 28, minimap 24, Quick bar 15, Match 10. Áp cho mọi hộp thoại UI sau này của Lastaway.

## 2026-11-07 - Vitals: bỏ khung trong từng hàng, tách bằng đậm nhạt
- **Quyết định:** Bỏ đường viền khung bao quanh từng hàng (thẻ) trong Vitals. Ba hàng tách nhau bằng độ đậm nhạt của nền thẻ (lòng thẻ tối/sáng khác nhau), không vẽ đường kẻ.
- **Người quyết:** Huy (07/11, "Bỏ cái đường khung bên trong đi thay bằng đậm nhạt của các thẻ là được rồi mà").
- **Hệ quả:** Khung Vitals gen lại bằng GPT; rim ngoài 24 px và hoa văn góc giữ nguyên. Áp cùng cách cho mọi panel có nhiều hàng sau này.

## 2026-11-07 - Duyệt T-015 bản r11
- **Quyết định:** Bộ asset HUD skin A bản r11 (GPT, theo concept toàn màn) được duyệt; T-015 đóng. qa chấm asset trong Unity ở T-016 thay vì chấm file riêng (CPO, để khỏi chấm hai lần).
- **Người quyết:** Huy (07/11, "Duyệt"); cách chấm: CPO.

## 2026-11-07 - Icon giáp, mũ, Adrenaline khi trống theo ref
- **Quyết định:** Khi giáp, mũ, Adrenaline bằng 0, icon vẫn sáng như khi có và hiện số "0" (theo ảnh ref), bỏ hành vi làm mờ từ T-013.
- **Người quyết:** Huy (07/11, "Theo ref"), theo lỗi N-DIM của qa T-016.

## 2026-11-07 - Đợt UI toàn game: phương án L
- **Quyết định:** Sản xuất toàn bộ UI/UX theo skin A, phương án L trong `de-xuat/ui-toan-game.md`: UI kit, mock mọi màn, làn V.Huy trong Unity (phản hồi HUD, balo, bản đồ lớn, ESC, spectator), đổi skin menu/lobby/kết quả và màn còn thiếu của UI-02. Đi theo chặng, Huy duyệt từng chặng.
- **Người quyết:** Huy (07/11, "L toàn game").
- **Ràng buộc:** UI-02 PIC là Vinh. T-024, T-025 (sửa LobbyUi, MatchOverlay, màn UI-02) chỉ bắt đầu khi Huy xác nhận Vinh đồng ý. Màn bị hạ gục, máy bay nhảy dù ngoài phạm vi (không có trong GDD).
- **Cập nhật 07/11:** Vinh đồng ý cho đội AI sửa LobbyUi, MatchOverlay và màn UI-02 (Huy báo, "Vinh đồng ý rồi"). T-024, T-025 bỏ chặn; vẫn chỉ đổi hình và thêm màn, không đổi logic mạng/phòng của Vinh.
