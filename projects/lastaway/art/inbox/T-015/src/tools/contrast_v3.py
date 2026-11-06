import sys, pathlib
from PIL import Image, ImageFilter
sys.stdout.reconfigure(encoding="utf-8")
ROOT = pathlib.Path(r"D:\Lastaway\projects\lastaway\art\inbox\T-015"); ICON = pathlib.Path(r"D:\Lastaway\projects\lastaway\art\inbox\hud-icons\png")
lin = lambda c: (c / 255) / 12.92 if c / 255 <= .03928 else ((c / 255 + .055) / 1.055) ** 2.4
L = lambda p: .2126 * lin(p[0]) + .7152 * lin(p[1]) + .0722 * lin(p[2])
cr = lambda a, b: (max(a, b) + .05) / (min(a, b) + .05)
hx = lambda h: tuple(int(h[i:i + 2], 16) for i in (1, 3, 5))
def slab(img, box):
    v = sorted(L(p) for p in img.crop(box).convert("RGBA").get_flattened_data() if p[3] > 250) if hasattr(Image.Image, "get_flattened_data") else sorted(L(p) for p in img.crop(box).convert("RGBA").getdata() if p[3] > 250)
    return v[int(len(v) * .95)], v[int(len(v) * .99)]
def icon_l(n):
    im = Image.open(ICON / f"icon_{n}@2x.png").convert("RGBA")
    og = Image.open(ICON.parent / "png-vector-goc" / f"icon_{n}@2x.png").convert("RGBA").getchannel("A").filter(ImageFilter.MinFilter(3))
    ls = sorted(L(p) for p, o in zip(im.getdata(), og.getdata()) if o > 250); return sum(ls) / len(ls), ls[int(len(ls) * .05)]
for v in sys.argv[1:]:
    d = ROOT / "lan3" / v
    S = {"Vitals": slab(Image.open(d / "hud_panel_vitals.png"), (44, 24, 267, 149)), "Quick bar": slab(Image.open(d / "hud_panel_quickbar.png"), (48, 22, 544, 104)), "Match": slab(Image.open(d / "hud_panel_match.png"), (34, 18, 206, 71))}
    print(f"\n### {v}\n\n| Khối | nền p95 / p99 | mục | màu | cr p99 | ngưỡng | Đạt |\n|---|---|---|---|---:|---:|---|")
    rows = [("Vitals", "số HP", "#EDF2EE", 3.0), ("Vitals", "số giáp/mũ muted", "#A6BBB7", 4.5), ("Quick bar", "tên vũ khí", "#EDF2EE", 4.5), ("Quick bar", "số phím kem", "#F1E7C8", 4.5), ("Match", "số 1/7", "#EDF2EE", 4.5), ("Match", "số bo kem", "#F1E7C8", 3.0)]
    for k, lab, c, th in rows:
        p95, p99 = S[k]; x = cr(L(hx(c)), p99); print(f"| {k} | {p95:.3f} / {p99:.3f} | {lab} | {c} | {x:.2f} | {th} | {'Đạt' if x >= th else 'KHÔNG'} |")
    for n, k in (("hp", "Vitals"), ("armor", "Vitals"), ("helmet", "Vitals"), ("adrenaline", "Vitals"), ("alive", "Match"), ("zone-timer", "Match")):
        m, p5 = icon_l(n); p99 = S[k][1]; x = cr(p5, p99); print(f"| {k} | {S[k][0]:.3f} / {p99:.3f} | icon {n} (p5 ruột) | L {p5:.3f} | {x:.2f} | 4,5 | {'Đạt' if x >= 4.5 else 'KHÔNG'} |")
