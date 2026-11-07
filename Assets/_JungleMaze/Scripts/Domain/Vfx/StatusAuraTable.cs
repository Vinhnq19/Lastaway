using JungleMaze.Domain.CombatFeedback;

namespace JungleMaze.Domain.Vfx
{
    /// <summary>
    /// Logical status auras keyed by (netId, kind): applying again refreshes the timer (never stacks), removal and
    /// expiry are explicit. The handle of the drawn instance is stored separately so a hidden aura (handle 0) keeps its
    /// timer and comes back as exactly one instance.
    /// </summary>
    public sealed class StatusAuraTable
    {
        private struct Entry
        {
            public byte NetId;
            public StatusKind Kind;
            public VfxCue Cue;
            public byte Tint;
            public float Remaining;
            public bool Timed;
            public int Handle;
        }

        private readonly Entry[] _entries;

        public StatusAuraTable(int capacity = 64) => _entries = new Entry[capacity < 1 ? 1 : capacity];

        public int Count { get; private set; }
        public int Capacity => _entries.Length;

        public byte NetIdAt(int index) => _entries[index].NetId;
        public StatusKind KindAt(int index) => _entries[index].Kind;
        public VfxCue CueAt(int index) => _entries[index].Cue;
        public byte TintAt(int index) => _entries[index].Tint;
        public int HandleAt(int index) => _entries[index].Handle;
        public float RemainingAt(int index) => _entries[index].Remaining;
        public void SetHandle(int index, int handle) => _entries[index].Handle = handle;

        /// <summary>
        /// Adds or refreshes. <paramref name="seconds"/> ≤ 0 means untimed. Returns the index (−1 when full) and
        /// whether the entry is new.
        /// </summary>
        public int Apply(byte netId, StatusKind kind, VfxCue cue, byte tint, float seconds, out bool added)
        {
            var i = IndexOf(netId, kind);
            added = i < 0;
            if (added)
            {
                if (Count >= _entries.Length) return -1;
                i = Count++;
                _entries[i] = new Entry { NetId = netId, Kind = kind, Cue = cue, Tint = tint };
            }
            _entries[i].Timed = seconds > 0f;
            _entries[i].Remaining = seconds > 0f ? seconds : 0f;
            return i;
        }

        public int IndexOf(byte netId, StatusKind kind)
        {
            for (var i = 0; i < Count; i++)
                if (_entries[i].NetId == netId && _entries[i].Kind == kind) return i;
            return -1;
        }

        /// <summary>Counts timers down; untimed entries never expire.</summary>
        public void Advance(float deltaTime)
        {
            for (var i = 0; i < Count; i++)
                if (_entries[i].Timed) _entries[i].Remaining -= deltaTime;
        }

        public bool IsExpired(int index) => _entries[index].Timed && _entries[index].Remaining <= 0f;

        /// <summary>Removes by swapping with the last entry; iterate backwards when removing in a loop.</summary>
        public void RemoveAt(int index)
        {
            Count--;
            _entries[index] = _entries[Count];
            _entries[Count] = default;
        }

        /// <summary>Clears every reference to a handle released elsewhere.</summary>
        public void ForgetHandle(int handle)
        {
            if (handle == 0) return;
            for (var i = 0; i < Count; i++)
                if (_entries[i].Handle == handle) _entries[i].Handle = 0;
        }

        public void Clear()
        {
            for (var i = 0; i < Count; i++) _entries[i] = default;
            Count = 0;
        }
    }
}
