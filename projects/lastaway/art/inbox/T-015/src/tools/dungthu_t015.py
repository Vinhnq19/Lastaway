import sys, pathlib
from PIL import Image, ImageDraw, ImageFont
OUT = pathlib.Path(r"D:\Lastaway\projects\lastaway\art\inbox\T-015")
ICON = pathlib.Path(r"D:\Lastaway\projects\lastaway\art\inbox\hud-icons\png")
BASE = pathlib.Path(r"D:\Lastaway\projects\lastaway\qa\T-013\01-dau-tran-1920x1080.png")
FB = r"D:\Lastaway\Assets\_JungleMaze\Art\Fonts\BeVietnamPro-Bold.ttf"
def F(n): return ImageFont.truetype(FB, n)
im = Image.open(BASE).convert("RGBA")
def spr(n): return Image.open(OUT / f"{n}.png").convert("RGBA")
def ico(n, s): return Image.open(ICON / f"icon_{n}@2x.png").convert("RGBA").resize((s, s), Image.LANCZOS)
def patch(rect, dx, dy):
    x, y, w, h = rect
    im.paste(im.crop((x + dx, y + dy, x + dx + w, y + dy + h)), (x, y))
def tint(white, rgb):
    r, g, b, a = white.split(); out = Image.new("RGBA", white.size, rgb + (255,)); out.putalpha(a); return out
def bar(x, y, w, h, frac, color, thin=False):
    tr = spr("hud_bar_track_thin" if thin else "hud_bar_track_hp").resize((w, h), Image.LANCZOS); im.alpha_composite(tr, (x, y))
    inset = 3 if not thin else 2
    fw = max(1, int((w - 2 * inset) * frac)); fh = h - 2 * inset
    fill = tint(spr("hud_bar_fill_white").resize((fw, fh), Image.LANCZOS), color); im.alpha_composite(fill, (x + inset, y + inset))
d = ImageDraw.Draw(im)
def text(xy, s, size, fill=(237, 242, 238), anchor="la"):
    d.text(xy, s, font=F(size), fill=fill, anchor=anchor)

# 1) cover old panels with world from a shifted region (mock only)
VIT = (36, 872, 311, 173); QB = (664, 918, 592, 126); MS = (1644, 288, 240, 89); MM = (1644, 36, 240, 240)
patch(VIT, 0, -250); patch(QB, 0, -270); patch(MS, -260, 0)
d = ImageDraw.Draw(im)
# 2) panels
im.alpha_composite(spr("hud_panel_vitals"), (VIT[0], VIT[1]))
im.alpha_composite(spr("hud_panel_quickbar"), (QB[0], QB[1]))
im.alpha_composite(spr("hud_panel_match"), (MS[0], MS[1]))
ov = spr("hud_minimap_overlay"); crop = im.crop((MM[0], MM[1], MM[0] + 240, MM[1] + 240)).convert("RGB"); cp = crop.load(); oa = ov.load()
for yy in range(240):
    for xx in range(240):
        r_, g_, b_ = cp[xx, yy]
        if (r_ > 165 and g_ > 150 and b_ < 90) or (r_ > 150 and g_ < 60 and b_ < 60) or (r_ > 220 and g_ > 200 and b_ > 180): oa[xx, yy] = (0, 0, 0, 0)   # overlay nam DUOI vong bo va marker
im.alpha_composite(ov, (MM[0], MM[1])); im.alpha_composite(spr("hud_minimap_frame"), (MM[0], MM[1]))
# 3) Vitals content (safe inset L/R 30, T/B 12), 3 rows
x0, y0 = VIT[0] + 34, VIT[1] + 14
im.alpha_composite(ico("hp", 36), (x0, y0))
bar(x0 + 44, y0 + 6, 110, 24, 1.0, (229, 72, 77)); text((x0 + 238, y0 + 18), "100", 24, anchor="rm")
y1 = y0 + 44
im.alpha_composite(ico("armor", 36), (x0, y1)); text((x0 + 44, y1 + 18), "0", 20, (166, 187, 183), "lm")
im.alpha_composite(ico("helmet", 36), (x0 + 110, y1)); text((x0 + 154, y1 + 18), "0", 20, (166, 187, 183), "lm")
y2 = y1 + 44
im.alpha_composite(ico("adrenaline", 32), (x0 + 2, y2 + 2)); bar(x0 + 44, y2 + 10, 110, 14, 0.0, (245, 197, 66), thin=True); text((x0 + 238, y2 + 17), "0", 18, (166, 187, 183), "rm")
d = ImageDraw.Draw(im)
# 4) Quick bar: slots + name strip
text((QB[0] + 30, QB[1] + 24), "Bare Hands", 16)
sx = [678.8 + i * 69 for i in range(4)] + [971.3 + i * 69 for i in range(4)]
for i, x in enumerate(sx):
    s = spr("hud_slot_selected" if i == 0 else "hud_slot_empty"); im.alpha_composite(s, (round(x), 969))
    d = ImageDraw.Draw(im); d.text((round(x) + 7, 972), str(i + 1), font=F(15), fill=(241, 231, 200), stroke_width=2, stroke_fill=(10, 12, 12))
# 5) Match status
im.alpha_composite(ico("alive", 24), (MS[0] + 28, MS[1] + 32)); d = ImageDraw.Draw(im); text((MS[0] + 58, MS[1] + 44), "1/1", 22, anchor="lm")
im.alpha_composite(ico("zone-timer", 24), (MS[0] + 124, MS[1] + 32)); d = ImageDraw.Draw(im); text((MS[0] + 154, MS[1] + 44), "1:22", 22, (162, 116, 255), "lm")
# 6) killfeed
kf = spr("hud_killfeed_plate"); im.alpha_composite(kf, (36, 36)); d = ImageDraw.Draw(im)
for i, (t, a) in enumerate([("Hao ha Linh", 255), ("Vinh ha Mai", 170), ("Vu ha Son", 100)]):
    text((60, 66 + i * 28), t, 16, (237, 242, 238))
im.convert("RGB").save(OUT / "dung-thu-hud-A.png")
print("ok")
