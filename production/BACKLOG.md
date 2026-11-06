# Backlog - Lastaway

Nguồn sự thật duy nhất về việc cần làm. CPO sở hữu cấu trúc; mọi vai cập nhật trạng thái task của mình.

Trạng thái: `todo` · `doing` · `review` · `done` · `blocked` (thêm `loại`, `gộp vào T-###`). Ước lượng: S (một lượt ngắn) · M (một lượt đầy) · L (phải chia nhỏ).

Mã task lớn nhất đã cấp: **T-006**. Task mới lấy số tiếp theo.

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

## Icebox

| ID | Task | Agent | Phụ thuộc | Ước lượng | Trạng thái | Tiêu chí hoàn thành |
|---|---|---|---|---|---|---|
