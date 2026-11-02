# Phase 2 — kiểm thử tài khoản và mạng Local/AWS, 02/11/2026

**PASS luồng mạng được đo trên bản build mới Unity6000.3.15f1.** Local6/6 và AWS6/6 client đều hoàn tất kịch bản, đủ tám quan sát và exit0. Auto fallback AWS1/1 PASS. Chưa chứng minh đã vá lỗi native của engine hoặc hoàn chỉnh mọi gameplay PvP.

## 1. Phạm vi và tiêu chí

Đăng nhập → tạo/vào phòng → Lobby/Loading → movement/bo/HP → kết quả/history → replay. Giữ10 người/phòng,5 phòng,50 phiên; LAN/online cần account-ticket v4. Offline một người không cần API/Docker. DRG-01,ART-06,Game-10 ngoài phạm vi. Các test lần này là một PC nhiều process, hai PC thật NOT_TESTED.

## 2. Những thay đổi đã thực hiện

- Chặn callback đăng nhập/đăng ký đến muộn sau logout/expiry; phản hồi cũ không khôi phục token hoặc ghi đè yêu cầu mới. Token đã hết hạn bị từ chối.
- Đổi API giữa AWS và tự host sẽ xóa token trong RAM và logout; endpoint không đổi hoặc lưu lỗi giữ phiên. Không chuyển JWT sang backend khác.
- Sửa driver kiểm thử: tính ba giây di chuyển bằng thời gian thực sự phát input sau world readiness; reset mốc vị trí mỗi MatchId. Spawn/replay không được dùng làm bằng chứng di chuyển.
- Giữ sửa ACK mới nhất mỗi frame, lỗi transport gốc, UI debug chỉ bật F3, authored map215 vật thể và audio đã tích hợp từ lượt trước.
- Build mới từ source hiện tại, đóng gói/deploy backend aws-account-20261102; giữ secret, Mongo/Caddy và volume cũ. Không đổi Editor hoặc protocol để né kiểm account.

## 3. Môi trường và nguồn bằng chứng

Unity6000.3.15f1/URP trên PC Ryzen7 4800H,16GiB RAM. BuildPipeline báo Succeeded. Nhánh codex/phase2-account-flow-20261101; baseline lượt này0f069bdd. Local Docker test2 người tăng tốc trước, sau đó main6 người theo luật thật, GameServer hard cap256MiB. AWS t3.small Sydney, CPU Standard, HTTPS certificate hợp lệ, public IP13.239.10.69. Client chạy từ PC thật, không nằm trong EC2.

## 4. Kết quả đã quan sát

| Hạng mục | Kết quả | Bằng chứng |
|---|---|---|
| AccountSession/chuyển endpoint | PASS24/24 | Unity EditMode, có ca callback cũ/logout và đổi origin |
| Hai client Local | PASS2/2 | UDP+WS, đủ8 quan sát, exit0 |
| Sáu client Local | PASS6/6 | 3WS+3UDP,300–311s, đủ8 quan sát, exit0 |
| Server Local256MiB | PASS | 30,0018Hz; CPU peak15,4% một core; cgroup peak22,783%;0 dropped tick |
| Sáu client AWS | PASS6/6 | 3WSS+3UDP, mỗi client300s, đủ8 quan sát, exit0 |
| Server AWS | PASS | 29,9997Hz; CPU peak14,39% một core; RAM peak lấy mẫu24,81%;0 dropped tick; không OOM/restart |
| UDP timeout → WSS | PASS1/1 | UDP7779 không có listener; fallbackReason ConnectTimeout; WSS join/pong; exit0 |
| JWT/room policy | PASS trong launcher thật | Thiếu/sai JWT hoặc Guest bị từ chối; register/login, JWT tạo phòng, mã6 ký tự |
| Hai PC vật lý | NOT_TESTED | Cần checklist LAN/VPN/AWS trên hai thiết bị |

RTT p50/p95/p99, tốc độ input/snapshot từng client ở phase2-checks.csv và report.json. WAN Sydney có RTT hàng trăm ms, không áp ngưỡng50ms Local. Năm phút là thời gian kết nối; nhân vật có thể bị loại/spectator trong trận, tốc độ input chỉ tính các khoảng còn sống/được phép gửi. Bài đo không khẳng định mỗi nhân vật phát input liên tục300s hoặc tìm giới hạn gameplay.

## 5. Vấn đề mạng và lỗi còn lại

Không tái hiện disconnect WSS hoặc crash native trong các lượt build mới kể trên; tất cả14 client gameplay và1 client fallback đều exit0 (2+6+6+1=15 process).

**Lỗi engine vẫn BLOCKED theo điều kiện UI Automation:** crash cũ UnityPlayer.dll0xc29819 có stack PlatformAccessibilityManager::StaticDestroy. Unity ghi UUM-146676 đã sửa ở6000.3.21f1. Giữ15f1 nên không tuyên bố đã vá native; không thêm Process.Kill, không tắt accessibility, không bỏ kiểm exit. Mono startup0x26bef6 trước đây chưa tìm nguyên nhân cuối, không tái hiện trong lượt mới.

PASS module/full-suite854EditMode và120PlayMode từ lượt trước vẫn là bằng chứng lịch sử; lượt này chạy24 test tập trung, không ghi thành full-suite mới. Tranh loot/PvP/audio hai người thật và đầy đủ luật bo theo thời gian đều cần nghiệm thu riêng. Không tự ký DONE các task ngoài phạm vi.

## 6. Phương án và quyền vận hành

AWS đang dùng release aws-account-20261102, SHA256 f4603ef871003ba3f3c06ea584d1221dce9e71ae319b963f812ccaeddd425e77. EC2 giữ Running theo yêu cầu; credits tiếp tục phát sinh. Không Stop/Start, nâng instance hoặc cài watchdog tắt EC2 trong lượt này. Room test đã xóa; client đã đóng. Local Docker dừng và trả compose về cấu hình gốc, giữ dữ liệu.

Chạy build: chọn AWS trong cấu hình máy chủ, đăng ký/đăng nhập, tạo phòng hoặc nhập RoomCode; người khác dùng cùng backend và mã phòng. Selfhost/LAN/VPN phải có IP host và API HTTPS hợp lệ của chính backend đó; đổi backend yêu cầu đăng nhập lại. Offline chọn Luyện tập để vào Game ngay. Hướng dẫn chi tiết ở Docs/Phase2_Match_Guest_Zone.vi.md và checklist hai PC hiện có.

## 7. Bằng chứng, tài liệu và dọn dẹp

Gói evidence mới chứa JSON/CSV/ảnh/log của Local2,Local6,AWS6 và Auto; có checksum và kiểm CRC trước khi xóa raw trùng. Giữ test source/launcher tái sử dụng, build mới, source/.meta/.env, DB/outbox và rollback. Chỉ dọn output thuộc lượt này.

Docker đã xóa image API aws-rooms-20261016 và12,46MB cache cũ. Lượt xóa thêm hai image AWS ngày01/11 bị bộ duyệt tự động từ chối vì khả năng cần rollback; hai image được giữ. Không xóa volume Mongo.

Cập nhật menu sau lượt AWS ngày02/11: chỉ hiện năm môi trường, ẩn alias cũ và nguồn user:client.json khỏi dropdown. ClientConfigTests20/20 PASS, đã kiểm danh sách runtime trong Unity, Windows build0errors. Bằng chứng AWS trong gói zip thuộc binary trước thay đổi UI này; không chạy lại bài mạng cho bản UI mới.
