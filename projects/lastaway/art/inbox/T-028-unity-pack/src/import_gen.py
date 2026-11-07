"""T-028: nhap 3 asset gen GPT (icon_ammo, icon_ground, kit_badge_key): tach nen magenta, cat sat, dua ve o vuong, doi co, ghi meta.
PYTHONIOENCODING=utf-8 python import_gen.py"""
import sys, json, pathlib
sys.path.insert(0, r"D:\Lastaway\projects\lastaway\art\inbox\T-015\src\tools")
from extract_t015 import key_magenta
from PIL import Image
G = pathlib.Path(__file__).parent / "gen"; N = pathlib.Path(__file__).parent.parent / "files" / "new"; N.mkdir(parents=True, exist_ok=True); meta = {}
def prep(src, pad=0.04):
    a = key_magenta(Image.open(G / f"{src}.png")); a = a.crop(a.getchannel("A").getbbox()); s = max(a.size); sq = round(s * (1 + 2 * pad)); o = Image.new("RGBA", (sq, sq), (0, 0, 0, 0)); o.alpha_composite(a, ((sq - a.width) // 2, (sq - a.height) // 2)); return o
for src, name, px, border in (("ammo", "icon_ammo", 64, None), ("ground", "icon_ground", 64, None), ("badge-key", "kit_badge_key", 48, [14, 14, 14, 14])):
    sq = prep(src, 0.0 if name == "kit_badge_key" else 0.04)
    for suf, k in (("", 1), ("@2x", 2)): sq.resize((px * k, px * k), Image.LANCZOS).save(N / f"{name}{suf}.png")
    meta[name] = {"size": [px, px], "border_LRTB": border, "slice": bool(border), "nguon": f"gen GPT qua codex-img.sh, prompt `src/gen/{src}.prompt.src.txt`, ref nét: kit T-020 + icon Assets"}
json.dump(meta, open(N / "meta.json", "w", encoding="utf-8"), ensure_ascii=False, indent=1); print(meta.keys())
