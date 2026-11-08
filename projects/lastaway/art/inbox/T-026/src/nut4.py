"""T-026 v4: nut mat kaki theo crop vien cua Huy. Chay: PYTHONIOENCODING=utf-8 python nut4.py compare | slice <k1|k2>"""
import sys, pathlib, json
sys.path.insert(0, str(pathlib.Path(__file__).parent))
import nut, nut2, nut3
from nut import *
import mock as M
GEN4 = ROOT / "src" / "gen4"; NUT4 = ROOT / "nut-v4"
HUY = Image.open(ROOT / "ref-mau-vien-huy.png").convert("RGBA")
class Set4(nut3.Set3):
    def __init__(self, key):
        self.name = key; im = E.key_magenta(Image.open(GEN4 / f"nut-{key}.png")); bs = E.components(im, min_area=3000); assert len(bs) == 12, (key, len(bs))
        bs.sort(key=lambda b: (b[1] + b[3]) / 2); rows = [sorted(bs[i * 4:(i + 1) * 4], key=lambda b: b[0]) for i in range(3)]
        d = NUT4 / key; d.mkdir(parents=True, exist_ok=True); self.p = {}
        for mi, mat in enumerate(MATS):
            for si, st in enumerate(STATES):
                b = rows[mi][si]; p = im.crop(tuple(b[:4])); p.save(d / f"{mat}_{st}.png"); self.p[(mat, st)] = p
        self.hn = self.p[("primary", "normal")].height; self.base = {k: base_px(v) for k, v in self.p.items()}
        for m in MATS: self.base[(m, "disabled")] = 3
        hp = self.p[("primary", "pressed")].height; self.at64 = self.base[("secondary", "normal")] * 64 / self.hn; self.sink = (self.hn - hp) * 64 / self.hn
        self.outline3 = False; self.dtxt = {"k2": (85, 20, 15), "k1": (61, 14, 11)}[key]
def compare():
    s3 = nut3.Set3("p2-8"); s3.at64 = s3.base[("secondary", "normal")] * 64 / s3.hn
    sets = [("P2 xin 8 (v3, ngà)", s3), ("K2 phụ kaki", Set4("k2")), ("K1 cả ba kaki", Set4("k1"))]
    Wd, Hd = 1900, 1850; im = Image.new("RGBA", (Wd, Hd), (31, 29, 24, 255)); im.alpha_composite(M.bg(0.62).resize((Wd, Hd))); M.W = Wd; xs = [40, 660, 1280]
    big = HUY.resize((HUY.width * 3 // 2, HUY.height * 3 // 2), Image.NEAREST)
    for i, (lab, S) in enumerate(sets):
        S.info = f"đế {S.at64:.1f} px khi nút cao 64, nhấn lún {S.sink:.1f} px"; nut.column(im, xs[i], str(i), lab, S)
        text(im, (xs[i] + 280, 132), S.hexes(), 16, MUTED, "mm"); im.alpha_composite(big, (xs[i] + 4, 4))
        text(im, (xs[i], 1420), "Ghép lên nền concept (cùng cỡ pixel với HUD và gạch map)", 18, MUTED, "lm"); nut2.scene(im, xs[i], 1440, S)
    text(im, (Wd / 2, 1825), "Crop viền của Huy ở góc trên trái mỗi cột (kaki #AA9360 đến #B09A66, mép sáng #CEB681, bóng #94835A). Chữ Thoát trận đỏ đậm: v3 #7A1C16, K2 #55140F, K1 #3D0E0B.", 20, MUTED, "mm")
    im.convert("RGB").save(ROOT / "so-sanh-nut-v4.png")
    rep = {}
    for lab, S in sets:
        rr = {st: round(min(S.worst("secondary", st, S.dtxt) for _ in [0]), 2) for st in STATES[:3]}
        rep[lab] = {"de_px_o_cao_64": round(S.at64, 1), "lun_px": round(S.sink, 1), "mat": S.hexes(), "chu_do_dam_tren_phu": rr, "chu": {f"{m[:3]}_{s[:3]}": ("trang" if S.txtcol(m, s)[0] > 200 else "toi") + f" {S.txtratio(m, s):.2f}" for m in MATS for s in STATES[:3]}}
    (ROOT / "nut-v4-do.json").write_text(json.dumps(rep, ensure_ascii=False, indent=1), encoding="utf-8"); print(json.dumps(rep, ensure_ascii=False)[:3000])
def slicetest(key):
    S = Set4(key); sizes = [("S", 48), ("M", 64), ("L", 80)]; widths = [96, 240, 420]
    Wd, Hd = 1700, 780; im = Image.new("RGBA", (Wd, Hd), (43, 40, 33, 255)); text(im, (30, 30), f"9-slice v4 {key}: S 48, M 64, L 80 x rộng 96, 240, 420", 26, GOLD, "lm"); y = 80
    for sn, h in sizes:
        text(im, (30, y + h / 2), sn, 28, TXT, "lm"); x = 100
        for w in widths: S.draw(im, "primary", "normal", (x, y), (w, h), "Vào phòng" if w > 150 else "Vào", 24 if h < 60 else 28); x += w + 30
        S.draw(im, "secondary", "pressed", (x, y), (240, h), "Hủy", 24 if h < 60 else 28); x += 270; S.draw(im, "danger", "normal", (x, y), (240, h), "Xóa", 24 if h < 60 else 28); y += h + 70
    z = Image.new("RGBA", (240, 64), (0, 0, 0, 0)); S.draw(z, "primary", "normal", (0, 0), (240, 64), "", 26); im.alpha_composite(z.resize((720, 192), Image.NEAREST), (100, y + 10))
    z2 = Image.new("RGBA", (420, 80), (0, 0, 0, 0)); S.draw(z2, "secondary", "normal", (0, 0), (420, 80), "", 26); im.alpha_composite(z2.resize((840, 160), Image.NEAREST), (860, y + 10))
    im.convert("RGB").save(ROOT / f"nut-v4-9slice-{key}.png")
if __name__ == "__main__":
    a = sys.argv[1] if len(sys.argv) > 1 else "compare"
    if a == "compare": compare()
    elif a == "slice": slicetest(sys.argv[2])
