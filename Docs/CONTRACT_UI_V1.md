# Contract UI v1 - skin A cho mọi màn (T-022..T-025, khóa 07/11)

Integrator: phiên `unity`. Engineer: 3 làn `module-engineer` (A, B, C). Phối hợp, khóa Unity, nhật ký: `docs/COORDINATION.md`.

## 1. Nguồn hình và số (đọc, không chép lại)

| Cần | File |
|---|---|
| Mock 1920x1080 (sự thật hình) | `projects/lastaway/art/inbox/T-021/a/*.png` (đợt A), `T-021/b/*.png` (đợt B); bảng màn `T-021/man-hinh.md` |
| Kit, cỡ, border, luật nút, chữ | `projects/lastaway/art/inbox/T-020/ui-kit.md` (mục 2, 4, 9-13), `T-020/meta.json` |
| Số Huy chốt | `projects/lastaway/so-hien-hanh.md`; luật màu đỏ giữ cho cung chỉ hướng và vignette (DECISIONS 07/11) |
| Gói cắt sẵn của art-ux (CPO duyệt 07/11) | `projects/lastaway/art/inbox/T-028-unity-pack/`: đọc `README.md`, `bang-tong.md`, rồi bảng ánh xạ của màn mình (`1-sanh-va-phong/`, `2-hud-balo-ban-do/`, `3-esc-spectator/`, `4-loading-mat-ket-noi-ket-qua/`). Bỏ 18 file không đuôi trong `files/items/` |
| Cổng cỡ chữ | `python -I projects/lastaway/art/inbox/T-027/src/do_co_chu.py --gate --unity` (đọc prefab YAML) |

## 2. API chung (Integrator giữ, engineer chỉ gọi)

`Assets/_JungleMaze/Tools/Editor/UiKit/UiKit.cs` (asmdef `JungleMaze.UiKit.Editor`, đã nối vào Lobby, MatchOverlay, HudFeedback, HudLayout editor asmdef). Sprite đã nhập: `Art/UI/Kit/kit_*.png` (114, @2x 300 PPU, border từ meta.json), `Art/UI/Kit/Fx/fx_*.png` (15, Simple). Font: `Art/Fonts/TMP/ChakraPetch-Bold SDF.asset` (đủ dấu, static).

| Gọi | Làm |
|---|---|
| `UiKit.Px(px)` | px 1080p -> đơn vị canvas (chia 1,5) |
| `UiKit.Place(t, x, y, w, h)` | đặt theo góc trên trái của cha, px mock, y xuống |
| `UiKit.PlaceCentred(t, x, y, w, h)` | tọa độ tuyệt đối mock 1920x1080, neo tâm cha (hộp thoại giữa màn) |
| `UiKit.Stretch(t, insetPx)`, `UiKit.Node(parent, name)` | |
| `UiKit.Image(t, "kit_x" hoặc "fx_x", tint)` | Sliced nếu sprite có border, Simple + preserveAspect nếu không |
| `UiKit.FitHeight(img, hPx)` | co 9-slice theo cao khác cỡ gốc |
| `UiKit.Panel(t, PanelKind.S/M/L/Popup)` | S, M, L cùng nguồn S border 47; Popup có thanh tiêu đề 88 |
| `UiKit.Dim(parent)` | nền tối 0,6 sau modal, chặn click |
| `UiKit.Floor(parent, washAlpha)` | nền gạch lát kín màn (`bg_floor_tile`, T-028) + lớp phủ tối 0,66; đặt đầu tiên dưới gốc màn |
| Sprite mới T-028 | `icon_ammo` (đạn, B4), `icon_ground` (đồ ở gần, B4), `kit_badge_key` (nhãn số phím quick bar, border 14) trong `Art/UI/Kit/` |
| `UiKit.Button(parent, name, ButtonKind.Primary/Secondary/Danger/Exit, ButtonSize.S/M/L, wPx, "kit_ico_x", "Chữ")` | nút K2 4 trạng thái, icon + 10 px + chữ căn theo mặt nút (`KitButtonFace`), chữ tối, tắt thì chữ xám (`KitButtonLabel`). Exit = mặt phụ + X đỏ |
| `UiKit.Text(parent, name, Tier, text, color, align)` | chữ đúng bậc và font tầng; `UiKit.ApplyTier(text, tier)` cho chữ có sẵn |
| `UiKit.Snap(px, floorPx)`, `UiKit.SnapAll(root, floorPx, keep)` | đưa cỡ về thang 96/40/30/24/20 (hòa thì xuống), font theo tầng |
| `UiKit.Colors.Text/Muted/Gold/Ink/ExitRed/DangerText/Outline/DisabledText/Dim` | token màu |

Bậc: `Tier.H1` 96 và `H2` 40 = Chakra Petch Bold; `B1` 30, `B2` 24, `C` 20 = Be Vietnam Pro Bold. Chữ nút theo cỡ nút (S 24, M 30, L 40) luôn Be Vietnam Pro. Sàn 20; màn gameplay (HUD, túi đồ, bản đồ lớn, spectator, toast) sàn 24, riêng số phím và số trên ô được 20.

Cần thêm vào UiKit: ghi yêu cầu vào báo cáo, Integrator thêm. Helper riêng của màn đặt trong builder của làn.

## 3. Luật làm

1. Chỉ đổi hình. Không đổi luồng, trạng thái, mạng, phòng (`Scripts/Application/**`, `Scripts/Infrastructure/**`, `Backend/**` không sửa). View có thể thêm field `[SerializeField]` cho phần tử hình mới; presenter chỉ được sửa phần gán hình (màu, sprite, bật tắt phần tử trang trí), ghi từng chỗ trong báo cáo.
2. Mọi tọa độ, cỡ đo từ mock (px 1920x1080). Ghi lệch có chủ ý trong báo cáo.
3. Không vẽ hình bằng code (không Texture2D, không vector). Thiếu sprite: dùng sprite kit gần nhất làm giữ chỗ, ghi vào "Cần art-ux".
4. Test: được sửa assertion layout và hình đã có khi do đổi skin; ghi tên từng test. Không sửa assertion luồng, logic. Thêm test cho thành phần mới (ít nhất: tồn tại, đúng sprite, chữ đúng bậc).
5. Không chạy Unity. Integrator chạy builder, compile, test, chụp ảnh.
6. Không sửa: `Tools/Editor/UiKit/**`, `Scripts/Presentation/UI/Kit/**`, `*.asmdef`, `HudPalette.cs`, scene, `Scripts/Bootstrap/**`, file của làn khác, vùng Hào (Hero, map, balance), Vinh (`Backend/`), Vũ (netcode client), Quang Huy (ITM, ENV, MAP), `Assets/Imported/JSurviv/`.

## 4. Làn và bảng quyền file

| Làn | Việc | Được sửa / tạo |
|---|---|---|
| A | T-024 sảnh, đăng nhập, vào/tạo phòng, phòng của tôi, phòng chờ, thắng/thua (ResultsView); rồi T-025 loading, mất kết nối/reconnect | `Tools/Editor/Lobby/LobbyUiPrefabBuild.cs`, `Scripts/Presentation/UI/Lobby/*.cs` (theo luật 1), `Tools/Editor/UiArt/UiArtAssetBuild.cs`, `Tests/EditMode/Lobby/**`, `Tests/PlayMode/Lobby/**`, `Tests/PlayMode/UiArtIntegrationTests.cs`, `Tests/EditMode/UiArt/**` |
| B | T-022 phản hồi HUD (B9), túi đồ (B4), bản đồ lớn (B5), thang chữ trong Hud.prefab | `Tools/Editor/HudFeedback/HudFeedbackPrefabBuild.cs`, `Tools/Editor/Hud/HudLayoutBBuild.cs`, `Tools/Editor/Hud/HudSkinABuild.cs`, file builder mới trong `Tools/Editor/Hud/`, `Scripts/Presentation/UI/Hud/**` (trừ `HudPalette.cs`), `Tests/EditMode/Hud/**`, `Tests/PlayMode/Hud*.cs`, `Tests/PlayMode/Art04IconSweepTests.cs`, test mới `Tests/*/Hud/**` |
| C | T-023 ESC cài đặt và xác nhận thoát (B1), bị loại (B2), spectator (B3) | `Tools/Editor/MatchOverlay/MatchOverlayPrefabBuild.cs`, `Scripts/Presentation/UI/MatchOverlay/*.cs` (theo luật 1), `Tests/EditMode/MatchUi/**`, `Tests/PlayMode/MatchUi/**` |

Prefab, asset sinh ra (`LobbyUi.prefab`, `MatchOverlay.prefab`, `Hud*.prefab`, `SpectatorRow.prefab`, `*Set.asset`) chỉ Integrator dựng lại bằng builder.

## 5. Bàn giao của engineer

Báo cáo cuối: bậc WRITTEN; file đã sửa/tạo; test đã sửa (tên, lý do) và thêm; lệnh menu builder Integrator phải chạy; bảng "màn mock -> phần tử -> sprite"; mục "Cần art-ux" (sprite thiếu, đang giữ chỗ bằng gì); rủi ro.
