# T-027 Nghiên cứu cơ sở thang cỡ chữ UI/HUD và cỡ sàn (1920x1080, PC ngồi bàn)

Ngày truy cập tất cả nguồn: 2026-11-07. Phương pháp: WebSearch + WebFetch, chỉ đọc trang, không tải file. Mẫu: không áp dụng (không phải đo ván), đây là tài liệu hướng dẫn.

Lưu ý đo: XAG tính "font size" là body height = từ đỉnh ascender cao nhất tới đáy descender thấp nhất (px đo trên ảnh chụp), KHÔNG phải em-size nhập trong engine. Các nguồn khác (Amazon, IGDA) không nói rõ cách đo. Phải quy đổi bằng đo thật trên Be Vietnam Pro (chưa xác minh tỉ lệ).

## 1. Sự thật có nguồn

### 1.1 Cỡ chữ tối thiểu

| # | Nguồn | URL | Con số | Nhắm tới |
|---|---|---|---|---|
| 1 | XAG 101 Text display (Microsoft, cập nhật 2026-06-17) | https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/101 | Console: >= 26 px @1080p, 52 px @4K. PC/VR: >= 18 px @1080p, 36 px @4K. Mobile/streaming: 18 px @100 DPI, tuyến tính theo DPI. Là mặc định tối thiểu khi khởi động. Cho phép scale tới 200% (PC 18 -> 36 px). Đo theo body height (ascender + x-height + descender). Pixel viền có tương phản < 2:1 với nền không tính vào chiều cao. | Console (TV) và PC/VR tách riêng |
| 2 | XAG 101, phần khoảng cách dòng và chữ | cùng URL | Line spacing >= 1.5 (đo ascender dòng 1 tới ascender dòng 2) cho khối > 2 dòng. Letter spacing >= 0.12 x cỡ chữ. Word spacing >= 0.16 x cỡ chữ. Dòng <= 80 ký tự. Paragraph spacing >= 2 x line spacing. | Mọi nền tảng, cho khối chữ dài |
| 3 | IGDA GASIG | https://igda-gasig.org/?p=1387 | Tối thiểu 32 px @1080p cho chữ mặc định; chữ hiện có thời hạn (subtitle) 46 px @1080p, đặc biệt khi cỡ không chỉnh được. | Không nêu rõ; ngữ cảnh chung (nghiêng TV/console) |
| 4 | Game Accessibility Guidelines, mục subtitle | https://gameaccessibilityguidelines.com/if-any-subtitles-captions-are-used-present-them-in-a-clear-easy-to-read-way/ | Subtitle >= 46 px @1080p (khoảng 4,3% chiều cao màn, tự tính 46/1080). Nền đặc hoặc bán trong suốt phía sau; thêm outline hoặc drop shadow. Tối đa 40 ký tự/dòng, 2 dòng. | Không nêu rõ |
| 5 | Amazon 10-foot UI (qua bài trích) | https://accessrewind.substack.com/p/solving-small-text-and-contrast-issues và https://gameaccessibilityguidelines.com/?p=3285 | Tối thiểu 28 px @1080p, "dùng 28 px làm sàn chứ không phải mục tiêu". Bài substack đề xuất cho TV: body 28-36, menu item 32-44, title 48-80, subtitle 55-75 px; line-height 150-170%; contrast 7:1 chữ, 10:1 UI nhỏ. Đây là gợi ý của tác giả bài, không phải của Amazon. | TV 10 foot, khoảng cách ngồi 8-12 ft |
| 6 | Game Accessibility Guidelines, "Allow the font size to be adjusted" | https://gameaccessibilityguidelines.com/allow-the-font-size-to-be-adjusted/ | Không có con số px. Nguyên tắc: không có một cỡ nào ai cũng đọc được, nên cho người chơi chỉnh. | Chung |
| 7 | Steam Deck (blog Blendo Games, 2026-02-19) | https://blendogames.com/news/post/2026-02-19-steamdeckverification/ | Màn 1280x800 cần chữ cao "ít nhất khoảng 12 pixel"; tác giả nói đây là điểm hay làm game trượt verify. Đây là ý kiến nhà phát triển, không phải số Valve. | Handheld 1280x800 |
| 8 | GamingOnLinux (2026-01) trích tiêu chí Valve | https://www.gamingonlinux.com/2026/01/steam-machine-verification-will-have-fewer-constraints-than-steam-deck-but-text-sizing-worries-me/ | Tiêu chí Display của Valve chỉ nói chữ phải "legible", hỗ trợ 1280x800 hoặc 1280x720. Valve không công bố số px. | Handheld |
| 9 | Apple HIG Typography | https://developer.apple.com/design/human-interface-guidelines/typography (đọc qua https://developer.apple.com/tutorials/data/design/human-interface-guidelines/typography.json) | Mặc định / tối thiểu: iOS 17 / 11 pt; macOS 13 / 10 pt. Font mảnh nên lớn hơn mức này. | Phone / Mac (pt, không phải px 1080p) |

### 1.2 Bảng cỡ Apple HIG (pt, đọc trực tiếp từ JSON của trang)

macOS built-in styles (size / line height, pt):

| Style | Size | Line height |
|---|---|---|
| Large Title | 26 | 32 |
| Title 1 | 22 | 26 |
| Title 2 | 17 | 22 |
| Title 3 | 15 | 20 |
| Headline (bold) | 13 | 16 |
| Body | 13 | 16 |
| Callout | 12 | 15 |
| Subheadline | 11 | 14 |
| Footnote / Caption 1 / Caption 2 | 10 | 13 |

iOS Large (mặc định), size / leading pt: Large Title 34/41, Title 1 28/34, Title 2 22/28, Title 3 20/25, Headline 17/22, Body 17/22, Callout 16/21, Subhead 15/20, Footnote 13/18, Caption 1 12/16, Caption 2 11/13.

Tỉ lệ line-height / size: macOS body 16/13 = 1,23; iOS body 22/17 = 1,29. Apple dùng leading chặt hơn nhiều so với XAG 1,5.

### 1.3 Material Design 3 type scale

Giá trị size (sp) đọc từ mã nguồn thư viện bên thứ ba (docs.rs/bevy_material_ui, https://docs.rs/crate/bevy_material_ui/latest/source/src/typography.rs), khớp bảng M3 chuẩn. Trang m3.material.io render bằng JS nên WebFetch không đọc được bảng.

| Role | Large | Medium | Small |
|---|---|---|---|
| Display | 57 | 45 | 36 |
| Headline | 32 | 28 | 24 |
| Title | 22 | 16 | 14 |
| Label | 14 | 12 | 11 |
| Body | 16 | 14 | 12 |

Line-height đã xác nhận qua nguồn đọc được: body medium 16 sp/1,5 rem = 24 px?? KHÔNG dùng; xem lưu ý. Đã xác nhận: material-web token body-medium `size: 1rem`, `line-height: 1.5rem` (https://github.com/material-components/material-web/blob/main/docs/theming/typography.md). Đây là token web (1rem = 16 px), tương ứng body LARGE của M3 gốc (16/24), nên tỉ lệ 1,5. Các line-height khác: chưa xác minh bằng nguồn đọc được trong phiên này (xem mục 2).

Quy đổi: sp của M3 là đơn vị mobile (xấp xỉ dp), không quy trực tiếp sang px 1080p PC; chỉ dùng tỉ lệ giữa các bậc (display 57 : body 16 = 3,56; label small 11 : body 16 = 0,69).

### 1.4 Tương phản, viền, nền chữ

| Nguồn | URL | Nội dung |
|---|---|---|
| XAG 102 Contrast | https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/102 | Chữ thường 4,5:1; chữ lớn 3:1; chữ phần tử bị vô hiệu 3:1; chế độ high contrast 7:1. "Chữ lớn" định nghĩa theo px: console 52 px @1080p, PC/VR 36 px @1080p. Chữ trên nền không đồng nhất: đo tại vùng nền tương phản thấp nhất. Gợi ý: cho nền đặc sau chữ hoặc chỉnh độ mờ nền; thêm viền quanh chữ. Ví dụ game: Fenyx Rising chữ trắng viền đen; For Honor biểu tượng viền đen trong viền trắng để đọc được trên cả nền sáng lẫn tối. |
| Game Accessibility Guidelines, high contrast | https://gameaccessibilityguidelines.com/provide-high-contrast-between-text-ui-and-background | Đặt chữ trên nền trơn tương phản cao; nếu không được thì dùng outline hoặc shadow đậm. Tối thiểu 4,5:1. |
| Game Accessibility Guidelines, clear text | https://gameaccessibilityguidelines.com/use-simple-clear-text-formatting/ | Line spacing 1,5 cho khối dài; khoảng 70 ký tự/dòng; font dễ đọc trên nền "không rối" giúp đọc tốt hơn. |
| WCAG 2.2 SC 1.4.3 Understanding | https://w3.org/WAI/WCAG22/Understanding/contrast-minimum.html | 4,5:1 chữ thường; 3:1 chữ lớn (>= 18 pt hoặc >= 14 pt bold; 1 pt = 1,333 px nên 24 px / khoảng 18,7 px bold). Viền hẹp quanh chữ tính là một phần chữ; viền rộng lấp chi tiết bên trong coi như halo và tính là nền. Trang không bàn về shadow. Không làm tròn (4,499:1 trượt). |
| WCAG 2.2 SC 1.4.11 Non-text Contrast | https://www.w3.org/WAI/WCAG21/Understanding/non-text-contrast | 3:1 cho UI component và đối tượng đồ họa mang thông tin (thanh máu, icon). Qua kết quả tìm kiếm: bóng 3D hoặc viền tối giữa hai nền được gộp vào màu gần nhất về độ sáng; tức outline và shadow không tự động tính là nguồn tương phản riêng. Chưa mở trang đầy đủ, ghi nhận qua tóm tắt tìm kiếm. |

Hàm ý cho chữ HUD trên nền biến thiên (suy luận của tôi, tách khỏi nguồn): vì XAG đo ở vùng nền xấu nhất, giải pháp chắc nhất là backplate bán trong suốt đủ tối; outline/shadow là lớp bổ sung. Cần test tại nền sáng nhất của map.

### 1.5 Tiếng Việt có dấu và line-height

| Nguồn | URL | Nội dung |
|---|---|---|
| Google Fonts Guide, Vertical metrics | https://googlefonts.github.io/gf-guide/metrics.html | Rule 10: (typo/hhea) Ascender phải bằng Abreveacute (U+1EAE) để dấu chồng tiếng Việt không bị cắt; Agrave là mức sàn bắt buộc. Rule 8: LineGap = 0. Ví dụ UPM 1000: ascender 900, descender -300, tổng 1200 = 120% em; nếu Abreveacute cao hơn, ví dụ ascender 1015, tổng khoảng 1330 (130%). Tổng quá cao làm dòng lỏng. Be Vietnam Pro là font thiết kế cho tiếng Việt nên khả năng cao theo rule này, nhưng giá trị thật chưa xác minh. |
| Vietnamese Typography, design challenges | https://www.vietnamesetypography.com/design-challenges/ | Chữ hoa có dấu "gây khó cho leading vì hạn chế không gian"; dấu có thể nằm sát chữ hoa nhưng không được chạm. Trang không có con số. |
| XAG 101 | như trên | Line spacing >= 1,5 cho khối chữ; áp dụng được cho đoạn tiếng Việt dài. |

### 1.6 Game battle royale (Fortnite, Apex, PUBG, Warzone)

Không tìm thấy số công bố về cỡ chữ HUD/menu (px ở 1080p) cho bốn game này. Chỉ thấy tùy chọn người chơi: Fortnite có HUD Scale theo %, Apex có phàn nàn thiếu tùy chỉnh HUD trên PC (forums.ea.com, ví dụ https://forums.ea.com/discussions/apex-legends-feedback-en/reduce-hud-size/8528971). PUBG và Warzone: không có gì liên quan. Muốn có số phải tự đo ảnh chụp, việc này chưa làm.

## 2. Chưa xác minh

| Mục | Ghi chú |
|---|---|
| Line-height M3 từng bậc (display 64/52/44, headline 40/36/32, title 28/24/20, body 24/20/16, label 20/16/16) | Đây là giá trị tôi nhớ từ spec M3, KHÔNG đọc được từ nguồn trong phiên. Chỉ xác nhận được body large 16 sp / 24 (token material-web body-medium 1rem/1,5rem) và, qua kết quả tìm kiếm, Label Large 20, Headline Small 32. Cần đọc m3.material.io bằng trình duyệt trước khi dùng. |
| Tỉ lệ body height (ascender tới descender) / em-size của Be Vietnam Pro | Chưa đo. Cần đo bằng ảnh chụp trong Unity theo cách XAG 101 chỉ dẫn, hoặc đọc metric font. |
| Giá trị hhea/typo ascender, descender của Be Vietnam Pro | Không tìm thấy công bố; file font nhị phân không mở được bằng WebFetch. Cần đọc bằng fontTools trên file font đã có trong repo. |
| Số px chữ HUD của Fortnite/Apex/PUBG/Warzone | Không tìm thấy số công bố. |
| "Khoảng 12 px @1280x800" của Steam Deck | Chỉ là ý kiến một nhà phát triển; một bài tìm kiếm khác nói "khoảng 9 px" và "14/18 px" từ blog khác, không mở được, bỏ qua. |
| Khuyến nghị cỡ chữ theo góc nhìn cho PC ngồi 50-70 cm | Không tìm thấy nguồn công bố cho khoảng cách đó. Mọi số tìm được nhắm TV hoặc là mức sàn chung. Cần suy ra theo góc nhìn hoặc test thật ở 1080p trên màn 24 inch. |
| Apple HIG/XAG có quy đổi sang px 1080p PC | Apple dùng pt logic, không công bố px cho 1080p. Chỉ so tỉ lệ. |
| WCAG 1.4.11 và shadow/outline | Chỉ qua tóm tắt tìm kiếm, chưa đọc nguyên trang chính thức. |

## 3. Bảng tóm tắt khuyến nghị cỡ sàn (quy ra px @1080p)

| Nguồn | Cỡ sàn chữ thường @1080p | Chữ lớn / ngưỡng khác | Nhắm tới |
|---|---|---|---|
| XAG 101 PC/VR | 18 px (body height) | "Chữ lớn" 36 px, scale tới 200% (36 px) | PC ngồi bàn (nguồn gần đúng nhất) |
| XAG 101 Console | 26 px | "Chữ lớn" 52 px | TV 10 foot |
| IGDA GASIG | 32 px; 46 px cho subtitle | - | TV / console (không nêu rõ) |
| Amazon 10-foot (qua trích dẫn) | 28 px, "sàn không phải mục tiêu" | Gợi ý tác giả bài: body 28-36, menu 32-44 | TV 10 foot |
| GAG subtitle | 46 px | Cần nền + outline/shadow | Không nêu rõ (ngữ cảnh subtitle) |
| Steam Deck (ý kiến dev Blendo) | khoảng 12 px @1280x800 | - | Handheld, không quy đổi thẳng sang 1080p |
| Apple HIG macOS | 10 pt tối thiểu, 13 pt body | line-height body 16/13 = 1,23 | Mac desktop, đơn vị pt logic, không phải px 1080p |
| Apple HIG iOS | 11 pt tối thiểu, 17 pt body | - | Phone |
| Material 3 | Body 14-16 sp, label nhỏ nhất 11 sp | Display 57 sp | Mobile, sp không quy đổi sang px PC |
| WCAG 1.4.3 | Không có sàn cỡ chữ | Chữ lớn >= 24 px (18 pt) hoặc >= 18,7 px bold (14 pt) mới được hạ xuống 3:1 | Web, áp dụng tương phản |

Tương phản: chữ thường >= 4,5:1; chữ lớn >= 3:1; phần tử UI/icon HUD >= 3:1; chế độ high contrast 7:1 (XAG 102, WCAG).

## Báo cáo

- Đã làm: đọc 15 nguồn mở được (XAG 101/102, GAG 4 trang, IGDA GASIG, Apple HIG, M3 qua 2 nguồn, WCAG 1.4.3/1.4.11, Google Fonts metrics, Vietnamese Typography, accessrewind, Blendo, GamingOnLinux). Ghi vào file này.
- File: D:\Lastaway\projects\lastaway\art\inbox\T-027\nghien-cuu.md
- Chưa xác minh: line-height M3 từng bậc, tỉ lệ body height/em và metric thật của Be Vietnam Pro, số px chữ HUD của 4 game BR (không có số công bố), nguồn riêng cho PC ngồi bàn 50-70 cm (không có).
- Đề xuất: lấy XAG 101 PC (18 px body height) làm sàn tuyệt đối cho chữ phụ, nhưng vì mọi nguồn khác (28-32 px) nhắm TV 10 foot nên chữ cần đọc nhanh trong trận nên đặt cao hơn sàn PC, đo thực ở 1080p. Thang gợi ý để thử: sàn 18 px chỉ cho chữ không quan trọng, chữ HUD chính >= 24 px (còn là ngưỡng "chữ lớn" WCAG 3:1). Con số cuối do designer/Huy chốt sau khi chơi thử.
- Line-height cho tiếng Việt: dùng >= 1,3 em cho chữ một dòng (Google Fonts rule), 1,5 cho khối nhiều dòng (XAG). Cần đo Abreveacute của Be Vietnam Pro bằng fontTools trước.
- Chữ trên nền biến thiên: backplate bán trong suốt là chắc nhất; outline/shadow chỉ bổ trợ.
