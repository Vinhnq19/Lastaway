# Mẫu file phối hợp đợt / hộp thư (docs/COORDINATION.md)

Dùng khi nhiều phiên hoặc nhiều AI (Claude, Codex) cùng làm trên một repo Unity. Integrator tạo file khi mở đợt đầu tiên.

```
# Phối hợp <slug>

Các bên không nhắn trực tiếp được; file này là hộp thư. Chỉ **nối** mục mới cuối "Nhật ký", không sửa hay xóa mục của bên khác.
Mỗi mục: `[YYYY-MM-DD HH:mm] TỪ -> ĐẾN: nội dung`.

## Quy ước
1. **Khóa Unity:** chỉ một bên chạy Unity (Editor, batchmode, build, player) tại một thời điểm. Trước khi chạy ghi `UNITY_LOCK: <bên>` ở dòng đầu "Trạng thái"; xong ghi `UNITY_LOCK: none`. Các bộ test chạy tuần tự.
2. **Sở hữu file:** bên nào đang sửa file nào ghi ở "Đang sửa". Không sửa file bên khác đang giữ.
3. **Source:** trong một đợt chỉ một bên sửa source; bên kia rà bằng chứng và tài liệu điều phối.
4. **Tài liệu chung** (`PROJECT_STATUS.md`, README): do <bên> cập nhật; bên khác gửi nội dung cần ghi qua nhật ký.
5. **Sắp hết hạn mức:** ghi mục `HANDOFF:` theo khuôn bên dưới. Bên nhận đọc, chạy lại test, ghi xác nhận rồi mới sửa tiếp.
6. Báo cáo chỉ ghi điều đã chạy thật, kèm đường dẫn log hoặc file evidence và hash build nếu có.

## Trạng thái
UNITY_LOCK: none
Contract hiện hành: v0.x (`docs/CONTRACT_V0x.md`)
Build mới nhất: `Builds/<tên>/` - manifest `docs/evidence/<tên>_MANIFEST.md`

## Đang sửa
| Bên / người | File hoặc vùng | Từ lúc | Theo việc |
|---|---|---|---|
| unity (Integrator) | Bootstrap, contract | YYYY-MM-DD HH:mm | |
| engineer 1 | | | |
| Codex | chỉ tài liệu điều phối | | |

## HANDOFF
<!-- Mỗi lần bàn giao một khối, mới nhất ở cuối. Bên nhận ghi "Đã nhận" vào Nhật ký. -->
HANDOFF: [YYYY-MM-DD HH:mm] <bên>
- Việc đang dở:
- File đã đổi:
- Lệnh / test đã chạy và kết quả (kèm log):
- Chưa chạy:
- Việc chưa làm, bước tiếp theo đề xuất:

## Nhật ký
[YYYY-MM-DD HH:mm] unity -> Codex: <đã chạy gì, kết quả số, đường dẫn evidence, bậc WRITTEN/INTEGRATED/VERIFIED, việc tiếp theo>. UNITY_LOCK: none.
```
