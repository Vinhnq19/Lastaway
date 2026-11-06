import sys, json, pathlib
sys.path.insert(0, str(pathlib.Path(__file__).parent))
from extract_t015 import *
OUT = pathlib.Path(r"D:\Lastaway\projects\lastaway\art\inbox\T-015")
P, _ = pieces("sheet-panels"); S, _ = pieces("sheet-slots")
large, wide, small, mini, kill = P
slot_e, slot_s, bar_t, bar_f, ammo = S
meta = {}
from PIL import ImageFilter
def sharpen(img, s):
    r, g, bb, a = img.split(); rgb = Image.merge("RGB", (r, g, bb)).filter(ImageFilter.UnsharpMask(radius=1.0 * s, percent=110, threshold=2))
    r, g, bb = rgb.split(); return Image.merge("RGBA", (r, g, bb, a))


def emit(name, im, k, size, border_src, note):
    for s, suf in ((1, ""), (2, "@2x")):
        tw, th = size[0] * s, size[1] * s
        out, b = nine(im, k * s, (tw, th), border_src)
        out = sharpen(out, s)
        out.save(OUT / f"{name}{suf}.png")
        if s == 1: meta[name] = {"size": [tw, th], "border_LRTB": [b[0], b[1], b[2], b[3]], "note": note}
        else: meta[name]["border_LRTB_2x"] = [b[0], b[1], b[2], b[3]]

# size = pixels at 1080p; k = uniform scale src->1080 px
emit("hud_panel_vitals", large, 0.36, (311, 173), (90, 90, 90, 90), "PANEL-LARGE")
emit("hud_panel_quickbar", wide, 0.36, (592, 126), (70, 65, 70, 65), "PANEL-WIDE")
emit("hud_panel_match", small, 0.36, (240, 89), (75, 75, 75, 75), "PANEL-SMALL")
emit("hud_minimap_frame", mini, 0.36, (240, 240), (90, 90, 90, 90), "MINIMAP-FRAME (hole transparent)")
emit("hud_killfeed_plate", kill, 135 / kill.size[1], (256, 135), (40, 40, 40, 40), "KILLFEED-PLATE")
emit("hud_slot_empty", slot_e, 63 / slot_e.size[1], (63, 63), (90, 90, 90, 90), "SLOT-EMPTY")
emit("hud_slot_selected", slot_s, 63 / slot_s.size[1], (63, 63), (90, 90, 90, 90), "SLOT-SELECTED")
emit("hud_bar_track_hp", bar_t, 21 / bar_t.size[1], (164, 21), (110, 40, 110, 40), "BAR-TRACK")
emit("hud_bar_track_thin", bar_t, 12 / bar_t.size[1], (164, 12), (110, 40, 110, 40), "BAR-TRACK thin")
emit("hud_bar_fill_white", bar_f, 16 / bar_f.size[1], (64, 16), (24, 20, 24, 20), "BAR-FILL-WHITE (tint red/yellow)")
emit("hud_ammo_plate", ammo, 36 / ammo.size[1], (220, 36), (110, 40, 110, 40), "AMMO-PLATE")

from PIL import ImageDraw
def draw_slot(s, selected):
    n = 63 * s; u = s
    im = Image.new("RGBA", (n, n), (0, 0, 0, 0)); g = ImageDraw.Draw(im)
    def rr(box, fill): g.rectangle(box, fill=fill)
    rr((0, 0, n - 1, n - 1), (20, 22, 21, 255))                       # dark outline
    rim = (198, 174, 120, 255) if selected else (112, 104, 90, 255)
    hi = (232, 208, 150, 255) if selected else (146, 138, 120, 255)
    lo = (150, 124, 70, 255) if selected else (74, 69, 60, 255)
    rr((1 * u, 1 * u, n - 1 - u, n - 1 - u), rim)
    rr((1 * u, 1 * u, n - 1 - u, 2 * u - 1 + u), hi); rr((1 * u, 1 * u, 2 * u - 1 + u, n - 1 - u), hi)       # highlight top/left
    rr((1 * u, n - 3 * u, n - 1 - u, n - 1 - u), lo); rr((n - 3 * u, 1 * u, n - 1 - u, n - 1 - u), lo)       # shade bottom/right
    rr((4 * u, 4 * u, n - 1 - 4 * u, n - 1 - 4 * u), (20, 22, 21, 255))
    rr((5 * u, 5 * u, n - 1 - 5 * u, n - 1 - 5 * u), (35, 40, 42, 255))                                        # recess
    rr((5 * u, 5 * u, n - 1 - 5 * u, 6 * u + u - 1), (26, 30, 32, 255)); rr((5 * u, 5 * u, 6 * u + u - 1, n - 1 - 5 * u), (26, 30, 32, 255))
    if selected:
        for i in range(3):
            c = (245, 197, 66, 90 - i * 25); g.rectangle((5 * u + i * u, 5 * u + i * u, n - 1 - 5 * u - i * u, n - 1 - 5 * u - i * u), outline=c)
    # cut the 4 outer corner pixels (stepped corner)
    for (x, y) in ((0, 0), (n - 1, 0), (0, n - 1), (n - 1, n - 1)):
        for dx in range(u):
            for dy in range(u):
                xx = x - dx if x else x + dx; yy = y - dy if y else y + dy; im.putpixel((xx, yy), (0, 0, 0, 0))
    return im
for suf, s in (("", 1), ("@2x", 2)):
    draw_slot(s, False).save(OUT / f"hud_slot_empty{suf}.png"); draw_slot(s, True).save(OUT / f"hud_slot_selected{suf}.png")
meta["hud_slot_empty"]["note"] = "SLOT procedural (Pillow, mau lay tu concept A), khong gach dut"; meta["hud_slot_selected"]["note"] = "SLOT-SELECTED procedural, vien vang + glow"

# N3: minimap overlay (lop pho nhe): giua 50% toi, vien 70% (vignette 20 px) -> map doc ro hon duoi khung
def minimap_overlay(s):
    n = 240 * s; im = Image.new("RGBA", (n, n), (0, 0, 0, 0)); px = im.load(); edge = 24 * s
    for y in range(n):
        for x in range(n):
            dmin = min(x, y, n - 1 - x, n - 1 - y)
            t = max(0.0, 1 - dmin / edge)
            a = 0.50 + 0.20 * t * t
            px[x, y] = (12, 20, 23, int(255 * a))
    return im
for suf, s in (("", 1), ("@2x", 2)):
    minimap_overlay(s).save(OUT / f"hud_minimap_overlay{suf}.png")
meta["hud_minimap_overlay"] = {"size": [240, 240], "border_LRTB": [0, 0, 0, 0], "note": "N3 overlay"}
for suf in ("", "@2x"):
    p = OUT / f"hud_killfeed_plate{suf}.png"; im = Image.open(p); a = im.getchannel("A").point(lambda v: int(v * 0.6)); im.putalpha(a); im.save(p)
(OUT / "meta.json").write_text(json.dumps(meta, ensure_ascii=False, indent=1), encoding="utf-8")
for k, v in meta.items(): print(k, v["size"], v["border_LRTB"])
