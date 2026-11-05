# GHCR AWS — bản sửa hiệu năng đã deploy; no-change chưa kiểm

## 1. Kết luận

**Bản GameServer sửa spike đã được nghiệm thu Full và đang chạy trên AWS.** Current: `ghcr-20261105-135413-1215f52f`, previous: `ghcr-20261105-135847-83678135`. WSS từ PC thật đủ 300 giây, Auto hai client, UDP/WSS smoke, JWT/phòng/history/replay và gate hiệu năng PASS. Không hạ ngưỡng hoặc tăng tài nguyên.

Đã kiểm cập nhật riêng API và rollback server thành công. PC báo lỗi giả khi worker rollback kết thúc giữa hai lần đọc trạng thái; đã sửa và qua 26 test pipeline Local. **AWS no-change và lệnh rollback mới với observer đã sửa chưa được chạy lại.** Không đánh toàn bộ pipeline là hoàn tất.

Lượt thực tế 21:02:13–21:18:51 ngày 05/11/2026 (UTC+7), **998.36s <1200s**. Trước đó preflight dừng sau 5s vì AWS CLI hết hạn, không thay service; người dùng đăng nhập lại rồi mới bắt đầu lượt thực tế. Kết thúc EC2 **Running**, không Start/Stop/nâng máy hoặc Unlimited.

## 2. Những gì đã sửa và cơ chế deploy

A* trước đây giới hạn node nhưng chuẩn bị 50 điểm đầu/cuối vẫn làm dồn một lần. Nay giữ tiến độ probing/frontier qua nhiều tick, thêm soft budget 4ms dùng chung toàn server cùng trần 1500 node/500 node mỗi yêu cầu. Đến điểm kiểm budget thì nhường; đơn vị công việc đang chạy có thể vượt 4ms. Giữ luân phiên bot/phòng, tốc độ, collision và luật gameplay. Fixture reload đặt đối thủ trong tầm bắn để kiểm reload, tránh lẫn thời gian đi đường ngẫu nhiên.

Docker GameServer publish Linux AMD64 ReadyToRun để giảm JIT lúc gọi đầu. Thêm số đo max từng stage bằng bộ đếm cố định. Không đổi accumulator, MaxCatchUpSteps hoặc cách tính dropped tick. Profile Local chỉ cho thấy tương quan; không chứng minh chắc chắn nguyên nhân cửa sổ AWS cũ chưa có stage.

Pipeline dùng GHCR private/digest, clean commit/fingerprint riêng hai service, checksum và bundle không chứa image/secret/DB. Pull trước switch; flock/systemd giữ giao dịch qua kiểm từ PC. Digest đối chiếu Compose đã resolve; control nội bộ có secret/occupancy. Giữ Mongo/outbox/map/private config, không migration DB.

Observer PC nay đọc lại trạng thái cuối khi unit inactive, chỉ nhận PASS nếu **transactionId + releaseId** khớp; ghi `remote-final.json`. Không nhận thành công của lượt cũ. Đây là sửa công cụ PC; root/image đã chạy Full không bị đổi sau bài đo.

## 3. GHCR, dữ liệu truyền và thời gian

Hai package vẫn Private, liên kết repository Lastaway. PC dùng credential store Docker Desktop; EC2 token chỉ read:packages, file600/thư mục700. Không token/JWT/password trong báo cáo hoặc bundle.

| Release | Service đổi | Build/push hoặc reuse | Layer nén mới | Bundle |
|---|---|---|---:|---:|
| `ghcr-20261105-135413-1215f52f` |GameServer |10,584s; API reuse5,783s |1.284.257byte |23.086byte |
| `ghcr-20261105-135847-83678135` |API branding Swagger |21,455s; GS reuse5,680s |3.798.709byte |22.744byte |

Đây là cache ấm; layer delta không phải wire bytes hoặc traffic EC2. Wire bytes NOT_MEASURED. Full AWS pull5,868s; API-only pull2,888s. Tổng bước Full536,475s; API-only107,397s. Lệnh rollback PC55,555s báo lỗi giả; journal worker cho thấy thành công14:13:56–14:14:07 UTC. Phần lớn thời gian Full là kiểm bắt buộc, không phải upload image archive.

## 4. Local, build và kiểm hồi quy

Backend0error/0warning; Network149, Bot192, World168 assertions PASS; DualTransport/MapCatalog/MatchResult/Combat/Inventory/World/MatchChecks PASS. Pipeline26 tests PASS, gồm stale transaction/wrong release bị từ chối. Launcher37 fixture checks đã PASS ở lượt trước; fixture không phải gameplay.

Windows6000.3.15f1 giữ đúng artifact/source receipt đã xác minh; thay đổi chỉ server/deploy/Swagger, không protocol/client/shared data. Không cần rebuild Windows. Camera Follow Player, spectator giữ đúng.

Profile ngắn 1human+9bot: tickmax51,272ms (bot47,373) → time-slice39,887ms (bot13,050) → ReadyToRun23,329ms (bot6,324). Seed khác nhau nên không suy phần trăm cải thiện nhân quả. Năm seed BotChecks độc lập PASS sau sửa fixture.

Full Local đầu của bản sửa: gameplay PASS nhưng toàn lượt FAIL10drop ở cửa sổ0peer/0bot, loop327ms/tick<0,1ms/GC0; giữ bằng chứng, không gọi chắc chắn là lỗi host. Lượt kiểm lại PASS: WSS300,02s, Auto197,9s/197,8s, account/result/history/replay/UDP/WSS; **110 cửa sổ**, tick29.958Hz, CPUmax30.17%, RAM20.76%,0drop/OOM/restart. Limit256MiB thật; cuối trả dev limit0. DockerMemPerc loại inactive file cache, không phải RSS/tổng mọi trang cgroup.

## 5. AWS từ PC thật

| Kiểm tra | Kết quả | Bằng chứng |
|---|---|---|
| Full release/digest/readiness |PASS |Root PASS; API/GS thay khi chuyển legacy |
| WSS1human+9bot |PASS |300.01s, exit0, 300/300pong, timeout0 |
| Auto hai client |PASS |WSS sau UDP7779 không listener;49,86s/50,23s, exit0 |
| UDP7777/WSS + JWT/phòng |PASS |PC thật; thiếu/sai JWT bị từ chối |
| Hiệu năng server |PASS |85 cửa sổ, tick30.001Hz; **0dropped tick/OOM/restart** |
| API-only |PASS |Chỉ API đổi; GS/Mongo/Caddy giữ ID |
| Rollback server |PASS |Đúng transaction, journal exit thành công, API về digest trước |
| PC xác nhận rollback lượt gốc |FAIL, đã sửa Local |Race status/inactive; giữ nguyên raw exitcode1 |
| Public sau rollback |PASS |28.49s: login/JWT/phòng/UDP/WSS |
| AWS no-change |NOT_TESTED |Dừng chuỗi; không đủ budget an toàn mở giao dịch mới |

WSS RTTp50/p95/p99=**166.72/200.15/233.44ms**. Input29.94Hz trên135.66s active; snapshot30.00Hz.10roster/view, tối đa4trong viewport Follow,2trận hoàn tất. Có spectator; không suy10nhân vật sống đủ300s. Client catchupDrops=1, khác dropped tick server0; tốc độ phát vẫn đạt. Auto RTTp99=392,24/422,76ms. Không áp50ms Local cho WAN Sydney hoặc kết luận gameplay hoàn toàn không giật.

Server CPU một core max**19.99%**, RAMmax**25.59%**/83 mẫu, tickmax36.46ms. Max tick không phải deadline bảo đảm từng tick; gate dùng ngưỡng hiện hành. Không tăng CPU/RAM để đạt.

## 6. Rollback, dữ liệu và phần chưa kiểm

API đổi ID khi cập nhật nhỏ và rollback; GameServer giữ cùng ID xuyên hai bước. Mongo/Caddy ID, Mongo volume và private config hash giữ nguyên từ đầu đến cuối. Cùng tài khoản login được sau Full/API-only. Sau rollback kiểm login public mới và dịch vụ, không gọi đó là cùng-account qua rollback. Room/client test đã dọn; không tự xóa account test Mongo.

Current giữ release sửa tick đã PASS, previous là API branding đã thử; branding Swagger mới chưa được giữ trên AWS. Không Rollback mù: previous đã đổi có thể đảo ngược lựa chọn vừa thực hiện.

Observer mới PASS26 fixture tests, **chưa gọi một rollback AWS mới**. AWS no-change, health-failure injection riêng và hai PC vật lý NOT_TESTED. Module no-change/health-failure tests không thay các trường hợp đó. Không chạy tải50–100phiên.

Lượt trước vẫn giữ JSON/zip: checksum bị chặn; reporter cũ lỗi dù client đủ300s; bản trước FAIL7drop và rollback legacy. Không xóa lỗi cũ hoặc đổi kết quả thành PASS. Lượt sửa này mới có bằng chứng Full server0drop.

## 7. Bàn giao

[Setup/deploy](../../AWS_GHCR_Deploy.vi.md), [Network Architecture](../../Network_Architecture.vi.md), Graphify cập nhật sau code/config. Một báo cáo JSON/CSV/evidence.zip+SHA256, snapshot/digest/raw exit/journal và screenshot. Không push Git, sửa secrets/Unity version hoặc prune volume/package GHCR.

Client test đã đóng. Camera Follow Player; dev Docker trả cấu hình cũ. EC2 xác nhận **Running lúc21:18:51 UTC+7**, không thao tác AWS sau khi chốt. **Credits tiếp tục phát sinh** theo lựa chọn giữ máy. Các mục NOT_TESTED ở trên chưa được nghiệm thu; không tự mở thêm cửa sổ AWS.
