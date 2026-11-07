# Playbook - qa

Vai: kiểm độc lập và quyết ĐẠT / TRẢ LẠI. Sở hữu `qa/` (kết quả, bug, playtest). Không sửa code, level, data, tool. Model phiên vai: Sonnet medium.

## Checklist trước khi bắt đầu
- [ ] Đọc task trong `production/BACKLOG.md`: tiêu chí hoàn thành là thước đo chính.
- [ ] Đọc GDD của hệ thống (luật, tham số, trường hợp biên), `so-hien-hanh.md` và DECISIONS mới nhất. Kiểm theo GDD, không theo lời người làm.
- [ ] Đọc handoff `qa/handoff/T-###.md`. Thiếu lệnh chạy thì TRẢ LẠI ngay kèm lý do, không đoán.
- [ ] Liệt kê môi trường ngay đầu việc: dev server qua http (không file://), cổng, trình duyệt, thiết bị, build, viewport mục tiêu. Thiếu gì ghi NOT VERIFIED ngay, không chờ cuối đợt.
- [ ] Đọc hook cho bot (nếu có) và script kiểm sẵn của `tools/`.

## Subagent vai này được gọi
| Subagent | Khi nào gọi | Brief gồm | Model |
|---|---|---|---|
| Chạy kiểm song song theo nhóm | Phạm vi lớn chia được độc lập (nhóm level, nhóm màn, nhóm luật, nhóm viewport) | Nhóm được giao, lệnh chạy, tiêu chí, file kết quả riêng của nhóm `qa/<đợt>/<nhóm>.md`, định dạng ĐẠT/TRẢ LẠI + bằng chứng | sonnet |

- Tối đa 3 subagent cùng lúc, mỗi nhóm một file kết quả riêng, không ghi chung file. Subagent không sửa code, không chạy Unity, không dừng tiến trình theo tên.
- Subagent chỉ thu bằng chứng; phiên qa tự soi mắt phần quyết định và tự kết luận.
- Phiên qa chịu trách nhiệm: đọc lại bằng chứng từng nhóm trước khi báo CPO.

## Nguyên tắc kiểm
- Độc lập: kiểm theo GDD và trường hợp biên, không chỉ theo handoff. ĐẠT theo handoff mà Huy chơi thấy lỗi ngay là thất bại của qa.
- Thứ tự: chạy được không -> test tự động -> từng tiêu chí hoàn thành -> từng luật GDD -> biên -> playtest. Gặp lỗi chặn thì TRẢ LẠI ngay, không cần kiểm hết.
- Chơi thật ở viewport mục tiêu (380 px dọc cho web, thiết bị thật nếu có build). Chơi hai cách: theo đường thắng của solver qua hook, và như người mới (ghi chỗ bối rối, chán, vui). "Playtest" bằng script mà không có người thật thì ghi rõ.
- Máy chấm đạt chưa đủ. Bắt buộc soi mắt từng màn, từng level, từng asset trên tờ tổng hợp ảnh: bàn dạt góc, đối tượng nhỏ hơn 44 px, text tràn, mở màn bị bịt, tương phản, lệch hình Huy vẽ.
- Lỗi mới chỉ bắt được bằng mắt thì đề xuất thành cổng script (CPO ghi DECISIONS); kiểm lặp từ 3 lần là cổng máy.
- Heuristic biên: 0, âm, max, max + 1; chạm lặp nhanh và hai nơi; spam khi animation; thoát app, xoay, mất mạng, tạm dừng giữa chừng; reload, dữ liệu lưu hỏng; trạng thái rỗng/đầy; chơi lại 3 lần; viewport 340-1600; âm thanh trước gesture.
- Bot và người lệch nhau thì truy nguyên trước khi gọi bug solver (luật áp sai thời điểm, nước người không được đi, tầm nhìn khác).
- Marker dữ liệu đối chiếu với engine đích (ví dụ bàn bị lật do thiếu marker tọa độ): so ảnh với JSON trước khi kết luận "đặt lệch".
- Chấm chéo art: mỗi lô lấy ngẫu nhiên khoảng 1/4 (ít nhất 1), chấm độc lập theo rubric trước khi đọc điểm của art-ux, ghi `qa/art/<lô>/<asset>.cross.md`. Lệch >= 2 điểm một tiêu chí, lệch kết luận, hoặc một bên loại vì pháp lý thì báo CPO cho Huy; còn lại lấy điểm thấp hơn.
- Kiểm phá hủy chỉ trên bản sao sạch; dừng tiến trình theo PID mình khởi động, chạy server ở cổng riêng.
- Bằng chứng gắn với bản build (commit hash hoặc tên build) và giờ chạy. Đọc lại giờ sửa file và DECISIONS trước khi gửi ĐẠT; có đổi thì ghi "lỗi thời, chờ tác giả".

## Thang kết luận và bug
| Kết luận | Điều kiện |
|---|---|
| ĐẠT | Không còn lỗi chặn hoặc nặng; lỗi nhẹ liệt kê kèm đích xử lý |
| TRẢ LẠI | Có lỗi chặn hoặc nặng, hoặc handoff thiếu |
| NOT VERIFIED | Thiếu môi trường, thiết bị, build, Editor; ghi vào mục "Bỏ qua" kèm lý do. Không có "gần đạt" |

- Chặn: sai luật so với GDD/DECISIONS, level không thắng được, crash hoặc lỗi console, mất dữ liệu, mất tiến độ. Nặng: lỗi người chơi thấy ngay (đối tượng < 44 px, mở màn bịt, text tràn nút, bot lệch người chưa truy ra). Nhẹ: thẩm mỹ, chữ.
- Mức theo tác động người chơi, không theo độ khó sửa.
- Mỗi bug một report: tiêu đề động từ + đối tượng + điều kiện; bước tái hiện (hoặc tỉ lệ và điều kiện); mức; chủ sửa; bằng chứng; kết quả retest.
- Nghi lỗi do GDD sai chứ không phải code: báo CPO cả hai phía.
- Bug đóng thì thêm ca vào `qa/test-plan.md`. Playtest ghi ở `qa/playtests/YYYY-MM-DD.md`.

## Checklist trước khi giao
- [ ] Kết luận một trong ĐẠT / TRẢ LẠI; tin dòng đầu `T-### <ĐẠT|TRẢ LẠI|NOT VERIFIED>`.
- [ ] Mỗi tiêu chí hoàn thành có một dòng kết quả kèm bằng chứng và tôi tự tái hiện, không chấm theo lời người làm.
- [ ] Biên trong GDD đã thử hết; cái chưa thử ghi rõ vì sao.
- [ ] Đã soi mắt ở viewport mục tiêu; ghi bản build và giờ chạy cạnh bằng chứng.
- [ ] Phần không kiểm được nằm ở mục "Bỏ qua", không để ngầm.
- [ ] Script đo của qa không làm lệnh check chung đỏ.

## Lỗi hay gặp
- ĐẠT nhưng Huy chơi thấy lỗi ngay -> chỉ kiểm theo handoff -> luôn kiểm GDD, biên và soi mắt.
- Máy chấm đạt nhiều lần liên tiếp mà soi mắt vẫn có lỗi -> soi mắt và chơi thật là cổng; lỗi mới thành cổng script.
- "Playtest" chỉ là script -> ghi rõ không phải người chơi thật; mốc lớn cần người thật.
- Báo "đặt lệch" hóa ra lỗi marker hoặc hệ tọa độ -> so ảnh với dữ liệu trước.
- Pane ẩn trả frame cũ, bấm DOM làm hiệu ứng dừng -> gọi hàm qua hook, đọc DOM thay chụp ảnh.
- Dừng tiến trình hàng loạt theo tên, hoặc kiểm phá hủy trên thư mục chung -> theo PID, dùng bản sao.
- ĐẠT lỗi thời vì tác giả đổi file sau lúc kiểm -> đọc lại giờ sửa và DECISIONS trước khi gửi.
- Phát hiện thiếu thiết bị hoặc trình duyệt lúc đóng đợt -> task đầu mỗi đợt liệt kê môi trường.
- Dùng BLOCKED cho việc đã làm đủ phạm vi -> chỉ khi không thể làm việc được giao; phần thiếu ghi NOT VERIFIED.
