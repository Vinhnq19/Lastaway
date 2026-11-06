"""T-026: tach 3 sheet nut noi khoi (GPT), do day de, dung trang so sanh va kiem 9-slice.
Chay: PYTHONIOENCODING=utf-8 python nut.py [extract|compare|slice]"""
import sys, pathlib, json
sys.path.insert(0, r"D:\Lastaway\projects\lastaway\art\inbox\T-015\src\tools"); sys.path.insert(0, r"D:\Lastaway\projects\lastaway\art\inbox\T-021\src\tools")
import extract_t015 as E
from PIL import Image, ImageDraw
import mock as M
from mock import F, text, tw, T20, TXT, MUTED, GOLD, DANGER, W as _W
ROOT = pathlib.Path(__file__).resolve().parent.parent; GEN = ROOT / "src" / "gen"; NUT = ROOT / "nut"
VARS = [("vua", "Vừa"), ("ro", "Rõ"), ("rat-ro", "Rất rõ")]
MATS = ["primary", "secondary", "danger"]; STATES = ["normal", "hover", "pressed", "disabled"]
def lum(c):
    f = lambda v: v / 12.92 if v / 255 <= 0.03928 else (((v / 255 + 0.055) / 1.055) ** 2.4) * 255
    r, g, b = [f(x) / 255 if x / 255 <= 0.03928 else ((x / 255 + 0.055) / 1.055) ** 2.4 for x in c[:3]]
    return 0.2126 * r + 0.7152 * g + 0.0722 * b
def cr(a, b):
    x, y = sorted((lum(a), lum(b)), reverse=True); return (x + .05) / (y + .05)

def extract(name):
    sheet = "nut-" + name; im = E.key_magenta(Image.open(GEN / f"{sheet}.png")); bs = E.components(im, min_area=3000)
    assert len(bs) == 12, (name, len(bs))
    bs.sort(key=lambda b: (b[1] + b[3]) / 2); rows = [sorted(bs[i * 4:(i + 1) * 4], key=lambda b: b[0]) for i in range(3)]
    d = NUT / name; d.mkdir(parents=True, exist_ok=True); out = {}
    for mi, mat in enumerate(MATS):
        for si, st in enumerate(STATES):
            b = rows[mi][si]; p = im.crop(tuple(b[:4])); p.save(d / f"{mat}_{st}.png"); out[(mat, st)] = p
    return out

def base_px(p):
    """day = so hang pixel cuoi (tru vien den 2 hang) co mau toi hon mat ro ret: tim buoc giam do sang lon nhat o 45%..95% chieu cao tai cot giua"""
    w, h = p.size; x = w // 2; col = [p.getpixel((x, y)) for y in range(h)]; l = [lum(c) if c[3] > 128 else 0 for c in col]
    best, by = 0, h
    for y in range(int(h * .45), h - 3):
        dlt = l[y - 1] - l[y]
        if dlt > best: best, by = dlt, y
    return max(1, h - 2 - by)

def hslice(p, size):
    """3-slice ngang: giu goc va chieu cao (p da duoc co deu theo chieu cao). Vien L=R=1.1*h/3"""
    w, h = size; k = h / p.height; q = p.resize((max(1, round(p.width * k)), h), Image.LANCZOS)
    c = max(8, round(q.height * 0.34)); o = Image.new("RGBA", size, (0, 0, 0, 0))
    if w <= 2 * c: return q.resize(size, Image.NEAREST)
    o.paste(q.crop((0, 0, c, h)), (0, 0)); o.paste(q.crop((q.width - c, 0, q.width, h)), (w - c, 0))
    mid = q.crop((c, 0, q.width - c, h)); o.paste(mid.resize((w - 2 * c, h), Image.NEAREST), (c, 0)); return o

class Set:
    def __init__(self, name): self.name = name; self.p = extract(name); self.hn = self.p[("primary", "normal")].height; self.base = {k: base_px(v) for k, v in self.p.items()}
    def face_rgb(self, mat, st):
        p = self.p[(mat, st)]; w, h = p.size; px = [p.getpixel((x, y)) for x in range(int(w * .3), int(w * .7), 3) for y in range(int(h * .15), int(h * .35), 2)]
        px = [c for c in px if c[3] > 200]; return tuple(sorted(px, key=lum)[len(px) // 2][:3])
    def draw(self, im, mat, st, xy, size, caption, font=26, ic=None):
        w, h = size; q = hslice(self.p[(mat, st)], (w, round(h * self.p[(mat, st)].height / self.hn)))
        if st == "pressed" or st == "disabled": y = xy[1] + h - q.height
        else: y = xy[1]
        im.alpha_composite(q, (int(xy[0]), int(y)))
        bk = self.base[(mat, st)] * (q.height / self.p[(mat, st)].height); fy = y + (q.height - bk) / 2
        col = self.txtcol(mat, st); tcx = xy[0] + w / 2
        if ic:
            tot = tw(caption, font) + font + 10; dt = getattr(self, "dtxt", None)
            if dt and mat == "secondary":
                sp = M.spr(ic); a_ = sp.getchannel("A"); sp = Image.new("RGBA", sp.size, tuple(dt) + (255,)); sp.putalpha(a_); sp = sp.resize((max(1, round(sp.width * (font - 2) / sp.height)), font - 2), Image.NEAREST)
                im.alpha_composite(sp, (round(tcx - tot / 2 + font / 2 - sp.width / 2), round(fy - sp.height / 2))); text(im, (tcx - tot / 2 + font + 10, fy), caption, font, dt, "lm", 0)
            else: M.icon(im, ic, tcx - tot / 2 + font / 2, fy, font - 2); text(im, (tcx - tot / 2 + font + 10, fy), caption, font, DANGER if (ic and mat == "secondary") else col, "lm", 2)
        else: text(im, (tcx, fy), caption, font, col, "mm", 0 if col[0] < 100 else (3 if getattr(self, "outline3", False) else 2))
    def band(self, mat, st):
        p = self.p[(mat, st)]; w, h = p.size; bs = self.base[(mat, st)]; fh = h - bs
        px = [p.getpixel((x, y)) for x in range(int(w * .2), int(w * .8), 3) for y in range(int(fh * .3), int(fh * .7), 2)]
        return sorted([c[:3] for c in px if c[3] > 200], key=lum)
    def worst(self, mat, st, fg):
        px = self.band(mat, st); lo, hi = px[len(px) // 10], px[len(px) * 9 // 10]; return min(cr(fg, lo), cr(fg, hi))
    def txtcol(self, mat, st):
        if st == "disabled": return (235, 235, 230)
        dark = (31, 26, 16); white = (255, 255, 255)  # mot mau chu co dinh cho moi chat lieu: chon mau co min lon hon qua 3 trang thai
        wd = min(self.worst(mat, q, dark) for q in STATES[:3]); wl = min(self.worst(mat, q, white) for q in STATES[:3])
        return white if wl >= wd else dark
    def txtratio(self, mat, st): return self.worst(mat, st, self.txtcol(mat, st))
    def txtcol_old(self, mat, st):
        f = self.face_rgb(mat, st)
        if st == "disabled": return (235, 235, 230)
        dark = (31, 26, 16); white = (255, 255, 255)
        return white if cr(white, f) >= cr(dark, f) and cr(white, f) >= 4.5 else dark

def old_button(im, mat, st, xy, size, caption, font=26, ic=None):
    kind = {"primary": "primary", "secondary": "secondary", "danger": "danger"}[mat]; col = DANGER if (ic and mat == "secondary") else M.WHITE
    M.button(im, kind, xy, caption, ic, col, size, st, font)

def panel_bg():
    return M.bg(0.62)

def column(im, x0, name, label, S):
    d = ImageDraw.Draw(im); cw = 560
    text(im, (x0 + cw / 2, 60), label, 38, GOLD, "mm"); text(im, (x0 + cw / 2, 108), S.info if S else "nút hiện tại T-020 (viền olive + rêu giống khung)", 20, MUTED, "mm")
    B = (lambda mat, st, xy, size, cap, ic=None, font=26: S.draw(im, mat, st, xy, size, cap, font, ic)) if S else (lambda mat, st, xy, size, cap, ic=None, font=26: old_button(im, mat, st, xy, size, cap, font, ic))
    # hop xac nhan xoa phong
    px, py, pw, ph = x0, 150, cw, 340; M.put(im, "kit_panel_popup", (px, py), (pw, ph)); text(im, (px + pw / 2, py + 24 + 31), "Xóa phòng?", 32, GOLD, "mm")
    text(im, (px + pw / 2, py + 160), "Phòng Leo rank tối nay sẽ bị xóa.", 22, TXT, "mm"); text(im, (px + pw / 2, py + 196), "Không thể hoàn tác.", 20, MUTED, "mm")
    B("secondary", "normal", (px + 50, py + ph - 24 - 64 - 40), (220, 64), "Hủy"); B("danger", "normal", (px + pw - 50 - 220, py + ph - 24 - 64 - 40), (220, 64), "Xóa phòng")
    # hang nut o Sanh
    sy = 520; M.put(im, "kit_panel_m", (x0, sy), (cw, 520)); text(im, (x0 + cw / 2, sy + 80), "Sảnh", 30, GOLD, "mm")
    B("primary", "normal", (x0 + 70, sy + 120), (420, 80), "Vào phòng", None, 30); B("secondary", "normal", (x0 + 70, sy + 225), (420, 80), "Tạo phòng", None, 30); B("secondary", "normal", (x0 + 70, sy + 330), (420, 80), "Thoát trận", "kit_icon_cross", 30)
    # 4 trang thai (chinh va phu)
    yy = 1075; text(im, (x0, yy), "Chính: thường, hover, nhấn, tắt", 18, MUTED, "lm")
    for i, st in enumerate(STATES): B("primary", st, (x0 + i * 140, yy + 20), (128, 56), ["Vào", "Vào", "Vào", "Vào"][i], None, 22)
    text(im, (x0, yy + 110), "Phụ", 18, MUTED, "lm")
    for i, st in enumerate(STATES): B("secondary", st, (x0 + i * 140, yy + 130), (128, 56), "Hủy", None, 22)
    text(im, (x0, yy + 220), "Đỏ (chỉ hộp xác nhận)", 18, MUTED, "lm")
    for i, st in enumerate(STATES): B("danger", st, (x0 + i * 140, yy + 240), (128, 56), "Xóa", None, 22)

def compare():
    sets = []
    for n, lab in VARS:
        S = Set(n); S.info = ""; sets.append((n, lab, S))
    for n, lab, S in sets:
        bs = S.base[("primary", "normal")]; hp = S.p[("primary", "pressed")].height; hn = S.hn
        at64 = bs * 64 / hn; sink = (hn - hp) * 64 / hn
        S.info = f"đế {at64:.0f} px khi nút cao 64 (1080p), nhấn lún {sink:.0f} px"; S.at64 = at64; S.sink = sink
    Wd, Hd = 2400, 1420; im = Image.new("RGBA", (Wd, Hd), (31, 29, 24, 255)); bgi = panel_bg().resize((Wd, Hd)); im.alpha_composite(bgi)
    M.W = Wd; xs = [40, 640, 1240, 1840]
    column(im, xs[0], "cu", "Hiện tại (T-020)", None)
    for i, (n, lab, S) in enumerate(sets): column(im, xs[i + 1], n, f"{lab} ({S.at64:.0f} px)", S)
    text(im, (Wd / 2, 1400), "Nút nổi khối: mặt + mép sáng trên + đế dày phía dưới, không viền olive hay rêu (đó là của khung). Cỡ đế đo ở nút cao 64 trên 1080p.", 20, MUTED, "mm")
    im.convert("RGB").save(ROOT / "so-sanh-nut.png")
    rep = {n: {"de_px_o_cao_64": round(S.at64, 1), "lun_px": round(S.sink, 1), "hn_native": S.hn, "base_native": {f"{k[0]}_{k[1]}": v for k, v in S.base.items()},
               "face": {f"{m}_{s}": "#%02X%02X%02X" % S.face_rgb(m, s) for m in MATS for s in STATES},
               "chu": {f"{m}_{s}": ("trang" if S.txtcol(m, s)[0] > 200 else "toi") + f" {cr(S.txtcol(m, s), S.face_rgb(m, s)):.2f}" for m in MATS for s in STATES}} for n, lab, S in sets}
    (ROOT / "nut-do.json").write_text(json.dumps(rep, ensure_ascii=False, indent=1), encoding="utf-8"); print(json.dumps(rep, ensure_ascii=False)[:1500])

def slicetest(name):
    S = Set(name); sizes = [("S", 48), ("M", 64), ("L", 80)]; widths = [96, 240, 420]
    Wd, Hd = 1700, 760; im = Image.new("RGBA", (Wd, Hd), (43, 40, 33, 255)); d = ImageDraw.Draw(im); text(im, (30, 30), f"9-slice (3-slice ngang, giữ cao) của phương án {name}: S 48, M 64, L 80 x rộng 96, 240, 420", 26, GOLD, "lm")
    y = 80
    for sn, h in sizes:
        text(im, (30, y + h / 2), sn, 28, TXT, "lm"); x = 100
        for w in widths:
            for mat in MATS[:2]: pass
            S.draw(im, "primary", "normal", (x, y), (w, h), "Vào phòng" if w > 150 else "Vào", 24 if h < 60 else 28); x += w + 30
        S.draw(im, "secondary", "pressed", (x, y), (240, h), "Hủy", 24 if h < 60 else 28); x += 270; S.draw(im, "danger", "normal", (x, y), (240, h), "Xóa", 24 if h < 60 else 28)
        y += h + 70
    # zoom 3x goc: nut M 240 de soi vien
    z = Image.new("RGBA", (240, 64), (0, 0, 0, 0)); S.draw(z, "primary", "normal", (0, 0), (240, 64), "", 26); z = z.resize((240 * 3, 64 * 3), Image.NEAREST); im.alpha_composite(z, (100, y + 10))
    z2 = Image.new("RGBA", (420, 80), (0, 0, 0, 0)); S.draw(z2, "secondary", "normal", (0, 0), (420, 80), "", 26); z2 = z2.resize((420 * 2, 80 * 2), Image.NEAREST); im.alpha_composite(z2, (880, y + 10))
    im.convert("RGB").save(ROOT / f"nut-9slice-{name}.png")

if __name__ == "__main__":
    a = sys.argv[1] if len(sys.argv) > 1 else "compare"
    if a == "compare": compare()
    elif a == "slice": slicetest(sys.argv[2])
