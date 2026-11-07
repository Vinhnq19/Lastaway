# Definition of Done

Một task chỉ chuyển sang `done` khi đạt **mọi** mục áp dụng. CPO là người đối chiếu cuối.

## Mọi loại task
- [ ] Khoảng trống kiểm thử (trình duyệt, kịch bản chưa chạy) được CPO chấp nhận và ghi DECISIONS.md trước khi done.
- [ ] Tiêu chí hoàn thành trong `production/BACKLOG.md` được kiểm chứng bằng cách mở file hoặc chạy lệnh, không bằng lời kể.
- [ ] File đầu ra nằm đúng thư mục chủ sở hữu, đặt tên theo convention.
- [ ] Không có TODO, placeholder text, hay "sẽ bổ sung sau" trong phần được tuyên bố xong. Việc chưa làm phải nằm trong backlog.
- [ ] Quyết định phát sinh đã ghi `production/DECISIONS.md`.

## Task thiết kế (GDD, level, UX, art)
- [ ] (Chỉ khi dự án làm lại hoặc bám một game tham chiếu) Mọi chỗ khác bản gốc có mục "Lệch khỏi bản gốc": cách bản gốc làm, điểm yếu được sửa (ghi trong phân tích tham chiếu ở `docs/research/`), bằng chứng không làm game tệ hơn (sim, playtest, hoặc so sánh có lý do). Thiếu bằng chứng thì giữ cách gốc hoặc làm dạng tùy chọn.
- [ ] Theo đúng mẫu trong `docs/process/templates/`.
- [ ] Có tham số dạng bảng hoặc dữ liệu, không chôn số trong văn xuôi.
- [ ] Có mục "Yêu cầu cho engineer" đủ để implement mà không hỏi lại.
- [ ] Không mâu thuẫn với `00-overview.md` và DECISIONS.md, hoặc đã nêu rõ lý do đổi.

## Task code
- [ ] Build chạy bằng lệnh trong `docs/tech/00-stack.md`, không lỗi.
- [ ] Test cho luật lõi và trường hợp biên GDD liệt kê; toàn bộ test pass.
- [ ] Tham số đọc từ dữ liệu, text đọc từ strings, không hardcode.
- [ ] Code review của unity không còn mục "Phải sửa".
- [ ] `qa/handoff/T-###.md` tồn tại và đủ để QA chạy được.
- [ ] QA kết luận ĐẠT trong `qa/results/T-###.md`.
- [ ] CPO commit sau khi nghiệm thu (`T-###: ...`); pre-commit hook chạy kiểm BACKLOG và `npm run check` (khi đã có).

## Task sửa bug
- [ ] Bug report có bước tái hiện; sau khi sửa, QA tái hiện lại và xác nhận không còn.
- [ ] Có test chặn bug quay lại, trừ khi unity ghi rõ vì sao không cần.
- [ ] Nguyên nhân gốc ghi trong bug report, không chỉ ghi "đã sửa".
