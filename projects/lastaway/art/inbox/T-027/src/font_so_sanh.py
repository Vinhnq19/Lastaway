"""T-027 buoc 2: trang so sanh font chinh (tieu de, so lon) tren 3 canh: tieu de phong cho, so HUD, man Thang.
Chay: PYTHONIOENCODING=utf-8 python font_so_sanh.py   (ra ../font-so-sanh.png, ../font-so-sanh-crop100.png)
Font chinh ap cho moi chu tu bac H2 (40 px) tro len (thang 5 bac: 40, 96) va cho so HUD (100, 0) de xem thu; chu than, nut, chu thich van la Be Vietnam Pro."""
import sys, json, pathlib
sys.path.insert(0, str(pathlib.Path(__file__).parent))
import demo, mock, screens_a, screens_b
from PIL import Image, ImageDraw, ImageFont
OUT = demo.OUT; ART = pathlib.Path(r"D:\Lastaway\Assets\_JungleMaze\Art")
FONTS = [("Be Vietnam Pro Bold (hiện tại, mọi bậc)", None), ("Denk One (OFL, trong repo)", ART / "JSurviv/Fonts/DenkOne.ttf")]
S = demo.S; S["force"] = False; S["main"] = None; orig_F = demo.orig_F
def F2(n):
    m = S["map"]; sz = int(m(n)) if m else int(n)
    if S["main"] and (sz >= 40 or S["force"]): return ImageFont.truetype(str(S["main"]), sz)
    return orig_F(sz)
mock.F = F2
def scene(kind, main):
    S["main"] = main; S["force"] = False; demo.setmap("a6" if kind[0] == "a" else "b6", "5"); S["rec"] = []; mock.LOG.clear(); mock.LOGB.clear()
    if kind == "a6": im = screens_a.ALL["a6-phong-cho-cho-du-nguoi"]()
    elif kind == "a6d": im = screens_a.ALL["a6-phong-cho-dem-nguoc"]()
    elif kind == "b6": im = screens_b.ALL["b6-thang"]()
    elif kind == "b7": im = screens_b.ALL["b7-thua"]()
    else:
        demo.setmap("hud", "5"); im = screens_b.hudbg(0)
        for b in demo.ERASE[:4]: demo.erase(im, b)
        S["force"] = True
        for est, s, xy, an in demo.BAKED[3:7]: demo.text(im, xy, s, est, mock.TXT, an)
        S["force"] = False
    return im.convert("RGB")
CROPS = [("1. Tiêu đề phòng chờ (H2 40 px: \"Phòng chờ\"; H1 96 px: \"Chờ\")", "a6", (600, 30, 1560, 720)), ("2a. Số HUD: máu 100 và 0 (thử ở 30 px)", "hud", (10, 855, 470, 1075)), ("2b. Đếm ngược 12 (H1 96 px)", "a6d", (900, 250, 1900, 750)),
         ("2c. Hạng #4 (H1 96 px) và thẻ nguyên nhân", "b7", (60, 380, 900, 780)), ("3. Màn Thắng: CHIẾN THẮNG (H1 96 px), #1 / 10 (H2 40 px)", "b6", (40, 130, 960, 600))]
def main():
    imgs = {}
    for (lab, fp) in FONTS:
        for _, k, _ in CROPS: imgs[(lab, k)] = scene(k, fp)
    loc = {r["file"].split("Fonts/")[-1]: r for r in json.load(open(OUT / "font-loc.json", encoding="utf-8"))}
    W = 1920; hs = [c[2][3] - c[2][1] for c in CROPS]; H = 330 + sum(hs) + len(CROPS) * 60 + 40; im = Image.new("RGB", (W, H), (24, 22, 18)); d = ImageDraw.Draw(im); f = orig_F
    d.text((20, 10), "Font chính: so sánh trên 3 cảnh. Trái = Be Vietnam Pro Bold (hiện tại), phải = Denk One (font duy nhất trong repo qua lọc dấu + giấy phép)", font=f(26), fill=(255, 220, 110))
    d.text((20, 54), "Lọc (src/loc_font.py, 14 font trong repo): đủ dấu và OFL = Be Vietnam Pro (đã là font thân), Denk One. Không đạt:", font=f(20), fill=(237, 242, 238))
    L = ["Pixelify Sans: OFL nhưng thiếu 92/146 ký tự có dấu (ơ ư và mọi dấu nặng, hỏi, sắc...)", "FVF Fernando 08: đủ dấu nhưng chỉ ghi \"All rights reserved\" (Font Viet 2004), không có giấy phép thương mại", "BeaufortforLOL: đủ dấu nhưng font thương hiệu (Riot), loại", "ArcadeClassic, DisposableDroid BB, Renogare Soft: thiếu 134, 134, 98 ký tự có dấu; bản quyền không ghi giấy phép dùng thương mại"]
    for i, t in enumerate(L): d.text((40, 84 + i * 26), "- " + t, font=f(20), fill=(214, 218, 214))
    d.text((20, 200), "Thử dấu chồng (cùng dòng, 96 px và 40 px):", font=f(22), fill=(255, 220, 110))
    s = "Ắ Ặ Ề Ộ Ữ Đ ắ ặ ề ộ ữ đ Chờ"
    for j, (lab, fp) in enumerate(FONTS):
        ft = ImageFont.truetype(str(fp), 64) if fp else f(64); d.text((20 + j * 960, 236), s, font=ft, fill=(237, 242, 238), stroke_width=2, stroke_fill=(10, 12, 12))
        d.text((20 + j * 960, 316 - 2), lab.split(" (")[0], font=f(18), fill=(214, 218, 214))
    y = 340
    for title, k, box in CROPS:
        d.text((20, y), title, font=f(24), fill=(255, 220, 110)); y += 40
        for j, (lab, fp) in enumerate(FONTS): im.paste(imgs[(lab, k)].crop(box), (10 + j * 960, y))
        y += box[3] - box[1] + 20
    im.crop((0, 0, W, y)).save(OUT / "font-so-sanh.png"); print("ok", y)
main()
