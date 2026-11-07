using System;
using JungleMaze.App.Combat;
using JungleMaze.World;
using JungleMaze.World.Inventory;
using JungleMaze.World.Throwables;

namespace JungleMaze.App.World
{
    /// <summary>
    /// WPN-03 in offline practice: the same <see cref="ThrowableService"/> rules as the server, run against the local
    /// player only. There is nobody else to hit, so this mostly demos throws, blasts, traps, smoke and the rope's
    /// self-pull; a pull moves the hero through the portal teleport path the scene already follows.
    /// </summary>
    public sealed partial class OfflineWorldSession : IThrowableWorld, IThrowableEventSource
    {
        private ThrowableService _throwables;
        private uint _throwRequest;

        /// <summary>Rules from the packaged weapons.json; the GDD baseline when unset.</summary>
        public ThrowableRules ThrowableRules { get; set; }

        public event Action<ThrowableEvent> ThrowableRaised;

        public ThrowableService Throwables
        {
            get
            {
                if (_throwables != null) return _throwables;
                _throwables = new ThrowableService(ThrowableRules ?? ThrowableRules.Baseline(), this);
                _throwables.Raised += e => ThrowableRaised?.Invoke(e);
                return _throwables;
            }
        }

        /// <summary>Uses the held thrown or special weapon toward the aim; false when nothing was used.</summary>
        public bool TryUseThrowable(float aimDegrees, float distance)
        {
            if (!IsActive || _items == null || _match.IsDead) return false;
            var kind = ThrowableRules.KindOf(_items.HeldWeapon(0).ItemId);
            if (kind == ThrowableKind.None) return false;
            if (++_throwRequest == 0) _throwRequest = 1;
            return Throwables.Use(0, _throwRequest, kind, aimDegrees, distance, _now) == ThrowableResult.Ok;
        }

        private void TickThrowables()
        {
            _throwables?.Tick(_now);
        }

        private void ResetThrowables()
        {
            _throwables?.Reset();
            _throwRequest = 0;
        }

        int IThrowableWorld.ActorCount => 1;

        ThrowActor IThrowableWorld.ActorAt(int index) => Self();

        bool IThrowableWorld.TryGetActor(byte id, out ThrowActor actor)
        {
            actor = Self();
            return id == 0;
        }

        ThrowableKind IThrowableWorld.HeldThrowable(byte actor) =>
            actor == 0 && _items != null ? ThrowableRules.KindOf(_items.HeldWeapon(0).ItemId) : ThrowableKind.None;

        bool IThrowableWorld.CanAct(byte actor) => actor == 0 && _items != null && _items.CanAttack(0);

        bool IThrowableWorld.TryConsume(byte actor, ThrowableRule rule) =>
            actor == 0 && _items != null && _items.TryConsumeHeldUse(0, rule.ItemId, rule.IsSpecial ? rule.Charges : 0);

        float IThrowableWorld.CoverFraction(float x0, float y0, float x1, float y1)
        {
            if (_map == null) return 1f;
            var best = Math.Min(Exit(x0, x1, _map.Size), Exit(y0, y1, _map.Size));
            foreach (var o in _map.Objects)
            {
                var blocks = o.IsHardCover || (o.IsDestructible && (_destructibles == null || !_destructibles.IsBroken(o.Id))) ||
                             (o.Kind == WorldObjectKind.Door && _hazards != null && _hazards.IsDoorClosed(o.Id));
                if (blocks && o.Bounds.TrySegmentEntry(x0, y0, x1, y1, out var t) && t < best) best = t;
            }
            return best;
        }

        bool IThrowableWorld.IsWalkable(float x, float y, float radius) =>
            _map != null && _map.IsWalkable(new WorldPoint(x, y), radius, _destructibles != null ? _destructibles.IsBroken : (Func<ushort, bool>)null,
                _hazards != null ? _hazards.IsDoorClosed : (Func<ushort, bool>)null);

        int IThrowableWorld.Damage(byte attacker, byte target, int amount) => 0;

        void IThrowableWorld.DamageContainers(byte attacker, WorldBounds blast, int amount)
        {
            if (_destructibles == null || _hazards == null) return;
            if (++_hitSequence == 0) _hitSequence = 1;
            _destructibles.DamageContainersInBlast(blast, amount, attacker, _hitSequence, _hazards.IsDoorClosed);
        }

        void IThrowableWorld.ApplyEffect(byte target, ThrowableEffect effect, double seconds) { }

        void IThrowableWorld.Move(byte actor, float x, float y)
        {
            if (actor != 0) return;
            var destination = new WorldPoint(x, y);
            SetPlayerPose(x, y);
            _pendingPortalDestination = destination;
        }

        private ThrowActor Self() =>
            new ThrowActor(0, _playerPose.X, _playerPose.Y, .4f, _match == null || !_match.IsDead);

        private static float Exit(float from, float to, float size)
        {
            if (to >= 0 && to <= size) return 1f;
            var limit = to < 0 ? 0f : size;
            var span = to - from;
            return Math.Abs(span) < 1e-6f ? 1f : Math.Max(0f, Math.Min(1f, (limit - from) / span));
        }

    }
}
