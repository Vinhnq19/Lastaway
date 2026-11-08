"""T-015 luot 7: khung minimap vien mong (GPT src/v4/sheet-mm-r5.png). Chi tach nen + doi co + do border."""
import sys, json, pathlib
sys.path.insert(0, str(pathlib.Path(__file__).parent))
from extract_t015 import *
import numpy as np
ROOT = pathlib.Path(r"D:\Lastaway\projects\lastaway\art\inbox\T-015")
pcs, _ = pieces("v4/sheet-mm-r5"); assert len(pcs) == 1, len(pcs)
src = pcs[0]; size = (318, 316); meta = json.load(open(ROOT / "meta.json", encoding="utf-8"))
def measure(im):
    a = np.asarray(im.getchannel("A")); H, W = a.shape; cx, cy = W // 2, H // 2
    col = a[:, cx]; row = a[cy, :]
    ty = int(np.argmax(col > 128)); by = int(H - 1 - np.argmax(col[::-1] > 128))   # mep ngoai (ban dau)
    # lo = vung trong suot lien tuc tu tam
    def run(v, c):
        i = c
        while i > 0 and v[i] < 128: i -= 1
        j = c
        while j < len(v) - 1 and v[j] < 128: j += 1
        return i + 1, j - 1
    l, r = run(row, cx); t, b = run(col, cy)
    return {"L": int(l), "R": int(W - 1 - r), "T": int(t), "B": int(H - 1 - b), "hole": [int(l), int(t), int(r), int(b)],
            "hole_center": [(l + r) / 2, (t + b) / 2], "size": [W, H]}
for s in (1, 2):
    out = resize(src, (size[0] * s, size[1] * s)); out.save(ROOT / f"hud_minimap_frame{'@2x' if s == 2 else ''}.png")
    m = measure(out)
    if s == 1:
        meta["hud_minimap_frame"] = {"size": list(size), "border_LRTB": [m["L"], m["R"], m["T"], m["B"]], "hole": m["hole"], "slice": False}; print(m)
json.dump(meta, open(ROOT / "meta.json", "w", encoding="utf-8"), indent=1, ensure_ascii=False)
