"""Cong can giua thi giac cho nut (T-026). Chay: PYTHONIOENCODING=utf-8 python check_center.py
Do tren PIXEL that: ve nut mot minh (mock.button) len nen phang, so voi ban chi co sprite de tach muc.
- muc chu: chi tinh dai hang day (hang co >= 20% so pixel cua hang dac nhat), de dau tieng Viet va net xuong (g, p, y) khong keo lech.
- muc icon: hop bao pixel cua icon.
- tam muc = tam hop bao hop nhat (dai chu + icon); tam MAT nut = (dinh sprite + (day sprite - de)) / 2, de = BASE_FRAC[state] * cao.
Lech > 1 px (theo truc doc) la FAIL. Chay tren: ho nut (S 48, M 64, L 80 x 4 trang thai x cac chu) va toan bo nut da ve trong mock A (ve lai tung nut rieng).
Ra: a/center.md va a/center.json."""
import sys, pathlib, json
sys.path.insert(0, str(pathlib.Path(__file__).parent))
import mock, thang
from mock import *
from PIL import Image, ImageChops
import numpy as np
TOL = 1.0
SIZES = [("S", 48, 22), ("M", 64, 26), ("L", 80, 30)]
STATES = ["normal", "hover", "pressed", "disabled"]
KINDS = ["primary", "secondary", "danger"]

def draw(kind, state, w, h, font, caption, icon, PAD=30):
    im = Image.new("RGBA", (w + 2 * PAD, h + 2 * PAD), (60, 60, 60, 255)); n = len(mock.LOGB)
    mock.button(im, kind, (PAD, PAD), caption, None, WHITE, (w, h), state, font, icon if icon else ""); del mock.LOGB[n:]; return im

def ink_mask(a, b):
    d = np.asarray(ImageChops.difference(a.convert("RGB"), b.convert("RGB"))).sum(axis=2); return d > 30

def measure(kind, state, w, h, font, caption, icon):
    PAD = 30; base = draw(kind, state, w, h, font, "", None); full = draw(kind, state, w, h, font, caption, icon); ico = draw(kind, state, w, h, font, "", icon)
    m_all = ink_mask(base, full); m_ico = ink_mask(base, ico) if icon else np.zeros_like(m_all); m_txt = m_all & ~m_ico if caption else np.zeros_like(m_all)
    rows = []; spans = []
    if m_txt.any():
        cnt = m_txt.sum(axis=1); thr = 0.2 * cnt.max(); ys = np.where(cnt >= thr)[0]; spans.append((ys.min(), ys.max() + 1))
    if m_ico.any():
        ys = np.where(m_ico.any(axis=1))[0]; spans.append((ys.min(), ys.max() + 1))
    if not spans: return None
    top = min(s[0] for s in spans); bot = max(s[1] for s in spans); ink_c = (top + bot) / 2.0 - PAD
    a = kbtn(kind, state, w, h).getchannel("A").getbbox(); face_c = (a[1] + a[3] - h * BASE_FRAC[state]) / 2.0
    return round(ink_c - face_c, 2), (top - PAD, bot - PAD), round(face_c, 2)

def main():
    rows = []; fails = 0
    for sn, h, font in SIZES:
        for kind in KINDS:
            for st in STATES:
                for cap, icon in (("Vào phòng", None), ("Vào phòng", "play"), ("Hủy", "@cross_ink"), ("Thoát trận", "@cross_red"), ("Xóa phòng", "trash"), ("Sẵn sàng", "check"), ("Đăng nhập", "key")):
                    w = 240 if len(cap) < 8 else 320
                    r = measure(kind, st, w, h, font, cap, icon); ok = bool(abs(r[0]) <= TOL); fails += (not ok)
                    rows.append({"nguon": "ho-nut", "cỡ": sn, "loai": kind, "trang_thai": st, "chu": cap, "icon": icon or "-", "lech_px": float(r[0]), "ok": ok})
    # nut that trong mock A (ve lai tung nut rieng, cung ham button)
    SET = sys.argv[1] if len(sys.argv) > 1 else "a"
    SA = __import__("screens_b" if SET == "b" else "screens_a")
    seen = set(); mrows = []
    for n, f in SA.ALL.items(): mock.CUR[0] = n; f()
    logged = list(mock.LOGB); mock.LOGB.clear(); wrong = [b for b in logged if b["font"] != thang.btn_size(b["h"], b["font"])]  # chu nut phai dung bac cua co nut
    for b in logged:
        key = (b["kind"], b["state"], b["w"], b["h"], b["font"], b["caption"], b["icon"])
        if key in seen: continue
        seen.add(key); r = measure(b["kind"], b["state"], b["w"], b["h"], b["font"], b["caption"], ("@" + b["icon"][1:]) if b["icon"] and b["icon"].startswith("@") else b["icon"])
        if r is None: continue
        ok = bool(abs(r[0]) <= TOL); fails += (not ok); mrows.append({"nguon": "mock " + b["mock"], "cỡ": f'{b["w"]}x{b["h"]}', "loai": b["kind"], "trang_thai": b["state"], "chu": b["caption"], "icon": b["icon"] or "-", "lech_px": float(r[0]), "ok": ok})
    nwrong = len(wrong); allr = rows + mrows; bad = [r for r in allr if not r["ok"]]
    out = pathlib.Path(__file__).resolve().parent.parent.parent / SET; (out / "center.json").write_text(json.dumps(allr, ensure_ascii=False, indent=0), encoding="utf-8")
    md = ["# Cổng căn giữa thị giác của nút (T-021, T-026)", "", "Đo trên pixel thật: tâm hộp mực (dải hàng đậm của chữ + hộp icon) so với tâm MẶT nút (không tính đế), ngưỡng ±1 px dọc.", "",
          f"| Nguồn | Số ca | FAIL | Lệch lớn nhất |", "|---|---:|---:|---:|", f"| họ nút (S 48, M 64, L 80 × 3 loại × 4 trạng thái × 7 chữ/icon) | {len(rows)} | {sum(not r['ok'] for r in rows)} | {max(abs(r['lech_px']) for r in rows):.2f} |",
          f"| nút trong mock (không trùng) | {len(mrows)} | {sum(not r['ok'] for r in mrows)} | {max(abs(r['lech_px']) for r in mrows):.2f} |", "", f"Chữ nút đúng bậc của cỡ nút (S 48 = 24, M 64 = 30, L 80 = 40): **{len(logged) - nwrong}/{len(logged)}** lần vẽ đúng, {nwrong} sai.", ""]
    if bad:
        md += ["## Ca FAIL", "", "| Nguồn | Cỡ | Loại | Trạng thái | Chữ | Icon | Lệch |", "|---|---|---|---|---|---|---:|"]
        for r in sorted(bad, key=lambda r: -abs(r["lech_px"]))[:80]: md.append(f"| {r['nguon']} | {r['cỡ']} | {r['loai']} | {r['trang_thai']} | {r['chu']} | {r['icon']} | {r['lech_px']} |")
    (out / "center.md").write_text("\n".join(md), encoding="utf-8"); print("ca", len(allr), "FAIL", len(bad), "max", max(abs(r['lech_px']) for r in allr), "nut sai bac", nwrong, "/", len(logged))
    return 1 if (bad or nwrong) else 0
if __name__ == "__main__": sys.exit(main())
