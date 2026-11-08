# T-027 cổng cỡ chữ (src/do_co_chu.py --gate)

Luật: cỡ thuộc {20, 24, 30, 40, 96}; sàn 20; gameplay không dưới 24 (số phím và số trên ô 20); font đúng tầng (từ 40 trở lên, không phải nút = Chakra Petch Bold; còn lại và mọi chữ nút = Be Vietnam Pro Bold).

| Nguồn | Dòng chữ | Dòng lỗi | Loại lỗi |
|---|---:|---:|---|
| A | 303 | 0 | không |
| B | 567 | 0 | không |
| Unity | 389 | 18 | gameplay duoi 24: 14, ngoai thang: 4, duoi san 20: 4 |

Mẫu lỗi Unity:

- HudEffectChip | 3s | 20.0 px | gameplay duoi 24
- HudKeyCap | R | 19.5 px | ngoai thang, duoi san 20
- HudSlot | (chu dong) | 19.5 px | ngoai thang, duoi san 20
- HudSlot | (chu dong) | 19.5 px | ngoai thang, duoi san 20
- HudSlot | 1 | 19.5 px | ngoai thang, duoi san 20
- MatchOverlay | Chưa thoát được, thử lại | 20.0 px | gameplay duoi 24
- MatchOverlay | '#10' | 20.0 px | gameplay duoi 24
- MatchOverlay | '#8' | 20.0 px | gameplay duoi 24