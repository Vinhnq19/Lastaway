# Mẫu contract đợt code (docs/CONTRACT_V0x.md)

Integrator (unity) viết và khóa trước khi giao module engineer. Sau khi khóa chỉ Integrator sửa; mỗi lần sửa ghi ngày và lý do ngay tại mục bị sửa.

```
# Contract v0.x - <slug> - Đợt <số>[ chặng <số>]: <tên ngắn> (khóa YYYY-MM-DD)

Nguồn kiểu dữ liệu: `<đường dẫn file contract C#>`.
Phiên bản trước: v0.<x-1> (`docs/CONTRACT_V0<x-1>.md`). Contract **chỉ thêm**.

## Căn cứ quyết định
- Huy chọn ngày YYYY-MM-DD: <phương án S/M/L trong docs/SPRINT_0x_PROPOSAL.md>, <câu chốt bằng lời của Huy>.
- Số đang hiệu lực: `projects/<slug>/so-hien-hanh.md` (mục ...). Giá trị trong data là preset thử nghiệm, chỉnh bằng data, không bằng code.
- Hành vi mặc định khi không truyền dữ liệu mới: <giữ nguyên chính xác hành vi v0.<x-1>>.

## Kiểu dữ liệu thêm mới
| Kiểu / thành viên | Thuộc file | Ý nghĩa | Ghi chú |
|---|---|---|---|
| `<TypeName>` | | | |

## Luật
- <Luật 1: điều kiện chấp nhận, kết quả, lý do từ chối>
- <Bất biến phải kiểm bằng test: ví dụ không âm, không trừ hai lần, deterministic theo seed>
- <Ranh giới thông tin: lớp nào được đọc gì, không được đọc gì>

## Bảng quyền file theo việc
| Việc | Người làm | Chỉ được sửa/tạo |
|---|---|---|
| <mã> <tên> | engineer 1 | `<file>` (mới), `<file>` (chỉ thêm ca) |
| <mã> <tên> | engineer 2 | |
| <mã> <tên> | engineer 3 | |
| Tích hợp | Integrator | Contract, Bootstrap, build script, scene chính, chạy Unity |

Hai việc không chung file. Module engineer không chạy Unity, không sửa contract, không sửa assertion test cũ.

## Test hồi quy phải giữ
| Bộ test | Số ca hiện có | Chạy trên gói |
|---|---:|---|
| | | gốc |

Tất cả PASS không sửa assertion. Ca cũ buộc phải đổi: liệt kê ca, lý do, ngày Huy duyệt.

## Test mới bắt buộc
- <Bộ checks mới / số ca tối thiểu / biên phải có>

## Gói nội dung riêng (chỉ khi đổi số của test cũ)
- Thư mục: `StreamingAssets/Content/<tên-gói>/` (bộ file đầy đủ riêng: ...).
- Chọn gói khi chạy: `-pack <gốc>|<tên-gói>`; mặc định: <gói nào, sau điều kiện gì>.
- Validator cho gói mới: `<file>`.

## Ngoài phạm vi
- <Việc không làm trong đợt này và vì sao>

## Tiêu chí đóng đợt
- [ ] INTEGRATED: compile sạch, mọi file đổi nằm trong bảng quyền.
- [ ] VERIFIED: test hồi quy + test mới PASS, build `Builds/<tên-đợt>-<nền tảng>/` có manifest SHA256, smoke đạt trên build đó, JSON và ảnh ở `docs/evidence/`.
- [ ] Mốc lớn: kiểm trên bản sao sạch không `Library/`.
- [ ] Số đo cân bằng (nếu có) kèm seed, số trận, ngày, commit.
- [ ] PLAYTESTED: Huy chơi build có hash ... và đồng ý (ngày).
```
