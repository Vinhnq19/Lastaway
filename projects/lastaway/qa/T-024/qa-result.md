# QA T-024 - Sảnh, đăng nhập, vào/tạo phòng, phòng chờ, thắng/thua (lastaway)

Kết luận: **ĐẠT** (0 lỗi Nặng; "lòi UI" sửa phần nhìn thấy được, phần cần server thật là NOT VERIFIED). Bậc VERIFIED trong Editor, chưa PLAYTESTED.
QA: qa · 2026-11-07 · Working tree chưa commit, scene Boot, 1920x1080. Đăng nhập thật thất bại (API không với tới, thông báo đúng); qa nhét JWT giả vào `AccountSession` rồi dùng mock HappyPath. So sánh: `qa/so-sanh-qa-a2-a3-a4-a6-b6-b7.png`.

## Theo tiêu chí
| Màn | Kết quả | Ghi chú |
|---|---|---|
| Đăng nhập | Đạt | thông báo lỗi mạng hiện đúng; bố cục theo kit |
| A2 Sảnh | Đạt, lỗi Nhẹ | chip tài khoản cắt chữ "Đã đăng nh…" (mock đủ "Đã đăng nhập"); thiếu "v0.2" và dấu tick "API sẵn sàng" |
| A3 Vào phòng | Đạt | Dim phủ kín, chip không đè modal; 1 ô nhập mã thay 6 ô (đã khai) |
| A4 Phòng của tôi / tạo phòng | Đạt | khớp mock; hàng tạo phòng ở cuối (đã khai) |
| Đang kết nối | Đạt | modal có thanh tiến độ |
| A6 Phòng chờ | Đạt, lệch đã khai | nền gạch kín, **không còn lộ world**; Sẵn sàng / Bắt đầu hiện sau vài giây khi bật cờ; "Thêm bot", "Điền đủ 6" dùng nút kit nhỏ. Số 96 px thay câu 40 px, thiếu nút quay lại, thiếu dòng "Bản đồ" |
| B6 Thắng | Đạt | vòng nguyệt quế, #1 / 4, thẻ sống sót, bảng xếp hạng |
| B7 Thua | Đạt | huy hiệu sọ, "HẠNG #1", thẻ sống sót, nút. Ca offline solo ghi "HẠNG #1 / #1 / 1" (số liệu từ luồng offline, không phải UI) |

## Lỗi "lòi UI" của Huy
- Kiểm bằng mắt: Phòng chờ, Đang tải, Lỗi tải, Kết quả đều có nền gạch; modal Vào phòng có Dim; chip không đè. Đạt.
- **NOT VERIFIED**: thẻ map, nút "Xóa" của `RoomMapPanel` chỉ hiện với chủ phòng có API thật; `MapCardTemplate` đang tắt trong mock. Cần chạy lại với API + GameServer local.
- Nền tối của màn Mất kết nối (T-025) là chỗ "lòi" còn sót.

## Test
EditMode 962/968; PlayMode 160/161; cổng cỡ chữ 395 chữ, 1 báo nhầm. Chi tiết và tên ca đỏ ở T-022. Console có 1 lỗi `Curl error 60: Cert verify failed` khi màn Đăng nhập gọi API thật (mạng, không phải code đợt này) như unity đã báo.

## Lỗi
| # | Lỗi | Mức |
|---|---|---|
| T24-1 | Chip tài khoản cắt chữ "Đã đăng nh…" | Nhẹ |
| T24-2 | Thiếu nhãn phiên bản và dấu tick API ở Sảnh | Nhẹ |
| T24-3 | Cách lái mock: `GoHome` khi mock còn Joined làm `LobbyFlow` kẹt `_leaving`, màn Kết nối không thoát. Chỉ xảy ra với mock và panel F9 (cùng đường gọi). Sản phẩm thật chưa thấy | Nhẹ, ghi nhận |

## Chưa kiểm
Server thật, bản đồ phòng chủ, Xóa phòng, màn Lịch sử và Bảng xếp hạng (không mở), hàng phòng đầy / sai mã, JWT hết hạn, 1280 và 2560, phím thật, build player.

---
# R3 (2026-11-07 15:45): ĐẠT
Kết luận R3: **ĐẠT** (0 lỗi). Ảnh: `qa-r3/`. Test và cổng chữ như T-022 R3.

| Lỗi lần trước | Kết quả R3 | Bằng chứng |
|---|---|---|
| T24-1 chip cắt chữ | Hết | `qa-r3/a2-sanh.png`: "Đã đăng nhập" đủ (dòng tên trống vì qa không đặt username trong ca giả) |
| T24-2 thiếu v0.2 và tick API | Hết | "v1.0" góc dưới phải (đúng quyết định CPO, không tính mock v0.2), dấu tick xanh "API sẵn sàng" |
| Icon tạm | Đã thay | Luyện tập offline có icon mới; Điền đủ 6 dùng icon mới ở Phòng chờ (`qa-r3/a6-phong-cho.png`) |
| Dim modal | Tốt | `qa-r3/a3-vao-phong.png`: nền tối rõ, ô mã hiện "K7M2Q9" giãn chữ |
Hồi quy: A4 `qa-r3/a4-phong-cua-toi.png`, A6 không đổi; Thua/Thắng chưa chụp lại (code không đổi theo R3 ngoài hạng/thẻ, đã thấy ở R3 T-023).
Vẫn NOT VERIFIED: thẻ map và Xóa của chủ phòng (cần server thật).
