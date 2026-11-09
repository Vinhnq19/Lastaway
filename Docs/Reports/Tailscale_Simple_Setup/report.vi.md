# Tailscale setup đơn giản — 09/11/2026

## 1. Kết quả bàn giao

**PASS Local**, bao gồm backend thật qua Tailscale của chính PC và hai process Windows. **Hai PC ở hai mạng khác nhau: NOT_TESTED**; không suy diễn số RTT đo trên cùng PC thành độ trễ WAN.

Bản game đầy đủ: `D:/Unity/Lastaway/Builds/Tailscale-Setup/Lastaway.exe` cùng `_Data`, DLL và MonoBleedingEdge. Build Unity6000.3.21f1, job `build-618eeb33c6`, 154.18s,0 error/6 warning. Bản này dùng thử Local, không deploy AWS hoặc push Git trong tác vụ. Đã bỏ thư mục debug DoNotShip riêng; giữ runtime cần thiết.

## 2. Đã triển khai

- Bảng Máy chủ cũ có **VPN · Tailscale**, phần người chơi nhập mã và phần host Setup/Start/Stop/Status/copy. Editor/EXE trong source tự tìm Backend; host có thể chọn thư mục source tin cậy một lần. Client không cần source/Docker.
- Mã `LASTAWAY-VPN1` chỉ có tên host/IP100.64/10/cổng/hostname ts.net; payload giới hạn và đúng version, không chứa RoomCode/password/JWT. Client kiểm DNS khớp IP và HTTPS/TLS trước khi lưu Auto. Chỉ mở tab VPN không đổi endpoint AWS; hủy/chuyển tab bỏ callback kiểm tra cũ.
- Windows adapter gọi fixed script bất đồng bộ; setup có cửa sổ riêng để người dùng tự nhập mật khẩu/UAC/đăng nhập/HTTPS. Không đưa mật khẩu vào argv, log, mã kết nối hoặc repository. Người dùng đã hoàn tất Setup thật; không tự tạo lại secret.
- Script có lock, kiểm ownership stack/Serve/foreground/Funnel/cổng. Không chiếm stack Public/thủ công/checkout khác; dừng chỉ tài nguyên mình quản lý và giữ volume. Setup-only Serve cũng dọn được; không dùng `serve reset`.
- Cổng proxy mới **127.0.0.1:18080 → Caddy container8080** để tránh Unity MCP8080. Cấu hình cũ không bị tự đổi port; gặp xung đột báo rõ. UDP bind IP Tailscale host; DB/API/control/WS không publish trực tiếp. Giữ10 người/phòng,5 phòng,50 slot và account-ticket v4.
- Thêm volume map host bền vững; secrets/config host tách khỏi AWS/Local. Compose bỏ các biến nội suy liên quan thừa kế từ môi trường khác, dùng env-file host thực. Sửa module path PowerShell5 khi Unity thừa kế môi trường PS7, tránh lỗi Security module.
- Cập nhật hướng dẫn Host/Phase2/Network Architecture và Graphify local. Không gọi model trả phí hoặc gửi source ra ngoài trong tác vụ.

## 3. Kiểm thử và bằng chứng

| Nhóm | Kết quả |
|---|---|
| Safety script không thao tác live | 22/22 PASS: endpoint sai, Serve ứng dụng khác/foreground/Funnel, stack Public/thủ công, cổng chiếm, PS5 module |
| Host compose/preflight | Static fixture PASS; khác với bằng chứng host runtime bên dưới |
| Backend solution | Build PASS,0 warning/0 error |
| Unity EditMode | 1197/1197 PASS |
| Unity Lobby PlayMode | 30/30 PASS: kích thước/khôi phục form, host async, đóng panel không tự Stop |
| Windows native | Mở UI mới và thoát bình thường exit0; nhập mã thật thấy API HTTPS thông, xác nhận file lưu Auto/API/WSS không chứa credential; endpoint trước test được khôi phục |
| Host thật | Start readiness PASS, register/login HTTPS PASS, thiếu JWT bị401, JWT tạo phòng, catalog hai map chính đúng |
| NetSmoke | Udp/Wss/Auto đều1 requested/connected/joined,0 failed; Auto dùng UDP7779 không listener để chứng minh fallback WSS, không đổi firewall |
| Stop–Start | Account và catalog map tồn tại; config/secret giữ nguyên ngoại trừ bind IP VPN được công cụ quản lý |
| Hai Windows client | WSS + UDP/Auto,2 người +4 bot, khoảng215s; movement/bo/HP/result/history/replay PASS; cả hai exit0, không timeout pong |
| Hai PC/mạng khác nhau | NOT_TESTED; cần checklist thủ công |
| AWS | NOT_TESTED trong tác vụ này; không gọi deploy/Start/Stop AWS |

Flow Windows dùng lịch chính, không sửa HP/thời gian để ép kết quả. Không đo FPS60 hoặc tuyên bố sức chứa từ bài setup này. Account/Guest fixture do test API tạo có thể còn trong DB host; không xóa dữ liệu người dùng. Mật khẩu cấu hình người dùng không bị thay bằng mật khẩu test.

Các lượt fail trước đã được sửa hoặc giải quyết: icon kit không tồn tại, bộ kiểm đường dẫn ném exception, PowerShell5 nạp nhầm module PS7, proxy8080 trùng MCP; HTTPS chưa bật được ghi BLOCKED cho tới khi người dùng xác nhận thật. Không lấy kết quả bị chặn làm PASS.

## 4. Cách dùng và trạng thái cuối

**Host:** bật Docker/Tailscale → EXE hoặc Editor → Máy chủ → VPN Tailscale → **Bật server**. Lần đầu trên máy khác dùng Thiết lập và tự xác nhận UAC/mật khẩu/HTTPS. Muốn chơi trên host vừa bật thì bấm **Kiểm tra / Kết nối**, đăng nhập tài khoản backend host. Sao chép mã endpoint cho bạn chơi; lời mời tailnet gửi riêng, mã phòng chia sẻ riêng.

**Client:** cài/đăng nhập Tailscale, nhận lời mời cùng tailnet → dán mã → Kiểm tra/Kết nối → đăng ký/đăng nhập → tạo/vào phòng → chọn map, thêm bot đủ6 rồi bắt đầu. Không cần nhập ba endpoint riêng. Tài khoản AWS không tự có trong DB self-host.

Đã đóng mọi client tác vụ mở, dừng stack và Serve test. **Giữ cấu hình host, rule VPN do người dùng xác nhận, Tailscale đăng nhập, Mongo/map/outbox volume và Docker Desktop** để lần sau Bật server nhanh. Đóng game không tự Stop host; dùng nút Dừng server khi hết buổi. Không đụng AWS, nguồn Unity hoặc credential khác; camera Follow Player/spectator được giữ.

### Provenance và Editor riêng

Git vẫn pin6000.3.15f1 cho team; chỉ checkout của Vinh dùng21 và package lock local như trước. Bản Windows đã được đối chiếu **đúng source snapshot lúc build và đúng player fingerprint**, rồi kiểm gameplay thực. Sau nghiệm thu, khôi phục thay đổi tự serialize của Editor21 trong Boot Light2D/ProjectSettings về baseline dùng chung để không commit migration vào feature này; bản snapshot input21 được lưu riêng trong checkpoint.

Do đó receipt của **artifact thử Local21** không đồng nhất byte với shared scene/settings đã khôi phục; receipt không bị viết lại. Không dùng artifact này làm chứng cứ release GHCR từ commit sạch: lần deploy tiếp theo phải qua build/Full gate như quy trình. Chưa chạy test bằng Editor15 trên máy dev khác.

`evidence.zip` chứa kết quả JSON/CSV và ảnh gameplay/UI đã chọn; không chứa env/secret, config endpoint gốc người dùng, mật khẩu, token, RoomCode hoặc ảnh lobby có mã phòng. SHA256SUMS kiểm ba file bàn giao. Graphify có cảnh báo parser UniTask có sẵn; không phải lỗi C# Unity.

## Cập nhật mã kết nối ngắn

Host sao chép dạng `desktop-jdqdiqh.tail5af32d` thay chuỗi Base64; game tự thêm `.ts.net` và UDP7777. Nhận cả hostname đầy đủ và `:port` tùy chọn, giữ mã dài cũ. DNS bắt buộc trả địa chỉ Tailscale, HTTPS vẫn kiểm chứng chỉ. Không cần deploy AWS cho VPN tự host. Bản EXE cũ cần build lại để nhận dạng ngắn.

Kiểm tra phần mã ngắn: PASS 24/24 Unity EditMode codec và 23/23 PowerShell safety checks; chưa build lại Windows EXE hoặc kiểm hai PC cho thay đổi này.
