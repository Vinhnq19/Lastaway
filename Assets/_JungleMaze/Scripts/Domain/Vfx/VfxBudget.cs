namespace JungleMaze.Domain.Vfx
{
    /// <summary>
    /// Caps live VFX instances (64 by default, AC15). When full, the oldest instance with a strictly lower priority than
    /// the request is evicted; <see cref="VfxPriority.Critical"/> is therefore never evicted. Without a lower one the
    /// request is refused.
    /// </summary>
    public sealed class VfxBudget
    {
        private readonly int[] _handles;
        private readonly VfxPriority[] _priorities;
        private readonly long[] _order;
        private long _counter;

        public VfxBudget(int capacity = 64)
        {
            if (capacity < 1) capacity = 1;
            _handles = new int[capacity];
            _priorities = new VfxPriority[capacity];
            _order = new long[capacity];
        }

        public int Capacity => _handles.Length;
        public int Count { get; private set; }
        /// <summary>Requests refused because nothing lower could be evicted (for the debug panel).</summary>
        public int Refused { get; private set; }

        /// <summary>
        /// True when a slot is available; <paramref name="evictHandle"/> is non-zero when the caller must release that
        /// instance first (it has already been removed from the budget).
        /// </summary>
        public bool TryReserve(VfxPriority priority, out int evictHandle)
        {
            evictHandle = 0;
            if (Count < _handles.Length) return true;
            var victim = -1;
            for (var i = 0; i < Count; i++)
            {
                if (_priorities[i] >= priority) continue;
                if (victim < 0 || _priorities[i] < _priorities[victim] ||
                    _priorities[i] == _priorities[victim] && _order[i] < _order[victim]) victim = i;
            }
            if (victim < 0) { Refused++; return false; }
            evictHandle = _handles[victim];
            RemoveAt(victim);
            return true;
        }

        public void Add(int handle, VfxPriority priority)
        {
            if (handle == 0 || Count >= _handles.Length) return;
            _handles[Count] = handle;
            _priorities[Count] = priority;
            _order[Count] = ++_counter;
            Count++;
        }

        public void Free(int handle)
        {
            for (var i = 0; i < Count; i++)
                if (_handles[i] == handle) { RemoveAt(i); return; }
        }

        public void Clear()
        {
            Count = 0;
            Refused = 0;
        }

        private void RemoveAt(int i)
        {
            Count--;
            _handles[i] = _handles[Count];
            _priorities[i] = _priorities[Count];
            _order[i] = _order[Count];
        }
    }
}
