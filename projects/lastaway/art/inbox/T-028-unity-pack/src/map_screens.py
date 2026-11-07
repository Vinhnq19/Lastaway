"""T-028: xuat bang anh xa phan tu mock -> file PNG cho tung man T-021 (de Unity dung T-022..T-025).
Chay: PYTHONIOENCODING=utf-8 python map_screens.py   (ra ../<nhom>/<man>.md, ../bang-tong.md, ../files/*)
Cach lam: render lai moi mock that cua T-021 va ghi lai moi lan mock goi put / icon / ico / close_btn / itm / put_fx / logo / bg / hudbg
(khong sua mock). Nut lay tu mock.LOGB, chu lay tu mock.LOG. Sprite kit lay tu T-020 (kit_*.png va @2x, meta.json cho border 9-slice)."""
import sys, json, pathlib, shutil, collections
TOOLS = r"D:\Lastaway\projects\lastaway\art\inbox\T-021\src\tools"; sys.path.insert(0, TOOLS)
import mock, screens_a, screens_b, thang
from PIL import Image
IN = pathlib.Path(r"D:\Lastaway\projects\lastaway\art\inbox"); OUT = IN / "T-028-unity-pack"; T20 = IN / "T-020"; FXD = IN / "T-021/b/assets"; ART = pathlib.Path(r"D:\Lastaway\Assets\_JungleMaze\Art")
META = mock.meta; NM = json.load(open(OUT / "files" / "new" / "meta.json", encoding="utf-8")); R = []; inside = [0]
def rec(**k): R.append(k)
def wrap(mod, name, fn):
    if hasattr(mod, name): setattr(mod, name, fn)
o_put, o_spr, o_icon, o_ico, o_close = mock.put, mock.spr, mock.icon, mock.ico, mock.close_btn
def put(im, n, xy, size=None):
    inside[0] += 1
    try: o_put(im, n, xy, size)
    finally: inside[0] -= 1
    if inside[0] == 0: m = META[n]; rec(kind="sprite", file=n, size=list(size) if size else m["size"], border=m["border_LRTB"], xy=[int(xy[0]), int(xy[1])])
def spr(n, root=mock.T20):
    if inside[0] == 0 and root == mock.T20 and n in META: rec(kind="sprite_raw", file=n, size=META[n]["size"], border=META[n]["border_LRTB"])
    return o_spr(n, root)
def icon(im, n, cx, cy, h):
    inside[0] += 1
    try: o_icon(im, n, cx, cy, h)
    finally: inside[0] -= 1
    rec(kind="icon", file=n, size=[round(META[n]["size"][0] * h / META[n]["size"][1]), h], border=None, xy=[int(cx), int(cy)], note="icon ink tối, đặt trên mặt sáng" if n.startswith("kit_ico_") else "")
def ico(im, name, cx, cy, s=32, plate=True):
    inside[0] += 1
    try: o_ico(im, name, cx, cy, s, plate)
    finally: inside[0] -= 1
    if plate: pw = s + 20; rec(kind="sprite", file="kit_iconbtn_round_normal", size=[pw, pw], border=None, xy=[int(cx - pw / 2), int(cy - pw / 2)], note="nút tròn kaki làm nền icon")
    rec(kind="icon", file=f"kit_ico_{name}", size=[s, s], border=None, xy=[int(cx), int(cy)], note="icon ink tối (đặt lên nền kaki)")
def close_btn(im, x0, y0, w):
    inside[0] += 1
    try: o_close(im, x0, y0, w)
    finally: inside[0] -= 1
    rec(kind="sprite", file="kit_close_normal", size=[52, 52], border=META["kit_close_normal"]["border_LRTB"], xy=[x0 + w - 86, y0 + 29], note="nút đóng"); rec(kind="icon", file="kit_icon_cross_red", size=[26, 26], border=None, xy=[x0 + w - 60, y0 + 55], note="X đỏ #94281F trên nút đóng")
def itm(im, key, cx, cy, s):
    inside[0] += 1
    try: screens_b._itm(im, key, cx, cy, s)
    finally: inside[0] -= 1
    if key is None: rec(kind="sprite", file="kit_slot_item_empty", size=[s, s], border=None, xy=[int(cx), int(cy)], note="ô giữ chỗ trong mock; dùng `files/new/icon_ammo.png` (64x64, gen T-028) cho đạn")
    else: rec(kind="item_icon", file=screens_b.ITEMS[key], size=[s, s], border=None, xy=[int(cx), int(cy)], note=f"icon vật phẩm có sẵn trong Assets, khóa mock '{key}'")
def put_fx(im, n, cx, cy, h=None, w=None):
    inside[0] += 1
    try: screens_b._put_fx(im, n, cx, cy, h, w)
    finally: inside[0] -= 1
    s = Image.open(FXD / f"{n}.png"); sz = [round(s.width * h / s.height), h] if h else [w, round(s.height * w / s.width)]; rec(kind="fx", file=n, size=sz, border=None, xy=[int(cx), int(cy)], note="FX/emblem gen GPT (nét cũ, Huy giữ)")
def logo(im, cx, y, w=700): o_logo(im, cx, y, w); rec(kind="logo", file="Assets/_JungleMaze/Art/UI/Menu/Logo.png (crop 76,68,952,184)", size=[w, round(116 * w / 876)], border=None, xy=[int(cx), y], note="logo pixel LASTAWAY, đã có trong Assets")
def bg(dim=0.62, vignette=True): rec(kind="bg", file="files/bg/nen-gach-tile.png", size=[510, 280], border=None, xy=[0, 0], note=f"ô nền gạch lát 510x280 lát kín 1920x1080, phủ tối alpha {dim}" + (" + vignette (gradient chạy lúc render)" if vignette else "")); return o_bg(dim, vignette)
def code(note, size, color=None): rec(kind="code", file=note, size=size, border=None, xy=[0, 0], note=color or "")
def newspr(f, size, note, border=False): rec(kind="sprite", file=f, size=size, border=NM[f]["border_LRTB"], xy=[0, 0], note=note, src="new")
def overlay(im, a): rec(kind="sprite", file="kit_dim", size=[1920, 1080], border=None, xy=[0, 0], note=f"lớp phủ tối toàn màn: kéo giãn full màn, tint alpha {a}/255 (màu gốc #0A0A08)"); o_overlay(im, a)
def dim(im, a): rec(kind="sprite", file="kit_dim", size=[1920, 1080], border=None, xy=[0, 0], note=f"lớp phủ tối toàn màn: kéo giãn full màn, tint alpha {a}/255 (màu gốc #0A0A08)"); o_dim(im, a)
def bigmap(state="main"):
    newspr("kit_map_bg", [840, 840], "nền khung bản đồ lớn (bản đồ thật vẽ lúc chạy trên nền này)"); newspr("map_grid", [840, 840], "lưới 10x10 phủ lên bản đồ, nền trong suốt (giữ chỗ trong mock)")
    newspr("map_ring_zone", [680, 680], "vòng bo hiện tại, tâm (530,530)"); newspr("map_ring_safe", [180, 180], "vòng bo tiếp theo (an toàn), tâm (570,480)"); newspr("map_ring_safe", [32, 32], "vòng trắng chú giải (cùng sprite, cỡ nhỏ)")
    return o_bigmap(state)
def hudbg(a=0): newspr("kit_killfeed_panel", [270, 154], "panel kill feed, rộng thêm theo chữ (tên + icon vũ khí 32 + tên, 24 px, dòng cách 44), alpha ~0,98"); [newspr("kit_badge_key", [30, 30], f"nhãn số phím quick bar ({i + 1}), số 20 px vàng #FFDC6E đè lên") for i in range(8)]; rec(kind="hudbg", file="HUD trong trận (Hud.prefab T-016)", size=[1920, 1080], border=None, xy=[0, 0], note="nền HUD đã có trong Unity (T-016); mock chỉ dựng lại để thấy lớp phủ" + (f"; phủ tối alpha {a}" if a else "")); return screens_b._hudbg(a)
o_header, o_button = mock.header, mock.button
def header(im, title, right=None, back=True):
    inside[0] += 1
    try: o_header(im, title, right, back)
    finally: inside[0] -= 1
    rec(kind="sprite", file="kit_screen_header", size=[1800, 104], border=META["kit_screen_header"]["border_LRTB"], xy=[60, 30], note="thanh tiêu đề màn")
    if back: rec(kind="sprite", file="kit_iconbtn_square_normal", size=[64, 64], border=None, xy=[96, 50], note="nút quay lại"); rec(kind="icon", file="kit_ico_arrow_left", size=[40, 40], border=None, xy=[128, 78], note="icon ink tối trên nút quay lại")
def button(*a, **k):
    inside[0] += 1
    try: return o_button(*a, **k)
    finally: inside[0] -= 1
o_logo, o_bg = mock.logo, mock.bg; o_overlay, o_dim, o_bigmap = screens_a.overlay, screens_b.dim, screens_b.bigmap; screens_b._itm, screens_b._put_fx, screens_b._hudbg = screens_b.itm, screens_b.put_fx, screens_b.hudbg
for mod in (mock, screens_a, screens_b):
    for nm, fn in (("put", put), ("icon", icon), ("ico", ico), ("close_btn", close_btn), ("logo", logo), ("bg", bg), ("header", header), ("button", button)): wrap(mod, nm, fn)
mock.spr = spr; screens_a.overlay = overlay; screens_b.dim = dim; screens_b.bigmap = bigmap; screens_b.ALL = {k: (lambda f: f) for k in ()} if False else screens_b.ALL; screens_b.itm, screens_b.put_fx, screens_b.hudbg = itm, put_fx, hudbg
GROUPS = [("1-sanh-va-phong", "T-024", ["a1-khoi-dong", "a2-sanh-chinh", "a3-vao-phong", "a3-vao-phong-loi", "a3-vao-phong-dang-ket-noi", "a3b-dang-nhap", "a3b-dang-nhap-loi", "a4-phong-cua-toi", "a4-phong-cua-toi-trong", "a4-phong-cua-toi-loi", "a4-xoa-phong", "a5-tao-phong", "a5-tao-phong-loi", "a6-phong-cho-cho-du-nguoi", "a6-phong-cho-dem-nguoc"]),
          ("2-hud-balo-ban-do", "T-022", ["b9-phan-hoi-hud", "b9-phan-hoi-mau-thap", "b4-tui-do", "b4-tui-do-tooltip", "b4-tui-do-trong", "b5-ban-do-lon"]),
          ("3-esc-spectator", "T-023", ["b1-esc-cai-dat", "b1-esc-xac-nhan-thoat", "b3-spectator", "b3-spectator-chuyen", "b3-spectator-het", "b2-bi-loai", "b2-bi-loai-bo", "b2-bi-loai-het-nguoi"]),
          ("4-loading-mat-ket-noi-ket-qua", "T-025", ["a7-dang-tai", "a7-dang-tai-loi", "b8-mat-ket-noi", "b8-mat-ket-noi-that-bai", "b8-mat-ket-noi-het-han", "b6-thang", "b7-thua"])]
ALL = {**screens_a.ALL, **screens_b.ALL}; USED = collections.defaultdict(set); SUMMARY = {}
def sz(s): return f"{s[0]}x{s[1]}"
def files_for(name):
    p1, p2 = T20 / f"{name}.png", T20 / f"{name}@2x.png"; return ("có" if p1.exists() else "không") + " / " + ("có" if p2.exists() else "không")
def table(recs):
    rows = collections.OrderedDict()
    for r in recs:
        k = (r["kind"], r["file"], tuple(r["size"]), str(r.get("border")), r.get("src")); rows.setdefault(k, [r, 0]); rows[k][1] += 1
    out = ["| Phần tử mock | File nguồn | Cỡ vẽ 1080p | Border 9-slice LRTB | @1x / @2x | Số lần | Ghi chú |", "|---|---|---:|---|---|---:|---|"]
    for (k, f, s, b, _src), (r, n) in rows.items():
        kind = {"sprite": "khung/thành phần kit", "sprite_raw": "sprite kit", "icon": "icon", "item_icon": "icon vật phẩm", "fx": "FX/emblem", "logo": "logo", "bg": "nền", "hudbg": "nền HUD", "code": "vẽ bằng mã (Image/primitive, không phải file)"}[k]
        if r.get("src") == "new": path = f"files/new/{f}.png"; ex = "có / có"
        elif k in ("sprite", "sprite_raw", "icon"): path = f"T-020/{f}.png"; ex = files_for(f)
        elif k == "fx": path = f"T-021/b/assets/{f}.png"; ex = "có / không"
        elif k == "item_icon": path = f"Assets/_JungleMaze/Art/{f}.png"; ex = "64x64 / không"
        elif k in ("bg", "code"): path = f; ex = "1x" if k == "bg" else "-"
        else: path = f; ex = "-"
        out.append(f"| {kind} | `{path}` | {sz(s)} | {r['border'] if r.get('border') else 'không (không cắt)'} | {ex} | {n} | {r.get('note', '')} |")
        if k in ("sprite", "sprite_raw", "icon"): USED[f].add(None)
    return out
def run():
    (OUT / "files").mkdir(exist_ok=True, parents=True)
    for grp, tk, names in GROUPS:
        (OUT / grp).mkdir(exist_ok=True); idx = []
        for n in names:
            R.clear(); mock.LOG.clear(); mock.LOGB.clear(); mock.CUR[0] = n; im = ALL[n]()
            sp = list(R); logs = list(mock.LOG); btn = list(mock.LOGB)
            md = [f"# {n} ({tk})", "", f"Ảnh mock: `T-021/{'a' if n.startswith('a') else 'b'}/{n}.png` (1920x1080). Border 9-slice là của sprite @1x (LRTB); @2x nhân đôi.", "", "## Khung, icon, FX, nền", ""] + table(sp)
            bt = collections.OrderedDict()
            for b in btn:
                k = (b["kind"], b["state"], b["w"], b["h"], b["caption"], b["icon"], b["font"]); bt[k] = bt.get(k, 0) + 1
            if bt:
                md += ["", "## Nút", "", "Sprite `T-020/kit_btn_<loại>_<trạng thái>.png` (+ `@2x`), 9-slice [22,22,28,28]; chữ Be Vietnam Pro Bold, icon trái cao bằng cỡ chữ, gap 10 px, căn giữa mặt nút (trừ đế).", "", "| Loại | Trạng thái | Cỡ nút | Cỡ chữ | Chữ | Icon |", "|---|---|---:|---:|---|---|"]
                for (kd, st, w, h, cap, ic, ft), c in bt.items(): md.append(f"| {kd} | {st} | {w}x{h} | {ft} | {cap} | {('kit_ico_' + ic) if ic and not ic.startswith('@') else (ic or '-')} |")
            tx = collections.OrderedDict()
            for l in logs: tx.setdefault((l["text"], l["size"], l["font"], l["fill"]), 0); tx[(l["text"], l["size"], l["font"], l["fill"])] += 1
            md += ["", "## Chữ (không phải asset: render bằng font)", "", "Thang 5 bậc; bậc 96 và 40 dùng Chakra Petch Bold, còn lại Be Vietnam Pro Bold (chữ nút luôn Be Vietnam Pro). Viền chữ max(2, em/12) px màu #0A0C0C.", "", "| Chữ | Cỡ | Font | Màu |", "|---|---:|---|---|"]
            for (t, s, ft, fill), c in tx.items(): md.append(f"| {t[:60]} | {s} | {'Chakra Petch Bold' if ft == 'Main' else 'Be Vietnam Pro Bold'} | {fill} |")
            if n.startswith("b4"): md += ["", "## Asset mới T-028 dùng cho màn này", "", "| Chỗ trong mock | File mới | Cỡ vẽ 1080p |", "|---|---|---:|", "| dòng Đạn cột Trang bị (ô giữ chỗ 4/90) | `files/new/icon_ammo.png` (+ @2x) | 72x72 |", "| tiêu đề cột trái đồ ở gần (mock dùng `kit_ico_list` tạm) | `files/new/icon_ground.png` (+ @2x) | 30x30 trên nút tròn |"]
            ex3 = []
            if n.startswith("b2"): ex3.append("| nguyên nhân bị loại Bo thu (thẻ `kit_card_cause`) | `files/new/icon_zone_cause.png` (+ @2x) | 60x60 (mock dùng `icon_zone-timer`) |") if n == "b2-bi-loai-bo" else None
            if n.startswith("b3"): ex3.append("| vũ khí của mục tiêu spectator (cạnh tên trong thẻ, 44x44) | icon vũ khí Assets, đủ 18 loại: xem `weapons-icons.md`, `files/items/<TÊN>.png` | 44x44 |")
            if n in ("a2-sanh-chinh", "a4-phong-cua-toi", "a6-phong-cho-cho-du-nguoi", "a3b-dang-nhap"): ex3.append("| nút Luyện tập offline / Lưu endpoint / Xem online / Điền đủ (unity thêm, mock chưa có) | `files/new/kit_ico_practice`, `kit_ico_save`, `kit_ico_globe`, `kit_ico_fill` (+ @2x), icon mục tối 48x48 cao bằng cỡ chữ | cao 24 (nút S) hoặc 30 (nút M) |")
            if ex3: md += ["", "## Asset mới T-028 đợt 3 dùng cho màn này", "", "| Chỗ | File | Cỡ vẽ 1080p |", "|---|---|---:|"] + [e for e in ex3 if e]
            (OUT / grp / f"{n}.md").write_text("\n".join(md), encoding="utf-8"); idx.append((n, len(sp), len(bt), len(tx)))
            for r in sp:
                if r["kind"] in ("sprite", "sprite_raw", "icon"): USED[r["file"]].add(n)
                if r["kind"] == "fx": USED["fx:" + r["file"]].add(n)
                if r["kind"] == "item_icon": USED["item:" + r["file"]].add(n)
            for b in btn:
                USED[f"kit_btn_{b['kind']}_{b['state']}"].add(n)
                if b["icon"]: USED[("kit_icon_cross_red" if b["icon"] == "@cross_red" else "kit_icon_cross_ink" if b["icon"] == "@cross_ink" else f"kit_ico_{b['icon']}")].add(n)
        SUMMARY[grp] = (tk, idx)
    return SUMMARY
if __name__ == "__main__":
    run(); print(json.dumps({g: [v[0], [x[0] for x in v[1]]] for g, v in SUMMARY.items()}, ensure_ascii=False)); json.dump({k: sorted(x for x in v if x) for k, v in USED.items()}, open(OUT / "dung-o-man.json", "w", encoding="utf-8"), ensure_ascii=False, indent=0)
