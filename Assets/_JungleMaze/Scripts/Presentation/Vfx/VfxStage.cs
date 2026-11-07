using System;
using JungleMaze.App.Pooling;
using JungleMaze.App.Ports;
using JungleMaze.App.Vfx;
using JungleMaze.Core.DI;
using JungleMaze.Domain.Vfx;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JungleMaze.Presentation.Vfx
{
    /// <summary>
    /// <see cref="IVfxOutput"/> over the CLI-03 pool (plan 4.1): one <see cref="PooledPrefabFactory{T}"/> per cue, a
    /// fixed table of 64 slots with a generation per slot (stale handles are ignored), instances parented to this
    /// DontDestroyOnLoad object so a scene change never destroys them, and anchored instances moved onto their body
    /// every LateUpdate (order 500, after <c>PlayerView</c>). It never uses delayed despawn: the director owns lifetime.
    /// </summary>
    [DefaultExecutionOrder(500)]
    public sealed class VfxStage : MonoBehaviour, IVfxOutput
    {
        public const int SlotCount = 64;
        private const int CueSlots = 256;

        private struct Slot
        {
            public bool Used;
            public VfxParticleEffect Instance;
            public VfxCue Cue;
            public byte Anchor;
            public int Generation;
        }

        private sealed class CueBinding
        {
            public PooledPrefabFactory<VfxParticleEffect> Factory;
            public Vector2 Offset;
        }

        private readonly Slot[] _slots = new Slot[SlotCount];
        private readonly CueBinding[] _cues = new CueBinding[CueSlots];
        private IPoolService _pool;
        private VfxAnchorRegistry _anchors;
        private VfxCatalog _catalog;
        private Transform _root;
        private int _active;

        /// <summary>Raised when the active scene changes (scene unload); the composition resets the director on it.</summary>
        public event Action SceneChanged;

        public int ActiveCount => _active;
        public VfxTuningValues Tuning { get; private set; } = VfxTuningValues.Default;
        public VfxCatalog Catalog => _catalog;
        public Transform Root => _root != null ? _root : transform;

        [Inject]
        public void Construct(IPoolService pool, VfxAnchorRegistry anchors)
        {
            _pool = pool;
            _anchors = anchors;
        }

        /// <summary>Builds the factories from the catalog and prewarms the pool. Call once after injection.</summary>
        public VfxStage Configure(VfxCatalog catalog, VfxTuningValues tuning)
        {
            _catalog = catalog;
            Tuning = tuning;
            if (_root == null)
            {
                _root = new GameObject("[VFX]").transform;
                _root.SetParent(transform, false);
            }
            Array.Clear(_cues, 0, _cues.Length);
            if (catalog == null || _pool == null) return this;
            foreach (var entry in catalog.Cues)
            {
                if (entry == null || entry.prefab == null || entry.cue == VfxCue.None) continue;
                _cues[(int)entry.cue] = new CueBinding
                {
                    Factory = new PooledPrefabFactory<VfxParticleEffect>(_pool, entry.prefab, entry.prewarm),
                    Offset = entry.anchorOffset
                };
            }
            return this;
        }

        /// <summary>The prefab a cue spawns (tests count pool instances with it).</summary>
        public VfxParticleEffect PrefabOf(VfxCue cue) => _cues[(int)cue]?.Factory.Prefab;

        /// <summary>Live instance behind a handle, or null (tests, debug panel).</summary>
        public VfxParticleEffect InstanceOf(int handle) => TryResolve(handle, out var slot) ? _slots[slot].Instance : null;

        public int Spawn(VfxCue cue, in VfxSpawn spawn)
        {
            var binding = _cues[(int)cue];
            if (binding == null) return 0;
            var slot = -1;
            for (var i = 0; i < SlotCount; i++)
                if (!_slots[i].Used) { slot = i; break; }
            if (slot < 0) return 0;
            var position = Place(spawn.X, spawn.Y, spawn.AnchorNetId, binding.Offset);
            var instance = binding.Factory.Create(new SpawnPose(position, Quaternion.identity, Root));
            if (instance == null) return 0;
            ref var s = ref _slots[slot];
            s.Used = true;
            s.Instance = instance;
            s.Cue = cue;
            s.Anchor = spawn.AnchorNetId;
            s.Generation = (s.Generation + 1) & 0x7FFFFF;
            _active++;
            var tint = Color.white;
            var hasTint = _catalog != null && _catalog.TryGetTint(spawn.Tint, out tint);
            instance.Begin(spawn, tint, hasTint, this);
            return (slot + 1) | (s.Generation << 8);
        }

        public void Release(int handle)
        {
            if (!TryResolve(handle, out var slot)) return;
            Free(slot);
        }

        public void ReleaseAll()
        {
            for (var i = 0; i < SlotCount; i++)
                if (_slots[i].Used) Free(i);
        }

        public void Flash(byte netId, float seconds)
        {
            if (_anchors != null && _anchors.TryGetAnchor(netId, out var anchor)) anchor.Flash(seconds);
        }

        public void CancelFlash(byte netId)
        {
            if (_anchors != null && _anchors.TryGetAnchor(netId, out var anchor)) anchor.CancelFlash();
        }

        private void OnEnable() => SceneManager.activeSceneChanged += OnActiveSceneChanged;

        private void OnDisable() => SceneManager.activeSceneChanged -= OnActiveSceneChanged;

        private void OnDestroy()
        {
            // The pool root may already be gone at shutdown; just forget the slots.
            for (var i = 0; i < SlotCount; i++) _slots[i] = default;
            _active = 0;
        }

        private void OnActiveSceneChanged(Scene from, Scene to)
        {
            ReleaseAll();
            SceneChanged?.Invoke();
        }

        private void LateUpdate() => Drive();

        /// <summary>Moves anchored instances onto their body; public so PlayMode tests can drive it frame by frame.</summary>
        public void Drive()
        {
            for (var i = 0; i < SlotCount; i++)
            {
                ref var s = ref _slots[i];
                if (!s.Used || s.Instance == null || s.Anchor == VfxSpawn.NoAnchor) continue;
                if (_anchors == null || !_anchors.TryGetPosition(s.Anchor, out var x, out var y)) continue;
                var binding = _cues[(int)s.Cue];
                s.Instance.transform.position = Place(x, y, s.Anchor, binding != null ? binding.Offset : Vector2.zero);
            }
        }

        private Vector3 Place(float x, float y, byte anchor, Vector2 offset)
        {
            if (anchor != VfxSpawn.NoAnchor && _anchors != null && _anchors.TryGetPosition(anchor, out var ax, out var ay))
            {
                x = ax;
                y = ay;
            }
            return new Vector3(x + offset.x, y + offset.y, 0f);
        }

        private bool TryResolve(int handle, out int slot)
        {
            slot = (handle & 0xFF) - 1;
            if (handle == 0 || slot < 0 || slot >= SlotCount) return false;
            ref var s = ref _slots[slot];
            return s.Used && s.Generation == (handle >> 8);
        }

        private void Free(int slot)
        {
            ref var s = ref _slots[slot];
            var instance = s.Instance;
            s.Used = false;
            s.Instance = null;
            s.Cue = VfxCue.None;
            _active--;
            // A destroyed instance (scene teardown, play mode exit) is skipped instead of logging a pool warning.
            if (instance != null && _pool != null) _pool.Despawn(instance);
        }
    }
}
