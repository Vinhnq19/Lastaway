# Post-processing gameplay (bloom, màu, vignette)

Cập nhật 07/11/2026. Mục tiêu: trận đấu nhìn rực rỡ hơn, các nguồn sáng (đồ dưới sàn, tia lửa, ngôi sao choáng, quầng VFX) phát sáng lan ra.

## Cách hoạt động

- `GameplayPostProcessing` (`Scripts/Presentation/Rendering/`) được Bootstrap gắn vào object Bootstrap (sống xuyên scene). Nó dựng một **Volume toàn cục** lúc chạy, gồm Bloom, Color Adjustments, White Balance, Vignette. Tonemapping để `None` để giữ đúng bảng màu pixel art.
- Volume chỉ có tác dụng trong các scene ghi ở `GameplayPostTuning.Scenes` (mặc định `Game`). Menu, Lobby, Level Editor không bị đổi.
- Camera của scene Game được bật `Post Processing` lúc chạy (scene không cần sửa).
- HUD là Canvas Screen Space Overlay nên không bị bloom hay vignette.

## Chỉnh

Asset `Art/Rendering/Resources/GameplayPostTuning.asset`. Khi đang Play trong Editor, sửa số trong Inspector là thấy ngay.

| Nhóm | Trường | Mặc định | Ý nghĩa |
| --- | --- | --- | --- |
| Bloom | `BloomThreshold` | 0,78 | Độ sáng bắt đầu phát sáng; thấp hơn thì nhiều thứ phát sáng hơn |
| | `BloomIntensity` | 1,1 | Độ mạnh của quầng sáng |
| | `BloomScatter` | 0,72 | Quầng lan rộng hay gọn |
| | `BloomTint` | vàng kem | Màu quầng sáng |
| Màu | `PostExposure` | 0,08 EV | Sáng toàn cảnh |
| | `Contrast` | 14 | Tương phản |
| | `Saturation` | 24 | Độ rực màu |
| | `Temperature` | 12 | Ấm (dương) / lạnh (âm) |
| Vignette | `VignetteIntensity` | 0,26 | Tối viền màn hình |
| | `VignetteSmoothness` | 0,42 | Độ mềm của viền |

## Ánh nắng (kiểu HD-2D)

Ánh sáng nằm **trên map** (Point Light 2D đứng yên theo thế giới), không phủ lên màn hình.

- **Map nhận ánh sáng:** trong trận `LevelArtView` vẽ map bằng sprite Lit (mặc định của Renderer 2D), `litInGame` bật. Level Editor vẫn Unlit. Global Light 2D của scene `Game` giảm còn 0,82, màu hơi ấm (1; 0,97; 0,9) để vũng nắng nổi lên.
- **Vũng nắng** (`MapLights`, dựng cùng art của map): mỗi khối 10×10 ô có tối đa một Point Light vàng nắng, vị trí lệch theo seed của map, chỉ đặt ở chỗ trống (ít nhất 60% ô quanh đó không có cây, tường hay vật), như nắng lọt qua tán xuống khoảng trống. Bán kính 3,5–6,5 ô, cường độ 0,35–0,6. Cùng map thì mọi máy thấy đèn ở cùng chỗ.
- **Đèn vật thể:** đèn đá (`StoneLantern`) ánh cam ấm, cột rune ánh xanh nhạt, portal ánh xanh.
- **Chỉnh màu nắng** (post): `HighlightTint` làm vùng sáng ngả vàng, `ShadowTint` giữ vùng tối hơi lạnh; `Temperature` 12, `BloomTint` vàng kem.

| Trường (`GameplayPostTuning`) | Mặc định | Ý nghĩa |
| --- | --- | --- |
| `MapLightsEnabled` | bật | Tắt mọi đèn trên map |
| `SunPoolSpacing` | 10 ô | Khoảng cách lưới giữa các vũng nắng (nhỏ hơn = dày hơn) |
| `SunPoolOpen` | 0,6 | Tỉ lệ ô trống cần có quanh vũng nắng |
| `SunPoolColor` | (1; 0,86; 0,55) | Màu nắng |
| `SunPoolRadius` / `SunPoolIntensity` | 3,5–6,5 / 0,35–0,6 | Độ lớn và độ sáng |
| `Lantern*`, `Rune*`, `Portal*` | xem asset | Màu, bán kính, độ sáng đèn vật thể |

Đèn được dựng khi art của map dựng (vào trận), nên sửa số đèn phải vào lại trận mới thấy; số post (bloom, màu) thấy ngay khi đang Play.

## Nguồn sáng HDR

Sprite thường có màu tối đa 1, nên chỉ phần rất sáng mới vượt ngưỡng bloom. Các material phát sáng được tô màu HDR (> 1) để chắc chắn phát sáng:

| Material | Hệ số | Ở đâu đặt |
| --- | --- | --- |
| `Art/World/Loot/GroundLootLight.mat` (quầng, cột sáng, đốm lấp lánh của đồ dưới sàn) | `_Color` 1,8 | `GroundLootAssetBuilder.LightHdrBoost` |
| `Art/Vfx/Materials/glow_vfx.mat`, `sparkle_ADD_vfx.mat`, `star_ADD_vfx.mat` (tia lửa trúng đòn, ngôi sao choáng, quầng) | `_BaseColor` 1,6 | `VfxAssetBuilder.GlowHdrBoost` |

Chạy lại builder (menu *Jungle Maze → Loot → Build Ground Loot Assets*, *Jungle Maze → VFX → Build VFX-01 Assets*) giữ nguyên các hệ số này.
