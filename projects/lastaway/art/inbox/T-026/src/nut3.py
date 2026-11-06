"""T-026 v3: nut noi nhe, mau tru ngà theo vien da UI. Chay: PYTHONIOENCODING=utf-8 python nut3.py compare | slice <p1-6|p1-8|p2-6|p2-8>"""
import sys, pathlib, json
sys.path.insert(0, str(pathlib.Path(__file__).parent))
import nut, nut2
from nut import *
import mock as M
GEN3 = ROOT / "src" / "gen3"; NUT3 = ROOT / "nut-v3"
KEYS = [("p1-6", "P1 đá ngà + nhấn (xin đế 6)"), ("p1-8", "P1 đá ngà + nhấn (xin đế 8)"), ("p2-6", "P2 vàng/đỏ dịu (xin đế 6)"), ("p2-8", "P2 vàng/đỏ dịu (xin đế 8)")]

class Set3(nut.Set):
    def __init__(self, key):
        self.name = key; im = E.key_magenta(Image.open(GEN3 / f"nut-{key}.png")); bs = E.components(im, min_area=3000)
        assert len(bs) == 12, (key, len(bs))
        bs.sort(key=lambda b: (b[1] + b[3]) / 2); rows = [sorted(bs[i * 4:(i + 1) * 4], key=lambda b: b[0]) for i in range(3)]
        d = NUT3 / key; d.mkdir(parents=True, exist_ok=True); self.p = {}
        for mi, mat in enumerate(MATS):
            for si, st in enumerate(STATES):
                b = rows[mi][si]; p = im.crop(tuple(b[:4])); p.save(d / f"{mat}_{st}.png"); self.p[(mat, st)] = p
        self.hn = self.p[("primary", "normal")].height; self.base = {k: base_px(v) for k, v in self.p.items()}
        hp = self.p[("primary", "pressed")].height; self.at64 = self.base[("secondary", "normal")] * 64 / self.hn; self.sink = (self.hn - hp) * 64 / self.hn
        self.outline3 = False; self.dtxt = (122, 28, 22)
        for m in MATS: self.base[(m, "disabled")] = 3  # do tu dong sai tren nut tat (lech chu)
    def hexes(self):
        h = lambda m: "#%02X%02X%02X" % self.face_rgb(m, "normal"); return f"mặt: chính {h('primary')} · phụ {h('secondary')} · đỏ {h('danger')}"

def scene(im, x0, y0, S):
    nut2.scene(im, x0, y0, S)

def compare():
    v1 = nut.Set("ro"); v1.at64 = nut.base_px(v1.p[("primary", "normal")]) * 64 / v1.hn; v1.sink = (v1.hn - v1.p[("primary", "pressed")].height) * 64 / v1.hn
    sets = [("v1 Rõ hiện tại", v1)] + [(lab, Set3(k)) for k, lab in KEYS]
    Wd, Hd = 3040, 1850; im = Image.new("RGBA", (Wd, Hd), (31, 29, 24, 255)); im.alpha_composite(M.bg(0.62).resize((Wd, Hd))); M.W = Wd; xs = [40 + i * 600 for i in range(5)]
    for i, (lab, S) in enumerate(sets):
        S.info = f"đế {S.at64:.1f} px khi nút cao 64, nhấn lún {S.sink:.1f} px"; nut.column(im, xs[i], str(i), lab, S)
        text(im, (xs[i] + 280, 132), S.hexes() if hasattr(S, "hexes") else "mặt: chính #DC9D23 · phụ #5F5342 · đỏ #B4342A", 16, MUTED, "mm")
        text(im, (xs[i], 1420), "Ghép lên nền concept (cùng cỡ pixel với HUD và gạch map)", 18, MUTED, "lm"); scene(im, xs[i], 1440, S)
    text(im, (Wd / 2, 1825), "Nút nổi nhẹ, màu theo đá trắng ngà của viền khung UI (viền mẫu: sáng #E3D29F, trung bình #B79F6F). Khung hộp thoại giữ nguyên.", 20, MUTED, "mm")
    im.convert("RGB").save(ROOT / "so-sanh-nut-v3.png")
    rep = {}
    for lab, S in sets:
        rep[lab] = {"de_px_o_cao_64": round(S.at64, 1), "lun_px": round(S.sink, 1), "mat": S.hexes() if hasattr(S, "hexes") else "", "chu": {f"{m[:3]}_{s[:3]}": ("trang" if S.txtcol(m, s)[0] > 200 else "toi") + f" {S.txtratio(m, s):.2f}" for m in MATS for s in STATES}}
    (ROOT / "nut-v3-do.json").write_text(json.dumps(rep, ensure_ascii=False, indent=1), encoding="utf-8"); print(json.dumps(rep, ensure_ascii=False)[:3000])

def slicetest(key):
    S = Set3(key); sizes = [("S", 48), ("M", 64), ("L", 80)]; widths = [96, 240, 420]
    Wd, Hd = 1700, 780; im = Image.new("RGBA", (Wd, Hd), (43, 40, 33, 255)); text(im, (30, 30), f"9-slice v3 {key}: S 48, M 64, L 80 x rộng 96, 240, 420", 26, GOLD, "lm"); y = 80
    for sn, h in sizes:
        text(im, (30, y + h / 2), sn, 28, TXT, "lm"); x = 100
        for w in widths: S.draw(im, "primary", "normal", (x, y), (w, h), "Vào phòng" if w > 150 else "Vào", 24 if h < 60 else 28); x += w + 30
        S.draw(im, "secondary", "pressed", (x, y), (240, h), "Hủy", 24 if h < 60 else 28); x += 270; S.draw(im, "danger", "normal", (x, y), (240, h), "Xóa", 24 if h < 60 else 28); y += h + 70
    z = Image.new("RGBA", (240, 64), (0, 0, 0, 0)); S.draw(z, "primary", "normal", (0, 0), (240, 64), "", 26); im.alpha_composite(z.resize((720, 192), Image.NEAREST), (100, y + 10))
    z2 = Image.new("RGBA", (420, 80), (0, 0, 0, 0)); S.draw(z2, "secondary", "normal", (0, 0), (420, 80), "", 26); im.alpha_composite(z2.resize((840, 160), Image.NEAREST), (860, y + 10))
    im.convert("RGB").save(ROOT / f"nut-v3-9slice-{key}.png")
if __name__ == "__main__":
    a = sys.argv[1] if len(sys.argv) > 1 else "compare"
    if a == "compare": compare()
    elif a == "slice": slicetest(sys.argv[2])
