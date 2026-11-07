using System;
using JungleBackend.Protocol.Gameplay;
using JungleMaze.App.Match;
using JungleMaze.App.Combat;
using JungleMaze.App.Hud.State;
using JungleMaze.World;
using JungleMaze.World.Throwables;
using JungleMaze.App.World;
using JungleMaze.App.Ports;
using JungleMaze.Core.DI;
using JungleMaze.Core.Ticking;
using JungleMaze.Domain.Audio;

namespace JungleMaze.App.Audio
{
    /// <summary>Read-only view of the match state the audio adapter needs; lets tests fake the match without a network.</summary>
    public interface IMatchAudioFeed
    {
        /// <summary>255 = unknown.</summary>
        byte OwnNetId { get; }
        bool TryGetZone(out bool inWarning, out byte phaseIndex);
    }

    /// <summary><see cref="IMatchAudioFeed"/> over the live <see cref="MatchSession"/>.</summary>
    public sealed class MatchSessionAudioFeed : IMatchAudioFeed
    {
        private readonly MatchSession _match;

        private readonly OfflineMatchSession _offline;
        public MatchSessionAudioFeed(MatchSession match, OfflineMatchSession offline = null)
        { _match = match; _offline = offline; if (match == null && offline == null) throw new ArgumentNullException(nameof(match)); }

        public byte OwnNetId => _offline?.IsActive == true ? (byte)0 : (_match?.OwnNetId ?? (byte)255);

        public bool TryGetZone(out bool inWarning, out byte phaseIndex)
        {
            if (_offline?.IsActive == true)
            { inWarning = _offline.Zone.InWarning; phaseIndex = _offline.Zone.PhaseIndex; return true; }
            var has = _match != null && _match.HasZone;
            var zone = _match != null ? _match.Zone : default;
            inWarning = has && zone.InWarning;
            phaseIndex = has ? zone.PhaseIndex : (byte)0;
            return has;
        }
    }

    /// <summary>
    /// Routes real combat, movement, and match state into cues without changing simulation ownership:
    /// <c>EventPacket.PlayerDied</c> → <see cref="SfxId.Eliminated"/> (2D for the local player, at the victim's pose
    /// otherwise) and the zone warning rising edge → <see cref="SfxId.ZoneBell3"/>, once per <c>PhaseIndex</c>.
    /// PlayerHit/ItemPickedUp/MatchEnded are deliberately not mapped: those sounds come from the HUD ports or
    /// AppScreenChanged, and mapping both would play them twice (T-8).
    /// </summary>
    public sealed class MatchAudioEventSource : IAudioEventSource, IInitializable, ITickable, IDisposable
    {
        public const byte KindPlayerDied = 1;
        public const byte KindZoneWarning = 2;
        private const int NoPhase = -1;

        private readonly IPacketReceiver _receiver;
        private readonly IMatchAudioFeed _feed;
        private readonly IPlayerPoseSource _poses;
        private readonly Action<EventPacket> _onEvent;
        private bool _wasWarning;
        private int _belledPhase = NoPhase;
        private bool _subscribed;
        private readonly OfflineMatchSession _offline;
        private bool _wasOfflineDead;
        private OnlineCombatSession _combat;
        private IThrowableEventSource _throwables;
        private OfflineWorldSession _offlineWorld;
        private WorldClientSession _world;
        private IHudState _hud;
        private bool _hasMotionPose, _inBush;
        private float _lastX, _lastY, _stepDistance;

        public MatchAudioEventSource(IPacketReceiver receiver, IMatchAudioFeed feed, IPlayerPoseSource poses, OfflineMatchSession offline = null)
        {
            _offline = offline;
            _receiver = receiver;
            _feed = feed;
            _poses = poses;
            _onEvent = OnEvent;
        }

        public event Action<AudioCue> CueRaised;

        public void Initialize()
        {
            if (_subscribed || _receiver == null) return;
            _receiver.Subscribe(_onEvent);
            _subscribed = true;
        }

        public void Dispose()
        {
            BindCombat(null, null);
            if (_subscribed) _receiver.Unsubscribe(_onEvent);
            _subscribed = false;
        }

        // ComposeWorld runs after audio; rebind safely when a world is recreated.
        public void BindCombat(OnlineCombatSession online, OfflineWorldSession offline, WorldClientSession world = null, IHudState hud = null, IThrowableEventSource throwables = null)
        {
            if (_combat != null) _combat.Changed -= OnOnlineCombat;
            if (_offlineWorld != null) _offlineWorld.CombatChanged -= OnOfflineCombat;
            if (_throwables != null) _throwables.ThrowableRaised -= OnOnlineThrow;
            if (_offlineWorld != null) _offlineWorld.ThrowableRaised -= OnOfflineThrow;
            _throwables = throwables;
            _combat = online; _offlineWorld = offline; _world = world; _hud = hud;
            _hasMotionPose = false; _stepDistance = 0; _inBush = false;
            if (_throwables != null) _throwables.ThrowableRaised += OnOnlineThrow;
            if (_offlineWorld != null) _offlineWorld.ThrowableRaised += OnOfflineThrow;
            if (_combat != null) _combat.Changed += OnOnlineCombat;
            if (_offlineWorld != null) _offlineWorld.CombatChanged += OnOfflineCombat;
        }

        private void OnOnlineThrow(ThrowableEvent value)
        { if (_offlineWorld?.IsActive != true) OnThrowable(value); }
        private void OnOfflineThrow(ThrowableEvent value)
        { if (_offlineWorld?.IsActive == true) OnThrowable(value); }

        public void OnThrowable(ThrowableEvent value)
        {
            if (value.Kind != ThrowableEventKind.Thrown) return;
            CueRaised?.Invoke(AudioCue.At(SfxId.Throw, value.FromX, value.FromY, value.Actor,
                SfxDeduper.Key(7, value.Actor, value.Id, (uint)value.Item)));
        }

        private void OnOnlineCombat(CombatEvent value)
        { if (_offlineWorld?.IsActive != true) OnCombat(value); }
        private void OnOfflineCombat(CombatEvent value)
        { if (_offlineWorld?.IsActive == true) OnCombat(value); }

        /// <summary>Maps real controller events; confirmation never replays a locally predicted shot.</summary>
        public void OnCombat(CombatEvent value)
        {
            var own = _feed != null && value.NetId == _feed.OwnNetId;
            if (value.Kind == CombatEventKind.EmptyFire && own)
            {
                CueRaised?.Invoke(AudioCue.Global(SfxId.EmptyClick, value.NetId,
                    SfxDeduper.Key(5, value.NetId, value.RequestSeq)));
                return;
            }
            if (value.Kind == CombatEventKind.MeleeImpact)
            {
                EmitImpact(value.NetId, SfxDeduper.Key(6, value.NetId, value.RequestSeq));
                return;
            }
            var shot = value.Kind == CombatEventKind.AttackPredicted && own ||
                       value.Kind == CombatEventKind.RemoteAttackConfirmed && !own;
            var reload = value.Kind == CombatEventKind.ReloadStarted && own ||
                         value.Kind == CombatEventKind.RemoteReloadStarted && !own;
            if (value.Outcome != CombatOutcome.Accepted || (!shot && !reload) ||
                (byte)value.Weapon > (byte)CombatWeaponId.Shotgun) return;
            var id = reload ? SfxId.Reload : value.Weapon == CombatWeaponId.Crossbow ? SfxId.ShootCrossbow :
                     (byte)value.Weapon >= (byte)CombatWeaponId.RubberGun ? SfxId.ShootRanged : SfxId.MeleeSwing;
            var key = SfxDeduper.Key(reload ? (byte)4 : (byte)3, value.NetId, value.RequestSeq, (uint)value.Weapon);
            if (own) CueRaised?.Invoke(AudioCue.Global(id, value.NetId, key));
            else if (_poses != null && _poses.TryGetPose(value.NetId, out var pose))
                CueRaised?.Invoke(AudioCue.At(id, pose.X, pose.Y, value.NetId, key));
        }

        private void EmitImpact(byte actor, ulong key)
        {
            if (_feed != null && actor == _feed.OwnNetId)
                CueRaised?.Invoke(AudioCue.Global(SfxId.MeleeHit, actor, key));
            else if (_poses != null && _poses.TryGetPose(actor, out var pose))
                CueRaised?.Invoke(AudioCue.At(SfxId.MeleeHit, pose.X, pose.Y, actor, key));
        }

        public void Tick(float deltaTime)
        {
            TickMovement();
            var dead = _offline?.IsActive == true && _offline.IsDead;
            if (dead && !_wasOfflineDead) CueRaised?.Invoke(AudioCue.Global(SfxId.Eliminated));
            _wasOfflineDead = dead;
            if (_feed == null) return;
            if (!_feed.TryGetZone(out var warning, out var phase))
            {
                // No zone (between matches): the next match may reuse phase numbers.
                _wasWarning = false;
                _belledPhase = NoPhase;
                return;
            }
            if (warning && !_wasWarning && phase != _belledPhase)
            {
                _belledPhase = phase;
                CueRaised?.Invoke(AudioCue.Global(SfxId.ZoneBell3, 0, SfxDeduper.Key(KindZoneWarning, phase)));
            }
            _wasWarning = warning;
        }

        private void TickMovement()
        {
            var solo = _offline?.IsActive == true;
            if (_feed == null || _hud == null || !_hud.HasPose || _hud.Hp <= 0 ||
                (solo ? _offline.IsDead || _offlineWorld?.IsReady != true : _combat?.CanAct != true))
            { _hasMotionPose = false; _stepDistance = 0; _inBush = false; return; }
            var pose = _hud.Pose;
            var objects = solo ? _offlineWorld.Objects : _world?.Objects;
            var bush = false; var swamp = false;
            if (objects != null) foreach (var item in objects)
            {
                if ((item.Flags & 1) != 0 || System.Math.Abs(pose.X - item.X) > item.Width * .5f ||
                    System.Math.Abs(pose.Y - item.Y) > item.Height * .5f) continue;
                bush |= item.Kind == (byte)WorldObjectKind.Bush;
                swamp |= item.Kind == (byte)WorldObjectKind.Swamp;
            }
            if (_hasMotionPose)
            {
                var dx = pose.X - _lastX; var dy = pose.Y - _lastY;
                var distance = (float)System.Math.Sqrt(dx * dx + dy * dy);
                // Portal/resume jumps are not footsteps; idle frames never generate a step.
                if (distance > 2) _stepDistance = 0;
                else if (distance > .001f)
                {
                    if (bush && !_inBush) CueRaised?.Invoke(AudioCue.Global(SfxId.BushEnter, _feed.OwnNetId));
                    _stepDistance += distance;
                    if (_stepDistance >= 1)
                    {
                        _stepDistance %= 1;
                        CueRaised?.Invoke(AudioCue.Global(swamp ? SfxId.FootstepSwamp : SfxId.FootstepGrass, _feed.OwnNetId));
                    }
                }
            }
            _hasMotionPose = true; _lastX = pose.X; _lastY = pose.Y; _inBush = bush;
        }

        /// <summary>Handles one server event; public so tests can feed packets without a network.</summary>
        public void OnEvent(EventPacket packet)
        {
            if (packet.EventType == GameEventType.MeleeImpact)
            {
                EmitImpact(packet.SourceNetId, SfxDeduper.Key(6, packet.SourceNetId, packet.Tick));
                return;
            }
            if (packet.EventType != GameEventType.PlayerDied || packet.TargetNetId == EventPacket.NoTarget) return;
            var key = SfxDeduper.Key(KindPlayerDied, packet.Tick, packet.SourceNetId, packet.TargetNetId);
            var target = packet.TargetNetId;
            if (_feed != null && target == _feed.OwnNetId)
            {
                CueRaised?.Invoke(AudioCue.Global(SfxId.Eliminated, target, key));
                return;
            }
            if (_poses != null && _poses.TryGetPose(target, out var pose))
                CueRaised?.Invoke(AudioCue.At(SfxId.Eliminated, pose.X, pose.Y, target, key));
        }
    }
}
