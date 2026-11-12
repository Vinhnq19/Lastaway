# Kiểm thử Gameplay Polish — 12/11/2026

## 1. Kết luận hiện tại

**PASS cổng Local mới nhất.** Đã xử lý bot đứng tại spawn, lỗi thiếu dữ liệu world khiến client không gửi input, và giảm cấp phát bộ nhớ trong gameplay. Full bằng Windows client mới + Docker giới hạn 256 MiB đạt; **AWS đã deploy và PASS Full** trong lượt tiếp theo cùng ngày, chi tiết cuối phần7.

**Bản đóng gói hiện tại:** `D:/Unity/Lastaway/Builds/Lastaway-Release/Lastaway.exe`, **152,14 MiB**, job `build-0aea4c149b`, 0 lỗi/0 warning. Chỉ Boot + Game và nén LZ4. Bản 306 MiB dùng cho các lượt Full trước đã được dọn theo yêu cầu; bằng chứng/fingerprint cũ vẫn giữ để đối chiếu. Unity cá nhân 6000.3.21f1; Git chung vẫn 6000.3.15f1.

- SourceHash của bản Full cũ: `40c799f06c4fb2b4d06c4fca33fd865204e3e1262ba9336fa03e9ad123cbb279`.
- Player fingerprint của bản Full cũ: `0be25b7fc3b1a2b58441f64a20d4f2e920232d2df686adcb9762b1d02e57a647`.
- Receipt dùng cho pipeline: `Backend/out/bot-world-stream-gc-20261112/local-evidence.json`.
- Báo cáo này gồm cả FAIL cũ và PASS mới; không sửa receipt hoặc giảm gate để nhận PASS.

## 2. Nguyên nhân và sửa đổi

**Bot:** ca cố định MatchId `fb9db6ce-2f53-4e27-8fef-452abc13f29b`, NetId 3 từng sống gần90 s mà không dịch chuyển. Bot đổi qua lại giữa hai mục tiêu loot không thể tới, trong khi chỉ nhớ một mục tiêu thất bại. Bản sửa tạm ngừng tìm loot5 s theo cấu hình hiện có, chọn bước đi gần qua segment đã kiểm collision; giữ ưu tiên bo/hazard/combat. Đồng thời giữ A* pending, không hủy partial route hợp lệ, rearm timer kẹt một lần và đo tiến triển tích lũy. Không teleport/tăng tốc hoặc sửa HP.

**World stream:** map có hơn64page; hàng đợi reliable WebSocket chứa64message. Bản cũ gửi cả batch, bỏ qua TrySend=false rồi đánh dấu revision đã gửi; client thiếu page giữ trạng thái chưa sẵn sàng và không phát input. Sửa bằng cursor từng phiên, tối đa8page/tick, giữ nguyên revision bất biến tới khi gửi đủ; dành16 slot cho control/inventory/reply, không tăng hàng đợi. Không tiến triển5 s thì ngắt rõ ServerBusy. Điều khiển reliable không gửi được không còn bị bỏ im lặng. Không đổi wire protocol/account guard.

**Cấp phát/GC:** một lượt Full có5dropped tick đi cùng GC pause305,798 ms. Đã bỏ Floor.Items.ToArray cho từng actor từng tick, tái dùng snapshot buffer, writer theo batch và bỏ copy page trung gian. Phép đo100 lần snapshot giảm1.401.600→488.000 byte, khoảng65%; không phải giảm65% RAM toàn server. Lượt Full sau sửa không dropped tick; không cam kết mọi pause GC/giật mạng đã biến mất.

Các sửa checkpoint trước vẫn giữ: nhãn tên gọn, camera spectator theo map/runtime, hàng đợi world action có giới hạn và xử lý sau movement, chờ nhặt trên client, interpolation thích ứng jitter, Local Docker dành cho dev. Prediction/reconciliation hiện có không bị thay bằng mô phỏng thứ hai.

## 3. Kiểm module và ca lỗi

- Cổng mới chạy lại9suite: Network, DualTransport, MapCatalog, Bot, MatchResult, Combat, Inventory, World, Match — PASS.
- DualTransport60assertion: fixture lớn hơn64page, chỉ nhận2page/tick, revision đổi liên tục vẫn nhận đủ revision đầu; không tiến triển thì ngắt có giới hạn. **Chạy code sender cũ với cùng fixture: FAIL đúng ca thiếu page; khôi phục bản sửa: PASS.**
- WorldChecks177assertion: thứ tự/nội dung snapshot, tái dùng buffer không giữ đồ đã xóa, snapshot độc lập cũ không bị sửa, giảm cấp phát và roundtrip page.
- WorldNetwork Mixed82: socket UDP/WebSocket thực, loot tranh, inventory, combat và20request dồn nhận đủ phản hồi. Airdrop215: trứng/loot/stream và fixture seed42 ổn định.
- Unity tại checkpoint client:1.254 EditMode và215 PlayMode PASS. Không chạy lại hai bộ này chỉ vì sửa server; Windows build hiện tại được kiểm trực tiếp dưới đây.
- Các con số là assertion/check, có kiểm lặp theo slice, không phải số kịch bản độc lập.

## 4. Bot và sức chứa

- Seed lỗi cũ:1.693check PASS,18/18 bot dịch chuyển. Bot NetId3 dịch chuyển tối đa15,11 cell từ origin trước khi bị loại ở42,3 s.
- Stress realtime300 s:5 phòng/50 slot,45 bot đồng thời,90lượt sống qua replay. **0 bot sống≥15 s đứng nguyên chỗ** theo ngưỡng displacement0,5 cell.1.730check PASS.
- Tick trung bình29.998 Hz; dropped0; max tick49.55 ms; CPU một core max36.52%; RSSmax96.57 MiB; pathnodesmax1455.0/1.500.
- Đây là harness in-process, không phải50 người thật/Docker/WAN. Displacement là độ lệch lớn nhất từ origin, không phải tổng đường đi; portal cũng làm thay đổi vị trí. Bot được dừng hợp lệ để bắn, phá vật, hồi máu hoặc phản ứng.

## 5. Windows + Docker thật mới nhất

| Bài kiểm | Kết quả |
|---|---|
| WSS1 người+9 bot | PASS300,03 s; exit0; input29.99 Hz khi còn điều khiển; snapshot30.00 Hz trong InGame; pong300/300, timeout0 |
| Độ trễ loopback WSS | p50/p95/p99=33.34/33.59/38.53 ms; không phải RTT AWS |
| Auto2client+4 bot | PASS136.43/136.29 s; cả hai fallback WSS theo cấu hình UDP không có listener; kết quả/history/replay; exit0 |
| Smoke UDP/account/phòng | PASS; giữ account guard và kiểm không được tạo phòng khi thiếu/sai JWT |
| Thị trấn1 người+9 bot | PASS29.66 s qua WSS; profile tăng tốc riêng; kết quả/history/replay; không coi là benchmark300 s map này |
| Container GameServer256 MiB | PASS96 mẫu tick/96 mẫu RAM; tick trung bình29.917 Hz; CPU một core max41.18%; RAMmax30.98%; dropped0/restart0/OOMfalse |

Đủ roster/view và dữ liệu world vẫn là điều kiện bắt buộc. Không dùng bước spawn làm bằng chứng bot đã đi; không yêu cầu cả10nhân vật sống đủ300 s. Main và test profile giữ lịch riêng. Client xác nhận9 bot có dịch chuyển trong lượt main; không suy ra bot luôn có chiến thuật tối ưu.

Lượt FAIL ngay trước sửa cấp phát được giữ tại `world-stream-prior-gc-fail/`: các client đã PASS nhưng server có5dropped tick, nên **toàn bài vẫn FAIL**. Bản cũ input0/thiếu dữ liệu cũng giữ trong bằng chứng checkpoint. Catalog volume Local cũ1.1.0 đã được backup tại `Backend/data/maps-before-bot-recovery-20261112`, publish/reload gói generation5 hai map1.2.0; image mới không tự thay catalog volume. Không gộp việc đổi map và sửa queue thành một phép thử một biến.

## 6. Phần chưa nghiệm thu và giới hạn

**NOT_TESTED:** hai PC vật lý, LAN thật/hai mạng, buổi6–10 người, chất lượng60FPS/720p và cảm giác/cân bằng PvP người thật. AWS từ một PC đã được kiểm, nhưng chưa có đối chiếu có kiểm soát để kết luận giảm RTT Sydney/VPN; Thị trấn chưa có lượt Full AWS riêng.

WSS dự phòng vẫn chịu đặc tính TCP khi mạng mất gói. Bản sửa giải quyết thiếu world page và giảm công việc cấp phát; không thay vị trí region, đường mạng hoặc bỏ TLS. Các lượt local không thay bằng chứng WAN. Một số log lúc thoát Unity còn cảnh báo cleanup ComputeBuffer; mọi client nghiệm thu đều exit0. Chưa điều tra nguồn cảnh báo này, không coi là đã sửa. Shared Editor15f1 chưa được build lại trong lượt này; bản Windows bàn giao dùng21f1 theo lựa chọn cá nhân đã chốt.

## 7. Bàn giao, deploy và trạng thái sau test

Dùng toàn bộ thư mục `Builds/Lastaway-Release`, không chỉ chép EXE. Trong game chọn **Local Docker dành cho dev** → đăng nhập → tạo phòng → chọn Tháp cổ/Thị trấn → thêm9 bot → Bắt đầu. Quan sát bot, bo, loot/combat, chết/spectator, kết quả/history rồi chơi lại. Với server source mới cần rebuild Docker; chờ ready trước khi mở client.

```powershell
Set-Location 'D:\Unity\Lastaway\Backend'
docker compose --profile account up -d --build game-server api mongo
docker compose logs --tail 20 game-server
```

Local đã trả `MATCH_PROFILE=main`, memory_limit=0 và giữ stack dev đang chạy như khi bắt đầu. Client tác vụ đã đóng; camera Follow Player, spectator giữ nguyên. Mongo/map/outbox/secret không bị xóa. Không nâng máy hoặc tác động lifecycle AWS.

Bước chuẩn bị registry dùng receipt mới và source đã commit sạch. Phần Local không thay bằng chứng WAN; AWS được kiểm bằng lượt độc lập dưới đây. Bằng chứng chính ở `world-stream-full/`, `world-stream-town/`, các log regression và `bot-recovery-evidence.zip`; checksum tại `evidence.sha256`. Graphify cập nhật theo code, cảnh báo parser top-level/UniTask là giới hạn bản đồ, không phải lỗi compiler.

Chuẩn bị GHCR **PASS**: release `ghcr-20261112-160052-b6e76560`, source commit `b6e76560`; hai image private đã publish theo digest, bundle23.206 byte và checksum hợp lệ. Build/push API24,708 s, GameServer11,567 s; metadata ghi compressed OCI layer delta, không phải số byte truyền trên dây. Xem `registry-preparation.json`/`registry-manifest.json`. PrepareOnly không truy cập EC2. AWS được deploy ở lượt tiếp theo; không push Git. Commit bàn giao tài liệu sau đó không đổi fingerprint code của release.


### Deploy AWS — kết quả Full từ PC

**PASS**, release `ghcr-20261112-160052-b6e76560`, source commit `b6e76560`. Pipeline9 phút37,47 giây; pull image11,209 giây; giữ EC2 **Running**, t3.small, IP13.239.10.69. Không Start/Stop/nâng máy, không mở thêm cổng hoặc migration DB. Kiểm lại symlink current và image thực tế khớp digest đã publish.

- API và GameServer được thay; Mongo và Caddy giữ nguyên container, private config được bảo toàn. Mongo/map/outbox/secret không bị xóa. Kiểm read-only sau deploy xác nhận cả4container Running, restartCount0.
- WSS1 người+9 bot: **PASS300,02 s**, input30.00 Hz khi còn điều khiển, snapshot30.00 Hz trong InGame; pong300/300, timeout0, catchup0; cả9 bot có displacement. Có kết quả/history/replay, exit0.
- RTT WSS p50/p95/p99: **166.89/200.15/300.14 ms**. Đây là WAN thực của lượt này, không dùng ngưỡng Local50 ms hoặc suy ra game hết độ trễ.
- Hai client Auto+4 bot: PASS134.14/133.94 s, fallback WSS theo bài cấu hình cổng UDP không có listener; kết quả/history/replay và exit0. RTTp99 khoảng566.69/463.40 ms: vẫn có đuôi trễ mạng, không che bằng RTT trung vị.
- Smoke UDP public7777 và WSS đều join1/failed0; register/login/JWT, tạo/list/xóa phòng PASS. UDP thông trong lượt này không bảo đảm mọi mạng trường/VPN đều cho phép.
- Server: **30.001 Hz**, 101 mẫu tick/99 mẫu RAM; dropped0, CPU một core max27.40%, RAMmax32.55% giới hạn256 MiB; OOMfalse, restart0. CPU Standard; CloudWatch trước lượt khoảng575,84 credit, mẫu sau gần nhất576 credit (có độ trễ lấy mẫu), không phải số tiền thanh toán.
- Guard đã nhận quyết định accept và xác nhận PASS đúng transaction/release. Không cần rollback; không coi lượt này là đã chạy lại bài rollback có chủ đích.
- Client tác vụ đều đóng, phòng test dọn bởi runner. Camera Follow Player, spectator giữ; Local vẫn profile main/memory0. EC2 tiếp tục Running và tiếp tục phát sinh chi phí/tiêu credits theo tài khoản sau khi test kết thúc.

Bằng chứng: `aws-deploy/` gồm deployment/remote-ready/remote-final, digest thực, số đo server, CSV, hai nhóm client, UDP/WSS, ảnh và log. Hai PC vật lý, buổi6–10 người,60FPS/720p và Full AWS riêng map Thị trấn vẫn **NOT_TESTED**. Dùng bản đóng gói mới `Builds/Lastaway-Release/Lastaway.exe` cùng toàn bộ thư mục → **Online · AWS** → đăng nhập → tạo/vào phòng → chọn map/bot → Bắt đầu.


### Đóng gói lại Windows — 152,14 MiB

Đã đổi scene mặc định còn Boot và Game; giữ nguyên source SampleScene/GameConfig/GameMain để tham khảo. Build mới dùng `compress_lz4`, `strict_mode`, `detailed_report`; không Development. Cấu hình sử dụng MCP được ghi trong AGENTS.md và hướng dẫn deploy để tránh lặp lại build không nén. Bản cũ152,42 MiB làm đối chiếu; bản mới152,14 MiB thay cho306,86 MiB. Runtime/editor/gameplay không bị đổi để giảm dung lượng.

- Fingerprint source/player của bản mới đã khớp; xem `compact-client/registry-build.json` và `packaging.json`. Không sửa registry-build.json thủ công.
- Tháp cổ: hai Windows client, WSS và UDP, cùng PASS33,59/33,55s, gồm movement, bo/HP, kết quả/history/replay, exit0.
- Thị trấn: một người+9bot qua WSS, PASS34,71s, kết quả/history/replay, exit0; profile test tăng tốc. Lượt đầu của wrapper dừng ở kiểm quyền Guest trước khi mở player, phù hợp quota5yêu cầu tạo phòng ẩn danh/phút khi chạy nối tiếp. Lượt độc lập PASS, không đổi API/account/rate limit.
- Đây là kiểm đóng gói/khởi động/flow trên Local, không phải một receipt Full300s mới hoặc lượt AWS mới cho artifact này. Backend AWS đã PASS trước đó vẫn giữ; lần phát hành backend tiếp theo cần receipt khớp player hiện tại.
- Xóa `Builds/Bot-Recovery-Main`306,86 MiB sau khi bản mới PASS. `Bot-Recovery` và `Gameplay-Polish` đã không còn ở lần kiểm đường dẫn để dọn; không quy phần dung lượng đó cho thao tác xóa của lượt này. Hiện chỉ giữ `Builds/Lastaway` cũ và `Builds/Lastaway-Release` mới.
- Docker Local lúc bắt đầu đã Stopped; sau test trả main/memory0 rồi Stop, giữ DB/map/outbox. Camera Follow Player/spectator giữ nguyên. Không truy cập/deploy hoặc thay trạng thái AWS trong lượt đóng gói này. Chưa commit các thay đổi đóng gói.
