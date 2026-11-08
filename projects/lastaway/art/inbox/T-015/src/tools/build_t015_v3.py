"""T-015 luot 3: khung da mong, phang, ve bang code (khong dung GPT), dung dung kich thuoc.
Chay: python build_t015_v3.py            -> ghi 3 bien the vao T-015/lan3/<ten>/ (sprite @1x, @2x)
Moi bien the doi MOT huong tach UI khoi gameplay; khung dung chung hinh."""
import sys, json, shutil, pathlib
import numpy as np
from PIL import Image, ImageDraw, ImageFilter
sys.stdout.reconfigure(encoding="utf-8")
ROOT = pathlib.Path(r"D:\Lastaway\projects\lastaway\art\inbox\T-015")
RECT = {"hud_panel_vitals": (311, 173), "hud_panel_quickbar": (592, 126), "hud_panel_match": (240, 89),
        "hud_minimap_frame": (240, 240), "hud_killfeed_plate": (256, 135)}
T = 8      # be day gio khung (px @1x), truoc 13-14
K = 13     # o khac goc (px @1x), truoc ~28 gom hoa van + reu
BORDER = 16  # 9-slice @1x

def vgrad(w, h, c0, c1):
    t = np.linspace(0, 1, h)[:, None, None]
    a = np.array(c0)[None, None, :] * (1 - t) + np.array(c1)[None, None, :] * t
    return Image.fromarray(np.repeat(a, w, axis=1).astype("uint8"), "RGB")

def poly(w, h, inset, cut):
    i, c = inset, cut
    return [(i + c, i), (w - 1 - i - c, i), (w - 1 - i, i + c), (w - 1 - i, h - 1 - i - c),
            (w - 1 - i - c, h - 1 - i), (i + c, h - 1 - i), (i, h - 1 - i - c), (i, i + c)]

def mask(w, h, inset, cut):
    m = Image.new("L", (w, h), 0); ImageDraw.Draw(m).polygon(poly(w, h, inset, cut), fill=255); return m

def panel(name, w, h, s, P, hole=False, plate=False):
    W, H, u = w * s, h * s, s
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    t = 3 if plate else T
    cut = (2 if plate else 4) * u
    # vien ngoai manh, nhat (alpha thap)
    out = Image.new("RGBA", (W, H), P["outline"]); out.putalpha(mask(W, H, 0, cut).point(lambda v: P["outline"][3] if v else 0)); im.alpha_composite(out)
    band = vgrad(W, H, P["frame_hi"], P["frame_lo"]).convert("RGBA"); bn = np.asarray(band.convert('RGB')).astype('float') + (np.random.RandomState(7).randn(H, W, 1) * 4.5); band = Image.fromarray(np.clip(bn, 0, 255).astype('uint8'), 'RGB').filter(ImageFilter.GaussianBlur(0.5 * u)).convert('RGBA')
    band.putalpha(mask(W, H, u, cut - u // 2)); im.alpha_composite(band)
    g = ImageDraw.Draw(im, "RGBA")
    if not plate:
        # moi noi da: vach toi mo, thua (chat da khac nhung it chi tiet)
        for x in range(26 * u, W - 20 * u, 36 * u):
            g.line([(x, u), (x, (t + 1) * u - 1)], fill=P["joint"]); g.line([(x, H - (t + 1) * u), (x, H - 1 - u)], fill=P["joint"])
        for y in range(26 * u, H - 20 * u, 36 * u):
            g.line([(u, y), ((t + 1) * u - 1, y)], fill=P["joint"]); g.line([(W - (t + 1) * u, y), (W - 1 - u, y)], fill=P["joint"])
    # long panel
    ins = (1 + t) * u
    if hole:
        hm = mask(W, H, ins, 2 * u)
        keep = Image.eval(hm, lambda v: 255 - v)       # chua lo thanh trong suot
        a = im.getchannel("A"); im.putalpha(Image.composite(Image.new("L", (W, H), 0), a, hm))
    else:
        base = Image.new("RGB", (W, H), P["fill"])
        if P["noise"]:
            n = Image.effect_noise((W, H), P["noise"] * 6).filter(ImageFilter.GaussianBlur(0.6 * u))
            nn = (np.asarray(n).astype("float") - 128) * P["noise"] / max(1, np.asarray(n).astype("float").std())
            arr = np.clip(np.asarray(base).astype("float") + nn[..., None], 0, 255).astype("uint8"); base = Image.fromarray(arr, "RGB")
        base = base.convert("RGBA"); fm = mask(W, H, ins, 2 * u)
        base.putalpha(fm.point(lambda v: int(v * P["fill_a"]))); im.alpha_composite(base)
    g = ImageDraw.Draw(im, "RGBA")
    g.polygon(poly(W, H, ins - u, 2 * u), outline=P["inner_line"])      # 1 duong khac toi gioi han long
    if P["inner_light"] and not plate:
        g.polygon(poly(W, H, ins, 2 * u), outline=P["inner_light"])     # gờ sáng mảnh phía trong
    if not plate:
        # o khac goc: vuong nho + cham giua
        k = K * u
        for (x0, y0) in ((2 * u, 2 * u), (W - 2 * u - k, 2 * u), (2 * u, H - 2 * u - k), (W - 2 * u - k, H - 2 * u - k)):
            g.rectangle((x0, y0, x0 + k - 1, y0 + k - 1), fill=P["corner"], outline=P["corner_edge"])
            c = 3 * u; cx = x0 + (k - c) // 2; cy = y0 + (k - c) // 2
            g.rectangle((cx, cy, cx + c - 1, cy + c - 1), fill=P["corner_dot"])
    return im

def shadow_sprite(s, blur, alpha):
    n = 64 * s; im = Image.new("RGBA", (n, n), (0, 0, 0, 0)); m = Image.new("L", (n, n), 0)
    pad = 24 * s; ImageDraw.Draw(m).rectangle((pad, pad, n - pad, n - pad), fill=255)
    m = m.filter(ImageFilter.GaussianBlur(blur * s)); im.putalpha(m.point(lambda v: int(v * alpha))); return im

def slot(s, selected, P):
    n = 60 * s; u = s; im = Image.new("RGBA", (n, n), (0, 0, 0, 0)); g = ImageDraw.Draw(im, "RGBA")
    rim_hi = (206, 180, 120, 255) if selected else P["frame_hi"] + (255,)
    rim_lo = (160, 128, 70, 255) if selected else P["frame_lo"] + (255,)
    g.rectangle((0, 0, n - 1, n - 1), fill=P["outline"][:3] + (P["outline"][3],))
    g.rectangle((u, u, n - 1 - u, n - 1 - u), fill=rim_lo)
    g.rectangle((u, u, n - 1 - u, 3 * u - 1), fill=rim_hi); g.rectangle((u, u, 3 * u - 1, n - 1 - u), fill=rim_hi)
    g.rectangle((3 * u, 3 * u, n - 1 - 3 * u, n - 1 - 3 * u), fill=P["fill"] + (255,))
    if selected: g.rectangle((3 * u, 3 * u, n - 1 - 3 * u, n - 1 - 3 * u), outline=(245, 197, 66, 120))
    for (x, y) in ((0, 0), (n - 1, 0), (0, n - 1), (n - 1, n - 1)):
        for dx in range(u):
            for dy in range(u): im.putpixel((x - dx if x else x + dx, y - dy if y else y + dy), (0, 0, 0, 0))
    return im

def track(w, h, s, P, r=2):
    W, H, u = w * s, h * s, s; im = Image.new("RGBA", (W, H), (0, 0, 0, 0)); g = ImageDraw.Draw(im, "RGBA")
    g.rectangle((0, 0, W - 1, H - 1), fill=P["outline"])
    g.rectangle((u, u, W - 1 - u, H - 1 - u), fill=P["frame_lo"] + (255,))
    g.rectangle((u, u, W - 1 - u, 2 * u - 1), fill=P["frame_hi"] + (255,))
    g.rectangle(((1 + r) * u, (1 + r) * u, W - 1 - (1 + r) * u, H - 1 - (1 + r) * u), fill=tuple(max(0, c - 22) for c in P["fill"]) + (255,))
    return im

VARIANTS = {
    # 1: bong do mem duoi panel, tong da am nhu cu (khong doi mau)
    "A1-bong-do": dict(frame_hi=(138, 130, 112), frame_lo=(100, 94, 82), joint=(70, 64, 54, 210), outline=(24, 22, 20, 190),
                       inner_line=(26, 28, 27, 255), inner_light=None, fill=(44, 47, 46), noise=2.0, fill_a=1.0,
                       corner=(118, 111, 96, 255), corner_edge=(70, 65, 56, 255), corner_dot=(150, 142, 122, 255),
                       shadow=(10, 0.55)),
    # 2: da lanh, khu bao hoa (xanh xam) de lech khoi world olive am; bong nhe
    "A2-da-lanh": dict(frame_hi=(132, 140, 148), frame_lo=(92, 99, 108), joint=(56, 64, 76, 210), outline=(14, 20, 26, 190),
                       inner_line=(18, 24, 30, 255), inner_light=None, fill=(36, 43, 50), noise=1.5, fill_a=1.0,
                       corner=(112, 120, 130, 255), corner_edge=(62, 70, 80, 255), corner_dot=(150, 158, 168, 255),
                       shadow=(6, 0.35)),
    # 3: long dac hon va toi, gờ sang manh phia trong (khong bong)
    "A3-long-dac": dict(frame_hi=(138, 130, 112), frame_lo=(100, 94, 82), joint=(70, 64, 54, 210), outline=(20, 18, 16, 190),
                        inner_line=(14, 15, 15, 255), inner_light=(190, 180, 150, 150), fill=(26, 28, 28), noise=0.0, fill_a=1.0,
                        corner=(118, 111, 96, 255), corner_edge=(70, 65, 56, 255), corner_dot=(150, 142, 122, 255),
                        shadow=None),
}

if __name__ == "__main__":
    meta = {}
    for vn, P in VARIANTS.items():
        d = ROOT / "lan3" / vn; d.mkdir(parents=True, exist_ok=True)
        for s, suf in ((1, ""), (2, "@2x")):
            for nm, (w, h) in RECT.items():
                im = panel(nm, w, h, s, P, hole=(nm == "hud_minimap_frame"), plate=(nm == "hud_killfeed_plate"))
                if nm == "hud_killfeed_plate":
                    im.putalpha(im.getchannel("A").point(lambda v: int(v * 0.78)))
                im.save(d / f"{nm}{suf}.png")
            slot(s, False, P).save(d / f"hud_slot_empty{suf}.png"); slot(s, True, P).save(d / f"hud_slot_selected{suf}.png")
            if P["shadow"]: shadow_sprite(s, *P["shadow"]).save(d / f"hud_panel_shadow{suf}.png")
        for s_, suf_ in ((1, ""), (2, "@2x")):
            track(164, 21, s_, P).save(d / f"hud_bar_track_hp{suf_}.png"); track(164, 12, s_, P, 1).save(d / f"hud_bar_track_thin{suf_}.png"); track(220, 36, s_, P).save(d / f"hud_ammo_plate{suf_}.png")
        for nm in ("hud_bar_fill_white", "hud_minimap_overlay"):
            for suf in ("", "@2x"): shutil.copy(ROOT / f"{nm}{suf}.png", d / f"{nm}{suf}.png")
    print("ok")
