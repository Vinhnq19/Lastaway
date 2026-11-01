# Audit Phase 2 — 01/11/2026

## 1. Kết luận và phạm vi

Đối chiếu 22 task Phase2, bỏ qua hoàn toàn cột Status. **Chưa đủ căn cứ ký hoàn tất toàn bộ Phase2 hoặc xem đây là một flow gameplay hoàn chỉnh.** Nhiều module đã hoạt động và pass hồi quy Local; lỗi tích hợp thật vẫn tồn tại. Không đồng nhất “chưa nghiệm thu đủ” với “chưa viết code”.

Nguồn: Google Drive connector đọc trực tiếp spreadsheet `17z41xmXWXdJhTJcziRoNuVw0jJQlEI8PS13_g3kYWUo`, tab PROJECT TRACKING, 22 dòng Phase2. Snapshot scope/DoD trong evidence.zip. Không sửa spreadsheet. Bản browser phụ bị chặn `browser connection timed out while waiting for approval` (Chrome remote debugging); kết quả fallback của worker có nội dung mâu thuẫn snapshot connector nên **không dùng làm tiêu chí audit**.

Dùng graphify đã có để định vị MatchRuntime, WorldClientSession, LobbyFlow, MatchSession, RoomManager, SessionHub; chỉ đọc source mục tiêu để xác minh. Không dựng lại graph vì không sửa production code. Các liên kết INFERRED của graph không được coi là kết quả kiểm thử.

Chạy Local, Unity 6000.3.15f1. Không Start/deploy/load test AWS. Docker daemon không chạy; không bật Docker hoặc truy cập Mongo thật. WS trong fixture là loopback không TLS, không phải chứng nhận HTTPS/WSS public.

## 2. Đã kiểm thử thực tế

| Bộ kiểm | Kết quả mới |
|---|---:|
| Unity EditMode | 823/823 PASS, 0 skipped |
| Unity PlayMode | 120/120 PASS, 0 skipped |
| MatchChecks | 133 PASS |
| MatchResultChecks | 53 PASS |
| CombatChecks | 27 PASS |
| InventoryChecks | 83 PASS |
| WorldChecks | 73 PASS |
| NetworkChecks | 132 PASS |
| DualTransportChecks | 53 PASS |
| GameplayNetworkChecks | Udp 16; Wss 16; Mixed 16 PASS |
| WorldNetworkChecks | Udp 22; Wss 22; Mixed 22 PASS |

EditMode job `d8c1d9e1e90d4ea8b5061cb0e2442bbf`; PlayMode job `53d7b53230a64fcaaf9384db2a36cfaf`. Không sửa assertion để đạt test.

Fixture gameplay mở socket thật và 10 phiên, kiểm slot thứ11, snapshot/zone, combat xác nhận, resume, result/replay. Fixture world dùng **WORLD_LAYOUT_MODE=Prototype**, hai client .NET, không phải hai Unity Player. Các setup damage/position trong fixture là dữ liệu kiểm thử authoritative, không chứng minh vũ khí trong map thật đã đi trọn flow. Các tên Wss của fixture không biến kết nối `ws://127.0.0.1` thành TLS WAN.

Editor thật: chạy Boot, gọi OpenJoin chưa login: `before=Login, after=Login, DevSkip=true`. Vào Local bằng presenter thật: scene=Game, OfflineMatchSession active, HP100, clock tăng; `GameFlow=InMatch` nhưng `LobbyFlow=Login`, Lobby Canvas vẫn active và che gameplay. Tạm ẩn riêng LobbyUiRoot trong PlayMode để quan sát: Hero, bo, HUD hiển thị; nền và bốn cover vẫn graybox. Đây là **bypass chẩn đoán**, không đánh dấu flow người dùng PASS.

Đặt vị trí ngoài bo bằng hook kiểm thử runtime (không sửa HP trực tiếp), chờ game tick: HP=0, HasResult=true. Gọi StartRound: MatchId mới, HP=100. Chưa kiểm nút replay người dùng end-to-end vì lớp UI lệch trạng thái. Console sau lượt game: 0 error. Kết thúc: Stop PlayMode, trở về Boot. Khôi phục duy nhất EditorSettings do Test Runner đổi; giữ nguyên ProBuilder Settings và các thay đổi có sẵn.

## 3. Đánh giá từng task

“Đạt module Local” nghĩa phần chức năng độc lập đã có bằng chứng; **không** có nghĩa mọi DoD tích hợp/WAN của task đã hoàn tất.

| ID | Đánh giá độc lập status | Còn thiếu / cần xử lý |
|---|---|---|
| SRV-03 | Đạt lõi runtime + socket Local | Cần nghiệm thu hai Unity client hiện tại và 6–10 người; flow UI đang chặn Guest; nội dung authored map chưa đủ. |
| NET-02 | Đạt kiểm thử result/Guest/outbox Local | Chưa chạy Mongo/API thật và khôi phục lịch sử sau restart trong lượt này; UI Guest đang chặn quyền chơi dù backend không bắt JWT join. |
| ZON-01 | Đạt lịch 5 phase/clock/HP theo config code | Cần đồng bộ luật: code r25, 5HP/s cố định; OVERVIEW bản GDD hiện nêu r36 phủ map/damage tăng phase. Luật cũ từng được chốt trong kế hoạch, không tự thay bằng giả định mới. |
| MOV-02 | Đạt module prediction + impairment giả, UDP/WS sockets | Chưa đo hai Unity client bản mới ở RTT50/100, jitter20, loss1/3%; không dùng kết quả fake để ghi WAN PASS. |
| MOV-03 | Đạt module interpolation/packet ordering | Cần đối chiếu hình ảnh/jitter hai Unity client thật trên UDP và WSS, đổi phòng không ghost. |
| WPN-01 | Đạt core melee/ranged/ammo/reload + broadcast socket | Chưa nghiệm thu hai Unity view nhìn cùng attack/reload trong map thật; map không có nguồn loot mặc định để thử vòng lấy súng. Không yêu cầu task này tự làm vũ khí ném/đặc biệt ngoài scope. |
| WPN-02 | Đạt 27 combat checks và fixture packet thật | Cần hai Unity client kiểm HP/ammo/death có impairment và world production; không suy ra combat feel từ loopback. |
| ENV-02 | Có service/destruction/loot và fixture Prototype PASS | Authored map không có rương/chum; chưa có vòng phá→rơi→nhặt trong scene chơi mặc định. |
| ENV-03 | Có hazard/portal/cooldown/dedupe kiểm thử | Authored map không có hazard/portal/cửa; chưa nghiệm thu tương tác hai Unity client trên map thực. |
| Game-10 | Chưa hoàn chỉnh | Có Slow/Poison/Stun/Speed/Invisible/Immune; server world chưa đưa Root/Reverse vào điều khiển/packet như yêu cầu đầy đủ; cần policy stack/cap/immunity thống nhất và test tích hợp. |
| UI-02 | FAIL flow tích hợp | OpenJoin không token quay Login; bypass chỉ Editor/dev. Runtime offline InMatch nhưng Login Canvas che Game. Cần tách quyền tạo/quản lý phòng khỏi Guest join, thống nhất một nguồn phase/screen. |
| DRG-01 | Chưa triển khai đủ | Chưa tìm thấy scheduler authoritative 3 mốc, trứng và loot6–8 theo task; DragonRoar và thông số config đơn lẻ không chứng minh sự kiện rồng đã chạy. |
| AUD-01 | Đạt module playback/mixer/dedupe Local | Cần nghe xác nhận cues ưu tiên trong trận hoàn chỉnh; rồng không có event live để nghiệm thu toàn flow. Không coi mock audio là rồng thật. |
| UI-03 | Đạt module ESC/settings/spectator/replay fake + quyền replay backend | Cần kiểm replay/spectator/result thật hai Unity client và sửa lỗi phối hợp với UI-02; không chỉ gọi StartRound bằng API test. |
| MAP-02 | Chưa đủ map tích hợp | map_demo.json có4 cover,10 spawn, không worldObjects; chưa đạt60 rương/90 chum/45 bụi/3 cặp portal cùng hotspot/đường loot. Prototype algorithm PASS không thay authored layout. |
| ITM-01 | Đạt service83 checks + world network fixture | Cần nguồn loot và UI tương tác live trên authored map, test hai Unity nhặt tranh/dùng/đổi/bỏ/death-drop; không kết luận từ inventory fake. |
| ART-01 | Có Hero/sprite và tests; thấy Hero trong Editor | Chưa kiểm đủ local/remote Idle/Move/Aim/Attack/UseItem/Die trong vòng chơi hiện tại; không xác nhận remote animation từ screenshot một Hero. |
| ART-02 | Chưa ghép đầy đủ vào Game mặc định | Screenshot thực vẫn bốn cover dạng khối graybox và nền đơn sắc; cần gắn bộ hình đã chọn vào scene production, giữ collider. |
| ART-03 | Có visual catalog/demo và tests | Vật tương tác không có trong authored map; cần nối event thật vỡ/portal/hazard/rồng vào bản chơi. |
| ART-04 | Đạt mapping/UI module theo tests | Cần nghiệm thu icon item sàn và inventory thật khi map có loot; các ký xác nhận/readability thủ công cũ không tự được coi đã hoàn tất. |
| ART-05 | Đạt catalog/import/playback tự động | License một số nguồn JSurviv/clip rồng vẫn ghi chưa kiểm chứng trong tài liệu; cần nghe/duyệt cues còn lại. Yêu cầu task là ghi trung thực, không tự coi mọi license thiếu là lỗi C#. |
| ART-06 | Chưa đủ tiêu chí nghiệm thu | Tracking chỉ ghi “Tạo hệ thống vẽ map”, DoD trống. Có tool Build World Demo/Build Visual Assets nhưng chưa đủ chứng minh editor vẽ/xóa/lưu/load/export/undo đáp ứng mong muốn. Cần định nghĩa DoD trước khi ký. |

Không gán % tiến độ vì các DoD có trọng số khác nhau và còn nghiệm thu thiết bị/nghe/nhìn. Không ký bất kỳ task nào “hoàn chỉnh toàn bộ DoD” chỉ từ lượt Local một Editor này. Các module đã PASS có thể được giữ làm nền, tập trung sửa điểm tích hợp thay vì viết lại.

## 4. Lỗi / khoảng trống cần ưu tiên

1. **P1 UI quyền Guest + offline:** `Application/Lobby/LobbyFlow.cs` NeedsLogin/OpenJoin và `Bootstrap/Lobby/Bootstrap.Lobby.cs` chỉ bật DevSkipLogin ở build dev. Backend RoomCode join vẫn đúng. `LobbyUiPresenter` chỉ tắt Canvas khi LobbyScreen.InGame, trong khi offline GameFlow đã InMatch nhưng LobbyFlow đứng Login. Tái hiện bằng Boot→StartOffline; bằng chứng `offline.png` và `game-bypass-ui.png`.
2. **P1 authored map thiếu nội dung:** `Backend/Shared/GameData/world_settings.json` chọn Authored; `map_demo.json` không worldObjects. `AuthoredMapDocument` mặc định mảng rỗng; pipeline không tự lấp60/90/... từ config. Test `NormalWorldUsesAuthoredTeamMapWithoutProceduralObjects` chủ động xác nhận hành vi hiện tại. Không tự chuyển sang Prototype rồi tuyên bố đã xong map thiết kế.
3. **P1 gameplay chưa đủ:** DRG scheduler/trứng/loot chưa có; Root/Reverse chưa đủ state/movement sync trong `MatchWorldRuntime`/InventoryStatePacket. Hiện Cleanse có nhắc Root/Reverse không đồng nghĩa chúng được áp dụng.
4. **P2 luật GDD/code lệch:** bo25 vs36, damage cố định vs tăng phase, duration420 mục tiêu vs giờ cưỡng bức trong OVERVIEW. Cần bảng luật/version thống nhất; không sửa các số rời rạc ở client và server.
5. **P2 assets và flow production:** Hero có hình nhưng map Game vẫn graybox; các demo riêng không tự nối thành sản phẩm. Hệ art/audio cần được nghiệm thu trong cùng Game scene.

## 5. Đánh giá tiến trình mạng

**Đã xác nhận Local:** UDP và WebSocket nhận/join/slot/timeout; packet malformed/duplicate; phòng và session; gameplay snapshots/zone; attack broadcast/reject; resume/result/replay; world/item state trên fixture. Không thấy lỗi làm hỏng transport trong các bài này.

**Cần sửa ở luồng phía client trước:** gate login chặn Guest; thống nhất LobbyFlow/GameFlow/OfflineMatchSession; dùng một authored layout/hash/data version đủ nội dung trên server và client. Các lỗi này khiến người dùng tưởng “không vào mạng/game được” dù transport vẫn PASS.

**Chưa kiểm trong lượt này:** HTTPS certificate/WSS qua Caddy public, mạng chặnUDP thật, DNS DuckDNS, hai PC LAN/Radmin/VPN/AWS, sleep/reconnect WAN, gameplay 6–10 Unity client bản mới, RTT/correction WAN, Mongo account/history restart và outbox API recovery thật. Docker không chạy; fixture result53 dùng fake/in-memory không thay bài DB. Không áp số liệu AWS từ build28/10 vào source01/11.

## 6. Thứ tự xử lý đề nghị

- Chốt quyền: JWT bắt buộc tạo/list/xóa phòng; Guest RoomCode chơi được cả release và khi API lỗi. Sửa đồng thời offline/menu/result navigation.
- Hoàn thiện authored map: export collider + spawn + worldObjects, version/hash; đưa art cover/terrain và vật tương tác vào cùng scene Game.
- Hoàn thiện Game-10 và DRG-01 đúng contract/timer/dedupe; chốt luật bo trong một config chuẩn.
- Chạy lại các suite đã có, sau đó hai Unity client current build: loot→attack→HP/effect→elimination→result→replay; kiểm UDP và Auto fallback. Cuối cùng hai PC thật rồi AWS theo cửa sổ đã duyệt.

## 7. Bàn giao / giới hạn

Evidence: `Phase2_Audit_2026-11-01/evidence.zip`, SHA256 sidecar; chứa snapshot DoD connector, log tests, Unity summaries và ảnh Game. Browser fallback khác connector bị loại khỏi evidence. Không đưa credentials/.env vào archive. Các thao tác vị trí/hide UI/StartRound là hook chẩn đoán trong PlayMode, không lưu prefab/scene hoặc sửa production logic. Không cập nhật Tracking/status, không commit/push và không tiêu credits AWS.
