# Phát hành Windows/AWS — tiếp tục ngày 09/11/2026

**Trạng thái: PARTIAL về chất lượng; phát hành/flow AWS PASS. Build Windows21 và backend Full PASS; Tháp cổ/Thị trấn v1.2.0 đã publish và kiểm thực từ PC. Frame-time InGame đạt ngưỡng; còn stall khi chờ/tải ban đầu.**

## 1. Phạm vi và phiên bản

Source `85b9768b632775736974883870c17d9956c6f98a`, nhánh `codex/aws-quality-release`; không merge main hoặc push Git. Riêng máy Vinh/build này dùng Unity6000.3.21f1; Git vẫn pin6000.3.15f1 và giữ package lock của team. Hai file override dùng skip-worktree tại checkout này.

Bản Windows đầy đủ: `D:/Unity/Lastaway/Builds/AWS-Quality-6000.3.21f1/` (giữ EXE, _Data, MonoBleedingEdge, DLL; không đóng ZIP game). Build Release Mono: 354,95s, Unity báo152,06MiB, 0 error/5 warning. Log có cảnh báo shader Terrain3D bị strip trong pipeline2D và legacy/imported code; không đổi renderer để né cảnh báo. Receipt/source/player fingerprint đã đối chiếu sau khi khôi phục ba field iOS Unity tự thêm.

## 2. Công việc và sửa lỗi

- Giữ pin/lock21 chỉ cục bộ, không ép dev khác nâng Editor; gate đọc pin thực và từ chối receipt sai Editor.
- Hoàn thiện logo LAST AWAY/prefab/builder. PlayMode phát hiện transparent rect của logo đăng nhập vượt mép khoảng3px; đã dịch xuống10 design pixels.
- Chuẩn hóa HUD PISTOL/ASSAULT_RIFLE/SNIPER_RIFLE theo lưới32→64, palette/outline/padding chung; cập nhật catalog và inventory từ34→37. Không sửa art vũ khí cầm trên tay. Fixture audio đổi từ46 sang47 clip vì đã có menu loop thứ tư.
- Engine15 từng crash native khi đóng. Build21 đã qua probe UI Automation + đóng menu bình thường và nhiều lượt gameplay/Auto exit0; không dùng kill hoặc bỏ kiểm exit để nhận PASS.
- Bổ sung preflight trong runner chung: map yêu cầu phải tồn tại trong catalog JWT trước khi tạo phòng/chạy client. Guard từ chối map không có ngay; đường map hợp lệ PASS Local.

## 3. Kiểm thử Local

| Nhóm | Kết quả |
|---|---|
| EditMode | 1.183/1.183 PASS; lượt đầu có4 lỗi art/audio, đã sửa |
| PlayMode | 207/207 PASS; lượt đầu206/207 do logo overflow |
| Backend regression | 2.539 check PASS theo chín nhóm hiện có |
| Pipeline / icon native32 | 28/28 và44/44 PASS |
| Local Full r1 | FAIL hiệu năng:2 dropped tick, loop gap211,904ms; không phải native crash |
| Local Full r2 cô lập | PASS: WSS300s, Auto hai client, UDP, result/history/replay; mọi process exit0 |
| R2 server | 92 mẫu tick,93 mẫu RAM; mean29,912Hz, CPUmax37,19% một core, RAMpeak27,54%/256MiB;0 dropped/OOM/restart |

Không khẳng định tuyệt đối nguyên nhân loop gap r1: simulation/poll nhỏ, CPUmax24%, nhưng có một khoảng gián đoạn giữa vòng lặp. R2 dùng cache đã warm và Editor được thu nhỏ; giữ nguyên ngưỡng. Các lượt ngắn quality Local ghi được720p/frame-time khoảng58FPS trong InGame, nhưng chỉ15s kết nối, không thay bằng bài300s.

## 4. AWS — deploy và chất lượng

Release `ghcr-20261108-184421-85b9768b`; API/GameServer được cập nhật theo digest, Mongo/volume/secret và release trước được giữ. Window1 PASS Full trong686,30s. WSS PC→EC2 kết nối405,82s, input29,71Hz, snapshot30Hz, RTTp50/p95/p99≈233,35/233,62/235,08ms,0 pong timeout,exit0; Auto hai client PASS. UDP public7777 và WSS smoke đều requested=connected=joined=1,failed=0.

Server AWS:121 mẫu, mean29,9997Hz, CPUmax21,8% một core;118 mẫu RAM,peak25,47%/256MiB;0 dropped/OOM/restart. CPUCreditBalance lúc đầu576; instanceStandard. Đây là đo backend từ PC thật, không phải generator trong EC2; không áp ngưỡng RTT Local cho Sydney.

**Window2 INVALID:** client quality không xuất evidence, bị watchdog dừng; không có mẫu FPS AWS300s hợp lệ. Ảnh lobby đã thu trong window1 xác nhận khi đó AWS phát hành **Di tích cổ v1.1.0/Bãi đá v1.0.0**, không phải hai map dự kiến. Mình đã bỏ sót đối chiếu catalog trước window2. Runtime map nằm ở volume ngoài image nên cập nhật image không tự thay catalog. Bài yêu cầu `thap-co` không hợp lệ trên catalog này; không quy lượt thiếu telemetry thành server yếu hoặc FPS PASS. Tại thời điểm kết thúc window2, Thị trấn online chưa nghiệm thu; kết quả bổ sung nằm ở mục5.

## 5. Hai map mới và lượt AWS bổ sung đã duyệt

Lượt 3 hoàn tất trong 836.81s (<20 phút). Trước publish đã xác nhận server trống, đúng image digest; khóa deploy, validate catalog baked và snapshot catalog cũ. Publish nguyên tử generation5 rồi reload HTTP200; không build/pull image hoặc restart container. Catalog mới: **Tháp cổ/Thị trấn v1.2.0**, đúng MapHash/previewHash đã kiểm Local. Gói chuyển map111.334bytes chỉ là phương án dự phòng; thực tế publish trực tiếp dữ liệu đã có trong image, không upload gói này.

| Map/đường | Thời gian | Input/snapshot | RTT p50/p95/p99 | FPS InGame; frame p95/p99 |
|---|---|---|---|---|
| Tháp cổ/Wss | 330.01s | 29.95/30.00Hz | 239.59/267.10/534.00ms | 59.34; 16.68/17.45ms |
| Thị trấn/Udp | 330.00s | 29.99/29.98Hz | 250.01/266.82/272.36ms | 59.84; 16.67/17.05ms |

Cả hai flow 1 người +9 bot PASS: JWT/ticket guard, map có trong catalog, movement/bo/HP, kết quả/history/replay; client exit0, 1280×720, backgroundFrames0. Số liệu FPS tách theo phase; mức mục tiêu60 không bị ép30. Server 160 mẫu, mean30.0002Hz, CPUmax26.57% một core, RAMpeak47.97%/256MiB; dropped0, OOM=False, restartDelta=0.

**Điểm cần cải thiện:**
- thap-co/Waiting: frame p95=16.75ms, p99=128.02ms; 5 stall≥100ms. Đánh FAIL tiêu chí frame-tail phase này, không gộp với InGame để báo toàn bộ UI mượt.
- thi-tran/Waiting: frame p95=16.77ms, p99=180.02ms; 6 stall≥100ms. Đánh FAIL tiêu chí frame-tail phase này, không gộp với InGame để báo toàn bộ UI mượt.
- Tháp cổ/WSS có RTT p99 tăng rõ so với median. Không có pong timeout; chưa đủ bằng chứng quy nguyên nhân cho server hay TCP/network. Không coi FPS tốt là không có giật do mạng.
- Natural match có người chết/spectator; đây không phải10 participant còn sống liên tục cả5 phút. Không sửa HP hoặc rút lịch chính để giữ người sống. Các mốc bo/rồng chưa quan sát đủ trong trận thật vẫn chưa được nghiệm thu bằng lượt này.
- Hai PC vật lý,6–10 người thật, cảm giác PvP/audio và correction/snapshot-gap tail đầy đủ vẫn NOT_TESTED/NOT_MEASURED.

Catalog mới được giữ; backup catalog cũ và payload bất biến vẫn còn để rollback. Rollback map không được kích hoạt trong lượt thành công này; không tuyên bố đã diễn tập rollback ở đây.

## 6. Vận hành và chi phí

Endpoint game `junglemaze.duckdns.org`, API HTTPS443, UDP7777, WSS`/game`. Account bắt buộc; chọn Online AWS rồi đăng nhập, tạo/vào phòng. Catalog mới đã lên AWS và cả hai map đã qua flow test từ PC thật. Giữ Follow Player và spectator theo trạng thái thật.

EC2 được xác nhận **Running**,t3.small,x86_64,Standard; tác vụ không Start/Stop/nâng máy hoặc bật Unlimited. Running tiếp tục phát sinh chi phí/credits AWS. Giữ cấu hình/volume Mongo, secret và security group; không tạo hạ tầng. Register/login smoke có tạo fixture account/guest qua API; không xóa dữ liệu người dùng. Window2 tool/sampler được dọn trong1.067,25s. Window3 được người dùng duyệt thêm riêng; đã dừng sampler, dọn staging riêng, đóng client/dọn phòng test, xác nhận EC2 Running. Sampler đã tự kết thúc thành công trước lệnh stop nên systemctl trả code5; đã xác minh journal inactive/success và thu đủ144 mẫu, không phải lỗi game. CPUCreditBalance cuối576 như đầu; đây là credit CPU T3, không phải số dư tiền/Free Tier AWS. Không chạy thêm bài tải sau cửa sổ. OCI layer delta là số nén theo manifest, không phải đo wire bytes thực.

## 7. Bằng chứng và bàn giao

report.json/measurements.csv/evidence.zip/SHA256SUMS cùng thư mục. Giữ cả các lượt FAIL/INVALID, probe UIA/exit0, báo cáo test mới, manifest/digest, server metrics và gói map/checksum. Không đưa .env, credential/token, dump bộ nhớ hoặc ảnh lobby chứa RoomCode vào gói bằng chứng. Ảnh gameplay được phân biệt rõ map/catalog đang chạy. Báo cáo15f1 trước đây nằm trong phần history của evidence; số cũ không được dùng làm PASS release21.
