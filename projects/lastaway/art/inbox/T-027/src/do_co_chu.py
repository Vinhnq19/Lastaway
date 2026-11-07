"""T-027 cong may co chu. Do co chu o mock dot A, dot B (a/contrast.json, b/contrast.json, do tu pixel luc ve) va Unity prefab.
Chay: PYTHONIOENCODING=utf-8 python do_co_chu.py [--gate] [--unity]
  --gate  : thoat 1 neu mock co chu pham luat; --unity: tinh ca Unity prefab vao ket qua gate.
Luat (thang 5 bac, chot 07/11): co phai thuoc {20, 24, 30, 40, 96}; san tuyet doi 20; man gameplay (b3, b4, b5, b9) khong duoi 24,
  rieng so phim va so tren o (chu so, x2, mot chu cai, Tab) duoc 20; font dung tang: tu 40 tro len (khong phai nut) = Chakra Petch Bold, con lai va MOI chu nut = Be Vietnam Pro.
Unity: m_fontSize x 1,5 (CanvasScaler ref 1280x720 -> 1080p). Ra: ../hien-trang.md, ../hien-trang.json, ../cong-co-chu.md"""
import json, re, sys, pathlib, collections
from PIL import ImageFont
ROOT = pathlib.Path(r"D:\Lastaway"); IN = ROOT / "projects/lastaway/art/inbox"; OUT = IN / "T-027"
FONT = ROOT / "Assets/_JungleMaze/Art/Fonts/BeVietnamPro-Bold.ttf"
LAD = [20, 24, 30, 40, 96]; SAN = 20; SAN_GAME = 24; MAIN_MIN = 40; SCALE = 1.5
EXC = re.compile(r"^(×?\d+|x\d+|[A-Za-z]|Tab|ESC|WASD)$")   # so phim, so tren o, nhan phim
GAME = ("b3", "b4", "b5", "b9", "hud")
def cap_ratio():
    f = ImageFont.truetype(str(FONT), 1000); b = f.getbbox("H"); asc = f.getbbox("Ắ"); xb = f.getbbox("x"); return (b[3] - b[1]) / 1000, (xb[3] - xb[1]) / 1000, (b[1] - asc[1]) / 1000

def load_mock(setname):
    rows = json.load(open(IN / "T-021" / setname / "contrast.json", encoding="utf-8"))
    return [{"nguon": setname.upper(), "man": r["mock"], "text": r["text"], "px": float(r["size"]), "font": r.get("font"), "btn": r.get("btn", False), "game": r.get("game", r["mock"].split("-")[0] in GAME)} for r in rows]
def load_unity():
    out = []
    for p in (ROOT / "Assets/_JungleMaze/Prefabs/UI").rglob("*.prefab"):
        txt = p.read_text(encoding="utf-8", errors="ignore")
        for blk in re.split(r"\n--- !u!", txt):
            m = re.search(r"m_text: (.*)", blk); s = re.search(r"m_fontSize: ([0-9.]+)", blk)
            if not (m and s): continue
            t = m.group(1).strip().strip('"'); t = re.sub(r"\\(?:x([0-9A-Fa-f]{2})|u([0-9A-Fa-f]{4}))", lambda mm: chr(int(mm.group(1) or mm.group(2), 16)), t)
            out.append({"nguon": "Unity", "man": p.stem, "text": t or "(chu dong)", "px": round(float(s.group(1)) * SCALE, 1), "font": None, "btn": False, "game": p.stem.startswith(("Hud", "MatchOverlay", "SpectatorRow"))})
    return out
def check(r):
    """tra ve danh sach loi cua mot dong chu"""
    e = []; px = r["px"]
    if px not in LAD: e.append("ngoai thang")
    if px < SAN: e.append("duoi san 20")
    elif r["game"] and px < SAN_GAME and not EXC.match(r["text"].strip()): e.append("gameplay duoi 24")
    if r["font"]:
        want = "Main" if (px >= MAIN_MIN and not r["btn"]) else "BVP"
        if r["font"] != want: e.append(f"sai font ({r['font']}, can {want})")
    return e

def main():
    cap, xh, dau = cap_ratio(); mocks = load_mock("a") + load_mock("b"); uni = load_unity(); rows = mocks + uni
    by = collections.defaultdict(list)
    for r in rows: by[r["px"]].append(r)
    tab = []
    for px in sorted(by):
        rs = by[px]; man = collections.Counter(f'{r["nguon"]}:{r["man"]}' for r in rs); ex = []
        for r in rs:
            if r["text"] not in ex and len(ex) < 3: ex.append(r["text"][:22])
        tab.append({"px": px, "cap_px": round(px * cap, 1), "so_lan": len(rs), "A": sum(r["nguon"] == "A" for r in rs), "B": sum(r["nguon"] == "B" for r in rs), "Unity": sum(r["nguon"] == "Unity" for r in rs), "so_man": len(man), "dung_o": ", ".join(k for k, _ in man.most_common(3)), "mau": " | ".join(ex), "thuoc_thang": px in LAD})
    (OUT).mkdir(exist_ok=True, parents=True); (OUT / "hien-trang.json").write_text(json.dumps({"rows": tab, "cap_ratio": cap}, ensure_ascii=False, indent=0), encoding="utf-8")
    md = ["# T-027: hiện trạng cỡ chữ (đo bằng `src/do_co_chu.py`)", "", f"Nguồn: mock A ({len(load_mock('a'))} dòng), mock B ({len(load_mock('b'))}), Unity prefab ({len(uni)} thành phần, m_fontSize x 1,5). Chiều cao chữ hoa = {cap:.3f} em; thân chữ d đến g = 0,972 em.", "",
          f"Số cỡ khác nhau: **{len(tab)}**, trong đó thuộc thang {{20, 24, 30, 40, 96}}: {sum(t['thuoc_thang'] for t in tab)}.", "", "| Cỡ (px, 1080p) | Cao chữ hoa | Số lần | A | B | Unity | Số màn | Dùng nhiều ở | Mẫu | Thuộc thang |", "|---:|---:|---:|---:|---:|---:|---:|---|---|---|"]
    for t in tab: md.append(f'| {t["px"]:g} | {t["cap_px"]} | {t["so_lan"]} | {t["A"]} | {t["B"]} | {t["Unity"]} | {t["so_man"]} | {t["dung_o"]} | {t["mau"]} | {"có" if t["thuoc_thang"] else "không"} |')
    (OUT / "hien-trang.md").write_text("\n".join(md), encoding="utf-8")
    res = {}
    for nm, rs in (("A", [r for r in mocks if r["nguon"] == "A"]), ("B", [r for r in mocks if r["nguon"] == "B"]), ("Unity", uni)):
        bad = [(r, check(r)) for r in rs if check(r)]; res[nm] = {"dong": len(rs), "loi": len(bad), "chi_tiet": collections.Counter(e for _, es in bad for e in es), "mau": [(r["man"], r["text"][:24], r["px"], es) for r, es in bad[:8]]}
    g = ["# T-027 cổng cỡ chữ (src/do_co_chu.py --gate)", "", "Luật: cỡ thuộc {20, 24, 30, 40, 96}; sàn 20; gameplay không dưới 24 (số phím và số trên ô 20); font đúng tầng (từ 40 trở lên, không phải nút = Chakra Petch Bold; còn lại và mọi chữ nút = Be Vietnam Pro Bold).", "", "| Nguồn | Dòng chữ | Dòng lỗi | Loại lỗi |", "|---|---:|---:|---|"]
    for nm, v in res.items(): g.append(f'| {nm} | {v["dong"]} | {v["loi"]} | {", ".join(f"{k}: {n}" for k, n in v["chi_tiet"].items()) or "không"} |')
    for nm, v in res.items():
        if v["mau"]: g += ["", f"Mẫu lỗi {nm}:", ""] + [f"- {a} | {b} | {c} px | {', '.join(d)}" for a, b, c, d in v["mau"]]
    (OUT / "cong-co-chu.md").write_text("\n".join(g), encoding="utf-8")
    print({nm: (v["dong"], v["loi"], dict(v["chi_tiet"])) for nm, v in res.items()}, "so co", len(tab))
    fail = res["A"]["loi"] + res["B"]["loi"] + (res["Unity"]["loi"] if "--unity" in sys.argv else 0)
    return 1 if (fail and "--gate" in sys.argv) else 0
if __name__ == "__main__": sys.exit(main())
