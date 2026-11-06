using System;
using JungleBackend.Protocol.Gameplay;
using JungleMaze.App.Match;
using JungleMaze.App.Ports;
using JungleMaze.App.World;
using JungleMaze.Core.DI;
using JungleMaze.Core.Ticking;
using JungleMaze.World.Throwables;

namespace JungleMaze.App.Combat
{
    /// <summary>Confirmed WPN-03 events for presentation, online or offline.</summary>
    public interface IThrowableEventSource
    {
        event Action<ThrowableEvent> ThrowableRaised;
    }

    /// <summary>
    /// Client side of WPN-03 in a networked match: sends the request to use the held thrown or special weapon and
    /// relays the room's confirmed events. Nothing is predicted or decided here; the server owns hits, damage and
    /// control. The one local consequence is the bell's reversed controls, which local prediction must mirror.
    /// </summary>
    public sealed class OnlineThrowableSession : IInitializable, ITickable, IDisposable, IThrowableEventSource
    {
        private readonly MatchSession _match;
        private readonly WorldClientSession _world;
        private readonly IPacketSender _sender;
        private readonly IPacketReceiver _receiver;
        private Guid _matchId;
        private uint _request;
        private double _now, _reversedUntil;

        public OnlineThrowableSession(MatchSession match, WorldClientSession world, IPacketSender sender, IPacketReceiver receiver)
        { _match = match; _world = world; _sender = sender; _receiver = receiver; }

        public event Action<ThrowableEvent> ThrowableRaised;

        public bool CanUse => _match.Connected && _match.Phase == MatchPhase.InGame && _match.OwnHp > 0 &&
            _match.CanSendGameplayInput && _world.IsReady;

        /// <summary>True while the server has this player's movement reversed by a bell.</summary>
        public bool Reversed => _now < _reversedUntil;

        public void Initialize() => _receiver.Subscribe<ThrowableEventPacket>(OnEvent);
        public void Dispose() => _receiver.Unsubscribe<ThrowableEventPacket>(OnEvent);

        public void Tick(float dt)
        {
            _now += dt;
            if (_matchId != _match.MatchId) { _matchId = _match.MatchId; _request = 0; _reversedUntil = 0; }
            // The elixir's cleanse removes the reversal on the server; immunity shows up in the effect state.
            _match.Prediction.Reversed = Reversed;
        }

        /// <summary>Sends one use of the held throwable. Returns false when the match cannot take gameplay input.</summary>
        public bool TryUse(ThrowableKind kind, float aimDegrees, float distance)
        {
            if (!CanUse || kind == ThrowableKind.None) return false;
            if (++_request == 0) _request = 1;
            var packet = new ThrowableRequestPacket
            {
                MatchId = _match.MatchId, RequestId = _request, Kind = (byte)kind,
                Aim = CombatShotMath.EncodeAim(aimDegrees), Distance = Math.Max(0f, Math.Min(64f, distance))
            };
            return _sender.Send(ref packet, PacketDelivery.ReliableOrdered);
        }

        /// <summary>Clears a reversal early (cleanse); the next server state is authoritative anyway.</summary>
        public void ClearReversal() => _reversedUntil = 0;

        private void OnEvent(ThrowableEventPacket p)
        {
            if (!_match.Connected || p.MatchId != _match.MatchId) return;
            var e = new ThrowableEvent
            {
                Kind = (ThrowableEventKind)p.Event, Item = (ThrowableKind)p.Item, Actor = p.Actor, Target = p.Target, Id = p.Id,
                FromX = p.FromX, FromY = p.FromY, ToX = p.ToX, ToY = p.ToY, Seconds = p.Seconds, Size = p.Size, Amount = p.Amount, Flags = p.Flags
            };
            if (e.Kind == ThrowableEventKind.Hit && e.Item == ThrowableKind.Bell && e.Target == _match.OwnNetId && e.Seconds > 0)
                _reversedUntil = _now + e.Seconds;
            ThrowableRaised?.Invoke(e);
        }
    }
}
