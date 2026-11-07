#nullable disable
using System;
using System.Collections.Generic;
using JungleMaze.World.Inventory;

namespace JungleMaze.World.Loot
{
    /// <summary>One floor item to create: what and where. <see cref="Spot"/> is the floor spot index, -1 for container drops.</summary>
    public struct LootPlacement
    {
        public ItemStack Stack;
        public WorldPoint Position;
        public int Spot;
        /// <summary>True for the AMMO box that accompanies a gun.</summary>
        public bool Companion;
        public LootPlacement(ItemStack stack, WorldPoint position, int spot = -1, bool companion = false)
        { Stack = stack; Position = position; Spot = spot; Companion = companion; }
    }

    /// <summary>Stable seed helpers shared by server and offline practice.</summary>
    public static class LootSeeds
    {
        /// <summary>Murmur3 finalizer of a combined pair; never returns 0.</summary>
        public static uint Mix(uint a, uint b)
        {
            var h = a ^ (b * 0x9e3779b9u + 0x7f4a7c15u + (a << 6) + (a >> 2));
            h ^= h >> 16; h *= 0x85ebca6bu; h ^= h >> 13; h *= 0xc2b2ae35u; h ^= h >> 16;
            return h == 0 ? 0x6d2b79f5u : h;
        }
        /// <summary>Per-match seed from the match identity (server). Offline practice can pass any fresh value instead.</summary>
        public static uint FromGuid(Guid id)
        {
            var bytes = id.ToByteArray(); var h = 2166136261u;
            foreach (var b in bytes) { h ^= b; h *= 16777619u; }
            return Mix(h, 0x10071u);
        }
    }

    /// <summary>
    /// Fast "can an item lie here" test: inside the map, clear of hard cover, intact containers, doors, portals and hazards
    /// by <see cref="Clearance"/>. Objects are bucketed per cell once, so a query only checks objects near the point.
    /// Immutable after construction; safe to share between threads when <c>broken</c> is null or thread-safe.
    /// </summary>
    public sealed class LootSurface
    {
        private readonly WorldObjectDefinition[] _objects;
        private readonly int[] _start, _index;
        private readonly Func<ushort, bool> _broken;
        public int Size { get; }
        public float Clearance { get; }

        /// <param name="broken">Destructible state; a broken container no longer blocks. Null = every container blocks.</param>
        public LootSurface(MapLayout layout, float clearance = .3f, Func<ushort, bool> broken = null)
        {
            if (layout == null) throw new ArgumentNullException(nameof(layout));
            if (float.IsNaN(clearance) || clearance <= 0 || clearance > .5f) throw new ArgumentException("Loot clearance must be in (0, 0.5].");
            Size = layout.Size; Clearance = clearance; _broken = broken;
            var relevant = new List<WorldObjectDefinition>();
            foreach (var o in layout.Objects) if (Blocks(o)) relevant.Add(o);
            _objects = relevant.ToArray();
            var cells = Size * Size; _start = new int[cells + 1];
            for (var pass = 0; pass < 2; pass++)
            {
                int[] fill = pass == 1 ? new int[cells] : null;
                for (var n = 0; n < _objects.Length; n++)
                {
                    var b = _objects[n].Bounds;
                    // Expand by half a cell (>= clearance) so the point's own cell lists every object within the clearance.
                    int x0 = Clamp((int)Math.Floor(b.MinX - .5f)), x1 = Clamp((int)Math.Floor(b.MaxX + .5f));
                    int y0 = Clamp((int)Math.Floor(b.MinY - .5f)), y1 = Clamp((int)Math.Floor(b.MaxY + .5f));
                    for (var y = y0; y <= y1; y++)
                        for (var x = x0; x <= x1; x++)
                        {
                            var cell = x + y * Size;
                            if (pass == 0) _start[cell + 1]++; else _index[_start[cell] + fill[cell]++] = n;
                        }
                }
                if (pass == 0) { for (var i = 0; i < cells; i++) _start[i + 1] += _start[i]; _index = new int[_start[cells]]; }
            }
        }

        private static bool Blocks(WorldObjectDefinition o) =>
            o.IsHardCover || o.IsDestructible || o.IsHazard || o.Kind == WorldObjectKind.Door || o.Kind == WorldObjectKind.Portal;
        private int Clamp(int v) => v < 0 ? 0 : v >= Size ? Size - 1 : v;

        public bool IsFree(WorldPoint p)
        {
            if (!WorldMath.Finite(p.X) || !WorldMath.Finite(p.Y) || p.X < Clearance || p.Y < Clearance || p.X > Size - Clearance || p.Y > Size - Clearance) return false;
            var cell = Clamp((int)p.X) + Clamp((int)p.Y) * Size;
            for (var i = _start[cell]; i < _start[cell + 1]; i++)
            {
                var o = _objects[_index[i]];
                if (o.IsDestructible && _broken != null && _broken(o.Id)) continue;
                if (o.Bounds.IntersectsCircle(p, Clearance)) return false;
            }
            return true;
        }
    }
}
