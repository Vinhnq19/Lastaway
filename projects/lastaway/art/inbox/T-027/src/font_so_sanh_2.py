"""T-027 FONT 2: Denk One so voi 3 font ngoai qua loc dau (Bangers, Chakra Petch Bold, Tiny5) tren cung canh, crop 100%.
Chay: PYTHONIOENCODING=utf-8 python font_so_sanh_2.py   (ra ../font-so-sanh-2.png)
Khong sua mock: doi thang.MAIN sang font thu roi dung lai mock da co (lop thang dat font chinh cho bac 96 va 40 khong phai nut).
Tiny5 la font pixel (luoi 8 px/em: cap 5 px, x-height 4 px): co 40 va 96 deu la boi so cua 8; so HUD thu o 32."""
import sys, pathlib, json
sys.path.insert(0, r"D:\Lastaway\projects\lastaway\art\inbox\T-021\src\tools")
import thang, mock, screens_a, screens_b
from PIL import Image, ImageDraw, ImageFont
OUT = pathlib.Path(r"D:\Lastaway\projects\lastaway\art\inbox\T-027"); FN = OUT / "font-ngoai"
FONTS = [("Denk One (trong repo, đã bỏ)", thang.DENK, 30), ("Bangers (OFL, 91 KB)", FN / "bangers/Bangers-Regular.ttf", 30), ("Chakra Petch Bold (OFL, 77 KB)", FN / "chakrapetch/ChakraPetch-Bold.ttf", 30), ("Tiny5 (OFL, pixel, 423 KB)", next((FN / "tiny5").glob("*.ttf")), 32)]
REC = []; orig_text = mock.text
def text(im, xy, s, size, fill=mock.TXT, anchor="la", stroke=2, skip=False, btn=False, exempt=False):
    if s.strip() and not skip:
        sz = thang.size(size, thang.is_game(mock.CUR[0]) and not exempt); REC.append((s, sz, ImageDraw.Draw(im).textbbox(xy, s, font=thang.font(sz, btn), anchor=anchor)))
    return orig_text(im, xy, s, size, fill, anchor, stroke, skip, btn, exempt)
mock.text = text; screens_a.text = text; screens_b.text = text
def scene(kind, fp, hudpx):
    thang.MAIN = fp; thang._c.clear(); REC.clear(); mock.CUR[0] = {"a6": "a6", "a6d": "a6", "hud": "b9", "b7": "b7", "b6": "b6"}[kind]
    if kind == "a6": im = screens_a.ALL["a6-phong-cho-cho-du-nguoi"]()
    elif kind == "a6d": im = screens_a.ALL["a6-phong-cho-dem-nguoc"]()
    elif kind == "b7": im = screens_b.ALL["b7-thua"]()
    elif kind == "b6": im = screens_b.ALL["b6-thang"]()
    else:
        im = screens_b.hudbg(0); screens_b._erase(im, (352, 880, 424, 916)); ImageDraw.Draw(im).text((383, 898), "100", font=ImageFont.truetype(str(fp), hudpx), fill=mock.TXT, anchor="mm", stroke_width=2, stroke_fill=mock.OUTL)
        REC.append(("100", hudpx, (352, 880, 424, 916)))
    return im.convert("RGB"), list(REC)
# (nhan, canh, chu can cat, rong, cao)
ITEMS = [("1a. Tiêu đề phòng chờ, bậc 40", "a6", "Phòng chờ", 460, 100), ("1b. \"Chờ\", bậc 96", "a6", "Chờ", 460, 170), ("2a. Máu 100, thử ở 30 (Tiny5: 32)", "hud", "100", 460, 120), ("2b. Đếm ngược 12, bậc 96", "a6d", "12", 460, 170),
         ("2c. Hạng #4, bậc 96", "b7", "HẠNG #4", 460, 150), ("3b. #1 / 10, bậc 40", "b6", "#1 / 10", 460, 100)]
def main():
    data = {}
    for lab, fp, hp in FONTS:
        for kind in ("a6", "a6d", "hud", "b7", "b6"): data[(lab, kind)] = scene(kind, fp, hp)
    B = thang.BVP; f = lambda n: ImageFont.truetype(str(B), n); CW = 470; W = 1920
    H = 330 + sum(h + 44 for *_, h in ITEMS) + 30 + 2 * 190 + 44; im = Image.new("RGB", (W, H), (24, 22, 18)); d = ImageDraw.Draw(im)
    d.text((20, 10), "Font ngoài đã qua lọc dấu (146 ký tự), đặt cạnh Denk One. Mọi crop 100%, không thu nhỏ.", font=f(26), fill=(255, 220, 110))
    d.text((20, 52), "Loại: Tilt Warp (OFL, có subset vietnamese theo metadata nhưng thiếu chữ đ U+0111 trong font).", font=f(20), fill=(237, 242, 238))
    d.text((20, 82), "Bậc 96 và 40 dùng font thử; bậc 30, 24, 20 và mọi chữ nút vẫn Be Vietnam Pro. Tiny5 (pixel, lưới 8 px/em): 40 = 5 x 8 và 96 = 12 x 8 đều là bội của lưới.", font=f(20), fill=(214, 218, 214))
    for j, (lab, fp, hp) in enumerate(FONTS):
        x = 20 + j * CW; d.text((x, 118), lab, font=f(20), fill=(255, 220, 110)); ft = ImageFont.truetype(str(fp), 48)
        d.text((x, 150), "Ắ Ặ Ề Ộ Ữ Đ", font=ft, fill=(237, 242, 238), stroke_width=2, stroke_fill=(10, 12, 12)); d.text((x, 218), "ắ ặ ề ộ ữ đ Chờ", font=ft, fill=(237, 242, 238), stroke_width=2, stroke_fill=(10, 12, 12))
    d.text((20, 290), "Hàng thử dấu chồng ở 48 px (trên); các cảnh dưới.", font=f(20), fill=(214, 218, 214)); y = 330
    for title, kind, s, w, h in ITEMS:
        d.text((20, y), title, font=f(22), fill=(255, 220, 110)); y += 34
        for j, (lab, fp, hp) in enumerate(FONTS):
            img, rec = data[(lab, kind)]; cand = [r for r in rec if r[0] == s]
            if not cand: continue
            bb = max(cand, key=lambda r: r[1])[2]; cx, cy = (bb[0] + bb[2]) // 2, (bb[1] + bb[3]) // 2; X = min(max(0, cx - w // 2), 1920 - w); Y = min(max(0, cy - h // 2), 1080 - h); im.paste(img.crop((X, Y, X + w, Y + h)), (20 + j * CW, y))
        y += h + 10
    d.text((20, y), "3a. CHIẾN THẮNG, bậc 96 (ô rộng 940 px, hai font mỗi hàng)", font=f(22), fill=(255, 220, 110)); y += 34
    for j, (lab, fp, hp) in enumerate(FONTS):
        img, rec = data[(lab, "b6")]; bb = max([r for r in rec if r[0] == "CHIẾN THẮNG"], key=lambda r: r[1])[2]; cx, cy = (bb[0] + bb[2]) // 2, (bb[1] + bb[3]) // 2; X = min(max(0, cx - 470), 1920 - 940); Y = min(max(0, cy - 80), 1080 - 160)
        im.paste(img.crop((X, Y, X + 940, Y + 160)), (20 + (j % 2) * 950, y + (j // 2) * 190)); d.text((24 + (j % 2) * 950, y + (j // 2) * 190 + 132), lab.split(" (")[0], font=f(18), fill=(255, 220, 110))
    y += 2 * 190
    im.crop((0, 0, W, y)).save(OUT / "font-so-sanh-2.png"); print("ok", y)
main()
