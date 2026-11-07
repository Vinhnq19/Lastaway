# T-022 - Phản hồi HUD, túi đồ, bản đồ lớn, kill feed, thang chữ HUD sang skin A (unity, 07/11)

Bậc: **VERIFIED trong Editor** (trận Luyện tập offline, Game view 1920x1080; giáp nhặt thật qua loot, máu hạ còn 22). Chưa PLAYTESTED.

## Đã làm
| Phần | Nội dung |
|---|---|
| Phản hồi HUD (B9) | `HudFeedbackPrefabBuild.cs` viết lại bằng kit/fx: hit marker, cung sát thương (giữ đỏ), mũi tên nguy hiểm, chip hiệu ứng (2 hàng x 3), toast/danh sách nhặt (icon 56/72 kèm tên), "Đang dùng", cảnh báo, vignette máu thấp |
| Thang chữ HUD | máu 30, giáp/mũ/adrenaline 30, "1/1" và đồng hồ bo 24, số phím và số trên ô 20 (nhãn `kit_badge_key`), font Be Vietnam Pro Bold; rect panel r11 giữ nguyên |
| Túi đồ (B4) | view + prefab mới `HudInventory.prefab`, Tab/I mở, chỉ hiển thị (không thao tác vứt), `icon_ammo`, `icon_ground` |
| Bản đồ lớn (B5) | `HudBigMap.prefab`, M mở; `kit_map_bg`, `map_grid` phủ ảnh bản đồ thật, vòng bo `map_ring_zone` và vòng an toàn `map_ring_safe` theo bán kính thật |
| Kill feed | `HudKillFeedView` + `kit_killfeed_panel`, tối đa 3 dòng, tự mất 5 s; **chưa nối dữ liệu** (client chưa có nguồn hạ gục cho mọi người chơi), API `Push(killer, icon, victim)`; ảnh dùng dữ liệu đẩy tay |
| ESC một lớp | túi/bản đồ đăng ký `UiLayerStack`; ESC đóng bản đồ, rồi túi, rồi mới mở menu (ảnh `r2/esc-1-dong-ban-do.png`, `esc-2-dong-tui.png`) |

## Vignette máu thấp (CPO yêu cầu 2 mức)
`so-sanh-vignette.png`: trái 1 lớp `fx_vignette` (cũ), phải 2 lớp (đề xuất, đang dựng sẵn trong prefab), HP 22/100 trên nền cỏ sáng; giữa màn vẫn trong, không che nhân vật. Đổi về 1 lớp: `HudFeedbackPrefabBuild.VignetteLayers = 1` rồi chạy "Build Feedback Layer".

## Ảnh
| File | Màn |
|---|---|
| `so-sanh-hud-tui-ban-do.png` | B9 máu thấp, B4 túi đồ, B5 bản đồ lớn |
| `r2/*.png` | ảnh gốc 1920x1080 |

## Test
Cùng lượt với T-024 (EditMode 962/968, PlayMode 160/161, đỏ có sẵn). Thêm: `Tests/PlayMode/HudPanelsTests.cs` (12), `Tests/EditMode/Hud/HudPanelMathTests.cs`, `KillFeedLogTests.cs` (3). Sửa assertion hình/cỡ: `HudFeedbackAcceptanceTests` (6), `HudFigmaV2Feedback`, `HudAcceptanceTests.Text_MinSize13`.

## Lệch / chưa làm
- Chưa có toast "F Nhặt" và cột đồ dưới đất trong túi (chưa có state đồ ở gần); pha, khoảng cách tới bo, điều khiển chuột của bản đồ lớn tắt (tính năng chưa có).
- Chip 2 hàng x 3, cung nhỏ hơn mock 10%, mũi tên 70% để giữ luật không lấn vùng giữa 8 u.
- Click chuột khi túi mở vẫn vào game (chưa chặn input gameplay; vùng input Hero).

## Vòng R3 (làn B, 07/11, WRITTEN - chưa dựng lại prefab, chưa chụp)

| Lỗi QA | Sửa |
|---|---|
| T22-1 vòng bo bản đồ lớn 12 px, răng cưa | Vòng vẽ bằng `RingGraphic` (lưới vành khuyên, nét cố định, lấy màu từ dải vành của sprite `map_ring_zone`, `map_ring_safe`): nét 4 px ở mọi bán kính (đo mock b5: vòng cam và vòng trắng đều 4 px) |
| T22-3 minimap đỏ + vàng | Minimap dùng cùng `HudZoneRings` + `RingGraphic` + sprite: cam = bo hiện tại, trắng = an toàn kế tiếp, nét 2 px (đo `T-015/dung-thu-hud-A.png`). Đường đỏ/vàng chụp từ world vẫn tách khỏi ảnh địa hình nhưng không hiện. Đường vòng bo vẽ trên world giữ nguyên |
| T22-2 chú giải, thẻ trên | Đủ 4 hàng điều khiển như mock. Thẻ trên đủ 3 mục |
| T22-4 toast nhặt | Khung `kit_toast` 308x92, icon 72, tên tiếng Việt theo GDD ITEM LIST (`HudItemNames`, 35 mục) |

### Nghĩa và nguồn dữ liệu thẻ trên bản đồ lớn
| Mục mock | Nghĩa (nguồn) | Dữ liệu |
|---|---|---|
| cảnh báo + "3" | Pha bo ("Pha 3 · Bo thu sau 01:24" ở bản mock trước luật chữ, `T-021/src/tools/screens_b.before-luat-chu.py` dòng 113) | `PhaseIndex` của vùng bo (0 trước lần co đầu). Không có dữ liệu bo thì để trống |
| đồng hồ + "01:24" | Bo thu sau | `IHudState.MatchStatus` |
| ghim + "420" | Khoảng cách tới bo tiếp theo ("Bạn cách bo tiếp theo 420 m", cùng nguồn) | Tính từ vị trí nhân vật tới mép vòng an toàn, đơn vị ô thế giới, 0 khi đã ở trong. Thiếu vị trí hoặc dữ liệu bo thì để trống |

Mục đầu không phải "số người còn sống": số người còn sống đã ở khối trạng thái trận góc phải.

### Chưa có tính năng (icon vẫn hiện theo mock)
| Hàng chú giải | Trạng thái |
|---|---|
| chuột -> ghim (đặt dấu) | chưa có |
| chuột -> + (phóng to) | chưa có |
| chuột -> <- -> (kéo bản đồ) | chưa có |
| M -> X (đóng) | có |

### Tên vật phẩm
Theo cột "Item name cũ" của GDD ITEM LIST: GOD_LEAF là "Lá thần mộc" (mock ghi "Lá thần"), ADRENALINE là "Nước tăng lực" (mock ghi "Tăng lực"). Data ITM (tên tiếng Anh) không sửa. Thanh "Đang dùng" vẫn lấy tên từ logic (tiếng Anh), chưa đổi.

## R3 gộp và kiểm (unity, 07/11)
Bậc: **VERIFIED trong Editor**. Ảnh so mock `r3/so-sanh-r3.png` (B9 toast nhặt, B9 máu thấp + minimap, B5 bản đồ lớn); ảnh gốc `r3/*.png`.

| Lỗi QA | Kết quả trong Game view |
|---|---|
| T22-1 vòng bo 12 px | vòng trắng và cam nét mảnh đều (`RingGraphic`), không răng cưa |
| T22-2 chú giải, thẻ trên | 4 hàng điều khiển; thẻ trên "0 / 01:03 / 2" (pha bo, đồng hồ, khoảng cách tới vùng an toàn, ô thế giới) |
| T22-3 màu minimap | minimap cùng cam + trắng, nét 2 px |
| T22-4 toast | khung `kit_toast`, "Giáp gỗ ×1", "Mũ gỗ ×1"; thanh "Đang dùng" cũng tên Việt |

Integrator sửa thêm khi gộp: (1) `PickupToastView.cs` (file một dòng) có comment `//` nuốt mất lệnh ghi số lượng nên toast luôn hiện "×2" của prefab; đổi sang `/* */`, thêm assertion "×1"/"×2" vào `HudFeedbackAcceptanceTests.Pickup_NewestAtBottom_MergesAndDoesNotWriteInventory`. (2) Ô nhãn "Đang dùng" 280 -> 340 px, thanh tiến độ 180 -> 120 px (test `Use_GodLeaf...` đỏ vì "Lá thần m…").
