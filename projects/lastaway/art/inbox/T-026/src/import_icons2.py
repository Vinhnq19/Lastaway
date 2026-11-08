"""T-026: nhap 11 icon muc bo sung + X do #94281F vao kit T-020."""
import sys; sys.argv = [sys.argv[0]]
import import_p0 as P
from import_p0 import *
G7 = ROOT / "src" / "gen7"
im, bs = P.boxes(G7 / "icons2.png", 50, 150); assert len(bs) == 12, len(bs)
bs.sort(key=lambda b: (((b[1] + b[3]) // 2) // 300, b[0]))
names = ["play", "plus", "logout", "door", "trash", "check", "key", "home", "server", "list", "people"]
for n, b in zip(names, bs[:11]):
    p = im.crop(tuple(b)); p.save(P0 / f"ico_{n}.png")
    def mk(S):
        k = (S - 8 * S // 48) / max(p.width, p.height); q = p.resize((max(1, round(p.width * k)), max(1, round(p.height * k))), Image.LANCZOS); c = Image.new("RGBA", (S, S), (0, 0, 0, 0)); c.alpha_composite(q, ((S - q.width) // 2, (S - q.height) // 2)); return c
    K.put_sprite(f"kit_ico_{n}", mk(48), mk(96), (48, 48), None)
x = im.crop(tuple(bs[11])); x.save(P0 / "icon_x_red.png")
a = x.resize((32, round(32 * x.height / x.width)), Image.LANCZOS); b = x.resize((64, round(64 * x.height / x.width)), Image.LANCZOS); K.put_sprite("kit_icon_cross_red", a, b, a.size, None)
(K.T20 / "meta.json").write_text(json.dumps(K.meta, ensure_ascii=False, indent=1), encoding="utf-8"); print("ok", a.size)
