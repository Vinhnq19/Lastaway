# Jungle Backend

Backend .NET8 cho game lớp học:10 người/phòng,5 phòng gồm mặc định,50 phiên; mỗi account tạo tối đa2 phòng bổ sung, mã mới6 ký tự. Unity và NetSmokeClient hỗ trợ UDP/WSS/Auto. LAN/online cần đăng ký/đăng nhập và ticket v4 trước khi join; RoomCode không thay account. Offline luyện tập chạy riêng trong Unity, không API/Docker. Đọc báo cáo để biết chính xác bản đã nghiệm thu; account-flow01/11 chưa deploy AWS do cổng Local còn lỗi.

- [Hướng dẫn setup, phát triển và kiểm thử chung](../Docs/Phase1_First4Tasks.vi.md): setup lần đầu, chạy lại hằng ngày, Docker, API/Compass, AWS SSH/deploy/Start-Stop, credits, kiểm thử và giao client cho dev/tester.
- [Kiến trúc mạng đã thực hiện](../Docs/Network_Architecture.vi.md): project/module, session/identity, packet, tick và luồng triển khai.
- [Thực hành AWS: SSH, Swagger và Compass](../Docs/AWS_SSH_Swagger_Compass.vi.md): đăng nhập bằng alias `junglemaze`, test account API và xem DB qua SSH tunnel.

Local mạng cần cả GameServer/API/Mongo qua compose profile account. JWT chỉ qua API; handshake RoomCode v4 mang vé chơi ngắn hạn. AWS dùng API HTTPS/Caddy; giữ image Mongo của release hiện hành khi deploy AppsOnly, không downgrade volume tùy tiện. Lấy source đầy đủ gồm Shared/global.json/Directory.Build.props; mỗi dev tạo .env riêng.

Các hướng dẫn tài khoản/UDP tách riêng trước đây đã được gộp vào hai file này. Tra mục 3–4 cho dev local, mục 6 cho admin AWS lần đầu, mục 7 cho mỗi buổi test và kiểm soát credits; phần gameplay/account/bo và checklist hai PC ở [Phase2](../Docs/Phase2_Match_Guest_Zone.vi.md).

Tạo phòng/đo tải: **mục 14** trong hướng dẫn chung. `Tools/Enable-RoomControl.ps1` thêm secret local; `Tools/room_benchmark.py` tự tạo/dọn stack benchmark Docker riêng và xuất Markdown/JSON/CSV. Không chạy tải hoặc deploy AWS tự động.

Đo vượt chính sách trên local: `Tools/local_capacity_probe.py` build bản sao GameServer riêng, đo một phòng tăng số phiên và nhiều phòng × 10 phiên bằng `NetSmokeClient --room-plan`. Công cụ không đổi `.env`/trần 10/5/50 của bản dev, không dùng API/MongoDB hoặc AWS. Cách chạy và cleanup ở mục 14.5; [báo cáo mới nhất](../Docs/Reports/Local_Capacity_Retest_2026-10-14/report.vi.md) giữ kết quả, phương án đã áp dụng, JSON/CSV và bằng chứng thô nén. Kiểm tra room-plan đã gộp trong `Tests/NetworkChecks`, không cần script Python riêng.

**Công cụ hiện hành:** `Tests/auth_smoke.py`, các suite .NET, NetSmokeClient profile v4 và `Deploy/test-unity-remote.ps1`/`test-unity-online.ps1`. Các nhánh benchmark/probe dùng protocol1/2 là công cụ lịch sử chưa chuyển sang account-ticket, không chạy với runtime v4 bình thường hoặc dùng kết quả cũ để ký sức chứa gameplay.
