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

Mục tiêu: cảnh như có nắng vàng chiếu xiên từ góc trên trái. Gồm hai phần:

- **Chỉnh màu nắng:** `HighlightTint` (1,08; 1,03; 0,88) làm vùng sáng ngả vàng; `ShadowTint` (0,94; 0,98; 1,06) giữ vùng tối hơi lạnh cho tương phản; `Temperature` 12, `BloomTint` vàng kem.
- **Lớp nắng** (`SunlightOverlay`, dựng lúc chạy, texture sinh bằng code): 5 cột nắng mềm nghiêng `SunAngle` 32°, đung đưa chậm và sáng tối nhẹ; một quầng nắng ở góc trên trái (`SunCorner`); 40 hạt bụi sáng trôi lên chậm và lấp lánh. Vẽ ở layer `VFX` (trên tán cây, dưới HUD), màu HDR ×1,35 nên có bloom.

| Trường | Mặc định | Ý nghĩa |
| --- | --- | --- |
| `SunEnabled` | bật | Tắt hẳn lớp nắng |
| `SunColor` | vàng nắng (1; 0,87; 0,55) | Màu cột, quầng, bụi |
| `SunRays` / `SunRayAlpha` | 5 / 0,085 | Số cột và độ đậm |
| `SunRayWidth` | 1,2–3,4 ô | Độ rộng cột |
| `SunGlowAlpha` / `SunGlowSize` | 0,2 / 1,5 màn hình | Quầng nắng ở góc |
| `DustCount` / `DustAlpha` / `DustSpeed` | 40 / 0,55 / 0,35 ô/s | Bụi sáng |

## Nguồn sáng HDR

Sprite thường có màu tối đa 1, nên chỉ phần rất sáng mới vượt ngưỡng bloom. Các material phát sáng được tô màu HDR (> 1) để chắc chắn phát sáng:

| Material | Hệ số | Ở đâu đặt |
| --- | --- | --- |
| `Art/World/Loot/GroundLootLight.mat` (quầng, cột sáng, đốm lấp lánh của đồ dưới sàn) | `_Color` 1,8 | `GroundLootAssetBuilder.LightHdrBoost` |
| `Art/Vfx/Materials/glow_vfx.mat`, `sparkle_ADD_vfx.mat`, `star_ADD_vfx.mat` (tia lửa trúng đòn, ngôi sao choáng, quầng) | `_BaseColor` 1,6 | `VfxAssetBuilder.GlowHdrBoost` |

Chạy lại builder (menu *Jungle Maze → Loot → Build Ground Loot Assets*, *Jungle Maze → VFX → Build VFX-01 Assets*) giữ nguyên các hệ số này.
