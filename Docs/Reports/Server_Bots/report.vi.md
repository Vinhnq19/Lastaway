# Bot server-authoritative — triển khai và nghiệm thu

Cập nhật **05/11/2026**. **PASS trong phạm vi đã đo, có giới hạn.** AWS đang chạy **`bots-20261105-r2`**; EC2 được giữ **Running** theo yêu cầu. Các lượt không đạt được giữ trong bằng chứng, không đổi thành PASS.

## 1. Kết quả, phạm vi và phiên bản

Đã ghép luồng **đăng nhập → tạo/vào phòng → chọn map → trưởng phòng thêm bot → Start → trận FFA → kết quả/lịch sử → chơi lại**. Một người +5 bot đủ min6; phòng tối đa10 participant, 5 phòng, tổng50 slot tính cả bot và human pending/grace. Bot không tự bị thay khi có người mới vào. Không triển khai bot offline, đội hoặc nhiều độ khó.

Code gồm nền bot đã có ở `abc83657668ce87e557cc5504b5ddea19e89efbd` và các sửa/test hiện tại trên nhánh `codex/server-bots-20261103`; thay đổi mới **chưa commit**, không merge main/push. Giữ Unity **6000.3.15f1**, backend target/runtime **.NET8**; máy kiểm thử dùng SDK10.0.401. Handshake account-ticket **v4**, gameplay protocol **v3**; client gameplay v2 cũ không tương thích.

Windows build được kiểm là `Builds/Windows-Bots-6000.3.15f1/Lastaway.exe`. Fingerprint cả build nằm trong JSON; SHA của riêng launcher exe không đủ chứng minh cùng nội dung game. Release đầy đủ ở `Backend/out/aws-bots-20261105-r2/release.tar.gz`, SHA256:

`97a230a9495caa5bd715d7ae771d9025acc09580ca9ebf15c90e15e22c32be1c`

## 2. Những phần đã hoàn thiện

- **Phòng/quyền:** lệnh reliable thêm/xóa/điền đủ min, kiểm trưởng phòng, Waiting, RoomId/revision/request ID và hai trần sức chứa. Bot là participant nội bộ, không có peer/socket/tài khoản/JWT giả; chỉ người thật làm trưởng phòng.
- **AI:** FSM Idle/Roam/Loot/BreakContainer/Chase/Combat/Search/Retreat/Heal/EscapeZone/Dead. Decision/perception5Hz lệch lịch; intent movement/combat đi qua luật server30Hz. LOS/Invisible/bụi, phản ứng350ms, nhớ3s, aim có sai số; loot/vũ khí/đạn/thuốc dùng inventory/combat hiện có. Bỏ item bị catalog tắt; không biết trước loot hoặc inventory đối thủ.
- **Navigation:** A*0,5cell với collision/radius/segment, không cắt góc; tối đa500node/yêu cầu, ngân sách1500node/tick toàn server, luân phiên phòng/bot. Đường mất hiệu lực được bỏ; đổi vị trí lớn làm tính lại, không teleport/tăng tốc để thoát kẹt.
- **Các sửa hành vi thực:** hoàn thiện stagger think, cached attack intent30Hz, nhặt/nâng cấp khi đầy4 slot, reload, tiếp cận rương đúng tầm; sửa bot mắc trong lava/poison bằng đích thoát gần đã kiểm geometry. Bot đang ngoài hazard vẫn tránh đi vào; sau khi thoát mới tiếp tục đường vào bo. Test trước sửa đã tái hiện bot đứng yên trong lava, sau sửa di chuyển ra bằng input thường.
- **Vòng đời:** bot tự loading bằng map server; đổi map/chơi lại giữ số bot nhưng reset AI/HP/đồ/input. Hết human ở Waiting dọn bot. Trong trận giữ human grace; không còn kết nối hoặc grace hợp lệ thì NoHumans, không trao win. Human đã chết nhưng còn xem vẫn giữ trận. Bot chết dừng input và dùng death-drop chung.
- **Protocol/UI/kết quả:** roster/result có IsBot/BotId; nút bot và số human/bot/tổng theo trạng thái server. WithBots được ghi vào result/outbox/API/history, bot không có ProfileKey. Không coi là PvP thuần người; chưa có hệ cộng điểm PvP tổng mới. Hash HumanOnly cũ vẫn tương thích.
- **Công cụ:** smoke/Windows runner thêm bot/chọn map, chụp ảnh bằng game, đo transport/rate/RTT; server thêm bot/path queue metrics. Launcher giữ máy thức trong lượt test và trả yêu cầu nguồn điện cũ ở finally. DeepSeek viết các gói text, Codex review trước áp dụng và sửa lỗi tích hợp/fixture theo quyền đã duyệt; không gửi `.env` hoặc credentials. Usage thô được giữ, không suy đoán tiết kiệm token.

## 3. Phương pháp và môi trường

Local: Windows11, 16 logical CPU, Windows player thực; Docker GameServer giới hạn256MiB. AWS: EC2 hiện có t3.small ở Sydney, GameServer256MiB, API384MiB, Mongo768MiB; không nâng máy hoặc bật Unlimited. API/Mongo/control/WebSocket listener giữ mạng nội bộ; public HTTPS443 và UDP7777 như cấu hình đã có.

Ba lớp bằng chứng tách riêng: runtime C# và fixture; Unity EditMode/PlayMode; Windows player thật qua Local/AWS. Stress50 là **5 human fixture +45 bot**, không phải50 người thật hoặc máy phát tải WAN. Các bài300s tính thời gian kết nối, có lobby/kết quả/replay; inputHz chỉ tính lúc avatar phát input. Không gọi đó là300s người chơi sống liên tục.

Multi-client launcher dùng cùng account QA trong một lượt và các phiên/tên khác nhau. Nó kiểm nhiều participant/kết nối; chưa thay kiểm hai tài khoản riêng trên hai PC vật lý. Không dùng generator trong EC2 để thay bằng chứng PC→AWS.

## 4. Kết quả chức năng và hồi quy

| Hạng mục | Kết quả | Bằng chứng/phạm vi |
|---|---|---|
| BotChecks cuối | PASS | 159 assertion, gồm assertion thiết lập fixture; quyền/quota/idempotency/slot, loading, grace/NoHumans/replay, movement, reaction/combat, loot/upgrade/reload/heal, LOS/Invisible/bụi/memory, phá rương một lần, thoát bo/lava/poison, A* không cắt góc |
| MatchResultChecks | PASS | 85 assertion; WithBots/IsBot canonical hash, binary HumanOnly cũ, PvP eligibility, outbox retry/quarantine/restart |
| Hồi quy backend | PASS | 11 suite: MatchChecks, MatchResultChecks, InventoryChecks, CombatChecks, WorldChecks, MapCatalogChecks, TelemetryChecks, NetworkChecks, DualTransportChecks, GameplayNetworkChecks, WorldNetworkChecks |
| Unity | PASS | 910/910 EditMode;133/133 PlayMode; build Windows0error/41warning; Console cuối0error |
| Local Di tích cổ | PASS flow | 1+5 UDP;2+4 có một client UDP và một WS; hoàn tất kết quả/history/replay |
| Local Bãi đá | PASS flow | 1+5 và2+4 qua Auto→WS; thêm1+9 UDP/WS300s. Ma trận đầy đủ này trước hotfix hazard; sau hotfix kiểm lại1+5 WS và runtime stress |
| Local hotfix r2 | PASS flow | Windows1+5 WS,66,62s kết nối, exit0, đủ các assertion trận/history/replay |
| AWS r2 WSS | PASS flow | PC thật1+9,300,01s; input29,994Hz, snapshot29,976Hz;300ping/300pong, timeout0; kết quả/history/replay, exit0 |
| AWS r2 Auto | PASS flow | Hai player +4bot, cố ý UDP7779 không listener; cả hai fallback WSS,120,62/120,88s, kết quả/history/replay, exit0 |
| AWS r2 UDP7777 | PASS smoke | PC thật join/pong bằng UDP,5,02s, exit0. **Không phải** bài gameplay UDP WAN5 phút |
| Account guard AWS | PASS | WSS thật từ PC: handshake v3 không ticket →AuthRequired; v4 với vé không đăng ký →InvalidTicket. API thiếu/sai JWT hoặc Guest không được tạo phòng/lấy vé; JWT hợp lệ tạo được |
| Mongo/API thật | PASS | Local201→200 idempotent; cùng MatchId sửa dữ liệu→409; bot có ProfileKey hoặc HumanOnly chứa bot→400. Leaderboard có BOT/WithBots, không lộ ProfileKey |
| Dữ liệu AWS qua release/rollback | PASS | Hai bản ghi r2 gồm LastAlive và NoHumans, mỗi bản9bot/1account, bot không profile; hai bản ghi r1 vẫn còn. NoHumans phát sinh khi đóng client trong lượt chơi lại, không phải cộng trùng trận |
| Hai PC vật lý / đánh giá độ vui | NOT_TESTED | Cần checklist hai máy và người chơi thật; không thay bằng nhiều cửa sổ |

Ảnh: [phòng chờ](screenshots/lobby.png), [trận AWS](screenshots/aws-game.png), [kết quả AWS](screenshots/aws-result.png). Nút/nhãn được kiểm layout và đường điều khiển tự động; chưa có buổi đánh giá thao tác/cảm giác chơi bởi hai người thật.

## 5. Hiệu năng và giới hạn kết luận

| Lượt | Tick Hz theo cửa sổ | Dropped tick | CPU tối đa một core | RSS tối đa | Ngân sách AI tối đa |
|---|---:|---:|---:|---:|---:|
| Local1+9 UDP300s, trước hotfix | 29,97–30,20 | 0 | 6,6% | 111,21MiB | 483node/tick |
| Local1+9 WS300s cách ly, trước hotfix | 29,97–30,20 | 0 | 8,4% | 122,96MiB | 663node/tick |
| Local r2 stress50/60s, lượt cách ly cuối | 29,79–30,18 | 0 | 25,6% | 72,83MiB | 925node/tick |
| AWS r2 WSS1+9 | 29,96–30,19 | 0 | 20,0% | 120,43MiB | 500node/tick |

AWS cgroup peak khoảng65,76MiB/256MiB, không OOM/restart. RSS và cgroup là hai phép đo khác nhau, giữ cả hai trong JSON. Queue đường đi không tích lũy trong các cửa sổ đã ghi.

RTT WSS AWS1+9: **p50 266,68 / p95 300,03 / p99 300,41ms**. Đây là roundtrip ứng dụng thực gồm xử lý transport/player, không phải ping ICMP thuần. Không áp ngưỡng Local50ms cho WAN Sydney. UDP chỉ smoke ngắn, p50 khoảng300ms; chưa đủ mẫu để kết luận UDP luôn nhanh hơn WSS trên mạng này.

**Có tick spike:** max74,44ms trong stress Local cuối và130,97ms trong lượt AWS r2 dù không dropped tick. Vì vậy kết luận là chạy được trong bài đo, không phải mọi frame luôn33ms hoặc đảm bảo mượt trong mọi mạng. Giữ10/5/50 như chính sách; mới xác nhận50 participant runtime Local và10 participant AWS, chưa nghiệm thu50 participant WAN hay tìm giới hạn cuối của server.

## 6. Các lượt không đạt, sửa chữa và vận hành AWS

- Windows có lượt dùng UI Automation hoàn tất assertion nhưng crash native khi thoát; giữ FAIL theo exitcode. Các lượt bàn giao không dùng UI Automation trên player, giữ nguyên15f1 và accessibility. Không tuyên bố lỗi engine đã được sửa triệt để.
- Lượt WS cũ mất kết nối có khoảng trống UTC lớn trùng khả năng PC sleep/đổi mạng; chưa chứng minh nguyên nhân duy nhất. Launcher được bổ sung yêu cầu giữ thức theo thread, không đổi power plan lâu dài.
- Một lượt WS Local trùng đóng gói có6 dropped tick; client PASS không được dùng để ký hiệu năng. Đo lại riêng đạt0. Một stress r2 có1 dropped tick; giữ kết quả đó và lượt cách ly sau đạt0, không che dao động máy dùng chung.
- Chạy hai fixture quyền API song song từng chạm429 ở quota anonymous5/phút; giữ FAIL của fixture rồi chạy lại sau cửa sổ quota. Không nới rate limit hoặc đổi yêu cầu xác thực để đạt test.
- AWS r1 có3 dropped tick trong một cửa sổ dù client flow PASS; đã rollback về maps-hvu-20261103. R2 sửa lỗi hazard và được kiểm trong **lượt20 phút bổ sung do người dùng duyệt**. Không khẳng định lỗi hazard là nguyên nhân của3 dropped tick cũ.
- Release full có checksum; delta bị từ chối đúng do image ID nền không khớp, không bỏ kiểm chứng. Checksum tệp cấu hình và mount Mongo trước/sau giống nhau. Rollback có restart dịch vụ/container; **volume/dữ liệu vẫn giữ**, không Stop/Start EC2.
- Lượt r2 kết thúc trước hard deadline20phút: đóng toàn bộ client/probe, xóa phòng test, dừng collector. Snapshot cuối chỉ phòng mặc định,0peer/0bot. EC2 xác nhận Running, t3.small, release bots-20261105-r2; bản maps-hvu giữ làm rollback. Không tự Stop vì người dùng yêu cầu giữ Running; credits tiếp tục phát sinh.

## 7. Bàn giao và cách thử lại

1. **Local:** bật Docker Desktop; tại `Backend` chạy `docker compose --profile account up -d --build`. Mở bản exe ở mục1, chọn Local, đăng ký/đăng nhập Local, tạo phòng/chọn map.
2. **AWS:** mở cùng bản exe, chọn Online/AWS với `junglemaze.duckdns.org`, đăng nhập tài khoản backend AWS. Không dùng build cũ gameplay v2. Người chơi không cần AWS CLI/VPN.
3. Trưởng phòng bấm **Điền đủ6**, đợi map ready rồi **Bắt đầu**. Muốn1+9 thì thêm tới tổng10. Xem bot loot/đánh/HP/bo/chết, kết quả CÓ BOT, lịch sử và Chơi lại. Muốn đón người mới khi đầy phải xóa bot trước ở Waiting.
4. Hai PC dùng tài khoản riêng và [checklist](../../../Backend/Tests/OnlineReadiness/PHYSICAL_TWO_PC_CHECKLIST.md); vẫn cần nghiệm thu thêm đường mạng/so vị trí/cảm giác chơi thực. Các tổ hợp động như cửa đóng ngay khi bot đuổi/portal liên tiếp chưa được playtest tay exhaustive.

Hướng dẫn được bổ sung trong `Docs/Phase2_Match_Guest_Zone.vi.md`, kiến trúc trong `Docs/Network_Architecture.vi.md`. Graphify cập nhật19.379node/73.467edge; HTML tổng hợp486community. Có4 cảnh báo parser UniTask,19 file không trích được symbol và2.830 dangling reference ở input; đây là giới hạn bản đồ, không phải lỗi compile C#. Không gọi semantic API trả phí; token lịch sử trong fragment không được nhận là chi phí mới.

Dữ liệu: [JSON chung](report.json), [CSV từng client](summary.csv), [bằng chứng zip](evidence.zip), [checksum](evidence.zip.sha256). Zip giữ cả lượt fail, log/ảnh/metadata test và usage DeepSeek; không có `.env`, mật khẩu/JWT/RoomCode thực. Giữ Windows build mới và các release/rollback; không xóa Mongo volume hoặc lịch sử QA.

Đã dọn **39,06MiB /432file tạm** của đúng tác vụ sau khi kiểm zip/checksum; giữ315file bằng chứng trong zip2,78MiB, build/release/rollback, source và volume. Không dọn các thư mục tạm của tác vụ khác.

Cập nhật sau nghiệm thu: đã khôi phục camera theo player và đổi countdown main thành5s, đồng bộ settings/hash server–client. Theo yêu cầu không chạy test/build/deploy cho hai thay đổi này; số đo và build AWS r2 phía trên vẫn thuộc bản trước thay đổi camera/countdown. Cần build client và cập nhật backend cùng bộ dữ liệu để dùng cấu hình mới.
