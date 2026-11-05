# GHCR AWS — kết nối/reporter/rollback PASS, release bị từ chối vì hiệu năng

## 1. Kết luận

**Chưa nghiệm thu đầy đủ rollout AWS.** Pipeline private GHCR đã publish/pull, kiểm image thực tế và chạy game từ PC; reporter mới hoạt động, WSS300giây/Auto hai client/UDP/JWT đều PASS. Gate hiệu năng ghi7dropped tick nên từ chối candidate và rollback đúng. EC2 cuối lượt vẫn **Running**, current là`bots-20261105-r2`; thay đổi candidate chưa được giữ trên AWS.

Lượt cuối bắt đầu19:15:03 và kết thúc can thiệp19:32:44 ngày05/11/2026 (UTC+7), **1060.95s <1200s** đã duyệt. Không tự Start/Stop/nâng máy hoặc bật Unlimited. Đã bổ sung stage profiling, sửa ngân sách tìm đường và ReadyToRun; Full Local bản sửa PASS. Lượt AWS mới đang chuẩn bị, chưa kết luận bản sửa đạt AWS.

## 2. Những gì đã triển khai

Entrypoint PrepareOnly/Deploy/Rollback; working tree sạch/commit cụ thể; fingerprint riêng API/GameServer/base LinuxAMD64; reuse image không đổi, manifest đủ hai digest và checksum. Bundle chỉ script/config/manifest, không image archive/DB/secret. Pull trước switch, chỉ recreate service thay đổi; Mongo không restart. Flock/systemd giữ transaction qua kiểm từ PC, thiếu acceptance thì restore image/config trước.

Endpoint deployment-status nội bộ qua queue, có secret; từ chối GameServer còn human/pending/grace/bot/entity/trận. Kiểm đúng VM, DNS/TLS, đĩa, kiến trúc, image và health. Guard so sánh **Compose đã resolve** với manifest, chặn image override từ private config. Hỗ trợ rollback legacy/base-images.env, không migration Mongo. Source và template secret/volume/outbox/map giữ nguyên.

Đã sửa bundle tự sinh`__pycache__`. Reporter tính SHA256 bằng .NET, giữ module-bound command từ trước launch, phát hiện build đổi giữa lượt và ghi tên command thiếu an toàn. Root chụp`failureMetrics` trước recreate để giữ log candidate. Lỗi reporter của lượt trước **không tái hiện** ở lượt AWS cuối; không khẳng định đã truy ra tên command cũ vì reporter cũ không lưu. Không thêm framework/dependency hoặc đổi Unity6000.3.15f1.

## 3. GHCR, dung lượng và thời gian

Hai package Private, liên kết Vinhnq19/Lastaway, đã xác minh qua browser/API. PC dùng Docker Desktop credential store; EC2 token riêng chỉ read:packages, file600/thư mục700. Người dùng tự xác thực/nhập token; không token trong repo/chat/log.

Các publish dùng layer đã ấm cache, không gọi là cold-push. Lượt đầu API7,624s/GameServer7,198s; API nhỏ22,226s và GSreuse5,701s. Các bản sau reuse đủ phần không đổi; JSON giữ từng bước/fingerprint/digest. Chuẩn bị release guard cuối113,41s, không truy cập EC2. Các bước hồi quy vẫn nằm ngoài thời gian pull.

AWS cuối truyền bundle**23,057byte**, EC2 pull image mất**4,140s**. Không upload`images.tar.gz` trong luồng thường. Bundle bytes là payload archive, không phải tổng traffic SCP; compressed OCI layer delta là so với publisher ledger, không phải wire bytes hoặc cache EC2. Wire bytes **NOT_MEASURED**. API-only/no-change chưa đạt tới trên AWS nên không có số thời gian thành công cho hai trường hợp này.

## 4. Local, build và kiểm thử lỗi

Bộ kiểm trước sửa spike: Backend0error/0warning; Network148/Bot187/World168 assertions PASS, DualTransport/MapCatalog/MatchResult/Combat/Inventory/MatchChecks PASS. Pipeline**22 tests**, launcher**37 kiểm bằng fixture** PASS, gồm checksum/tamper, digest/architecture, credentials/private visibility, env/image overrides, dirty tree, lock, occupancy, selective plan, rollback failure, metrics trước recreate và hash không phụ thuộc cmdlet autoload/public function binding. Fixture không được diễn giải thành gameplay thực.

Windows6000.3.15f1 đã build192,63s,0error/6warning; receipt/source/binary được kiểm. Client code không đổi trong các sửa reporter/guard cuối nên dùng đúng artifact đã xác minh. Camera Follow Player, spectator giữ đúng. Hai fixture map cũ sau merge đã pin Di tích cổ để giữ golden/collision assertions.

Full Local cuối PASS: WSS300,03s exit0, Auto hai client khoảng96,9s exit0, result/history/replay và UDP/WSS smoke. Server256MiB,89cửa sổ/89mẫu RAM: tick29,944Hz, CPUmax21,60%, DockerMemPerc22,20%,0drop/OOM/restart; dev memory limit trả0. Docker CLI MemPerc loại inactive file cache, không phải RSS/tổng mọi trang cgroup. Archive giữ cả lượt Local75s FAIL và các lượt PASS sau, không viết lại kết quả cũ.


Bản sửa tick: endpoint probing A* có tiến độ qua nhiều tick, dùng chung soft budget4ms và trần1500node; không đổi MaxCatchUpSteps/drop counters. Docker LinuxAMD64 publish ReadyToRun. Profile ngắn cùng loại workload nhưng khác seed: tickmax51,272 →39,887 →23,329ms; botmax47,373 →13,050 →6,324ms, không suy phần trăm cải thiện nhân quả. Full Local lần đầu FAIL10drop lúc0peer/0bot (loop327ms, tick<0,1ms, GC0), giữ bằng chứng; lần kiểm lại PASS: 110cửa sổ, tick29.958Hz, CPUmax30.17%, RAM20.76%,0drop/OOM/restart. BotChecks192 assertions, giữ fixture reload trong tầm bắn để đo reload thay vì thời gian tìm đường. Build Windows/source không đổi; reuse đúng artifact có receipt. AWS bản sửa chưa kiểm.

## 5. AWS cuối từ PC thật

Readiness/image pin PASS; changedServices chỉAPI/GameServer, Mongo/Caddy giữ nguyên. Flock thật đã chặn admin thứ hai trong lượt trước; snapshot InGame trả1human+9bot/10entity và`safeToDeploy=false`.

WSS**300.02s**, **exit0**, 300/300pong, timeout0; RTTp50/p95/p99=**166.77/200.24/266.85ms**. Input29.95Hz trên160.27s active; snapshot29.95Hz;10roster/view, tối đa4trên viewport,2trận hoàn tất và movement/zone/HP/result/history/replay PASS. Có spectator, không suy10nhân vật đều sống suốt300s. Không áp ngưỡng50ms Local cho Sydney/WSS.

Auto hai client PASS, fallback qua WSS sau UDP7779 cố ý không có listener; exit0, cùng kết quả/history/replay. RTTp99 hai client lần lượt967,07ms và473,84ms, cần phân biệt tail WAN với ngưỡng Local. UDP7777/WSS smoke riêng trên candidate đã chạy. Đây là một PC, không thay bằng generator trong EC2.

Server: **88cửa sổ**, tick trung bình**29.983Hz**, CPUmộtcore max**23.38%**, RAMmax**25.96%**/87mẫu,0OOM/restart. Nhưng **7dropped tick** trong một cửa sổ lúc19:17:15: tickp99=148,665ms, tickmax=175,387ms, loopgapmax=392,014ms, GCpause=9,963ms; botPathNodesMax186/pending0. **FAIL theo tiêu chí0drop**, không lấy average29,98Hz để thay điều kiện đó.

Không thấy CPU quota/cgroup throttling (`NanoCpus=0`, quota0, `nr_throttled=0`); CPUCreditBalance576 và modeStandard. Chưa đủ để quy nguyên nhân cho AI, GC, JIT hoặc host scheduling. Cần profile spike theo timestamp/stage. Root đã giữ91cửa sổ trong`failureMetrics` trước rollback; không còn mất số đo như lượt reporter cũ.

## 6. Rollback và phần chưa kiểm

Rollback thật về legacy `bots-20261105-r2` **PASS**: API/GameServer đúng image trước đó; config hash/Mongo container/Mongo volume/Caddy container giữ nguyên. Public smoke sau rollback PASS trong20.53s: HTTPS, register/login/JWT, tạo/list/xóa phòng, UDP/WSS; phòng/client test đã dọn. Kết thúc EC2 Running.

Pipeline đã hành xử đúng khi candidate không đạt. Chuỗi dừng trước API-only update, explicit registry-to-registry rollback và deploy lại cùng release: các bài đó **NOT_TESTED trên AWS**, dù module/plan tests PASS. Bài cố ý healthcheck failure AWS cũng NOT_TESTED; rollback do PC gate reject không được gọi là health-failure injection. Hai PC vật lý NOT_TESTED. Không kết luận sức chứa gameplay vô hạn hoặc độ mượt hoàn chỉnh từ ping/pong.

Lượt AWS đầu checksum bị chặn trước switch; lượt tiếp reporter lỗi nhưng client evidence PASS300s và rollback; lượt cuối reporter/exitcode đầy đủ, bị từ chối do performance. Các lần đều giữ riêng bằng chứng. Cần sửa hoặc giải thích bằng profiling cửa sổ spike rồi Local/Full và cửa sổ AWS mới trước khi nghiệm thu còn lại; không tự chạy thêm AWS sau thời hạn.

## 7. Bàn giao

[Hướng dẫn setup/deploy](../../AWS_GHCR_Deploy.vi.md), [Network Architecture](../../Network_Architecture.vi.md). Code/Graphify và một báo cáo JSON/CSV/evidence.zip+SHA256; không push Git. Camera Follow Player; giữ scene, package, secrets, Mongo/outbox/maps và release phục hồi. Không prune toàn hệ thống hoặc xóa volume/package GHCR. EC2 được giữ Running theo yêu cầu, credits tiếp tục phát sinh.
