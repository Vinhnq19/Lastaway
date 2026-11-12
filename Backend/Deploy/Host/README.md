# Tự host backend Local / VPN / public

## LAN bằng một mã — giao diện hiện hành (11/11/2026)

Bảng **Máy chủ** có đúng ba lựa chọn: **Online · AWS**, **LAN · cùng mạng**, **VPN · Tailscale**. Game khởi động thường dùng AWS. Mở một mục chỉ xem cấu hình, không đổi máy chủ hoặc đăng xuất; chỉ bấm Kết nối và kiểm tra thành công mới áp dụng. Địa chỉ lưu dưới tên `user` vẫn được nhận diện theo cấu hình thực. Local Docker và Internet tự host nằm trong phần dev của Editor/Development Build; CLI/profile cũ vẫn dùng được. Luyện tập offline là nút riêng, không cần backend.

**Máy host LAN:** dùng Windows có source Backend và Docker Desktop. Hai máy cùng router/Wi-Fi thông thường; tránh Wi-Fi khách hoặc mạng chặn các thiết bị liên lạc. Trong Windows, chỉ với mạng bạn tin cậy, mở Settings → Network & Internet → Wi-Fi/Ethernet → mạng hiện tại → chọn **Private**. Game không tự đổi thiết lập này.

1. Bật Docker Desktop. Trong game, mở **Máy chủ → LAN · cùng mạng**.
2. Ở **Tự làm máy chủ**, chọn Wi-Fi/Ethernet phù hợp. Có nhiều card thì chọn đúng mạng mà bạn bè đang dùng. Bản EXE chọn thư mục `Backend` một lần; Editor tự tìm source.
3. Bấm **Thiết lập** lần đầu. Nhập mật khẩu riêng bảo vệ Swagger trong cửa sổ setup, xác nhận UAC mở kết nối LAN khi Windows hỏi. Không gửi mật khẩu qua mã kết nối. Lần chuẩn bị image đầu tiên có thể cần Internet.
4. Bấm **Bật server**; chỉ khi báo sẵn sàng mới **Sao chép mã kết nối**. Host muốn chơi trên chính máy mình cũng dán mã và Kết nối, xác nhận máy chủ rồi đăng nhập game.
5. Gửi mã cho bạn. Lần sau chỉ bật Docker, mở LAN và Bật server. Đóng game không dừng server; hết buổi bấm Dừng server. Dữ liệu tài khoản/map/outbox/secret vẫn còn; phòng và trận trong RAM mất khi GameServer dừng.

**Máy người chơi:** chỉ cần bản game và cùng mạng, không cần Docker/source/Tailscale/quyền admin. Chọn LAN → **Dán mã → Kết nối → Tin máy chủ này** (lần đầu, chỉ tin mã nhận trực tiếp từ host). Sau kiểm tra thành công, đóng bảng và đăng ký/đăng nhập game trên máy chủ đó → tạo/vào phòng → chọn map, thêm bot → Bắt đầu. **Mã kết nối máy chủ khác mã phòng**. Lần sau cùng mã đã xác nhận sẽ được kiểm lại kết nối mà không hỏi lại. IP hoặc chứng chỉ đổi thì cần mã mới và xác nhận mới.

Nếu báo mạng Public, host tự kiểm tra mạng đáng tin rồi đổi Private; không tắt firewall. Cổng bị chiếm hoặc đang có host Local/Tailscale/Internet khác thì tự kết thúc buổi cũ trước; công cụ không giành cổng hay tự dừng chương trình khác. Khi đang có người, bot/grace hoặc trận chưa dọn, đổi chế độ/cấu hình bị từ chối. Chưa sẵn sàng thì thử Trạng thái; không tự chuyển sang AWS. Chỉ thông dịch vụ tài khoản chưa chứng minh gameplay thông: phải vào phòng và nhận phản hồi game.

**Dev:** `Deploy/Host/manage-lan.ps1 -Action Setup|Start|Status|Stop -LanIp <IPv4>` là owner duy nhất của LAN, tái sử dụng `compose.host.yaml` và project `junglemaze-host`. `.env.host.lan`, `.lan-state.json` và `Backend/data/lan-tls/` là dữ liệu riêng bị Git bỏ qua. Không chia sẻ key/secret. Lệnh cấp chứng chỉ của GameServer là `--lan-cert <thư-mục> <IPv4> 7777 8443`, chỉ dùng qua công cụ host khi server trống. Không mở thêm API/Mongo/control ra mạng. Nếu đổi card/IP, chạy Thiết lập lại để cập nhật đúng rule trước Bật server.

**Nghiệm thu hai PC:** máy A host, máy B dùng code mới rồi xác nhận/đăng nhập, cùng phòng; kiểm chọn cả hai map, bot, movement/loot/combat/bo/kết quả/history/chơi lại; kiểm đường dự phòng khi đường chính bị chặn có chủ đích. Sau khi tải đủ image/map/client, ngắt Internet nhưng giữ router và lặp lại; tài khoản/API/HTTPS LAN phải hoạt động. Stop–Start phải giữ account/map, phòng cũ không được khôi phục giả. Ghi rõ thiết bị, hai card mạng, profile Private, thời điểm và kết quả; hai process trên một máy không thay nghiệm thu này.


Hướng dẫn đầy đủ: [Phase 2, mục6–9](../../../Docs/Phase2_Match_Guest_Zone.vi.md#6-chọn-môi-trường-online). Chạy các lệnh dưới từ `Backend`. Đây là công cụ chuẩn bị host; chỉ ghi PASS kết nối từ xa khi đã chạy checklist với thiết bị thật.

## Cách nhanh trong game — Tailscale

**Host Windows có source:** bật Docker Desktop → trong game mở **Máy chủ → VPN · Tailscale**. Editor tự tìm Backend; EXE cho chọn thư mục Backend của source tin cậy một lần. Bấm **Thiết lập host**, tự xác nhận UAC/cài đặt/đăng nhập Tailscale và mật khẩu Swagger trong cửa sổ riêng. Nếu Tailscale yêu cầu HTTPS, mở liên kết do CLI hiển thị và xác nhận trong trình duyệt. Thiết lập không ghi đè secret đã có.

Sau đó bấm **Bật server**. Chỉ khi API HTTPS, Serve và GameServer hoạt động mới hiện **Server sẵn sàng**, cho **Sao chép mã kết nối**. Host muốn chơi trên máy mình cũng bấm **Kiểm tra / Kết nối**, đăng ký/đăng nhập tài khoản của backend host. Những lần sau bật Docker/Tailscale rồi bấm Bật server; Docker dùng build cache, không cần tạo lại cấu hình.

**Người chơi:** nhận lời mời tailnet/thiết bị từ host qua Tailscale một lần, dùng tài khoản riêng; mở game → VPN Tailscale → **Cài / đăng nhập VPN** nếu cần → dán mã `may-host.tail123` (hoặc hostname đầy đủ `.ts.net`; mã dài cũ vẫn được chấp nhận) → **Kiểm tra / Kết nối** → đăng ký/đăng nhập game → nhập mã phòng. Không cần source/Docker hoặc nhập ba endpoint riêng.

Mã kết nối chỉ chứa tên host, IP VPN, cổng UDP và hostname `.ts.net`; không chứa RoomCode/credential. Đây không phải lời mời tailnet hoặc vé chơi. Game kiểm Tailscale, DNS khớp IP và HTTPS/TLS trước khi lưu; báo API thông không có nghĩa đã xác minh UDP. Gameplay được xác nhận sau join/pong, Auto dùng WSS dự phòng.

**Dừng:** bấm **Dừng server**; giữ Mongo/map/outbox/secret. Đóng game không tự dừng server. Công cụ không tự chiếm cổng7777, không dừng stack thủ công/Public hoặc reset Serve của ứng dụng khác. File ownership `.vpn-state.json` và lock nằm ngoài Git. Không xóa file ownership khi stack/Serve đang hoạt động.

CLI tương đương từ Backend (Setup có cửa sổ tương tác, các action khác trả JSON không có secret):

```powershell
powershell -File Deploy/Host/manage-vpn.ps1 -Action Setup
powershell -File Deploy/Host/manage-vpn.ps1 -Action Start
powershell -File Deploy/Host/manage-vpn.ps1 -Action Status
powershell -File Deploy/Host/manage-vpn.ps1 -Action Stop
```

Nếu host đã chạy thủ công, hãy tự dừng đúng stack/Serve cũ trước khi dùng công cụ mới. Đừng dùng `tailscale serve reset` để xử lý khi Serve còn phục vụ ứng dụng khác. Công cụ bảo lưu volume map riêng `game-host-maps`; không thêm `-v` vào lệnh down.

Kiểm thử bắt buộc hai PC: host ở mạng A, client ở mạng B cùng tailnet; import mã → HTTPS → login → tạo/join → hai map/bot → kết quả/chơi lại. Chặn UDP ở client thử Auto/WSS; bỏ chặn sau test. Dùng hai process trên một PC không thay bước này.

## Tạo cấu hình một lần

```powershell
# Docker Desktop đang chạy. Script hỏi mật khẩu Swagger >=12 ký tự.
powershell -File Deploy/Host/configure-host.ps1 -Mode vpn
# Hoặc public, dùng domain RIÊNG, không dùng hostname AWS:
powershell -File Deploy/Host/configure-host.ps1 -Mode public -Domain your-host.duckdns.org -AcmeEmail you@example.com
```

Script tạo ba secret riêng, mã phòng mặc định và bcrypt qua stdin; không ghi password vào dòng lệnh. File `.env.host.<mode>` chỉ current-user đọc và không nằm trong Git. Có file rồi thì giữ nguyên, không tự thay key/DB. Không copy secret từ `.env` Local hoặc AWS.

## VPN

Cài và đăng nhập Tailscale trên host và máy chơi, bật HTTPS/Serve theo hướng dẫn Phase2. API/WSS dùng hostname HTTPS `.ts.net` hợp lệ; UDP dùng IP100.x của host.

```powershell
powershell -File Deploy/Host/preflight-host.ps1 -Mode vpn -EnvFile Deploy/Host/.env.host.vpn
docker compose -f compose.host.yaml --env-file Deploy/Host/.env.host.vpn --profile vpn up -d --build
tailscale serve --bg http://127.0.0.1:18080
```

`caddy-vpn` chỉ publish loopback18080; không mở18080 từ mạng ngoài. Trong PowerShell **Administrator**, chỉ khi cần mở rule riêng cho UDP VPN:

```powershell
powershell -File Deploy/Host/setup-firewall.ps1 -Action Enable -Mode Vpn
```

## Public

DNS phải trỏ về host; router forward TCP80/443 và UDP7777. CGNAT hoặc chính sách mạng chặn inbound cần giải quyết trước, không coi compose là đã mở được Internet.

```powershell
powershell -File Deploy/Host/preflight-host.ps1 -Mode public -EnvFile Deploy/Host/.env.host.public
docker compose -f compose.host.yaml --env-file Deploy/Host/.env.host.public --profile public up -d --build
# Chạy riêng trong PowerShell Administrator:
powershell -File Deploy/Host/setup-firewall.ps1 -Action Enable -Mode Public
```

Preflight dùng file env thực, kiểm tra secret và render compose. `-StaticOnly` bỏ kiểm tra host đang chạy; kết quả đó không chứng minh kết nối từ xa.

## Dừng và giữ dữ liệu

```powershell
docker compose -f compose.host.yaml --env-file Deploy/Host/.env.host.vpn --profile vpn down
# Public: đổi cả env-file và --profile thành public.
tailscale serve reset
# Administrator, chỉ gỡ rule của nhóm JungleMaze-Host:
powershell -File Deploy/Host/setup-firewall.ps1 -Action Disable
```

Không thêm `-v`: Mongo/outbox có volume riêng của `junglemaze-host`. Dừng GameServer dev đang chiếm UDP7777 trước khi bật host này. Không publish Mongo27017, API5000, control7780 hoặc WS7781. Caddy chặn `/internal`, chỉ Swagger yêu cầu Basic auth. LAN/VPN/public đều cần account: JWT qua API HTTPS → ticket v4 → join RoomCode. Tạo/list/xóa phòng cần JWT; Guest cũ chỉ xem lịch sử, không lấy vé chơi mới. Offline luyện tập không đi qua stack này.

### Kiểm lại LAN dành cho dev

Tại root source, dùng `dotnet run --project Backend/Tests/LanHostChecks -c Release` (mã, SAN/pin/thời hạn và HTTPS/WebSocket thật trên loopback) và `powershell -NoProfile -File Backend/Tests/LanHostChecks/HostScriptChecks.ps1` (cổng/mode/phiên đang hoạt động, không sửa firewall thật).

Để kiểm cổng LAN với API/Mongo/GameServer thật: chuẩn bị image Local `backend-api:latest`, `backend-game-server:latest`, `caddy:2`, `mongo:7.0.43` và GameServer Release, rồi chạy `python Backend/Tests/LanHostChecks/ProxyIntegration.py`. Công cụ dùng project/volume riêng có ID ngẫu nhiên, chỉ publish loopback, tạo credential/chứng chỉ tạm và dọn chúng khi kết thúc; không gọi AWS hoặc sửa profile Windows. JSON kết quả ở `Backend/out/lan-host-tests/`. Bài này kiểm chính sách proxy/TLS và account; không thay nghiệm thu Unity LAN hai PC.

Cấu hình LAN đặt `PUBLIC_HOSTING=true`, `ALLOW_LOCAL_HTTP=false`: đây là chính sách API yêu cầu kết nối HTTPS từ proxy nội bộ, không phải yêu cầu Internet hoặc mở thêm cổng. Chứng chỉ tự ký LAN chỉ được game tin sau xác nhận riêng; không cài CA hệ thống.

### Đối chiếu lỗi đồ họa khi test Windows

Nếu bản Windows crash trước khi hiện menu, dev có thể thử `Lastaway.exe -force-d3d11`. Đây là tùy chọn chẩn đoán theo máy, không thay Unity/URP hoặc mặc định renderer của cả team. Runner hiện có nhận `-ForceD3D11` (cả `test-unity-remote.ps1` và `test-unity-online.ps1`), ghi rõ chế độ ở kết quả. Không tự dùng một lượt Direct3D11 PASS để kết luận Direct3D12 đã hết lỗi. Luôn đợi `/health` Local trả200 trước khi gọi runner sau khi vừa bật Docker.
