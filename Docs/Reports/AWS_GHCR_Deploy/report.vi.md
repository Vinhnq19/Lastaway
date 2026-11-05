# Pipeline AWS qua GHCR private — release sửa lỗi đã sẵn sàng

## 1. Phạm vi và kết luận hiện tại

Đã commit pipeline, publish hai package private và hoàn tất Full Local. **Chưa nghiệm thu deploy GHCR trên AWS**: lượt đầu bị guard checksum chặn trước khi thay service. Đã sửa nguyên nhân, thêm test tái hiện, chạy lại Full Local và publish release sửa lỗi `ghcr-20261105-100410-d81eb2b6`. Đang xin lượt AWS mới tối đa20phút theo giới hạn kế hoạch; không tự kéo dài cửa sổ cũ. Không push Git, Start/Stop hoặc nâng EC2.

## 2. Những gì đã lập trình

PrepareOnly/Deploy/Rollback; fingerprint riêng API/GameServer và base digest LinuxAMD64; manifest đầy đủ hai image pin digest; bundle chỉ script/config/checksum, không image archive/secret/DB. EC2 pull trước khi switch, chỉ recreate service thay đổi với `--no-deps`; Mongo không restart. Flock và systemd guard giữ transaction qua kiểm từ PC, thiếu acceptance thì rollback. Deployment-status nội bộ đọc snapshot qua queue, bảo vệ secret và từ chối GameServer còn participant/trận.

Sửa hai fixture map cũ sau merge để chọn Di tích cổ; giữ golden/collision assertions. Build receipt và gate đối chiếu source/binary6000.3.15f1. Runner đếm roster/network view riêng với viewport Follow Player. Swagger sửa mô tả Guest: chỉ đọc lịch sử cũ, không lấy vé chơi/quản lý phòng.

## 3. GHCR và đóng gói

PC dùng Docker Desktop credential store, PAT package scopes; EC2 token riêng chỉ read:packages, thư mục700/file600. Người dùng tự xác thực/nhập token; không đưa token vào chat hoặc repository. Hai package đã quan sát **Private** và liên kết Vinhnq19/Lastaway qua browser, rồi kiểm lại GitHub API. Publish digest thực PASS.

Lần publish khởi tạo được đo sau khi layer đã ấm cache: API7,624s, GameServer7,198s; không coi đây là cold-push benchmark. Bản sửa Swagger riêng API: build/push22,226s, GameServer reuse5,701s, deltaAPI3.798.597byte compressed OCI. Các số này là bước service, không bao gồm toàn bộ hồi quy. Bản sửa script checksum tái sử dụng cả hai image; xem manifest/JSON cho digest. Compressed layer delta **không phải** traffic mạng thực; wire bytes chưa đo. Không dùng `latest` để deploy.

## 4. Kiểm thử Local

Backend build0lỗi/0warning; **20 pipeline tests** PASS, gồm subprocess import bundle đã seal mà không được tạo bytecode. Network148, Bot187, World168 assertions PASS; DualTransport/MapCatalog/MatchResult/Combat/Inventory/MatchChecks PASS. Windows6000.3.15f1 đã build thành công192,63s,0error/6warning; build hiện tại được đối chiếu receipt và hash. Không đổi Editor.

Lượt sửa checksum mới: WSS1người+9bot **401.86s**, 402/402pong, timeout0; RTTp50/p95/p99=33.35/33.76/34.17ms. Input29.86Hz trong 51.51s có điều khiển, snapshot30.00Hz; đủ10roster/view, tối đa4trên viewport. Có bị loại/spectator, kết quả, lịch sử và chơi lại; không suy rằng cả10nhân vật đều sống/đánh nhau suốt402giây. Auto2client+4bot, fallback sau UDP không listener, kết quả/history/replay PASS. UDP7777/WSS smoke riêng PASS.

Server256MiB: 101cửa sổ tick/101mẫu RAM; ticktrungbình29.924Hz, CPUmộtcore max23.00%, DockerMemPerc max22.33%,0dropped/OOM/restart. Docker CLI MemPerc loại inactive file cache, không phải RSS hay tổng mọi trang cgroup. Cấu hình dev được trả memory limit0 như trước. Lượt75s cũ vẫn giữ FAIL trong bằng chứng, không đổi thành PASS.

## 5. Lượt AWS đầu và lỗi đã sửa

EC2 Running13.239.10.69; DNS khớp, HTTPS200. Lượt đầu bị chặn sau53,40s trong entrypoint/76,01s cả wrapper, lỗi `Release checksum/file inventory mismatch`. Python import helper sinh `scripts/__pycache__` trong bundle đã seal; guard từ chối trước pull/switch. Sửa tại owner bằng `sys.dont_write_bytecode = True`, commit`d81eb2b6`, có test subprocess không dùng `-B`. Không bỏ checksum hoặc nới whitelist để che lỗi.

Snapshot trước/sau bằng nhau: release vẫn`bots-20261105-r2`, mọi container ID/config hash/volumeMongo giữ nguyên. Chưa có rollback thực vì chưa thay service. Smoke từ PC trên bản AWS cũ: UDP7777 và WSS443 đều PASS, phòng test đã dọn; không thay bằng generator trong EC2. Tool không tự bật/tắt máy.

## 6. Cổng còn lại và giới hạn

Release sửa lỗi đã sẵn sàng; chờ chấp thuận lượt AWS mới20phút. Sau đó kiểm Full từ PC, cập nhật riêng API, rollback thật, deploy lại cùng release không recreate; ghi thời gian/byte và dữ liệu bảo toàn. Chưa đánh PASS các bước này từ unit test/mock. Hai PC vật lý NOT_TESTED; không suy diễn ping/pong thành sức chứa gameplay vô hạn. Quick không thay nghiệm thu combat/history/performance.

## 7. Bàn giao và vận hành

[Hướng dẫn setup/deploy](../../AWS_GHCR_Deploy.vi.md), [Network Architecture](../../Network_Architecture.vi.md). Các commit đã được người dùng cho phép; không push Git. Camera bàn giao Follow Player, spectator giữ nguyên. Evidence.zip giữ cả lần FAIL và các lần PASS; JSON/CSV/checksum đi kèm. EC2 hiện vẫn Running theo yêu cầu, credits tiếp tục phát sinh. Giữ secret, volume, release hiện tại/bản trước và GHCR package; không prune toàn hệ thống.
