import sys, pathlib
from PIL import Image, ImageDraw, ImageFont
ROOT = pathlib.Path(r"D:\Lastaway\projects\lastaway\art\inbox\T-015")
VAR = sys.argv[1]; OUT = ROOT / "lan3" / VAR
SHADOW = (OUT / "hud_panel_shadow.png").exists()
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
def ns(img, b, size):
    w, h = img.size; W, H = size; o = Image.new("RGBA", size, (0, 0, 0, 0))
    xs = [(0, b, 0, b), (b, w - b, b, W - b), (w - b, w, W - b, W)]; ys = [(0, b, 0, b), (b, h - b, b, H - b), (h - b, h, H - b, H)]
    for a0, a1, d0, d1 in xs:
        for c0, c1, e0, e1 in ys:
            o.paste(img.crop((a0, c0, a1, c1)).resize((d1 - d0, e1 - e0), Image.LANCZOS), (d0, e0))
    return o
if SHADOW:
    sh = spr("hud_panel_shadow"); m = 24
    for r in (VIT, QB, MS, MM, KF):
        im.alpha_composite(ns(sh, 32, (r[2] + 2 * m + 16, r[3] + 2 * m + 16)), (r[0] - m - 8, r[1] - m - 8 + 4))
for n, r in (("hud_panel_vitals", VIT), ("hud_panel_quickbar", QB), ("hud_panel_match", MS), ("hud_minimap_frame", MM), ("hud_killfeed_plate", KF)):
    im.alpha_composite(spr(n), (r[0], r[1]))

# --- Vitals: lề nội dung L/R 40, T/B 20 ---
x0, y0 = VIT[0] + 40, VIT[1] + 20
im.alpha_composite(ico("hp", 36), (x0, y0)); bar(x0 + 44, y0 + 6, 104, 24, 1.0, (229, 72, 77)); text((VIT[0] + 311 - 40, y0 + 18), "100", 24, anchor="rm")
y1 = y0 + 44
im.alpha_composite(ico("armor", 36), (x0, y1)); text((x0 + 44, y1 + 18), "0", 20, (166, 187, 183), "lm")
im.alpha_composite(ico("helmet", 36), (x0 + 110, y1)); text((x0 + 154, y1 + 18), "0", 20, (166, 187, 183), "lm")
y2 = y1 + 44
im.alpha_composite(ico("adrenaline", 32), (x0 + 2, y2 + 2)); bar(x0 + 44, y2 + 10, 104, 14, 0.0, (245, 197, 66), thin=True); text((VIT[0] + 311 - 40, y2 + 17), "0", 18, (166, 187, 183), "rm")
# --- Quick bar: lề L/R 40, ô 60 px, khe 3, giữa nhóm 12 ---
text((QB[0] + 40, QB[1] + 31), "Bare Hands", 16, anchor="lm")
xs = [QB[0] + 41 + i * 63 for i in range(4)] + [QB[0] + 41 + 261 + i * 63 for i in range(4)]
for i, x in enumerate(xs):
    im.alpha_composite(spr("hud_slot_selected" if i == 0 else "hud_slot_empty"), (x, QB[1] + 45))
    text((x + 6, QB[1] + 46), str(i + 1), 15, (241, 231, 200), "la", stroke=2)
# --- Match status: lề L/R 32 ---
im.alpha_composite(ico("alive", 24), (MS[0] + 32, MS[1] + 32)); text((MS[0] + 62, MS[1] + 44), "1/1", 22, anchor="lm")
im.alpha_composite(ico("zone-timer", 24), (MS[0] + 118, MS[1] + 32)); text((MS[0] + 148, MS[1] + 44), "1:22", 22, (241, 231, 200), "lm")
# --- Killfeed: lề L 16, T 14 ---
for i, t in enumerate(["Hao ha Linh", "Vinh ha Mai", "Vu ha Son"]):
    text((KF[0] + 16, KF[1] + 24 + i * 32), t, 17, (237, 242, 238) if i == 0 else (214, 218, 214))
im.convert("RGB").save(OUT / "full-1920x1080.png")
print("ok")
