# Loot rải sàn, hiệu ứng độ hiếm và đồ bắn ra khi phá thùng

Cập nhật 07/11/2026. Yêu cầu của người: bớt dựa vào thùng/hộp phá được, thay bằng thuật toán rải đồ quanh map (cân bằng, ở những chỗ nhất định); đồ trên sàn to bằng cỡ cầm tay, lơ lửng và có ánh sáng theo độ hiếm (kiểu Fortnite/Borderlands); đồ trong thùng khi đập thì bắn bật ra có animation (kiểu Fortnite); súng cần đạn thì có hộp đạn ở gần (lơ lửng, ánh sáng trắng common).

Người đã bỏ qua phần hỏi thiết kế; mọi mục dưới đây là quyết định của Claude, ghi để người sửa nếu không đúng ý.

## 1. Nguyên tắc

- **Server quyết loot.** Rải sàn chạy lúc bắt đầu trận trong code dùng chung `Backend/Shared/World` (package `com.junglemaze.world`), dùng cho cả GameServer (`MatchWorldRuntime`) lẫn Luyện tập offline (`OfflineWorldSession`). Client chỉ vẽ.
- **Không đổi protocol.** Đồ trên sàn vẫn là `WorldObjectWire` Kind 100 (`ItemId`, `Count`) như hiện nay. Hoạt ảnh bắn ra suy từ snapshot (mục 5).
- **Số liệu trong JSON** (`Backend/Shared/GameData/items_catalog.json`, đồng bộ sang `StreamingAssets/JungleMaze/runtime/` bằng menu *Jungle Maze → Phase 2 → Sync Runtime Data*; giữ manifest Sync sinh ra).

## 2. Chỗ rải đồ (cố định theo map) — `FloorLootPlanner`

Chỗ rải được tính **tất định từ hình học map** (cùng map = cùng chỗ, không cần sửa map hay Level Editor); thứ rơi ra ở mỗi chỗ thì **ngẫu nhiên theo trận**, giống loot sàn của Fortnite.

1. Ứng viên: tâm các ô đi được với bán kính hero (`MapLayout.IsWalkable`, cửa coi là mở), không nằm trên hazard (đầm/lava/độc), cách portal, cửa và vật phá được ≥ 1 ô, cách điểm spawn ≥ 3 ô, nằm trong bo đầu trận (`InitialZoneRadius`; với map tác giả dùng r = size/2).
2. Điểm ưu tiên của ô:
   - **Trong nhà** (có tường/che cứng trong 4 ô ở ít nhất 3 trong 4 hướng): cao nhất.
   - **Sát cover** (che cứng trong 2 ô): vừa.
   - **Bãi trống**: thấp.
3. Chọn theo điểm ưu tiên giảm dần với khoảng cách tối thiểu (Poisson): 3 ô giữa hai chỗ trong nhà, 5 ô ở ngoài. Dừng khi đủ số lượng.
4. Số chỗ = `diện tích đi được / AreaPerSpot` − `giá trị đồ của thùng có sẵn` (bình = 1, rương = 2 chỗ). Vì vậy map nhiều thùng tự rải ít đồ sàn hơn, map ít thùng rải nhiều hơn. Chặn trên theo ngân sách object: `WorldWire.MaxObjects − Layout.Objects.Count − ReserveObjects` (dự phòng cho đồ rơi từ thùng/xác chết).

## 3. Đồ ở mỗi chỗ (ngẫu nhiên theo trận)

- Seed trận khác nhau mỗi trận (server: seed của trận; offline: seed mới mỗi lần vào luyện tập). Cùng seed = cùng kết quả (test được).
- Mỗi chỗ có `SpotChance` (mặc định 0,75) để có đồ.
- Vòng độ khó theo khoảng cách tới tâm map: ngoài (r > 0,66R), giữa, trong (r < 0,33R). Vòng trong nhiều đồ hiếm hơn.
- Chọn đồ: nhóm theo `FloorGroupWeights` (6 nhóm như `LootTable`), món trong nhóm theo `Weight × hệ số độ hiếm của vòng` (`TierRarityMultipliers[vòng][độ hiếm]`). Chỉ món `Enabled` có nguồn `Pot` hoặc `Chest` (không lấy món chỉ có ở `Supply`).
- **Công bằng spawn:** mỗi spawn có ít nhất `MinNearSpawn` (2) chỗ có đồ trong 6–14 ô, trong đó ít nhất một vũ khí cận chiến hoặc thuốc hồi máu (ép bật chỗ gần nhất nếu thiếu).

## 4. Súng kèm hộp đạn

Mọi lần sinh súng có băng đạn (`MagazineSize > 0`) — rải sàn, rơi từ rương, từ thùng tiếp tế — đều sinh thêm `AMMO` (`AmmoBoxes`, mặc định 2 hộp) ở điểm đi được cách súng 0,8–1,2 ô. Logic đặt trong helper dùng chung (một chỗ), không chép ở từng nơi.

## 5. Đồ rơi từ thùng

- Server: đồ rơi ra **toả vòng 0,9–1,4 ô** quanh tâm thùng (góc chia đều + lệch theo seed), mỗi món ở điểm đi được (không được thì kéo dần vào tâm). Thay cho cụm 0,25 ô hiện nay.
- Client: một món Kind 100 **mới xuất hiện** trong cùng lần cập nhật (hoặc ≤ 0,5 s sau) với một rương/bình/thùng tiếp tế cách ≤ 2 ô **vừa chuyển sang vỡ** → bay từ tâm thùng theo đường cong (cao ~0,8 ô, 0,45 s, ease-out, nảy nhẹ khi chạm đất), mỗi món trễ hơn món trước 0,05 s, rồi chuyển sang lơ lửng. Đồ có sẵn lúc vào trận (rải sàn) chỉ hiện dần, không bay.

## 6. Độ hiếm

Thêm trường `Rarity` (`Common, Uncommon, Rare, Epic, Legendary`) vào `ItemDefinition` và `items_catalog.json`:

| Độ hiếm | Món |
| --- | --- |
| Common (trắng) | TREE_BRANCH, KNIFE, BONE, BANANA, SEED, WOODEN_HELMET, WOODEN_ARMOR, GOD_LEAF, SPIRIT_TEAR, AMMO |
| Uncommon (xanh lá) | AXE, SMOKE, METEORITE_FRAGMENT, SPIKE_HELMET, ARMOR, MERMAID_TAIL, ADRENALINE, RUBBER_GUN |
| Rare (xanh dương) | CRYSTAL, ROPE, SAHUR_BAT, PHOENIX_FEATHERS, MAGIC, CROSSBOW |
| Epic (tím) | SHOTGUN, BELL, MAGIC_HELMET, MAGIC_ARMOR, ELIXIR, UNICORN_BLOOD, SCEPTER |
| Legendary (vàng cam) | MACE, ELIXIR_OF_LIFE, COMPASS |

## 7. Hình đồ trên sàn (client)

- **Cỡ cầm tay:** vũ khí cận chiến/súng dùng sprite và `Length` của `HeldWeaponCatalog`; đồ ném dùng `ThrowableArtCatalog`; giáp, thuốc và hộp đạn dùng icon (`IItemIconProvider`) cỡ ~0,8 ô (hộp đạn ~0,55 ô). Nghiêng ~30° cho dễ nhìn.
- **Lơ lửng:** nhấp nhô ±0,08 ô, chu kỳ ~1,6 s, lệch pha theo Id; bóng elip mờ dưới đất co giãn theo độ cao.
- **Ánh sáng độ hiếm:** quầng sáng mềm dưới chân + cột sáng dọc (cao hơn với độ hiếm cao hơn), màu theo bảng mục 6; Epic/Legendary có thêm vài đốm lấp lánh. Hộp đạn: quầng trắng nhỏ, không cột sáng.
- Vẽ ở layer `Default` (theo Y như vật khác) để tán cây che được; pooled, không `Destroy` mỗi lần cập nhật.

## 8. Map Rừng Sinh Tồn

`Tools/Level/survival_map_150.py` bớt hẳn bình/thùng gỗ/thùng tròn rải ngoài đồng; nhà vẫn giữ một ít rương; thùng tiếp tế giữ nguyên. Phần đồ còn lại do rải sàn bù (mục 2.4). Map `map1_thap_co`/`map2_thi_tran` của Vinh không sửa: rải sàn tự bù theo số thùng của chúng.

## 9. Số đo (phần shared/server, 07/11/2026)

Đo bằng `dotnet run --project Backend/Tests/FloorLootChecks/FloorLootChecks.csproj` (203 check), 200 seed mỗi map, map đọc từ `Assets/_JungleMaze/Levels/`. "Món" = món loot không tính hộp đạn đi kèm súng; giá trị thùng là kỳ vọng `LootTable.Roll` (rương 3,5, bình 1,5, tiếp tế ~6 món).

| Map | Object map | Rương/bình/tiếp tế | Ô đi được trong bo | Chỗ theo diện tích | Ngân sách object | Chỗ (công bằng / trong nhà) | Món sàn/trận | Hộp đạn kèm súng/trận | Object/trận (trần 4096) | Món trong thùng | Tổng món | PlanSpots |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| map1_thap_co | 1605 | 32/56/2 | 16349 | 41 | 1632 | 52 (11 / 41) | 43,2 | 1,6 | ~1650 (max 1656) | 207 | 250 | 4,9 ms |
| map2_thi_tran | 1493 | 35/45/2 | 16518 | 52 | 1642 | 62 (10 / 51) | 50,8 | 1,5 | ~1545 (max 1554) | 201 | 252 | 4,8 ms |
| map3_rung_sinh_ton (hiện tại) | 3459 | 64/201/3 | 15080 | 0 | **0** | 0 | 0 | 0 | 3459 | 542 | 542 | 5,0 ms |
| map3 mô phỏng mục 8 (0 bình, giữ 1/3 rương) | 3216 | 22/0/3 | 15335 | 179 | 608 | 181 | 136,9 | – | – | 94 | 230 | – |

- Thời gian: `PlanSpots` 4–8 ms cho map 150×150 (lưới ô tính trước + tổng tiền tố, không gọi `IsWalkable` từng ô); `Roll` ~0,02 ms. So với `AuthoredMapDocument.Build`+Validate cùng máy: 18–78 ms. Server tính chỗ một lần mỗi map lúc khởi động (`MatchWorldSetup.Prewarm`, cả khi reload catalog trên thread phụ), mỗi trận chỉ `Roll`.
- Phân bố độ hiếm (map1, theo vòng): ngoài Common 67% / Uncommon 25% / Rare 5% / Epic 3%; giữa 53/28/10/9; trong 36/30/17/17. Legendary chỉ có ở tiếp tế nên không xuất hiện trên sàn.
- **map3 hiện tại không còn chỗ cho đồ sàn**: 3459 object + dự phòng xấu nhất cho đồ rơi từ thùng (64×4 + 201×2 + 3×8 = 682) + `ReserveObjects` 160 đã vượt 4096. Cần mục 8 (bớt bình/thùng) thì rải sàn mới có tác dụng; bản mô phỏng ở dòng cuối cho thấy sau khi bớt, 60% món nằm trên sàn và tổng món (230) ngang map1/map2.

Mặc định đã chọn (`Loot.Floor` trong `items_catalog.json`) và lý do:

- `AreaPerSpot = 55`, `ChestSpots = 4.5`, `PotSpots = 2`, `SupplySpots = 0`, `SpotChance = 0.75`. Một chỗ ra trung bình 0,75 món, nên một rương (3,5 món) tương đương 4,5 chỗ và một bình (1,5 món) tương đương 2 chỗ. Với các hệ số này, tổng món mỗi trận ≈ 0,75 × diện tích / 55 ≈ 220–230, **không phụ thuộc số thùng** (miễn còn chỗ dương). Map nhiều thùng thì đồ chủ yếu nằm trong thùng, map ít thùng thì nằm trên sàn. map1: 207 món trong thùng + 43 món sàn = 250 (+21% so với hiện trạng 207). Phần dư chủ yếu đến từ chỗ công bằng spawn (11 chỗ). *Khác mục 2.4*: tài liệu ghi bình = 1, rương = 2 chỗ. Với số đó, map ít thùng chỉ được khoảng một nửa tổng đồ của map1, trái với mục tiêu "tương đương".
- `GroupWeights = [22, 8, 3, 22, 25, 20]` (cận chiến, súng, đạn, ném, tiêu hao, giáp). Tên trong JSON là `Floor.GroupWeights` (tài liệu gọi `FloorGroupWeights`). Súng thấp vì rương đã ra nhiều súng (nhóm súng của rương là 20). Đạn lẻ thấp vì súng đã có hộp đi kèm.
- `TierRarityMultipliers` (hàng: ngoài, giữa, trong; cột: Common…Legendary) = `[1.3, 1, .45, .2, .1]`, `[1, 1.1, 1, .6, .3]`, `[.6, 1, 1.5, 1, 1]`. Trọng số món = `GroupWeights[nhóm] × Weight × hệ số / tổng Weight của nhóm`. Khi mọi hệ số bằng 1, công thức này đúng bằng "chọn nhóm rồi chọn món theo Weight". Khi hệ số khác 1 thì nó dời được cả nhóm có một món duy nhất (SHOTGUN): nếu chỉ nhân trong nhóm thì shotgun ngoài rìa vẫn ra như ở tâm.
- Chỗ: trong nhà = có tường/che cứng/cửa trong 4 ô ở **ít nhất 7/8 hướng**. *Khác mục 2.2* (3/4 hướng): cây import thành `Wall` nên với 3/4 hướng, 100% chỗ ở map rừng bị coi là trong nhà. Sát cover = có che cứng trong ô vuông ±2. Ngoài ra còn tránh bụi cây (đồ trong bụi bị che mất) và giữ ≥ 1 ô khỏi portal/cửa/vật phá được.
- Ngân sách object = `min(4096 − object map, 2048) − ReserveObjects (160) − số món tối đa thùng có thể rơi`. *Khác mục 2.4*: dự phòng cho đồ thùng tính theo số thùng thực tế thay vì một hằng số, vì map3 riêng phần đồ thùng đã cần tới 682 object.
- `MinNearSpawn = 2` trong 6–14 ô, `AmmoBoxes = 2` (một stack AMMO số lượng 2; chỗ sàn ra đạn lẻ cũng là stack 2), hộp đạn cách súng 0,8–1,2 ô, toả đồ thùng 0,9–1,4 ô, `DropClearance = 0.3`, `ReserveObjects = 160`.
- Seed: server lấy `LootSeeds.FromGuid(MatchId)`. Đồ trong thùng giờ cũng trộn seed trận (`LootSeeds.Mix(seed, LootSeed)`). Trước đây map tác giả có `Layout.Seed = 0` nên cùng một rương luôn ra cùng một bộ đồ ở mọi trận.
