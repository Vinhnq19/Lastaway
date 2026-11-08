"""T-029: dung 3 mock + trang kich co tu anh gameplay that (qa/T-022/zone-main). python -P build_mock.py
Nen: 01-dau-tran (minimap toan map that), 02-ban-do-lon (ban do lon that). Chi ve them: cam, mui ten, cham dich, dau thay lan cuoi, chi bao zoom.
Vung ngoai bo to hong (167,55,80) o ban do lon la remap mau cua anh chup (mock only, Huy chot 07/11)."""
import sys, json, math, pathlib
sys.path.insert(0, r"D:\Lastaway\projects\lastaway\art\inbox\T-021\src\tools")
import thang
from PIL import Image, ImageDraw, ImageFont
S = pathlib.Path(__file__).parent; T = S.parent; F = T / "files"; K = T.parent / "T-020"; N28 = T.parent / "T-028-unity-pack/files/new"
QA = pathlib.Path(r"D:\Lastaway\projects\lastaway\qa\T-022\zone-main")
PINK = (167, 55, 80); TXT = (241, 231, 200); DARK = (10, 12, 12)
LOG = []   # ghi chu van ban de cong tuong phan: (mock, bbox, text, size)
def spr(name, px):
    p = F / f"{name}@2x.png"
    if not p.exists(): p = N28 / f"{name}@2x.png"
    im = Image.open(p).convert("RGBA"); return im.resize((px, px), Image.LANCZOS)
def put(im, sp, cx, cy, a=1.0):
    if a < 1: sp = sp.copy(); sp.putalpha(sp.getchannel("A").point(lambda v: int(v * a)))
    im.alpha_composite(sp, (round(cx - sp.width / 2), round(cy - sp.height / 2)))
def nine(src, size, border):
    L, R, Tp, B = border; w, h = size; im = src.convert("RGBA"); sw, sh = im.size; o = Image.new("RGBA", size, (0, 0, 0, 0))
    xs = [(0, L, 0, L), (L, sw - R, L, w - R), (sw - R, sw, w - R, w)]; ys = [(0, Tp, 0, Tp), (Tp, sh - B, Tp, h - B), (sh - B, sh, h - B, h)]
    for sx0, sx1, dx0, dx1 in xs:
        for sy0, sy1, dy0, dy1 in ys:
            if dx1 > dx0 and dy1 > dy0: o.alpha_composite(im.crop((sx0, sy0, sx1, sy1)).resize((dx1 - dx0, dy1 - dy0), Image.NEAREST), (dx0, dy0))
    return o
def text(im, mock, xy, s, size=24, anchor="mm"):
    d = ImageDraw.Draw(im); f = thang.font(size, True); st = max(2, round(size / 12)); d.text(xy, s, font=f, fill=TXT, anchor=anchor, stroke_width=st, stroke_fill=DARK)
    LOG.append((mock, d.textbbox(xy, s, font=f, anchor=anchor), s, size))
def line(im, a, b, w=1, col=(189, 230, 248), alpha=0.6):
    ov = Image.new("RGBA", im.size, (0, 0, 0, 0)); ImageDraw.Draw(ov).line([a, b], fill=col + (int(255 * alpha),), width=w); im.alpha_composite(ov)
def dashed_ring(im, c, r, w=4, dash=6, gap=5, clip=None):
    ov = Image.new("RGBA", im.size, (0, 0, 0, 0)); d = ImageDraw.Draw(ov); circ = 2 * math.pi * r; n = max(1, round(circ / (dash + gap))); step = 360 / n
    for i in range(n): d.arc([c[0] - r, c[1] - r, c[0] + r, c[1] + r], i * step, i * step + step * dash / (dash + gap), fill=(255, 255, 255, 255), width=w)
    if clip:
        m = Image.new("L", im.size, 0); ImageDraw.Draw(m).rectangle(clip, fill=255); ov.putalpha(Image.composite(ov.getchannel("A"), Image.new("L", im.size, 0), m))
    im.alpha_composite(ov)
def pinkify(tile):
    px = tile.load()
    for y in range(tile.height):
        for x in range(tile.width):
            r, g, b = px[x, y][:3]
            if r > 150 and g < 100 and b < 75: px[x, y] = (min(255, PINK[0] + (r - 180) // 2), max(0, PINK[1] + (g - 72) // 2), min(255, PINK[2] + (b - 41) // 2))
    return tile
HOLE = (1600, 55, 1869, 324)   # lo minimap 269 px (do tu 01-dau-tran)
tag_src = Image.open(K / "kit_tag_neutral@2x.png"); badge_src = Image.open(N28 / "kit_badge_key@2x.png")
def zoom_tag(im, mock, x, y, label, w=150):
    # chi bao zoom: the kit_tag_neutral keo gian 36 px cao + kinh lup 24 + chu 24
    t = nine(badge_src.resize((badge_src.width // 2, badge_src.height // 2), Image.NEAREST), (w, 48), (14, 14, 14, 14)); im.alpha_composite(t, (x, y))
    put(im, spr("kit_ico_zoom", 24), x + 22, y + 24); text(im, mock, (x + 44, y + 27), label, 24, "lm")
def dist_plate(im, mock, cx, cy, s):
    w = 64; p = nine(badge_src.resize((badge_src.width // 2, badge_src.height // 2), Image.NEAREST), (w, 36), (14, 14, 14, 14)); im.alpha_composite(p, (round(cx - w / 2), round(cy - 18))); text(im, mock, (cx, cy + 1), s, 24, "mm")
def clamp_edge(p, rect):
    return (min(max(p[0], rect[0] + 9), rect[2] - 9), min(max(p[1], rect[1] + 9), rect[3] - 9))

# ---------- M1 minimap muc toan map + world ----------
def m1():
    im = Image.open(QA / "01-dau-tran.png").convert("RGBA"); x0, y0, x1, y1 = HOLE; m = "m1"
    hero = (1824, 158); flag = (1654, 200)
    line(im, hero, flag); put(im, spr("map_self_marker", 26), *hero)
    for p in ((1706, 128), (1690, 205), (1790, 240)): put(im, spr("map_enemy_dot", 12), *p)
    put(im, spr("map_lastseen_ring", 12), 1740, 100, 0.6); put(im, spr("wp_flag_mini", 18), flag[0] + 1, flag[1] - 4)
    zoom_tag(im, m, x0 + 8, y0 + 8, "Toàn")
    # world: cam trong man (khong so) + mui ten mep man co so khoang cach
    # co ngoai man: world khong ve co, chi mui ten mep trai huong ve co (Hero giua man 960,540; co o 170 px trai, 42 px duoi tren minimap = 32,5 cell)
    dx, dy = flag[0] - hero[0], flag[1] - hero[1]; n = round(math.hypot(dx, dy) / (269 / 50)); ang = math.degrees(math.atan2(-dx, -dy)); t = (960 - 70) / abs(dx / math.hypot(dx, dy))
    ax, ay = 960 + dx / math.hypot(dx, dy) * t, 540 + dy / math.hypot(dx, dy) * t
    put(im, spr("wp_arrow_edge", 40).rotate(ang, expand=True, resample=Image.BICUBIC), ax, ay); dist_plate(im, m, ax + 62, ay, str(n))
    return im
# ---------- M2 minimap muc gan (30 cell, theo Hero) ----------
BM = (112, 102, 836)   # ban do lon trong 02: goc x, y, canh (50 cell)
def m2():
    im = Image.open(QA / "01-dau-tran.png").convert("RGBA"); x0, y0, x1, y1 = HOLE; m = "m2"; big = clean_big()
    cell = BM[2] / 50; hero_c = (30.0, 17.0)   # o (x, y tu tren) - Hero ben trai de co cam o mep
    wx0 = min(max(hero_c[0] - 15, 0), 20); wy0 = min(max(hero_c[1] - 15, 0), 20)
    crop = big.crop((round(BM[0] + wx0 * cell), round(BM[1] + wy0 * cell), round(BM[0] + (wx0 + 30) * cell), round(BM[1] + (wy0 + 30) * cell))).resize((269, 269), Image.LANCZOS)
    im.paste(pinkify(crop.convert("RGBA")), (x0, y0)); k = 269 / 30
    def P(cx, cy): return (x0 + (cx - wx0) * k, y0 + (cy - wy0) * k)
    # vong ke tiep nen (net dut) tam (27,19) r 12 cell
    dashed_ring(im, P(27, 19), 12 * k, 4, clip=HOLE)
    hero = P(*hero_c); flag_c = (48, 40); fp = clamp_edge(P(*flag_c), HOLE)
    line(im, hero, fp); put(im, spr("map_self_marker", 26), *hero); put(im, spr("wp_flag_mini", 18), fp[0] + 1, fp[1] - 4)
    for c in ((24, 14), (36, 22), (28, 27)): put(im, spr("map_enemy_dot", 12), *P(*c))
    put(im, spr("map_lastseen_ring", 12), *P(33, 12), 0.6)
    zoom_tag(im, m, x0 + 8, y0 + 8, "Gần"); return im
# ---------- M3 ban do lon co waypoint ----------
def clean_big():
    big = Image.open(QA / "02-ban-do-lon-dau-tran.png").convert("RGB"); big.paste(big.crop((690, 640, 770, 720)), (760, 395)); return big   # xoa marker do cu (797,433)
def m3():
    im = clean_big().convert("RGBA"); m = "m3"; mx, my, ms = BM; c = ms / 50
    reg = im.crop((mx, my, mx + ms, my + ms)); im.paste(pinkify(reg), (mx, my))
    P = lambda cx, cy: (mx + cx * c, my + cy * c)
    d = ImageDraw.Draw(im);
    hero = (797, 433); flag = P(14.5, 38.4)
    dashed_ring(im, (580, 533), 300, 4, 6, 5, clip=(mx, my, mx + ms, my + ms))
    line(im, hero, flag); put(im, spr("map_self_marker", 40), *hero); put(im, spr("wp_flag", 24), flag[0] + 4, flag[1] - 10)
    for cc in ((20.5, 21), (33, 31), (31, 12), (11, 14)): put(im, spr("map_enemy_dot", 16), *P(*cc))
    put(im, spr("map_lastseen_ring", 16), *P(22, 33), 0.6)
    # chu giai (hang 6 icon, tam x 1235,1348,1462,1576,1690,1805; y 276): vong -> vong net dut, ghim xanh -> co, cham do -> marker Amber
    panel = (45, 37, 31); dd = ImageDraw.Draw(im)
    for cx in (1462, 1690, 1805): dd.rectangle([cx - 38, 236, cx + 38, 314], fill=panel)
    put(im, spr("legend_ring_next_dashed", 32), 1462, 276); put(im, spr("wp_flag", 40), 1690, 276); put(im, spr("map_self_marker", 44), 1805, 276)
    # huong dan: thay ghim xanh dau hang chuot trai bang co
    ImageDraw.Draw(im).rectangle([1520, 395, 1600, 465], fill=panel); put(im, spr("wp_flag", 40), 1560, 430)
    return im
def sheet():
    items = [("wp_flag", 48, "24 / 40"), ("wp_flag_mini", 16, "14 (18)"), ("wp_arrow_edge", 48, "40"), ("map_enemy_dot", 16, "12 / 16"), ("map_lastseen_ring", 16, "12 / 16, alpha 0,6"), ("kit_ico_zoom", 48, "24"), ("map_self_marker", 32, "26")]
    im = Image.new("RGBA", (1920, 420), (92, 100, 24, 255)); d = ImageDraw.Draw(im); f = ImageFont.truetype(str(thang.BVP), 20)
    for i, (n, px, sz) in enumerate(items):
        x = 130 + i * 255; draw = int(sz.split(" ")[0]); put(im, spr(n, draw), x, 90); put(im, spr(n, 96), x, 250)
        d.text((x, 340), n, font=f, fill=TXT, anchor="mm"); d.text((x, 372), f"vẽ {sz} px", font=f, fill=TXT, anchor="mm")
    return im
if __name__ == "__main__":
    out = T; out.mkdir(exist_ok=True)
    for n, fn in (("m1-minimap-toan-map", m1), ("m2-minimap-gan", m2), ("m3-ban-do-lon-waypoint", m3), ("trang-kich-co", sheet)): fn().convert("RGB").save(out / f"{n}.png"); print(n)
    json.dump(LOG, open(T / "src/van-ban.json", "w", encoding="utf-8"), ensure_ascii=False)
