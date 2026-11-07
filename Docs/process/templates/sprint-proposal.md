# Mẫu đề xuất đợt (docs/SPRINT_0x_PROPOSAL.md)

Integrator viết khi mở đợt code lớn, gửi CPO để Huy chọn. Đề xuất không khóa luật và không bắt đầu triển khai. Huy chọn phương án rồi mới khóa contract.

```
# Đề xuất đợt 0x - <slug>: <chủ đề>

Trạng thái: ĐỀ XUẤT, chờ Huy chọn phương án và chốt câu hỏi ở mục 5.
Điều kiện bắt đầu: <đợt trước đóng ở bậc PLAYTESTED / điều kiện khác>.
Số liệu gameplay trong tài liệu là giả thuyết thử nghiệm. Ước lượng là ngày công kỹ thuật tập trung, sai số khoảng ±50%.

## 1. Vấn đề (từ dữ liệu thật của đợt trước)
| # | Quan sát có bằng chứng (số, seed, số trận, ngày, file) | Hệ quả với thiết kế | Chưa biết |
|---|---|---|---|
| P1 | | | |

Kết luận ngắn: <đợt trước chứng minh được gì, chưa chứng minh gì>.

## 2. Hạng mục ứng viên
| Mã | Hạng mục | Ngày công | Nguồn ước lượng | Phụ thuộc | Giải vấn đề |
|---|---|---:|---|---|---|
| H1 | | | backlog / ước tính | | P1 |

## 3. Phương án
### S - <tên>: <hạng mục>
| Ngày công | Phụ thuộc | Rủi ro chính |
|---|---|---|
| | | |
Tiêu chí nghiệm thu đo được:
1. Test hồi quy hiện có PASS không sửa assertion.
2. <tiêu chí mới, có số; ngưỡng là giả thuyết thì ghi rõ>
Đánh đổi: <giải vấn đề nào, không giải vấn đề nào>.

### M - <tên> (khuyến nghị nếu có)
<cùng khuôn S, ghi phần thêm so với S>

### L - <tên>
<cùng khuôn, nêu rõ rủi ro nhiều biến chưa kiểm cùng lúc>

## 4. So sánh nhanh
| | S | M | L |
|---|---|---|---|
| Ngày công | | | |
| Vấn đề giải được | | | |
| Số chặng | | | |
| Rủi ro chính | | | |

## 5. Câu hỏi Huy cần chốt
| # | Câu hỏi | Phương án nghiêng về | Ảnh hưởng nếu chọn khác |
|---|---|---|---|
| Q1 | | | |

## 6. Chặng và cổng Huy chơi
| Chặng | Hạng mục | Build ra | Cổng Huy chơi (mở build nào, thử gì, câu hỏi cần trả lời) | Nếu cổng không qua |
|---|---|---|---|---|
| 1 | | `Builds/<tên>-c1/` | | dừng ở chặng 1, dồn ngân sách sang cân bằng |
| 2 | | | | |
Chặng sau chỉ mở khi Huy chơi chặng trước và đồng ý (qua CPO).

## 7. Cách kiểm chứng
- Test hồi quy giữ (bộ, số ca), chạy tuần tự trên cùng source và build.
- Test mới, số đo mới (công cụ đo có test, số kèm seed, số trận, ngày).
- Gate người thật: phần nào không thay bằng test tự động được.

## 8. Rủi ro kỹ thuật
| Rủi ro | Dấu hiệu sớm | Cách giảm |
|---|---|---|

## 9. Khuyến nghị
<Một đoạn: chọn phương án nào, vì sao, theo số ở mục 1.>
```
