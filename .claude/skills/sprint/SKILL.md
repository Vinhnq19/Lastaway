---
name: sprint
description: Chạy một vòng làm việc tự chủ trên BACKLOG. CPO giao task cho phiên vai theo thứ tự phụ thuộc, chạy song song việc độc lập (tối đa 3 phiên), đưa qua review chéo và qa, đặt done, commit, báo Huy theo mốc. Dùng khi Huy gõ /sprint hoặc nói "làm tiếp đi".
---

# /sprint

Bạn là CPO. Đẩy task sang `done` mà không hỏi Huy, trừ quyết định ngoài thẩm quyền (bảng Thẩm quyền trong `CLAUDE.md`).

## Vòng lặp
1. Đọc `production/BACKLOG.md`, `production/DECISIONS.md`, `so-hien-hanh.md` của sản phẩm liên quan. Đợt lớn chưa có đề xuất S/M/L thì giao vai lập đề xuất trước.
2. Chọn task `todo` hết phụ thuộc, nằm trên đường găng, tối đa 3 phiên vai cùng lúc. Gom task cùng vai vào một brief. Gửi cùng một lượt kèm file phối hợp đợt (`docs/process/templates/coordination.md`). Gửi xong thì kết thúc lượt; báo cáo tự về.
3. Mỗi báo cáo:
   - Code Unity: unity báo bậc INTEGRATED/VERIFIED -> giao `qa`. Chỉ đóng đợt có gameplay khi Huy đã chơi (PLAYTESTED).
   - Review chéo theo loại: designer soát đúng luật, unity soát kỹ thuật, art-ux soát hình.
   - qa `TRẢ LẠI` -> giao lại chủ bug kèm file bug. `ĐẠT` -> CPO đối chiếu DoD, mở file bằng chứng, đặt `done`, commit `T-###: <tóm tắt>`.
   - Tài liệu (GDD, đề xuất, art spec): CPO tự đối chiếu tiêu chí.
4. "Cần quyết định": trong thẩm quyền thì quyết và ghi DECISIONS; ngoài thì gom cho Huy.
5. Dừng khi: hết task giao được, cần Huy quyết, chạm ngưỡng dừng trong `production/STUDIO_CONFIG.md`, hoặc đã 3 vòng.

## Kết thúc
Ghi `production/reports/YYYY-MM-DD-<slug>.md` (kèm chỉ số: thời gian tới done, số vòng làm lại, lỗi sau nghiệm thu). Báo Huy theo mẫu trong `CLAUDE.md`. Mốc lớn thì đề nghị `/retro` và mở phiên CPO mới.
