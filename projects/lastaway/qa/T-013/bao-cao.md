# T-013 - HUD bản B trong Unity (unity, 2026-11-06)

Bậc: **VERIFIED trong Editor** (compile sạch, EditMode + PlayMode chạy, Game view 1920x1080 trong trận offline). Chưa build player, chưa PLAYTESTED (chờ Huy chơi).

## Đã làm
- `Tools/Editor/Hud/HudLayoutBBuild.cs` (menu `Jungle Maze/HUD/Apply layout B (T-013)`): áp spec `ux/hud-B-spec.md` vào `Prefabs/UI/Hud.prefab`, chạy lại cho ra cùng kết quả. Skin (ArtEdge/ArtBevel, font) giữ nguyên.
- Icon: 8 PNG @2x nhập vào `Art/Hud/StatusIcons/` (Sprite, không mipmap, không nén, `Image.color` trắng).
- Code: `HudGaugeRowView` thêm `icon` (mờ alpha 0,35 khi hàng không có số); `QuickbarView` thêm `panelBackground` (nền Surface cho cả Quick bar); `MinimapMapCapture` mới (minimap hiện map thật).
- Minimap: camera trực giao tắt sẵn, chụp toàn map vào RenderTexture 256x256 mỗi 1 s; ẩn Hero và người chơi khác (`forceRenderingOff`) và canvas world-space trong lúc chụp, rồi trả lại ngay; nới nét LineRenderer để vòng bo còn thấy. Không thêm chức năng (zoom, ping). Dữ liệu map chỉ đọc (`MapGridView.MapSizeCells`), không sửa nguồn của Vũ/Quang Huy.

## Ảnh (Game view 1920x1080, scene `Game`, trận Luyện tập offline)
| File | Nội dung |
|---|---|
| `01-dau-tran-1920x1080.png` | Đầu trận, HUD B mặc định |
| `02-co-do-1920x1080.png` | Đã nhặt mũ gỗ (số 30, icon sáng) và gậy (ô 1 viền Amber), pickup feed |
| `03-do-che-nen-magenta.png` | Camera xóa nền magenta, chỉ còn HUD: dùng đếm % che |
| `so-sanh-mock-B-va-unity.png` | Trái: `de-xuat/hud-mock/hud-B.png`; phải: ảnh 01 |
| `do-rect-1920x1080.txt` | Số đo rect từ `RectTransform.GetWorldCorners` |

## Rect khối, 1920x1080 (px, gốc trái trên)
| Khối | Spec | Đo trong Unity | Lệch (u) |
|---|---|---|---|
| Vitals | 36, 872, 311 x 172 | 36, 871,5, 310,5 x 172,5 | <= 0,3 |
| Quick bar | 664, 918, 592 x 126 | 663,8, 918, 592,5 x 126 | <= 0,3 |
| Minimap | 1644, 36, 240 x 240 | 1644, 36, 240 x 240 | 0 |
| Match status | 1644, 288, 240 x 89 | 1644, 288, 240 x 88,5 | 0,3 |
| EffectsRow | y đáy = 147 u | 36, 772,5, 441 x 87 (đáy 859,5 = 1080 - 147 x 1,5) | 0 |

Test PlayMode `Layout_FourBlocksMatchLayoutB_At1280x720` và `Layout_ChildElementsMatchLayoutB` kiểm các rect này với dung sai 2 u.

## % che màn
| Số | Spec (khung ngoài) | Đo pixel thật trong Unity |
|---|---|---|
| Toàn màn | 11,65% | **9,81%** (203.389 px) |
| Góc dưới trái | 17,52% | **17,18%** |

Đo lúc không có pickup toast, không có hiệu ứng; góc bo tròn và vùng trong suốt của khung không tính.

## Test
| Bộ | Kết quả | Ghi chú |
|---|---|---|
| EditMode | 953/959 | 6 đỏ là test map của Vũ/Vinh (`map_demo.json` mới), như T-008 |
| PlayMode trước khi sửa | 132/133 | Đỏ: `DepthSortingPlayTests.HeroWithInterpolatedRigidbodySortsByFinalFramePose` (vùng Hero) |
| PlayMode sau khi sửa | 134/135 | Cùng 1 ca đỏ đó; 2 test mới đạt |
| Console | 0 lỗi đỏ | Sau compile, build prefab, Play/thoát |

## Test cũ đã đổi (vì Huy đổi bố cục v2 sang B, DECISIONS 06/11)
| Test | Đổi gì | Vì sao |
|---|---|---|
| `HudAcceptanceTests.Layout_FourBlocksMatchFigmaV2_At1280x720` -> `..._MatchLayoutB_...` | Rect v2 -> B (`HudLayoutB.cs`) | Bố cục mới |
| `HudAcceptanceTests.Layout_ChildElementsMatchFigmaV2` -> `..._MatchLayoutB` | Kiểm icon, thanh, ô 42, dải 375 thay cho hàng v2 | Bỏ nhãn và 2 thanh độ bền |
| `HudAcceptanceTests.WeaponStrip_StaysCentered_WithAndWithoutAmmo` | Dải cố định 375 u, hai trạng thái cùng rộng | v2 co theo nội dung |
| `HudAcceptanceTests.Hud_DefaultState_BeforeAnyData` | "/100" -> `HpMax` bị ẩn | B ẩn "/max" |
| `HudAcceptanceTests.HpTrail_WidthMatchesLoss` | 21 -> 8% x 109 | Thanh máu ngắn hơn |
| `HudAcceptanceTests.Minimap_FollowsHeroPose` | 100/200 -> 80/160 | Minimap 160 |
| `HudAcceptanceTests.Text_MinSize13_AndVietnameseGlyphs`, `HudFeedbackAcceptanceTests.FeedbackText_MinSize13_...` | Ngưỡng 13 u trừ đúng ngoại lệ đã duyệt (12 u: số Adrenaline, đạn dự trữ, tên vũ khí; 10 u: số phím ô) | Ngoại lệ Huy duyệt 06/11 |
| `HudFigmaV2Feedback.Effects/Use`, `HudFeedbackAcceptanceTests.FeedbackLayout_*` | EffectsRow y 466 -> 515; dải dùng đồ trong Quick bar; vùng ô 375 x 42 | EffectsRow 196 -> 147, Quick bar mới |
| `UiArtIntegrationTests.RealQuickbar_KnownUnknownKnownAndEmpty` | Icon ô 28 -> 22 | Ô 42 u |

Test mới: `LayoutB_VitalsIcons_DimUntilTheRowHasANumber`, `LayoutB_Minimap_DrawsTheMapWithoutPlayers`.

## Lệch spec (unity tự quyết)
- Match status: spec đặt đồng hồ bo x 116 w 44, chạm mép khung 160 nên "01:22" bị cắt. Dời icon bo 88 -> 80, số 116 -> 106 (đệm phải 10 u), số còn sống rộng 38.
- Ô 42 u: icon 22 u ở (10, 16), không phải 26 căn giữa, để không đè số phím và số lượng (test ART-04 bắt chồng).
- Chữ ngoài ngoại lệ giữ 13 u: số lượng trong ô, phím R, "Nạp", dải dùng đồ.
- Vòng bo trên minimap lấy màu vòng trong world (đỏ: bo hiện tại, vàng: bo kế tiếp), không phải Purple như spec mục 5.
- Icon nhập Bilinear, không phải Point như brief: icon là nét vector thu nhỏ từ 80 xuống 33-39 px, Point làm răng cưa. Spec mục 8 cũng đề xuất Bilinear.
- `WeaponStrip` nằm trong khung mới `QuickBar/StripArea`: scene `Game.unity` và `WorldDemo.unity` (của Vũ, Quang Huy) có override cũ `m_SizeDelta.x = 0`; bọc vào khung giãn thì 0 là đúng, không phải sửa scene.

## Thấy khi chạy, không sửa (vùng khác)
- Nhãn vũ khí vẫn ghi "Bare Hands" khi đang cầm gậy (vùng Hào).
- `DepthSortingPlayTests` đỏ sẵn (vùng Hero).

## Chưa làm
- Project map (`Tools/update_project_map.py`) chưa chạy: máy thiếu `graphify-out/.graphify_python` và gói `graphifyy`; cài gói cần Huy duyệt.
- Chưa build player, chưa chạy online.
