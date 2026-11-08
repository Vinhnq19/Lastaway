# Đề xuất minimap, zoom và đánh dấu - Lastaway (T-017, cho unity T-018)

Trạng thái: **đề xuất, chờ Huy chọn phương án S/M/L và trả lời mục 11**. Chưa lan sang GDD, Sheet, code, T-018.
Viết lại 07/11/2026 khi mở lại T-017 (bản hoãn cùng ngày lưu ở scratchpad phiên designer; phần hiện trạng và luật địch giữ lại, cập nhật theo merge `43fa4759`).
Số liệu gameplay trong tài liệu là giả thuyết thử nghiệm. Ngày công là ước tính của designer, unity ước lại khi khóa contract, sai số khoảng ±50%.

Đầu vào nguyên văn Huy (`DECISIONS.md` 07/11):
- "Hiện địch khi vào vùng Hero thấy, thêm hiện vùng địa hình, hiện bo. Hiện đồng đội. Phóng to thu nhỏ"
- "Nghiên cứu thêm tính năng cho Minimap như phóng to thu nhỏ bằng phím M, thêm các tính năng đánh dấu các thứ như trong PUBG, cái này cần nghiên cứu kĩ"

## 0. Kết luận

- **Nghiêng về phương án M** (mục 10): minimap 2 mức zoom, bản đồ lớn nét lại, **1 waypoint cá nhân**, lớp địch theo tầm nhìn kèm dấu "thấy lần cuối". Không gửi gì lên server (0 byte mạng). Khoảng 4 ngày công.
- **Phím:** M giữ vai bật/tắt bản đồ lớn (thao tác dùng nhiều nhất, phải tức thì); đổi mức minimap bằng **N** hoặc cuộn chuột khi trỏ trên minimap. Cách khác theo đúng chữ "phím M": M nhấn ngắn đổi mức, M giữ mở bản đồ lớn (mục 4.3, câu hỏi 1).
- **Đánh dấu kiểu PUBG:** phần lớn là công cụ báo đồng đội (6 loại Tactical Marker, ping địch, ping loot). Lastaway là đấu đơn 10 người nên chỉ giữ hai thứ có giá trị cho một người: waypoint đích đến và dấu địch thấy lần cuối (tự động). Phần còn lại ghi riêng cho chế độ đội sau này (mục 8).
- Hai lỗi hiện có cần sửa dù chọn phương án nào: bản đồ lớn phóng ảnh 256 px lên 880 px (mờ gấp 3,4 lần), và click chuột trái trên bản đồ lớn vẫn bắn (mục 1).

## 1. Hiện trạng (grep repo 07/11, sau merge `43fa4759`)

| Thứ | Hiện có | File |
|---|---|---|
| Lỗ bản đồ minimap | 269 x 268 px ở 1080p (khung 318 x 316, viền 24) | `art/inbox/T-015/asset-list.md` dòng 33 |
| Nền minimap | Camera ẩn chụp cả map vào RenderTexture **256 px**, mỗi 1 s; ẩn người chơi. Không zoom, không ping | `Presentation/UI/Hud/MinimapMapCapture.cs:21-22` |
| Bo trên minimap | **Đã làm theo kiểu main** (Huy "Kiểu main" 07/11): tô vùng ngoài bo (alpha 0,62), vòng kế tiếp trắng nét đứt, shader `Resources/ZoneOverlay` | `Presentation/UI/ZoneOverlayView.cs` (Quang Huy), `Hud/Zone/HudZoneOverlay.cs` |
| Bản đồ lớn | Phím M bật/tắt, ESC đóng trước menu. Ô bản đồ 880 x 880 px (panel 1000, lề 60). Dùng **chung texture 256 px** của minimap. Có bo kiểu main, pin Hero, pha bo, đồng hồ bo, khoảng cách tới bo kế. Chưa đánh dấu, chưa zoom | `Hud/Panels/HudBigMapView.cs:29,79`; `Tools/Editor/Hud/HudPanelsBuild.cs:27,194` |
| Bản đồ lớn chặn bắn? | **Không.** Chỉ menu ESC chặn input gameplay; click trái trên bản đồ lớn vẫn tấn công | `Application/MatchUi/MatchMenuController.cs:47`, `Presentation/Combat/CombatWeaponPresenter.cs:207` |
| Cuộn chuột | Đang dùng: đổi dòng chọn trong danh sách nhặt đồ (F), đọc ở mọi vị trí con trỏ | `Presentation/World/Phase2/WorldSceneView.cs:248` |
| Vị trí người khác | Server gửi cùng một snapshot có cả 10 người cho mọi client. Lọc ở client chỉ là hiển thị, không chống cheat | `Backend/GameServer/Networking/SessionHub.cs:109-120` |
| Cờ người khác | Dead, Invulnerable, Attacking, Dashing. Không có cờ tàng hình, cờ trong bụi | `Backend/Shared/Protocol/Gameplay/PlayerStatePacket.cs:8-15` |
| Camera trận | Orthographic size 8: màn 16:9 thấy 28,4 x 16 cell | `Scenes/Game.unity:1236` |

Độ nét theo cell (map 50 cell):

| Nơi hiện | Cỡ ở 1080p | Khung thấy | px / cell cần | Texture 256 hiện tại cho | Thiếu |
|---|---|---|---|---|---|
| Minimap mức Toàn map | 269 px | 50 cell | 5,4 | 5,1 | Gần đủ |
| Minimap mức Gần | 269 px | 30 cell | 9,0 | 5,1 | Phóng 1,8 lần |
| Bản đồ lớn | 880 px | 50 cell | 17,6 | 5,1 | Phóng 3,4 lần |

Texture cần tối thiểu 17,6 x 50 = 880 px, nên đề xuất **1024 px** dùng chung cho minimap và bản đồ lớn. Unity đo chi phí chụp 1024 mỗi 1 s; nếu nặng thì tách địa hình tĩnh (chụp một lần) khỏi phần đổi trạng thái.

Phím đang dùng trong trận (grep `Input.GetKey*`, `GetMouseButton`, `mouseScrollDelta`, bỏ Level Editor):

| Phím | Việc | File |
|---|---|---|
| WASD, mũi tên | Di chuyển | `Controls/PlayerInputReader.cs:26` |
| Chuột trái | Tấn công | `PlayerInputReader.cs:36` |
| Cuộn chuột | Chọn dòng danh sách nhặt | `WorldSceneView.cs:248` |
| E | Tương tác (intent) | `PlayerInputReader.cs:37` |
| F | Nhặt, mở cửa | `WorldSceneView.cs:297,316` |
| R | Nạp đạn | `PlayerInputReader.cs:38`, `CombatWeaponPresenter.cs:150` |
| Space | Dash | `PlayerInputReader.cs:39` |
| Q, Backspace | Bỏ vũ khí (Q cũng là intent UseItem) | `WorldSceneView.cs:164`, `PlayerInputReader.cs:40` |
| 1-4, 5-8 (tới 0) | Chọn vũ khí, dùng đồ | `WorldSceneView.cs:155-156,215-220` |
| Tab, I | Túi đồ | `Hud/Panels/HudInventoryView.cs:42` |
| M | Bản đồ lớn | `HudBigMapView.cs:29` |
| ESC | Đóng lớp trên cùng, rồi menu trận | `MatchOverlay/MatchOverlayInput.cs:30` |
| F3-F10 | Debug | các `*DebugPanel.cs` |

**Còn trống:** N, chuột phải, chuột giữa, `=`, `-`, Z, X, C, V, G, T, B.

## 2. Benchmark (chi tiết và link: `de-xuat/_research-minimap-benchmark.md`)

Nguồn chính thức của PUBG, Apex, Fortnite phần lớn bị chặn khi tra (402/403/410); chỉ Suroi đọc được mã nguồn. Tách theo luật chống làm lại số 10:

**Sự thật có nguồn**

| Game | Sự thật | Nguồn |
|---|---|---|
| PUBG | Update 29 (2019): 6 Tactical Map Marker (Attack, Danger, Defend, Loot, Regroup, Vehicle); mở world map, giữ chuột phải ra wheel, thả vào icon | Dexerto patch 29 |
| PUBG | Mỗi người 1 marker tại một thời điểm; marker hiện trên world map, minimap, la bàn | Dexerto patch 29 (hiện hành chưa kiểm) |
| Fortnite | Chuột giữa 1 lần = ping vị trí, trắng; 2 lần hoặc ngắm rồi ping = ping địch, đỏ | Upcomer |
| Suroi | Minimap không zoom, tỉ lệ cố định theo cỡ cửa sổ; map lớn phím G hoặc M, vừa màn, không zoom, không kéo; N ẩn minimap | mã `mapManager.ts`, suroi.io/rules |
| Suroi | 4 ping người chơi (arrow, gift, heal, warning) + 2 ping hệ thống (airdrop, unlock); ping mới thay ping cũ cùng loại cùng người; màu theo người; ping hiện cả trong world | mã `mapPings.ts`, `mapManager.ts` |
| Suroi | Mobile: chạm minimap mở map lớn | mã `mapManager.ts` |

**Chưa xác minh** (không dùng làm căn cứ số)

| Thứ | Ghi chú |
|---|---|
| Phím zoom minimap PUBG | Không có nguồn phím `+`/`-`; guide nói có "Minimap Dynamic Zoom" tự zoom theo tốc độ, phím N mở rộng minimap ở bản 2018 |
| PUBG: M, cuộn, kéo trên map lớn; Alt + chuột phải đặt waypoint; Insert đánh dấu chỗ đứng | Chỉ thấy qua snippet tìm kiếm |
| Thời hạn ping | Apex ping thường 15 s, Fortnite ping item/địch 10 s: đều là guide bên thứ ba |
| Solo có dùng marker không | Không game nào có nguồn trực tiếp |
| Suroi thời hạn ping | Mã có hai số lệch nhau: định nghĩa 120 s, client hết hạn 10 s |

**Mẫu chung (>= 2 game):** mỗi người 1 dấu mỗi loại, đặt mới thay cũ (PUBG, Apex, Suroi); dấu hiện cả minimap lẫn map lớn (PUBG, Suroi); màu theo người đặt (PUBG, Suroi); map lớn phím riêng M (Suroi có nguồn, PUBG chưa); ping địch, ping item ngắn hạn 10-15 s (Apex, Fortnite, chưa xác minh).

**Kẽ hở:** chưa có số chắc cho thời hạn ping và phím zoom PUBG. Muốn chắc thì cần một người chơi thật PUBG và Apex 15 phút và quay lại; đề xuất này không dựa vào các số đó (Lastaway không có ping đội ở S, M).

## 3. Lọc cho Lastaway (đấu đơn 10 người, map 50 cell, trận khoảng 7 phút)

`targetMatchDurationSeconds` 420 (`Backend/Shared/GameData/match_settings.json:14`); bo về 0 ở giây 390 (`zone_schedule.json`).

| Mã | Dấu (tham chiếu) | Giá trị khi chơi một mình | Quyết định đề xuất |
|---|---|---|---|
| D1 | Waypoint đích đến (PUBG marker, Suroi arrow) | Có: lên đường xoay bo, nhớ hướng tới chỗ đã định khi đánh nhau giữa đường; màn chỉ thấy 16 cell dọc | **Làm (M)** |
| D2 | Dấu địch thấy lần cuối (tự động) | Có: địch ra khỏi tầm nhìn thì vẫn biết hướng nó đi | **Làm (M)**, gộp vào lớp địch |
| D3 | Ping địch thủ công (Apex, Fortnite đỏ) | Không: chỉ để báo người khác; D2 đã giữ vị trí cuối tự động | Bỏ. Chế độ đội: có |
| D4 | Ping loot, rương (PUBG Loot, Suroi gift) | Thấp: 60 rương trên map 50 cell, rương nhìn thấy ngay trên world | Bỏ. Chế độ đội: có |
| D5 | Wheel loại dấu (Attack, Danger, Defend, Regroup...) | Không: là ngôn ngữ ra lệnh cho đội | Bỏ. Chế độ đội: rút còn 4 loại |
| D6 | Đánh dấu chỗ mình đứng (Insert) | Không | Bỏ |
| D7 | Ping hệ thống tiếp tế (Suroi airdrop) | Có, nhưng thuộc DRG-01 (tự cấp icon minimap) | Không làm ở T-018; minimap chừa lớp icon hệ thống |
| D8 | La bàn | Lastaway không có la bàn | Không làm |

## 4. Zoom

### 4.1 Minimap: 2 mức

| Mức | Khung thấy | Tâm khung | px / cell (1080p) | Vòng nhìn 12 cell trên minimap (bán kính) |
|---|---|---|---|---|
| 1 Toàn map (mặc định) | 50 x 50 cell | Cố định giữa map (25, 25) | 5,4 | 64 px |
| 2 Gần | 30 x 30 cell | Theo Hero, kẹp trong biên map | 9,0 | 108 px |

- 30 cell = đường kính vòng nhìn 24 cell + lề 3 cell mỗi bên: mọi địch có thể "thấy" đều nằm trong khung. Màn chơi chỉ thấy 16 cell dọc, nên địch cách 8-12 cell theo chiều dọc chỉ thấy trên minimap.
- Kẹp biên: Hero ở (3, 47) mức 2 thì khung x 0-30, y 20-50; Hero hiện ở (3/30 x 269, 27/30 x 268) = (27, 241) px từ góc trái dưới, không ở giữa. Hero ở (25, 25): khung 10-40, Hero ở giữa.
- Đổi mức: nội suy tỉ lệ và tâm 0,15 s (không theo time scale). Mức chọn giữ tới hết trận; trận mới về mức 1.
- Bắc luôn ở trên, không xoay. Chấm, nét, icon có cỡ theo px màn hình, không theo cell.
- Không làm mức thứ 3 và không làm dynamic zoom kiểu PUBG: map 50 cell nhỏ, 2 mức đã phủ "cả trận" và "quanh tầm nhìn".

### 4.2 Bản đồ lớn

- Ở S và M: **không zoom, không kéo**, vừa ô 880 px như Suroi. 17,6 px / cell là đủ để click đúng một cell; map 50 cell không cần zoom như map 8 km của PUBG.
- Chỉ sửa: texture 1024 (mục 1) và chặn tấn công khi bản đồ lớn mở (M1 dưới đây).
- Ở L: 2 mức 50 và 25 cell, cuộn chuột zoom quanh con trỏ, giữ chuột phải kéo.

| Mã | Luật bản đồ lớn | Ví dụ |
|---|---|---|
| M1 | Khi mở: chặn tấn công, nạp, dash, dùng đồ (như menu ESC); vẫn cho di chuyển WASD | Mở map trong lúc bị đuổi vẫn chạy được, click đặt waypoint không bắn |
| M2 | Đang giữ chuột trái bắn mà mở map: ngừng bắn ngay frame mở | |
| M3 | Hiện cùng các lớp với minimap mức 1: địa hình, bo, Hero, địch đang thấy, dấu thấy lần cuối, waypoint | |

### 4.3 Phím

**K1 (nghiêng về):**

| Thao tác | Phím | Ghi chú |
|---|---|---|
| Bật/tắt bản đồ lớn | M (giữ như hiện tại) | ESC cũng đóng |
| Đổi mức minimap (1 <-> 2) | N | Phím cạnh M, đang trống |
| Đổi mức minimap | Cuộn lên = mức 2, cuộn xuống = mức 1 | **Chỉ khi con trỏ nằm trong khung minimap.** Khi đó danh sách nhặt không nhận cuộn ở frame đó; ngoài khung, cuộn vẫn chọn dòng nhặt như cũ |

Lý do: mở bản đồ lớn là thao tác dùng nhiều nhất và cần tức thì; đổi mức minimap thường chỉ một hai lần một trận. "Phóng to bằng M" vẫn đúng nghĩa: M phóng minimap thành bản đồ lớn rồi thu lại. Thể loại quen M là map lớn (Suroi có nguồn).

**K2 (đúng chữ "phím M"):**

| Thao tác | Phím |
|---|---|
| Đổi mức minimap | M nhấn ngắn: nhả trước 0,25 s |
| Mở bản đồ lớn | M giữ tới 0,25 s: mở và ở lại mở sau khi nhả |
| Đóng bản đồ lớn | M nhấn hoặc ESC |

Giá phải trả: mở bản đồ lớn chậm 0,25 s mỗi lần; đổi mức chỉ chạy lúc nhả phím.

- Mobile sau này (ghi trước, không làm): chạm minimap mở bản đồ lớn (Suroi), nút nhỏ cạnh minimap đổi mức, chạm trên bản đồ lớn đặt waypoint.
- UI-03 (cài đặt) chưa có đổi phím; khi có thì thêm M, N.

## 5. Luật từng dấu (ví dụ số)

### D1 Waypoint cá nhân

| Mã | Luật | Ví dụ số |
|---|---|---|
| W1 | Đặt: click chuột trái trên ô bản đồ lớn. Tọa độ = điểm click, kẹp trong [0, 50] hai trục; đặt được ở mọi chỗ, kể cả trong vật cản, lava, ngoài bo | Click ở (40,2; 35,7) thì waypoint ở (40,2; 35,7) |
| W2 | Tối đa **1**. Đặt mới thì thay cái cũ ngay, không hỏi | Đặt (40, 35) rồi đặt (12, 8): chỉ còn (12, 8) |
| W3 | Xóa tay: chuột phải trên ô bản đồ lớn (ở đâu cũng xóa), hoặc click trái trong 1,5 cell quanh waypoint đang có | Waypoint (40, 35), click (41, 35,5): 1,1 cell, xóa |
| W4 | Tự xóa khi Hero vào trong 1,5 cell quanh waypoint | Hero (10, 10), waypoint (40, 35): còn 39,1 cell. Hero tới (39, 34): 1,4 cell, xóa |
| W5 | Không hết hạn theo thời gian | |
| W6 | Xóa khi: Hero chết, hết trận, rời trận. Vào lại sau mất kết nối (`disconnectGrace` 15 s): giữ, vì chỉ nằm ở client | |
| W7 | Hiện trên bản đồ lớn: icon cờ 24 px + đường mảnh từ Hero tới cờ (1 px, alpha 0,5) | |
| W8 | Hiện trên minimap: icon cờ 14 px + đường mảnh. Ở mức 2 mà cờ ngoài khung: icon kẹp ở mép khung theo hướng tới cờ | Hero (25, 25) mức 2, cờ (48, 25): khung x 10-40, icon ở mép phải, giữa chiều dọc |
| W9 | Hiện trên world: cờ ngoài màn thì mũi tên ở mép màn chỉ hướng, kèm số khoảng cách cell làm tròn; cờ trong màn thì icon cờ trên mặt đất, không số | Cách 39,1 cell: mũi tên kèm "39" |
| W10 | Màu cờ: một màu riêng, không trùng đỏ địch, trắng vòng bo kế, Amber bản thân. art-ux chọn | |
| W11 | Mạng: **0 byte**, chỉ ở client | |

### D2 Lớp địch và dấu thấy lần cuối

Giữ luật bản trước, chỉ đổi nơi hiện (thêm bản đồ lớn):

| Mã | Luật | Ví dụ số |
|---|---|---|
| E1 | Địch hiện khi và chỉ khi interface visibility trả "thấy" cho cặp (mình, địch) ở frame đó. Minimap không có luật tầm nhìn riêng | |
| E2 | Bất biến khi FOW-01 đã nối: world đang vẽ địch X thì minimap và bản đồ lớn vẽ X, và ngược lại | Test so ba tập mỗi frame |
| E3 | Vị trí chấm = vị trí nội suy mà world dùng để vẽ sprite địch, cập nhật mỗi frame | |
| E4 | Mất thấy: chấm thành dấu thấy lần cuối (vòng rỗng cùng màu) đứng yên tại vị trí cuối, alpha 0,6 giảm về 0 trong 2,0 s rồi tắt | Địch ra khỏi 12 cell ở giây 10,0: dấu tắt ở giây 12,0 |
| E5 | Thấy lại khi đang có dấu: dấu tắt, chấm hiện ở vị trí mới | |
| E6 | Địch chết: chấm và dấu tắt ngay (killfeed lo) | |
| E7 | Chấm địch đỏ có viền tối 1 px: 12 px trên minimap, 16 px trên bản đồ lớn; không kim hướng; nhiều chấm chồng thì vẽ hết theo NetId | Tối đa 9 chấm |
| E8 | Không hiện địch ngoài tầm nhìn vì tiếng súng, tiếng chân, radar (MAP DESIGN STT 4 "chưa chốt") | |
| E9 | Mình chết: tắt lớp địch | |
| E10 | Mạng: 0 byte thêm (đọc snapshot đã có) | |

Policy tầm nhìn chờ FOW-01 (Quang Huy) trả: <= `visionRadius` 12 cell và không vật cản cứng giữa thì thấy (địch cách đúng 12,0 cell: thấy; 12,5 cell: không); trong bụi không thấy, bắn trong bụi thì lộ; tàng hình 5 s không thấy (Equipments STT 14); khói chưa chốt.

Khi FOW-01 chưa xong: adapter tạm trong làn UI, cùng interface `bool IsVisibleToLocal(byte targetNetId)`, bật bằng config `minimapVisibilitySource = "temp"`; thấy khi (1) khoảng cách <= `visionRadius` đọc config, (2) tia `CoverQuery` không trúng `HardCover`, (3) địch không lọt trong bounds bụi trừ khi cờ `Attacking` bật trong 1,0 s gần nhất. FOW-01 xong thì đổi `"fow"`, xóa adapter.

## 6. Địa hình

Giữ ảnh bản đồ thật đang chụp (Huy 06/11 "hiện bản đồ thật"), chỉ nâng texture lên 1024 (mục 1). Bỏ lớp ký hiệu vẽ từ dữ liệu của bản trước: với lỗ 269 px và texture 1024, bụi, vật cản, hazard đọc được ở mức 2 (9 px / cell). Soi ở 1080p trong T-018; mức nào khó đọc thì art-ux đề xuất viền nhấn cho hazard và portal.

## 7. Trường hợp biên

| Trường hợp | Hành vi |
|---|---|
| Nhấn N 5 lần trong 1 s | Vòng 1 -> 2 -> 1..., tween đang chạy thì chạy tiếp tới đích mới |
| Cuộn chuột ngoài khung minimap | Không đổi mức; danh sách nhặt nhận như cũ |
| Cuộn trong khung minimap khi có đồ trong tầm nhặt | Chỉ đổi mức, dòng chọn nhặt đứng yên |
| Mở bản đồ lớn khi đang bắn | Ngừng bắn (M2) |
| Mở bản đồ lớn khi menu ESC mở | Không mở; ESC đóng theo thứ tự lớp (`UiLayerStack`) |
| Click đặt waypoint ngoài ô bản đồ (vùng header) | Không làm gì |
| Waypoint trong vùng ngoài bo | Vẫn đặt; không cảnh báo |
| Hero đứng sẵn trong 1,5 cell lúc đặt | Đặt rồi xóa ngay frame sau (W4); chấp nhận |
| Lobby, chưa có map | Minimap chỉ khung; M không mở bản đồ lớn |
| Mình chết | Tắt waypoint, lớp địch, marker bản thân; giữ địa hình, bo |
| Mất kết nối | Xóa chấm và dấu địch ngay; waypoint giữ (W6) |

## 8. Chế độ đội sau này (ghi riêng, không làm ở S, M)

Chỉ khi Huy mở chế độ đội (đổi luật lõi, server, matchmaking: ngoài làn V.Huy, cần Vinh).

| Thứ | Đề xuất | Căn cứ |
|---|---|---|
| Đồng đội trên minimap | Luôn hiện bất kể tầm nhìn, chấm màu theo người, có kim hướng | Suroi màu theo người |
| Waypoint | Mỗi người 1 (W1-W4 giữ), màu theo người, đồng đội thấy | PUBG 1 marker mỗi người |
| Ping nhanh | Chuột giữa trong world hoặc trên bản đồ lớn: nhấn 1 lần = ping vị trí; nhấn đúp hoặc nhấn trúng địch đang thấy = ping địch (đỏ) | Fortnite, Apex |
| Wheel | Giữ chuột giữa: 4 loại (Đi tới, Nguy hiểm, Loot, Giữ chỗ) | Rút từ 6 loại PUBG |
| Số tối đa | 1 mỗi loại mỗi người, mới thay cũ | PUBG, Apex, Suroi |
| Thời hạn | Ping vị trí 10 s, ping địch 5 s, waypoint không hết hạn `[GIẢ ĐỊNH]`, đo lại khi playtest | Apex, Fortnite 10-15 s chưa xác minh |
| Chống spam | Tối đa 3 ping trong 3 s, quá thì khóa 2 s `[GIẢ ĐỊNH]` | Chưa có nguồn |
| Mạng | Client gửi server, server chuyển cho đồng đội. Gói ước khoảng 8 byte dữ liệu (loại 1, x y 2 x 2, NetId 1, số thứ tự 2) cộng header LiteNetLib; trần 1 gói/s mỗi người nhờ luật chống spam | Ước tính, chưa đo |

## 9. Mạng và chống lộ

| Lớp | Đọc từ | Gửi server | Lộ thông tin chưa cho phép |
|---|---|---|---|
| Địa hình | Capture map client | Không | Không |
| Bo | `ZoneStatePacket` | Không | Không, mọi client cùng nhận |
| Waypoint | Input local | Không, 0 byte | Không |
| Địch | Interface visibility + vị trí nội suy | Không | Server đang gửi vị trí cả 10 người; minimap **không** phải cơ chế chống cheat. Chống xem lén cần SRV-03 lọc snapshot |

## 10. Phương án

### S - Zoom và nét

| Ngày công | Phụ thuộc | Rủi ro chính |
|---|---|---|
| 1-1,5 | Không | Chụp 1024 mỗi 1 s nặng trên máy yếu |

Hạng mục: minimap 2 mức (4.1), phím theo K1 hoặc K2, cuộn trên minimap tách khỏi danh sách nhặt, texture 1024 cho minimap và bản đồ lớn, M1-M3 (chặn bắn khi mở map).
Nghiệm thu đo được:
1. Test hồi quy HUD hiện có (`Tests/EditMode/Hud/HudLogicTests.cs`, `Tests/PlayMode/HudPanelsTests.cs`) PASS không sửa assertion.
2. Ở 1080p: bản đồ lớn 1 cell >= 17 px không mờ; minimap mức 2 1 cell >= 9 px.
3. Click trái trên bản đồ lớn không tạo đòn tấn công nào (test 100 click).
4. Cuộn trong khung minimap không đổi dòng chọn nhặt; ngoài khung đổi như cũ.
Đánh đổi: giải "phóng to thu nhỏ"; chưa có đánh dấu, chưa hiện địch.

### M - Zoom, waypoint, địch (nghiêng về)

| Ngày công | Phụ thuộc | Rủi ro chính |
|---|---|---|
| 4-5 (S + 1 waypoint + 2 lớp địch) | FOW-01 cho lớp địch, có adapter tạm | Adapter tạm lệch với world khi FOW-01 chưa nối; tàng hình, khói vẫn lộ trên minimap như đang lộ trên world |

Thêm so với S: D1 waypoint (W1-W11) trên bản đồ lớn, minimap, world; D2 lớp địch (E1-E10) trên minimap và bản đồ lớn, adapter tạm có công tắc.
Nghiệm thu thêm:
1. Waypoint: đủ ca W2, W3, W4 bằng ví dụ số mục 5 (EditMode); mũi tên mép màn chỉ đúng hướng sai <= 5 độ.
2. Không chấm địch nào mà visibility trả "không thấy" (quét 300 frame); dấu thấy lần cuối tắt đúng 2,0 s ± 1 frame.
3. Chấm địch lệch sprite world <= 1 px minimap ở cả hai mức.
4. Không gói mạng mới (đếm loại gói trước và sau bằng `NetworkStatsOverlay`).
Đánh đổi: giải "phóng to thu nhỏ", "đánh dấu" (phần có nghĩa cho đấu đơn), "hiện địch khi vào vùng Hero thấy". Không giải "hiện đồng đội", ping kiểu PUBG cho đội.

### L - Chế độ đội đầy đủ

| Ngày công | Phụ thuộc | Rủi ro chính |
|---|---|---|
| 10+ phía client, chưa tính server, matchmaking | Huy chốt chế độ đội; Vinh (server relay ping, đội trong snapshot); FOW-01 | Nhiều biến chưa kiểm cùng lúc: luật đội, cân bằng 10 người chia đội, mạng |

Thêm so với M: mục 8 (đồng đội, ping nhanh, wheel 4 loại, chống spam), zoom 2 mức và kéo trên bản đồ lớn.
Đánh đổi: giải đủ lời Huy kể cả "hiện đồng đội", nhưng là đổi luật lõi, không nằm trong làn V.Huy.

### So sánh nhanh

| | S | M | L |
|---|---|---|---|
| Ngày công | 1-1,5 | 4-5 | 10+ (chỉ client) |
| Lời Huy giải được | Phóng to thu nhỏ | + đánh dấu (đấu đơn), hiện địch | + đồng đội, ping đội |
| Gói mạng mới | 0 | 0 | Có (server) |
| Người ngoài làn V.Huy | Không | Báo Quang Huy interface FOW | Vinh, Quang Huy, Hào |
| Rủi ro chính | Hiệu năng chụp 1024 | Adapter tạm lệch world | Đổi luật lõi |

### Chặng và cổng Huy chơi (M)

| Chặng | Hạng mục | Cổng Huy chơi | Không qua thì |
|---|---|---|---|
| 1 | S | Chơi 1 trận: đổi mức minimap bằng phím đã chọn, mở bản đồ lớn khi đang bị bắn. Câu hỏi: mức Gần có đáng giữ không | Dừng ở S |
| 2 | Waypoint + lớp địch | 1 trận với 2-3 người: đặt waypoint lúc xoay bo; đuổi một địch ra khỏi tầm. Câu hỏi: 2,0 s dấu thấy lần cuối có đủ, có thừa | Chỉnh số trong config, không thêm loại dấu |

## 11. Câu hỏi cho Huy

1. **Phím:** K1 (M bản đồ lớn như cũ, N và cuộn trên minimap đổi mức) hay K2 (M nhấn ngắn đổi mức, M giữ 0,25 s mở bản đồ lớn)? Nghiêng về K1: bản đồ lớn dùng nhiều hơn nên cần tức thì, và thể loại quen M là map lớn.
2. **Đánh dấu cho đấu đơn:** chỉ 1 waypoint + dấu địch thấy lần cuối tự động, bỏ ping và wheel kiểu PUBG (vốn để báo đồng đội)? Nghiêng về: đồng ý. Muốn nhiều dấu cá nhân thì đề xuất tối đa 3 waypoint, đánh số 1-3.
3. **Đồng đội:** GDD là đấu đơn 10 người. "Hiện đồng đội" là muốn mở chế độ đội (phương án L, đổi luật lõi, cần Vinh) hay bỏ? Nghiêng về: bỏ khỏi đợt này, giữ mục 8 làm hồ sơ khi mở chế độ đội.

## 12. Nơi phải đổi cùng khi Huy chốt

| Nơi | Ai |
|---|---|
| `so-hien-hanh.md`: dòng "Minimap chức năng" (mức zoom, phím, số dấu, 2,0 s) | CPO |
| `ux/hud-B-spec.md` mục 5; icon cờ waypoint, mũi tên mép màn, chấm địch, dấu thấy lần cuối; mock minimap 2 mức và bản đồ lớn có waypoint (gen qua art-bridge) | art-ux |
| `MinimapMapCapture.cs` (texture), `MinimapProjection.cs` (khung thấy, kẹp biên), `MinimapView.cs`, `HudBigMapView.cs` (chặn input, waypoint, lớp địch), `WorldSceneView.cs:248` (nhường cuộn khi trỏ trên minimap), config, test | unity (T-018) |
| FOW-01: interface `IsVisibleToLocal` | CPO báo Quang Huy |
| GDD PROJECT TRACKING: dòng UI-04 (mục 13) | Huy dán hoặc CPO |
| Test case minimap, bản đồ lớn | qa |

## 13. Dòng ticket (theo phương án M, cột PROJECT TRACKING, tab phân tách)

```
UI-04	Giai đoạn 3	07/11/2026	UI/UX	Minimap: 2 mức zoom, waypoint cá nhân, địch theo tầm nhìn	Việc cần làm: - Minimap 2 mức: toàn map 50 cell và gần 30 cell theo Hero, kẹp biên; đổi bằng phím (K1: N, K2: M nhấn ngắn) và cuộn chuột khi trỏ trên minimap. - Bản đồ lớn (M): texture 1024, chặn tấn công khi mở, vẫn cho di chuyển. - 1 waypoint cá nhân: click trái trên bản đồ lớn đặt, chuột phải xóa, tự xóa khi Hero vào 1,5 cell; hiện ở bản đồ lớn, minimap, mũi tên mép màn kèm khoảng cách. - Hiện địch khi và chỉ khi visibility FOW-01 trả thấy; mất thấy để dấu mờ 2 s. FOW-01 chưa xong thì dùng adapter tạm có công tắc config. Ranh giới: UI-04 chỉ vẽ và không gửi gói mạng; FOW-01 (Quang Huy) sở hữu luật tầm nhìn, ZON-01 dữ liệu bo, SRV-03 chống xem lén, DRG-01 icon tiếp tế. Không có đồng đội (đấu đơn).	Hoàn thành khi: 1. Ở 1080p bản đồ lớn 1 cell >= 17 px, minimap mức gần 1 cell >= 9 px; click trên bản đồ lớn không tấn công. 2. Waypoint đúng luật thay, xóa, tự xóa; không chấm địch nào khi visibility trả không thấy, dấu mờ tắt sau 2 s. 3. Không thêm gói mạng; đổi adapter tạm sang FOW-01 chỉ đổi config.	UI-01, ZON-01, FOW-01 (lớp địch, có phương án tạm)	P1	Not Yet	V.Huy	MATCH DESIGN DATA (mapSize, visionRadius); MAP DESIGN!4,11; Obstacles!9; Equipments!14
```

## 14. Giả định

- Mọi số ở mục 4, 5 (30 cell, 0,15 s, 0,25 s, 1,5 cell, 2,0 s, cỡ icon) do designer đặt, chưa ai duyệt; đều để trong config.
- Ước tính ngày công là của designer, unity ước lại.
- Benchmark PUBG, Apex, Fortnite chủ yếu từ nguồn thứ ba; không số nào trong S, M dựa vào phần chưa xác minh.
- `MATCH DESIGN DATA` đang "Baseline cũ - chưa duyệt"; minimap đọc config nên đổi số không sửa code.
- Mock cần art-ux sau khi Huy chọn: minimap 2 mức, bản đồ lớn có waypoint và đường kẻ, mũi tên mép màn.
