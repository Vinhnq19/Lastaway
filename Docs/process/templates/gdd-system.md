# Mẫu tài liệu hệ thống gameplay (docs/gdd/20-systems/<ten-he-thong>.md)

```
# <Tên hệ thống>

Trạng thái: nháp | đã duyệt | đã implement | cần sửa
Chủ sở hữu: designer · Cập nhật: YYYY-MM-DD

## Mục đích với người chơi
<Người chơi cảm thấy gì, muốn gì khi dùng hệ thống này. Một đoạn.>

## Luật
<Danh sách luật đánh số. Mỗi luật một câu, không có "thường", "có thể".>
1. ...
2. ...

## Tham số
| Tên | Giá trị khởi điểm | Khoảng hợp lệ | Ghi chú |
|---|---|---|---|
| ... | ... | ... | ... |
Nguồn dữ liệu: docs/gdd/data/<file>.csv

## Trường hợp biên
- Khi <điều kiện> thì <kết quả>.
- ...

## Yêu cầu cho engineer
- Input: ... Output: ...
- Sự kiện phát ra: ...
- Dữ liệu cần lưu: ...

## Yêu cầu cho art / UI
- Art: ...
- UI: màn hình / thành phần ...
- Audio: sự kiện cần tiếng ...
- Text: key strings ...

## Cách đo thành công
- <Hành vi quan sát được khi playtest, hoặc số đo nếu có.>

## Lệch khỏi bản gốc
(Chỉ dùng khi dự án làm lại hoặc bám một game tham chiếu; không có thì xóa mục này.)
| Điểm lệch | Bản gốc làm thế nào | Sửa điểm yếu số | Bằng chứng không làm tệ (sim / playtest / lý do) | Tùy chọn bật/tắt? |
|---|---|---|---|---|
| ... | ... | ... | ... | ... |
(Không lệch thì ghi "Không lệch".)

## Câu hỏi mở
- <Việc chưa quyết, ai cần quyết.>
```
