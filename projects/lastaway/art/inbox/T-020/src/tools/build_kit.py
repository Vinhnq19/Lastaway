"""T-020 UI kit skin A: sprite tu sheet GPT (src/*.png). Script chi tach nen, cat, 9-slice, doi co, do. Khong ve tay."""
import sys, json, pathlib
sys.path.insert(0, r"D:\Lastaway\projects\lastaway\art\inbox\T-015\src\tools")
import extract_t015 as E
from extract_t015 import pieces, resize, nine
from rim_measure import rim2
import numpy as np
from PIL import Image
HERE = pathlib.Path(r"D:\Lastaway\projects\lastaway\art\inbox\T-020"); E.SRC = HERE / "src"
meta = json.load(open(HERE / "meta.json", encoding="utf-8")) if (HERE / "meta.json").exists() else {}
only = set(sys.argv[1:])
def want(g): return not only or g in only

def rim_color(im, side, run=6, tol=20):
    """day vien cho linh kien long khong toi: tu mep ngoai den noi mau lan can on dinh voi mau long (tam)."""
    a = np.asarray(im.convert("RGBA")).astype(int); H, W = a.shape[:2]
    c = np.median(a[H // 2 - 4:H // 2 + 4, W // 2 - 4:W // 2 + 4, :3].reshape(-1, 3), axis=0)
    v = a[H // 2, :, :] if side in "LR" else a[:, W // 2, :]
    if side in "RB": v = v[::-1]
    s = int(np.argmax(v[:, 3] > 128))
    for i in range(s + 3, len(v) - run):
        if all(np.abs(v[j, :3] - c).max() <= tol and v[j, 3] > 128 for j in range(i, i + run)): return i - s
    return None

def save(nm, im, s=1):
    im.save(HERE / f"{nm}{'@2x' if s == 2 else ''}.png")

def build_nine(nm, src, size, k, cap):
    for s in (1, 2):
        out, b = nine(src, k * s, (size[0] * s, size[1] * s), cap); save(nm, out, s)
        if s == 1: meta[nm] = {"size": list(size), "border_LRTB": [b[0], b[2], b[1], b[3]], "slice": True}
    return meta[nm]["border_LRTB"]

def build_fit(nm, src, h):
    """doi co deu theo chieu cao h (giu ti le), khong 9-slice."""
    for s in (1, 2):
        w = round(src.width * h * s / src.height); save(nm, resize(src, (w, h * s)), s)
        if s == 1: meta[nm] = {"size": [w, h], "border_LRTB": None, "slice": False}

log = {}
if want("buttons"):
    P, _ = pieces("sheet-buttons"); assert len(P) == 12
    types = ["primary", "secondary", "danger"]; states = ["normal", "hover", "pressed", "disabled"]
    for i, p in enumerate(P):
        r, c = divmod(i, 3); nm = f"kit_btn_{types[c]}_{states[r]}"
        k = 64 / p.height; b = build_nine(nm, p, (240, 64), k, (34, 34, 34, 34))
        log[nm] = {"rim_src": [rim_color(p, x) for x in "LRTB"], "border": b}
    print("buttons rim_src (normal primary)", log["kit_btn_primary_normal"], "k", round(64 / P[0].height, 3))
if want("small"):
    P, _ = pieces("sheet-small"); assert len(P) == 15
    names = [f"kit_iconbtn_round_{s}" for s in ("normal", "hover", "pressed", "disabled")] + [f"kit_iconbtn_square_{s}" for s in ("normal", "hover", "pressed", "disabled")] + \
        ["kit_toggle_off", "kit_toggle_on", "kit_checkbox_off", "kit_checkbox_on", "kit_badge", "kit_knob", "kit_arrow_down"]
    hs = [64] * 8 + [48, 48, 40, 40, 24, 32, 10]
    for nm, p, h in zip(names, P, hs):
        build_fit(nm, p, h); log[nm] = {"rim_src": [rim_color(p, x) for x in "LRTB"] if "arrow" not in nm else None, "k": round(h / p.height, 3)}
    print("iconbtn rim_src", log["kit_iconbtn_square_normal"], "k", log["kit_iconbtn_square_normal"]["k"])
if want("panels"):
    P, _ = pieces("sheet-panels3"); assert len(P) == 4
    P2, _ = pieces("sheet-popup2"); assert len(P2) == 1, len(P2)
    S0 = P[0]; v = [rim2(S0, c) for c in "LRTB"]; v = [x for x in v if x and x > 5]; rs = sum(v) / len(v)
    k = (24 - 3.5) / rs; cap = round(2.3 * rs)   # cap phu het ca khoi goc + reu (>= 40 px dau ra), goc chep nguyen pixel tu nguon S
    for nm, size in zip(["kit_panel_s", "kit_panel_m", "kit_panel_l"], [(360, 240), (640, 400), (1000, 640)]):
        b = build_nine(nm, S0, size, k, (cap, cap, cap, cap)); out = Image.open(HERE / f"{nm}.png")
        log[nm] = {"rim_src": round(rs, 1), "k": round(k, 3), "cap": cap, "border": b, "rim_out": [rim2(out, c) + 3.5 for c in "LRTB"]}; print(nm, log[nm])
    # popup: nen = 9-slice cua S (4 goc chep nguyen); thanh tieu de lay tu nguon GPT popup2 (phan giua + hai dai ben), khong de len goc
    size = (720, 480); b = build_nine("kit_panel_popup", S0, size, k, (cap, cap, cap, cap)); base = Image.open(HERE / "kit_panel_popup.png").convert("RGBA")
    pp = P2[0]; v2 = [rim2(pp, c) for c in "LRB"]; rs2 = sum(v2) / 3; k2 = (24 - 3.5) / rs2
    from rim_measure import lum
    Lc = lum(np.asarray(pp.convert("RGB")).astype(float))[:, pp.width // 2]; band = [y for y in range(int(rs2) + 4, pp.height // 2) if .040 < Lc[y] < .2]; bot = max(band)
    cap2 = round(1.45 * rs2); capT = bot + 3
    tmp, _b = nine(pp, k2, size, (cap2, capT, cap2, cap2)); tb = round((bot - rs2) * k2); cc = round(cap * k)
    for box in ((cc, 24, size[0] - cc, 24 + tb), (24, cc, cc, 24 + tb), (size[0] - cc, cc, size[0] - 24, 24 + tb)):
        base.paste(tmp.crop(box), box[:2])
    def fill_corners(img, src_tmp, sc):
        a = np.asarray(img).astype(int); t = np.asarray(src_tmp).astype(int); H, W = a.shape[:2]; sa_ = np.asarray(Image.open(HERE / f"kit_panel_s{'@2x' if sc == 2 else ''}.png").convert("RGBA")).astype(int)
        ic = np.median(sa_[sa_.shape[0] // 2 - 4:sa_.shape[0] // 2 + 4, sa_.shape[1] // 2 - 4:sa_.shape[1] // 2 + 4, :3].reshape(-1, 3), axis=0)
        y1 = (24 + tb) * sc
        for x0, x1 in ((0, cc * sc), ((size[0] - cc) * sc, size[0] * sc)):
            reg = a[24 * sc:y1, x0:x1]; m = (np.abs(reg[..., :3] - ic).max(axis=2) < 8) & (reg[..., 3] > 250)
            reg[m] = t[24 * sc:y1, x0:x1][m]
        return Image.fromarray(a.astype("uint8"), "RGBA")
    base = fill_corners(base, tmp, 1)   # phan lon goc tren la lon dark cua S: thay bang mau thanh tieu de (khong cham ornament, reu, vien)
    save("kit_panel_popup", base)
    big = nine(S0, k * 2, (size[0] * 2, size[1] * 2), (cap, cap, cap, cap))[0]; tmp2, _ = nine(pp, k2 * 2, (size[0] * 2, size[1] * 2), (cap2, capT, cap2, cap2))
    for box in ((cc * 2, 48, (size[0] - cc) * 2, (24 + tb) * 2), (48, cc * 2, cc * 2, (24 + tb) * 2), ((size[0] - cc) * 2, cc * 2, (size[0] - 24) * 2, (24 + tb) * 2)): big.paste(tmp2.crop(box), box[:2])
    big = fill_corners(big, tmp2, 2); save("kit_panel_popup", big, 2)
    meta["kit_panel_popup"] = {"size": list(size), "border_LRTB": [cc, cc, 24 + tb + 2, cc], "slice": True, "title_bar_px": tb}
    out = Image.open(HERE / "kit_panel_popup.png"); log["kit_panel_popup"] = {"rim_src": round(rs, 1), "k": round(k, 3), "cap": cap, "title_bar_px": tb, "rim_out": [rim2(out, c) + 3.5 for c in "LRB"]}; print("popup", log["kit_panel_popup"])
    # tu kiem: diff 4 goc 40x40 so voi S (@1x va @2x)
    def corners(a, n):
        H, W = a.shape[:2]; return {"TL": a[:n, :n], "TR": a[:n, W - n:], "BL": a[H - n:, :n], "BR": a[H - n:, W - n:]}
    rep = []
    sa = {1: np.asarray(Image.open(HERE / "kit_panel_s.png").convert("RGBA")).astype(int), 2: np.asarray(Image.open(HERE / "kit_panel_s@2x.png").convert("RGBA")).astype(int)}
    for nm in ("kit_panel_m", "kit_panel_l", "kit_panel_popup"):
        for sc in (1, 2):
            a = np.asarray(Image.open(HERE / f"{nm}{'@2x' if sc == 2 else ''}.png").convert("RGBA")).astype(int); n = 40 * sc
            cs, ca = corners(sa[sc], n), corners(a, n); rep.append((nm, sc, {c: int(np.abs(cs[c] - ca[c]).max()) for c in cs}))
    for r in rep: print("diff goc 40x40 vs S", r)
    json.dump(rep, open(HERE / "src" / "corner_diff.json", "w"), indent=1)

def sized(nm, src, size, cap, k=None):
    k = k or size[1] / src.height; b = build_nine(nm, src, size, k, cap)
    log[nm] = {"rim_src": [rim_color(src, x) for x in "LRTB"], "k": round(k, 3), "border": b}; return b
if want("fields"):
    P, _ = pieces("sheet-fields"); assert len(P) == 12, len(P)
    for nm, p in zip(["kit_input_normal", "kit_input_focus", "kit_input_error"], P[0:3]): sized(nm, p, (360, 56), (28, 28, 28, 28))
    for nm, p in zip(["kit_dropdown_closed", "kit_dropdown_open"], P[3:5]): sized(nm, p, (360, 56), (28, 28, 100, 28))
    for nm, p in zip(["kit_row_normal", "kit_row_alt", "kit_row_selected"], P[7:10]): sized(nm, p, (600, 48), (14, 2, 2, 2))
    sized("kit_toast", P[10], (360, 64), (28, 28, 28, 28)); sized("kit_tooltip", P[11], (240, 80), (28, 28, 28, 44))
if want("tabs"):
    P, _ = pieces("sheet-tabs"); assert len(P) == 3, len(P)
    for nm, p in zip(["kit_tab_off", "kit_tab_hover", "kit_tab_on"], P): sized(nm, p, (160, 56), (28, 28, 28, 2), 56 / p.height)
if want("sem"):
    P, _ = pieces("sheet-sem1"); assert len(P) == 8, len(P)
    for nm, p in zip(["kit_btn_success_normal", "kit_btn_success_hover", "kit_btn_success_pressed", "kit_btn_success_disabled"], P[0:4]): sized(nm, p, (240, 64), (34, 34, 34, 34))
    sized("kit_input_success", P[4], (360, 56), (28, 28, 28, 28))
    for nm, p in zip(["kit_toast_success", "kit_toast_error", "kit_toast_warning"], P[5:8]): sized(nm, p, (360, 64), (76, 28, 28, 28))
    Q, _ = pieces("sheet-sem2"); assert len(Q) == 12, len(Q)
    for nm, p in zip(["kit_tag_success", "kit_tag_error", "kit_tag_warning", "kit_tag_neutral"], Q[0:4]): sized(nm, p, (110, 36), (24, 18, 24, 18))
    for nm, p in zip(["kit_icon_check", "kit_icon_cross", "kit_icon_warn", "kit_icon_dot"], Q[4:8]): build_fit(nm, p, 32)
    for nm, p in zip(["kit_badge_success", "kit_badge_error", "kit_badge_warning", "kit_badge_neutral"], Q[8:12]): build_fit(nm, p, 40)
if want("sem3"):
    P, _ = pieces("sheet-sem3"); assert len(P) == 4, len(P)
    for nm, p in zip(["kit_icon_check", "kit_icon_cross", "kit_icon_warn", "kit_icon_dot"], P): build_fit(nm, p, 32)
if want("bars"):
    P, _ = pieces("sheet-bars"); assert len(P) == 7, len(P)
    sized("kit_progress_track", P[0], (480, 28), (26, 12, 26, 12)); sized("kit_progress_fill", P[1], (470, 16), (8, 6, 8, 6))
    sized("kit_slider_track", P[2], (320, 16), (20, 8, 20, 8)); sized("kit_slider_fill", P[3], (320, 10), (6, 4, 6, 4))
    sized("kit_scroll_track", P[4], (14, 320), (12, 14, 12, 14), 14 / P[4].width); sized("kit_scroll_thumb", P[5], (14, 96), (10, 12, 10, 12), 14 / P[5].width)
    build_fit("kit_dim", resize(P[6].crop((20, 20, P[6].width - 20, P[6].height - 20)), (64, 64)), 64); log["kit_dim"] = {"note": "nen toi phang #1F1D18, dung voi alpha 0,6 sau popup"}
json.dump(meta, open(HERE / "meta.json", "w", encoding="utf-8"), indent=1, ensure_ascii=False)
json.dump(log, open(HERE / "src" / "build_log.json", "w", encoding="utf-8"), indent=1, ensure_ascii=False)
