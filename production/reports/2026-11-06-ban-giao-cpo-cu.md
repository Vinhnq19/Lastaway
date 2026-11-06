# Bàn giao tri thức Lastaway cho bộ máy istudio - 06/11/2026

Người viết: phiên `Lastaway · CPO` (bộ máy cũ, skill `/cpo`). Người nhận: phiên `CPO` của istudio.
Mục đích: gom những gì bộ máy cũ biết mà chưa nằm trong `production/` hay `projects/lastaway/`, để CPO mới đưa vào BACKLOG, RISKS, so-hien-hanh, DECISIONS. File này không tự sửa các file đó vì CPO mới đang giữ cấu trúc.

## 1. Trạng thái repo đã kiểm ngày 06/11

| Mục | Giá trị |
|---|---|
| Thư mục | `D:\Lastaway` (đường cũ `D:\GitHub\Lastaway` và `E:\Github\...` không còn) |
| Nhánh | `huycv` = `origin/main` 6376f7df + commit `88a70cad` (docs, chưa push) |
| Nhánh team chưa vào main | `origin/haonh/dev` hơn main 11 commit: WPN-03 (vũ khí ném, đặc biệt), chọn skin hero, tay cầm vũ khí |
| File đang sửa chưa commit | `ProjectSettings/ProjectSettings.asset` (không rõ ai sửa, không phải CPO cũ) và toàn bộ khung istudio |
| Scheduled task còn sống của CPO cũ | Không còn (các `cpo-resume-*` ngày 27/10 đã hết); lịch Codex nền cũng không còn |
| Thay đổi của CPO cũ | `88a70cad` đã commit (gộp 6 vai thành 3 phiên trong `Team_Workflow`, `CPO_Runbook`, skill `cpo`); nội dung này đã bị khung istudio ghi đè trong working tree, đúng ý. Chưa commit duy nhất là file này |

## 2. Nguồn sự thật và độ tin

| Nguồn | Độ tin | Ghi chú |
|---|---|---|
| Cột Status trong PROJECT TRACKING | Thấp | 29/40 ghi "Not Yet" trong khi main đã có phần lớn GĐ2 ở mức module. File xlsx local còn có thể cũ hơn Google Sheet `17z41xmXWXdJhTJcziRoNuVw0jJQlEI8PS13_g3kYWUo` |
| `Docs/Tickets/<ID>.md` | Trung bình | Lệch đã thấy: `UI-02.md` ghi "Spec" dù đã G3 vòng 2 ngày 31/10; `ART-05.md` ghi "Spec (chờ G1)" dù runbook ghi G3 có điều kiện 27/10. Nghi do commit "Fix conflicts" 01/11 của Vinh đè file |
| `Docs/Reports/Phase2_Audit_2026-11-01.vi.md` | Cao cho ngày 01/11 | Bị vượt bởi commit 02-05/11 (map survival, art jungle, bot server, account bắt buộc) |
| `Docs/Network_Architecture.vi.md` (05/11) | Cao | Luật mới nhất về account, Guest, ticket |
| `game-designer-team/projects/lastaway/` | Cao cho phần thiết kế | GDD v0.3, 26 câu chờ Lead, rà soát 28/10. Hai phiên `GD-manager - lastaway`, `gdm-lastaway` đã xóa 06/11; tri thức của chúng nằm ở thư mục này |

## 3. Quyết định đã có, cần biết khi giao việc

| Ngày | Quyết định | Nguồn |
|---|---|---|
| 22/10 | Tên game Lastaway (đọc Lát-ơ-wây), slug `lastaway`, tên miền kỹ thuật `junglemaze.duckdns.org` giữ nguyên | game-designer-team `projects/lastaway/DECISIONS.md` |
| 27/10 | GDD v0.3 dán lên Sheet (14 tab, trừ PROJECT TRACKING). Năm luật lõi (thắng thua, bo và HP_LOSS, túi đồ, trần khống chế, súng) vẫn nhãn **Đề xuất**, Huy chưa trả lời 26 câu | như trên, `gdd/v0.3-nhap/cho-lead-quyet.md` |
| 27/10 | Huy giao CPO cũ quyền duyệt G1/G3 thay, "tới khi thu hồi". Với istudio, quyền này coi như thay bằng bảng Thẩm quyền trong `CLAUDE.md` | `Docs/CPO_Runbook.vi.md` mục 1 |
| 27/10 | `EditorSettings.asset` hoàn về 0 sau mỗi PlayMode; không commit `TimeManager.asset`, `.codex/config.toml` | Runbook mục 3 |
| 05/11 | **Account bắt buộc.** Guest cũ chỉ giữ lịch sử, không được cấp vé chơi mới hay quản lý phòng. Mất API thì không join mới; Local luyện tập vẫn chạy không cần API. Thay mục P1 "Guest phải chơi được" của audit 01/11 | `Docs/Network_Architecture.vi.md` mục 4 |
| 05/11 | Camera Follow Player phải được trả lại trước khi xong task | `AGENTS.md` |

Nhật ký quyết định từng ticket (ART-04, AUD-01, ADHOC, UI-03, ART-05, UI-02): `Docs/CPO_Runbook.vi.md` mục 4 và mục 4/8 của từng ticket.

## 4. Làn V.Huy: ticket và trạng thái thật

| Ticket | Trạng thái thật | Còn lại |
|---|---|---|
| UI-01 (+P2) | Done 24-25/10 | Release và GC toàn frame chưa đo |
| ART-04 | Done 27/10 | AC14 chuyển ITM-01; AC20, Q11, Q12 là việc tay của Huy |
| AUD-01 | Done 27/10 | Nghe bằng tai (AC-A28) là việc của Huy |
| UI-03 | Done 27/10 | AC39-43 chờ SRV-03 và UI-02 thật |
| ART-05 | G3 có điều kiện 27/10 | Thay `MatchEnd` (F1), dọn Placeholder (F4 = T-002), license một số clip JSurviv chưa kiểm chứng |
| UI-02 | G3 vòng 2 ngày 31/10, ticket vẫn mở | **FAIL tích hợp (audit 01/11):** offline thì Canvas Login che Game; LobbyFlow, GameFlow, OfflineMatchSession không chung một nguồn phase. Phải bám luật account 05/11. AC7/AC8 chờ Vinh, Vũ; lịch sử chờ NET-02 |
| TEL-01 | Spec chờ G1 (= T-001) | Phần server nằm trong `Backend/Telemetry`, vùng Vinh |

## 5. Khoảng trống chặn QA-01, theo PIC người

Đội AI không sửa các vùng này; CPO chỉ báo Huy để chuyển cho từng người.

| Việc | PIC | Tình trạng |
|---|---|---|
| MAP-02: authored map chưa có rương, chum, bụi, portal (audit 01/11) | Quang Huy, Vũ (Level Editor 03/11) | Survival map merge 05/11, chưa ai kiểm lại |
| Game-10: Root/Reverse chưa vào state và packet | Vũ | Chưa xong |
| DRG-01: chưa có lịch 3 mốc, trứng, loot | Vinh | Chưa triển khai |
| ZON-01: code bo r25, 5 HP/s cố định; GDD r36, damage tăng theo phase | Huy chốt luật | Lệch, chưa chốt |
| ART-02: map graybox | Hào | Đổi sang jungle art 03/11, chưa nghiệm thu |
| WPN-03 | Hào | Trên `haonh/dev`, chưa vào main |
| Hai Unity client thật chơi trọn vòng (loot, đánh, HP, loại, kết quả, replay) | QA | Chưa chạy, nên chưa ticket mạng nào ký được |

## 6. Đề xuất task cho BACKLOG (CPO mới đặt mã)

| Task | Vai | Phụ thuộc | Ước lượng | Tiêu chí hoàn thành |
|---|---|---|---|---|
| Audit lại Phase 2 trên main hiện tại, thay bản 01/11 | qa | - | M | Bảng 22 task GĐ2 ĐẠT/chưa đạt có bằng chứng, ghi `Docs/Reports/Phase2_Audit_2026-11-<dd>.vi.md` |
| UI-02 sửa luồng: một nguồn phase/screen, offline không bị Canvas Login che, bám luật account 05/11 | designer cập nhật spec, unity build | audit trên | L | Boot -> StartOffline vào được Game; Login -> phòng -> trận -> kết quả -> về menu chạy được; qa ĐẠT |
| Sửa trạng thái lệch trong `UI-02.md`, `ART-05.md` theo runbook | CPO | - | S | Dòng Trạng thái khớp mục 8 của ticket |
| Bảng luật bo ZON-01 một nguồn (bán kính, damage theo phase, thời lượng) làm đề xuất | designer | - | S | `projects/lastaway/de-xuat/luat-bo.md`; Huy chốt rồi mới lan sang code |
| Trả lời 26 câu luật lõi GDD v0.3 | Huy, designer soạn gọn | - | M | Câu nào Huy chốt thì vào `so-hien-hanh.md` và đổi nhãn trên Sheet |

## 7. Rủi ro nên ghi vào RISKS

| Rủi ro | Giảm thiểu |
|---|---|
| Nếu ưu tiên theo cột Status của GDD thì sẽ giao lại việc đã có code, vì Status chậm hơn code nhiều ngày | Lấy trạng thái từ ticket và audit; CPO nhắc Huy cập nhật Sheet sau mỗi G3 |
| Nếu ai chép key mới từ Sheet v0.3 vào `match.json` thì game từ chối nạp config, vì `BalanceConfigJson` whitelist cứng 33 key (Sheet thêm `zoneDamagePerPhase`, `airdropWarnDuration`, `respawnEnabled`, `botTakeoverDuration`, thiếu `dragonFlyDuration`) | Giữ tên `dragonFlyDuration` trên Sheet, ghi tên mới ở cột Ghi chú; key mới chỉ vào code cùng lúc Huy chốt luật. Đang chờ Huy đồng ý sửa Sheet |
| Nếu hai bên cùng mở Unity Editor hoặc cùng sửa một file thì hỏng scene hoặc mất việc, vì mọi phiên dùng chung `D:\Lastaway` | Ghi `Docs/Agent_Coordination.vi.md` mục 4 trước và sau khi dùng Editor; chỉ giao song song khi không chung file |
| Nếu merge của team ghi đè file ticket thì mất trạng thái nghiệm thu, như nghi ngờ ở commit "Fix conflicts" 01/11 | Sau mỗi lần đồng bộ main, so dòng Trạng thái ticket với runbook |
| Nếu đội AI sửa vùng của Hào, Vinh, Vũ, Quang Huy thì xung đột với người thật | Ranh giới trong `projects/lastaway/CLAUDE.md`; backend chỉ khi Vinh đồng ý |

## 8. Bài học từ bộ máy cũ

- Báo cáo của agent là lời khai: luôn mở file và đếm test thật trước khi chuyển bước (đã bắt được nhiều lần số test lệch).
- Mock và fixture PASS không thay hai Unity client thật; audit 01/11 không ký task mạng nào chỉ từ loopback.
- Codex không tương tác: `--approve-for-me` không đi cùng `-s workspace-write`; lần chạy sau bị bộ lọc quyền chặn, CPO phải tự vẽ lại bằng script.
- Asset world (ART-02, ART-03) thuộc vùng đồng đội, không giao AI ngoài làm.
- Âm thanh thiếu: lấp bằng CC0 (Kenney, OpenGameArt), ghi nguồn ở `Tools/Audio/Sources/`, nhãn "chưa kiểm chứng" cho tới khi Huy nghe.

## 9. Phiên cũ

| Phiên | Đề xuất |
|---|---|
| `Lastaway · Thiết kế & Kế hoạch`, `Lastaway · Unity Dev`, `Lastaway · Review & QA` | Tạo 06/11, chỉ mới đọc file vai, không có việc dở. Lưu trữ được ngay |
| `Lastaway · CPO` (phiên viết file này) | Lưu trữ sau khi CPO mới xác nhận đã nhận bàn giao |
| Các phiên UI-02 cũ (Tech Lead, Unity Dev, Code Reviewer, QA Tester UI-02), Game Designer, Tech Lead, Art Director, UX/UI Designer | Đang offline qua Remote Control, thuộc bộ máy trước 31/10. Huy quyết có lưu trữ không |
