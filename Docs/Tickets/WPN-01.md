# WPN-01 — Điều khiển và hiển thị vũ khí (client)

| Trường | Giá trị |
| --- | --- |
| Giai đoạn / Deadline | Giai đoạn 1 · 30/10/2026 |
| Module | Combat (client) |
| Ưu tiên | P0 |
| PIC | Hào (huyhao27) |
| Phụ thuộc | CLI-03 · CLI-04 · MOV-01 — Done |
| Trạng thái | Build (đã merge main; còn việc mở bên dưới) |
| Sheet GDD tham chiếu | Combat row 30/10/2026; Weapons; Hero |

## 1. Mục tiêu

Người chơi chọn một trong 10 vũ khí (tay không, 5 cận chiến, 4 súng), đánh hoặc bắn bằng chuột trái, nạp bằng R. Hiệu ứng dự đoán hiện ngay khi bấm; server xác nhận hoặc hủy. Người chơi khác trong phòng thấy đòn đánh và nạp của mình sau khi server xác nhận.

## 2. Phạm vi và ranh giới

**Trong phạm vi (client):**

- `WeaponController` (Application/Combat): chọn vũ khí, ngắm, nhịp đánh/bắn, băng đạn, nạp, dự đoán và đối chiếu với kết quả server.
- `OnlineCombatSession`: nối controller với gói `AttackRequestPacket`/`ReloadRequestPacket` và kết quả `AttackResultPacket`/`ReloadResultPacket`.
- `CombatWeaponPresenter` + `HeldWeaponView` + `MeleeSwingVisual` + `CoverProbeProjectile`: hiển thị đòn, tia đạn, shotgun 5 viên/25°, nạp và vũ khí cầm tay, cho người chơi mình và người chơi khác.
- Config đọc từ `balance/weapons.json` qua `IBalanceConfigProvider`; đổi nhịp, đạn, tầm bắn không cần sửa code.

**Ngoài phạm vi (thuộc ticket khác):**

- Xác nhận hit, sát thương, HP, stun, ammo thật → WPN-02 (server `CombatService`).
- Hiệu ứng dùng chung → VFX-01.
- Prediction/reconciliation di chuyển → MOV-02.
- Đối tượng phá được (rương, chum) nhận damage → ENV-02.

**Số liệu từ GDD:** lấy từ sheet Weapons qua `balance/weapons.json` (xem `CombatWeaponIds` và `CombatShotMath`).

## 3. Tiêu chí nghiệm thu

| # | Tiêu chí (đo được) | Cách kiểm | Ưu tiên |
| --- | --- | --- | --- |
| AC1 | Chọn, đánh, bắn, nạp từng vũ khí trong demo độc lập | EditMode `WeaponControllerTests`, `WeaponCombatConfigTests` | P0 |
| AC2 | Gói gửi đúng schema (RequestSeq tăng, Weapon, Aim) | EditMode `WeaponControllerTests` | P0 |
| AC3 | Dự đoán được xác nhận hoặc hủy khi server trả kết quả; không tự cộng HP/damage | EditMode `WeaponControllerTests` (accept/reject) | P0 |
| AC4 | Người chơi khác thấy đòn đánh sau xác nhận server (`RemoteAttackConfirmed`) | EditMode + demo Game scene | P0 |
| AC5 | Người chơi khác thấy nạp (`RemoteReloadStarted`): vũ khí cầm tay hạ xuống theo thời gian nạp | EditMode `HeldWeaponViewTests.ReloadDipPeaksMidReloadAndIsZeroAtBothEnds` | P1 |
| AC6 | Test tích hợp hai client cùng thấy sự kiện tấn công và nạp sau xác nhận | Chưa làm (cần 2 client + GameServer) | P1 |
| AC7 | Playtest thật chuột/phím: cảm giác bắn, nạp, ngắm | Chưa làm — cần người chơi thật | P1 |

## 4. Kế hoạch kỹ thuật

**Lớp và file:**

| Lớp | File | Ghi chú |
| --- | --- | --- |
| Application | `Application/Combat/WeaponController.cs` | Event `RemoteReloadStarted` đã có từ trước |
| Application | `Application/Combat/OnlineCombatSession.cs` | Sink thật: `IPacketSender` → GameServer |
| Application | `Application/Combat/Mocks/FakeCombatBackend.cs` | Chỉ đăng ký trong `UNITY_EDITOR \|\| DEVELOPMENT_BUILD` (Bootstrap), dùng cho demo offline |
| Presentation | `Presentation/Combat/CombatWeaponPresenter.cs` | Thêm `AnimateReload` cho `ReloadStarted` và `RemoteReloadStarted` |
| Presentation | `Presentation/Combat/HeldWeaponView.cs` | Thêm `OnReload(seconds)` và `ReloadDip` (hạ tay giữa lúc nạp) |
| Tests | `Tests/EditMode/World/HeldWeaponViewTests.cs` | Thêm test cho `ReloadDip` |

**Protocol / config:** không đổi. `AttackRequestPacket`, `ReloadRequestPacket`, `AttackResultPacket`, `ReloadResultPacket` giữ nguyên.

**Test đã viết / đã chạy:**

- EditMode `HeldWeaponViewTests` + `WeaponControllerTests`: 11/11 pass sau khi thêm reload visual.

**Chia commit (đề xuất, chưa commit):**

1. `Presentation/Combat/HeldWeaponView.cs` + test: reload dip.
2. `Presentation/Combat/CombatWeaponPresenter.cs`: gọi reload visual cho người chơi mình và người khác.
3. Docs ticket này.

**Rủi ro / câu hỏi mở:**

- Thay đổi `Game.unity` do Unity ghi lại (TMP normalization, ~650 dòng, commit `fa45bdab`) chưa được xử lý. Cần xem trước khi merge.
- `FakeCombatBackend` vẫn đăng ký trong Bootstrap cho editor/dev build. Kết nối thật đi qua `OnlineCombatSession`; fake chỉ dùng cho demo offline.
- Playtest online trong Play Mode bị chặn: client báo `AuthenticationRequired` khi join GameServer local (`AUTH_MODE=RoomCode`). Chưa điều tra sâu nguyên nhân.
- Cảm giác chuột/phím chưa kiểm được qua MCP.

## 5. Nhật ký build

- 2026-11-05: thêm reload visual cho người chơi mình và người khác. Compile Unity sạch (chỉ còn warning TMP có sẵn). EditMode 11/11 pass.
- 2026-11-05: Play Mode smoke test: vào scene Boot không có lỗi console. Join GameServer local bị chặn bởi `AuthenticationRequired` nên chưa vào được Game.unity để kiểm presenter online.
- 2026-11-05 (tiếp): tay theo skin, đòn cận chiến vung vũ khí trong tay theo kiểu JSurviv, đấm khi tay không, súng to hơn, súng hai tay có tay đỡ, lửa nòng ở đầu nòng. EditMode 959/966: 7 test fail đều thuộc map/LevelEditor, do commit `b153b6ed` đổi dữ liệu map. PlayMode 133/133. Đã playtest offline thật (chém rìu, bắn và nạp shotgun, đấm tay không) và chụp ảnh từng khung.
