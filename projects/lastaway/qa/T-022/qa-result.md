# QA T-022 - Phản hồi HUD, túi đồ, bản đồ lớn, kill feed, thang chữ (lastaway)

Kết luận: **TRẢ LẠI** (bản đồ lớn lệch mock ở vòng bo, màu và chú giải; phần còn lại đạt). Bậc VERIFIED trong Editor, chưa PLAYTESTED.
QA: qa · 2026-11-07 · Working tree chưa commit, Unity 6000.3.15f1, Game view 1920x1080, trận Luyện tập offline (scene Game). Ảnh gốc 1080p trong `qa/`; so sánh mock (trái) và Unity (phải): `qa/so-sanh-qa-b4-b5-b9.png`.

## Theo tiêu chí
| Tiêu chí | Kết quả | Bằng chứng |
|---|---|---|
| Cung sát thương đỏ | Đạt | `qa/b9-nhat-do-cung-do.png`: cung đỏ trên-phải, hướng đúng góc 35 độ |
| Toast nhặt đồ (icon + tên + số) | Đạt có lỗi Nhẹ | cùng ảnh: "+ icon God Leaf x2". Tên hiện **tiếng Anh** "God Leaf", mock "Lá thần"; toast không có khung kit, nhỏ hơn mock (N-TOAST) |
| Máu dưới 22%, vignette 2 lớp | Đạt | `qa/b9-mau-thap.png`: HP 22 số đỏ, vignette dither cam-đỏ như mock, giữa màn trong; mũi tên nguy hiểm, banner bom, chip độc/chậm 8s, đồng hồ 6s hiện đủ |
| Đường vàng chéo ở ảnh vignette 1 lớp (CPO hỏi d) | Giải thích | Là **vòng bo kế tiếp vẽ trên world** (đường vàng khi ở trong bo, đỏ khi ra ngoài: thấy ở mọi ảnh trận), không phải lỗi vignette. Có trong ảnh gốc `so-sanh-vignette.png` |
| Túi Tab (B4) | Đạt | `qa/b4-tui-1080.png`: 3 cột, 8 ô, giáp/mũ/adrenaline/đạn; cấu trúc khớp mock (mock cũng chỉ icon, không tiêu đề). Trống vì chưa có đồ; chưa có cột đồ dưới đất (unity đã khai) |
| Bản đồ lớn (B5) | **Không đạt** | xem dưới |
| ESC đóng panel trên cùng trước | Đạt | gọi `UiLayerStack.TryCloseTop` (phím thật không vào được Game view): mở túi rồi bản đồ -> ESC1 đóng panel mở sau, ESC2 đóng panel còn lại, ESC3 trả false và `MatchOverlayInput.Poll` mở menu Cài đặt |
| Thang chữ | Đạt | Cổng Unity `do_co_chu.py --gate --unity` (bản sao ghi ra thư mục tạm, không đè T-027): 395 chữ, 1 báo nhầm (ô số rỗng HudSlot). Quét lúc chạy 64 chữ đang hiện: 100% thuộc {24, 30, 40, 96}, font đúng tầng. Không quét được cỡ 20 (số phím) khi HUD ẩn lúc spectator |
| Kill feed | Chưa nối dữ liệu | Không có trong ảnh nào (unity đã khai). Ghi nhận, cần task nối nguồn hạ gục |

## Bản đồ lớn lệch mock (CPO a, b, c: đã xác nhận)
| Điểm | Mock | Unity | Mức |
|---|---|---|---|
| Vòng an toàn (trắng) | nét 4 px, mảnh | **12 px**, răng cưa (đo `qa/b5-ban-do-lon-1080.png`, ngang qua tâm) | Vừa |
| Màu vòng | mock: cam ngoài, trắng trong | khớp mock, **nhưng minimap dùng đỏ + vàng**: hai bản đồ hai bộ màu | Vừa (c) |
| Chú giải | 4 hàng icon (chuột, mũi tên, ghim, +, <-, ->, M, X) | chỉ còn 1 hàng "M -> X"; 3 hàng trống; thẻ trên chỉ có đồng hồ (mock có cảnh báo 3 và 420) | Vừa (b) |
| Nền bản đồ | tối, lưới | địa hình thật sáng + lưới; khác mock nhưng đọc được | Nhẹ |
Người chơi hiện chấm đỏ, chưa thấy ghim xanh của mock.

## Test
Chạy lại qua UnityMCP: EditMode 962/968 (6 đỏ có sẵn: 4 LevelDocumentTests, Phase2Tests.PackagedMapHashMustMatchServer, OnlineWorldIntegrationTests); PlayMode 160/161 (đỏ `DepthSortingPlayTests.HeroWithInterpolatedRigidbodySortsByFinalFramePose`, 10,15 không vượt 10,2). Khớp báo cáo unity. Console không đánh giá riêng (0 lỗi code khi chơi).

## Bug và lỗi
| # | Lỗi | Mức |
|---|---|---|
| T22-1 | Vòng bo bản đồ lớn 12 px, răng cưa, khác mock 4 px | Vừa, **lý do TRẢ LẠI** |
| T22-2 | Chú giải và thẻ trên của bản đồ lớn gần trống so mock | Vừa, **lý do TRẢ LẠI** |
| T22-3 | Màu vòng bo minimap (đỏ + vàng) khác bản đồ lớn (cam + trắng) | Vừa; CPO chọn một bộ |
| T22-4 | Toast nhặt đồ hiện tên tiếng Anh, không khung | Nhẹ |
| T22-5 | Kill feed chưa nối | Ghi nhận |

## Chưa kiểm
Phím thật (Tab, M, ESC): cửa sổ không nhận phím từ công cụ, đã gọi API tương đương. Click chuột khi túi mở vẫn vào game (unity đã khai). Chưa chơi bị bắn thật (dùng mock HUD state), chưa có bot. 1280/2560 không thử. Chưa build player.

---
# R3 (2026-11-07 15:45): ĐẠT
Kết luận R3: **ĐẠT** (0 lỗi Nặng; 1 lỗi Nhẹ mới). Ảnh: `qa-r3/`. Chạy lại: EditMode 966/972 (cùng 6 đỏ map/world), PlayMode 171/172 (cùng đỏ DepthSorting Hero), cổng chữ Unity 399 chữ 1 báo nhầm HudSlot, quét lúc chạy 115 chữ ở Phòng chờ: 0 lệch thang, font đúng tầng (6 chữ 40 px Be Vietnam đều là nhãn nút).

| Lỗi lần trước | Kết quả R3 | Bằng chứng |
|---|---|---|
| T22-1 vòng bo 12 px răng cưa | Hết | `qa-r3/b5-ban-do.png`: trắng 2-3 px, cam 1-2 px (mock 4 px), mảnh, nét sạch |
| T22-2 chú giải trống | Hết | 4 hàng icon (chuột, mũi tên, ghim, +, <-, ->, M, X) như mock; thẻ trên có cảnh báo 0, đồng hồ 00:42, số người 2 |
| T22-3 màu minimap khác bản đồ lớn | Hết | `qa-r3/b9-toast-cung.png`, `b9-mau-thap.png`: minimap cam (hiện tại) + trắng (kế tiếp), giống bản đồ lớn, đúng DECISIONS 07/11 |
| T22-4 toast tên Anh, không khung | Hết | toast có khung kit, "Lá thần mộc ×2" (xuống 2 dòng), số ×2 đúng (unity đã sửa comment nuốt lệnh ghi) |
| Hồi quy | Không | cung đỏ, vignette 22% dither, banner bom, chip độc/chậm, túi Tab (`qa-r3/b4-tui.png`), ESC: map mở sau đóng trước, rồi túi, ESC3 -> menu |

Lỗi Nhẹ mới: **vòng bo trên world vẫn vàng (trong bo) và đỏ (ngoài bo)** trong khi minimap và bản đồ lớn là cam + trắng (renderer world, vùng Hào; ghi nhận, CPO quyết có đồng bộ không).
Ghi chú: toast tên vật phẩm Việt kiểm bằng "GOD_LEAF"; ID không có trong danh sách (qa gõ nhầm "ENERGY_DRINK") hiện id thô và ô icon đen: hành vi dự phòng, không phải lỗi.
Công cụ: ảnh chụp đồng bộ của công cụ đôi khi thiếu lớp overlay (HUD biến mất); dùng chụp bất đồng bộ cho ảnh đúng. Không phải lỗi sản phẩm.
