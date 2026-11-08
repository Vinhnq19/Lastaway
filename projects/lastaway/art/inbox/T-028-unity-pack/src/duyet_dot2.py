"""T-028 dot 2: trang duyet `files/new/duyet-dot2.png`: asset gen moi dat vao mock o co 100% (truoc: ve bang ma; sau: sprite gen).
PYTHONIOENCODING=utf-8 python duyet_dot2.py"""
import sys, json, pathlib
sys.path.insert(0, r"D:\Lastaway\projects\lastaway\art\inbox\T-021\src\tools")
import mock, screens_b
from PIL import Image, ImageDraw
IN = pathlib.Path(r"D:\Lastaway\projects\lastaway\art\inbox"); N = IN / "T-028-unity-pack/files/new"; meta = json.load(open(N / "meta.json", encoding="utf-8"))
def spr(n, size, k=2, root=None):
    im = Image.open((root or N) / f"{n}@2x.png").convert("RGBA"); b = meta[n]["border_LRTB"]; W, H = size
    if not b: return im.resize((W, H), Image.LANCZOS)
    L, R, T, B = [v * 2 for v in b]; o = Image.new("RGBA", (W, H), (0, 0, 0, 0)); w, h = im.size; s = 2
    xs = [(0, L, 0, L), (L, w - R, L, W - R), (w - R, w, W - R, W)]; ys = [(0, T, 0, T), (T, h - B, T, H - B), (h - B, h, H - B, H)]
    # border o @2x duoc thu ve 1x: dung ti le 1/2
    o = Image.new("RGBA", (W, H), (0, 0, 0, 0)); l, r_, t, b_ = b
    x1 = [(0, l), (l, W - r_), (W - r_, W)]; y1 = [(0, t), (t, H - b_), (H - b_, H)]; sx = [(0, L, ), (L, w - R), (w - R, w)]; sy = [(0, T), (T, h - B), (h - B, h)]
    for (a0, a1), (d0, d1) in zip(sx, x1):
        for (c0, c1), (e0, e1) in zip(sy, y1):
            if d1 > d0 and e1 > e0 and a1 > a0 and c1 > c0: o.alpha_composite(im.crop((a0, c0, a1, c1)).resize((d1 - d0, e1 - e0), Image.LANCZOS), (d0, e0))
    return o
mock.CUR[0] = "b9"
before9 = Image.open(IN / "T-021/b/b9-phan-hoi-hud.png").convert("RGBA"); b5 = Image.open(IN / "T-021/b/b5-ban-do-lon.png").convert("RGBA")
# --- kill feed + so phim quick bar
a9 = before9.copy(); sp = 44; K = screens_b.KILLFEED
wd = max(mock.tw(k, 24) + 12 + 32 + 12 + mock.tw(v, 24) for k, _, v in K); pw, ph = int(max(270, wd + 70)), 22 + sp * 3
a9.alpha_composite(spr("kit_killfeed_panel", (pw, ph)), (15, 34))
for i, (k, w, v) in enumerate(K):
    yc = 34 + 11 + sp // 2 + i * sp; mock.text(a9, (37, yc), k, 24, mock.TXT, "lm"); x = 37 + mock.tw(k, 24) + 12; screens_b.itm(a9, w, x + 16, yc, 32); mock.text(a9, (x + 44, yc), v, 24, mock.TXT, "lm")
for i, cx in enumerate(screens_b.QBX):
    a9.alpha_composite(spr("kit_badge_key", (30, 30)), (cx - 15, 1021 - 15)); mock.text(a9, (cx, 1020), str(i + 1), 20, mock.GOLD, "mm", 0, exempt=True)
# --- ban do lon: truoc (mock), sau v1 (tu _cu/dot2-v1), sau v2 (hien hanh)
V1 = N.parent.parent / "_cu" / "dot2-v1"; mx, my, ms = 110, 100, 840; cx_, cy_ = mx + 420, my + 430
def build5(root):
    a = b5.copy(); a.alpha_composite(spr("kit_map_bg", (ms, ms)), (mx, my)); a.alpha_composite(spr("map_grid", (ms, ms), root=root), (mx, my)); a.alpha_composite(spr("map_ring_zone", (680, 680), root=root), (cx_ - 340, cy_ - 340)); a.alpha_composite(spr("map_ring_safe", (180, 180)), (cx_ - 70, cy_ - 140))
    for n, x, y, h in (("fx_map_player", cx_ - 180, cy_ + 230, 66), ("fx_map_mark", cx_ + 40, cy_ - 60, 70), ("fx_map_drop", cx_ + 250, cy_ - 220, 90)): screens_b.put_fx(a, n, x, y, h)
    return a
a5v1 = build5(V1); a5 = build5(None)
im = Image.new("RGB", (2580, 8 + 50 + 32 + 214 + 32 + 124 + 32 + 840 + 40), (24, 22, 18)); dr = ImageDraw.Draw(im); y = 8
dr.text((10, y), "Duyệt đợt 2 (v2): asset gen mới đặt trong mock ở cỡ 100%. Trái = trước (vẽ bằng mã), giữa = sau v1, phải = sau v2", font=mock.F(26), fill=(255, 220, 110)); y += 50
def row2(lbl, box):
    global y
    dr.text((10, y), lbl + " (không đổi ở v2)", font=mock.F(22), fill=(237, 242, 238)); y += 32; w, h = box[2] - box[0], box[3] - box[1]
    im.paste(before9.crop(box).convert("RGB"), (10, y)); im.paste(a9.crop(box).convert("RGB"), (10 + w + 20, y)); y += h + 14
row2("Panel kill feed (kit_killfeed_panel), cột trái = trước, giữa = sau", (0, 20, 600, 220)); row2("Nhãn số phím quick bar (kit_badge_key), cột trái = trước, giữa = sau", (700, 965, 1300, 1075))
dr.text((10, y), "Bản đồ lớn (840x840, 100%): trước | sau v1 (lưới lệch, vòng dày) | sau v2 (lưới lát 10x10 khớp khung, vòng bo mảnh)", font=mock.F(22), fill=(237, 242, 238)); y += 32; box = (110, 100, 950, 940)
for x, src in ((10, b5), (870, a5v1), (1730, a5)): im.paste(src.crop(box).convert("RGB"), (x, y))
y += 840 + 10; im.crop((0, 0, 2580, y)).save(N / "duyet-dot2.png"); print(y)
