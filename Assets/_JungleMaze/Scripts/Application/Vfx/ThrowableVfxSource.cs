using System;
using JungleMaze.App.Audio;
using JungleMaze.App.Combat;
using JungleMaze.App.World;
using JungleMaze.Core.DI;
using JungleMaze.Core.Events;
using JungleMaze.Domain.CombatFeedback;
using JungleMaze.World.Throwables;

namespace JungleMaze.App.Vfx
{
    /// <summary>
    /// Real source of thrown/special weapon feedback (VFX-01 plan 4.12.3): turns confirmed WPN-03
    /// <see cref="ThrowableEvent"/>s into the three VFX contract events on the bus. <c>ThrowableView</c> listens to the
    /// same sources and draws the item, blast, smoke, trap, rope and vines; this adapter only adds the fuse warning,
    /// body hits (number, flash, spark), stun/reverse auras and the camera shake. Read-only on every source.
    /// </summary>
    public sealed class ThrowableVfxSource : IInitializable, IDisposable
    {
        private const byte TagBlast = 21, TagHit = 22, TagStatus = 23;
        /// <summary>Events without an id (bell hit, bat hit, rope) get keys from a private counter in this range.</summary>
        private const ulong UniqueKeyBase = 0xA500_0000_0000_0000UL;

        private readonly IEventBus _bus;
        private readonly OfflineWorldSession _offline;
        private readonly IThrowableEventSource[] _sources;
        private readonly Action<ThrowableEvent>[] _handlers;
        private ulong _unique;
        private bool _subscribed;

        public ThrowableVfxSource(IEventBus bus, OfflineWorldSession offline, params IThrowableEventSource[] sources)
        {
            _bus = bus;
            _offline = offline;
            _sources = sources ?? Array.Empty<IThrowableEventSource>();
            _handlers = new Action<ThrowableEvent>[_sources.Length];
            for (var i = 0; i < _sources.Length; i++)
            {
                // Online relays are muted while offline practice runs (same gate as the combat binding of AUD-01).
                var gated = _sources[i] is OnlineThrowableSession;
                _handlers[i] = gated ? (Action<ThrowableEvent>)OnGated : OnThrowableEvent;
            }
        }

        public void Initialize()
        {
            if (_subscribed) return;
            for (var i = 0; i < _sources.Length; i++)
                if (_sources[i] != null) _sources[i].ThrowableRaised += _handlers[i];
            _subscribed = true;
        }

        public void Dispose()
        {
            if (!_subscribed) return;
            for (var i = 0; i < _sources.Length; i++)
                if (_sources[i] != null) _sources[i].ThrowableRaised -= _handlers[i];
            _subscribed = false;
        }

        private void OnGated(ThrowableEvent e)
        {
            if (_offline != null && _offline.IsActive) return;
            OnThrowable(e);
        }

        private void OnThrowableEvent(ThrowableEvent e) => OnThrowable(e);

        /// <summary>Maps one event (table 4.12.3); public for tests.</summary>
        public void OnThrowable(in ThrowableEvent e)
        {
            if (_bus == null) return;
            switch (e.Kind)
            {
                case ThrowableEventKind.Thrown:
                    if (e.Item < ThrowableKind.Banana || e.Item > ThrowableKind.Seed) return;
                    _bus.Publish(new ExplosionEvent(BlastKey(e.Id), ThrowableRules.IdOf(e.Item), ExplosionPhase.Fuse,
                        e.ToX, e.ToY, e.Size, e.Size, e.Seconds, e.Actor));
                    return;
                case ThrowableEventKind.Detonated:
                    _bus.Publish(new ExplosionEvent(BlastKey(e.Id), ThrowableRules.IdOf(e.Item), ExplosionPhase.Detonate,
                        e.FromX, e.FromY, e.Size, e.Size, 0f, e.Actor));
                    return;
                case ThrowableEventKind.Hit:
                    PublishHit(e);
                    if (e.Item == ThrowableKind.Bell && e.Seconds > 0f) PublishStatus(e, StatusKind.Reverse);
                    else if (e.Item == ThrowableKind.Seed && e.Seconds > 0f) PublishStatus(e, StatusKind.Root);
                    return;
                case ThrowableEventKind.BatHit:
                    PublishHit(e);
                    if ((e.Flags & ThrowableEvent.StunnedFlag) != 0 && e.Seconds > 0f) PublishStatus(e, StatusKind.Stun);
                    return;
                case ThrowableEventKind.TrapTriggered:
                    if (e.Seconds > 0f) PublishStatus(e, StatusKind.Stun);
                    return;
                case ThrowableEventKind.RopePulledTarget:
                    PublishHit(e);
                    return;
            }
        }

        private void PublishHit(in ThrowableEvent e)
        {
            // Amount is the real damage; 0 when the target is immortal (plan 4.12.1).
            if (e.Amount <= 0 || e.Target == ThrowableEvent.NoTarget) return;
            _bus.Publish(new HitConfirmedEvent(Key(TagHit, e.Id, e.Target), e.Actor, e.Target, e.Amount, e.ToX, e.ToY));
        }

        private void PublishStatus(in ThrowableEvent e, StatusKind kind)
        {
            if (e.Target == ThrowableEvent.NoTarget) return;
            _bus.Publish(new StatusEvent(Key(TagStatus, e.Id, e.Target, (uint)kind), e.Target, kind, true, e.Seconds, e.Seconds));
        }

        private static ulong BlastKey(ushort id) => SfxDeduper.Key(TagBlast, id);

        /// <summary>Id ≠ 0 names one blast/trap/cloud, so a repeat is a duplicate; Id = 0 is never deduped.</summary>
        private ulong Key(byte tag, ushort id, byte target, uint extra = 0) =>
            id != 0 ? SfxDeduper.Key(tag, id, target, extra) : UniqueKeyBase | ++_unique;
    }
}
