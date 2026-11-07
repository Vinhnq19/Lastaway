"""T-026 v2: nut khoi dung chat lieu khop game (da, go, dong). Chay: PYTHONIOENCODING=utf-8 python nut2.py compare | slice <a|b|c>"""
import sys, pathlib, json
sys.path.insert(0, str(pathlib.Path(__file__).parent))
import nut
from nut import *
import mock as M
GEN2 = ROOT / "src" / "gen2"; NUT2 = ROOT / "nut-v2"
CONCEPT = Image.open(r"D:\Lastaway\projects\lastaway\art\brief\T-015\concept-toan-man-huy-07-11.png").convert("RGBA").resize((1920, 1080), Image.LANCZOS)
V2 = [("a", "(a) Đá khắc, chính ốp vàng"), ("b", "(b) Đá + rune phát sáng"), ("c", "(c) Gỗ thùng + đồng, phụ đá")]

class Set2(nut.Set):
    def __init__(self, key, suf="r2"):
        self.name = key; sheet = f"nut-v2{key}{suf}"; im = E.key_magenta(Image.open(GEN2 / f"{sheet}.png")); bs = E.components(im, min_area=3000)
        if len(bs) != 12:  # dung nhom cac manh ro hon (rune roi) neu thua
            bs = sorted(bs, key=lambda b: -(b[2] - b[0]) * (b[3] - b[1]))[:12]
        bs.sort(key=lambda b: (b[1] + b[3]) / 2); rows = [sorted(bs[i * 4:(i + 1) * 4], key=lambda b: b[0]) for i in range(3)]
        d = NUT2 / key; d.mkdir(parents=True, exist_ok=True); self.p = {}
        for mi, mat in enumerate(MATS):
            for si, st in enumerate(STATES):
                b = rows[mi][si]; p = im.crop(tuple(b[:4])); p.save(d / f"{mat}_{st}.png"); self.p[(mat, st)] = p
        self.outline3 = True; self.hn = self.p[("primary", "normal")].height; bn = 0.18 * self.hn  # uoc theo mat: day ~18% chieu cao; do tu dong that bai tren mat da co van
        self.base = {k: (bn if k[1] in ("normal", "hover") else (bn * 0.3 if k[1] == "pressed" else 3)) for k in self.p}
        hp = self.p[("primary", "pressed")].height; self.at64 = bn * 64 / self.hn; self.sink = (self.hn - hp) * 64 / self.hn

def scene(im, x0, y0, S, w=560, h=330):
    """ghep nut len nen concept that (vung quanh Vitals + tuong) de soi dong bo"""
    crop = CONCEPT.crop((0, 600, w, 600 + h)); im.alpha_composite(crop, (x0, y0)); d = ImageDraw.Draw(im); d.rectangle([x0, y0, x0 + w, y0 + h], outline=(150, 146, 120), width=3)
    S.draw(im, "primary", "normal", (x0 + 20, y0 + 20), (240, 64), "Vào phòng", 24); S.draw(im, "secondary", "normal", (x0 + 290, y0 + 20), (240, 64), "Tạo phòng", 24)
    S.draw(im, "secondary", "hover", (x0 + 20, y0 + 110), (240, 64), "Hover", 24); S.draw(im, "primary", "pressed", (x0 + 290, y0 + 110), (240, 64), "Nhấn", 24)

def old_set():
    S = nut.Set("ro"); S.info = ""; return S

def compare():
    sets = [("Rõ hiện tại (v1)", old_set())]
    for k, lab in V2: sets.append((lab, Set2(k)))
    Wd, Hd = 2400, 1820; im = Image.new("RGBA", (Wd, Hd), (31, 29, 24, 255)); im.alpha_composite(M.bg(0.62).resize((Wd, Hd))); M.W = Wd; xs = [40, 640, 1240, 1840]
    for i, (lab, S) in enumerate(sets):
        if i == 0: S.at64 = nut.base_px(S.p[("primary", "normal")]) * 64 / S.hn; S.sink = (S.hn - S.p[("primary", "pressed")].height) * 64 / S.hn; S.info = f"đế {S.at64:.0f} px khi nút cao 64, nhấn lún {S.sink:.0f} px"
        else: S.info = f"đế {S.at64:.0f} px khi nút cao 64 (1080p), nhấn lún {S.sink:.0f} px"
        nut.column(im, xs[i], str(i), lab, S)
        text(im, (xs[i], 1420), "Ghép lên nền concept (cùng cỡ pixel với HUD và gạch map)", 18, MUTED, "lm"); scene(im, xs[i], 1440, S)
    text(im, (Wd / 2, 1800), "Nút vẫn nổi khối (mặt + đế, lún khi nhấn); chất liệu theo concept toàn màn: đá gạch, gỗ thùng, rune. Khung hộp thoại giữ olive + rêu.", 20, MUTED, "mm")
    im.convert("RGB").save(ROOT / "so-sanh-nut-v2.png")
    rep = {}
    for lab, S in sets:
        rep[lab] = {"de_px_o_cao_64": round(S.at64, 1), "lun_px": round(S.sink, 1), "chu": {f"{m}_{s}": ("trang" if S.txtcol(m, s)[0] > 200 else "toi") + f" {S.txtratio(m, s):.2f}" for m in MATS for s in STATES}}
    (ROOT / "nut-v2-do.json").write_text(json.dumps(rep, ensure_ascii=False, indent=1), encoding="utf-8"); print(json.dumps(rep, ensure_ascii=False)[:2500])

def slicetest(key):
    S = Set2(key); sizes = [("S", 48), ("M", 64), ("L", 80)]; widths = [96, 240, 420]
    Wd, Hd = 1700, 780; im = Image.new("RGBA", (Wd, Hd), (43, 40, 33, 255)); text(im, (30, 30), f"9-slice phương án v2{key}: S 48, M 64, L 80 x rộng 96, 240, 420", 26, GOLD, "lm"); y = 80
    for sn, h in sizes:
        text(im, (30, y + h / 2), sn, 28, TXT, "lm"); x = 100
        for w in widths: S.draw(im, "primary", "normal", (x, y), (w, h), "Vào phòng" if w > 150 else "Vào", 24 if h < 60 else 28); x += w + 30
        S.draw(im, "secondary", "pressed", (x, y), (240, h), "Hủy", 24 if h < 60 else 28); x += 270; S.draw(im, "danger", "normal", (x, y), (240, h), "Xóa", 24 if h < 60 else 28); y += h + 70
    z = Image.new("RGBA", (240, 64), (0, 0, 0, 0)); S.draw(z, "primary", "normal", (0, 0), (240, 64), "", 26); im.alpha_composite(z.resize((720, 192), Image.NEAREST), (100, y + 10))
    z2 = Image.new("RGBA", (420, 80), (0, 0, 0, 0)); S.draw(z2, "secondary", "normal", (0, 0), (420, 80), "", 26); im.alpha_composite(z2.resize((840, 160), Image.NEAREST), (860, y + 10))
    im.convert("RGB").save(ROOT / f"nut-v2-9slice-{key}.png")
if __name__ == "__main__":
    a = sys.argv[1] if len(sys.argv) > 1 else "compare"
    if a == "compare": compare()
    elif a == "slice": slicetest(sys.argv[2])
