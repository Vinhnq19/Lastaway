import sys, pathlib
from PIL import Image, ImageDraw, ImageFont
OUT = pathlib.Path(r"D:\Lastaway\projects\lastaway\art\inbox\T-015")
ICON = pathlib.Path(r"D:\Lastaway\projects\lastaway\art\inbox\hud-icons\png")
BASE = pathlib.Path(r"D:\Lastaway\projects\lastaway\qa\T-013\01-dau-tran-1920x1080.png")
FB = r"D:\Lastaway\Assets\_JungleMaze\Art\Fonts\BeVietnamPro-Bold.ttf"
F = lambda n: ImageFont.truetype(FB, n)
im = Image.open(BASE).convert("RGBA")
spr = lambda n: Image.open(OUT / f"{n}.png").convert("RGBA")
ico = lambda n, s: Image.open(ICON / f"icon_{n}@2x.png").convert("RGBA").resize((s, s), Image.LANCZOS)
def patch(rect, dx, dy):
    x, y, w, h = rect; im.paste(im.crop((x + dx, y + dy, x + dx + w, y + dy + h)), (x, y))
def tint(white, rgb):
    a = white.getchannel("A"); out = Image.new("RGBA", white.size, rgb + (255,)); out.putalpha(a); return out
def bar(x, y, w, h, frac, color, thin=False):
    tr = spr("hud_bar_track_thin" if thin else "hud_bar_track_hp").resize((w, h), Image.LANCZOS); im.alpha_composite(tr, (x, y))
    inset = 2 if thin else 3; fw = max(1, int((w - 2 * inset) * frac)); fh = h - 2 * inset
    im.alpha_composite(tint(spr("hud_bar_fill_white").resize((fw, fh), Image.LANCZOS), color), (x + inset, y + inset))
def text(xy, s, size, fill=(237, 242, 238), anchor="la", stroke=0):
    ImageDraw.Draw(im).text(xy, s, font=F(size), fill=fill, anchor=anchor, stroke_width=stroke, stroke_fill=(10, 12, 12))

VIT = (36, 872, 311, 173); QB = (664, 918, 592, 126); MS = (1644, 288, 240, 89); MM = (1644, 36, 240, 240); KF = (36, 36, 256, 135)
patch(VIT, 0, -250); patch(QB, 0, -270); patch(MS, -260, 0); patch(KF, 0, 200)
# minimap: overlay duoi vong bo va marker (che nhung pixel vang/do/trang), roi khung
ov = spr("hud_minimap_overlay"); cp = im.crop((MM[0], MM[1], MM[0] + 240, MM[1] + 240)).convert("RGB").load(); oa = ov.load()
for yy in range(240):
    for xx in range(240):
        r_, g_, b_ = cp[xx, yy]
        if (r_ > 165 and g_ > 150 and b_ < 90) or (r_ > 150 and g_ < 60 and b_ < 60) or (r_ > 220 and g_ > 200 and b_ > 180): oa[xx, yy] = (0, 0, 0, 0)
im.alpha_composite(ov, (MM[0], MM[1]))
for n, r in (("hud_panel_vitals", VIT), ("hud_panel_quickbar", QB), ("hud_panel_match", MS), ("hud_minimap_frame", MM), ("hud_killfeed_plate", KF)):
    im.alpha_composite(spr(n), (r[0], r[1]))

def ns(img, bx, by, size):
    w, h = img.size; W, H = size; o = Image.new("RGBA", size, (0, 0, 0, 0))
    xs = [(0, bx, 0, bx), (bx, w - bx, bx, W - bx), (w - bx, w, W - bx, W)]; ys = [(0, by, 0, by), (by, h - by, by, H - by), (h - by, h, H - by, H)]
    for a0, a1, d0, d1 in xs:
        for c0, c1, e0, e1 in ys:
            if a1 > a0 and c1 > c0 and d1 > d0 and e1 > e0: o.paste(img.crop((a0, c0, a1, c1)).resize((d1 - d0, e1 - e0), Image.NEAREST), (d0, e0))
    return o
def ic(n, xy):
    im.alpha_composite(Image.open(ICON / f"icon_{n}.png").convert("RGBA"), xy)
def bar2(x, y, w, h, frac, color, gold=False):
    tr = spr("hud_bar_track_thin" if gold else "hud_bar_track_hp"); b = 6 if gold else 10
    im.alpha_composite(ns(tr, b, b, (w, h)), (x, y))
    inset = 3 if gold else 3; fw = max(0, int((w - 2 * inset) * frac)); fh = h - 2 * inset
    if fw > 0: im.alpha_composite(tint(spr("hud_bar_fill_white").resize((fw, fh), Image.NEAREST), color), (x + inset, y + inset))
TXT = (237, 242, 238)
x0, y0 = VIT[0] + 40, VIT[1] + 20
ic("hp", (x0 - 2, y0 - 2)); bar2(x0 + 44, y0 + 6, 134, 24, 1.0, (200, 58, 65)); text((VIT[0] + 311 - 40, y0 + 18), "100", 22, TXT, "rm")
y1 = y0 + 44
ic("armor", (x0 - 2, y1 - 2)); text((x0 + 44, y1 + 18), "0", 20, TXT, "lm")
ic("helmet", (x0 + 108, y1 - 2)); text((x0 + 154, y1 + 18), "0", 20, TXT, "lm")
y2 = y1 + 44
ic("adrenaline", (x0, y2)); bar2(x0 + 44, y2 + 10, 134, 14, 0.0, (245, 197, 66), gold=True); text((VIT[0] + 311 - 40, y2 + 17), "0", 18, TXT, "rm")
# --- Quick bar: tab dan tren, o 60 px, so phim DUOI o ---
ic("ammo", (QB[0] + 166, QB[1] + 7)); im.alpha_composite(ns(spr("hud_ammo_plate"), 8, 8, (220, 16)), (QB[0] + 188, QB[1] + 8))
xs = [QB[0] + 41 + i * 63 for i in range(4)] + [QB[0] + 41 + 261 + i * 63 for i in range(4)]
for i, x in enumerate(xs):
    im.alpha_composite(spr("hud_slot_selected" if i == 0 else "hud_slot_empty"), (x, QB[1] + 36))
    text((x + 30, QB[1] + 104), str(i + 1), 14, (255, 220, 110) if i == 0 else (240, 216, 140), "mm", stroke=2)
# --- Match status ---
ic("alive", (MS[0] + 30, MS[1] + 26)); text((MS[0] + 70, MS[1] + 44), "1/1", 22, TXT, "lm")
ic("zone-timer", (MS[0] + 120, MS[1] + 26)); text((MS[0] + 160, MS[1] + 44), "1:22", 22, (241, 231, 200), "lm")
for i, t in enumerate(["Hao ha Linh", "Vinh ha Mai", "Vu ha Son"]):
    text((KF[0] + 16, KF[1] + 24 + i * 32), t, 17, TXT if i == 0 else (214, 218, 214))
im.convert("RGB").save(OUT / "dung-thu-hud-A.png")
print("ok")
