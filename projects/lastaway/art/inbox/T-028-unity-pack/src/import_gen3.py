"""T-028 dot 3: nhap 5 icon gen GPT (kit_ico_practice, kit_ico_save, kit_ico_globe, kit_ico_fill: icon muc 48x48; icon_zone_cause 64x64 mau),
tach nen magenta, cat sat, dua ve o vuong; chep 18 icon vu khi tu Assets vao files/items + weapons-icons.md. PYTHONIOENCODING=utf-8 python import_gen3.py"""
import sys, json, shutil, pathlib
sys.path.insert(0, r"D:\Lastaway\projects\lastaway\art\inbox\T-015\src\tools")
from extract_t015 import key_magenta
from PIL import Image
G = pathlib.Path(__file__).parent / "gen"; P = pathlib.Path(__file__).parent.parent; N = P / "files" / "new"; ART = pathlib.Path(r"D:\Lastaway\Assets\_JungleMaze\Art"); meta = json.load(open(N / "meta.json", encoding="utf-8"))
def prep(src):
    a = key_magenta(Image.open(G / f"{src}.png")); a = a.crop(a.getchannel("A").getbbox()); s = max(a.size); o = Image.new("RGBA", (s, s), (0, 0, 0, 0)); o.alpha_composite(a, ((s - a.width) // 2, (s - a.height) // 2)); return o
for src, name, px, inner, note in (("practice", "kit_ico_practice", 48, 0.9, "icon mục tối cho nút Luyện tập offline (bia tập bắn có mũi tên)"), ("save", "kit_ico_save", 48, 0.9, "icon mục tối cho nút Lưu endpoint (đĩa mềm)"), ("globe", "kit_ico_globe", 48, 0.9, "icon mục tối cho nút Xem online (quả địa cầu)"),
                                   ("fill", "kit_ico_fill", 48, 0.9, "icon mục tối cho nút Điền đủ (nhóm 3 người và dấu cộng)"), ("zonecause", "icon_zone_cause", 64, 1.0, "icon màu nguyên nhân chết Bo thu v2 (khói bo đỏ lấn vào nền đá, nhân vật nhỏ ở ngoài vòng; lấp khoảng 90% ô)")):
    sq = prep(src)
    for suf, k in (("", 1), ("@2x", 2)):
        c = px * k; t = round(c * inner); ic = sq.resize((t, t), Image.LANCZOS); o = Image.new("RGBA", (c, c), (0, 0, 0, 0)); o.alpha_composite(ic, ((c - t) // 2, (c - t) // 2)); o.save(N / f"{name}{suf}.png")
    meta[name] = {"size": [px, px], "border_LRTB": None, "slice": False, "nguon": f"gen GPT qua codex-img.sh, prompt `src/gen/{src}.prompt.src.txt`; {note}"}
json.dump(meta, open(N / "meta.json", "w", encoding="utf-8"), ensure_ascii=False, indent=1)
W = [("Tay không", "HAND"), ("Cành cây", "TREE_BRANCH"), ("Dao", "KNIFE"), ("Xương", "BONE"), ("Rìu", "AXE"), ("Chuỳ gai", "MACE"), ("Ná cao su", "RUBBER_GUN"), ("Quyền trượng", "SCEPTER"), ("Nỏ", "CROSSBOW"), ("Shotgun", "SHOTGUN"),
     ("Chuối", "BANANA"), ("Bom khói", "SMOKE"), ("Mảnh thiên thạch", "METEORITE_FRAGMENT"), ("Tinh thể", "CRYSTAL"), ("Hạt", "SEED"), ("Dây thừng", "ROPE"), ("Chuông", "BELL"), ("Gậy đánh bóng", "SAHUR_BAT")]
(P / "files" / "items").mkdir(exist_ok=True)
for _, f in W: shutil.copy2(ART / f"UI/Icons/Weapons/{f}.png", P / "files" / "items" / f"{f}.png")
md = ["# Icon vũ khí cho mục tiêu spectator (T-028 đợt 3)", "", "18 vũ khí trong GDD ITEM LIST (loại Weapon, dòng 1 đến 19 trừ Đạn) đều có icon sẵn trong `Assets/_JungleMaze/Art/UI/Icons/Weapons/` (64x64), nên **không gen thêm**. Bản sao ở `files/items/`. Icon Đạn dùng chung: `files/new/icon_ammo.png`.", "", "Cỡ gợi ý trong thẻ mục tiêu spectator: 44x44 cạnh tên; trong kill feed: 32x32; trong ô túi đồ: 104x104 (xem các bảng màn).", "", "| STT GDD | Tên vũ khí | Icon trong Assets | Bản sao |", "|---:|---|---|---|"]
stt = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 12, 13, 14, 15, 16, 17, 18, 19]
for (vn, f), i in zip(W, stt): md.append(f"| {i} | {vn} | `Assets/_JungleMaze/Art/UI/Icons/Weapons/{f}.png` | `files/items/{f}.png` |")
(P / "weapons-icons.md").write_text("\n".join(md), encoding="utf-8"); print("ok", len(W))
