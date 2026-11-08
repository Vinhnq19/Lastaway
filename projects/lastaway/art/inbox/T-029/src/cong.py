"""T-029 cong: tuong phan chu (>=4.5:1 voi p10/p90 nen quanh chu, bo qua pixel chu va vien) + META BORDER. python -P cong.py"""
import sys, json, pathlib
sys.stdout.reconfigure(encoding='utf-8')
import json, pathlib, subprocess, sys
from PIL import Image
T = pathlib.Path(__file__).parent.parent
def lum(c):
    f = lambda v: (v / 255 / 12.92) if v / 255 <= 0.03928 else (((v / 255 + 0.055) / 1.055) ** 2.4); r, g, b = c[:3]; return 0.2126 * f(r) + 0.7152 * f(g) + 0.0722 * f(b)
def ratio(a, b): a, b = sorted((a, b), reverse=True); return (a + 0.05) / (b + 0.05)
TXT = lum((241, 231, 200)); bad = 0; rows = []
for mock, bb, s, size in json.load(open(T / "src/van-ban.json", encoding="utf-8")):
    im = Image.open(T / ({"m1": "m1-minimap-toan-map", "m2": "m2-minimap-gan", "m3": "m3-ban-do-lon-waypoint"}[mock] + ".png")).convert("RGB")
    x0, y0, x1, y1 = [round(v) for v in bb]; px = []
    for y in range(y0 - 5, y1 + 6):
        for x in range(x0 - 5, x1 + 6):
            if x0 - 2 <= x <= x1 + 2 and y0 - 2 <= y <= y1 + 2: continue   # bo vung chu + vien
            px.append(lum(im.getpixel((x, y))))
    px.sort(); lo, hi = px[len(px) // 10], px[len(px) * 9 // 10]; r = min(ratio(TXT, lo), ratio(TXT, hi)); ok = r >= 4.5; bad += (not ok); rows.append((mock, s, size, round(r, 1), "dat" if ok else "LOI"))
for r in rows: print(*r)
print("TUONG PHAN:", f"{bad} loi / {len(rows)} dong chu")
print(subprocess.run([sys.executable, "-I", str(T.parent / "T-020/check_meta_border.py"), str(T / "files/meta.json")], capture_output=True, text=True).stdout.strip())
