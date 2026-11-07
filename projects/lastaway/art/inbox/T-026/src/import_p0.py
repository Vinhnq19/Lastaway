"""T-026: nhap P0 (icon, header, footer, o ma, keycap, the phong, o nguoi choi) vao kit T-020."""
import sys, pathlib, json
sys.argv = [sys.argv[0]]
import import_k2 as K
from import_k2 import *
G6 = ROOT / "src" / "gen6"; P0 = ROOT / "nut-p0"; P0.mkdir(exist_ok=True)
def boxes(path, gap, min_area=200):
    im = E.key_magenta(Image.open(path)); bs = [list(b[:4]) for b in E.components(im, min_area=min_area)]; ch = True
    while ch:
        ch = False
        for i in range(len(bs)):
            for j in range(i + 1, len(bs)):
                A, B = bs[i], bs[j]
                if not (A[2] + gap < B[0] or B[2] + gap < A[0] or A[3] + gap < B[1] or B[3] + gap < A[1]):
                    bs[i] = [min(A[0], B[0]), min(A[1], B[1]), max(A[2], B[2]), max(A[3], B[3])]; bs.pop(j); ch = True; break
            if ch: break
    return im, bs
def scale_h(p, H): return p.resize((max(1, round(p.width * H / p.height)), H), Image.LANCZOS)
def sprite_h(name, p, H, border):
    a = scale_h(p, H); b = scale_h(p, H * 2); K.put_sprite(name, a, b, a.size, border); return a.size
NAMES = ["gear", "map", "bag", "copy", "refresh", "person", "clock", "wifi", "sound", "music", "vibrate", "eye", "skull", "arrow_left", "arrow_right", "trophy"]
def icons():
    im, bs = boxes(G6 / "p0-a.png", 60); assert len(bs) == 16, len(bs)
    bs.sort(key=lambda b: (((b[1] + b[3]) // 2) // 300, b[0]))
    for n, b in zip(NAMES, bs):
        p = im.crop(tuple(b)); p.save(P0 / f"ico_{n}.png")
        def mk(S):
            k = (S - 8 * S // 48) / max(p.width, p.height); q = p.resize((max(1, round(p.width * k)), max(1, round(p.height * k))), Image.LANCZOS); c = Image.new("RGBA", (S, S), (0, 0, 0, 0)); c.alpha_composite(q, ((S - q.width) // 2, (S - q.height) // 2)); return c
        K.put_sprite(f"kit_ico_{n}", mk(48), mk(96), (48, 48), None)
    print("icons ok")
def strips():
    im, bs = boxes(G6 / "p0-b.png", 20, 400); assert len(bs) == 9, len(bs)
    bs.sort(key=lambda b: (((b[1] + b[3]) // 2) // 120, b[0])); crops = [im.crop(tuple(b)) for b in bs]
    for i, c in enumerate(crops): c.save(P0 / f"b_{i}.png")
    hd, ft = crops[0], crops[1]; print("header", sprite_h("kit_screen_header", hd, 104, [36, 36, 12, 12]), "footer", sprite_h("kit_dialog_footer", ft, 96, [8, 8, 8, 8]))
    for n, c in zip(("empty", "active", "filled", "error"), crops[2:6]): sprite_h(f"kit_codebox_{n}", c, 78, None)
    for n, c in zip(("normal", "active", "disabled"), crops[6:9]): sprite_h(f"kit_keycap_{n}", c, 52, [14, 14, 14, 14])
    print("strips ok")
def cards():
    im, bs = boxes(G6 / "p0-c.png", 20, 3000); assert len(bs) == 6, len(bs)
    left = sorted([b for b in bs if b[0] < 700], key=lambda b: b[1]); right = sorted([b for b in bs if b[0] >= 700], key=lambda b: b[1]); assert len(left) == 3 and len(right) == 3
    for n, b in zip(("normal", "selected", "full"), left): p = im.crop(tuple(b)); p.save(P0 / f"card_{n}.png"); print("card", n, sprite_h(f"kit_card_room_{n}", p, 120, [24, 24, 24, 24]))
    for n, b in zip(("empty", "notready", "ready"), right): p = im.crop(tuple(b)); p.save(P0 / f"slot_{n}.png"); print("slot", n, sprite_h(f"kit_slot_player_{n}", p, 96, [100, 40, 24, 24]))
icons(); strips(); cards()
(K.T20 / "meta.json").write_text(json.dumps(K.meta, ensure_ascii=False, indent=1), encoding="utf-8"); print("meta saved")
