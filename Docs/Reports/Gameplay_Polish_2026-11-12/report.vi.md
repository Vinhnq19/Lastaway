# Kiểm thử Gameplay Polish — 12/11/2026

## 1. Kết luận hiện tại

**PASS cổng Local mới nhất.** Đã xử lý bot đứng tại spawn, lỗi thiếu dữ liệu world khiến client không gửi input, và giảm cấp phát bộ nhớ trong gameplay. Full bằng Windows client mới + Docker giới hạn 256 MiB đạt; chưa deploy hoặc truy cập AWS.

Bản bàn giao: `D:/Unity/Lastaway/Builds/Bot-Recovery-Main/Lastaway.exe` cùng toàn bộ thư mục. Unity cá nhân **6000.3.21f1**; phiên bản chung trong Git vẫn **6000.3.15f1**. Build `build-a09ec938a2`: 0 lỗi, 2 warning, 306,49 MiB. Không coi warning là lỗi compile.

- SourceHash: `40c799f06c4fb2b4d06c4fca33fd865204e3e1262ba9336fa03e9ad123cbb279`.
- Player fingerprint: `0be25b7fc3b1a2b58441f64a20d4f2e920232d2df686adcb9762b1d02e57a647`.
- Receipt dùng cho pipeline: `Backend/out/bot-world-stream-gc-20261112/local-evidence.json`.
- Báo cáo này gồm cả FAIL cũ và PASS mới; không sửa receipt hoặc giảm gate để nhận PASS.

## 2. Nguyên nhân và sửa đổi

**Bot:** ca cố định MatchId `fb9db6ce-2f53-4e27-8fef-452abc13f29b`, NetId 3 từng sống gần90s mà không dịch chuyển. Bot đổi qua lại giữa hai mục tiêu loot không thể tới, trong khi chỉ nhớ một mục tiêu thất bại. Bản sửa tạm ngừng tìm loot5s theo cấu hình hiện có, chọn bước đi gần qua segment đã kiểm collision; giữ ưu tiên bo/hazard/combat. Đồng thời giữ A* pending, không hủy partial route hợp lệ, rearm timer kẹt một lần và đo tiến triển tích lũy. Không teleport/tăng tốc hoặc sửa HP.

**World stream:** map có hơn64page; hàng đợi reliable WebSocket chứa64message. Bản cũ gửi cả batch, bỏ qua TrySend=false rồi đánh dấu revision đã gửi; client thiếu page giữ trạng thái chưa sẵn sàng và không phát input. Sửa bằng cursor từng phiên, tối đa8page/tick, giữ nguyên revision bất biến tới khi gửi đủ; dành16slot cho control/inventory/reply, không tăng hàng đợi. Không tiến triển5s thì ngắt rõ ServerBusy. Điều khiển reliable không gửi được không còn bị bỏ im lặng. Không đổi wire protocol/account guard.

**Cấp phát/GC:** một lượt Full có5dropped tick đi cùng GC pause305,798ms. Đã bỏ Floor.Items.ToArray cho từng actor từng tick, tái dùng snapshot buffer, writer theo batch và bỏ copy page trung gian. Phép đo100lần snapshot giảm1.401.600→488.000byte, khoảng65%; không phải giảm65% RAM toàn server. Lượt Full sau sửa không dropped tick; không cam kết mọi pause GC/giật mạng đã biến mất.

Các sửa checkpoint trước vẫn giữ: nhãn tên gọn, camera spectator theo map/runtime, hàng đợi world action có giới hạn và xử lý sau movement, chờ nhặt trên client, interpolation thích ứng jitter, Local Docker dành cho dev. Prediction/reconciliation hiện có không bị thay bằng mô phỏng thứ hai.

## 3. Kiểm module và ca lỗi

- Cổng mới chạy lại9suite: Network, DualTransport, MapCatalog, Bot, MatchResult, Combat, Inventory, World, Match — PASS.
- DualTransport60assertion: fixture lớn hơn64page, chỉ nhận2page/tick, revision đổi liên tục vẫn nhận đủ revision đầu; không tiến triển thì ngắt có giới hạn. **Chạy code sender cũ với cùng fixture: FAIL đúng ca thiếu page; khôi phục bản sửa: PASS.**
- WorldChecks177assertion: thứ tự/nội dung snapshot, tái dùng buffer không giữ đồ đã xóa, snapshot độc lập cũ không bị sửa, giảm cấp phát và roundtrip page.
- WorldNetwork Mixed82: socket UDP/WebSocket thực, loot tranh, inventory, combat và20request dồn nhận đủ phản hồi. Airdrop215: trứng/loot/stream và fixture seed42 ổn định.
- Unity tại checkpoint client:1.254 EditMode và215 PlayMode PASS. Không chạy lại hai bộ này chỉ vì sửa server; Windows build hiện tại được kiểm trực tiếp dưới đây.
- Các con số là assertion/check, có kiểm lặp theo slice, không phải số kịch bản độc lập.

## 4. Bot và sức chứa

- Seed lỗi cũ:1.693check PASS,18/18bot dịch chuyển. Bot NetId3 dịch chuyển tối đa15,11cell từ origin trước khi bị loại ở42,3s.
- Stress realtime300s:5phòng/50slot,45bot đồng thời,90lượt sống qua replay. **0bot sống≥15s đứng nguyên chỗ** theo ngưỡng displacement0,5cell.1.730check PASS.
- Tick trung bình29.998Hz; dropped0; max tick49.55ms; CPU một core max36.52%; RSSmax96.57MiB; pathnodesmax1455.0/1.500.
- Đây là harness in-process, không phải50người thật/Docker/WAN. Displacement là độ lệch lớn nhất từ origin, không phải tổng đường đi; portal cũng làm thay đổi vị trí. Bot được dừng hợp lệ để bắn, phá vật, hồi máu hoặc phản ứng.

## 5. Windows + Docker thật mới nhất

| Bài kiểm | Kết quả |
|---|---|
| WSS1người+9bot | PASS300,03s; exit0; input29.99Hz khi còn điều khiển; snapshot30.00Hz trong InGame; pong300/300, timeout0 |
| Độ trễ loopback WSS | p50/p95/p99=33.34/33.59/38.53ms; không phải RTT AWS |
| Auto2client+4bot | PASS136.43/136.29s; cả hai fallback WSS theo cấu hình UDP không có listener; kết quả/history/replay; exit0 |
| Smoke UDP/account/phòng | PASS; giữ account guard và kiểm không được tạo phòng khi thiếu/sai JWT |
| Thị trấn1người+9bot | PASS29.66s qua WSS; profile tăng tốc riêng; kết quả/history/replay; không coi là benchmark300s map này |
| Container GameServer256MiB | PASS96mẫu tick/96mẫu RAM; tick trung bình29.917Hz; CPU một core max41.18%; RAMmax30.98%; dropped0/restart0/OOMfalse |

Đủ roster/view và dữ liệu world vẫn là điều kiện bắt buộc. Không dùng bước spawn làm bằng chứng bot đã đi; không yêu cầu cả10nhân vật sống đủ300s. Main và test profile giữ lịch riêng. Client xác nhận9bot có dịch chuyển trong lượt main; không suy ra bot luôn có chiến thuật tối ưu.

Lượt FAIL ngay trước sửa cấp phát được giữ tại `world-stream-prior-gc-fail/`: các client đã PASS nhưng server có5dropped tick, nên **toàn bài vẫn FAIL**. Bản cũ input0/thiếu dữ liệu cũng giữ trong bằng chứng checkpoint. Catalog volume Local cũ1.1.0 đã được backup tại `Backend/data/maps-before-bot-recovery-20261112`, publish/reload gói generation5 hai map1.2.0; image mới không tự thay catalog volume. Không gộp việc đổi map và sửa queue thành một phép thử một biến.

## 6. Phần chưa nghiệm thu và giới hạn

**NOT_TESTED:** hai PC vật lý, LAN thật/hai mạng, buổi6–10người, AWS/public Internet của release này, chất lượng60FPS/720p và cảm giác/cân bằng PvP người thật. Chưa chứng minh giảm RTT Sydney hoặc VPN.

WSS dự phòng vẫn chịu đặc tính TCP khi mạng mất gói. Bản sửa giải quyết thiếu world page và giảm công việc cấp phát; không thay vị trí region, đường mạng hoặc bỏ TLS. Các lượt local không thay bằng chứng WAN. Shared Editor15f1 chưa được build lại trong lượt này; bản Windows bàn giao dùng21f1 theo lựa chọn cá nhân đã chốt.

## 7. Bàn giao, deploy và trạng thái sau test

Dùng toàn bộ thư mục `Builds/Bot-Recovery-Main`, không chỉ chép EXE. Trong game chọn **Local Docker dành cho dev** → đăng nhập → tạo phòng → chọn Tháp cổ/Thị trấn → thêm9bot → Bắt đầu. Quan sát bot, bo, loot/combat, chết/spectator, kết quả/history rồi chơi lại. Với server source mới cần rebuild Docker; chờ ready trước khi mở client.

```powershell
Set-Location 'D:\Unity\Lastaway\Backend'
docker compose --profile account up -d --build game-server api mongo
docker compose logs --tail 20 game-server
```

Local đã trả `MATCH_PROFILE=main`, memory_limit=0 và giữ stack dev đang chạy như khi bắt đầu. Client tác vụ đã đóng; camera Follow Player, spectator giữ nguyên. Mongo/map/outbox/secret không bị xóa. Không nâng máy hoặc tác động lifecycle AWS.

Bước chuẩn bị registry dùng receipt mới và source đã commit sạch. Chưa gọi Deploy AWS; chưa dùng kết quả Local để ghi PASS AWS. Bằng chứng chính ở `world-stream-full/`, `world-stream-town/`, các log regression và `bot-recovery-evidence.zip`; checksum tại `evidence.sha256`. Graphify cập nhật theo code, cảnh báo parser top-level/UniTask là giới hạn bản đồ, không phải lỗi compiler.
