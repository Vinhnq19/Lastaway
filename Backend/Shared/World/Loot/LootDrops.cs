#nullable disable
using System;
using System.Collections.Generic;
using JungleMaze.World.Inventory;

namespace JungleMaze.World.Loot
{
    /// <summary>
    /// Container drops and gun ammo companions, shared by GameServer and offline practice so both place loot identically.
    /// A gun with a magazine always brings <see cref="FloorLootProfile.AmmoBoxes"/> AMMO in one stack next to it.
    /// </summary>
    public static class LootDrops
    {
        public const string AmmoId = "AMMO";
        private const float MinSeparation = .45f;

        public static LootSource SourceOf(WorldObjectKind kind) =>
            kind == WorldObjectKind.Chest ? LootSource.Chest : kind == WorldObjectKind.Pot ? LootSource.Pot : LootSource.Supply;

        /// <summary>
        /// Loot table a broken object rolls. Only the dragon egg (an airdrop) rolls the supply table; a supply crate placed
        /// on the map (the wooden box, "Thùng tiếp tế") rolls like a chest, so it tops out at Rare too (08/11).
        /// </summary>
        public static WorldObjectKind LootKind(WorldObjectKind kind, bool dragonEgg) =>
            kind == WorldObjectKind.Supply && !dragonEgg ? WorldObjectKind.Chest : kind;

        /// <summary>True when this stack needs an AMMO companion (an enabled gun and AMMO is in the catalog).</summary>
        public static bool NeedsAmmo(LootTable table, ItemStack stack)
        {
            if (table == null) throw new ArgumentNullException(nameof(table));
            return !stack.IsEmpty && table.Catalog.TryGet(stack.ItemId, out var item) && LootTable.IsGun(item) && table.Catalog.TryGet(AmmoId, out var ammo) && ammo.Enabled;
        }

        public static ItemStack AmmoStack(LootTable table) => new ItemStack(AmmoId, table.FloorProfile.AmmoBoxes);

        /// <summary>World objects a set of stacks will occupy once ammo companions are added.</summary>
        public static int CountObjects(LootTable table, ItemStack[] items, bool includeAmmo = true)
        {
            if (items == null) return 0;
            var count = items.Length;
            foreach (var item in items) if (includeAmmo && NeedsAmmo(table, item)) count++;
            return count;
        }

        /// <summary>Exact object count of <see cref="ForContainer"/> for the same arguments (use for CanSpawnLoot).</summary>
        public static int CountForContainer(LootTable table, WorldObjectKind kind, uint seed) =>
            CountObjects(table, table.Roll(SourceOf(kind), seed), kind != WorldObjectKind.Supply);

        /// <summary>Roll a broken container's loot and fan it out around <paramref name="center"/>, with ammo next to guns.</summary>
        public static List<LootPlacement> ForContainer(LootTable table, WorldObjectKind kind, WorldPoint center, uint seed, LootSurface surface)
        {
            if (table == null) throw new ArgumentNullException(nameof(table));
            var output = new List<LootPlacement>();
            Spread(table, table.Roll(SourceOf(kind), seed), center, seed, surface, output, kind != WorldObjectKind.Supply);
            return output;
        }

        /// <summary>
        /// Fan <paramref name="items"/> out ScatterMin..ScatterMax around the center: even angles with a seeded offset and jitter.
        /// A blocked point rotates around the circle, then pulls toward the center, and finally uses the center itself.
        /// </summary>
        public static void Spread(LootTable table, ItemStack[] items, WorldPoint center, uint seed, LootSurface surface, List<LootPlacement> output, bool includeAmmo = true)
        {
            if (table == null || surface == null || output == null) throw new ArgumentNullException();
            if (items == null || items.Length == 0) return;
            var p = table.FloorProfile; var rng = new ItemRandom(LootSeeds.Mix(seed, 0x51ca77e5u));
            var first = output.Count; var step = Math.PI * 2 / items.Length; var start = rng.NextDouble() * Math.PI * 2;
            for (var i = 0; i < items.Length; i++)
            {
                var angle = start + i * step + (rng.NextDouble() - .5) * step * .4;
                var radius = p.ScatterMinRadius + (p.ScatterMaxRadius - p.ScatterMinRadius) * rng.NextDouble();
                output.Add(new LootPlacement(items[i], Ring(center, angle, radius, p.ScatterMinRadius, p.ScatterMaxRadius, surface, output, first)));
            }
            var count = output.Count;
            for (var i = first; i < count; i++)
            {
                if (!includeAmmo || !NeedsAmmo(table, output[i].Stack)) continue;
                var ammo = TryAmmoPoint(table, output[i].Position, LootSeeds.Mix(seed, (uint)i + 1), surface, output, first, out var point) ? point : output[i].Position;
                output.Add(new LootPlacement(AmmoStack(table), ammo, -1, true));
            }
        }

        /// <summary>A free point AmmoMin..AmmoMax from the gun, preferring the middle distance and a seeded direction.</summary>
        public static bool TryAmmoPoint(LootTable table, WorldPoint gun, uint seed, LootSurface surface, out WorldPoint point) =>
            TryAmmoPoint(table, gun, seed, surface, null, 0, out point);

        internal static bool TryAmmoPoint(LootTable table, WorldPoint gun, uint seed, LootSurface surface, List<LootPlacement> avoid, int avoidFrom, out WorldPoint point)
        {
            var p = table.FloorProfile; var mid = (p.AmmoMinDistance + p.AmmoMaxDistance) / 2;
            var radii = new[] { mid, p.AmmoMinDistance, p.AmmoMaxDistance };
            var start = new ItemRandom(LootSeeds.Mix(seed, 0xa3303u)).NextDouble() * Math.PI * 2;
            for (var pass = 0; pass < 2; pass++)
                foreach (var radius in radii)
                    for (var k = 0; k < 12; k++)
                    {
                        var a = start + k * Math.PI / 6;
                        var candidate = new WorldPoint(gun.X + (float)(Math.Cos(a) * radius), gun.Y + (float)(Math.Sin(a) * radius));
                        // First pass also keeps clear of the other drops; the second accepts touching them.
                        if (surface.IsFree(candidate) && (pass == 1 || Separated(candidate, avoid, avoidFrom))) { point = candidate; return true; }
                    }
            point = gun; return false;
        }

        private static WorldPoint Ring(WorldPoint center, double angle, double radius, float min, float max, LootSurface surface, List<LootPlacement> placed, int from)
        {
            var radii = new[] { radius, min, max, (min + max) / 2.0 };
            for (var pass = 0; pass < 2; pass++)
                for (var k = 0; k < 16; k++)
                {
                    // 0, +22.5, -22.5, +45, ... degrees around the intended direction.
                    var offset = (k + 1) / 2 * (Math.PI / 8) * (k % 2 == 1 ? 1 : -1);
                    foreach (var r in radii)
                    {
                        var p = new WorldPoint(center.X + (float)(Math.Cos(angle + offset) * r), center.Y + (float)(Math.Sin(angle + offset) * r));
                        if (surface.IsFree(p) && (pass == 1 || Separated(p, placed, from))) return p;
                    }
                }
            for (var t = 1; t <= 6; t++)
            {
                var r = radius * (1 - t / 7.0);
                var p = new WorldPoint(center.X + (float)(Math.Cos(angle) * r), center.Y + (float)(Math.Sin(angle) * r));
                if (surface.IsFree(p)) return p;
            }
            return center;
        }

        private static bool Separated(WorldPoint p, List<LootPlacement> placed, int from)
        {
            if (placed == null) return true;
            for (var i = from; i < placed.Count; i++) if (placed[i].Position.DistanceSquared(p) < MinSeparation * MinSeparation) return false;
            return true;
        }
    }
}
