"""T-029: nhap 7 icon gen GPT vao files/ (tach nen magenta, cat sat, o vuong, resize @1x/@2x). python -P import.py"""
import sys, json, pathlib
sys.path.insert(0, r"D:\Lastaway\projects\lastaway\art\inbox\T-015\src\tools")
from extract_t015 import key_magenta
from PIL import Image
S = pathlib.Path(__file__).parent; G = S / "gen"; F = S.parent / "files"; F.mkdir(exist_ok=True)
SPEC = [("flag", "wp_flag", 48, "cờ waypoint (xanh trời #5BB8E8): bản đồ lớn vẽ 24 px, trên world vẽ 40 px"),
        ("flagmini", "wp_flag_mini", 16, "cờ waypoint đơn giản hóa cho minimap, vẽ 14 px"),
        ("arrow", "wp_arrow_edge", 48, "mũi tên mép màn hướng lên, xoay theo hướng tới cờ, vẽ 40 px; số khoảng cách đặt trên `kit_badge_key` kéo giãn"),
        ("enemydot", "map_enemy_dot", 16, "chấm địch đỏ #E5484D viền tối: vẽ 12 px (minimap) và 16 px (bản đồ lớn)"),
        ("lastseen", "map_lastseen_ring", 16, "dấu thấy lần cuối (vòng rỗng đỏ), cỡ như chấm địch; alpha 0,6 giảm về 0 trong 2,0 s do code"),
        ("zoom", "kit_ico_zoom", 48, "kính lúp có dấu cộng, icon mục tối: chỉ báo mức zoom minimap, vẽ 24 px"),
        ("self", "map_self_marker", 32, "marker bản thân Amber #C6AE78 (thay marker đỏ cũ), mũi nhọn trên chỉ hướng Hero, vẽ 26 px")]
meta = {}
for src, name, px, note in SPEC:
    if not (G / f"{src}.png").exists(): continue
    a = key_magenta(Image.open(G / f"{src}.png")); a = a.crop(a.getchannel("A").getbbox()); s = max(a.size); sq = Image.new("RGBA", (s, s), (0, 0, 0, 0)); sq.alpha_composite(a, ((s - a.width) // 2, (s - a.height) // 2))
    for suf, k in (("", 1), ("@2x", 2)): sq.resize((px * k, px * k), Image.LANCZOS).save(F / f"{name}{suf}.png")
    meta[name] = {"size": [px, px], "border_LRTB": None, "slice": False, "nguon": f"gen GPT qua codex-img.sh, prompt `src/gen/{src}.prompt.src.txt`; {note}"}
json.dump(meta, open(F / "meta.json", "w", encoding="utf-8"), ensure_ascii=False, indent=1); print(len(meta))
