# QA T-023 - ESC cài đặt, xác nhận thoát, bị loại, spectator (lastaway)

Kết luận: **ĐẠT** (0 lỗi Nặng, 1 lỗi Vừa cần CPO duyệt: thiếu lớp tối nền khi mở Cài đặt). Bậc VERIFIED trong Editor, chưa PLAYTESTED.
QA: qa · 2026-11-07 · Working tree chưa commit, Game view 1920x1080. So sánh mock (trái) và Unity (phải): `qa/so-sanh-qa-b1-b2-b3.png`.

## Theo tiêu chí
| Tiêu chí | Kết quả | Bằng chứng |
|---|---|---|
| ESC mở Cài đặt (B1) | Đạt | `qa/b1-esc-cai-dat.png`: slider, toggle, Mặc định / Thoát trận / Tiếp tục, nút X; qa gọi `MatchOverlayInput.Poll` với ESC giả lập |
| Xác nhận thoát | Đạt | `qa/b1-xac-nhan-thoat.png`: "Thoát trận?" lồng trên Cài đặt, Thoát trận / Ở lại |
| ESC đóng một lớp | Đạt | cùng chuỗi với T-022: túi + bản đồ đóng từng lớp rồi mới mở menu |
| Bị loại (B2) | Đạt | `qa/b2-bi-loai.png` (mock overlay "Người chơi"): #4 / 10, thẻ nguyên nhân, 2 thẻ thống kê, Về sảnh / Theo dõi; nền gạch kín |
| Spectator (B3) | Đạt | `qa/b3-spectator.png`: thẻ trên, danh sách 9 người, thẻ mục tiêu 93/100, keycap A D E, nút về |
| Thắng / thua | Đạt | xem T-024 |

## Lệch mock
| Điểm | Mức |
|---|---|
| **Cài đặt và xác nhận thoát không có lớp tối nền**: mock phủ tối cả màn, Unity để world sáng nguyên. Panel vẫn đọc được trên cỏ sáng, nhưng mất độ nổi và lệch mock rõ | Vừa (T23-1) |
| Tiêu đề "Bạn đã bị loại" (mock "BỊ LOẠI"), hạng "#4 / 10" cùng cỡ (mock "/ 10" nhỏ), thẻ thống kê có nhãn: unity đã khai | Nhẹ |
| Thẻ nguyên nhân thiếu icon vũ khí, spectator "Còn sống 9/10" thay cúp và hạng: unity đã khai, chờ art-ux | Nhẹ |
| Menu mở nhưng trận vẫn chạy: HP rơi 100 -> 75 trong lúc mở Cài đặt (offline, bo thu). Đúng luật battle royale, chỉ ghi nhận | Thông tin |

## Test
Cùng lượt T-022: EditMode 962/968, PlayMode 160/161, đỏ có sẵn (liệt kê ở T-022). Cổng cỡ chữ Unity 395 chữ, 1 báo nhầm.

## Chưa kiểm
Phím ESC thật; killcam replay; "người đang xem bị loại" và "hết trận" (không bấm trong lượt này); bị loại với 4 nguyên nhân còn lại; không có server thật.

---
# R3 (2026-11-07 15:45): ĐẠT
Kết luận R3: **ĐẠT** (0 lỗi). Ảnh: `qa-r3/`. Test và cổng chữ như T-022 R3.

| Lỗi lần trước | Kết quả R3 | Bằng chứng |
|---|---|---|
| T23-1 thiếu lớp tối khi mở Cài đặt | Hết | độ sáng trung bình vùng world (1400-1800 x 600-900): không dim 105; R2 67; **R3 47**; mock 52. Đính chính R2: lúc đó có dim nhẹ (alpha 0,6, đo 67), không phải "không có"; vẫn nhạt hơn mock. `qa-r3/b1-esc.png`, `b1-xac-nhan.png` |
| Tiêu đề "Bạn đã bị loại" | Hết | `qa-r3/b2-bi-loai.png`: "BỊ LOẠI", "#4 / 10" với "/ 10" nhỏ, 2 thẻ chỉ số (2, 05:12), nguyên nhân kèm icon nỏ |
| Icon Bo thu | Có | `qa-r3/b2-bo-thu.png`: icon bo cam trên đĩa, "Bị bo thu hẹp loại" |
| Spectator | Đạt, 1 Nhẹ | `qa-r3/b3-spectator.png`: danh sách 9, thẻ mục tiêu; **thẻ mục tiêu chưa có icon vũ khí** (mock có nỏ ở góc thẻ): Nhẹ, có thể do mock data không có vũ khí |

Hồi quy: ESC mở Cài đặt, xác nhận thoát, Ở lại / Tiếp tục đóng đúng.
