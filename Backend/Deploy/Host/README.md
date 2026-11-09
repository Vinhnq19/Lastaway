# Tự host backend Local / VPN / public

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
