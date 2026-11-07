"""T-026/T-021: nhap P1 (the thong ke, the nguyen nhan, o vat pham, the chi tiet, duong chia) vao kit T-020."""
import sys; sys.argv = [sys.argv[0]]
import import_p0 as P
from import_p0 import *
im, bs = P.boxes(ROOT / "src" / "gen8" / "p1.png", 20, 100); print(len(bs)); assert len(bs) == 8, len(bs)
bs.sort(key=lambda b: (((b[1] + b[3]) // 2) // 150, b[0]))
# hang 1: stat, cause; hang 2: 4 o; hang 3: tip; hang 4: divider
stat, cause = bs[0], bs[1]; slots = bs[2:6]; tip, div = bs[6], bs[7]
assert len(bs) >= 8
def put(name, b, H, border):
    p = im.crop(tuple(b)); p.save(P0 / f"{name}.png"); return P.sprite_h(name, p, H, border)
print("stat", put("kit_card_stat", stat, 130, [28, 28, 28, 28]))
print("cause", put("kit_card_cause", cause, 150, [110, 28, 28, 28]))
for n, b in zip(("normal", "selected", "empty", "drag"), slots): print("slot", n, put(f"kit_slot_item_{n}", b, 140, None))
print("tip", put("kit_tip_item", tip, 230, [30, 30, 30, 30]))
print("div", put("kit_divider", div, 14, [10, 10, 2, 2]))
(K.T20 / "meta.json").write_text(json.dumps(K.meta, ensure_ascii=False, indent=1), encoding="utf-8"); print("ok")
