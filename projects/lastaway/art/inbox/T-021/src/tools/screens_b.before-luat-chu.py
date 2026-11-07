"""T-021 dot B (dung lai tren kit K2 + P0 + P1, 07/11): trong va sau tran. PYTHONIOENCODING=utf-8 python screens_b.py [ten...]
Ghi b/<ten>.png, b/contrast.md/.json; cong can giua: check_center.py (chay ca b)."""
import sys, pathlib, json
sys.path.insert(0, str(pathlib.Path(__file__).parent))
import mock
from mock import *
from screens_a import popup
mock.ICONS.update({"Mặc định": "refresh", "Tiếp tục": "play", "Ở lại": "arrow_left", "Theo dõi người khác": "eye", "Về sảnh": "home", "Chơi lại": "refresh", "Thử lại ngay": "refresh", "Đóng": "@cross_ink"})
ASSETS = OUT / "b" / "assets"; HUDA = Image.open(T15 / "dung-thu-hud-A.png").convert("RGBA")
def fx(n): return Image.open(ASSETS / f"{n}.png").convert("RGBA")
def put_fx(im, n, cx, cy, h=None, w=None):
    s = fx(n)
    if h: s = s.resize((round(s.width * h / s.height), h), Image.NEAREST)
    elif w: s = s.resize((w, round(s.height * w / s.width)), Image.NEAREST)
    im.alpha_composite(s, (round(cx - s.width / 2), round(cy - s.height / 2)))
def dim(im, a): im.alpha_composite(Image.new("RGBA", (W, H), (10, 10, 8, a)))
def hudbg(a=0):
    im = HUDA.copy()
    if a: dim(im, a)
    return im
def slider(im, x, y, v, w=320):
    put(im, "kit_slider_track", (x, y + 8), (w, 16)); put(im, "kit_slider_fill", (x, y + 11), (max(12, int(w * v)), 10)); put(im, "kit_knob", (x + int(w * v) - 16, y))
def toggle(im, x, y, on): put(im, "kit_toggle_on" if on else "kit_toggle_off", (x, y))
def tag(im, cx, cy, s, size=20, w=110): put(im, "kit_tag_neutral", (cx - w // 2, cy - 18), (w, 36)); text(im, (cx, cy), s, size, TXT, "mm")
def stat(im, x, y, label_, val, w=300, h=130):
    put(im, "kit_card_stat", (x, y), (w, h)); text(im, (x + w / 2, y + 38), label_, 20, MUTED, "mm"); text(im, (x + w / 2, y + 84), val, 44, TXT, "mm")
def hr(im, x, y, w): put(im, "kit_divider", (x, y), (w, 14))
def btn(im, kind, x, y, w, h, cap, font=26, state="normal"): button(im, kind, (x, y), cap, None, WHITE, (w, h), state, font)

def esc(state="main"):
    im = hudbg(150); w, h = 820, 800; x0, y0 = popup(im, w, h, "Cài đặt", -10); L = x0 + 70; R = x0 + w - 70; y = y0 + 150
    for nm, v, s in (("Âm lượng chung", 0.8, "80%"), ("Hiệu ứng (SFX)", 0.7, "70%"), ("Nhạc nền (Music)", 0.5, "50%")):
        ico(im, "sound" if "chung" in nm else ("sound" if "SFX" in nm else "music"), L + 14, y + 16, 24); text(im, (L + 44, y + 16), nm, 24, TXT, "lm"); slider(im, L + 300, y, v, 270); text(im, (R, y + 16), s, 22, MUTED, "rm"); y += 64
    hr(im, L, y - 8, R - L); y += 14
    text(im, (L, y + 16), "Bật nhạc nền", 24, TXT, "lm"); toggle(im, R - 114, y - 8, False); label(im, (L + 300, y + 16), "Nhạc nền đang tắt", 18); y += 70
    text(im, (L, y + 16), "Độ nhạy chuột", 24, TXT, "lm"); slider(im, L + 300, y, 0.33, 270); text(im, (R, y + 16), "1,0", 22, MUTED, "rm"); y += 64
    ico(im, "vibrate", L + 14, y + 16, 24); text(im, (L + 44, y + 16), "Rung màn hình", 24, TXT, "lm"); toggle(im, R - 114, y - 8, True); y += 60
    label(im, (L, y + 16), "Trận vẫn đang chạy: nhân vật đứng yên và vẫn có thể bị tấn công.", 18)
    by = footer(im, x0, y0, w, h); btn(im, "secondary", x0 + 60, by, 200, 64, "Mặc định", 24); btn(im, "secondary", x0 + 280, by, 240, 64, "Thoát trận", 24); btn(im, "primary", x0 + w - 60 - 220, by, 220, 64, "Tiếp tục", 24)
    if state == "confirm":
        dim(im, 170); x1, y1 = popup(im, 700, 420, "Thoát trận?", 0); text(im, (W / 2, y1 + 165), "Bạn sẽ bị loại khỏi trận.", 26, TXT, "mm"); text(im, (W / 2, y1 + 205), "Trận vẫn tiếp tục với người còn lại.", 20, MUTED, "mm")
        by = footer(im, x1, y1, 700, 420); btn(im, "secondary", x1 + 60, by, 270, 64, "Thoát trận"); btn(im, "primary", x1 + 700 - 60 - 270, by, 270, 64, "Ở lại")
    return im

def eliminated(state="killed"):
    im = hudbg(0); dim(im, 215); put(im, "kit_panel_m", (W // 2 - 460, 110), (920, 860)); cx = W // 2
    icon(im, "kit_icon_cross_red", cx - 90, 190, 30); text(im, (cx - 62, 190), "BỊ LOẠI", 34, TXT, "lm")
    rw = tw("#4", 130) + 18 + tw("/ 10", 44); rx = cx - rw / 2; text(im, (rx, 300), "#4", 130, TXT, "lm"); text(im, (rx + tw("#4", 130) + 18, 320), "/ 10", 44, MUTED, "lm")
    put(im, "kit_card_cause", (cx - 400, 390), (800, 150)); ico(im, "skull", cx - 400 + 88, 465, 40)
    if state in ("killed", "last"): text(im, (cx - 235, 440), "Bị Hào hạ", 30, TXT, "lm"); text(im, (cx - 235, 490), "Nỏ · cách 6 ô", 22, MUTED, "lm")
    elif state == "zone": text(im, (cx - 235, 465), "Bị bo thu hạ", 30, TXT, "lm")
    stats = [("Hạ gục", "2"), ("Sống sót", "04:38")] if state != "zone" else [("Sống sót", "04:38")]
    tot = 300 * len(stats) + 20 * (len(stats) - 1); sx = cx - tot // 2
    for i, (k, v) in enumerate(stats): stat(im, sx + i * 320, 570, k, v)
    if state == "zone": label(im, (cx - 400, 725), "Dòng Hạ gục ẩn vì máy chủ chưa gửi số hạ gục (không hiện 0 hay trống).", 18)
    by = 800; btn(im, "secondary", cx - 400, by, 360, 72, "Về sảnh"); btn(im, "primary", cx + 40, by, 360, 72, "Theo dõi người khác", 24, "disabled" if state == "last" else "normal")
    if state == "last": label(im, (cx + 40, by + 100), "Không còn ai để theo dõi", 18)
    return im

ALIVE = ["Tuan", "Hào", "Vinh", "Vũ", "Quang Huy", "Khoa", "Khánh", "Minh", "Long"]
def spectate(state="main"):
    im = bg(0.45); put(im, "kit_card_room_selected", (W // 2 - 310, 20), (620, 150)); text(im, (W // 2, 78), "Đang theo dõi: Tuan", 32, GOLD, "mm"); text(im, (W // 2, 126), "Còn sống 9/10 · Hạng của bạn #4", 20, TXT, "mm")
    rows = ALIVE if state != "last" else ALIVE[:1]
    for i, nm in enumerate(rows):
        y = 190 + i * 56; put(im, "kit_row_selected" if i == 0 else ("kit_row_alt" if i % 2 else "kit_row_normal"), (30, y), (300, 48)); text(im, (50, y + 24), nm, 22, GOLD if i == 0 else TXT, "lm")
    label(im, (30, 190 + len(rows) * 56 + 8), "Chỉ hiện người còn sống, không gồm bạn", 16)
    put(im, "kit_card_stat", (30, 750), (460, 200)); text(im, (84, 810), "Tuan", 28, TXT, "lm"); put(im, "kit_progress_track", (84, 838), (350, 24)); put(im, "kit_progress_fill", (90, 842), (290, 16)); text(im, (84, 892), "HP 86/100 · Nỏ", 20, MUTED, "lm")
    x = 540; keycap(im, (x, 990), "A"); keycap(im, (x + 62, 990), "D"); text(im, (x + 134, 1016), "Đổi người", 20, TXT, "lm"); keycap(im, (x + 280, 990), "E"); text(im, (x + 342, 1016), "Kẻ đã hạ bạn", 20, TXT, "lm")
    btn(im, "secondary", 1620, 970, 280, 72, "Về sảnh")
    if state == "switch": put(im, "kit_toast", (W // 2 - 270, 190), (540, 70)); ico(im, "eye", W // 2 - 230, 225, 22); text(im, (W // 2 - 190, 225), "Vinh đã bị loại, chuyển sang Tuan", 22, TXT, "lm")
    if state == "last": put(im, "kit_toast", (W // 2 - 300, 500), (600, 90)); text(im, (W // 2, 545), "Trận kết thúc, đang chuyển sang kết quả...", 26, TXT, "mm")
    return im

def inventory(state="main"):
    im = hudbg(0); dim(im, 200); put(im, "kit_panel_l", (130, 70), (1660, 860))
    text(im, (210, 150), "Túi đồ", 38, GOLD, "lm"); keycap(im, (1590, 124), "Tab", w=70); text(im, (1675, 150), "đóng", 20, TXT, "lm"); hr(im, 190, 188, 1460)
    top, ph = 215, 590
    put(im, "kit_panel_s", (180, top), (440, ph)); text(im, (232, top + 70), "Dưới đất", 26, TXT, "lm"); label(im, (410, top + 70), "bán kính 2 ô", 18)
    floor = [] if state == "empty" else [("Shotgun", "F"), ("Lá thần", "×2"), ("Máu kỳ lân", "×1"), ("Nỏ cao su", "")]
    for i, (n, s) in enumerate(floor):
        y = top + 110 + i * 58; put(im, "kit_row_selected" if i == 0 else "kit_row_normal", (215, y), (370, 50)); text(im, (237, y + 25), n, 22, TXT, "lm")
        if s == "F": keycap(im, (535, y + 3), "F", True, 44)
        else: text(im, (570, y + 25), s, 22, MUTED, "rm")
    if state == "empty": text(im, (400, top + 300), "Không có đồ gần bạn", 24, MUTED, "mm")
    label(im, (232, top + ph - 52), "F hoặc chuột phải: nhặt nhanh", 18)
    put(im, "kit_panel_s", (650, top), (680, ph)); text(im, (705, top + 70), "Vũ khí", 26, TXT, "lm")
    for i, n in enumerate(["Tay không", "Nỏ", "Nỏ cao su", ""]):
        x = 705 + i * 150; put(im, "kit_slot_item_selected" if i == 1 else ("kit_slot_item_normal" if n else "kit_slot_item_empty"), (x, top + 100), (130, 140)); text(im, (x + 65, top + 205), n or "Trống", 18, TXT if n else MUTED, "mm"); text(im, (x + 16, top + 122), str(i + 1), 18, MUTED if n else MUTED, "lm")
    text(im, (705, top + 290), "Dược phẩm", 26, TXT, "lm")
    for i, (n, c) in enumerate([("Lá thần", "×6"), ("Máu kỳ lân", "×1"), ("", ""), ("Tăng lực", "×4")]):
        x = 705 + i * 150; put(im, "kit_slot_item_normal" if n else "kit_slot_item_empty", (x, top + 320), (130, 140)); text(im, (x + 65, top + 425), n or "Trống", 18, TXT if n else MUTED, "mm"); text(im, (x + 16, top + 342), str(i + 5), 18, MUTED, "lm")
        if c: text(im, (x + 118, top + 342), c, 20, TXT, "rm")
    label(im, (705, top + ph - 52), "Kéo vật ra ngoài để vứt (đề xuất, chưa có trong GDD)", 18)
    put(im, "kit_panel_s", (1360, top), (390, ph)); text(im, (1410, top + 70), "Trang bị", 26, TXT, "lm")
    for i, (k, v, ic) in enumerate([("Mũ", "Mũ gỗ · 18", "person"), ("Giáp", "Giáp gai · 48", "bag"), ("Adrenaline", "55 giây", "clock"), ("Đạn", "Nỏ 4/90", "gear")]):
        y = top + 105 + i * 98; put(im, "kit_row_normal", (1395, y), (320, 88)); label(im, (1417, y + 26), k, 18); text(im, (1417, y + 60), v, 24, TXT, "lm")
    label(im, (1410, top + ph - 52), "Giáp, mũ, đạn tự nhặt (R-054)", 18)
    if state == "main":
        put(im, "kit_toast", (W // 2 - 340, 950), (680, 70)); ico(im, "wifi", W // 2 - 300, 985, 24); text(im, (W // 2 - 262, 985), "Có tiếng bước chân phía đông bắc · 18 ô", 22, TXT, "lm")
        # the chi tiet vat pham (ben phai Duoi dat, hang Shotgun dang chon)
        put(im, "kit_tip_item", (200, 560), (390, 190)); text(im, (255, 598), "Shotgun", 26, GOLD, "lm"); text(im, (255, 634), "Vũ khí · Khai hỏa, tiêu thụ đạn", 18, TXT, "lm"); hr(im, 255, 650, 290)
        text(im, (255, 680), "Nguồn: tiếp tế, rương gỗ, xác người chơi", 16, MUTED, "lm"); text(im, (255, 712), "Số liệu chi tiết lấy từ sheet Weapons", 16, MUTED, "lm")
    return im

def bigmap(state="main"):
    im = hudbg(0); dim(im, 215); put(im, "kit_panel_l", (50, 40), (1090, 1000)); mx, my, ms = 110, 100, 840
    d = ImageDraw.Draw(im); d.rectangle([mx, my, mx + ms, my + ms], fill=(31, 29, 24, 255), outline=(90, 86, 70, 255), width=2)
    for i in range(1, 10):
        d.line([(mx + i * ms / 10, my), (mx + i * ms / 10, my + ms)], fill=(60, 57, 46), width=1); d.line([(mx, my + i * ms / 10), (mx + ms, my + i * ms / 10)], fill=(60, 57, 46), width=1)
    cx_, cy_ = mx + 420, my + 430; d.ellipse([cx_ - 340, cy_ - 340, cx_ + 340, cy_ + 340], outline=(219, 166, 69), width=4); d.ellipse([cx_ - 70, cy_ - 140, cx_ + 110, cy_ + 40], outline=(237, 242, 238), width=4)
    put_fx(im, "fx_map_player", cx_ - 180, cy_ + 230, 66); put_fx(im, "fx_map_mark", cx_ + 40, cy_ - 60, 70); put_fx(im, "fx_map_drop", cx_ + 250, cy_ - 220, 90)
    label(im, (mx + 10, my + ms + 28), "Nền bản đồ và vòng bo vẽ lúc chạy (giữ chỗ). Lưới chữ-số của wireframe bỏ vì GDD không có tọa độ ô.", 18)
    put(im, "kit_panel_s", (1170, 40), (700, 170)); text(im, (1215, 112), "Pha 3 · Bo thu sau 01:24", 34, GOLD, "lm"); label(im, (1215, 158), "Bạn cách bo tiếp theo 420 m", 22, TXT)
    put(im, "kit_panel_s", (1170, 230), (700, 400)); text(im, (1215, 300), "Chú thích", 28, TXT, "lm")
    for i, (ic, kind, s) in enumerate([("kit_icon_warn", None, "Bo hiện tại (nét vàng cam)"), (None, "w", "Bo tiếp theo, vùng an toàn (nét trắng)"), ("fx_map_drop", "fx", "Thính rơi"), ("fx_map_mark", "fx", "Điểm đánh dấu của bạn"), ("fx_map_player", "fx", "Vị trí của bạn")]):
        y = 358 + i * 52
        if kind == "fx": put_fx(im, ic, 1230, y, 40)
        elif ic: icon(im, ic, 1230, y, 30)
        else: ImageDraw.Draw(im).ellipse([1216, y - 14, 1244, y + 14], outline=(237, 242, 238), width=3)
        text(im, (1275, y), s, 22, TXT, "lm")
    put(im, "kit_panel_s", (1170, 650), (700, 370)); text(im, (1215, 712), "Điều khiển", 28, TXT, "lm")
    for i, (k, s) in enumerate([("Chuột phải", "Đặt hoặc xóa điểm đánh dấu"), ("Cuộn", "Phóng to, thu nhỏ"), ("Giữ chuột trái", "Kéo bản đồ"), ("M", "Đóng bản đồ")]):
        y = 770 + i * 56; wk = max(52, int(tw(k, 20)) + 30); keycap(im, (1215, y - 26), k, w=wk); text(im, (1215 + wk + 20, y), s, 22, TXT, "lm")
    return im

RANK = [("Huy", 3), ("Hào", 4), ("Vinh", 1), ("V.Huy", 2), ("Vũ", 0), ("Quang Huy", 1), ("Khoa", 0), ("Khánh", 0), ("Minh", 0), ("Long", 0)]
def result(win=True):
    im = bg(0.62); me = 0 if win else 3; put(im, "kit_panel_m", (1000, 120), (820, 820)); text(im, (1050, 190), "Bảng xếp hạng", 32, GOLD, "lm"); ico(im, "trophy", 1760, 190, 30)
    for i, (nm, k) in enumerate(RANK):
        y = 230 + i * 68; put(im, "kit_row_selected" if i == me else ("kit_row_alt" if i % 2 else "kit_row_normal"), (1050, y), (720, 60)); text(im, (1075, y + 30), f"#{i + 1}", 24, GOLD if i == me else MUTED, "lm"); text(im, (1160, y + 30), nm + ("  (bạn)" if i == me else ""), 24, TXT, "lm"); text(im, (1745, y + 30), f"{k} hạ", 22, MUTED, "rm")
    put_fx(im, "fx_emblem_win" if win else "fx_emblem_lose", 480, 250, 230)
    if win: text(im, (480, 450), "CHIẾN THẮNG", 78, GOLD, "mm"); text(im, (480, 520), "Hạng #1 / 10", 30, TXT, "mm")
    else:
        text(im, (480, 450), "HẠNG #4", 78, TXT, "mm"); put(im, "kit_card_cause", (120, 490), (720, 110)); text(im, (240, 545), "Bị Hào hạ · Nỏ · cách 6 ô", 26, TXT, "lm"); ico(im, "skull", 190, 545, 36)
    sy = 560 if win else 640
    for i, (k, v) in enumerate([("Hạ gục", "3" if win else "2"), ("Sống sót", "07:00" if win else "04:38")]): stat(im, 120 + i * 350, sy, k, v, 330, 130)
    btn(im, "secondary", 120, 830, 330, 72, "Về sảnh"); btn(im, "primary", 480, 830, 330, 72, "Chơi lại")
    label(im, (120, 925), "Chơi lại dẫn về Phòng chờ của phòng cũ. Đồng hạng hiện đúng số hạng máy chủ gửi.", 18)
    return im

def disconnect(state="retry"):
    im = hudbg(120); dim(im, 120); w, h = 760, 480; x0, y0 = (W - w) // 2, (H - h) // 2; put(im, "kit_panel_m", (x0, y0), (w, h)); cx = W // 2
    put_fx(im, "fx_icon_disconnect", cx, y0 + 90, 64)
    if state == "retry":
        text(im, (cx, y0 + 170), "Đang kết nối lại...", 36, TXT, "mm"); text(im, (cx, y0 + 218), "Mất tín hiệu tới máy chủ. Nhân vật vẫn ở trong trận.", 22, MUTED, "mm")
        put(im, "kit_progress_track", (x0 + 70, y0 + 262), (w - 140, 28)); put(im, "kit_progress_fill", (x0 + 76, y0 + 268), (int((w - 152) * 0.7), 16)); label(im, (x0 + 70, y0 + 318), "Rời trận sau", 20); text(im, (x0 + w - 70, y0 + 318), "11 s", 24, GOLD, "rm")
        btn(im, "secondary", x0 + 70, y0 + h - 110, 300, 64, "Về sảnh"); btn(im, "primary", x0 + w - 70 - 300, y0 + h - 110, 300, 64, "Thử lại ngay")
    elif state == "failed":
        text(im, (cx, y0 + 170), "Chưa kết nối lại được", 36, TXT, "mm"); err(im, (cx - 230, y0 + 230), "Thử lại thất bại. Còn 6 s trước khi bị loại.", 22)
        put(im, "kit_progress_track", (x0 + 70, y0 + 280), (w - 140, 28)); put(im, "kit_progress_fill", (x0 + 76, y0 + 286), (int((w - 152) * 0.4), 16))
        btn(im, "secondary", x0 + 70, y0 + h - 110, 300, 64, "Về sảnh"); btn(im, "primary", x0 + w - 70 - 300, y0 + h - 110, 300, 64, "Thử lại")
    else:
        text(im, (cx, y0 + 170), "Đã mất kết nối", 36, TXT, "mm"); text(im, (cx, y0 + 222), "Quá 15 giây không có tín hiệu, bạn đã bị loại khỏi trận.", 22, MUTED, "mm"); text(im, (cx, y0 + 262), "Hạng #4 / 10", 30, TXT, "mm")
        btn(im, "primary", cx - 160, y0 + h - 110, 320, 64, "Về sảnh")
    return im

def feedback(low=False):
    im = hudbg(0)
    if low: im.alpha_composite(fx("fx_vignette").resize((W, H), Image.NEAREST))
    put_fx(im, "fx_hitmarker", 1230, 432, 96); put_fx(im, "fx_damage_arc", 1455, 495, 200); put_fx(im, "fx_danger_arrow", 480, 590, 90); tag(im, 590, 590, "8s", 24, 90)
    put(im, "kit_toast", (735, 36), (452, 60)); icon(im, "kit_icon_warn", 770, 66, 26); text(im, (800, 66), "Bom nổ sau 8s", 22, TXT, "lm"); text(im, (1015, 66), "Tây Nam, 4 ô", 20, MUTED, "lm")
    for i, (n, t) in enumerate((("poison", "3s"), ("slow", "5s"), ("stealth", "9s"))): put_fx(im, f"fx_chip_{n}", 90 + i * 124, 730, 116); text(im, (90 + i * 124, 772), t, 20, TXT, "mm")
    put(im, "kit_toast", (570, 858), (780, 48)); text(im, (600, 882), "Đang dùng Lá thần", 22, TXT, "lm"); put(im, "kit_progress_track", (840, 870), (260, 24)); put(im, "kit_progress_fill", (846, 874), (150, 16)); text(im, (1120, 882), "1,2 s", 22, GOLD, "lm"); label(im, (1190, 882), "Di chuyển để hủy", 18, TXT)
    put(im, "kit_toast", (1020, 610), (330, 72)); keycap(im, (1040, 620), "F", True, 52); text(im, (1112, 646), "Nhặt Shotgun", 24, TXT, "lm")
    for i, (n, c) in enumerate((("Giáp gai", "×1"), ("Lá thần", "×2"), ("Shotgun", "×1"))):
        y = 800 + i * 56; put(im, "kit_row_alt", (1672, y), (210, 48)); text(im, (1690, y + 24), "+", 24, TXT, "lm"); text(im, (1720, y + 24), n, 20, TXT, "lm"); text(im, (1866, y + 24), c, 20, MUTED, "rm")
    tag(im, 1777, 780, "+2 món khác", 16, 140); return im

ALL = {"b1-esc-cai-dat": lambda: esc(), "b1-esc-xac-nhan-thoat": lambda: esc("confirm"), "b2-bi-loai": lambda: eliminated("killed"), "b2-bi-loai-bo": lambda: eliminated("zone"), "b2-bi-loai-het-nguoi": lambda: eliminated("last"),
       "b3-spectator": lambda: spectate(), "b3-spectator-chuyen": lambda: spectate("switch"), "b3-spectator-het": lambda: spectate("last"), "b4-tui-do": lambda: inventory(), "b4-tui-do-trong": lambda: inventory("empty"),
       "b5-ban-do-lon": lambda: bigmap(), "b6-thang": lambda: result(True), "b7-thua": lambda: result(False), "b8-mat-ket-noi": lambda: disconnect("retry"), "b8-mat-ket-noi-that-bai": lambda: disconnect("failed"), "b8-mat-ket-noi-het-han": lambda: disconnect("expired"),
       "b9-phan-hoi-hud": lambda: feedback(), "b9-phan-hoi-mau-thap": lambda: feedback(True)}
if __name__ == "__main__":
    only = sys.argv[1:]; (OUT / "b").mkdir(exist_ok=True)
    for n, f in ALL.items():
        if only and n not in only: continue
        mock.CUR[0] = n; save(f(), f"b/{n}.png"); print(n)
    rows = mock.LOG; (OUT / "b" / "contrast.json").write_text(json.dumps(rows, ensure_ascii=False, indent=0), encoding="utf-8")
    bad = [r for r in rows if r["ratio"] < 4.5]; md = ["# T-021 đợt B: tương phản chữ (đo tự động từ pixel nền ngay trước khi vẽ chữ)", "", f"Tổng {len(rows)} dòng chữ. Dưới 4,5:1: {len(bad)}. Thấp nhất theo p10 và p90 độ sáng nền trong khung chữ; chưa trừ viền chữ 2 px.", "", "| Mock | Chữ | Cỡ | Màu | Tỉ số |", "|---|---|---:|---|---:|"]
    for r in sorted(bad, key=lambda r: r["ratio"]): md.append(f"| {r['mock']} | {r['text'][:50]} | {r['size']} | {r['fill']} | {r['ratio']} |")
    (OUT / "b" / "contrast.md").write_text("\n".join(md), encoding="utf-8"); print("tong", len(rows), "duoi 4.5:", len(bad))
