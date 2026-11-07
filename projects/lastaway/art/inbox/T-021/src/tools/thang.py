"""Thang chu T-027 (Huy/CPO chot 07/11): 5 bac 96/40/30/24/20; chu nut theo co nut S 24, M 30, L 40; san gameplay 24.
Font: Chakra Petch Bold cho bac 96 va 40 (font chinh) (tieu de, so lon), Be Vietnam Pro Bold cho bac 30, 24, 20 va MOI chu tren nut (ke ca nut L).
Tat de dung lai ban truoc thang: dat bien moi truong LASTAWAY_THANG=0."""
import os, pathlib
from PIL import ImageFont
ON = os.environ.get("LASTAWAY_THANG", "1") != "0"
ART = pathlib.Path(r"D:\Lastaway\Assets\_JungleMaze\Art")
BVP = ART / "Fonts/BeVietnamPro-Bold.ttf"; MAIN = ART / "JSurviv/Fonts/ChakraPetch-Bold.ttf"; DENK = ART / "JSurviv/Fonts/DenkOne.ttf"  # MAIN = font chinh (Chakra Petch Bold, Huy chot 07/11 thay Denk One)
LAD = [20, 24, 30, 40, 96]; BTN = {48: 24, 64: 30, 80: 40}; MAIN_MIN = 40   # tu bac 40 tro len (khong phai nut) dung MAIN (Chakra Petch Bold)
GAME = ("b3", "b4", "b5", "b9", "hud")
def is_game(mock_name): return mock_name.split("-")[0] in GAME
def size(sz, game=False):
    if not ON: return sz
    b = min(LAD, key=lambda v: (abs(v - sz), -v)); return max(b, 24) if game and b < 24 else b
def btn_size(h, font):
    if not ON: return font
    return BTN[min(BTN, key=lambda k: (abs(k - h), k))]
def stroke(sz, s): return s if (not ON or not s) else max(2, round(sz / 12))
_c = {}
def font(sz, btn=False):
    main = ON and sz >= MAIN_MIN and not btn; k = (sz, main)
    if k not in _c: _c[k] = ImageFont.truetype(str(MAIN if main else BVP), sz)
    return _c[k]
def tier(sz, btn=False): return "Main" if (ON and sz >= MAIN_MIN and not btn) else "BVP"
