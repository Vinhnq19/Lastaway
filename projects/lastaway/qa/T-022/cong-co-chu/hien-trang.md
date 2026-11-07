# T-027: hiện trạng cỡ chữ (đo bằng `src/do_co_chu.py`)

Nguồn: mock A (303 dòng), mock B (567), Unity prefab (389 thành phần, m_fontSize x 1,5). Chiều cao chữ hoa = 0.740 em; thân chữ d đến g = 0,972 em.

Số cỡ khác nhau: **6**, trong đó thuộc thang {20, 24, 30, 40, 96}: 5.

| Cỡ (px, 1080p) | Cao chữ hoa | Số lần | A | B | Unity | Số màn | Dùng nhiều ở | Mẫu | Thuộc thang |
|---:|---:|---:|---:|---:|---:|---:|---|---|---|
| 19.5 | 14.4 | 4 | 0 | 0 | 4 | 2 | Unity:HudSlot, Unity:HudKeyCap | R | (chu dong) | 1 | không |
| 20 | 14.8 | 255 | 81 | 112 | 62 | 32 | Unity:LobbyUi, Unity:HudInventory, Unity:MatchOverlay | Kiểm tra phiên đăng nh | Kết nối máy chủ | Máy chủ: Local 127.0.0 | có |
| 24 | 17.8 | 677 | 113 | 321 | 243 | 45 | Unity:LobbyUi, B:b7-thua, B:b6-thang | Đang kết nối máy chủ.. | Đăng xuất | Huy | có |
| 30 | 22.2 | 202 | 41 | 107 | 54 | 39 | Unity:LobbyUi, Unity:MatchOverlay, B:b1-esc-xac-nhan-thoat | huycv | Hủy | Vào phòng | có |
| 40 | 29.6 | 112 | 66 | 22 | 24 | 28 | Unity:LobbyUi, A:a3-vao-phong-loi, A:a3-vao-phong-dang-ket-noi | Vào phòng | Tạo phòng | Lịch sử | có |
| 96 | 71.0 | 9 | 2 | 5 | 2 | 9 | A:a6-phong-cho-cho-du-nguoi, A:a6-phong-cho-dem-nguoc, B:b2-bi-loai | Chờ | 12 | #4 | có |