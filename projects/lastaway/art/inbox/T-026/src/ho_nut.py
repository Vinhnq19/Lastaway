"""T-026: sheet ho nut K2 (luat nut: icon trai + chu toi, can giua mat nut, X do #94281F). PYTHONIOENCODING=utf-8 python ho_nut.py"""
import sys, pathlib
sys.path.insert(0, r"D:\Lastaway\projects\lastaway\art\inbox\T-021\src\tools")
import mock as M
from mock import put, text, spr, tw
from PIL import Image
OUT = pathlib.Path(r"D:\Lastaway\projects\lastaway\art\inbox\T-026"); DARK = M.DARK
def btn(im, mat, st, xy, size, cap, font=26, icon=None):
    M.CUR[0] = "ho-nut"; M.button(im, mat, xy, cap, None, M.WHITE, size, st, font, icon)
def main():
    W, H = 2000, 1700; im = Image.new("RGBA", (W, H), (43, 40, 33, 255)); im.alpha_composite(M.bg(0.62).resize((W, H)))
    text(im, (40, 40), "Họ nút K2 (T-026): icon trái + chữ tối, căn giữa mặt nút (không tính đế); X đỏ #94281F", 28, M.GOLD, "lm", 2, skip=True)
    labs = ["thường", "hover", "nhấn", "tắt"]; y = 100
    for mat, nm, cap in (("primary", "Nút chính", "Vào phòng"), ("secondary", "Nút phụ", "Tạo phòng"), ("danger", "Nút phá hủy (chỉ hộp xác nhận)", "Xóa phòng")):
        text(im, (40, y), nm, 22, M.TXT, "lm", 2, skip=True)
        for i, st in enumerate(["normal", "hover", "pressed", "disabled"]): btn(im, mat, st, (40 + i * 300, y + 20), (280, 64), cap); text(im, (180 + i * 300, y + 100), labs[i], 16, M.MUTED, "mm", 2, skip=True)
        y += 140
    text(im, (40, y), "Cỡ S 48 (chữ 22, icon 22), M 64 (26, 26), L 80 (30, 30)", 22, M.TXT, "lm", 2, skip=True)
    for i, (hh, ww, f) in enumerate(((48, 230, 22), (64, 280, 26), (80, 340, 30))): btn(im, "primary", "normal", (40 + [0, 260, 570][i], y + 20), (ww, hh), "Vào phòng", f)
    btn(im, "secondary", "normal", (950, y + 20), (320, 64), "Thoát trận"); btn(im, "secondary", "normal", (1300, y + 20), (320, 64), "Rời phòng"); text(im, (1120, y + 110), "chữ tối + icon X đỏ #94281F", 16, M.MUTED, "mm", 2, skip=True); y += 140
    text(im, (40, y), "Đồng bộ icon + chữ (mọi nút có chữ đều có icon trái)", 22, M.TXT, "lm", 2, skip=True)
    caps = ["Vào phòng", "Tạo phòng", "Lịch sử", "Bảng xếp hạng", "Cài đặt", "Thoát", "Đăng xuất", "Hủy", "Xóa", "Sao chép", "Thử lại", "Đăng nhập", "Máy chủ (nâng cao)", "Sẵn sàng (tùy chọn)", "Về trang chủ", "Rời phòng"]
    for i, c in enumerate(caps):
        col, row = i % 4, i // 4; btn(im, "primary" if c == "Vào phòng" else "secondary", "normal", (40 + col * 330, y + 20 + row * 70), (310, 52), c, 20)
    y += 20 + 4 * 70 + 10
    text(im, (40, y), "Nút icon tròn, vuông", 22, M.TXT, "lm", 2, skip=True)
    for r, sh in enumerate(("round", "square")):
        for i, st in enumerate(["normal", "hover", "pressed", "disabled"]): put(im, f"kit_iconbtn_{sh}_{st}", (40 + i * 100, y + 20 + r * 80), (64, 64))
    text(im, (520, y), "Nút đóng (X đỏ) và tab", 22, M.TXT, "lm", 2, skip=True)
    for i, st in enumerate(["normal", "hover", "pressed", "disabled"]):
        put(im, f"kit_close_{st}", (520 + i * 80, y + 20), (52, 52)); ink = spr("kit_icon_cross_red", M.T20); k = 26 / ink.height; q = ink.resize((max(1, round(ink.width * k)), 26), Image.LANCZOS); im.alpha_composite(q, (520 + i * 80 + 26 - q.width // 2, y + 20 + 13))
    for i, st in enumerate(["off", "hover", "on"]):
        put(im, f"kit_tab_{st}", (900 + i * 170, y + 12), (160, 64)); text(im, (980 + i * 170, y + 12 + [46, 42, 36][i]), ["Chung", "Âm thanh", "Điều khiển"][i], 18, DARK if st != "off" else M.TXT, "mm", 0 if st != "off" else 2, skip=True)
    y += 190
    px, py, pw, ph = 40, y, 760, 340; put(im, "kit_panel_popup", (px, py), (pw, ph)); text(im, (px + pw / 2, py + 55), "Xóa phòng?", 32, M.GOLD, "mm", 2, skip=True); M.close_btn(im, px, py, pw)
    text(im, (px + pw / 2, py + 160), "Phòng Leo rank tối nay sẽ bị xóa.", 22, M.TXT, "mm", 2, skip=True); by = M.footer(im, px, py, pw, ph)
    btn(im, "secondary", "normal", (px + 70, by), (250, 64), "Hủy"); btn(im, "danger", "normal", (px + pw - 70 - 250, by), (250, 64), "Xóa phòng")
    im.convert("RGB").crop((0, 0, W, y + 380)).save(OUT / "ho-nut-k2.png"); print("ok")
main()
