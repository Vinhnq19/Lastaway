"""T-015 luot 3 (dich cuoi): bam anh muc tieu cua Huy (art/brief/T-015/muc-tieu-huy-07-11.png).
Ve bang code, dung kich thuoc, ghi de sprite goc T-015/ va icon hud-icons/png/ (ban cu: T-015/lop2, hud-icons/png-metal-lop2)."""
import sys, json, pathlib, random
import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageChops
sys.stdout.reconfigure(encoding="utf-8")
ROOT = pathlib.Path(r"D:\Lastaway\projects\lastaway\art\inbox\T-015")
ICON = pathlib.Path(r"D:\Lastaway\projects\lastaway\art\inbox\hud-icons\png")

# mau lay tu anh muc tieu (do trung binh vung)
OUTLINE = (34, 29, 21, 235)
BAND_HI, BAND_LO = (128, 116, 94), (104, 93, 72)
GROOVE = (56, 50, 38, 255); INNER_LIGHT = (150, 138, 112, 110)
FILL = (74, 70, 57); FILL_ROW2 = (74, 70, 57)
DIV_DARK = (48, 44, 34, 255); DIV_LIGHT = (112, 105, 86, 200)
SLOT_FILL = (62, 57, 46); SLOT_RIM_HI = (140, 128, 104); SLOT_RIM_LO = (96, 87, 68)
GOLD = (214, 168, 56); GOLD_HI = (245, 205, 90)
MOSS = [(98, 122, 42), (124, 146, 56), (72, 98, 36)]
T = 8; CUT = 10

def poly_mask(W, H, pts, s):
    m = Image.new("L", (W, H), 0); ImageDraw.Draw(m).polygon([(x * s, y * s) for x, y in pts], fill=255); return m
def erode(m, px):
    if px <= 0: return m
    pm = Image.new('L', (m.width + 2 * px, m.height + 2 * px), 0); pm.paste(m, (px, px)); pm = pm.filter(ImageFilter.MinFilter(2 * px + 1)); return pm.crop((px, px, px + m.width, px + m.height))
def dilate(m, px): return m.filter(ImageFilter.MaxFilter(2 * px + 1)) if px > 0 else m
def vgrad(W, H, c0, c1, y0=0, y1=None):
    y1 = y1 or H; t = np.clip((np.arange(H) - y0) / max(1, y1 - y0), 0, 1)[:, None, None]
    a = np.array(c0)[None, None, :] * (1 - t) + np.array(c1)[None, None, :] * t
    return Image.fromarray(np.repeat(a, W, axis=1).astype("uint8"), "RGB")

def rect_pts(w, h, y0=0, c=CUT, tab=None):
    pts = [(c, y0), (w - 1 - c, y0), (w - 1, y0 + c), (w - 1, h - 1 - c), (w - 1 - c, h - 1), (c, h - 1), (0, h - 1 - c), (0, y0 + c)]
    if tab:  # tab tren giua, canh xien
        tx0, tx1, tt = tab; pts = [pts[0], (tx0, y0), (tx0 + 12, 9), (tx0 + 21, 0), (tx1 - 21, 0), (tx1 - 12, 9), (tx1, y0)] + pts[1:]
    return pts

def moss(g, x, y, s, n, rnd, sx=1, sy=1):
    for _ in range(n):
        dx, dy = int(abs(rnd.gauss(0, 6))), int(abs(rnd.gauss(0, 5)))
        c = rnd.choice(MOSS); px, py = (x + sx * dx) * s, (y + sy * dy) * s
        g.rectangle((px, py, px + 2 * s - 1, py + 2 * s - 1), fill=c + (255,))

def glyph(g, x, y, s, fx=False, fy=False):
    """goc khac xoan vuong nho (rut gon tu concept): 15x15, net sang + bong toi"""
    pts = [(0, 0), (14, 0), (14, 14), (2, 14), (2, 4), (11, 4), (11, 11), (5, 11), (5, 7), (8, 7)]
    if fx: pts = [(14 - a, b) for a, b in pts]
    if fy: pts = [(a, 14 - b) for a, b in pts]
    P = [((x + a) * s + s // 2, (y + b) * s + s // 2) for a, b in pts]
    g.line([(a + s, b + s) for a, b in P], fill=(40, 35, 26, 255), width=int(1.6 * s) + 1, joint="curve")
    g.line(P, fill=(168, 156, 128, 255), width=max(1, int(1.2 * s)), joint="curve")

def panel(w, h, s, kind):
    """kind: vitals | match | quickbar | minimap | killfeed"""
    W, H = w * s, h * s; rnd = random.Random(11); u = s
    y0 = 26 if kind == "quickbar" else 0
    tab = (150, w - 151, 26) if kind == "quickbar" else None
    c = 6 if kind == "killfeed" else CUT
    m0 = poly_mask(W, H, rect_pts(w, h, y0, c, tab), s)
    tb = 3 if kind == "killfeed" else T
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    o = Image.new("RGBA", (W, H), OUTLINE[:3] + (0,)); o.putalpha(m0.point(lambda v: OUTLINE[3] if v else 0)); im.alpha_composite(o)
    m1 = erode(m0, u)
    band = vgrad(W, H, BAND_HI, BAND_LO).convert("RGB")
    nz = (np.random.RandomState(5).randn(H, W, 1) * 3.5); band = Image.fromarray(np.clip(np.asarray(band).astype("float") + nz, 0, 255).astype("uint8"), "RGB").filter(ImageFilter.GaussianBlur(0.4 * u)).convert("RGBA")
    band.putalpha(m1); im.alpha_composite(band)
    if kind != "killfeed":  # bevel: sang tren-trai, toi duoi-phai (gai da khac)
        sh = 2 * u
        lt = ImageChops.subtract(m1, ImageChops.offset(m1, sh, sh)); dk = ImageChops.subtract(m1, ImageChops.offset(m1, -sh, -sh))
        for mk, col in ((lt, (186, 174, 146, 215)), (dk, (66, 58, 44, 200))):
            ov = Image.new("RGBA", (W, H), col[:3] + (0,)); ov.putalpha(mk.point(lambda v, a=col[3]: a if v else 0)); im.alpha_composite(ov)
    g = ImageDraw.Draw(im, "RGBA")
    rv = random.Random(3)
    if kind != "killfeed":  # van nhe tren gờ
        for _ in range(int(w * h / 3500) + 4):
            x = rv.randrange(10, w - 10); yb = rv.choice((y0 + 3, y0 + 3, h - 6, h - 6)) if rv.random() < .6 else rv.randrange(y0 + 10, h - 10)
            xs_ = rv.choice((x, 3, w - 6)) if yb not in (y0 + 3, h - 6) else x
            ln = rv.randint(6, 14); g.line([(xs_ * u, yb * u), ((xs_ + (ln if yb in (y0 + 3, h - 6) else 0)) * u, (yb + (0 if yb in (y0 + 3, h - 6) else ln)) * u)], fill=(70, 62, 48, 90), width=u)
    if kind != "killfeed":   # moi noi da thua tren gờ
        for x in range(30 * u, W - 20 * u, 44 * u):
            if kind == "quickbar" and x < 150 * u or x > (w - 150) * u and kind == "quickbar": pass
            g.line([(x, (y0 + 1) * u), (x, (y0 + T) * u - 1)], fill=(80, 72, 56, 150)); g.line([(x, H - T * u), (x, H - 1 - u)], fill=(80, 72, 56, 150))
    mi = erode(m0, (1 + tb) * u)
    # long panel
    if kind == "minimap":
        a = im.getchannel("A"); im.putalpha(ImageChops.subtract(a, mi))
    else:
        base = Image.new("RGB", (W, H), FILL)
        nz2 = Image.effect_noise((W, H), 12).filter(ImageFilter.GaussianBlur(0.8 * u))
        nn = (np.asarray(nz2).astype("float") - 128) / 40 * 2.0
        base = Image.fromarray(np.clip(np.asarray(base).astype("float") + nn[..., None], 0, 255).astype("uint8"), "RGB").convert("RGBA")
        d2 = ImageDraw.Draw(base, "RGBA")
        for _ in range(int(w * h / 5000) + 2):
            x, y = rv.randrange(20, w - 20), rv.randrange(y0 + 16, h - 16); pts = [(x, y)]
            for _ in range(rv.randint(3, 5)): x += rv.randint(-9, 9); y += rv.randint(-6, 8); pts.append((x, y))
            d2.line([(a * u, b * u) for a, b in pts], fill=(48, 44, 34, 70), width=u)
        base.putalpha(mi); im.alpha_composite(base)
    g = ImageDraw.Draw(im, "RGBA")
    # ranh toi + vach sang mong phia trong
    ring = ImageChops.subtract(dilate(mi, u), mi); gr = Image.new("RGBA", (W, H), GROOVE[:3] + (0,)); gr.putalpha(ring.point(lambda v: 255 if v else 0)); im.alpha_composite(gr)
    if kind != "minimap":
        r2 = ImageChops.subtract(mi, erode(mi, u)); lt = Image.new("RGBA", (W, H), INNER_LIGHT[:3] + (0,)); lt.putalpha(r2.point(lambda v: INNER_LIGHT[3] if v else 0)); im.alpha_composite(lt)
    if kind != "minimap":   # bevel long: toi tren-trai, sang duoi-phai
        for mk, col in ((ImageChops.subtract(mi, ImageChops.offset(mi, u, u)), (30, 26, 18, 120)), (ImageChops.subtract(mi, ImageChops.offset(mi, -u, -u)), (150, 138, 112, 90))):
            ov = Image.new("RGBA", (W, H), col[:3] + (0,)); ov.putalpha(mk.point(lambda v, a=col[3]: a if v else 0)); im.alpha_composite(ov)
    g = ImageDraw.Draw(im, "RGBA")
    if kind in ("vitals", "match", "quickbar", "minimap"):
        yy0 = y0
        for (x, y) in ((tb + 3, yy0 + tb + 3), (w - tb - 3 - 15, yy0 + tb + 3), (tb + 3, h - tb - 3 - 15), (w - tb - 3 - 15, h - tb - 3 - 15)):
            if kind == "match" and False: pass
            glyph(g, x, y, s, x > w // 2, y - yy0 > (h - yy0) // 2)
        # reu it: 3 goc lon, goc con lai nho
        moss(g, 2, yy0 + 2, s, 30, rnd, 1, 1); moss(g, w - 4, yy0 + 2, s, 30, rnd, -1, 1); moss(g, 2, h - 5, s, 26, rnd, 1, -1); moss(g, w - 4, h - 5, s, 10, rnd, -1, -1)
        if kind == 'quickbar': moss(g, 151, 20, s, 14, rnd, 1, -1); moss(g, w - 153, 20, s, 14, rnd, -1, -1); moss(g, 156, 6, s, 8, rnd, 1, 1); moss(g, w - 158, 6, s, 8, rnd, -1, 1)
    if kind == "vitals":   # 2 vach khac chia 3 hang (y co dinh theo layout: hang 20-56, 64-100, 108-140)
        for y in (60, 104):
            g.rectangle((int((tb + 4) * u), y * u, int((w - tb - 5) * u), y * u + 2 * u - 1), fill=DIV_DARK)
            g.rectangle((int((tb + 4) * u), (y + 2) * u, int((w - tb - 5) * u), (y + 3) * u - 1), fill=DIV_LIGHT)
    if kind == "killfeed":
        im.putalpha(im.getchannel("A").point(lambda v: int(v * 0.82)))
    return im

def rr(n, r, fill, outline=None, ow=1):
    im = Image.new("RGBA", (n[0], n[1]), (0, 0, 0, 0)); ImageDraw.Draw(im).rounded_rectangle((0, 0, n[0] - 1, n[1] - 1), radius=r, fill=fill, outline=outline, width=ow); return im

def slot(s, selected):
    n = 60 * s; u = s; im = Image.new("RGBA", (n, n), (0, 0, 0, 0)); g = ImageDraw.Draw(im, "RGBA")
    rad = 5 * u
    g.rounded_rectangle((0, 0, n - 1, n - 1), radius=rad, fill=(30, 26, 19, 235))
    if selected:
        g.rounded_rectangle((u, u, n - 1 - u, n - 1 - u), radius=rad - u, fill=GOLD + (255,))
        g.rounded_rectangle((2 * u, 2 * u, n - 1 - 2 * u, n - 1 - 2 * u), radius=rad - 2 * u, fill=GOLD_HI + (255,))
        g.rounded_rectangle((4 * u, 4 * u, n - 1 - 4 * u, n - 1 - 4 * u), radius=rad - 3 * u, fill=SLOT_FILL + (255,))
        g.rounded_rectangle((3 * u, 3 * u, n - 1 - 3 * u, n - 1 - 3 * u), radius=rad - 2 * u, outline=(30, 26, 19, 200), width=u)
        g.rounded_rectangle((4 * u, 4 * u, n - 1 - 4 * u, n - 1 - 4 * u), radius=rad - 3 * u, fill=SLOT_FILL + (255,))
    else:
        g.rounded_rectangle((u, u, n - 1 - u, n - 1 - u), radius=rad - u, fill=SLOT_RIM_LO + (255,))
        g.rounded_rectangle((u, u, n - 1 - u, n - 1 - 2 * u), radius=rad - u, fill=SLOT_RIM_HI + (255,))
        g.rounded_rectangle((3 * u, 3 * u, n - 1 - 3 * u, n - 1 - 3 * u), radius=rad - 2 * u, fill=(30, 26, 19, 255))
        g.rounded_rectangle((4 * u, 4 * u, n - 1 - 4 * u, n - 1 - 4 * u), radius=rad - 3 * u, fill=SLOT_FILL + (255,))
    return im

def track(w, h, s, kind):
    W, H, u = w * s, h * s, s; im = Image.new("RGBA", (W, H), (0, 0, 0, 0)); g = ImageDraw.Draw(im, "RGBA"); r = min(H // 2 - 1, 5 * u)
    if kind == "gold":    # thanh Adrenaline: ruot toi, vien vang
        g.rounded_rectangle((0, 0, W - 1, H - 1), radius=r, fill=GOLD + (255,)); g.rounded_rectangle((u, u, W - 1 - u, H - 1 - u), radius=max(1, r - u), fill=(214, 168, 56, 255))
        g.rounded_rectangle((2 * u, 2 * u, W - 1 - 2 * u, H - 1 - 2 * u), radius=max(1, r - 2 * u), fill=(24, 21, 15, 255))
    else:                 # thanh HP / dan: ruot toi, vien da sang
        g.rounded_rectangle((0, 0, W - 1, H - 1), radius=r, fill=(28, 24, 17, 255)); g.rounded_rectangle((u, u, W - 1 - u, H - 1 - u), radius=max(1, r - u), fill=SLOT_RIM_HI + (255,))
        g.rounded_rectangle((2 * u, 2 * u, W - 1 - 2 * u, H - 1 - 2 * u), radius=max(1, r - 2 * u), fill=(26, 23, 16, 255))
    return im

def fill_white(s):
    W, H = 64 * s, 16 * s; im = Image.new("RGBA", (W, H), (0, 0, 0, 0)); g = ImageDraw.Draw(im, "RGBA")
    g.rounded_rectangle((0, 0, W - 1, H - 1), radius=4 * s, fill=(255, 255, 255, 255))
    g.rounded_rectangle((s, s, W - 1 - s, 6 * s), radius=3 * s, fill=(255, 255, 255, 255))
    g.rectangle((3 * s, 2 * s, W - 4 * s, 3 * s), fill=(255, 255, 255, 255))   # (tint do/vang trong code; khong sang rieng)
    return im

def shadow_none(): pass

# ---------------- icon pixel (anh muc tieu: tim do, ao giap, mu hiep si, tia set vang) ----------------
PAL = {"k": (44, 33, 22), "R": (214, 48, 58), "r": (150, 28, 42), "L": (255, 128, 128), "G": (236, 186, 76), "g": (236, 186, 76),
       "T": (214, 200, 160), "t": (196, 182, 142), "S": (52, 42, 30), "l": (236, 224, 188), "H": (204, 200, 188), "h": (118, 114, 104), "v": (36, 29, 20),
       "Y": (250, 214, 70), "y": (230, 180, 62), "C": (240, 232, 205), "c": (196, 186, 156), "W": (250, 250, 240)}
ART = {
 "hp": ["..GGGGG....GGGGG..", ".GRRRRRG..GRRRRRG.", "GRRLLLRRGGRRRRRRRG", "GRLLLRRRRRRRRRRRrG", "GRLLRRRRRRRRRRRRrG", "GRRRRRRRRRRRRRRrrG", ".GRRRRRRRRRRRRrrG.", ".GRRRRRRRRRRRrrrG.", "..GRRRRRRRRRRrrG..", "...GRRRRRRRRrrG...", "....GRRRRRRrrG....", ".....GRRRRrrG.....", "......GRRrrG......", ".......GGGG......."],
 "armor": ["..kkkkk....kkkkk..", ".kTTTTTkkkkTTTTTk.", "kTTlTTTTSSTTTTlTTk", "kTTlTTTTSSTTTTlTTk", "kkkkkTTTSSTTTkkkkk", "...kTTTTSSTTTTk...", "...kTTTTSSTTTTk...", "...kTTTTSSTTTTk...", "...kTTTTSSTTTTk...", "...kTTTTSSTTTTk...", "...kTTTTSSTTTTk...", "...kTTTTSSTTTTk...", "...kTTTTTTTTTTk...", "...kkkkkkkkkkkk..."],
 "helmet": ["....kkkkkkkk....", "...kHHHHHHHHk...", "..kHHHHggHHHHk..", ".kHHHHHggHHHHHk.", ".kkkkkkkkkkkkkk.", ".kHHHvHggHvHHHk.", ".kHHHvHggHvHHHk.", ".kHHHvHggHvHHHk.", ".kHHHvHggHvHHHk.", "..kHHHHggHHHHk..", "..kHHHHggHHHHk..", "...kHHHggHHHk...", "....kkHggHkk....", ".....kkkkkk....."],
 "adrenaline": ["......kkkk......", ".....kYYYYk.....", "....kYYYYk......", "...kYYYYk.......", "..kYYYYYYYYk....", "...kkkYYYYk.....", "......kYYk......", ".....kYYk.......", "....kYYk........", "....kYk.........", "....kk.........."],
 "bullet": ["..kkk..", ".kYYYk.", ".kYWYk.", ".kYYYk.", "kYYYYYk", "kYYYYYk", "kyyyyyk", "kkkkkkk"],
 "alive": ["...kkk...", "..kCCCk..", "..kCCCk..", "...kkk...", "..kCCCk..", ".kCCCCCk.", ".kCCCCCk.", ".kCCkCCk.", ".kCCkCCk.", "..kckck.."],
 "zone-timer": [".kkkkkkkk.", ".kCCCCCCk.", "..kCCCCk..", "...kCCk...", "....kk....", "...kckk...", "..kcCCck..", ".kcCCCCck.", ".kkkkkkkk."],
 "kill": ["..kkkkkk..", ".kCCCCCCk.", "kCCCCCCCCk", "kCvvCCvvCk", "kCvvCCvvCk", "kCCCkkCCCk", ".kCCCCCCk.", "..kCkCkCk.", "..kkkkkkk."],
}
CANVAS = {"hp": 40, "armor": 40, "helmet": 40, "helmet-alt": 40, "adrenaline": 36, "alive": 36, "zone-timer": 36, "kill": 32}

def icon_png(name, canvas, s):
    rows = ART["helmet" if name == "helmet-alt" else name]; w = max(len(r) for r in rows); h = len(rows)
    sc = 2 * s; im = Image.new("RGBA", (canvas * s, canvas * s), (0, 0, 0, 0)); ox = (canvas * s - w * sc) // 2; oy = (canvas * s - h * sc) // 2
    g = ImageDraw.Draw(im)
    for y, r in enumerate(rows):
        for x, ch in enumerate(r):
            if ch != ".": g.rectangle((ox + x * sc, oy + y * sc, ox + (x + 1) * sc - 1, oy + (y + 1) * sc - 1), fill=PAL[ch] + (255,))
    return im

RECT = {"hud_panel_vitals": (311, 173, "vitals"), "hud_panel_quickbar": (592, 126, "quickbar"), "hud_panel_match": (240, 89, "match"),
        "hud_minimap_frame": (240, 240, "minimap"), "hud_killfeed_plate": (256, 135, "killfeed")}

if __name__ == "__main__":
    for s, suf in ((1, ""), (2, "@2x")):
        for nm, (w, h, k) in RECT.items(): panel(w, h, s, k).save(ROOT / f"{nm}{suf}.png")
        slot(s, False).save(ROOT / f"hud_slot_empty{suf}.png"); slot(s, True).save(ROOT / f"hud_slot_selected{suf}.png")
        track(164, 21, s, "plain").save(ROOT / f"hud_bar_track_hp{suf}.png"); track(164, 12, s, "gold").save(ROOT / f"hud_bar_track_thin{suf}.png")
        track(220, 16, s, "plain").save(ROOT / f"hud_ammo_plate{suf}.png"); fill_white(s).save(ROOT / f"hud_bar_fill_white{suf}.png")
        for n, cv in CANVAS.items(): icon_png(n, cv, s).save(ICON / f"icon_{n}{suf}.png")
        icon_png("bullet", 18, s).save(ICON / f"icon_ammo{suf}.png") if False else None
    # icon dan rieng (vien trong tab): 16x16
    for s, suf in ((1, ""), (2, "@2x")): icon_png("bullet", 16, s).save(ICON / f"icon_ammo{suf}.png")
    meta = {}
    for nm, (w, h, k) in RECT.items():
        meta[nm] = {"size": [w, h], "border_LRTB": [16, 16, 16, 16] if k != "quickbar" else None, "slice": k != "quickbar"}
    meta["hud_slot_empty"] = {"size": [60, 60], "border_LRTB": [18] * 4}; meta["hud_slot_selected"] = meta["hud_slot_empty"]
    meta["hud_bar_track_hp"] = {"size": [164, 21], "border_LRTB": [10, 10, 10, 10]}; meta["hud_bar_track_thin"] = {"size": [164, 12], "border_LRTB": [6, 6, 6, 6]}
    meta["hud_ammo_plate"] = {"size": [220, 16], "border_LRTB": [8, 8, 8, 8]}; meta["hud_bar_fill_white"] = {"size": [64, 16], "border_LRTB": [5, 5, 5, 5]}
    json.dump(meta, open(ROOT / "meta.json", "w", encoding="utf-8"), indent=1, ensure_ascii=False)
    print("ok")
