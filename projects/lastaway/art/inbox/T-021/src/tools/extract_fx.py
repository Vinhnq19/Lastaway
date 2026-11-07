"""T-021 tach nen 3 anh GPT (feedback, emblems, vignette) thanh sprite trong b/assets. Chi tach nen, cat, doi co."""
import sys, pathlib
sys.path.insert(0, r"D:\Lastaway\projects\lastaway\art\inbox\T-015\src\tools")
import extract_t015 as E
from PIL import Image, ImageFilter
GEN = pathlib.Path(r"D:\Lastaway\projects\lastaway\art\inbox\T-021\src\gen"); OUT = pathlib.Path(r"D:\Lastaway\projects\lastaway\art\inbox\T-021\b\assets"); OUT.mkdir(parents=True, exist_ok=True)

def boxes(name, gap):
    im = E.key_magenta(Image.open(GEN / f"{name}.png")); bs = [list(b[:4]) for b in E.components(im, min_area=300)]
    ch = True
    while ch:
        ch = False
        for i in range(len(bs)):
            for j in range(i + 1, len(bs)):
                A, B = bs[i], bs[j]
                if not (A[2] + gap < B[0] or B[2] + gap < A[0] or A[3] + gap < B[1] or B[3] + gap < A[1]):
                    bs[i] = [min(A[0], B[0]), min(A[1], B[1]), max(A[2], B[2]), max(A[3], B[3])]; bs.pop(j); ch = True; break
            if ch: break
    return im, bs

def cut(name, gap, rowh, names, hs):
    im, bs = boxes(name, gap); bs.sort(key=lambda b: (((b[1] + b[3]) // 2) // rowh, b[0]))
    assert len(bs) == len(names), (name, len(bs), bs)
    for n, b, h in zip(names, bs, hs):
        s = im.crop(tuple(b)); s = s.resize((round(s.width * h / s.height), h), Image.LANCZOS) if h else s
        s.save(OUT / f"{n}.png"); print(n, s.size)

cut("fx-sheet-feedback", 120, 512, ["fx_hitmarker", "fx_damage_arc", "fx_danger_arrow", "fx_chip_poison", "fx_chip_slow", "fx_chip_stealth"], [0] * 6)
cut("fx-sheet-emblems", 60, 420, ["fx_emblem_win", "fx_emblem_lose", "fx_map_player", "fx_map_mark", "fx_map_drop", "fx_icon_disconnect", "fx_icon_eye", "fx_icon_skull"], [0] * 8)
v = E.key_magenta(Image.open(GEN / "fx-vignette.png")).resize((1920, 1080), Image.NEAREST)
a = v.getchannel("A").filter(ImageFilter.GaussianBlur(24)).point(lambda p: int(p * 0.38)); v.putalpha(a); v.save(OUT / "fx_vignette.png"); print("fx_vignette", v.size)
