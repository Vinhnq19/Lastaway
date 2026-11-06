# Mẫu brief giao việc (CPO → agent)

Sao chép nguyên khối dưới vào prompt khi gọi Agent. Điền đủ 5 phần. Đường dẫn tuyệt đối tính từ gốc repo.

```
Task: T-### - <tiêu đề ngắn>

## Mục tiêu
<Một đoạn: làm gì, vì sao, phục vụ ai. Nêu kết quả, không nêu cách làm.>

## Đọc trước
- CLAUDE.md (phần Quy trình) và .claude/agents/_shared-protocol.md
- docs/playbooks/<vai-trò>.md
- production/DECISIONS.md
- <file GDD / ADR / handoff liên quan>

## Ghi vào
- <đường dẫn file đầu ra 1> - <nội dung mong đợi>
- production/BACKLOG.md - đổi trạng thái T-### khi bắt đầu và khi xong

## Tiêu chí hoàn thành
- <kiểm chứng được 1: mở file X thấy Y / chạy lệnh Z pass>
- <kiểm chứng được 2>

## Giới hạn
- Không sửa file ngoài danh sách "Ghi vào" trừ BACKLOG.md.
- Không đổi thiết kế / stack / phạm vi. Mâu thuẫn thì dừng và báo.
- <giới hạn riêng của task>

Kết thúc bằng khối "## Báo cáo cho CPO" theo _shared-protocol.md.
```
