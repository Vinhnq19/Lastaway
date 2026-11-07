using System.Collections.Generic;
using JungleBackend.Protocol.Gameplay;
using JungleMaze.App.World;
using JungleMaze.Core.Pooling;
using JungleMaze.Presentation.Combat;
using JungleMaze.Presentation.UI.Hud;
using JungleMaze.World.Inventory;
using UnityEngine;

namespace JungleMaze.Presentation.World.Loot
{
    /// <summary>
    /// Draws every ground item (WorldObjectWire kind 100) of a world snapshot with a pooled <see cref="GroundLootView"/>,
    /// online and offline alike. <see cref="GroundLootArrivals"/> decides from the snapshots alone whether a new item fades in
    /// or bursts out of a container that just broke. Views are reused, never destroyed per snapshot.
    /// </summary>
    public sealed class GroundLootLayer
    {
        public const byte GroundItemKind = GroundLootArrivals.GroundItemKind;
        private readonly Transform _root;
        private readonly GroundLootTuning _tuning;
        private readonly IItemIconProvider _icons;
        private readonly ItemCatalog _catalog;
        private readonly Sprite _fallback;
        private readonly GroundLootArrivals _arrivals;
        private readonly ObjectPool<GroundLootView> _pool;
        private readonly Dictionary<ushort, GroundLootView> _views = new Dictionary<ushort, GroundLootView>();
        private readonly Dictionary<ushort, GroundLootArrival> _new = new Dictionary<ushort, GroundLootArrival>();
        private readonly List<GroundLootArrival> _arrivalBuffer = new List<GroundLootArrival>();
        private readonly HashSet<ushort> _seen = new HashSet<ushort>();
        private readonly List<ushort> _gone = new List<ushort>();

        /// <summary>Views created so far (pool growth); stays flat while snapshots repeat.</summary>
        public int Created { get; private set; }
        public int ActiveCount => _views.Count;
        public int PooledCount => _pool.CountInactive;
        public GroundLootArrivals Arrivals => _arrivals;

        public GroundLootLayer(Transform root, GroundLootTuning tuning, IItemIconProvider icons, ItemCatalog catalog, Sprite fallback)
        {
            _root = root; _tuning = tuning; _icons = icons; _catalog = catalog; _fallback = fallback;
            _arrivals = new GroundLootArrivals(tuning.LaunchWindow, tuning.LaunchRadius, tuning.LaunchStagger, tuning.LaunchCompanionRadius);
            _pool = new ObjectPool<GroundLootView>(Create, v => v.gameObject.SetActive(true), v => v.gameObject.SetActive(false), 0, 4096);
        }

        private GroundLootView Create()
        {
            var go = new GameObject("Loot");
            go.transform.SetParent(_root, false);
            var view = go.AddComponent<GroundLootView>();
            view.Build(_tuning);
            Created++;
            return view;
        }

        public bool TryGetView(ushort id, out GroundLootView view) => _views.TryGetValue(id, out view);

        /// <summary>Shows the ground items of <paramref name="states"/> (other kinds are only read for container breaks).</summary>
        public void Apply(IReadOnlyList<WorldObjectWire> states, double now)
        {
            _arrivalBuffer.Clear(); _new.Clear(); _seen.Clear();
            _arrivals.Observe(states, now, _arrivalBuffer);
            foreach (var a in _arrivalBuffer) _new[a.Id] = a;
            for (var i = 0; i < states.Count; i++)
            {
                var s = states[i];
                if (s.Kind != GroundItemKind) continue;
                _seen.Add(s.Id);
                var known = _views.TryGetValue(s.Id, out var view);
                if (_new.TryGetValue(s.Id, out var arrival) || !known)
                {
                    if (!known) { view = _pool.Get(); _views.Add(s.Id, view); }
                    if (!_new.ContainsKey(s.Id)) arrival = new GroundLootArrival { Id = s.Id, FromX = s.X, FromY = s.Y };
                    view.Show(s.Id, s.ItemId, s.X, s.Y, ResolveArt(s.ItemId), arrival, now);
                }
                else view.Move(s.X, s.Y);
            }
            _gone.Clear();
            foreach (var entry in _views) if (!_seen.Contains(entry.Key)) _gone.Add(entry.Key);
            foreach (var id in _gone) { _pool.Release(_views[id]); _views.Remove(id); }
        }

        /// <summary>Hides every item and forgets the snapshot history (world hidden, new match).</summary>
        public void Clear()
        {
            foreach (var view in _views.Values) _pool.Release(view);
            _views.Clear();
            _arrivals.Reset();
        }

        /// <summary>Picks the floor art: throwable flight art, in-hand weapon art or the HUD icon, sized by the tuning.</summary>
        public GroundLootArt ResolveArt(string itemId)
        {
            ItemDefinition item = null;
            if (_catalog != null && !string.IsNullOrEmpty(itemId)) _catalog.TryGet(itemId, out item);
            var held = default(HeldWeaponCatalog.Entry);
            var throwable = default(ThrowableArtCatalog.Entry);
            var hasHeld = _tuning.HeldArt != null && _tuning.HeldArt.TryGet(itemId, out held);
            var isThrowable = _tuning.ThrowableArt != null && _tuning.ThrowableArt.TryGet(itemId, out throwable);
            var hasThrowable = isThrowable && throwable.Flight.IsSet && throwable.Flight.Frames.Sprites[0] != null &&
                               Aspect(throwable.Flight.Frames.Sprites[0]) <= _tuning.MaxThrowableAspect;
            // A throwable never uses its tiny in-hand sprite: unusable flight art (the ROPE stroke) falls back to the HUD icon.
            var art = new GroundLootArt { Shape = GroundLootLook.ShapeOf(item, hasHeld && !isThrowable, hasThrowable), Rarity = GroundLootLook.RarityOf(item) };
            switch (art.Shape)
            {
                case GroundLootShape.Held:
                    art.Sprite = held.Sprite; art.Scale = held.Scale * _tuning.HeldScale; art.Angle = held.AngleOffset + _tuning.HeldAngle;
                    art.Center = held.ContentSize.x > 0f && held.ContentSize.y > 0f ? held.ContentMin + held.ContentSize * .5f : (Vector2)held.Sprite.bounds.center;
                    return art;
                case GroundLootShape.Throwable:
                    art.Sprite = throwable.Flight.Frames.Sprites[0];
                    art.Scale = throwable.Flight.ScaleFor(throwable.FlightSize * _tuning.ThrowableScale);
                    art.Angle = _tuning.ThrowableAngle;
                    art.Center = art.Sprite.bounds.center;
                    return art;
                default:
                    var size = art.Shape == GroundLootShape.Ammo ? _tuning.AmmoSize : _tuning.IconSize;
                    var icon = _icons != null && !string.IsNullOrEmpty(itemId) ? _icons.GetIcon(itemId) : null;
                    if (icon == null && art.Shape == GroundLootShape.Ammo) icon = _tuning.AmmoBox;
                    if (icon == null && hasHeld) icon = held.Sprite;
                    art.Placeholder = icon == null;
                    art.Sprite = icon != null ? icon : _fallback;
                    art.Angle = _tuning.IconAngle;
                    if (art.Sprite == null) return art;
                    var bounds = art.Sprite.bounds;
                    var longest = Mathf.Max(bounds.size.x, bounds.size.y);
                    art.Scale = longest > 0f ? (art.Placeholder ? size * .45f : size) / longest : 1f;
                    art.Center = bounds.center;
                    return art;
            }
        }

        private static float Aspect(Sprite sprite)
        {
            var size = sprite.bounds.size;
            var shortest = Mathf.Min(size.x, size.y);
            return shortest > 0f ? Mathf.Max(size.x, size.y) / shortest : float.MaxValue;
        }
    }
}
