# VFX-01 — hiệu ứng giao tranh (quyết định không suy ra được từ code)

Ticket: `Docs/Tickets/VFX-01.md` (Done 07/11/2026, PIC Vũ).

- **Phạm vi bị thu hẹp sau khi merge main (07/11):** vùng bo do `ZoneOverlayView` + `ZoneOverlay.shader` của Quang Huy vẽ (đỏ, không phải "tím α 0,4" trong dòng ticket); nổ/khói/vỏ chuối/dây leo/vòng chuông do `ThrowableView` của Hào vẽ. VFX-01 **không** vẽ bo và không vẽ lại nổ/khói; chỉ nghe `IThrowableEventSource` (qua `ThrowableVfxSource`) để vẽ vòng ngòi nổ, icon choáng, hit flash/số/hạt, quầng trạng thái, rung. Người duyệt ở G1 lần 2 (D1/D2).
- **Layer:** quầng/hit spark/số ở `Default` (dưới tán cây `Foreground`) để không lộ người trốn bụi trước khi có FOW-01; nổ/ngòi ở `VFX`.
- **Chỉ dự đoán ra đòn của chính mình**, đối soát theo `RequestSeq`; hit/nổ/trạng thái chỉ hiện khi server xác nhận.
- **Seam FOW:** `IVisibilityPolicy`; Bootstrap dùng `DeferredVisibilityPolicy` (cho hiện tất cả tới khi FOW-01 đăng ký policy thật, trước hay sau `ComposeVfx` đều được). Người chơi local luôn hiện.
- **Online:** thân của mình là `PlayerView` của mình (Hero chỉ là proxy ẩn); offline là `Hero`. `PlayerView.IsBound` thêm cho việc này. Chưa chạy online thật.
- **Material Epic Toon:** `_BaseColor` 0,5 nên dùng bản sao `Art/Vfx/Materials/*_vfx.mat`. Unity tự nâng cấp material gốc trong `Assets/Imported/JSurviv/Epic Toon FX` khi chạy test → `git checkout`, đừng commit.
- **GC trong Editor:** không assert tổng frame = 0 (Editor tự tốn ~14 KB/frame); kiểm từng lệnh VFX 0 B + VFX ≤ nền. Số trên Player build chưa đo.
- **Không chạy QA** cho ticket này: người chỉ muốn QA khi tự yêu cầu.
- Prefab VFX dựng lại bằng menu "Jungle Maze/VFX/Build VFX-01 Assets" (không chỉnh tay).
