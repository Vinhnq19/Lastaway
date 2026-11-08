# Bo kế tiếp, splash và phản hồi HP — 08/11/2026

Đã tích hợp source/prefab; các test Local dưới đây PASS. Chưa hoàn tất nghiệm thu hai client mạng thực do build Windows bị chặn.

## Thay đổi
- Viền trắng bo kế tiếp độc lập với kiểu bo hiện tại, giữ lớp phủ nguy hiểm; zoom/clipping đúng cửa sổ minimap. Đích r0 là chấm trắng, hết lịch thì ẩn. Lịch server/offline vốn đã công bố đích kế tiếp ngay sau mỗi đợt; không đổi luật hay protocol.
- Splash fantasy pixel art tạo bằng imagegen, được đóng gói cùng asset và meta. Chỉ đăng nhập/đăng ký và menu chính dùng nền này; giữ nhạc menu. AspectRatioFitter EnvelopeParent giữ tỉ lệ, crop mép khi cần; nền không nhận raycast.
- Sự kiện PlayerHealthLost lấy chênh lệch HP authoritative qua snapshot hoặc health offline, bỏ baseline/heal/packet cũ. VfxDirector dùng nguồn này thay raw damage combat để tránh số trùng, tái sử dụng flash 0,16s và pool số damage. Không đổi HP/giáp/inventory hoặc transform vũ khí.
- Đồng bộ lại rect thẻ map/notice trong prefab với builder hiện có sau khi test bắt lỗi bố cục kế thừa từ merge.

## Bằng chứng Local
- PASS: 115/115 EditMode VFX; 14/14 EditMode protocol, gồm baseline, heal, snapshot lặp và trận cũ.
- PASS: 61/61 PlayMode HUD/Lobby/VFX, gồm vòng trắng khi kiểu main tắt vòng hiện tại, r0/hết lịch, splash không chặn input, nháy đỏ/số HP thực mất một lần.
- PASS render UI: 1280×720, 1280×800, 1024×768. Ảnh cùng thư mục; preview tĩnh không thay thế playtest mạng.
- Không tính lượt test bị reload gián đoạn vào kết quả PASS.

## Giới hạn và bàn giao
- BLOCKED: BuildPipeline trong Unity bị kẹt ở import Settings/UniversalRP.asset. Không có EXE mới. Đã khởi động lại đúng Editor Lastaway; Boot mở lại, MCP chưa kết nối lại ở thời điểm bàn giao.
- NOT_TESTED: hai Unity client bản mới qua UDP/WS, hai PC vật lý, playtest online trên hai map và đủ toàn bộ thời lượng năm đợt bo. Không dùng EXE cũ để nghiệm thu source mới.
- Chưa chạy script tạo tài khoản/phòng test; không deploy AWS, không commit/push. Camera source/prefab vẫn Follow Player, spectator giữ nguyên.
- Dọn bốn file PerformanceTestRun* do test framework tạo; trả EditorSettings về baseline trước tác vụ.
- Graphify cập nhật theo source; còn cảnh báo parser UniTask và dangling edges có sẵn, không phải lỗi compile Unity.

## Kiểm nhanh sau khi Editor sẵn sàng
1. Mở Boot, Play: xem đăng nhập/menu với splash; vào luyện tập và xác nhận nhạc nền tắt trong trận.
2. Quan sát viền trắng ở minimap, zoom và bản đồ lớn. Khi co xong, viền chuyển sang bo tiếp theo.
3. Ra ngoài bo để mất 5 HP: người chơi nháy đỏ và hiện số HP mất. Hồi máu không hiện số damage.
4. Build mới rồi dùng runner hai client Local qua UDP và WS; giữ account-ticket, không bypass xác thực hoặc hash.
