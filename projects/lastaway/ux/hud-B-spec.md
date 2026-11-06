# Spec dựng HUD bản B - Lastaway (T-012, cho unity T-013)

Trạng thái: **bản B Huy chọn 06/11/2026** (`DECISIONS.md`). Spec này chỉ đổi **bố cục, kích thước, nội dung hiển thị**. Skin art giữ như hiện tại (đổi skin chờ T-011). Chưa sửa code, prefab, Figma.

Nguồn đo: `de-xuat/hud-mock/do-che.json` bản B (đã dựng lại 06/11 sau khi bó khung Quick bar), `de-xuat/hud-toi-gian.md`. Cấu trúc hiện có đọc từ `Assets/_JungleMaze/Prefabs/UI/Hud.prefab` và `Presentation/UI/Hud/HudPalette.cs`.

## 1. Đơn vị

- Prefab dùng Canvas Scaler Scale With Screen Size, `m_ReferenceResolution` **1280x720**, match width. Unity nhập số theo **đơn vị tham chiếu (u)**. Màn 1080p = 1,5 lần.
- Bảng dưới ghi **u** (để gõ vào RectTransform) và **px 1080p** (để đối chiếu mock, `hud-B.png`). Số u đã làm tròn nguyên; sai số so với mock dưới 1 px 1080p.
- Neo và gốc theo kiểu đang dùng trong prefab (Vitals neo trái dưới, QuickBar neo giữa dưới, Minimap và MatchStatus neo phải trên).

## 2. Rect và neo từng khối

Lề ngoài vẫn **24 u (36 px)** như v2.

| Khối | Object prefab | Neo (anchorMin = anchorMax) | Pivot | anchoredPosition (u) | Size (u) | Size px 1080p | v2 (u) |
|---|---|---|---|---|---|---|---|
| Vitals | `Vitals` | (0, 0) trái dưới | (0, 0) | (24, 24) | **207 x 115** | 311 x 172 | 296 x 164 |
| Quick bar | `QuickBar` | (0.5, 0) giữa dưới | (0.5, 0) | (0, 24) | **395 x 84** | 592 x 126 | 564 x 130 |
| Minimap | `Minimap` | (1, 1) phải trên | (1, 1) | (-24, -24) | **160 x 160** | 240 x 240 | 200 x 200 |
| Match status | `MatchStatus` | (1, 1) phải trên | (1, 1) | (-24, -192) | **160 x 59** | 240 x 89 | 200 x 74 |
| Killfeed (chừa chỗ, chưa có object) | - | (0, 1) trái trên | (0, 1) | (24, -24) | 171 x 90 | 256 x 135 | giữ |

Ghi chú:
- Minimap kết thúc ở y = 24 + 160 = 184 u từ trên; Match status cách 8 u (như v2): `-(24 + 160 + 8) = -192`.
- **Quick bar bó sát (điểm CPO soi trên mock B):** cao **84 u**, không còn khoảng trống dưới. Cấu tạo dọc: đệm trên 8, dải vũ khí 22, cách 4, hàng ô 42, đệm dưới 8 = 84. v2 cao 130 u vì hàng ô 84 u + dải 38 u + nhãn nhóm.
- **Các khối neo theo Vitals phải dời cùng:** `EffectsRow` v2 ở y = 196 (đỉnh Vitals 188 + 8). B: đỉnh Vitals = 24 + 115 = 139, nên `EffectsRow` anchoredPosition y = **147**. `PickupFeed` (phải dưới, 141 x 132) và `DangerBanner` (giữa trên) **không đổi**; kiểm không chồng với Quick bar (mép phải Quick bar x = 640 + 197,5 = 837,5 u; PickupFeed bắt đầu x = 1280 - 24 - 141 = 1115 u: cách 277 u).
- **Vùng giữa sạch** `320, 180, 640 x 360` (u) không đổi. B nhỏ hơn v2 nên vẫn không giao.

## 3. Vitals (207 x 115 u)

Padding **10 u** mọi cạnh. Nền Surface. Bỏ **toàn bộ nhãn chữ** (HP, Giáp, Mũ, Adrenaline, tên món). Bỏ 2 thanh (giáp, mũ): còn **2 thanh** (HP, Adrenaline) thay vì 4.

Tọa độ tương đối góc trái trên của Vitals, trục y xuống (u):

| Hàng | Phần tử | Object prefab | x | y | w x h | Nội dung |
|---|---|---|---|---|---|---|
| 1 HP | icon máu | (mới) `HpIcon` | 10 | 10 | 26 x 26 | `icon_hp.png` |
| 1 | thanh máu | `HpBar` (Track, Fill, Trail) | 42 | 16 | 109 x 14 | Fill đỏ, Trail như hiện có |
| 1 | số máu | `HpValue/HpNumber` | 157 | 8 | 40 x 30, căn phải | số lớn, **16 u** |
| 1 | "/max" | `HpValue/HpMax` | - | - | - | **ẩn** (xem giả định) |
| 2 Giáp | icon giáp | (mới) `ArmorIcon` | 10 | 46 | 26 x 26 | `icon_armor.png` |
| 2 | số giáp | `ArmorRow/Value` | 40 | 50 | 40 x 18 | **13 u** |
| 2 Mũ | icon mũ | (mới) `HelmetIcon` | 104 | 46 | 26 x 26 | `icon_helmet.png` |
| 2 | số mũ | `HelmetRow/Value` | 134 | 50 | 40 x 18 | **13 u** |
| 3 Adrenaline | icon | (mới) `AdrenalineIcon` | 12 | 80 | 24 x 24 | `icon_adrenaline.png` |
| 3 | thanh | `AdrenalineRow/Bar` | 42 | 88 | 109 x 8 | Fill vàng |
| 3 | số | `AdrenalineRow/Value` | 157 | 83 | 40 x 18, căn phải | **12 u**, "5s" khi chạy |

- `ArmorRow/Bar`, `HelmetRow/Bar`, 4 `Label` (`ArmorRow`, `HelmetRow`, `AdrenalineRow`): **tắt** (SetActive false, không xóa component để test hiện có không gãy).
- Hàng 2 khi không mặc: icon mờ (Muted), số ẩn (giữ quy tắc "không mặc thì không hiện số" của G-4). Hàng 3 khi không có hiệu ứng: icon mờ, thanh rỗng, số ẩn (giữ quy tắc luôn chiếm chỗ).
- HP ≤ 25%: số HP đổi `RedText`, giữ như G-9.
- Số ở hàng 2 là độ bền còn lại (G-13), không đổi.

## 4. Quick bar (395 x 84 u)

Padding ngang 10 u, trên 8 u, dưới 8 u. Nền Surface. Bỏ nhãn nhóm "VŨ KHÍ", "DƯỢC PHẨM" (`Heading`): hai nhóm cách nhau **15 u**.

| Phần tử | Object | x | y | w x h | Ghi |
|---|---|---|---|---|---|
| Dải vũ khí | `WeaponStrip` | 10 (căn giữa dải) | 8 | 375 x 22 | Tên (trái), đạn trong băng **16 u** + "/ dự trữ" **12 u** (giữa), `ReloadHint` "R" (phải). Cùng quy tắc G-5, G-12 |
| Nhóm vũ khí | `Slots/WeaponGroup` | 10 | 34 | 180 x 42 | 4 ô 42 x 42, cách 4 |
| Nhóm dược phẩm | `Slots/ConsumableGroup` | 205 | 34 | 180 x 42 | 4 ô 42 x 42, cách 4 |
| Dải dùng đồ | `UseActionStrip` | 10 | 8 | 375 x 22 | Thay chỗ `WeaponStrip` khi đang dùng đồ (như UI-01 phần 2); thanh `Track` cao 4 |

- Ô: 42 u (63 px 1080p; v2 64 u = 96 px, nhỏ hơn 34%). Số phím (1-8) **10 u** ở góc trên trái ô, là nhãn phím, không phải chữ thông tin. Ô chọn dùng Amber như hiện có.
- Tổng ngang: 2 x 180 + 15 = 375 u; trong 395 u còn 10 u mỗi bên.

## 5. Minimap (160 x 160 u)

- `MapArea` phủ toàn khung. **Hiển thị bản đồ thật**: nền địa hình, tường và vật cản, vòng bo (viền `Purple`), marker bản thân **14 x 14 u** kèm hướng xoay Hero. Bắc ở trên, không xoay (như UI-01).
- Lưới `Grid` giữ 4 vạch, mỗi **32 u** (= 10 cell, như v2 ở 200 px là 40 px). Màu `Grid`.
- **Không thêm chức năng** (không zoom, ping, mở rộng) theo quyết định Huy. Nguồn dữ liệu bản đồ (texture dựng sẵn hay vẽ từ dữ liệu map) do unity chọn; art-ux chưa quy định.
- Khung ngoài, `ArtBevel`, `ArtEdge`: giữ, scale theo size mới.

## 6. Match status (160 x 59 u)

Một hàng ngang, padding 10 u, căn giữa dọc. Bỏ nhãn chữ "Bo thu sau" và "Còn sống".

| Phần tử | Object | x | y | w x h | Nội dung |
|---|---|---|---|---|---|
| Icon còn sống | (mới) `AliveIcon` | 10 | 17 | 24 x 24 | `icon_alive.png` |
| Số còn sống | `AliveRow/Value` | 38 | 18 | 42 x 22 | "7/10", **16 u** |
| Icon đồng hồ bo | (mới) `ZoneIcon` | 88 | 17 | 24 x 24 | `icon_zone-timer.png` |
| Đồng hồ bo | `ZoneRow/Value` | 116 | 18 | 44 x 22, căn phải | "1:24", **16 u**, màu `Purple` |

`AliveRow` và `ZoneRow` chuyển từ xếp dọc (v2) sang xếp ngang; đổi `anchoredPosition` hai object.

## 7. Màu và cỡ chữ

- Màu **chỉ dùng token trong `HudPalette.Default`**, không thêm màu mới: Red `E5484D` (máu), Blue `5BB8E8` (giáp, mũ), Yellow `F5C542` (Adrenaline), Purple `A274FF` (bo), Amber `C6AE78` (ô chọn), Text `EDF2EE`, Muted `A6BBB7`, Surface `202A2D` @92%, Track `0C1417` @80%.
- Font **giữ nguyên** theo từng Text hiện có trong prefab; chỉ đổi cỡ.
- Cỡ chữ số (u): HP 16, giáp/mũ 13, Adrenaline 12, đạn 16 + dự trữ 12, Match status 16, tên vũ khí 12, nhãn phím 10.
- **Lệch so với UI-01:** UI-01 ghi chữ HUD tối thiểu 13 u. B có 12 u (Adrenaline, dự trữ, tên vũ khí) và 10 u (nhãn phím): Huy chốt giảm 30% nên không giữ nổi 13 u cho mọi chữ. 12 u = 18 px ở 1080p. Cần Huy hoặc CPO duyệt ngoại lệ (xem mục 10).

## 8. Icon (PNG)

Thư mục: `projects/lastaway/art/inbox/hud-icons/png/` (inbox, chưa qua cổng duyệt; unity chưa nhập cho tới khi art-ux chuyển `art/final/`). PNG RGBA, nền trong suốt, **màu đã nướng theo token** ở bảng; `Image.color` phải để **trắng** (không tint lại). Xuất bằng Microsoft Edge headless từ SVG, cắt đúng cỡ; đã kiểm kênh alpha: góc trong suốt, thân đặc.

| Dùng ở | File @1x (px) | File @2x (px) | Nguồn SVG (tác giả) | Màu nướng | Size hiển thị (u) |
|---|---|---|---|---|---|
| Vitals hàng 1, HP | `icon_hp.png` (40) | `icon_hp@2x.png` (80) | heart-organ (Lorc) | Red | 26 x 26 |
| Vitals hàng 2, giáp | `icon_armor.png` (40) | `icon_armor@2x.png` (80) | breastplate (Lorc) | Blue | 26 x 26 |
| Vitals hàng 2, mũ | `icon_helmet.png` (40) | `icon_helmet@2x.png` (80) | barbute (Lorc) | Blue | 26 x 26 |
| Vitals hàng 3, Adrenaline | `icon_adrenaline.png` (36) | `icon_adrenaline@2x.png` (72) | lightning-helix (Lorc) | Yellow | 24 x 24 |
| Match status, còn sống | `icon_alive.png` (36) | `icon_alive@2x.png` (72) | person (Delapouite) | Text | 24 x 24 |
| Match status, bo | `icon_zone-timer.png` (36) | `icon_zone-timer@2x.png` (72) | hourglass (Lorc) | Purple | 24 x 24 |
| Dự phòng (killfeed, kết quả) | `icon_kill.png` (32) | `icon_kill@2x.png` (64) | skull-crossed-bones (Lorc) | Text | chưa dùng |
| Phương án mũ 2 | `icon_helmet-alt.png` (40) | `icon_helmet-alt@2x.png` (80) | crested-helmet (Lorc) | Blue | chưa dùng |

- Import đề xuất: Sprite 2D/UI, Single, FilterMode Bilinear (nét vector, không pixel-art), tắt mipmap, không nén; dùng @2x làm nguồn, hiển thị ở size u ở trên. Quyết định import cuối của unity.
- Giấy phép CC BY 3.0: ghi công ở `ux/credits.md`.

## 9. Đo lại và tiêu chí

| Số | Giá trị (khung ngoài, 1920x1080) |
|---|---|
| Che màn v2 | 20,84% |
| Che màn B (đã bó Quick bar) | **11,65%** (`do-che.json`; bản đầu T-009 ghi 11,95% với Quick bar chưa bó) |
| Che góc dưới trái (x < 960, y > 540) | v2 36,98% -> B **17,52%** |

- **Chấp nhận:** Vitals (311 x 172 px) vẫn che một phần vùng góc dưới trái. Không giảm thêm vì Huy chốt -30%. Đo lại **trong GameView sau khi dựng** (T-013), đo bằng pixel thật không dùng khung ngoài; ghi số vào báo cáo T-013.
- Mock B lệch spec ở một chỗ: mock Vitals có khoảng trống dưới hàng 3 (~38 px 1080p); spec dàn 3 hàng theo bảng mục 3 (đệm dưới 11 u). Rect Vitals giữ 207 x 115 theo chốt Huy; muốn bó sát thêm thì hỏi Huy, vì đổi số đã chốt.
- Kiểm trong cảnh (qa): đọc được số ở 1080p; icon nhận ra ở 24-26 u; Quick bar không còn khoảng trống dưới; `EffectsRow` không chồng Vitals; không chồng PickupFeed, DangerBanner.

## 10. Giả định và câu hỏi

1. Bỏ `HpMax` ("/100"): giả định theo "icon kèm số nhỏ". Test hiện có tham chiếu `HpMax` thì chỉ ẩn, không xóa.
2. Bỏ tên món giáp/mũ (nhãn) và hai thanh độ bền: mất thông tin "mặc món gì" và vạch độ bền, chỉ còn số. Cần Huy xác nhận.
3. Match status thay chữ bằng icon: ngoài nguyên văn Huy, đã ghi ở T-009.
4. Cỡ chữ 12 u và 10 u dưới ngưỡng 13 u của UI-01: cần duyệt ngoại lệ.
5. Skin art giữ nguyên (`ArtBevel`, `ArtEdge`); icon PNG đang màu phẳng, chưa khớp art hướng A. Chờ T-011.
6. Chưa kiểm tương phản icon trên nền Surface bằng script: **NOT VERIFIED**; chưa xem trong Unity.

Một câu hỏi cho Huy qua CPO: duyệt ngoại lệ cỡ chữ 12 u (Adrenaline, dự trữ, tên vũ khí) và bỏ tên món giáp/mũ cùng thanh độ bền? Nghiêng về: duyệt cả hai, vì Huy chốt -30% và comment Figma #1 chê nhiều thanh và chữ.
