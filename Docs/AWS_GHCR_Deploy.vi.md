# Deploy AWS qua GHCR private

Luồng hằng ngày: kiểm Local → build/push service đổi → gói manifest nhỏ → EC2 pull digest → cập nhật riêng service cần đổi → kiểm tra từ PC → chấp nhận hoặc rollback.

Pipeline mới nằm ở `Backend/Deploy/deploy-aws.ps1`. Đây là deploy có gián đoạn trong cửa sổ bảo trì. Restart GameServer làm mất session/phòng/trận RAM. Không có migration DB, blue-green hoặc tự Start/Stop EC2. Mongo, config bí mật, outbox và catalog map ngoài image được giữ nguyên.

## 1. Điều kiện và setup một lần

PC: Docker Desktop Linux containers + credential store, Docker Buildx, Python 3.12+, .NET SDK chạy được net8, Windows PowerShell, AWS CLI profile `junglemaze-admin`, SSH alias `junglemaze` qua SSM đã hoạt động. Unity vẫn là **6000.3.15f1**. EC2 hiện có phải là x86_64/t3.small, đã cài Docker Compose, Python 3.12 và systemd; script không tạo hoặc nâng máy.

### 1.1. Token và package

Tại GitHub → Settings → Developer settings → Personal access tokens → Tokens (classic), tạo hai token riêng, đặt ngày hết hạn phù hợp (đề xuất 30 ngày):

| Nơi dùng | Quyền classic PAT | Nơi giữ |
|---|---|---|
| PC publish | `write:packages`, gồm quyền đọc package | Credential store của Docker Desktop |
| EC2 pull | chỉ `read:packages` | Docker config của root, thư mục 700/file 600 |

Không chọn `repo`, `delete:packages` hoặc quyền admin chỉ để publish hai image. Nếu GitHub yêu cầu Confirm access/passkey/2FA, chủ tài khoản thực hiện trực tiếp. Không gửi PAT vào chat hoặc đặt vào `.env`, argv, report hay repository.

Tại PowerShell ở thư mục dự án:

```powershell
& .\Backend\Deploy\setup-ghcr.ps1 -Target PC
# Nhập PAT publish khi được hỏi; chữ không hiện là bình thường.
& .\Backend\Deploy\setup-ghcr.ps1 -Target EC2
# Nhập token read-only riêng. EC2 phải đang Running và SSH alias hoạt động.
```

Lệnh dùng `--password-stdin`, xác minh owner/scopes, không in nội dung token hoặc HTTP response lỗi. Setup không bật EC2. Muốn đổi token/hết hạn: tạo token thay thế đúng quyền, chạy lại setup cho đúng nơi, xác nhận pull/publish rồi revoke token cũ tại GitHub. Không dùng chung PAT publish cho EC2.

Hai package:

- `ghcr.io/vinhnq19/lastaway-api`
- `ghcr.io/vinhnq19/lastaway-game-server`

Lần push đầu tạo package; GHCR mặc định private. OCI source label liên kết với `Vinhnq19/Lastaway`. Sau lần đầu, kiểm trên GitHub profile → Packages → từng package → Package settings: **Private**, repository **Lastaway**, account dùng token EC2 có quyền đọc. Publisher kiểm visibility/repository trước khi nhận release; package đã public hoặc sai liên kết sẽ bị chặn. Chưa tự xóa package/image GHCR.

[Tài liệu GHCR](https://docs.github.com/en/packages/working-with-a-github-packages-registry/working-with-the-container-registry). [Chính sách giá](https://docs.github.com/en/billing/concepts/product-billing/github-packages): container storage/bandwidth hiện miễn phí; không coi đây là bảo đảm vĩnh viễn.

### 1.2. Working tree và build client

Publisher yêu cầu Git working tree sạch, commit cụ thể; không tự commit hoặc push Git. Kiểm tra diff, commit phần cần phát hành trước khi publish. Output trong `Backend/out/` được ignore. Không bỏ qua file `.meta` có ý nghĩa chỉ để vượt cổng sạch.

Khi client/protocol/data thay đổi, dùng Unity → **Jungle Maze → Phase 2 → Sync Runtime Data**, rồi build Windows mới. Build hook hiện có xuất `registry-build.json` bên cạnh EXE, ghi Unity version, hash source/data và fingerprint toàn bộ player. Source hash chụp đầu vào trước build, tập trung vào code, serialized runtime config/scene/prefab và Shared; không quét lại bulk texture/audio. Cổng Local/publish so lại hash sau khi Editor trả các thiết lập build tạm về trạng thái thường; không stamp source mới vào một EXE cũ. Không tự viết file này để nhận build cũ là build mới. Giữ camera Follow Player, spectator chỉ khi đang xem người khác.

## 2. Nghiệm thu Local Full

Mở Docker Desktop. Kiểm `Backend/.env` có các secret Local và account-ticket như hướng dẫn Phase 1, độc lập AWS. Đóng client dev trước khi runner build/up stack Local. Runner không xóa DB hoặc volume.

```powershell
& .\Backend\Deploy\deploy-aws.ps1 -Mode PrepareOnly -VerifyLocal `
  -Player 'D:\Unity\Lastaway\Builds\Windows-GHCR-6000.3.15f1\Lastaway.exe' `
  -OutputDirectory 'D:\Unity\Lastaway\Backend\out\registry-local'
```

Mỗi lượt dùng OutputDirectory mới (hoặc bỏ tham số để tự tạo thư mục theo giờ); runner từ chối ghi đè bằng chứng cũ. Lượt này **không gọi AWS/GHCR publish**. Nó chạy hồi quy backend và unit tests pipeline; resolve/pin SDK/runtime base Linux AMD64; build/up Docker Local (tạm đặt GameServer 256MiB và thu CPU/RAM/tick, sau đó trả cấu hình dev thường); dùng runner Unity hiện có cho một người +9 bot WSS (kết nối ít nhất 300 giây), hai client Auto +4 bot, kết quả/history/replay; thêm smoke UDP/WSS/JWT/phòng. API Local mặc định `http://127.0.0.1:5000`, đổi bằng `-LocalApi` khi cần. Kết quả là `local-evidence.json` cùng log/JSON của runner.

Không sửa source/config/client sau bằng chứng; thay đổi làm hash lệch phải build hoặc chạy lại cổng tương ứng. Ngày hết hạn token không phải lý do hạ xuống Guest hoặc bỏ TLS. Hai process chỉ chứng minh một PC; hai PC vật lý vẫn **NOT_TESTED**.

## 3. PrepareOnly: chuẩn bị khi EC2 đang tắt

Sau khi commit sạch và Local đạt:

```powershell
& .\Backend\Deploy\deploy-aws.ps1 -Mode PrepareOnly `
  -Player 'D:\Unity\Lastaway\Builds\Windows-GHCR-6000.3.15f1\Lastaway.exe' `
  -LocalEvidence 'D:\Unity\Lastaway\Backend\out\registry-local\local-evidence.json' `
  -OutputDirectory 'D:\Unity\Lastaway\Backend\out\registry-release'
```

API fingerprint gồm API, hai Shared contract đang compile, Dockerfile/dependency/build config. GameServer gồm server + Shared protocol/world/game data. Resolve SDK/runtime **digest Linux AMD64** cũng nằm trong fingerprint. Service không đổi reuse digest từ ledger `Backend/out/registry/published.json`; service đổi dùng Docker Buildx cache và push layer cần thiết. Không có `latest` trong deploy.

Output gồm thư mục release, `manifest.json`, `release.env` trỏ đủ hai image theo digest, compose/Caddyfile/scripts, `SHA256SUMS`, archive release nhỏ và checksum archive. **Không có `images.tar.gz`, secret hoặc DB**. Archive nhỏ chỉ vận chuyển script/config/manifest; image được EC2 pull riêng từ GHCR.

Manifest ghi commit, fingerprint, base pins, protocol/data version và thời gian/layer delta mỗi service. Layer bytes là kích thước compressed OCI, không phải đo traffic TLS/retry chính xác. Giữ ledger và bundle để reuse; mất ledger vẫn build/push được nhưng phải coi là phát hành Full đầu tiên.

## 4. Deploy: một cửa sổ bảo trì

Dành riêng server, không có người chơi; bật EC2 thủ công nếu cần, đợi Running/SSM/DuckDNS/HTTPS sẵn sàng. Pipeline tự từ chối máy Stopped, sai instance SSH, DNS/TLS lỗi hoặc hết dung lượng; không tự Start/Stop. Đồng hồ AWS chỉ bắt đầu sau phần chuẩn bị/publish nếu dùng một lệnh gộp.

```powershell
& .\Backend\Deploy\deploy-aws.ps1 -Mode Deploy `
  -Release 'D:\Unity\Lastaway\Backend\out\registry-release\<release-id>' `
  -Player 'D:\Unity\Lastaway\Builds\Windows-GHCR-6000.3.15f1\Lastaway.exe' `
  -LocalEvidence 'D:\Unity\Lastaway\Backend\out\registry-local\local-evidence.json' `
  -BudgetMinutes 20
```

Thay `<release-id>` bằng thư mục thực được PrepareOnly in ra. Có thể bỏ `-Release` để Prepare + Deploy trong một lệnh. Không dùng ký hiệu `<...>` nguyên xi trong terminal.

Lần chuyển đổi từ image cũ chưa có deployment-status: chỉ sau khi đã dành riêng cửa sổ trống, thêm `-ConfirmEmptyMigration`. Server cũ phải có metrics gần đây, human/pending/bot đều 0 qua hai lần đọc cách nhau hơn thời gian Ending cleanup. Không dùng cờ này để bỏ qua occupancy endpoint đã có: endpoint mới báo bận thì vẫn từ chối.

Remote giữ `flock` suốt pull, switch và kiểm từ PC. HTTP control `/internal/deployment-status` chỉ đọc qua queue ứng dụng, cần secret riêng; không public 7780 và Caddy chặn `/internal/*`. Snapshot gồm active/pending/grace, bot, entity và phase, không trả RoomCode/credential. Có participant hoặc trận chưa dọn thì không cập nhật GameServer.

Không thêm `API_IMAGE`/`GAME_IMAGE` vào `.env.aws`: image ứng dụng lấy từ release đã pin. Root so sánh image của Compose đã resolve với manifest; khác digest sẽ bị chặn trước pull hoặc đổi service.

Pull digest trước khi đổi service; kiểm Linux AMD64, image cũ để rollback, đĩa trống và Mongo config hiệu lực. Chỉ force-recreate service thay đổi, `--no-deps`; Caddy chỉ khi proxy/config của nó đổi. Mongo không restart. Secret/outbox/maps giữ nguyên. Health kiểm container, image thực tế, status server và public PC tests. Chỉ sau PASS mới đổi current/previous.

Guard chạy bằng **transient systemd unit**, không phải timer Stop EC2. PC Sleep/mất SSH/không gửi chấp nhận đúng hạn sẽ rollback service đã thay. Không có việc tự tắt EC2. Nếu không xác nhận được kết quả remote, report ghi unit/staging để admin kiểm qua SSM trước khi deploy lại.

## 5. Auto, Full và deadline

- `Auto`: API-only thông thường → health, register/login/JWT, tạo/list/xóa phòng, UDP/WSS ping/pong ngắn. Không kết luận combat/history/performance PASS từ Quick.
- GameServer/protocol/rules/shared contract hoặc deploy config ảnh hưởng kết nối đổi → bắt buộc Full. `-TestMode Full` có thể tăng mức kiểm, không có cờ hạ xuống Quick.
- Full: một người +9 bot WSS đủ **300 giây**, thêm hai client Auto cố ý thử UDP không listener rồi fallback WSS, kết quả/history/replay; smoke UDP thật trên 7777. Không thay generator trong EC2 cho PC.
- Full dùng scenario tối đa 480 giây cho lượt WSS và 240 giây cho lượt Auto; thời lượng đo WSS tối thiểu vẫn 300 giây, không rút ngắn để báo PASS. Phải còn đủ ngân sách cộng rollback mới bắt đầu.
- Ngưỡng: input/snapshot từng client ≥28,5Hz; pong timeout <1%; tick trung bình 29–31Hz, không dropped/OOM/restart; CPU một core ≤85%, container RAM ≤80%. Giữ RTT WAN thực, không áp 50ms Local cho Sydney.

Lượt AWS tối đa 20 phút, dành khoảng 200 giây cho phục hồi. Deadline dừng công cụ/client, không Stop instance. Nếu pull/kiểm ban đầu quá lâu để còn đủ Full thì dừng, không giảm thời lượng. Nếu root guard chưa xác nhận xong, không xóa staging mà nó đang dùng.

## 6. Rollback, lỗi và giữ dữ liệu

```powershell
& .\Backend\Deploy\deploy-aws.ps1 -Mode Rollback `
  -Release 'D:\Unity\Lastaway\Backend\out\registry-release\<release-id>'
```

`-Release` cung cấp bundle công cụ đã xác minh; **image được khôi phục là `/opt/junglemaze/previous`**, không phải digest người dùng tự gõ. Hỗ trợ previous archive cũ và registry mới, đọc cả `base-images.env` kế thừa trước `release.env`. Không hạ major Mongo, không ghi `.env.aws`, không migration DB. Rollback explicit kiểm health server/digest; không gọi một lượt gameplay Full mới trên bản đã nghiệm thu, và báo public acceptance NOT_TESTED nếu chưa chạy lại.

Nếu lỗi sau switch, deploy tự restore các service đã thay và kiểm health. `ROLLED_BACK` khác `ROLLBACK_FAILED`; trường hợp sau cần admin xử lý qua SSM, không suy rằng server đã được phục hồi. Kiểm:

```bash
sudo cat /opt/junglemaze/deploy-state/state.json
sudo journalctl -u lastaway-deploy-<id> --no-pager -n 30
sudo docker ps --format 'table {{.Names}}\t{{.Status}}'
```

Nếu kiểm từ PC lỗi sau readiness, root transaction chụp `failureMetrics` trước khi recreate GameServer cũ vì Docker có thể xóa log container candidate khi rollback. Đọc trong `remote-final.json`; `UNAVAILABLE` là thiếu bằng chứng, không được đổi thành PASS. Reporter ghi tên lệnh PowerShell bị thiếu và vị trí script, không ghi token hoặc HTTP body. Client có evidence PASS nhưng thiếu exit code/report tổng hợp vẫn chưa đủ để chấp nhận release.

Token sai/hết hạn, thiếu digest, checksum sai, sai architecture, đĩa thấp hoặc đang deploy/bận sẽ bị chặn. Token chỉ nhập lại bằng setup; không chép Docker config root vào repo. Không mở Mongo/control ra Internet để debug.

Giữ current + previous cùng image tương ứng. Chưa tự xóa GHCR package. Không `docker system prune -a`, không xóa volume; chỉ dọn staging thuộc tác vụ đã kết thúc và image đã chứng minh không còn tham chiếu. Caddy không đổi có thể còn bind file từ release cũ, nên không xóa cả thư mục release chỉ theo tuổi. Archive cũ `build-aws-release.ps1` vẫn là đường phục hồi chủ động, không tự fallback upload image khi registry lỗi.

## 7. Bằng chứng và giới hạn

Output runner có report JSON/CSV/log, remote-ready/final, server metrics và trạng thái lifecycle. Báo cáo chung ghi rõ phần nào PASS module, PASS flow, BLOCKED hoặc NOT_TESTED. Cần đo riêng first publish/deploy, cập nhật nhỏ và no-change sau khi có quyền GHCR; chưa có số đo thì không hứa thời gian. Client Windows phát hành riêng; publish map dùng catalog ngoài image như trước.

Kết thúc xác nhận EC2 vẫn Running. Chi phí EC2/EBS vẫn phát sinh theo tài khoản AWS dù không còn client. Camera bàn giao phải Follow Player; không dùng bản build thất bại hoặc cũ làm bằng chứng bản mới.

### Khi gate bị từ chối vì spike tick

Đọc `server-metrics.json` và `remote-final.json.failureMetrics` để đối chiếu các stage `botDecisionMaxMs`, `botPathMaxMs`, `worldMaxMs`, `lifecycleMaxMs`, `pollMaxMs` với `tickMaxMs`, GC và loop gap. Không tự đổi0drop thành ngưỡng dễ hơn. A* hiện chia endpoint probes và node expansion qua tick, có budget mềm4ms toàn server ngoài cap node. LinuxAMD64 dùng [ReadyToRun của .NET](https://learn.microsoft.com/en-us/dotnet/core/deploying/ready-to-run) để giảm chi phí JIT ở lần dùng đầu; vẫn phải chạy Full Local và PC→AWS300s trước khi giữ release.
