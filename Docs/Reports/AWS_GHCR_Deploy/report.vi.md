# AWS GHCR — WSS đã có bằng chứng PASS, gate reporter lỗi và rollback đã kiểm chứng

## 1. Kết luận hiện tại

**Pipeline chưa hoàn tất nghiệm thu.** Hai package GHCR private đã publish/pull thực. Lượt AWS mới đã chạy client Unity từ PC qua WSS đủ300giây và có kết quả/history/replay, nhưng reporter PowerShell lỗi nên gate từ chối release và tự rollback. AWS đã trở về`bots-20261105-r2`, giữ Running; không tự Start/Stop hoặc nâng máy. Cửa sổ được duyệt20phút đã kết thúc; công việc tiếp theo hiện chỉ Local.

## 2. Code đã triển khai và gia cố

PrepareOnly/Deploy/Rollback, fingerprint riêng API/GameServer/base LinuxAMD64, manifest đủ hai image theo digest, checksum và bundle nhỏ không image archive/DB/secret. Pull trước switch; chỉ recreate service thay đổi, Mongo không restart. Flock/systemd guard giữ transaction qua kiểm từ PC; mất acceptance thì rollback. Endpoint nội bộ đọc snapshot qua queue, secret riêng, kiểm human/pending/grace/bot/entity/phase.

Đã sửa bundle không được sinh`__pycache__`; có test subprocess tái hiện. Reporter mới tính hash bằng .NET, giữ CommandInfo từ trước khi mở player, đối chiếu build trước/sau, ghi tên lệnh thiếu/vị trí script an toàn. Root lưu`failureMetrics` trước recreate để giữ log candidate. **Chưa khẳng định đã tìm được chính xác command gây lỗi AWS**: reporter cũ chỉ giữ loại exception, Local ngắn không tái hiện. Các thay đổi mới đã PASS Full Local; vẫn cần thử lại AWS để xác nhận lỗi không tái hiện.

## 3. GHCR và thời gian chuẩn bị

PC dùng Docker Desktop credential store; token EC2 riêng chỉ read:packages, thư mục700/file600. Người dùng tự xác thực/nhập token. Hai package quan sát Private, liên kết Vinhnq19/Lastaway, rồi kiểm lại qua GitHub API. Không in token hoặc đưa token vào repo/report.

Publish khởi tạo đo khi layer đã ấm cache: API7,624s/GameServer7,198s, không gọi là cold-push. Bản API riêng sửa mô tả Guest:22,226s, GameServer reuse5,701s, deltaAPI3.798.597byte compressed OCI. Bản tiêu đề map API:19,253s, GameServer reuse5,519s, delta3.798.591byte. Đây là bước service, không gồm toàn bộ hồi quy; chuẩn bị bản sửa checksum toàn lệnh127,71s, API nhỏ cuối114,14s. EC2 pull thực hai image mới mất**6,262s**. Các layer này không phải traffic TLS/wire bytes; wire chưa đo. Chưa đạt bước API-only trên AWS để kết luận thời gian deploy nhỏ.

## 4. Local và build

Backend0error/0warning, Network148/Bot187/World168 assertions PASS; DualTransport/MapCatalog/MatchResult/Combat/Inventory/MatchChecks PASS. Pipeline hiện**22 tests**, reporter**37 kiểm launcher bằng fixture** PASS, gồm hash vector độc lập, tắt cmdlet autoload, xóa public function binding nhưng cached command vẫn chạy, và không nhận crash/thiếu evidence thành PASS. Hai fixture map cũ đã pin Di tích cổ sau merge để giữ golden/collision test.

Build6000.3.15f1 đã thành công192,63s,0error/6warning; receipt/source/binary hash được kiểm, không đổi Editor. Full Local bản sửa checksum trước đó PASS: WSS401,86s,402/402pong, RTT33,35/33,76/34,17ms p50/p95/p99, input29,86Hz trong51,51s điều khiển và snapshot30,00Hz; có spectator, không suy10nhân vật đều sống suốt402s. Auto2+4bot, kết quả/history/replay và UDP/WSS smoke riêng PASS. Server256MiB: tick29,924Hz, CPUmax23%, DockerMemPerc22,33%,0drop/OOM/restart,101cửa sổ/101mẫu RAM. Docker CLI MemPerc loại inactive file cache, không phải RSS/tổng cgroup. Giới hạn dev được trả0. **Full Local reporter mới đã PASS**: WSS300.02s, exit0, 300/300pong, RTTp50/p95/p99=33.34/33.71/34.07ms, input29.97Hz/snapshot30.00Hz. Auto2client exit0 và report tổng hợp PASS; UDP/WSS smoke PASS. Server tick29.914Hz, CPUmax24.18%, DockerMemPerc21.86%,0drop/OOM/restart. Cấu hình dev đã khôi phục. Lượt75s cũ giữ FAIL.

Guard cuối so sánh image trong **Compose đã resolve** với manifest, chặn `.env.aws`/config ghi đè digest. Cấu hình đúng và hai trường hợp API/GameServer bị ghi đè đã qua unit test. Full Local cuối: WSS300.03s exit0, 300/301pong, input29.97Hz, snapshot30.00Hz; Auto hai client khoảng96,9s exit0 và UDP/WSS smoke PASS. Server89cửa sổ/89mẫu RAM: tick29.944Hz, CPUmax21.60%, DockerMemPerc22.20%,0drop/OOM/restart; cấu hình dev đã khôi phục.

## 5. Hai lượt AWS và dữ liệu từ PC

Lượt đầu bị checksum chặn trước thay service: Python import tạo bytecode trong bundle; đã sửa, không nới checksum. Lượt mới readiness PASS, thay đúngAPI/GameServer, Mongo/Caddy giữ nguyên. Flock Linux thực chặn admin thứ hai; snapshot lúc InGame báo1human+9bot,10entity,`safeToDeploy=false`.

Client evidence WSS mới PASS**300.01s**, 300/300pong, timeout0; RTTp50/p95/p99=266.70/299.96/400.12ms. Input29.97Hz trên272.01s active, snapshot29.97Hz;10roster/view, tối đa5trên viewport Follow Player,2trận hoàn tất và các assertion movement/zone/HP/result/history/replay PASS. Đây là WAN Sydney, không áp50ms Local.

Sau player, reporter gặp`System.Management.Automation.CommandNotFoundException`; thiếu `players/report.json`, nên **exit code chưa có bằng chứng tổng hợp**. Log có shutdown bình thường/không có native crash marker nhưng không dùng điều đó để tự gán exit0. Gate bị FAIL, không đánh Full deployment PASS chỉ từ JSON client. Candidate có65mẫu RAM, max24,83%; tick/CPU không thu kịp trước rollback xóa container log, ghi **INCOMPLETE**, không lấy số của container cũ thay thế. Reporter mới sẽ lưu trước rollback.

## 6. Rollback, dữ liệu và các cổng còn lại

Tự rollback thật đã PASS: image API/GameServer khôi phục đúng, current/previous trở về bản cũ, config hash/Mongo ID/volume/Caddy ID không đổi. Smoke lại từ PC sau rollback PASS: HTTPS/JWT, tạo/list/xóa phòng, UDP7777 và WSS443 trong21,12s; phòng/client test đã dọn. Không biến rollback do PC gate lỗi thành bài cố ý healthcheck failure; bài riêng đó chưa chạy.

Controller chính kết thúc sau466,80s; tính tới lần can thiệp AWS cuối là**1011.18s**, trong1200s đã duyệt. Các bước API-only update, explicit registry-to-registry rollback, deploy lại cùng release và Auto2 AWS **chưa chạy** vì gate đầu thất bại. Fresh Full Local/report fix và guard actual-image đã đạt; cần lượt AWS mới được duyệt để kiểm release cuối. Hai PC vật lý NOT_TESTED; không suy diễn số đo thành gameplay capacity vô hạn. Quick không thay combat/history/performance.

Release retry `ghcr-20261105-111555-b3be0ead` đã chuẩn bị xong từ commit `b3be0ead`, reuse hai image và có Full Local mới khớp. Người dùng đã duyệt thêm một lượt AWS tối đa20phút; chưa đánh dấu nghiệm thu trước khi thực hiện.

## 7. Bàn giao và vận hành

[Hướng dẫn GHCR](../../AWS_GHCR_Deploy.vi.md), [Network Architecture](../../Network_Architecture.vi.md). Một báo cáo JSON/CSV/evidence.zip+SHA256 giữ cả FAIL và PASS. Các commit trước đã được người dùng duyệt; không push Git. Camera Follow Player và spectator giữ đúng; build client không đổi gameplay trong lượt này. EC2 giữ Running theo lựa chọn, credits tiếp tục phát sinh. Giữ secret, Mongo/outbox/map volumes và image/release phục hồi; không prune toàn hệ thống.
