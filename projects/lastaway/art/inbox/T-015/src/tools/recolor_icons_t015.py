import sys, shutil, pathlib, json
from PIL import Image, ImageFilter, ImageChops
sys.stdout.reconfigure(encoding="utf-8")
ICON = pathlib.Path(r"D:\Lastaway\projects\lastaway\art\inbox\hud-icons\png")
BAK = ICON.parent / "png-vector-goc"
BAK.mkdir(exist_ok=True)

# (ten file, mau kim loai sang, mau kim loai toi, ghi chu)
METAL = {
    "hp":         ("#FFAA84", "#EE8F68", "dong do"),
    "armor":      ("#C3CBCC", "#8E999C", "sat cu xam"),
    "helmet":     ("#EFC463", "#D3A04A", "dong thau"),
    "helmet-alt": ("#EFC463", "#D3A04A", "dong thau"),
    "adrenaline": ("#FFE08A", "#E8B945", "vang sang"),
    "alive":      ("#F2ECD8", "#C9C2AA", "bac kem"),
    "kill":       ("#F2ECD8", "#C9C2AA", "bac kem"),
    "zone-timer": ("#F4EAC9", "#D2C7A4", "bac kem am"),
}
hexc = lambda h: tuple(int(h[i:i + 2], 16) for i in (1, 3, 5))
OUTLINE = (20, 16, 14)
OUT_A = 150     # vien nhe: alpha 150/255 (truoc 255)
FLAT = 0.5      # 0 = gradient day du (luot 2), 1 = mot mau; luot 3 = 0.5

def metal(src, hi, lo, px):
    im = src.convert("RGBA"); w, h = im.size
    a = im.getchannel("A"); a_bin = a.point(lambda v: 255 if v > 127 else 0)
    # nen: gradient doc sang tren, toi duoi
    grad = Image.new("RGB", (w, h)); g = grad.load()
    for y in range(h):
        t = y / max(1, h - 1)
        c = tuple(int(hi[i] * (1 - t) + lo[i] * t) for i in range(3)); mid = tuple((hi[i] + lo[i]) // 2 for i in range(3)); c = tuple(int(c[i] * (1 - FLAT) + mid[i] * FLAT) for i in range(3))
        for x in range(w): g[x, y] = c
    # vien sang gan canh tren-trai (cat canh): lech anh mask 1 px
    sh = ImageChops.offset(a_bin, px, px)
    edge = ImageChops.subtract(a_bin, sh)          # phan nhin thay o canh tren-trai
    fill = grad.convert("RGBA"); fill.putalpha(a)
    # vien toi 'px' pixel quanh icon
    ring = a_bin.filter(ImageFilter.MaxFilter(2 * px + 1))
    out = Image.new("RGBA", (w, h), OUTLINE + (0,)); out.putalpha(ring.point(lambda v: OUT_A if v else 0))
    out.alpha_composite(fill)
    return out

meta = {}
for name, (hi, lo, note) in METAL.items():
    for suf, px in (("", 1), ("@2x", 2)):
        f = ICON / f"icon_{name}{suf}.png"
        bak = BAK / f.name
        if not bak.exists(): shutil.copy(f, bak)          # luon dung ban vector goc lam nguon
        out = metal(Image.open(bak), hexc(hi), hexc(lo), px)
        bb = out.getchannel("A").getbbox(); s = out.size
        if bb and (bb[0] == 0 or bb[1] == 0 or bb[2] == s[0] or bb[3] == s[1]): print("CANH BI CAT", f.name, bb)
        out.save(f)
    meta[name] = {"sang": hi, "toi": lo, "chat": note}
(ICON.parent / "metal.json").write_text(json.dumps(meta, ensure_ascii=False, indent=1), encoding="utf-8")
print("ok")
