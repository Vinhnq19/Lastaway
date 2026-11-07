"""Xếp map sinh tồn 150x150 "Rừng Sinh Tồn" cho Level Editor (kiểu surviv.io).

Ghi bản nháp chỉ có khối `editor` (placements theo ô + đường đá). Level Editor (LevelDocument.FromJson → Validate →
ToJson) dựng obstacles/worldObjects/spawnPoints đúng như khi bấm Lưu trong scene, nên collider chân, id và hash luôn
khớp game. Tất định theo seed.

    python Tools/Level/survival_map_150.py <out_draft.json> [--png preview.png] [--report]

Nguyên tắc cân bằng (số đo in ra bằng --report):
- Bo đầu (map 50 r=25, nhân 3) là hình tròn r=75 quanh tâm: góc map nằm ngoài bo ngay từ đầu, nên loot/portal/POI
  chỉ đặt trong r <= 68; góc là rừng rậm không loot.
- Loot chủ yếu rải sàn: GameServer/Luyện tập offline tự rải đồ theo hình học map lúc vào trận (FloorLootPlanner,
  Docs/hvu/07_Floor_Loot.vi.md), kể cả công bằng quanh spawn. Map chỉ giữ khoảng 1/3 số rương cũ, đặt trong nhà và
  phòng đền (mỗi phòng đền 1 rương), cùng 3 thùng tiếp tế (chùy, đồ phép) ở đền trung tâm và vòng đá tây bắc → điểm
  nóng có rủi ro. Không còn bình, thùng gỗ, thùng tròn: mỗi vật phá được chiếm ngân sách object mà rải sàn cần.
- Spawn không có rương trong 8 ô (phải di chuyển).
- Cover thật (đá, khối đá, tường, tượng...) đủ dày để ô trống xa cover nhất vẫn trong tầm súng ngắn.
"""
import json, math, random, sys

N = 150
C = N / 2
SEED = 5150

# Footprint (w, h) theo LevelEditorCatalog.asset; mặc định 1x1.
FOOT = {'Well': (2, 1), 'BenchS': (2, 1), 'BenchE': (1, 2), 'BenchW': (1, 2), 'CoffinH': (2, 1), 'CoffinV': (1, 2),
        'Rock07': (2, 1), 'Gate01': (2, 2), 'Gate02': (2, 3), 'Portal': (3, 2)}
WORLD = {'Chest', 'Pot', 'Supply', 'Bush', 'Door', 'Portal'}
DECOR = {'Pebble', 'Brick', 'GrassTuft', 'Flower', 'SignE', 'SignW'}
# Cover chặn đạn đáng kể (collider rộng >= ~0.6 ô); cây chỉ có gốc nhỏ nên không tính.
REAL_COVER = {'Wall', 'Statue01', 'Pillar01', 'Pillar02', 'RunePillarX2', 'RunePillarX3', 'RunePillarBroken',
              'Gravestone01', 'Gravestone03', 'StoneCube', 'Well', 'BenchS', 'BenchE', 'BenchW', 'CoffinH', 'CoffinV',
              'CrateSmall', 'Rock06', 'Rock07', 'Gate01', 'Gate02'}
TREES = ['Tree01', 'Tree02', 'Tree03']


class Map:
    def __init__(self, seed):
        self.rng = random.Random(seed)
        self.occ = {}          # ô -> index placement (lớp chung: cover, vật game, spawn)
        self.dec = set()       # ô decor
        self.path = set()
        self.reserved = set()  # ô giữ trống (lối đi, sân spawn)
        self.openings = set()  # lối vào nhà/đền (khe tường để trống, không có cửa)
        self.items = []

    # ---------- đặt vật ----------
    def free(self, t, x, y, natural=False):
        w, h = FOOT.get(t, (1, 1))
        if x < 0 or y < 0 or x + w > N or y + h > N:
            return False
        for i in range(x, x + w):
            for j in range(y, y + h):
                c = (i, j)
                if t in DECOR:
                    if c in self.dec:
                        return False
                elif c in self.occ or c in self.reserved:
                    return False
                if natural and c in self.path:
                    return False
        return True

    def put(self, t, x, y, flip=False, natural=False, **extra):
        if not self.free(t, x, y, natural):
            return None
        group = 'Decoration' if t in DECOR else 'Spawn' if t == 'Spawn' else 'WorldObject' if t in WORLD else 'Cover'
        p = {'type': t, 'group': group, 'cellX': x, 'cellY': y}
        if flip:
            p['flipX'] = True
        p.update(extra)
        w, h = FOOT.get(t, (1, 1))
        for i in range(x, x + w):
            for j in range(y, y + h):
                if t in DECOR:
                    self.dec.add((i, j))
                else:
                    self.occ[(i, j)] = len(self.items)
        self.items.append(p)
        return p

    def breakable(self, t, x, y, hp, loot):
        return self.put(t, x, y, destructible=True, loot=loot, hp=hp)

    def road(self, x0, y0, x1, y1):
        for x in range(min(x0, x1), max(x0, x1) + 1):
            for y in range(min(y0, y1), max(y0, y1) + 1):
                if 0 <= x < N and 0 <= y < N:
                    self.path.add((x, y))

    def reserve(self, x0, y0, x1, y1):
        for x in range(x0, x1 + 1):
            for y in range(y0, y1 + 1):
                self.reserved.add((x, y))

    def opening(self, x, y):
        """Khe tường làm lối vào: để trống hẳn (không đặt cửa), không cho vật nào chắn."""
        self.reserved.add((x, y))
        self.openings.add((x, y))
        self.path.add((x, y))  # lát đá cho liền sàn trong và đường ngoài

    def wall_line(self, x0, y0, x1, y1, gaps=()):
        for x in range(min(x0, x1), max(x0, x1) + 1):
            for y in range(min(y0, y1), max(y0, y1) + 1):
                if (x, y) not in gaps:
                    self.put('Wall', x, y)

    # ---------- khu vực ----------
    def house(self, x, y, w, h, doors, loot, chest_share=1 / 3, inner=None):
        """Nhà tường gạch kín, lối vào để trống (không cửa). Rương dọc chân tường (khoảng chest_share của `loot` chỗ cũ),
        chừa trống quanh mỗi lối vào; phần còn lại của nhà do rải sàn lấp."""
        d = set(doors)
        self.wall_line(x, y, x + w - 1, y, d)
        self.wall_line(x, y + h - 1, x + w - 1, y + h - 1, d)
        self.wall_line(x, y + 1, x, y + h - 2, d)
        self.wall_line(x + w - 1, y + 1, x + w - 1, y + h - 2, d)
        for c in doors:
            self.opening(*c)
        self.road(x, y + 1, x + w - 1, y + h - 2)  # sàn lát tới sát dải đá của tường dọc
        keep = set()
        for dx, dy in doors:
            for i in range(-1, 2):
                for j in range(-1, 2):
                    keep.add((dx + i, dy + j))
                    keep.add((dx + 2 * i, dy + 2 * j))
        if inner:
            inner(keep)
        spots = [(i, j) for i in range(x + 1, x + w - 1) for j in range(y + 1, y + h - 1)
                 if (i in (x + 1, x + w - 2) or j in (y + 1, y + h - 2)) and (i, j) not in keep]
        self.rng.shuffle(spots)
        chests = max(1, round(loot * chest_share))
        placed = 0
        for s in spots[:loot]:
            if placed >= chests:
                break
            placed += 1 if self.put('Chest', *s) else 0
        # nội thất làm cover trong nhà
        for s in spots[loot:loot + max(1, (w * h) // 30)]:
            self.put(self.rng.choice(['StoneLantern', 'BenchS', 'StoneCube']), *s)

    def temple(self, cx, cy):
        """Đền trung tâm 28x28: hot drop. 4 cổng rộng 2 ô, 4 phòng góc có lối vào, sân giữa có 2 thùng tiếp tế."""
        s = 28
        x, y = cx - s // 2, cy - s // 2
        m, n = cx, cy
        gates = [(m - 1, y), (m, y), (m - 1, y + s - 1), (m, y + s - 1), (x, n - 1), (x, n), (x + s - 1, n - 1), (x + s - 1, n)]
        self.wall_line(x, y, x + s - 1, y, gates)
        self.wall_line(x, y + s - 1, x + s - 1, y + s - 1, gates)
        self.wall_line(x, y + 1, x, y + s - 2, gates)
        self.wall_line(x + s - 1, y + 1, x + s - 1, y + s - 2, gates)
        for g in gates:
            self.opening(*g)
        # ụ tháp 3x3 ở 4 góc ngoài
        for ox, oy in ((x - 1, y - 1), (x + s - 2, y - 1), (x - 1, y + s - 2), (x + s - 2, y + s - 2)):
            for i in range(3):
                for j in range(3):
                    self.put('Wall', ox + i, oy + j)
        self.road(x, y + 1, x + s - 1, y + s - 2)
        # 4 phòng góc 9x9 (tường trong), mỗi phòng 1 lối ra sân + 1 lối ra hành lang ngoài
        rooms = [(x, y, 1, 1), (x + s - 9, y, -1, 1), (x, y + s - 9, 1, -1), (x + s - 9, y + s - 9, -1, -1)]
        for rx, ry, sx, sy in rooms:
            ix = rx + 8 if sx == 1 else rx      # cạnh dọc trong
            iy = ry + 8 if sy == 1 else ry      # cạnh ngang trong
            d1 = (ix, ry + 4)
            d2 = (rx + 4, iy)
            self.wall_line(ix, ry + 1, ix, ry + 7, (d1,))
            self.wall_line(rx + 1, iy, rx + 7, iy, (d2,))
            self.put('Wall', ix, iy)
            self.opening(*d1)
            self.opening(*d2)
            ax = rx + (2 if sx == 1 else 6)
            ay = ry + (2 if sy == 1 else 6)
            self.put('Chest', ax, ay)
            self.put('StoneLantern', ax + sx * 4, ay + sy * 1)
        # sân giữa: 2 thùng tiếp tế đối xứng, tượng/cột làm cover, giếng giữa
        self.put('Well', m - 1, n)
        self.put('Supply', m - 4, n - 3)
        self.put('Supply', m + 3, n + 3)
        for sx, sy in ((-1, -1), (1, -1), (-1, 1), (1, 1)):
            self.put('Statue01', m + sx * 5 - (1 if sx < 0 else 0), n + sy * 5 - (1 if sy < 0 else 0), flip=sx > 0)
        for sx, sy in ((-1, 0), (1, 0), (0, -1), (0, 1)):
            self.put('RunePillarX3', m + sx * 7 - (1 if sx < 0 else 0), n + sy * 7 - (1 if sy < 0 else 0))
        self.put('StoneCube', m - 6, n + 2)
        self.put('StoneCube', m + 5, n - 3)
        # sân ngoài: vòng đường đá + cột rune / đèn làm cover tiếp cận
        self.road(x - 4, y - 4, x + s + 3, y - 3)
        self.road(x - 4, y + s + 2, x + s + 3, y + s + 3)
        self.road(x - 4, y - 4, x - 3, y + s + 3)
        self.road(x + s + 2, y - 4, x + s + 3, y + s + 3)
        for k in range(2, s, 5):
            self.put('RunePillarX2', x + k, y - 6)
            self.put('RunePillarX2', x + k, y + s + 5)
            self.put('StoneLantern', x - 6, y + k)
            self.put('StoneLantern', x + s + 5, y + k)

    def village(self, cx, cy):
        """Làng phía bắc: 4 nhà quanh quảng trường giếng."""
        self.road(cx - 14, cy - 2, cx + 14, cy + 1)
        self.road(cx - 2, cy - 12, cx + 1, cy + 12)
        houses = [(cx - 14, cy + 3, 10, 8), (cx + 4, cy + 3, 10, 8), (cx - 14, cy - 11, 10, 8), (cx + 4, cy - 11, 10, 8)]
        for i, (x, y, w, h) in enumerate(houses):
            front = (x + w // 2, y) if y > cy else (x + w // 2, y + h - 1)
            side = (x + w - 1, y + h // 2) if x < cx else (x, y + h // 2)
            self.house(x, y, w, h, [front, side], 5)
        for dx, dy in ((-4, -4), (3, -4), (-4, 3), (3, 3)):
            self.put('StoneLantern', cx + dx, cy + dy)
        self.put('BenchS', cx - 7, cy + 1)
        self.put('BenchS', cx + 6, cy - 2)
        self.put('SignE', cx + 2, cy - 12)

    def graveyard(self, cx, cy):
        """Nghĩa địa có tường thấp, hầm mộ nhỏ có rương."""
        w, h = 24, 20
        x0, y0 = cx - w // 2, cy - h // 2
        gaps = {(cx - 1, y0), (cx, y0), (x0, cy), (x0 + w - 1, cy - 1)}
        self.wall_line(x0, y0, x0 + w - 1, y0, gaps)
        self.wall_line(x0, y0 + h - 1, x0 + w - 1, y0 + h - 1, gaps)
        self.wall_line(x0, y0 + 1, x0, y0 + h - 2, gaps)
        self.wall_line(x0 + w - 1, y0 + 1, x0 + w - 1, y0 + h - 2, gaps)
        self.road(cx - 1, y0, cx, y0 + h - 2)
        # hầm mộ 8x6 ở cuối lối
        self.house(cx - 4, y0 + h - 8, 8, 6, [(cx - 1, y0 + h - 8)], 3, chest_share=.67)
        for gx in range(x0 + 2, x0 + w - 2, 3):
            for gy in range(y0 + 3, y0 + h - 9, 3):
                if abs(gx - cx) < 3:
                    continue
                r = self.rng.random()
                t = 'Gravestone01' if r < .3 else 'Gravestone03' if r < .55 else 'Gravestone02' if r < .7 else 'CoffinH' if r < .85 else None
                if t is None:   # trước đây là bình: giờ để trống cho rải sàn
                    continue
                self.put(t, gx, gy)
        self.put('Statue01', x0 + 2, y0 + h - 3)
        self.put('Statue01', x0 + w - 3, y0 + h - 3, flip=True)
        self.put('StoneLantern', cx - 3, y0 + 1)
        self.put('StoneLantern', cx + 2, y0 + 1)

    def farm(self, cx, cy):
        """Trang trại phía đông: kho lớn nhiều lối vào + bãi đá."""
        self.house(cx - 9, cy - 1, 14, 10, [(cx - 2, cy - 1), (cx - 9, cy + 4), (cx + 4, cy + 4)], 7)
        self.house(cx + 7, cy - 12, 8, 7, [(cx + 7, cy - 9)], 3)
        self.road(cx - 12, cy - 6, cx + 4, cy - 3)
        for i in range(6):
            x = cx - 8 + 3 * i
            self.put('Rock06' if i % 3 == 0 else 'StoneCube', x + 1, cy - 11)
        for k in range(0, 18, 3):           # hàng rào ghế đá
            self.put('BenchS', cx - 10 + k, cy - 14)

    def quarry(self, cx, cy):
        """Mỏ đá đông nam: cover dày, có lán thợ mỏ."""
        for x in range(cx - 13, cx + 13, 3):
            for y in range(cy - 11, cy + 11, 3):
                tx, ty = x + self.rng.randrange(2), y + self.rng.randrange(2)
                r = self.rng.random()
                if r < .35:
                    self.put('Rock07', tx, ty, flip=self.rng.random() < .5, natural=True)
                elif r < .55:
                    self.put('StoneCube', tx, ty, natural=True)
                elif r < .7:
                    self.put('Rock06', tx, ty, natural=True)
        self.house(cx - 4, cy - 3, 8, 6, [(cx, cy - 3)], 3)
        self.road(cx - 1, cy - 14, cx, cy - 4)

    def market(self, cx, cy):
        """Chợ phía nam: 3 nhà dọc phố + dãy ghế đá trước sạp."""
        self.road(cx - 18, cy - 1, cx + 18, cy + 1)
        self.house(cx - 18, cy + 3, 10, 8, [(cx - 13, cy + 3)], 5)
        self.house(cx + 8, cy + 3, 10, 8, [(cx + 13, cy + 3)], 5)
        self.house(cx - 5, cy - 11, 10, 8, [(cx, cy - 4), (cx - 5, cy - 8)], 5)
        for k in (-6, -3, 3, 6):
            self.put('BenchS', cx + k - 1, cy + 5)
        self.put('Well', cx - 1, cy + 7)
        self.put('StoneLantern', cx - 9, cy - 3)
        self.put('StoneLantern', cx + 9, cy - 3)

    def ruins(self, cx, cy):
        """Phế tích tây nam: lưới cột + cổng đá, chiến trường tầm trung; đồ do rải sàn đặt."""
        self.road(cx - 10, cy - 10, cx + 9, cy + 9)
        for gx in range(-9, 10, 4):
            for gy in range(-9, 10, 4):
                if abs(gx) <= 1 and abs(gy) <= 1:
                    continue
                r = self.rng.random()
                t = 'Pillar01' if r < .3 else 'Pillar02' if r < .5 else 'RunePillarBroken' if r < .65 else 'StoneCube' if r < .85 else 'Rock07'
                self.put(t, cx + gx, cy + gy)
        self.put('Gate02', cx - 1, cy + 3)
        self.put('Gate01', cx - 1, cy - 5)
        self.put('Statue01', cx - 3, cy)
        self.put('Statue01', cx + 2, cy, flip=True)

    def camp(self, cx, cy):
        """Trại thợ săn trong rừng phía tây + chòi canh."""
        self.house(cx - 4, cy + 2, 9, 7, [(cx, cy + 2), (cx + 4, cy + 5)], 4)
        self.put('BenchE', cx + 3, cy - 3)
        self.put('StoneLantern', cx + 5, cy - 1)
        self.put('SignE', cx - 6, cy)
        self.road(cx - 6, cy - 5, cx + 6, cy + 1)

    def stone_circle(self, cx, cy):
        """Vòng đá rune tây bắc: điểm nóng phụ có 1 thùng tiếp tế, trống trải → phải liều."""
        self.road(cx - 7, cy - 7, cx + 6, cy + 6)
        for k in range(12):
            a = k * math.pi / 6
            x, y = cx + round(7.5 * math.cos(a)) - 1, cy + round(7.5 * math.sin(a)) - 1
            self.put('RunePillarX3' if k % 3 else 'Statue01', x, y)
        for k in range(6):
            a = k * math.pi / 3 + .5
            self.put('StoneLantern', cx + round(4 * math.cos(a)) - 1, cy + round(4 * math.sin(a)) - 1)
        self.put('Supply', cx - 1, cy - 1)

    # ---------- thiên nhiên ----------
    def field(self, centers, sigma):
        """Trường mật độ mượt 0..1 (tổng các đốm Gauss) để rừng/bãi đá thành cụm thay vì nhiễu đều."""
        blobs = [(bx, by, self.rng.uniform(*sigma)) for bx, by in centers]

        def f(x, y):
            return min(1.0, sum(math.exp(-((x - bx) ** 2 + (y - by) ** 2) / (2 * s * s)) for bx, by, s in blobs))
        return f

    def nature(self):
        """Rải từng ô theo xác suất: cụm rừng (dày ở nửa tây), cụm đá (dày ở đông nam), đồng cỏ có bụi + đá lẻ. Không rải
        bình: đồ ngoài đồng do rải sàn đặt. Góc ngoài bo đầu là rừng rậm."""
        u = self.rng.uniform
        forest = self.field([(u(5, 65), u(5, 145)) for _ in range(9)] + [(u(5, 145), u(5, 145)) for _ in range(5)], (8, 14))
        rocks = self.field([(u(80, 145), u(5, 70)) for _ in range(5)] + [(u(5, 145), u(5, 145)) for _ in range(8)], (4, 8))
        # Quanh tường/lối vào chừa 2 ô thoáng: lối vào nhà rõ, tán cây không che tường.
        walls = [(p['cellX'], p['cellY']) for p in self.items if p['type'] == 'Wall'] + list(self.openings)
        building = {(x + i, y + j) for x, y in walls for i in range(-2, 3) for j in range(-2, 3)}
        cells = [(x, y) for x in range(N) for y in range(N) if (x, y) not in building]
        self.rng.shuffle(cells)
        for x, y in cells:
            r_c = math.hypot(x + .5 - C, y + .5 - C)
            if r_c > 70:            # góc ngoài bo đầu: rừng rậm, không loot
                trees, rock, bush, pot = .13, .03, .02, 0
            else:
                fo, ro = forest(x, y), rocks(x, y)
                trees = .008 + .26 * fo ** 1.5
                rock = .014 + .06 * ro
                bush = .013 + .012 * fo
                pot = .0055
            r = self.rng.random()
            if r < trees:
                # cây không đứng sát nhau theo hàng ngang (tán chồng xấu)
                if (x - 1, y) in self.occ or (x + 1, y) in self.occ:
                    continue
                self.put(self.rng.choice(TREES), x, y, flip=self.rng.random() < .5, natural=True)
            elif r < trees + rock:
                q = self.rng.random()
                t = 'Rock07' if q < .35 else 'Rock06' if q < .7 else 'StoneCube' if q < .85 else 'Rock05'
                self.put(t, x, y, flip=self.rng.random() < .5, natural=True)
            elif r < trees + rock + bush:
                self.put('Bush', x, y, natural=True)
            elif r < trees + rock + bush + pot:
                continue        # chỗ bình cũ: để trống (giữ chuỗi ngẫu nhiên nên rừng/đá không đổi)
            elif self.rng.random() < .006:
                self.put(self.rng.choice(['Flower', 'GrassTuft', 'Pebble']), x, y, natural=True)

    def portals(self, ends):
        for i, (x, y) in enumerate(ends):
            self.put('Portal', x - 1, y - 1, pairId=i // 2 + 1)
            self.reserve(x - 3, y - 3, x + 3, y + 2)


def polar(r, deg):
    a = math.radians(deg)
    return round(C + r * math.cos(a)), round(C + r * math.sin(a))


def ring_road(m, r, width=3):
    for x in range(N):
        for y in range(N):
            if abs(math.hypot(x + .5 - C, y + .5 - C) - r) <= width / 2:
                m.path.add((x, y))


def loot_value(m, x, y, r=25):
    val = {'Pot': 1, 'Chest': 3, 'Supply': 8}
    v = 0
    for p in m.items:
        k = p.get('loot') if p.get('destructible') else p['type']
        if k in val:
            w, h = FOOT.get(p['type'], (1, 1))
            if math.hypot(p['cellX'] + w / 2 - x - .5, p['cellY'] + h / 2 - y - .5) <= r:
                v += val[k]
    return v


def nearest(m, x, y, kinds):
    best = 999.0
    for p in m.items:
        k = p.get('loot') if p.get('destructible') else p['type']
        if k in kinds:
            best = min(best, math.hypot(p['cellX'] + .5 - x - .5, p['cellY'] + .5 - y - .5))
    return best


def build():
    m = Map(SEED)
    # Đường: vòng r=30 quanh đền (giữa đền và 8 khu) + 4 trục từ đền ra rìa bo.
    ring_road(m, 30)
    for deg in (0, 90, 180, 270):
        for t in range(18, 68):
            x, y = polar(t, deg)
            m.road(x - 1, y - 1, x + 1, y + 1)

    pois = {
        'N': polar(46, 90), 'NE': polar(47, 45), 'E': polar(47, 0), 'SE': polar(47, -45),
        'S': polar(46, -90), 'SW': polar(46, -135), 'W': polar(47, 180), 'NW': polar(46, 135),
    }
    m.temple(75, 75)
    m.village(*pois['N'])
    m.graveyard(*pois['NE'])
    m.farm(*pois['E'])
    m.quarry(*pois['SE'])
    m.market(*pois['S'])
    m.ruins(*pois['SW'])
    m.camp(*pois['W'])
    m.stone_circle(*pois['NW'])
    # 3 cặp portal đối diện qua tâm, nằm giữa 2 khu kề nhau (r=37): xoay vòng nhanh sang phía bên kia bo.
    m.portals([polar(37, 22.5), polar(37, 202.5), polar(37, 112.5), polar(37, 292.5), polar(37, 157.5), polar(37, 337.5)])

    # 10 spawn quanh vòng r≈57, mỗi điểm: sân 3x3 thoáng, rương gần nhất >= 9 ô, cách portal >= 6, cách nhau >= 20.
    spawn_cells = []
    for i in range(10):
        best = None
        for r in range(53, 64):
            for da in range(-10, 11, 2):
                x, y = polar(r, 18 + 36 * i + da)
                if not all(m.free('Spawn', x + a, y + b) for a in (-1, 0, 1) for b in (-1, 0, 1)):
                    continue
                if any(math.hypot(x - a, y - b) < 20 for a, b in spawn_cells):
                    continue
                chest = nearest(m, x, y, ('Chest', 'Supply'))
                portal = nearest(m, x, y, ('Portal',))
                if chest < 9 or portal < 6:
                    continue
                score = abs(r - 57) + abs(da) * .3
                if best is None or score < best[0]:
                    best = (score, x, y)
        assert best, i
        _, x, y = best
        m.reserve(x - 1, y - 1, x + 1, y + 1)
        spawn_cells.append((x, y))

    # Quanh spawn: 2 bụi trong 3–6 ô làm chỗ nấp. Đồ khởi đầu và công bằng giữa các spawn do rải sàn lo
    # (FloorLootPlanner: mỗi spawn có đồ trong 6–14 ô, ít nhất một vũ khí cận chiến hoặc thuốc).
    for (sx, sy) in spawn_cells:
        bushes = tries = 0
        while bushes < 2 and tries < 300:
            tries += 1
            a = m.rng.random() * 2 * math.pi
            d = 2.5 + m.rng.random() * 2.5
            x, y = round(sx + d * math.cos(a)), round(sy + d * math.sin(a))
            if m.put('Bush', x, y, natural=True):
                bushes += 1
    m.nature()

    spawns = []
    for (x, y) in spawn_cells:
        for c in [(x + i, y + j) for i in (-1, 0, 1) for j in (-1, 0, 1)]:
            m.reserved.discard(c)
        p = m.put('Spawn', x, y)
        assert p, (x, y)
        spawns.append(p)
    # Spawn đầu danh sách: game BFS từ spawn đầu, Luyện tập offline xuất phát ở spawn đầu (tây nam, giữa phế tích và chợ).
    order = sorted(range(10), key=lambda i: i != 6)
    items = [spawns[i] for i in order] + [p for p in m.items if p['group'] != 'Spawn']
    return m, items, spawn_cells, pois


def draft(m, items):
    return {
        'version': '1.1.0',
        'mapBounds': {'minX': 0.0, 'minY': 0.0, 'maxX': float(N), 'maxY': float(N)},
        'editor': {'tool': 'JungleMaze.LevelEditor', 'seed': SEED, 'decor': .06,
                   'path': [list(c) for c in sorted(m.path, key=lambda c: (c[1], c[0]))], 'placements': items},
    }


def report(m, items, spawns):
    from collections import Counter, deque
    kinds = Counter()
    loot = []
    cover = []
    for p in items:
        t = p['type']
        k = p.get('loot') if p.get('destructible') else t
        kinds[k] += 1
        w, h = FOOT.get(t, (1, 1))
        cx, cy = p['cellX'] + w / 2, p['cellY'] + h / 2
        if k in ('Pot', 'Chest', 'Supply'):
            loot.append((k, cx, cy))
        if t in REAL_COVER:
            for i in range(w):
                for j in range(h):
                    cover.append((p['cellX'] + i, p['cellY'] + j))
    print('Vật:', dict(kinds))
    val = {'Pot': 1, 'Chest': 3, 'Supply': 8}
    print('Spawn  pot<=6  chest<=8  gầnRương  giáTrị<=25  (đồ sàn không tính)')
    vs = []
    for (x, y) in spawns:
        def within(kind, r):
            return sum(1 for k, a, b in loot if k == kind and math.hypot(a - x - .5, b - y - .5) <= r)
        near = min(math.hypot(a - x - .5, b - y - .5) for k, a, b in loot if k == 'Chest')
        v = sum(val[k] for k, a, b in loot if math.hypot(a - x - .5, b - y - .5) <= 25)
        vs.append(v)
        print(f'({x:3},{y:3})  {within("Pot", 6):5}  {within("Chest", 8):8}  {near:7.1f}  {v:6}')
    print(f'giá trị loot quanh spawn: min {min(vs)} max {max(vs)} tỉ lệ {max(vs) / max(min(vs), 1):.2f}')
    # khoảng cách tới cover thật (BFS đa nguồn, Chebyshev) trên ô trong bo đầu
    dist = {}
    q = deque()
    for c in cover:
        if c not in dist:
            dist[c] = 0
            q.append(c)
    while q:
        x, y = q.popleft()
        for dx in (-1, 0, 1):
            for dy in (-1, 0, 1):
                n = (x + dx, y + dy)
                if 0 <= n[0] < N and 0 <= n[1] < N and n not in dist:
                    dist[n] = dist[(x, y)] + 1
                    q.append(n)
    ds = sorted(dist[(x, y)] for x in range(N) for y in range(N) if math.hypot(x + .5 - C, y + .5 - C) <= 72 and (x, y) not in m.occ)
    pct = lambda p: ds[int(p * (len(ds) - 1))]
    print(f'cách cover thật (ô): p50 {pct(.5)} p90 {pct(.9)} p99 {pct(.99)} max {ds[-1]}; >6 ô: {sum(d > 6 for d in ds) / len(ds):.1%}')


def render(m, items, path):
    from PIL import Image, ImageDraw
    col = {'Wall': (120, 110, 100), 'Bush': (90, 200, 90), 'Chest': (255, 200, 0), 'Pot': (255, 140, 60),
           'Supply': (255, 0, 255), 'Door': (160, 90, 30), 'Portal': (0, 200, 255), 'Spawn': (255, 0, 0)}
    k = 6
    im = Image.new('RGB', (N * k, N * k), (110, 160, 80))
    dr = ImageDraw.Draw(im)
    dr.ellipse([(C - 75) * k, (C - 75) * k, (C + 75) * k, (C + 75) * k], outline=(255, 255, 255))
    for x, y in m.path:
        dr.rectangle([x * k, (N - 1 - y) * k, x * k + k - 1, (N - 1 - y) * k + k - 1], fill=(190, 180, 150))
    for p in items:
        t = p['type']
        if p['group'] == 'Decoration':
            continue
        w, h = FOOT.get(t, (1, 1))
        x, y = p['cellX'], p['cellY']
        c = col.get(t, (20, 100, 30) if t in TREES else (150, 150, 160))
        if p.get('destructible'):
            c = (230, 120, 170) if p['loot'] == 'Chest' else (230, 170, 120)
        dr.rectangle([x * k, (N - y - h) * k, (x + w) * k - 1, (N - y) * k - 1], fill=c)
    im.save(path)


if __name__ == '__main__':
    m, items, spawns, pois = build()
    out = sys.argv[1]
    with open(out, 'w', encoding='utf-8', newline='\n') as f:
        json.dump(draft(m, items), f, ensure_ascii=False)
    if '--png' in sys.argv:
        render(m, items, sys.argv[sys.argv.index('--png') + 1])
    if '--report' in sys.argv:
        report(m, items, spawns)
