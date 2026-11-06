"""T-015 luot 6: khung Quick bar khong tab (GPT src/v4/sheet-qb-r4.png). Script chi tach nen, 9-slice, doi co, @2x."""
import sys, json, pathlib
sys.path.insert(0, str(pathlib.Path(__file__).parent))
from extract_t015 import *
ROOT = pathlib.Path(r"D:\Lastaway\projects\lastaway\art\inbox\T-015")
pcs, _ = pieces("v4/sheet-qb-r4"); assert len(pcs) == 1, len(pcs)
src = pcs[0]; size = (593, 112); meta = json.load(open(ROOT / "meta.json", encoding="utf-8"))
for s in (1, 2):
    out, b = nine(src, size[1] * s / src.height, (size[0] * s, size[1] * s), (125, 125, 125, 125))
    out.save(ROOT / f"hud_panel_quickbar{'@2x' if s == 2 else ''}.png")
    if s == 1: meta["hud_panel_quickbar"] = {"size": list(size), "border_LRTB": [b[0], b[2], b[1], b[3]], "slice": False}
json.dump(meta, open(ROOT / "meta.json", "w", encoding="utf-8"), indent=1, ensure_ascii=False)
print(src.size, meta["hud_panel_quickbar"])
