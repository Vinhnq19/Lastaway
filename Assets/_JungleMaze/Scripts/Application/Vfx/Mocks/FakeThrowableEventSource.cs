#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using JungleMaze.App.Combat;
using JungleMaze.Core.Ticking;
using JungleMaze.World.Throwables;

namespace JungleMaze.App.Vfx.Mocks
{
    /// <summary>
    /// Dev-only <see cref="IThrowableEventSource"/> for the VfxPreview scene and the F10 panel: plays whole WPN-03
    /// sequences (throw → fuse → blast → hits, trap, smoke, bell, bat) with the numbers of <see cref="ThrowableRules"/>,
    /// so <c>ThrowableView</c> and <see cref="ThrowableVfxSource"/> can be checked together on the same events.
    /// Hits go to every candidate body inside the area, found through <see cref="IVfxTargetLocator"/>.
    /// Not compiled into release builds.
    /// </summary>
    public sealed class FakeThrowableEventSource : IThrowableEventSource, ITickable
    {
        private struct Pending
        {
            public float At;
            public ThrowableEvent Event;
            /// <summary>Detonation: also emit the per-kind follow-up (hits, trap, smoke).</summary>
            public bool Blast;
        }

        private readonly ThrowableRules _rules;
        private readonly IVfxTargetLocator _targets;
        private readonly Pending[] _pending = new Pending[128];
        private int _pendingCount;
        private float _time;
        private ushort _nextId;
        private ushort _lastTrap;
        private float _lastTrapX, _lastTrapY;

        public FakeThrowableEventSource(ThrowableRules rules, IVfxTargetLocator targets)
        {
            _rules = rules ?? ThrowableRules.Baseline();
            _targets = targets;
        }

        public event Action<ThrowableEvent> ThrowableRaised;

        /// <summary>Bodies that may be hit (the preview dummies use 0..10).</summary>
        public byte[] Candidates { get; set; } = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        public int PendingCount => _pendingCount;
        public ThrowableRules Rules => _rules;

        /// <summary>Throws a grenade-type item (BANANA, SMOKE, METEORITE, CRYSTAL, SEED) from one point to another.</summary>
        public ushort Throw(ThrowableKind kind, byte actor, float fromX, float fromY, float toX, float toY)
        {
            if (!_rules.TryGet(kind, out var rule) || !rule.IsThrown) return 0;
            var id = NextId();
            Raise(new ThrowableEvent
            {
                Kind = ThrowableEventKind.Thrown, Item = kind, Actor = actor, Target = ThrowableEvent.NoTarget, Id = id,
                FromX = fromX, FromY = fromY, ToX = toX, ToY = toY, Seconds = (float)rule.FuseSeconds, Size = rule.AreaWidth
            });
            Schedule((float)rule.FuseSeconds, new ThrowableEvent
            {
                Kind = ThrowableEventKind.Detonated, Item = kind, Actor = actor, Target = ThrowableEvent.NoTarget, Id = id,
                FromX = toX, FromY = toY, ToX = toX, ToY = toY, Size = rule.AreaWidth
            }, true);
            return id;
        }

        /// <summary>Steps the most recent banana trap on <paramref name="target"/> (stun from the rule).</summary>
        public bool TriggerTrap(byte target)
        {
            if (_lastTrap == 0 || !_rules.TryGet(ThrowableKind.Banana, out var rule)) return false;
            var x = _lastTrapX;
            var y = _lastTrapY;
            if (_targets != null) _targets.TryGetPosition(target, out x, out y);
            Raise(new ThrowableEvent
            {
                Kind = ThrowableEventKind.TrapTriggered, Item = ThrowableKind.Banana, Actor = 0, Target = target, Id = _lastTrap,
                FromX = _lastTrapX, FromY = _lastTrapY, ToX = x, ToY = y, Seconds = (float)rule.StunSeconds, Flags = ThrowableEvent.StunnedFlag
            });
            _lastTrap = 0;
            return true;
        }

        /// <summary>Rings the bell: every candidate in the square gets a hit (Id 0) with the reverse duration.</summary>
        public void RingBell(byte actor)
        {
            if (!_rules.TryGet(ThrowableKind.Bell, out var rule) || _targets == null || !_targets.TryGetPosition(actor, out var x, out var y)) return;
            Raise(new ThrowableEvent
            {
                Kind = ThrowableEventKind.BellRung, Item = ThrowableKind.Bell, Actor = actor, Target = ThrowableEvent.NoTarget,
                FromX = x, FromY = y, ToX = x, ToY = y, Seconds = (float)rule.ReverseSeconds, Size = rule.AreaWidth
            });
            HitArea(ThrowableKind.Bell, actor, 0, x, y, rule.AreaWidth, rule.Damage, (float)rule.ReverseSeconds, actor);
        }

        /// <summary>Bat impact on <paramref name="target"/>; <paramref name="stunned"/> sets the stun flag and seconds.</summary>
        public void BatHit(byte actor, byte target, bool stunned)
        {
            if (!_rules.TryGet(ThrowableKind.Bat, out var rule) || _targets == null || !_targets.TryGetPosition(target, out var x, out var y)) return;
            Raise(new ThrowableEvent
            {
                Kind = ThrowableEventKind.BatHit, Item = ThrowableKind.Bat, Actor = actor, Target = target,
                FromX = x, FromY = y, ToX = x + 1f, ToY = y, Amount = (short)rule.Damage,
                Seconds = stunned ? (float)rule.StunSeconds : 0f, Flags = stunned ? ThrowableEvent.StunnedFlag : (byte)0
            });
        }

        /// <summary>Raises one event as is (tests: repeats, Id 0).</summary>
        public void Raise(in ThrowableEvent e) => ThrowableRaised?.Invoke(e);

        /// <summary>Drops pending steps (room change).</summary>
        public void Clear()
        {
            _pendingCount = 0;
            _lastTrap = 0;
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime > 0f) _time += deltaTime;
            for (var i = 0; i < _pendingCount; i++)
            {
                if (_pending[i].At > _time) continue;
                var p = _pending[i];
                _pending[i] = _pending[--_pendingCount];
                i--;
                Raise(p.Event);
                if (p.Blast) FollowUp(p.Event);
            }
        }

        private void FollowUp(in ThrowableEvent blast)
        {
            if (!_rules.TryGet(blast.Item, out var rule)) return;
            HitArea(blast.Item, blast.Actor, blast.Id, blast.FromX, blast.FromY, rule.AreaWidth, rule.Damage, (float)rule.RootSeconds, ThrowableEvent.NoTarget);
            if (blast.Item == ThrowableKind.Banana)
            {
                _lastTrap = blast.Id;
                _lastTrapX = blast.FromX;
                _lastTrapY = blast.FromY;
                Raise(new ThrowableEvent
                {
                    Kind = ThrowableEventKind.TrapArmed, Item = blast.Item, Actor = blast.Actor, Target = ThrowableEvent.NoTarget, Id = blast.Id,
                    FromX = blast.FromX, FromY = blast.FromY, ToX = blast.FromX, ToY = blast.FromY, Seconds = (float)rule.TrapSeconds, Size = rule.TrapRadius * 2f
                });
            }
            else if (blast.Item == ThrowableKind.Smoke)
            {
                var smoke = new ThrowableEvent
                {
                    Kind = ThrowableEventKind.SmokeStarted, Item = blast.Item, Actor = blast.Actor, Target = ThrowableEvent.NoTarget, Id = blast.Id,
                    FromX = blast.FromX, FromY = blast.FromY, ToX = blast.FromX, ToY = blast.FromY, Seconds = (float)rule.SmokeSeconds, Size = rule.AreaWidth
                };
                Raise(smoke);
                smoke.Kind = ThrowableEventKind.SmokeEnded;
                Schedule((float)rule.SmokeSeconds, smoke, false);
            }
        }

        private void HitArea(ThrowableKind item, byte actor, ushort id, float cx, float cy, float size, int damage, float seconds, byte skip)
        {
            if (_targets == null || Candidates == null) return;
            var half = Math.Max(.5f, size * .5f);
            foreach (var target in Candidates)
            {
                if (target == skip || !_targets.TryGetPosition(target, out var x, out var y)) continue;
                if (Math.Abs(x - cx) > half || Math.Abs(y - cy) > half) continue;
                Raise(new ThrowableEvent
                {
                    Kind = ThrowableEventKind.Hit, Item = item, Actor = actor, Target = target, Id = id,
                    FromX = cx, FromY = cy, ToX = x, ToY = y, Amount = (short)damage, Seconds = seconds
                });
            }
        }

        private ushort NextId()
        {
            if (++_nextId == 0) _nextId = 1;
            return _nextId;
        }

        private void Schedule(float delay, in ThrowableEvent e, bool blast)
        {
            if (_pendingCount < _pending.Length) _pending[_pendingCount++] = new Pending { At = _time + delay, Event = e, Blast = blast };
        }
    }
}
#endif
