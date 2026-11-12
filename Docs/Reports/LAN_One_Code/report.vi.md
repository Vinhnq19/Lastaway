# Chọn máy chủ và LAN bằng một mã — 11/11/2026

## 1. Kết luận và phạm vi

Đã tích hợp ba lựa chọn AWS / LAN / Tailscale, mã LAN và chính sách tin cậy riêng trong game, công cụ host và tài liệu. Module/flow Local có bằng chứng PASS; **chưa nghiệm thu LAN hai PC hoặc buổi chơi không Internet**. Không deploy, bật hoặc thay đổi AWS; không commit/push trong tác vụ này. Kiểm bằng Unity cá nhân6000.3.21f1, không đổi pin Editor chung6000.3.15f1.

## 2. Giao diện và vòng đời

Bỏ placeholder ở danh sách chính; Local Docker / Internet tự host chỉ ở phần dev, CLI/profile vẫn dùng được. AWS mặc định khi mở thường; lựa chọn `user` được nhận diện theo cấu hình thực. Luyện tập offline giữ riêng. Mở LAN/Tailscale không đổi account/backend; chỉ xác nhận và health thành công mới áp dụng. Đổi tab, đóng bảng hoặc sửa mã vô hiệu callback cũ. Host có Setup/Start/Status/Stop, chọn Wi-Fi/Ethernet và copy; client có Dán/Kết nối, xác nhận lần đầu. Stop vẫn dùng được khi card mạng đã mất. Không tự dừng host khi đóng game.

Presenter, view, builder và prefab cùng được cập nhật. Upgrade giữ nguyên 3.660 ID prefab cũ; chỉ thêm các điều khiển mới, không dựng lại hàng loạt. Camera Follow Player và spectator không bị thay đổi.

## 3. Tin cậy và host

Mã61 ký tự gồm RFC1918 IPv4, hai cổng và SHA256 leaf certificate; không URL tùy ý, RoomCode, JWT hoặc secret. .NET8 tạo RSA2048, SAN IP, server EKU và hạn một năm. Key ngoài image/repo; owner cấp quyền thư mục riêng. Pin chỉ lưu ở persistentDataPath/lan-trust sau xác nhận và probe không credential; không cài CA Windows. HTTP, tải map, vé/lịch sử và WebSocket dùng cùng chính sách exact pin/origin/IP/time. Không redirect mang thông tin đăng nhập. AWS/Tailscale vẫn TLS hệ điều hành. Resume giữ pin; đổi sang VPN bỏ pin LAN; đổi pin cùng origin vô hiệu phiên account cũ.

Dùng compose.host.yaml/project junglemaze-host và volume hiện có; .env.host.lan tách VPN/AWS. LAN chỉ bind IP đã chọn, UDP7777 + HTTPS8443; API/Mongo/control không publish. PUBLIC_HOSTING=true ở LAN là chính sách API yêu cầu proxy HTTPS, ALLOW_LOCAL_HTTP=false. Caddy chặn/internal, bảo vệ Swagger và chuyển/game tới listener riêng. Khóa chung tránh đổi host đồng thời; kiểm checkout, mode, cổng và snapshot deployment-status. Còn người/grace/bot/trận thì từ chối thay cấu hình; không tự giành cổng hoặc thay Tailscale Serve. Rule chỉ Private + LocalSubnet + IP/interface; rule Public/trùng tên không sở hữu được giữ.

## 4. Kiểm thử đã làm

- LAN host/TLS:43 PASS, gồm malformed code, private IP, SAN, fingerprint, hạn cert, reuse/đổi IP; **socket HTTPS/WebSocket thật** dùng chính LanTls HttpClient/WebSocket code trên.NET. Chỉ vỏ type UnityWebRequest được stub; không gọi đó là bằng chứng Unity LAN hai PC.
- Host script:11 PASS với fixture lệnh hệ thống; kiểm chiếm cổng, sai mode, server còn người và nhận lại container đã dừng cùng checkout. Helper firewall thật được gọi trên Wi-Fi Public và từ chối trước thay đổi; không xin UAC hoặc thêm rule thật.
- Proxy:12 PASS với Caddy/API/Mongo/GameServer thật, project/volume ngẫu nhiên, chỉ loopback: HTTPS, register/login, JWT quản lý phòng, catalog, ticket, route game và chặn/internal. Cấu hình/chứng chỉ test được dọn; đây chưa phải kết nối qua router.
- NetworkChecks149, DualTransportChecks53, API/account68 PASS. Các bài này giữ kiểm v4, auth guard, slot và hai đường vận chuyển.
- PlayMode UI34/34 PASS. Full EditMode gần cuối có1.245/1.246 PASS: ca mới resume gọi sai thời điểm trước khi tick xử lý lỗi vé. Đã sửa **fixture** để đi đúng state machine và xác minh lại **61/61 ca** network/trust/VPN bằng phiên Unity CLI sạch, gồm ca resume đó; XML được lưu. Hai ca mới đưa tổng số ca EditMode lên1.246; không ghi một lượt suite thất bại thành PASS. Không sửa luật runtime để né assertion.
- Windows build và các lượt game được ghi theo fingerprint trong JSON/CSV. Hai process chỉ là **một PC**. Các lượt tăng tốc kiểm movement, bo/HP, kết quả/history/replay; không thay nghiệm thu PvP/người thật hoặc benchmark50slot.

## 5. Giới hạn phát hiện

Một lượt hai client Direct3D12 của bản cuối crash native ở khởi tạo graphics, trước Begin MonoManager và trước kết nối server. Không có bằng chứng để quy lỗi này cho LAN; cũng **không tuyên bố mặc định Direct3D12 đã ổn định**. Cùng artifact chạy Direct3D11 với startup cách nhau2,5s đã PASS. Sau khi đóng Editor bị kẹt, bộ nhớ ảo trống tăng lên khoảng10GiB. Lượt mặc định sau đó không crash native: cả hai vào được game, có movement/bo/HP/kết quả/history; một client hết120s vì cờ quan sát replay của runner không được ghi nhận, dù completedMatches=2. Lượt này vẫn giữ FAIL, không suy diễn thành lỗi transport hoặc tự bỏ tiêu chí replay. Dùng flag chẩn đoán rõ ràng, không đổi renderer/URP chung của team. Có warning ComputeBuffer khi thoát ở lượt PASS; chưa xử lý trong phạm vi LAN.

Trong tác vụ, ba container dev từng cùng dừng với exit255, OOMKilled=false; đã khởi động lại cho lượt đối chiếu. Có mẫu free virtual memory khoảng1,7GiB. Chưa đủ bằng chứng kết luận nguyên nhân VM/driver/bộ nhớ hay thao tác ngoài tác vụ. Các lượt API chưa ready và crash không được tính PASS. Không lấy một lượt functional ngắn để kết luận độ mượt hoặc sức chứa tối đa.

Wi-Fi hiện tại là Public: host setup đúng yêu cầu đã từ chối; không tự đổi Windows profile. **NOT_TESTED:** UAC/firewall trên Private thật, nhiều card vật lý, hai PC cùng router, ngắt Internet, chuyển host Tailscale thật. Giữ các giới hạn này khi bàn giao.

## 6. Chạy và kiểm lại

Host tin cậy: bật Docker, đặt mạng Wi-Fi/Ethernet của mình thành Private trong Windows → game: Máy chủ → LAN → chọn mạng → Thiết lập (nhập mật khẩu Swagger và tự xác nhận UAC) → Bật server → Sao chép mã. Client chỉ cần game, cùng router: Dán → Kết nối → xác nhận mã do host gửi → đăng ký/đăng nhập → tạo/vào phòng, map/bot, chơi. Mã máy chủ khác mã phòng. Host muốn chơi cũng kết nối tới mã của mình. Hết buổi dùng Dừng server; đóng game không dừng host.

Build: toàn bộ thư mục Builds/LAN-One-Code. Trên máy đang gặp lỗi graphics có thể chạy `Lastaway.exe -force-d3d11`; giữ ghi chú hạn chế mặc định ở trên. Test Local dev dùng phần dev Editor/Development Build hoặc -clientConfig, không đưa Local trở lại danh sách chính của Release.

Dev chạy lại43 ca bằng `dotnet run --project Backend/Tests/LanHostChecks -c Release`;11 ca script bằng `powershell -NoProfile -File Backend/Tests/LanHostChecks/HostScriptChecks.ps1`. ProxyIntegration.py tạo/dọn stack loopback riêng, yêu cầu image đã chuẩn bị. Hướng dẫn đầy đủ ở Backend/Deploy/Host/README.md và Docs/Phase1_First4Tasks.vi.md; kiến trúc ở Docs/Network_Architecture.vi.md.

## 7. Bàn giao

JSON/CSV ghi fingerprint và trạng thái từng lượt; evidence.zip + SHA256 giữ log/check/ảnh đã chọn, không chứa.env, private key, password, JWT, ticket hoặc RoomCode. Source vẫn trên feature branch chưa commit. Bản sửa Map100x100.prefab và các file riêng có từ trước được giữ. Graphify cập nhật AST và semantic LAN hiện hành; cảnh báo parser UniTask là giới hạn extractor, không phải lỗi compile C#. Không gọi model API trả phí.

Bản build cuối:255,0s,306,49MiB,0 error/42 warning; Unity6000.3.21f1. Hai client bản này qua WSS+UDP và lượt cố ý làm đường chính không tới được đều PASS khi dùng Direct3D11; mỗi client đủ8/8 quan sát, exit0. Đồ họa mặc định giữ hạn chế ghi ở mục5. Đây là hạn chế nghiệm thu còn mở, không được ghi toàn bộ Windows runtime là PASS vô điều kiện.

Đã trả container dev về MATCH_PROFILE=main,10 người/phòng,5phòng,50slot rồi dừng stack Local do tác vụ mở. Không xóa volume dev hoặc host; fixture loopback tự xóa volume riêng. Key/.env tạm của fixture được xóa, cấu hình AWS/Tailscale và dữ liệu người dùng được giữ. Boot/ProjectSettings chỉ hoàn nguyên các field serialize tự sinh đã đối chiếu; bản sửa map có từ trước được giữ. Editor được mở lại bằng Direct3D11 theo phiên cá nhân, không đổi Graphics API trong project.

registry-build.json giữ hash **đầu vào thật tại thời điểm build**. Việc trả field serialize Editor riêng về baseline có thể làm hash working tree khác; không sửa giả metadata và không coi artifact này là đã qua gate publish GHCR/AWS cho commit tương lai. Publish sau này phải build/gate theo source đã chốt.

Graphify bỏ dữ liệu JSON báo cáo khỏi nhóm code/config để cập nhật báo cáo không kích rebuild kiến trúc vô ích; vẫn giữ nhóm thư mục báo cáo. Semantic được trích lại có phạm vi LAN và các phần topology đã rà, không gọi mảnh semantic stale cũ là hiện hành.
