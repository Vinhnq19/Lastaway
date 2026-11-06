"""T-015 luot 8: Vitals, Quick bar, Match cung vien 24 px (GPT src/v4/sheet-panels-r7.png). Chi tach nen + 9-slice + doi co + do rim."""
import sys, json, pathlib
sys.path.insert(0, str(pathlib.Path(__file__).parent))
from extract_t015 import *
from rim_measure import rim, rim2
ROOT = pathlib.Path(r"D:\Lastaway\projects\lastaway\art\inbox\T-015")
pcs, _ = pieces("v4/sheet-panels-r7"); assert len(pcs) == 3
vit, match, qb = sorted(pcs, key=lambda p: p.size[0] / p.size[1])
meta = json.load(open(ROOT / "meta.json", encoding="utf-8")); TARGET = 24
def src_rim(p):
    v = [rim2(p, c) for c in "LRTB"]; v = [x for x in v if x and x > 5]; return sum(v) / len(v)
def build(nm, src, size, cap, kmul=1.0):
    k = kmul * (TARGET - 3.5) / src_rim(src)   # 3.5 = ranh toi trong cung (hieu chuan tren minimap: rim2 20,5 <-> border 24)
    for s in (1, 2):
        out, b = nine(src, k * s, (size[0] * s, size[1] * s), cap)
        out.save(ROOT / f"{nm}{'@2x' if s == 2 else ''}.png")
        if s == 1:
            meta[nm] = {"size": list(size), "border_LRTB": [b[0], b[2], b[1], b[3]], "slice": False}
            print(nm, size, "k", round(k, 3), "rim2", [rim2(out, c) for c in "LRTB"], "rim+3.5", [rim2(out, c) + 3.5 for c in "LRTB"], "border", meta[nm]["border_LRTB"])
build("hud_panel_vitals", vit, (422, 206), (50, 50, 50, 50), 1.13)
build("hud_panel_quickbar", qb, (593, 112), (36, 36, 36, 36))
build("hud_panel_match", match, (318, 90), (40, 40, 40, 40), 1.05)
json.dump(meta, open(ROOT / "meta.json", "w", encoding="utf-8"), indent=1, ensure_ascii=False)
