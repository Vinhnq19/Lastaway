# Phase2 — ghép flow tài khoản và gameplay, 01/11/2026

**Đang sửa và kiểm thử; chưa nghiệm thu toàn flow hoặc deploy AWS.** Module backend/account và Unity unit/play tests đã có bằng chứng PASS. Windows player còn crash lúc thoát; lượt sáu client mới phát hiện thêm lỗi WebSocket trong lúc chơi. Không dùng PASS của module hay bản24–25/10 để kết luận bản hiện tại sẵn sàng online.

## 1. Phạm vi và tiêu chí

Luồng đích: đăng ký/đăng nhập → tạo/vào phòng → Lobby/Loading → movement, loot/combat, bo/HP → elimination/spectator → kết quả/account history → replay/menu. Offline một người không cần API/Docker và không ghi lịch sử online. Giữ10 người/phòng,5 phòng gồm mặc định,50 phiên,2 phòng bổ sung/account. DRG-01, ART-06, Game-10 ngoài phạm vi; hai PC vật lý chưa có thiết bị để nghiệm thu.

## 2. Những thay đổi đã thực hiện

- Account bắt buộc ở Unity/API/GameServer. JWT chỉ tới API HTTPS; game nhận ticket64hex,30s, một lần, ràng buộc account/phòng/attempt trước roster. Resume cần proof và ticket mới cùng account. Production từ env không có tùy chọn tắt account guard.
- Protocol v4 có cap512B. UDP/WSS dùng chung phòng/slot; Auto giữ một lượt ticket và một slot, callback UDP muộn không tạo lại session. v1/v2/v3 còn ở fixture riêng, không bypass server chạy bình thường.
- UI LAN/online đi qua AccountSession thật; offline luôn có từ Login/Home. Mã phòng join giữ nguyên hoa/thường và hỗ trợ mã mặc định dài; phòng tạo mới vẫn mã6 ký tự. Logout/đổi identity dọn token và history cũ.
- Dữ liệu1.1.0 có60 rương,90 chum,45 bụi,3 cặp portal,2 cửa,2 swamp,2 lava,4 poison; giữ4 cover/10spawn. Tổng215 object. Unity/server đồng bộ manifest/hash; build từ chối data cũ.
- OfflineWorldSession dùng lại destructible, hazard, item/inventory và combat service thuần C#. OfflineMatchSession giữ quyền HP; HeroController giữ chuyển động. Có death-drop, reset replay/menu, slow/boost và portal. Không chạy hai mô phỏng.
- Ghép visual có sẵn. Đã sửa sprite cover bị phóng theo kích thước texture: child visual khớp bounds XY của collider, không đổi geometry. Không chứng nhận license audio chưa rõ.
- Mở rộng test thực với Mongo/API: ticket/account/UDP/WebSocket/Auto, hạn mức10, kết quả idempotent và history sau restart. Launcher giữ kiểm tra exit code, không biến file evidence PASS thành toàn lượt PASS nếu player crash.
- Sau lượt sáu client, đang sửa gửi SnapshotAck theo snapshot mới nhất mỗi frame để tránh burst ACK khi nhận backlog; thêm phân biệt queue gửi/nhận và giữ nguyên lý do/transport lỗi trước khi resume reset stats. Thay đổi mới này cần chạy Windows lại.

## 3. Môi trường và nguồn bằng chứng

Nhánh `codex/phase2-account-flow-20261101`, baseline `0eb51d89af5c786551a168ab04d4c3fb011df37e`; thay đổi chưa commit. PC Ryzen7 4800H/16 luồng, khoảng16GiB RAM, GTX1650. Unity6000.3.15f1/URP. Docker Local có GameServer, API và Mongo thật, dữ liệu tách AWS. Player Windows mới được build từ source này; không dùng build28/10 làm nghiệm thu bản mới.

Hai profile được dùng: `test` cho2 người/timer tăng tốc và `main` min6/lobby15s/loading30s/lịch bo GDD. Test tự gửi input thật qua các port hiện có; không tự sửa HP, snapshot hoặc rank. Bằng chứng một PC nhiều process không thay hai PC vật lý.

## 4. Kết quả đã quan sát

| Hạng mục | Trạng thái | Bằng chứng/giới hạn |
|---|---|---|
| Backend protocol, ticket/account/owner/resume/slot | PASS module | Network142, DualTransport53, MatchResult74 và các suite liên quan; log giữ trong checkpoint |
| Mongo/API/UDP/WS/Auto thực | PASS module |68 checks; JWT thiếu/sai/hết hạn và Guest không lấy vé; concurrent result một201/ba200, conflict409, history sau restart |
| Authored map | PASS module | World168; counts, geometry/spawn, portal và đường tiếp cận container |
| Unity EditMode/PlayMode hiện hành | PASS module |854/854 và120/120 sau sửa ACK/audio; nhãn Ammo sửa nhỏ sau lượt này được kiểm riêng |
| Offline Windows | PASS bước chơi, FAIL process exit | Hai vòng, HP/bo, replay/menu, socket0; crash native khi thoát |
| Editor Game thật | PASS bước đã kiểm |215 placements + loot; phá rương, nhặt giáp/vũ khí, chọn/bỏ, bắn/nạp Shotgun4→3→4 (reserve4→3), phá3 chum và dùng GOD_LEAF1→0; tối đa5 voice audio thực. Đặt pose có kiểm soát; không thay nghiệm thu PvP |
| Hai Windows UDP+WS | PASS các assertion, FAIL flow/process | Có cùng view/bo/HP/result/history/replay; exit0xc0000005 |
| Sáu Windows main, lượt đầu | FAIL |5 client ghi đủ assertion/300s; một client thiếu evidence ở deadline. Có cửa sổ mất8 tick khi khởi động; không xóa bằng chứng lỗi |
| Sáu Windows main sau sửa driver HP | FAIL | UDP3 client ghi đủ assertion ở310–312s; WS3 client mất kết nối143–147s trước khi xác nhận history. Cả6 crash lúc thoát |
| AWS release v4 mới | BLOCKED | Chưa deploy do cổng Local chưa đạt; release chuẩn bị riêng |
| Hai PC LAN/Radmin/AWS | NOT_TESTED | Cần làm theo checklist thiết bị thật |

Runner cũ để nhân vật đứng yên sau3s nên người thắng có thể giữ HP100 mãi. Driver mới chủ động đi ra ngoài bo bằng input sau30s khi chưa quan sát damage, có kiểm collision; không hạ yêu cầu HP hoặc giả lập damage.

Lượt mới **local6-audio-ack-final** (3WSS+3Auto chọnUDP, khởi động cách1.500ms, main profile): cả6 ghi đủ8 assertion qua2 trận, kết nối387,9–395,2s; input29,983–29,997Hz, snapshot29,978–29,986Hz. RTTp95 WSS33,83–33,92ms / UDP33,93–34,43ms; tổng pong timeout0. Năm client không input catch-up drop, mộtUDP có1 lần; không ghi nhận disconnect ngoài dự kiến. Đây là loopback PC→Docker, không phải WAN Sydney.

**Chưa đạt nghiệm thu:** cả6 exit0xc0000005. Log server86 cửa sổ,78 có6peer: tick trung bình29,979Hz, min28,38/max30,20, CPU1core peak25,78%, working set peak125MiB; tổng8 dropped tick và1 tick overrun. Không loại cửa sổ lỗi khỏi kết luận. Container dev lượt này không đặt memory hard cap, nên không dùng nó thay bằng chứng256MiB/AWS. Phòng test đã dọn, cleanupErrors rỗng. Phát hiện panel debug đè kết quả; đang sửa để chỉ bật bằngF3. Source UI sửa sau lượt này chưa có trong binary của bằng chứng sáu client.

## 5. Vấn đề mạng và lỗi còn lại

**Lượt sau sửa ACK (01/11 20:29):** hai Windows player crash trong `mono-2.0-bdwgc.dll`, RVA `0x26bef6`, trước khi đủ sáu người. Bốn process còn lại được dừng có kiểm tra đúng exe/output thuộc lượt này; phòng test đã dọn. Đánh dấu INVALID cho hiệu năng, không dùng lượt này kết luận sửa WSS thành công. Đây là lỗi startup khác với Accessibility lúc thoát; chưa xác định nguyên nhân cuối.

**Audio thực:** đã nối sự kiện đánh/bắn/nạp đạn thật từ online/offline controller, tránh lặp prediction/confirmation. Bước chân và vào bụi lấy pose/địa hình thực; không phát khi idle/teleport/menu. Kiểm tập trung16/16 PASS, đầy đủ854/854 EditMode và120/120 PlayMode PASS; chưa thay cho kiểm nghe/flow PvP hai player thực. Job DeepSeek audio mới đã hủy sau25 lượt đọc, không tạo staged patch; phần này Codex sửa tại chỗ.

**WebSocket:** mất kết nối trong gameplay là lỗi độc lập với native quit. Log có `ConnectionLost(WSS send failed)`; resume sau đó reset LastReason/stats khiến evidence cũ ghi sai `None` và transport UDP. Đang giữ snapshot lỗi từ event và coalesce ACK; chưa kết luận nguyên nhân cuối hoặc ký ổn định5 phút. Một lượt đo không đủ tốc độ phát vẫn phải INVALID.

**Windows native quit:** Event1000 ghi0xc0000005 trong UnityPlayer.dll. Stack giải bằng symbol đúng bản Unity đi qua `PlatformAccessibilityManager::StaticDestroy` và runtime cleanup. Chờ scene chuyển xong và épD3D11 vẫn tái hiện; các workaround không hiệu quả không được giữ làm giải pháp. Dấu hiệu khớp lỗi Accessibility UUM-146676 được Unity sửa tại [6000.3.21f1](https://unity.com/releases/editor/whats-new/6000.3.21f1). Người dùng chọn giữ6000.3.15f1 và ghi BLOCKED; không đổi Editor, tắt accessibility hoặc bỏ kiểm exit.

Các bài backend và smoke movement không chứng minh toàn đường tranh loot/PvP/UI/audio của hai Unity player đã hoàn chỉnh. Những phần chưa có bằng chứng flow thực vẫn cần kiểm; không tự ký DONE.

## 6. Phương án và quyền vận hành

Giữ server authoritative,10/5/50 và account-ticket bắt buộc. Không deploy bản mới khi Local chưa đạt. AWS đã được kiểm read-only là Running/IP13.239.10.69; chưa upload/deploy, không Stop/Start hoặc cài watchdog tắt máy. Theo yêu cầu, EC2 giữ Running và tiếp tục phát sinh credits.

Ưu tiên kế tiếp: xác minh sửa WebSocket bằng player mới → hoàn tất các bước gameplay còn thiếu bằng chứng → cập nhật nghiệm thu. Native exit giữ BLOCKED theo quyết định phiên bản Unity; không cam kết AWS đã sẵn sàng.

## 7. Bằng chứng, tài liệu và dọn dẹp

Checkpoint hiện tại ở `.utmp/phase2-account-flow-20261101/run.json`; có log backend, kết quả Unity, build, hai/sáu player, ảnh và native stack. Báo cáo này là bản chung hiện hành; số liệu24–25/10 thuộc archive lịch sử có checksum, không phải source hiện tại. Gói evidence/JSON/CSV mới được chốt sau lượt sửa đang chạy.

Cập nhật hướng dẫn Phase1, Phase2, SSH/Swagger/Compass và Network Architecture theo v4. Graphify sẽ cập nhật sau khi code/config ổn định, kèm semantic fragment kiến trúc mới. Chỉ dọn temp/output xác định được; giữ source/.meta/.env, DB/outbox, release rollback và build28/10 để đối chiếu vì bản mới còn BLOCKED. Kế hoạch tạm giữ trong checkpoint khi còn việc chưa hoàn thành.

Review Antigravity bổ sung bị bộ duyệt tự động từ chối vì yêu cầu ủy quyền source egress tới Google; lượt đó chưa chạy. Codex tiếp tục review tại workspace. Các gói Antigravity/DeepSeek trước đó đã được review/sửa; không coi worker SUCCESS là nghiệm thu.
