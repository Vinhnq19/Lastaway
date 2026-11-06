"""T-020 mau ngu nghia v2 (quy tac: mau la diem nhan nho, khong to ca nut hanh dong thuong). Chi dat sprite GPT len nen, ghi chu."""
import sys, pathlib, colorsys
sys.path.insert(0, str(pathlib.Path(__file__).parent))
import compose_kit as K
from compose_kit import *
import compose_sem as S0
import numpy as np
from PIL import Image, ImageDraw
H = K.H; meta = K.meta; WHITE = (255, 255, 255); DANGER_TXT = (255, 138, 120); NOTE = (214, 218, 214)

def icon(im, n, cx, cy, h):
    i = spr(n); i = i.resize((round(i.width * h / i.height), h), Image.NEAREST); im.alpha_composite(i, (round(cx - i.width / 2), round(cy - i.height / 2)))
def tw(im, s, size): return ImageDraw.Draw(im).textlength(s, font=F(size))
def btn(im, kind, xy, caption, ic=None, col=WHITE, size=(240, 64)):
    put(im, f"kit_btn_{kind}_normal", xy, size); cx, cy = xy[0] + size[0] / 2, xy[1] + size[1] / 2
    if ic:
        w = tw(im, caption, 26); total = w + 36; icon(im, ic, cx - total / 2 + 14, cy, 24); text(im, (cx - total / 2 + 36, cy), caption, 26, col, "lm", 2)
    else: text(im, (cx, cy), caption, 26, col, "mm", 2)
def tag(im, xy, caption, ic, size):
    put(im, "kit_tag_neutral", xy, size); cx, cy = xy[0] + size[0] / 2, xy[1] + size[1] / 2 - 1
    if ic: w = tw(im, caption, 18); total = w + 24; icon(im, ic, cx - total / 2 + 9, cy, 18); text(im, (cx - total / 2 + 24, cy), caption, 18, WHITE, "lm", 2)
    else: text(im, (cx, cy), caption, 18, WHITE, "mm", 2)
def toast(im, xy, caption, ic, w=360):
    put(im, "kit_toast", xy, (w, 64))
    if ic: icon(im, ic, xy[0] + 32, xy[1] + 32, 26); text(im, (xy[0] + 62, xy[1] + 32), caption, 22, TXT, "lm", 2)
    else: text(im, (xy[0] + w / 2, xy[1] + 32), caption, 22, TXT, "mm", 2)
def base():
    b = Image.open(K.GAME).convert("RGBA"); b.alpha_composite(Image.new("RGBA", b.size, (31, 29, 24, 153))); return b
def note(im, s): text(im, (960, 1050), s, 18, NOTE, "mm", 2)

def settings(mode):
    im = base(); W, Hh = 820, 640; x0, y0 = (1920 - W) // 2, (1080 - Hh) // 2; tb = meta["kit_panel_popup"]["title_bar_px"]; t = 24 + tb
    put(im, "kit_panel_popup", (x0, y0), (W, Hh)); text(im, (x0 + W / 2, y0 + 24 + tb / 2), "Cài đặt", 36, GOLD, "mm", 2)
    put(im, "kit_iconbtn_square_normal", (x0 + W - 24 - 8 - 48, y0 + 24 + (tb - 48) // 2), (48, 48)); text(im, (x0 + W - 24 - 8 - 24, y0 + 24 + tb // 2), "X", 26, TXT, "mm", 2)
    ty = y0 + t + 12
    for i, nm in enumerate(["Âm thanh", "Hình ảnh", "Điều khiển"]):
        put(im, "kit_tab_on" if i == 0 else "kit_tab_off", (x0 + 40 + i * 168, ty)); text(im, (x0 + 40 + i * 168 + 80, ty + 28), nm, 22, TXT if i == 0 else MUTED, "mm", 2)
    ry = ty + 84; lx = x0 + 60
    for j, (nm, v) in enumerate([("Nhạc nền", .7), ("Hiệu ứng", .45)]):
        yy = ry + j * 64; text(im, (lx, yy + 20), nm, 24, TXT, "lm", 2); put(im, "kit_slider_track", (lx + 220, yy + 12)); put(im, "kit_slider_fill", (lx + 220, yy + 15), (int(320 * v), 10)); put(im, "kit_knob", (lx + 220 + int(320 * v) - 16, yy + 4)); text(im, (lx + 560, yy + 20), f"{int(v * 100)}%", 22, MUTED, "lm", 2)
    yy = ry + 128; text(im, (lx, yy + 24), "Rung", 24, TXT, "lm", 2); put(im, "kit_toggle_on", (lx + 220, yy))
    yy += 64; text(im, (lx, yy + 20), "Hiện tên", 24, TXT, "lm", 2); put(im, "kit_checkbox_on", (lx + 220, yy))
    yy += 56; text(im, (lx, yy + 28), "Chất lượng", 24, TXT, "lm", 2); put(im, "kit_dropdown_closed", (lx + 220, yy)); text(im, (lx + 240, yy + 28), "Cao", 22, TXT, "lm", 2); put(im, "kit_arrow_down", (lx + 220 + 360 - 42, yy + 23))
    by = y0 + Hh - 24 - 64 - 18
    btn(im, "secondary", (x0 + W - 24 - 40 - 240 * 2 - 24, by), "Hủy"); btn(im, "primary", (x0 + W - 24 - 40 - 240, by), "Lưu")
    if mode == "sua": toast(im, (x0 + 40, y0 - 80), "Đã lưu cài đặt", "kit_icon_check")
    return im.convert("RGB")

def lobby(mode):
    im = base(); W, Hh = 1000, 640; x0, y0 = (1920 - W) // 2, (1080 - Hh) // 2
    put(im, "kit_panel_l", (x0, y0), (W, Hh)); text(im, (x0 + 60, y0 + 62), "Phòng chờ", 36, GOLD, "lm", 2); text(im, (x0 + W - 60, y0 + 62), "Phòng #2481", 22, MUTED, "rm", 2)
    ry = y0 + 110
    ppl = [("Hao ha Linh", 1), ("Vinh ha Mai", 1), ("Vu ha Son", 0), ("Quang Huy", 0), ("Ban (Chu phong)", 1)]
    for i, (nm, rd) in enumerate(ppl):
        put(im, "kit_row_selected" if nm.startswith("Ban") else ("kit_row_alt" if i % 2 else "kit_row_normal"), (x0 + 60, ry + i * 56), (W - 120, 52)); text(im, (x0 + 84, ry + i * 56 + 26), nm, 24, TXT, "lm", 2)
        xy = (x0 + W - 60 - 12 - 190, ry + i * 56 + 7); cap = "Sẵn sàng" if rd else "Chưa sẵn sàng"
        tag(im, xy, cap, (("kit_icon_check" if rd else "kit_icon_dot") if mode == "sua" else None), (190, 38))
    text(im, (x0 + 60, ry + 5 * 56 + 24), "3/5 sẵn sàng. Chờ mọi người để bắt đầu.", 20, MUTED, "lm", 2)
    by = y0 + Hh - 24 - 64 - 24
    if mode == "sua": btn(im, "secondary", (x0 + 60, by), "Rời phòng", "kit_icon_cross", DANGER_TXT); toast(im, (x0 + W - 60 - 440, y0 - 80), "Còn 2 người chưa sẵn sàng", "kit_icon_warn", 440)
    else: btn(im, "secondary", (x0 + 60, by), "Rời phòng")
    btn(im, "primary", (x0 + W - 60 - 240, by), "Sẵn sàng")
    note(im, "Minh họa: luật Sẵn sàng/Chưa sẵn sàng chưa được duyệt trong GDD")
    return im.convert("RGB")

def confirm():
    im = base(); W, Hh = 640, 330; x0, y0 = (1920 - W) // 2, (1080 - Hh) // 2; tb = meta["kit_panel_popup"]["title_bar_px"]
    put(im, "kit_panel_popup", (x0, y0), (W, Hh)); text(im, (x0 + W / 2, y0 + 24 + tb / 2), "Xóa phòng?", 34, GOLD, "mm", 2)
    text(im, (x0 + W / 2, y0 + 24 + tb + 54), "Hành động này không hoàn tác.", 24, TXT, "mm", 2); text(im, (x0 + W / 2, y0 + 24 + tb + 90), "Mọi người trong phòng sẽ bị đưa ra.", 22, MUTED, "mm", 2)
    by = y0 + Hh - 24 - 64 - 16
    btn(im, "secondary", (x0 + 40, by), "Hủy"); btn(im, "danger", (x0 + W - 40 - 240, by), "Xóa phòng", "kit_icon_cross")
    return im.convert("RGB")

def pct(img, box):
    a = np.asarray(img.crop(box).convert("RGB")).astype(float) / 255; mx = a.max(2); mn = a.min(2); d = mx - mn; s = np.where(mx > 0, d / np.maximum(mx, 1e-6), 0)
    h = np.zeros_like(mx); r, g, b = a[..., 0], a[..., 1], a[..., 2]; m = d > 1e-6
    h = np.where(m & (mx == r), ((g - b) / np.maximum(d, 1e-6)) % 6, h); h = np.where(m & (mx == g), (b - r) / np.maximum(d, 1e-6) + 2, h); h = np.where(m & (mx == b), (r - g) / np.maximum(d, 1e-6) + 4, h); h = h * 60
    red = ((h <= 14) | (h >= 346)) & (s >= .45) & (mx >= .2); grn = (h >= 85) & (h <= 160) & (s >= .30) & (mx >= .2); n = a.shape[0] * a.shape[1]
    return 100 * red.sum() / n, 100 * grn.sum() / n

if __name__ == "__main__":
    out = {}
    sd, ss = settings("dau"), settings("sua"); ld, ls = lobby("dau"), lobby("sua"); cf = confirm()
    sd.save(H / "man-thu-cai-dat-dau.png"); ss.save(H / "man-thu-cai-dat-sua.png"); ld.save(H / "man-thu-phong-cho-dau.png"); ls.save(H / "man-thu-phong-cho-sua.png"); cf.save(H / "man-thu-xac-nhan-xoa-phong.png")
    lamS = Image.open(H / "man-thu-cai-dat-lamdung.png"); lamL = Image.open(H / "man-thu-phong-cho-lamdung.png")
    BS, BL, BC = (540, 120, 1380, 870), (460, 100, 1460, 880), (600, 300, 1320, 800)
    rows = [("Cài đặt", [("Bản đầu", sd), ("Bản lạm dụng", lamS), ("Bản sửa", ss)], BS), ("Phòng chờ", [("Bản đầu", ld), ("Bản lạm dụng", lamL), ("Bản sửa", ls)], BL)]
    tbl = ["| Màn (vùng đo) | Bản | % đỏ | % xanh lá | Tổng |", "|---|---|---:|---:|---:|"]; sheet = []
    for nm, vs, box in rows:
        w, h = 620, int(620 * (box[3] - box[1]) / (box[2] - box[0])); row = Image.new("RGB", (w * 3 + 40, h + 40), (20, 20, 20)); dd = ImageDraw.Draw(row)
        for i, (lab_, im_) in enumerate(vs):
            r_, g_ = pct(im_, box); tbl.append(f"| {nm} | {lab_} | {r_:.2f} | {g_:.2f} | {r_ + g_:.2f} |"); row.paste(im_.crop(box).resize((w, h), Image.LANCZOS), (i * (w + 20), 40)); dd.text((i * (w + 20) + 6, 6), f"{nm}: {lab_}", font=F(22), fill=GOLD)
        sheet.append(row)
    r_, g_ = pct(cf, BC); tbl.append(f"| Xác nhận Xóa phòng (chỉ có bản sửa) | Bản sửa | {r_:.2f} | {g_:.2f} | {r_ + g_:.2f} |")
    W2 = sheet[0].width; big = Image.new("RGB", (W2, sum(r.height for r in sheet) + 10), (20, 20, 20)); y = 0
    for r in sheet: big.paste(r, (0, y)); y += r.height + 10
    big.save(H / "so-sanh-3-ban-cai-dat-phong-cho.png"); (H / "src" / "dien-tich-do-xanh.md").write_text("\n".join(tbl) + "\n", encoding="utf-8"); print("\n".join(tbl))
    # mu mau (ban sua)
    simrows = []
    for nm, scr in (("Phòng chờ", ls), ("Cài đặt", ss), ("Xác nhận Xóa phòng", cf)):
        s_ = [scr, S0.sim(scr, "deuteranopia"), S0.sim(scr, "protanopia")]; w, h = 640, 360; row = Image.new("RGB", (w * 3 + 40, h + 40), (20, 20, 20)); dd = ImageDraw.Draw(row)
        for i, (im_, lab_) in enumerate(zip(s_, ["Thường", "Deuteranopia", "Protanopia"])): row.paste(im_.resize((w, h), Image.LANCZOS), (i * (w + 20), 40)); dd.text((i * (w + 20) + 6, 6), f"{nm} (bản sửa): {lab_}", font=F(22), fill=GOLD)
        simrows.append(row)
    o = Image.new("RGB", (simrows[0].width, sum(r.height for r in simrows) + 20), (20, 20, 20)); y = 0
    for r in simrows: o.paste(r, (0, y)); y += r.height + 10
    o.save(H / "mo-phong-mu-mau.png"); print("ok")
