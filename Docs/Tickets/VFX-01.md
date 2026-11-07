# VFX-01 — Tận dụng prefab VFX JSurviv cho giao tranh và Safe Zone

| Trường | Giá trị |
| --- | --- |
| Giai đoạn / Deadline | Giai đoạn 3 · cột Deadline trong sheet = 46326 (serial Excel = 31/10/2026, **đã qua** so với 05/11/2026; xem câu hỏi Q12) |
| Module | Client Core |
| Ưu tiên | P2 |
| PIC | Vũ (sở hữu pooling và VFX-01: runtime, prefab VFX, vòng đời hiệu ứng) |
| Phụ thuộc | CLI-03: Done (`IPoolService`, `PooledPrefabFactory<T>`, `PoolService`, `PooledObject`, đăng ký trong `Bootstrap`). Tích hợp về sau với WPN-02/WPN-03/Game-10/ZON-01 (Not Yet trong sheet), FOW-01 (Not Yet); ticket làm độc lập bằng fake event |
| Trạng thái | Done (G3 nghiệm thu 07/11/2026) |
| Sheet GDD tham chiếu | PROJECT TRACKING (VFX-01); Assets dòng 5 (VFX hành động/nguy hiểm), 7 (Visibility/Pressure), 9; WIREFRAME dòng 11–13 (ghi "13–15" trong ticket nhưng sheet chỉ có tới dòng 13, xem Q13); Weapons; Equipments; Obstacles; MATCH DESIGN DATA (Config, ZONE BASELINE) |

## 1. Mục tiêu

Sau ticket này, mọi sự kiện giao tranh và vùng an toàn mà client nhận được đều có phản hồi hình ảnh trong thế giới 2D: nháy khi trúng đòn, số sát thương nổi, hạt va chạm, nổ ném (chuối, thiên thạch, tinh thể, hạt, chuông), khói, quầng trạng thái (choáng, trói, đảo hướng, miễn nhiễm, bất tử, tàng hình, độc), tín hiệu nguy hiểm sắp xảy ra (ngòi nổ, bo sắp thu), rung màn hình, và vùng ngoài bo tô tím α 0,4 kèm viền bo. Mọi hiệu ứng đi qua một lớp VFX riêng ở `_JungleMaze`, dùng lại `IPoolService` của CLI-03, **không phát đôi** khi cùng một sự kiện đến lặp, đến trễ hoặc bị server từ chối sau khi client đã dự đoán, **được thu hồi sạch** khi đổi phòng, và **không làm lộ** vị trí/trạng thái của mục tiêu đang bị Fog of War che (qua một seam visibility tối thiểu để FOW-01 cắm vào sau).

Kèm theo là bảng kiểm kê prefab JSurviv: cái nào dùng lại, chỉnh, loại bỏ và vì sao; và một scene trình diễn bắn fake event để kiểm từng hiệu ứng trước khi nối WPN/ZON/FOW.

Hiện trạng code đã kiểm (không giả định):

- Có sẵn trong WPN-01 và **giữ nguyên**: `MeleeSwingVisual`, `ShotFlashVisual`, `CoverProbeProjectile` (spawn trong `CombatWeaponPresenter` từ `CombatEvent.AttackPredicted` và `RemoteAttackConfirmed`); `WorldObjectView` nháy 0,16 s khi vật thể thế giới mất HP (ENV).
- Có sẵn trong UI-01 và **không thuộc ticket này**: `HitMarkerFeedback`/`HitMarkerView` (4 vạch quanh tâm ngắm), `DamageArc`, `DangerFeedback` (banner và mũi tên bom/bẫy), `EffectChipFeedback` (chip hiệu ứng của người chơi local), `HudEdgeRing`. Wireframe `48:461` ghi rõ "Hit marker: particle, số sát thương nổi thuộc VFX-01".
- Chưa có: render vùng bo trong thế giới (chỉ có HUD/minimap/Match status), số sát thương nổi, nổ ném, khói, quầng trạng thái trên thân người khác, rung camera, và bất kỳ interface visibility nào (grep `Visib|CanSee|Fog` trong `Scripts/` không có interface; chỉ có `HardCoverType`, `visionRadius` trong config).
- Protocol hiện có: `ZoneStatePacket` (thật, `MatchSession.Zone/HasZone`, có tâm/bán kính hiện tại + đích, `InWarning`, `PhaseIndex`, `ShrinkStartTick/EndTick`), `AttackResultPacket` (thật, phát cả phòng), `EventPacket` có enum `PlayerHit` (Param1 = damage) nhưng **server chưa phát** (grep `GameServer` chỉ thấy `PlayerDied`), `PlayerStateFlags.Invulnerable/Attacking`. **Chưa có** packet Explosion hay Status của người khác (`EffectKind` trong `Domain/Hud` chỉ phục vụ HUD người chơi local).
- `IPoolService` **không có** "despawn tất cả"; chỉ có `Despawn`, `Despawn(delay)`, `CountActive/CountInactive`, `Prewarm`. `PoolService.Dispose` hủy pool nhưng phải do adapter VFX tự theo dõi instance của mình để thu hồi khi đổi phòng.

## 2. Phạm vi và ranh giới

**Trong phạm vi:**

- **Kiểm kê và chọn VFX** (bảng 2.1): 16 prefab `Assets/Imported/JSurviv/Prefabs/VFX` + các nhóm Epic Toon FX liên quan; ra danh sách dùng lại / chỉnh / loại bỏ, có lý do. Không sửa file gốc trong `Assets/Imported/JSurviv/` (Team_Workflow mục 6.6): bản chỉnh là prefab mới (variant hoặc copy) trong `Assets/_JungleMaze/`.
- **Lớp VFX runtime ở `_JungleMaze`** (tech-lead quyết định cấu trúc lớp/file): nhận 5 loại event (bảng 2.2), tra bảng event → prefab, spawn qua `IPoolService`, quản vòng đời, khử trùng, thu hồi khi đổi phòng. Seam event vào bằng các struct client-side để fake và để WPN-03/Game-10/WPN-02/ZON-01 nối sau.
- **Hiệu ứng cần làm:**
  - Hit flash trên thân mục tiêu (0,16 s như `WorldObjectView`), hạt va chạm, **số sát thương nổi** trong thế giới.
  - Hiệu ứng nổ theo vùng (3×3, 7×7), khói 10 s vùng 7×7, **tín hiệu cảnh báo** ngòi nổ trong thế giới (vòng báo vùng nổ) và cảnh báo bo.
  - Quầng trạng thái trên thân: Stun, Root, Reverse, CcImmune (aura trắng), Invisible (aura mờ), Invulnerable/bất tử 2 s, Poison; Adrenaline là tuỳ chọn P2.
  - **Rung màn hình** có tôn trọng `IPlayerSettings.ScreenShakeEnabled` (UI-03).
  - **Vùng bo trong thế giới**: tô tím α 0,4 phần ngoài bo, viền bo, cảnh báo bo thu (theo `ZoneStatePacket`), cả online và offline (`OfflineMatchSession.Zone`).
- **Khử trùng và đối soát predicted/authoritative** cho mọi hiệu ứng; thu hồi pool khi đổi trận/phòng/ngắt kết nối.
- **Seam visibility tối thiểu** (mục 2.4) với mặc định "cho hiện" và bản fake để test.
- **Scene trình diễn** (đề xuất `Assets/_JungleMaze/Scenes/Dev/VfxPreview.unity`, không thêm vào Build Settings, theo mẫu `HudPreview`) có: hit, stun, smoke, explosion, warning (ngòi nổ và bo), viền bo; nút phát lặp, nút rollback, nút "đổi phòng", công tắc ẩn/hiện mục tiêu (fake FOW).
- Nhật ký kiểm kê cuối cùng (sau khi dev xem trong Editor) ghi vào mục 5 của file này.

**Ngoài phạm vi (thuộc ticket khác):**

- Luật vũ khí ném/đặc biệt: fuse, vùng nổ, bẫy, kéo/đẩy, thời lượng CC → WPN-03 (Hào). VFX-01 chỉ vẽ từ event.
- Tính sát thương, HP, sống/chết, quyết định hit → WPN-02. VFX-01 không ghi HP và không tính sát thương.
- Logic hiệu ứng trạng thái (áp, hết hạn, miễn nhiễm, chặn hành động) → Game-10. Không port damage/CC cũ.
- Policy ẩn/hiện (ngoài 12 cell, sau cover, trong bụi, trong khói, lộ khi bắn, luật ẩn thân/khói) → FOW-01 (Quang Huy). VFX-01 chỉ **hỏi** seam. Cách khói che người trong khói là quyết định FOW-01/WPN-03, VFX-01 chỉ vẽ particle khói.
- Server lọc state theo người xem (chống xem lén dữ liệu mạng) → SRV-03. Shader/VFX không phải cơ chế chống cheat.
- Tính lịch bo, tâm/bán kính, sát thương bo → ZON-01. VFX-01 chỉ vẽ theo `ZoneStatePacket`.
- HUD: hit marker, cung nhận sát thương, banner/mũi tên cảnh báo bom, chip hiệu ứng, minimap/vòng bo trên minimap, thanh HP → UI-01 (V.Huy, đã Done). VFX-01 **không gọi** `IHudStateWriter` và không sửa prefab `Hud`.
- Âm thanh cho các sự kiện này → AUD-01 (đã có `SfxRouter`/`MatchAudioEventSource`). VFX-01 không phát SFX.
- Muzzle flash, vung cận chiến, đạn bay → giữ ở WPN-01 (`CombatWeaponPresenter`). VFX-01 chỉ cung cấp dedupe nếu WPN-01 muốn dùng chung, không viết lại.
- Phản hồi vỡ rương/chum, cửa đá, portal, đầm lầy/lava/ổ gai (hình ảnh vật thể) → ENV-02/ENV-01 (`WorldObjectView`). Chỉ **quầng trạng thái** (Poison/Slow) trên người chơi nằm trong VFX-01.
- Art pixel mới do AI render / thay art hàng loạt → ART-04/ART-05. VFX-01 chỉ chọn và chỉnh asset sẵn có.
- Trứng rồng/airdrop, rồng bay → DRG-01 (chưa có ticket VFX riêng; ghi nhận).
- Killcam và spectate phát lại VFX → UI-03. Killcam hiện chỉ có dữ liệu bo (`KillcamFramePayload`).
- Đổi protocol (thêm `PlayerHit` thật, Explosion, Status packet, id sự kiện) → WPN-02/WPN-03/Game-10/SRV-03, **cần G2**; ticket này không đổi protocol.
- Sửa `IPoolService` (thêm "despawn all") → follow-up CLI-03 nếu người muốn; mặc định adapter tự theo dõi instance.

### 2.1. Kiểm kê VFX JSurviv

> **Quyết định cuối (sau build 07/11/2026):** cột "Quyết định" dưới đây là bản trước merge. Bản chốt sau khi xem trong Editor nằm ở **mục 5.3** (theo 4.12.4).

Nguồn kiểm: đọc file prefab/material (YAML), shader URP trong `Library/PackageCache`, `ProjectSettings/TagManager.asset`. Sorting layer `-1678964477` = layer **VFX** (id 2616002819 unsigned); `0` = `Default`. **Chưa mở Editor**: mọi mục "cần dev xác minh trong Editor" phải được dev chốt khi xem bằng Play preview và ghi lại ở mục 5. 1 world unit = 1 cell = 48 px (toạ độ server theo cell).

Chung cho toàn bộ: material hiệu ứng dùng shader `Universal Render Pipeline/Particles/Unlit` (guid `0406db5a…`, có trong URP 17.3), nên **không phải shader pink**; 9 material Demo dùng URP `Lit` (chỉ trong `Epic Toon FX/Demo`, không dùng). Root scale (1,1,1). Đa số prefab ở sorting layer `Default`, order 0, tức **sai layer** cho VFX (project có layer `VFX`); chỉnh trong bản `_JungleMaze`.

| Prefab | Gồm | Sự kiện/trạng thái GDD dùng | Quyết định | Lý do và việc chỉnh |
| --- | --- | --- | --- | --- |
| `SmokeVfx` | 1 PS, sprite sheet `smoke-Sheet.png` (art pixel trong `_JungleMaze/Art/JSurviv/Weapons/Sprites/Bom khói`), billboard, layer VFX order 10, `looping 0`, **`stopAction 2` (Destroy)** | Explosion: SMOKE (khói 10 s, vùng 7×7) | **Chỉnh** | Đúng phong cách pixel. Phải đổi `stopAction` sang None (pool không được để particle tự Destroy), căn kích cỡ về vùng 7×7 cell, thời lượng 10 s theo Weapons (loop hoặc tái phát tới hết), không để `playOnAwake` chạy trước `OnSpawned`. |
| `BananaExploitVfx` | 1 PS, sprite sheet `banana_explosing-Sheet.png` + `BananaExplosingSheet.mat`, billboard | Explosion: BANANA (3×3), sau đó bẫy stun 1 s | **Dùng lại** (chỉ gom vào pool + layer VFX) | Sheet pixel sẵn của JungleMaze; `looping 0`. Kiểm kích cỡ so với 3×3 cell. |
| `MeteoExploitVfx` | 1 PS, sprite sheet `meteorite_explosion-Sheet.png` + `MeteoExplosing.mat` | Explosion: METEORITE_FRAGMENT (3×3), CRYSTAL (3×3, tinh thể) | **Dùng lại** cho Mảnh thiên thạch; **chỉnh** (đổi màu/tint) cho Tinh thể | Weapons ghi Tinh thể "cần phân biệt vai trò với Mảnh thiên thạch"; đề xuất tint xanh làm bản tạm (Q5). |
| `SeedExploit` | 2 PS: `Droplets` (render mode 1 = Stretched Billboard) + `cloud_2x2_hard.mat` | Explosion: SEED (3×3, root 1,5 s) | **Chỉnh** | Giọt nước kéo dài trông 3D/sai hợp với hạt; đổi sang sprite pixel, màu xanh lá, giới hạn số hạt; cần dev xác minh trong Editor. |
| `BellExploit` | 6 PS + 1 `Light`, Nova (mesh sphere 10210 / billboard), Glow, Sparks, `lightning_spritesheet_3x3.mat` | Explosion: BELL (vùng 7×7 tâm bản thân, đảo hướng) | **Chỉnh nặng hoặc thay** | Quá rối (6 hệ), có `Light` 3D (không dùng trong 2D URP: bỏ), có mesh. Giữ tối đa 1 vòng nova + vài tia, bỏ Light, nhắm 7×7. Nếu không đủ đơn giản thì thay bằng một Epic Toon 2D nova. |
| `StunnedVfx` | 2 PS: `Stars` (billboard) + hệ gốc render **mesh sphere** (mode 4), có `ETFXRotation` (script demo), loop, layer Default | Status: Stun (trên đầu) | **Chỉnh** | Sao quay là hợp ý; bỏ mesh sphere và bỏ script `ETFXRotation` (không dùng script nhập; việc xoay do particle/adapter), chuyển sang layer VFX, gắn vào đầu thân; thời lượng do event quyết (1 s, 2 s, 5 s). |
| `ReverseVfx` | 2 PS: ReverseVfx + `Glow` (`glow.mat`), loop | Status: Reverse (Chuông 10 s) | **Chỉnh** | Hợp làm quầng trên đầu; gọn (2 hệ). Cần đổi màu để không lẫn với Immune/Invisible; kiểm scale. |
| `ImmortalVfx` | 2 PS: `MagicSparkles` (`sparkle_ADD.mat`) + hệ **mesh sphere** (mode 4, `etfx_shield`), loop | Status: bất tử 2 s (`PlayerStateFlags.Invulnerable`, Tiên dược sự sống) | **Chỉnh** | Cầu 3D không hợp top-down pixel; thay bằng vòng tròn/aura 2D, giữ sparkle. File **giống hệt** `ImmunityVfx` (cùng cấu trúc, 9618 dòng): chỉ cần một gốc, hai biến thể màu. |
| `ImmunityVfx` | như trên | Status: CcImmune (Thần dược, 7 s, "aura trắng") | **Chỉnh** (biến thể của Immortal, màu trắng) | Equipments dòng 13: aura trắng cho người xem. |
| `PoisonsVfx` | 3 PS: `Skull`, `cloud_2x2_hard.mat`, `Droplets` | Status: Poison (Ổ gai, 3 s) | **Chỉnh**, ưu tiên P2 | Cần cho `EffectKind.Poison`; Skull có thể lệch phong cách, giảm lại còn đám mây xanh + giọt, cần dev xác minh trong Editor. |
| `BleedVfx` | 3 PS (Trail, `BigGore`, loop), `circle.mat` | Không có trạng thái chảy máu trong GDD/`EffectKind` | **Loại bỏ** | Không có luật; phong cách gore sai tone. Hit spark dùng nhóm Epic Toon 2D. |
| `BlindVfx` | 2 PS: `Cloud` (`cloud_2x2_soft.mat`), loop | Không có "mù" trong GDD (che tầm nhìn do khói/bụi thuộc FOW-01) | **Loại bỏ** | Dễ bị nhầm với khói và đụng quyền FOW-01. |
| `BuffDamageVfx` | 2 PS, hệ gốc **mesh** (mode 4, `etfx_shield`) | GDD không có buff sát thương | **Loại bỏ** | Không có luật tương ứng; mesh 3D. (`TinySparkles` là ứng viên cho Adrenaline nếu người muốn P2, Q9.) |
| `BurnedVfx` | 4 PS + 1 `Light`, prewarm, scaling Shape | GDD không có trạng thái cháy; Lava là hazard của ENV, không phải trạng thái người chơi | **Loại bỏ** | Có `Light` 3D, 4 hệ rối; không có sự kiện nào cần. |
| `CantUseWeaponVfx` | 3 PS + 1 `Light`, mesh portal (mode 0 mesh `cdd7228b…`) | Stun "không dùng được vũ khí": HUD đã có chip/khoá | **Loại bỏ** | Hình portal sai ngữ nghĩa, có `Light`. Phần báo "không thể dùng" thuộc HUD (UI-01). |
| `WaterVfx` | 1 PS stretched billboard, loop | Không có nguồn | **Loại bỏ** | Không có sự kiện GDD; vật liệu `circle.mat` chung. |

**Epic Toon FX** (600 prefab; `Prefabs/Combat/{Blood,BombFuse,Explosions,Muzzleflash,Nova,Sword,Text,...}`, `Prefabs 2D/{Blood,Explosions,Water,Weather}`, `Prefabs/Misc`; 116 material dùng URP `ParticlesUnlit`):

| Nhóm | Dự kiến | Lý do |
| --- | --- | --- |
| `Prefabs 2D/Explosions` (`MetalHit2D`, `SparkExplosion2D`, `StarBurst2D`) | **Ứng viên** cho hit spark (HitConfirmed): chọn 1, chỉnh màu/scale | Bản 2D, nhẹ. Cần dev xác minh trong Editor phong cách và scale so với sprite 48 px. |
| `Prefabs/Combat/BombFuse` (`BombFuse`, `BombFuse2`) | **Ứng viên** cho cảnh báo ngòi nổ trên quả đang cháy | Cần xác minh trong Editor (có thể 3D/có Light). |
| `Prefabs/Combat/Explosions/- Misc/StunExplosion`, `FlashBomb`, `StarBurst` | Ứng viên tuỳ chọn cho Stun-on-apply / hit đặc biệt | Chỉ dùng nếu tiết kiệm công; không bắt buộc. |
| `Prefabs/Combat/Nova`, `Explosions/*` còn lại | **Không dùng** trừ khi thay cho `BellExploit` | Nhiều biến thể 3D phép thuật, lệch pixel-art. |
| `Prefabs/Combat/Text` (`KaPow`, `Pow`, `Zap`) | **Loại bỏ** | Chữ hoạt hình, không phải số sát thương; số sát thương dùng text tự dựng (Q6). |
| `Prefabs/Combat/Blood`, `Prefabs 2D/Blood` | **Loại bỏ** | Không có yêu cầu máu trong GDD; Hit spark thay thế. |
| `Prefabs/Combat/Muzzleflash`, `Sword`, `Missiles`, `Flamethrower`, `Magic` | **Loại bỏ** | WPN-01 đã có muzzle/swing; không có vũ khí tương ứng. |
| `Prefabs 2D/Water`, `Weather`, `Prefabs/Environment`, `Interactive`, `Misc` | **Loại bỏ** | Ngoài phạm vi (môi trường/ENV). |
| `Demo/` và mọi script `ETFX*` | **Không dùng** | Script demo, material `Lit`. Không copy script nhập (rule: "không copy script mạng cũ"; tương tự). |

Tiêu chí chọn chung (để dev loại nhanh): không `Light`, không mesh 3D (sphere/portal), không `stopAction=Destroy`, tối đa 4 `ParticleSystem` mỗi prefab (giả định), hình và màu đọc được ở kích thước 48 px/cell, không lẫn nghĩa giữa các trạng thái, nhìn ở nền xanh rừng và nền map.

### 2.2. Ánh xạ event → hiệu ứng → nguồn dữ liệu

Khoá khử trùng ("event key") ghi ở cột 4. Tất cả hiệu ứng "đã xác nhận" chỉ phát khi có event authoritative; ngoại lệ duy nhất là hiệu ứng ngắn của **người chơi local** đã có ở WPN-01.

| Event | Hiệu ứng | Nguồn dữ liệu (hiện có / thiếu) | Khoá khử trùng | Visibility |
| --- | --- | --- | --- | --- |
| **AttackStarted** | Hạt/lóe đánh trúng không thuộc ở đây; phần vung/muzzle đã có ở WPN-01. VFX-01 thêm: bụi/khoảnh khắc ra đòn **của người khác** khi cần và cung cấp đối soát predicted/confirmed | `CombatEvent.AttackPredicted` (local, đoán), `AttackConfirmed` (local, server xác nhận), `RemoteAttackConfirmed` (người khác), `AttackRejected` (huỷ). Gốc: `AttackResultPacket{RequestSeq, Weapon, Outcome, Tick, NetId}` thật | `(NetId, RequestSeq)` | Remote: hỏi seam theo vị trí người bắn (`IPlayerPoseSource`) |
| **HitConfirmed** | Hit flash thân mục tiêu 0,16 s; hạt va chạm; số sát thương nổi; rung nhẹ nếu mục tiêu là người chơi local | **Thiếu**. `EventPacket.PlayerHit` (enum có, Param1 = damage) chưa được server phát; chưa có packet trúng vật thể/người kèm vị trí. Cần WPN-02. Ticket dùng struct fake `HitConfirmedEvent{EventKey, TargetNetId, Damage, WorldPos}` | `EventKey` (tick + nguồn + đích + seq) | Hỏi seam theo vị trí mục tiêu: ẩn thì **không** spawn (kể cả số, hạt, rung) |
| **Explosion** | Hai pha: (1) **cảnh báo** vòng vùng nổ trong thế giới trong lúc ngòi cháy; (2) nổ theo vũ khí và vùng: BANANA/METEORITE/CRYSTAL/SEED 3×3, BELL 7×7; SMOKE: nổ rồi vùng khói 10 s (7×7); bẫy chuối còn lại sau nổ | **Thiếu** (WPN-03/Game-10 Not Yet). Struct fake `ExplosionEvent{EventKey, WeaponId, Center, AreaCells, FuseEndTick, Phase}` | `EventKey` + `Phase` | Hỏi seam theo tâm vùng nổ |
| **Status** | Quầng/attach lên thân: Stun, Root, Reverse, CcImmune, Invisible (aura mờ), Invulnerable (bất tử), Poison; gỡ khi hết hạn hoặc nhận event gỡ | **Một phần**: `PlayerStateFlags.Invulnerable` có trong snapshot; `EffectKind` (Stun=5, Root=6, Reverse=7, CcImmune=8, Invisible=4, Poison=2, Slow=3, Adrenaline=1) chỉ ghi cho HUD local. Cần Game-10 phát cho người khác. Struct fake `StatusEvent{EventKey, NetId, Kind, Applied, DurationSeconds, StartTick}` | `(NetId, Kind)`; áp lại chỉ làm mới hẹn giờ, không tạo instance thứ hai | Hỏi seam theo thân mục tiêu; mục tiêu bị che thì ẩn (xem 2.4) |
| **ZoneState** | Vùng ngoài bo tô tím α 0,4; viền bo (đứt nét theo Figma); khi `InWarning` hiện vòng báo ở bán kính đích (bo sắp thu); thu mượt theo `ShrinkStartTick/EndTick` | **Thật**: `ZoneStatePacket` (`MatchSession.Zone`, offline `OfflineMatchSession.Zone`): `CurrentCenterX/Y`, `CurrentRadius`, `TargetCenterX/Y`, `TargetRadius`, `InWarning`, `PhaseIndex`, `ShrinkStartTick/EndTick`, `Tick` | `(MatchId, Tick)` (bỏ gói cũ như `MatchSession` đã làm); cảnh báo là cạnh lên, mỗi `PhaseIndex` một lần (cùng cách `MatchAudioEventSource`) | Bo là thông tin chung (mọi người đều thấy), không qua seam |

Ghi chú nguồn thiếu: ticket **không đổi protocol**. Các fake event chỉ là hợp đồng phía client; khi WPN-02/WPN-03/Game-10 chốt packet thì thêm một adapter nhỏ (G2 thuộc ticket của họ).

### 2.3. Ranh giới dự đoán và đối soát

- **Hiệu ứng ngắn của người chơi local** (muzzle, vung) được dự đoán ngay khi bấm (`AttackPredicted`), như WPN-01 đang làm. Khi server trả `AttackConfirmed` cùng `RequestSeq` thì không phát lại; khi `AttackRejected` (Cooldown, NoAmmo, Duplicate, RateLimited…) thì thu hồi phần VFX còn sống của seq đó trong cùng frame.
- **Hit, nổ, trạng thái, bo không bao giờ được dự đoán**: chỉ phát khi có event authoritative (đơn giản hơn và không thể phát đôi). Khuyến nghị giữ (Q4).
- Event trùng khoá (gói lặp, gói resend, replay sau reconnect) bị bỏ; bộ nhớ khoá giới hạn (giả định 64 khoá gần nhất, hoặc 5 s theo tick) và **xoá khi đổi phòng/trận** (`MatchId` đổi, hết `InGame`, ngắt kết nối).

### 2.4. Seam visibility tối thiểu (giả định, chốt ở G1)

FOW-01 chưa có và là chủ policy. VFX-01 chỉ cần một điểm hỏi duy nhất, ví dụ ở mức ý niệm (tech-lead đặt tên/chữ ký): "với vị trí thế giới P (và NetId nếu là thân người), người chơi local có được phép thấy dấu hiệu này không?". Quy tắc:

- **Mặc định (chưa có FOW-01): cho hiện** (`AlwaysVisible`). Có bản fake ẩn/hiện điều khiển được cho test và scene preview.
- Người chơi local luôn thấy hiệu ứng của chính mình.
- Phải hỏi seam ở **mọi** hiệu ứng có thể làm lộ: số sát thương nổi, hit spark, quầng trạng thái (kể cả aura tàng hình), cảnh báo ngòi nổ có vị trí, vụ nổ/khói của người ném, rung màn hình do nổ xa. Vùng bo thì không.
- Hiệu ứng gắn lâu (quầng trạng thái) phải hỏi lại khi trạng thái visibility đổi: bị che thì ẩn/thu hồi trong ≤1 frame mà không giữ vị trí cuối; hiện lại thì đúng một instance.
- Khi ẩn, **không** dùng mức thấp hơn để lộ ("mờ", "chấm"): trừ khi FOW-01 chủ động trả về "hiện mờ" (ví dụ Gậy thần: "Aura mờ cho người xem", Equipments dòng 14); đây là quyết định của FOW-01.
- Đây không phải biện pháp chống cheat (SRV-03 lọc state).

## 2.5. Số liệu từ GDD

Nguồn mặc định: sheet **Weapons**, **Equipments**, **Obstacles**, **MATCH DESIGN DATA** (đọc bằng `read_gdd.py`) và JSON `Assets/StreamingAssets/JungleMaze/balance/match.json` (khớp GDD cho các khoá bo; `weapons.json`/`equipments.json` chưa đối chiếu từng dòng trong lượt này, xem Q11). Mọi baseline GDD đều "Baseline cũ - chưa duyệt".

| Tham số | Giá trị | Nguồn |
| --- | --- | --- |
| 1 cell = 1 world unit = 48 px; map 50×50 cell | 48 px | MATCH DESIGN DATA `cellSize`, `mapSize`; `match.json` khớp |
| Bán kính nhìn (FOW) | 12 cell | MATCH DESIGN DATA `visionRadius` |
| Mục tiêu hiệu năng | 60 fps, 10 nhân vật cùng xả đạn | MATCH DESIGN DATA `targetFPS` |
| Hình dạng bo | `circle` | MATCH DESIGN DATA `zoneShape` |
| Thời gian cảnh báo bo | 10 s | `zoneWarningTime` |
| Tốc độ co | 1,0 cell/s | `zoneShrinkSpeed` |
| Lịch bo (bán kính từ → đến, s) | P1 25→18 (90–97); P2 18→12 (182–188); P3 12→7 (268–273); P4 7→3 (343–347); P5 3→0 (387–390) | ZONE BASELINE (lịch cũ, chưa duyệt, ZON-01 sở hữu); `ZoneSchedule.Default()` khớp |
| Tâm bo | Đến từ `ZoneStatePacket`, VFX không tự quyết | ZON-01 |
| **Bo tím alpha** | **0,4** | **Chỉ có trong dòng ticket VFX-01, không có trong sheet GDD nào (đã tìm "alpha", "tím", "viền")** → **giả định**: phần ngoài bo tô màu `HudPalette.Purple` (#A274FF, UI-01) α 0,4; Figma `48:461` vẽ viền đứt nét tím trong thế giới và trên minimap, không có tô ngoài: xem Q1 |
| Chuối (BANANA) | ngòi 1,5 s; vùng 3×3; DMG 30; bẫy stun 1 s | Weapons dòng 11 |
| Bom khói (SMOKE) | ngòi 1 s; khói 10 s; vùng 7×7; DMG 0 | Weapons dòng 12 |
| Mảnh thiên thạch | ngòi 1,5 s; vùng 3×3; DMG 35 | Weapons dòng 13 |
| Tinh thể | ngòi 1,5 s; vùng 3×3; DMG 45 | Weapons dòng 14 |
| Hạt (SEED) | ngòi 1,5 s; vùng 3×3; DMG 20; root 1,5 s | Weapons dòng 15 |
| Chuông (BELL) | vùng 7×7 tâm bản thân; đảo hướng 10 s ("ở bản cũ"); DMG 15 | Weapons dòng 17 |
| Chuỳ gai (MACE) | stun 1,0 s | Weapons dòng 6 |
| Gậy bóng (SAHUR_BAT) | stun 2 s khi va chạm | Weapons dòng 18 |
| La bàn (COMPASS) | stun 5 s trong 9×9 (kích hoạt 2 s) | Equipments dòng 16; ITEM LIST 35 |
| Thần dược (ELIXIR) | miễn nhiễm CC 7 s, **aura trắng** cho người xem | Equipments dòng 13 |
| Gậy thần (MAGIC) | tàng hình 5 s, **aura mờ** cho người xem | Equipments dòng 14 |
| Tiên dược sự sống | bất tử 2 s sau khi cứu (`Invulnerable`) | Equipments dòng 15 |
| Nước tăng lực | +30% tốc độ trong 8 s | Equipments dòng 12 |
| Ổ gai | Poison 3 s | Obstacles dòng 14 |
| Đầm lầy | giảm 40% tốc độ (Slow) | Obstacles dòng 12 |
| Thời lượng mọi trạng thái | **Do server/event quyết** (`DurationSeconds`), bảng trên chỉ là kỳ vọng demo | WPN-03/Game-10 |
| Hit flash | 0,16 s | Giả định, khớp `WorldObjectView._flashRemaining = .16f` |
| Số sát thương nổi | trôi lên ≈0,6 cell trong ≈0,7 s, mờ dần, một event một số | **Giả định**, người chốt ở G1 (Q6) |
| Rung màn hình | biên độ tối đa ≤0,15 cell, tắt ≤0,3 s | **Giả định** (Q8) |
| Tắt rung | `IPlayerSettings.ScreenShakeEnabled` (mặc định bật) | UI-03 (`PlayerSettingsRules.DefaultScreenShake = true`) |
| Số instance VFX cùng lúc | tối đa 64 (giả định); vượt thì bỏ hiệu ứng ưu tiên thấp nhất (chấm hit/số) trước | **Giả định** |
| Dung sai kích cỡ nổ | ±15% so với vùng cấu hình | **Giả định** |
| Sorting layer VFX | `VFX` (có trong `TagManager`); `SortingLayers.cs` hiện chưa có hằng này | Ghi nhận cho tech-lead |

**Figma liên quan** (file `oeZgisxdiR80hRqbikUwPb`): `48:239` HUD phản hồi (ảnh `Docs/Wireframes/v2/48-239_hud_feedback.png`) và bản chú thích `48:461`; HUD `41:2`; style guide `39:3`. Wireframe gốc v1 `3:2` (WF-5/13). WF-11 và WF-12 chưa có frame v1. Bố cục lấy từ `48:461`: viền bo tím đứt nét ngoài thế giới, hit marker 4 vạch quanh tâm ngắm (**UI-01, không làm**), cung đỏ nhận sát thương trên vòng biên (**UI-01**), banner/mũi tên cảnh báo bom (**UI-01**). **Khối Figma bị bỏ hoặc đổi:** (a) Figma không tô tím phần ngoài bo; ticket yêu cầu α 0,4 → đề xuất tô nhẹ **và** giữ viền đứt (Q1); (b) hit marker, hướng sát thương, banner nguy hiểm không thuộc VFX-01; (c) khung nguồn là battle royale chung (squad, 100 người): không liên quan đến VFX.

## 3. Tiêu chí nghiệm thu

Cách kiểm: **EditMode** (không cần scene; pool giả hoặc pool thật qua `PoolService` với prefab test), **PlayMode** (scene preview, pool thật), **review** (đọc code/prefab), **người cảm nhận** (playtest). Mỗi test PlayMode chạy trong scene preview, chưa tải scene Game (như UI-01 mục 11). "Active" = `IPoolService.CountActive(prefab)` cộng instance VFX đang tồn tại do adapter theo dõi.

| # | Tiêu chí (đo được) | Cách kiểm | Ưu tiên |
| --- | --- | --- | --- |
| AC1 | Có bảng kiểm kê cho **cả 16** prefab trong `Prefabs/VFX` với quyết định dùng lại/chỉnh/loại bỏ và lý do, cộng quyết định cho từng nhóm Epic Toon liên quan. Mọi mục "cần dev xác minh trong Editor" được chốt sau khi xem (ảnh chụp ở mục 5). Mọi prefab "dùng lại/chỉnh" có bản tương ứng trong `Assets/_JungleMaze/`. `git diff --stat -- Assets/Imported/JSurviv` sau ticket = rỗng. | Review (bảng 2.1 + mục 5); lệnh git | P0 |
| AC2 | Scene trình diễn chạy được từ menu/phím và phát được tối thiểu **sáu** mục: hit (flash + số + hạt), stun, smoke, explosion (ít nhất BANANA 3×3 và một vụ nổ lớn hơn), warning (vòng ngòi nổ **và** cảnh báo bo), viền bo. Mỗi mục: sau khi phát, số instance active của loại đó >0; sau thời lượng (nổ ≤3 s, hit ≤1 s, stun/smoke theo thời lượng ± 0,2 s) active về 0. | PlayMode test điều khiển fake event; kiểm bằng playtest | P0 |
| AC3 | **Khử trùng:** phát cùng `EventKey` hai lần liên tiếp (và lần ba sau 1 s) cho **mỗi** loại Attack, Hit, Explosion, Status, ZoneState → số instance mới = 1 (Zone: một lần cảnh báo cho mỗi `PhaseIndex`, gói `Tick` cũ không đổi vòng). Gói đến trễ (tick nhỏ hơn đã có) bị bỏ, không kéo bán kính bo ngược. | EditMode test với pool giả/đếm, hoặc PlayMode với pool thật | P0 |
| AC4 | **Dự đoán/đối soát:** (a) `AttackPredicted` rồi `AttackConfirmed` cùng `RequestSeq` → tổng đúng 1 instance, không spawn thêm lúc confirm; (b) `AttackPredicted` rồi `AttackRejected` (mỗi giá trị `CombatOutcome` ≠ Accepted) → instance của seq đó về pool trong ≤1 frame; (c) rejected rồi Confirmed muộn của seq cũ → không phát lại. | EditMode test dùng `FakeCombatBackend`/`WeaponController` thật hoặc event giả | P0 |
| AC5 | **Trạng thái:** áp cùng `(NetId, Kind)` hai lần → 1 instance, hẹn giờ làm mới (không cộng dồn); event gỡ → về pool ≤1 frame; hết `DurationSeconds` mà không có event gỡ → về pool trong khoảng `Duration` ± 0,1 s (đồng hồ giả). Mỗi `EffectKind` ngoài bảng ánh xạ (hoặc `Kind` lạ) → không ném exception, log cảnh báo đúng 1 lần. | EditMode (đồng hồ giả) | P0 |
| AC6 | **Đổi phòng:** khi `MatchId` đổi, phase rời `InGame`, ngắt kết nối hoặc scene unload → sau tối đa 1 frame mọi hiệu ứng của adapter (`CountActive` mọi prefab VFX) = 0, các Despawn trễ đang chờ không làm lỗi (không có log `bị Despawn hai lần`), bộ nhớ khoá khử trùng bị xoá (cùng `EventKey` ở trận mới lại spawn được 1 lần). Sau đổi phòng không còn số/viền bo/quầng nào của trận cũ trên màn. | EditMode (pool thật + prefab test) + PlayMode (kéo `MatchId` giả) | P0 |
| AC7 | **Pool tái dùng đúng:** sau `Prewarm`, 100 chu kỳ spawn/despawn mỗi loại không tạo thêm instance (`CountActive + CountInactive` không tăng); không `Destroy` trong gameplay; Despawn trễ của lượt cũ không thu hồi instance đã được dùng lại (`SpawnVersion`); không có log lỗi pool. GC alloc mỗi frame = 0 B ở trạng thái ổn định với 20 hiệu ứng active sau warmup 120 frame (Profiler, như UI-01). | EditMode (đếm) + PlayMode/Profiler đo số | P0 |
| AC8 | **FOW seam:** với policy fake "ẩn" cho mục tiêu T: (a) Attack/Hit trên T → 0 spawn (flash, số, hạt, rung); (b) Explosion có tâm bị ẩn → 0 spawn; (c) Status trên T → 0 quầng; (d) quầng đang hiển thị mà T chuyển sang ẩn → renderer tắt/thu hồi ≤1 frame, chuyển lại hiện → đúng 1 instance (không nhân); (e) không có hiệu ứng nào của T còn sót khi T ẩn kể cả vệt/hạt đang bay. Mục tiêu là người chơi local và bo luôn hiện. Mặc định không có FOW-01 (`AlwaysVisible`) → hành vi như không có seam. Rung màn hình từ nổ ở vùng ẩn = 0. | EditMode với policy fake; PlayMode với công tắc ẩn/hiện | P0 |
| AC9 | **Đúng 2D/URP:** quét tự động mọi prefab VFX của `_JungleMaze`: không `Light`, không `Collider/Rigidbody`, không script từ `Imported/…/ETFX*`, không mesh 3D (sphere/portal), `stopAction` ≠ Destroy, root scale (1,1,1), z = 0, sorting layer = `VFX` (hoặc layer đã duyệt cho từng loại), mọi material có shader URP hỗ trợ (không `Hidden/InternalErrorShader`, không hồng). Mở scene preview 1 phút: console 0 lỗi/0 cảnh báo thuộc VFX. | EditMode test quét asset + PlayMode đọc console | P0 |
| AC10 | **Scale đúng vùng:** kích cỡ nhìn thấy của vụ nổ BANANA/METEORITE/CRYSTAL/SEED = 3×3 cell, BELL/SMOKE = 7×7 cell, dung sai ±15% (đo bằng bounds của `ParticleSystemRenderer` hoặc ảnh chụp có lưới 1 cell); quầng trạng thái đặt trong phạm vi thân (±1 cell) ở camera mặc định. Ảnh chụp 1280×720 đối chiếu kích cỡ so với sprite 48 px. | PlayMode đo bounds + review ảnh chụp | P1 |
| AC11 | **Vùng bo:** với `ZoneStatePacket` giả theo 5 phase của lịch mặc định: bán kính/tâm nhìn thấy đúng `CurrentRadius`/`Current Center` ±0,05 cell; điểm ngoài bo có độ phủ α **0,4** màu #A274FF (±0,01 trên thuộc tính; hoặc ±2/255 đo pixel), điểm trong bo = 0; **bốn góc map 50×50 khi r = 25 vẫn được tô**; r = 0 và r ≥ 35,4 (nửa đường chéo) không NaN/không vỡ; `InWarning = true` → vòng báo ở `TargetRadius` hiện, đúng 1 lần mỗi `PhaseIndex`; co mượt: mỗi frame ở 60 fps thay đổi bán kính ≤ `zoneShrinkSpeed`·dt + 0,05 cell và không bao giờ phình lại. Chạy được cả online (`MatchSession.Zone`) lẫn offline (`OfflineMatchSession.Zone`). Overlay không chặn raycast/ngắm (không collider, không raycast target). | EditMode (tính hình học) + PlayMode (đọc pixel/material, kéo `PhaseIndex`) | P0 |
| AC12 | **Hit flash và số sát thương:** nháy 0,16 s ± 1 frame rồi trả đúng màu/material gốc (không còn tint sau 100 lần trúng liên tiếp, không rò property block); số hiển thị đúng bằng `Damage` của event (số nguyên), đúng 1 số mỗi event, lifetime ≈0,7 s rồi về pool; hit flash/số/hạt không chạy khi mục tiêu đã chết/despawn (bỏ qua, không lỗi). VFX-01 **không** gọi `IHudStateWriter` và không đọc/ghi HP (kiểm bằng grep tham chiếu của assembly/namespace VFX); không trùng chức năng HUD (không vẽ hit marker, cung sát thương, banner). | EditMode + review grep | P0 |
| AC13 | **Rung màn hình:** `ScreenShakeEnabled = false` → độ lệch camera do VFX luôn bằng 0 tại mọi frame; bật → đỉnh ≤ giới hạn (giả định 0,15 cell), về 0 trong ≤0,3 s, nhiều event chồng không vượt giới hạn; không có độ lệch khi không `InGame`; không xung đột với `HeroCameraFollow` (camera vẫn kẹp trong map, vị trí sau khi hết rung = vị trí theo dõi ban đầu ± 0,001). Cảm giác cường độ: cần người cảm nhận khi playtest. | EditMode/PlayMode đo offset; người cảm nhận | P1 |
| AC14 | **Kiến trúc/ranh giới:** mã mới nằm dưới `Assets/_JungleMaze/`; lấy `IPoolService` qua `[Inject]`/constructor, không `FindObjectOfType`, không singleton tĩnh mới; không đụng `WeaponController`/`OnlineCombatSession`/`WorldObjectView` ngoài việc nhận event; không copy mã damage/CC cũ và không dùng script `ETFX*`; không thay đổi protocol (`git diff --stat -- Backend` rỗng). Test hiện có (EditMode/PlayMode) vẫn xanh. | Review + chạy toàn bộ test | P0 |
| AC15 | **Tải 10 người:** scene preview chạy 10 nguồn fake cùng bắn/nổ/trạng thái trong ≥60 s: số instance VFX không vượt 64, không lỗi khi chạm trần (bỏ hiệu ứng ưu tiên thấp, không bỏ cảnh báo bo/ngòi nổ/viền bo), frame time trung bình ≤16,6 ms (60 fps, `targetFPS`). Ghi cấu hình máy; số trong Editor chỉ tham khảo, chưa thay cho Player build. | PlayMode/Profiler đo số | P1 |
| AC16 | **Nhìn ra là của hiệu ứng nào:** người chơi phân biệt được Stun / Root / Reverse / Immune / Invulnerable / Poison / Invisible chỉ bằng hình khi nhìn ở khoảng cách 6 cell (không cần đọc chữ), và không nhầm hiệu ứng khói với nổ; tín hiệu cảnh báo ngòi nổ đọc ra vùng nguy hiểm trước khi nổ. | **Cần người cảm nhận khi playtest** (ít nhất 2 người, ghi kết quả) | P1 |

## 4. Kế hoạch kỹ thuật

Tech-lead viết ngày 05/11/2026. Kế hoạch theo **khuyến nghị của designer cho Q1–Q15**. Chỗ nào sẽ đổi nếu người chọn khác được đánh dấu **[Qn≠]**. Câu hỏi kỹ thuật mới T-1…T-8 nằm ở 4.10. Viết tắt đường dẫn: `Scripts/` = `Assets/_JungleMaze/Scripts/`, `Tests/` = `Assets/_JungleMaze/Tests/`, `Art/` = `Assets/_JungleMaze/Art/`, `Prefabs/` = `Assets/_JungleMaze/Prefabs/`.

### 4.0. Hiện trạng đã đối chiếu bằng code (05/11, nhánh `hvu`)

Các điểm sau đã đối chiếu với code. Có hai điểm sửa lại spec.

| Điểm | Kết quả kiểm | Hệ quả cho kế hoạch |
| --- | --- | --- |
| `EventPacket.PlayerHit` | Server chỉ phát `PlayerDied` (`Backend/GameServer/Simulation/MatchRuntime.cs:1205`) | Hit chỉ có nguồn fake. Đúng với spec |
| `PlayerStateFlags.Invulnerable` | **Server không bật cờ này trong snapshot.** `HealthService` chỉ bật `Dead`; `IsInvulnerable` chỉ dùng bên trong `CombatService` | **Sửa spec 2.2:** quầng bất tử của người khác **không có nguồn thật**, chỉ có fake. Không viết adapter đọc cờ (sẽ là code chết) |
| Trạng thái của **người chơi local** | `IEffectState` (UI-01) được ghi từ nguồn thật: online `WorldClientSession.ApplyEffect` (Slow/Poison/Stun/Adrenaline/Invisible/CcImmune), offline `OfflineWorldSession` (Slow/Poison/Adrenaline) | **Bổ sung:** quầng trên thân **mình** có nguồn thật ở cả hai chế độ. Kế hoạch có adapter chỉ đọc (`LocalEffectStatusSource`), không ghi port nào |
| `ZoneStatePacket` | Gửi kèm **mọi** frame snapshot (`MatchRuntime.cs:946`). `MatchSession` đã bỏ gói khác `MatchId` hoặc có tick cũ (`MatchSession.cs:49`). Offline dùng `ZoneSimulation` (`CurrentCenter/Radius`, `NextCenter/Radius`, `InWarning`, `PhaseIndex`, `ShrinkStart/EndTick`) với `OfflineMatchSession.CurrentTick` | Một feed đọc chung cho hai chế độ, theo mẫu `MatchSessionAudioFeed` của AUD-01 |
| `CombatEvent` | `OnlineCombatSession.Changed` (online) và `OfflineWorldSession.CombatChanged` (offline). Có `Kind`, `NetId`, `RequestSeq`, `Outcome`. AUD-01 gate theo `OfflineWorldSession.IsActive` | Cùng cách bind với `MatchAudioEventSource.BindCombat` |
| Vòng bo trong thế giới | **Đã có vòng debug**: `MatchDemoView` vẽ 2 `LineRenderer` đỏ/vàng (bo hiện tại/bo đích) cả online lẫn offline (`MatchDemoView.cs:106-107, 150-151`) | Có trùng hình với viền bo của VFX-01, xem **T-1** |
| Camera | `HeroCameraFollow` ghi vị trí ở LateUpdate (order 0). `MatchOverlayCameraDriver` ghi ở order 1000, chỉ khi đang Spectator. Online demo: `MatchDemoView` tắt follow, đặt camera tổng (25,25), size 28 mỗi Update | Rung là offset cộng thêm sau mọi bên ghi, trả lại sau khi camera render, xem 4.1 |
| `IPlayerSettings` | Chỉ được đăng ký khi có `MatchOverlaySet` (`Bootstrap.MatchOverlay.cs`) | VFX dùng `TryResolve`. Không có thì rung theo `PlayerSettingsRules.DefaultScreenShake` |
| Sorting layer | Thứ tự từ sau ra trước: `Ground, Weapon, Enemy, WorldDynamic, Default, Foreground, VFX`. **`VFX` nằm trên `Foreground` (tán cây che người)** | Quầng trên thân ở layer `VFX` sẽ vẽ đè tán cây, xem **T-2** |
| `EffectKind` | Không có giá trị bất tử. Thêm giá trị vào enum sẽ đổi `EffectPolarity` của HUD (mặc định thành Harmful) | VFX dùng enum riêng `StatusKind`, xem 4.2 |
| Prefab nguồn | Đã quét YAML: `BellExploit`, `BurnedVfx`, `CantUseWeaponVfx` có `Light`. `BellExploit`, `StunnedVfx` có 1 MonoBehaviour. `SmokeVfx` có `stopAction: 2`. Mọi prefab `playOnAwake: 1`. `scalingMode` lẫn lộn (0 Hierarchy / 1 Local / 2 Shape). Epic Toon `Prefabs 2D/Explosions/{MetalHit2D,SparkExplosion2D,StarBurst2D}` và `Combat/BombFuse/{BombFuse,BombFuse2}` **không có Light, không MonoBehaviour, không mesh** | Quy trình làm prefab ở 4.5 |

**Protocol: không đổi** (`git diff --stat -- Backend` phải rỗng). Không cần G2. Nếu thấy cần đổi thì chỉ ghi vào rủi ro (4.9), không lên kế hoạch đổi.

### 4.1. Thiết kế chính

Kiến trúc theo đúng mẫu AUD-01 đã nghiệm thu (`SfxRouter` → `SfxPlaybackService` → `IAudioOutput` do `AudioManager` ở Presentation hiện thực):

```text
Nguồn sự kiện                         Application (C# không MonoBehaviour)            Presentation (Unity)
CombatEvent (online/offline) ─┐
IEventBus: HitConfirmedEvent  ├─►  VfxDirector ── dedupe, đối soát, seam, ngân sách ─►  IVfxOutput = VfxStage ─► IPoolService (CLI-03)
           ExplosionEvent     │     │   (Domain: ledger, bảng trạng thái, budget,      │   PooledPrefabFactory<VfxParticleEffect> mỗi cue
           StatusEvent        │     │    ZoneVfxModel, ScreenShakeModel)               ├─► VfxBodyAnchor.Flash (nháy thân)
IVfxMatchFeed (bo, phiên) ────┘     ├─► ZoneVfxModel ─────────────────────────────────►  ZoneOverlayView (tô tím + viền + vòng báo)
IVisibilityPolicy (seam FOW) ◄──────┘── ScreenShakeModel ─────────────────────────────►  CameraShakeApplier
```

- **Một nơi quyết định: `VfxDirector`** (Application, `ITickable`). Mọi luật nằm ở đây và ở Domain: khử trùng, đối soát predicted/confirmed/rejected, hỏi seam, hẹn giờ, ngân sách 64, reset khi đổi phòng. Director **không** tham chiếu kiểu Unity Component nào ngoài port, nên EditMode test bằng `FakeVfxOutput` đếm handle và đồng hồ giả (gọi `Tick(dt)`).
- **Thực thi: `VfxStage`** (Presentation, MonoBehaviour gắn vào Bootstrap `DontDestroyOnLoad`) hiện thực `IVfxOutput`. Mỗi `VfxCue` có một `PooledPrefabFactory<VfxParticleEffect>` (tái dùng nguyên CLI-03) với số prewarm lấy từ catalog. Instance được spawn **làm con của root của Stage** (cũng `DontDestroyOnLoad`), nên đổi scene không Destroy instance đang chạy và không làm lệch bộ đếm pool.
- **Theo dõi instance để thu hồi (Q14), không sửa `IPoolService`:** Stage giữ mảng cố định 64 slot `{instance, generation, cue}`. Handle trả cho Director là `slot | generation << 8`. `Release(handle)` sai generation thì bỏ qua (an toàn với handle cũ, cùng ý tưởng `SpawnVersion` của pool). `ReleaseAll()` duyệt 64 slot rồi gọi `IPoolService.Despawn` ngay. VFX-01 **không dùng** `Despawn(instance, delay)`: thời lượng do Director đếm. Vì vậy khi reset không còn Despawn trễ nào treo, và không thể có log "bị Despawn hai lần" do VFX gây ra (AC6, AC7).
- **Không parent quầng vào `PlayerView`.** `PlayerView` cũng lấy từ pool. Nếu quầng là con của nó, khi view về pool thì quầng bị kéo theo vào root inactive nhưng vẫn ở trạng thái "spawned" (rò rỉ). Stage tự đặt vị trí quầng theo anchor ở `LateUpdate` (order 500, sau `PlayerView.LateUpdate`). Anchor biến mất (chết/despawn) thì Director thu hồi.
- **Nguồn sự kiện vào (seam để fake và để ticket sau nối):**
  - **Attack:** Director nghe `CombatEvent` qua `BindCombat(OnlineCombatSession, OfflineWorldSession)`, gate theo `OfflineWorldSession.IsActive`, giống AUD-01. Không sửa `WeaponController`/`OnlineCombatSession`.
  - **Hit, Explosion, Status:** ba `readonly struct` publish qua `IEventBus` (thông báo liên module, đúng Developer Guide 4.3). WPN-02/WPN-03/Game-10 sau này chỉ cần **một dòng** `_bus.Publish(new HitConfirmedEvent(...))` trong adapter packet của họ, không cần biết VFX. AUD-01 sau này có thể nghe cùng event. Event là struct nên publish không cấp phát.
  - **Zone:** `IVfxMatchFeed.TryGetZone` được đọc mỗi tick.
  - **Trạng thái của người chơi local:** `LocalEffectStatusSource` nghe `IEffectState.Changed` rồi publish `StatusEvent` cho `OwnNetId`. Chỉ đọc port đọc của UI-01, **không** gọi writer nào (AC12).
- **Seam visibility (2.4, Q15):** port `IVisibilityPolicy` ở `Application/Ports`, chữ ký tối thiểu `bool IsVisible(float x, float y, byte netId)` (`netId = 255` khi hỏi theo vị trí). Mặc định là `AlwaysVisiblePolicy`. Director bỏ qua seam khi `netId == OwnNetId` và với bo.
  - Mỗi tick Director **hỏi lại mọi handle đang sống** có vị trí hoặc anchor (tối đa 64 lần hỏi mỗi frame). Handle bị ẩn thì `Release` ngay trong tick đó, tức trước khi frame render (0 frame lộ, AC8d/e). Hiệu ứng "ngắn" (hit, số, nổ) bị ẩn thì không phát lại. Hiệu ứng "bền" (quầng, khói, vòng ngòi nổ) còn trạng thái logic trong bảng, nên khi hiện lại thì spawn đúng **một** instance.
  - Nháy thân cũng bị huỷ (`CancelFlash`) khi mục tiêu bị ẩn.
  - "Hiện mờ" (Gậy thần) **không** có trong chữ ký. Nếu FOW-01 cần thì FOW-01 thêm một overload/enum, không phải việc của VFX (Q15).
- **Đổi phòng/trận (AC6):** mỗi tick Director so `feed.SessionKey` (Guid: `MatchSession.MatchId` hoặc `OfflineMatchSession.MatchId`) và `feed.InGame`. Một trong hai đổi thì `ResetAll()`: `IVfxOutput.ReleaseAll()`, xoá bảng trạng thái/ledger/flash/budget, `deduper.Clear()`, reset `ZoneVfxModel` và `ScreenShakeModel`. `InGame` là `false` khi mất kết nối, khi phase online khác `InGame` hoặc khi offline không còn active, nên một luật này bao cả ba trường hợp. Thêm vào đó, `VfxStage` nghe `SceneManager.activeSceneChanged` để gọi `ResetAll` (trường hợp scene unload).
- **Khử trùng:** dùng lại `SfxDeduper` của AUD-01 (`Scripts/Application/Audio/SfxDeduper.cs`), không sửa file, với tham số riêng **5 s, 256 khoá**. Spec gợi ý 64 khoá, nhưng 10 người bắn liên tục có thể đẩy khoá ra trước 1 s, làm hỏng AC3 "lần ba sau 1 s". Khoá tạo bằng `SfxDeduper.Key(kind, a, b, c)`. Riêng bảng trạng thái làm khử trùng theo `(NetId, Kind)` (áp lại chỉ làm mới hẹn giờ). Zone dùng `(SessionKey, Tick)` và cạnh lên mỗi `PhaseIndex`, giống `MatchAudioEventSource`.
- **Đối soát Attack (2.3, Q4):** `AttackVfxLedger` (Domain) có vòng 32 mục `{netId, seq, state, handle}`.
  - `AttackPredicted` (mình): spawn `AttackPuff` và ghi `Predicted`. Seq đã có trong ledger thì bỏ qua.
  - `AttackConfirmed`: đổi sang `Confirmed`, không spawn. Seq đang `Rejected` hoặc không có thì bỏ qua.
  - `AttackRejected` (mọi `CombatOutcome` ≠ Accepted): `Release(handle)` **ngay trong handler**, rồi ghi `Rejected`.
  - `RemoteAttackConfirmed`: khử trùng theo `(NetId, RequestSeq)`, hỏi seam theo pose người bắn, rồi spawn.
  - Hit/nổ/trạng thái/bo **không bao giờ dự đoán**. **[Q4≠]** Nếu người muốn dự đoán nổ thì cần thêm một ledger thứ hai cho Explosion và kiểu event "ExplosionRejected". Đây là thay đổi lớn, phải lập lại kế hoạch.
- **Ngân sách (AC15):** `VfxBudget` (Domain) đếm instance theo ưu tiên: `Low` (hit spark, số, attack puff), `Medium` (quầng), `High` (nổ, khói), `Critical` (vòng ngòi nổ). Khi đã có 64 instance: thu hồi instance cũ nhất có ưu tiên **thấp hơn** yêu cầu mới. Không có instance nào thấp hơn thì bỏ yêu cầu mới (log cảnh báo một lần mỗi phiên). `Critical` không bao giờ bị thu hồi để nhường chỗ. Viền bo/tô tím không phải instance pool nên không tính vào ngân sách.
- **Vùng bo (AC11, Q1):** `ZoneVfxModel` (Domain) nhận `ZoneSnapshot`. Bán kính/tâm hiển thị nội suy tuyến tính theo `ShrinkStartTick`→`ShrinkEndTick` với `feed.NowTick` (`MatchSession.ServerTick` hoặc `OfflineMatchSession.CurrentTick`), có ba ràng buộc:
  - (a) trong cùng phiên, bán kính hiển thị **không bao giờ tăng**;
  - (b) mỗi tick bán kính chỉ đổi tối đa `v·dt + 0,05`, với `v` suy từ snapshot (`(R_hiện tại − R_đích)/(EndTick − now)·30`, kẹp ≥ `zoneShrinkSpeed` của balance). Lý do suy `v` từ snapshot: map offline lớn hơn 50 có tốc độ co nhân theo `ZoneSchedule.ScaledTo`;
  - (c) snapshot có tick cũ hơn thì bỏ.

  Vòng báo hiện khi `InWarning`, đếm cạnh lên một lần mỗi `PhaseIndex`. Hiển thị bằng `ZoneOverlayView` (Presentation, một instance tạo lúc compose, không pool):
  - **Tô ngoài bo:** một `SpriteRenderer` trắng phủ toàn bounds map (`IClientRuntimeData.Bounds`, map offline có thể > 50), cộng lề bằng nửa viewport. Màu `#A274FF` α 0,4, `maskInteraction = VisibleOutsideMask`. Thêm một `SpriteMask` hình tròn (sprite đĩa 1024 px) scale = 2r, đặt tại tâm. **Không cần shader mới.** Không có `SpriteMask` nào khác trong dự án (đã grep), nên không đụng renderer khác.
  - **Viền bo:** `LineRenderer` 96 điểm, texture đứt nét `textureMode = Tile`, α 1.
  - **Vòng báo** ở `TargetRadius`: `LineRenderer` thứ hai, nhấp nháy.
  - Overlay không có Collider và nằm layer `Ignore Raycast` (AC11).
  - **[Q1≠]** Nếu chỉ giữ viền: đặt α tô = 0 trong `VfxTuning` (không sửa code), AC11 đổi thành "không tô". Nếu đổi màu/α: chỉ sửa asset.
- **Rung màn hình (AC13, Q8):** `ScreenShakeModel` (Domain). Cộng xung có trần, biên độ tổng ≤ `shakeMaxCells` (0,15), tắt dần tuyến tính về 0 trong ≤ 0,3 s. Nhiễu dùng hàm băm theo thời gian, không `Random` cấp phát. Director thêm xung khi:
  - (a) `HitConfirmedEvent.TargetNetId == OwnNetId`;
  - (b) `ExplosionEvent` pha nổ **được seam cho phép** và cách người chơi local ≤ 6 cell.

  Chỉ rung khi `feed.InGame` và `IPlayerSettings.ScreenShakeEnabled` (đọc ở **mỗi tick**, tắt giữa chừng thì offset về 0 ngay). `CameraShakeApplier` (Presentation, `[DefaultExecutionOrder(1100)]`, sau camera driver 1000 của UI-03) cộng offset vào `Camera.main` trong `LateUpdate`, kẹp bằng `SpectatorCameraMath.ClampToMap`, rồi **trả lại vị trí cũ** ở `RenderPipelineManager.endCameraRendering`. Nhờ vậy `HeroCameraFollow`, `MatchDemoView` và spectator driver không bao giờ đọc phải vị trí đã rung, và sau khi hết rung vị trí = vị trí theo dõi ±0,001.
- **Nháy thân và số sát thương (AC12, Q6):**
  - **Nháy:** `VfxBodyAnchor` (component mới gắn trên `PlayerView.prefab` và `Hero.prefab`, xem T-3) giữ màu gốc của body lúc **bắt đầu** nháy. Nháy chồng chỉ gia hạn, không chụp lại màu (nên không "kẹt tint" sau 100 lần). Hết 0,16 s, `OnDisable` hoặc `CancelFlash` đều trả lại màu gốc. Tint mặc định `(1, .55, .5)`, giống `WorldObjectView` (xem T-4).
  - **Số:** prefab `Vfx_DamageNumber` dùng `TextMeshPro` (world, font `Art/Fonts/TMP/HudNumber SDF.asset`), `SetText("{0}", dmg)` không cấp phát. Số trôi lên 0,6 cell trong 0,7 s, mờ dần. Màu: trắng khi nguồn là mình, đỏ khi đích là mình, xám nhạt khi khác. Mọi giá trị nằm ở `VfxTuning`. **[Q6≠]** Đổi font/màu/không hiện trên mình: sửa tuning/một cờ trong tuning, không đổi cấu trúc.
  - Đích không tìm được anchor/pose (chết, đã despawn) thì bỏ qua cả nháy, số lẫn hạt, không lỗi.
- **Kích cỡ vùng nổ (AC10):** prefab được dựng ở **1×1 cell danh nghĩa**, ghi trong `VfxParticleEffect.nominalCells`. Lúc spawn, `localScale = (areaW/nominal, areaH/nominal, 1)`, mọi `ParticleSystem` đặt `scalingMode = Hierarchy`. Diện tích lấy từ event (`AreaWidth/Height`). Event để 0 thì fallback `IBalanceConfigProvider.TryGetWeapon(id).AreaWidth/AreaHeight` (`weapons.json` đã có 3×3/7×7). **Không hard-code vùng nổ.**
- **Số liệu:** vùng nổ, ngòi, thời lượng trạng thái đều đến từ event hoặc balance JSON. Số **trình bày** (0,16 s, 0,7 s, 0,15 cell, α 0,4, 64, 5 s) nằm trong `VfxTuning` của asset `VfxRuntimeSet`, giống `HudFeedbackTuning` của UI-01. Đây không phải số cân bằng, nên **không đổi schema JSON**. `weapons.json` **thiếu** thời lượng khói 10 s và đảo hướng của Chuông 10 s, nên hai số này chỉ có trong event. Kịch bản fake ghi chúng là "giá trị demo theo GDD 2.5", chỉ có trong `Mocks/` (Q11).

### 4.2. Chữ ký chính (dev giữ đúng tên)

```csharp
// ---- Domain: Scripts/Domain/CombatFeedback/ (JungleMaze.Domain.CombatFeedback) — hợp đồng cho WPN-02/WPN-03/Game-10 ----
public enum StatusKind : byte { None = 0, Adrenaline = 1, Poison = 2, Slow = 3, Invisible = 4, Stun = 5, Root = 6,
                                Reverse = 7, CcImmune = 8, Invulnerable = 100 }   // 1..8 trùng số với EffectKind (test khoá)
public enum ExplosionPhase : byte { Fuse = 0, Detonate = 1 }
public readonly struct HitConfirmedEvent { ulong EventKey; byte SourceNetId; byte TargetNetId; int Damage; float X, Y; }
public readonly struct ExplosionEvent { ulong EventKey; string WeaponId; ExplosionPhase Phase; float CenterX, CenterY;
    float AreaWidth, AreaHeight /*cell, 0 = theo balance*/; float SecondsRemaining /*Fuse*/; float LingerSeconds /*SMOKE*/; byte OwnerNetId; }
public readonly struct StatusEvent { ulong EventKey; byte NetId; StatusKind Kind; bool Applied; float DurationSeconds /*0 = tới khi gỡ*/; float RemainingSeconds; }
// Khác spec 2.2: dùng giây còn lại thay FuseEndTick/StartTick (cùng quy ước IEffectStateWriter.Apply và HUD SetDanger), để offline/online/fake
// không phải đổi tick. Producer tự quy đổi từ tick server.

// ---- Domain: Scripts/Domain/Vfx/ (JungleMaze.Domain.Vfx) — C# thuần ----
public enum VfxCue : byte { None, HitSpark, DamageNumber, AttackPuff, FuseWarning, ExplosionBanana, ExplosionMeteorite, ExplosionCrystal,
    ExplosionSeed, ExplosionBell, ExplosionSmoke, SmokeCloud, AuraStun, AuraRoot, AuraReverse, AuraCcImmune, AuraInvisible,
    AuraInvulnerable, AuraPoison, AuraAdrenaline /*P2, mặc định không có prefab*/ }
public enum VfxPriority : byte { Low, Medium, High, Critical }
public readonly struct VfxRule { VfxPriority Priority; float LifetimeSeconds /*0 = theo event*/; bool Persistent; }
public sealed class VfxRuleTable { bool TryGet(VfxCue cue, out VfxRule rule); }          // mảng theo (int)VfxCue
public readonly struct VfxTuningValues { float FlashSeconds, NumberSeconds, NumberRiseCells, ShakeMaxCells, ShakeSeconds,
    ShakeRadiusCells, DedupeSeconds; int DedupeCapacity, Budget; }
public sealed class AttackVfxLedger { /* Predicted/Confirmed/Rejected theo (netId, seq), 32 mục */ }
public sealed class StatusAuraTable { /* (netId, kind) → hạn, handle; Apply/Remove/Expire(now) */ }
public sealed class VfxBudget { bool TryReserve(VfxPriority p, out int evictHandle); void Free(int handle); }
public readonly struct ZoneSnapshot { uint Tick; byte PhaseIndex; bool InWarning; float CurX, CurY, CurR, TgtX, TgtY, TgtR; uint ShrinkStartTick, ShrinkEndTick; }
public sealed class ZoneVfxModel { void Reset(); void Apply(in ZoneSnapshot s); void Tick(uint nowTick, float dt);
    bool Visible; float CenterX, CenterY, Radius; bool WarningVisible; float WarnX, WarnY, WarnR; int WarningsRaised; }
public sealed class ScreenShakeModel { void Add(float amplitudeCells, float seconds); void Tick(float dt); float OffsetX, OffsetY; void Clear(); }

// ---- Application: Scripts/Application/Ports/IVisibilityPolicy.cs (JungleMaze.App.Ports) — FOW-01 sở hữu sau ----
public interface IVisibilityPolicy { bool IsVisible(float x, float y, byte netId); }        // netId = 255: hỏi theo vị trí

// ---- Application: Scripts/Application/Vfx/ (JungleMaze.App.Vfx) ----
public readonly struct VfxSpawn { float X, Y; float AreaWidth, AreaHeight; byte AnchorNetId /*255 = đứng yên*/; int Number; byte Tint; }
public interface IVfxOutput      // Presentation hiện thực (VfxStage)
{
    int Spawn(VfxCue cue, in VfxSpawn spawn);   // 0 = không spawn được (thiếu prefab)
    void Release(int handle);                   // handle cũ/sai generation: bỏ qua
    void ReleaseAll();
    void Flash(byte netId, float seconds);
    void CancelFlash(byte netId);
    int ActiveCount { get; }
}
public interface IVfxTargetLocator { bool TryGetPosition(byte netId, out float x, out float y); }   // Presentation: anchor → pose
public interface IVfxMatchFeed { Guid SessionKey { get; } bool InGame { get; } byte OwnNetId { get; } uint NowTick { get; }
                                 bool TryGetZone(out ZoneSnapshot zone); }
public sealed class VfxDirector : IInitializable, ITickable, IDisposable
{
    VfxDirector(IEventBus bus, IVfxMatchFeed feed, IVfxTargetLocator targets, IVisibilityPolicy visibility, IVfxOutput output,
                VfxRuleTable rules, VfxTuningValues tuning, IPlayerSettings settings /*null được*/, ILog log);
    void BindCombat(OnlineCombatSession online, OfflineWorldSession offline);
    void OnCombat(CombatEvent e);                          // public cho test
    ZoneVfxModel Zone { get; }  ScreenShakeModel Shake { get; }
    void ResetAll();
}
```

**Ticket sau nối vào đâu (không sửa VFX-01):**

| Ticket | Việc |
| --- | --- |
| WPN-02 | Adapter packet hit (khi có) publish `HitConfirmedEvent`; khoá = `SfxDeduper.Key(1, tick, src<<8\|dst, seq)` hoặc id sự kiện server (Q2) |
| WPN-03 | Publish `ExplosionEvent` Fuse rồi Detonate. **Dùng cùng một id** cho `IHudStateWriter.SetDanger(id, FusedBomb…)` và `EventKey`, để vòng thế giới và banner HUD không lệch nhau (Q7) |
| Game-10 | Publish `StatusEvent` cho người khác. Người chơi local đã có qua `IEffectState` (đừng phát đôi: Game-10 chỉ publish cho `NetId ≠ own`, hoặc đổi `LocalEffectStatusSource` sang tắt) |
| FOW-01 | Đăng ký `IVisibilityPolicy` **trước** `ComposeVfx` (Bootstrap.Vfx dùng `TryResolve`). Lưu ý: hiệu ứng ra đòn của WPN-01 (`CombatWeaponPresenter`) **không** qua seam (rủi ro R6) |
| ZON-01 | Không cần gì thêm nếu giữ `ZoneStatePacket` |

### 4.3. Lớp và file

| Lớp (asmdef) | File (mới, trừ dòng ghi **sửa**) | Trách nhiệm |
| --- | --- | --- |
| Domain | `Scripts/Domain/CombatFeedback/CombatFeedbackEvents.cs` | `HitConfirmedEvent`, `ExplosionEvent`, `StatusEvent`, `ExplosionPhase` |
| Domain | `Scripts/Domain/CombatFeedback/StatusKind.cs` | `StatusKind` + `StatusKinds.FromEffect(EffectKind)` |
| Domain | `Scripts/Domain/Vfx/VfxCue.cs` | `VfxCue`, `VfxPriority`, `VfxRule`, `VfxRuleTable`, `VfxTuningValues` |
| Domain | `Scripts/Domain/Vfx/AttackVfxLedger.cs` | Đối soát theo `(netId, seq)`, vòng 32 |
| Domain | `Scripts/Domain/Vfx/StatusAuraTable.cs` | Bảng `(netId, kind)` → hạn/handle/persistent; làm mới không cộng dồn; hết hạn theo đồng hồ truyền vào |
| Domain | `Scripts/Domain/Vfx/VfxBudget.cs` | Trần 64, thu hồi theo ưu tiên thấp + cũ nhất, không đụng Critical |
| Domain | `Scripts/Domain/Vfx/ZoneVfxModel.cs` | `ZoneSnapshot`; nội suy, đơn điệu, kẹp bước, cạnh lên cảnh báo theo phase, không NaN với r = 0 hoặc r lớn |
| Domain | `Scripts/Domain/Vfx/ScreenShakeModel.cs` | Xung có trần, tắt dần, nhiễu không cấp phát |
| Application | `Scripts/Application/Ports/IVisibilityPolicy.cs` | Port seam (2.4) |
| Application | `Scripts/Application/Visibility/AlwaysVisiblePolicy.cs` | Mặc định khi chưa có FOW-01 |
| Application | `Scripts/Application/Visibility/Mocks/ToggleVisibilityPolicy.cs` | (Editor/Dev) bọc policy thật; ẩn theo danh sách NetId hoặc hình chữ nhật; dùng cho preview, panel và test |
| Application | `Scripts/Application/Vfx/IVfxOutput.cs`, `IVfxTargetLocator.cs`, `IVfxMatchFeed.cs`, `VfxSpawn.cs` | Port như 4.2 |
| Application | `Scripts/Application/Vfx/VfxDirector.cs` | Toàn bộ luật ở 4.1 |
| Application | `Scripts/Application/Vfx/VfxMatchFeed.cs` | `IVfxMatchFeed` đọc `MatchSession` + `OfflineMatchSession` (+ `MockVfxMatch` khi Dev đang bật). `ZoneSimulation.Next*` → `Tgt*` |
| Application | `Scripts/Application/Vfx/LocalEffectStatusSource.cs` | `IEffectState.Changed` → `StatusEvent` cho `OwnNetId` (Added/Refreshed → Applied, Removed → gỡ) |
| Application | `Scripts/Application/Vfx/Mocks/MockVfxMatch.cs` | (Editor/Dev) phiên giả: `SessionKey`, `InGame`, `OwnNetId`, zone giả theo 5 phase của `ZoneSchedule.Default()`; nút "đổi phòng" = Guid mới |
| Application | `Scripts/Application/Vfx/Mocks/VfxScenarios.cs` | (Editor/Dev) fake event: hit, stun, smoke, từng vụ nổ, ngòi nổ, zone theo phase, lặp cùng khoá, predicted→rejected (gọi `OnCombat`), tải 10 nguồn. Diện tích/ngòi đọc từ `IBalanceConfigProvider` |
| Infrastructure | — | **Không có file mới.** `PoolService` dùng nguyên (Q14) |
| Presentation | `Scripts/Presentation/Vfx/VfxStage.cs` | `IVfxOutput`: factory theo cue, 64 slot + generation, đặt vị trí theo anchor ở LateUpdate (order 500; thân là `public void Drive()` để test gọi từng frame, giống `MatchOverlayCameraDriver`), `ReleaseAll` khi `activeSceneChanged` |
| Presentation | `Scripts/Presentation/Vfx/VfxParticleEffect.cs` | `IPoolable` gốc của mọi prefab VFX: cache `ParticleSystem[]`, `OnSpawned` Clear+Play, `OnDespawned` Stop+Clear, scale theo vùng, tint qua `MainModule.startColor` (không tạo material) |
| Presentation | `Scripts/Presentation/Vfx/DamageNumberVfx.cs` | TMP world: số, trôi, mờ dần, reset khi về pool |
| Presentation | `Scripts/Presentation/Vfx/VfxBodyAnchor.cs` | Trên prefab nhân vật: NetId (từ `PlayerView.NetId`, hoặc `isLocalHero`, hoặc `previewNetId`), điểm đầu/chân, nháy màu; tự đăng ký/huỷ với registry ở OnEnable/OnDisable/[Inject] |
| Presentation | `Scripts/Presentation/Vfx/VfxAnchorRegistry.cs` | `IVfxTargetLocator`: duyệt ≤ 16 anchor; local NetId ưu tiên anchor Hero; fallback `IPlayerPoseSource` |
| Presentation | `Scripts/Presentation/Vfx/ZoneOverlayView.cs` | Tô tím (SpriteMask), viền đứt nét, vòng báo; đọc `ZoneVfxModel` ở LateUpdate |
| Presentation | `Scripts/Presentation/Vfx/CameraShakeApplier.cs` | Offset camera sau mọi bên ghi, trả lại sau render |
| Presentation | `Scripts/Presentation/Vfx/VfxCatalog.cs` | ScriptableObject: cue → prefab, prewarm, ưu tiên, persistent, layer được duyệt; weaponId → cue (+tint); `StatusKind` → cue hoặc **NoVisual** (Slow, Adrenaline: không cảnh báo); `BuildRules()` ra `VfxRuleTable` |
| Presentation | `Scripts/Presentation/Vfx/VfxRuntimeSet.cs` | ScriptableObject `Resources/VfxRuntimeSet`: catalog, `VfxTuning` (đổi ra `VfxTuningValues`), prefab `ZoneOverlay`, `IsComplete` |
| Presentation | `Scripts/Presentation/Vfx/VfxDebugPanel.cs` | (Editor/Dev) IMGUI **F10**, tự mở trong scene `VfxPreview`: nút từng hiệu ứng, phát lặp, rollback, đổi phòng, công tắc ẩn/hiện mục tiêu, tải 10 nguồn, bộ đếm active/inactive/budget |
| Presentation | `Scripts/Presentation/Rendering/SortingLayers.cs` (**sửa**, 1 dòng) | Thêm `public const string Vfx = "VFX";` |
| Bootstrap | `Scripts/Bootstrap/Vfx/Bootstrap.Vfx.cs` | `ComposeVfx(TickRunner, MatchSession)`, xem 4.4 |
| Bootstrap | `Scripts/Bootstrap/Bootstrap.cs` (**sửa**, 1 dòng) | Gọi `ComposeVfx(tickRunner, match);` ngay **sau** `ComposeWorld(tickRunner, match);` |
| Editor tool | `Assets/_JungleMaze/Tools/Editor/Vfx/VfxPrefabSanitizer.cs` + `JungleMaze.Vfx.Editor.asmdef` (Editor, tham chiếu Domain/Application/Presentation) | Menu "Jungle Maze/VFX/Sanitize Selected Prefab" làm bước máy móc ở 4.5. Menu "Build Zone Overlay Prefab" dựng `ZoneOverlay.prefab` |
| Art | `Art/Vfx/Sprites/vfx_disc_1024.png`, `vfx_dash_8x2.png`, `vfx_area_frame.png` (9-slice) | Mask tròn, nét đứt, khung vùng nổ. Hình học đơn sắc do tool tạo, không phải art AI |
| Art | `Art/Vfx/Materials/*.mat` (chỉ khi cần) | Bản sao material khi phải đổi; **không** sửa material gốc |
| Art | `Art/Vfx/Resources/VfxRuntimeSet.asset`, `Art/Vfx/VfxCatalog.asset` | Asset cấu hình |
| Prefab | `Prefabs/Vfx/*.prefab` (bảng 4.5) + `Prefabs/Vfx/ZoneOverlay.prefab` | Bản sao đã làm sạch |
| Prefab | `Prefabs/PlayerView.prefab`, `Prefabs/Hero.prefab` (**sửa**: thêm `VfxBodyAnchor`) | Xem T-3 |
| Scene | `Assets/_JungleMaze/Scenes/Dev/VfxPreview.unity` | 4.6 |
| Test | `Tests/EditMode/Vfx/*.cs`, `Tests/PlayMode/Vfx/*.cs` | 4.7 |
| Docs | `Docs/Client_Developer_Guide.vi.md` (**sửa**: thêm 4.8 "Phát VFX và seam visibility") | Cách publish 3 event, cách FOW-01 cắm policy, F10 |

Không có asmdef runtime mới. Toàn bộ code nằm trong các assembly sẵn có, đúng chiều phụ thuộc: Domain không tham chiếu Unity/Protocol; Application chỉ tham chiếu Domain/Core/Protocol; Presentation không tham chiếu Infrastructure.

### 4.4. Composition (`Bootstrap.Vfx.cs`)

Theo mẫu `Bootstrap.Audio.cs`/`Bootstrap.MatchOverlay.cs`:

1. `Resources.Load<VfxRuntimeSet>(VfxRuntimeSet.ResourcePath)` **một lần**. Thiếu asset hoặc catalog thì `_log.Warn` rồi `return`: game chạy, không có VFX, không có exception.
2. `IVisibilityPolicy`: `TryResolve`, thiếu thì đăng ký `AlwaysVisiblePolicy`. Ở Editor/Dev, bọc thêm `ToggleVisibilityPolicy` (đăng ký theo kiểu cụ thể để panel dùng, không đè port).
3. `MockVfxMatch` (Editor/Dev). `VfxMatchFeed(match, Resolve<OfflineMatchSession>(), mock)`.
4. `VfxAnchorRegistry(Resolve<IPlayerPoseSource>())`, đăng ký để `VfxBodyAnchor` nhận qua `[Inject]`.
5. `Attach<VfxStage>()` (được inject `IPoolService`), sau đó `Configure(set)`. Prewarm theo catalog.
6. `TryResolve<IPlayerSettings>`. Có `new VfxDirector(...)`, sau đó `BindCombat(Resolve<OnlineCombatSession>(), Resolve<OfflineWorldSession>())`, `Register`, `ticks.Add(director)`. Director được thêm **sau** world/combat nên event của frame đã có trước khi Director tick.
7. `Register(new LocalEffectStatusSource(Resolve<IEffectState>(), bus, feed))`.
8. `Instantiate(set.ZoneOverlayPrefab, stage.transform)`, sau đó `Bind(director.Zone, Resolve<IClientRuntimeData>(), balance, tuning)`.
9. `Attach<CameraShakeApplier>().Bind(director.Shake, mapSize)`.
10. Editor/Dev: `Register(new VfxScenarios(...))`, `Attach<VfxDebugPanel>()`.

Đăng ký sau `InitializeAll` là hợp lệ (`ServiceRegistry.Register` tự gọi `Initialize`, như `ComposeWorld`).

### 4.5. Prefab VFX: tạo bản trong `_JungleMaze` (AC1, AC9, AC10)

**Cách làm (không sửa `Assets/Imported/JSurviv/`):**

1. **Copy, không làm Prefab Variant.** Dùng `AssetDatabase.CopyAsset` (hoặc Unity MCP `manage_asset` duplicate) từ `Imported/JSurviv/Prefabs/VFX/X.prefab` sang `Prefabs/Vfx/Vfx_<Tên>.prefab`. Lý do: Variant giữ phụ thuộc vào prefab gốc. Component bị "remove override" (Light, `ETFXRotation`) vẫn tồn tại ở base, và base đổi thì bản của ta đổi theo. Bản copy độc lập; material/sprite vẫn trỏ về asset gốc (chỉ đọc, hợp lệ).
2. Chạy `VfxPrefabSanitizer` trên bản copy. Tool tự làm:
   - xoá `Light`, mọi MonoBehaviour có script nằm trong `Imported/` (ETFX*), `Collider*`, `Rigidbody*`;
   - mọi `ParticleSystemRenderer`: `sortingLayer` = layer duyệt trong catalog (mặc định `VFX`, xem T-2), `sortingOrder` theo bảng;
   - `main.stopAction = None`, `main.playOnAwake = false`, `main.scalingMode = Hierarchy`;
   - root `localScale = 1`, `position.z = 0`;
   - thêm `VfxParticleEffect` nếu chưa có;
   - cuối cùng báo các renderer `renderMode = Mesh` còn lại để dev xử lý tay.
3. **Phần tay trong Editor:** bỏ/thay hệ mesh (sphere/portal) bằng billboard hoặc xoá child. Giảm số `ParticleSystem` (≤ 4). Đặt `looping` (quầng, khói: loop; nổ: không). Đo kích cỡ ở scale 1 rồi ghi `nominalCells`. Nếu phải đổi material: duplicate vào `Art/Vfx/Materials/`, sửa bản sao.
4. Chụp ảnh trước/sau ở scene preview (1280×720, có lưới 1 cell), ghi vào mục 5 + `Docs/Reports/VFX-01_<ngày>/`.

| Prefab mới | Nguồn | Cue | Layer/order dự kiến | Việc chính |
| --- | --- | --- | --- | --- |
| `Vfx_Explosion_Banana` | `BananaExploitVfx` | ExplosionBanana | VFX / 20 | Sanitize, đo 3×3 |
| `Vfx_Explosion_Meteorite` | `MeteoExploitVfx` | ExplosionMeteorite, ExplosionCrystal (**tint xanh trong catalog**, Q5) | VFX / 20 | Sanitize, đo 3×3. **[Q5≠]** Có art riêng thì thêm prefab, đổi 1 dòng catalog |
| `Vfx_Explosion_Seed` | `SeedExploit` | ExplosionSeed | VFX / 20 | Bỏ stretched billboard của `Droplets`, đổi xanh lá, ≤ 2 hệ |
| `Vfx_Explosion_Bell` | `BellExploit` (giữ 1 nova + tia) hoặc Epic Toon nova 2D | ExplosionBell | VFX / 20 | Bỏ `Light` + 4 hệ + mesh; đo 7×7. Nếu không gọn được thì thay nguồn và ghi lý do ở mục 5 |
| `Vfx_Explosion_Smoke` | `StarBurst2D` nhỏ (Epic Toon) | ExplosionSmoke | VFX / 20 | Lóe ngắn khi bom khói nổ, rồi cue `SmokeCloud` |
| `Vfx_Smoke` | `SmokeVfx` | SmokeCloud | VFX / 15 | `stopAction` 2→0, loop, đo 7×7; Director thu hồi sau `LingerSeconds`, ngừng phát 0,2 s trước |
| `Vfx_Hit_Spark` | 1 trong `MetalHit2D`/`SparkExplosion2D`/`StarBurst2D` | HitSpark | VFX / 30 | Chọn bản đọc tốt nhất ở 48 px, ≈ 0,6 cell, ≤ 0,4 s |
| `Vfx_Attack_Puff` | bản nhỏ của hit spark | AttackPuff | VFX / 25 | ≈ 0,4 cell, ≤ 0,25 s (T-5) |
| `Vfx_FuseWarning` | **mới** từ `vfx_area_frame.png` (+ tuỳ chọn `BombFuse`/`BombFuse2` ở tâm) | FuseWarning | VFX / 10 | Khung 9-slice theo vùng, nhấp nháy nhanh dần tới lúc nổ |
| `Vfx_DamageNumber` | **mới**, TMP | DamageNumber | VFX / 40 | 4.1 |
| `Vfx_Status_Stun` | `StunnedVfx` | AuraStun | T-2 | Bỏ hệ mesh sphere + `ETFXRotation`; sao quay bằng `rotationOverLifetime`/orbital velocity |
| `Vfx_Status_Reverse` | `ReverseVfx` | AuraReverse | T-2 | Đổi màu (đề xuất cam) để khác Immune/Invisible |
| `Vfx_Status_Shield` | `ImmortalVfx` (bỏ cầu mesh, thêm vòng 2D) | AuraInvulnerable (vàng), AuraCcImmune (**trắng**, Equipments 13) qua tint catalog | T-2 | `ImmunityVfx` giống hệt nên **không** copy riêng. Mục 5 ghi cả hai dòng kiểm kê trỏ về prefab này |
| `Vfx_Status_Invisible` | `MagicSparkles` của `ImmortalVfx` (chỉ hạt, α thấp, xanh nhạt) | AuraInvisible | T-2 | **Không có prefab nguồn riêng** (spec không chỉ định); phải khác hẳn Shield (AC16) |
| `Vfx_Status_Root` | mây + giọt xanh của `SeedExploit`, đặt ở chân | AuraRoot | T-2 | **Không có prefab nguồn riêng.** Vòng ở chân để phân biệt với quầng trên đầu |
| `Vfx_Status_Poison` | `PoisonsVfx` (bỏ `Skull` nếu lệch phong cách) | AuraPoison | T-2 | P2 theo spec, nhưng có nguồn thật (Ổ gai offline) nên làm luôn nếu kịp |
| `ZoneOverlay` | **mới** (tool dựng) | — | VFX / −100 | 4.1 |

Loại bỏ (không copy): `BleedVfx`, `BlindVfx`, `BuffDamageVfx`, `BurnedVfx`, `CantUseWeaponVfx`, `WaterVfx`, nhóm Epic Toon `Text`, `Blood`, `Muzzleflash`, `Sword`, `Missiles`, `Flamethrower`, `Magic`, `Water`, `Weather`, `Environment`, `Interactive`, `Misc`, `Demo`, giữ nguyên lý do ở bảng 2.1. **[Q9≠]** Nếu người muốn quầng Adrenaline: copy `TinySparkles` của `BuffDamageVfx` thành `Vfx_Status_Adrenaline`, đổi 1 dòng catalog từ NoVisual sang cue.

### 4.6. Scene trình diễn và đường nối vào trận thật

**`Scenes/Dev/VfxPreview.unity`** (không thêm vào Build Settings, theo mẫu `HudPreview`; `BootstrapAutoCreate` tự tạo Bootstrap vì scene nằm trong `_JungleMaze/`):

- Main Camera orthographic, cùng `orthographicSize` với camera scene Game (để đánh giá "6 cell" ở AC16); nền lưới 1 cell (SpriteRenderer tile từ sprite `Cell48` trắng có sẵn).
- 10 dummy (sprite hero + `VfxBodyAnchor.previewNetId` = 1…10, dummy 0 là "local"). Không có `PlayerView`/`RemotePlayerSpawner` hay map gameplay.
- Panel F10 tự mở khi scene đang active là `VfxPreview`. Lúc đó `MockVfxMatch` tự bật (`InGame = true`, `OwnNetId = 0`, zone giả). Nút gồm:
  - hit (flash + số + hạt), stun, smoke, từng vụ nổ, ngòi nổ, zone phase 0…5, cảnh báo bo;
  - **phát lặp** (cùng khoá ×2 + lần ba sau 1 s);
  - **rollback** (predicted → rejected, chọn `CombatOutcome`);
  - **đổi phòng** (Guid mới);
  - **ẩn/hiện mục tiêu** (`ToggleVisibilityPolicy` theo NetId);
  - **tải 10 nguồn** (60 s).

  Panel có bộ đếm active/inactive theo cue và mức dùng budget.

**Nối vào trận thật**, không cần thêm code khi chơi:

| Chế độ | Có nguồn thật | Còn fake (tới khi ticket kia xong) |
| --- | --- | --- |
| Offline practice (Game scene, `OfflineMatchSession`) | Zone (`ZoneSimulation`), Attack (`OfflineWorldSession.CombatChanged`), quầng của mình từ `IEffectState` (Poison ổ gai, Slow đầm lầy = NoVisual, Adrenaline = NoVisual) | Hit, Explosion, Status người khác |
| Online (GameServer local) | Zone (`MatchSession.Zone`), Attack (`OnlineCombatSession.Changed`, kể cả `RemoteAttackConfirmed`), quầng của mình từ `IEffectState` | Hit (WPN-02), Explosion (WPN-03), Status người khác (Game-10) |

Ở Game scene, panel F10 vẫn bắn được fake event chồng lên trận thật (Editor/Dev) để kiểm anchor trên `PlayerView`/`Hero` thật.

### 4.7. Test theo tiêu chí nghiệm thu

Mọi test VFX nằm ở `Tests/EditMode/Vfx/` và `Tests/PlayMode/Vfx/`. EditMode có `FakeVfxOutput` (đếm spawn/release/flash theo cue), `FakeMatchFeed`, `FakeLocator`, đồng hồ giả qua `Tick(dt)`. PlayMode dựng `PoolService` thật + `VfxStage` + catalog thật (`Resources.Load`), không load scene Game (memory UI-01 mục 11).

| AC | Test tự động | Loại | Phần cần người / QA đo tay |
| --- | --- | --- | --- |
| AC1 | `VfxCatalogTests.EveryKeptInventoryRowHasPrefab` (mỗi dòng "dùng lại/chỉnh" của 2.1 có prefab trong `Prefabs/Vfx` theo danh sách cố định trong test) | EditMode | Review bảng 2.1 + mục 5 có ảnh; QA chạy `git diff --stat -- Assets/Imported/JSurviv` = rỗng |
| AC2 | `VfxPreviewAcceptanceTests.SixItemsSpawnThenReturn` (hit, stun, smoke, Banana + Bell, fuse warning + zone warning, viền bo: active > 0 rồi = 0 sau thời lượng; test dùng smoke/stun ngắn 2 s để chạy nhanh, sai số ±0,2 s) | PlayMode | Playtest mở `VfxPreview`, F10, bấm từng nút |
| AC3 | `VfxDedupeTests`: một case cho mỗi loại Attack/Hit/Explosion(+Phase)/Status/Zone, lặp ×2 + lần ba sau 1 s → 1 spawn; zone tick cũ không kéo bán kính lên; 1 cảnh báo mỗi `PhaseIndex` | EditMode | — |
| AC4 | `AttackReconcileTests`: (a) predicted + confirmed = 1; (b) `[TestCaseSource]` mọi `CombatOutcome ≠ Accepted` → release trong cùng lời gọi; (c) rejected rồi confirmed muộn → 0. Thêm một case tích hợp `WeaponController` + `FakeCombatBackend.AttackOverride` | EditMode | — |
| AC5 | `StatusLifetimeTests`: áp lại → 1 instance, hạn làm mới; gỡ → release ngay; hết hạn trong `Duration ± 0,1`; `StatusKind` lạ → không exception, `FakeLog` nhận đúng 1 cảnh báo; NoVisual (Slow/Adrenaline) → 0 cảnh báo; `StatusKinds.FromEffect` khớp số 1..8 | EditMode | — |
| AC6 | `VfxResetTests` (EditMode, pool thật + prefab test): đổi `SessionKey` / `InGame=false` / `ResetAll` → `CountActive` mọi cue = 0 trong cùng tick, `LogAssert.NoUnexpectedReceived()` (không có "bị Despawn hai lần"), cùng `EventKey` ở phiên mới spawn lại 1 lần. `VfxRoomChangePlayTests` (PlayMode): đang có số/quầng/khói/viền bo, đổi phòng mock → frame sau 0 renderer VFX active, overlay ẩn | EditMode + PlayMode | — |
| AC7 | `VfxPoolReuseTests`: prewarm rồi 100 chu kỳ mỗi cue → `CountActive + CountInactive` không tăng; handle cũ sai generation bị bỏ qua; không gọi `Object.Destroy` (đếm qua `PooledObject` vẫn sống). `VfxGcTests`: 20 hiệu ứng active, warmup 120 frame, `Assert.That(() => { director.Tick(dt); stage.Drive(); }, Is.Not.AllocatingGCMemory())` | EditMode + PlayMode | QA chạy Profiler toàn frame (cùng giới hạn đã ghi ở UI-01: chưa gồm Canvas/Release player) |
| AC8 | `VisibilitySeamTests` (a)–(e) với `ToggleVisibilityPolicy`: Attack/Hit ẩn → 0 spawn/0 flash/0 rung; nổ tâm ẩn → 0; Status ẩn → 0 quầng; đang hiện chuyển ẩn → release trong tick, hiện lại → đúng 1; mọi handle của T (kể cả hạt/số đang bay) bị thu; local + bo luôn hiện; kịch bản giống hệt chạy với `AlwaysVisiblePolicy` → đếm trùng bản không seam; nổ ẩn → `Shake` = 0. `VfxSeamPlayTests` kiểm renderer thật tắt ở frame kế | EditMode + PlayMode | — |
| AC9 | `VfxPrefabAuditTests` quét `Prefabs/Vfx/*.prefab`: không Light/Collider/Collider2D/Rigidbody/Rigidbody2D, không MonoBehaviour có script nằm trong `Assets/Imported/`, không `renderMode = Mesh`, `stopAction ≠ Destroy`, `playOnAwake = false`, root scale 1 và z = 0, sorting layer đúng layer catalog cho phép, material có shader hợp lệ (không null, không `Hidden/InternalErrorShader`, `isSupported`). `VfxConsoleCleanTests` chạy kịch bản "tất cả" 10 s với `LogAssert.NoUnexpectedReceived()` | EditMode + PlayMode | QA mở `VfxPreview` 60 s, đọc console = 0 lỗi/0 cảnh báo VFX |
| AC10 | `VfxScaleTests`: spawn mỗi vụ nổ/khói với diện tích của balance, `ParticleSystem.Simulate(t_đỉnh)` rồi đọc `ParticleSystemRenderer.bounds` → trong ±15% của 3×3 / 7×7; quầng nằm trong ±1 cell quanh anchor | PlayMode | Ảnh 1280×720 có lưới 1 cell trong mục 5 (bounds particle có thể lệch với hình nhìn thấy) |
| AC11 | `ZoneVfxModelTests`: 5 phase của `ZoneSchedule.Default()` → bán kính/tâm ±0,05; r = 0 và r = 35,4 / 60 không NaN; mỗi bước 1/60 s ≤ v·dt + 0,05, không tăng; `InWarning` → 1 cảnh báo mỗi phase; feed offline map `Next*` → `Tgt*`. `ZoneOverlayPlayTests`: scale mask = 2r ±0,05, màu tô = #A274FF α 0,4 ±0,01, điểm ngoài/trong bo theo hình học (4 góc map 50 với r = 25 nằm trong quad tô và ngoài đĩa), không Collider, layer `Ignore Raycast` | EditMode + PlayMode | Ảnh online (GameServer local) + offline. Đo pixel ±2/255 là tuỳ chọn |
| AC12 | `HitFeedbackTests`: số = `Damage`, 1 số/event, sống 0,7 s; đích không có anchor/pose → 0 spawn, không lỗi. `HitFlashPlayTests`: nháy 0,16 s ±1 frame; 100 lần trúng liên tiếp → `body.color` về đúng màu gốc. `VfxArchitectureTests`: duyệt reflection mọi kiểu trong namespace `JungleMaze.*.Vfx` + `CombatFeedback`, không field/tham số constructor/method `[Inject]` nào có kiểu `IHudStateWriter`/`IInventoryStateWriter`/`IEffectStateWriter`, và không đọc HP (`IHudState.Hp`, `PlayerModel` HP) | EditMode + PlayMode | Reviewer grep thêm |
| AC13 | `ScreenShakeModelTests`: tắt → offset 0 mọi tick; bật → đỉnh ≤ 0,15, về 0 ≤ 0,3 s, nhiều xung chồng không vượt trần; `InGame = false` → 0. `CameraShakePlayTests`: có `HeroCameraFollow` ở mép map → vị trí sau offset vẫn trong map; sau khi hết rung vị trí = vị trí theo dõi ±0,001; tắt `ScreenShakeEnabled` giữa lúc rung → offset 0 ở tick kế | EditMode + PlayMode | **Người cảm nhận** cường độ khi playtest |
| AC14 | `VfxArchitectureTests` (không dùng `FindObjectOfType`/`FindAnyObjectByType` trong code VFX: quét text file `.cs` trong thư mục Vfx; không kiểu ETFX); chạy **toàn bộ** EditMode + PlayMode | EditMode | Reviewer: `git diff --stat -- Backend` rỗng; không sửa `WeaponController`/`OnlineCombatSession`/`WorldObjectView` |
| AC15 | `VfxBudgetTests`: tràn 64 → thu Low cũ nhất trước, không thu Critical/zone; Critical vẫn spawn khi đầy Low. `VfxLoadPlayTests` (`[Category("Perf")]`, 60 s, 10 nguồn): max active ≤ 64, không log lỗi, ghi frame time trung bình | EditMode + PlayMode | QA ghi cấu hình máy, Profiler; số Editor chỉ để tham khảo |
| AC16 | — | — | **Người cảm nhận:** ≥ 2 người, nhìn ở 6 cell trong `VfxPreview`, phân biệt 7 quầng + khói/nổ + đọc vùng ngòi nổ; ghi kết quả ở mục 7 |

Ngoài ra có `VfxMatchFeedTests` (online/offline/mock, `SessionKey`/`InGame`) và `LocalEffectStatusSourceTests` (Added/Refreshed/Removed → `StatusEvent`, chỉ cho `OwnNetId`).

### 4.8. Agent build, song song và commit

- **Agent build: chỉ unity-dev.** backend-dev **không** có việc (không đổi protocol, không sửa `Backend/`).
- **Song song:** về file, hai phần tách được: (A) code Domain/Application/Presentation + EditMode test; (B) prefab/art trong Editor (copy, sanitize, đo, chụp ảnh). B chỉ cần `VfxParticleEffect` và `VfxCatalog` (bước 3 của A) mới gắn component được. Nhưng **Unity Editor dùng chung** (Agent_Coordination mục 1.5), nên khuyến nghị **một unity-dev làm tuần tự**. Nếu CPO tách hai phiên thì B bắt đầu sau bước 3 của A và hai bên không mở Editor cùng lúc. code-reviewer và qa-tester chạy song song với nhau sau build (chỉ đọc).
- **Thứ tự làm bên trong build.** Không commit giữa chừng; mỗi bước phải compile + test của bước đó xanh rồi mới qua bước sau:
  1. Domain `CombatFeedback` + `Vfx` + EditMode test model (ledger, bảng trạng thái, budget, zone, shake).
  2. Application: port, `IVisibilityPolicy` + `AlwaysVisiblePolicy` + toggle, `VfxDirector`, `VfxMatchFeed`, `LocalEffectStatusSource`, mocks + EditMode test với fake (AC3–AC6, AC8, AC12, AC13, AC15).
  3. Presentation: `VfxParticleEffect`, `DamageNumberVfx`, `VfxBodyAnchor`, registry, `VfxStage`, `ZoneOverlayView`, `CameraShakeApplier`, `VfxCatalog`, `VfxRuntimeSet`, panel; `SortingLayers.Vfx`.
  4. Editor tool + sprite hình học + prefab (4.5) + catalog/runtime set asset + `ZoneOverlay.prefab`; thêm `VfxBodyAnchor` vào `PlayerView.prefab`, `Hero.prefab`.
  5. `Bootstrap.Vfx.cs` + 1 dòng `Bootstrap.cs`.
  6. `VfxPreview.unity`.
  7. PlayMode test + test quét asset (AC9).
  8. Kiểm kê trong Editor → mục 5 + ảnh; Developer Guide 4.8.
- **Commit theo quy tắc 8:**

| # | Chặng | Message gợi ý | Nội dung |
| --- | --- | --- | --- |
| 1 | Plan (CPO commit sau G1) | `docs(vfx-01): technical plan` | `Docs/Tickets/VFX-01.md` |
| 2 | **Build (một commit)** | `feat(vfx-01): pooled combat and safe-zone VFX layer` | Mọi file ở 4.3 + `.meta` + prefab/asset/scene + test + Developer Guide + dòng ghi `Agent_Coordination.vi.md` mục 4 + mục 5 ticket |
| 3 | Sửa sau review | `fix(vfx-01): review findings` | Chỉ file của VFX-01 |
| 4 | Đóng | `docs(vfx-01): QA and acceptance` | Mục 7–8, `Docs/Reports/VFX-01_*`, `.claude/memory/vfx-01.md` |

  Luôn `git commit -m "…" -- <danh sách đường dẫn>`. Working tree đang có thay đổi chưa commit của việc khác (map, `LevelWalls`, `OfflineWorldSession`…): **không** stage file đó. `OfflineWorldSession.cs` chỉ được **đọc** (VFX bind vào event `CombatChanged` có sẵn). Sau mỗi lần PlayMode qua MCP: `git checkout -- ProjectSettings/EditorSettings.asset`.
- **File dùng chung phải ghi Agent_Coordination mục 4 trước khi sửa:** `Bootstrap.cs` (1 dòng), `SortingLayers.cs` (1 dòng), `PlayerView.prefab`, `Hero.prefab` (thêm component), `Scenes/Dev/VfxPreview.unity` (mới), `MatchDemoView.cs` (chỉ khi T-1 = có).

### 4.9. Rủi ro, chỗ dễ vỡ, ticket kề cận

| # | Rủi ro | Giảm thiểu |
| --- | --- | --- |
| R1 | Vòng debug đỏ/vàng của `MatchDemoView` chồng lên viền bo mới | T-1 |
| R2 | Layer `VFX` nằm trên `Foreground`: quầng/số của người đứng dưới tán cây vẽ đè tán cây, nên **lộ người trốn trong bụi** ngay cả trước FOW-01 | T-2: quầng và hạt trúng thân dùng `Default` (dưới `Foreground`) |
| R3 | Online: người chơi local có thể có cả `Hero` lẫn `PlayerView` của mình (`MatchDemoView` dời `Hero` theo view), nên nháy/quầng có thể vào sai cái | Registry ưu tiên anchor `isLocalHero` đang active; dev xác minh bằng GameServer local và ghi ở mục 5 |
| R4 | `SpriteMask` 1024 px phóng tới đường kính 50–70 cell sẽ có răng cưa ở mép | Viền đứt nét che mép. Nếu vẫn xấu: phương án B là shader unlit tính khoảng cách (Shader Graph có sẵn trong URP, không thêm package), cần người duyệt |
| R5 | `PlayerView.Bind` đổi `body.color` đúng lúc đang nháy, nên màu trả về là màu cũ | Anchor chụp lại màu gốc khi `PlayerView` bind lại (OnEnable) và khi không nháy; có test 100 lần |
| R6 | Muzzle/vung/đạn của WPN-01 cho **người khác** (`CombatWeaponPresenter`) không qua seam, vẫn lộ khi có FOW | Ghi cho FOW-01; VFX-01 không sửa WPN-01 (spec 2) |
| R7 | Không có nguồn thật cho Hit/Explosion/Status người khác/bất tử, nên các AC này chỉ kiểm bằng fake | Hợp đồng event ở 4.2; ticket kia nối 1 dòng. Nếu WPN-02/03/Game-10 chọn packet có field khác (vd tick thay giây) thì họ đổi trong adapter của họ, không đổi VFX |
| R8 | `RequestSeq` là `ushort` quay vòng; ledger 32 mục có thể nhầm seq cũ sau 65 536 lần bắn | Ledger so cả trạng thái và tuổi (≤ 5 s); seq trùng mà quá tuổi coi là mới |
| R9 | `ParticleSystem.bounds` không khớp hình nhìn thấy (sprite sheet có viền trong suốt) | AC10 có ảnh lưới làm bằng chứng chính |
| R10 | `VfxPreview` chạy cùng Bootstrap thật: menu IMGUI `ConnectionMenuView` vẫn vẽ (giống `HudPreview`) | Chấp nhận như `HudPreview`; panel F10 đặt bên phải |
| R11 | Spectator/killcam (UI-03) đổi camera: rung chỉ chạy khi `InGame` và mình còn sống, killcam không phát lại VFX (ngoài phạm vi) | Test `CameraShakePlayTests` có case driver spectator đang ghi |
| R12 | Deadline trong sheet đã qua (Q12) | CPO hỏi ngày mới |
| R13 | Unity Editor dùng chung, `EditorSettings.asset` bị MCP đổi | Quy trình Agent_Coordination |

**Ticket kề cận có thể bị ảnh hưởng:**

- **MOV-03/MOV-01:** prefab `PlayerView`, `Hero` thêm component.
- **UI-03:** `CameraShakeApplier` sau driver 1000; đọc `IPlayerSettings`.
- **AUD-01:** dùng lại `SfxDeduper`, không sửa; nếu sau này AUD-01 nghe cùng `HitConfirmedEvent` thì bỏ map trùng từ HUD.
- **FOW-01:** hiện thực `IVisibilityPolicy`; R2, R6.
- **WPN-02/WPN-03/Game-10/ZON-01:** hợp đồng 4.2.
- **ENV:** `WorldObjectView` giữ nguyên.

### 4.10. Câu hỏi kỹ thuật cho G1 (kèm mặc định của kế hoạch)

| # | Câu hỏi | Mặc định trong kế hoạch |
| --- | --- | --- |
| T-1 | `MatchDemoView` đang vẽ vòng bo debug đỏ/vàng trong thế giới. Giữ cả hai, hay ẩn vòng debug khi không bật F3? | **Ẩn khi `DetailsVisible == false`** (sửa 2 dòng trong `MatchDemoView.cs`, ghi Agent_Coordination). Nếu người không cho sửa file đó thì hai lớp vòng cùng hiện (chấp nhận trùng hình) |
| T-2 | Layer cho hiệu ứng gắn thân (quầng, hit spark, số): `VFX` (trên tán cây, lộ người trốn bụi) hay `Default` (dưới `Foreground`, tán cây che giống thân)? | **`Default`, order 100** cho quầng/hit spark/số; `VFX` cho nổ, khói, ngòi nổ, bo. AC9 kiểm theo "layer catalog cho phép" (spec đã cho phép) |
| T-3 | Cho phép thêm `VfxBodyAnchor` vào `PlayerView.prefab` và `Hero.prefab` (prefab của MOV-03/MOV-01)? | **Có** (chỉ thêm component, không sửa script). Nếu không: Stage chỉ đặt quầng theo `IPlayerPoseSource`, **không có nháy thân** (AC12 phần nháy không làm được) |
| T-4 | Nháy trúng đòn: tint đỏ nhạt `(1, .55, .5)` như `WorldObjectView`, hay nháy **trắng**? Trắng cần shader sprite riêng | **Tint như ENV**, không shader mới |
| T-5 | VFX-01 có thêm `AttackPuff` (bụi nhỏ ≤ 0,25 s khi ra đòn, cả mình lẫn người khác) để có đối tượng cho AC3/AC4 phần Attack không, hay để WPN-01 lo hoàn toàn phần ra đòn? | **Có**, nhỏ và ngắn. Nếu không: bỏ cue khỏi catalog; ledger vẫn chạy, AC4 kiểm bằng prefab test |
| T-6 | Dùng `IEffectState` (UI-01) làm nguồn thật cho quầng trên thân **mình** (online + offline)? | **Có** (chỉ đọc). Game-10 sau này chỉ publish cho người khác |
| T-7 | Bất tử không có trong `EffectKind`. VFX dùng enum riêng `StatusKind` (1..8 trùng số + `Invulnerable = 100`). Đồng ý, hay muốn Game-10 thêm `Invulnerable` vào `EffectKind`? (thêm vào sẽ đổi polarity HUD) | **Enum riêng** |
| T-8 | Bộ nhớ khử trùng: 5 s / **256** khoá (spec gợi ý 64) | **256**, chỉnh trong `VfxTuning` |

### 4.11. Kết quả G1 (07/11/2026)

Người duyệt **theo khuyến nghị**: mọi câu Q1–Q15 theo khuyến nghị của designer, T-1…T-8 theo mặc định trong 4.10. Cụ thể: bo tô #A274FF α 0,4 + viền đứt (Q1); chỉ dự đoán ra đòn của mình, đối soát theo `RequestSeq` (Q4); seam FOW mặc định `AlwaysVisible` (Q15); quầng/hit spark/số ở layer `Default` dưới tán cây, nổ/khói/ngòi/bo ở `VFX` (T-2); ẩn vòng debug `MatchDemoView` khi tắt F3 (T-1); thêm `VfxBodyAnchor` vào `Hero.prefab`/`PlayerView.prefab` (T-3); nháy tint đỏ nhạt (T-4). Q12 (deadline mới) chưa có trả lời, không chặn build.

### 4.12. Điều chỉnh sau khi merge main (07/11/2026)

Tech-lead viết lại một phần sau khi nhánh `hvu` merge `origin/main` (commit merge `11c78d20`, 40 commit mới). Mục 1–3 và 4.0–4.11 giữ nguyên để làm lịch sử. **Chỗ nào 4.12 nói khác thì làm theo 4.12**, sau khi người chốt D1 và D2 (4.12.11). Mọi điểm dưới đây đã đối chiếu bằng code sau merge.

#### 4.12.1. Hiện trạng mới (bổ sung và sửa 4.0)

| Điểm | Kết quả kiểm sau merge | Hệ quả cho VFX-01 |
| --- | --- | --- |
| Vùng bo trong thế giới | **Đã có**: `Scripts/Presentation/UI/ZoneOverlayView.cs` + `Resources/ZoneOverlay.shader` (Quang Huy, `529d59c1`, `ab940c6a`, `1924b47f`). Một quad lớn ở sorting layer `Foreground`, order 100. Phần ngoài bo tô **đỏ** `(.85, .025, .035)` α 0,62, viền bo đỏ đậm hơn α 0,88. Có đường trắng liền ở `TargetRadius` khi `InWarning` hoặc đang co. Minimap dùng cùng shader (đỏ đô, nét đứt). Khi r = 0 thì tô toàn bộ. `MatchDemoView.ShowZone` gọi overlay cho cả online (`MatchSession.Zone`) lẫn offline (`ZoneSimulation.GetStatePacket`). Overlay vẽ thẳng gói mới nhất (server gửi zone kèm mọi snapshot), không nội suy và không kẹp đơn điệu. Chữ cảnh báo đã bỏ, chuông âm thanh vẫn giữ | VFX-01 **không vẽ bo** nữa (D1). Tên `ZoneOverlayView` ở 4.3 trùng tên lớp này, nên bỏ hẳn |
| Vòng debug của `MatchDemoView` | Đã xoá (không còn `LineRenderer` đỏ/vàng) | **T-1 không còn đối tượng.** R1 đóng. VFX-01 không sửa `MatchDemoView.cs` |
| Camera | `HeroCameraFollow` có `[DefaultExecutionOrder(900)]` và `FollowOverride`. Online, camera bám `PlayerView` của mình; demo không còn đặt camera tổng (25,25) size 28. Spectator driver vẫn ở order 1000 | `CameraShakeApplier` ở order 1100 vẫn đúng. Test AC13 thêm một case có `FollowOverride` |
| Người chơi local khi online | Online, `Hero` chỉ là proxy ẩn cho camera; nhân vật mình được vẽ bằng `PlayerView` của chính mình (`c8f8e4c2`, `8cd81b76`). `ThrowableView.ViewOf/IsLocal` đi theo đúng luật này | **R3 đổi cách làm:** khi online (`MatchSession.Connected`), registry phải ưu tiên anchor `PlayerView` có `NetId == OwnNetId`; khi offline thì dùng anchor `Hero` (netId 0). Ngược với dòng `VfxAnchorRegistry` ở 4.3 |
| Protocol | Thêm `ThrowableRequestPacket` (client gửi server) và `ThrowableEventPacket` (server gửi cả phòng, ReliableOrdered) trong `Backend/Shared/Protocol/Gameplay/ThrowablePackets.cs`. Event 1..14 gồm `Thrown`, `Detonated`, `TrapArmed/Triggered/Expired`, `SmokeStarted/Ended`, `RopePulledTarget/Self`, `RopeMissed`, `BellRung`, `BatSwing`, `BatHit`, `Hit`. Có thêm `Actor`, `Target`, `Id`, `From/To`, `Seconds`, `Size`, `Amount` (sát thương thực), `Flags` (bit 0 = bị choáng). Phía client đi qua `IThrowableEventSource` (`OnlineThrowableSession`, `OfflineWorldSession`), và `ThrowableEvent` **không** mang `Tick`. `MatchProtocol.Version` lên 3 vì có field bot, không liên quan VFX | **Sửa 4.0:** đã có **nguồn thật** cho Explosion (`Thrown` = ngòi, `Detonated` = nổ), cho Hit có sát thương của đồ ném (`Hit`, `BatHit`, `RopePulledTarget`), và cho Status của **người khác** do đồ ném gây ra (Stun từ `TrapTriggered`/`BatHit` có cờ choáng; Reverse từ `Hit` của BELL; Root từ `Hit` của SEED). VFX-01 **vẫn không đổi protocol** |
| `EventPacket.PlayerHit`, `PlayerStateFlags.Invulnerable` | Grep lại: server vẫn chỉ phát `PlayerDied`, vẫn chỉ bật cờ `Dead` | Hit của đạn và cận chiến vẫn chỉ có nguồn fake. Quầng bất tử vẫn chỉ có nguồn fake |
| `ThrowableView` (Hào, `7b744214`) | Vẽ bằng sprite sheet JSurviv qua `ThrowableArtCatalog` (Resources): đường bay rồi nháy ngòi tại điểm rơi; nổ (sheet chuối; sheet thiên thạch cho cả METEORITE và CRYSTAL, CRYSTAL tô xanh); hạt dùng `seed_stun`. Kích cỡ nổ = `Size` cell. Vẽ thêm vỏ chuối, mây khói (1,15 × `Size`, sống theo `Seconds`), dây thừng, vòng vuông 7×7 của chuông, chuông lắc trên đầu, dây leo trên người bị trói, và animation của người dùng. Dùng free list riêng (không qua `IPoolService`). Sorting theo layer thân hero (`Default`), order 20 hoặc −5. **Không** hỏi seam visibility, **không** dọn khi đổi trận. Được tạo bằng `AddComponent` trong `CombatWeaponPresenter.Start`. Các thành viên public: `OnEvent`, `Advance(dt)`, `ActiveEffects` | Nổ, khói, bẫy, dây leo, chuông đã có hình (D2). VFX-01 không vẽ lại các phần này |
| `Docs/Tickets/WPN-03.md` | Ghi rõ: "Particle và cảnh báo vùng nổ → VFX-01" và "Icon choáng trên đầu người bị stun (VFX-01)" | Hai việc này vẫn thuộc VFX-01 |
| Luật đồ ném (`ThrowableService`) | `Seconds` trong event là thời lượng theo luật, gửi cả khi mục tiêu đang miễn nhiễm CC (`ApplyEffect` có thể đã từ chối). `Amount` = 0 khi mục tiêu bất tử hoặc khi là khói. `Id` (ushort) tăng trong mỗi trận cho nổ/bẫy/khói; `Hit` của BELL, `BatHit` và dây thừng có `Id = 0` | Adapter cần quy tắc riêng: bỏ quầng CC khi biết đích đang miễn nhiễm, không hiện số khi `Amount ≤ 0`, và không khử trùng theo `Id = 0` (4.12.3) |
| HUD cảnh báo bom (UI-01 `SetDanger`) | Chưa có ai gọi; WPN-03 không nối vào | Q7 chưa có cặp để khớp id. Ngoài VFX-01, ghi follow-up |
| `Hero.prefab`, `PlayerView.prefab` | WPN-01 (`01c2a1b8`) sửa nhiều: thêm tay và vũ khí cầm | T-3 vẫn làm được, nhưng dễ conflict (R20) |
| Working tree | `Backend/JungleBackend.sln` đang bị sửa, không phải của VFX-01 | Không stage file này |

#### 4.12.2. Bảng thay đổi phạm vi

| Hạng mục | Kế hoạch cũ (4.1–4.6) | Sau merge | Lý do |
| --- | --- | --- | --- |
| Tô ngoài bo, viền bo, vòng báo bo thu | VFX-01: `ZoneVfxModel` + `ZoneOverlayView` (SpriteMask, tím α 0,4) | **Bỏ** (D1) | `ZoneOverlayView` của Quang Huy đã làm đủ ba việc, cho cả online, offline và minimap |
| Nổ BANANA/METEORITE/CRYSTAL/SEED, vòng chuông, khói 10 s, bẫy chuối | VFX-01: 5 prefab nổ + `Vfx_Smoke` | **Bỏ** (D2) | `ThrowableView` đã vẽ bằng **cùng** sprite sheet JSurviv, cỡ theo `Size` từ server |
| Quầng Root | `Vfx_Status_Root` | **Bỏ**; catalog map `Root` → NoVisual | `ThrowableView` đã vẽ dây leo trên người bị trói |
| Tint Tinh thể (Q5) | Tint trong catalog VFX | **Bỏ** | `ThrowableView.BlastTint` đã tô xanh |
| Vòng cảnh báo ngòi nổ (`FuseWarning`) | Nguồn fake `ExplosionEvent` | **Giữ, đổi nguồn**: `Thrown` (ToX/ToY, `Size`×`Size`, `Seconds`) mở vòng; `Detonated` cùng `Id` thì gỡ. Bỏ `BombFuse` ở tâm | WPN-03 giao cho VFX-01; `ThrowableView` đã nháy vật ở điểm rơi |
| Hit flash, số sát thương, hit spark | Nguồn fake | **Giữ**. Nguồn thật mới: `Hit`/`BatHit`/`RopePulledTarget` có `Amount > 0`. Đạn và cận chiến vẫn fake | Như trên |
| Quầng Stun, Reverse, CcImmune, Invulnerable, Invisible, Poison | Fake cho người khác, `IEffectState` cho mình | **Giữ**. Stun và Reverse của người khác có nguồn thật từ đồ ném. **Quy tắc mới:** không áp quầng CC (Stun/Root/Reverse) cho NetId đang có quầng CcImmune | WPN-03 giao icon choáng cho VFX-01; R16 |
| Rung màn hình | Fake | **Giữ**. Nguồn thật: `Detonated` cách mình ≤ 6 cell và được seam cho phép; đồ ném trúng mình | — |
| Attack puff + ledger đối soát | Có (T-5) | **Giữ** như G1. Nếu chồng với tay/vung của WPN-01 mà rối mắt, dev tắt bằng 1 dòng catalog và ghi ở mục 5 (không cần hỏi lại) | AC4 vẫn cần đối tượng để đo |
| Khử trùng, reset khi đổi phòng, seam, ngân sách 64 | Có | **Giữ**; bỏ phần zone khỏi feed | — |
| Scene `VfxPreview` | Có zone giả, nổ VFX | **Giữ, đổi nội dung**: thêm một `ThrowableView` chạy bằng `ThrowableEvent` giả để xem tích hợp; bỏ nút zone | Kiểm hai lớp cùng một event |
| Kiểm kê 16 prefab | Bảng 2.1 | **Giữ đủ 16 dòng**; đổi quyết định (4.12.4) | AC1 |
| T-1 (sửa `MatchDemoView`) | 2 dòng | **Bỏ** | Không còn vòng debug |

#### 4.12.3. Cách nối với `ThrowableView`: hai lớp cùng nghe một nguồn, không sửa file của Hào

Đã cân nhắc ba cách:

- **(A) VFX-01 cấp particle cho `ThrowableView`:** phải sửa `ThrowableView`/`ThrowableArtCatalog`, có hai cơ chế pool chồng nhau, và thay art đã playtest. Không chọn.
- **(B) VFX-01 gọi vào `ThrowableView`:** tạo phụ thuộc giữa hai view mà không được gì thêm. Không chọn.
- **(C) Chọn:** cả hai nghe `IThrowableEventSource` (chỉ đọc). `ThrowableView` lo **vật và vùng** (bay, nổ, khói, bẫy, dây, dây leo). VFX-01 lo **phản hồi trên thân, cảnh báo trước và rung**. VFX-01 không sửa dòng nào trong `ThrowableView.cs`, `ThrowableArtCatalog.cs`, `OnlineThrowableSession.cs`, `OfflineWorldSession*.cs`.

Adapter mới `Scripts/Application/Vfx/ThrowableVfxSource.cs` đổi `ThrowableEvent` thành 3 event của 4.2 rồi publish lên `IEventBus`. Director giữ nguyên hợp đồng; fake và nguồn thật đi chung một đường.

```csharp
public sealed class ThrowableVfxSource : IInitializable, IDisposable
{
    public ThrowableVfxSource(IEventBus bus, OfflineWorldSession offline, params IThrowableEventSource[] sources);
    public void OnThrowable(in ThrowableEvent e);   // public cho test
}
```

| `ThrowableEventKind` | `ThrowableView` (giữ nguyên) | VFX-01 (`ThrowableVfxSource` publish) |
| --- | --- | --- |
| `Thrown` (Item 1..5) | Bay, nháy ngòi | `ExplosionEvent(Fuse)` tại `ToX/ToY`, vùng `Size×Size`, `SecondsRemaining = Seconds` |
| `Detonated` | Nổ | `ExplosionEvent(Detonate)`: gỡ vòng cùng khoá; rung nếu ≤ 6 cell và seam cho phép. **Không spawn nổ** |
| `Hit` | Dây leo nếu SEED | `HitConfirmedEvent(Target, Amount, ToX/ToY)` nếu `Amount > 0`. Thêm `StatusEvent(Reverse, Seconds)` nếu Item = BELL và `Seconds > 0`. SEED → `StatusEvent(Root)`, mà Root là NoVisual |
| `BatHit` | — (đã animate ở `BatSwing`) | `HitConfirmedEvent` nếu `Amount > 0`; `StatusEvent(Stun, Seconds)` nếu có cờ choáng và `Seconds > 0` |
| `TrapTriggered` | Gỡ vỏ chuối | `StatusEvent(Stun, Seconds)` cho `Target` |
| `RopePulledTarget` | Dây | `HitConfirmedEvent` nếu `Amount > 0` |
| `TrapArmed/Expired`, `SmokeStarted/Ended`, `BellRung`, `BatSwing`, `RopePulledSelf/Missed` | Như hiện tại | Không làm gì |

- **Chặn nguồn:** bỏ event online khi `OfflineWorldSession.IsActive`, giống cách bind combat. `OnlineThrowableSession` vốn đã lọc theo `MatchId`.
- **Khoá khử trùng:**
  - Khi `Id ≠ 0`: `SfxDeduper.Key(tag(kind), Id, Target)`.
  - Khi `Id = 0`: dùng **bộ đếm riêng của adapter**, nên không bao giờ trùng. Kênh ReliableOrdered vốn không lặp gói; nếu khử trùng theo `Id = 0` thì hai cú chuông khác nhau trong 5 s sẽ bị nuốt mất một.
  - `Id` reset mỗi trận, và deduper đã được xoá khi `SessionKey` đổi.
- **Quầng CC khi đích miễn nhiễm:** Director bỏ `StatusEvent` Stun/Root/Reverse cho NetId đang có quầng `CcImmune`. Với chính mình, `CcImmune` đến từ `IEffectState` (nguồn thật). Với người khác, phải chờ Game-10 (R16).
- **Hệ quả phụ:** `VfxSpawn` cần thêm `Seconds` để prefab `FuseWarning` nháy nhanh dần tới lúc nổ. `IVfxOutput.Stop` (chỉ dùng cho khói) bỏ đi.

#### 4.12.4. Kiểm kê cập nhật (thay cột "Quyết định" của bảng 2.1 và bảng prefab 4.5)

| Prefab JSurviv | Quyết định mới | Lý do |
| --- | --- | --- |
| `SmokeVfx` | **Loại bỏ** (trước: Chỉnh) | `ThrowableView` vẽ cùng sheet `smoke-Sheet.png` |
| `BananaExploitVfx` | **Loại bỏ** (trước: Dùng lại) | `ThrowableView` dùng bản copy `banana_explosing-Sheet` đã cắt 8 khung (`Art/Combat/Throwables`) |
| `MeteoExploitVfx` | **Loại bỏ** (trước: Dùng lại/Chỉnh) | `ThrowableView` dùng `meteorite_explosion-Sheet` cho METEORITE và CRYSTAL (tô xanh) |
| `SeedExploit` | **Loại bỏ** (trước: Chỉnh) | `ThrowableView` dùng `seed_stun.png` cho cả nổ và dây leo |
| `BellExploit` | **Loại bỏ** (trước: Chỉnh nặng) | `ThrowableView` vẽ vòng vuông 7×7 và chuông lắc |
| `StunnedVfx` | Chỉnh (giữ) → `Vfx_Status_Stun` | Icon choáng, WPN-03 giao cho VFX-01 |
| `ReverseVfx` | Chỉnh (giữ) → `Vfx_Status_Reverse` | Nay có nguồn thật (Hit của BELL) |
| `ImmortalVfx` | Chỉnh (giữ) → `Vfx_Status_Shield` (Invulnerable vàng, CcImmune trắng) | Như 4.5 |
| `ImmunityVfx` | Biến thể của Shield, không copy | Như 4.5 |
| `PoisonsVfx` | Chỉnh, P2 (giữ) → `Vfx_Status_Poison` | Như 4.5 |
| `BleedVfx`, `BlindVfx`, `BuffDamageVfx`, `BurnedVfx`, `CantUseWeaponVfx`, `WaterVfx` | Loại bỏ (không đổi) | Như 2.1 |
| Epic Toon `Prefabs 2D/Explosions` | Giữ một bản cho `Vfx_Hit_Spark` và `Vfx_Attack_Puff` | Như 4.5 |
| Epic Toon `Combat/BombFuse` | **Loại bỏ** (trước: tuỳ chọn) | `ThrowableView` đã nháy vật đang chờ nổ |
| Các nhóm Epic Toon còn lại | Như 2.1 | — |

**Prefab VFX-01 sẽ làm (9 cái, thay bảng 4.5):** `Vfx_Hit_Spark`, `Vfx_Attack_Puff`, `Vfx_FuseWarning` (khung 9-slice `vfx_area_frame.png`, layer `VFX`), `Vfx_DamageNumber`, `Vfx_Status_Stun`, `Vfx_Status_Reverse`, `Vfx_Status_Shield`, `Vfx_Status_Invisible`, `Vfx_Status_Poison`. Layer/order theo T-2 (quầng, hit spark, số ở `Default` order 100; ngòi nổ ở `VFX`). Không còn `ZoneOverlay.prefab`, `Vfx_Explosion_*`, `Vfx_Smoke`, `Vfx_Status_Root`.

#### 4.12.5. File và lớp cập nhật (so với 4.3)

| Lớp | File | Thay đổi |
| --- | --- | --- |
| Domain | `Domain/Vfx/ZoneVfxModel.cs` | **Bỏ** |
| Domain | `Domain/Vfx/VfxCue.cs` | Bỏ cue `Explosion*`, `SmokeCloud`, `AuraRoot`. Bỏ `MapWeapon`/`WeaponVisual`/`LingerCue`. Bỏ `StopLeadSeconds` trong `VfxTuningValues` |
| Domain | `Domain/CombatFeedback/CombatFeedbackEvents.cs` | `ExplosionEvent` bỏ `LingerSeconds` (khói không còn thuộc VFX-01). Giữ nguyên phần còn lại |
| Application | `Application/Vfx/IVfxMatchFeed.cs`, `VfxMatchFeed.cs` | Bỏ `TryGetZone`/`FromPacket` |
| Application | `Application/Vfx/IVfxOutput.cs`, `VfxSpawn.cs` | Bỏ `Stop`; thêm `VfxSpawn.Seconds` |
| Application | `Application/Vfx/VfxDirector.cs` | Bỏ zone, spawn nổ và linger khói; giữ linger cho `FuseWarning`; thêm luật CcImmune; bỏ `HitConfirmedEvent` có `Damage ≤ 0` (adapter đã lọc, Director chặn thêm cho nguồn fake) |
| Application | `Application/Vfx/ThrowableVfxSource.cs` | **Mới** (4.12.3) |
| Application | `Application/Vfx/Mocks/FakeThrowableEventSource.cs` | **Mới** (Editor/Dev): `IThrowableEventSource` giả. Phát `Thrown` → `Detonated` → `Hit` cho từng dummy trong vùng, `SmokeStarted/Ended`, `TrapTriggered`, `BellRung` + `Hit`, `BatHit` có cờ choáng. Số lấy từ `ThrowableRules.Baseline()` hoặc `ThrowableRulesJson` |
| Application | `Application/Vfx/Mocks/MockVfxMatch.cs` | Bỏ `ZoneSimulation`; chỉ còn `SessionKey`, `InGame`, `OwnNetId`, `NowTick` |
| Application | `Application/Vfx/Mocks/VfxScenarios.cs` | Ném/nổ/khói đi qua `FakeThrowableEventSource`. Hit súng và Status giữ đường bus. Bỏ các nút zone |
| Presentation | `Presentation/Vfx/ZoneOverlayView.cs` | **Bỏ** |
| Presentation | `Presentation/Vfx/VfxAnchorRegistry.cs` | Online: own → `PlayerView`; offline: own → `Hero` (4.12.1) |
| Presentation | `Presentation/Vfx/VfxRuntimeSet.cs`, `VfxCatalog.cs` | Bỏ `ZoneOverlayPrefab`, α/màu bo và bảng weapon → cue |
| Presentation | `Presentation/Vfx/VfxPreviewRig.cs` | **Mới** (Editor/Dev, chỉ dùng trong `VfxPreview`): `AddComponent<ThrowableView>().Bind(dummy0, null, match, fakeSource)`. Chỉ gọi API public, không sửa lớp đó |
| Bootstrap | `Bootstrap/Vfx/Bootstrap.Vfx.cs` | 4.4 đổi: bỏ bước 8 (ZoneOverlay). Thêm `Register(new ThrowableVfxSource(bus, offlineWorld, Resolve<OnlineThrowableSession>(), offlineWorld))`. Editor/Dev đăng ký thêm `FakeThrowableEventSource`. Vẫn gọi **sau** `ComposeWorld`, vì `OnlineThrowableSession` được tạo ở đó |
| Editor tool | `Tools/Editor/Vfx/VfxPrefabSanitizer.cs` | Bỏ menu "Build Zone Overlay Prefab" |
| Art | `Art/Vfx/Sprites/vfx_disc_1024.png`, `vfx_dash_8x2.png` | **Bỏ**. Giữ `vfx_area_frame.png`; thêm một sprite vòng nhỏ nếu Shield cần vòng 2D |
| Không sửa (chỉ đọc) | `ThrowableView.cs`, `ThrowableArtCatalog.cs`, `OnlineThrowableSession.cs`, `OfflineWorldSession*.cs`, `ZoneOverlayView.cs`, `ZoneOverlay.shader`, `MatchDemoView.cs`, `CombatWeaponPresenter.cs`, `HeldWeaponView.cs`, toàn bộ `Backend/` | AC14 mở rộng |

Các dòng khác của 4.3 giữ nguyên. Các file dùng chung phải ghi Agent_Coordination mục 4 trước khi sửa: `Bootstrap.cs` (1 dòng), `SortingLayers.cs` (1 dòng), `PlayerView.prefab`, `Hero.prefab`, `Scenes/Dev/VfxPreview.unity` (mới). `MatchDemoView.cs` rút khỏi danh sách.

#### 4.12.6. Bản dở của unity-dev trước merge (21 file `.cs`, 0 test, 0 Presentation/Bootstrap/prefab)

Bản dở được cất ở `C:/Users/HVU/AppData/Local/Temp/claude/c1594b47673d4e08/51975365-442b-4e2a-b21f-96220adeb628/scratchpad/vfx01_partial/` (giữ nguyên cấu trúc đường dẫn repo). Nó chỉ dùng API vẫn còn sau merge: `SfxDeduper`, `CombatEvent`, `OfflineWorldSession.CombatChanged`, `IBalanceConfigProvider.TryGetWeapon`, `IPlayerSettings`. Copy lại vào repo **kèm `.meta`**, giữ GUID.

| Nhóm | File | Xử lý |
| --- | --- | --- |
| Dùng nguyên | `Domain/CombatFeedback/StatusKind.cs`, `Domain/Vfx/AttackVfxLedger.cs`, `StatusAuraTable.cs`, `VfxBudget.cs`, `ScreenShakeModel.cs`, `Application/Ports/IVisibilityPolicy.cs` (có `VisibilityQuery.PositionOnly`), `Application/Visibility/AlwaysVisiblePolicy.cs`, `Visibility/Mocks/ToggleVisibilityPolicy.cs`, `Application/Vfx/IVfxTargetLocator.cs`, `LocalEffectStatusSource.cs`, các `.meta` thư mục rỗng (`Bootstrap/Vfx`, `Presentation/Vfx`, `Tests/*/Vfx`, `Tools/Editor/Vfx`) | Copy vào |
| Dùng, sửa nhỏ | `CombatFeedbackEvents.cs`, `VfxCue.cs`, `IVfxOutput.cs`, `VfxSpawn.cs`, `IVfxMatchFeed.cs`, `VfxMatchFeed.cs`, `MockVfxMatch.cs` | Theo 4.12.5 |
| Dùng, sửa vừa | `VfxDirector.cs` (541 dòng): bỏ `Zone`, nhánh nổ có `TryGetWeapon` + `LingerCue`, `Stop`; giữ ledger, live/aura/linger/flash, seam hỏi lại mỗi tick, reset theo session. `VfxScenarios.cs`: viết lại phần ném qua nguồn throwable giả | Theo 4.12.3 và 4.12.5 |
| Bỏ | `Domain/Vfx/ZoneVfxModel.cs` | D1 |
| Chưa có, phải viết | Toàn bộ Presentation, Bootstrap, Editor tool, prefab, scene, mọi test EditMode/PlayMode, `ThrowableVfxSource`, `FakeThrowableEventSource`, `VfxPreviewRig` | — |

#### 4.12.7. Tiêu chí nghiệm thu bị ảnh hưởng (đề xuất sửa mục 3; designer/CPO cập nhật sau khi người chốt)

| AC | Đề xuất |
| --- | --- |
| AC1 | Giữ. Bảng 16 dòng theo 4.12.4; "mọi prefab dùng lại/chỉnh có bản trong `_JungleMaze`" áp cho 9 prefab |
| AC2 | **Sửa:** scene preview phát được: hit (flash + số + hạt), stun, reverse, shield, **vòng ngòi nổ**, và một cú ném đầy đủ BANANA và SMOKE bằng `ThrowableEvent` giả. Cue của VFX-01 có active > 0 rồi về 0 đúng hạn. `ThrowableView.ActiveEffects` > 0 trong lúc nổ/khói (chỉ đọc, đo tích hợp). Vòng ngòi biến mất trong cùng frame với `Detonated`. Bỏ "cảnh báo bo" và "viền bo" |
| AC3 | **Sửa:** bỏ dòng ZoneState. Thêm: cùng `ThrowableEvent` (Id ≠ 0) đến 2 lần → 1 spawn; hai event `Id = 0` khác nhau trong 1 s → 2 spawn |
| AC6 | **Sửa:** bỏ "viền bo". Phạm vi là instance của VFX-01; `ThrowableView` không thuộc AC này (R14) |
| AC8 | Giữ cho hiệu ứng của VFX-01. Thêm ghi chú: hình của `ThrowableView` và `CombatWeaponPresenter` chưa qua seam (R6, R15) |
| AC10 | **Sửa:** bỏ phần kích cỡ vụ nổ (`ThrowableView` đã scale theo `Size`). Còn lại: khung ngòi nổ = `Size`×`Size` cell ±5%; quầng nằm trong ±1 cell quanh thân |
| AC11 | **Bỏ khỏi VFX-01** (D1). Nghiệm thu vùng bo thuộc ticket của Quang Huy. Thay bằng một dòng trong AC14: VFX-01 không thêm renderer bo và không sửa `ZoneOverlayView`/shader |
| AC12 | Giữ. Thêm: số lấy từ `Amount` của đồ ném; `Amount ≤ 0` → không có số, không flash |
| AC13 | Giữ. Thêm case `FollowOverride` (camera bám `PlayerView` online) |
| AC14 | **Mở rộng** danh sách file không được sửa (bảng 4.12.5) |
| AC15 | Giữ. Tải 10 nguồn gồm cả ném giả; ngân sách 64 chỉ tính instance của VFX-01; frame time đo cả scene (có `ThrowableView`) |
| AC16 | **Sửa:** VFX-01 chịu Stun / Reverse / Immune / Invulnerable / Poison / Invisible và vòng ngòi nổ. Root (dây leo) và "khói khác nổ" là hình của `ThrowableView`, chỉ ghi quan sát |

**Đề xuất sửa mục 1–2:**

- Mục 1 bỏ các cụm "nổ ném (chuối, thiên thạch, tinh thể, hạt, chuông), khói", "bo sắp thu" và "vùng ngoài bo tô tím α 0,4 kèm viền bo".
- Mục 2 "Ngoài phạm vi" thêm hai dòng: vùng bo (thế giới + minimap) → `ZoneOverlayView` của Quang Huy; hình vật ném/nổ/khói/bẫy/dây/dây leo/chuông → `ThrowableView` (WPN-03).
- Bảng 2.5 bỏ dòng "Bo tím alpha".

#### 4.12.8. Test cập nhật (so với 4.7)

- **Bỏ:** `ZoneVfxModelTests`, `ZoneOverlayPlayTests`, phần nổ của `VfxScaleTests`, các case zone trong `VfxDedupeTests`/`VfxMatchFeedTests`.
- **Thêm (EditMode):**
  - `ThrowableVfxSourceTests`: một case cho mỗi dòng bảng 4.12.3; `Amount = 0` → không có Hit; khoá `Id ≠ 0` lặp → 1; `Id = 0` × 2 → 2; khi offline active thì bỏ nguồn online; Item 6..8 không có `Thrown`.
  - `StatusCcImmuneGuardTests`: có CcImmune thì Stun/Reverse → 0 quầng; gỡ CcImmune rồi mới áp → 1 quầng.
  - `VfxAnchorRegistryTests`: online own → `PlayerView`, offline own → `Hero`.
- **Thêm (PlayMode):** `VfxThrowableIntegrationPlayTests` trong preview. Một `FakeThrowableEventSource` cấp cho cả `ThrowableView` và adapter. Kiểm: vòng ngòi sống từ `Thrown` tới `Detonated`; `ThrowableView.ActiveEffects` > 0 (AC2).
- Các test còn lại của 4.7 giữ nguyên.

#### 4.12.9. Thứ tự build và agent (thay danh sách bước của 4.8)

- **Agent:** chỉ unity-dev; backend-dev không có việc. Song song và quy tắc dùng chung Editor như 4.8. Commit như 4.8 (một commit build). Riêng 4.12 do CPO commit `docs(vfx-01): re-plan after merging main` sau khi người chốt D1/D2.
- **Thứ tự** (mỗi bước compile và test của bước đó phải xanh rồi mới sang bước sau):
  1. Copy Domain từ bản dở (bỏ `ZoneVfxModel`, cắt `VfxCue`) + EditMode test model.
  2. Application: port; `VfxDirector` đã cắt + luật CcImmune; `VfxMatchFeed` không zone; `LocalEffectStatusSource`; **`ThrowableVfxSource`**; mocks (`MockVfxMatch`, `FakeThrowableEventSource`, `VfxScenarios`) + EditMode test.
  3. Presentation: `VfxParticleEffect`, `DamageNumberVfx`, `VfxBodyAnchor`, `VfxAnchorRegistry` (luật mới), `VfxStage`, `CameraShakeApplier`, `VfxCatalog`, `VfxRuntimeSet`, `VfxDebugPanel`, `VfxPreviewRig`; `SortingLayers.Vfx`.
  4. Editor tool sanitizer + `vfx_area_frame.png` + 9 prefab + asset catalog/runtime set. **Pull main trước**, rồi thêm `VfxBodyAnchor` vào root của `PlayerView.prefab`/`Hero.prefab`, không đụng con.
  5. `Bootstrap.Vfx.cs` + 1 dòng `Bootstrap.cs`.
  6. `VfxPreview.unity`.
  7. PlayMode test + quét asset.
  8. Mục 5 (kiểm kê + ảnh), Developer Guide 4.8.

#### 4.12.10. Rủi ro mới hoặc đổi (bổ sung 4.9)

| # | Rủi ro | Giảm thiểu |
| --- | --- | --- |
| R1, R4 | Vòng debug chồng; răng cưa SpriteMask | **Đóng** (không còn vòng debug; VFX-01 không vẽ bo) |
| R3 | Nháy/quầng vào `Hero` ẩn khi online | Luật registry mới (4.12.1) + `VfxAnchorRegistryTests`; kiểm bằng GameServer local, ghi mục 5 |
| R14 | `ThrowableView` không dọn khi đổi trận: vỏ chuối (20 s) hoặc khói (10 s) có thể còn ở trận mới | Ngoài VFX-01. Follow-up cho Hào: dọn khi `MatchId`/phiên offline đổi |
| R15 | Hình của `ThrowableView` (bay, nổ, khói, dây leo, chuông) không qua seam: khi có FOW sẽ lộ vị trí người ném/bị trói | Mở rộng R6. FOW-01 gắn `IVisibilityPolicy` cho `ThrowableView` và `CombatWeaponPresenter` (follow-up) |
| R16 | `Seconds` của event không phản ánh miễn nhiễm; người khác chưa có nguồn CcImmune, nên quầng choáng có thể hiện trên người đang miễn nhiễm | Luật CcImmune (đúng với mình); chấp nhận cho người khác tới Game-10 |
| R17 | Thần dược gỡ stun/reverse trên server nhưng không có event gỡ cho người khác, nên quầng còn tới hết `Seconds` | Chấp nhận tới Game-10 |
| R18 | `ZoneOverlay` (Foreground 100, α 0,62) phủ lên quầng/số ở `Default` khi người đứng ngoài bo; số có thể khó đọc | Dev chụp ảnh ngoài bo ở mục 5. Nếu khó đọc thì đổi order/layer của **số** trong catalog (AC9 cho phép theo catalog), không sửa overlay |
| R19 | Hit spark + số chồng lên sprite nổ của `ThrowableView`, rối mắt | Catalog có cờ tắt hit spark cho Hit của đồ ném; dev quyết khi xem, ghi mục 5 |
| R20 | `PlayerView.prefab`/`Hero.prefab` vừa bị WPN-01 sửa nhiều, dễ conflict | Pull trước, chỉ thêm component ở root, ghi Agent_Coordination, commit sớm trong commit build |
| R21 | Đạn/cận chiến vẫn không có hit thật (`PlayerHit` chưa phát) | Như R7. Follow-up WPN-02 (server phát hit, cần G2 của ticket đó) |

**Follow-up đề xuất cho CPO** (không chặn VFX-01):

- Hào: `ThrowableView` dọn khi đổi trận (R14); nối `SetDanger` của HUD và dùng **`ThrowableEvent.Id`** làm id, để banner và vòng ngòi của VFX-01 khớp nhau (Q7).
- FOW-01: R15.
- WPN-02: R21.

#### 4.12.11. Quyết định người cần chốt lại

| # | Câu hỏi | Khuyến nghị |
| --- | --- | --- |
| D1 | G1 đã duyệt "bo tô #A274FF α 0,4 + viền đứt" (Q1), nhưng main đã có `ZoneOverlayView` tô **đỏ** α 0,62, viền đỏ đậm, đường trắng ở bo đích, cho cả thế giới và minimap. Giữ bản đỏ của Quang Huy và rút phần bo khỏi VFX-01 (bỏ AC11, bỏ dòng "Bo tím alpha" ở 2.5)? | **Có.** VFX-01 không vẽ bo. Nếu người vẫn muốn tím α 0,4 thì đó là việc đổi màu và `_FillAlpha` trong `ZoneOverlay.shader`, giao cho Quang Huy, không làm trong VFX-01 |
| D2 | Nổ, khói, bẫy chuối, dây leo, vòng chuông: để `ThrowableView` (Hào) vẽ như hiện tại, VFX-01 không copy 5 prefab nổ/khói JSurviv. VFX-01 chỉ nghe `IThrowableEventSource` (chỉ đọc) để vẽ vòng ngòi nổ, hit/số, quầng choáng/đảo hướng và rung? | **Có** (cách C ở 4.12.3; không sửa file nào của Hào) |

Mọi điểm khác của G1 (Q2–Q15 trừ Q1 và Q5; T-2…T-8) giữ nguyên. Q5 tự hết vì `ThrowableView` đã tô xanh Tinh thể. T-1 hết đối tượng.

#### 4.12.x. Kết quả duyệt lại (07/11/2026)

Người duyệt **phạm vi mới theo 4.12**: D1 = Có (giữ bo đỏ của `ZoneOverlayView`, rút toàn bộ phần bo khỏi VFX-01, bỏ AC11 và dòng "bo tím α 0,4"); D2 = Có (`ThrowableView` tiếp tục vẽ nổ/khói/bẫy/dây leo/vòng chuông; VFX-01 chỉ nghe `IThrowableEventSource` để vẽ vòng ngòi nổ, icon choáng, hit/số/hạt, quầng, rung; không sửa file của Hào/Quang Huy). Mọi quyết định G1 khác giữ nguyên. Đề xuất sửa spec ở 4.12.7 được coi là đã duyệt.

## 5. Nhật ký build

Unity-dev, 07/11/2026, nhánh `hvu` sau merge `11c78d20`. Làm theo 4.12 (D1, D2 đã duyệt) và phần còn hiệu lực của 4.1–4.11. Hash commit build ghi trong báo cáo cuối, vì commit này chứa chính mục 5.

### 5.1. Đã làm

- **Bản dở trước merge**: đã chép lại theo bảng 4.12.6, giữ GUID `.meta`. Bỏ `ZoneVfxModel.cs`. `VfxCue`, `ExplosionEvent`, `IVfxOutput`, `VfxSpawn`, `IVfxMatchFeed`, `VfxMatchFeed`, `MockVfxMatch` sửa theo 4.12.5. `VfxDirector` đã cắt phần zone, phần nổ (`TryGetWeapon`/`LingerCue`) và `Stop`; giữ ledger, live, aura, flash, seam và reset. Thêm vòng ngòi nổ riêng (`Fuse`), luật CcImmune, và chặn hit có `Damage ≤ 0`. `VfxScenarios` viết lại để ném qua nguồn throwable giả.
- **Mới ở Application**: `ThrowableVfxSource` (bảng 4.12.3; khoá theo `Id`, `Id = 0` dùng bộ đếm riêng; chỉ chặn `OnlineThrowableSession` khi offline đang chạy) và `Mocks/FakeThrowableEventSource` (cả chuỗi ném → ngòi → nổ → hit/bẫy/khói, chuông, gậy).
- **Presentation**: `VfxParticleEffect` (+ `IVfxSpawnListener`), `DamageNumberVfx`, `FuseWarningVfx`, `VfxBodyAnchor`, `VfxAnchorRegistry` (luật R3 mới), `VfxStage`, `CameraShakeApplier`, `VfxCatalog`, `VfxRuntimeSet`, `VfxDebugPanel` (F10), `VfxPreviewRig`. Thêm `SortingLayers.Vfx`.
- **Composition**: `Bootstrap/Vfx/Bootstrap.Vfx.cs`, cộng 1 dòng `ComposeVfx(tickRunner, match)` trong `Bootstrap.cs` ngay sau `ComposeWorld`.
- **Editor tool** (`Tools/Editor/Vfx`, asmdef `JungleMaze.Vfx.Editor`):
  - `VfxPrefabSanitizer`: bước máy móc ở 4.5, kèm menu "Sanitize Selected Prefab".
  - `VfxAssetBuilder`: menu **Jungle Maze → VFX → Build VFX-01 Assets** dựng sprite hình học, 9 prefab, bản sao material, catalog và runtime set. Chạy lại thì ghi đè tại chỗ, GUID giữ nguyên.
- **Asset**:
  - 9 prefab trong `Prefabs/Vfx`.
  - `Art/Vfx/Sprites/vfx_area_frame.png` (9-slice) và `vfx_area_fill.png`.
  - 6 bản sao material `Art/Vfx/Materials/*_vfx.mat`.
  - `Art/Vfx/VfxCatalog.asset`, `Art/Vfx/Resources/VfxRuntimeSet.asset`.
- **Prefab dùng chung**: chỉ thêm component `VfxBodyAnchor` ở root. `PlayerView.prefab` +17 dòng, giá trị mặc định. `Hero.prefab` +17 dòng, `localHero: 1`. Không đổi gì khác (đã kiểm `git diff`).
- **Scene** `Scenes/Dev/VfxPreview.unity` (không thêm vào Build Settings):
  - camera size 8 như Game, lưới 1 cell;
  - 11 dummy, dummy 0 là "own" (`localHero`), dummy 1–10 có `previewNetId`;
  - `VfxPreviewRig` gắn `ThrowableView` thật vào cùng nguồn ném giả;
  - panel F10 tự mở trong scene này.
- **Docs**: `Client_Developer_Guide.vi.md` thêm mục 4.8; `Agent_Coordination.vi.md` mục 4 thêm dòng giữ/trả Editor và các file dùng chung đã sửa.

### 5.2. Lệch kế hoạch và lý do

| # | Lệch | Lý do |
| --- | --- | --- |
| L1 | `IVfxMatchFeed` thêm `bool Online`, bỏ `NowTick` và `TryGetZone` | Registry cần biết đang online để áp luật R3 mới (online thì thân mình là `PlayerView`, offline là `Hero`). `NowTick` chỉ phục vụ bo, mà bo đã bỏ (D1) |
| L2 | Prefab dựng bằng tool lặp lại được (`VfxAssetBuilder`), không chỉnh tay trong Editor | Bước "phần tay" ở 4.5 được ghi thành công thức cho từng prefab, nên ai dựng lại cũng ra cùng kết quả. Root scale = 1; phần thu nhỏ nằm ở con `Fx` (scaling Hierarchy) |
| L3 | Bản sao material `Art/Vfx/Materials/*_vfx.mat` với `_BaseColor` trắng | Material Epic Toon có `_BaseColor` = 0,5/0,5 (làm cho shader particle cũ nhân đôi), nên trong URP Particles/Unlit chỉ sáng một nửa. Ảnh lần 1 quá mờ. Đây là cách 4.5 bước 3 cho phép: copy, không sửa bản gốc |
| L4 | Shield dùng material vòng trung tính của `StunnedVfx` thay cho `circlecurve_magic`, và bỏ sắc màu trong colour-over-lifetime | Texture/gradient gốc màu vàng, nhân với tint trắng vẫn ra vàng, nên CcImmune không trắng được (Equipments 13). Đã kiểm bằng ảnh: Invulnerable vàng, CcImmune trắng |
| L5 | Stun: sao dùng `star_ADD` + quỹ đạo quanh đầu; vòng chuyển Mesh → Billboard | Sao alpha-blend quá tối. Sao bay thẳng ra xa không đọc được là "choáng" |
| L6 | Thêm sprite `vfx_area_fill.png` (8 px trắng) | Khung 9-slice có tâm trong suốt; nền mờ của vùng ngòi nổ cần một sprite đặc |
| L7 | Order trên layer `Default`: quầng 100, hạt 105, số 110; ngòi nổ ở `VFX`, order 10 (nền 9) | T-2. Tách order để số luôn nằm trên quầng |
| L8 | `Tests/PlayMode/JungleMaze.Tests.PlayMode.asmdef` thêm reference `JungleMaze.World` | PlayMode test cần `ThrowableKind`/`ThrowableEvent` để chạy `ThrowableView` cùng adapter (4.12.8) |
| L9 | `VfxStage.SceneChanged` là event; `Bootstrap.Vfx` nối nó vào `director.ResetAll` | Stage không biết Director (đúng chiều phụ thuộc). Stage vẫn tự `ReleaseAll` khi `activeSceneChanged` |
| L10 | Hit spark vẫn hiện khi trúng đồ ném (R19) | Hạt nằm ở thân mục tiêu, còn nổ của `ThrowableView` nằm ở tâm vùng; xem ảnh thì không rối. Không thêm cờ |
| L11 | `AttackPuff` giữ (T-5); làm từ `SparkExplosion2D` thu nhỏ (0,15), màu bụi nhạt | Cần đối tượng cho AC3/AC4 phần Attack |
| L12 | Builder chỉ lưu asset của mình (`SaveAssetIfDirty`), không dùng `AssetDatabase.SaveAssets()` | Lượt dựng đầu dùng `SaveAssets()` nên đã ghi 26 material Epic Toon mà URP tự nâng cấp khi nạp. Đã `git checkout` toàn bộ, xem 5.6 |
| L13 | Test tải AC15 chạy 60 s **mô phỏng** (3600 bước, không chờ thời gian thật) | Đo số instance và log lỗi, không đo frame time của cả frame. Số ms chỉ là của logic VFX trong Editor |

### 5.3. Kiểm kê cuối (thay cột "Quyết định" của 2.1; chốt sau khi xem trong Editor)

| Prefab JSurviv | Quyết định | Bản trong `_JungleMaze` | Chốt trong Editor |
| --- | --- | --- | --- |
| `SmokeVfx` | Loại bỏ | — | D2: `ThrowableView` vẽ khói bằng cùng sheet. Ảnh 06 cho thấy khói đã có |
| `BananaExploitVfx` | Loại bỏ | — | D2 |
| `MeteoExploitVfx` | Loại bỏ | — | D2 (CRYSTAL do `ThrowableView.BlastTint` tô xanh) |
| `SeedExploit` | Loại bỏ | — | D2. Mục "giọt stretched trông 3D" không còn cần xác minh |
| `BellExploit` | Loại bỏ | — | D2 (vòng vuông 7×7 và chuông lắc của `ThrowableView`) |
| `StunnedVfx` | Chỉnh | `Vfx_Status_Stun` | Hệ gốc là **mesh Quad** (vòng `ring (alphablended)`), không phải sphere như 2.1 ghi. Đã chuyển sang Billboard, bỏ `ETFXRotation`, cho vòng tự xoay, sao `star_ADD` bay quỹ đạo quanh đầu, fit 0,6, offset +0,75 cell. 2 hệ particle |
| `ReverseVfx` | Chỉnh | `Vfx_Status_Reverse` | Đổi sang màu cam (1, .55, .1); `Glow` chuyển World → Local; hạt 0,22, 14/s; offset +0,55. 2 hệ |
| `ImmortalVfx` | Chỉnh | `Vfx_Status_Shield` | Hệ gốc là **mesh Quad** (`circlecurve_magic`), đã chuyển sang Billboard và đổi sang material vòng trung tính (L4); fit 0,45 (≈1,35 cell). Tint 4 = vàng (Invulnerable). 2 hệ |
| `ImmunityVfx` | Biến thể của Shield, không copy | `Vfx_Status_Shield` tint 5 = trắng | Ảnh 03: vòng trắng, khác vòng vàng |
| `PoisonsVfx` | Chỉnh (P2, đã làm) | `Vfx_Status_Poison` | Bỏ `Skull` (lệch phong cách); mây xanh lặp 4/s, cỡ 1,6, tốc 0,5; giọt 5/s. 2 hệ |
| `BleedVfx`, `BlindVfx`, `BuffDamageVfx`, `BurnedVfx`, `CantUseWeaponVfx`, `WaterVfx` | Loại bỏ | — | Như 2.1 |
| (không có nguồn riêng) | Mới từ sparkle của `ImmortalVfx` | `Vfx_Status_Invisible` | Chỉ có sparkle `sparkle_ADD`, xanh nhạt α 0,75, 16/s. **Rất mờ khi nhìn ở camera 8** (ảnh 03): đúng ý "aura mờ" nhưng cần người cảm nhận AC16 |
| Root | NoVisual | — | Dây leo do `ThrowableView` vẽ |

| Nhóm Epic Toon | Quyết định | Chốt |
| --- | --- | --- |
| `Prefabs 2D/Explosions` | Dùng **`SparkExplosion2D`** cho `Vfx_Hit_Spark` (fit 0,25: glow ≈ 0,68 cell, 0,2 s; tia 6 cell/s × 0,3 s) và `Vfx_Attack_Puff` (fit 0,15, ≤ 0,25 s) | Loại hai bản kia theo thông số đọc trong Editor, chưa dựng thử để chụp: `MetalHit2D` dùng scaling Shape, phần lóe chỉ 0,15 nên thu về 48 px gần như mất; `StarBurst2D` có tốc 16–28 nên văng ra xa quá 1 cell |
| `Combat/BombFuse` | Loại bỏ | `ThrowableView` đã nháy vật ở điểm rơi; vòng ngòi nổ dùng khung `vfx_area_frame` |
| Các nhóm còn lại (`Text`, `Blood`, `Muzzleflash`, `Sword`, `Nova`, `Demo`, `ETFX*`…) | Như 2.1 | — |

`git diff --stat -- Assets/Imported/JSurviv` lúc commit: rỗng.

### 5.4. Test (số thật, Unity 6000.3.15f1 qua MCP, 07/11/2026)

| Bộ | Tổng | Pass | Fail | Ghi chú |
| --- | --- | --- | --- | --- |
| EditMode `JungleMaze.Tests.Vfx` (mới) | 107 | 107 | 0 | Model, dedupe (AC3), reconcile (AC4, mọi `CombatOutcome ≠ Accepted`), lifetime/CcImmune (AC5), reset với pool thật (AC6), pool reuse/handle cũ (AC7), seam (AC8), hit/số (AC12), luật rung (AC13), budget (AC15), `ThrowableVfxSource` (4.12.3), registry R3, nguồn local effect, feed, kiến trúc (AC12/AC14), catalog (AC1), quét prefab (AC9), khung ngòi ±5% (AC10) |
| EditMode toàn bộ | 1089 | 1083 | 6 | 6 fail **không thuộc VFX** (xem dưới) |
| PlayMode `JungleMaze.Tests.PlayMode.Vfx` (mới) | 13 | 13 | 0 | AC2 (hit; stun/reverse/shield; ném chuối có `ThrowableView`; ném khói), đổi phòng (AC6), seam (AC8), quầng trong 1 cell (AC10), 0 B GC/frame với 20 hiệu ứng sau 120 frame (AC7), console sạch sau 10 s (AC9), tải 60 s (AC15), rung ở mép map, `FollowOverride` và model 0 (AC13) |
| PlayMode toàn bộ | 146 | 144 | 2 | Lần cuối. Lần trước đó: 145/146 |

Test đỏ không thuộc VFX-01:

- `LevelDocumentTests` (4 test), `Phase2Tests.PackagedMapHashMustMatchServer`, `OnlineWorldIntegrationTests.NormalWorldUsesAuthoredTeamMapWithFixedInteractivePlacements`. Cả sáu chỉ đếm vật thể trong JSON map ở `StreamingAssets` (vd mong 4 cover, đọc được 167). Dữ liệu map đổi ở commit main `68e23065` "Integrate Thap Co and Thi Tran as the default maps". VFX-01 không đụng `StreamingAssets`, `git status` sạch ở đó, và không test nào tham chiếu code VFX. **Chưa chạy lại trên bản HEAD tách riêng** để đối chứng.
- `AudioAcceptanceTests.ZoneBell_ThreeBellsOnAlert_DucksMusic_SfxUntouched` ("three bell starts", mong 3, được 1). Fail ổn định cả khi chạy riêng. Test tự dựng audio, không dùng Bootstrap hay code VFX. Console có `FMOD failed to switch back to normal output … Error initializing output device`, nên có thể do thiết bị âm thanh của máy. **Chưa đối chứng trên HEAD.**
- `DepthSortingPlayTests.HeroWithInterpolatedRigidbodySortsByFinalFramePose` fail một lần trong lượt chạy toàn bộ cuối (hero đi được 10,10, mong > 10,2), chạy riêng thì pass. Test tự tạo `GameObject` Hero, không dùng `Hero.prefab`; lỗi phụ thuộc thời gian physics (flaky).

Console sau compile: 0 lỗi; chỉ còn cảnh báo cũ (`enableWordWrapping` obsolete, CS0067 trong `SoloOfflineTests`) và cảnh báo WebSocket của MCP.

### 5.5. Kiểm trong Editor và ảnh

Ảnh (Game view, camera `Main Camera`, khung 842×575) nằm ở `Docs/Reports/VFX-01_2026-11-07/`:

| Ảnh | Nội dung |
| --- | --- |
| `01_overview_auras_camera8.png` | Toàn cảnh preview ở camera 8 (cỡ của Game), 6 quầng cùng lúc |
| `02_stun_reverse_invulnerable.png` | Choáng (sao + vòng trên đầu), đảo hướng (cam), bất tử (vòng vàng) |
| `03_immune_invisible_poison.png` | Miễn CC (vòng trắng), tàng hình (sparkle mờ), độc (mây xanh) |
| `04_hit_flash_number_spark.png`, `04b_damage_number_closeup.png` | Nháy thân, số sát thương (font HudNumber, "45" và "12"), hạt trúng đòn |
| `05_fuse_warning_banana3x3_smoke7x7.png` | Vòng ngòi nổ 3×3 (chuối) và 7×7 (khói) khớp lưới 1 cell; vật đang bay do `ThrowableView` vẽ |
| `06_after_detonation_throwableview.png` | Sau `Detonated`: vòng ngòi đã mất; khói và vỏ chuối do `ThrowableView` vẽ |

Kiểm thêm trong **trận offline thật** (Boot → Luyện tập offline → Game):

- `VfxAnchorRegistry` trả anchor `Hero` (`localHero`) cho own net id 0.
- `IEffectStateWriter.Apply(Poison)` hiện quầng độc trên Hero (đi qua `LocalEffectStatusSource`).
- Hit lên chính mình: số đỏ, Hero nháy, rung biên độ 0,079 cell, offset camera (−0,01; 0,07).
- Ném chuối giả: vòng 3×3 hiện.
- Console 0 lỗi/0 cảnh báo VFX.
- Ảnh tạm của lượt này ở `Temp/VfxShots/07_*` (không commit).

**Rung màn hình chưa chụp được bằng ảnh tĩnh**; chỉ có số đo ở trên và test PlayMode.

### 5.6. Mục "cần dev xác minh" và việc còn mở

- **R3 online (GameServer local)**: **chưa kiểm chứng**. Muốn vào phòng online ở protocol v4 phải có vé tài khoản (API + MongoDB + GameServer, `NetworkClient` dòng 181). Lượt build này không dựng chuỗi đó. Phần đã có: luật đã code và có test `VfxAnchorRegistryTests.Online_OwnIsThePlayerView_NotTheHero` / `Offline_OwnIsTheHero`. Cần QA chạy online và nhìn quầng/nháy nằm trên `PlayerView` của mình.
- **R18** (overlay bo `Foreground` 100 phủ lên số/quầng ở `Default` khi đứng ngoài bo): **chưa chụp ảnh ngoài bo**. Nếu khó đọc thì đổi layer/order của số trong catalog.
- **AC16** (phân biệt 6 quầng ở 6 cell, ≥ 2 người) và **cảm nhận cường độ rung (AC13)**: cần người. Tàng hình là quầng dễ bị bỏ sót nhất.
- **AC15 frame time cả scene** và **Profiler GC toàn frame**: chưa đo. Test chỉ đo logic VFX (0,072 ms mỗi bước mô phỏng, max 13 instance vì bộ sinh tải nhẹ) và 0 B trong `Director.Tick + Stage.Drive`.
- **Material Epic Toon bị URP sửa khi nạp**: Unity tự nâng cấp material trong `Assets/Imported/JSurviv/Epic Toon FX/Materials` (`disabledShaderPasses`, `_DstBlendAlpha`…) khi chúng được nạp. Test Runner và `SaveAssets()` sẽ ghi các thay đổi đó xuống đĩa. Đã `git checkout` 2 lần và reimport, nên lúc commit `Assets/Imported` sạch. Phiên sau nếu thấy các `.mat` này bị sửa thì đừng commit, hãy `git checkout -- "Assets/Imported/JSurviv"`.
- **Project map**: chưa chạy `Tools/update_project_map.py` vì checkout này không có `graphify-out/.graphify_python`.
- **Follow-up giữ nguyên như 4.12.10**: R14 (Hào dọn `ThrowableView` khi đổi trận), Q7 (`SetDanger` dùng `ThrowableEvent.Id`), R15 (FOW cho `ThrowableView`), R21 (WPN-02 phát hit đạn/cận chiến).

### 5.7. Sửa sau review vòng 1 (07/11/2026, trên `7adb1aa5`)

Hash commit sửa ghi trong báo cáo cuối, vì commit này chứa chính mục này.

| Mục review | Đã sửa | File |
| --- | --- | --- |
| Nên sửa 1 (AC7, GC cả frame) | Thêm PlayMode test `VfxFrameGcPlayTests.WholeFrame_NumberFuseFlashAuras_VfxAddsZeroGcAfterWarmup`. Test chạy trên frame thật, có camera nên particle, sprite và số TMP đều được render. Logic VFX được tick từ `Update` (`VfxFrameDriver`). Lúc đo có: 1 số "45" đang bay, 1 vòng ngòi nổ 3×3 đang nháy, thân số 3 đang nháy, 4 quầng (choáng, độc, bất tử, tàng hình) có hạt đang sống. Flash và số dùng thời lượng 60 s để vẫn còn chạy sau 120 frame warmup. Đọc counter `ProfilerRecorder` "GC Allocated In Frame" (Memory) qua 300 frame. Có thêm kiểm tra chính xác `Is.Not.AllocatingGCMemory` trên mọi lệnh VFX chạy mỗi frame (`Director.Tick`, `Stage.Drive`, `VfxBodyAnchor.Advance` ×11, `DamageNumberVfx.Advance`, `FuseWarningVfx.Advance`) | `Tests/PlayMode/Vfx/VfxFramePlayTests.cs` (mới), `VfxPlayTests.cs` (`VfxPlayRig` nhận tuning tuỳ chọn) |
| Gợi ý 2 (đăng ký `AlwaysVisiblePolicy`) | `Bootstrap.Vfx` không đăng ký policy mặc định vào registry nữa. Nếu registry chưa có `IVisibilityPolicy` thì dùng `DeferredVisibilityPolicy` (Application). Policy này mỗi lần hỏi sẽ tra registry, đến khi FOW-01 đăng ký thì chuyển hẳn sang policy đó; trước lúc ấy cho hiện tất cả. FOW-01 đăng ký trước hay sau `ComposeVfx` đều không ném lỗi trùng kiểu. Doc `IVisibilityPolicy` và `Client_Developer_Guide` 4.8 đã cập nhật | `Application/Visibility/DeferredVisibilityPolicy.cs` (mới), `Bootstrap/Vfx/Bootstrap.Vfx.cs`, `Application/Ports/IVisibilityPolicy.cs`, `Docs/Client_Developer_Guide.vi.md` |
| Gợi ý 5 (vòng ngòi tự gỡ khi hết giờ) | Thêm `VfxTuningValues.FuseGraceSeconds` = 0,15 s (field `fuseGraceSeconds` trong `VfxRuntimeSet`/asset). Vòng ngòi chỉ tự gỡ khi `Remaining ≤ −grace`. `Detonated` vẫn gỡ ngay trong cùng lệnh. Vòng đang bị che lúc hết giờ thì không vẽ lại trong khoảng grace | `Domain/Vfx/VfxCue.cs`, `Presentation/Vfx/VfxRuntimeSet.cs`, `Art/Vfx/Resources/VfxRuntimeSet.asset`, `Application/Vfx/VfxDirector.cs` |
| Gợi ý 1 (`PlayerView` chưa Bind) | `PlayerView` thêm getter `IsBound` (đúng một dòng, read-only, không đổi hành vi). `VfxBodyAnchor.NetId` trả 255 khi view chưa bind. Thêm `HasIdentity`: Hero, dummy preview, hoặc view đã bind. `IsShowing` cần `HasIdentity`, nên registry bỏ qua view chưa bind (và view đã despawn) | `Presentation/Player/PlayerView.cs` (+3 dòng), `Presentation/Vfx/VfxBodyAnchor.cs` |
| Gợi ý 3, 4 | **Để sau**, không làm trong vòng này: `VfxDirector` dùng `App.Audio.SfxDeduper`; số trình bày mặc định lặp 3 nơi và nhịp nháy vòng ngòi cứng trong `FuseWarningVfx` | — |

Lệch so với yêu cầu sửa:

- **Gợi ý 1, "chỉ đăng ký anchor khi đã bind"**: anchor vẫn tự đăng ký khi `OnEnable`, nhưng registry không trả anchor chưa bind (`IsShowing` = false, `NetId` = 255). Lý do: `RemotePlayerSpawner` gọi `Create` (kích hoạt `OnEnable`) rồi mới gọi `Bind` ngay sau đó. Nếu chờ bind mới đăng ký thì cần một hook bind trong `PlayerView`, hoặc chịu trễ 1 frame (hit đến đúng frame spawn sẽ không có nháy). Lọc lúc tra cứu thì cho cùng kết quả mà không cần hook. Không có cách nào biết view đã bind mà không thêm getter: `NetId` của view chưa bind cũng là 0, trùng với net id 0 thật. Vì vậy đã thêm getter `IsBound`, đúng phạm vi người giao cho phép (file của MOV-03).
- **Nên sửa 1, assert "0 B"**: trong Editor, counter "GC Allocated In Frame" tính cả phần Editor và test runner cấp phát, nên tổng cả frame không bao giờ bằng 0. Test vì vậy assert hai điều: (a) mọi lệnh VFX chạy mỗi frame cấp phát 0 B (`AllocatingGCMemory`, chính xác); (b) trung vị và min mỗi frame khi có VFX ≤ khi đã gỡ hết VFX, trong cùng scene, cùng driver, đo ngay sau đó. Số đo:

| Lượt | Recorder | Kiểm soát (+64 KB trong `Update`) | Có VFX (300 frame) | Không VFX (300 frame) | Phần của VFX |
| --- | --- | --- | --- | --- | --- |
| Chạy riêng test | `Valid = true` | 80 919 B (nền ≈ 16 KB + 65 536 B) | trung vị 14 241 B, min 14 241, max 31 798, tổng 4 369 533 | trung vị 14 653 B, min 14 653, max 63 072, tổng 4 460 063 | **≤ 0 B/frame** |
| Bản đầu của test (assert tổng = 0) | `Valid = true` | 81 221 B | trung vị 16 065 B | nền trước VFX: 15 653 B | +412 B, **không phải VFX** (xem dưới) |

Đo chẩn đoán từng hiệu ứng riêng (test tạm, đã xoá, không commit): 120 frame trước và 120 frame sau khi bật **một** hiệu ứng. Số, ngòi, nháy, choáng, độc, bất tử, tàng hình, hạt trúng đòn: chênh **0 B/frame**. Trường hợp `none` (không bật gì) cũng chênh +412 B. Trường hợp `nodriver` (không tick VFX) nền vẫn là 16 065 B/frame. Kết luận: khoảng 14–16 KB/frame là của Editor/test runner, có cả khi scene không có object VFX. Bước +412 B là nền Editor tăng theo thời gian sau khi vào Play mode, không phụ thuộc VFX. Lượt này không mở Profiler để chỉ ra cụ thể đoạn code Editor nào cấp phát. Số GC trên bản build (Development + Profiler) vẫn là việc QA (Nên sửa 1, phần QA).

Test (số thật, Unity 6000.3.15f1 qua MCP, 07/11/2026):

| Bộ | Tổng | Pass | Fail | Ghi chú |
| --- | --- | --- | --- | --- |
| EditMode `JungleMaze.Tests.Vfx` | 114 | 114 | 0 | +7 so với lượt build: fuse grace (3 test mới, `NoDetonate_WarningEndsWithTheFuse` đổi thành `…PlusTheGrace`), `DeferredVisibilityPolicyTests` (4) |
| PlayMode `JungleMaze.Tests.PlayMode.Vfx` | 15 | 15 | 0 | +2: `VfxFrameGcPlayTests` (AC7 cả frame), `VfxBodyAnchorBindingPlayTests` (view chưa bind / despawn không phải anchor; online lẫn offline) |
| EditMode toàn bộ | 1096 | 1090 | 6 | Vẫn đúng 6 test map-data như 5.4 (`LevelDocumentTests` ×4, `Phase2Tests.PackagedMapHashMustMatchServer`, `OnlineWorldIntegrationTests.NormalWorldUsesAuthoredTeamMapWithFixedInteractivePlacements`). Không có test mới đỏ |
| PlayMode toàn bộ | — | — | — | Không chạy lại trong vòng này |

- Console sau compile: 0 lỗi. Cảnh báo còn lại là cảnh báo cũ (`enableWordWrapping`, CS0067), cộng log cảnh báo do các test khác cố ý tạo (`[Pool] … Despawn hai lần`, `[Audio] No clip for SFX 'Throw'`).
- `git diff -- Assets/Imported`: rỗng.
- `ProjectSettings/EditorSettings.asset` bị lượt chạy PlayMode đổi `m_EnterPlayModeOptions` 0 → 1. Đã đặt lại `None` trong Editor rồi `git checkout`, không commit.

## 6. Review

### Vòng 1 (07/11/2026, code-reviewer, commit `7adb1aa5`): **Qua** — 0 Chặn, 4 Nên sửa, 5 Gợi ý

Đã kiểm đạt: phạm vi 4.12 (không vẽ bo/nổ/khói, không chạm `Throwable*`/`ZoneOverlay*`, `Assets/Imported` và `Backend` không đổi); `Hero.prefab`/`PlayerView.prefab` chỉ thêm `VfxBodyAnchor`; lớp (Domain không UnityEngine, Application không tham chiếu Presentation); DI qua Bootstrap/`[Inject]`, không singleton/`FindObjectOfType`; không ghi HUD/HP; khử trùng (`Id = 0` không khử), đối soát `RequestSeq`, reset khi đổi phòng (64 slot + generation, `activeSceneChanged`), seam visibility hỏi lại mỗi tick, hẹn giờ trạng thái làm mới không cộng dồn, `CameraShakeApplier` (order 1100, trả vị trí, tắt khi `ScreenShakeEnabled = false`), nháy thân không rò tint. Nghi vấn chữ số "2" bị lật: **không phải lỗi** — ảnh `04b` là số 45 (glyph "5" của Pixelify Sans giống "S"), ảnh `04` có "12" đúng chiều.

Nên sửa:
1. AC7: test GC chỉ bọc `Director.Tick`/`Stage.Drive`; chưa đo cả frame (`VfxBodyAnchor.Update`, `DamageNumberVfx.LateUpdate`, `FuseWarningVfx.LateUpdate`, hạt) → thêm PlayMode test `ProfilerRecorder` "GC Allocated In Frame" (dev) + QA ghi số Profiler.
2. R3 online (`VfxAnchorRegistry.cs:35-58`) chưa chạy thật → QA chạy GameServer local, ảnh quầng/nháy trên `PlayerView` của mình (P0).
3. Test đỏ "không thuộc VFX" chưa đối chứng trên `83e11065`, nhất là `AudioAcceptanceTests.ZoneBell_ThreeBellsOnAlert…` → QA đối chứng trên bản copy sạch.
4. AC15 chỉ mô phỏng logic, chưa đo frame time cả scene → giữ là việc mở.

Gợi ý: (1) `VfxBodyAnchor` coi `PlayerView` chưa `Bind` (NetId 0) là hợp lệ; (2) `Bootstrap.Vfx.cs:44-47` đăng ký `AlwaysVisiblePolicy` vào registry — FOW-01 đăng ký sau sẽ ném trùng kiểu; (3) `VfxDirector` dùng `App.Audio.SfxDeduper` (phụ thuộc chéo feature); (4) số trình bày mặc định lặp ở 3 nơi, nhịp nháy vòng ngòi cứng trong `FuseWarningVfx.cs:48`; (5) `VfxDirector.cs:486` vòng ngòi tự gỡ khi `Remaining ≤ 0` có thể biến mất trước vụ nổ khi lag → thêm dung sai 0,1–0,2 s.

### Vòng 2 (07/11/2026, code-reviewer, commit `843da542`): **Qua** — 0 Chặn, 2 Nên sửa (nhẹ), 2 Gợi ý

Đã kiểm đạt: test GC gọi đúng đường chạy thật (`VfxBodyAnchor.Update`, `DamageNumberVfx.LateUpdate`, `FuseWarningVfx.LateUpdate`, `VfxStage.LateUpdate` → `Advance`/`Drive`), recorder có đối chứng dương; chấp nhận "từng lệnh VFX 0 B + VFX ≤ nền Editor" thay cho "tổng = 0 B". `DeferredVisibilityPolicy`: FOW-01 đăng ký trước/sau đều không ném, không alloc mỗi lần hỏi. `FuseGraceSeconds` 0,15 s từ `VfxRuntimeSet`, `Detonated` gỡ ngay. `PlayerView.IsBound` không đổi hành vi (`_model = null` ở `OnDespawned`), registry lọc anchor chưa bind. Không chạm Backend/ProjectSettings/Imported/file của Hào và Quang Huy.

Nên sửa (không chặn, để sau): (1) so trung vị có/không VFX đo theo thứ tự cố định nên nền Editor tăng dần có thể che alloc nhỏ — coi là chống alloc lớn, hoặc xen kẽ bật/tắt; (2) khối `Is.Not.AllocatingGCMemory` chỉ chạy 1 frame, có thể lọt alloc theo chu kỳ — bọc 60–120 lần gọi. Gợi ý: tra registry thêm mỗi query khi chưa có FOW-01 (không đáng kể); `VfxDirector.cs:497` hợp lý.

## 7. QA

**Không chạy QA** cho ticket này theo yêu cầu của người (07/11/2026: chỉ chạy QA khi người yêu cầu). Các mục chỉ QA chứng minh được ghi "chưa kiểm chứng" trong bảng dưới.


Bảng tổng hợp của CPO từ bằng chứng build (mục 5) và review (mục 6), không có lượt QA độc lập:

| # | Kết quả | Bằng chứng |
| --- | --- | --- |
| AC1 | ✓ | Kiểm kê cuối ở 5.3; `git diff --stat -- Assets/Imported` rỗng ở cả hai commit |
| AC2 (sửa theo 4.12.7) | ✓ | Scene `Scenes/Dev/VfxPreview.unity` + F10; ảnh `Docs/Reports/VFX-01_2026-11-07/01–06`; PlayMode nhóm Vfx 15/15 |
| AC3 | ✓ | EditMode nhóm Vfx (khử trùng theo `EventKey`, `Id = 0` không khử) |
| AC4 | ✓ | EditMode `AttackVfxLedger` (predicted/confirmed/rejected/muộn) |
| AC5 | ✓ | EditMode `StatusAuraTable` với đồng hồ giả |
| AC6 | ✓ | EditMode + PlayMode reset khi đổi phòng/scene |
| AC7 | ✓ một phần | Từng lệnh VFX mỗi frame 0 B (`VfxFrameGcPlayTests`); VFX ≤ nền Editor; chưa đo trên Player build |
| AC8 | ✓ | EditMode/PlayMode với `ToggleVisibilityPolicy`; mặc định `DeferredVisibilityPolicy` |
| AC9 | ✓ | Test quét prefab VFX (không Light/ETFX/mesh 3D, layer, shader URP) |
| AC10 | ✓ | Ảnh `05_fuse_warning_banana3x3_smoke7x7.png` có lưới 1 cell |
| AC11 | — | Bỏ khỏi VFX-01 (D1: bo do `ZoneOverlayView`) |
| AC12 | ✓ | Test nháy 100 lần trả màu gốc; ảnh `04`/`04b` (số 45, 12) |
| AC13 | ✓ phần đo | Test offset = 0 khi tắt; trận offline đo biên độ 0,079 cell; **cảm nhận chưa kiểm** |
| AC14 | ✓ | Review vòng 1–2 |
| AC15 | chưa kiểm chứng | Chỉ mô phỏng logic 60 s (0,072 ms/bước); chưa đo frame time cả scene |
| AC16 | chưa kiểm chứng | Cần ≥ 2 người cảm nhận khi playtest |
| R3 online | chưa kiểm chứng | Chỉ có `VfxAnchorRegistryTests`; chưa chạy phòng online thật |
| R18 | chưa kiểm chứng | Chưa có ảnh số/quầng khi đứng ngoài bo đỏ |

Test đỏ không thuộc VFX (chưa đối chứng trên `83e11065`): EditMode 6 test dữ liệu map (`LevelDocumentTests` ×4, `Phase2Tests.PackagedMapHashMustMatchServer`, `OnlineWorldIntegrationTests.NormalWorld…`); PlayMode `AudioAcceptanceTests.ZoneBell_ThreeBellsOnAlert…`, `DepthSortingPlayTests.HeroWithInterpolatedRigidbody…` (flaky).

## 8. Nghiệm thu

**G3 07/11/2026: người nghiệm thu, ticket Done.** Commit: `b5694dc3` (spec + plan), `83e11065` (re-plan sau merge main), `7adb1aa5` (build), `843da542` (sửa review vòng 1), `a1cd1eb5` + commit đóng (review vòng 2, trạng thái AC). Không chạy QA theo yêu cầu của người.

Việc còn mở (không chặn Done):
- Chạy phòng online thật để xác nhận quầng/nháy/số của mình nằm trên `PlayerView` (R3) và ảnh khi đứng ngoài bo đỏ (R18).
- Đo GC/frame time trên Player build (AC7, AC15); playtest ≥ 2 người cho AC16 và cảm nhận rung (AC13).
- Đối chứng các test đỏ không thuộc VFX trên `83e11065`.
- Follow-up ticket khác: Hào/WPN-03 — `ThrowableView` dọn khi đổi trận; FOW-01 — đưa `ThrowableView` qua seam `IVisibilityPolicy` (đăng ký policy trước hay sau `ComposeVfx` đều được); WPN-02 — server phát `PlayerHit` cho đạn/cận chiến (VFX-01 đã có đường nhận); Game-10 — nguồn trạng thái thật của người khác (Invulnerable, CcImmune).
- Báo Hào/MOV-03: `PlayerView` thêm getter chỉ đọc `IsBound`.
- Gợi ý review để sau: chuyển `SfxDeduper` lên Core nếu có nơi thứ ba dùng; gom số trình bày mặc định về một nơi; test GC chặt hơn (xen kẽ bật/tắt, bọc nhiều frame).

## Phụ lục. Câu hỏi mở cần chốt ở G1 (kèm khuyến nghị)

| # | Câu hỏi | Khuyến nghị |
| --- | --- | --- |
| Q1 | "Bo tím alpha 0,4" nghĩa là gì? Ticket nói α 0,4; Figma `48:461` chỉ vẽ viền đứt nét tím ngoài thế giới, không tô vùng ngoài. Màu, và α 0,4 áp cho tô ngoài hay cho viền? | Tô phần ngoài bo #A274FF α 0,4 **và** viền đứt nét tím đậm hơn (α 1,0). Cần người xác nhận vì tô 40% che bớt thế giới ngoài bo. |
| Q2 | Nguồn `HitConfirmed`: server chưa phát `EventPacket.PlayerHit`. Chờ WPN-02 hay suy từ HP snapshot? | **Chờ WPN-02**; không suy từ độ giảm HP (không có nguồn/đích/vị trí/seq, dễ phát đôi). VFX-01 dùng event fake và seam. Cần `EventKey` đủ để khử trùng: đề xuất (Tick, SourceNetId, TargetNetId, seq). |
| Q3 | Nguồn Explosion/Status của người khác: chưa có packet (WPN-03/Game-10 Not Yet). | VFX-01 định nghĩa struct client; WPN-03/Game-10 chốt packet ở G2 của họ; có `EventKey` và `DurationSeconds` hoặc `StartTick`+`EndTick`. |
| Q4 | Có dự đoán hit/nổ/trạng thái không? | **Không** (chỉ authoritative). Chỉ hiệu ứng ngắn của người bắn local (muzzle/vung) được dự đoán và đối soát theo `RequestSeq`. |
| Q5 | Tinh thể và Mảnh thiên thạch dùng chung sheet nổ; Weapons yêu cầu phân biệt vai trò. | Bản tạm: tint xanh cho Tinh thể; art phân biệt chờ ART-04/ART-05. |
| Q6 | Số sát thương nổi: font, màu, có hiện trên chính mình không, kích cỡ? | Pixelify Sans cho số (đã dùng ở HUD), trắng cho gây ra, đỏ cho nhận; hiện trên mọi mục tiêu **được seam cho phép**, kể cả mình (HUD chỉ báo hướng, không báo số). Không dùng `KaPow/Pow/Zap` của Epic Toon. |
| Q7 | Cảnh báo ngòi nổ: ở thế giới (vòng) và HUD (banner/mũi tên). Cách chia? | Vòng báo trong thế giới thuộc VFX-01; banner/mũi tên/đếm giây thuộc UI-01 (đã có). Hai bên dùng chung `EventKey`/thời gian nổ để không lệch. |
| Q8 | Rung màn hình: khi nào và bao nhiêu? | Chỉ khi (a) người chơi local nhận sát thương, (b) nổ **được seam cho phép** trong 6 cell; biên độ tối đa 0,15 cell, ≤0,3 s; tắt hẳn khi `ScreenShakeEnabled = false`. Lưu ý: rung dịch ngắm, nên cap nhỏ. |
| Q9 | Adrenaline (+30% tốc độ 8 s) có cần quầng hình không? | **Không** trong P0; ứng viên `BuffDamageVfx/TinySparkles` nếu thêm P2. |
| Q10 | Có tô tím toàn màn (vignette) khi người chơi đứng ngoài bo? | Không tô toàn màn trong VFX-01. UI-01 chưa có thông tin này; nếu cần thì thuộc UI-01/ZON-01. **Cần người xác nhận** vì chưa kiểm hết HUD. |
| Q11 | Đối chiếu số trong `weapons.json`/`equipments.json` so với GDD chưa làm trong lượt này. | Dev/CPO đối chiếu thời lượng và vùng nổ khi nối; mâu thuẫn thì ghi cả hai và hỏi người. |
| Q12 | Deadline trong sheet (46326 = 31/10/2026) đã qua; ticket P2. | Hỏi Vũ/CPO ngày mới. |
| Q13 | Ref "WIREFRAME!13–15": sheet chỉ có dòng 1–13 (WF-11 gây/nhận sát thương, WF-12 nguy hiểm sắp xảy ra, WF-13 loot/hành động/trạng thái). | Đọc là WF-11/12/13; cập nhật Ref trong GDD. |
| Q14 | Pool không có "despawn all". | Adapter VFX tự theo dõi instance để thu hồi; không sửa `IPoolService` trong ticket này. Nếu muốn chuẩn hoá thì tách follow-up CLI-03. |
| Q15 | Seam visibility mặc định "cho hiện" và hợp đồng hỏi `(vị trí, NetId?)`. | Giữ tối thiểu, FOW-01 cắm sau; hiện "mờ" (aura Gậy thần) là quyết định của FOW-01, không của VFX. |
