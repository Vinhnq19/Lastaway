import numpy as np
from PIL import Image
from rim_measure import lum
def corner_ext(im, run=6, thr=.05):
    """co khoi hoa van goc = khoang cach doc duong cheo tu goc ngoai den noi lo/long toi bat dau (px)."""
    a = np.asarray(im.convert("RGBA")).astype(float); H, W = a.shape[:2]; L = lum(a[..., :3]); A = a[..., 3] > 128; out = {}
    for nm, (sx, sy) in {"TL": (0, 0), "TR": (W - 1, 0), "BL": (0, H - 1), "BR": (W - 1, H - 1)}.items():
        dx = 1 if sx == 0 else -1; dy = 1 if sy == 0 else -1; vals = []; ops = []
        for t in range(0, int(min(W, H) * .6)): vals.append(L[sy + dy * t, sx + dx * t]); ops.append(A[sy + dy * t, sx + dx * t])
        s = int(np.argmax(ops)); res = None
        for i in range(s + 3, len(vals) - run):
            if (not ops[i]) or all(v < thr for v in vals[i:i + run]): res = i; break
        out[nm] = res
    return out
def gap_check(*a): pass
if __name__ == "__main__":
    import sys
    for n in ["hud_minimap_frame", "hud_panel_vitals", "hud_panel_quickbar", "hud_panel_match"]:
        print(n, corner_ext(Image.open(f"../../{n}.png")))
