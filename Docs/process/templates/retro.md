# Mẫu biên bản retro (production/retros/YYYY-MM-DD-sprint-N.md)

```
# Retro Sprint N - YYYY-MM-DD

## Số liệu
| Chỉ số | Giá trị |
|---|---|
| Task lên kế hoạch / done | |
| Task bị trả lại ở review | |
| Task bị trả lại ở QA | |
| Bug S1-S2 phát hiện | |
| Số lần agent hỏi lại vì brief thiếu | |

## Việc tốt, lặp lại
- <điều gì, vì sao hiệu quả>

## Việc chưa tốt
| Vấn đề | Nguyên nhân gốc | Xảy ra ở agent / bước nào |
|---|---|---|
| ... | ... | ... |

## Hành động cải thiện
| Hành động | Loại | File sẽ sửa | Trạng thái |
|---|---|---|---|
| ... | playbook / prompt agent / quy trình / template | docs/playbooks/x.md | đã sửa / task T-### |

Loại hành động:
- **playbook**: thêm checklist hoặc lỗi hay gặp vào docs/playbooks/<vai-trò>.md
- **prompt agent**: sửa nguyên tắc trong .claude/agents/<vai-trò>.md
- **quy trình**: sửa docs/process/00-workflow.md hoặc definition-of-done.md, ghi DECISIONS.md
- **template**: sửa docs/process/templates/

## Bài học cho CPO
- <về cách brief, cách chia task, cách quyết>
```
