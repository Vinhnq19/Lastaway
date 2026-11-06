"""T-026: p0-sheet.png tu sprite kit da nhap."""
import sys
sys.path.insert(0, r"D:\Lastaway\projects\lastaway\art\inbox\T-021\src\tools")
import mock as M
from mock import put, text, spr, tw
from PIL import Image
OUT = r"D:\Lastaway\projects\lastaway\art\inbox\T-026\p0-sheet.png"
DARK = (31, 26, 16)
W, H = 2000, 1500; im = Image.new("RGBA", (W, H), (43, 40, 33, 255)); im.alpha_composite(M.bg(0.62).resize((W, H)))
text(im, (40, 40), "P0 kit (T-026): chân hộp thoại, header, thẻ phòng, ô người chơi, ô mã phòng, keycap, bộ icon", 28, M.GOLD, "lm")
names = ["gear", "map", "bag", "copy", "refresh", "person", "clock", "wifi", "sound", "music", "vibrate", "eye", "skull", "arrow_left", "arrow_right", "trophy"]
labs = ["Cài đặt", "Bản đồ", "Túi", "Sao chép", "Làm mới", "Người", "Đồng hồ", "Mạng", "Âm thanh", "Nhạc", "Rung", "Mắt", "Sọ", "Trái", "Phải", "Cúp"]
text(im, (40, 100), "T04 Bộ 16 icon (mực tối trên nút kaki)", 22, M.TXT, "lm")
for i, n in enumerate(names):
    c, r = i % 8, i // 8; x = 40 + c * 120; y = 130 + r * 130
    put(im, "kit_iconbtn_square_normal", (x, y), (64, 64)); im.alpha_composite(spr(f"kit_ico_{n}", M.T20).resize((40, 40), Image.NEAREST), (x + 12, y + 8)); text(im, (x + 32, y + 90), labs[i], 15, M.MUTED, "mm")
text(im, (1060, 100), "T08 Keycap, T07 Ô mã phòng", 22, M.TXT, "lm")
for i, (st, k) in enumerate((("normal", "F"), ("active", "M"), ("disabled", "Q"))):
    put(im, f"kit_keycap_{st}", (1060 + i * 80, 130), (52, 52)); text(im, (1060 + i * 80 + 26, 130 + 24), k, 22, DARK if st != "disabled" else (235, 235, 230), "mm", 0 if st != "disabled" else 2)
text(im, (1060, 200), "keycap: thường, nhấn mạnh, tắt", 15, M.MUTED, "lm")
for i, (st, ch) in enumerate((("empty", ""), ("active", ""), ("filled", "4"), ("error", "X"))):
    put(im, f"kit_codebox_{st}", (1060 + i * 90, 240), (66, 78)); text(im, (1060 + i * 90 + 33, 240 + 39), ch, 36, M.TXT, "mm")
text(im, (1060, 335), "ô mã: rỗng, đang nhập, đã điền, lỗi (kèm dòng lỗi có icon X)", 15, M.MUTED, "lm")
text(im, (40, 395), "T03 Header màn + nút quay lại", 22, M.TXT, "lm")
put(im, "kit_screen_header", (40, 425), (1200, 104)); put(im, "kit_iconbtn_square_normal", (70, 445), (64, 64)); im.alpha_composite(spr("kit_ico_arrow_left", M.T20).resize((40, 40), Image.NEAREST), (82, 453)); text(im, (640, 477), "Phòng của tôi", 34, M.GOLD, "mm")
text(im, (40, 575), "T02 Chân hộp thoại (trong popup)", 22, M.TXT, "lm")
px, py, pw, ph = 40, 605, 760, 340; put(im, "kit_panel_popup", (px, py), (pw, ph)); text(im, (px + pw / 2, py + 55), "Xóa phòng?", 32, M.GOLD, "mm")
put(im, "kit_close_normal", (px + pw - 82, py + 26), (52, 52)); im.alpha_composite(spr("kit_icon_cross_ink", M.T20).resize((24, 24), Image.NEAREST), (px + pw - 68, py + 38))
fy = py + ph - 47 - 96; put(im, "kit_dialog_footer", (px + 47, fy), (pw - 94, 96)); M_ = None
def btn(kind, xy, w, cap):
    put(im, f"kit_btn_{kind}_normal", xy, (w, 64)); text(im, (xy[0] + w / 2, xy[1] + 28), cap, 26, DARK, "mm", 0)
btn("secondary", (px + 70, fy + 16), 250, "Hủy"); btn("danger", (px + pw - 70 - 250, fy + 16), 250, "Xóa phòng"); text(im, (px + pw / 2, py + 160), "Phòng Leo rank tối nay sẽ bị xóa.", 22, M.TXT, "mm")
text(im, (860, 575), "T05 Thẻ phòng: thường, chọn, đầy", 22, M.TXT, "lm")
for i, (st, nm, cap) in enumerate((("normal", "Phòng mặc định", "6/10"), ("selected", "Leo rank tối nay", "3/10"), ("full", "Luyện tập", "10/10 đầy"))):
    y = 605 + i * 135; put(im, f"kit_card_room_{st}", (860, y), (700, 120)); text(im, (900, y + 60), nm, 28, M.TXT if st != "full" else M.MUTED, "lm"); text(im, (1520, y + 60), cap, 24, M.TXT if st != "full" else M.MUTED, "rm")
text(im, (1600, 575), "T06 Ô người chơi (rỗng, chưa, sẵn sàng)", 22, M.TXT, "lm")
for i, (st, nm) in enumerate((("empty", "Chỗ trống"), ("notready", "V.Huy"), ("ready", "Hào"))):
    y = 605 + i * 120; put(im, f"kit_slot_player_{st}", (1600, y), (380, 96)); text(im, (1700, y + 48), nm, 24, M.MUTED if st == "empty" else M.TXT, "lm")
text(im, (40, 1040), "Sprite: kit_dialog_footer, kit_screen_header, kit_card_room_*, kit_slot_player_*, kit_codebox_*, kit_keycap_*, kit_ico_* (16, 48x48 + @2x). 9-slice trong meta.json.", 18, M.MUTED, "lm")
im.convert("RGB").crop((0, 0, W, 1090)).save(OUT); print("ok")
