"""T-027 demo cho thang 5 bac da chot + font chinh Chakra Petch Bold (Huy chot 07/11). Dung mock that, khong vá lai.
Chay: PYTHONIOENCODING=utf-8 python demo_final.py   (ra ../thang-5-bac.png, ../*-truoc-sau-5.png, ../crop-100-5.png, ../loan-co-5.png)
Truoc = anh `_cu/truoc-T027/` (pre-thang); sau = mock hien hanh (thang.ON). Loan co: bbox lay tu LOG cua mock (truoc: render lai voi thang.ON=False
va ban screens_b truoc khi xoa chu in san; chu in san cua anh HUD tinh bang cac hop uoc luong)."""
import sys, json, colorsys, pathlib, collections, importlib.util
TOOLS = r"D:\Lastaway\projects\lastaway\art\inbox\T-021\src\tools"; sys.path.insert(0, TOOLS); sys.path.insert(0, str(pathlib.Path(__file__).parent))
import thang, mock, screens_a, screens_b, do_co_chu
from PIL import Image, ImageDraw, ImageFont
IN = pathlib.Path(r"D:\Lastaway\projects\lastaway\art\inbox"); OUT = IN / "T-027"; T21 = IN / "T-021"
BV = lambda n: ImageFont.truetype(str(thang.BVP), n); MN = lambda n: ImageFont.truetype(str(thang.MAIN), n)
spec = importlib.util.spec_from_file_location("screens_b_old", TOOLS + r"\screens_b.before-hud-clean.py"); sb_old = importlib.util.module_from_spec(spec); spec.loader.exec_module(sb_old)
PAIRS = {"hud": ("b", "b9-phan-hoi-hud"), "b4": ("b", "b4-tui-do"), "a6": ("a", "a6-phong-cho-cho-du-nguoi"), "esc": ("b", "b1-esc-cai-dat")}
def pair(name):
    s, n = PAIRS[name]; b = Image.open(T21 / s / "_cu" / "truoc-T027" / f"{n}.png").convert("RGB"); a = Image.open(T21 / s / f"{n}.png").convert("RGB")
    w = Image.new("RGB", (3840, 1130), (24, 22, 18)); w.paste(b, (0, 50)); w.paste(a, (1920, 50)); d = ImageDraw.Draw(w)
    d.text((20, 10), "TRƯỚC (hiện trạng, trước thang chữ), 1920x1080", font=BV(28), fill=(255, 220, 110)); d.text((1940, 10), "SAU: thang 5 bậc [20, 24, 30, 40, 96], Chakra Petch Bold cho 96 và 40, 1920x1080", font=BV(28), fill=(255, 220, 110)); w.save(OUT / f"{name}-truoc-sau-5.png")
# crop 100%: cung cua so tren anh truoc va sau
CROPS = [("hud", "kill feed", (0, 30, 600, 200)), ("hud", "số phím quick bar", (700, 965, 1300, 1075)), ("hud", "máu 100", (300, 860, 900, 970)), ("hud", "1/1 và 1:21", (1500, 350, 1900, 460)), ("hud", "toast nhặt đồ", (980, 590, 1580, 700)),
         ("b4", "số trên ô túi đồ", (680, 300, 1280, 410)), ("b4", "dòng dưới đất", (200, 310, 800, 420)), ("a6", "tiêu đề Phòng chờ (40)", (700, 40, 1300, 130)), ("a6", "\"Chờ\" (96)", (1000, 290, 1500, 410)), ("a6", "nút Chép", (1400, 30, 1900, 140)), ("esc", "nhãn thanh trượt", (620, 310, 1220, 440)), ("esc", "tiêu đề Cài đặt (40)", (700, 190, 1300, 290))]
def crops():
    im = Image.new("RGB", (1920, 40 + len(CROPS) * 150), (24, 22, 18)); d = ImageDraw.Draw(im); d.text((20, 6), "Crop tỉ lệ 100% (không thu nhỏ): trái = trước, phải = sau (thang 5 bậc, Chakra Petch Bold cho 96 và 40)", font=BV(24), fill=(255, 220, 110))
    cache = {}
    for j, (k, lab, win) in enumerate(CROPS):
        s, n = PAIRS[k]; bb = cache.setdefault((k, 0), Image.open(T21 / s / "_cu" / "truoc-T027" / f"{n}.png").convert("RGB")); aa = cache.setdefault((k, 1), Image.open(T21 / s / f"{n}.png").convert("RGB")); y = 44 + j * 150
        im.paste(bb.crop(win), (460, y)); im.paste(aa.crop(win), (1200, y)); d.text((20, y + 20), {"hud": "HUD trong trận", "b4": "B4 túi đồ", "a6": "Phòng chờ", "esc": "ESC"}[k], font=BV(22), fill=(237, 242, 238)); d.text((20, y + 56), lab, font=BV(20), fill=(214, 218, 214))
    im.save(OUT / "crop-100-5.png")
# loan co
def run_all(on):
    thang.ON = on; out = []
    for mod, tag in ((screens_a, "a"), (sb_old if not on else screens_b, "b")):
        for n, f in mod.ALL.items(): mock.LOG.clear(); mock.CUR[0] = n; f(); out.append((tag, n, list(mock.LOG)))
    thang.ON = True; return out
BAKED = [(18, "Hao ha Linh", (37, 58), "lm"), (18, "Vinh ha Mai", (37, 93), "lm"), (18, "Vu ha Son", (37, 128), "lm"), (32, "100", (383, 898), "mm"), (30, "0", (143, 958), "mm"), (30, "0", (319, 958), "mm"), (28, "0", (396, 1014), "mm"), (25, "1/1", (1690, 396), "mm"), (25, "1:21", (1808, 396), "mm")] + [(14, str(i + 1), (cx, 1021), "mm") for i, cx in enumerate((764, 822, 882, 941, 1022, 1082, 1142, 1201))]
def hue(i, n): return tuple(int(255 * c) for c in colorsys.hsv_to_rgb(0.85 * i / max(1, n - 1), 0.85, 1.0))
def tint(im, rec, pal):
    ov = Image.new("RGBA", im.size, (0, 0, 0, 0)); d = ImageDraw.Draw(ov)
    for sz, bb in rec: c = pal[float(sz)]; d.rectangle([bb[0] - 2, bb[1] - 2, bb[2] + 2, bb[3] + 2], fill=c + (95,), outline=c + (255,), width=3)
    o = im.convert("RGBA"); o.alpha_composite(ov); return o.convert("RGB")
def loan_co(before, after):
    def rec(allr, tag, n): return [(r["size"], r["bbox"]) for t, m, lg in allr if (t, m) == (tag, n) for r in lg]
    scr = [("HUD trong trận", "b", "b9-phan-hoi-hud"), ("Phòng chờ", "a", "a6-phong-cho-cho-du-nguoi")]; W = 1920; f = BV
    d0 = ImageDraw.Draw(Image.new("RGB", (1, 1)))
    baked = [(est, d0.textbbox(xy, s, font=BV(est), anchor=an)) for est, s, xy, an in BAKED]
    items = []
    for title, tag, n in scr:
        rb = rec(before, tag, n) + (baked if tag == "b" else []); ra = rec(after, tag, n); items.append((title, tag, n, rb, ra))
    sb = collections.Counter(float(r[0]) for it in items for r in it[3]); sa = collections.Counter(float(r[0]) for it in items for r in it[4])
    pb = {s: hue(i, len(sb)) for i, s in enumerate(sorted(sb))}; pa = {s: hue(i, 5) for i, s in enumerate(sorted(set(thang.LAD) | set(sa)))}
    hb = collections.Counter(); ha = collections.Counter()
    for t, m, lg in before:
        for r in lg: hb[float(r["size"])] += 1
    for t, m, lg in after:
        for r in lg: ha[float(r["size"])] += 1
    for r in do_co_chu.load_unity(): hb[r["px"]] += 1; ha[float(thang.size(r["px"], r["game"]))] += 1
    H = 60 + 2 * (569 + 80) + 40 + 560; im = Image.new("RGB", (W, H), (24, 22, 18)); d = ImageDraw.Draw(im)
    d.text((20, 10), "Độ lộn xộn cỡ chữ: mỗi hộp chữ tô màu theo cỡ. Trái = trước (nhiều màu), phải = sau (thang 5 bậc, tối đa 5 màu)", font=f(26), fill=(255, 220, 110)); y = 60
    for title, tag, n, rb, ra in items:
        s, nm = ("b", n) if tag == "b" else ("a", n)
        b = Image.open(T21 / s / "_cu" / "truoc-T027" / f"{n}.png").convert("RGB"); a = Image.open(T21 / s / f"{n}.png").convert("RGB")
        im.paste(tint(b, rb, pb).resize((940, 529), Image.LANCZOS), (10, y + 30)); im.paste(tint(a, ra, pa).resize((940, 529), Image.LANCZOS), (970, y + 30))
        cb = collections.Counter(float(r[0]) for r in rb); ca = collections.Counter(float(r[0]) for r in ra)
        d.text((20, y), f"{title}: TRƯỚC, {len(cb)} cỡ khác nhau", font=f(24), fill=(237, 242, 238)); d.text((980, y), f"SAU: {len(ca)} cỡ", font=f(24), fill=(237, 242, 238)); y += 579
        for x0, cnt, pal in ((20, cb, pb), (980, ca, pa)):
            xs = x0; yy = y
            for s_ in sorted(cnt):
                lab = f"{s_:g} ({cnt[s_]})"; wd = 28 + int(f(20).getlength(lab)) + 20
                if xs + wd > x0 + 930: xs = x0; yy += 28
                d.rectangle([xs, yy, xs + 22, yy + 22], fill=pal[s_]); d.text((xs + 28, yy), lab, font=f(20), fill=(214, 218, 214)); xs += wd
        y += 70
    d.text((20, y), f"Số dòng chữ theo cỡ font (A + B + Unity, {sum(hb.values())} dòng). Vàng = trước ({len(hb)} cỡ), xanh = sau ({len(ha)} cỡ). Chữ in sẵn của ảnh HUD gốc không tính.", font=f(24), fill=(255, 220, 110))
    cats = sorted(set(hb) | set(ha)); cw = (W - 80) // len(cats); base = y + 60 + 440; mx = max(list(hb.values()) + list(ha.values()))
    for i, s_ in enumerate(cats):
        x = 40 + i * cw
        for j, (cnt, col) in enumerate(((hb, (255, 200, 80)), (ha, (90, 170, 255)))):
            v = cnt.get(s_, 0); h = int(430 * v / mx); xx = x + 2 + j * (cw // 2 - 2)
            if v: d.rectangle([xx, base - h, xx + cw // 2 - 4, base], fill=col); d.text((xx + (cw // 2 - 4) // 2, base - h - 4), str(v), font=f(14), fill=(237, 242, 238), anchor="ms")
        d.text((x + cw // 2, base + 16), f"{s_:g}", font=f(16), fill=(237, 242, 238), anchor="mt")
    d.line([(40, base), (W - 40, base)], fill=(120, 110, 90), width=2); im.crop((0, 0, W, base + 50)).save(OUT / "loan-co-5.png")
    return {"so_co_truoc": len(hb), "so_co_sau": len(ha), "dong": sum(hb.values()), "hud_truoc": len(set(float(r[0]) for r in items[0][3])), "hud_sau": len(set(float(r[0]) for r in items[0][4])), "a6_truoc": len(set(float(r[0]) for r in items[1][3])), "a6_sau": len(set(float(r[0]) for r in items[1][4]))}
ROLES = [(96, "1,05", "H1 Số lớn", "hạng, đếm ngược, CHIẾN THẮNG", "HẠNG #4"), (40, "1,2", "H2 Tiêu đề", "tiêu đề màn và panel, số thống kê lớn", "Bảng xếp hạng · Ắ Ặ Ề"), (30, "1,3", "B1 Thân lớn", "nút M, tên nổi bật, số HUD chính (máu)", "Tạo phòng · Quang Huy"),
         (24, "1,35", "B2 Thân", "nút S, thân, dòng danh sách, toast, tooltip, số HUD phụ; sàn chữ gameplay", "Bạn sẽ bị loại khỏi trận."), (20, "1,4", "C Chú thích", "chú thích, nhãn phụ, số trên ô và keycap; sàn tuyệt đối", "Tiếp tế, rương gỗ, xác người chơi")]
def wrap(s, font, wmax):
    out, cur = [], ""
    for w in s.split():
        t = (cur + " " + w).strip()
        if font.getlength(t) <= wmax: cur = t
        else: out.append(cur); cur = w
    return out + [cur]
def ladder():
    im = Image.new("RGB", (1920, 1080), (31, 29, 24)); d = ImageDraw.Draw(im); y = 30; O = dict(stroke_width=2, stroke_fill=(10, 12, 12))
    d.text((40, y), "Thang chữ 5 bậc (px cỡ font, 1080p)", font=BV(34), fill=(255, 220, 110), **O); y += 56
    d.text((40, y), "Chữ trên nút theo cỡ nút (Be Vietnam Pro, kể cả L): S 48 = 24, M 64 = 30, L 80 = 40 px", font=BV(22), fill=(214, 218, 214)); y += 40
    for px, lh, nm, use, sample in ROLES:
        h = max(int(px * 1.3) + 8, 84); fnt = MN(px) if px >= 40 else BV(px)
        d.text((40, y + h // 2 - 16), nm, font=BV(24), fill=(237, 242, 238), anchor="lm"); d.text((40, y + h // 2 + 16), f"{px} px · lh {lh} · {'Chakra Petch' if px >= 40 else 'Be Vietnam Pro'}", font=BV(20), fill=(214, 218, 214), anchor="lm")
        d.text((420, y + h // 2), sample, font=fnt, fill=(237, 242, 238), anchor="lm", **O)
        lines = wrap(use, BV(20), 1880 - 1330); oy = y + h // 2 - (len(lines) - 1) * 14
        for k, ln in enumerate(lines): d.text((1330, oy + k * 28), ln, font=BV(20), fill=(214, 218, 214), anchor="lm")
        d.line([(40, y + h + 4), (1880, y + h + 4)], fill=(60, 57, 46)); y += h + 14
    d.text((40, y + 6), "Dấu chồng (Ắ Ặ Ề Ộ Ữ) cao hơn chữ hoa khoảng 0,44 em: line-height một dòng không dưới 1,3; khối nhiều dòng 1,5.", font=BV(20), fill=(214, 218, 214)); im.save(OUT / "thang-5-bac.png")
if __name__ == "__main__":
    for k in PAIRS: pair(k)
    crops(); ladder(); before = run_all(False); after = run_all(True); print(json.dumps(loan_co(before, after), ensure_ascii=False))
