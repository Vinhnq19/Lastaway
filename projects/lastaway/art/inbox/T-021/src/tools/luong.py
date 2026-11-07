"""T-021 so do luong man hinh (dot A net lien, dot B net dut)."""
import sys, pathlib, math
sys.path.insert(0, str(pathlib.Path(__file__).parent))
from mock import F, OUT, GOLD, TXT, MUTED
from PIL import Image, ImageDraw
Wd, Hd = 2400, 1000; im = Image.new("RGB", (Wd, Hd), (31, 29, 24)); d = ImageDraw.Draw(im)
N = {"boot": "Khởi động", "login": "Đăng nhập\n(Đăng ký)", "home": "Sảnh chính\n(đã đăng nhập)", "join": "Vào phòng\n(modal: tên + mã)", "create": "Tạo phòng\n(modal)", "rooms": "Phòng của tôi\n(danh sách, xóa)",
     "set": "Cài đặt\n(modal)", "lobby": "Phòng chờ\n(countdown 15 s)", "load": "Đang tải trận\n(lỗi: Về trang chủ)", "play": "Trong trận\n(HUD r11)", "more": "Trong trận mở thêm:\nESC cài đặt, túi đồ (Tab),\nbản đồ M, phản hồi HUD",
     "spec": "Bị loại: xem tiếp\n(spectator)", "win": "Thắng\n(Chơi lại / Về trang chủ)", "lose": "Thua: hạng, nguyên nhân\n(Chơi lại / Về trang chủ)", "dc": "Mất kết nối: kết nối lại\n(xảy ra ở Phòng chờ,\nĐang tải, Trong trận)"}
POS = {"boot": (200, 300, "A"), "login": (200, 520, "A"), "home": (620, 300, "A"), "join": (1060, 175, "A"), "create": (1060, 300, "A"), "rooms": (1060, 470, "A"), "set": (620, 520, "A"), "lobby": (1500, 300, "A"), "load": (1900, 300, "A"),
       "play": (2250, 300, "B"), "more": (2250, 130, "B"), "spec": (1900, 560, "B"), "win": (2250, 560, "B"), "lose": (1900, 780, "B"), "dc": (1250, 880, "B")}
SZ = (330, 100)
def box(k):
    cx, cy, g = POS[k]; x0, y0 = cx - SZ[0] // 2, cy - SZ[1] // 2; col = GOLD if g == "A" else (170, 170, 160)
    if g == "A": d.rectangle([x0, y0, x0 + SZ[0], y0 + SZ[1]], fill=(43, 40, 33), outline=col, width=3)
    else:
        d.rectangle([x0, y0, x0 + SZ[0], y0 + SZ[1]], fill=(36, 34, 29))
        for x in range(x0, x0 + SZ[0], 14): d.line([(x, y0), (x + 8, y0)], fill=col, width=2); d.line([(x, y0 + SZ[1]), (x + 8, y0 + SZ[1])], fill=col, width=2)
        for y in range(y0, y0 + SZ[1], 14): d.line([(x0, y), (x0, y + 8)], fill=col, width=2); d.line([(x0 + SZ[0], y), (x0 + SZ[0], y + 8)], fill=col, width=2)
    d.multiline_text((cx, cy), N[k], font=F(20), fill=TXT, anchor="mm", align="center", spacing=4)
def edge(a, b, lab="", dash=False):
    ax, ay, _ = POS[a]; bx, by, _ = POS[b]; dx, dy = bx - ax, by - ay
    def clip(cx, cy, vx, vy):
        sx = (SZ[0] / 2) / abs(vx) if vx else 1e9; sy = (SZ[1] / 2) / abs(vy) if vy else 1e9; t = min(sx, sy); return cx + vx * t, cy + vy * t
    p0 = clip(ax, ay, dx, dy); p1 = clip(bx, by, -dx, -dy); col = (200, 200, 190)
    if dash:
        n = int(math.hypot(p1[0] - p0[0], p1[1] - p0[1]) / 16)
        for i in range(0, n, 2): d.line([(p0[0] + (p1[0] - p0[0]) * i / n, p0[1] + (p1[1] - p0[1]) * i / n), (p0[0] + (p1[0] - p0[0]) * (i + 1) / n, p0[1] + (p1[1] - p0[1]) * (i + 1) / n)], fill=col, width=2)
    else: d.line([p0, p1], fill=col, width=3)
    ang = math.atan2(p1[1] - p0[1], p1[0] - p0[0]); d.polygon([p1, (p1[0] - 16 * math.cos(ang - .4), p1[1] - 16 * math.sin(ang - .4)), (p1[0] - 16 * math.cos(ang + .4), p1[1] - 16 * math.sin(ang + .4))], fill=col)
    if lab: d.text(((p0[0] + p1[0]) / 2, (p0[1] + p1[1]) / 2 - 14), lab, font=F(17), fill=MUTED, anchor="mm", stroke_width=3, stroke_fill=(31, 29, 24))
E = [("boot", "login", "chưa có phiên", 0), ("boot", "home", "JWT ok", 0), ("login", "home", "đăng nhập xong", 0), ("home", "join", "Vào phòng", 0), ("home", "create", "Tạo phòng", 0), ("home", "set", "Cài đặt", 0),
     ("join", "lobby", "mã đúng", 0), ("create", "rooms", "tạo xong", 0), ("rooms", "lobby", "Vào", 0), ("lobby", "load", "15 s", 0), ("load", "play", "", 0),
     ("play", "more", "ESC, Tab, M", 0), ("play", "spec", "bị loại", 0), ("spec", "lose", "hết người", 0), ("play", "win", "còn một người", 0)]
for a, b, l, dash in E: edge(a, b, l, bool(dash))
for k in POS: box(k)
d.text((40, 30), "Sơ đồ luồng màn hình Lastaway (T-021)", font=F(34), fill=GOLD)
d.text((40, 80), "Viền vàng = đợt A (luồng vào trận). Viền xám = đợt B (trong và sau trận). Cả hai đã có mock. Ngoài phạm vi: bị hạ gục, nhảy dù.", font=F(20), fill=MUTED)
d.text((40, 950), "Chơi lại -> Phòng chờ; Về sảnh -> Sảnh. Mất kết nối: kết nối lại được thì về trận, quá 15 s thì bị loại, luôn có Về sảnh. Đăng nhập bắt buộc trước Sảnh (Huy, 07/11).", font=F(20), fill=MUTED)
im.save(OUT / "luong.png"); print("ok")
