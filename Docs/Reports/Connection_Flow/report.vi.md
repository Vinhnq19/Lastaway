# Làm rõ lỗi và luồng kết nối — 11/11/2026

Đã sửa đủ 7 điểm và hoàn tất cổng Local. Windows Release mới và hai lượt hai client đã PASS. Commit theo yêu cầu tiếp theo của người dùng; không push hoặc thay đổi AWS.

## 1. Lỗi xin quyền vào phòng

Trước: HTTP lỗi, timeout hoặc dữ liệu rỗng bị gộp thành đăng nhập thất bại. Sau: kết quả giữ HTTP status và loại lỗi; chỉ401 của phiên hiện hành yêu cầu login lại.403/404/429/503/mạng/dữ liệu sai và lượt vào phòng hết hạn giữ account. Lỗi nghiệp vụ không đổi transport để thử vượt điều kiện. Retry là lần bấm mới, sinh attempt/vé mới. Hủy bỏ task cũ không mở socket về sau.

## 2. Callback phòng thuộc account cũ

Account có revision; list/create/delete ghi nhận generation, revision và token lúc gửi. Kiểm trước mọi thay đổi busy/message/rows và trước xử lý401. Logout/login/Clear reset model. Đổi máy chủ hủy lượt kết nối đang chờ; đổi đường dẫn backend trên cùng hostname cũng tách phiên. Request đã tới server có thể đã được thực hiện; client không tự gửi lại mutation.

## 3. Validation cục bộ

ConnectionError mang loại lỗi; adapter giữ loại này thay vì biến mọi lỗi thành LoginFailed. Địa chỉ/cổng/trường nhập sai giữ account và hiển thị lỗi form. Không hiện exception/raw response lên UI.

## 4. Reconnect

Thất bại chưa có kết quả chỉ báo chưa xác nhận được trạng thái trận. Proof bị từ chối báo không thể tiếp tục lượt chơi; không tuyên bố đã bị loại. Chỉ LoadingTimeout authoritative mới dùng thông báo tải trận bị loại. Đồng hồ là ước tính. Không đổi grace, proof guard hoặc tự join như nhân vật mới.

## 5. Reason từ GameServer

Mapping rõ GameplayVersionMismatch, InvalidResumeToken, ServerBusy, InvalidTicket/TicketExpired và các reason phòng hiện có. Reason lạ dùng thông báo an toàn, không in enum lên UI. Reason gốc vẫn có trong log mạng.

## 6. Tải bản đồ

HTTP401/403/404 và lỗi mạng/máy chủ có thông báo riêng; chỉ dữ liệu/hash/schema không hợp lệ mới báo dữ liệu bản đồ lỗi.401 map chỉ hết phiên khi revision, backend và token vẫn hiện hành. Giữ cancellation/generation/hash/schema/size/TLS; download lỗi không gửi Ready, không đổi map thay người chơi. Lệnh map/bot bị từ chối cũng được diễn giải bằng tiếng Việt thay vì enum.

## 7. Bảng hạng

Mỗi lượt gắn generation, MatchId và backend; rời màn, Clear hoặc đổi account bỏ phản hồi cũ. Giữ khác biệt giữa kết quả chưa lưu, danh sách rỗng và dịch vụ tạm ngừng.

## UI và kiến trúc

Giữ các owner hiện có; không thêm framework retry/lỗi, không đổi wire protocol, REST endpoint, DB schema hoặc luật game10/5/50. Thông tin kết nối dùng lời phổ thông; Release không mở F3/F8 hay trường API/cổng nâng cao. Dev giữ số đo và log. Prefab chỉ đổi4 nhãn, builder tương ứng được cập nhật; không giữ diff ID được tạo lại hàng loạt.

## Kiểm thử và bằng chứng

- EditMode:1.239/1.239 PASS; PlayMode UI:31/31 PASS bằng Unity 6000.3.21f1.
- NetworkChecks:149 PASS; DualTransportChecks:53 PASS, gồm fallback, giới hạn slot, proof/resume và TLS.
- Regression mới gồm HTTP giả lập401/403/404/429/503, JSON hỏng, callback A sau login B, Clear khi pending, bảng hạng A trả sau B,401 map cũ và thông báo UI trên frame thật.
- Hai lượt MCP PlayMode đầu lỗi trình chạy trước test đầu tiên; không tính PASS. Chạy lại phiên CLI sạch, sửa guard null của ticker fixture và kỳ vọng nhãn cũ; không sửa package test framework. Cài đặt DisableDomainReload của người dùng được trả lại.
- Windows Release mới:PASS, Unity 6000.3.21f1,306,43 MiB,232,1 giây,0 error và42 warning của build/import. Các warning không được coi là đã xử lý hết trong đợt sửa flow.
- Hai client thật trên một PC:PASS cả UDP + WebSocket, cùng thấy movement/bo/HP; đủ 8/8 tiêu chí mỗi client, có kết quả/history/replay. API thật đã kiểm register/login, thiếu/sai JWT, Guest không được tạo phòng/lấy vé và account tạo phòng thành công.
- Ép Auto bằng cổng không có listener:PASS, cả hai chuyển sang WebSocket sau ConnectTimeout; mỗi client hoàn tất trận, lịch sử và chơi lại. Đây là mô phỏng đường UDP không tới được ở Local, không phải nghiệm thu mạng công cộng bị chặn.
- Client 720p đã chạy và có ảnh gameplay. Menu hiển thị câu chữ mới; thao tác bấm F3/F8 bị người dùng điều khiển cửa sổ xen vào nên kiểm tay phím này là NOT_TESTED. Guard compile Release vẫn loại bỏ thao tác mở bảng F3/F8; không dùng điều đó thay bằng chứng hai PC.
- Không kiểm lại Editor 15f1; giữ pin chung của dự án. Editor 21 là môi trường kiểm thử cá nhân đã được chấp thuận.
- Hai PC vật lý và AWS:NOT_TESTED; không lấy hai process thay hai PC. Resume đã kiểm bằng module/harness; nghiệm thu gián đoạn mạng hai PC thật là lượt riêng.

DeepSeek bị provider HTTP 401 ngay trước khi đọc/sửa source; Codex tiếp quản theo xác nhận của người dùng. Không gửi secret/.env/credential cho worker. Graphify được cập nhật AST; bộ phân tích có giới hạn với 4 file UniTask import, không phải lỗi compile Unity. Semantic kiến trúc cũ đã stale từ trước nên công cụ bỏ phần claim cũ, không đánh dấu giả là đã trích xuất lại toàn bộ tài liệu.


## Số đo lượt game thật

| Lượt | Client | Đường được chọn | Input/s | Snapshot/s | RTT p50/p95/p99(ms) | Pong timeout |
|---|---|---|---:|---:|---|---:|
| local-mixed | evidence-client-1 | Wss | 29.66 | 30.01 | 33.29/33.32/33.85 | 0 |
| local-mixed | evidence-client-2 | Udp | 29.67 | 30.05 | 33.31/33.55/74.06 | 0 |
| local-fallback | evidence-client-1 | Wss | 29.99 | 29.99 | 16.68/16.75/18.18 | 0 |
| local-fallback | evidence-client-2 | Wss | 29.99 | 29.99 | 33.35/34.34/34.48 | 0 |

Các số đo này xác minh flow mạng Local, không thay benchmark gameplay50slot hoặc PvP người thật. Lượt mixed mất khoảng 25 giây kết nối/client; lượt fallback khoảng 32 giây. Không chạy tải hoặc tiêu credits AWS.

## Bàn giao và chạy lại

- Mở toàn bộ thư mục `Builds/Connection-Flow`, chạy `Lastaway.exe`; không chỉ chép mỗi EXE. Bộ runtime và `registry-build.json` nằm cùng thư mục.
- Chạy Local: ở `Backend`, bật `docker compose --profile account up -d --build`. Chọn **Local · Docker** trong cài đặt trước khi đăng nhập. Đăng ký/đăng nhập → tạo/vào phòng → chọn map → đủ người/bot → bắt đầu. Dev mặc định cần 6 participant;10/phòng,5phòng,50slot.
- Để lặp bài hai client tăng tốc, đặt riêng `MATCH_PROFILE=test` lúc bật stack rồi dùng runner `Backend/Deploy/test-unity-remote.ps1` với `-Local`, API `http://127.0.0.1:5000`, scenario `local2-accelerated.json`; thêm `-ForceAutoFallback` cho lượt dự phòng. Không sửa lịch/main để rút ngắn bài đo.
- Thử lỗi: tắt API, vào phòng phải giữ đăng nhập; bật lại rồi bấm Thử lại. Lỗi phiên account thực sự yêu cầu đăng nhập lại. Các lỗi có kiểm soát401/403/404/429/503 và callback trễ có test tự động nêu trên.
- Đã trả profile **main**,10/5/50 và chạy dừng stack theo xác nhận của người dùng. Kiểm cuối thấy Docker Desktop engine đã tắt/không truy cập được, không bật lại chỉ để kiểm trạng thái. Không xóa volume Mongo, map, outbox hoặc secret. Client tác vụ đã đóng; game riêng của người dùng không bị dừng.
- Camera giữ **Follow Player**, spectator giữ nguyên. Các thay đổi serialize tự sinh của Editor 21 ở Boot/map cũ/PlayerSettings đã được đối chiếu và trả lại; không đưa chúng vào commit. Metadata build giữ hash đầu vào thực tại thời điểm build; pipeline publish sau này phải chạy gate/rebuild theo source/config hiện hành, không coi lượt Local này là bằng chứng deploy AWS.
- Bằng chứng: `report.json`, `checks.csv`, `evidence.zip` và SHA256. ZIP gồm XML test, log/số đo runner và ảnh gameplay/result; không chứa.env, secret, credential hoặc ảnh lobby có mã phòng.
