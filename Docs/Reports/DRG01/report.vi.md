# DRG-01 — Rồng thả trứng, loot, art và sound

## 1. Kết luận và phạm vi

Cập nhật 07/11/2026 trên `vinhnq`, `D:/Unity/Lastaway`. **Code DRG-01 đã tích hợp; nghiệm thu Local PASS có giới hạn.** Offline, Docker Local và hai Windows player hiện hành đã chạy; chưa nghiệm thu LAN/VPN hai thiết bị hoặc AWS. Hậu kiểm cùng ngày đã sửa cả5 lỗi EditMode được nêu ở bản bàn giao đầu: **1136/1136 EditMode và150/150 PlayMode PASS**; xem mục6. Bằng chứng cũ được giữ để đối chiếu lịch sử. Chưa commit/push trong đợt này.

Luồng: vào trận → rồng bay/công bố điểm rơi → trứng đáp → đánh/bắn phá → loot dưới đất → F nhặt; bot tham gia theo FSM. Giữ camera Follow Player, spectator khi đã bị loại. Gameplay protocol **4**, account-ticket handshake vẫn **v4**; client/server phải cùng bản, bản AWS cũ chưa được cập nhật.

## 2. Những phần đã thực hiện

| Hạng mục | Kết quả và vị trí chính |
| --- | --- |
| Config/lịch | `Backend/Shared/GameData/airdrop_schedule.json`; bay 110/230/320s, đáp 120/240/330s; kích thước 2×2, HP150. Đưa vào RulesHash và đồng bộ StreamingAssets. |
| Logic dùng chung | `Backend/Shared/World/Airdrops/`: C# thuần, seed theo trận, state/revision riêng từng lượt, retry 1s tối đa 5s, bounded candidates. |
| Điểm đáp | Toàn footprint trong bo dự báo khi đáp, cách mép ít nhất3 cell; tránh geometry, hazard, portal, trứng/người; lấy tập điểm BFS đi được. Sửa lỗi ban đầu chỉ xét 48 điểm floor-loot khiến bỏ lỡ điểm hợp lệ. |
| Runtime trứng | Clone map theo trận; không sửa catalog/hash authored. Trên không không có collision/damage; đáp mới đăng ký Supply vào destructible/movement/combat. Dành trước3 ID và24 stack. |
| Damage/loot | Shared damage và transactional sink; không đủ capacity thì giữ trứng để retry. Supply luôn6–8 loại khác nhau, MACE+COMPASS đảm bảo; trọng số chọn không hoàn lại. Shotgun giữ đạn trong stack, không tạo thêm AMMO ngoài số stack. |
| Network/resync | `AirdropStatePacket` reliable có MatchId/DropId/tick/revision; UDP/WS dùng cùng dữ liệu. Baseline khôi phục state, không phát lại âm cũ hoặc sinh loot. |
| Bot | Ghép mục tiêu thính công bố vào FSM hiện có, giữ ưu tiên nguy hiểm/bo/combat và ngân sách pathfinding. Phá/nhặt qua combat và inventory thật. |
| Art/UI | Prefab và visual set Lastaway dùng animation rồng/sprite trứng hiện có; bay, bóng, rơi, đáp, HP khi bị đánh, effect vỡ. Marker minimap cùng phép chiếu player/bo, xóa khi vỡ/cancel/reset; không có nhãn F trên trứng. |
| Audio | DragonRoar toàn cục một lần/lượt; EggLand/EggBreak tại vị trí, có suy giảm; baseline im lặng. Kenney Impact soft/glass đã có trong thư viện, provenance/recipe cập nhật. |

Phát hiện thêm clip Throw/Victory có nội dung MP3 nhưng đuôi WAV; đã giải mã PCM16 mono44.1kHz, giữ GUID. Chuẩn hóa MeleeHit/EmptyClick theo recipe, giữ file nguồn. EmptyClick có ngoại lệ mức loudness được ghi rõ; không đổi luồng âm thắng/thua. Không nâng trạng thái license hoặc đánh dấu “đã nghe duyệt” chỉ vì test kỹ thuật PASS.

DeepSeek hoàn thành phần nền settings/runtime; sau các lỗi patch gói loot, người dùng cho phép Codex tiếp quản. Không còn worker đang ghi. Giữ nguyên thay đổi riêng `axe_held.png.meta` và `Map100x100.prefab`.

## 3. Kiểm thử module và Editor

| Nhóm | Kết quả | Bằng chứng trong evidence.zip |
| --- | --- | --- |
| Build backend | PASS, 0 warning/0 error ở lần kiểm solution | build log, danh mục source |
| AirdropChecks | **215/215** | `backend/final-AirdropChecks.log` |
| Inventory / FloorLoot | **1619/1619**, **203/203** | backend logs |
| World / Combat / Throwable | **174**, **27**, **85** PASS | backend logs |
| Match / MapScale / MapCatalog / Telemetry | **133 /29 /81 /71** PASS | backend logs |
| Network / DualTransport / GameplayNetwork / MatchResult | PASS | backend logs; đây là regression module, không thay LAN/WAN |
| WorldNetwork Mixed | Hai lượt cuối **37/37** | `final-WorldNetworkChecks-1.log`, `-2.log`; fixture ban đầu có race auto-pickup, đã sửa fixture chờ batch trước khi nhặt |
| Account/API/Mongo thật | **68/68** | `auth-smoke.log`; stack cách ly được runner dọn |
| Unity PlayMode | **150/150**; lượt DRG cuối **2/2** | `unity/playmode-all.json`, `playmode-drg-final.json` |
| Unity EditMode | **1136/1136 PASS** sau sửa5 lỗi và thêm2 regression | `editmode-repair/editmode-final.json`; `unity/editmode-delivery.json` là kết quả trước sửa |

Coverage mới: lịch/nhảy tick/idempotency/retry/cancel, footprint/bo/hazard, capacity không mất hoặc nhân đôi loot, map cache bất biến, ba lượt trên cả hai map; blast hụt/cover chắn/trúng/replay; UDP+WS cùng điểm đáp, HP/broken/loot/resync. Socket fixture chủ động gọi damage đã-confirmed để kiểm stream: không gọi đó là người chơi thật bắn trứng.

Editor đã chạy đủ lịch chính tới sau330s; fly/land quan sát đúng ba mốc. Fixture chỉ bảo vệ nhân vật khỏi damage bo để có thể quan sát đủ lịch, không đổi JSON mặc định. Riêng combat offline dùng `TryAttack` thật: **19 đòn tay không được chấp nhận**, trứng vỡ sau9,714s và rơi7 stack: MACE, COMPASS, UNICORN_BLOOD, ELIXIR_OF_LIFE, MAGIC_ARMOR, SHOTGUN, ELIXIR. Không đặt HP trứng về0 để báo PASS. Log âm ghi đúng3 roar,3 land,1 break của lượt phá đã thực hiện.

## 4. Windows và flow mạng Local

Windows **6000.3.15f1**, `Builds/Windows-DRG01-6000.3.15f1/Lastaway.exe`: build thành công,0 error,42 warning;222,16s,130,92MiB. Fingerprint source và player đã đối chiếu khớp; thông tin trong `report.json` và `build/`. Cả7 process của bốn lượt cuối thoát **exit0**. Đây không phải tuyên bố mọi lỗi engine15f1 cũ đã được sửa.

| Lượt | Kết quả |
| --- | --- |
| Tháp cổ,2 Windows player | PASS:1 UDP+1 WS, cùng quan sát đủ3 lượt đáp trong cùng trận, movement/bo; mỗi client nối khoảng16s. |
| Thị trấn,2 Windows player | PASS cùng tiêu chí trên map thứ hai. |
| Full flow2 player | PASS đăng nhập/tạo phòng/join, movement/HP/bo, kết quả/history/chơi lại; khoảng29s/client theo profile test. |
| Thị trấn,1 người+9 bot | PASS roster10; Auto cố ý thử UDP không listener rồi fallback WS, đủ3 lượt đáp; input29,49Hz, snapshot29,94Hz,0/16 pong timeout. |

RTT p50 các lượt khoảng33,3ms; p99 khoảng34,3–104,6ms (chi tiết từng client trong `report.json`). Đây là loopback cùng máy, không phải độ trễ Sydney. Enum transport ghi `Wss` nhưng endpoint thực **ws://127.0.0.1:7781/game**; nhãn “HTTPS” trong check runner là tên dùng chung, không chứng minh TLS trên Local. Không bypass TLS ở mạng từ xa.

Scenario tăng tốc chỉ xác minh các assertion đã liệt kê; không tự diễn giải thành hai người thật tranh loot/bắn trứng bằng tay. Dữ liệu hai process không thay bằng chứng hai PC. Lịch chính còn được kiểm riêng ở mục3.

## 5. Hiệu năng và giới hạn phép đo

Máy Ryzen7 4800H,8 core/16 thread, RAM15,4GiB, GTX1650 4GB. Runtime chạy real fixed-tick với human fixture, bot thật và vòng trận tự chơi lại; không có socket người giả và không giữ nhân vật bất tử suốt một trận. Không dùng kết quả này thay phép đo WAN hoặc continuous human PvP. Container Local không áp giới hạn256MiB của AWS.

| Bài | Thời gian | Tick Hz min–max | CPU max một core | RAM max | Dropped tick | Nodes path max/tick |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 người fixture+9 bot |300s|29,41–30,90|42,89%|65,31MiB|0|30 |
| 5 phòng/50 participant |60s|29,80–30,14|17,81%|67,27MiB|0|23 |

CSV gồm toàn bộ samples (297 và11). Ngân sách path1500node/tick không bị vượt. Không suy ra gameplay vô hạn hoặc AWS256MiB đã đạt. Windows1+9 là lượt tích hợp ngắn, không phải bài Windows300s;5 phút là phép đo runtime riêng.

## 6. Lỗi ngoài phạm vi và phần chưa nghiệm thu

- **ĐÃ SỬA5 lỗi ban đầu:** fixture map dùng LevelCatalog giống controller thật; Swamp có fixture riêng, overlap đặt đúng vào cover, decoration tính tăng so với dữ liệu hiện có và cover phá được được tính vào world objects.
- **Lỗi production đã sửa:** cửa import HP0/thiếu HP không bị tự đổi60; ưu tiên bounds game thay center đã làm tròn; metadata Editor giữ đủ float để lần lưu thứ hai không dịch collider0,0001 ô; giữ art/loot của cover phá được khi đọc map legacy. Hai regression mới kiểm hash qua2 lần lưu và bảo toàn art, không bỏ assertion hash.
- **ART04 đã sửa:** raw weapon sprites được kéo vào dependency HUD qua `GroundLootTuning → HeldWeaponCatalog`, không phải catalog icon chuẩn hóa. Bỏ reference serialized dư trên HUD; `PickupListView` dùng fallback Resources sẵn có nên giữ màu rarity và icon. Không xóa art gameplay hoặc nới test.
- **Kết quả hậu kiểm:** EditMode1136/1136 vàPlayMode150/150 PASS. Lượt PlayMode đầu bị MCP timeout khởi tạo; đã stop, khôi phục Boot và chạy lại thật, summary cuối150/150. Raw JSON nằm trong `editmode-repair/` của zip.
- **Windows:** bản build ở mục4 được kiểm trước lần sửa Editor/HUD này; chưa rebuild Windows sau hậu kiểm. Không tái sử dụng fingerprint cũ để ký nghiệm thu source mới; build lại trước khi phát hành/deploy. Không có thao tác AWS trong lần sửa.
- **NOT_TESTED:** LAN/VPN hai thiết bị, public tự host, AWS/TLS thật; không deploy hoặc gọi AWS trong đợt này.
- **NOT_TESTED:** đánh giá nghe/âm lượng bởi người thật và quyền phát hành của nguồn chưa xác minh. Playback/encoding/dedup PASS không thay các bước này.
- **NOT_TESTED:** hai người thật tranh từng trứng bằng tay, mọi tổ hợp súng/nổ trên cả hai map trong cùng trận; đã có module collision/combat/blast và offline melee thật, không ký thay kiểm PvP đó.
- Graphify đã cập nhật code/config và semantic kiến trúc; các warning parser UniTask/vendor vẫn được giữ trong log, graph không thay bằng chứng runtime.

Vì còn các mục trên, trạng thái bàn giao là **code hoàn thiện, Local kỹ thuật PASS trong phạm vi nêu rõ**, không tự sửa status Google Sheets hoặc ký nghiệm thu toàn dự án.

## 7. Bàn giao và chạy lại

1. Chơi ngay: mở Windows build trên, chọn **Luyện tập offline**. Camera Follow Player. Sống tới1:50 để thấy rồng,2:00 trứng đáp; đánh/bắn phá rồi F nhặt. Chơi đủ lịch cần tới sau5:30.
2. Local mạng: tại Backend chạy `docker compose --profile account up -d --build`; dùng source mới cả API/server/client, đăng nhập, tạo/vào phòng, chọn một trong hai map, thêm5 bot để đủ6 rồi Start. Không dùng AWS cũ với client protocol4.
3. Lệnh test backend/Windows, profile tăng tốc và cách trả về main: xem mục **Kiểm thử DRG-01** trong [hướng dẫn Phase2](../../Phase2_Match_Guest_Zone.vi.md). Ngoài profile test, countdown/luật chính giữ nguyên cấu hình hiện hành.
4. Sửa schedule/loot qua JSON chung rồi Sync Runtime Data/build cả hai bên. Art builder tại **Jungle Maze → Dragon → Build Airdrop Assets**. File audio/recipe/provenance được chỉ rõ trong hướng dẫn và manifest.
5. Bằng chứng: `report.json`, `measurements.csv`, `evidence.zip`, `evidence.zip.sha256`. Zip chỉ giữ kết quả/log/ảnh cần thiết, không đóng gói `.env`, token hoặc credentials. File tạm sau đó được dọn; giữ source test tái sử dụng và bản build cuối. Console còn một log cố ý của test pooling `DoubleDespawn_LogsError_DoesNotDuplicate` (có `LogAssert.Expect`), không phải lỗi gameplay mới.
6. Kết thúc đưa Editor về Boot, dừng Play; dừng3 service Docker Local vốn đã dừng trước lượt kiểm, giữ Mongo volume và dữ liệu. Không xóa image/volume hoặc thay đổi service AWS.
