from PIL import Image, ImageDraw, ImageFont
import pathlib
OUT = pathlib.Path(r"D:\Lastaway\projects\lastaway\art\inbox\T-015")
con = Image.open(r"D:\Lastaway\projects\lastaway\art\inbox\T-014\01-da-khac\spawn.png").convert("RGB").resize((1920, 1080), Image.LANCZOS)
mine = Image.open(OUT / "dung-thu-hud-A.png").convert("RGB")
F = ImageFont.truetype(r"D:\Lastaway\Assets\_JungleMaze\Art\Fonts\BeVietnamPro-Bold.ttf", 22)
W = 1920; pad = 10
pairs = [("Toan man (thu 50%)", None, None), ("Vitals", (30, 850, 360, 1062), (30, 862, 360, 1054)), ("Quick bar", (650, 900, 1280, 1062), (650, 905, 1280, 1055)), ("Minimap + Match status", (1560, 25, 1900, 440), (1630, 25, 1900, 395))]
rows = []
full = Image.new("RGB", (W, 540 + 40), (15, 21, 23)); full.paste(con.resize((950, 534), Image.LANCZOS), (pad, 36)); full.paste(mine.resize((950, 534), Image.LANCZOS), (W // 2 + pad, 36))
rows.append(full)
for name, a, b in pairs[1:]:
    ca, cb = con.crop(a), mine.crop(b); h = 330
    ca = ca.resize((int(ca.width * h / ca.height), h), Image.LANCZOS); cb = cb.resize((int(cb.width * h / cb.height), h), Image.LANCZOS)
    r = Image.new("RGB", (W, h + 40), (15, 21, 23)); r.paste(ca, (pad, 36)); r.paste(cb, (W // 2 + pad, 36)); rows.append(r)
H = sum(r.height for r in rows); out = Image.new("RGB", (W, H), (15, 21, 23)); y = 0
for (name, _, _), r in zip(pairs, rows):
    out.paste(r, (0, y)); d = ImageDraw.Draw(out)
    d.text((pad, y + 6), f"Concept A (T-014)  |  {name}", font=F, fill=(237, 242, 238)); d.text((W // 2 + pad, y + 6), f"Dung thu T-015 v2  |  {name}", font=F, fill=(245, 197, 66)); y += r.height
out.save(OUT / "so-sanh-concept.png"); print(out.size)
