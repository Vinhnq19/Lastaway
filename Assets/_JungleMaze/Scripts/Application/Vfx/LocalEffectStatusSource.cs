using System;
using JungleMaze.App.Hud.State;
using JungleMaze.Core.DI;
using JungleMaze.Core.Events;
using JungleMaze.Domain.CombatFeedback;
using JungleMaze.Domain.Hud;

namespace JungleMaze.App.Vfx
{
    /// <summary>
    /// Real source for auras on the local player (T-6): turns <see cref="IEffectState.Changed"/> (UI-01 read port,
    /// fed online by <c>WorldClientSession</c> and offline by <c>OfflineWorldSession</c>) into <see cref="StatusEvent"/>
    /// for the own net id. Read-only: it never writes any HUD port. Game-10 publishes only for other players.
    /// </summary>
    public sealed class LocalEffectStatusSource : IInitializable, IDisposable
    {
        private readonly IEffectState _effects;
        private readonly IEventBus _bus;
        private readonly IVfxMatchFeed _feed;
        private readonly Action<EffectChange> _onChanged;
        private bool _subscribed;

        public LocalEffectStatusSource(IEffectState effects, IEventBus bus, IVfxMatchFeed feed)
        {
            _effects = effects;
            _bus = bus;
            _feed = feed;
            _onChanged = OnChanged;
        }

        public void Initialize()
        {
            if (_subscribed || _effects == null) return;
            _effects.Changed += _onChanged;
            _subscribed = true;
        }

        public void Dispose()
        {
            if (_subscribed) _effects.Changed -= _onChanged;
            _subscribed = false;
        }

        /// <summary>Public so tests can feed changes directly.</summary>
        public void OnChanged(EffectChange change)
        {
            if (_bus == null || _feed == null || !_feed.InGame || _feed.OwnNetId == 255) return;
            var kind = StatusKinds.FromEffect(change.Kind);
            if (kind == StatusKind.None) return;
            if (change.Type == EffectChangeType.Removed)
            {
                _bus.Publish(new StatusEvent(0, _feed.OwnNetId, kind, false, 0f, 0f));
                return;
            }
            if (!_effects.TryGet(change.Kind, out var effect)) return;
            _bus.Publish(new StatusEvent(0, _feed.OwnNetId, kind, true, effect.DurationSeconds, effect.RemainingSeconds));
        }
    }
}
