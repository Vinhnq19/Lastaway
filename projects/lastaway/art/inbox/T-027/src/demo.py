"""T-027 demo v2 thang co chu: trang thang, truoc/sau tren 4 man, crop 100%, loan co, cong tuong phan + can giua + bac nut.
Chay: PYTHONIOENCODING=utf-8 python demo.py   (ra D:/Lastaway/projects/lastaway/art/inbox/T-027/*.png + demo-cong.json)
Cach lam: render lai mock da co (khong sua mock): va mock.F (co chu) va mock.button (font nut) bang ham gan co chu vao bac:
- chu thuong: bac gan nhat (hoa thi len), man gameplay (HUD, B3, B4, B5, B9) khong duoi 24;
- chu tren nut: theo bac cua CO NUT (S 48, M 64, L 80 -> bac rieng), khong theo co chu cu.
Chu trong mock render bang font qua script (chu khong phai asset). Ghi chu mock ("Minh hoa: ...") khong ve va khong do."""
import sys, colorsys, pathlib, json, collections
TOOLS = r"D:\Lastaway\projects\lastaway\art\inbox\T-021\src\tools"; sys.path.insert(0, TOOLS); sys.path.insert(0, str(pathlib.Path(__file__).parent))
import mock, screens_a, screens_b, check_center, do_co_chu
import numpy as np
from PIL import Image, ImageDraw
OUT = pathlib.Path(r"D:\Lastaway\projects\lastaway\art\inbox\T-027"); FB = mock.FB
LAD = {"5": [20, 24, 30, 40, 96], "7": [20, 24, 28, 32, 40, 64, 96]}
BTN = {"5": {48: 24, 64: 30, 80: 40}, "7": {48: 24, 64: 28, 80: 32}}   # bac chu nut theo co nut S, M, L
GAME = ("b3", "b4", "b5", "b9", "hud")
orig_F, orig_text, orig_button = mock.F, mock.text, mock.button
S = {"map": None, "rec": [], "opt": None}
def snap(sz, lad, game): b = min(lad, key=lambda v: (abs(v - sz), -v)); return max(b, 24) if game and b < 24 else b
def btn_font(h, opt): return BTN[opt][min((48, 64, 80), key=lambda k: (abs(k - h), k))]
def F(n): m = S["map"]; return orig_F(int(m(n)) if m else int(n))
def text(im, xy, s, size, fill=mock.TXT, anchor="la", stroke=2, skip=False):
    if s.startswith("Minh họa:"): return  # ghi chu mock, khong phai UI
    if s.strip():
        m = S["map"]; sz = m(size) if m else size; S["rec"].append((sz, s, ImageDraw.Draw(im).textbbox(xy, s, font=orig_F(int(sz)), anchor=anchor)))
    return orig_text(im, xy, s, size, fill, anchor, stroke, skip)
def button(*a, **k):
    a = list(a); size = a[6] if len(a) > 6 else k.get("size", (240, 64)); f = btn_font(size[1], S["opt"]) if S["opt"] else None
    if len(a) > 8: a[8] = f or a[8]
    else: k["font"] = f or k.get("font", 26)
    return orig_button(*a, **k)
mock.F = F; mock.text = text; mock.button = button
for mod in (screens_a, screens_b): mod.text = text; mod.button = button

# --- chu in san trong anh HUD (uoc luong tu anh): ve lai khi demo "sau"
BAKED = [(18, "Hao ha Linh", (37, 58), "lm"), (18, "Vinh ha Mai", (37, 93), "lm"), (18, "Vu ha Son", (37, 128), "lm"), (32, "100", (383, 898), "mm"), (30, "0", (143, 958), "mm"), (30, "0", (319, 958), "mm"), (28, "0", (396, 1014), "mm"),
         (25, "1/1", (1690, 396), "mm"), (25, "1:21", (1808, 396), "mm")] + [(14, str(i + 1), (cx, 1021), "mm") for i, cx in enumerate((764, 822, 882, 941, 1022, 1082, 1142, 1201))]
ERASE = [(352, 880, 424, 916), (128, 940, 172, 976), (303, 940, 347, 976), (380, 996, 426, 1032), (1666, 380, 1714, 412), (1780, 380, 1838, 412)]
def erase(im, box, pad=3):
    x0, y0, x1, y1 = box; a = np.asarray(im.convert("RGB")); col = tuple(int(v) for v in np.median(np.concatenate([a[y0 - pad:y0, x0:x1].reshape(-1, 3), a[y1:y1 + pad, x0:x1].reshape(-1, 3)]), axis=0))
    ImageDraw.Draw(im).rectangle([x0, y0, x1, y1], fill=col + (255,))
def baked_rec(im):  # ban "truoc": ghi lai chu in san (co uoc luong) de to mau o trang loan co
    d = ImageDraw.Draw(im)
    for est, s, xy, an in BAKED: S["rec"].append((est, s, d.textbbox(xy, s, font=orig_F(est), anchor=an)))
def hud_patch(im):
    m = S["map"]; em = m(18); sp = max(40, round(em * 1.5)); wmax = max(orig_F(int(em)).getlength(s) for s in ("Hao ha Linh", "Vinh ha Mai", "Vu ha Son"))
    plate = Image.new("RGBA", (int(max(270, wmax + 70)), 22 + sp * 3), (20, 18, 14, 250)); im.alpha_composite(plate, (15, 34))  # panel nho theo chu, khong tran
    for i, s in enumerate(("Hao ha Linh", "Vinh ha Mai", "Vu ha Son")): text(im, (37, 34 + 11 + sp // 2 + i * sp), s, 18, mock.TXT, "lm")
    for b in ERASE: erase(im, b)
    for est, s, xy, an in BAKED[3:9]: text(im, xy, s, est, mock.TXT, an)
    S["map"] = None; d = ImageDraw.Draw(im)
    for i, (est, s, xy, an) in enumerate(BAKED[9:]):
        cx = xy[0]; d.rounded_rectangle([cx - 15, 1021 - 15, cx + 15, 1021 + 15], radius=6, fill=(31, 26, 16, 235), outline=(176, 154, 102, 255), width=2)
        orig_text(im, (cx, 1020), s, 20, mock.GOLD, "mm", 0); S["rec"].append((20, s, d.textbbox((cx, 1020), s, font=orig_F(20), anchor="mm")))
    S["map"] = m

def setmap(name, opt): S["opt"] = opt; S["map"] = (lambda n: snap(n, LAD[opt], name.split("-")[0] in GAME)) if opt else None
def build(name, opt):
    setmap(name, opt); S["rec"] = []; mock.LOG.clear(); mock.LOGB.clear(); mock.CUR[0] = name
    if name == "hud":
        im = screens_b.feedback()
        if opt: hud_patch(im)
        else: baked_rec(im)
    elif name == "b4": im = screens_b.inventory()
    elif name == "esc": im = screens_b.esc()
    else: im = screens_a.ALL["a6-phong-cho-cho-du-nguoi"]()
    return im.convert("RGB"), list(S["rec"]), list(mock.LOG), list(mock.LOGB)

NAMES = {"hud": "HUD trong trận (phản hồi, nhặt đồ, kill feed, quick bar)", "b4": "B4 túi đồ", "a6": "Phòng chờ (A6)", "esc": "ESC cài đặt (B1)"}
def pair(name, opt, store):
    b, rb, lb, _ = build(name, None); a, ra, la, bt = build(name, opt)
    w = Image.new("RGB", (3840, 1130), (24, 22, 18)); w.paste(b, (0, 50)); w.paste(a, (1920, 50)); d = ImageDraw.Draw(w)
    for x, t in ((20, "TRƯỚC (hiện trạng), 1920x1080"), (1940, f"SAU: thang {opt} bậc {LAD[opt]}, 1920x1080")): d.text((x, 10), t, font=orig_F(28), fill=(255, 220, 110))
    w.save(OUT / f"{name}-truoc-sau-{opt}.png"); store[(name, opt)] = (rb, ra, b, a, la, bt)

def render_all(opt):
    """render moi mock A va B voi thang opt (None = hien trang): [(ten, rec, LOG, LOGB)]"""
    out = []
    for mod in (screens_a, screens_b):
        for n, f in mod.ALL.items():
            setmap(n, opt); S["rec"] = []; mock.LOG.clear(); mock.LOGB.clear(); mock.CUR[0] = n; f(); out.append((n, list(S["rec"]), list(mock.LOG), list(mock.LOGB)))
    return out
def gates_all(opt, allr):
    ct = 0; ncase = 0; mx = 0.0; fails = 0; wrong = 0; bad = []; seen = set()
    S["map"] = None; S["opt"] = None; mock.button = orig_button
    for n, rec, lg, lb in allr:
        bad += [(n, r["text"][:24], r["ratio"]) for r in lg if r["ratio"] < 4.5]; ct += len(lg)
        for b in lb:
            wrong += b["font"] != btn_font(b["h"], opt)
            k = (b["kind"], b["state"], b["w"], b["h"], b["font"], b["caption"], b["icon"])
            if k in seen: continue
            seen.add(k); r = check_center.measure(b["kind"], b["state"], b["w"], b["h"], b["font"], b["caption"], b["icon"])
            if r is None: continue
            ncase += 1; mx = max(mx, abs(float(r[0]))); fails += abs(float(r[0])) > 1
    mock.button = button
    return {"man": len(allr), "dong_chu": ct, "duoi_4.5": len(bad), "xau_nhat": [f"{a}|{b}:{c}" for a, b, c in sorted(bad, key=lambda x: x[2])[:5]], "nut_ca": ncase, "nut_FAIL": int(fails), "nut_lech_max": round(mx, 2), "nut_sai_bac": int(wrong)}

def hist(opt, before_all, after_all):
    """so dong chu theo co: A + B (render) + Unity prefab (m_fontSize x 1,5); khong tinh chu in san cua anh HUD"""
    bf = collections.Counter(); af = collections.Counter(); lad = LAD[opt]
    for n, rec, _, _ in before_all:
        for sz, s, bb in rec: bf[float(sz)] += 1
    for n, rec, _, _ in after_all:
        for sz, s, bb in rec: af[float(sz)] += 1
    for r in do_co_chu.load_unity():
        bf[r["px"]] += 1; af[float(snap(r["px"], lad, r["man"].startswith(("Hud", "MatchOverlay", "SpectatorRow"))))] += 1
    return bf, af

def hue(i, n): return tuple(int(255 * c) for c in colorsys.hsv_to_rgb(0.85 * i / max(1, n - 1), 0.85, 1.0))
def tint(im, rec, pal):
    ov = Image.new("RGBA", im.size, (0, 0, 0, 0)); d = ImageDraw.Draw(ov)
    for sz, s, bb in rec: c = pal[float(sz)]; d.rectangle([bb[0] - 2, bb[1] - 2, bb[2] + 2, bb[3] + 2], fill=c + (95,), outline=c + (255,), width=3)
    o = im.convert("RGBA"); o.alpha_composite(ov); return o.convert("RGB")
def loan_co(opt, store, bf, af):
    scr = [("hud", "HUD trong trận"), ("a6", "Phòng chờ")]; W = 1920; f = orig_F
    # bang mau theo cac cỡ xuat hien trong 2 man (truoc) va theo bac thang (sau)
    sb_all = collections.Counter(); sa_all = collections.Counter()
    for name, _ in scr:
        rb, ra = store[(name, opt)][:2]; sb_all.update(float(r[0]) for r in rb); sa_all.update(float(r[0]) for r in ra)
    pb = {s: hue(i, len(sb_all)) for i, s in enumerate(sorted(sb_all))}; pa = {s: hue(i, len(LAD[opt])) for i, s in enumerate(sorted(set(LAD[opt]) | set(sa_all)))}
    H = 60 + 2 * (569 + 80) + 40 + 560; im = Image.new("RGB", (W, H), (24, 22, 18)); d = ImageDraw.Draw(im)
    d.text((20, 10), f"Độ lộn xộn cỡ chữ: mỗi hộp chữ tô màu theo cỡ. Trái = trước (nhiều màu), phải = sau (thang {opt} bậc, tối đa {len(LAD[opt])} màu)", font=f(26), fill=(255, 220, 110)); y = 60
    for name, title in scr:
        rb, ra, b, a, _, _ = store[(name, opt)]; sb = collections.Counter(float(r[0]) for r in rb); sa = collections.Counter(float(r[0]) for r in ra)
        im.paste(tint(b, rb, pb).resize((940, 529), Image.LANCZOS), (10, y + 30)); im.paste(tint(a, ra, pa).resize((940, 529), Image.LANCZOS), (970, y + 30))
        d.text((20, y), f"{title}: TRƯỚC, {len(sb)} cỡ khác nhau", font=f(24), fill=(237, 242, 238)); d.text((980, y), f"SAU: {len(sa)} cỡ", font=f(24), fill=(237, 242, 238)); y += 569 + 10
        for x0, cnt, pal in ((20, sb, pb), (980, sa, pa)):
            xs = x0; yy = y
            for s in sorted(cnt):
                lab = f"{s:g} ({cnt[s]})"; wd = 28 + int(f(20).getlength(lab)) + 20
                if xs + wd > x0 + 930: xs = x0; yy += 28
                d.rectangle([xs, yy, xs + 22, yy + 22], fill=pal[s]); d.text((xs + 28, yy), lab, font=f(20), fill=(214, 218, 214)); xs += wd
        y += 70
    d.text((20, y), f"Số dòng chữ theo cỡ font (A + B + Unity, {sum(bf.values())} dòng). Vàng = trước ({len(bf)} cỡ), xanh = sau ({len(af)} cỡ). Chữ in sẵn của ảnh HUD không tính.", font=f(24), fill=(255, 220, 110))
    cats = sorted(set(bf) | set(af)); cw = (W - 80) // len(cats); base = y + 60 + 440; mx = max(list(bf.values()) + list(af.values()))
    for i, s in enumerate(cats):
        x = 40 + i * cw
        for j, (cnt, col) in enumerate(((bf, (255, 200, 80)), (af, (90, 170, 255)))):
            v = cnt.get(s, 0); h = int(430 * v / mx); xx = x + 2 + j * (cw // 2 - 2)
            if v: d.rectangle([xx, base - h, xx + cw // 2 - 4, base], fill=col); d.text((xx + (cw // 2 - 4) // 2, base - h - 4), str(v), font=f(14), fill=(237, 242, 238), anchor="ms")
        d.text((x + cw // 2, base + 16), f"{s:g}", font=f(16), fill=(237, 242, 238), anchor="mt")
    d.line([(40, base), (W - 40, base)], fill=(120, 110, 90), width=2)
    im.crop((0, 0, W, base + 50)).save(OUT / f"loan-co-{opt}.png")

def crops(store, opt):
    rows = []
    for name in NAMES:
        rb, ra, b, a, _, _ = store[(name, opt)]; seen = set(); n = 0
        for i in sorted(range(len(rb)), key=lambda i: (not any(c.isalpha() for c in rb[i][1]), rb[i][0])):
            t = rb[i][1]
            if t in seen or i >= len(ra) or ra[i][1] != t or len(rb[i]) < 3: continue
            seen.add(t); rows.append((NAMES[name].split(" (")[0], t, rb[i], ra[i], b, a)); n += 1
            if n == 3: break
    _, _, b, a, _, _ = store[("hud", opt)]
    fx = lambda s0, s1, lab, win: (("HUD ảnh gốc", lab, (s0, "", (0, 0, 0, 0)), (s1, "", (0, 0, 0, 0)), b, a, win))
    m = LAD[opt]
    rows += [fx(18, snap(18, m, True), "kill feed (in sẵn, ước ~18 px)", (0, 30, 600, 140)), fx(14, 20, "số phím quick bar (in sẵn, ước ~14 px)", (700, 965, 1300, 1075)), fx(32, snap(32, m, True), "số máu 100 (in sẵn, ước ~32 px)", (300, 860, 900, 970)), fx(25, snap(25, m, True), "1/1 và 1:21 (in sẵn, ước ~25 px)", (1500, 350, 1900, 460))]
    W = 1920; im = Image.new("RGB", (W, 40 + len(rows) * 150), (24, 22, 18)); d = ImageDraw.Draw(im); d.text((20, 6), f"Crop tỉ lệ 100% (không thu nhỏ): chữ nhỏ nhất mỗi màn, trái = trước, phải = sau (thang {opt} bậc)", font=orig_F(24), fill=(255, 220, 110))
    for j, row in enumerate(rows):
        scr, t, r0, r1, b_, a_ = row[:6]; fixed = row[6] if len(row) > 6 else None; y = 44 + j * 150
        for x0, r, src in ((460, r0, b_), (1200, r1, a_)):
            bb = r[2]; cx, cy = (bb[0] + bb[2]) // 2, (bb[1] + bb[3]) // 2; X = min(max(0, cx - 300), 1320); Y = min(max(0, cy - 55), 970); win = fixed or (X, Y, X + 600, Y + 110); im.paste(src.crop(win), (x0, y))
            d.text((x0, y + 114), f"{r[0]:g} px", font=orig_F(20), fill=(237, 242, 238))
        d.text((20, y + 20), scr, font=orig_F(22), fill=(237, 242, 238)); d.text((20, y + 56), t[:30], font=orig_F(20), fill=(214, 218, 214))
    im.save(OUT / f"crop-100-{opt}.png")

ROLES = {"5": [(96, "1,05", "H1 Số lớn", "hạng, đếm ngược, CHIẾN THẮNG", "HẠNG #4"), (40, "1,2", "H2 Tiêu đề", "tiêu đề màn và panel, nút L, số thống kê lớn", "Bảng xếp hạng · Ắ Ặ Ề"),
                 (30, "1,3", "B1 Thân lớn", "nút M, tên nổi bật, số HUD chính (máu)", "Tạo phòng · Quang Huy"), (24, "1,35", "B2 Thân", "nút S, thân, dòng danh sách, toast, tooltip, số HUD phụ; sàn chữ gameplay", "Bạn sẽ bị loại khỏi trận."),
                 (20, "1,4", "C Chú thích", "chú thích, nhãn phụ, số trên ô và keycap; sàn tuyệt đối", "Tiếp tế, rương gỗ, xác người chơi")],
         "7": [(96, "1,05", "D Số lớn", "hạng, đếm ngược, CHIẾN THẮNG", "HẠNG #4"), (64, "1,1", "N Số kết quả", "số thống kê kết quả, #hạng / 10", "04:38 · 12"), (40, "1,2", "H1 Tiêu đề màn", "tiêu đề màn, popup", "Bảng xếp hạng · Ắ Ặ Ề"),
               (32, "1,25", "H2 Tiêu đề panel", "tiêu đề panel, nút L", "Tạo phòng · Quang Huy"), (28, "1,3", "L Thân lớn", "nút M, tên nổi bật, số HUD chính, tên trong tooltip", "Phòng mặc định · Leo rank"), (24, "1,35", "B Thân", "nút S, thân, danh sách, toast, số HUD phụ; sàn chữ gameplay", "Bạn sẽ bị loại khỏi trận."),
               (20, "1,4", "C Chú thích", "chú thích, nhãn phụ, số trên ô và keycap; sàn tuyệt đối", "Tiếp tế, rương gỗ, xác người chơi")]}
def wrap(s, font, wmax):
    out, cur = [], ""
    for w in s.split():
        t = (cur + " " + w).strip()
        if font.getlength(t) <= wmax: cur = t
        else: out.append(cur); cur = w
    return out + [cur]
def ladder(opt):
    im = Image.new("RGB", (1920, 1080), (31, 29, 24)); d = ImageDraw.Draw(im); y = 30
    d.text((40, y), f"Thang chữ {opt} bậc (px cỡ font ở 1080p, Be Vietnam Pro Bold)", font=orig_F(34), fill=(255, 220, 110), stroke_width=2, stroke_fill=(10, 12, 12)); y += 56
    d.text((40, y), f"Chữ trên nút theo cỡ nút: S 48 = {BTN[opt][48]}, M 64 = {BTN[opt][64]}, L 80 = {BTN[opt][80]} px", font=orig_F(22), fill=(214, 218, 214)); y += 40
    for px, lh, nm, use, sample in ROLES[opt]:
        h = max(int(px * 1.3) + 8, 84); d.text((40, y + h // 2 - 16), f"{nm}", font=orig_F(24), fill=(237, 242, 238), anchor="lm"); d.text((40, y + h // 2 + 16), f"{px} px · lh {lh}", font=orig_F(20), fill=(214, 218, 214), anchor="lm")
        d.text((330, y + h // 2), sample, font=orig_F(px), fill=(237, 242, 238), anchor="lm", stroke_width=2, stroke_fill=(10, 12, 12))
        lines = wrap(use, orig_F(20), 1880 - 1330); oy = y + h // 2 - (len(lines) - 1) * 14
        for k, ln in enumerate(lines): d.text((1330, oy + k * 28), ln, font=orig_F(20), fill=(214, 218, 214), anchor="lm")
        d.line([(40, y + h + 4), (1880, y + h + 4)], fill=(60, 57, 46)); y += h + 14
    d.text((40, y + 6), "Dấu chồng (Ắ Ặ Ề Ộ) cao hơn chữ hoa khoảng 0,44 em: line-height một dòng không dưới 1,3; khối nhiều dòng 1,5.", font=orig_F(20), fill=(214, 218, 214))
    im.save(OUT / f"thang-{opt}-bac.png")

if __name__ == "__main__":
    store = {}; rep = {}; before_all = render_all(None)
    for opt in ("5", "7"):
        ladder(opt)
        for n in NAMES: pair(n, opt, store)
        after_all = render_all(opt); bf, af = hist(opt, before_all, after_all); loan_co(opt, store, bf, af); crops(store, opt)
        rep[opt] = {"cong": gates_all(opt, after_all), "so_co_truoc": len(bf), "so_co_sau": len(af), "dong_tong": sum(bf.values()), "dong_truoc_duoi_20": sum(v for k, v in bf.items() if k < 20), "dong_truoc_duoi_24": sum(v for k, v in bf.items() if k < 24), "dong_sau_duoi_24": sum(v for k, v in af.items() if k < 24)}
    (OUT / "demo-cong.json").write_text(json.dumps(rep, ensure_ascii=False, indent=1, default=float), encoding="utf-8"); print(json.dumps(rep, ensure_ascii=False, indent=1, default=float))
