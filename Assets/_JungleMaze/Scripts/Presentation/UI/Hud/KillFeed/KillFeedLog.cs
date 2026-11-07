using System;

namespace JungleMaze.Presentation.UI.Hud
{
    /// <summary>
    /// Kill feed rows (T-022, mock B9 top-left): at most <see cref="Capacity"/> entries, oldest first, newest at the
    /// bottom; a push when full drops the oldest; each entry leaves after <see cref="Seconds"/>. Pure, no allocation per push.
    /// </summary>
    public sealed class KillFeedLog<TIcon>
    {
        public readonly struct Entry
        {
            public readonly string Killer, Victim;
            public readonly TIcon Weapon;
            public readonly float Age;
            public Entry(string killer, TIcon weapon, string victim, float age) { Killer = killer; Weapon = weapon; Victim = victim; Age = age; }
        }

        private readonly Entry[] _entries;
        public int Capacity => _entries.Length;
        public float Seconds { get; }
        public int Count { get; private set; }

        public KillFeedLog(int capacity, float seconds)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            _entries = new Entry[capacity];
            Seconds = seconds;
        }

        /// <summary>Index 0 is the oldest shown row.</summary>
        public Entry this[int index] => index >= 0 && index < Count ? _entries[index] : throw new ArgumentOutOfRangeException(nameof(index));

        public void Push(string killer, TIcon weapon, string victim)
        {
            if (Count == _entries.Length) Shift(1);
            _entries[Count++] = new Entry(killer, weapon, victim, 0f);
        }

        /// <summary>Ages every entry; returns true when at least one expired (the view re-lays out only then).</summary>
        public bool Tick(float dt)
        {
            var expired = 0;
            for (var i = 0; i < Count; i++)
            {
                var e = _entries[i];
                _entries[i] = new Entry(e.Killer, e.Weapon, e.Victim, e.Age + dt);
                if (e.Age + dt >= Seconds) expired++; // oldest entries age first, so the expired ones are the leading run
            }
            if (expired > 0) Shift(expired);
            return expired > 0;
        }

        public void Clear()
        {
            Array.Clear(_entries, 0, _entries.Length);
            Count = 0;
        }

        private void Shift(int n)
        {
            for (var i = n; i < Count; i++) _entries[i - n] = _entries[i];
            for (var i = Count - n; i < Count; i++) _entries[i] = default;
            Count -= n;
        }
    }
}
