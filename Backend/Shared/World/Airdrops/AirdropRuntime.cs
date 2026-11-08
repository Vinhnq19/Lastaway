#nullable disable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
namespace JungleMaze.World.Airdrops
{
    public enum AirdropPhase : byte
    {
        Scheduled = 0,
        Flying = 1,
        Falling = 2,
        Landed = 3,
        Broken = 4,
        Cancelled = 5,
    }

    public struct AirdropState
    {
        public int DropId;
        public AirdropPhase Phase;
        public WorldPoint Position;
        public float FlyAt, FallAt, LandAt;
        public ushort WorldId;
        public uint Revision;
    }

    public interface IAirdropHost
    {
        bool TryChooseLanding(int dropId, uint seed, float landAt, out WorldPoint position);
        bool TryLand(int dropId, WorldPoint position, float size, int hp, out ushort worldId);
        bool IsBroken(ushort worldId);
    }

    /// <summary>
    /// Deterministic airdrop clock. Pure C#: the host does the world work and is only
    /// called from Tick, once per due phase, with a bounded attempt budget per phase.
    /// </summary>
    public sealed class AirdropRuntime
    {
        private readonly AirdropState[] _states;
        private readonly ReadOnlyCollection<AirdropState> _view;
        private readonly float[] _scheduledFly;
        private readonly float[] _scheduledFall;
        private readonly float[] _scheduledLand;
        private readonly int[] _selectionAttempts;
        private readonly int[] _landingAttempts;
        private readonly IAirdropHost _host;
        private readonly uint _seed;
        private readonly bool _enabled;
        private readonly float _eggSize;
        private readonly int _eggHp;
        private readonly float _retrySeconds;
        private readonly int _maxRetries;
        private float _now = -1f;

        public event Action<AirdropState> Changed;

        public IReadOnlyList<AirdropState> States { get { return _view; } }

        public AirdropRuntime(AirdropSettings settings, uint seed, IAirdropHost host)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (host == null) throw new ArgumentNullException(nameof(host));
            settings.Validate();

            _host = host;
            _seed = seed;
            _enabled = settings.Enabled;
            _eggSize = settings.EggSize;
            _eggHp = settings.EggHp;
            _retrySeconds = settings.RetrySeconds;
            _maxRetries = settings.MaxRetries;

            var drops = settings.Drops;
            var count = drops.Length;
            _scheduledFly = new float[count];
            _scheduledFall = new float[count];
            _scheduledLand = new float[count];
            _selectionAttempts = new int[count];
            _landingAttempts = new int[count];
            _states = new AirdropState[count];
            for (var i = 0; i < count; i++)
            {
                var drop = drops[i];
                _scheduledFly[i] = drop.FlyAt;
                _scheduledLand[i] = drop.LandAt;
                _scheduledFall[i] = (drop.FlyAt + drop.LandAt) * 0.5f;
                _states[i] = new AirdropState
                {
                    DropId = drop.Id,
                    Phase = AirdropPhase.Scheduled,
                    Position = default(WorldPoint),
                    FlyAt = drop.FlyAt,
                    FallAt = _scheduledFall[i],
                    LandAt = drop.LandAt,
                    WorldId = 0,
                    Revision = 0,
                };
            }
            _view = Array.AsReadOnly(_states);
        }

        /// <summary>Advances the match clock to an absolute time in seconds.</summary>
        public void Tick(float seconds)
        {
            if (!WorldMath.Finite(seconds) || seconds < 0f)
                throw new ArgumentException("Airdrop tick time must be finite and nonnegative.");
            if (seconds < _now)
                throw new ArgumentException("Airdrop tick time must not move backwards.");
            if (seconds == _now)
                return;
            _now = seconds;
            if (!_enabled)
                return;
            for (var i = 0; i < _states.Length; i++)
                TickDrop(i);
        }

        private void TickDrop(int index)
        {
            ref AirdropState state = ref _states[index];
            if (state.Phase == AirdropPhase.Scheduled)
                TryLaunch(index, ref state);
            if (state.Phase == AirdropPhase.Flying && _now >= state.FallAt)
                Publish(ref state, AirdropPhase.Falling);
            if (state.Phase == AirdropPhase.Falling)
                TryLand(index, ref state);
            if (state.Phase == AirdropPhase.Landed && _host.IsBroken(state.WorldId))
                Publish(ref state, AirdropPhase.Broken);
        }

        private void TryLaunch(int index, ref AirdropState state)
        {
            while (state.Phase == AirdropPhase.Scheduled)
            {
                if (_selectionAttempts[index] > _maxRetries)
                {
                    Publish(ref state, AirdropPhase.Cancelled);
                    return;
                }
                var delay = _selectionAttempts[index] * _retrySeconds;
                if (_now < _scheduledFly[index] + delay)
                    return;
                var landingAt = _scheduledLand[index] + delay;
                _selectionAttempts[index]++;
                WorldPoint position;
                if (_host.TryChooseLanding(state.DropId, StableSeed(_seed, state.DropId), landingAt, out position))
                {
                    state.Position = position;
                    state.FlyAt = _scheduledFly[index] + delay;
                    state.FallAt = _scheduledFall[index] + delay;
                    state.LandAt = landingAt;
                    Publish(ref state, AirdropPhase.Flying);
                    return;
                }
            }
        }

        private void TryLand(int index, ref AirdropState state)
        {
            while (state.Phase == AirdropPhase.Falling)
            {
                if (_landingAttempts[index] > _maxRetries)
                {
                    Publish(ref state, AirdropPhase.Cancelled);
                    return;
                }
                if (_now < state.LandAt)
                    return;
                _landingAttempts[index]++;
                ushort worldId;
                if (_host.TryLand(state.DropId, state.Position, _eggSize, _eggHp, out worldId) && worldId != 0)
                {
                    state.WorldId = worldId;
                    Publish(ref state, AirdropPhase.Landed);
                    return;
                }
                if (_landingAttempts[index] > _maxRetries)
                {
                    Publish(ref state, AirdropPhase.Cancelled);
                    return;
                }
                state.LandAt = state.LandAt + _retrySeconds;
                if (_now < state.LandAt)
                {
                    // Keep the visual clock coherent while waiting for the next attempt.
                    Emit(ref state);
                    return;
                }
            }
        }

        private void Publish(ref AirdropState state, AirdropPhase phase)
        {
            state.Phase = phase;
            Emit(ref state);
        }

        private void Emit(ref AirdropState state)
        {
            state.Revision++;
            var handler = Changed;
            if (handler != null) handler(state);
        }

        private static uint StableSeed(uint seed, int dropId)
        {
            unchecked
            {
                var random = new StableRandom(seed ^ ((uint)dropId * 0x9e3779b9u));
                return random.Next();
            }
        }
    }
}
