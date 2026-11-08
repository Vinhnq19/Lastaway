# Phát hành Windows/AWS — tiếp tục ngày 09/11/2026

**Trạng thái: PARTIAL. Build Windows Unity21 và deploy backend AWS Full đã PASS. AWS còn catalog map cũ; gói Tháp cổ/Thị trấn đã sẵn sàng Local, chờ cửa sổ AWS bổ sung để publish và nghiệm thu.**

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

**Window2 INVALID:** client quality không xuất evidence, bị watchdog dừng; không có mẫu FPS AWS300s hợp lệ. Ảnh lobby đã thu trong window1 xác nhận AWS đang phát hành **Di tích cổ/Bãi đá v1.0.0**, không phải hai map dự kiến. Mình đã bỏ sót đối chiếu catalog trước window2. Runtime map nằm ở volume ngoài image nên cập nhật image không tự thay catalog. Bài yêu cầu `thap-co` không hợp lệ trên catalog này; không quy lượt thiếu telemetry thành server yếu hoặc FPS PASS. Thị trấn online chưa nghiệm thu.

## 5. Gói map và phần còn lại

Gói Local111.334bytes, source catalog generation5: **Tháp cổ/Thị trấn v1.2.0**. Validator và publisher nguyên tử Local PASS; checksum trong JSON/evidence. Cần dùng CLI --maps publish/reload nội bộ để cập nhật volume, giữ payload/history cũ để rollback; không cần rebuild image hoặc restart Mongo/EC2. Generation thực sẽ tăng theo catalog đang chạy.

Cần cửa sổ bổ sung tối đa20 phút: publish gói đã validate, xác minh GET/api/maps sau JWT, đo hai map720p/60FPS tối thiểu300s và dọn test. Hai window trước đã dùng; theo kế hoạch “không tự mở lượt thứ ba”, đang chờ phê duyệt. Hai PC vật lý,6–10 người thật, nghe audio/cảm giác PvP và correction/snapshot-gap tail đầy đủ vẫn NOT_TESTED/NOT_MEASURED. Module test không thay nghiệm thu flow hai map mới trên AWS.

## 6. Vận hành và chi phí

Endpoint game `junglemaze.duckdns.org`, API HTTPS443, UDP7777, WSS`/game`. Account bắt buộc; chọn Online AWS rồi đăng nhập, tạo/vào phòng. Hiện chưa khẳng định catalog mới đã lên AWS. Giữ Follow Player và spectator theo trạng thái thật.

EC2 được xác nhận **Running**,t3.small,x86_64,Standard; tác vụ không Start/Stop/nâng máy hoặc bật Unlimited. Running tiếp tục phát sinh chi phí/credits AWS. Không đổi Mongo, secret, security group hoặc tạo hạ tầng. Window2 tool/sampler được dọn trong1.067,25s; không chạy thêm bài tải sau cửa sổ. OCI layer delta là số nén theo manifest, không phải đo wire bytes thực.

## 7. Bằng chứng và bàn giao

report.json/measurements.csv/evidence.zip/SHA256SUMS cùng thư mục. Giữ cả các lượt FAIL/INVALID, probe UIA/exit0, báo cáo test mới, manifest/digest, server metrics và gói map/checksum. Không đưa .env, credential/token, dump bộ nhớ hoặc ảnh lobby chứa RoomCode vào gói bằng chứng. Ảnh gameplay được phân biệt rõ map/catalog đang chạy. Báo cáo15f1 trước đây nằm trong phần history của evidence; số cũ không được dùng làm PASS release21.
