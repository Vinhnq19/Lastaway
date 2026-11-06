import pathlib
ROOT = pathlib.Path(r"D:\Lastaway\projects\lastaway\art\inbox\T-015\src")
ROOT.mkdir(parents=True, exist_ok=True)

BASE = """The attached image is a concept screenshot of our pixel-art battle royale HUD in the 'Carved Stone' skin: mossy grey-brown stone slabs with incised glyph borders, small green moss tufts on the corners, thin gold accents, a dark outer edge and a light highlight bevel on the inner edge.

TASK: draw a clean UI-kit sprite sheet of EMPTY pieces of exactly that skin (same stone, same moss, same gold, same crisp pixel-art look, same edge treatment), each piece isolated on a perfectly flat solid magenta background (#FF00FF, no gradient, no noise, no shadow, no glow, magenta must not appear inside any piece). Pieces are laid out in a grid with at least 80 px of pure magenta between them and around the sheet border. No text, no numbers, no icons, no characters, no game world. Interiors of the pieces must be a SOLID flat very dark stone color (about #23282a) with NO texture, NO pattern, NO dashed lines, so they can be stretched. Draw each piece large and sharp. Do not crop any piece.

PIECES (left to right, top to bottom):
"""

SHEETS = {
    "sheet-panels": """1. PANEL-LARGE: an empty rectangle frame, aspect about 1.8:1 (wide), carved stone border about 8% of its height thick, ornamental corner pieces with small moss, solid dark interior.
2. PANEL-WIDE: an empty rectangle frame, aspect about 4.7:1 (very wide and short), same border style and corners, solid dark interior.
3. PANEL-SMALL: an empty rectangle frame, aspect about 2.7:1, same style, solid dark interior.
4. MINIMAP-FRAME: an empty SQUARE frame, 1:1, same carved stone border and corner ornaments, and the interior is a flat solid magenta hole (#FF00FF) so the map can show through (the border must fully enclose the hole).
5. KILLFEED-PLATE: a thin dark translucent-looking plain plaque, aspect about 3:1, with only a very faint thin stone edge (no ornaments), solid dark interior.""",
    "sheet-slots": """1. SLOT-EMPTY: one empty square item slot, 1:1, a dark recessed square set into a thin carved stone rim, no dashed lines.
2. SLOT-SELECTED: the same square slot but highlighted: a bright warm gold rim and a faint warm inner glow, still 1:1, still empty inside.
3. BAR-TRACK: an empty horizontal bar groove, aspect about 10:1, dark recessed channel with a thin stone rim and rounded square ends, empty inside (dark).
4. BAR-FILL-WHITE: a horizontal bar fill piece of the same aspect and the same inner size as the groove interior, pure flat white (#FFFFFF) with a very subtle 1px darker bottom edge, rounded square ends, so the game can tint it red or yellow.
5. AMMO-PLATE: a small horizontal stone plaque, aspect about 5:1, with small notched ends, solid dark interior, gold hairline accent.""",
}

for k, body in SHEETS.items():
    (ROOT / f"{k}.prompt.src.txt").write_text(BASE + body + "\n", encoding="utf-8")
print("ok")
