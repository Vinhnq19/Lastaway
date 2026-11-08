#nullable disable
using System;
using System.Collections.Generic;

namespace JungleMaze.World.Airdrops
{
    public struct AirdropZone
    {
        public WorldPoint Center;
        public float Radius;
        public AirdropZone(float x, float y, float radius) { Center = new WorldPoint(x, y); Radius = radius; }
    }

    /// <summary>Per-round dynamic eggs. Candidates come from the existing reachable floor-spot planner.</summary>
    public sealed class AirdropController : IAirdropHost
    {
        private readonly MapLayout _map;
        private readonly DestructibleService _destructibles;
        private readonly WorldPoint[] _candidates;
        private readonly Func<float, AirdropZone> _zoneAt;
        private readonly Func<WorldPoint, float, bool> _occupied;
        private readonly Action _landed;
        private readonly Dictionary<int, ushort> _ids = new Dictionary<int, ushort>();
        private readonly HashSet<int> _released = new HashSet<int>();
        private readonly float _size, _margin;
        private readonly int _limit;
        private float _now;
        public float EggSize => _size;
        public ushort EggHp {get;}
        public AirdropRuntime Runtime { get; }
        public int ReservedLootRemaining { get; private set; }
        public int ReservedEggsRemaining
        {
            get { var n=0;foreach(var state in Runtime.States)if(state.Phase==AirdropPhase.Scheduled||state.Phase==AirdropPhase.Flying||state.Phase==AirdropPhase.Falling)n++;return n; }
        }
        public event Action<AirdropState> Changed;

        public AirdropController(AirdropSettings settings, uint seed, MapLayout map,
            DestructibleService destructibles, IReadOnlyList<WorldPoint> reachableCandidates,
            Func<float, AirdropZone> zoneAt, Func<ushort> allocate,
            Action landed, Func<WorldPoint, float, bool> occupied = null)
        {
            settings.Validate();
            _map = map ?? throw new ArgumentNullException(nameof(map));
            _destructibles = destructibles ?? throw new ArgumentNullException(nameof(destructibles));
            _zoneAt = zoneAt ?? throw new ArgumentNullException(nameof(zoneAt));
            _landed = landed;
            _occupied = occupied;
            EggHp=(ushort)settings.EggHp;
            _size = settings.EggSize; _margin = settings.EdgeMargin; _limit = settings.CandidateLimit;
            _candidates = new WorldPoint[reachableCandidates.Count];
            for (var i = 0; i < _candidates.Length; i++) _candidates[i] = reachableCandidates[i];
            if (settings.Enabled)
            {
                foreach (var drop in settings.Drops)
                {
                    var id = allocate();
                    if (id == 0) throw new InvalidOperationException("No world IDs for scheduled eggs.");
                    _ids.Add(drop.Id, id);
                }
                ReservedLootRemaining = 8 * settings.Drops.Length;
            }
            Runtime = new AirdropRuntime(settings, seed, this);
            Runtime.Changed += state =>
            {
                if (state.Phase == AirdropPhase.Broken || state.Phase == AirdropPhase.Cancelled)
                    Release(state.DropId);
                Changed?.Invoke(state);
            };
        }

        public void Tick(float seconds) { _now = seconds; Runtime.Tick(seconds); }
        public bool Owns(ushort worldId) { foreach (var id in _ids.Values) if (id == worldId) return true; return false; }

        public void Release(int dropId)
        {
            if (_ids.ContainsKey(dropId) && _released.Add(dropId))
                ReservedLootRemaining = Math.Max(0, ReservedLootRemaining - 8);
        }

        public static MapLayout CopyLayout(MapLayout source)
        {
            var copy = new MapLayout { Version = source.Version, Seed = source.Seed, Size = source.Size, Hash = source.Hash };
            copy.Spawns.AddRange(source.Spawns);
            copy.Objects.AddRange(source.Objects);
            return copy;
        }

        public bool TryChooseLanding(int dropId, uint seed, float landAt, out WorldPoint position)
        {
            var zone = _zoneAt(landAt);
            var random = new StableRandom(seed);
            // Filter the forecast zone before spending the bounded footprint-check budget.
            var eligible=new List<int>();
            for(var i=0;i<_candidates.Length;i++)if(InsideZone(_candidates[i],zone))eligible.Add(i);
            var order=eligible.ToArray();
            for (var i = 0; i < Math.Min(_limit, order.Length); i++)
            {
                var picked = random.Range(i, order.Length);
                var swap = order[i]; order[i] = order[picked]; order[picked] = swap;
                var point = _candidates[order[i]];
                if (InsideZone(point, zone) && Clear(point, _size))
                { position = point; return true; }
            }
            position = default(WorldPoint);
            return false;
        }

        private bool InsideZone(WorldPoint point, AirdropZone zone)
        {
            var safe = zone.Radius - _margin;
            if (!WorldMath.Finite(safe) || safe <= 0) return false;
            var half = _size / 2f;
            for (var x = -1; x <= 1; x += 2)
                for (var y = -1; y <= 1; y += 2)
                    if (new WorldPoint(point.X + x * half, point.Y + y * half).DistanceSquared(zone.Center) > safe * safe)
                        return false;
            return true;
        }

        private bool Clear(WorldPoint point, float size)
        {
            if (!WorldMath.Finite(point.X) || !WorldMath.Finite(point.Y)) return false;
            var box = new WorldBounds(point.X, point.Y, size, size);
            if (box.MinX < .4f || box.MinY < .4f || box.MaxX > _map.Size - .4f || box.MaxY > _map.Size - .4f) return false;
            foreach (var obstacle in _map.Objects)
            {
                if (obstacle.IsDestructible && _destructibles.IsBroken(obstacle.Id)) continue;
                if (box.Overlaps(obstacle.Bounds, .4f)) return false;
            }
            return true;
        }

        public bool TryLand(int dropId, WorldPoint position, float size, int hp, out ushort worldId)
        {
            worldId = 0;
            if (!_ids.TryGetValue(dropId, out var id) || !Clear(position, size) ||
                !InsideZone(position, _zoneAt(_now)) || (_occupied != null && _occupied(position, size))) return false;
            var definition = new WorldObjectDefinition { Id = id, Kind = WorldObjectKind.Supply,
                Bounds = new WorldBounds(position.X, position.Y, size, size), Hp = (ushort)hp };
            if (!_destructibles.TryRegister(definition)) return false;
            _map.Objects.Add(definition);
            worldId = id;
            _landed?.Invoke();
            return true;
        }

        public bool IsBroken(ushort worldId) => _destructibles.IsBroken(worldId);
    }
}
