# Ảnh gameplay hiện tại (T-010, 2026-11-06)

- Commit HEAD lúc chụp: `43c41a98`; Unity 6000.3.15f1, Editor (không phải build).
- Độ phân giải: 1920x1080, Game view đặt cỡ cố định, chụp bằng `ScreenCapture.CaptureScreenshot` (có HUD Screen Space Overlay).
- Cách vào: mở scene `Assets/_JungleMaze/Scenes/Game.unity`, Play. Trận **Luyện tập offline** (`OfflineMatchSession`): một người, không bot, bo thu theo `zone_schedule.json`.
- Thao tác tạm trong runtime, không lưu vào scene: dịch nhân vật tới hòm bằng code, gọi `OfflineWorldSession.TryAttack/Pickup/SelectWeapon` thay cho chuột và phím. Camera là Follow Player của scene (`HeroCameraFollow`), không đổi.

| Ảnh | Tình huống | Thấy gì |
|---|---|---|
| `01-spawn-dau-tran.png` | Spawn, đầu trận (t ~9 s) | Nhân vật ở điểm spawn, sân lát đá, hòm/thùng/cột. HUD: máu 100/100, Giáp/Mũ/Adrenaline, quick bar trống (VŨ KHÍ, DƯỢC PHẨM), "Bare Hands", minimap, "Bo thu sau 01:21", "Còn sống 1/1" |
| `02-loot-hom-vua-pha.png` | Vừa phá hòm bằng tay không, đồ rơi ra | 3 món (gậy SAHUR_BAT, 2 mũ gỗ) nằm sát đầu nhân vật, icon rất nhỏ; khu nhà đổ có tường, cây, chậu, thùng |
| `03-nhat-do-quickbar.png` | Đã nhặt mũ gỗ và gậy, cầm gậy | Ô 1 quick bar có "Sahur B...", vitals hiện "Wooden ... 30" (độ bền mũ); nhãn vũ khí vẫn ghi "Bare Hands" |
| `04-bo-thu-hep.png` | Bo đang thu | Vòng bo hiện tại (đỏ) và bo kế tiếp (vàng) cắt qua khu nhà; "Bo thu sau 00:33" |
| `05-danh-hom-ngoai-bo.png` | Đánh hòm khi đứng ngoài bo | Nhân vật ở tư thế đánh, đứng ngoài vòng đỏ nên mất máu (85/100) |

## Không chụp được
- **Giao tranh có địch hoặc bot:** trận offline không có bot (`OfflineMatchSession` ghi rõ "without ... bots"; `Docs/Phase2_Match_Guest_Zone.vi.md` dòng 288). Bot chỉ có trong phòng mạng, cần backend Local qua Docker (máy này không có lệnh `docker`, Mongo không chạy) hoặc AWS (studio không dùng). Ảnh 05 là cảnh đánh hòm thay thế, không có địch.
- **Mở túi đồ:** game không có màn túi đồ riêng; túi đồ là quick bar dưới đáy màn (ảnh 03).
- **Hiệu ứng vung vũ khí:** gọi `TryAttack` bằng code không hiện vệt chém trong ảnh. Đánh bằng gậy SAHUR_BAT bị từ chối (`TryAttack` trả false: offline combat chỉ nhận vũ khí có trong `CombatWeaponIds`), nên các ảnh dùng tay không.

## Ghi chú cho art
- Minimap gần như tối, chưa hiện bản đồ (khớp quyết định "minimap hiện bản đồ thật" ngày 06/11).
- Nhãn vũ khí "Bare Hands" chưa đổi theo món đang cầm; quick bar trống hiện viền nét đứt mờ.
