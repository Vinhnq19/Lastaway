# Quy trình làm việc của studio

Luật chơi nội bộ. Đổi quy trình chỉ qua `/retro`, ghi `production/DECISIONS.md`. Lý do và sự cố gốc: `KIEN-TRUC-STUDIO.md`.

## 1. Ba tầng

| Tầng | Ai | Giao việc bằng |
|---|---|---|
| 1 | Huy | Tin cho CPO |
| 2 | CPO | Tạo phiên vai (`/role <vai>`), sau đó `SendMessage` brief 5 phần |
| 2 | Phiên vai: designer, level, unity, art-ux, qa | `Agent` gọi subagent (luôn truyền `model`), nhắn vai khác trong đợt |
| 3 | Subagent: research, economy, algo, module-engineer, art-bridge, general-purpose | Trả kết quả về phiên vai đã gọi |

## 2. Vòng đời một task

```
todo -> doing -> review -> done
  ^       |        |
  +--- blocked <- trả lại
```

| Trạng thái | Ai đổi |
|---|---|
| `todo` (có tiêu chí "mở file X thấy Y" / "chạy Z ra W") | CPO |
| `doing`, `review`, `blocked` | Vai nhận task |
| `done` | Chỉ CPO, sau khi đối chiếu DoD và mở file bằng chứng |

Việc có code Unity báo thêm bậc: WRITTEN -> INTEGRATED -> VERIFIED -> PLAYTESTED. Chỉ PLAYTESTED đóng đợt có gameplay.

## 3. Pipeline theo loại việc

| Việc | Đường đi |
|---|---|
| Sửa số, text, bug hẹp | 1 vai làm -> CPO kiểm -> done |
| Ý tưởng mới | `/kickoff`: designer (thẻ luật) -> Huy chốt bằng lời -> level (engine, solver, 15 level đo bot, prototype web) -> qa -> Huy chơi: cổng "vui chưa" |
| Tính năng Unity | designer (luật + biên) -> unity (đề xuất S/M/L -> contract -> module-engineer -> gộp, test, build) -> qa -> Huy chơi |
| Bộ level, curve | level (bảng mục tiêu, dựng, đo, soi mắt) -> qa (chơi thật) -> designer cập nhật GDD |
| UI, asset | art-ux (spec + rubric) -> tự làm hoặc art-bridge -> kiểm máy -> qa chấm chéo 1/4 -> Huy duyệt -> unity gắn -> qa chụp cảnh |
| Economy, liveops | designer gọi economy; chỉ sau cổng "vui chưa" |

Review chéo: designer soát đúng luật, unity soát kỹ thuật, art-ux soát hình. qa luôn độc lập với người làm.

## 4. Đợt và phối hợp

- Đợt lớn mở bằng `templates/sprint-proposal.md` (S/M/L); Huy chọn rồi mới khóa `templates/contract.md`.
- Mỗi đợt một file phối hợp theo `templates/coordination.md`: ai giữ file nào, `UNITY_LOCK`, nhật ký chỉ nối, `HANDOFF:` khi sắp hết hạn mức.
- Tối đa 3 phiên vai chạy cùng lúc, mỗi phiên vai tối đa 3 subagent.

## 5. Nghi thức

| Nghi thức | Khi | Ra |
|---|---|---|
| Kickoff | Sản phẩm mới | `projects/<slug>/`, thẻ luật, backlog |
| Sprint | Huy gõ `/sprint` hoặc "làm tiếp" | Task done, báo cáo mốc |
| Retro | Sau mỗi mốc | `production/retros/`, sửa playbook, prompt, cổng |
| Playtest | Có bản chơi được | `projects/<slug>/qa/playtest/`, Huy chơi |

## 6. Data và mã nguồn

- Tham số và level ở data có schema/validator; designer, level sửa data không cần unity.
- Repo Unity: trong `projects/<slug>/unity/` hoặc repo riêng có README trỏ tới; phải có git từ ngày đầu.
- Thư mục làm việc chung luôn xanh: hook pre-commit chạy cổng kiểm.
