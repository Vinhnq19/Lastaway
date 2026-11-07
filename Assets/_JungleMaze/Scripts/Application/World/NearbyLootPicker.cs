using System.Collections.Generic;
using JungleBackend.Protocol.Gameplay;
using JungleMaze.World.Inventory;

namespace JungleMaze.App.World
{
    /// <summary>One ground item listed by <see cref="NearbyLootPicker"/>.</summary>
    public struct NearbyLootRow
    {
        public ushort Id;
        public string ItemId;
        public int Count;
        /// <summary>Center distance to the hero in cells.</summary>
        public float Distance;
        public ItemRarity Rarity;
        /// <summary>True for stacks worth a "×N" (consumables, throwables, ammo boxes).</summary>
        public bool ShowsCount;
        /// <summary>False while the row lingers just after leaving the reach (drawn dimmed, never picked by F).</summary>
        public bool InReach;
    }

    /// <summary>
    /// The F pickup list (Docs/hvu/07_Floor_Loot.vi.md section 11): ground items (WorldObjectWire kind 100) within
    /// <see cref="Reach"/> of the hero, nearest first. The selection follows the nearest item until the mouse wheel moves it;
    /// a wheel-chosen item stays selected by Id while it is in reach, otherwise the nearest one is selected again.
    /// An item that leaves the reach lingers <see cref="Linger"/> seconds (not pickable) so the list does not flicker at the
    /// reach edge; an item that disappears from the snapshot (picked up) goes at once. Shared by the online and the offline
    /// branch of WorldSceneView and read by the HUD panel. Presentation only: the server still validates every pickup.
    /// Pure C#, no allocation per update once the buffers have grown.
    /// </summary>
    public sealed class NearbyLootPicker
    {
        /// <summary>Same radius as the server pickup check (InventoryService pickupRadius).</summary>
        public const float DefaultReach = 1.2f;
        public const int DefaultMaxRows = 5;
        public const float DefaultLinger = .2f;

        private readonly ItemCatalog _catalog;
        private readonly List<NearbyLootRow> _rows = new List<NearbyLootRow>();
        private readonly Dictionary<ushort, double> _lastInReach = new Dictionary<ushort, double>();
        private readonly List<ushort> _expired = new List<ushort>();
        private ushort _selectedId;
        private bool _manual;

        public NearbyLootPicker(ItemCatalog catalog = null, float reach = DefaultReach, int maxRows = DefaultMaxRows, float linger = DefaultLinger)
        {
            _catalog = catalog;
            Reach = reach;
            MaxRows = maxRows < 1 ? 1 : maxRows;
            Linger = linger < 0f ? 0f : linger;
        }

        public float Reach { get; }
        public int MaxRows { get; }
        public float Linger { get; }

        /// <summary>Hero position of the last update (the panel is placed beside it).</summary>
        public float HeroX { get; private set; }
        public float HeroY { get; private set; }

        /// <summary>Listed rows, in reach and lingering, nearest first.</summary>
        public int Count => _rows.Count;
        /// <summary>Rows that F can pick; they are always the first rows.</summary>
        public int InReachCount { get; private set; }
        /// <summary>Index of the selected row, -1 when nothing is in reach.</summary>
        public int SelectedIndex { get; private set; } = -1;
        /// <summary>World Id that F picks up; 0 when nothing is in reach (F then falls back to doors).</summary>
        public ushort TargetId => SelectedIndex >= 0 ? _rows[SelectedIndex].Id : (ushort)0;
        /// <summary>True after the wheel chose the selection; false while it follows the nearest item.</summary>
        public bool ManualSelection => _manual && SelectedIndex >= 0;

        /// <summary>First drawn row: scrolls so the selected row stays among the <see cref="MaxRows"/> drawn ones.</summary>
        public int WindowStart => SelectedIndex >= MaxRows ? SelectedIndex - (MaxRows - 1) : 0;
        public int WindowCount { get { var left = _rows.Count - WindowStart; return left < MaxRows ? left : MaxRows; } }
        /// <summary>Rows not drawn, shown as "+N".</summary>
        public int HiddenCount => _rows.Count - WindowCount;

        public NearbyLootRow Row(int index) => _rows[index];

        /// <summary>
        /// Rebuilds the list from a world snapshot. <paramref name="scrollSteps"/> moves the selection through the in-reach rows
        /// (positive = down the list, farther; wraps around); it is ignored while nothing is in reach.
        /// </summary>
        public void Update(IReadOnlyList<WorldObjectWire> objects, float heroX, float heroY, int scrollSteps, double now)
        {
            HeroX = heroX; HeroY = heroY;
            _rows.Clear();
            var reachSq = Reach * Reach;
            if (objects != null)
            {
                for (var i = 0; i < objects.Count; i++)
                {
                    var o = objects[i];
                    if (o.Kind != GroundLootArrivals.GroundItemKind) continue;
                    var dx = o.X - heroX; var dy = o.Y - heroY;
                    var sq = dx * dx + dy * dy;
                    var inReach = sq <= reachSq;
                    if (inReach) _lastInReach[o.Id] = now;
                    else if (!_lastInReach.TryGetValue(o.Id, out var seen) || now - seen > Linger) continue;
                    Insert(Describe(o, (float)System.Math.Sqrt(sq), inReach));
                }
            }
            InReachCount = 0;
            while (InReachCount < _rows.Count && _rows[InReachCount].InReach) InReachCount++;
            Forget();
            Select(scrollSteps);
        }

        /// <summary>Empties the list (menu open, hero dead, not in the Game scene).</summary>
        public void Clear()
        {
            _rows.Clear();
            _lastInReach.Clear();
            InReachCount = 0;
            SelectedIndex = -1;
            _selectedId = 0;
            _manual = false;
        }

        /// <summary>"×N" for consumables, throwables (stackable weapons) and ammo; unknown items only when N &gt; 1.</summary>
        public static bool ShowsCountFor(ItemDefinition item, int count)
        {
            if (count <= 0) return false;
            if (item == null) return count > 1;
            return item.Kind == ItemKind.Consumable || item.Kind == ItemKind.Ammo || (item.Kind == ItemKind.Weapon && item.MaxStack > 1);
        }

        private NearbyLootRow Describe(in WorldObjectWire o, float distance, bool inReach)
        {
            ItemDefinition item = null;
            if (_catalog != null && !string.IsNullOrEmpty(o.ItemId)) _catalog.TryGet(o.ItemId, out item);
            return new NearbyLootRow
            {
                Id = o.Id, ItemId = o.ItemId, Count = o.Count, Distance = distance, InReach = inReach,
                Rarity = GroundLootLook.RarityOf(item), ShowsCount = ShowsCountFor(item, o.Count)
            };
        }

        /// <summary>Insertion keeps in-reach rows first, then nearest first, ties by Id (stable order between frames).</summary>
        private void Insert(NearbyLootRow row)
        {
            var at = _rows.Count;
            while (at > 0 && Before(row, _rows[at - 1])) at--;
            _rows.Insert(at, row);
        }

        private static bool Before(in NearbyLootRow a, in NearbyLootRow b)
        {
            if (a.InReach != b.InReach) return a.InReach;
            if (a.Distance != b.Distance) return a.Distance < b.Distance;
            return a.Id < b.Id;
        }

        /// <summary>Drops linger stamps of items that are no longer listed (picked up or expired).</summary>
        private void Forget()
        {
            if (_lastInReach.Count == _rows.Count) return;
            _expired.Clear();
            foreach (var entry in _lastInReach) if (IndexOf(entry.Key, _rows.Count) < 0) _expired.Add(entry.Key);
            foreach (var id in _expired) _lastInReach.Remove(id);
        }

        private void Select(int scrollSteps)
        {
            if (InReachCount == 0) { SelectedIndex = -1; _selectedId = 0; _manual = false; return; }
            var index = _manual ? IndexOf(_selectedId, InReachCount) : -1;
            if (index < 0) { index = 0; _manual = false; }
            if (scrollSteps != 0)
            {
                index = ((index + scrollSteps) % InReachCount + InReachCount) % InReachCount;
                _manual = true;
            }
            SelectedIndex = index;
            _selectedId = _rows[index].Id;
        }

        private int IndexOf(ushort id, int limit)
        {
            for (var i = 0; i < limit; i++) if (_rows[i].Id == id) return i;
            return -1;
        }
    }
}
