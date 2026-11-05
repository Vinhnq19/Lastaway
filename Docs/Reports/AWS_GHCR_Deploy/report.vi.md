# Pipeline AWS qua GHCR private — Local đã đạt, chờ commit/publish

## 1. Phạm vi

Thêm PrepareOnly/Deploy/Rollback, image pin digest, update riêng service và giữ Mongo/secret/outbox/map. Không tự Start/Stop EC2, nâng máy, tạo instance hoặc push Git. **Chưa nghiệm thu toàn pipeline AWS.**

## 2. Code đã triển khai

- Fingerprint API/GameServer theo code compile, dependency, Dockerfile và base image digest; reuse digest cho phần không đổi.
- Manifest đầy đủ hai service, checksum và bundle nhỏ không image archive/DB/secret.
- Internal deployment-status qua queue có giới hạn; kiểm active/pending/grace/bot/entity/phase.
- Root transaction giữ flock qua pull, switch và kiểm từ PC; systemd guard sống qua mất SSH/PC, tự rollback service khi thiếu acceptance.
- Kiểm đúng instance, DNS/TLS, architecture/digest, đĩa, health và giữ container không đổi. Không restart Mongo trong luồng mới; rollback đọc đúng base-images.env của legacy apps-only.
- Gate Full có artifact Windows đúng nguồn, WSS 300s, Auto hai client, UDP thật và server metrics; Local tạm giới hạn 256MiB rồi trả cấu hình dev.

## 3. Setup GHCR

Đã quan sát PC đăng nhập trong Docker Desktop credential store, đúng owner và chỉ package scopes. EC2 có token riêng **chỉ read:packages**, root config **600**, thư mục **700**. Không lưu hoặc in token trong repo/report. Antigravity dừng tại GitHub Confirm access và thiếu browser evaluate; người dùng đã tự xác thực/nhập token trực tiếp. Chưa ghi PASS cho package publish/pull thực từ registry.

## 4. Kiểm thử Local

Backend build 0 lỗi/0 warning. Pipeline 19 tests, NetworkChecks148, BotChecks187 và WorldChecks168 PASS. DualTransport, MapCatalog, MatchResult, Combat, Inventory và MatchChecks PASS. Hai fixture cũ hỏng sau merge do map mặc định đổi; đã pin chúng vào map Di tích cổ để giữ golden test, không đổi map sản xuất.

Windows 6000.3.15f1 đã build thành công. Build hook chụp source trước build, cổng Local/publish đối chiếu khi Editor trả các thiết lập tạm; không stamp EXE cũ. Build mới đã gồm sửa test runner: đủ roster/network view thay vì đòi cả map nằm trên viewport Follow Player; ghi riêng số view trên màn hình và call site của disconnect.

Lượt Local WSS đầu chỉ đạt75,11s rồi FAIL `UserRequested`, **không tính thành300s PASS**. Login/JWT/tạo phòng PASS, WSS có74/74pong, timeout0; RTTp50=33,35ms/p95=34,13ms/p99=66,99ms; input29,86Hz/snapshot30,00Hz. Việc đủ10 view chưa được assertion cũ xác nhận do chỉ2 view cùng nằm trong camera. Bằng chứng thất bại giữ trong evidence.zip; không dùng lại số liệu75s làm nghiệm thu hiệu năng.

Lượt mới dùng thư mục bằng chứng riêng đã **PASS Full Local**: WSS1người+9bot đủ300.01s,300/300pong,timeout0,input29.91Hz,snapshot30.00Hz. RTTp50/p95/p99=33.34/33.52/33.93ms. Đủ10roster và10networkview, tối đa3cùng trên viewport Follow Player. Trận có loại nhân vật/spectator; không diễn giải thành10nhân vật sống và chạyAI suốt300s.

Auto2client+4bot PASS, cả hai fallbackWSS sau UDP thử7779, process exit0; kết quả/history/replay PASS. Smoke UDP7777 và WSS riêng PASS. Server256MiB: ticktrungbình29.942Hz,0dropped/restart/OOM; CPUmộtcore cao nhất22.18%, RAMtheoDockerstats cao nhất23.50%. Đây là MemPerc CLI Docker (working set, có loại cache), không phải số đo RSS hoặc mọi trang cache của cgroup. Cấu hình GameServer dev thường được khôi phục sau test. Lượt75s cũ vẫn giữ FAIL, không đổi kết quả cũ; UserRequested không tái hiện trong lượt300s mới.

## 5. AWS và thời gian/byte truyền

EC2 quan sát Running tại13.239.10.69. Tác vụ chỉ kiểm tra trạng thái và setup quyền đọc registry; chưa deploy/start/stop. First/small/no-change GHCR timings và traffic thực **NOT_TESTED/NOT_MEASURED**. Layer compressed delta được tool ghi riêng, không coi là traffic TLS chính xác. Rollback fault-injection PASS ở module; rollback AWS thật chưa kiểm.

## 6. Cổng còn lại

Windows rebuild và Full Local đã đạt. Lượt retry từng bị chặn bởi thư mục bằng chứng đã tồn tại; đã thêm chặn sớm và dùng thư mục mới, không coi dữ liệu cũ là lượt mới. Review rồi được phép commit để working tree sạch theo kế hoạch đã chốt; tool không tự commit. Sau đó mới publish, đo và chạy một cửa sổ AWS tối đa20phút, giữ Running. Hai PC vật lý vẫn NOT_TESTED.

## 7. Bàn giao hiện tại

[Hướng dẫn setup/deploy](../../AWS_GHCR_Deploy.vi.md), [Network Architecture](../../Network_Architecture.vi.md). Camera scene giữ Follow Player; spectator không đổi. Không phát hành build cũ/failed làm bằng chứng mới. EC2 vẫn phát sinh credits khi Running; chưa có hành động tự tắt máy trong pipeline.
