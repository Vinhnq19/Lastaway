"""T-015 luot 4: cat bo icon GPT (src/v4/sheet-icons.png) ve canvas spec. Khong ve tay: chi tach nen, cat, doi co."""
import sys, pathlib
sys.path.insert(0, str(pathlib.Path(__file__).parent))
from extract_t015 import *
OUTI = pathlib.Path(r"D:\Lastaway\projects\lastaway\art\inbox\hud-icons\png")
pcs, boxes = pieces("v4/sheet-icons")
order = ["hp", "armor", "helmet", "adrenaline", "alive", "zone-timer", "ammo", "kill"]
assert len(pcs) == 8, len(pcs)
# (canvas, chieu lon nhat cua hinh trong canvas @1x)
SPEC = {"hp": (44, 38), "armor": (50, 46), "helmet": (46, 43), "adrenaline": (44, 39), "alive": (40, 37), "zone-timer": (40, 37), "ammo": (24, 21), "kill": (32, 28)}
for n, im in zip(order, pcs):
    cv, big = SPEC[n]
    for s, suf in ((1, ""), (2, "@2x")):
        k = big * s / max(im.size); sz = (max(1, round(im.width * k)), max(1, round(im.height * k)))
        r = resize(im, sz); c = Image.new("RGBA", (cv * s, cv * s), (0, 0, 0, 0)); c.alpha_composite(r, ((cv * s - sz[0]) // 2, (cv * s - sz[1]) // 2))
        c.save(OUTI / f"icon_{n}{suf}.png")
        if n == "helmet": c.save(OUTI / f"icon_helmet-alt{suf}.png")
    print(n, im.size)
