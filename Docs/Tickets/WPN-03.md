# WPN-03 — Vũ khí ném và vũ khí đặc biệt

| Trường | Giá trị |
| --- | --- |
| Giai đoạn / Deadline | Giai đoạn 3 · 31/10/2026 |
| Module | Combat (server + client) |
| Ưu tiên | P2 |
| PIC | Hào (huyhao27) |
| Phụ thuộc | CLI-03 · CLI-04 — Done; WPN-02 (HP) — Done |
| Trạng thái | Build xong, chờ review và playtest online |
| Sheet GDD tham chiếu | Weapons!13–20; Equipments!14–18 |

## 1. Mục tiêu

Người chơi dùng được 5 vũ khí ném (Chuối, Bom khói, Mảnh thiên thạch, Tinh thể, Hạt) và 3 vũ khí đặc biệt (Dây thừng, Chuông, Gậy sahur). Server quyết mọi kết quả. Mọi client trong phòng thấy cùng một cú ném, vụ nổ, làn khói, cú kéo và hiệu ứng khống chế.

## 2. Phạm vi và ranh giới

**Trong phạm vi:**

- Luật kích hoạt từng món: ngòi nổ, vùng nổ, bẫy, khói, kéo, đẩy lùi, đảo hướng, trói.
- Chống trùng request theo id.
- Tiêu đúng một món hoặc một lượt dùng mỗi lần.
- Protocol, tích hợp server, tích hợp offline và trình bày phía client.

**Ngoài phạm vi (thuộc ticket khác):**

- HP và cái chết → WPN-02. Sát thương của đồ ném đi qua `CombatService.ApplyExternalDamage`, nên vẫn qua giáp, bất tử và passive cứu mạng.
- Lưu và hết hạn hiệu ứng Stun/Root/Reverse → Game-10. Phần này nằm trong `MatchWorldRuntime._effects`, kể cả việc thuốc giải xoá hiệu ứng.
- Cách khói che tầm nhìn → FOW-01 (quyết định còn mở). Server đã có `IsInSmoke(x, y)` để FOW và bot dùng.
- Particle và cảnh báo vùng nổ → VFX-01. WPN-03 chỉ vẽ bằng art JSurviv có sẵn.

**Số liệu:** đọc từ `weapons.json`, cùng file server dùng và đã khớp hash với client. Những giá trị GDD không có cột riêng lấy mặc định trong `ThrowableRules.Baseline()`. Thêm cột vào JSON thì ghi đè được mà không phải sửa code.

| Tham số | Giá trị | Nguồn |
| --- | --- | --- |
| Chuối | 30 dmg, ngòi 1.5 s, tầm 8, 3×3, bẫy stun 1 s | weapons.json |
| Bom khói | 0 dmg, ngòi 1 s, tầm 6, 7×7, khói 10 s | weapons.json + GDD ("khói 10s") |
| Mảnh thiên thạch / Tinh thể | 35 / 45 dmg, 3×3 | weapons.json |
| Hạt | 20 dmg, trói 1.5 s (vẫn đánh được) | cột `stunSeconds` của Hạt được đọc thành Root |
| Dây thừng | 15 dmg, tầm 8, 2 lượt, CD 2 s | weapons.json |
| Chuông | 15 dmg, 7×7 quanh người dùng, đảo WASD 10 s, 1 lượt | weapons.json + GDD |
| Gậy sahur | 25 dmg, cung 90°, tầm 2.5, đẩy 3 ô, va vật cản hoặc người thì stun 2 s, 3 lượt, CD 1.5 s | weapons.json + GDD |
| Bẫy chuối còn hiệu lực | 20 s, bán kính 0.6 ô, kích hoạt sau 0.5 s | **giả định**, chờ duyệt |

**Giả định chờ duyệt:**

- Người ném không bị dính bom và bẫy của chính mình.
- Bẫy kích hoạt sau 0.5 s, để người vừa ăn nổ kịp bước ra.
- Dây thừng quăng trượt (không trúng người hay vật cản) vẫn mất một lượt.
- Đồ ném bay tới chỗ chuột, dừng trước vật cản 0.3 ô và không bay qua vật cản.

## 3. Tiêu chí nghiệm thu

| # | Tiêu chí | Cách kiểm | Kết quả |
| --- | --- | --- | --- |
| AC1 | Đủ 8 món theo config, có đủ 2 chế độ của dây thừng, bẫy chuối, hạt trói và gậy đẩy lùi | `Backend/Tests/ThrowableChecks` | 73/73 PASS |
| AC2 | Request lặp hoặc cũ không tiêu lượt, không tạo bẫy, stun hay damage lần hai; client không tự quyết HP hay effect | ThrowableChecks + WorldNetworkChecks | PASS |
| AC3 | Hai client thấy cùng cú ném, vụ nổ, cú trúng và bẫy; phòng khác không nhận event | `WorldNetworkChecks` Udp/Wss/Mixed | 33/33 mỗi chế độ |
| AC4 | Khói tuân policy đã duyệt | — | **Mở**: chờ FOW-01 chốt cách che |
| AC5 | Client dự đoán đúng khi bị đảo hướng (Chuông) | EditMode `LocalPredictionTests.Reversed_…` | PASS |
| AC6 | Demo offline đủ 8 món có hình | Playtest offline trong Play Mode (ảnh từng khung) | Đã chạy |

## 4. Kế hoạch kỹ thuật (đã làm)

| Lớp | File | Ghi chú |
| --- | --- | --- |
| Shared (.NET thuần) | `Backend/Shared/World/Throwables/ThrowableRules.cs`, `ThrowableService.cs` | Luật dùng chung cho server và offline |
| Shared inventory | `InventoryService.TryConsumeHeldUse` | Đồ ném trừ 1 món; đồ đặc biệt lưu số lượt còn lại trong `Magazine` |
| Protocol | `Backend/Shared/Protocol/Gameplay/ThrowablePackets.cs` | `ThrowableRequestPacket` (client → server), `ThrowableEventPacket` (server → cả phòng). Packet nhận diện theo hash, client cũ bỏ qua nên không đổi version |
| Server | `MatchWorldRuntime.Throwables.cs`, `MatchRuntime.World.cs`, `SessionHub.World.cs`, `CombatService.ApplyExternalDamage` | Root làm tốc độ = 0, Reverse đảo input trong bước di chuyển |
| Client | `OnlineThrowableSession`, `OfflineWorldSession.Throwables.cs`, `ThrowableRulesJson`, `LocalPrediction.Reversed` | Đảo hướng khi dự đoán theo event Chuông |
| Presentation | `ThrowableView`, `ThrowableArtCatalog` (Resources), `CombatWeaponPresenter.HandleThrowable` | Cầm đồ ném rồi chuột trái là ném tới chỗ con trỏ |
| Tool | `Tools/Editor/World/ThrowableArtBuild.cs` (menu **Jungle Maze → Art → Build Throwable Art**) | Sheet nổ chuối của JSurviv là texture thường, nên được copy và cắt 8 khung trong `Art/Combat/Throwables`; bản gốc không bị sửa |

**Còn mở:**

- Policy che khói (FOW-01).
- Bot chưa biết dùng đồ ném: chấm điểm 0 nên không chọn cầm.
- Vụ nổ chưa gây sát thương cho rương và chum.
- Icon choáng trên đầu người bị stun (VFX-01).
- Server AWS phải deploy lại thì online mới có WPN-03.

## 5. Nhật ký build

- 2026-11-05:
  - Backend: ThrowableChecks 73/73. WorldNetworkChecks Udp/Wss/Mixed 33/33 mỗi chế độ. CombatChecks 27, WorldChecks 168, MatchChecks 133, BotChecks 192, InventoryChecks 83 đều PASS.
  - Unity EditMode 967/973: 6 fail là test map/LevelEditor có từ trước, do commit `b153b6ed` đổi dữ liệu map.
  - Playtest offline thật trong Play Mode cho cả 8 món.
