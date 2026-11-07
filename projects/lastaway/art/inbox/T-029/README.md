# T-029 asset và mock minimap phương án M

Nguồn: `de-xuat/minimap-chuc-nang.md` mục 5 (D1 W1-W11, D2 E1-E10). Gen GPT (7 job, ảnh gen ở `src/gen/`), script chỉ tách nền, cắt, đổi cỡ, đo (`src/import.py`). Không sửa `Assets/`, chưa commit.

## Mock (1920x1080, nền là ảnh gameplay thật trong `qa/T-022/zone-main/`)
| File | Nội dung |
|---|---|
| `m1-minimap-toan-map.png` | Minimap mức toàn map (lỗ 269 px): Hero Amber, cờ ở tây nam Hero (33 cell), đường kẻ, 3 chấm địch, 1 dấu thấy lần cuối, chỉ báo "Toàn"; trên world: cờ ngoài màn nên không vẽ cờ, chỉ mũi tên mép trái chỉ về cờ kèm "33" |
| `m2-minimap-gan.png` | Minimap mức gần (30 cell theo Hero): cờ ngoài khung kẹp ở mép góc phải dưới, chỉ báo "Gần" |
| `m3-ban-do-lon-waypoint.png` | Bản đồ lớn: cờ 24 px, đường kẻ Hero tới cờ, 4 chấm địch 16 px, 1 dấu thấy lần cuối, chú giải và hướng dẫn đã đổi sang cờ, marker Amber, vòng kế tiếp nét đứt |
| `trang-kich-co.png` | Từng icon ở cỡ vẽ thật (hàng trên) và 96 px (hàng dưới) |
Mock only: vùng ngoài bo ở bản đồ lớn được đổi từ cam (ảnh chụp) sang hồng (167,55,80) theo quyết định Huy 07/11; vòng kế tiếp vẽ bằng script (không phải asset). Game thật vẽ vòng và đường kẻ bằng code.

## Màu cờ (W10)
Xanh trời `#5BB8E8` (token Blue của `HudPalette`, cùng hệ giáp) viền tối `#0A0C0C`. Không trùng đỏ địch `#E5484D`, trắng vòng bo kế, Amber `#C6AE78` của bản thân, hồng vùng bo `#A73750`, xanh lá địa hình.

## File (trong `files/`, @1x và @2x, nền trong suốt, không 9-slice)
| File | Cỡ file @1x | Vẽ ở 1080p | Dùng |
|---|---:|---|---|
| `wp_flag.png` | 48x48 | 24 (bản đồ lớn), 40 (trên world, cờ trong màn) | W7, W9 |
| `wp_flag_mini.png` | 16x16 | 14 trong mock, **đề xuất 18** | W8 minimap |
| `wp_arrow_edge.png` | 48x48 | 40, hướng lên, xoay theo hướng tới cờ | W9 mũi tên mép màn |
| `map_enemy_dot.png` | 16x16 | 12 (minimap), 16 (bản đồ lớn) | E7 |
| `map_lastseen_ring.png` | 16x16 | 12 / 16, alpha 0,6 giảm về 0 trong 2,0 s do code | E4 |
| `kit_ico_zoom.png` | 48x48 | 24, ink tối | chỉ báo mức zoom |
| `map_self_marker.png` | 32x32 | 26 (minimap), 40 (bản đồ lớn); mũi nhọn trên chỉ hướng Hero | marker bản thân Amber, thay marker đỏ |
`files/meta.json` ghi cỡ, border (không có), nguồn. Chấm địch và vòng thấy lần cuối dùng chung màu đỏ nên chỉ khác hình (đặc / rỗng), như E4.

## Ô ghép từ kit sẵn (không gen mới)
| Thành phần | Sprite | Cỡ 1080p | Border LRTB | Chữ |
|---|---|---|---|---|
| Số khoảng cách mũi tên mép màn | `T-028 files/new/kit_badge_key` kéo giãn | 64x36 đặt cạnh mũi tên | [14,14,14,14] | Be Vietnam Pro Bold 24, cream `#F1E7C8`, viền 2 px `#0A0C0C`; làm tròn cell |
| Chỉ báo mức zoom | `kit_badge_key` kéo giãn + `kit_ico_zoom` 24 + chữ | 150x48, góc trên trái lỗ minimap (cách mép 8 px) | [14,14,14,14] | Be Vietnam Pro Bold 24 "Toàn" / "Gần"; cao 48 vì dấu tiếng Việt tràn ra khỏi ô 36 |
| Đường kẻ Hero - cờ | code | 1 px, alpha 0,6, `#BDE6F8` (minimap), 1 px alpha 0,5 (bản đồ lớn theo W7) | - | - |
| Vòng bo kế tiếp | code (chú giải: `legend_ring_next_dashed`, T-028) | nét 4 px trắng, nét đứt 6 on 5 off | - | - |

## Cổng (`python -P src/cong.py`)
| Cổng | Kết quả |
|---|---|
| Tương phản chữ 4,5:1 (3 dòng chữ: "39", "Toàn", "Gần") | 0 lỗi (4,7 / 13,8 / 13,8) |
| META BORDER (`T-020/check_meta_border.py` trên `files/meta.json`) | 0 lỗi, không có sprite sliced mới |
| Soi cỡ ở lỗ 269 px | chấm địch 12 px, mũi nhọn Hero 26 px, đường kẻ đọc được. **Cờ 14 px quá nhỏ ở mức toàn map: đề xuất 18 px.** Dấu thấy lần cuối 12 px mảnh, vẫn thấy được ở alpha 0,6 nhưng khó thấy khi alpha giảm; đề xuất 14 px |

## Giả định
- Số khoảng cách dùng bậc 24 (sàn gameplay), không dùng 20.
- Marker bản thân đổi sang Amber theo W10; marker đỏ cũ chỉ còn trong ảnh chụp.
- Chú giải bản đồ lớn chưa thêm dòng cho chấm địch và dấu thấy lần cuối (hàng chú giải đủ 6 ô); cần Huy hoặc CPO quyết nếu muốn chú giải riêng.
