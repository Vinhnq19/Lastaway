"""T-028 dot 2: nhap 5 asset gen GPT thay cac phan tu 've bang ma': kit_killfeed_panel, kit_map_bg, map_grid, map_ring_zone, map_ring_safe.
Tach nen magenta (khong xoi mon net manh: grid), cat sat, doi co, ghi meta. PYTHONIOENCODING=utf-8 python import_gen2.py"""
import sys, json, pathlib
import numpy as np
sys.path.insert(0, r"D:\Lastaway\projects\lastaway\art\inbox\T-015\src\tools")
from extract_t015 import key_magenta
from PIL import Image
G = pathlib.Path(__file__).parent / "gen"; N = pathlib.Path(__file__).parent.parent / "files" / "new"; meta = json.load(open(N / "meta.json", encoding="utf-8"))
def key_thin(im):
    a = np.asarray(im.convert("RGB")).astype(int); d = (255 - a[..., 0]) + a[..., 1] + (255 - a[..., 2]); al = np.clip((d - 70) / 80, 0, 1)
    rgb = np.zeros_like(a); rgb[...] = (60, 57, 46)  # duong luoi mot mau, bo vien magenta
    out = np.dstack([rgb, (al * 255).astype(int)]).astype("uint8"); return Image.fromarray(out, "RGBA")
def prep(src, thin=False):
    a = (key_thin if thin else key_magenta)(Image.open(G / f"{src}.png")); return a.crop(a.getchannel("A").getbbox())
JOBS = [("killfeed", "kit_killfeed_panel", (192, 64), [16, 16, 16, 16], False, "panel kill feed (Image #14120E nền, kéo giãn theo số dòng)"), ("mapbg", "kit_map_bg", (256, 256), [24, 24, 24, 24], False, "nền khung bản đồ lớn"),
        ("mapgrid", "map_grid", (840, 840), None, True, "lưới 10x10 phủ lên bản đồ (đường mảnh, nền trong suốt)"), ("ringzone", "map_ring_zone", (512, 512), None, False, "vòng bo hiện tại (nét vàng cam), vẽ 680x680"), ("ringsafe", "map_ring_safe", (256, 256), None, False, "vòng bo tiếp theo, vùng an toàn (nét trắng), vẽ 180x180; cỡ 32x32 làm chú giải")]
for src, name, (w, h), border, thin, note in JOBS:
    a = prep(src, thin)
    if name in ("map_ring_zone", "map_ring_safe", "map_grid"): s = max(a.size); sq = Image.new("RGBA", (s, s), (0, 0, 0, 0)); sq.alpha_composite(a, ((s - a.width) // 2, (s - a.height) // 2)); a = sq
    for suf, k in (("", 1), ("@2x", 2)): a.resize((w * k, h * k), Image.LANCZOS).save(N / f"{name}{suf}.png")
    meta[name] = {"size": [w, h], "border_LRTB": border, "slice": bool(border), "nguon": f"gen GPT qua codex-img.sh, prompt `src/gen/{src}.prompt.src.txt`; {note}"}
json.dump(meta, open(N / "meta.json", "w", encoding="utf-8"), ensure_ascii=False, indent=1); print(list(meta))
