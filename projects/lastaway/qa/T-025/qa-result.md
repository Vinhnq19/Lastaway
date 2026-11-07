# QA T-025 - Đang tải, mất kết nối, lỗi tải (lastaway)

Kết luận: **ĐẠT** (0 lỗi Nặng; 1 lỗi Vừa ở màn Mất kết nối). Bậc VERIFIED trong Editor, chưa PLAYTESTED.
QA: qa · 2026-11-07 · Working tree chưa commit, mock HappyPath và Disconnect, 1920x1080. So sánh: `qa/so-sanh-qa-a7-b8.png`.

## Theo tiêu chí
| Màn | Kết quả | Bằng chứng |
|---|---|---|
| A7 Đang tải | Đạt | `qa/a7-dang-tai.png`: thanh tiến độ, "Còn 34 s", 3 bước, thẻ Điều khiển, mẹo, nền gạch. Thanh chạy theo thời gian, 3 bước cùng một kiểu chấm (chưa phân biệt xong / đang làm), đúng như unity đã khai |
| A7 Lỗi tải | Đạt | `qa/a7-loi-tai.png`: sau timeout "Tải trận quá lâu", icon X, nút Về trang chủ, nền gạch |
| B8 Mất kết nối (hết hạn) | Đạt có lỗi Vừa | `qa/b8-mat-ket-noi-het-han.png`: icon rớt wifi, nút Về trang chủ. **Nền gần đen phẳng, không có sàn gạch** như mọi màn khác (lỗi "lòi" kiểu ngược: lộ nền clear của camera); **tiêu đề và nội dung đều "Mất kết nối"** (trùng chữ) |
| B8 Đang kết nối lại (đếm ngược) | **NOT VERIFIED** | qa bật `ReconnectEnabled`, đặt `CanResume`, cho rớt kết nối giữa trận: flow vẫn đi thẳng Disconnected, không hiện màn đếm ngược. Chưa rõ do cách lái mock hay luật `LobbyScreenRules.OnLost`; chỉ có ảnh `T-024/play/f-reconnect.png` của unity |
| B8 Thất bại / Thử lại ngay | Chưa dựng | unity đã khai (netcode của Vũ) |

## Test
Cùng lượt T-022: EditMode 962/968, PlayMode 160/161, 395 chữ, 1 báo nhầm.

## Lỗi
| # | Lỗi | Mức |
|---|---|---|
| T25-1 | Màn Mất kết nối (Disconnected) nền đen phẳng, thiếu `HasFloor` như Phòng chờ, Đang tải, Kết quả | Vừa |
| T25-2 | Chữ trùng "Mất kết nối" ở tiêu đề và nội dung (chuỗi từ `ReconnectController.LostText`) | Nhẹ |
| T25-3 | Màn đếm ngược reconnect chưa thấy được trong lượt này | NOT VERIFIED |

## Chưa kiểm
Màn đếm ngược reconnect, thử lại, server thật, 1280 và 2560, build player.

---
# R3 (2026-11-07 15:45): ĐẠT
Kết luận R3: **ĐẠT** (0 lỗi). Ảnh: `../T-025/qa-r3/b8-mat-ket-noi.png`.

| Lỗi lần trước | Kết quả R3 | Bằng chứng |
|---|---|---|
| T25-1 nền đen màn Mất kết nối | Hết | sàn gạch phủ kín, tối theo Dim |
| T25-2 chữ trùng | Hết | chỉ còn tiêu đề "Mất kết nối" và nút Về trang chủ (khoảng trống phía dưới tiêu đề, chấp nhận được) |
| T25-3 đếm ngược reconnect | **Vẫn NOT VERIFIED** | qa chỉ đặt `CanResume=false` để chụp màn hết hạn; chưa lái lại nhánh reconnect |
Hồi quy A7 đang tải/lỗi tải: không chụp lại (code R3 chỉ đổi nền sàn).
