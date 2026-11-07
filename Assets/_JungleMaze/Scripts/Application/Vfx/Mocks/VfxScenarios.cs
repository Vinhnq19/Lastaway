#if UNITY_EDITOR || DEVELOPMENT_BUILD
using JungleBackend.Protocol.Gameplay;
using JungleMaze.App.Combat;
using JungleMaze.App.Visibility.Mocks;
using JungleMaze.Core.Events;
using JungleMaze.Core.Ticking;
using JungleMaze.Domain.CombatFeedback;
using JungleMaze.World.Throwables;

namespace JungleMaze.App.Vfx.Mocks
{
    /// <summary>
    /// Dev-only fake events for the F10 panel and the VfxPreview scene. Gun/melee hits and statuses travel the bus as
    /// WPN-02/Game-10 will publish them; throws go through <see cref="FakeThrowableEventSource"/>, so the same event
    /// reaches <c>ThrowableView</c> and <see cref="ThrowableVfxSource"/> like a real WPN-03 event (plan 4.12.5).
    /// </summary>
    public sealed class VfxScenarios : ITickable
    {
        public const int LoadSources = 10;

        private struct Pending
        {
            public float At;
            public byte Type; // 1 hit, 3 status
            public HitConfirmedEvent Hit;
            public StatusEvent Status;
        }

        private readonly IEventBus _bus;
        private readonly VfxDirector _director;
        private readonly MockVfxMatch _mock;
        private readonly ToggleVisibilityPolicy _visibility;
        private readonly FakeThrowableEventSource _throwables;
        private readonly IVfxTargetLocator _targets;
        private readonly Pending[] _pending = new Pending[128];
        private int _pendingCount;
        private float _time, _loadUntil, _nextLoad;
        private ulong _nextKey = 1000;
        private ushort _seq = 1;
        private uint _loadStep;

        public VfxScenarios(IEventBus bus, VfxDirector director, MockVfxMatch mock, ToggleVisibilityPolicy visibility,
            FakeThrowableEventSource throwables, IVfxTargetLocator targets)
        {
            _bus = bus;
            _director = director;
            _mock = mock;
            _visibility = visibility;
            _throwables = throwables;
            _targets = targets;
        }

        public bool LoadRunning => _time < _loadUntil;
        public int PendingCount => _pendingCount;
        public MockVfxMatch Mock => _mock;
        public ToggleVisibilityPolicy Visibility => _visibility;
        public FakeThrowableEventSource Throwables => _throwables;
        public VfxDirector Director => _director;

        public ulong NextKey() => ++_nextKey;

        /// <summary>A confirmed gun/melee hit (fake WPN-02 source).</summary>
        public void Hit(byte target, int damage, byte source = 0) => PublishHit(NextKey(), target, damage, source);

        public void Status(byte target, StatusKind kind, float seconds) =>
            _bus.Publish(new StatusEvent(NextKey(), target, kind, true, seconds, seconds));

        public void ClearStatus(byte target, StatusKind kind) =>
            _bus.Publish(new StatusEvent(NextKey(), target, kind, false, 0f, 0f));

        /// <summary>A full WPN-03 throw from <paramref name="owner"/> onto (x, y).</summary>
        public void Throw(ThrowableKind kind, float x, float y, byte owner = 1)
        {
            if (_throwables == null) return;
            var fromX = x - 3f;
            var fromY = y;
            if (_targets != null && _targets.TryGetPosition(owner, out var ox, out var oy)) { fromX = ox; fromY = oy; }
            _throwables.Throw(kind, owner, fromX, fromY, x, y);
        }

        /// <summary>The same hit twice now and a third time after 1 s: exactly one effect (AC3).</summary>
        public void RepeatHit(byte target)
        {
            var key = NextKey();
            PublishHit(key, target, 12, 0);
            PublishHit(key, target, 12, 0);
            if (_targets != null && _targets.TryGetPosition(target, out var x, out var y))
                Schedule(new Pending { At = _time + 1f, Type = 1, Hit = new HitConfirmedEvent(key, 0, target, 12, x, y) });
        }

        public void RepeatStatus(byte target, StatusKind kind, float seconds)
        {
            var e = new StatusEvent(NextKey(), target, kind, true, seconds, seconds);
            _bus.Publish(e);
            _bus.Publish(e);
            Schedule(new Pending { At = _time + 1f, Type = 3, Status = e });
        }

        /// <summary>Own predicted attack answered by <paramref name="outcome"/>; Accepted confirms instead.</summary>
        public void Rollback(CombatOutcome outcome)
        {
            var seq = _seq++;
            var own = _mock != null ? _mock.OwnNetId : (byte)0;
            _director.OnCombat(new CombatEvent(CombatEventKind.AttackPredicted, CombatWeaponId.Hand, own, seq, 0f, CombatOutcome.Accepted));
            _director.OnCombat(outcome == CombatOutcome.Accepted
                ? new CombatEvent(CombatEventKind.AttackConfirmed, CombatWeaponId.Hand, own, seq, 0f, CombatOutcome.Accepted)
                : new CombatEvent(CombatEventKind.AttackRejected, CombatWeaponId.Hand, own, seq, 0f, outcome));
        }

        public void RemoteAttack(byte netId) =>
            _director.OnCombat(new CombatEvent(CombatEventKind.RemoteAttackConfirmed, CombatWeaponId.Knife, netId, _seq++, 0f, CombatOutcome.Accepted));

        public void ChangeRoom()
        {
            _pendingCount = 0;
            _loadUntil = 0f;
            _throwables?.Clear();
            _mock?.NewSession();
        }

        /// <summary>Ten fake sources attacking, hitting, throwing and applying statuses for <paramref name="seconds"/> (AC15).</summary>
        public void StartLoad(float seconds = 60f)
        {
            _loadUntil = _time + seconds;
            _nextLoad = _time;
        }

        public void StopLoad() => _loadUntil = 0f;

        public void Tick(float deltaTime)
        {
            if (deltaTime > 0f) _time += deltaTime;
            for (var i = _pendingCount - 1; i >= 0; i--)
            {
                if (_pending[i].At > _time) continue;
                var p = _pending[i];
                _pending[i] = _pending[--_pendingCount];
                if (p.Type == 1) _bus.Publish(p.Hit);
                else if (p.Type == 3) _bus.Publish(p.Status);
            }
            if (LoadRunning && _time >= _nextLoad)
            {
                _nextLoad = _time + .1f;
                LoadStep();
            }
        }

        private void LoadStep()
        {
            var source = (byte)(1 + _loadStep % LoadSources);
            var target = (byte)(1 + (_loadStep * 7 + 3) % LoadSources);
            if (target == source) target = (byte)(1 + target % LoadSources);
            switch (_loadStep % 6)
            {
                case 0:
                case 3: RemoteAttack(source); break;
                case 1:
                case 4: PublishHit(NextKey(), target, (int)(5 + _loadStep % 30), source); break;
                case 2:
                    if (_targets != null && _targets.TryGetPosition(target, out var x, out var y))
                        Throw(_loadStep % 12 == 2 ? ThrowableKind.Smoke : ThrowableKind.Banana, x + 1f, y, source);
                    break;
                case 5: Status(target, (StatusKind)(4 + _loadStep % 5), 2f); break;
            }
            _loadStep++;
        }

        private void PublishHit(ulong key, byte target, int damage, byte source)
        {
            if (_targets == null || !_targets.TryGetPosition(target, out var x, out var y)) { x = 0f; y = 0f; }
            _bus.Publish(new HitConfirmedEvent(key, source, target, damage, x, y));
        }

        private void Schedule(in Pending pending)
        {
            if (_pendingCount < _pending.Length) _pending[_pendingCount++] = pending;
        }
    }
}
#endif
