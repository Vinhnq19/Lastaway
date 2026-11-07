"""T-027 loc font trong repo: du dau tieng Viet (cmap) + giay phep thuong mai (bang name + file license canh font).
Chay: PYTHONIOENCODING=utf-8 python loc_font.py    (ra ../font-loc.md, ../font-loc.json)
Font lay tu `git ls-files` (chi font co trong repo). Dat/khong dat: DAU = du moi chu cai co dau (hoa va thuong) + Đđ; LICENSE = co giay phep cho phep dung thuong mai ghi trong font hoac file canh font."""
import subprocess, json, pathlib, re
from fontTools.ttLib import TTFont
ROOT = pathlib.Path(r"D:\Lastaway"); OUT = ROOT / "projects/lastaway/art/inbox/T-027"
V = "aàáảãạ ăằắẳẵặ âầấẩẫậ eèéẻẽẹ êềếểễệ iìíỉĩị oòóỏõọ ôồốổỗộ ơờớởỡợ uùúủũụ ưừứửữự yỳýỷỹỵ".replace(" ", "")
VN = sorted(set(V + V.upper() + "đĐ" + "ĂÂÊÔƠƯ".lower() + "ĂÂÊÔƠƯ"))
files = [f for f in subprocess.run(["git", "ls-files"], cwd=ROOT, capture_output=True, text=True, encoding="utf-8").stdout.splitlines() if re.search(r"\.(ttf|otf)$", f, re.I) and "Imported" not in f]
def name(t, i):
    for n in t["name"].names:
        if n.nameID == i:
            try: return n.toUnicode().replace("\r", " ").replace("\n", " ")[:260]
            except Exception: pass
    return ""
rows = []
for f in files:
    p = ROOT / f; t = TTFont(str(p), fontNumber=0, lazy=True); cm = t.getBestCmap(); miss = [c for c in VN if ord(c) not in cm]
    lic_dir = [x.name for x in p.parent.iterdir() if re.search(r"(ofl|licen[sc]e|readme|copying)", x.name, re.I) and not x.name.endswith(".meta")]
    lic_txt = ""
    for x in p.parent.iterdir():
        if re.search(r"(ofl|licen[sc]e)", x.name, re.I) and x.suffix.lower() in (".txt", ".md", ""): lic_txt = x.read_text(encoding="utf-8", errors="ignore")[:200].replace("\n", " ")
    rows.append({"file": f, "em": t["head"].unitsPerEm, "glyphs": len(t.getGlyphOrder()), "thieu_dau": "".join(miss), "n_thieu": len(miss), "tong_can": len(VN), "copyright": name(t, 0), "license_nhung": name(t, 13), "license_url": name(t, 14), "nsx": name(t, 8), "file_license_canh": lic_dir, "dau": "đủ" if not miss else f"thiếu {len(miss)}/{len(VN)}"})
(OUT / "font-loc.json").write_text(json.dumps(rows, ensure_ascii=False, indent=1), encoding="utf-8")
md = ["# T-027 lọc font trong repo (src/loc_font.py)", "", f"Tập chữ kiểm: {len(VN)} ký tự (mọi nguyên âm có dấu hoa và thường, ĂÂÊÔƠƯ, Đđ).", "", "| Font | Dấu tiếng Việt | Bản quyền / giấy phép ghi trong font | File license cạnh font |", "|---|---|---|---|"]
for r in rows: md.append(f'| {r["file"].split("Fonts/")[-1]} | {r["dau"]} | {(r["copyright"] + " | " + r["license_nhung"] + " " + r["license_url"])[:230]} | {", ".join(r["file_license_canh"]) or "không có"} |')
(OUT / "font-loc.md").write_text("\n".join(md), encoding="utf-8")
for r in rows: print(r["file"].split("Fonts/")[-1], "|", r["dau"], "|", r["copyright"][:70], "|", r["license_nhung"][:80], "|", r["file_license_canh"])
