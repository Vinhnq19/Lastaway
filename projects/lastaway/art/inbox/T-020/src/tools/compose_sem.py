"""T-020 mau ngu nghia: man Cai dat truoc/sau, man phong cho thu, ban mo phong mu mau, strip ngu nghia. Chi dat sprite GPT len nen, ghi chu, mo phong."""
import sys, json, pathlib, math
sys.path.insert(0, str(pathlib.Path(__file__).parent))
import compose_kit as K
from compose_kit import *
import numpy as np
from PIL import Image, ImageDraw
H = K.H; meta = K.meta
WHITE = (255, 255, 255)

def icon(im, n, cx, cy, h=None):
    i = spr(n)
    if h: i = i.resize((round(i.width * h / i.height), h), Image.NEAREST)
    im.alpha_composite(i, (round(cx - i.width / 2), round(cy - i.height / 2)))
def sbtn(im, kind, xy, caption, ic=None, size=(240, 64), state="normal"):
    put(im, f"kit_btn_{kind}_{state}", xy, size)
    cx = xy[0] + size[0] / 2; cy = xy[1] + size[1] / 2
    if ic:
        text(im, (cx + 14, cy), caption, 26, WHITE, "mm", 2); tw = ImageDraw.Draw(im).textlength(caption, font=F(26)); icon(im, ic, cx + 14 - tw / 2 - 24, cy, 26)
    else: text(im, (cx, cy), caption, 26, WHITE, "mm", 2)
def tag(im, kind, xy, caption, ic, size=(150, 38)):
    put(im, f"kit_tag_{kind}", xy, size); col = WHITE
    cx = xy[0] + size[0] / 2; cy = xy[1] + size[1] / 2 - 1
    tw = ImageDraw.Draw(im).textlength(caption, font=F(18)); total = tw + 22
    icon(im, ic, cx - total / 2 + 9, cy, 18); text(im, (cx - total / 2 + 24, cy), caption, 18, col, "lm", 2)
def toast(im, kind, xy, caption, ic, w=360):
    put(im, f"kit_toast_{kind}", xy, (w, 64)); icon(im, ic, xy[0] + 28, xy[1] + 32, 24); text(im, (xy[0] + 62, xy[1] + 32), caption, 22, TXT, "lm", 2)

def base():
    b = Image.open(K.GAME).convert("RGBA"); b.alpha_composite(Image.new("RGBA", b.size, (31, 29, 24, 153))); return b

def settings(after):
    im = base(); W, Hh = 820, 640; x0, y0 = (1920 - W) // 2, (1080 - Hh) // 2; tb = meta["kit_panel_popup"]["title_bar_px"]; t = 24 + tb
    put(im, "kit_panel_popup", (x0, y0), (W, Hh)); text(im, (x0 + W / 2, y0 + 24 + tb / 2), "Cài đặt", 36, GOLD, "mm", 2)
    put(im, "kit_iconbtn_square_normal", (x0 + W - 24 - 8 - 48, y0 + 24 + (tb - 48) // 2), (48, 48)); text(im, (x0 + W - 24 - 8 - 24, y0 + 24 + tb // 2), "X", 26, TXT, "mm", 2)
    ty = y0 + t + 12
    for i, nm in enumerate(["Âm thanh", "Hình ảnh", "Điều khiển"]):
        put(im, "kit_tab_on" if i == 0 else "kit_tab_off", (x0 + 40 + i * 168, ty)); text(im, (x0 + 40 + i * 168 + 80, ty + 28), nm, 22, TXT if i == 0 else MUTED, "mm", 2)
    ry = ty + 84; lx = x0 + 60
    for j, (nm, v) in enumerate([("Nhạc nền", .7), ("Hiệu ứng", .45)]):
        yy = ry + j * 64; text(im, (lx, yy + 20), nm, 24, TXT, "lm", 2); put(im, "kit_slider_track", (lx + 220, yy + 12)); put(im, "kit_slider_fill", (lx + 220, yy + 15), (int(320 * v), 10)); put(im, "kit_knob", (lx + 220 + int(320 * v) - 16, yy + 4))
        text(im, (lx + 560, yy + 20), f"{int(v * 100)}%", 22, MUTED, "lm", 2)
    yy = ry + 128; text(im, (lx, yy + 24), "Rung", 24, TXT, "lm", 2); put(im, "kit_toggle_on", (lx + 220, yy))
    yy += 64; text(im, (lx, yy + 20), "Hiện tên", 24, TXT, "lm", 2); put(im, "kit_checkbox_on", (lx + 220, yy))
    yy += 56; text(im, (lx, yy + 28), "Chất lượng", 24, TXT, "lm", 2); put(im, "kit_dropdown_closed", (lx + 220, yy)); text(im, (lx + 240, yy + 28), "Cao", 22, TXT, "lm", 2); put(im, "kit_arrow_down", (lx + 220 + 360 - 42, yy + 23))
    by = y0 + Hh - 24 - 64 - 18
    if after:
        sbtn(im, "secondary", (x0 + W - 24 - 40 - 240 * 2 - 24, by), "Hủy"); sbtn(im, "success", (x0 + W - 24 - 40 - 240, by), "Lưu", "kit_icon_check")
        toast(im, "success", (x0 + 40, y0 - 80), "Đã lưu cài đặt", "kit_icon_check")
    else:
        sbtn(im, "secondary", (x0 + W - 24 - 40 - 240 * 2 - 24, by), "Hủy"); sbtn(im, "primary", (x0 + W - 24 - 40 - 240, by), "Lưu")
    return im.convert("RGB")

def lobby():
    im = base(); W, Hh = 1000, 640; x0, y0 = (1920 - W) // 2, (1080 - Hh) // 2
    put(im, "kit_panel_l", (x0, y0), (W, Hh)); text(im, (x0 + 60, y0 + 62), "Phòng chờ", 36, GOLD, "lm", 2); text(im, (x0 + W - 60, y0 + 62), "Phòng #2481", 22, MUTED, "rm", 2)
    ry = y0 + 110
    ppl = [("Hao ha Linh", "ready"), ("Vinh ha Mai", "ready"), ("Vu ha Son", "wait"), ("Quang Huy", "wait"), ("Ban (Chu phong)", "ready")]
    for i, (nm, st) in enumerate(ppl):
        put(im, "kit_row_selected" if nm.startswith("Ban") else ("kit_row_alt" if i % 2 else "kit_row_normal"), (x0 + 60, ry + i * 56), (W - 120, 52))
        text(im, (x0 + 84, ry + i * 56 + 26), nm, 24, TXT, "lm", 2)
        if st == "ready": tag(im, "success", (x0 + W - 60 - 12 - 190, ry + i * 56 + 7), "Sẵn sàng", "kit_icon_check", (190, 38))
        else: tag(im, "neutral", (x0 + W - 60 - 12 - 190, ry + i * 56 + 7), "Chưa sẵn sàng", "kit_icon_dot", (190, 38))
    text(im, (x0 + 60, ry + 5 * 56 + 24), "3/5 sẵn sàng. Chờ mọi người để bắt đầu.", 20, MUTED, "lm", 2)
    by = y0 + Hh - 24 - 64 - 24
    sbtn(im, "danger", (x0 + 60, by), "Rời phòng", "kit_icon_cross"); sbtn(im, "success", (x0 + W - 60 - 240, by), "Sẵn sàng", "kit_icon_check")
    toast(im, "warning", (x0 + W - 60 - 440, y0 - 80), "Còn 2 người chưa sẵn sàng", "kit_icon_warn", 440)
    return im.convert("RGB")

# mo phong mu mau (Machado 2009, severity 1.0), tren RGB tuyen tinh
M = {"deuteranopia": [[.367322, .860646, -.227968], [.280085, .672501, .047413], [-.011820, .042940, .968881]],
     "protanopia": [[.152286, 1.052583, -.204868], [.114503, .786281, .099216], [-.003882, -.048116, 1.051998]]}
def lin(a): a = a / 255; return np.where(a <= .04045, a / 12.92, ((a + .055) / 1.055) ** 2.4)
def delin(a): a = np.clip(a, 0, 1); return np.where(a <= .0031308, a * 12.92, 1.055 * a ** (1 / 2.4) - .055) * 255
def sim(im, kind):
    a = lin(np.asarray(im.convert("RGB")).astype(float)); o = a @ np.array(M[kind]).T; return Image.fromarray(delin(o).astype("uint8"))
def lab(c):
    c = lin(np.array(c, float)); X = c @ np.array([.4124, .3576, .1805]); Y = c @ np.array([.2126, .7152, .0722]); Z = c @ np.array([.0193, .1192, .9505])
    f = lambda t: np.where(t > .008856, np.cbrt(t), 7.787 * t + 16 / 116); fx, fy, fz = f(X / .95047), f(Y), f(Z / 1.08883); return np.array([116 * fy - 16, 500 * (fx - fy), 200 * (fy - fz)])
def de(a, b): return float(np.linalg.norm(lab(a) - lab(b)))

if __name__ == "__main__":
    before, after, lob = settings(False), settings(True), lobby()
    before.save(H / "man-thu-cai-dat-truoc.png"); after.save(H / "man-thu-cai-dat-sau.png"); lob.save(H / "man-thu-phong-cho-1920x1080.png")
    # so sanh truoc/sau (cat vung popup)
    box = (540, 120, 1380, 870); a, b = before.crop(box), after.crop(box); cmp_ = Image.new("RGB", (a.width * 2 + 30, a.height + 50), (20, 20, 20)); cmp_.paste(a, (0, 50)); cmp_.paste(b, (a.width + 30, 50))
    d = ImageDraw.Draw(cmp_); d.text((20, 10), "TRƯỚC: Lưu vàng (vàng = chọn/thương hiệu)", font=F(24), fill=GOLD); d.text((a.width + 50, 10), "SAU: Lưu xanh lá + dấu tích, toast thành công", font=F(24), fill=GOLD); cmp_.save(H / "so-sanh-truoc-sau-cai-dat.png")
    # mo phong mu mau: man phong cho va man cai dat sau, 3 cot (thuong, deuteranopia, protanopia)
    rows = []
    for nm, scr in (("Phòng chờ", lob), ("Cài đặt (sau)", after)):
        s = [scr, sim(scr, "deuteranopia"), sim(scr, "protanopia")]; w, h = 640, 360; row = Image.new("RGB", (w * 3 + 40, h + 40), (20, 20, 20)); dd = ImageDraw.Draw(row)
        for i, (im_, lab_) in enumerate(zip(s, ["Thường", "Deuteranopia", "Protanopia"])): row.paste(im_.resize((w, h), Image.LANCZOS), (i * (w + 20), 40)); dd.text((i * (w + 20) + 6, 6), f"{nm}: {lab_}", font=F(22), fill=GOLD)
        rows.append(row)
    out = Image.new("RGB", (rows[0].width, sum(r.height for r in rows) + 10), (20, 20, 20)); y = 0
    for r in rows: out.paste(r, (0, y)); y += r.height + 10
    out.save(H / "mo-phong-mu-mau.png")
    # so do ΔE giua cap xanh/do, mau fill sprite
    fills = {"success": (77, 105, 40), "error": (118, 45, 36), "warning": (150, 104, 18), "neutral": (68, 60, 37), "gold(primary)": (150, 118, 50)}
    def conv(c, k): return tuple(int(v) for v in np.asarray(sim(Image.new("RGB", (1, 1), c), k))[0, 0])
    lines = ["| Cặp | ΔE thường | ΔE deuteranopia | ΔE protanopia | L tỉ lệ (không màu) |", "|---|---:|---:|---:|---:|"]
    cr_ = lambda a, b: (max(a, b) + .05) / (min(a, b) + .05); Lm = lambda c: float(.2126 * lin(np.array(c[0])) + .7152 * lin(np.array(c[1])) + .0722 * lin(np.array(c[2])))
    for a_, b_ in (("success", "error"), ("success", "warning"), ("error", "warning"), ("success", "neutral"), ("error", "neutral"), ("success", "gold(primary)")):
        ca, cb = fills[a_], fills[b_]; lines.append(f"| {a_} / {b_} | {de(ca, cb):.1f} | {de(conv(ca, 'deuteranopia'), conv(cb, 'deuteranopia')):.1f} | {de(conv(ca, 'protanopia'), conv(cb, 'protanopia')):.1f} | {cr_(Lm(ca), Lm(cb)):.2f} |")
    (H / "src" / "mo-phong-mu-mau-de.md").write_text("\n".join(lines) + "\n", encoding="utf-8"); print("\n".join(lines))
