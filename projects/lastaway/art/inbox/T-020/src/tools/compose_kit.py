"""T-020: trang so sanh ui-kit-sheet (nen gameplay) + ui-kit-sheet-dark (nen toi) + man thu Cai dat 1920x1080. Chi dat sprite GPT len nen va ghi nhan chu."""
import sys, json, pathlib
from PIL import Image, ImageDraw, ImageFont
H = pathlib.Path(r"D:\Lastaway\projects\lastaway\art\inbox\T-020"); meta = json.load(open(H / "meta.json", encoding="utf-8"))
FB = r"D:\Lastaway\Assets\_JungleMaze\Art\Fonts\BeVietnamPro-Bold.ttf"; F = lambda n: ImageFont.truetype(FB, n)
GAME = pathlib.Path(r"D:\Lastaway\projects\lastaway\art\inbox\T-015\dung-thu-hud-A.png")
TXT = (237, 242, 238); GOLD = (255, 220, 110); MUTED = (214, 218, 214); DANGER = (224, 90, 79); OUTL = (10, 12, 12)

def spr(n): return Image.open(H / f"{n}.png").convert("RGBA")
def sized(n, size=None):
    im = spr(n); m = meta[n]
    if size is None or tuple(size) == tuple(m["size"]): return im
    b = m["border_LRTB"]
    if b is None: return im.resize(size, Image.NEAREST)
    L, R, T, B = b; w, h = im.size; W, Hh = size; o = Image.new("RGBA", size, (0, 0, 0, 0))
    xs = [(0, L, 0, L), (L, w - R, L, W - R), (w - R, w, W - R, W)]; ys = [(0, T, 0, T), (T, h - B, T, Hh - B), (h - B, h, Hh - B, Hh)]
    for a0, a1, d0, d1 in xs:
        for c0, c1, e0, e1 in ys:
            if a1 > a0 and c1 > c0 and d1 > d0 and e1 > e0: o.paste(im.crop((a0, c0, a1, c1)).resize((d1 - d0, e1 - e0), Image.NEAREST), (d0, e0))
    return o
def put(im, n, xy, size=None): im.alpha_composite(sized(n, size), (int(xy[0]), int(xy[1])))
def text(im, xy, s, size, fill=TXT, anchor="la", stroke=0):
    ImageDraw.Draw(im).text(xy, s, font=F(size), fill=fill, anchor=anchor, stroke_width=stroke, stroke_fill=OUTL)
def label(im, xy, s, anchor="la"): text(im, xy, s, 18, GOLD, anchor, 2)
def btn(im, kind, state, xy, caption, size=(240, 64)):
    put(im, f"kit_btn_{kind}_{state}", xy, size)
    col = (150, 148, 140) if state == "disabled" else (255, 255, 255)
    text(im, (xy[0] + size[0] / 2, xy[1] + size[1] / 2 + (2 if state == "pressed" else 0)), caption, 26, col, "mm", 2)

def kit_sheet(im):
    W, Hh = im.size
    label(im, (40, 24), "NÚT: chính, phụ, nguy hiểm x thường, hover, nhấn, tắt (240x64)")
    caps = {"primary": "Chơi", "secondary": "Hủy", "danger": "Rời trận"}
    for r, st in enumerate(["normal", "hover", "pressed", "disabled"]):
        for c, k in enumerate(["primary", "secondary", "danger"]): btn(im, k, st, (40 + c * 270, 60 + r * 84), caps[k], (240, 64))
    label(im, (900, 24), "NÚT ICON TRÒN VÀ VUÔNG (64), TOGGLE, CHECKBOX, BADGE, KNOB")
    for i, st in enumerate(["normal", "hover", "pressed", "disabled"]):
        put(im, f"kit_iconbtn_round_{st}", (900 + i * 84, 60)); put(im, f"kit_iconbtn_square_{st}", (900 + i * 84, 144))
    put(im, "kit_toggle_off", (900, 236)); put(im, "kit_toggle_on", (1040, 236)); put(im, "kit_checkbox_off", (1190, 240)); put(im, "kit_checkbox_on", (1250, 240))
    put(im, "kit_badge", (1320, 252)); put(im, "kit_knob", (1370, 248)); put(im, "kit_arrow_down", (1425, 256))
    label(im, (900, 310), "Ô NHẬP: thường, focus, lỗi (360x56)")
    for i, st in enumerate(["normal", "focus", "error"]): put(im, f"kit_input_{st}", (900, 340 + i * 70))
    label(im, (1300, 310), "DROPDOWN: đóng, mở")
    put(im, "kit_dropdown_closed", (1300, 340)); put(im, "kit_dropdown_open", (1300, 410)); put(im, "kit_arrow_down", (1300 + 360 - 42, 340 + 23))
    for i, n in enumerate(["Tùy chọn 1", "Tùy chọn 2", "Tùy chọn 3"]):
        put(im, "kit_row_selected" if i == 1 else ("kit_row_alt" if i == 2 else "kit_row_normal"), (1300, 466 + i * 48), (360, 48)); text(im, (1316, 490 + i * 48), n, 20, TXT, "lm", 2)
    label(im, (1300, 620), "TAB: không chọn, hover, chọn (160x56)"); put(im, "kit_tab_off", (1300, 650)); put(im, "kit_tab_hover", (1470, 650)); put(im, "kit_tab_on", (1640, 650))
    label(im, (40, 430), "DÒNG DANH SÁCH: thường, xen kẽ, chọn (600x48)")
    for i, n in enumerate(["normal", "alt", "selected", "normal", "alt"]):
        put(im, f"kit_row_{n}", (40, 460 + i * 48)); text(im, (60, 484 + i * 48), f"Người chơi {i + 1}", 20, TXT, "lm", 2)
    label(im, (700, 560), "TOAST (360x64), TOOLTIP (240x80)")
    put(im, "kit_toast", (700, 590)); text(im, (880, 622), "Đã lưu cài đặt", 22, TXT, "mm", 2); put(im, "kit_tooltip", (700, 670)); text(im, (820, 700), "Hồi 30 máu", 22, TXT, "mm", 2)
    label(im, (1300, 730), "THANH: loading, slider, scrollbar")
    put(im, "kit_progress_track", (1300, 770)); put(im, "kit_progress_fill", (1305, 776), (300, 16)); put(im, "kit_slider_track", (1300, 830)); put(im, "kit_slider_fill", (1300, 833), (190, 10)); put(im, "kit_knob", (1480, 822))
    put(im, "kit_scroll_track", (1850, 466)); put(im, "kit_scroll_thumb", (1850, 486))
    label(im, (40, 880), "PANEL S (360x240), M (640x400), POPUP (720x480), L (1000x640)")
    put(im, "kit_panel_s", (40, 920)); put(im, "kit_panel_m", (440, 920)); put(im, "kit_panel_popup", (1100, 920)); put(im, "kit_panel_l", (40, 1380))
    t = meta["kit_panel_popup"]["title_bar_px"]; text(im, (1100 + 360, 920 + 24 + t / 2), "Tiêu đề popup", 30, GOLD, "mm", 2)
    def ic_(n, cx, cy, h):
        i2 = spr(n); i2 = i2.resize((round(i2.width * h / i2.height), h), Image.NEAREST); im.alpha_composite(i2, (round(cx - i2.width / 2), round(cy - i2.height / 2)))
    label(im, (40, 2060), "MÀU NGỮ NGHĨA: điểm nhấn nhỏ, luôn kèm icon hoặc chữ. Nút chính vàng; đỏ đầy nút chỉ trong hộp xác nhận")
    put(im, "kit_btn_primary_normal", (40, 2100), (240, 64)); text(im, (160, 2132), "Lưu", 26, (255, 255, 255), "mm", 2)
    put(im, "kit_btn_secondary_normal", (300, 2100), (240, 64)); ic_("kit_icon_cross", 360, 2132, 24); text(im, (460, 2132), "Rời phòng", 26, (255, 138, 120), "mm", 2)
    put(im, "kit_btn_danger_normal", (560, 2100), (240, 64)); ic_("kit_icon_cross", 620, 2132, 24); text(im, (710, 2132), "Xóa phòng", 26, (255, 255, 255), "mm", 2); text(im, (680, 2180), "chỉ trong hộp xác nhận", 16, MUTED, "mm", 2)
    put(im, "kit_input_success", (880, 2108), (360, 56)); ic_("kit_icon_check", 1210, 2136, 22); put(im, "kit_input_error", (1280, 2108), (360, 56)); ic_("kit_icon_cross", 1610, 2136, 22); text(im, (1280, 2178), "Sai định dạng: nhập 4 số", 16, (255, 138, 120), "lm", 2)
    for i, (ic, tx) in enumerate([("kit_icon_check", "Đã lưu cài đặt"), ("kit_icon_cross", "Mất kết nối"), ("kit_icon_warn", "Bo sắp thu")]):
        put(im, "kit_toast", (40 + i * 380, 2210), (360, 64)); ic_(ic, 40 + i * 380 + 32, 2242, 26); text(im, (40 + i * 380 + 62, 2242), tx, 22, TXT, "lm", 2)
    for i, (tx, ic) in enumerate([("Sẵn sàng", "kit_icon_check"), ("Chưa sẵn sàng", "kit_icon_dot")]):
        put(im, "kit_tag_neutral", (1220 + i * 210, 2216), (190, 38)); ic_(ic, 1220 + i * 210 + 24, 2234, 18); text(im, (1220 + i * 210 + 44, 2235), tx, 18, (255, 255, 255), "lm", 2)
    for i, n in enumerate(["check", "cross", "warn", "dot"]): ic_(f"kit_icon_{n}", 1250 + i * 60, 2310, 32)
    return im.convert("RGB")

def settings_screen():
    base = Image.open(GAME).convert("RGBA"); dim = Image.new("RGBA", base.size, (31, 29, 24, 153)); base.alpha_composite(dim)   # nen toi 60%
    W, Hh = 820, 640; x0, y0 = (1920 - W) // 2, (1080 - Hh) // 2; tb = meta['kit_panel_popup']['title_bar_px']; t = 24 + tb
    put(base, "kit_panel_popup", (x0, y0), (W, Hh)); text(base, (x0 + W / 2, y0 + 24 + tb / 2), "Cài đặt", 36, GOLD, "mm", 2)
    put(base, "kit_iconbtn_square_normal", (x0 + W - 24 - 8 - 48, y0 + 24 + (tb - 48) // 2), (48, 48)); text(base, (x0 + W - 24 - 8 - 24, y0 + 24 + tb // 2), "X", 26, TXT, "mm", 2)
    ty = y0 + t + 12
    for i, nm in enumerate(["Âm thanh", "Hình ảnh", "Điều khiển"]):
        put(base, "kit_tab_on" if i == 0 else "kit_tab_off", (x0 + 40 + i * 168, ty)); text(base, (x0 + 40 + i * 168 + 80, ty + 28), nm, 22, TXT if i == 0 else MUTED, "mm", 2)
    ry = ty + 84; lx = x0 + 60
    for j, (nm, v) in enumerate([("Nhạc nền", .7), ("Hiệu ứng", .45)]):
        yy = ry + j * 64; text(base, (lx, yy + 20), nm, 24, TXT, "lm", 2); put(base, "kit_slider_track", (lx + 220, yy + 12)); put(base, "kit_slider_fill", (lx + 220, yy + 15), (int(320 * v), 10)); put(base, "kit_knob", (lx + 220 + int(320 * v) - 16, yy + 4))
        text(base, (lx + 220 + 340, yy + 20), f"{int(v * 100)}%", 22, MUTED, "lm", 2)
    yy = ry + 128; text(base, (lx, yy + 24), "Rung", 24, TXT, "lm", 2); put(base, "kit_toggle_on", (lx + 220, yy))
    yy += 64; text(base, (lx, yy + 20), "Hiện tên", 24, TXT, "lm", 2); put(base, "kit_checkbox_on", (lx + 220, yy))
    yy += 56; text(base, (lx, yy + 28), "Chất lượng", 24, TXT, "lm", 2); put(base, "kit_dropdown_closed", (lx + 220, yy)); text(base, (lx + 240, yy + 28), "Cao", 22, TXT, "lm", 2); put(base, "kit_arrow_down", (lx + 220 + 360 - 42, yy + 23))
    by = y0 + Hh - 24 - 64 - 18
    btn(base, "secondary", "normal", (x0 + W - 24 - 40 - 240 * 2 - 24, by), "Hủy"); btn(base, "primary", "normal", (x0 + W - 24 - 40 - 240, by), "Lưu")
    return base.convert("RGB")

if __name__ == "__main__":
    g = Image.open(GAME).convert("RGB"); tile = g.crop((640, 120, 1150, 400)); bg = Image.new("RGB", (1920, 2360))
    for yy in range(0, 2360, tile.height):
        for xx in range(0, 1920, tile.width): bg.paste(tile, (xx, yy))
    kit_sheet(bg.convert("RGBA")).save(H / "ui-kit-sheet.png")
    kit_sheet(Image.new("RGBA", (1920, 2360), (43, 40, 33, 255))).save(H / "ui-kit-sheet-dark.png")
    settings_screen().save(H / "man-thu-cai-dat-1920x1080.png"); print("ok")
