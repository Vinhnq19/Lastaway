# Sổ rủi ro - Lastaway

Viết theo công thức: "Nếu <sự kiện> thì <hậu quả> vì <lý do>. Giảm thiểu: <hành động cụ thể, ai>." Mã `R-###` tăng dần, không tái sử dụng.

| ID | Rủi ro | Xác suất | Tác động | Giảm thiểu | Theo dõi | Trạng thái |
|---|---|---|---|---|---|---|
| R-001 | Nếu ưu tiên theo cột Status của GDD thì giao lại việc đã có code, vì Status chậm hơn code nhiều ngày (29/40 ghi Not Yet) | Cao | Vừa | Lấy trạng thái từ `Docs/Tickets/` và audit; CPO nhắc Huy cập nhật Sheet sau mỗi nghiệm thu | CPO | mở |
| R-002 | Nếu chép key mới từ Sheet v0.3 vào `match.json` thì game từ chối nạp config, vì `BalanceConfigJson` whitelist cứng 33 key (Sheet thêm `zoneDamagePerPhase`, `airdropWarnDuration`, `respawnEnabled`, `botTakeoverDuration`, thiếu `dragonFlyDuration`) | Vừa | Cao | Key mới chỉ vào code cùng lúc Huy chốt luật; giữ tên `dragonFlyDuration` trên Sheet. Chờ Huy đồng ý sửa Sheet | unity | mở |
| R-003 | Nếu hai bên cùng mở Unity Editor hoặc cùng sửa một file thì hỏng scene hoặc mất việc, vì mọi phiên và người dùng chung `D:\Lastaway` | Vừa | Cao | Ghi `Docs/Agent_Coordination.vi.md` mục 4 trước và sau khi dùng Editor; chỉ unity chạy Editor; song song chỉ khi không chung file | unity | mở |
| R-004 | Nếu merge của team ghi đè file ticket thì mất trạng thái nghiệm thu, như nghi ở commit "Fix conflicts" 01/11 | Vừa | Vừa | Sau mỗi lần đồng bộ main, so dòng Trạng thái ticket với `Docs/CPO_Runbook.vi.md` | CPO | mở |
| R-005 | Nếu đội AI sửa vùng của Hào, Vinh, Vũ, Quang Huy thì xung đột với người thật | Thấp | Cao | Ranh giới trong `projects/lastaway/CLAUDE.md`; backend chỉ khi Vinh đồng ý | CPO | mở |
