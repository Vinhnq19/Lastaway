import sys, pathlib
from PIL import Image
sys.stdout.reconfigure(encoding="utf-8")
OUT = pathlib.Path(r"D:\Lastaway\projects\lastaway\art\inbox\T-015")
def lin(c):
    c /= 255; return c / 12.92 if c <= .03928 else ((c + .055) / 1.055) ** 2.4
def L(p): return .2126 * lin(p[0]) + .7152 * lin(p[1]) + .0722 * lin(p[2])
def cr(a, b):
    x, y = L(a), L(b); hi, lo = max(x, y), min(x, y); return (hi + .05) / (lo + .05)
def hexc(h): return tuple(int(h[i:i + 2], 16) for i in (1, 3, 5))
def slab(img, box):
    px = img.crop(box).convert("RGBA").getdata()
    v = sorted(L(p) for p in px if p[3] > 250); return v[int(len(v) * .95)], v[len(v) // 2], v[int(len(v) * .99)]
def bgcol(l):  # luminance -> grey with same luminance (cr only depends on luminance)
    g = ((l + 0.0) ** (1 / 2.4) * 1.055 - .055) * 255; return (g, g, g)
print("Bang do tuong phan (nen = diem sang p95 va p99 cua mat da trong long panel, xau nhat)")
rows = []
v = Image.open(OUT / "hud_panel_vitals.png"); q = Image.open(OUT / "hud_panel_quickbar.png"); m = Image.open(OUT / "hud_panel_match.png")
s_v = slab(v, (44, 24, 267, 149)); s_q = slab(q, (48, 22, 544, 104)); s_m = slab(m, (34, 18, 206, 71))
dt = Image.open(OUT / "dung-thu-hud-A.png").convert("RGB")
# killfeed: 72% panel tren world: lay vung trong cua dung-thu (khong co chu): x 250-285
kf = dt.crop((250, 50, 284, 160)).getdata(); kv = sorted(L(p) for p in kf); s_k = (kv[int(len(kv) * .95)], kv[len(kv) // 2], kv[int(len(kv) * .99)])
items = [
 ("Vitals", s_v, [("so HP 24px trang EDF2EE", "#EDF2EE", 3.0), ("so giap/mu, nhan 'muted' A6BBB7", "#A6BBB7", 4.5), ("icon mau do E5484D", "#E5484D", 3.0), ("icon giap/mu 5BB8E8", "#5BB8E8", 3.0), ("icon Adrenaline F5C542", "#F5C542", 3.0)]),
 ("Quick bar", s_q, [("ten vu khi EDF2EE", "#EDF2EE", 4.5), ("so phim kem F1E7C8 (co vien den)", "#F1E7C8", 4.5)]),
 ("Match status", s_m, [("so 1/7 va 1:22 EDF2EE", "#EDF2EE", 4.5), ("dong ho bo A274FF (22px)", "#A274FF", 3.0), ("icon nguoi EDF2EE", "#EDF2EE", 3.0), ("icon dong ho A274FF", "#A274FF", 3.0)]),
 ("Killfeed", s_k, [("dong 1 EDF2EE", "#EDF2EE", 4.5), ("dong 2-3 D6DAD6", "#D6DAD6", 4.5)]),
]
print("| Khoi | Nen: median / p95 / p99 (do sang) | Mau chu hoac icon | cr o p95 | cr o p99 | Nguong | Dat |\n|---|---|---|---:|---:|---:|---|")
for name, (p95, med, p99), lst in items:
    for lab, h, th in lst:
        c = hexc(h); a = cr(c, bgcol(p95)); b = cr(c, bgcol(p99))
        print(f"| {name} | {med:.3f} / {p95:.3f} / {p99:.3f} | {lab} | {a:.2f} | {b:.2f} | {th} | {'Dat' if b >= th else ('Dat o p95' if a >= th else 'KHONG')} |")
