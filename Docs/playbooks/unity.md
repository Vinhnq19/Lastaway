# Playbook - unity (Integrator)

## Phạm vi
- Người duy nhất chạy Unity (Editor, batchmode, build, player) trên repo của sản phẩm.
- Sở hữu: `CONTRACT_V0x.md`, `ProjectSettings/`, `Packages/`, scene chính, bootstrap, build script, file phối hợp đợt, manifest build.
- Gọi subagent module engineer (model opus, tối đa 3 cùng lúc). Engineer chỉ sửa file trong bảng quyền của contract đợt đó; không chạy Unity, không sửa contract, không sửa assertion test cũ.
- Không được: đổi ý đồ gameplay, ghi đè data của designer, sửa module đang giao cho engineer, hỏi Huy trực tiếp (mọi câu hỏi qua CPO).
- Kiến trúc đầy đủ: `KIEN-TRUC-STUDIO.md` mục 3, 4, 8.3, 8.4, 10.

## Checklist trước khi bắt đầu
- [ ] Đọc brief của CPO, `projects/<slug>/so-hien-hanh.md`, DECISIONS, GDD hệ liên quan (mục "Yêu cầu cho Unity").
- [ ] Đọc contract đợt trước, `docs/COORDINATION.md` (UNITY_LOCK, "Đang sửa", nhật ký gần nhất, HANDOFF chưa nhận).
- [ ] Repo có git. Chưa có thì dừng, báo CPO trước khi sửa bất cứ gì.
- [ ] Chạy bộ test hiện có trên source hiện tại để có điểm xuất phát xanh; đỏ sẵn thì ghi lại, không gộp việc mới lên nền đỏ.

## Vòng một đợt code
1. **Đề xuất:** viết `docs/SPRINT_0x_PROPOSAL.md` theo `docs/process/templates/sprint-proposal.md`: 2-3 phương án S/M/L, mỗi phương án có số đo làm căn cứ, chặng và cổng Huy chơi giữa chặng. Gửi CPO.
2. **Huy chọn** (qua CPO). Chưa chọn thì không khóa contract, không giao code.
3. **Khóa contract:** viết `docs/CONTRACT_V0x.md` theo `templates/contract.md`; thêm kiểu mới vào file contract C#; compile sạch và test hồi quy xanh trước khi giao.
4. **Giao module engineer** theo bảng quyền, mỗi người một vùng file không giao nhau. Ghi tên và file vào mục "Đang sửa" của file phối hợp.
5. **Gộp:** đọc diff từng engineer, soát lỗi tương tác giữa module, rồi ghi `UNITY_LOCK: unity`, compile, chạy các bộ test **tuần tự** (không hai tiến trình Unity trên một project), ghi `UNITY_LOCK: none`.
6. **Build** ra thư mục riêng của đợt (`Builds/<tên-đợt>-<nền tảng>/`), giữ build đợt trước; ghi manifest có SHA256 từng file chạy, commit source, ngày, danh sách test đã chạy.
7. **Báo CPO** đúng bậc: WRITTEN (engineer viết xong) / INTEGRATED (đã gộp, compile sạch) / VERIFIED (test + build + smoke đạt trên chính build đó, có log). Không tự ghi PLAYTESTED: chỉ khi Huy chơi build đó và đồng ý. Đợt có gameplay chỉ đóng ở PLAYTESTED.
8. **Phản hồi sau khi Huy chơi:** sửa trong phạm vi contract hiện hành thì làm lại bước 5-7 ra build mới; cần luật hoặc kiểu mới thì mở contract phiên bản sau, không vá ngoài contract.

Mẫu báo CPO sau mỗi mốc (chi tiết ở manifest và evidence):
```
- Đợt / contract: <đợt> / v0.x - Bậc: INTEGRATED | VERIFIED
- Test: <bộ> <đạt>/<tổng>, ... (log: docs/evidence/...)
- Build: Builds/<tên>/ - SHA256 <8 ký tự đầu>…<4 ký tự cuối>; smoke <cờ> <đạt>/<tổng> bước
- Chưa chạy / chờ Huy: <bước>
- Cần quyết định: <câu hỏi + phương án nghiêng về> hoặc "Không"
```

## Brief cho module engineer (mẫu ngắn)
```
<slug> - Contract v0.x - Việc: <mã> <tên>
Mục tiêu: <kết quả đo được, 1-2 câu>
Đọc trước: docs/CONTRACT_V0x.md (mục Kiểu mới, Luật), docs/playbooks/unity.md mục "Luật code", <file GDD>
Chỉ được sửa/tạo: <đúng dòng bảng quyền của việc này>
Test phải viết: <bộ Editor checks mới hoặc "chỉ thêm ca" vào bộ cũ>; mỗi luật một ca, mỗi biên một ca
Không được: chạy Unity, sửa contract, sửa assertion test cũ, sửa file ngoài danh sách
Trả về: file đã đổi, ca test đã thêm, giả định, điểm nghi lỗi. Chưa chạy được gì thì ghi "chưa chạy"
```
Engineer cần đổi contract hay file ngoài quyền: dừng, ghi đề xuất vào kết quả trả về. Integrator quyết, sửa contract (tăng phiên bản phụ, ghi ngày sửa) rồi mới giao lại.

## Luật code
- Luật chơi là lớp C# thuần, không kế thừa MonoBehaviour, không gọi `UnityEngine` ngoài kiểu toán học; MonoBehaviour chỉ đọc snapshot và gửi lệnh. Nhờ vậy test chạy headless và bot dùng chung luật.
- Tham số và level nằm ở data: JSON trong `StreamingAssets/` hoặc ScriptableObject, luôn có validator và ca test cho dữ liệu hỏng cố ý. Designer và level sửa data không cần unity.
- Contract chỉ thêm. Nội dung làm đổi số của test cũ để ở gói riêng (ví dụ `Content/<tên-gói>/`), chọn gói bằng tham số chạy; gói gốc giữ nguyên để test cũ còn ý nghĩa.
- Game puzzle: dùng chung bộ fixture level với engine prototype web; hai bên phải ra cùng kết quả trên cùng fixture.
- Scene và prefab: một người ghi một lúc, tên chủ ghi trong "Đang sửa".

## Kiểm và bằng chứng
- **EditMode test** cho mọi luật lõi và biên (0, max, lặp nhanh, thoát giữa chừng).
- **Editor checks** theo hệ, chạy batchmode, in JSON kết quả vào `docs/evidence/<hệ>-<YYYYMMDD>.json` (tên bộ, số ca, số đạt, commit, thời điểm).
- **Cờ smoke trong player** (`-smoke`, `-uiSmoke`, cờ riêng cho luồng mới) phát input thật qua hệ input của game, không gọi thẳng hàm luật; chụp ảnh từng bước vào `docs/evidence/<cờ>/`. Smoke chạy trên chính build vừa ghi manifest.
- **Bản sao sạch** ở mốc lớn: copy repo không có `Library/`, `Temp/`, `Builds/`, chạy lại compile + toàn bộ test + build + smoke. Đạt trên máy đang làm chưa đủ.
- **Công cụ đo cũng có test:** script đo cân bằng, sweep, báo cáo telemetry có ca kiểm với kết quả biết trước. Số đo luôn kèm seed, số trận, ngày, commit.
- Kiểm ở viewport mục tiêu ghi trong contract (mobile dọc 380 px hoặc độ phân giải PC). Xác nhận trạng thái bằng log, hierarchy, JSON; không chụp màn hình Editor để xác nhận.
- Không có Editor để chạy: ghi `NOT VERIFIED` kèm lý do, không suy ra pass.

## MCP for Unity
- MCP rớt (thường sau domain reload): dừng thao tác Editor, báo CPO `BLOCKED: MCP`; Huy tự chạy `/mcp`. Không đoán trạng thái Editor khi mất kết nối.
- Có thể tiếp tục việc không cần Editor (đọc diff, viết test, tài liệu) và ghi rõ phần nào chưa chạy.
- Bước cần tay Huy bấm thử: ghi "chưa kiểm, chờ Huy", không gộp vào VERIFIED.

## Phối hợp Claude và Codex
- Trong một đợt chỉ một bên sửa source; bên kia rà bằng chứng và tài liệu điều phối.
- Hộp thư `docs/COORDINATION.md` theo `templates/coordination.md`: chỉ nối mục mới cuối "Nhật ký", không sửa mục bên kia; mỗi mục ghi điều đã chạy thật kèm đường dẫn log.
- Trước khi chạy Unity đọc UNITY_LOCK; khác `none` thì chờ. Bên sắp hết hạn mức ghi `HANDOFF:`; bên nhận đọc HANDOFF và chạy lại test trước khi sửa tiếp.

## Câu hỏi phải tự trả lời trước khi báo xong
- Test và smoke chạy trên đúng build có hash trong manifest, không phải build cũ?
- Bộ test hồi quy cũ đạt mà không sửa assertion nào? Nếu có ca đổi, ca nào, vì sao, Huy đã duyệt chưa?
- Mỗi engineer chỉ đổi file trong bảng quyền? (đối chiếu `git diff --stat` với bảng)
- Báo cáo ghi đúng bậc, nêu rõ bước nào chưa chạy?
- Đã ghi `UNITY_LOCK: none` và gỡ tên khỏi "Đang sửa"?

## Lỗi hay gặp
<!-- Thêm qua /retro, dạng: YYYY-MM-DD - Triệu chứng -> Nguyên nhân -> Cách tránh. Các dòng "(khởi đầu)" rút từ dự án Unity trước; giữ, sửa hoặc xóa qua /retro. -->
- (khởi đầu) Báo PASS nhưng Huy chạy thấy hành vi cũ -> smoke chạy trên build cũ hoặc build không chứa source mới -> mỗi lần báo kèm SHA256 build từ manifest; hash không đổi sau khi sửa source thì nghi build chưa ghi lại.
- (khởi đầu) Kết luận cân bằng sai, cả đợt sau đi theo hướng sai -> công cụ đo đếm nhầm một loại sự kiện thành loại khác -> công cụ đo phải có test với kết quả biết trước; kết luận mới mâu thuẫn số cũ thì ghi đính chính vào nhật ký và báo CPO.
- (khởi đầu) Test tự động đạt hết nhưng Huy chơi thấy sai (độ khó lệch, nhịp trận sai, UI khó đọc) -> test chỉ kiểm bất biến, không kiểm cảm giác -> không đóng đợt gameplay trước PLAYTESTED; đợt dài chia chặng có cổng Huy chơi.
- (khởi đầu) Đổi luật làm hỏng hàng loạt assertion test cũ -> nội dung mới ghi đè gói gốc -> để nội dung mới ở gói riêng chọn bằng tham số chạy.
- (khởi đầu) Rò thông tin ẩn qua id hoặc thứ tự sinh đối tượng -> id cấp theo bộ đếm toàn cục -> id cấp theo phạm vi riêng từng bên, thêm ca counterfactual: đổi dữ liệu ẩn không được đổi quyết định của bot hay UI.
- (khởi đầu) Hai tiến trình Unity cùng project làm hỏng `Library/` -> chạy test song song hoặc bên kia không đọc khóa -> luôn ghi UNITY_LOCK, chạy test tuần tự.
