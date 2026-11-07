"""T-028: gom file vao ../files/ (kit, fx, items, bg, new) va viet bang-tong.md, README.md. Chay sau map_screens.py.
PYTHONIOENCODING=utf-8 python build_pack.py"""
import json, pathlib, shutil, collections
from PIL import Image
IN = pathlib.Path(r"D:\Lastaway\projects\lastaway\art\inbox"); OUT = IN / "T-028-unity-pack"; T20 = IN / "T-020"; ART = pathlib.Path(r"D:\Lastaway\Assets\_JungleMaze\Art")
used = json.load(open(OUT / "dung-o-man.json", encoding="utf-8")); meta = json.load(open(T20 / "meta.json", encoding="utf-8"))
F = OUT / "files"; [(F / d).mkdir(parents=True, exist_ok=True) for d in ("kit", "fx", "items", "bg", "new")]
kit = sorted(k for k in used if not k.startswith(("fx:", "item:"))); fx = sorted(k[3:] for k in used if k.startswith("fx:")); items = sorted(k[5:] for k in used if k.startswith("item:"))
# nut: dam bao du 4 trang thai cho moi loai nut da dung
for kd in ("primary", "secondary", "danger"):
    if any(k.startswith(f"kit_btn_{kd}_") for k in kit):
        for st in ("normal", "hover", "pressed", "disabled"): kit.append(f"kit_btn_{kd}_{st}")
nmeta = json.load(open(F / "new" / "meta.json", encoding="utf-8")); kit = sorted(n for n in set(kit) if n not in nmeta); km = {}
for n in kit:
    for suf in ("", "@2x"):
        p = T20 / f"{n}{suf}.png"
        if p.exists(): shutil.copy2(p, F / "kit" / p.name)
    km[n] = meta.get(n, {"size": None, "border_LRTB": None})
json.dump(km, open(F / "kit" / "meta.json", "w", encoding="utf-8"), ensure_ascii=False, indent=1)
for n in fx: shutil.copy2(IN / "T-021/b/assets" / f"{n}.png", F / "fx" / f"{n}.png")
for n in items: shutil.copy2(ART / f"{n}.png", F / "items" / (pathlib.Path(n).name + ".png"))
g = Image.open(IN / "T-015/dung-thu-hud-A.png").convert("RGB"); g.crop((640, 120, 1150, 400)).save(F / "bg" / "nen-gach-tile.png")
rows = ["# T-028 bảng tổng file trong `files/`", "", "Toạ độ và cỡ trong các bảng màn là **1080p (1920x1080)**. Unity (CanvasScaler tham chiếu 1280x720, match width) quy đổi chia 1,5. Border 9-slice là LRTB của sprite @1x (px ảnh gốc); sprite @2x có border gấp đôi.", "",
        "## Kit T-020 và T-026 (`files/kit/`, meta ở `files/kit/meta.json`)", "", "| File | Cỡ gốc @1x | Border LRTB | @2x | Dùng ở màn |", "|---|---:|---|---|---|"]
for n in kit:
    m = km[n]; rows.append(f"| `{n}.png` | {('x'.join(map(str, m['size'])) if m['size'] else '-')} | {m['border_LRTB'] or 'không'} | {'có' if (F / 'kit' / (n + '@2x.png')).exists() else 'không'} | {', '.join(used.get(n, [])[:6]) + (' ...' if len(used.get(n, [])) > 6 else '') or '-'} |")
rows += ["", "## FX và emblem (`files/fx/`, gen GPT nét cũ, Huy giữ)", "", "| File | Cỡ gốc | Dùng ở màn |", "|---|---:|---|"]
for n in fx: im = Image.open(F / "fx" / f"{n}.png"); rows.append(f"| `{n}.png` | {im.width}x{im.height} | {', '.join(used['fx:' + n])} |")
rows += ["", "## Icon vật phẩm (`files/items/`, bản sao từ `Assets/_JungleMaze/Art/`, đường dẫn gốc ghi cạnh)", "", "| File | Nguồn trong Assets | Dùng ở màn |", "|---|---|---|"]
for n in items: rows.append(f"| `{pathlib.Path(n).name}.png` | `Assets/_JungleMaze/Art/{n}.png` | {', '.join(used['item:' + n][:6])}{' ...' if len(used['item:' + n]) > 6 else ''} |")
rows += ["", "## Nền (`files/bg/`)", "", "| File | Ghi chú |", "|---|---|", "| `nen-gach-tile.png` (510x280) | ô nền gạch lát kín màn 1920x1080 (cắt từ ảnh HUD concept T-015); phủ tối alpha 0,62 đến 0,7 và vignette gradient chạy lúc render |"]
nm = json.load(open(F / "new" / "meta.json", encoding="utf-8"))
rows += ["", "## Asset gen mới T-028 (`files/new/`, qua cổng duyệt của CPO)", "", "| File | Cỡ @1x | Border LRTB | @2x | Nguồn |", "|---|---:|---|---|---|"]
for n, m in nm.items(): rows.append(f"| `{n}.png` | {m['size'][0]}x{m['size'][1]} | {m['border_LRTB'] or 'không'} | có | {m['nguon']} |")
(OUT / "bang-tong.md").write_text("\n".join(rows), encoding="utf-8"); print(len(kit), len(fx), len(items))
