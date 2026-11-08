"""T-015 luot 5: dung thu 1920x1080 theo rect/vi tri do tu concept (so-hien-hanh: lam giong anh ref)."""
import sys, pathlib
from PIL import Image, ImageDraw, ImageFont
OUT = pathlib.Path(r"D:\Lastaway\projects\lastaway\art\inbox\T-015")
ICON = pathlib.Path(r"D:\Lastaway\projects\lastaway\art\inbox\hud-icons\png")
BASE = pathlib.Path(r"D:\Lastaway\projects\lastaway\qa\T-013\01-dau-tran-1920x1080.png")
FB = r"D:\Lastaway\Assets\_JungleMaze\Art\Fonts\BeVietnamPro-Bold.ttf"
F = lambda n: ImageFont.truetype(FB, n)
im = Image.open(BASE).convert("RGBA")
spr = lambda n: Image.open(OUT / f"{n}.png").convert("RGBA")
def patch(rect, dx, dy):
    x, y, w, h = rect; im.paste(im.crop((x + dx, y + dy, x + dx + w, y + dy + h)), (x, y))
def tint(white, rgb):
    a = white.getchannel("A"); out = Image.new("RGBA", white.size, rgb + (255,)); out.putalpha(a); return out
def text(xy, s, size, fill=(237, 242, 238), anchor="lm", stroke=0):
    ImageDraw.Draw(im).text(xy, s, font=F(size), fill=fill, anchor=anchor, stroke_width=stroke, stroke_fill=(10, 12, 12))
def ic(n, cx, cy):
    i = Image.open(ICON / f"icon_{n}@2x.png").convert("RGBA"); i = i.resize((i.width // 2, i.height // 2), Image.LANCZOS)
    im.alpha_composite(i, (round(cx - i.width / 2), round(cy - i.height / 2)))
def ns(img, bx, by, size):
    w, h = img.size; W, H = size; o = Image.new("RGBA", size, (0, 0, 0, 0))
    xs = [(0, bx, 0, bx), (bx, w - bx, bx, W - bx), (w - bx, w, W - bx, W)]; ys = [(0, by, 0, by), (by, h - by, by, H - by), (h - by, h, H - by, H)]
    for a0, a1, d0, d1 in xs:
        for c0, c1, e0, e1 in ys:
            if a1 > a0 and c1 > c0 and d1 > d0 and e1 > e0: o.paste(img.crop((a0, c0, a1, c1)).resize((d1 - d0, e1 - e0), Image.NEAREST), (d0, e0))
    return o
def bar(x, y, w, h, frac, color, gold=False):
    tr = spr("hud_bar_track_thin" if gold else "hud_bar_track_hp"); b = 8 if gold else 10
    im.alpha_composite(tr.resize((w, h), Image.LANCZOS), (x, y))
    inset = 4; fw = max(0, int((w - 2 * inset) * frac)); fh = h - 2 * inset
    if fw > 0: im.alpha_composite(tint(spr("hud_bar_fill_white").resize((fw, fh), Image.NEAREST), color), (x + inset, y + inset))

# rect 1920x1080 do tu concept
VIT = (30, 857, 422, 206); QB = (668, 950, 593, 112); MM = (1577, 30, 318, 316); MS = (1577, 350, 318, 90); KF = (16, 44, 270, 130)
patch(VIT, 0, -250); patch((668, 904, 593, 159), 0, -270); patch((1577, 30, 318, 420), -400, 120); patch(KF, 0, 200)
cc = Image.open("D:/Lastaway/projects/lastaway/art/brief/T-015/concept-toan-man-huy-07-11.png").convert("RGBA")
im.alpha_composite(cc.crop((1394, 52, 1629, 285)).resize((269, 268), Image.LANCZOS), (MM[0] + 24, MM[1] + 24))  # noi dung ban do lay tu concept (mock)
for n, r in (("hud_panel_vitals", VIT), ("hud_panel_quickbar", QB), ("hud_panel_match", MS), ("hud_minimap_frame", MM), ("hud_killfeed_plate", KF)):
    im.alpha_composite(spr(n), (r[0], r[1]))
TXT = (237, 242, 238)
# Vitals (toa do tuong doi goc panel)
vx, vy = VIT[0], VIT[1]
ic("hp", vx + 64, vy + 42); bar(vx + 91, vy + 26, 224, 32, 1.0, (200, 58, 65)); text((vx + 380, vy + 41), "100", 30, TXT, "rm")
ic("armor", vx + 65, vy + 100); text((vx + 103, vy + 101), "0", 28, TXT)
ic("helmet", vx + 245, vy + 100); text((vx + 280, vy + 101), "0", 28, TXT)
ic("adrenaline", vx + 65, vy + 158); bar(vx + 95, vy + 146, 242, 24, 0.0, (245, 197, 66), True); text((vx + 366, vy + 158), "0", 28, TXT, "mm")
# Quick bar
qx, qy = QB[0], QB[1]
for i in range(8):
    x = qx + (50 + 60 * i if i < 4 else 310 + 60 * (i - 4)); y = qy + 29
    im.alpha_composite(spr("hud_slot_selected" if i == 0 else "hud_slot_empty"), (x, y))
    if i == 2: ic("adrenaline", x + 27, y + 27)  # o co icon (mau)
    text((x + 50, y + 50), str(i + 1), 16, (255, 220, 110), "rb", stroke=3)
# Match
mx, my = MS[0], MS[1]
ic("alive", mx + 71, my + 45); text((mx + 97, my + 45), "1/1", 22, TXT)
ImageDraw.Draw(im).line([(mx + 155, my + 28), (mx + 155, my + 62)], fill=(190, 180, 150, 255), width=2)
ic("zone-timer", mx + 185, my + 45); text((mx + 211, my + 45), "1:21", 22, (241, 231, 200))
for i, t in enumerate(["Hao ha Linh", "Vinh ha Mai", "Vu ha Son"]):
    text((37, 53 + i * 35), t, 19, TXT if i == 0 else (214, 218, 214))
im.convert("RGB").save(OUT / "dung-thu-hud-A.png")
# so sanh: concept tren, dung thu duoi; chong 50%; hai nua
c = Image.open(r"D:\Lastaway\projects\lastaway\art\brief\T-015\concept-toan-man-huy-07-11.png").convert("RGB").resize((1920, 1080), Image.LANCZOS)
d = im.convert("RGB")
o = Image.new("RGB", (1920, 2160), (20, 20, 20)); o.paste(c, (0, 0)); o.paste(d, (0, 1080)); o.save(OUT / "so-sanh-concept.png")
Image.blend(c, d, .5).save(OUT / "chong-concept-50.png")
z = im.convert("RGB").crop((QB[0] - 20, QB[1] - 20, QB[0] + QB[2] + 20, QB[1] + QB[3] + 20)); z.resize((z.width * 2, z.height * 2), Image.NEAREST).save(OUT / "quickbar-2x.png")
import numpy as np
mmz = im.convert("RGB").crop((MM[0], MM[1], MM[0] + MM[2], MM[1] + MM[3])); a = np.asarray(mmz).astype(int)
yel = (a[..., 0] > 200) & (a[..., 1] > 180) & (a[..., 2] < 140) & (a[..., 0] - a[..., 2] > 90); yy, xx = np.where(yel[30:-30, 30:-30]); yy += 30; xx += 30
cxm, cym = (xx.min() + xx.max()) / 2, (yy.min() + yy.max()) / 2; print("tam vong bo (trong sprite)", cxm, cym, "tam lo 158.0 157.5, tam khung 159 158")
zm = mmz.resize((mmz.width * 2, mmz.height * 2), Image.NEAREST); d = ImageDraw.Draw(zm)
d.line([(318, 0), (318, zm.height)], fill=(255, 0, 255), width=1); d.line([(0, 316), (zm.width, 316)], fill=(255, 0, 255), width=1)   # tam khung
d.line([(316, 0), (316, zm.height)], fill=(0, 255, 255), width=1); d.line([(0, 315), (zm.width, 315)], fill=(0, 255, 255), width=1)       # tam lo
d.ellipse([cxm * 2 - 4, cym * 2 - 4, cxm * 2 + 4, cym * 2 + 4], outline=(255, 0, 0), width=2)                                           # tam vong bo
zm.save(OUT / "minimap-2x-tam.png")
z2 = im.convert("RGB").crop((VIT[0] - 10, VIT[1] - 10, VIT[0] + VIT[2] + 10, VIT[1] + VIT[3] + 10)); z2.resize((z2.width * 2, z2.height * 2), Image.NEAREST).save(OUT / "vitals-2x.png")
print("ok")
