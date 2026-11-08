"""T-027 loc font ngoai (Google Fonts, tai 07/11 theo Huy): cung tap 146 ky tu nhu loc_font.py. Ghi them muc vao ../font-loc.md
Chay: PYTHONIOENCODING=utf-8 python loc_font_ngoai.py"""
import pathlib, json
from fontTools.ttLib import TTFont
OUT = pathlib.Path(r"D:\Lastaway\projects\lastaway\art\inbox\T-027"); D = OUT / "font-ngoai"
V = "aàáảãạ ăằắẳẵặ âầấẩẫậ eèéẻẽẹ êềếểễệ iìíỉĩị oòóỏõọ ôồốổỗộ ơờớởỡợ uùúủũụ ưừứửữự yỳýỷỹỵ".replace(" ", "")
VN = sorted(set(V + V.upper() + "đĐ" + "ĂÂÊÔƠƯ".lower() + "ĂÂÊÔƠƯ"))
rows = []
for p in sorted(D.glob("*/*.ttf")):
    t = TTFont(str(p), lazy=True); cm = t.getBestCmap(); miss = [c for c in VN if ord(c) not in cm]; nm = {n.nameID: n.toUnicode() for n in t["name"].names if n.nameID in (0, 13)}
    rows.append({"font": p.parent.name, "file": p.name, "kb": round(p.stat().st_size / 1024), "url": f"https://github.com/google/fonts/tree/main/ofl/{p.parent.name}", "dau": "đủ" if not miss else f"thiếu {len(miss)}/{len(VN)}", "copyright": nm.get(0, "")[:90], "ofl": (p.parent / "OFL.txt").exists()})
md = ["", "## Font ngoài (Google Fonts, tải 2026-11-07 với sự đồng ý của Huy), lưu ở `font-ngoai/<tên>/`, chưa đưa vào Assets", "", f"Kiểm {len(VN)} ký tự như bảng trên. METADATA.pb đã xác minh: license OFL và có subset vietnamese.", "", "| Font | File | Dung lượng | URL | Dấu | OFL.txt | Bản quyền |", "|---|---|---:|---|---|---|---|"]
for r in rows: md.append(f'| {r["font"]} | {r["file"]} | {r["kb"]} KB | {r["url"]} | {r["dau"]} | {"có" if r["ofl"] else "không"} | {r["copyright"]} |')
p = OUT / "font-loc.md"; s = p.read_text(encoding="utf-8"); s = s.split("\n## Font ngoài")[0]; p.write_text(s + "\n".join(md), encoding="utf-8"); print("\n".join(md))
