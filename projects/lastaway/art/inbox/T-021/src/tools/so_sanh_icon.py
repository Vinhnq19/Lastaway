"""So sanh cu/moi cho icon vat pham gameplay (T-021 B). Ra b/so-sanh-icon.png"""
import sys, pathlib
sys.path.insert(0, str(pathlib.Path(__file__).parent))
import screens_b as S
from PIL import Image, ImageDraw
import mock
def crops(old):
    S.SZ.clear(); S.SZ.update(S.OLD if old else S.NEW); mock.CUR[0] = "so-sanh"
    a = S.inventory("main"); b = S.feedback()
    return a.crop((170, 200, 1760, 820)).resize((1060, 413), Image.LANCZOS), b.crop((960, 560, 1920, 1080)).resize((640, 347), Image.LANCZOS)
o = crops(True); n = crops(False)
im = Image.new("RGB", (2200, 900), (24, 22, 18)); d = ImageDraw.Draw(im); f = mock.F(28)
d.text((20, 10), "CU: icon 40 (hang duoi dat), 76 (o), 56 (trang bi), 50 (toast nhat), 40 (danh sach nhat)", font=f, fill=(255, 220, 110)); im.paste(o[0], (20, 50)); im.paste(o[1], (1100, 50))
d.text((20, 470), "MOI: icon 56, 104, 72, 72 (kem ten + so luong), 56 (kem ten)", font=f, fill=(255, 220, 110)); im.paste(n[0], (20, 510)); im.paste(n[1], (1100, 510))
im.save(pathlib.Path(__file__).resolve().parent.parent.parent / "b" / "so-sanh-icon.png"); print("ok")
