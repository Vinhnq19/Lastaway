"""Cong META BORDER: border_LRTB moi canh < canh anh, L+R < rong, T+B < cao. Dung: python -I check_meta_border.py [meta.json ...]"""
import json, sys, pathlib
I = pathlib.Path(__file__).resolve().parent.parent
fs = sys.argv[1:] or [I/"T-020/meta.json", I/"T-028-unity-pack/files/kit/meta.json", I/"T-028-unity-pack/files/new/meta.json"]
bad = 0
for f in fs:
    for k, v in json.load(open(f, encoding="utf-8")).items():
        b, s = v.get("border_LRTB"), v.get("size")
        if not b or not s: continue
        L, R, T, B = b; w, h = s
        if not (L < w and R < w and T < h and B < h and L + R < w and T + B < h):
            bad += 1; print("BAD", f, k, s, b)
print("META BORDER:", "0 loi" if not bad else f"{bad} loi"); sys.exit(1 if bad else 0)
