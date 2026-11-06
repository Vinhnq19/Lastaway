import sys, json, pathlib
from collections import deque
from PIL import Image
sys.stdout.reconfigure(encoding="utf-8")
SRC = pathlib.Path(r"D:\Lastaway\projects\lastaway\art\inbox\T-015\src")
OUT = pathlib.Path(r"D:\Lastaway\projects\lastaway\art\inbox\T-015")

def key_magenta(im):
    """flat #FF00FF background -> alpha 0, with soft edge and magenta despill on edge pixels"""
    im = im.convert("RGB"); w, h = im.size; px = im.load()
    out = Image.new("RGBA", (w, h)); op = out.load()
    for y in range(h):
        for x in range(w):
            r, g, b = px[x, y]
            d = (255 - r) + g + (255 - b)          # distance from magenta
            if d < 70: op[x, y] = (0, 0, 0, 0)
            elif d < 150:
                a = int(255 * (d - 70) / 80)
                # despill: pull toward grey-ish by removing magenta tint
                m = min(r, b); g2 = max(g, 0); r2 = min(r, g2 + (r - m) // 3); b2 = min(b, g2 + (b - m) // 3)
                op[x, y] = (r2, g2, b2, a)
            else: op[x, y] = (r, g, b, 255)
    from PIL import ImageFilter
    al = out.getchannel("A").filter(ImageFilter.MinFilter(5)); out.putalpha(al)
    px2 = out.load()
    for y in range(h):
        for x in range(w):
            r, g, b, a = px2[x, y]
            if a and r > g + 45 and b > g + 25: px2[x, y] = (32, 30, 28, a)
    return out

def components(rgba, min_area=2000):
    w, h = rgba.size; a = rgba.getchannel("A").load(); seen = bytearray(w * h); boxes = []
    for y0 in range(0, h, 1):
        for x0 in range(0, w, 1):
            if a[x0, y0] > 20 and not seen[y0 * w + x0]:
                q = deque([(x0, y0)]); seen[y0 * w + x0] = 1; x1 = x2 = x0; y1 = y2 = y0; n = 0
                while q:
                    x, y = q.popleft(); n += 1
                    x1 = min(x1, x); x2 = max(x2, x); y1 = min(y1, y); y2 = max(y2, y)
                    for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                        nx, ny = x + dx, y + dy
                        if 0 <= nx < w and 0 <= ny < h and not seen[ny * w + nx] and a[nx, ny] > 20:
                            seen[ny * w + nx] = 1; q.append((nx, ny))
                if n >= min_area: boxes.append((x1, y1, x2 + 1, y2 + 1, n))
    return boxes

def pieces(sheet):
    im = key_magenta(Image.open(SRC / f"{sheet}.png"))
    bs = components(im)
    # merge boxes that overlap (e.g. plate with detached ends)
    changed = True
    while changed:
        changed = False
        for i in range(len(bs)):
            for j in range(i + 1, len(bs)):
                A, B = bs[i], bs[j]
                if not (A[2] + 20 < B[0] or B[2] + 20 < A[0] or A[3] + 20 < B[1] or B[3] + 20 < A[1]):
                    bs[i] = (min(A[0], B[0]), min(A[1], B[1]), max(A[2], B[2]), max(A[3], B[3]), A[4] + B[4]); bs.pop(j); changed = True; break
            if changed: break
    bs.sort(key=lambda b: (round(b[1] / 150), b[0]))
    return [im.crop(b[:4]) for b in bs], bs

def resize(im, size):
    # premultiplied-alpha downscale to avoid dark/magenta fringes
    from PIL import ImageChops
    r, g, b, a = im.split()
    pm = Image.merge("RGBA", (ImageChops.multiply(r, a), ImageChops.multiply(g, a), ImageChops.multiply(b, a), a))
    pm = pm.resize(size, Image.LANCZOS)
    r, g, b, a = pm.split(); ap = a.load(); rp, gp, bp = r.load(), g.load(), b.load()
    out = Image.new("RGBA", size); op = out.load()
    for y in range(size[1]):
        for x in range(size[0]):
            al = ap[x, y]
            if al == 0: op[x, y] = (0, 0, 0, 0)
            else: op[x, y] = (min(255, rp[x, y] * 255 // al), min(255, gp[x, y] * 255 // al), min(255, bp[x, y] * 255 // al), al)
    return out

def nine(im, k_h, target, border_src):
    """scale by k_h (uniform), then 9-slice stretch to target; border_src=(L,T,R,B) in source px. returns (img, border_out)"""
    L, T, R, B = border_src
    sw, sh = im.size
    kw = k_h
    l, t, r, b = [max(1, round(v * k_h)) for v in (L, T, R, B)]
    tw, th = target
    # cut source into 9 parts, resize each to final size
    xs_src = [0, L, sw - R, sw]; ys_src = [0, T, sh - B, sh]
    xs_dst = [0, l, tw - r, tw]; ys_dst = [0, t, th - b, th]
    out = Image.new("RGBA", target, (0, 0, 0, 0))
    for j in range(3):
        for i in range(3):
            part = im.crop((xs_src[i], ys_src[j], xs_src[i + 1], ys_src[j + 1]))
            dw, dh = xs_dst[i + 1] - xs_dst[i], ys_dst[j + 1] - ys_dst[j]
            if dw < 1 or dh < 1: continue
            out.paste(resize(part, (dw, dh)), (xs_dst[i], ys_dst[j]))
    return out, (l, r, t, b)

if __name__ == "__main__":
    res = {}
    P, bp = pieces("sheet-panels"); print("panels", len(P), [b[:4] for b in bp])
    S, bs = pieces("sheet-slots"); print("slots", len(S), [b[:4] for b in bs])
    (OUT / "debug").mkdir(exist_ok=True)
    for i, p in enumerate(P): p.save(OUT / "debug" / f"panel{i}.png")
    for i, p in enumerate(S): p.save(OUT / "debug" / f"slot{i}.png")
