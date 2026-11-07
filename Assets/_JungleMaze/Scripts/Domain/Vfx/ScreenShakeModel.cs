using System;

namespace JungleMaze.Domain.Vfx
{
    /// <summary>
    /// Camera shake as a sum of linearly decaying pulses, capped at <see cref="MaxCells"/> and never longer than
    /// <see cref="MaxSeconds"/> per pulse. The direction comes from a hash of time (no Random, no allocation).
    /// </summary>
    public sealed class ScreenShakeModel
    {
        private const int MaxPulses = 8;

        private readonly float[] _amplitude = new float[MaxPulses];
        private readonly float[] _duration = new float[MaxPulses];
        private readonly float[] _elapsed = new float[MaxPulses];
        private int _count;
        private float _time;

        public ScreenShakeModel(float maxCells = .15f, float maxSeconds = .3f)
        {
            MaxCells = maxCells > 0f ? maxCells : 0f;
            MaxSeconds = maxSeconds > 0f ? maxSeconds : .3f;
        }

        public float MaxCells { get; }
        public float MaxSeconds { get; }
        public float OffsetX { get; private set; }
        public float OffsetY { get; private set; }
        /// <summary>Current envelope (≤ <see cref="MaxCells"/>).</summary>
        public float Amplitude { get; private set; }
        public bool Active => _count > 0;

        public void Add(float amplitudeCells, float seconds)
        {
            if (!(amplitudeCells > 0f) || !(seconds > 0f)) return;
            if (seconds > MaxSeconds) seconds = MaxSeconds;
            var slot = _count < MaxPulses ? _count++ : Weakest();
            _amplitude[slot] = Math.Min(amplitudeCells, MaxCells);
            _duration[slot] = seconds;
            _elapsed[slot] = 0f;
        }

        public void Tick(float deltaTime)
        {
            if (!(deltaTime > 0f)) deltaTime = 0f;
            _time += deltaTime;
            var total = 0f;
            for (var i = _count - 1; i >= 0; i--)
            {
                _elapsed[i] += deltaTime;
                if (_elapsed[i] >= _duration[i])
                {
                    _count--;
                    _amplitude[i] = _amplitude[_count];
                    _duration[i] = _duration[_count];
                    _elapsed[i] = _elapsed[_count];
                    continue;
                }
                total += _amplitude[i] * (1f - _elapsed[i] / _duration[i]);
            }
            Amplitude = Math.Min(total, MaxCells);
            if (Amplitude <= 0f) { OffsetX = OffsetY = 0f; return; }
            // Hash of a 60 Hz time step → angle and a 0.6..1 magnitude: |offset| ≤ Amplitude.
            var step = (uint)(_time * 60f);
            var h = unchecked(step * 2654435761u ^ 0x9E3779B9u);
            h ^= h >> 15;
            var angle = (h & 0xFFFF) / 65536f * 6.2831853f;
            var scale = .6f + .4f * ((h >> 16) & 0xFF) / 255f;
            OffsetX = (float)Math.Cos(angle) * Amplitude * scale;
            OffsetY = (float)Math.Sin(angle) * Amplitude * scale;
        }

        public void Clear()
        {
            _count = 0;
            Amplitude = OffsetX = OffsetY = 0f;
        }

        private int Weakest()
        {
            var best = 0;
            for (var i = 1; i < _count; i++)
                if (_amplitude[i] * (1f - _elapsed[i] / _duration[i]) < _amplitude[best] * (1f - _elapsed[best] / _duration[best])) best = i;
            return best;
        }
    }
}
