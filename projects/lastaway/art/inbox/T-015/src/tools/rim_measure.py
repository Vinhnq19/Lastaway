import numpy as np
lin = lambda c: np.where(c / 255 <= .03928, c / 255 / 12.92, ((c / 255 + .055) / 1.055) ** 2.4)
def lum(a): return .2126 * lin(a[..., 0]) + .7152 * lin(a[..., 1]) + .0722 * lin(a[..., 2])
def rim(im, side, run=8, thr=.055, at=.5, hole=False):
    """do day vien (px) tu mep ngoai vao den noi bat dau lon toi lien tuc (>= run px) hoac lo trong suot."""
    a = np.asarray(im.convert("RGBA")).astype(float); H, W = a.shape[:2]
    L = lum(a[..., :3]); A = a[..., 3] > 128
    if side in "LR":
        v = L[int(H * at)]; al = A[int(H * at)]
    else:
        v = L[:, int(W * at)]; al = A[:, int(W * at)]
    if side in "RB": v = v[::-1]; al = al[::-1]
    s = int(np.argmax(al)); n = len(v)
    for i in range(s, n - run):
        if (not al[i]) if hole else bool((v[i:i + run] < thr).all() and i - s > 8): return i - s
    return None

def rim2(im, side, at=.5, run=14, thr=.06, light=.09):
    """day vien = vi tri diem sang cuoi cung (da vien) truoc doan toi >= run px (hoac lo trong suot); do tu mep ngoai."""
    a = np.asarray(im.convert("RGBA")).astype(float); H, W = a.shape[:2]; L = lum(a[..., :3]); A = a[..., 3] > 128
    v, al = (L[int(H * at)], A[int(H * at)]) if side in "LR" else (L[:, int(W * at)], A[:, int(W * at)])
    if side in "RB": v = v[::-1]; al = al[::-1]
    s = int(np.argmax(al)); n = len(v); last = s
    for i in range(s, n - run):
        if not al[i] or (v[i:i + run] < thr).all(): return last - s + 1
        if v[i] > light: last = i
    return None
