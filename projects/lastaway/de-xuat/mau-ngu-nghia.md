# Màu ngữ nghĩa cho UI kit skin A (T-020, viết lại sau phản hồi Huy 07/11: "lạm dụng đỏ và xanh lá")

Trạng thái: đề xuất, chờ Huy duyệt. Sprite, mock và số đo trong `art/inbox/T-020/`. Chưa Unity, chưa qa.

## 1. Nghiên cứu (trang mở được trong phiên này, 07/11)

| Nguồn | Điều rút ra (trích ngắn) |
|---|---|
| WCAG 2.2 SC 1.4.1, https://www.w3.org/WAI/WCAG22/Understanding/use-of-color.html | "Color is not used as the only visual means of conveying information." Người có thiếu hụt màu có thể không thấy màu |
| Carbon, status indicator, https://carbondesignsystem.com/patterns/status-indicator-pattern/ | Trạng thái nên "rely on at least two of the following elements: color, shape, or symbol". Đỏ = nguy hiểm/lỗi, xanh lá = bình thường/thành công (là liên tưởng điển hình, không phải luật cứng). Đừng lạm dụng: "having more than five or six indicators can overwhelm users"; chỉ dùng chỉ báo khi người dùng cần hành động |
| Atlassian, color, https://atlassian.design/foundations/color | Vai trò success dành cho "a favorable outcome, such as a success message", danger cho "serious error states"; cảnh báo không dùng màu nhấn (accent) cho nghĩa trạng thái; trạng thái chọn nên kèm một dấu hiệu khác như dấu tích |
| UX Movement, https://uxmovement.com/buttons/how-to-design-destructive-actions-to-prevent-data-loss/ | Đỏ cho hành động phá hủy: "Reserve them for destructive actions only." Và: "Don't use a red warning signal on a destructive action if it's not on a confirmation screen." Phương án khác: "a standalone button with only a red text label" |
| Game Accessibility Guidelines, https://gameaccessibilityguidelines.com/ensure-no-essential-information-is-conveyed-by-a-fixed-colour-alone/ | "Ensure no essential information is conveyed by a fixed colour alone"; khó phân biệt đỏ/lục "affecting around 8-10% of males"; kèm nhãn chữ, ký hiệu, hoa văn hoặc hình |
| Xbox Accessibility Guideline 103, https://learn.microsoft.com/en-gb/gaming/accessibility/xbox-accessibility-guidelines/103 | "Color alone should never be used to represent information." Ví dụ Grounded: thiếu nguyên liệu = tam giác cảnh báo đỏ + chữ + số đỏ (màu nhỏ, kèm icon và chữ). Điều khiển bị khóa: không chỉ xám mà thêm khóa hoặc chữ "Not available" |
| Roblox UI/UX, https://create.roblox.com/docs/production/game-design/ui-ux-design | Quy ước phổ biến: xám cho nút không dùng được; "Consistency helps players learn to navigate the game more efficiently" |

Không dùng được: Material Design 3 color roles (https://m3.material.io/styles/color/roles) và Apple HIG Color (https://developer.apple.com/design/human-interface-guidelines/color): trang tải về chỉ có tiêu đề, không đọc được nội dung, nên không trích. Chưa tìm được nguồn mở được riêng cho "game UI đá/gỗ battle royale": quy tắc dưới đây suy từ hệ thiết kế (Carbon, Atlassian), hướng dẫn game accessibility và vai trò màu vàng kim đã có trong HUD (chọn, nhấn mạnh).

## 2. Quy tắc rút ra

Màu ngữ nghĩa là điểm nhấn trạng thái, diện tích nhỏ; không tô cả nút hành động thường, không tô cả nền toast hoặc tag.

| # | Quy tắc | Dùng khi | Diện tích tối đa | Không dùng khi | Nguồn |
|---|---|---|---|---|---|
| R1 | Nút hành động thường (Lưu, Chơi, Sẵn sàng) giữ vàng kim thương hiệu `kit_btn_primary_*` | mọi hành động tích cực thường | 1 nút chính mỗi màn | không tô nút xanh lá hay đỏ | Atlassian (accent không gánh nghĩa), UX Movement |
| R2 | Đỏ đầy nút (`kit_btn_danger_*`) chỉ cho hành động phá hủy không hoàn tác và chỉ trong hộp xác nhận (Xóa phòng) | hộp xác nhận | 1 nút đỏ mỗi hộp (khoảng 2% khung hình) | ngoài hộp xác nhận; Rời phòng, Rời trận | UX Movement |
| R3 | Rời phòng, Rời trận: nút phụ `kit_btn_secondary_*` + icon X nhạt + chữ `danger-text` | thoát có thể làm lại | icon 24 px + chữ | không tô đỏ cả nút | UX Movement ("red text label") |
| R4 | Xanh lá chỉ ở phản hồi nhỏ: icon dấu tích (32 px), viền mảnh ô nhập hợp lệ | thành công, sẵn sàng | icon và viền mảnh | không tô nền nút, toast, tag | Carbon (chỉ báo thưa), Atlassian |
| R5 | Hổ phách chỉ là icon cảnh báo + chữ | bo sắp thu, sắp hết giờ | icon 26 px | không tô nền | Xbox XAG 103 (ví dụ Grounded) |
| R6 | Toast: nền trung tính `kit_toast`, màu chỉ ở icon (tích, X, chấm than) + chữ | thông báo | icon 26 px | không tô nền toast | Carbon (hai kênh trở lên) |
| R7 | Tag trạng thái: nền trung tính `kit_tag_neutral`, icon màu (tích hoặc chấm xám) + chữ | Sẵn sàng, Chưa sẵn sàng | icon 18 px | không tô nền tag | Carbon |
| R8 | Mọi nghĩa có ít nhất hai kênh: màu + icon + chữ | luôn luôn | | màu một mình | WCAG 1.4.1, GAG, XAG 103 |
| R9 | Điều khiển tắt: xám, thêm khóa hoặc chữ "Không khả dụng" khi cần giải thích (nút tắt hiện chỉ xám, chưa có kênh phụ) | nút không dùng được | | chỉ xám khi người chơi cần biết lý do | Roblox (xám là quy ước), XAG 103 |
| R10 | Hạ độ bão hòa cho hợp lòng đá; trần tổng diện tích đỏ + xanh khoảng 3% khung hình mock | | 3% | | quy tắc đề xuất (không phải chuẩn); đo ở mục 4 |

Màu icon (đã gen lại giảm bão hòa, mã đo trên sprite hoặc yêu cầu gen): tích `86B36B`, X `D9695B`, cảnh báo `DBA645`, chấm `C8C8BC`; chữ `danger-text FF8A78`. Vàng kim `FFDC6E` giữ cho chọn, focus, thương hiệu.

## 3. Tương phản (đo)

| Mục | Nền | Tỉ số | Ngưỡng |
|---|---|---:|---|
| Icon tích `86B36B` | lòng panel (`2B2821`) / toast | 6,07 / 5,49 | 3:1 (không chữ) |
| Icon X `D9695B` | lòng panel / toast / nút phụ | 4,29 / 3,87 / 3,11 | 3:1 (nút phụ sát ngưỡng) |
| Icon cảnh báo `DBA645` | lòng panel / toast | 6,69 / 6,05 | 3:1 |
| Chấm `C8C8BC` | lòng panel / toast | 8,71 / 7,88 | 3:1 |
| Chữ `danger-text FF8A78` | lòng panel / nút phụ | 6,41 / 4,65 | 4,5:1 |
| Chữ nút Xóa phòng (trắng) | nền nút đỏ | 9,41 | 4,5:1 |
| Chữ trắng nút chính vàng | nền | 5,11 (thường); hover 3,96 (viền chữ 2 px, đã chấp nhận) | 4,5:1 |

## 4. Diện tích đỏ + xanh trên mock (`src/dien-tich-do-xanh.md`)

Đo bằng HSV trên vùng popup và toast (đỏ: sắc độ <= 14 hoặc >= 346 độ, bão hòa >= 0,45; xanh lá: 85 đến 160 độ, bão hòa >= 0,30; sáng >= 0,2), tính phần trăm pixel của vùng:

| Màn | Bản đầu | Bản lạm dụng | Bản sửa |
|---|---:|---:|---:|
| Cài đặt (đỏ / xanh / tổng) | 0,00 / 0,00 / 0,01 | 0,00 / 2,74 / 2,75 | 0,00 / 0,04 / 0,05 |
| Phòng chờ | 0,00 / 0,00 / 0,00 | 1,01 / 1,60 / 2,61 | 0,21 / 0,05 / 0,26 |
| Xác nhận Xóa phòng | | | 2,16 / 0,00 / 2,17 (một nút đỏ trong hộp xác nhận, theo R2) |

Bản đầu = không màu ngữ nghĩa (nút phụ, nút vàng, tag trung tính, không icon). Bản lạm dụng = vòng trước (nút xanh lá, nút đỏ, tag và toast tô nền). Bản sửa = quy tắc R1 đến R9. Giảm khoảng 10 lần diện tích ở Phòng chờ và Cài đặt so với bản lạm dụng.

## 5. Kiểm mù màu

`art/inbox/T-020/mo-phong-mu-mau.png`: bản sửa của Phòng chờ, Cài đặt, Xác nhận Xóa phòng ở ba cột (thường, deuteranopia, protanopia; ma trận Machado 2009, độ nặng 1,0). Màu một mình không đủ: xanh lá và đỏ chỉ cách ΔE 10,4 ở deuteranopia, đỏ và xám nâu 0,6 ở protanopia (`src/mo-phong-mu-mau-de.md`, đo trên bản lạm dụng). Bản sửa không dựa vào màu nền: nghĩa nằm ở icon (tích, X, chấm than, chấm xám) và chữ ("Sẵn sàng", "Rời phòng", "Xóa phòng"), nên vẫn đọc được trên ảnh mô phỏng. Mô phỏng là phép tính ma trận trên ảnh, chưa thử với người mù màu thật.

## 6. Còn lệch và chưa kiểm

- Phòng chờ: luật Sẵn sàng/Chưa sẵn sàng chưa được duyệt trong GDD; mock chỉ minh họa (có ghi chú trên ảnh).
- Nút chính vàng hover 3,96:1 (chữ có viền tối 2 px, đã chấp nhận); icon X trên nút phụ 3,11:1 sát ngưỡng 3:1.
- Nút tắt chỉ xám, chưa có khóa hoặc chữ (R9).
- Sprite bản lạm dụng (nút xanh lá, toast, tag, badge tô nền) đã chuyển sang `art/inbox/T-020/bo-lam-dung/`, không còn trong kit; `kit_input_success` giữ (viền mảnh).
- Trần 3% (R10) là đề xuất của art-ux, không phải chuẩn từ nguồn.
- Material 3 và Apple HIG không mở được nội dung, chưa đối chiếu.
- Chưa Unity, chưa qa.

## 7. Ra

`art/inbox/T-020/`: `kit_icon_{check,cross,warn,dot}` (gen lại giảm bão hòa, `src/sheet-sem3.png`, 1 vòng), `ui-kit-sheet.png` (mục màu ngữ nghĩa viết lại), `so-sanh-3-ban-cai-dat-phong-cho.png` (bản đầu, bản lạm dụng, bản sửa), `man-thu-cai-dat-{dau,lamdung,sua}.png`, `man-thu-phong-cho-{dau,lamdung,sua}.png`, `man-thu-xac-nhan-xoa-phong.png`, `mo-phong-mu-mau.png`; bản lạm dụng lưu để so sánh: `ui-kit-sheet-lamdung.png`, `mo-phong-mu-mau-lamdung.png`. Script: `src/tools/compose_sem2.py`.
