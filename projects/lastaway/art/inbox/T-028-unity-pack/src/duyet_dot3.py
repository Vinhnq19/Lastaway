"""T-028 dot 3: trang duyet `files/new/duyet-dot3.png`: 4 icon nut moi tren nut K2 that (100%), icon Bo thu trong the nguyen nhan B2, bang 18 icon vu khi cho spectator.
PYTHONIOENCODING=utf-8 python duyet_dot3.py"""
import sys, pathlib
sys.path.insert(0, r"D:\Lastaway\projects\lastaway\art\inbox\T-021\src\tools")
import mock, screens_b
from PIL import Image, ImageDraw
IN = pathlib.Path(r"D:\Lastaway\projects\lastaway\art\inbox"); P = IN / "T-028-unity-pack"; N = P / "files/new"; mock.CUR[0] = "a2"
for nm in ("practice", "save", "globe", "fill"):
    a = Image.open(N / f"kit_ico_{nm}.png").convert("RGBA"); mock._ink[nm] = a.crop(a.getchannel("A").getbbox())
W = 1920; im = mock.bg(0.62).convert("RGBA").crop((0, 0, W, 1000)); d = ImageDraw.Draw(im)
mock.text(im, (40, 30), "Duyệt đợt 3 (ở cỡ 100%): 4 icon nút trên nút K2, icon Bo thu trên thẻ nguyên nhân, 18 icon vũ khí cho spectator", 28, mock.GOLD, "lm", 2, skip=True)
mock.text(im, (40, 90), "1. Icon nút mới (nút secondary M 64, chữ 30; so với icon tạm cũ)", 24, mock.TXT, "lm", 2, skip=True)
rows = [("Luyện tập offline", "practice", "play"), ("Lưu endpoint", "save", "check"), ("Xem online", "globe", "list"), ("Điền đủ", "fill", "people")]
for i, (cap, new, old) in enumerate(rows):
    x = 40 + (i % 2) * 940; y = 130 + (i // 2) * 90
    mock.button(im, "secondary", (x, y), cap, None, mock.WHITE, (400, 64), "normal", 30, new); mock.button(im, "secondary", (x + 440, y), cap, None, mock.WHITE, (400, 64), "normal", 30, old)
    mock.text(im, (x + 410, y + 32), "mới", 20, mock.MUTED, "mm", 2, skip=True) if False else None
mock.text(im, (40, 330), "Cột trái = icon mới; cột phải = icon tạm của unity (kit_ico_play, check, list, people)", 20, mock.MUTED, "lm", 2, skip=True)
# the nguyen nhan Bo thu: cu va moi
b2 = Image.open(IN / "T-021/b/b2-bi-loai-bo.png").convert("RGBA"); crop_box = (540, 370, 1380, 560); cx = 960
mock.text(im, (40, 380), "2. Nguyên nhân bị loại 'Bo thu' (thẻ kit_card_cause 800x150): trái = icon tạm (icon_zone-timer), phải = icon_zone_cause mới", 24, mock.TXT, "lm", 2, skip=True)
im.paste(b2.crop(crop_box), (40, 410))
new = b2.copy(); mock.CUR[0] = "b2"; mock.put(new, "kit_card_cause", (cx - 400, 390), (800, 150)); ic = Image.open(N / "icon_zone_cause@2x.png").convert("RGBA").resize((60, 60), Image.LANCZOS); new.alpha_composite(ic, (cx - 400 + 88 - 30, 465 - 30)); mock.text(new, (cx - 400 + 160, 465), "Bo thu", 30, mock.TXT, "lm")
im.paste(new.crop(crop_box), (40 + 880, 410))
# vu khi spectator
mock.text(im, (40, 640), "3. 18 icon vũ khí (GDD ITEM LIST) cho mục tiêu spectator: đủ icon sẵn trong Assets, không gen thêm; hàng dưới là thẻ mục tiêu spectator mẫu", 24, mock.TXT, "lm", 2, skip=True)
names = [("Tay không", "HAND"), ("Cành cây", "TREE_BRANCH"), ("Dao", "KNIFE"), ("Xương", "BONE"), ("Rìu", "AXE"), ("Chuỳ gai", "MACE"), ("Ná cao su", "RUBBER_GUN"), ("Quyền trượng", "SCEPTER"), ("Nỏ", "CROSSBOW"), ("Shotgun", "SHOTGUN"), ("Chuối", "BANANA"), ("Bom khói", "SMOKE"), ("Mảnh thiên thạch", "METEORITE_FRAGMENT"), ("Tinh thể", "CRYSTAL"), ("Hạt", "SEED"), ("Dây thừng", "ROPE"), ("Chuông", "BELL"), ("Gậy đánh bóng", "SAHUR_BAT")]
for i, (vn, f) in enumerate(names):
    x = 40 + (i % 6) * 310; y = 675 + (i // 6) * 58; ic = Image.open(P / "files/items" / f"{f}.png").convert("RGBA").resize((44, 44), Image.LANCZOS); im.alpha_composite(ic, (x, y)); mock.text(im, (x + 52, y + 22), vn, 20, mock.TXT, "lm", 2, skip=True)
mock.CUR[0] = "b3"; mock.put(im, "kit_card_stat", (40, 870), (640, 120)); mock.text(im, (84, 905), "Tuan", 28, mock.TXT, "lm", 2, skip=True); ic = Image.open(P / "files/items/CROSSBOW.png").convert("RGBA").resize((44, 44), Image.LANCZOS); im.alpha_composite(ic, (600, 885)); mock.text(im, (84, 950), "Mục tiêu đang theo dõi: tên + icon vũ khí", 20, mock.MUTED, "lm", 2, skip=True)
im.convert("RGB").save(N / "duyet-dot3.png"); print(im.size)
