"""T-021 mock helper: dat sprite UI kit T-020 (GPT) len nen, ghi chu chu. Khong ve tay do hoa; chi bo cuc, chu, nen toi."""
import sys, json, pathlib
import thang
from PIL import Image, ImageDraw, ImageFont, ImageFilter
T20 = pathlib.Path(r"D:\Lastaway\projects\lastaway\art\inbox\T-020"); T15 = pathlib.Path(r"D:\Lastaway\projects\lastaway\art\inbox\T-015"); OUT = pathlib.Path(r"D:\Lastaway\projects\lastaway\art\inbox\T-021")
FB = r"D:\Lastaway\Assets\_JungleMaze\Art\Fonts\BeVietnamPro-Bold.ttf"; F = lambda n: ImageFont.truetype(FB, n)
meta = json.load(open(T20 / "meta.json", encoding="utf-8"))
DARK = (31, 26, 16); XRED = (85, 20, 15); LIGHT = (235, 235, 230); TXT = (237, 242, 238); MUTED = (214, 218, 214); GOLD = (255, 220, 110); DANGER = (255, 138, 120); WHITE = (255, 255, 255); OUTL = (10, 12, 12)
W, H = 1920, 1080

def spr(n, root=T20): return Image.open(root / f"{n}.png").convert("RGBA")
def sized(n, size=None):
    im = spr(n); m = meta[n]
    if size is None or tuple(size) == tuple(m["size"]): return im
    b = m["border_LRTB"]
    if b is None: return im.resize(size, Image.NEAREST)
    L, R, T, B = b; w, h = im.size; Wd, Hd = size; o = Image.new("RGBA", size, (0, 0, 0, 0))
    xs = [(0, L, 0, L), (L, w - R, L, Wd - R), (w - R, w, Wd - R, Wd)]; ys = [(0, T, 0, T), (T, h - B, T, Hd - B), (h - B, h, Hd - B, Hd)]
    for a0, a1, d0, d1 in xs:
        for c0, c1, e0, e1 in ys:
            if a1 > a0 and c1 > c0 and d1 > d0 and e1 > e0: o.paste(im.crop((a0, c0, a1, c1)).resize((d1 - d0, e1 - e0), Image.NEAREST), (d0, e0))
    return o
def put(im, n, xy, size=None): im.alpha_composite(sized(n, size), (int(xy[0]), int(xy[1])))
LOG = []; CUR = [""]  # LOG: do tuong phan chu
def _lum(c):
    f = lambda v: ((v / 255) / 12.92) if v / 255 <= 0.03928 else (((v / 255 + 0.055) / 1.055) ** 2.4)
    return 0.2126 * f(c[0]) + 0.7152 * f(c[1]) + 0.0722 * f(c[2])
def cratio(a, b):
    x, y = sorted((_lum(a), _lum(b)), reverse=True); return (x + .05) / (y + .05)
def text(im, xy, s, size, fill=TXT, anchor="la", stroke=2, skip=False, btn=False, exempt=False):
    size = thang.size(size, thang.is_game(CUR[0]) and not exempt);  # exempt: so phim, so tren o (20 duoc phep o gameplay)
    fnt = thang.font(size, btn); stroke = thang.stroke(size, stroke)
    d = ImageDraw.Draw(im)
    if s.strip() and not skip:  # do tuong phan tu pixel nen ngay truoc khi ve: xau nhat o p10 va p90 do sang nen trong khung chu
        x0, y0, x1, y1 = [int(v) for v in d.textbbox(xy, s, font=fnt, anchor=anchor)]; x0, y0 = max(0, x0), max(0, y0); x1, y1 = min(im.width, x1), min(im.height, y1)
        if x1 > x0 and y1 > y0:
            px = sorted(im.crop((x0, y0, x1, y1)).convert("RGB").getdata(), key=_lum); lo, hi = px[len(px) // 10], px[len(px) * 9 // 10]
            LOG.append({"mock": CUR[0], "text": s, "size": size, "font": thang.tier(size, btn), "btn": btn, "bbox": [x0, y0, x1, y1], "game": thang.is_game(CUR[0]), "fill": "#%02X%02X%02X" % tuple(fill[:3]), "ratio": round(min(cratio(fill, lo), cratio(fill, hi)), 2)})
    d.text(xy, s, font=fnt, fill=fill, anchor=anchor, stroke_width=stroke, stroke_fill=OUTL)
def tw(s, size, btn=False):
    size = thang.size(size, thang.is_game(CUR[0])); return ImageDraw.Draw(Image.new("RGBA", (1, 1))).textlength(s, font=thang.font(size, btn))
def icon(im, n, cx, cy, h):
    i = spr(n); i = i.resize((max(1, round(i.width * h / i.height)), h), Image.NEAREST); im.alpha_composite(i, (round(cx - i.width / 2), round(cy - i.height / 2)))

def bg(dim=0.62, vignette=True):
    g = Image.open(T15 / "dung-thu-hud-A.png").convert("RGB"); tile = g.crop((640, 120, 1150, 400)); b = Image.new("RGB", (W, H))
    for y in range(0, H, tile.height):
        for x in range(0, W, tile.width): b.paste(tile, (x, y))
    b = b.convert("RGBA"); b.alpha_composite(Image.new("RGBA", (W, H), (31, 29, 24, int(255 * dim))))
    if vignette:
        v = Image.new("L", (W, H), 0); d = ImageDraw.Draw(v); d.ellipse([-300, -250, W + 300, H + 250], fill=255); v = v.filter(ImageFilter.GaussianBlur(160))
        dark = Image.new("RGBA", (W, H), (10, 10, 8, 150)); dark.putalpha(Image.eval(v, lambda p: 150 - int(p * 150 / 255))); b.alpha_composite(dark)
    return b
_logo = None
def logo(im, cx, y, w=700):
    global _logo
    if _logo is None: _logo = Image.open(r"D:\Lastaway\Assets\_JungleMaze\Art\UI\Menu\Logo.png").convert("RGBA").crop((76, 68, 952, 184))
    l = _logo.resize((w, round(_logo.height * w / _logo.width)), Image.NEAREST); im.alpha_composite(l, (round(cx - w / 2), y))

_cache = {}
def kbtn(kind, state, w, h):
    key = (kind, state, w, h)
    if key not in _cache:
        sp = Image.open(T20 / f"kit_btn_{kind}_{state}@2x.png").convert("RGBA"); k = h / sp.height; q = sp.resize((max(1, round(sp.width * k)), h), Image.LANCZOS); c = max(8, round(h * 0.34)); o = Image.new("RGBA", (w, h), (0, 0, 0, 0))
        if w <= 2 * c: o = q.resize((w, h), Image.NEAREST)
        else:
            o.paste(q.crop((0, 0, c, h)), (0, 0)); o.paste(q.crop((q.width - c, 0, q.width, h)), (w - c, 0)); o.paste(q.crop((c, 0, q.width - c, h)).resize((w - 2 * c, h), Image.NEAREST), (c, 0))
        _cache[key] = o
    return _cache[key]
BASE_FRAC = {"normal": 0.11, "hover": 0.11, "pressed": 0.04, "disabled": 0.0}  # de theo ti le cao nut (spec chung voi check_center.py)
ICONS = {"Vào phòng": "play", "Vào": "play", "Tạo phòng": "plus", "Lịch sử": "clock", "Bảng xếp hạng": "trophy", "Cài đặt": "gear", "Thoát": "door", "Đăng xuất": "logout",
         "Hủy": "@cross_ink", "Xóa": "trash", "Xóa phòng": "trash", "Sao chép": "copy", "Chép": "copy", "Thử lại": "refresh", "Đăng nhập": "key", "Máy chủ (nâng cao)": "server",
         "Rời phòng": "@cross_red", "Thoát trận": "@cross_red", "Sẵn sàng (tùy chọn)": "check", "Về trang chủ": "home"}
LOGB = []
_ink = {}
def icon_ink(name):
    """sprite icon cat sat vung muc (icon trong canvas 48 co le); tra ve anh da cat"""
    if name not in _ink:
        sp = spr(("kit_icon_cross_ink" if name == "@cross_ink" else "kit_icon_cross_red") if name.startswith("@") else f"kit_ico_{name}", T20); _ink[name] = sp.crop(sp.getchannel("A").getbbox())
    return _ink[name]
def face_center(kind, state, w, h):
    a = kbtn(kind, state, w, h).getchannel("A").getbbox(); top, bot = a[1], a[3]; return (top + bot - h * BASE_FRAC[state]) / 2.0
def button(im, kind, xy, caption, ic=None, col=WHITE, size=(240, 64), state="normal", font=26, icon_name=None):
    """nut K2: chu toi, icon ben trai (tu chon theo ICONS; icon_name='' de bo), icon cao ~ font, can giua thi giac theo MAT nut (tru de)."""
    w, h = size; font = thang.btn_size(h, font); x0, y0 = int(xy[0]), int(xy[1]); im.alpha_composite(kbtn(kind, state, w, h), (x0, y0))
    nm = ICONS.get(caption) if icon_name is None else (icon_name.replace("kit_ico_", "") if icon_name else None)
    LOGB.append({"mock": CUR[0], "kind": kind, "state": state, "w": w, "h": h, "font": font, "caption": caption, "icon": nm})
    fc = y0 + face_center(kind, state, w, h); tcol = DARK
    capH = -thang.font(font, True).getbbox("H", anchor="ls")[1]; base_y = fc + capH / 2.0
    icn = None
    if nm:
        ink = icon_ink(nm); ih = font; iw = max(1, round(ink.width * ih / ink.height)); icn = ink.resize((iw, ih), Image.LANCZOS)
    if caption:
        tot = tw(caption, font, True) + ((icn.width + 10) if icn else 0); x = x0 + w / 2 - tot / 2
        if icn: im.alpha_composite(icn, (round(x), round(fc - icn.height / 2.0))); x += icn.width + 10
        text(im, (x, base_y), caption, font, tcol, "ls", 0, btn=True)
    elif icn: im.alpha_composite(icn, (round(x0 + w / 2 - icn.width / 2), round(fc - icn.height / 2.0)))
def label(im, xy, s, size=20, col=MUTED): text(im, xy, s, size, col, "lm")
def err(im, xy, s, size=20):
    icon(im, "kit_icon_cross", xy[0] + 12, xy[1], 22); text(im, (xy[0] + 32, xy[1]), s, size, DANGER, "lm")
def ok(im, xy, s, size=20):
    icon(im, "kit_icon_check", xy[0] + 12, xy[1], 22); text(im, (xy[0] + 32, xy[1]), s, size, TXT, "lm")
def note(im, s, y=1050): text(im, (W / 2, y), s, 18, MUTED, "mm", skip=True)  # ghi chu mock, khong phai UI: khong do, khong tinh vao cong
def avatar(im, cx, cy, ghost=False):
    put(im, "kit_iconbtn_round_disabled" if ghost else "kit_iconbtn_round_normal", (cx - 24, cy - 24), (48, 48))
def input_(im, xy, w, value, state="normal", ph=False, size=24):
    put(im, f"kit_input_{state}", xy, (w, 56)); text(im, (xy[0] + 20, xy[1] + 28), value, size, MUTED if ph else TXT, "lm")
def codebox(im, xy, chars, active, err=False):
    for i in range(6):
        x = xy[0] + i * 76; st = "error" if err else ("active" if i == active else ("filled" if i < len(chars) else "empty")); put(im, f"kit_codebox_{st}", (x, xy[1]), (66, 78))
        if i < len(chars): text(im, (x + 33, xy[1] + 38), chars[i], 38, TXT, "mm")
def sized_hud(n, size): return spr(n, T15).resize(size, Image.NEAREST)
def close_btn(im, x0, y0, w):
    put(im, "kit_close_normal", (x0 + w - 52 - 34, y0 + 29), (52, 52)); ink = icon_ink("@cross_red"); k = 26 / ink.height; q = ink.resize((max(1, round(ink.width * k)), 26), Image.LANCZOS)
    cx, cy = x0 + w - 52 - 34 + 26, y0 + 29 + (52 - 52 * BASE_FRAC["normal"]) / 2 + 1; im.alpha_composite(q, (round(cx - q.width / 2), round(cy - q.height / 2)))
def footer(im, x0, y0, w, h):
    fy = y0 + h - 47 - 96; put(im, "kit_dialog_footer", (x0 + 47, fy), (w - 94, 96)); return fy + 16
def header(im, title, right=None, back=True):
    put(im, "kit_screen_header", (60, 30), (1800, 104))
    if back: put(im, "kit_iconbtn_square_normal", (96, 50), (64, 64)); im.alpha_composite(spr("kit_ico_arrow_left").resize((40, 40), Image.NEAREST), (108, 58))
    text(im, (W / 2, 84), title, 38, GOLD, "mm")
    if right: text(im, (1820, 84), right, 22, MUTED, "rm")
def ico(im, name, cx, cy, s=32, plate=True):
    """icon muc toi chi dat tren mat sang: mac dinh lot nut tron kaki phia sau (icon tren nen toi se mat)"""
    if plate: pw = s + 20; put(im, "kit_iconbtn_round_normal", (round(cx - pw / 2), round(cy - pw / 2)), (pw, pw))
    im.alpha_composite(spr(f"kit_ico_{name}").resize((s, s), Image.NEAREST), (round(cx - s / 2), round(cy - s / 2 - (2 if plate else 0))))
def keycap(im, xy, k, active=False, w=52):
    put(im, "kit_keycap_active" if active else "kit_keycap_normal", xy, (w, 52)); text(im, (xy[0] + w / 2, xy[1] + 23), k, 22, DARK, "mm", 0)
def save(im, name): im.convert("RGB").save(OUT / name); return OUT / name
