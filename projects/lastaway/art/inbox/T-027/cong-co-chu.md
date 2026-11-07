# T-027 cổng cỡ chữ (src/do_co_chu.py --gate)

Luật: cỡ thuộc {20, 24, 30, 40, 96}; sàn 20; gameplay không dưới 24 (số phím và số trên ô 20); font đúng tầng (từ 40 trở lên, không phải nút = Chakra Petch Bold; còn lại và mọi chữ nút = Be Vietnam Pro Bold).

| Nguồn | Dòng chữ | Dòng lỗi | Loại lỗi |
|---|---:|---:|---|
| A | 303 | 0 | không |
| B | 567 | 0 | không |
| Unity | 353 | 206 | ngoai thang: 205, duoi san 20: 49, gameplay duoi 24: 29 |

Mẫu lỗi Unity:

- Hud | Adrenaline | 19.5 px | ngoai thang, duoi san 20
- Hud | Giáp | 19.5 px | ngoai thang, duoi san 20
- Hud | Di chuyển để hủy | 19.5 px | ngoai thang, duoi san 20
- Hud | (chu dong) | 28.0 px | ngoai thang
- Hud | /100 | 19.5 px | ngoai thang, duoi san 20
- Hud | +2 món khác | 19.5 px | ngoai thang, duoi san 20
- Hud | (chu dong) | 28.0 px | ngoai thang
- Hud | / 30 | 18.0 px | ngoai thang, duoi san 20