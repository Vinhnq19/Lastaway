"""T-021 mock helper: dat sprite UI kit T-020 (GPT) len nen, ghi chu chu. Khong ve tay do hoa; chi bo cuc, chu, nen toi."""
import sys, json, pathlib
from PIL import Image, ImageDraw, ImageFont, ImageFilter
T20 = pathlib.Path(r"D:\Lastaway\projects\lastaway\art\inbox\T-020"); T15 = pathlib.Path(r"D:\Lastaway\projects\lastaway\art\inbox\T-015"); OUT = pathlib.Path(r"D:\Lastaway\projects\lastaway\art\inbox\T-021")
FB = r"D:\Lastaway\Assets\_JungleMaze\Art\Fonts\BeVietnamPro-Bold.ttf"; F = lambda n: ImageFont.truetype(FB, n)
meta = json.load(open(T20 / "meta.json", encoding="utf-8"))
TXT = (237, 242, 238); MUTED = (214, 218, 214); GOLD = (255, 220, 110); DANGER = (255, 138, 120); WHITE = (255, 255, 255); OUTL = (10, 12, 12)
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
def text(im, xy, s, size, fill=TXT, anchor="la", stroke=2):
    ImageDraw.Draw(im).text(xy, s, font=F(size), fill=fill, anchor=anchor, stroke_width=stroke, stroke_fill=OUTL)
def tw(s, size): return ImageDraw.Draw(Image.new("RGBA", (1, 1))).textlength(s, font=F(size))
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

def button(im, kind, xy, caption, ic=None, col=WHITE, size=(240, 64), state="normal", font=26):
    put(im, f"kit_btn_{kind}_{state}", xy, size); cx, cy = xy[0] + size[0] / 2, xy[1] + size[1] / 2
    if state == "disabled": col = (150, 148, 140)
    if ic:
        w = tw(caption, font); total = w + font + 10; icon(im, ic, cx - total / 2 + font / 2 + 2, cy, font - 2); text(im, (cx - total / 2 + font + 10, cy), caption, font, col, "lm")
    else: text(im, (cx, cy), caption, font, col, "mm")
def label(im, xy, s, size=20, col=MUTED): text(im, xy, s, size, col, "lm")
def err(im, xy, s, size=20):
    icon(im, "kit_icon_cross", xy[0] + 12, xy[1], 22); text(im, (xy[0] + 32, xy[1]), s, size, DANGER, "lm")
def ok(im, xy, s, size=20):
    icon(im, "kit_icon_check", xy[0] + 12, xy[1], 22); text(im, (xy[0] + 32, xy[1]), s, size, TXT, "lm")
def note(im, s, y=1050): text(im, (W / 2, y), s, 18, MUTED, "mm")
def avatar(im, cx, cy, ghost=False):
    put(im, "kit_iconbtn_round_disabled" if ghost else "kit_iconbtn_round_normal", (cx - 24, cy - 24), (48, 48))
def input_(im, xy, w, value, state="normal", ph=False, size=24):
    put(im, f"kit_input_{state}", xy, (w, 56)); text(im, (xy[0] + 20, xy[1] + 28), value, size, MUTED if ph else TXT, "lm")
def codebox(im, xy, chars, active):
    for i in range(6):
        x = xy[0] + i * 76; im.alpha_composite(sized_hud("hud_slot_selected" if i == active else "hud_slot_empty", (66, 72)), (x, xy[1]))
        if i < len(chars): text(im, (x + 33, xy[1] + 36), chars[i], 38, TXT, "mm")
def sized_hud(n, size): return spr(n, T15).resize(size, Image.NEAREST)
def save(im, name): im.convert("RGB").save(OUT / name); return OUT / name
