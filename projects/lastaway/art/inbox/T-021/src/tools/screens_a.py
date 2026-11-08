"""T-021 dot A (dung lai T-026, 07/11): luong vao tran tren kit K2 + P0. Chay: PYTHONIOENCODING=utf-8 python screens_a.py [ten...]
Ghi a/<ten>.png, a/contrast.json va a/contrast.md (do tuong phan chu tu pixel nen cho TOAN BO chu, khong do tay)."""
import sys, pathlib, json
sys.path.insert(0, str(pathlib.Path(__file__).parent))
import mock
from mock import *
TB = meta["kit_panel_popup"]["title_bar_px"]

def popup(im, w, h, title, y_off=0, close=True):
    x0, y0 = (W - w) // 2, (H - h) // 2 + y_off; put(im, "kit_panel_popup", (x0, y0), (w, h)); text(im, (x0 + w / 2, y0 + 24 + TB / 2), title, 36, GOLD, "mm")
    if close: close_btn(im, x0, y0, w)
    return x0, y0
def menu_btn(im, y, caption, kind="secondary", ic=None, w=420, state="normal", icon_name=None): button(im, kind, ((W - w) // 2, y), caption, ic, DANGER if ic else WHITE, (w, 80), state, 30, icon_name)
def overlay(im, a): im.alpha_composite(Image.new("RGBA", (W, H), (10, 10, 8, a)))

def account_card(im):
    put(im, "kit_toast", (1480, 36), (404, 96)); ico(im, "person", 1516, 84, 28)
    label(im, (1544, 62), "Đã đăng nhập", 18); text(im, (1544, 100), "huycv", 28, GOLD, "lm"); button(im, "secondary", (1688, 60), "Đăng xuất", None, WHITE, (180, 48), font=20)

def splash():
    im = bg(); logo(im, W / 2, 330, 820)
    put(im, "kit_progress_track", (720, 560), (480, 28)); put(im, "kit_progress_fill", (726, 566), (230, 16))
    text(im, (W / 2, 620), "Đang kết nối máy chủ...", 24, TXT, "mm")
    ok(im, (780, 680), "Kiểm tra phiên đăng nhập"); ico(im, "wifi", 796, 722, 22); text(im, (812, 722), "Kết nối máy chủ", 20, TXT, "lm")
    text(im, (60, 1030), "Máy chủ: Local 127.0.0.1:7777", 18, MUTED, "lm"); text(im, (1860, 1030), "v0.2", 18, MUTED, "rm"); return im

def home():
    im = bg(); logo(im, W / 2, 80, 760); account_card(im)
    y = 340; menu_btn(im, y, "Vào phòng", "primary"); y += 100; menu_btn(im, y, "Tạo phòng"); y += 100
    for c, n in (("Lịch sử", "kit_ico_clock"), ("Bảng xếp hạng", "kit_ico_trophy"), ("Cài đặt", "kit_ico_gear")): menu_btn(im, y, c, icon_name=n); y += 100
    menu_btn(im, y, "Thoát"); y += 100; ok(im, (W / 2 - 80, y + 18), "API sẵn sàng", 20)
    text(im, (1860, 1030), "v0.2", 18, MUTED, "rm"); return im

def join(state):
    im = home(); overlay(im, 205); x0, y0 = popup(im, 800, 620, "Vào phòng"); L = x0 + 60
    label(im, (L, y0 + 150), "Tên hiển thị", 20); input_(im, (L, y0 + 170), 680, "Huy")
    label(im, (L, y0 + 260), "Mã phòng (6 ký tự)", 20)
    chars = {"normal": "4X7K2", "err": "4X7K2P", "conn": "4X7K2P"}[state]; codebox(im, (L + 112, y0 + 290), chars, 5 if state == "normal" else -1, err=(state == "err"))
    if state == "err": err(im, (L + 112, y0 + 390), "Phòng đã đầy (10/10). Thử mã khác.")
    elif state == "conn": label(im, (L + 112, y0 + 390), "Đang kết nối tới phòng 4X7K2P...", 20); put(im, "kit_progress_track", (L, y0 + 420), (680, 28)); put(im, "kit_progress_fill", (L + 6, y0 + 426), (300, 16))
    else: label(im, (L + 112, y0 + 390), "Nhập mã do chủ phòng gửi. Không phân biệt chữ hoa.", 20)
    label(im, (L, y0 + (462 if state == "conn" else 436)), "Vào bằng tài khoản huycv", 18)
    by = footer(im, x0, y0, 800, 620); button(im, "secondary", (x0 + 80, by), "Hủy", None, WHITE, (250, 64)); button(im, "primary", (x0 + 800 - 80 - 250, by), "Vào phòng", None, WHITE, (250, 64), "disabled" if state == "conn" else "normal")
    return im

def login(error):
    im = bg(); logo(im, W / 2, 70, 700)
    x0, y0 = (W - 700) // 2, 220; put(im, "kit_panel_m", (x0, y0), (700, 650)); text(im, (x0 + 60, y0 + 74), "Đăng nhập", 36, GOLD, "lm")
    put(im, "kit_tab_on", (x0 + 60, y0 + 100), (280, 64)); text(im, (x0 + 200, y0 + 140), "Đăng nhập", 22, DARK, "mm", 0); put(im, "kit_tab_off", (x0 + 360, y0 + 100), (280, 64)); text(im, (x0 + 500, y0 + 150), "Đăng ký", 22, TXT, "mm")
    label(im, (x0 + 60, y0 + 215), "Tên đăng nhập", 20); input_(im, (x0 + 60, y0 + 237), 580, "huycv", "error" if error else "normal")
    label(im, (x0 + 60, y0 + 321), "Mật khẩu", 20); input_(im, (x0 + 60, y0 + 343), 580, "••••••••" if error else "Mật khẩu", "error" if error else "normal", ph=not error)
    if error: err(im, (x0 + 60, y0 + 429), "Sai tên đăng nhập hoặc mật khẩu.")
    button(im, "primary", (x0 + 60, y0 + 470), "Đăng nhập", None, WHITE, (580, 64)); button(im, "secondary", (x0 + 60, y0 + 550), "Máy chủ (nâng cao)", None, WHITE, (580, 56), font=22)
    return im

ROOMS = [("Phòng mặc định", "Mặc định", "6/10", None, "normal"), ("Leo rank tối nay", "Của bạn", "3/10", "4X7K2P", "selected"), ("Luyện tập", "Của bạn", "10/10", "9HM3TQ", "full")]
def room_card(im, y, nm, tg, cap, code, st):
    put(im, f"kit_card_room_{st}", (120, y), (1680, 120)); dim_ = st == "full"; tc = TXT
    text(im, (170, y + 60), nm, 28, tc, "lm"); put(im, "kit_tag_neutral", (560, y + 40), (140, 40)); text(im, (630, y + 60), tg, 18, WHITE, "mm"); text(im, (760, y + 60), cap + (" đầy" if dim_ else ""), 24, tc, "lm")
    if code:
        text(im, (960, y + 60), code, 26, GOLD, "lm"); button(im, "secondary", (1130, y + 30), "Sao chép", None, WHITE, (190, 52), font=20, icon_name="kit_ico_copy")
        button(im, "primary", (1340, y + 30), "Vào", None, WHITE, (110, 52), "disabled" if dim_ else "normal", 22); button(im, "secondary", (1470, y + 30), "Xóa", None, WHITE, (110, 52), font=22)
    else:
        label(im, (960, y + 60), "mã do chủ phòng gửi", 18); button(im, "primary", (1340, y + 30), "Vào", None, WHITE, (110, 52), font=22)
def rooms(state):
    im = bg(0.7); header(im, "Phòng của tôi", "2 phòng bổ sung tối đa")
    if state == "ok":
        for i, r in enumerate(ROOMS): room_card(im, 190 + i * 140, *r)
    elif state == "empty":
        room_card(im, 190, *ROOMS[0]); text(im, (W / 2, 520), "Bạn chưa có phòng bổ sung.", 28, TXT, "mm"); text(im, (W / 2, 566), "Bấm Tạo phòng để tạo phòng riêng và gửi mã cho bạn bè.", 22, MUTED, "mm")
    else:
        put(im, "kit_toast", (120, 200), (1680, 72)); icon(im, "kit_icon_cross", 170, 236, 28); text(im, (210, 236), "Không tải được danh sách phòng. Kiểm tra máy chủ rồi thử lại.", 24, TXT, "lm"); button(im, "secondary", (1560, 210), "Thử lại", None, WHITE, (220, 52), font=22, icon_name="kit_ico_refresh")
    button(im, "primary", (W - 120 - 280, 940), "Tạo phòng", None, WHITE, (280, 72), font=28); return im

def rooms_delete():
    im = rooms("ok"); overlay(im, 190); w, h = 720, 400; x0, y0 = (W - w) // 2, (H - h) // 2; put(im, "kit_panel_popup", (x0, y0), (w, h)); text(im, (x0 + w / 2, y0 + 24 + TB / 2), "Xóa phòng?", 34, GOLD, "mm"); close_btn(im, x0, y0, w)
    text(im, (x0 + w / 2, y0 + 24 + TB + 56), "Xóa phòng Luyện tập (9HM3TQ)? Không hoàn tác.", 24, TXT, "mm"); text(im, (x0 + w / 2, y0 + 24 + TB + 94), "Chỉ xóa được phòng đang trống.", 20, MUTED, "mm")
    by = footer(im, x0, y0, w, h); button(im, "secondary", (x0 + 70, by), "Hủy", None, WHITE, (240, 64)); button(im, "danger", (x0 + w - 70 - 240, by), "Xóa phòng", None, WHITE, (240, 64))
    return im

def create(state):
    im = home(); overlay(im, 205); x0, y0 = popup(im, 820, 660, "Tạo phòng"); L = x0 + 60
    label(im, (L, y0 + 150), "Tên phòng (1 đến 48 ký tự)", 20); input_(im, (L, y0 + 172), 700, "Leo rank tối nay")
    label(im, (L, y0 + 264), "Sức chứa", 20); text(im, (L + 700, y0 + 264), "10 người", 26, GOLD, "rm")
    put(im, "kit_slider_track", (L, y0 + 304), (700, 16)); put(im, "kit_slider_fill", (L, y0 + 307), (700, 10)); put(im, "kit_knob", (L + 700 - 16, y0 + 296))
    for i, t in enumerate(("1", "5", "10")): label(im, (L + [0, 350, 700][i] - (0 if i == 0 else (10 if i == 1 else 20)), y0 + 342), t, 18)
    label(im, (L, y0 + 392), "Cần tối thiểu 6 người để bắt đầu trận.", 20); label(im, (L, y0 + 424), "Còn 1/2 phòng bổ sung. Phòng trống sau 10 phút sẽ bị thu hồi.", 20)
    if state == "err": err(im, (L, y0 + 464), "Đã đủ 2 phòng bổ sung. Xóa một phòng rồi thử lại.")
    by = footer(im, x0, y0, 820, 660); button(im, "secondary", (x0 + 80, by), "Hủy", None, WHITE, (250, 64)); button(im, "primary", (x0 + 820 - 80 - 250, by), "Tạo phòng", None, WHITE, (250, 64), "disabled" if state == "err" else "normal")
    return im

def lobby(countdown):
    im = bg(0.7); header(im, "Phòng chờ", None); text(im, (1700, 84), "Mã phòng 4X7K2P", 24, TXT, "rm"); button(im, "secondary", (1715, 56), "Chép", None, WHITE, (130, 56), font=20, icon_name="kit_ico_copy")
    n = 7 if countdown else 4; names = ["Huy (chủ phòng)", "V.Huy", "Hào", "Vinh", "Vũ", "Quang Huy", "Khoa", "Minh", "Long", "Nam"]
    for i in range(10):
        col, row = divmod(i, 5); rx = 60 + col * 470; ry = 170 + row * 112; filled = i < n
        put(im, "kit_slot_player_notready" if filled else "kit_card_room_full", (rx, ry), (440, 96)); text(im, (rx + 112, ry + 48) if filled else (rx + 40, ry + 48), names[i] if filled else "Chỗ trống", 24 if filled else 22, TXT if filled else MUTED, "lm")
    px = 1030; put(im, "kit_panel_m", (px, 170), (830, 520)); text(im, (px + 60, 250), "Bắt đầu sau", 24, TXT, "lm")
    if countdown: text(im, (px + 60, 350), "12", 120, GOLD, "lm"); text(im, (px + 60 + tw("12", 120) + 16, 378), "giây", 28, TXT, "lm")
    else: text(im, (px + 60, 350), "Chờ", 90, TXT, "lm"); text(im, (px + 60, 430), "Chưa đủ người, chưa đếm ngược", 22, MUTED, "lm")
    ico(im, "person", px + 76, 480, 26); text(im, (px + 118, 480), f"{n}/10 người trong phòng", 24, TXT, "lm"); label(im, (px + 60, 540), "Tối thiểu 6 người để bắt đầu", 22); label(im, (px + 60, 580), "Bản đồ: Đảo rừng 50x50 ô", 22)
    label(im, (60, 760), "Trận tự bắt đầu sau 15 giây khi đủ 6 người. Thiếu người thì hủy đếm ngược.", 22)
    button(im, "secondary", (60, 900), "Rời phòng", "kit_icon_cross", DANGER, (320, 72), font=28)
    button(im, "secondary", (W - 60 - 420, 900), "Sẵn sàng (tùy chọn)", None, WHITE, (420, 72), font=26)
    note(im, "Minh họa: nút Sẵn sàng là luật chưa duyệt trong GDD (M2); mặc định tắt bằng cờ lobbyReadyEnabled.", 1030)
    return im

STEPS = ["Kết nối máy chủ", "Nạp cấu hình cân bằng", "Tải bản đồ và vật cản", "Chờ người chơi khác"]
def loading(error):
    im = bg(0.7); logo(im, W / 2, 110, 440); text(im, (W / 2, 280), "Đảo rừng · 50x50 ô · 10 người", 34, TXT, "mm")
    if not error:
        put(im, "kit_progress_track", (680, 330), (560, 28)); put(im, "kit_progress_fill", (686, 336), (int(548 * .68), 16)); label(im, (680, 385), "Đang tải bản đồ...", 22); text(im, (1240, 385), "68%", 24, TXT, "rm")
        for i, s in enumerate(STEPS):
            y = 440 + i * 40; icon(im, "kit_icon_check" if i < 2 else "kit_icon_dot", 700, y, 22); text(im, (730, y), s, 22, TXT if i < 3 else MUTED, "lm")
        label(im, (680, 610), "Còn tối đa 30 giây. Người chưa tải xong sẽ bị loại khỏi trận.", 20)
    else:
        put(im, "kit_panel_s", (680, 330), (560, 300)); icon(im, "kit_icon_cross", 740, 392, 30); text(im, (780, 392), "Không vào được trận", 28, TXT, "lm")
        text(im, (730, 450), "Dữ liệu bản đồ không khớp với máy chủ.", 22, TXT, "lm"); text(im, (730, 485), "Lý do: phiên bản dữ liệu khác nhau (ví dụ).", 20, MUTED, "lm")
        button(im, "primary", (730, 520), "Về trang chủ", None, WHITE, (460, 56), font=24)
    put(im, "kit_panel_s", (480, 660), (960, 200)); label(im, (530, 712), "Điều khiển", 20, GOLD)
    for i, (k, d) in enumerate([("WASD", "Di chuyển"), ("Chuột", "Ngắm, bắn"), ("F", "Nhặt"), ("M", "Bản đồ"), ("ESC", "Cài đặt")]):
        cx = 570 + i * 170; wk = max(52, int(tw(k, 22)) + 30); keycap(im, (cx - wk / 2, 735), k, w=wk); text(im, (cx, 812), d, 18, MUTED, "mm")
    put(im, "kit_toast", (460, 900), (1000, 64)); text(im, (490, 932), "MẸO", 18, GOLD, "lm"); text(im, (560, 932), "Bụi rậm che bạn khỏi tầm nhìn, nhưng bắn sẽ lộ vị trí ngay lập tức.", 22, TXT, "lm")
    return im

ALL = {"a1-khoi-dong": splash, "a2-sanh-chinh": home, "a3-vao-phong": lambda: join("normal"), "a3-vao-phong-loi": lambda: join("err"), "a3-vao-phong-dang-ket-noi": lambda: join("conn"),
       "a3b-dang-nhap": lambda: login(False), "a3b-dang-nhap-loi": lambda: login(True), "a4-phong-cua-toi": lambda: rooms("ok"), "a4-phong-cua-toi-trong": lambda: rooms("empty"), "a4-phong-cua-toi-loi": lambda: rooms("err"), "a4-xoa-phong": rooms_delete,
       "a5-tao-phong": lambda: create("ok"), "a5-tao-phong-loi": lambda: create("err"), "a6-phong-cho-cho-du-nguoi": lambda: lobby(False), "a6-phong-cho-dem-nguoc": lambda: lobby(True), "a7-dang-tai": lambda: loading(False), "a7-dang-tai-loi": lambda: loading(True)}
if __name__ == "__main__":
    only = sys.argv[1:]
    for n, f in ALL.items():
        if only and n not in only: continue
        mock.CUR[0] = n; save(f(), f"a/{n}.png"); print(n)
    rows = mock.LOG; (OUT / "a" / "contrast.json").write_text(json.dumps(rows, ensure_ascii=False, indent=0), encoding="utf-8")
    bad = [r for r in rows if r["ratio"] < 4.5]; md = ["# T-021 đợt A: tương phản chữ (đo tự động từ pixel nền ngay trước khi vẽ chữ)", "", f"Tổng {len(rows)} dòng chữ (chữ trên nút tắt không tính). Dưới 4,5:1: {len(bad)}. Thấp nhất theo p10 và p90 độ sáng nền trong khung chữ; chưa trừ viền chữ 2 px.", "", "| Mock | Chữ | Cỡ | Màu | Tỉ số |", "|---|---|---:|---|---:|"]
    for r in sorted(bad, key=lambda r: r["ratio"]): md.append(f"| {r['mock']} | {r['text'][:50]} | {r['size']} | {r['fill']} | {r['ratio']} |")
    (OUT / "a" / "contrast.md").write_text("\n".join(md), encoding="utf-8"); print("tong", len(rows), "duoi 4.5:", len(bad))
