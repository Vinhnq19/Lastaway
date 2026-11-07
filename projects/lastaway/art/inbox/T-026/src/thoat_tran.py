"""T-026: so sanh chu + X do tren nut Thoat tran (nut phu kaki K2). Do bang script tren sprite that."""
import sys, pathlib, json
sys.path.insert(0, r"D:\Lastaway\projects\lastaway\art\inbox\T-021\src\tools"); sys.path.insert(0, str(pathlib.Path(__file__).parent))
import mock as M, nut4
from nut import STATES
from PIL import Image
OUT = pathlib.Path(r"D:\Lastaway\projects\lastaway\art\inbox\T-026")
S = nut4.Set4("k2"); ST3 = STATES[:3]
def worst(c): return min(S.worst("secondary", st, c) for st in ST3)
def per(c): return [round(S.worst("secondary", st, c), 2) for st in ST3]
def lightest(base, need):
    best = None
    for k in range(40, 400):
        f = k / 100; c = tuple(min(255, round(v * f)) for v in base)
        if worst(c) >= need: best = c
        elif best is not None: break
    return best
CUR = (0x55, 0x14, 0x0F); R1 = lightest((85, 20, 15), 4.5); R2x = lightest((154, 42, 32), 3.0); DARK = (31, 26, 16)
print("R1", R1, per(R1), "R2 X", R2x, per(R2x), "cur", per(CUR))
hx = lambda c: "#%02X%02X%02X" % c
cols = [("Hiện tại", CUR, CUR), ("R1: chữ đỏ sáng nhất đạt 4,5", R1, R1), ("R2: chữ tối, X đỏ tươi", DARK, R2x), ("R3: chữ R1 + X đỏ tươi", R1, R2x)]
W, H = 1760, 560; im = Image.new("RGBA", (W, H), (43, 40, 33, 255)); im.alpha_composite(M.bg(0.62).resize((W, H)))
M.text(im, (40, 36), "Nút Thoát trận (phụ kaki K2, cỡ M 64): chữ + X đỏ, so sánh độ sáng", 28, M.GOLD, "lm", 2, skip=True)
icon_src = M.spr("kit_icon_cross_dark")
def tint(c):
    a = icon_src.getchannel("A"); im2 = Image.new("RGBA", icon_src.size, tuple(c) + (255,)); im2.putalpha(a); return im2
rep = {}
for i, (nm, tc, ic) in enumerate(cols):
    x0 = 40 + i * 430; M.text(im, (x0, 90), nm, 22, M.TXT, "lm", 2, skip=True)
    for j, st in enumerate(ST3):
        y = 130 + j * 100; w, h = 380, 64; im.alpha_composite(M.kbtn("secondary", st, w, h), (x0, y)); base = round(h * 0.11)
        fy = y + (h - base) / 2 + (h * 0.06 if st == "pressed" else 0); font = 26; xh = -M.F(font).getbbox("x", anchor="ls")[1]; by = fy + xh / 2 + 1
        tot = M.tw("Thoát trận", font) + font + 8; xs = x0 + w / 2 - tot / 2; im.alpha_composite(tint(ic).resize((font, font), Image.NEAREST), (round(xs), round(fy - font / 2))); M.text(im, (xs + font + 8, by), "Thoát trận", font, tc, "ls", 0, skip=True)
    wt = worst(tc); wi = worst(ic)
    M.text(im, (x0, 440), f"chữ {hx(tc)}: tệ nhất {wt:.2f}:1 ({'/'.join(map(str, per(tc)))})", 18, M.TXT, "lm", 2, skip=True)
    M.text(im, (x0, 470), f"X {hx(ic)}: tệ nhất {wi:.2f}:1 (cần >= 3)", 18, M.TXT, "lm", 2, skip=True)
    M.text(im, (x0, 500), "thường · hover · nhấn (trên xuống)", 16, M.MUTED, "lm", 2, skip=True)
    rep[nm] = {"chu": hx(tc), "chu_tap_nhat": round(wt, 2), "chu_theo_trang_thai": per(tc), "x": hx(ic), "x_tap_nhat": round(wi, 2), "x_theo_trang_thai": per(ic)}
im.convert("RGB").save(OUT / "so-sanh-thoat-tran.png"); (OUT / "so-sanh-thoat-tran.json").write_text(json.dumps(rep, ensure_ascii=False, indent=1), encoding="utf-8"); print(json.dumps(rep, ensure_ascii=False))
