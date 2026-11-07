#nullable disable
using System;
using System.Collections.Generic;
using JungleMaze.World.Inventory;

namespace JungleMaze.World.Loot
{
    /// <summary>A fixed floor-loot location of one map.</summary>
    public struct FloorLootSpot
    {
        public WorldPoint Position;
        /// <summary>3 indoor (cover within IndoorProbe in at least 7 of 8 directions), 2 next to cover, 1 open ground.</summary>
        public byte Score;
        public LootTier Tier;
        /// <summary>Added only to satisfy spawn fairness (beyond the area target).</summary>
        public bool Fairness;
        public bool Indoor => Score == 3;
    }

    /// <summary>
    /// Floor-loot locations of one map, deterministic from geometry alone. Immutable; plan once per map and roll every match.
    /// </summary>
    public sealed class FloorLootSpots
    {
        internal FloorLootSpot[] SpotArray;
        internal int[][] Rings;
        public MapLayout Layout { get; internal set; }
        public LootSurface Surface { get; internal set; }
        public IReadOnlyList<FloorLootSpot> Spots => SpotArray;
        /// <summary>Spot indices NearSpawnMin..NearSpawnMax from each spawn, nearest first.</summary>
        public IReadOnlyList<int> NearSpawn(int spawn) => Rings[spawn];
        /// <summary>Reachable walkable cells inside the start zone.</summary>
        public int WalkableCells { get; internal set; }
        public int Candidates { get; internal set; }
        /// <summary>Candidate cells by score: [1] open, [2] next to cover, [3] indoor.</summary>
        public int[] CandidateScores { get; internal set; }
        /// <summary>Spots replaced by pre-placed containers (Chest/Pot/SupplySpots).</summary>
        public float ContainerSpots { get; internal set; }
        /// <summary>Area-based spot count before the object budget and fairness.</summary>
        public int Target { get; internal set; }
        /// <summary>Most floor objects (items + ammo) the initial plan may create.</summary>
        public int Budget { get; internal set; }
        /// <summary>Worst-case objects container breaks can add later (reserved on top of ReserveObjects).</summary>
        public int ContainerReserve { get; internal set; }
        public float ZoneRadius { get; internal set; }
        public double PlanMilliseconds { get; internal set; }
    }

    /// <summary>What one match puts on the floor.</summary>
    public sealed class FloorLootPlan
    {
        public readonly List<LootPlacement> Placements = new List<LootPlacement>();
        public int FilledSpots;
        public int Guns;
        public FloorLootSpots Spots;
    }

    /// <summary>
    /// Floor loot (Docs/hvu/07_Floor_Loot.vi.md sections 2-4). <see cref="PlanSpots"/> depends only on the map and profile;
    /// <see cref="Roll"/> picks items for a match seed. Same inputs give the same result on server and client.
    /// </summary>
    public static class FloorLootPlanner
    {
        /// <summary>Must equal JungleBackend.Protocol.Gameplay.WorldWire.MaxObjects (checked by backend tests).</summary>
        public const int MaxWorldObjects = 4096;
        /// <summary>Floor registry capacity of MatchWorldRuntime/OfflineWorldSession.</summary>
        public const int MaxFloorItems = 2048;

        /// <summary>Spots and items in one call (offline practice). Servers should cache <see cref="PlanSpots"/> per map.</summary>
        /// <param name="zoneRadius">Start zone radius around the map center; 0 or less = Size / 2 (authored maps).</param>
        public static FloorLootPlan Plan(MapLayout layout, LootTable table, uint matchSeed, float zoneRadius = 0, float heroRadius = .4f) =>
            Roll(PlanSpots(layout, table, zoneRadius, heroRadius), table, matchSeed);

        public static FloorLootSpots PlanSpots(MapLayout layout, LootTable table, float zoneRadius = 0, float heroRadius = .4f)
        {
            if (layout == null || table == null) throw new ArgumentNullException();
            if (float.IsNaN(heroRadius) || heroRadius <= 0 || heroRadius > .5f) throw new ArgumentException("Hero radius must be in (0, 0.5].");
            var started = System.Diagnostics.Stopwatch.GetTimestamp();
            var p = table.FloorProfile; var n = layout.Size; var cells = n * n;
            var center = new WorldPoint(n / 2f, n / 2f);
            var zone = zoneRadius > 0 && !float.IsInfinity(zoneRadius) ? zoneRadius : n / 2f;
            var result = new FloorLootSpots { Layout = layout, Surface = new LootSurface(layout, p.DropClearance), ZoneRadius = zone };

            // 1. Rasterize once: hero-walkable cells (doors open, intact containers block, as MapLayoutGenerator.Validate),
            //    cells to avoid (near portals/doors/containers, hazards, bushes) and solid cells for the indoor/cover score.
            var blocked = new bool[cells]; var avoid = new bool[cells]; var solid = new bool[cells];
            for (var i = 0; i < cells; i++)
            {
                float x = i % n + .5f, y = i / n + .5f;
                blocked[i] = x < heroRadius || y < heroRadius || x > n - heroRadius || y > n - heroRadius;
            }
            float chests = 0, pots = 0, supplies = 0;
            foreach (var o in layout.Objects)
            {
                var b = o.Bounds;
                if (o.Kind == WorldObjectKind.Chest) chests++; else if (o.Kind == WorldObjectKind.Pot) pots++; else if (o.Kind == WorldObjectKind.Supply) supplies++;
                if (o.IsHardCover || o.IsDestructible) Mark(blocked, n, b, heroRadius, c => b.IntersectsCircle(c, heroRadius));
                if (o.IsDestructible || o.Kind == WorldObjectKind.Door || o.Kind == WorldObjectKind.Portal) Mark(avoid, n, b, p.ObjectClearance, c => b.IntersectsCircle(c, p.ObjectClearance));
                else if (o.IsHazard) Mark(avoid, n, b, heroRadius, c => b.IntersectsCircle(c, heroRadius));
                else if (o.Kind == WorldObjectKind.Bush) Mark(avoid, n, b, 0, c => b.Contains(c));
                if (o.IsHardCover || o.Kind == WorldObjectKind.Door) MarkOverlap(solid, n, b);
            }

            // 2. Reachable from any spawn.
            var reach = new bool[cells]; var queue = new int[cells]; int head = 0, tail = 0;
            foreach (var s in layout.Spawns)
            {
                int sx = (int)s.X, sy = (int)s.Y; if (sx < 0 || sy < 0 || sx >= n || sy >= n) continue;
                var c = sx + sy * n; if (blocked[c] || reach[c]) continue; reach[c] = true; queue[tail++] = c;
            }
            while (head < tail)
            {
                var c = queue[head++]; int x = c % n, y = c / n;
                if (x > 0) Visit(c - 1); if (x < n - 1) Visit(c + 1); if (y > 0) Visit(c - n); if (y < n - 1) Visit(c + n);
            }
            void Visit(int c) { if (!blocked[c] && !reach[c]) { reach[c] = true; queue[tail++] = c; } }

            // 3. Prefix sums of solid cells for the O(1) "cover within CoverProbe" box test.
            var sum = new int[(n + 1) * (n + 1)];
            for (var y = 0; y < n; y++) for (var x = 0; x < n; x++)
                sum[(x + 1) + (y + 1) * (n + 1)] = (solid[x + y * n] ? 1 : 0) + sum[x + (y + 1) * (n + 1)] + sum[(x + 1) + y * (n + 1)] - sum[x + y * (n + 1)];

            // 4. Candidates ordered by score (indoor, cover, open), ties by a fixed per-cell hash so spots do not cluster in scan order.
            var keys = new List<ulong>(); var scores = new int[4]; var walkable = 0; var zone2 = zone * zone; var spawn2 = p.SpawnClearance * p.SpawnClearance;
            for (var c = 0; c < cells; c++)
            {
                if (!reach[c]) continue;
                int x = c % n, y = c / n; var point = new WorldPoint(x + .5f, y + .5f);
                if (point.DistanceSquared(center) > zone2) continue;
                walkable++;
                if (avoid[c]) continue;
                var nearSpawn = false; foreach (var s in layout.Spawns) if (s.DistanceSquared(point) < spawn2) { nearSpawn = true; break; }
                if (nearSpawn) continue;
                var score = Score(solid, sum, n, x, y, p);
                scores[score]++;
                keys.Add(((ulong)(3 - score) << 62) | ((ulong)LootSeeds.Mix((uint)x, (uint)y) << 30) | (uint)c);
            }
            keys.Sort();
            result.WalkableCells = walkable; result.Candidates = keys.Count; result.CandidateScores = scores;

            // 5. Count: area target minus pre-placed containers, capped by the world object budget.
            result.ContainerSpots = chests * p.ChestSpots + pots * p.PotSpots + supplies * p.SupplySpots;
            result.Target = Math.Max(0, (int)Math.Floor(walkable / p.AreaPerSpot - result.ContainerSpots));
            result.ContainerReserve = (int)chests * table.MaxRoll(LootSource.Chest) + (int)pots * table.MaxRoll(LootSource.Pot) + (int)supplies * table.MaxRoll(LootSource.Supply);
            var room = Math.Min(MaxWorldObjects - layout.Objects.Count, MaxFloorItems) - p.ReserveObjects - result.ContainerReserve;
            result.Budget = p.Enabled ? Math.Max(0, room) : 0;
            var cap = Math.Min(result.Target, result.Budget);

            // 6. Greedy Poisson selection: indoor spots IndoorSpacing apart, others OutdoorSpacing.
            var taken = new bool[cells]; var spots = new List<FloorLootSpot>(); var spotCells = new List<int>();
            for (var k = 0; k < keys.Count && spots.Count < cap; k++)
            {
                var c = (int)(keys[k] & 0x3fffffff); var score = (byte)(3 - (int)(keys[k] >> 62));
                if (!Clear(taken, n, c, score == 3 ? p.IndoorSpacing : p.OutdoorSpacing)) continue;
                Take(c, score, false);
            }

            // 7. Spawn fairness: at least MinNearSpawn spots NearSpawnMin..NearSpawnMax from each spawn (relaxed spacing).
            var min2 = p.NearSpawnMin * p.NearSpawnMin; var max2 = p.NearSpawnMax * p.NearSpawnMax;
            foreach (var s in layout.Spawns)
            {
                var near = 0; foreach (var c in spotCells) if (InRing(c)) near++;
                for (var k = 0; k < keys.Count && near < p.MinNearSpawn && spots.Count < result.Budget; k++)
                {
                    var c = (int)(keys[k] & 0x3fffffff);
                    if (taken[c] || !InRing(c) || !Clear(taken, n, c, p.IndoorSpacing)) continue;
                    Take(c, (byte)(3 - (int)(keys[k] >> 62)), true); near++;
                }
                bool InRing(int c) { var d = new WorldPoint(c % n + .5f, c / n + .5f).DistanceSquared(s); return d >= min2 && d <= max2; }
            }
            result.SpotArray = spots.ToArray();
            result.Rings = new int[layout.Spawns.Count][];
            for (var i = 0; i < layout.Spawns.Count; i++)
            {
                var s = layout.Spawns[i]; var ring = new List<int>();
                for (var j = 0; j < spots.Count; j++) { var d = spots[j].Position.DistanceSquared(s); if (d >= min2 && d <= max2) ring.Add(j); }
                ring.Sort((a, b) => { var c = spots[a].Position.DistanceSquared(s).CompareTo(spots[b].Position.DistanceSquared(s)); return c != 0 ? c : a.CompareTo(b); });
                result.Rings[i] = ring.ToArray();
            }
            result.PlanMilliseconds = (System.Diagnostics.Stopwatch.GetTimestamp() - started) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
            return result;

            void Take(int c, byte score, bool fairness)
            {
                taken[c] = true; spotCells.Add(c);
                var point = new WorldPoint(c % n + .5f, c / n + .5f); var r = (float)Math.Sqrt(point.DistanceSquared(center));
                var tier = r < p.InnerTierFraction * zone ? LootTier.Inner : r > p.OuterTierFraction * zone ? LootTier.Outer : LootTier.Middle;
                spots.Add(new FloorLootSpot { Position = point, Score = score, Tier = tier, Fairness = fairness });
            }
        }

        /// <summary>Items for one match. Deterministic for (spots, table, matchSeed); never exceeds <see cref="FloorLootSpots.Budget"/> objects.</summary>
        public static FloorLootPlan Roll(FloorLootSpots spots, LootTable table, uint matchSeed)
        {
            if (spots == null || table == null) throw new ArgumentNullException();
            var plan = new FloorLootPlan { Spots = spots };
            var p = table.FloorProfile; var list = spots.SpotArray; var count = list.Length;
            if (!p.Enabled || count == 0) return plan;
            var rng = new ItemRandom(LootSeeds.Mix(matchSeed, 0xf100du));
            var has = new bool[count]; var item = new ItemDefinition[count]; var ammo = new WorldPoint?[count];
            var chance = (int)Math.Round(p.SpotChance * 10000);
            for (var i = 0; i < count; i++) has[i] = rng.Next(10000) < chance;
            foreach (var ring in spots.Rings)
            {
                var filled = 0; foreach (var i in ring) if (has[i]) filled++;
                for (var k = 0; k < ring.Length && filled < p.MinNearSpawn; k++) if (!has[ring[k]]) { has[ring[k]] = true; filled++; }
            }
            var remaining = 0; foreach (var h in has) if (h) remaining++;
            var objects = 0;
            for (var i = 0; i < count; i++)
            {
                if (!has[i]) continue;
                remaining--;
                // A gun costs two objects; allow it only while every remaining filled spot still fits one object.
                var allowGuns = objects + 2 + remaining <= spots.Budget;
                var picked = table.SampleFloor(list[i].Tier, allowGuns, rng);
                if (LootTable.IsGun(picked))
                {
                    if (LootDrops.TryAmmoPoint(table, list[i].Position, LootSeeds.Mix(matchSeed, (uint)i + 1), spots.Surface, out var point)) ammo[i] = point;
                    else picked = table.SampleFloor(list[i].Tier, false, rng);
                }
                if (picked == null) { has[i] = false; continue; }
                item[i] = picked; objects += ammo[i].HasValue ? 2 : 1;
            }
            // Each spawn's ring must offer a melee weapon or a heal: convert the nearest filled spot if none does.
            foreach (var ring in spots.Rings)
            {
                var first = -1; var ok = false;
                foreach (var i in ring) if (has[i]) { if (first < 0) first = i; if (LootTable.IsStarter(item[i])) { ok = true; break; } }
                if (ok || first < 0) continue;
                var starter = table.SampleStarter(list[first].Tier, rng);
                if (starter == null) continue;
                item[first] = starter; ammo[first] = null;
            }
            for (var i = 0; i < count; i++)
            {
                if (!has[i]) continue;
                plan.FilledSpots++;
                plan.Placements.Add(new LootPlacement(Stack(table, item[i]), list[i].Position, i));
                if (ammo[i].HasValue) { plan.Guns++; plan.Placements.Add(new LootPlacement(LootDrops.AmmoStack(table), ammo[i].Value, i, true)); }
            }
            return plan;
        }

        /// <summary>A lone AMMO spot is one box stack like a gun companion; everything else is a single item as LootTable.Roll makes it.</summary>
        private static ItemStack Stack(LootTable table, ItemDefinition item) =>
            item.Kind == ItemKind.Ammo ? LootDrops.AmmoStack(table) : new ItemStack(item.Id, 1, item.Durability);

        private static byte Score(bool[] solid, int[] sum, int n, int x, int y, FloorLootProfile p)
        {
            // Walls of a house close almost every direction; scattered trees (also Wall after import) rarely close 7 of 8.
            var sides = 0;
            for (var d = 0; d < 8; d++) if (Ray(solid, n, x, y, Dx[d], Dy[d], p.IndoorProbe)) sides++;
            if (sides >= 7) return 3;
            int r = p.CoverProbe, x0 = Math.Max(0, x - r), y0 = Math.Max(0, y - r), x1 = Math.Min(n - 1, x + r) + 1, y1 = Math.Min(n - 1, y + r) + 1;
            var w = n + 1; var cover = sum[x1 + y1 * w] - sum[x0 + y1 * w] - sum[x1 + y0 * w] + sum[x0 + y0 * w];
            return (byte)(cover > 0 ? 2 : 1);
        }

        private static readonly int[] Dx = { 1, 1, 0, -1, -1, -1, 0, 1 }, Dy = { 0, 1, 1, 1, 0, -1, -1, -1 };

        private static bool Ray(bool[] solid, int n, int x, int y, int dx, int dy, int length)
        {
            for (var k = 1; k <= length; k++)
            {
                int cx = x + dx * k, cy = y + dy * k;
                if (cx < 0 || cy < 0 || cx >= n || cy >= n) return true; // the map edge encloses like a wall
                if (solid[cx + cy * n]) return true;
            }
            return false;
        }

        private static bool Clear(bool[] taken, int n, int c, float spacing)
        {
            int x = c % n, y = c / n, r = (int)Math.Ceiling(spacing); var limit = spacing * spacing;
            for (var cy = Math.Max(0, y - r); cy <= Math.Min(n - 1, y + r); cy++)
                for (var cx = Math.Max(0, x - r); cx <= Math.Min(n - 1, x + r); cx++)
                    if (taken[cx + cy * n] && (cx - x) * (cx - x) + (cy - y) * (cy - y) < limit) return false;
            return true;
        }

        /// <summary>Marks cells whose center passes <paramref name="test"/>, visiting only cells within <paramref name="margin"/> of the bounds.</summary>
        private static void Mark(bool[] grid, int n, WorldBounds b, float margin, Func<WorldPoint, bool> test)
        {
            int x0 = Math.Max(0, (int)Math.Floor(b.MinX - margin - .5f)), x1 = Math.Min(n - 1, (int)Math.Ceiling(b.MaxX + margin - .5f));
            int y0 = Math.Max(0, (int)Math.Floor(b.MinY - margin - .5f)), y1 = Math.Min(n - 1, (int)Math.Ceiling(b.MaxY + margin - .5f));
            for (var y = y0; y <= y1; y++) for (var x = x0; x <= x1; x++) { var i = x + y * n; if (!grid[i] && test(new WorldPoint(x + .5f, y + .5f))) grid[i] = true; }
        }

        private static void MarkOverlap(bool[] grid, int n, WorldBounds b)
        {
            int x0 = Math.Max(0, (int)Math.Floor(b.MinX)), x1 = Math.Min(n - 1, (int)Math.Ceiling(b.MaxX) - 1);
            int y0 = Math.Max(0, (int)Math.Floor(b.MinY)), y1 = Math.Min(n - 1, (int)Math.Ceiling(b.MaxY) - 1);
            for (var y = y0; y <= y1; y++) for (var x = x0; x <= x1; x++) grid[x + y * n] = true;
        }
    }
}
