"""T-028 legend: nhap legend_ring_next_dashed 32x32 + @2x tu gen/legend-ring.png. python -P import_gen4.py"""
import sys, json, pathlib
sys.path.insert(0, r"D:\Lastaway\projects\lastaway\art\inbox\T-015\src\tools")
from extract_t015 import key_magenta
from PIL import Image
G = pathlib.Path(__file__).parent / "gen"; N = pathlib.Path(__file__).parent.parent / "files/new"
a = key_magenta(Image.open(G / "legend-ring.png")); a = a.crop(a.getchannel("A").getbbox()); s = max(a.size); sq = Image.new("RGBA", (s, s), (0, 0, 0, 0)); sq.alpha_composite(a, ((s - a.width) // 2, (s - a.height) // 2))
for suf, c in (("", 32), ("@2x", 64)): sq.resize((c, c), Image.LANCZOS).save(N / f"legend_ring_next_dashed{suf}.png")
m = json.load(open(N / "meta.json", encoding="utf-8")); m["legend_ring_next_dashed"] = {"size": [32, 32], "border_LRTB": None, "slice": False, "nguon": "gen GPT qua codex-img.sh, prompt `src/gen/legend-ring.prompt.src.txt`; chú giải vòng bo kế tiếp: vòng trắng nét đứt 12 đoạn, nét ~2 px ở 32 px"}
json.dump(m, open(N / "meta.json", "w", encoding="utf-8"), ensure_ascii=False, indent=1)
