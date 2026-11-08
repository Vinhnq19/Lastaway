# T-025 - Đang tải, mất kết nối/reconnect, lỗi tải sang skin A (unity, 07/11)

Bậc: **VERIFIED trong Editor**, cùng lượt test với T-024 (xem `../T-024/index.md` mục Test). Chưa PLAYTESTED, chưa chạy với server thật.

## Đã làm
`LoadingView`, `ReconnectView`, `NoticeView` có phần tử hình mới (thanh `kit_progress_*`, `fx_icon_disconnect`, đếm ngược `disconnectGrace`), dựng trong `LobbyUiPrefabBuild`; nền gạch ở Đang tải và Lỗi tải.

## Ảnh
| File | Màn |
|---|---|
| `so-sanh-mat-ket-noi-loi-tai.png` | B8 đang kết nối lại, A7 lỗi tải (Play), A7 đang tải (ảnh tĩnh từ prefab, không có dữ liệu) |
| `r2/*.png` | ảnh gốc 1920x1080 |

## Chưa làm / lệch
- B8 "thất bại / Thử lại ngay": chưa dựng nút Thử lại ngay vì `ReconnectController` không có lệnh thử lại (vùng netcode của Vũ); B8 hết hạn dùng màn Disconnected có Về sảnh.
- A7 đang tải: thanh chạy theo thời gian đã dùng (không có dữ liệu % tải); chưa soi được trong Play vì luồng mock đi qua Loading quá nhanh (chỉ có ảnh tĩnh prefab). Các bước tải và keycap điều khiển thay bằng chuỗi gợi ý có sẵn.

## R3 (unity, 07/11)
Ảnh `r3/so-sanh-r3.png`. Màn Mất kết nối (hết hạn) có sàn gạch; nội dung trùng tiêu đề "Mất kết nối" được ẩn, chỉ còn tiêu đề + nút Về trang chủ. Mock có dòng "Hạng #4 / 10" và tiêu đề "Đã mất kết nối": client chưa có hạng khi rớt mạng, tiêu đề lấy từ `ReconnectController.LostText` (Application) nên giữ.
