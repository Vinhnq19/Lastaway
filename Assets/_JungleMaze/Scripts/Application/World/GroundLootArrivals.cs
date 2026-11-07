using System.Collections.Generic;
using JungleBackend.Protocol.Gameplay;

namespace JungleMaze.App.World
{
    /// <summary>How a ground item enters the screen.</summary>
    public enum GroundLootEntry : byte
    {
        /// <summary>Already there when the world was first seen, dropped by a player, or anything else: short fade-in.</summary>
        Appear,
        /// <summary>Burst out of a container that just broke nearby: flies from the container center.</summary>
        Launch
    }

    /// <summary>One ground item (WorldObjectWire kind 100) that was not in the previous snapshot.</summary>
    public struct GroundLootArrival
    {
        public ushort Id;
        public GroundLootEntry Entry;
        /// <summary>Launch origin (center of the broken container); the item position for <see cref="GroundLootEntry.Appear"/>.</summary>
        public float FromX, FromY;
        /// <summary>Seconds to wait before the flight starts (stagger between items of one container).</summary>
        public float Delay;
    }

    /// <summary>
    /// Infers the "loot bursts out of a broken container" animation from world snapshots alone (Docs/hvu/07_Floor_Loot.vi.md
    /// section 5), so offline practice and online matches behave the same without a protocol change. An item that is new in a
    /// snapshot taken at most <see cref="Window"/> seconds after a chest, pot or supply crate within <see cref="Radius"/> cells
    /// switched to broken launches from that container; each further item of the same container waits <see cref="Stagger"/> more.
    /// A new item within <see cref="CompanionRadius"/> of an item launched in the same snapshot (the AMMO box next to a dropped
    /// gun, up to ScatterMax + AmmoMax ≈ 2.6 cells from the container) launches from the same container.
    /// Repeated snapshots never report an item twice. Pure C#: no UnityEngine dependency.
    /// </summary>
    public sealed class GroundLootArrivals
    {
        public const byte GroundItemKind = 100;
        private const byte Chest = 5, Pot = 6, Supply = 13;

        private struct Break { public float X, Y; public double Time; public int Launched; }
        private struct Pending { public int Output; public float X, Y; public int Break; }

        private readonly Dictionary<ushort, string> _items = new Dictionary<ushort, string>();
        private readonly Dictionary<ushort, bool> _containers = new Dictionary<ushort, bool>();
        private readonly List<Break> _breaks = new List<Break>();
        private readonly HashSet<ushort> _seen = new HashSet<ushort>();
        private readonly List<ushort> _gone = new List<ushort>();
        private readonly List<Pending> _launched = new List<Pending>();
        private readonly List<Pending> _appeared = new List<Pending>();
        private bool _primed;

        public float Window { get; }
        public float Radius { get; }
        public float Stagger { get; }
        public float CompanionRadius { get; }

        public GroundLootArrivals(float window = .5f, float radius = 2f, float stagger = .05f, float companionRadius = 1.25f)
        {
            Window = window; Radius = radius; Stagger = stagger; CompanionRadius = companionRadius;
        }

        /// <summary>Items currently tracked (for tests and diagnostics).</summary>
        public int Count => _items.Count;

        /// <summary>Forget everything: the next snapshot is treated as the first one (all items just appear).</summary>
        public void Reset()
        {
            _items.Clear(); _containers.Clear(); _breaks.Clear(); _primed = false;
        }

        public static bool IsContainer(byte kind) => kind == Chest || kind == Pot || kind == Supply;

        /// <summary>
        /// Compares <paramref name="states"/> with the previous snapshot and appends one arrival per new ground item to
        /// <paramref name="output"/>. <paramref name="now"/> is any monotonic clock in seconds.
        /// </summary>
        public void Observe(IReadOnlyList<WorldObjectWire> states, double now, List<GroundLootArrival> output)
        {
            if (states == null || output == null) return;
            for (var i = _breaks.Count - 1; i >= 0; i--) if (now - _breaks[i].Time > Window) _breaks.RemoveAt(i);

            for (var i = 0; i < states.Count; i++)
            {
                var s = states[i];
                if (!IsContainer(s.Kind)) continue;
                var broken = (s.Flags & 1) != 0;
                if (_primed && broken && _containers.TryGetValue(s.Id, out var was) && !was)
                    _breaks.Add(new Break { X = s.X, Y = s.Y, Time = now });
                _containers[s.Id] = broken;
            }

            _seen.Clear(); _launched.Clear(); _appeared.Clear();
            for (var i = 0; i < states.Count; i++)
            {
                var s = states[i];
                if (s.Kind != GroundItemKind) continue;
                _seen.Add(s.Id);
                // Ids are reused after a practice reset: a different item under a known id is a new item.
                if (_items.TryGetValue(s.Id, out var known) && known == s.ItemId) continue;
                _items[s.Id] = s.ItemId;
                var source = _primed ? Nearest(s.X, s.Y) : -1;
                var pending = new Pending { Output = output.Count, X = s.X, Y = s.Y, Break = source };
                output.Add(new GroundLootArrival { Id = s.Id, Entry = GroundLootEntry.Appear, FromX = s.X, FromY = s.Y });
                if (source >= 0) { Launch(output, pending.Output, source); _launched.Add(pending); }
                else if (_primed) _appeared.Add(pending);
            }

            // Companions: an ammo box may land beyond Radius but always next to the gun it came with.
            for (var a = 0; a < _appeared.Count; a++)
            {
                var item = _appeared[a];
                var limit = CompanionRadius * CompanionRadius;
                for (var l = 0; l < _launched.Count; l++)
                {
                    var dx = _launched[l].X - item.X; var dy = _launched[l].Y - item.Y;
                    if (dx * dx + dy * dy > limit) continue;
                    Launch(output, item.Output, _launched[l].Break);
                    break;
                }
            }

            _gone.Clear();
            foreach (var id in _items.Keys) if (!_seen.Contains(id)) _gone.Add(id);
            foreach (var id in _gone) _items.Remove(id);
            _primed = true;
        }

        private void Launch(List<GroundLootArrival> output, int index, int source)
        {
            var b = _breaks[source];
            var arrival = output[index];
            arrival.Entry = GroundLootEntry.Launch;
            arrival.FromX = b.X; arrival.FromY = b.Y;
            arrival.Delay = b.Launched * Stagger;
            output[index] = arrival;
            b.Launched++;
            _breaks[source] = b;
        }

        private int Nearest(float x, float y)
        {
            var best = -1; var bestDistance = Radius * Radius;
            for (var i = 0; i < _breaks.Count; i++)
            {
                var dx = _breaks[i].X - x; var dy = _breaks[i].Y - y; var d = dx * dx + dy * dy;
                if (d <= bestDistance) { bestDistance = d; best = i; }
            }
            return best;
        }
    }
}
