"""T-015 luot 9: Vitals long phang chia 3 dai dam nhat (GPT src/v4/sheet-vit-r8.png). Chi tach nen + 9-slice + doi co + do."""
import sys, json, pathlib
sys.path.insert(0, str(pathlib.Path(__file__).parent))
from extract_t015 import *
from rim_measure import rim2, lum
import numpy as np
ROOT = pathlib.Path(r"D:\Lastaway\projects\lastaway\art\inbox\T-015")
pcs, _ = pieces("v4/sheet-vit-r11"); assert len(pcs) == 3; src = min(pcs, key=lambda p: p.size[0] / p.size[1])   # r9 = sua sheet r7, rim/goc giong het r7
meta = json.load(open(ROOT / "meta.json", encoding="utf-8")); size = (422, 206)
k = 0.653   # = he so r7 (rim/goc khong doi); rim2 L,R khong dung duoc vi dai giua sang hon
print("src", src.size, "rim2 T,B", [rim2(src, c) for c in "TB"])
for s in (1, 2):
    out, b = nine(src, k * s, (size[0] * s, size[1] * s), (50, 50, 50, 50)); out.save(ROOT / f"hud_panel_vitals{'@2x' if s == 2 else ''}.png")
    if s == 1: meta["hud_panel_vitals"] = {"size": list(size), "border_LRTB": [b[0], b[2], b[1], b[3]], "slice": False}; print("k", round(k, 3), "border", meta["hud_panel_vitals"]["border_LRTB"], "rim2 T,B", [rim2(out, c) for c in "TB"])
json.dump(meta, open(ROOT / "meta.json", "w", encoding="utf-8"), indent=1, ensure_ascii=False)
a = np.asarray(Image.open(ROOT / "hud_panel_vitals.png").convert("RGB")).astype(float); L = lum(a)
col = L[:, 211]; print("L theo y (cot giua):", [(y, round(float(col[y]), 3)) for y in range(20, 190, 6)])
