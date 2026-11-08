import sys, json, pathlib
sys.path.insert(0, str(pathlib.Path(__file__).parent))
from extract_t015 import *
from PIL import ImageFilter, ImageDraw
OUT = pathlib.Path(r"D:\Lastaway\projects\lastaway\art\inbox\T-015")

def lin(c):
    c /= 255; return c / 12.92 if c <= .03928 else ((c + .055) / 1.055) ** 2.4
def lum(p): return .2126 * lin(p[0]) + .7152 * lin(p[1]) + .0722 * lin(p[2])

P, _ = pieces("v2/sheet-panels2a"); S, _ = pieces("sheet-slots")
large, wide, small, mini, kill = P
slot_e, slot_s, bar_t, bar_f, ammo = S
meta = {}

def is_slab(p):  # da xam toi cua long panel: toi, it bao hoa mau
    r, g, b, a = p
    return a > 0 and lum(p) < 0.12 and abs(r - b) < 22 and max(r, g, b) > 20

def darken(img, f):
    px = img.load(); w, h = img.size
    for y in range(h):
        for x in range(w):
            p = px[x, y]
            if is_slab(p): px[x, y] = (int(p[0] * f), int(p[1] * f), int(p[2] * f), p[3])
    return img

def sharpen(img, s):
    r, g, bb, a = img.split(); rgb = Image.merge("RGB", (r, g, bb)).filter(ImageFilter.UnsharpMask(radius=1.0 * s, percent=90, threshold=3))
    r, g, bb = rgb.split(); return Image.merge("RGBA", (r, g, bb, a))

def p95_lum(img):
    v = sorted(lum(p) for p in img.getdata() if is_slab(p)); return v[int(len(v) * .95)], v[len(v) // 2]

# 1) tinh he so toi cho long da: p95 do sang <= 0.045 (chu muted/icon tim >= 3:1 va 4.5:1)
probe, _b = nine(large, 173 / large.size[1], (311, 173), (80, 80, 80, 80))
p95, med = p95_lum(probe); F = min(1.0, (0.045 / p95) ** 0.5 * 0.98) * 0.81  # 0.81: icon do E5484D can >= 3:1
print("p95", round(p95, 4), "median", round(med, 4), "factor", round(F, 3))

def emit(name, im, k, size, border_src, note, alpha=None, dark=True):
    for s, suf in ((1, ""), (2, "@2x")):
        tw, th = size[0] * s, size[1] * s
        out, b = nine(im, k * s, (tw, th), border_src)
        if dark: out = darken(out, F)
        out = sharpen(out, s)
        if alpha is not None:
            a = out.getchannel("A").point(lambda v: int(v * alpha)); out.putalpha(a)
        out.save(OUT / f"{name}{suf}.png")
        if s == 1: meta[name] = {"size": [tw, th], "border_LRTB": [b[0], b[1], b[2], b[3]], "note": note}
        else: meta[name]["border_LRTB_2x"] = [b[0], b[1], b[2], b[3]]

emit("hud_panel_vitals", large, 173 / large.size[1], (311, 173), (80, 80, 80, 80), "PANEL-LARGE v2")
emit("hud_panel_quickbar", wide, 126 / wide.size[1], (592, 126), (80, 76, 80, 76), "PANEL-WIDE v2")
emit("hud_panel_match", small, 89 / small.size[1], (240, 89), (72, 68, 72, 68), "PANEL-SMALL v2")
emit("hud_minimap_frame", mini, 240 / mini.size[1], (240, 240), (80, 80, 80, 80), "MINIMAP-FRAME v2 (hole transparent)", dark=False)
emit("hud_killfeed_plate", kill, 135 / kill.size[1], (256, 135), (30, 30, 30, 30), "KILLFEED-PLATE v2 (70% alpha)", alpha=0.72)
# thanh va ammo: giu tu lan 1 (nen toi rãnh)
emit("hud_bar_track_hp", bar_t, 21 / bar_t.size[1], (164, 21), (110, 40, 110, 40), "BAR-TRACK", dark=False)
emit("hud_bar_track_thin", bar_t, 12 / bar_t.size[1], (164, 12), (110, 40, 110, 40), "BAR-TRACK thin", dark=False)
emit("hud_bar_fill_white", bar_f, 16 / bar_f.size[1], (64, 16), (24, 20, 24, 20), "BAR-FILL-WHITE (tint)", dark=False)
emit("hud_ammo_plate", ammo, 36 / ammo.size[1], (220, 36), (110, 40, 110, 40), "AMMO-PLATE", dark=False)

# 2) o quick bar 60 px: vien da + long toi co van da (lay tu long panel)
tex = Image.open(OUT / "hud_panel_vitals@2x.png").convert("RGBA").crop((140, 130, 140 + 160, 130 + 160))
def draw_slot(s, selected):
    n = 60 * s; u = s
    im = Image.new("RGBA", (n, n), (0, 0, 0, 0)); g = ImageDraw.Draw(im)
    rr = lambda box, fill: g.rectangle(box, fill=fill)
    rr((0, 0, n - 1, n - 1), (16, 18, 17, 255))
    rim = (198, 174, 120, 255) if selected else (122, 112, 94, 255)
    hi = (236, 212, 156, 255) if selected else (160, 148, 126, 255)
    lo = (150, 124, 70, 255) if selected else (82, 76, 66, 255)
    rr((u, u, n - 1 - u, n - 1 - u), rim)
    rr((u, u, n - 1 - u, 3 * u - 1), hi); rr((u, u, 3 * u - 1, n - 1 - u), hi)
    rr((u, n - 3 * u, n - 1 - u, n - 1 - u), lo); rr((n - 3 * u, u, n - 1 - u, n - 1 - u), lo)
    inner = n - 10 * u
    t = tex.resize((inner, inner), Image.LANCZOS) if s == 1 else tex.crop((0, 0, 160, 160)).resize((inner, inner), Image.LANCZOS)
    t = Image.eval(t.convert("RGB"), lambda v: int(v * 0.75)).convert("RGBA")
    rr((4 * u, 4 * u, n - 1 - 4 * u, n - 1 - 4 * u), (16, 18, 17, 255))
    im.paste(t, (5 * u, 5 * u))
    g.rectangle((5 * u, 5 * u, n - 1 - 5 * u, n - 1 - 5 * u), outline=(14, 16, 16, 120))
    if selected:
        for i in range(3):
            g.rectangle((5 * u + i * u, 5 * u + i * u, n - 1 - 5 * u - i * u, n - 1 - 5 * u - i * u), outline=(245, 197, 66, 95 - i * 28))
    for (x, y) in ((0, 0), (n - 1, 0), (0, n - 1), (n - 1, n - 1)):
        for dx in range(u):
            for dy in range(u):
                im.putpixel((x - dx if x else x + dx, y - dy if y else y + dy), (0, 0, 0, 0))
    return im
for suf, s in (("", 1), ("@2x", 2)):
    draw_slot(s, False).save(OUT / f"hud_slot_empty{suf}.png"); draw_slot(s, True).save(OUT / f"hud_slot_selected{suf}.png")
for nme in ("hud_slot_empty", "hud_slot_selected"): meta[nme] = {"size": [60, 60], "border_LRTB": [18, 18, 18, 18], "border_LRTB_2x": [36, 36, 36, 36], "note": "SLOT 60 px procedural + van da"}

# 3) overlay minimap (nam duoi vong bo)
def minimap_overlay(s):
    n = 240 * s; im = Image.new("RGBA", (n, n), (0, 0, 0, 0)); px = im.load(); edge = 24 * s
    for y in range(n):
        for x in range(n):
            t = max(0.0, 1 - min(x, y, n - 1 - x, n - 1 - y) / edge); a = 0.50 + 0.20 * t * t
            px[x, y] = (12, 20, 23, int(255 * a))
    return im
for suf, s in (("", 1), ("@2x", 2)): minimap_overlay(s).save(OUT / f"hud_minimap_overlay{suf}.png")
meta["hud_minimap_overlay"] = {"size": [240, 240], "border_LRTB": [0, 0, 0, 0], "note": "N3 overlay"}
meta["_darken_factor"] = round(F, 4)
(OUT / "meta.json").write_text(json.dumps(meta, ensure_ascii=False, indent=1), encoding="utf-8")
for k, v in meta.items():
    if k.startswith("_"): print(k, v)
    else: print(k, v["size"], v["border_LRTB"])
