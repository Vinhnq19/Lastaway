"""T-015 luot 5: sprite tu GPT (src/v4/sheet-frames.png, sheet-slots.png). Script chi tach nen, cat, 9-slice, doi co 1080p, @2x. Khong ve tay."""
import sys, json, pathlib
sys.path.insert(0, str(pathlib.Path(__file__).parent))
from extract_t015 import *
import numpy as np
ROOT = pathlib.Path(r"D:\Lastaway\projects\lastaway\art\inbox\T-015")

def by_aspect(pcs):
    return sorted(pcs, key=lambda p: p.width / p.height)
fr, _ = pieces("v4/sheet-frames-r3")
assert len(fr) == 5, len(fr)
mini, vit, kill, match, qb = by_aspect(fr)           # 1.0, 1.86, 2.15, 3.1, 3.66
sl, _ = pieces("v4/sheet-slots"); assert len(sl) == 4, len(sl)
sq = sorted([p for p in sl if abs(p.width / p.height - 1) < .2], key=lambda p: 0)  # giu thu tu doc (trai -> phai)
tracks = by_aspect([p for p in sl if p.width / p.height > 3])
slot_e, slot_s = sq[0], sq[1]; track_plain, track_gold = tracks[0], tracks[1]
meta = {}

def save(nm, im, s):
    im.save(ROOT / f"{nm}{'@2x' if s == 2 else ''}.png")

def panel(nm, src, size, cap, simple=True):
    for s in (1, 2):
        k = size[1] * s / src.height
        out, b = nine(src, k, (size[0] * s, size[1] * s), cap)
        save(nm, out, s)
        meta.setdefault(nm, {"size": list(size), "border_LRTB": [b[0], b[2], b[1], b[3]] if s == 1 else None})
        if s == 2: meta[nm]["border_LRTB_2x"] = [b[0], b[2], b[1], b[3]]

CAP = (72, 72, 72, 72)   # L,T,R,B trong px nguon
panel("hud_panel_vitals", vit, (422, 206), CAP)
panel("hud_panel_match", match, (318, 69), (72, 60, 72, 60))
panel("hud_minimap_frame", mini, (318, 316), CAP)
kf = kill
panel("hud_killfeed_plate", kf, (270, 130), (30, 30, 30, 30))
# killfeed: nen toi mo 60%
for suf in ("", "@2x"):
    p = ROOT / f"hud_killfeed_plate{suf}.png"; im = Image.open(p).convert("RGBA"); im.putalpha(im.getchannel("A").point(lambda v: int(v * .6))); im.save(p)

# quick bar co tab: chia 5 cot, chi keo hai doan phang hai ben tab
def quickbar(src, size, tab_extra=72):
    W, H = src.size; al = np.asarray(src.getchannel("A"))
    xs = np.where(al[int(H * .17)] > 128)[0]; t0, t1 = xs.min(), xs.max()           # chan doc cua tab
    xt = np.where(al[3] > 128)[0]; f0, f1 = xt.min(), xt.max()                      # mep phang tren cung cua tab
    cap = 80; l0, l1 = cap, max(cap + 10, t0 - 10); m0, m1 = f0 + 60, f1 - 60; r0, r1 = min(W - cap - 10, t1 + 10), W - cap
    segs = [(0, l0), (l0, l1), (l1, m0), (m0, m1), (m1, r0), (r0, r1), (r1, W)]; kind = ["f", "s", "f", "t", "f", "s", "f"]
    for s_ in (1, 2):
        k = size[1] * s_ / H; tw, th = size[0] * s_, size[1] * s_
        base = [round((b - a_) * k) for (a_, b) in segs]
        widths = list(base); widths[3] += tab_extra * s_
        rest = tw - sum(widths); sw = sum(b - a_ for (a_, b), kd in zip(segs, kind) if kd == "s")
        for i, ((a_, b), kd) in enumerate(zip(segs, kind)):
            if kd == "s": widths[i] += round(rest * (b - a_) / sw)
        widths[-1] += tw - sum(widths)
        out = Image.new("RGBA", (tw, th), (0, 0, 0, 0)); x = 0; xpos = []
        for (a_, b), w_ in zip(segs, widths):
            xpos.append(x); part = src.crop((a_, 0, b, H)); out.alpha_composite(resize(part, (max(1, w_), th)), (x, 0)); x += w_
        save("hud_panel_quickbar", out, s_)
        if s_ == 1:
            tb0 = xpos[2]; tb1 = xpos[4] + widths[4]     # tab o chan doc (px @1x)
            meta["hud_panel_quickbar"] = {"size": list(size), "border_LRTB": None, "slice": False, "tab_x_base": [tb0, tb1]}
    return t0, t1
quickbar(qb, (593, 159), 90)

# slot 60x60, thanh
for s in (1, 2):
    save("hud_slot_empty", resize(slot_e, (54 * s, 54 * s)), s); save("hud_slot_selected", resize(slot_s, (54 * s, 54 * s)), s)
meta["hud_slot_empty"] = {"size": [54, 54], "border_LRTB": [16] * 4}; meta["hud_slot_selected"] = meta["hud_slot_empty"]
def track(nm, src, size, cap):
    for s in (1, 2):
        out, b = nine(src, size[1] * s / src.height, (size[0] * s, size[1] * s), cap); save(nm, out, s)
        meta.setdefault(nm, {"size": list(size), "border_LRTB": [b[0], b[2], b[1], b[3]]})
track("hud_bar_track_hp", track_plain, (236, 32), (50, 40, 50, 40))
track("hud_bar_track_thin", track_gold, (242, 24), (40, 30, 40, 30))
track("hud_ammo_plate", track_plain, (276, 19), (50, 40, 50, 40))
json.dump(meta, open(ROOT / "meta.json", "w", encoding="utf-8"), indent=1, ensure_ascii=False)
print(json.dumps(meta, ensure_ascii=False)[:600])
