# T-027: hiện trạng cỡ chữ (đo bằng `src/do_co_chu.py`)

Nguồn: mock A (303 dòng), mock B (567), Unity prefab (353 thành phần, m_fontSize x 1,5). Chiều cao chữ hoa = 0.740 em; thân chữ d đến g = 0,972 em.

Số cỡ khác nhau: **20**, trong đó thuộc thang {20, 24, 30, 40, 96}: 5.

| Cỡ (px, 1080p) | Cao chữ hoa | Số lần | A | B | Unity | Số màn | Dùng nhiều ở | Mẫu | Thuộc thang |
|---:|---:|---:|---:|---:|---:|---:|---|---|---|
| 18 | 13.3 | 2 | 0 | 0 | 2 | 1 | Unity:Hud | / 30 | Bare Hands | không |
| 19.5 | 14.4 | 47 | 0 | 0 | 47 | 8 | Unity:MatchOverlay, Unity:Hud, Unity:LobbyUi | Adrenaline | Giáp | Di chuyển để hủy | không |
| 20 | 14.8 | 194 | 81 | 112 | 1 | 29 | A:a5-tao-phong-loi, A:a5-tao-phong, A:a7-dang-tai | Kiểm tra phiên đăng nh | Kết nối máy chủ | Máy chủ: Local 127.0.0 | có |
| 21 | 15.5 | 16 | 0 | 0 | 16 | 1 | Unity:LobbyUi | Bạn sẽ quay về màn đăn | (chu dong) | không |
| 22 | 16.3 | 1 | 0 | 0 | 1 | 1 | Unity:Hud | --:-- | không |
| 22.5 | 16.6 | 70 | 0 | 0 | 70 | 3 | Unity:LobbyUi, Unity:MatchOverlay, Unity:SpectatorRow | (chu dong) | Mã phòng (phòng mới: 6 | Tên đăng nhập (3–32) | không |
| 24 | 17.8 | 576 | 113 | 321 | 142 | 38 | Unity:LobbyUi, B:b7-thua, B:b6-thang | Đang kết nối máy chủ.. | Đăng xuất | Huy | có |
| 25.5 | 18.9 | 38 | 0 | 0 | 38 | 2 | Unity:LobbyUi, Unity:MatchOverlay | (chu dong) | ​ | Bảng xếp hạng | không |
| 27 | 20.0 | 8 | 0 | 0 | 8 | 1 | Unity:LobbyUi | (chu dong) | Xóa phòng? | không |
| 28 | 20.7 | 3 | 0 | 0 | 3 | 1 | Unity:Hud | (chu dong) | không |
| 30 | 22.2 | 153 | 41 | 107 | 5 | 38 | B:b1-esc-xac-nhan-thoat, B:b2-bi-loai, B:b2-bi-loai-bo | huycv | Hủy | Vào phòng | có |
| 33 | 24.4 | 2 | 0 | 0 | 2 | 2 | Unity:LobbyUi, Unity:MatchOverlay | (chu dong) | Người chơi | không |
| 36 | 26.6 | 10 | 0 | 0 | 10 | 1 | Unity:LobbyUi | (chu dong) | Đang kết nối lại… | ​ | không |
| 39 | 28.9 | 4 | 0 | 0 | 4 | 2 | Unity:LobbyUi, Unity:MatchOverlay | Đang tải trận | (chu dong) | Vào phòng | không |
| 40 | 29.6 | 88 | 66 | 22 | 0 | 25 | A:a3-vao-phong-loi, A:a3-vao-phong-dang-ket-noi, A:a3-vao-phong | Vào phòng | Tạo phòng | Lịch sử | có |
| 42 | 31.1 | 1 | 0 | 0 | 1 | 1 | Unity:MatchOverlay | Bạn đã bị loại | không |
| 54 | 40.0 | 1 | 0 | 0 | 1 | 1 | Unity:LobbyUi | (chu dong) | không |
| 57 | 42.2 | 1 | 0 | 0 | 1 | 1 | Unity:LobbyUi | (chu dong) | không |
| 72 | 53.3 | 1 | 0 | 0 | 1 | 1 | Unity:MatchOverlay | '#4 / 10' | không |
| 96 | 71.0 | 7 | 2 | 5 | 0 | 7 | A:a6-phong-cho-cho-du-nguoi, A:a6-phong-cho-dem-nguoc, B:b2-bi-loai | Chờ | 12 | #4 | có |