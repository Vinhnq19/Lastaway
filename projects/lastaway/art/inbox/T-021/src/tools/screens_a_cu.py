"""T-021 dot A: luong vao tran. Moi man dung sprite kit T-020 (GPT) va chu; tra ve dict ten file -> anh."""
import sys, pathlib
sys.path.insert(0, str(pathlib.Path(__file__).parent))
from mock import *
TB = meta["kit_panel_popup"]["title_bar_px"]

def popup(im, w, h, title, y_off=0):
    x0, y0 = (W - w) // 2, (H - h) // 2 + y_off; put(im, "kit_panel_popup", (x0, y0), (w, h)); text(im, (x0 + w / 2, y0 + 24 + TB / 2), title, 36, GOLD, "mm"); return x0, y0
def menu_btn(im, y, caption, kind="secondary", ic=None, w=420, state="normal", col=WHITE): button(im, kind, ((W - w) // 2, y), caption, ic, col, (w, 64), state)

def account_card(im, logged):
    put(im, "kit_toast", (1480, 36), (404, 96))
    if logged:
        label(im, (1510, 62), "Đã đăng nhập", 18); text(im, (1510, 100), "huycv", 28, GOLD, "lm"); button(im, "secondary", (1710, 58), "Đăng xuất", None, WHITE, (150, 52), font=20)
    else:
        label(im, (1510, 62), "Chưa đăng nhập", 18); text(im, (1510, 100), "Khách", 28, TXT, "lm"); button(im, "secondary", (1700, 58), "Đăng nhập", None, WHITE, (160, 52), font=20)

def splash():
    im = bg(); logo(im, W / 2, 330, 820)
    put(im, "kit_progress_track", (720, 560), (480, 28)); put(im, "kit_progress_fill", (726, 566), (230, 16))
    text(im, (W / 2, 620), "Đang kết nối máy chủ...", 24, TXT, "mm")
    ok(im, (780, 680), "Kiểm tra phiên đăng nhập"); icon(im, "kit_icon_dot", 792, 722, 22); text(im, (812, 722), "Kết nối máy chủ", 20, TXT, "lm")
    text(im, (60, 1030), "Máy chủ: Local 127.0.0.1:7777", 18, MUTED, "lm"); text(im, (1860, 1030), "v0.2", 18, MUTED, "rm"); return im

def home(logged):
    im = bg(); logo(im, W / 2, 80, 760); account_card(im, logged)
    y = 340
    menu_btn(im, y, "Vào phòng", "primary"); y += 80
    if logged: menu_btn(im, y, "Tạo phòng")
    else:
        menu_btn(im, y, "Tạo phòng", "secondary", None, 420, "normal", MUTED); text(im, (W / 2 + 240, y + 32), "cần đăng nhập", 18, MUTED, "lm")
    y += 80
    for c in ("Lịch sử", "Bảng xếp hạng", "Cài đặt", "Thoát"):
        menu_btn(im, y, c); y += 80
    ok(im, (W / 2 - 80, y + 18), "API sẵn sàng", 20)
    if not logged: text(im, (W / 2, y + 70), "Vào phòng bằng mã không cần tài khoản. Tạo hoặc xóa phòng cần đăng nhập.", 20, MUTED, "mm")
    text(im, (1860, 1030), "v0.2", 18, MUTED, "rm"); return im

def join(state):
    im = bg(); home_bg = home(True); im = home_bg; im.alpha_composite(Image.new("RGBA", (W, H), (10, 10, 8, 205)))
    x0, y0 = popup(im, 800, 600, "Vào phòng"); L = x0 + 60
    label(im, (L, y0 + 150), "Tên hiển thị", 20); input_(im, (L, y0 + 170), 680, "Huy")
    label(im, (L, y0 + 260), "Mã phòng (6 ký tự)", 20)
    chars = {"normal": "4X7K2", "err": "4X7K2P", "conn": "4X7K2P"}[state]; codebox(im, (L + 104, y0 + 290), chars, 5 if state == "normal" else -1)
    if state == "err": err(im, (L + 104, y0 + 392), "Phòng đã đầy (10/10). Thử mã khác.")
    elif state == "conn": label(im, (L + 104, y0 + 392), "Đang kết nối tới phòng 4X7K2P...", 20); put(im, "kit_progress_track", (L, y0 + 420), (680, 28)); put(im, "kit_progress_fill", (L + 6, y0 + 426), (300, 16))
    else: label(im, (L + 104, y0 + 392), "Nhập mã do chủ phòng gửi. Không phân biệt chữ hoa.", 20)
    label(im, (L, y0 + 470), "Vào bằng tài khoản huycv", 18)
    by = y0 + 600 - 24 - 64 - 18
    button(im, "secondary", (x0 + 800 - 24 - 40 - 520 - 24, by), "Hủy"); button(im, "primary", (x0 + 800 - 24 - 40 - 240, by), "Vào phòng", None, WHITE, (240, 64), "disabled" if state == "conn" else "normal")
    return im

def login(error):
    im = bg(); logo(im, W / 2, 70, 700)
    x0, y0 = (W - 700) // 2, 220; put(im, "kit_panel_m", (x0, y0), (700, 640)); text(im, (x0 + 60, y0 + 70), "Đăng nhập", 36, GOLD, "lm")
    put(im, "kit_tab_on", (x0 + 60, y0 + 110), (280, 56)); text(im, (x0 + 200, y0 + 138), "Đăng nhập", 22, TXT, "mm"); put(im, "kit_tab_off", (x0 + 360, y0 + 110), (280, 56)); text(im, (x0 + 500, y0 + 138), "Đăng ký", 22, MUTED, "mm")
    label(im, (x0 + 60, y0 + 210), "Tên đăng nhập", 20); input_(im, (x0 + 60, y0 + 232), 580, "huycv", "error" if error else "normal")
    label(im, (x0 + 60, y0 + 316), "Mật khẩu", 20); input_(im, (x0 + 60, y0 + 338), 580, "••••••••" if error else "Mật khẩu", "error" if error else "normal", ph=not error)
    if error: err(im, (x0 + 60, y0 + 424), "Sai tên đăng nhập hoặc mật khẩu.")
    button(im, "primary", (x0 + 60, y0 + 470), "Đăng nhập", None, WHITE, (580, 64)); button(im, "secondary", (x0 + 60, y0 + 550), "Máy chủ (nâng cao)", None, WHITE, (580, 56), font=22)
    return im

ROOMS = [("Phòng mặc định", "Mặc định", "6/10", None), ("Leo rank tối nay", "Của bạn", "3/10", "4X7K2P"), ("Luyện tập", "Của bạn", "0/10", "9HM3TQ")]
def rooms(state):
    im = home(True); im.alpha_composite(Image.new("RGBA", (W, H), (10, 10, 8, 205)))
    w, h = 1280, 760; x0, y0 = (W - w) // 2, (H - h) // 2; put(im, "kit_panel_l", (x0, y0), (w, h)); text(im, (x0 + 60, y0 + 70), "Phòng của tôi", 36, GOLD, "lm"); text(im, (x0 + w - 60, y0 + 70), "2 phòng bổ sung tối đa", 20, MUTED, "rm")
    hy = y0 + 120
    if state == "ok":
        for i, (nm, tg, cap, code) in enumerate(ROOMS):
            ry = hy + i * 70; put(im, "kit_row_selected" if i == 1 else ("kit_row_alt" if i % 2 else "kit_row_normal"), (x0 + 60, ry), (w - 120, 62)); text(im, (x0 + 84, ry + 31), nm, 26, TXT, "lm")
            put(im, "kit_tag_neutral", (x0 + 520, ry + 12), (140, 38)); text(im, (x0 + 590, ry + 31), tg, 18, WHITE, "mm"); text(im, (x0 + 700, ry + 31), cap, 24, TXT, "lm")
            if code:
                text(im, (x0 + 790, ry + 31), code, 24, GOLD, "lm"); button(im, "secondary", (x0 + 920, ry + 9), "Sao chép", None, WHITE, (140, 44), font=18); button(im, "primary", (x0 + 1070, ry + 9), "Vào", None, WHITE, (90, 44), font=20)
                put(im, "kit_iconbtn_square_normal", (x0 + 1170 - 44 + 40, ry + 9), (44, 44)); icon(im, "kit_icon_cross", x0 + 1170 - 44 + 40 + 22, ry + 31, 20)
            else: label(im, (x0 + 790, ry + 31), "mã do chủ phòng gửi", 18)
    elif state == "empty":
        put(im, "kit_row_normal", (x0 + 60, hy), (w - 120, 62)); text(im, (x0 + 84, hy + 31), "Phòng mặc định", 26, TXT, "lm"); text(im, (x0 + 700, hy + 31), "6/10", 24, TXT, "lm")
        text(im, (x0 + w / 2, hy + 220), "Bạn chưa có phòng bổ sung.", 26, TXT, "mm"); text(im, (x0 + w / 2, hy + 262), "Bấm Tạo phòng để tạo phòng riêng và gửi mã cho bạn bè.", 20, MUTED, "mm")
    else:
        put(im, "kit_toast", (x0 + 60, hy + 40), (w - 120, 64)); icon(im, "kit_icon_cross", x0 + 96, hy + 72, 26); text(im, (x0 + 130, hy + 72), "Không tải được danh sách phòng. Kiểm tra máy chủ rồi thử lại.", 22, TXT, "lm"); button(im, "secondary", (x0 + w - 60 - 200, hy + 130), "Thử lại", None, WHITE, (200, 56), font=22)
    by = y0 + h - 24 - 64 - 24; button(im, "secondary", (x0 + 60, by), "Đóng"); button(im, "primary", (x0 + w - 60 - 240, by), "Tạo phòng")
    return im

def rooms_delete():
    im = rooms("ok"); im.alpha_composite(Image.new("RGBA", (W, H), (10, 10, 8, 190)))
    w, h = 720, 340; x0, y0 = (W - w) // 2, (H - h) // 2; put(im, "kit_panel_popup", (x0, y0), (w, h)); text(im, (x0 + w / 2, y0 + 24 + TB / 2), "Xóa phòng?", 34, GOLD, "mm")
    text(im, (x0 + w / 2, y0 + 24 + TB + 50), "Xóa phòng Luyện tập (9HM3TQ)? Không hoàn tác.", 24, TXT, "mm"); text(im, (x0 + w / 2, y0 + 24 + TB + 86), "Chỉ xóa được phòng đang trống.", 20, MUTED, "mm")
    by = y0 + h - 24 - 64 - 16; button(im, "secondary", (x0 + 44, by), "Hủy"); button(im, "danger", (x0 + w - 44 - 240, by), "Xóa phòng", "kit_icon_cross")
    return im

def create(state):
    im = home(True); im.alpha_composite(Image.new("RGBA", (W, H), (10, 10, 8, 205))); x0, y0 = popup(im, 820, 600, "Tạo phòng"); L = x0 + 60
    label(im, (L, y0 + 150), "Tên phòng (1 đến 48 ký tự)", 20); input_(im, (L, y0 + 172), 700, "Leo rank tối nay")
    label(im, (L, y0 + 264), "Sức chứa", 20); text(im, (L + 700, y0 + 264), "10 người", 26, GOLD, "rm")
    put(im, "kit_slider_track", (L, y0 + 304), (700, 16)); put(im, "kit_slider_fill", (L, y0 + 307), (700, 10)); put(im, "kit_knob", (L + 700 - 16, y0 + 296))
    for i, t in enumerate(("1", "5", "10")): label(im, (L + [0, 350, 700][i] - (0 if i == 0 else (10 if i == 1 else 20)), y0 + 342), t, 18)
    label(im, (L, y0 + 392), "Cần tối thiểu 6 người để bắt đầu trận.", 20); label(im, (L, y0 + 424), "Còn 1/2 phòng bổ sung. Phòng trống sau 10 phút sẽ bị thu hồi.", 20)
    if state == "err": err(im, (L, y0 + 464), "Đã đủ 2 phòng bổ sung. Xóa một phòng rồi thử lại.")
    by = y0 + 600 - 24 - 64 - 18; button(im, "secondary", (x0 + 820 - 24 - 40 - 520 - 24, by), "Hủy"); button(im, "primary", (x0 + 820 - 24 - 40 - 240, by), "Tạo phòng", None, WHITE, (240, 64), "disabled" if state == "err" else "normal")
    return im

def lobby(countdown):
    im = bg(0.7); x0, y0, w, h = 120, 130, 1680, 820; put(im, "kit_panel_l", (x0, y0), (w, h)); text(im, (x0 + 60, y0 + 62), "Phòng chờ", 38, GOLD, "lm")
    put(im, "kit_toast", (x0 + 330, y0 + 30), (360, 64)); text(im, (x0 + 360, y0 + 62), "Mã", 18, MUTED, "lm"); text(im, (x0 + 410, y0 + 62), "4X7K2P", 28, GOLD, "lm"); button(im, "secondary", (x0 + 570, y0 + 38), "Sao chép", None, WHITE, (110, 48), font=16)
    n = 7 if countdown else 4; names = ["Huy (chủ phòng)", "V.Huy", "Hào", "Vinh", "Vũ", "Quang Huy", "Khoa", "Minh", "Long", "Nam"]
    for i in range(10):
        col, row = divmod(i, 5); rx = x0 + 60 + col * 500; ry = y0 + 130 + row * 76; filled = i < n
        put(im, "kit_row_alt" if filled else "kit_row_normal", (rx, ry), (470, 66)); avatar(im, rx + 40, ry + 33, not filled)
        text(im, (rx + 84, ry + 33), names[i] if filled else "Chỗ trống", 24, TXT if filled else MUTED, "lm")
        if i == 0: put(im, "kit_tag_neutral", (rx + 330, ry + 14), (126, 38)); text(im, (rx + 393, ry + 33), "Chủ phòng", 17, WHITE, "mm")
    px = x0 + 1100; put(im, "kit_panel_s", (px, y0 + 130), (480, 380)); label(im, (px + 50, y0 + 190), "Bắt đầu sau", 22)
    if countdown: text(im, (px + 50, y0 + 275), "12", 110, GOLD, "lm"); label(im, (px + 190, y0 + 285), "giây", 26, TXT)
    else: text(im, (px + 50, y0 + 275), "Chờ", 80, TXT, "lm"); label(im, (px + 50, y0 + 345), "Chưa đủ người, chưa đếm ngược", 20)
    label(im, (px + 50, y0 + 375 if countdown else y0 + 385), f"{n}/10 người trong phòng", 22, TXT)
    label(im, (px + 50, y0 + 410), "Tối thiểu 6 người để bắt đầu", 20); label(im, (px + 50, y0 + 445), "Bản đồ: Đảo rừng 50x50 ô", 20)
    label(im, (x0 + 60, y0 + 560), "Trận tự bắt đầu sau 15 giây khi đủ 6 người. Thiếu người thì hủy đếm ngược.", 20)
    by = y0 + h - 24 - 64 - 20; button(im, "secondary", (x0 + 60, by), "Rời phòng", "kit_icon_cross", DANGER)
    button(im, "secondary", (x0 + w - 60 - 420, by), "Sẵn sàng (tùy chọn)", None, MUTED, (420, 64), "normal", 24)
    note(im, "Minh họa: nút Sẵn sàng là luật chưa duyệt trong GDD (M2); mặc định tắt bằng cờ lobbyReadyEnabled.")
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
        put(im, "kit_panel_s", (680, 330), (560, 280)); icon(im, "kit_icon_cross", 730, 380, 30); text(im, (770, 380), "Không vào được trận", 28, TXT, "lm")
        text(im, (730, 440), "Dữ liệu bản đồ không khớp với máy chủ.", 22, TXT, "lm"); text(im, (730, 475), "Lý do: phiên bản dữ liệu khác nhau (ví dụ).", 20, MUTED, "lm")
        button(im, "primary", (730, 520), "Về trang chủ", None, WHITE, (460, 56), font=24)
    # huong dan dieu khien
    put(im, "kit_panel_s", (480, 660), (960, 190)); label(im, (520, 706), "Điều khiển", 20, GOLD)
    for i, (k, d) in enumerate([("WASD", "Di chuyển"), ("Chuột", "Ngắm, bắn"), ("F", "Nhặt"), ("M", "Bản đồ"), ("ESC", "Cài đặt")]):
        cx = 540 + i * 180; put(im, "kit_iconbtn_square_normal", (cx - 40, 735), (80, 44)); text(im, (cx, 757), k, 20, TXT, "mm"); text(im, (cx, 806), d, 18, MUTED, "mm")
    put(im, "kit_toast", (460, 900), (1000, 64)); text(im, (490, 932), "MẸO", 18, GOLD, "lm"); text(im, (560, 932), "Bụi rậm che bạn khỏi tầm nhìn, nhưng bắn sẽ lộ vị trí ngay lập tức.", 22, TXT, "lm")
    return im

ALL = {"a1-khoi-dong": splash, "a2-sanh-chinh": lambda: home(True), "a3-vao-phong": lambda: join("normal"), "a3-vao-phong-loi": lambda: join("err"), "a3-vao-phong-dang-ket-noi": lambda: join("conn"),
       "a3b-dang-nhap": lambda: login(False), "a3b-dang-nhap-loi": lambda: login(True), "a4-phong-cua-toi": lambda: rooms("ok"), "a4-phong-cua-toi-trong": lambda: rooms("empty"), "a4-phong-cua-toi-loi": lambda: rooms("err"), "a4-xoa-phong": rooms_delete,
       "a5-tao-phong": lambda: create("ok"), "a5-tao-phong-loi": lambda: create("err"), "a6-phong-cho-cho-du-nguoi": lambda: lobby(False), "a6-phong-cho-dem-nguoc": lambda: lobby(True), "a7-dang-tai": lambda: loading(False), "a7-dang-tai-loi": lambda: loading(True)}
if __name__ == "__main__":
    only = sys.argv[1:]
    for n, f in ALL.items():
        if only and n not in only: continue
        save(f(), f"a/{n}.png"); print(n)
