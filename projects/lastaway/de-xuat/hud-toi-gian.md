# Đề xuất HUD tối giản - Lastaway

Trạng thái: **đề xuất, chờ Huy duyệt** (art-ux, 06/11/2026, T-009). Chưa đổi Figma, ticket, prefab.

Đầu vào: 4 điểm Huy chốt 06/11 (`so-hien-hanh.md`): (1) Vitals và Quick bar giảm 30%, (2) tham khảo nhiều game top-down BR, (3) Minimap hiện bản đồ thật, chưa thêm chức năng, (4) icon kèm số nhỏ, bỏ nhãn chữ, icon lấy từ thư viện. Gốc: comment Figma (`figma-feedback.md`, #1-#3, #5).

## 1. Benchmark HUD (subagent `research`, sonnet; thô ở `hud-benchmark-raw.md`)

**Giới hạn phải đọc trước:** Fandom wiki trả 402 nên chỉ có snippet; công cụ không xem được ảnh chụp màn hình. Chỉ **Suroi** (clone surviv.io mã nguồn mở) có số đo thật, lấy từ CSS. Các game còn lại chỉ có mô tả chữ hoặc tên element, cỡ ghi "không đo được". Chưa đạt "5 game đủ dữ liệu"; có **1 game có số đo + 4 game một phần**. Muốn chốt số theo benchmark thì cần chụp frame 1920x1080 từng game rồi đo (đề xuất Huy hoặc qa làm khi cần).

| Game | Bố cục | Máu / giáp / đạn | Minimap | Điều học được | Nguồn |
|---|---|---|---|---|---|
| Suroi | Trái trên: minimap + gas timer. Phải trên: kill counter. Giữa dưới: thanh máu (shield chồng cùng khung), adrenaline ngay trên, số đạn trên nữa. Phải dưới: 2 ô vũ khí. Trái dưới: ô hồi máu | Thanh ngang, shield chồng, adrenaline thanh mỏng, đạn là số lớn + số nhỏ; ô vũ khí là icon | Trái trên 220x220 (11,5% rộng màn), "màn lớn" 135x135 (7%). Có nút đóng | Cụm sinh tồn xếp dọc giữa dưới; có breakpoint co HUD khi màn lớn (thanh máu 330 xuống 230) | [game.scss](https://raw.githubusercontent.com/HasangerGames/suroi/master/client/src/scss/pages/client/game.scss) |
| surviv.io / survev | Có khối top-left, top-center scopes, bottom-center-right, weapon container, killfeed, leaderboard. Vị trí tuyệt đối không đo được | Thanh máu + số; boost là các thanh; đạn là số. Mod người chơi thêm số %, mod thu HUD 70-100% | Có nút thu nhỏ / mở rộng map; cỡ không đo được | Người chơi tự mod thêm số và đổi chỗ kill counter vì đè ô hồi máu / đạn: góc dưới dễ xung đột | [survev UI mod](https://greasyfork.org/en/scripts/509087-survev-io-ui-mod) |
| ZombsRoyale.io | Một nguồn tin cậy thấp: minimap trái trên, máu + shield giữa dưới | Hai thanh riêng health và shield, đạn chia 4 loại | Có (thấp tin cậy), cỡ không đo được | Máu và giáp tách riêng dễ đọc | [wiki guide](https://zombsroyaleio.fandom.com/wiki/How_to_play_guide_for_ZombsRoyale.io) (snippet) |
| Super Animal Royale | Trái trên: số người còn sống + killfeed; phần còn lại chưa xác minh | Máu thanh; giáp dạng tối đa 3 vạch theo tier; đạn băng + dự trữ | Chưa xác minh | Giáp dạng vạch rời đọc nhanh hơn số nhỏ. Có review chê HUD quá nhỏ: tránh chữ nhỏ ở 1080p | [Gaming Trend](https://gamingtrend.com/impressions/attack-of-the-adorable-woodland-creatures-super-animal-royale-impressions), [Giant Bomb review](https://giantbomb.com/super-animal-royale/3030-70232/user-reviews/2200-31070/) |
| Brawl Stars Showdown | Máu và đạn gắn trên đầu nhân vật (world-space), không HUD góc | Máu thanh, đạn thanh chia đoạn mỗi đoạn một phát, không nhãn chữ | Chưa xác minh | Giữ mắt ở nhân vật. Là game mobile, đổi sang PC cần thử | [PocketGamer](https://www.pocketgamer.com/brawl-stars/brawl-stars-cheats-and-tips-everything-you-need-to-know-about-showdown-mode), [wiki](https://brawlstars.fandom.com/wiki/Power_Cards) (snippet) |

Bullet Echo bỏ vì không nguồn nào mô tả HUD.

**So với Suroi (đã đo):** cụm sinh tồn của Suroi rộng 12-17% màn; Vitals đề xuất bên dưới rộng 16,2%: cùng khoảng. Minimap Suroi 7-11,5% rộng màn; HUD v2 đang 15,6% (300 px): to hơn khoảng 1,4 đến 2,2 lần, ủng hộ biến thể B ở mục 4.

**5 quy tắc rút ra (nguồn ở bảng trên):** 1) cụm sinh tồn gần tâm nhìn và gọn; 2) minimap 7-12% rộng màn, có thể thu gọn; 3) giáp dạng vạch hoặc icon, không số nhỏ không nhãn; 4) tránh chữ nhỏ ở 1080p; 5) tránh xung đột góc dưới (kill counter, ô hồi máu, Vitals).

## 2. Bộ icon (thư viện game-icons.net)

- Nguồn: repo `game-icons/icons` (https://github.com/game-icons/icons), tải ngày 06/11/2026 bằng `curl` từ `raw.githubusercontent.com`, 8 file SVG, tổng khoảng 11 KB.
- Giấy phép: **CC BY 3.0** (có thể dùng thương mại, có chỉnh sửa; **bắt buộc ghi công** tác giả và nguồn). Bản `LICENSE` nằm cạnh icon.
- Lưu ở `projects/lastaway/art/inbox/hud-icons/` (inbox, chưa qua cổng duyệt): `lorc/`, `delapouite/`, `LICENSE-game-icons.net.txt`.
- Định dạng: SVG trắng trên nền đen, tô màu bằng `fill` (mock dùng màu HudPalette: đỏ máu `#E5484D`, xanh giáp `#5BB8E8`, vàng Adrenaline `#F5C542`, tím bo `#A274FF`). Đưa vào Unity cần xuất PNG RGBA (việc của unity/art-ux sau khi duyệt); chưa làm.

| Chỉ số | Icon | File | Tác giả | Giấy phép | Màu |
|---|---|---|---|---|---|
| Máu | heart-organ | `lorc/heart-organ.svg` | Lorc | CC BY 3.0 | đỏ |
| Giáp | breastplate | `lorc/breastplate.svg` | Lorc | CC BY 3.0 | xanh giáp |
| Mũ | barbute | `lorc/barbute.svg` | Lorc | CC BY 3.0 | xanh giáp |
| Adrenaline | lightning-helix | `lorc/lightning-helix.svg` | Lorc | CC BY 3.0 | vàng |
| Còn sống | person | `delapouite/person.svg` | Delapouite | CC BY 3.0 | trắng |
| Đồng hồ bo | hourglass | `lorc/hourglass.svg` | Lorc | CC BY 3.0 | tím |
| Hạ gục (dự phòng: killfeed, kết quả) | skull-crossed-bones | `lorc/skull-crossed-bones.svg` | Lorc | CC BY 3.0 | trắng |
| Mũ (phương án 2) | crested-helmet | `lorc/crested-helmet.svg` | Lorc | CC BY 3.0 | xanh giáp |

**Nghĩa vụ ghi công (phải làm trước khi build ra ngoài):** thêm vào màn Credits hoặc README bản phát hành: "Icons by Lorc and Delapouite from game-icons.net, licensed under CC BY 3.0 (https://creativecommons.org/licenses/by/3.0/)". Chưa có màn Credits trong build; đề xuất unity ghi vào task riêng khi duyệt.

**Không nằm trong bộ này:** icon 34 vật phẩm và 7 icon trạng thái (ART-04, `UI_Asset_Packs`); đây là icon chỉ số HUD. Kenney (CC0) chưa dùng, giữ làm phương án nếu cần bộ không phải ghi công.

## 3. Mock HUD 1920x1080

Ảnh: `hud-mock/hud-v2.png` (HUD v2 hiện tại), `hud-mock/hud-A.png` (đúng 4 điểm Huy chốt), `hud-mock/hud-B.png` (A + thu thêm minimap và Match status 20%). Nguồn dựng: `hud-mock/hud-*.html` (HTML, không phải Unity). Quy đổi: `Hud.prefab` có `m_ReferenceResolution` 1280x720, match width, nên 1080p = 1,5 lần số trong `Docs/UI_Wireframes.vi.md`.

Thay đổi của A so với v2:

| Khối | v2 (1080p) | Đề xuất A (1080p) | Ghi |
|---|---|---|---|
| Vitals | 444x246 | 311x172 | -30% mỗi chiều. 3 hàng: HP (icon + thanh đỏ + số), Giáp và Mũ (icon + số, không thanh), Adrenaline (icon + thanh vàng mỏng + số). Bỏ 4 nhãn chữ; từ 4 thanh còn 2 thanh |
| Quick bar | 846x195 | 592x136 | -30% mỗi chiều. Ô 64 px (v2: 96 px, -33%), phím số nhỏ, bỏ nhãn "VŨ KHÍ" / "DƯỢC PHẨM", hai nhóm chỉ cách nhau khoảng hở |
| Minimap | 300x300 | 300x300 (A) / 240x240 (B) | Mock vẽ bản đồ mẫu: địa hình, vòng bo tím, chấm bản thân; nội dung thật do unity nối dữ liệu |
| Match status | 300x111 | 300x111 (A) / 240x89 (B) | Giả định của tôi: thay chữ "Bo thu sau" / "Còn sống" bằng icon người + hourglass + số |
| Killfeed | 256x135 | 256x135 | Giữ |

Đo độ che (script `gen_hud.py`, đo theo khung ngoài của từng khối, `hud-mock/do-che.json`):

| Phương án | Tổng diện tích HUD | % màn | % góc dưới trái bị che (nửa trái x nửa dưới, nơi kẻ địch bị Vitals che) |
|---|---:|---:|---:|
| v2 hiện tại | 432.122 px² | 20,84% | 36,98% |
| A (đúng Huy chốt) | 292.283 px² | 14,10% | 18,12% |
| B (A + minimap, Match status -20%) | 247.895 px² | 11,95% | 18,12% |

Che giảm 6,7 điểm % màn (A) hoặc 8,9 điểm % (B). Vitals riêng giảm 51% diện tích (`-30%` mỗi chiều); tôi hiểu "giảm 30%" là mỗi chiều, không phải diện tích (xem câu hỏi). Kẻ địch đặt giả định ở (300, 880) vẫn nằm sát mép Vitals trong cả A: góc dưới trái là chỗ HUD hay che nhất, kể cả sau khi thu.

Phương pháp đo là diện tích khung ngoài, không phải pixel thật; mock không có world thật, nên % che tầm nhìn thật cần đo lại trong GameView sau khi unity dựng.

## 4. Việc cần làm nếu duyệt (chưa làm, chưa giao)

| Việc | Vai | Ghi |
|---|---|---|
| Cập nhật HUD v2 trong Figma (`41:2`, `41:180`) theo A hoặc B | art-ux (cần quyền ghi Figma) | Hiện chỉ đọc |
| Xuất 8 icon PNG RGBA, qua cổng asset (qa chấm chéo, Huy duyệt `art/final/`) | art-ux | Chưa làm |
| Đổi layout Vitals, Quick bar, Match status, minimap có dữ liệu bản đồ thật | unity | UI-01 đã Done: mở ticket mới, không mở lại |
| Credits CC BY 3.0 | unity | Trước build phát hành |
| Chụp lại 36 frame Figma | unity | Xem `chup-lai-figma-116-3.md` |

## 5. Giả định và chưa kiểm

- "Giảm 30%" hiểu là mỗi chiều (chiều rộng và cao), không phải 30% diện tích.
- Match status dùng icon thay chữ: ngoài nguyên văn Huy, cần Huy xác nhận.
- Quick bar bỏ nhãn nhóm; hai nhóm vẫn phân biệt qua khoảng hở. Cần thử người mới có hiểu 4 ô trái là vũ khí và 4 ô phải là dược phẩm không (chưa thử).
- Icon tối giản đọc được ở cỡ hiển thị thật: mới xem mock ở 1920x1080, chưa kiểm ở 380 px dọc (không áp dụng, game PC) và chưa kiểm tương phản bằng script trên nền thật. **NOT VERIFIED.**
- Mock không phải Unity; chưa có cảnh thật trong HUD.

## 6. Câu hỏi cho Huy

Chọn **A** (đúng 4 điểm đã chốt) hay **B** (A + thu thêm minimap và Match status 20% theo benchmark Suroi)? Nghiêng về **B**: Suroi có minimap nhỏ hơn HUD v2 khoảng 1,4 đến 2,2 lần, comment Figma #3 chê UI to, và B giảm che thêm 2,2 điểm % màn.
