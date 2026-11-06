# QA T-015 - Bộ asset skin A đá khắc, lượt 2 + icon kim loại ấm (lastaway)

Kết luận: **ĐẠT** (0 lỗi Nặng, 6 lỗi Nhẹ có đích xử lý; NOT VERIFIED trong Unity, xem "Giới hạn")
QA: qa · Ngày: 2026-11-06 · Nền: commit `8673b5c7` + phần đổi icon chưa commit (working tree)
Phạm vi: chấm file, không lái Editor, không sửa asset. Chưa gắn prefab (T-016).

## Theo tiêu chí brief
| # | Tiêu chí | Kết quả | Bằng chứng (qa tự đo) |
|---|---|---|---|
| a1 | Cỡ sprite @1x và @2x đúng spec 1080p | **Đạt** | 12 sprite x 2 bản: đúng cỡ asset-list (Vitals 311x173, Quick bar 592x126, Match 240x89, Minimap 240x240, ô 60x60, track 164x21 và 164x12...). Ra đúng rect `do-rect-1920x1080.txt`; @2x = đúng gấp đôi |
| a2 | RGBA, nền trong suốt | **Đạt** | 4 góc alpha 0 ở mọi khung, ô, track; khung minimap có lỗ giữa. Ngoại lệ đúng thiết kế: `hud_minimap_overlay` (alpha 127-178 = 50-70%) và `hud_killfeed_plate` (tối đa 183 = 72%). `hud_bar_fill_white` góc alpha 1 ở @1x: không đáng kể |
| a3 | Border 9-slice ghi đúng, kéo giãn không vỡ góc | **Đạt** | Tự dựng 9-slice theo border trong `meta.json` rồi kéo: Vitals 311x173 -> 420x230 và 311x120; Quick bar 592 -> 800 và 420 cao 150; Match 240 -> 330 và cao 120; Minimap 240 -> 300; track HP 164 -> 300; ô 60 -> 90 và 42. Hoa văn góc, rêu, viền không méo, không đứt; tổng border luôn nhỏ hơn cạnh (không vùng giữa âm). `meta.json` khớp bảng asset-list (kể cả @2x) |
| b | Tương phản, chạy lại script | **Đạt** | Chạy lại `contrast_t015.py`: ra đúng số bảng (chữ HP 10,28 / số giáp 5,77 / số phím 9,46 / số bo 3,76 ở p99). Đo độc lập icon @2x bằng script riêng (lõi icon, bỏ viền tối, nền p99 = 0,040): cr trung bình hp 5,52, armor 5,51, helmet 5,96, adrenaline 7,69, alive 7,80, zone-timer 6,33; đều >= 4,5. Vòng bo trên minimap đo lại từ `dung-thu-hud-A.png`: vàng 7,61:1, đỏ 2,47:1 (art-ux ghi 6,87 và 2,48: khớp, đỏ < 3:1 như art-ux đã nêu) |
| c | HP / giáp / mũ phân biệt ở cỡ thật | **Đạt** | Độ sáng lõi đo lại 0,447 / 0,446 / 0,486 (CPO: 0,45 / 0,46 / 0,50). Gần nhau, nên phân biệt nhờ tông và hình: hue 19 (cam hồng) / 155 sat 0,06 (xám) / 40 (vàng). Ảnh icon cỡ @2x trên nền đá và bản xám: ba hình khác hẳn nhau (nội tạng, giáp ngực phân đoạn, mũ có khe mắt), vẫn tách được khi bỏ màu. Trên `dung-thu-hud-A.png` ở 1080p: đọc được cả ba, nhưng xem N-ICON-1 |
| d | So concept A | **Đạt, 2 lệch Nhẹ** | Xem mục "So concept". Khung, góc khắc, rêu, lòng vân đá, ô phẳng, số phím kem: đúng chất concept. Lệch: icon HP và đồng hồ bo |
| e | Credits icon CC BY | **Đạt, 1 sửa lời Nhẹ** | Danh sách tác giả và icon trong `ux/credits.md` vẫn đúng (Lorc: heart-organ, breastplate, barbute, lightning-helix, hourglass, skull-crossed-bones, crested-helmet; Delapouite: person). Hình giữ nguyên (bản mới chỉ thêm viền tối ngoài hình, không mất pixel nào của hình gốc, đo từ alpha hai bản). CC BY 3.0 cần nêu đã sửa: lời "recolored and resized" còn đúng nhưng chưa nói đến gradient kim loại và viền: xem N-CRED |

## So concept (điểm CPO hỏi)
| Điểm | Concept A | Bản dựng | Có đáng trả không |
|---|---|---|---|
| Icon HP | Tim biểu tượng (đỏ, hai thùy) | `heart-organ` của Lorc: tim giải phẫu, màu cam hồng kim loại. Ở 36 px trên `dung-thu-hud-A.png` đọc giống bàn tay hoặc nội tạng hơn là quả tim; cạnh thanh đỏ `E5484D` và số 100 thì vẫn hiểu là máu | **Không trả (Nhẹ)**. Hình đã nằm trong spec T-012 và Huy duyệt "đổi icon" ngày 06/11; vấn đề là đọc nhận diện, không phải tương phản. Đề xuất đổi hình sang tim biểu tượng (game-icons có `heart-inside`, `hearts`...), tác giả khác thì thêm vào credits. Huy quyết (hình nhìn thấy) |
| Đồng hồ bo | Cát và số kem/trắng | Icon cát bạc tím, số `A274FF` | **Không trả (Nhẹ)**. Số tím là token Purple trong spec; icon tím nhạt đã khớp số. Lệch concept, và lệch cả vòng bo thật (đỏ/vàng, DECISIONS 06/11). cr số 3,76 đủ 3:1. Cần CPO hoặc Huy chốt: giữ tím hay đổi sang kem như concept (đổi token là việc spec) |
| Giáp | Áo giáp da nâu | Giáp ngực xám bạc | Chấp nhận. Khác chất nhưng khác tông với mũ vàng và HP cam: tốt cho phân biệt |
| Lòng panel | Vân đá, hơi sáng hơn | Vân đá tối hơn (hạ 0,70 để đạt tương phản, asset-list đã khai) | Chấp nhận. Tối hơn concept, đổi lại đạt tương phản |

## Lỗi
Nặng: **không**.

| # | Lỗi | Mức | Đích | File |
|---|---|---|---|---|
| N-ICON-1 | Icon HP là tim giải phẫu, ở cỡ thật đọc giống bàn tay/nội tạng, lệch concept (tim biểu tượng) | Nhẹ | art-ux đề xuất đổi hình, Huy chọn; trong lúc chờ, không chặn T-016 | `hud-icons/png/icon_hp*.png`, nguồn `hud-icons/lorc/heart-organ.svg` |
| N-ICON-2 | Đồng hồ bo vẫn tím, lệch concept và lệch vòng bo đỏ/vàng trên world | Nhẹ | CPO hoặc Huy chốt token; spec sửa nếu đổi | `hud-B-spec.md` mục 7 (Purple), `icon_zone-timer*.png` |
| N-MAP | Overlay minimap tối 50-70% làm vòng bo nổi nhưng chi tiết địa hình trên mock gần như mất (nội dung dồn vào vòng và marker); vòng đỏ vẫn 2,47:1 < 3:1. Đo trên mock, chưa phải RenderTexture thật | Nhẹ | unity xem ở T-016 trong Editor; thêm viền đen 1 px quanh vòng đỏ như art-ux đã đề nghị, hoặc giảm độ tối overlay | `hud_minimap_overlay.png`, `asset-list.md` mục 7 N3 |
| N-DOC | Tài liệu lệch nhau sau khi đổi icon: (1) `asset-list.md` mục 9 vẫn ghi "Icon máu thiếu 0,02 ở p99" (cũ, nay 5,51); (2) `contrast.md` và output script còn dòng icon giáp/mũ `5BB8E8`, Adrenaline `F5C542` (màu vector cũ) nằm trên bảng icon kim loại mới; (3) `hud-B-spec.md` mục 8 vẫn ghi màu nướng Red/Blue/Yellow/Purple | Nhẹ | art-ux dọn trước khi unity đọc spec ở T-016 | `T-015/asset-list.md`, `T-015/contrast.md`, `T-015/src/tools/contrast_t015.py`, `ux/hud-B-spec.md` |
| N-CRED | `ux/credits.md` nói "recolored and resized"; bản mới thêm gradient kim loại và viền tối. Nên viết "recolored with metallic shading and outline, and resized" cho đủ nghĩa vụ nêu sửa đổi của CC BY 3.0. Chưa có màn Credits trong build, vẫn là việc phải xong trước khi phát hành | Nhẹ | art-ux | `ux/credits.md` |
| N-FILL | `hud_bar_fill_white` border trên/dưới 5+5 = 10 px, lớn hơn chiều cao fill của thanh Adrenaline (khoảng 8 px ở 1080p). Unity sẽ co border theo tỉ lệ; cần xem trong Editor khi gắn | Nhẹ | unity, T-016 | `hud_bar_fill_white.png`, `hud_bar_track_thin.png` |

## Giới hạn (NOT VERIFIED)
- Chưa xem trong Unity: import 9-slice thật bằng Border theo thứ tự Left, Bottom, Right, Top, PPU 150 (hoặc 300 với @2x), Bilinear. qa không lái Editor; kiểm ở T-016.
- `dung-thu-hud-A.png` là mock ghép bằng script, không phải ảnh chụp Unity: các số về nhận diện icon ở cỡ thật và overlay minimap là ước lượng từ mock.
- Chưa có ca động với skin A: máu thấp, bo thu, nhặt đồ, icon mờ khi số = 0 (icon bị mờ 0,35 còn khoảng 2,5:1 ở trạng thái rỗng, theo thiết kế).
- T-015 đổi kích thước ô quick bar từ 63 px (42 u) xuống 60 px (40 u), khe 3, nhóm 12. Huy đã duyệt trong DECISIONS 06/11 (lượt 2); unity phải sửa layout T-013 theo (asset-list mục 4).
- Bậc: **asset đạt trên file**. Chưa INTEGRATED, chưa VERIFIED trong Editor, chưa PLAYTESTED.
- Phương pháp: script đo và dựng thử nằm trong thư mục tạm của phiên qa, không đưa vào repo. Số đo lấy lại được bằng `src/tools/contrast_t015.py` (chạy bằng `python`, không `-I`).
