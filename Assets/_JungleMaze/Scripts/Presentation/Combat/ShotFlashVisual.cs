using JungleMaze.Core.Pooling;
using JungleMaze.Presentation.World.Phase2;
using UnityEngine;

namespace JungleMaze.Presentation.Combat
{
    /// <summary>
    /// Pooled one-shot muzzle flash (e.g. the JSurviv shotgun blast cone). It plays its frames once at the barrel,
    /// pointing along the aim and starting at the given point, then the caller's pool despawns it. Visual only.
    /// </summary>
    public sealed class ShotFlashVisual : MonoBehaviour, IPoolable
    {
        [SerializeField] private SpriteRenderer spriteRenderer;

        private WorldVisualSet.Frames _frames;
        private float _elapsed;
        private Vector3 _velocity;
        private float _spin;

        public float Duration => _frames != null && _frames.Duration > 0f ? _frames.Duration : .2f;

        private void Awake()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
        }

        /// <summary>Starts the flash with its back edge at <paramref name="start"/>, growing along the aim.</summary>
        public void Play(WorldVisualSet.Frames frames, Vector3 start, float aimDegrees, float scale)
        {
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
            _frames = frames;
            _elapsed = 0f;
            _velocity = Vector3.zero;
            _spin = 0f;
            transform.SetPositionAndRotation(start, Quaternion.Euler(0f, 0f, aimDegrees));
            if (spriteRenderer == null) return;
            var first = frames?.Sample(0f);
            spriteRenderer.sprite = first;
            spriteRenderer.enabled = first != null;
            var t = spriteRenderer.transform;
            t.localScale = new Vector3(scale, scale, 1f);
            t.localPosition = first != null ? new Vector3(-first.bounds.min.x * scale, -first.bounds.center.y * scale, 0f) : Vector3.zero;
        }

        private void Update() => Advance(Time.deltaTime);

        /// <summary>Visual-only casing, reusing the same pool and lifetime as flash clips.</summary>
        public void Eject(WorldVisualSet.Frames frames, Vector3 start, float aimDegrees, float scale)
        {
            Play(frames, start, aimDegrees, scale);
            float angle = aimDegrees * Mathf.Deg2Rad;
            _velocity = new Vector3(-Mathf.Sin(angle), Mathf.Cos(angle), 0f) * .8f;
            _spin = 420f;
        }

        /// <summary>Advances the flash; exposed for deterministic presentation tests.</summary>
        public void Advance(float seconds)
        {
            if (_frames == null || spriteRenderer == null || seconds <= 0f) return;
            _elapsed += seconds;
            transform.position += _velocity * seconds;
            transform.Rotate(0f, 0f, _spin * seconds);
            var frame = _frames.Sample(_elapsed);
            if (frame != null) spriteRenderer.sprite = frame;
            if (_elapsed >= Duration) spriteRenderer.enabled = false;
        }

        public void OnSpawned() { _frames = null; _elapsed = 0f; }
        public void OnDespawned() { _frames = null; if (spriteRenderer != null) spriteRenderer.enabled = false; }
    }
}
