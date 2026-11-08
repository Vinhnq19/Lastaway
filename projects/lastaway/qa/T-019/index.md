# T-019 - Giáp, mũ, Adrenaline bằng 0 theo ref (unity, 07/11)

Bậc: **VERIFIED trong Editor** (compile sạch, EditMode và PlayMode chạy, Game view 1920x1080 trận Luyện tập offline). Chưa build player, chưa PLAYTESTED.

## Đã làm
| File | Thay đổi |
|---|---|
| `Scripts/Presentation/UI/Hud/HudGaugeRowView.cs` | Bỏ làm mờ icon (alpha 0,35 từ T-013); icon luôn sáng như khi có |
| `Scripts/Presentation/UI/Hud/HudPresenter.cs` | Không mặc giáp/mũ: hiện "0" màu số thường (`Text` `EDF2EE`). Adrenaline hết hoặc chưa có: hiện "0" màu số thường. Adrenaline đang chạy giữ "Ns" vàng như cũ. Adrenaline không đếm giờ (thanh đầy) vẫn không có số |
| Test | `HudPresenterTests` (EditMode: ô trống và Adrenaline hết đọc "0", tone Normal), `HudAcceptanceTests` (PlayMode: mặc định "0" và icon trắng; test `LayoutB_VitalsIcons_DimUntilTheRowHasANumber` thay bằng `Vitals_EmptyRowsReadZeroWithFullIcon_ThenTheValue`: 0, mặc mũ 30, cởi ra về 0) |

Không đổi rect, sprite, font, vị trí số (số "0" dùng đúng ô chữ của số thường).

## Ảnh (1920x1080)
| File | Ca | Đọc từ HUD |
|---|---|---|
| `01-dau-tran-0-0-0.png` | Đầu trận | giáp "0", mũ "0", Adrenaline "0", icon sáng |
| `02-co-giap-mu.png` | Nhặt giáp, mũ thật (loot offline) | giáp "50", mũ "30", Adrenaline "0" |
| `03-co-adrenaline.png` | Adrenaline đang chạy | "8s" vàng, thanh vàng |
| `so-sanh-vitals-mock-0-giap.png` | Trái mock `dung-thu-hud-A.png`, giữa ảnh 01, phải ảnh 02 (phóng 2x) | |

## Test
| Bộ | Kết quả | Ghi chú |
|---|---|---|
| EditMode | 953/959 | 6 đỏ là test map có sẵn (như T-016) |
| PlayMode | 135/136 | Đỏ `DepthSortingPlayTests.HeroWithInterpolatedRigidbodySortsByFinalFramePose` (vùng Hero, có sẵn, chập chờn) |
| Console | 0 lỗi | |

## Giả định
- Số "0" của Adrenaline màu trắng như mock, không vàng (vàng chỉ khi đang có hiệu ứng, như trước).
