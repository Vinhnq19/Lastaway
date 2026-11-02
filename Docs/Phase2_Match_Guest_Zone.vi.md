# Phase 2: account, vòng trận, gameplay và Safe Zone

Cập nhật **01/11/2026**. Hướng dẫn setup và kiểm thử **SRV-03, NET-02, ZON-01**. Xem [báo cáo nghiệm thu Phase 2](Reports/Phase2_Local/report.vi.md) để đọc kết quả, giới hạn và bằng chứng. Setup nền và vận hành AWS nằm trong [hướng dẫn Phase 1](Phase1_First4Tasks.vi.md).


Phần này áp dụng **SRV-03 / NET-02 / ZON-01** trên source Phase 2. AWS đang chạy bản trước không tự có tính năng mới. Lượt account-flow được phép deploy/test trên EC2 đã dành riêng, giữ Running khi kết thúc; xem báo cáo để biết chính xác bản đã nghiệm thu. Xem [Network Architecture](Network_Architecture.vi.md) để biết contract và quyền truy cập.

## 0. Chọn Local, LAN hoặc Online

- **Local → Chơi Solo (Offline)**: vào Game ngay; không bật Docker/API, không nhập mã phòng. Một người chơi với di chuyển/cover/bo/HP → chết → kết quả luyện tập → chơi lại. Không tự thắng khi chỉ có một người và không ghi lịch sử online. Mở Game rồi Play trong Editor cũng chạy đường này, trừ runner mạng có chủ đích.
- **LAN**: máy host chạy GameServer; đặt `GAME_PUBLISH_HOST` trong Backend/.env thành IPv4 LAN của host, giữ `GAME_BIND_ADDRESS=0.0.0.0`, bật stack `docker compose --profile account up -d --build`. Firewall host cho UDP7777 trên mạng riêng. Client đăng ký/đăng nhập qua API HTTPS cùng backend rồi nhập IP game, RoomCode và tên. Mongo/control vẫn private. API loopback không dùng được từ PC khác; cấu hình gateway HTTPS trước.
- **Online → Radmin/VPN**: cả hai PC cài Radmin VPN và vào cùng mạng VPN. Máy host publish UDP7777 vào IP Radmin của chính nó qua `GAME_PUBLISH_HOST`; client nhập IP đó, tên và mã phòng. Cho phép UDP7777 ở firewall đúng interface; không mở Mongo/control ra VPN. Bắt buộc API HTTPS hợp lệ cùng backend để đăng nhập và lấy ticket; API hỏng thì không join mới. Không bỏ kiểm tra TLS dù đã dùng VPN.
- **Online → AWS**: hostname `junglemaze.duckdns.org`, Auto UDP rồi WSS qua443; không dùng IP thay hostname cho TLS. Chọn mode không tự bật EC2. Admin kiểm Running, DNS/TLS và bản release trước khi mời tester. Lượt kiểm thử này giữ EC2 Running; deadline không tắt máy, credits tiếp tục phát sinh.

**Nâng cao** giữ profile, host/port, RoomCode, API và nút vào backend loopback để test mạng local như trước. `phase2-local` là **backend Docker loopback**, khác nút Local offline. Profile mạng phát hành dùng v4; cấu hình cũ được nâng khi vào bằng account. Fixture protocol cũ chỉ chạy riêng trong test. Bảng chẩn đoán cũ mặc định ẩn cả khi hiện kết quả; F3 bật/tắt khi cần kiểm tra. Lỗi tải map vẫn hiện dù tắt bảng. F3 bật thông tin chẩn đoán; ESC chỉnh âm lượng/rời trận. Offline vẫn chạy bo khi ESC mở.

Muốn tạo/list/xóa phòng: Swagger → register/login → Authorize JWT → `/api/rooms`; Guest không được quản lý phòng. Menu LAN/online yêu cầu tài khoản trước cả tạo và vào phòng. Nút Luyện tập offline luôn có từ Login/Home, không gọi Guest/API.

Killcam dùng replay server theo chính phiên/phòng, tối đa5s (150frame), dữ liệu chỉ có nạn nhân và kẻ hạ. Mặc định vẫn tắt theo ticket; bật asset MatchOverlaySet hoặc `-killcam` ở development build để kiểm. Chết do bo/không rõ killer hiển thị fallback, không giả lập combat. Chỉ báo PASS theo bằng chứng combat/replay thật của bản build hiện tại.

## 0.1. Chạy từng môi trường từ bản Windows

**Mặc định khi mở game: Online · AWS** (`phase2-aws`, `junglemaze.duckdns.org`). Đây là chọn endpoint, không tự đăng nhập hoặc tự vào phòng. Cấu hình Local/LAN đã lưu không đổi mặc định lần mở sau. `-profile` và `-clientConfig` chỉ định rõ vẫn được ưu tiên để dev/test tự động dùng đúng môi trường. Nút Luyện tập offline luôn có trên menu.

Mở `Builds/Windows-6000.3.15f1/Lastaway.exe`. Vào cài đặt máy chủ để đổi môi trường **trước khi đăng nhập**; đổi API sẽ đăng xuất. Mỗi backend có tài khoản và dữ liệu riêng. Sau đăng nhập: tạo phòng hoặc nhập mã phòng của cùng server; chủ phòng chia sẻ mã, không chia sẻ JWT. Profile `main` cần ít nhất6 người mới bắt đầu; phòng chứa tối đa10. Muốn test2 người trên host riêng, dùng `MATCH_PROFILE=test`, không đổi AWS đang phục vụ người khác.

### Luyện tập offline — không dùng server

Chọn **Luyện tập offline** từ màn đăng nhập/menu → vào Game một người. Không cần Docker, API, tài khoản hoặc RoomCode. Thoát trận về menu; không ghi thành tích online. Đây là lựa chọn test gameplay một mình.

### Local · Docker — backend trên chính PC

1. Bật Docker Desktop và đợi engine chạy.
2. Mở PowerShell ở thư mục `Backend` của checkout. Lần đầu tạo `.env` nếu chưa có: `if (-not (Test-Path .env)) { Copy-Item .env.example .env }`. Điền secret theo hướng dẫn Phase1; giữ file riêng, không ghi đè cấu hình đã có.
3. Chạy `docker compose --profile account up -d --build`; kiểm `docker compose ps` và `docker compose logs --tail 30 game-server api`.
4. Trong game chọn **Local · Docker**: Host `127.0.0.1`, UDP7777, API `http://127.0.0.1:5000`, WS `ws://127.0.0.1:7781/game` từ preset.
5. Đăng ký/đăng nhập vào API Local, tạo phòng rồi mở thêm client cùng profile/mã phòng. Nếu đứng chờ Lobby, kiểm số người và MATCH_PROFILE.
6. Xong chạy `docker compose --profile account stop` từ Backend. Giữ volume Mongo; không thêm `-v`.

Lần sau chỉ bật Docker, chạy `docker compose --profile account up -d`, rồi chọn lại Local trong game. `--build` dùng khi code backend thay đổi. Có thể mở thẳng Local bằng `Lastaway.exe -profile phase2-local`.

### LAN · cùng mạng — một PC host, PC khác chơi

1. Chọn một PC chạy backend, lấy IPv4 của interface Wi-Fi/Ethernet đang dùng bằng `ipconfig` (ví dụ192.168.1.10). Máy còn lại phải truy cập được host; Wi-Fi guest/client isolation có thể chặn các máy với nhau.
2. Trên host, đặt `GAME_PUBLISH_HOST` trong Backend/.env thành IP LAN đó, giữ `GAME_BIND_ADDRESS=0.0.0.0`; bật stack account như Local. Cho phép UDP7777 ở Windows Firewall trên interface/mạng riêng cần dùng.
3. **Chuẩn bị API HTTPS hợp lệ của cùng backend host**. Dùng gateway tự host ở mục6/README Host; cấu hình domain/chứng chỉ và DNS phù hợp. Stack Local chỉ có API HTTP loopback5000 nên riêng bước2 chưa đủ để PC2 đăng nhập. Không dùng API AWS với GameServer LAN, không bỏ xác minh TLS.
4. Mọi client chọn **LAN · cùng mạng**, thay Host bằng IP LAN của host, API bằng URL HTTPS đã chuẩn bị; UDP7777. Lưu endpoint, đăng ký/đăng nhập trên host rồi dùng cùng mã phòng.
5. Xác nhận PC2 thật thấy cùng trận/vị trí/bo, sau đó kiểm kết quả và chơi lại. Khi xong dừng backend ở máy host.

Không cần port-forward router cho UDP LAN. Không mở Mongo27017/27018 hoặc control7780 ra LAN.

### VPN · tự host — người chơi ở mạng khác

1. Host và các máy chơi cài VPN và vào cùng mạng. Radmin dùng IP VPN mà ứng dụng hiển thị; Tailscale dùng IP/hostname được cấp. Chỉ chọn một tuyến thực, không dùng IP mẫu100.80.1.20 nếu đó không phải IP host.
2. Host thiết lập stack riêng bằng hướng dẫn [Host](../Backend/Deploy/Host/README.md). Với Tailscale, từ Backend chạy `powershell -File Deploy/Host/configure-host.ps1 -Mode vpn` một lần; chạy preflight và compose VPN theo README, sau đó `tailscale serve --bg http://127.0.0.1:8080`. Bật HTTPS trong tailnet nếu Tailscale yêu cầu; đăng nhập VPN trực tiếp, không gửi token vào repo.
3. Với **Radmin**, VPN chỉ tạo đường mạng riêng; cần tự thiết lập gateway HTTPS/chứng chỉ được client tin cậy cho API cùng host. Radmin không thay thế Tailscale Serve. Nếu chưa có HTTPS hợp lệ thì tuyến đăng nhập chưa sẵn sàng; dùng Tailscale Serve hoặc AWS.
4. Cho UDP7777 qua firewall đúng interface VPN. Client chọn **VPN · tự host**, đặt Host bằng IP VPN; API/WSS dùng hostname HTTPS hợp lệ của host. Preset Tailscale chứa địa chỉ mẫu, phải thay đúng host. Nếu chỉ dùng UDP Radmin, chọn transport UDP qua config riêng; không để WSS trỏ hostname mẫu.
5. Đăng nhập, tạo/join phòng như Local. Kiểm bằng hai máy ở hai mạng; VPN có thể dùng relay nên cần đo RTT thực.
6. Xong dừng compose VPN và Serve theo README, giữ volume. Không tắt VPN của buổi khác hoặc reset Serve khi host đang phục vụ ai.

### Internet · tự host — người chơi không cài VPN

1. Host cần IP public/đường inbound được phép. CGNAT cần làm việc với ISP hoặc chuyển sang VPN/AWS.
2. Chuẩn bị domain riêng trỏ về IP host; không dùng domain AWS `junglemaze.duckdns.org`. Router chuyển TCP80/443 và UDP7777 về host; firewall chỉ mở các cổng này.
3. Làm theo [Host README, mục Public](../Backend/Deploy/Host/README.md#public): tạo cấu hình public bằng `configure-host.ps1`, chạy preflight và compose public. Caddy cấp HTTPS; chờ API/DB/GameServer healthy và certificate hợp lệ.
4. Client chọn **Internet · tự host**, thay domain mẫu ở game/API/WSS bằng domain host; API `https://<domain>`, WSS `wss://<domain>/game`, UDP7777. Đăng nhập vào backend host, tạo/join bằng mã phòng.
5. Test từ PC ngoài mạng nhà; tự kết nối từ host chưa chứng minh port-forward hoạt động. Auto thử UDP rồi WSS443 nếu UDP bị chặn.
6. Dừng stack host khi xong. Giữ secret/volume và chỉ chia sẻ endpoint/mã phòng cho người chơi.

### Online · AWS — mặc định

1. Admin Start EC2 nếu đang Stopped; đợi status checks, DuckDNS cập nhật và HTTPS `/health` hoạt động. Quy trình SSH/Swagger/Compass ở [hướng dẫn AWS](AWS_SSH_Swagger_Compass.vi.md).
2. Mở game bình thường, mặc định đã là AWS; nếu đang đổi cấu hình trong phiên thì chọn **Online · AWS**. Endpoint `junglemaze.duckdns.org`, UDP7777, API HTTPS và WSS `/game` cùng domain.
3. Đăng ký/đăng nhập → tạo phòng hoặc nhập RoomCode. Người chơi không cần AWS CLI, SSH hay VPN. Auto fallback WSS khi UDP không tới được; DNS/HTTPS bị chặn vẫn phải đổi mạng được phép.
4. Kết thúc đóng client/rời phòng. Admin quản lý lịch chạy EC2; đóng game/SSH không dừng EC2. Stop khi hết buổi nếu không có yêu cầu giữ Running; EBS có thể tiếp tục tiêu credits.

**Kiểm bằng hai PC thật:** LAN, VPN và Internet tự host bắt buộc có máy thứ hai; checklist ở `Backend/Tests/OnlineReadiness/PHYSICAL_TWO_PC_CHECKLIST.md`. Một PC mở nhiều cửa sổ chỉ kiểm logic room/session, không xác nhận router/firewall/VPN của máy khác.

## 1. Chuẩn bị backend Docker loopback lần đầu

1. Bật Docker Desktop, dùng Linux containers. Mở PowerShell **tại `Backend`**, không chạy compose từ `C:\Users\...`.
2. Giữ `.env` riêng của máy và Mongo volume hiện có. Tạo từ `.env.example` nếu chưa có, theo [mục 3 của hướng dẫn Phase 1](Phase1_First4Tasks.vi.md). Không chép secret AWS sang Local.
3. Kiểm tra `ROOM_CODE`, `MAX_PLAYERS=10`, `MAX_ROOM_PLAYERS=10`, `MAX_ROOMS=5`, `MAX_TOTAL_PLAYERS=50`, `MAX_ROOMS_PER_USER=2`. Main dùng `MATCH_PROFILE=main`; thử hai client dùng `MATCH_PROFILE=test` và `PUBLIC_HOSTING=false`.
4. Để tạo phòng/cấp vé account cần `ROOM_CONTROL_TOKEN` tối thiểu32 byte và API trỏ đúng room-control nội bộ. Muốn lưu lịch sử cần **secret riêng** `MATCH_RESULT_TOKEN` tối thiểu32 byte, giống nhau giữa API/GameServer; không dùng JWT key hoặc control token thay thế. Có thể tạo mỗi secret bằng PowerShell: `$b = New-Object byte[] 32; $rng = [Security.Cryptography.RandomNumberGenerator]::Create(); $rng.GetBytes($b); $rng.Dispose(); [Convert]::ToBase64String($b)`, rồi điền vào `.env` bằng VS Code. Không commit file này.
5. Giữ `MATCH_OUTBOX_PATH` trong volume của game-server và `MATCH_RESULTS_API_URL` trỏ API nội bộ. Compose chịu trách nhiệm tên service/cổng; client không gọi tuyến ghi kết quả.
6. Khởi động cả hệ thống:

```powershell
Set-Location 'D:\Unity\Lastaway\Backend'
docker compose --profile account up -d --build
docker compose ps
docker compose logs --tail 40 game-server api
```

Đợi Mongo healthy, `/health` của API trả thành công, GameServer có `[ready]`. Thiếu control secret/API thì không cấp vé chơi mạng; chỉ Local luyện tập tiếp tục dùng được. Thiếu secret kết quả thì trận vẫn chạy, nhưng chưa nghiệm thu lưu lịch sử. Không dùng `docker compose down -v`: lệnh đó xóa dữ liệu.

## 2. Chạy demo Unity hai client

1. Mở project Unity **6000.3.15f1**, đợi hết compile. Trong menu **Jungle Maze → Phase 2 → Sync Runtime Data** đồng bộ map/luật/bo từ `Backend/Shared/GameData`. Dùng Verify Runtime Data để kiểm; không sửa bản sao StreamingAssets thay nguồn chuẩn.
2. Trong Nâng cao của menu kết nối scene Boot, chọn profile **phase2-local**, đặt host/port theo compose và nhập `ROOM_CODE` trong `.env`. Protocol v4 bắt buộc account. Đăng ký/đăng nhập trên menu trước khi vào phòng; `apiBaseUrl` trỏ API loopback đang chạy. API từ xa phải HTTPS. Vé30s được cấp trước socket, không nhập JWT vào RoomCode.
3. Build hai scene `Boot` và `Game` thành Windows player. Chạy một player + Unity Editor, hoặc hai player. Dùng hai tên dễ phân biệt. Cả hai dùng chung endpoint và mã phòng.
4. Với `MATCH_PROFILE=test`, hai người đủ bắt đầu; lobby 0,5s, loading tối đa 1s, disconnect grace 1s, kết quả 0,5s và lịch bo chạy ở 4% thời gian gốc. Với `main`, cần ít nhất 6 người, lobby 15s, loading tối đa 30s, grace 15s, kết quả 10s và bo đúng lịch GDD. **Không dùng test profile để đo thời lượng GDD hoặc đưa public.**
5. Quan sát roster/countdown/loading, hai nhân vật có nhãn tên/HP, bo đỏ hiện tại và bo vàng kế tiếp. WASD gửi input thật; server tính va chạm/speed/HP. Góc nhìn toàn map dành cho demo. Local HeroController bị tắt khi chơi mạng, tránh hai mô phỏng ghi vị trí.
6. Ra ngoài bo để thấy HP giảm5/s; đợi thắng/hòa, xem hạng và nhấn chơi lại. Cùng tick bị loại đồng hạng. Bo về0 vẫn gây damage toàn map;420s không phải timeout cưỡng bức.
7. Rời phòng bằng nút để loại ngay. Mất mạng đột ngột: nhân vật đứng yên và còn nhận damage; nút khôi phục chỉ thành công khi còn sống và trong grace từ lúc server phát hiện mất kết nối: 15s với `main`, 1s với `test`. Resume giữ PlayerId/slot và đổi proof; không tạo nhân vật thứ hai.
8. Nút lịch sử gọi API cho hồ sơ đã xác minh. Không tạo Guest mới trong flow chơi. JWT chỉ giữ RAM; mở lại client cần đăng nhập. Guest cũ không bị xóa/gộp. API mất kết nối vẫn xem kết quả trực tiếp; lịch sử báo không khả dụng. Không tự gộp tài khoản theo tên.

Unity hiện có UDP/WebSocket/Auto; xem các mục online bên dưới và báo cáo để phân biệt kết quả Local với AWS. Prediction/interpolation đầy đủ còn thuộc MOV-02/03; JSurviv Socket.IO cũ không phải client demo này. Chưa có combat vũ khí/inventory/hazard; kills bằng0.

## 3. Kiểm API qua Swagger

Mở [Swagger Local](http://127.0.0.1:5000/swagger/index.html), theo [mục 5 của hướng dẫn Phase 1](Phase1_First4Tasks.vi.md). Các endpoint phòng vẫn cần register/login và Authorize JWT account. Guest credential **không** thay JWT để tạo/list/xóa phòng.

| Endpoint | Cách thử |
| --- | --- |
| `POST /api/guests` | Không cần account; nhận Guest ID và credential, giữ riêng và không chia sẻ |
| `POST /api/game-tickets` | **Chỉ JWT account**; body attemptId + roomCode, ticket64hex/30s/dùng một lần; Guest trả401 |
| `GET /api/me/matches` | JWT hoặc Guest credential đúng; phân trang, chỉ lịch sử của hồ sơ đó |
| `GET /api/matches/{matchId}/leaderboard` | Đọc kết quả authoritative đã lưu, không có credential/ProfileKey |
| `/internal/match-results` | Không thử bằng JWT player; tuyến chỉ GameServer qua mạng nội bộ + secret riêng |

Client demo tự xử lý attempt/ticket; không nhập PlayerId tùy ý để nhận lịch sử người khác. Hai người cùng tên vẫn là hai hồ sơ nếu dùng credential khác. Kết quả cần kết thúc trận và outbox được API xác nhận trước khi có trong Mongo/history.

## 4. Kiểm thử tự động không tiêu credits

Đứng tại `Backend`:

```powershell
dotnet build JungleBackend.sln
dotnet run --project Tests/MatchChecks/MatchChecks.csproj
dotnet run --project Tests/MatchResultChecks/MatchResultChecks.csproj
dotnet run --project Tests/NetworkChecks/NetworkChecks.csproj
dotnet run --project Tests/DualTransportChecks/DualTransportChecks.csproj
dotnet run --project Tests/GameplayNetworkChecks/GameplayNetworkChecks.csproj -- Mixed
dotnet run --project Tests/GameplayNetworkChecks/GameplayNetworkChecks.csproj -- Udp
dotnet run --project Tests/GameplayNetworkChecks/GameplayNetworkChecks.csproj -- Wss
```

GameplayNetworkChecks tự mở server/port loopback riêng, không dùng phòng đang phục vụ dev; thử10 phiên, phiên11, snapshot/bo, resume, kết quả và vòng tiếp theo. Có thể thêm đường dẫn JSON ở cuối lệnh để lưu bằng chứng. Đây là kiểm chức năng gameplay tối thiểu, không phải benchmark sức chứa cuối.

Unity: Test Runner → EditMode và PlayMode. Build tự chặn dữ liệu không khớp nguồn. Dùng `Deploy/test-unity-remote.ps1 -Local -ApiBaseUrl http://127.0.0.1:5000 -Clients 2 -Player <exe-bản-mới> -Scenario Tests/OnlineReadiness/scenarios/local2-accelerated.json -OutputDirectory <thư-mục-bằng-chứng>` từ Backend. Launcher tạo tài khoản/phòng test, đưa credential qua môi trường của process và chạy `-onlineTest`; không truyền RoomCode/password/JWT trên command line. Runner dùng cùng màn đăng nhập và join với người chơi, kiểm view/bo/HP/kết quả/account history/replay. Chỉ chạy profile server `test` cho bài2 người; profile `main` cần6. Không bật runner khi chơi bình thường.

Để đo hiệu năng, đợi Unity build/shader compilation hoàn tất, tránh Sleep và chạy ứng dụng nặng đồng thời. Ghi dropped tick/CPU/RAM và tốc độ phát thực; không đổi một lượt đo không hợp lệ thành PASS.

## 5. Những lần chạy sau, dừng và cập nhật

Bật Docker Desktop → mở đúng thư mục Backend → `docker compose --profile account up -d` → kiểm `/health`/logs → chạy client. Khi sửa code backend thêm `--build`; khi sửa dữ liệu chuẩn, Sync Runtime Data và build client lại. Khi đổi `MATCH_PROFILE` hoặc biến môi trường, chạy compose up để recreate service; restart đơn thuần không nạp biến mới.

Xong buổi Local: đóng client, `docker compose --profile account stop`. Lần bật lại Mongo và outbox trên volume vẫn còn; phòng bổ sung, session và trận trong RAM mất. Không xóa outbox chưa gửi được để dọn dung lượng. Khi API trở lại, outbox retry ngoài tick, cùng MatchId không cộng kết quả lần nữa.

AWS cần deploy release mới sau khi cổng Local đạt; giữ JWT key, RoomCode, Mongo volume và cấu hình cũ; migration chỉ thêm cấu hình cần thiết, không publish internal port. Dùng checksum/rollback theo [hướng dẫn Phase1](Phase1_First4Tasks.vi.md). Lượt này có yêu cầu giữ Running nên dùng mục10, không dùng nhánh tự Stop. Hai process trên cùng PC không thay nghiệm thu hai thiết bị. Kết quả AWS bản cũ không chứng minh account-ticket v4.

## 6. Chọn môi trường online

Phần mở rộng ngày **25/10/2026** bổ sung Unity WSS/Auto và bộ test dùng lại. Kết quả thực tế cập nhật trong [báo cáo Phase 2](Reports/Phase2_Local/report.vi.md); có cấu hình không đồng nghĩa đã nghiệm thu tuyến mạng đó.

| Profile Unity | Host game / API | Việc phải cấu hình |
| --- | --- | --- |
| `phase2-local` | Loopback: UDP7777, API5000, WS7781 | Docker Local, RoomCode trong Backend/.env |
| `phase2-lan` | IP LAN của máy host, UDP | Thay IP mẫu và API HTTPS của cùng host; phải đăng nhập để lấy ticket, API null không vào phòng mạng |
| `phase2-vpn` | IP Tailscale cho UDP; hostname ts.net cho HTTPS/WSS | Thay IP/hostname mẫu, cùng tailnet, Tailscale Serve HTTPS |
| `phase2-selfhost` | Hostname riêng cho UDP/HTTPS/WSS | Thay YOUR-SELFHOST, DNS, router forwarding và TLS |
| `phase2-aws` | junglemaze.duckdns.org | EC2 phải được deploy release mới; game/API/WSS cùng backend |

Các profile nằm tại `Assets/StreamingAssets/JungleMaze/client.phase2-*.json`, đều dùng protocol4 và world stream. Các địa chỉ LAN/VPN/selfhost mẫu phải thay bằng địa chỉ thật. Không đổi hostname tự host sang domain của AWS.

Auto thử UDP tối đa5s, rồi WSS tối đa10s kết nối +5s join. RoomFull, ServerFull, sai RoomCode, MatchInProgress hoặc sai version không làm client thử transport khác. Sau join không tự đổi tuyến; mất mạng dùng Resume trong grace. F3 trong Editor/Development build cho biết transport/RTT/fallback, menu cũng hiện transport cấu hình.

API từ xa cần HTTPS, kể cả VPN. WS không TLS chỉ dùng loopback Local. Không dùng certificate callback cho phép mọi chứng chỉ, `curl -k`, hoặc chép Guest credential/JWT vào JSON cấu hình. `NET_TRANSPORT` và `NET_WS_URL` có thể override transport/URL; CLI có `-transport` và `-webSocketUrl`. Bỏ biến override sau khi test để không ảnh hưởng buổi sau.

### 6.1. Tự host qua VPN

Đứng tại `Backend`, bật Docker Desktop. Dùng config riêng, không sửa secret của `.env` Local hoặc `.env.aws`:

```powershell
.\Deploy\Host\configure-host.ps1 -Mode vpn
```

Script hỏi mật khẩu Swagger, tạo ba secret riêng và `.env.host.vpn` được bảo vệ bằng ACL Windows. Không ghi đè file đã có. Cấu hình ban đầu là `MATCH_PROFILE=main`; muốn thử2 người trong VPN riêng thì mở file và đổi thành `test`.

1. Cài/đăng nhập Tailscale bằng `Deploy/setup-phase1-vpn.ps1 -Target PC`, hoặc cài ứng dụng chính thức. Đăng nhập trực tiếp trong trình duyệt. Mạng chặn trang cài/đăng nhập thì ghi BLOCKED và đổi mạng được phép; không coi VPN luôn hoạt động.
2. Chạy preflight rồi mở stack:

```powershell
.\Deploy\Host\preflight-host.ps1 -Mode vpn -EnvFile .\Deploy\Host\.env.host.vpn
docker compose -f compose.host.yaml --env-file Deploy/Host/.env.host.vpn --profile vpn up -d --build
```

3. Trong PowerShell Administrator, thực hiện thủ công `Deploy/Host/setup-firewall.ps1 -Action Enable -Mode Vpn`. Chỉ cho UDP từ mạng Tailscale; không mở8080 ra mạng.
4. Chạy Tailscale Serve HTTPS chuyển vào `http://127.0.0.1:8080`, theo hướng dẫn CLI `tailscale serve --bg http://127.0.0.1:8080`. Xác nhận bật HTTPS trong tailnet khi được hỏi. Không bật Funnel.
5. Lấy IP bằng `tailscale ip -4` và hostname HTTPS mà Serve báo; điền cả hai vào profile `phase2-vpn`. Game UDP và HTTPS phải về cùng host. Tester cũng đăng nhập tailnet.
6. Xong buổi: `docker compose -f compose.host.yaml --env-file Deploy/Host/.env.host.vpn --profile vpn stop`. Tắt Serve bằng `tailscale serve reset` nếu không còn dùng cấu hình Serve này. Chỉ reset cấu hình thuộc host test, không đụng thiết bị khác.

Stack `junglemaze-host` có volume riêng, không dùng DB dev/AWS. Nếu UDP7777 đang bị stack dev chiếm, stop đúng service dev khi không ai dùng, hoặc chọn `HOST_GAME_UDP_PORT` khác và đổi client tương ứng. Không chạy hai stack publish cùng cổng trên một host.

### 6.2. Tự host public

1. Kiểm tra có IP public và quyền cấu hình router. CGNAT/double NAT chưa xử lý thì tuyến public chưa sẵn sàng; dùng VPN hoặc AWS. Không dùng kết nối từ chính host để khẳng định người ngoài vào được.
2. Tạo hostname riêng và trỏ tới public IP của host. Giữ domain `junglemaze.duckdns.org` cho AWS.
3. Chạy `Deploy/Host/configure-host.ps1 -Mode public -Domain TEN-HOST-THAT -AcmeEmail EMAIL-THAT`. File riêng được tạo, giữ profile main.
4. Router forward TCP80/443 và UDP7777 về máy host; chạy firewall helper thủ công với `-Mode Public`. Chưa có cách đọc WAN/router từ repo để tự xác nhận các bước này.
5. Preflight với mode public rồi chạy compose.host như VPN, đổi env/profile thành public. Caddy cấp TLS, `/game` chuyển đến GameServer; Swagger có basic auth. API/Mongo/control/WS nội bộ không publish trực tiếp.
6. Dùng PC ở mạng ngoài để kiểm HTTPS và game thật. Chỉ `/health`200 chưa chứng minh WSS/UDP hoặc gameplay. Xem checklist hai PC ở mục9.

## 7. Chạy test tự động bằng player thật

`Deploy/test-unity-online.ps1` dùng được khi các feature tiếp theo được phát triển. Nó không tạo NPC server, không ép phase hoặc tự ghi HP/kết quả. Mỗi player thật gửi input kiểm thử, nhận state server và xuất bằng chứng.

Đứng tại `Backend`, dùng build Windows đã tạo từ Boot/Game. `-Player` là đường dẫn .exe, không phải project Unity. Giữ thư mục `_Data`, DLL và MonoBleedingEdge đi cùng. Không sửa code để đổi endpoint; dùng `-Config`.

```powershell
$env:NET_ROOM_CODE = Read-Host 'Ma phong cua server dang test'
.\Deploy\test-unity-online.ps1 -Player '..\.utmp\phase2-account-flow-20261101\build\Lastaway.exe' -Config '..\Assets\StreamingAssets\JungleMaze\client.phase2-local.json' -Scenario '.\Tests\OnlineReadiness\scenarios\local2-accelerated.json' -Clients 2 -OutputDirectory '.\out\test-LUOT-MOI'
Remove-Item Env:NET_ROOM_CODE
```

Thay LUOT-MOI bằng nhãn chưa tồn tại. Công cụ từ chối ghi đè evidence cũ; mã phòng chỉ truyền qua env của process. Chỉ dọn process đúng PID và thời điểm khởi chạy mà công cụ đã tạo, không kill Editor/game của người khác.

| Scenario | Server cần dùng | Điều xác nhận |
| --- | --- | --- |
| `local2-accelerated` / `local2-udp` / `local2-wss` | Private `test`,2 người | Movement, bo, HP, kết quả, replay và đúng MatchId trong history |
| `main6-preflight` | `main`,6 client | Giữ kết nối30s, input/snapshot đủ tốc độ; dùng để kiểm PC phát tải trước AWS |
| `main6-smoke` | `main`,6 client | Kết nối ít nhất300s, đủ vòng trận/kết quả/replay/history; deadline600s, không tăng tốc luật |
| `connection` | Bất kỳ,1 client | Join/pong; không coi là hoàn thành gameplay |

Tên scenario không ép transport. Chọn transport trong config hoặc override, trước lần chạy tương ứng. `-ClientConfigs` có thể nhận một đường dẫn config cho mỗi client để thử hỗn hợp UDP/WSS/Auto cùng backend.

Mỗi lượt có report.json, summary.csv, JSON/log và ảnh từ từng player. PASS chỉ khi đúng scenario, đủ quan sát và process không crash. Thiếu tốc độ input yêu cầu là INVALID; kết nối bị chặn là BLOCKED. Các báo cáo một-PC luôn giữ `physicalTwoPc=NOT_TESTED`. Fingerprint bao gồm player DLL/data để lượt rehearse cũ không được dùng cho build đã thay đổi.

Kiểm công cụ báo cáo bằng `powershell -NoProfile -File Tests/OnlineReadiness/Test-StaticOnlineReadiness.ps1` từ Backend; nếu đứng tại repo root, thêm `Backend/` trước `Tests/`. Các process giả ở bài này chỉ kiểm launcher, không thay cho player Unity hoặc chứng minh gameplay.

## 8. Thêm test khi có combat, inventory hoặc feature mới

1. Tạo JSON scenario mới dựa trên schema; thay các điều kiện thời gian/số người/builtin flag. Không sửa luật server để làm scenario PASS. Parser từ chối field không hỗ trợ.
2. Assertion feature cần **implementation thật**. Tạo file partial `Bootstrap.FeatureTests.cs` trong assembly Bootstrap, triển khai `partial void ConfigureOnlineChecks()`. Hook chỉ gọi khi có `-onlineTest`; chơi bình thường không chạy driver.
3. Trong hook, đăng ký `OnlineTestExtensions.RegisterCheck("combat.server-damage", context => ...)`. Trả `OnlineCheckResult("WAIT")` khi chưa quan sát, PASS khi nhận state/event authoritative đúng, FAIL khi phát hiện vi phạm. Đưa tên vào `extraAssertions` của JSON. Không có check tương ứng thì runner FAIL, không bỏ qua.
4. Muốn điều khiển hành động mới, triển khai `IOnlineTestDriver.Apply(in OnlineTestContext)` và đăng ký bằng `RegisterDriver`. Dùng service/packet feature đã được server hỗ trợ; context có Match, Network, History, Sender và elapsed. Không tự sửa snapshot/HP hoặc ghi điểm từ client.
5. Driver mặc định chỉ gửi movement ngắn rồi đứng yên. JSON dạng `extensions.combat.requireKills` không tự tạo được bài combat và bị từ chối; phải tích hợp driver/check rồi build lại.
6. Chạy test đơn vị feature trước, sau đó scenario Local hai client, sáu-client rehearsal và mới đến online. File schema/evidence có version; nếu thay contract phải cập nhật cả C#, module đánh giá và các ca negative launcher.

## 9. Các bài bắt buộc hai máy thật

Làm theo [checklist hai PC](../Backend/Tests/OnlineReadiness/PHYSICAL_TWO_PC_CHECKLIST.md). Các phần một-PC không thay thế được:

- LAN: PC2 vào IP LAN của PC1, firewall và layout giống nhau.
- VPN: hai thiết bị riêng trong tailnet, kiểm direct/relay, UDP và HTTPS/WSS.
- Public tự host: PC2 ở mạng ngoài, xác nhận DNS/router/CGNAT và game thật.
- AWS: hai PC, tốt nhất khác mạng; so vị trí/bo/HP/kết quả, account riêng, reconnect và replay.

Private test có thể mỗi máy1 client. Public/AWS main cần ít nhất6 phiên gameplay: ví dụ mỗi máy3 client. Lưu report mỗi máy và biên bản đối chiếu; không đổi report một-PC thành hai-PC PASS.

## 10. AWS có kiểm soát credits

Build/verify release và hoàn thành rehearsal6 client bản mới ở Local trước khi deploy. EC2 đã Running chỉ được dùng khi admin dành riêng; không tự Stop máy trong lượt giữ Running. Công cụ kiểm fingerprint build để tránh dùng kết quả rehearsal không còn tương ứng.

```powershell
.\Deploy\test-aws-rooms.ps1 -DualTransport -UnityGameplay -UseRunningInstance -KeepInstanceRunning -Release 'out\RELEASE\release.tar.gz' -UnityPlayer '..\.utmp\phase2-account-flow-20261101\build\Lastaway.exe' -UnityScenario 'Tests\OnlineReadiness\scenarios\main6-smoke.json' -UnityRehearsalReport 'out\LOCAL-REHEARSAL\report.json' -OutputDirectory 'out\AWS-LUOT-MOI' -BudgetMinutes 20 -HardStopMinutes 30
```

Lệnh giữ Running bên dưới chỉ dùng EC2 đang chạy đã được admin dành riêng, không Start/Stop; có quyền deploy/test. `-PreflightOnly` chỉ kiểm trước; không bật máy. Delta release là lựa chọn khi đã verify base image/provenance theo Phase1; không bỏ checksum để upload nhanh.

Lượt AWS dùng Unity thực từ PC, JWT cho quản lý phòng và vé chơi qua HTTPS, config protocol4/Auto. Không dùng generator trong EC2 để thay bằng chứng WAN. Bài main giữ nguyên ngưỡng6 và lịch bo. Không chạy Stop–Start trong lượt giữ Running; ghi NOT_TESTED theo chính sách.

Mục tiêu20 phút, deadline30 phút chỉ đóng công cụ/client khi dùng `-KeepInstanceRunning`. Không cài watchdog tắt EC2 trong lượt này. Thiếu thời gian thì dừng, không rút ngắn bài đo rồi báo PASS. Công cụ đóng client, xóa phòng test và xác nhận `runningConfirmed=true`; admin tự Stop khi hết nhu cầu. Tắt máy PC hoặc đóng SSH không dừng EC2. EBS còn chi phí lưu trữ sau Stop. Những account/Guest/kết quả test đã lưu vẫn ở DB; không tự xóa lịch sử hoặc volume để dọn dung lượng.

## 11. Kiểm tra lặp lại Windows offline (development build)

Bản build development có runner opt-in, không chạy trong buổi chơi bình thường:

```powershell
Set-Location 'D:\Unity\Lastaway'
& '.\.utmp\phase2-account-flow-20261101\build\Lastaway.exe' -offlineTest -offlineReport "$PWD\.utmp\offline-player.json" -logFile "$PWD\.utmp\offline-player.log"
```

Runner dùng scene/physics/UI thật, đặt nhân vật ngoài bo để kiểm damage (không giả lập bắn/combat), hoàn thành hai vòng, kiểm MatchId/HP reset, ESC giữ timer, trở về menu và không mở socket game. Kết quả JSON phải có `passed=true`, `rounds=2`; timeout90s là FAIL. Đóng hết Docker vẫn chạy được. Không dùng kết quả này thay nghiệm thu hai PC, input người thật hoặc gameplay multiplayer.

Không truyền `-offlineTest` khi chơi bình thường. Chạy lại sau khi sửa flow/bo/HUD để kiểm hồi quy; nếu tự build sang thư mục khác thì thay đường dẫn exe ở lệnh trên.

## 12. Lưu ý bản Unity đang giữ

Giữ Unity **6000.3.15f1** theo quyết định01/11. Windows player có thể hoàn tất scenario nhưng crash trong cleanup Accessibility lúc thoát; báo cáo JSON `passed=true` hoặc `status=PASS` chưa đủ nếu process exit khác0. Launcher phải giữ FAIL/BLOCKED và không mở cổng deploy AWS. Stack native và Event1000 được lưu cùng bằng chứng. Unity công bố bản sửa UUM-146676 tại [6000.3.21f1](https://unity.com/releases/editor/whats-new/6000.3.21f1); chưa được phép đổi Editor. Không tắt hỗ trợ accessibility hoặc bỏ kiểm exit code để đổi kết quả thành PASS.

Lượt sáu client01/11 sau sửa ACK quan sát đủ flow qua2 trận, nhưng vẫn có8 dropped tick và crash native khi thoát; chưa đạt cổng Local/AWS. Lỗi Mono startup ở một lượt khác được ghi riêng, chưa xác định nguyên nhân. Xem báo cáo hiện hành, không suy ra mọi lỗi native đều là Accessibility.
