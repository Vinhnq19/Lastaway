# Kiểm thử Gameplay Polish — 12/11/2026

## 1. Kết luận hiện tại
BẢN SỬA BOT CUỐI: PASS các cổng Local. Ca seed cũ đứng im đã qua, 18/18 bot dịch chuyển; stress 5 phòng/50 slot, 45 bot đồng thời đủ 300 giây PASS, không bot sống ≥15 giây đứng nguyên chỗ. Cổng Local Full bằng client mới cũng PASS: WSS 300 giây, Auto hai client, UDP/account/phòng, kết quả/history/replay, fingerprint source/build và số đo server. Thị trấn 1+9 bot qua Auto→WSS PASS bằng cùng artifact.

Bản bàn giao: `Builds/Bot-Recovery/Lastaway.exe` cùng toàn bộ thư mục; Unity cá nhân 6000.3.21f1, shared Git vẫn 6000.3.15f1. SourceHash `b7f71312d348109f20f40e2ba069bbd8544cb3086caad48349d67b5fbce98a4c`, playerFingerprint `f8b837bd805f284fb4bf8472b2286fb145a93df586a036a87848910b6c333ea6`. Chưa publish GHCR hoặc truy cập/deploy AWS trong đợt sửa này. Các FAIL cũ bên dưới là lịch sử, không bị đổi thành PASS.

## 2. Các sửa đã thực hiện
- Bỏ nhãn dài trên nhân vật: ẩn nhãn của mình, tên người khác ngắn, tôn trọng visibility.
- Spectator dùng kích thước map runtime, tự chọn người còn sống sau khi chết/killcam, không suy ra kết quả trận từ việc thiếu target; giữ vị trí camera lúc chờ dữ liệu.
- Bot tiếp tục A* qua nhiều tick, theo đường partial hợp lệ, không tính chờ ngân sách là kẹt. Ưu tiên vào bo trong lúc đang co và tính thời gian đi; stun không làm sai ETA. Bỏ rương có chỗ đứng bị chặn.
- Dùng index không gian của map cho collision tìm đường; vẫn kiểm cửa/vật đã phá theo từng phòng, trứng động và cùng bán kính nhân vật.
- Hàng đợi world action tối đa 8/phiên, xử lý 1/tick sau movement; trả lỗi Busy khi đầy thay vì bỏ thao tác cùng tick. Client giữ số thứ tự qua resume, hạn chế thao tác đang chờ và hiện dấu chờ nhặt.
- Cache world pages theo phòng/trận/revision; bỏ cấp phát snapshot/string chỉ để kiểm effects. WebSocket xen snapshot mới giữa burst reliable. Buffer interpolation thích ứng jitter; prediction hiện có được giữ.
- Sửa panel Local Docker dev bị footer che; người chơi Release vẫn chỉ thấy 3 môi trường.

## 3. Kiểm module
- Unity 6000.3.21f1: 1.254 EditMode và 215 PlayMode PASS, không skipped. Các test killcam cập nhật kỳ vọng sau tick thành tự spectator, giữ kiểm thời lượng 5 giây và UI 500ms.
- Inventory, World, MapCatalog, Network, DualTransport, GameplayNetwork, Match, MatchResult, LanHost, Combat, Throwable, MapScale, FloorLoot, Telemetry PASS; log riêng trong thư mục này.
- WorldNetwork Mixed: 82 kiểm tra PASS trên UDP/WebSocket loopback thực, gồm 20 request dồn nhận đủ phản hồi, loot tranh và chống trùng.
- Airdrop: 215 PASS. Lượt seed ngẫu nhiên ban đầu có lượt thả thứ ba bị hủy hợp lệ do không có điểm đáp. Fixture socket cố định seed 42 để lặp lại; không sửa lịch/luật thả của production.

## 4. Kiểm bot và hiệu năng
### Sửa bổ sung sau phản hồi bot đứng im
- Đã tái hiện lỗi bằng MatchId `fb9db6ce-2f53-4e27-8fef-452abc13f29b` trên Tháp cổ; bot NetId 3 ở Loot, displacement 0 dù còn sống gần 90 giây. Lượt seed cố định sau đó vẫn FAIL, vì vậy lượt random 18/18 di chuyển không được dùng để kết luận đã hết lỗi.
- Vòng phục hồi có nhiều timer chồng nhau: timer ngắn hủy A* còn pending; timer actor không được rearm khi FailPath khiến đường mới bị hủy tiếp từng frame. Đi chậm bị đo bằng độ lệch mỗi tick thay vì tích lũy. Đi hết partial route bị coi là mục tiêu không thể tới.
- Bản cuối giữ frontier khi chờ, lấy partial route có kiểm collision, cho tìm tiếp phần còn lại; timer actor chỉ hoạt động sau khi thực sự có công việc tìm đường/di chuyển. FailPath rearm timer một lần để đường mới có cơ hội chạy. Heal/stun không bị tính là kẹt; đổi mục tiêu không reset timer actor đang theo dõi.
- Thêm kiểm chờ ngân sách 8 giây rồi tiếp tục, rearm recovery, di chuyển chậm và partial path. Fixture stress chỉ lấy origin sau InGame và kiểm từng bot sống ít nhất 15 giây phải dịch chuyển ít nhất 0,5 cell; không tính bước dịch chuyển spawn. Đây là kiểm phát hiện đứng im cả đời, không chứng minh bot luôn có chiến thuật tốt hoặc không bao giờ đứng lại để bắn/phá vật/hồi máu.
- `dotnet build Backend/Tests/BotChecks/BotChecks.csproj -c Release --no-restore`: PASS, 0 warnings/0 errors. Bản sửa cuối: runtime/gameplay NOT_TESTED; không dùng số PASS cũ thay thế.
- Graphify có cảnh báo parser tại BotChecks (top-level program) và một số file UniTask; phần trích xuất có thể thiếu symbol. Đây là giới hạn bản đồ mã, không thay kết quả compiler hoặc bằng chứng runtime.
- DeepSeek được gọi 2 lượt: lượt đầu NO_CHANGES; lượt thu hẹp có patch C# không hợp lệ, đã CANCELED và giữ đề xuất, không apply. Codex hoàn thiện theo quyền tiếp quản trước đó. Không gọi thêm model.

Có hai map thật Tháp cổ/Thị trấn; golden collision đối chiếu 1.000 điểm/map với truy vấn gốc, gồm trạng thái cửa/vật phá khác nhau. BotChecks còn kiểm perception, reload, loot, phá vật, hazard, bo đang co không có warning flag, thiếu budget, chết, NoHumans và chơi lại.

5 phòng/50 slot (45 bot), 300 giây: không dropped tick; 86/90 lượt sống bot từng dịch chuyển hơn 0,5 cell. Đây là các lượt sống qua reset, không phải 90 bot đồng thời. Bốn lượt chưa đạt khoảng dịch chuyển đó trước khi bị loại/reset. Lượt này chạy trước guard cuối tránh mục tiêu rương bị chặn.

Bản code cuối chạy lại 1 phòng/9 bot đủ 300 giây: BotChecks 1.660 checks PASS (có assertion lặp theo slice; không phải 1.660 kịch bản khác nhau). Xem bot-summary.json/CSV cho RAM, tick spike và số đo. Không dùng kết quả runtime để tuyên bố chất lượng cảm giác chơi hoặc Internet.

## 5. Windows build và chơi Docker thật
Unity 6000.3.21f1, build cuối `build-0e4c2cb632` tạo đầy đủ artifact mới. Lượt đầu serialize lại asset nên build incremental để ổn định fingerprint; `source-build-verification.json` xác nhận current == build, không sửa manifest thủ công. Gọi BuildPipeline qua execute_code trước đó không hoàn tất, đã khôi phục Editor và chuyển sang công cụ build chuyên dụng. Xem manifest `Builds/Gameplay-Polish/registry-build.json`.

- Tháp cổ, 1 người +9 bot, main profile, UDP: PASS 300 giây kết nối; cả 9 bot dịch chuyển >0,5 cell theo snapshot client. Input 29,94Hz, snapshot 30,01Hz, 300/300 pong; RTT p50 33,32ms, p95 33,84ms, p99 66,60ms. Đăng nhập, map, bo/HP, kết quả, history, replay và thoát code 0 đạt. Một catchup drop phía client được ghi nhận; không đánh đồng với dropped tick server.
- Thị trấn, 2 Windows client, profile test: PASS. Một UDP và một Auto fallback sang WebSocket với lý do ConnectTimeout (cố ý cấu hình cổng UDP không có listener). Input 29,71/29,72Hz, snapshot 30,02/30,04Hz; không timeout pong. Cả hai có kết quả/history/replay và thoát code 0.
- Thị trấn, 1+9 bot, profile tăng tốc riêng, Auto→WebSocket: PASS 29,29 giây kết nối, input 29,76Hz, snapshot 29,98Hz, không timeout pong. Có 4 bot dịch chuyển >0,5 cell trước khi bị loại/reset trong lịch test nhanh. Không dùng lượt này để tuyên bố 9 bot đi liên tục trong 5 phút trên map này.

Ảnh lobby/in-game/kết quả và JSON/CSV nằm trong ba thư mục unity-bots10, unity-mixed-two, unity-bots-town. maxBotsMoved/maxBotTravelCells đo từ snapshot authoritative; độ lệch vị trí có thể gồm portal, không phải tổng quãng đường đi bộ. Camera trong ảnh dùng Follow Player; maxOnScreenPlayers khác số actor đã spawn.

### Cổng Full trên artifact cuối
Fingerprint source `6e4cbe4e1a7750a178787161747537c74808981bee3428e9137f316a2c93ba26` đã khớp. Lượt `unity-final-full` PASS qua Auto→WebSocket, đủ 300.03 giây, code 0, có kết quả/history/replay. Client xác nhận 9 bot có di chuyển; input 29.88Hz, snapshot 30.00Hz, timeout pong 0. Đây là lượt Full gắn với manifest/fingerprint cuối.

Metrics container được giữ trước khi recreate: 101 mẫu, max RAM 130.18MiB/256MiB, max CPU một core 39.56%, dropped tick 0. Xem docker-final-full-metrics.log; không trộn với log container cũ.

## 6. Trạng thái nghiệm thu và giới hạn
PASS: module/regression ở phần 3, runtime bot phần 4, Windows artifact và các flow phần 5.
NOT_TESTED: hai PC vật lý, buổi LAN thật, chất lượng 60 FPS/720p, AWS/public Internet và cảm giác chơi WAN/VPN trong đợt này. Chưa chứng minh giảm RTT tới Sydney.
Một lượt HTTP test 503 ban đầu diễn ra ngay sau recreate GameServer, trước khi tạo phòng/client; chạy lại sau readiness đạt. Không có retry tự động vô hạn trong game.
Số đo Docker được quan sát trong test; log Docker lưu sau recreate không giữ đầy đủ lịch sử container main trước đó. Dùng các log benchmark runtime 300 giây để đánh giá đầy đủ tick/RAM; không che lấp giới hạn bằng chứng này.

## 7. Chuẩn bị deploy và dọn sau test
### Người dùng tự kiểm bản sửa bot cuối
Thoát trận trước khi rebuild: GameServer restart làm mất phòng/session RAM. Tại PowerShell:
```powershell
Set-Location 'D:\Unity\Lastaway\Backend'
docker compose --profile account up -d --build game-server api mongo
docker compose logs --tail 20 game-server
```
Chờ server ready, mở Editor hoặc client hiện có → Local Docker dành cho dev → đăng nhập → tạo phòng → Tháp cổ → thêm 9 bot → Bắt đầu. Quan sát từng bot sau khi spawn; bot có thể dừng để phản ứng, bắn, phá vật hoặc hồi máu, nhưng không được đứng tại spawn từ đầu đến cuối. Quan sát khi bo co, chết/spectator rồi chơi lại; đổi sang Thị trấn và lặp lại. AI chạy ở server: client mới nhưng image Docker cũ vẫn dùng AI cũ; không cần build Windows chỉ cho thay đổi server này.

Chạy ca hồi quy tự động (từ thư mục gốc, không gọi AWS):
```powershell
Set-Location 'D:\Unity\Lastaway'
dotnet run --project Backend/Tests/BotChecks/BotChecks.csproj -c Release -- --stress --authored --stress-rooms 2 --stress-seconds 90 --diagnose
```
Kỳ vọng không FAIL, `Stationary lives >=15s: 0`; dữ liệu từng bot tại `.utmp/bot-immobile-diagnostics.json`. Sau khi đạt mới chạy lại lượt 300 giây/kiểm Docker để nghiệm thu deploy. Chưa rebuild/restart Docker hoặc thay trạng thái AWS trong bước bàn giao này; giữ camera bình thường Follow Player và spectator hiện có.

### Dung lượng build đã đo
Build hiện có 306,86 MiB: asset/data 203,60 MiB; Managed DLL 52,43 MiB; native/runtime 50,47 MiB; debug 0,37 MiB. Build log có texture lớn như map_grid 10,8 MB, vignette 7,9 MB, logo/background cũ khoảng 6 MB mỗi ảnh, hiệu ứng nhập và hai splash. BuildSettings còn bật scene tham khảo SampleScene/GameConfig/GameMain; player còn Roslyn DLL và MCP runtime. Chưa đổi scene list/import settings/package hoặc xóa DLL thủ công. Tối ưu dung lượng cần một lượt riêng để kiểm đủ tham chiếu và chất lượng hình, không ảnh hưởng phạm vi sửa bot.

### Lịch sử khôi phục môi trường ở lượt kiểm trước
Giữ account-ticket v4, 10 nhân vật/phòng, 5 phòng, 50 slot. Local Docker đã rebuild GameServer mới; chỉ restart image cũ không có bản sửa bot. Đã trả MATCH_PROFILE=main, memory limit=0 (dev), dừng api/game-server/mongo và giữ Mongo/map/outbox. Client test do tác vụ mở đã thoát code 0; phòng RAM được dọn khi dừng server. Không thay trạng thái AWS.

Chưa chuyển sang publish/deploy: cần hoàn tất bản sửa bot và kiểm lại từng bot từ lúc InGame, không tính dịch chuyển lúc spawn. Unity21 sinh thêm ba giá trị mặc định thermal iOS trong ProjectSettings.asset; cần review khi commit, không có nâng phiên bản Editor dùng chung của team. Shared ProjectVersion pin vẫn được giữ.

### Lịch sử checkpoint trước commit / chuẩn bị deploy (đã xử lý sau)
Người dùng xác nhận chơi Local thấy ổn và yêu cầu commit, chuẩn bị deploy. Chạy lại ca seed cố định hai map, 18 bot, 90 giây: FAIL, một bot Tháp cổ NetId 3 sống 89,87 giây nhưng displacement 0, state Loot, search còn pending. Xem bot-precommit-final.log/json. Các kiểm rearm recovery, slow movement và chờ ngân sách ở đầu suite đã qua trước lỗi stress; không ghi toàn suite PASS.

Kết luận phát hành tại checkpoint đó: BLOCKED. Commit dùng để giữ checkpoint source hiện tại, không phải release đã nghiệm thu. Chưa chạy GHCR PrepareOnly publish, chưa gọi AWS/đổi service. Cần sửa và kiểm lại ca seed này, sau đó VerifyLocal Full bằng client có registry-build.json khớp source, rồi PrepareOnly và giao dịch deploy 20 phút với rollback theo Docs/AWS_GHCR_Deploy.vi.md. Không dùng báo cáo cũ để bỏ qua gate.

### Nghiệm thu sau sửa — kết quả mới nhất
- Nguyên nhân ca bot còn lại: chỉ giữ một mục tiêu vừa thất bại, bot đổi qua lại hai món loot không tới được. Khi hỏng đường, tạm ngừng săn loot 5 giây theo BlacklistSeconds, chọn bước di chuyển gần có segment hợp lệ; bo/hazard/combat vẫn ưu tiên. Không sửa vị trí/HP/tốc độ hoặc bỏ validation. Reset cả cooldown khi chơi lại.
- Seed cũ: BotChecks 1.693 checks PASS, 18/18 bot có dịch chuyển. Bot NetId3 từng đứng gần90s nay dịch chuyển15,11 cell trước khi bị loại ở42,3s. Khoảng dịch chuyển là độ lệch lớn nhất từ origin, không phải tổng đường đi.
- Stress realtime300s: 5 phòng, 50slot, 45bot đồng thời, 90 lượt sống qua reset; 0 lượt sống≥15s có displacement<0,5. BotChecks1.730 checks PASS (gồm assertion lặp theo slice). Tick trung bình30,00Hz, dropped0, max tick49,55ms, CPU một core max36,52%, RSSmax96,57MiB, pathnodesmax1.455/1.500. Đây là harness in-process, không phải50người thật/Docker/WAN.
- Full Docker256MiB + Windows: PASS; 113mẫu tick, 114mẫu RAM, tick29,92Hz, CPU một core max40,58%, RAMmax32,14%, dropped0/restart0/OOMfalse. WSS300,03s: input30,00Hz khi còn điều khiển, snapshot30,00Hz trong InGame, pong300/300, RTTp50/p95/p99=33,33/33,46/33,75ms. Không suy cả10nhân vật sống đủ300s. Auto hai client fallback WSS:220,39s/215,12s, exit0, kết quả/history/replay đạt; smoke UDP riêng PASS.
- Thị trấn1+9, profile tăng tốc riêng: PASS30,85s, Auto→WSS, exit0. Không coi đây là benchmark300s map Thị trấn hoặc50slot qua mạng.
- Các regression backend và29test pipeline PASS. Runner thêm deadline trước nhánh return sớm và đếm view mạng tồn tại riêng với visibility; không bỏ yêu cầu đủ roster/view. -ForceD3D11 là lựa chọn test rõ ràng, không đổi renderer mặc định. Build mới0lỗi/42warning; không gọi warning là lỗi C#.
- Lượt default/oldcatalog thiếu bằng chứng cuối FAIL; lượt D3D11/oldcatalog ghi input0 và thiếu movement FAIL. Local volume khi đó vẫn map1.1.0. Đã backup `Backend/data/maps-before-bot-recovery-20261112`, validate/publish/reload map1.2.0 từ image; lượt với dữ liệu hiện tại PASS. Không khẳng định chi tiết nguyên nhân world cũ đã được cách ly bằng một phép thử chỉ đổi một biến. Image mới không tự cập nhật catalog volume; đối chiếu catalog khi deploy.
- Bằng chứng tại `bot-recovery-full/`, `bot-recovery-town/`, bot-recovery-seeded-pass.*, bot-recovery-50slots-300s.* và bot-recovery-stress-summary.json. Receipt pipeline để PrepareOnly: `Backend/out/bot-recovery-current-map-20261112/local-evidence.json` (bản sao trong báo cáo để tham khảo).
- Đã trả Local về MATCH_PROFILE=main, memory_limit=0 và giữ stack đang chạy như lúc bắt đầu; Mongo/map/outbox giữ nguyên. Client tác vụ đều đóng; camera Follow Player, spectator giữ. Build chẩn đoán tạm không phải artifact bàn giao.
- Chưa kiểm: AWS/WAN của bản này, hai PC vật lý,60FPS/720p và cảm giác chơi/cân bằng người thật. Sẵn sàng bước commit/PrepareOnly theo guard; chưa phải nghiệm thu AWS. Không thay lifecycle hoặc tiêu tài nguyên AWS.
