namespace JungleMaze.Domain.Vfx
{
    /// <summary>State of one attack request in the <see cref="AttackVfxLedger"/>.</summary>
    public enum AttackVfxState : byte { None, Predicted, Confirmed, Rejected }

    /// <summary>
    /// Reconciles locally predicted attack effects with the server answer, keyed by (netId, requestSeq) in a ring of
    /// 32 entries. Entries older than <see cref="MaxAgeSeconds"/> are treated as unknown, so a wrapped ushort seq is new.
    /// </summary>
    public sealed class AttackVfxLedger
    {
        public const int Capacity = 32;
        public const float MaxAgeSeconds = 5f;

        private struct Entry
        {
            public byte NetId;
            public ushort Seq;
            public AttackVfxState State;
            public int Handle;
            public float Time;
        }

        private readonly Entry[] _entries = new Entry[Capacity];
        private int _next;

        public AttackVfxState StateOf(byte netId, ushort seq, float now) =>
            Find(netId, seq, now, out var i) ? _entries[i].State : AttackVfxState.None;

        /// <summary>True if the seq is new: the caller spawns the effect and stores it with <see cref="AttachHandle"/>.</summary>
        public bool TryPredict(byte netId, ushort seq, float now)
        {
            if (Find(netId, seq, now, out _)) return false;
            _entries[_next] = new Entry { NetId = netId, Seq = seq, State = AttackVfxState.Predicted, Time = now };
            _next = (_next + 1) % Capacity;
            return true;
        }

        public void AttachHandle(byte netId, ushort seq, int handle, float now)
        {
            if (Find(netId, seq, now, out var i)) _entries[i].Handle = handle;
        }

        /// <summary>Predicted → Confirmed. Never spawns; a rejected or unknown seq stays as it is.</summary>
        public bool Confirm(byte netId, ushort seq, float now)
        {
            if (!Find(netId, seq, now, out var i) || _entries[i].State != AttackVfxState.Predicted) return false;
            _entries[i].State = AttackVfxState.Confirmed;
            return true;
        }

        /// <summary>
        /// Marks the seq rejected and returns the handle of its predicted effect (0 if none) for the caller to release.
        /// An unknown seq is recorded as rejected so a late confirmation cannot revive it.
        /// </summary>
        public int Reject(byte netId, ushort seq, float now)
        {
            if (!Find(netId, seq, now, out var i))
            {
                _entries[_next] = new Entry { NetId = netId, Seq = seq, State = AttackVfxState.Rejected, Time = now };
                _next = (_next + 1) % Capacity;
                return 0;
            }
            var handle = _entries[i].State == AttackVfxState.Predicted ? _entries[i].Handle : 0;
            _entries[i].State = AttackVfxState.Rejected;
            _entries[i].Handle = 0;
            return handle;
        }

        /// <summary>Forgets a handle released elsewhere (budget, visibility, lifetime).</summary>
        public void ForgetHandle(int handle)
        {
            if (handle == 0) return;
            for (var i = 0; i < Capacity; i++)
                if (_entries[i].Handle == handle) _entries[i].Handle = 0;
        }

        public void Clear()
        {
            for (var i = 0; i < Capacity; i++) _entries[i] = default;
            _next = 0;
        }

        private bool Find(byte netId, ushort seq, float now, out int index)
        {
            for (var i = 0; i < Capacity; i++)
            {
                ref var e = ref _entries[i];
                if (e.State == AttackVfxState.None || e.NetId != netId || e.Seq != seq || now - e.Time > MaxAgeSeconds) continue;
                index = i;
                return true;
            }
            index = -1;
            return false;
        }
    }
}
