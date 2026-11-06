# T-013 bằng chứng 2 (unity, 2026-11-06)

Scene `Game`, trận Luyện tập offline, Unity 6000.3.15f1 Editor. Code T-013 chưa commit (cây làm việc trên HEAD `627125f7`). Chụp bằng `ScreenCapture`, có HUD Screen Space Overlay. Camera là Follow Player của scene, không đổi.

## 1. Console và test
| File | Chứng minh |
|---|---|
| `console-trong-play.json` | Console lúc đang Play sau mọi ca: 0 Error, 1 Warning (WebSocket của gói MCP) |
| `console-sau-recompile.json` | Sau refresh force + recompile: 0 Error |
| `test-EditMode.json` | EditMode 953/959; 6 đỏ là test map của Vũ/Vinh (`LevelDocumentTests` x4, `Phase2Tests.PackagedMapHashMustMatchServer`, `OnlineWorldIntegrationTests...`), giống T-008 |
| `test-PlayMode.json` | PlayMode 134/135; 1 đỏ `DepthSortingPlayTests.HeroWithInterpolatedRigidbodySortsByFinalFramePose` |
| `test-PlayMode-truoc-T013.json` | PlayMode lúc 10:53, trước khi sửa: 133 ca, đỏ đúng ca DepthSorting đó |
| `baseline-depthsorting.md` | Cách chứng minh ca DepthSorting đỏ sẵn: lần chạy trước khi sửa, giờ sửa file, `git diff` file test và code Hero không đổi. Không dùng stash (luật phiên) |

Kết quả xuất dạng JSON của MCP (tên ca, trạng thái, thông điệp, thời lượng); MCP không xuất XML NUnit.

## 2. Độ phân giải (neo, tràn)
| File | Chứng minh |
|---|---|
| `do-phan-giai-1280x720.png` + `.txt` | Hệ số canvas 1: lề 24 px, mọi khối `inside=True` |
| `do-phan-giai-1920x1080.png` + `.txt` | Hệ số 1,5: lề 36 px, khối khớp spec <= 0,3 u |
| `do-phan-giai-2560x1440.png` + `.txt` | Hệ số 2: lề 48 px, mọi khối `inside=True` |
| `chu-tran.txt` | 41 chữ đang hiện, không chữ nào tràn (`preferredWidth` <= khung, `isTextOverflowing` false) |

Bảng lề từ `.txt` (px, trái/phải/trên/dưới tùy neo):

| Khối | 1280x720 | 1920x1080 | 2560x1440 |
|---|---|---|---|
| Vitals trái/dưới | 24/24 | 36/36 | 48/48 |
| Quick bar dưới, cách đều hai bên | 24, 442,5/442,5 | 36, 663,8/663,8 | 48, 885/885 |
| Minimap phải/trên | 24/24 | 36/36 | 48/48 |
| Match status phải/trên | 24/192 | 36/288 | 48/384 |

## 3. Ca động (1920x1080)
| File | Ca | Cách tạo | Thấy |
|---|---|---|---|
| `ca-doi-o-1-chua-cam.png` | Đổi ô, trước | Phá hòm, nhặt mũ gỗ và gậy (luật offline thật) | Ô 1 có gậy, chưa viền chọn |
| `ca-doi-o-2-cam-o-1.png` | Đổi ô, sau | `SelectWeapon(0)` (lệnh phím 1) | Ô 1 viền Amber |
| `ca-bo-dang-thu.png` | Bo thu | Tăng tốc thời gian x3 tới khi bán kính bo nằm giữa hai mốc, về x1 rồi chụp: bán kính 37,6 -> 36 | Vòng đỏ (bo hiện tại) và vàng (bo kế) trên world và minimap |
| `ca-mac-giap-mu.png` | Mặc giáp, mũ | Mũ: nhặt thật (độ bền 30). Giáp: đặt thẳng vào store HUD `SetArmor("WOODEN_ARMOR", 50)` vì hòm không rơi giáp | Lúc chụp đọc được giáp "50", mũ "30", icon sáng. Sau đó trận offline ghi đè giáp từ túi đồ thật nên ảnh sau không còn số giáp |
| `ca-adrenaline-dang-chay.png` | Adrenaline đang chạy | `EffectStateStore.Apply(Adrenaline, 10 s)` | Icon sáng, thanh vàng, "5s" |
| `ca-hp-thap.png` | HP <= 25% | `OfflineMatchSession.ApplyDamage(78)` (sát thương thật) | "22" màu `E5484D` (Red) |

## 4. Minimap
| File | Chứng minh |
|---|---|
| `minimap-vung-chup.txt` | `MapSizeCells` 150; nền map (0,0)-(150,150); camera trực giao nửa cỡ 75, tâm (75,75), vùng chụp (0,0)-(150,150), tức trùng đúng map; RT 256x256 (1,71 texel/ô); marker (67,73; 69,87) bằng vị trí tính từ Hero (67,733; 69,867); camera tắt sẵn, culling bỏ layer UI |
| `minimap-an-nguoi-choi.txt` + `minimap-rt-*.png` | Người chơi khác: offline không có bot, nên dựng tạm một `PlayerView` (prefab của `RemotePlayerSpawner`, phóng to 12 lần) ở tâm map rồi gọi chụp. Tổng màu vùng tâm: gốc 2167,0; có `PlayerView` 2167,0 (lệch 0, bị ẩn); sprite đối chứng không phải người chơi 1891,0 (lệch -276, có hiện). Renderer được trả lại sau khi chụp. Chưa thử trong trận online thật có bot |

## 5. Ô tối cạnh icon giáp
| File | Chứng minh |
|---|---|
| `o-toi-nen-world.png`, `o-toi-nen-magenta.png` | Cùng một khung hình: nền world và nền magenta (camera không vẽ world) |
| `o-toi-vitals-phong-to.png` | Vitals phóng to x3: trái có ô tối, phải (chỉ HUD) không có. Kết luận: là viên đá lát của world nhìn xuyên nền Vitals (Surface alpha 0,92, như v2), không phải Track thanh giáp. Danh sách Graphic đang bật trong Vitals không có gì ở vị trí đó (thanh giáp/mũ đã tắt). Không sửa |

## Không làm được
- Minimap trong trận có người chơi thật hoặc bot: cần backend Local (Docker) hoặc AWS. Đã thay bằng thử nghiệm `PlayerView` ở mục 4.
