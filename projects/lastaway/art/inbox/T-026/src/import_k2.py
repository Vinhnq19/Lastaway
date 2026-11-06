"""T-026: nhap K2 vao kit T-020 (nut, nut icon, tab, nut dong, icon X dam). Sao luu ban cu vao T-020/_bo/. Chay: python import_k2.py buttons|family|sheet"""
import sys, pathlib, json, shutil
sys.path.insert(0, str(pathlib.Path(__file__).parent))
import nut
from nut import *
T20 = pathlib.Path(r"D:\Lastaway\projects\lastaway\art\inbox\T-020"); BO = T20 / "_bo"; K2 = ROOT / "nut-v4" / "k2"; G5 = ROOT / "src" / "gen5"; FAM = ROOT / "nut-ho"
meta = json.loads((T20 / "meta.json").read_text(encoding="utf-8"))
def backup(names):
    BO.mkdir(exist_ok=True)
    for n in names:
        for suf in (".png", "@2x.png"):
            s = T20 / f"{n}{suf}"; d = BO / f"{n}{suf}"
            if s.exists() and not d.exists(): shutil.copy2(s, d)
    mb = BO / "meta.before-T026.json"
    if not mb.exists(): shutil.copy2(T20 / "meta.json", mb)
def save(name, im, border, size1x, sl=True):
    im.save(T20 / f"{name}.png") if im.size == size1x else None
def put_sprite(name, im1, im2, size, border):
    im1.save(T20 / f"{name}.png"); im2.save(T20 / f"{name}@2x.png"); meta[name] = {"size": list(size), "border_LRTB": border, "slice": border is not None}
def canvas_state(piece, ref_h, size, bottom=True):
    """piece cao ref_h la nut normal; ve vao canvas size, giu ti le cao, can day; chieu ngang 3-slice"""
    w, h = size; ph = round(h * piece.height / ref_h); q = nut.hslice(piece, (w, ph)); c = Image.new("RGBA", size, (0, 0, 0, 0)); c.alpha_composite(q, (0, h - ph)); return c
def buttons():
    names = [f"kit_btn_{m}_{s}" for m in ("primary", "secondary", "danger") for s in STATES]; backup(names)
    for m in ("primary", "secondary", "danger"):
        pcs = {s: Image.open(K2 / f"{m}_{s}.png").convert("RGBA") for s in STATES}; rh = pcs["normal"].height
        for s in STATES:
            a = canvas_state(pcs[s], rh, (240, 64)); b = canvas_state(pcs[s], rh, (480, 128)); put_sprite(f"kit_btn_{m}_{s}", a, b, (240, 64), [22, 22, 28, 28])
    (T20 / "meta.json").write_text(json.dumps(meta, ensure_ascii=False, indent=1), encoding="utf-8"); print("buttons ok")
def cut(path, n, rowsplit, gap=60):
    im = E.key_magenta(Image.open(path)); bs = E.components(im, min_area=800)
    return im, bs
def family():
    FAM.mkdir(exist_ok=True)
    # nut icon
    im = E.key_magenta(Image.open(G5 / "nut-icon.png")); bs = E.components(im, min_area=3000); assert len(bs) == 8, len(bs)
    bs.sort(key=lambda b: (b[1] + b[3]) / 2); rows = [sorted(bs[:4], key=lambda b: b[0]), sorted(bs[4:], key=lambda b: b[0])]
    backup([f"kit_iconbtn_{sh}_{s}" for sh in ("round", "square") for s in STATES])
    for r, sh in zip(rows, ("round", "square")):
        pcs = {s: im.crop(tuple(b[:4])) for s, b in zip(STATES, r)}; rh = pcs["normal"].height
        for s in STATES:
            p = pcs[s]; p.save(FAM / f"iconbtn_{sh}_{s}.png")
            def mk(H):
                k = H / rh; q = p.resize((max(1, round(p.width * k)), max(1, round(p.height * k))), Image.LANCZOS); W = H; c = Image.new("RGBA", (W, H), (0, 0, 0, 0)); c.alpha_composite(q, ((W - q.width) // 2, H - q.height)); return c
            put_sprite(f"kit_iconbtn_{sh}_{s}", mk(64), mk(128), (64, 64), None)
    # tab + close
    im = E.key_magenta(Image.open(G5 / "nut-tab-close.png")); bs = E.components(im, min_area=2000); assert len(bs) == 7, len(bs)
    bs.sort(key=lambda b: (b[1] + b[3]) / 2); tabs = sorted(bs[:3], key=lambda b: b[0]); closes = sorted(bs[3:], key=lambda b: b[0])
    backup(["kit_tab_off", "kit_tab_hover", "kit_tab_on"])
    tp = {n: im.crop(tuple(b[:4])) for n, b in zip(("off", "hover", "on"), tabs)}; ref = max(p.height for p in tp.values())
    for n, p in tp.items():
        p.save(FAM / f"tab_{n}.png")
        def mkt(H, W):
            ph = round(H * p.height / ref); q = nut.hslice(p, (W, ph)); c = Image.new("RGBA", (W, H), (0, 0, 0, 0)); c.alpha_composite(q, (0, H - ph)); return c
        put_sprite(f"kit_tab_{n}", mkt(56, 160), mkt(112, 320), (160, 56), [19, 19, 19, 1])
    cp = {s: im.crop(tuple(b[:4])) for s, b in zip(STATES, closes)}; rh = cp["normal"].height
    for s, p in cp.items():
        p.save(FAM / f"close_{s}.png")
        def mkc(H):
            k = H / rh; q = p.resize((max(1, round(p.width * k)), max(1, round(p.height * k))), Image.LANCZOS); c = Image.new("RGBA", (H, H), (0, 0, 0, 0)); c.alpha_composite(q, ((H - q.width) // 2, H - q.height)); return c
        put_sprite(f"kit_close_{s}", mkc(52), mkc(104), (52, 52), None)
    # icon X dam
    im = E.key_magenta(Image.open(G5 / "nut-x.png")); bs = sorted(E.components(im, min_area=500), key=lambda b: b[0]); assert len(bs) >= 2, len(bs)
    big = im.crop(tuple(bs[0][:4])); big.save(FAM / "icon_x_dam.png")
    for sz in (32, 64):
        pass
    a = big.resize((32, round(32 * big.height / big.width)), Image.LANCZOS); b = big.resize((64, round(64 * big.height / big.width)), Image.LANCZOS)
    put_sprite("kit_icon_cross_dark", a, b, a.size, None)
    (T20 / "meta.json").write_text(json.dumps(meta, ensure_ascii=False, indent=1), encoding="utf-8"); print("family ok", a.size)
if __name__ == "__main__":
    if sys.argv[1] in ("buttons", "family"): {"buttons": buttons, "family": family}[sys.argv[1]]()

def ink():
    src = Image.open(FAM / "icon_x_dam.png").convert("RGBA"); px = src.load(); w, h = src.size; out = Image.new("RGBA", src.size)
    o = out.load(); base = (0x1F, 0x1A, 0x10); hi = (0x4A, 0x40, 0x30)
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if a == 0: continue
            t = max(0.0, min(1.0, (r - 85) / (138 - 85)))  # 0x55 nen -> 0, 0x8A vien sang -> 1
            o[x, y] = (round(base[0] + (hi[0] - base[0]) * t), round(base[1] + (hi[1] - base[1]) * t), round(base[2] + (hi[2] - base[2]) * t), a)
    out.save(FAM / "icon_x_muc.png")
    a = out.resize((32, round(32 * h / w)), Image.LANCZOS); b = out.resize((64, round(64 * h / w)), Image.LANCZOS)
    put_sprite("kit_icon_cross_ink", a, b, a.size, None); (T20 / "meta.json").write_text(json.dumps(meta, ensure_ascii=False, indent=1), encoding="utf-8"); print("ink ok", a.size)
def tabs2():
    im = E.key_magenta(Image.open(G5 / "nut-tab3.png")); bs = E.components(im, min_area=2000); assert len(bs) == 3, len(bs)
    bs.sort(key=lambda b: b[0]); tp = {n: im.crop(tuple(b[:4])) for n, b in zip(("off", "hover", "on"), bs)}; ref = tp["on"].height
    for n, p in tp.items():
        p.save(FAM / f"tab2_{n}.png")
        def mkt(H, W):
            ph = round(H * p.height / ref); q = nut.hslice(p, (W, ph)); c = Image.new("RGBA", (W, H), (0, 0, 0, 0)); c.alpha_composite(q, (0, H - ph)); return c
        put_sprite(f"kit_tab_{n}", mkt(64, 160), mkt(128, 320), (160, 64), [19, 19, 19, 1])
    (T20 / "meta.json").write_text(json.dumps(meta, ensure_ascii=False, indent=1), encoding="utf-8"); print("tabs2 ok", {n: p.size for n, p in tp.items()})
if __name__ == "__main__" and len(sys.argv) > 1 and sys.argv[1] in ("ink", "tabs2"):
    {"ink": ink, "tabs2": tabs2}[sys.argv[1]]()
