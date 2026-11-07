import sys, pathlib
from collections import Counter
from PIL import Image
sys.stdout.reconfigure(encoding="utf-8")
ROOT = pathlib.Path(r"D:\Lastaway\projects\lastaway\art\inbox\T-015"); ICON = pathlib.Path(r"D:\Lastaway\projects\lastaway\art\inbox\hud-icons\png")
lin = lambda c: (c / 255) / 12.92 if c / 255 <= .03928 else ((c / 255 + .055) / 1.055) ** 2.4
L = lambda p: .2126 * lin(p[0]) + .7152 * lin(p[1]) + .0722 * lin(p[2])
cr = lambda a, b: (max(a, b) + .05) / (min(a, b) + .05)
hx = lambda h: tuple(int(h[i:i + 2], 16) for i in (1, 3, 5))
def slab(name, boxes):
    im = Image.open(ROOT / f"{name}.png").convert("RGBA"); v = []
    for b in boxes: v += [L(p) for p in im.crop(b).getdata() if p[3] > 250]
    v.sort(); return v[int(len(v) * .95)], v[int(len(v) * .99)], v[len(v) // 2]
QK = "Quick bar (khoảng giữa hai nhóm ô)"
S = {"Vitals dải trên (HP)": slab("hud_panel_vitals", [(100, 32, 380, 66)]), "Vitals dải giữa (giáp, mũ)": slab("hud_panel_vitals", [(100, 78, 380, 126)]), "Vitals dải dưới (Adrenaline)": slab("hud_panel_vitals", [(100, 138, 380, 178)]),
     QK: slab("hud_panel_quickbar", [(288, 40, 306, 80)]),
     "Ô trống": slab("hud_slot_empty", [(14, 14, 40, 40)]),
     "Ô chọn": slab("hud_slot_selected", [(14, 14, 40, 40)]),
     "Match status": slab("hud_panel_match", [(100, 32, 260, 58)])}
out = ["# Tương phản T-015 lượt 5 (sprite GPT, cỡ và vị trí đo từ concept, Huy 07/11)", "",
       "Nền = điểm sáng p95 và p99 của mặt đá trong lòng panel (vùng không có vạch chia hay ô khắc góc). Lòng đá là bản GPT vẽ, không làm tối thêm. Ngưỡng: chữ thường và icon 4,5:1; số lớn và số đồng hồ bo 3:1.", "",
       "| Khối | Nền median / p95 / p99 | Mục | Màu | cr ở p99 | Ngưỡng | Kết quả |", "|---|---|---|---|---:|---:|---|"]
def row(k, lab, c, th):
    p95, p99, med = S[k]; x = cr(L(c), p99); out.append(f"| {k} | {med:.3f} / {p95:.3f} / {p99:.3f} | {lab} | `{'%02X%02X%02X' % c}` | {x:.2f} | {th} | {'Đạt' if x >= th else 'KHÔNG ĐẠT'} |")
row("Vitals dải trên (HP)", "số HP", hx("#EDF2EE"), 4.5); row("Vitals dải giữa (giáp, mũ)", "số giáp, mũ", hx("#EDF2EE"), 4.5); row("Vitals dải dưới (Adrenaline)", "số Adrenaline", hx("#EDF2EE"), 4.5)
row(QK, "nền thân thanh", (255, 220, 110), 4.5); row("Ô trống", "số phím trong góc ô (vàng, thêm viền tối 3 px)", (255, 220, 110), 4.5); row("Ô chọn", "số phím ô chọn", (255, 220, 110), 4.5)
row("Match status", "số 1/7", hx("#EDF2EE"), 4.5); row("Match status", "số đồng hồ bo kem", hx("#F1E7C8"), 3.0)
for n, k in (("hp", "Vitals dải trên (HP)"), ("armor", "Vitals dải giữa (giáp, mũ)"), ("helmet", "Vitals dải giữa (giáp, mũ)"), ("adrenaline", "Vitals dải dưới (Adrenaline)"), ("alive", "Match status"), ("zone-timer", "Match status")):
    im = Image.open(ICON / f"icon_{n}.png").convert("RGBA"); px = [p[:3] for p in im.getdata() if p[3] > 250 and L(p) > .06]
    q = Counter((p[0] // 32, p[1] // 32, p[2] // 32) for p in px); tot = len(px); p99 = S[k][1]
    for key, v in q.most_common(4):
        if v / tot < .10: continue
        cols = [p for p in px if (p[0] // 32, p[1] // 32, p[2] // 32) == key]; c = tuple(sum(p[i] for p in cols) // len(cols) for i in range(3))
        x = cr(L(c), p99); out.append(f"| {k} | {S[k][2]:.3f} / {S[k][0]:.3f} / {S[k][1]:.3f} | icon {n}, màu chiếm {100 * v / tot:.0f}% | `{'%02X%02X%02X' % c}` | {x:.2f} | 4,5 | {'Đạt' if x >= 4.5 else 'KHÔNG ĐẠT'} |")
open(ROOT / "contrast.md", "w", encoding="utf-8").write("\n".join(out) + "\n"); print("\n".join(out))
