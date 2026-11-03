# Kiểm thử bằng hai PC thật

Chỉ điền PASS khi đã dùng **hai thiết bị vật lý**. Hai cửa sổ, hai process hoặc máy ảo trên một PC không thay thế bài này. Report do launcher tạo luôn ghi `physicalDeviceCount=1`, `physicalTwoPc=NOT_TESTED`; giữ nguyên chúng và thêm biên bản đối chiếu hai máy bên dưới.

## 1. Chuẩn bị chung

- Cả hai PC Windows dùng cùng bản build, hash dữ liệu và profile trỏ cùng backend. Server host có thể nằm trên PC1 hoặc EC2.
- Bật Run In Background, tránh Sleep/đổi Wi-Fi trong bài đo. Ghi giờ UTC và thông tin mạng; không cần đặt điều kiện đồng hồ lệch dưới 50ms để đo RTT.
- Dùng tên khác nhau cho lần đầu. Chia sẻ RoomCode; không chia sẻ JWT, Guest credential, file DPAPI hoặc khóa AWS.
- Mỗi người đăng ký/đăng nhập account. Tạo phòng trong game hoặc Swagger bằng JWT hợp lệ; vào phòng cần vé account v4. Guest cũ chỉ đọc lịch sử, không được join mới.

## 2. Chọn đúng bài theo tuyến

| Tuyến | Cấu hình và cách thực hiện | Điều bắt buộc kiểm trên PC2 |
| --- | --- | --- |
| LAN riêng | PC1 chạy Docker account stack, publish UDP vào IP LAN bằng GAME_PUBLISH_HOST; mở firewall đúng mạng. PC2 dùng IP LAN, transport Udp, handshake v4/gameplay v2. Private test dùng min2. API phải có HTTPS hợp lệ/trusted trên cả hai máy. | Đăng nhập, tải catalog/map và JoinAccepted/pong/gameplay thật từ PC2. Không có chế độ join ẩn danh khi API chưa sẵn sàng; không dùng HTTP qua IP LAN để gửi JWT. |
| VPN Tailscale | Hai máy đăng nhập cùng tailnet. Host dùng compose.host profile vpn, proxy loopback8080 sau Tailscale Serve HTTPS. UDP dùng IP Tailscale; API/WSS dùng hostname HTTPS ts.net của **cùng host**. Private test có thể dùng min2. | UDP trực tiếp/relay bằng tailscale ping/status, rồi ép Wss và Auto. HTTPS phải có certificate hợp lệ. Không mở8080 ra mạng; không bật Funnel. |
| Tự host public | Hostname riêng, khác junglemaze.duckdns.org; TCP80/443 và UDP7777 forward về PC1. Public dùng main. PC2 phải ở mạng ngoài, như hotspot, để tránh chỉ kiểm hairpin của router. | DNS, HTTPS, Unity WSS và UDP từ mạng ngoài. CGNAT hoặc mạng chặn toàn bộ tuyến được ghi BLOCKED. |
| AWS | Hai PC dùng hostname AWS, HTTPS/WSS443 và UDP7777. Giữ main min6. Admin đặt thời lượng/tự Stop trước khi mời tester. | Game đi Internet trực tiếp, không dùng SSH/SSM tunnel để thay kết quả gameplay. |

Public/AWS cần **ít nhất6 gameplay client**. Với hai PC, chạy3 client trên mỗi máy hoặc hai người thật cộng4 client kiểm thử. Không dùng kịch bản2 người để đòi main bắt đầu, không hạ luật main.

## 3. Thao tác trên mỗi PC

1. Mở PowerShell tại thư mục gốc repo; với gói player-only thì dùng đường dẫn tương ứng đến script/config đã bàn giao.
2. Kiểm HTTPS: `curl.exe --fail --show-error https://TEN-HOST/health`. Không dùng `-k`. GET `/game` bằng HTTP có thể trả400 vì thiếu WebSocket Upgrade; chỉ client join/pong thật mới chứng minh WSS hoạt động.
3. Nhập mã bằng `$env:NET_ROOM_CODE = Read-Host 'Ma phong'`. Profile JSON không chứa mã phòng, password hoặc token.
4. LAN/VPN private test: trên **mỗi máy** chạy1 client với cùng scenario `local2-accelerated.json`:

```powershell
.\Backend\Deploy\test-unity-online.ps1 -Player 'D:\Game\Lastaway.exe' -Config 'D:\Game\client.vpn.json' -Scenario '.\Backend\Tests\OnlineReadiness\scenarios\local2-accelerated.json' -Clients 1 -OutputDirectory '.\Backend\out\two-pc-PC1-LUOT-MOI'
```

PC2 dùng tên output riêng. Profile/client config phải đi cùng đúng server `test`, HTTPS API hoạt động nếu scenario yêu cầu history.

5. Public/AWS main: đổi scenario thành `main6-smoke.json`, `-Clients 3` trên mỗi máy. Đợi đủ6 người và tất cả xác minh cùng map, trưởng phòng bấm Bắt đầu; runner tự gửi lệnh này chỉ khi nó là trưởng phòng và điều kiện authoritative đã đạt. Người vào/rời countdown15s sẽ hủy; trưởng phòng phải bắt đầu lại.
6. Chạy lần lượt: ép Udp, ép Wss, Auto. Muốn chứng minh Auto fallback, dùng profile thử có **cổng UDP không có listener**, giữ WSS/API đúng host; không tắt toàn bộ firewall. Report phải ghi selectedTransport Wss và fallbackReason.
7. `Test-NetConnection -Port7777` chỉ thử TCP, không dùng kết quả đó để kết luận UDP. Dùng client UDP thật và quan sát gói/header trên host nếu cần.

## 4. Đối chiếu giữa hai máy

- Cùng RoomId/MatchId; hai phía thấy vị trí, cover collision, bo và HP nhất quán.
- Cùng MapId/MapHash/RulesHash, preview và phiên bản. Thử cả Di tích cổ/Bãi đá, cache lần hai, lỗi tải/thử lại; không được dùng nhầm geometry/vật thể của map trước.
- Người không phải trưởng phòng không đổi map hoặc Start; trưởng phòng rời lúc chờ thì quyền chuyển, người cũ quay lại không giành lại quyền. Sau kết quả về phòng, chọn map và Start lượt mới.
- Mỗi client gửi ít nhất28,5 input/s trong đoạn còn sống; lưu RTT p50/p95/p99 thực, không áp ngưỡng Local cho WAN.
- Không mất kết nối ngoài dự kiến; pong timeout dưới1%; đối chiếu tick/CPU/RAM server.
- Có kết quả hợp lệ: một người thắng **hoặc hòa/đồng hạng khi chết cùng tick**. Không ép phải luôn có một winner.
- Replay sang MatchId mới, view cũ được dọn; history chứa đúng trận mới sau outbox ACK.
- Dùng hai account; PlayerSessionId khác nhau, lịch sử không trộn theo tên. Đóng/mở client phải đăng nhập lại; không tự tạo Guest hoặc bỏ kiểm account khi API lỗi.
- Rời phòng chủ động trả slot ngay. Với resume, giữ app đang chạy, làm gián đoạn mạng rồi nối lại trong grace15s(main)/1s(test) tính từ lúc server phát hiện; proof ở RAM không tự khôi phục sau khi kill/mở lại app.
- Mã sai, phòng đầy và late join bị từ chối; Auto không dùng đường khác để vượt giới hạn.

## 5. Biên bản nghiệm thu riêng

Ghi ngày/UTC, tên hai PC, bản build/hash, mạng mỗi bên, host, transport, direct/relay, MatchId, file report của từng máy và kết luận PASS/FAIL/BLOCKED/INVALID. Không chép credential vào biên bản.

Hai report một-PC giữ nguyên NOT_TESTED. Chỉ biên bản có bằng chứng hai thiết bị mới đánh dấu bài hai-PC đã đạt. Xong buổi tự host thì stop đúng stack; AWS phải Stop và xác nhận Stopped, không chỉ đóng SSH hoặc game.

## Radmin VPN và menu mới (28/10)

1. Hai PC cài Radmin VPN và vào cùng VPN; ghi IP VPN của PC host.
2. Host cấu hình GAME_PUBLISH_HOST=IP_VPN_HOST và GAME_BIND_ADDRESS=0.0.0.0 trong Backend/.env; chạy cả API/DB/GameServer (`docker compose --profile account up -d --build`). Giữ Mongo/control private, API sau HTTPS có chứng chỉ mà hai PC tin cậy; firewall cho UDP7777 ở interface VPN.
3. Hai player chọn Online → Radmin/VPN, nhập IP host và API HTTPS của cùng backend, đăng nhập rồi vào cùng RoomCode. Chờ tải/xác minh map, trưởng phòng chọn map và Bắt đầu. Test2 riêng cần MATCH_PROFILE=test/PUBLIC_HOSTING=false; main vẫn cần6 phiên, không hạ luật main.
4. Đối chiếu di chuyển/bo/HP/kết quả/replay; đóng một player rồi kiểm grace/resume; không dùng hai process trên một PC để ký nghiệm thu hai máy. Ghi NOT_TESTED nếu thiếu PC thứ hai.
5. Radmin chỉ cung cấp mạng VPN, không tự cung cấp API/HTTPS. Nếu chưa có API HTTPS thì ghi BLOCKED cho kết nối mới; không để trống API để bỏ qua account, không tắt kiểm TLS và không gửi JWT qua HTTP remote.
6. Khi xong, đóng player, stop container host; EC2 không liên quan bài này. AWS có checklist Start/DNS/TLS/Stop riêng.
