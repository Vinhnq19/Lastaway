# Phát hành AWS và kiểm tra chất lượng — 08/11/2026

**Kết luận: BLOCKED tại Local. Chưa publish GHCR, chưa deploy hoặc dùng hai cửa sổ AWS.** Bản Windows mới build được và hoàn tất các assertion của lượt WSS 300 giây, nhưng crash native lúc thoát. Không chuyển `PASS` trong JSON gameplay thành PASS phát hành khi exit code của process là lỗi.

## 1. Phạm vi và phiên bản

- Nguồn ban đầu: `main` tại `6ff902e8`; sửa riêng trên `codex/aws-quality-release`, commit code `1396d4f0eb9cafe5d6e14dbac8ae27b93f58b85e`. Không merge main hoặc push Git.
- Unity giữ **6000.3.15f1**, Mono Windows x64, development build. Không nâng Editor, thay renderer hoặc sửa gameplay để làm bài kiểm dễ hơn.
- Bản mới: `D:/Unity/Lastaway/Builds/AWS-Quality-6000.3.15f1/Lastaway.exe`, cùng `_Data`, DLL và runtime trong chính thư mục đó. Đây là **build ứng viên đang FAIL cổng thoát bình thường**, chưa phải bản phát hành đạt nghiệm thu. Không đóng ZIP game.
- Build job `build-efca7d1f18`: **PASS**, 318,80 giây, dung lượng Unity báo 218,23 MiB, 0 error, 2 warning. Receipt `registry-build.json` đã được cổng Local đối chiếu với source và fingerprint player.
- SourceHash: `ffb7cb0ee7f097f31380da3b6986a759f22f4cb84b3c86275cbe38d1a657cfb0`.
- PlayerFingerprint: `e50025ce18577df414d8be356be39239ef6b5cb2eb3b86d96c213b31fea04fae`.
- Catalog nguồn generation 5: Tháp cổ và Thị trấn, cùng version 1.2.0. Không thay luật 10 nhân vật/phòng, 5 phòng, 50 slot, tài khoản bắt buộc và account-ticket v4.

## 2. Công việc đã thực hiện

1. Kiểm compile/runtime data trong Editor, dùng công cụ build chuyên dụng. Truy vấn MCP có timeout trong lúc build; log và kết quả job sau đó xác nhận build thành công, không coi timeout đó là lỗi build.
2. Chạy các nhóm EditMode/PlayMode liên quan, phát hiện một lỗi kiến trúc của splash menu mới: runtime đổi `Image.sprite` trái quy tắc art nằm trong prefab. Sửa thành hai GameObject nền có art gắn sẵn; cập nhật builder, prefab và test. Giữ chức năng Login/Menu và không thêm màn trung gian.
3. Thêm tùy chọn `-QualityClient 1` vào runner online/remote hiện có. Client được chọn dùng 1280×720, mục tiêu 60 FPS; các client khác giữ chế độ nhẹ. Thu frame-time p95/p99, stall ≥100 ms và FPS theo phase khi cửa sổ có focus, tối đa 36.000 mẫu; không đổi tick/input 30 Hz. Phần tính thống kê có test, nhưng **chưa đo runtime 60 FPS đạt nghiệm thu**.
4. Chạy backend regression. Fixture bot cũ giả định RUBBER_GUN bị tắt, trong khi catalog hiện bật món đó. Sửa fixture tắt riêng món kiểm, khôi phục trong `finally`, dừng chờ ngay sau lượt nhặt dao để không lẫn loot phát sinh sau đó. Không tắt món trong game thật.
5. Giữ các lượt thất bại: Local r1 dừng ở fixture cũ; r2 dừng ở BotChecks, wrapper không lưu stdout lỗi nên không kết luận chính xác assertion của r2. Một lượt chạy riêng thất bại kiểm thoát bo khi có tác vụ song song; sau đó ba lượt độc lập PASS. Chưa chứng minh tuyệt đối nguyên nhân nhạy wall-clock, không xóa dấu vết này.
6. Chạy Local r3 độc lập sau bước index/build: toàn bộ backend PASS, dựng Docker theo base image đã khóa, giới hạn thật GameServer 256 MiB, chạy Windows mới qua WSS 1 người +9 bot ít nhất 300 giây.
7. Kiểm Windows Event Log sau khi runner báo lỗi thoát, giữ nguyên chính sách fail theo exit code; dừng phát hành, thu bằng chứng, khôi phục Docker dev và dọn client/phòng do tác vụ tạo.

## 3. Kết quả Local và hồi quy

| Hạng mục | Trạng thái | Bằng chứng |
|---|---|---|
| Backend regression | PASS module | 2.539 kiểm tra: Network 149, DualTransport 53, MapCatalog 81, Bot 206, MatchResult 85, Combat 39, Inventory 1.619, World 174, Match 133 |
| Pipeline registry | PASS module | Chạy lại 26/26 test; parser hai runner PowerShell PASS |
| Unity EditMode liên quan | PASS module | 490/490; job `202ee3cc20544b6fb0c654b3bd244f8d` |
| Unity PlayMode lượt rộng | 129 PASS / 1 FAIL ban đầu | 130 test; lỗi ArtSwapNeedsPrefabOnly được phát hiện và sửa |
| Unity PlayMode phòng chờ sau sửa | PASS module | 28/28; job `637aba67b1214f12a1d4cc3afa7a7dfe`; không ghi thành một lượt full-suite mới |
| Windows build | PASS build | Build mới có receipt hợp lệ; không tái sử dụng EXE cũ |
| Màn đăng nhập 1280×720 | PASS hiển thị / FAIL thoát | Quan sát splash và form đúng; Windows ghi crash khi đóng process menu |
| WSS 1 người +9 bot | PASS assertion module | 300,027 giây kết nối, 10 roster/view, movement, zone, HP, result, history, replay đều được runner quan sát |
| Toàn bộ Local Full | **FAIL** | Process thoát `-1073741819` / `0xC0000005`, dù evidence gameplay ghi PASS |
| Auto hai client / Quick UDP của Local Full | NOT_TESTED | Pipeline dừng ở bước WSS lỗi exit, chưa chạy các bước sau |
| Hai map, đầy đủ combat/loot/DRG qua Windows mới | NOT_TESTED ở mức flow đầy đủ | Regression có kiểm module; không thay bằng bằng chứng tranh loot/PvP hoặc quan sát đủ mốc thật |
| 720p/60 FPS, đủ các tỉ lệ màn hình và đánh giá nghe | NOT_TESTED | Không dùng lượt mạng 30 FPS hoặc unit test để nhận PASS chất lượng render/audio |

Các test module bao phủ logic damage, inventory, bo, trứng/loot, protocol và idempotency; chúng không tự chứng minh mọi hành vi đã được quan sát trong client thật của release này. Lượt Windows thực trong r3 dùng map mặc định Tháp cổ; chưa chạy trận Thị trấn của bản mới.

## 4. Số đo thực và lỗi chặn

| Chỉ số Local WSS | Giá trị |
|---|---:|
| Thời gian kết nối | 300,027 s |
| Tốc độ input khi được phép gửi | 29,941 Hz |
| Snapshot | 30,001 Hz |
| RTT p50 / p95 / p99 | 33,35 / 33,79 / 55,72 ms |
| Ping / pong / pong timeout | 301 / 300 / 0 |
| Match hoàn tất được quan sát | 1 |
| Client catch-up drop | 1; tách biệt với dropped tick server |
| Server mean tick | 29,786 Hz, 65 mẫu |
| CPU cao nhất, quy về một core | 48,38% |
| RAM cao nhất theo Docker | 26,52% giới hạn 256 MiB, 65 mẫu, không lỗi sampler |
| Dropped tick server trong log | 0 |

Input chỉ tính khoảng client còn sống/được phép gửi, không phải input liên tục 300 giây. Có một ping còn outstanding khi kết thúc, không coi đó là timeout. Các số đo server là số quan sát trong lượt kiểm; không ký PASS toàn bộ release, không suy ra sức chứa gameplay tối đa.

Máy Local: Ryzen 7 4800H, 8 core/16 logical processor, RAM khoảng 16 GiB, Windows 11; có Radeon tích hợp và GTX 1650. Lượt này không xác định GPU thực dùng bằng profiler và chưa đo FPS chất lượng. Chi tiết trong evidence.

**Hai crash cùng dấu vết:** menu PID 33624 lúc 19:58:21 và WSS player PID 47096 lúc 20:20:23 (UTC+7), đều `UnityPlayer.dll`, exception `0xC0000005`, offset `0xC29819`. Log gameplay đã đi đến cleanup, nhưng Windows Event 1000 và exit code vẫn xác nhận crash.

Dấu vết module/offset trùng lỗi native lịch sử liên quan cleanup Accessibility. Unity mô tả lỗi Windows player crash khi thoát sau tương tác UI Automation trong [release notes 6000.3.21f1, UUM-146676](https://unity.com/releases/editor/whats-new/6000.3.21f1). Đây là **suy luận đối chiếu**, chưa có dump được symbol hóa mới để khẳng định call stack chính xác hoặc chỉ ra UI Automation client nào đã kích hoạt lần này. Giữ 6000.3.15f1 theo yêu cầu; không vá DLL, tắt accessibility, ép kill process hoặc bỏ kiểm exit để nhận PASS.

## 5. AWS, chi phí và phần chưa nghiệm thu

- **AWS: NOT_TESTED/BLOCKED bởi cổng Local.** Không gọi AWS CLI/SSM, publish GHCR, pull/deploy, reload map, Start/Stop, nâng máy hoặc tạo hạ tầng. Hai cửa sổ 20 phút **chưa sử dụng**.
- Không có release/digest AWS mới, thời gian và byte push/pull/deploy chưa đo; không dùng số cũ thay thế. Chưa đối chiếu catalog/DNS/TLS/readiness/participant thực trên EC2.
- Không xác nhận được trạng thái EC2 cuối lượt vì đã dừng trước cổng AWS; không tự ghi Running. Tác vụ không thay đổi lifecycle. Nếu máy đang Running từ trước thì vẫn có thể tiếp tục tiêu credits; chưa đo balance hoặc chế độ Standard/Unlimited của máy ở lượt này.
- UDP public, WSS public, fallback public, hiệu năng AWS, CPU credits, stop/start: NOT_TESTED. Không mở lượt AWS hoặc bỏ guard chỉ để hoàn tất báo cáo.
- Hai PC vật lý và buổi 6–10 người thật: **NOT_TESTED**. Hai tiến trình Local sẽ không thay bằng chứng hai máy.

## 6. Bằng chứng và trạng thái bàn giao

- Một bộ báo cáo: `report.vi.md`, `report.json`, `measurements.csv`, `evidence.zip`, `SHA256SUMS` trong thư mục này. ZIP chỉ chứa bằng chứng, **không phải game Windows**.
- Archive gồm summary Unity, log pipeline/backend, kết quả/lịch sử kiểm Local, log server/player, ảnh gameplay/kết quả, receipt build, cấu hình phần cứng và Windows crash event. Không đưa `.env`, password, token, client config, WER memory dump hoặc credential vào gói.
- JSON có checksum từng entry; archive được đọc kiểm CRC trước bàn giao. Checksum ngoài gói trong `SHA256SUMS`.
- Client do tác vụ mở đã đóng, phòng test dọn không báo lỗi. Docker GameServer đã khôi phục `HostConfig.Memory=0` theo cấu hình dev trước đó; giữ API/Mongo và volume. Không prune image hoặc xóa DB.
- Camera Follow Player được giữ; screenshot gameplay xác nhận viewport theo nhân vật, spectator được giữ. Editor vẫn mở Boot; không force kill Editor khi MCP query cuối không trả dữ liệu.
- Hướng dẫn GHCR và Network Architecture đã cập nhật chế độ đo chất lượng; Graphify được cập nhật. Warning extractor về 4 file UniTask và 25 file không có symbol không được coi là lỗi C# Unity.

## 7. Cách tiếp tục và checklist chơi thật

Bước tiếp theo là xử lý/đối chiếu crash shutdown trên **6000.3.15f1**, xác nhận player exit bình thường, rồi chạy lại Local Full với output mới. Chưa phát hành build ứng viên này làm bản đã nghiệm thu.

Sau khi cổng Local thực sự PASS, dùng `Backend/Deploy/deploy-aws.ps1` theo hướng dẫn GHCR: publish từ working tree sạch, chọn đúng player/LocalEvidence, preflight máy trống, deploy theo digest và hoàn thành Full trong chính cửa sổ thứ nhất. Thiếu thời gian hoặc có lỗi thì rollback có xác nhận, không giữ release chỉ smoke PASS. Lượt thứ hai mới đo hai map/720p hoặc sửa-xác minh lại nếu lượt đầu lỗi; không tự mở lượt thứ ba. Kết thúc giữ EC2 Running và phải đọc xác nhận trạng thái thật.

Checklist buổi 6–10 người sau khi release đạt:

1. Mỗi người lấy **cả thư mục Windows build**, chọn Online AWS, đăng nhập tài khoản riêng. Admin kiểm DNS/TLS và thời hạn buổi chơi.
2. Tạo/vào cùng phòng, chọn skin, lần lượt thử Tháp cổ và Thị trấn; xác nhận số người, chủ phòng, map readiness và Bắt đầu.
3. Kiểm movement, F nhặt đồ, tranh loot, đổi súng, đạn/reload ring, hit/HP/armor, bo kế tiếp trên minimap, ba lượt rồng nếu trận kéo dài đủ; nghe SFX và đánh giá cảm giác điều khiển.
4. Kiểm loại/spectator, bảng kết quả/history, chơi lại, đổi map và menu. Có ít nhất hai PC thật; thử cả tuyến UDP thực và WSS/Auto từ mạng chặn UDP.
5. Thu FPS/RTT/correction/snapshot gap và server metrics. Mất mạng/Sleep hoặc máy phát không đạt tốc độ phải đánh INVALID, không quy lỗi ngay cho server. Không ghi PASS cho mốc bo/rồng chưa quan sát.
6. Đóng client, dọn phòng, giữ Follow Player và spectator đúng chức năng; admin kiểm lại EC2 Running và chủ động quản lý thời gian chạy/credits sau buổi chơi.
