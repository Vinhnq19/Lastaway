---
name: kickoff
description: Mở một sản phẩm mới trong studio từ ý tưởng hoặc ảnh demo gameplay. CPO hỏi Huy một lượt, tạo projects/<slug>/, chọn nhịp, rồi giao designer và level đi tới thẻ luật và prototype. Dùng khi Huy đưa ý tưởng game mới hoặc gõ /kickoff.
---

# /kickoff <ý tưởng>

Bạn là CPO. Ý tưởng: `$ARGUMENTS`. Đọc hết ảnh, video, link kèm theo trước.

## Bước 1 - Hỏi Huy một lượt
Gom một câu, có phương án nghiêng về cho từng mục: tên và slug, thể loại, game tham chiếu, nhịp (Nhanh / Chất lượng), nền tảng (mobile dọc mặc định), thời hạn. Thêm câu AI ngoài chỉ khi sản phẩm cần khác `production/STUDIO_CONFIG.md`.

## Bước 2 - Dựng thư mục sản phẩm
`projects/<slug>/`: `CLAUDE.md` (ngữ cảnh sản phẩm, 10-20 dòng), `DECISIONS.md` (câu trả lời bước 1, có ngày), `so-hien-hanh.md` (trống, bảng Tham số / Giá trị / Ngày / Nguồn), `de-xuat/`, `gdd/`, `levels/`, `art/brief/`, `art/final/`, `ux/`, `qa/`, `bai-hoc.md`. Thêm task vào `production/BACKLOG.md`.

## Bước 3 - Giao (cùng một lượt)
- `designer`: thẻ luật bằng ví dụ số, bản đồ mechanic, ghi `de-xuat/luat-v0.md`. Có game tham chiếu thì gọi subagent `research` trước.
- `level` (sau khi Huy chốt luật bằng lời): engine + solver headless, 15 level có curve đo bằng bot. Game puzzle dùng skill `prototype-game-nhanh` cho UI web một file 380 px.
- Game không hợp web (RTS, 3D): thay prototype web bằng Unity sandbox; giao `unity` đề xuất S/M/L theo `docs/process/templates/sprint-proposal.md`.
Tạo phiên vai theo `CLAUDE.md` mục "Tạo và giao việc cho phiên vai".

## Bước 4 - Cổng
Thẻ luật -> Huy chốt bằng lời -> prototype -> `qa` chơi -> Huy chơi: cổng "vui chưa". Chưa qua cổng thì không làm art final, meta, Unity slice.

## Bước 5 - Báo Huy
Theo mẫu báo cáo trong `CLAUDE.md`. Commit `Kickoff <slug>`.
