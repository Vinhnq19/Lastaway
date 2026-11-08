"""T-021 dot B (kit K2 + P0 + P1; luat chu gameplay Huy 07/11): trong va sau tran. PYTHONIOENCODING=utf-8 python screens_b.py [ten...]
Luat: man gameplay (B3, B4, B5, B9) chi giu chu cho ten nguoi choi, con so, phim tren keycap; con lai la icon.
Icon vat pham lay tu Assets/_JungleMaze/Art/UI/Icons (khong gen). Ten vat pham chi o tooltip.
Ghi b/<ten>.png, b/contrast.md/.json; cong can giua: check_center.py b."""
import sys, pathlib, json
sys.path.insert(0, str(pathlib.Path(__file__).parent))
import mock
from mock import *
from screens_a import popup
mock.ICONS.update({"Mặc định": "refresh", "Tiếp tục": "play", "Ở lại": "arrow_left", "Theo dõi": "eye", "Về sảnh": "home", "Chơi lại": "refresh", "Thử lại ngay": "refresh", "Đóng": "@cross_ink"})
ASSETS = OUT / "b" / "assets"; HUDA = Image.open(T15 / "dung-thu-hud-A.png").convert("RGBA")
ART = pathlib.Path(r"D:\Lastaway\Assets\_JungleMaze\Art")
# nguon icon co san (khong gen): ten trong mock -> file
ITEMS = {"Shotgun": "UI/Icons/Weapons/SHOTGUN", "Nỏ": "UI/Icons/Weapons/CROSSBOW", "Nỏ cao su": "UI/Icons/Weapons/RUBBER_GUN", "Tay không": "UI/Icons/Weapons/HAND",
         "Lá thần": "UI/Icons/Equipments/GOD_LEAF", "Máu kỳ lân": "UI/Icons/Equipments/UNICORN_BLOOD", "Tăng lực": "UI/Icons/Equipments/ELIXIR", "Mũ gỗ": "UI/Icons/Equipments/WOODEN_HELMET",
         "Giáp gai": "UI/Icons/Equipments/ARMOR", "Adrenaline": "UI/Icons/Equipments/ADRENALINE", "poison": "UI/Icons/Effects/Poison", "slow": "UI/Icons/Effects/Slow", "stealth": "UI/Icons/Effects/Invisible",
         "kill": "Hud/StatusIcons/icon_kill", "hp": "Hud/StatusIcons/icon_hp", "alive": "Hud/StatusIcons/icon_alive", "zone": "Hud/StatusIcons/icon_zone-timer", "mouse": "UI/MatchOverlay/IconMouse"}
def itm(im, key, cx, cy, s):
    if key is None:  # thieu icon: o giu cho co vien (bao danh sach thieu, khong gen thay ART-04)
        d = ImageDraw.Draw(im); d.rounded_rectangle([cx - s // 2, cy - s // 2, cx + s // 2, cy + s // 2], radius=6, outline=(214, 218, 214), width=3); return
    i = Image.open(ART / (ITEMS[key] + ".png")).convert("RGBA"); i = i.resize((s, round(i.height * s / i.width)), Image.LANCZOS); im.alpha_composite(i, (round(cx - i.width / 2), round(cy - i.height / 2)))
def pair(im, key, x, cy, s, val, size=26, col=TXT, anchor="l"):
    """icon + so, x la mep trai cua nhom; tra ve mep phai"""
    itm(im, key, x + s / 2, cy, s); text(im, (x + s + 10, cy), val, size, col, "lm"); return x + s + 10 + tw(val, size)
NEW = {"floor": 56, "slot": 104, "equip": 72, "pick": 72, "list": 56, "use": 52, "name": True}  # 1080p, px canh icon (Huy 07/11: icon to hon, nhat do co ten)
OLD = {"floor": 40, "slot": 76, "equip": 56, "pick": 50, "list": 40, "use": 36, "name": False}
SZ = dict(NEW)
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
def stat(im, x, y, key, val, w=300, h=130):
    """the thong ke: icon + so (khong nhan chu)"""
    put(im, "kit_card_stat", (x, y), (w, h)); gw = 56 + 14 + tw(val, 44); gx = x + w / 2 - gw / 2; itm(im, key, gx + 28, y + h / 2 - 2, 56); text(im, (gx + 70, y + h / 2 - 2), val, 44, TXT, "lm")
def hr(im, x, y, w): put(im, "kit_divider", (x, y), (w, 14))
def btn(im, kind, x, y, w, h, cap, font=26, state="normal"): button(im, kind, (x, y), cap, None, WHITE, (w, h), state, font)
def iconsq(im, name, x, y, s=64):  # nut vuong chi icon (man gameplay)
    put(im, "kit_iconbtn_square_normal", (x, y), (s, s)); k = round(s * 0.62); im.alpha_composite(spr(f"kit_ico_{name}").resize((k, k), Image.NEAREST), (x + (s - k) // 2, y + (s - k) // 2 - 3))
def cause(im, cx, y, killer, weapon, dist, w=800, h=150):
    """the nguyen nhan: so + icon, ten nguoi giet o giua (ten la chu duoc phep)"""
    put(im, "kit_card_cause", (cx - w // 2, y), (w, h)); cy = y + h // 2; ico(im, "skull", cx - w // 2 + 88, cy, 40)
    text(im, (cx - w // 2 + 160, cy), killer, 34, TXT, "lm"); x = cx - w // 2 + 160 + tw(killer, 34) + 40; itm(im, weapon, x + 34, cy, 68); text(im, (x + 84, cy), dist, 26, MUTED, "lm")

def esc(state="main"):
    im = hudbg(150); w, h = 820, 700; x0, y0 = popup(im, w, h, "Cài đặt", -10); L = x0 + 70; R = x0 + w - 70; y = y0 + 150
    for nm, v, s, ic in (("Âm lượng", 0.8, "80%", "sound"), ("Hiệu ứng", 0.7, "70%", "sound"), ("Nhạc nền", 0.5, "50%", "music")):
        ico(im, ic, L + 14, y + 16, 24); text(im, (L + 44, y + 16), nm, 24, TXT, "lm"); slider(im, L + 300, y, v, 270); text(im, (R, y + 16), s, 22, MUTED, "rm"); y += 64
    hr(im, L, y - 8, R - L); y += 14
    ico(im, "music", L + 14, y + 16, 24); text(im, (L + 44, y + 16), "Nhạc", 24, TXT, "lm"); toggle(im, R - 114, y - 8, False); y += 70
    text(im, (L + 44, y + 16), "Chuột", 24, TXT, "lm"); slider(im, L + 300, y, 0.33, 270); text(im, (R, y + 16), "1,0", 22, MUTED, "rm"); y += 64
    ico(im, "vibrate", L + 14, y + 16, 24); text(im, (L + 44, y + 16), "Rung", 24, TXT, "lm"); toggle(im, R - 114, y - 8, True)
    by = footer(im, x0, y0, w, h); btn(im, "secondary", x0 + 60, by, 200, 64, "Mặc định", 24); btn(im, "secondary", x0 + 280, by, 240, 64, "Thoát trận", 24); btn(im, "primary", x0 + w - 60 - 220, by, 220, 64, "Tiếp tục", 24)
    if state == "confirm":
        dim(im, 170); x1, y1 = popup(im, 700, 380, "Thoát trận?", 0); text(im, (W / 2, y1 + 175), "Bạn sẽ bị loại khỏi trận.", 26, TXT, "mm")
        by = footer(im, x1, y1, 700, 380); btn(im, "secondary", x1 + 60, by, 270, 64, "Thoát trận"); btn(im, "primary", x1 + 700 - 60 - 270, by, 270, 64, "Ở lại")
    return im

def eliminated(state="killed"):
    im = hudbg(0); dim(im, 215); put(im, "kit_panel_m", (W // 2 - 460, 110), (920, 860)); cx = W // 2
    icon(im, "kit_icon_cross_red", cx - 90, 190, 30); text(im, (cx - 62, 190), "BỊ LOẠI", 34, TXT, "lm")
    rw = tw("#4", 130) + 18 + tw("/ 10", 44); rx = cx - rw / 2; text(im, (rx, 300), "#4", 130, TXT, "lm"); text(im, (rx + tw("#4", 130) + 18, 320), "/ 10", 44, MUTED, "lm")
    if state in ("killed", "last"): cause(im, cx, 390, "Hào", "Nỏ", "6")
    else:
        put(im, "kit_card_cause", (cx - 400, 390), (800, 150)); itm(im, "zone", cx - 400 + 88, 465, 60); text(im, (cx - 400 + 160, 465), "Bo thu", 34, TXT, "lm")
    stats = [("kill", "2"), ("zone", "04:38")] if state != "zone" else [("zone", "04:38")]
    tot = 300 * len(stats) + 20 * (len(stats) - 1); sx = cx - tot // 2
    for i, (k, v) in enumerate(stats): stat(im, sx + i * 320, 570, k, v)
    by = 800; btn(im, "secondary", cx - 400, by, 360, 72, "Về sảnh"); btn(im, "primary", cx + 40, by, 360, 72, "Theo dõi", 26, "disabled" if state == "last" else "normal")
    return im

ALIVE = ["Tuan", "Hào", "Vinh", "Vũ", "Quang Huy", "Khoa", "Khánh", "Minh", "Long"]
def spectate(state="main"):
    im = bg(0.45); put(im, "kit_card_room_selected", (W // 2 - 300, 20), (600, 120)); cy = 80
    ico(im, "eye", W // 2 - 200, cy, 30); text(im, (W // 2 - 160, cy), "Tuan", 34, GOLD, "lm")
    x = pair(im, "alive", W // 2 + 20, cy, 36, "9/10", 26); ico(im, "trophy", x + 40, cy, 26); text(im, (x + 68, cy), "#4", 26, TXT, "lm")
    rows = ALIVE if state != "last" else ALIVE[:1]
    for i, nm in enumerate(rows):
        y = 190 + i * 56; put(im, "kit_row_selected" if i == 0 else ("kit_row_alt" if i % 2 else "kit_row_normal"), (30, y), (300, 48)); text(im, (50, y + 24), nm, 22, GOLD if i == 0 else TXT, "lm")
    put(im, "kit_card_stat", (30, 770), (460, 180)); text(im, (84, 810), "Tuan", 28, TXT, "lm"); itm(im, "Nỏ", 440, 810, 44)
    itm(im, "hp", 100, 872, 36); put(im, "kit_progress_track", (130, 860), (250, 24)); put(im, "kit_progress_fill", (136, 864), (205, 16)); text(im, (400, 872), "86", 24, TXT, "lm")
    x = 540; keycap(im, (x, 990), "A"); keycap(im, (x + 62, 990), "D"); ico(im, "people", x + 160, 1016, 28)  # A, D = doi nguoi
    keycap(im, (x + 240, 990), "E"); ico(im, "skull", x + 340, 1016, 28)  # E = xem ke da ha ban
    iconsq(im, "home", 1620, 976, 64)
    if state == "switch":
        put(im, "kit_toast", (W // 2 - 220, 170), (440, 70)); ico(im, "skull", W // 2 - 170, 205, 22); text(im, (W // 2 - 130, 205), "Vinh", 24, MUTED, "lm"); ico(im, "arrow_right", W // 2 - 20, 205, 22); ico(im, "eye", W // 2 + 40, 205, 22); text(im, (W // 2 + 78, 205), "Tuan", 24, TXT, "lm")
    if state == "last": put(im, "kit_toast", (W // 2 - 110, 500), (220, 90)); ico(im, "trophy", W // 2 - 40, 545, 30); ico(im, "refresh", W // 2 + 40, 545, 30)
    return im

def inventory(state="main", tip=False):
    im = hudbg(0); dim(im, 200); put(im, "kit_panel_l", (130, 70), (1660, 860)); ico(im, "bag", 235, 150, 44); keycap(im, (1640, 124), "Tab", w=70); hr(im, 190, 188, 1460)
    top, ph = 215, 590
    put(im, "kit_panel_s", (180, top), (440, ph)); ico(im, "map", 232, top + 70, 30)
    floor = [] if state == "empty" else [("Shotgun", "F"), ("Lá thần", "×2"), ("Máu kỳ lân", "×1"), ("Nỏ cao su", "")]
    for i, (n, s) in enumerate(floor):
        rh = SZ["floor"] + 10; y = top + 110 + i * (rh + 8); put(im, "kit_row_selected" if i == 0 else "kit_row_normal", (215, y), (370, rh)); itm(im, n, 215 + 14 + SZ["floor"] // 2, y + rh // 2, SZ["floor"])
        if s == "F": keycap(im, (535, y + (rh - 52) // 2), "F", True, 44)
        elif s: text(im, (570, y + rh // 2), s, 22, MUTED, "rm")
    put(im, "kit_panel_s", (650, top), (680, ph)); itm(im, "Nỏ", 705, top + 70, 44)
    for i, n in enumerate(["Tay không", "Nỏ", "Nỏ cao su", ""]):
        x = 705 + i * 150; put(im, "kit_slot_item_selected" if i == 1 else ("kit_slot_item_normal" if n else "kit_slot_item_empty"), (x, top + 100), (130, 140)); text(im, (x + 16, top + 122), str(i + 1), 18, MUTED, "lm")
        if n: itm(im, n, x + 65, top + 172, SZ["slot"])
    itm(im, "Lá thần", 705, top + 290, 44)
    for i, (n, c) in enumerate([("Lá thần", "×6"), ("Máu kỳ lân", "×1"), ("", ""), ("Tăng lực", "×4")]):
        x = 705 + i * 150; put(im, "kit_slot_item_normal" if n else "kit_slot_item_empty", (x, top + 320), (130, 140)); text(im, (x + 16, top + 342), str(i + 5), 18, MUTED, "lm")
        if n: itm(im, n, x + 65, top + 392, SZ["slot"])
        if c: text(im, (x + 118, top + 342), c, 20, TXT, "rm")
    put(im, "kit_panel_s", (1360, top), (390, ph)); itm(im, "Giáp gai", 1410, top + 70, 44)
    for i, (k, v) in enumerate([("Mũ gỗ", "18"), ("Giáp gai", "48"), ("Adrenaline", "55"), (None, "4/90")]):
        y = top + 105 + i * 98; put(im, "kit_row_normal", (1395, y), (320, 88)); itm(im, k, 1395 + 20 + SZ["equip"] // 2, y + 44, SZ["equip"]); text(im, (1700, y + 44), v, 28, TXT, "rm")
    if tip:  # tooltip khi re chuot len hang Shotgun (ten vat pham chi hien o day); du lieu tu ITEM LIST: loai, nguon
        put(im, "kit_tip_item", (470, 385), (390, 190)); itm(im, "Shotgun", 520, 440, 44); text(im, (560, 440), "Shotgun", 26, GOLD, "lm"); hr(im, 505, 472, 290)
        text(im, (505, 506), "Vũ khí", 22, TXT, "lm"); text(im, (505, 538), "Tiếp tế, rương gỗ, xác", 18, TXT, "lm")
    return im

def bigmap(state="main"):
    im = hudbg(0); dim(im, 215); put(im, "kit_panel_l", (50, 40), (1090, 1000)); mx, my, ms = 110, 100, 840
    d = ImageDraw.Draw(im); d.rectangle([mx, my, mx + ms, my + ms], fill=(31, 29, 24, 255), outline=(90, 86, 70, 255), width=2)
    for i in range(1, 10):
        d.line([(mx + i * ms / 10, my), (mx + i * ms / 10, my + ms)], fill=(60, 57, 46), width=1); d.line([(mx, my + i * ms / 10), (mx + ms, my + i * ms / 10)], fill=(60, 57, 46), width=1)
    cx_, cy_ = mx + 420, my + 430; d.ellipse([cx_ - 340, cy_ - 340, cx_ + 340, cy_ + 340], outline=(219, 166, 69), width=4); d.ellipse([cx_ - 70, cy_ - 140, cx_ + 110, cy_ + 40], outline=(237, 242, 238), width=4)
    put_fx(im, "fx_map_player", cx_ - 180, cy_ + 230, 66); put_fx(im, "fx_map_mark", cx_ + 40, cy_ - 60, 70); put_fx(im, "fx_map_drop", cx_ + 250, cy_ - 220, 90)
    put(im, "kit_panel_s", (1170, 40), (700, 150)); cy = 115
    icon(im, "kit_icon_warn", 1220, cy, 44); text(im, (1255, cy), "3", 44, GOLD, "lm"); itm(im, "zone", 1370, cy, 56); text(im, (1410, cy), "01:24", 40, TXT, "lm"); put_fx(im, "fx_map_player", 1610, cy, 52); text(im, (1645, cy), "420", 40, TXT, "lm")
    put(im, "kit_panel_s", (1170, 210), (700, 130))  # chu thich: chi icon (cung hinh voi tren ban do)
    for i, (kind, nm) in enumerate([("ic", "kit_icon_warn"), ("ring", None), ("fx", "fx_map_drop"), ("fx", "fx_map_mark"), ("fx", "fx_map_player")]):
        x = 1235 + i * 140; y = 275
        if kind == "fx": put_fx(im, nm, x, y, 52)
        elif kind == "ic": icon(im, nm, x, y, 40)
        else: ImageDraw.Draw(im).ellipse([x - 16, y - 16, x + 16, y + 16], outline=(237, 242, 238), width=3)
    put(im, "kit_panel_s", (1170, 360), (700, 360))
    rows = [("mouse", "fx_map_mark"), ("mouse", "kit_ico_plus"), ("mouse", "kit_ico_arrow_left"), ("M", "kit_icon_cross_red")]
    for i, (a, b) in enumerate(rows):
        y = 430 + i * 75
        if a == "M": keycap(im, (1250, y - 26), "M")
        else: itm(im, "mouse", 1276, y, 52)
        ico(im, "arrow_right", 1420, y, 26)
        if b.startswith("fx_"): put_fx(im, b, 1560, y, 52)
        elif b.startswith("kit_icon"): icon(im, b, 1560, y, 36)
        else:
            ico(im, b[8:], 1560, y, 30)
            if b.endswith("arrow_left"): ico(im, "arrow_right", 1640, y, 30)
    return im

RANK = [("Huy", 3), ("Hào", 4), ("Vinh", 1), ("V.Huy", 2), ("Vũ", 0), ("Quang Huy", 1), ("Khoa", 0), ("Khánh", 0), ("Minh", 0), ("Long", 0)]
def result(win=True):
    im = bg(0.62); me = 0 if win else 3; put(im, "kit_panel_m", (1000, 120), (820, 820)); ico(im, "trophy", 1075, 190, 36); text(im, (1130, 190), "Xếp hạng", 32, GOLD, "lm")
    for i, (nm, k) in enumerate(RANK):
        y = 230 + i * 68; put(im, "kit_row_selected" if i == me else ("kit_row_alt" if i % 2 else "kit_row_normal"), (1050, y), (720, 60)); text(im, (1075, y + 30), f"#{i + 1}", 24, GOLD if i == me else MUTED, "lm"); text(im, (1160, y + 30), nm, 24, TXT, "lm")
        itm(im, "kill", 1685, y + 30, 32); text(im, (1745, y + 30), str(k), 22, MUTED, "rm")
    put_fx(im, "fx_emblem_win" if win else "fx_emblem_lose", 480, 250, 230)
    if win: text(im, (480, 440), "CHIẾN THẮNG", 78, GOLD, "mm"); text(im, (480, 545), "#1 / 10", 36, TXT, "mm")
    else:
        text(im, (480, 440), "HẠNG #4", 78, TXT, "mm"); cause(im, 480, 520, "Hào", "Nỏ", "6", 720, 110)
    sy = 600 if win else 665
    for i, (k, v) in enumerate([("kill", "3" if win else "2"), ("zone", "07:00" if win else "04:38")]): stat(im, 120 + i * 350, sy, k, v, 330, 130)
    btn(im, "secondary", 120, 830, 330, 72, "Về sảnh"); btn(im, "primary", 480, 830, 330, 72, "Chơi lại")
    return im

def disconnect(state="retry"):
    im = hudbg(120); dim(im, 120); w, h = 760, 420; x0, y0 = (W - w) // 2, (H - h) // 2; put(im, "kit_panel_m", (x0, y0), (w, h)); cx = W // 2
    put_fx(im, "fx_icon_disconnect", cx, y0 + 90, 64)
    if state == "retry":
        text(im, (cx, y0 + 170), "Đang kết nối lại...", 36, TXT, "mm")
        put(im, "kit_progress_track", (x0 + 70, y0 + 230), (w - 140, 28)); put(im, "kit_progress_fill", (x0 + 76, y0 + 236), (int((w - 152) * 0.7), 16)); ico(im, "clock", x0 + w - 70 - 90, y0 + 290, 26); text(im, (x0 + w - 70, y0 + 290), "11", 24, GOLD, "rm")
        btn(im, "secondary", x0 + 70, y0 + h - 110, 300, 64, "Về sảnh"); btn(im, "primary", x0 + w - 70 - 300, y0 + h - 110, 300, 64, "Thử lại ngay")
    elif state == "failed":
        text(im, (cx, y0 + 170), "Chưa kết nối lại được", 36, TXT, "mm"); err(im, (cx - 70, y0 + 225), "Còn 6 s", 24)
        put(im, "kit_progress_track", (x0 + 70, y0 + 260), (w - 140, 28)); put(im, "kit_progress_fill", (x0 + 76, y0 + 266), (int((w - 152) * 0.4), 16))
        btn(im, "secondary", x0 + 70, y0 + h - 110, 300, 64, "Về sảnh"); btn(im, "primary", x0 + w - 70 - 300, y0 + h - 110, 300, 64, "Thử lại")
    else:
        text(im, (cx, y0 + 170), "Đã mất kết nối", 36, TXT, "mm"); text(im, (cx, y0 + 230), "Hạng #4 / 10", 30, TXT, "mm")
        btn(im, "primary", cx - 160, y0 + h - 110, 320, 64, "Về sảnh")
    return im

def feedback(low=False):
    im = hudbg(0)
    if low: im.alpha_composite(fx("fx_vignette").resize((W, H), Image.NEAREST))
    put_fx(im, "fx_hitmarker", 1230, 432, 96); put_fx(im, "fx_damage_arc", 1455, 495, 200); put_fx(im, "fx_danger_arrow", 480, 590, 90); itm(im, "zone", 546, 590, 34); tag(im, 610, 590, "8", 24, 70)
    put(im, "kit_toast", (790, 36), (340, 60)); icon(im, "kit_icon_warn", 830, 66, 26); itm(im, "zone", 884, 66, 32); text(im, (912, 66), "8", 24, TXT, "lm"); ico(im, "arrow_left", 996, 66, 20); text(im, (1030, 66), "4", 24, MUTED, "lm")
    for i, (n, t) in enumerate((("poison", "3"), ("slow", "5"), ("stealth", "9"))): itm(im, n, 90 + i * 124, 730, 76); tag(im, 90 + i * 124, 790, t, 20, 60)
    uh = SZ["use"] + 12; put(im, "kit_toast", (650, 882 - uh // 2), (620, uh)); itm(im, "Lá thần", 690, 882, SZ["use"]); put(im, "kit_progress_track", (730, 870), (360, 24)); put(im, "kit_progress_fill", (736, 874), (190, 16)); text(im, (1110, 882), "1,2", 22, GOLD, "lm")
    ph = SZ["pick"] + 20; pw = 20 + 52 + 14 + SZ["pick"] + (14 + tw("Shotgun", 26) + 24 if SZ["name"] else 6) + 10; put(im, "kit_toast", (1020, 646 - ph // 2), (int(pw), ph)); keycap(im, (1036, 646 - 26), "F", True, 52); itm(im, "Shotgun", 1036 + 52 + 14 + SZ["pick"] // 2, 646, SZ["pick"])
    if SZ["name"]: text(im, (1036 + 52 + 14 + SZ["pick"] + 14, 646), "Shotgun", 26, TXT, "lm")
    for i, (n, c) in enumerate((("Giáp gai", "×1"), ("Lá thần", "×2"), ("Shotgun", "×1"))):
        rh = SZ["list"] + 12; y = 1060 - 20 - (3 - i) * (rh + 6); lw = 300 if SZ["name"] else 210; x = 1900 - lw; put(im, "kit_row_alt", (x, y), (lw, rh)); text(im, (x + 14, y + rh // 2), "+", 24, TXT, "lm"); itm(im, n, x + 40 + SZ["list"] // 2, y + rh // 2, SZ["list"])
        if SZ["name"]: text(im, (x + 40 + SZ["list"] + 10, y + rh // 2), n, 22, TXT, "lm")
        text(im, (x + lw - 14, y + rh // 2), c, 22, MUTED, "rm")
    tag(im, 1900 - 45, 1060 - 20 - 3 * (SZ["list"] + 18) - 30, "+2", 20, 90); return im

ALL = {"b1-esc-cai-dat": lambda: esc(), "b1-esc-xac-nhan-thoat": lambda: esc("confirm"), "b2-bi-loai": lambda: eliminated("killed"), "b2-bi-loai-bo": lambda: eliminated("zone"), "b2-bi-loai-het-nguoi": lambda: eliminated("last"),
       "b3-spectator": lambda: spectate(), "b3-spectator-chuyen": lambda: spectate("switch"), "b3-spectator-het": lambda: spectate("last"), "b4-tui-do": lambda: inventory(), "b4-tui-do-tooltip": lambda: inventory(tip=True), "b4-tui-do-trong": lambda: inventory("empty"),
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
