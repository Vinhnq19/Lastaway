using JungleMaze.App.Player;
using JungleMaze.App.Ports;
using JungleMaze.Core.DI;
using JungleMaze.Core.Pooling;
using JungleMaze.Presentation.World.Phase2;
using UnityEngine;

namespace JungleMaze.Presentation.World
{
    /// <summary>
    /// Pooled local acceptance projectile. Sweep the complete step so even a fast shot cannot tunnel
    /// through a thin wall. Hit or range exhaustion returns the object to the pool, without damage/network.
    /// The shot always faces its flight direction; <see cref="SetLook"/> swaps in per-weapon art without touching the sweep.
    /// </summary>
    public sealed class CoverProbeProjectile : MonoBehaviour, IPoolable
    {
        // Encloses the entire 0.18 x 0.08 probe sprite, including its leading edge.
        private const float Radius = .1f;
        private IPoolService _pool;
        private Vector2 _direction;
        private float _speed, _remaining;
        private bool _flying;
        public Collider2D LastHit { get; private set; }
        public float Damage { get; private set; }
        public float Speed => _speed;
        public float RemainingRange => _remaining;
        public System.Action<Vector3> HitEffect { private get; set; }

        private SpriteRenderer _renderer;
        private Sprite _defaultSprite;
        private Color _defaultColor = Color.white;
        private Vector3 _defaultScale = Vector3.one;
        private bool _cached;
        private WorldVisualSet.Frames _frames;
        private float _angleOffset, _spin, _lookElapsed;

        [Inject]
        public void Construct(IPoolService pool) => _pool = pool;

        public void Launch(in ConfiguredShot shot, Vector2 direction)
        {
            _direction = direction.normalized;
            _speed = shot.Speed;
            _remaining = shot.Range;
            Damage = shot.Damage;
            _flying = true;
            _lookElapsed = 0f;
            Orient();
        }

        /// <summary>
        /// Visual only: draws the shot with these frames (null keeps the default tracer), uniform scale, an angle
        /// that turns the drawn axis onto the flight direction, an optional spin and a tint. Hit sweep is unchanged.
        /// </summary>
        public void SetLook(WorldVisualSet.Frames frames, float scale, float angleOffset, float spinDegreesPerSecond, Color tint)
        {
            CacheDefaults();
            if (_renderer == null) return;
            var hasFrames = frames != null && frames.Sprites != null && frames.Sprites.Length > 0;
            _frames = hasFrames ? frames : null;
            _angleOffset = angleOffset;
            _spin = spinDegreesPerSecond;
            _lookElapsed = 0f;
            if (hasFrames)
            {
                _renderer.sprite = frames.Sample(0f);
                transform.localScale = new Vector3(scale, scale, 1f);
                _renderer.color = Color.white;
            }
            else
            {
                _renderer.sprite = _defaultSprite;
                transform.localScale = _defaultScale;
                _renderer.color = tint;
            }
            Orient();
        }

        /// <summary>Back to the plain tracer of the prefab.</summary>
        public void ResetLook()
        {
            CacheDefaults();
            _frames = null; _angleOffset = 0f; _spin = 0f;
            if (_renderer == null) return;
            _renderer.sprite = _defaultSprite;
            _renderer.color = _defaultColor;
            transform.localScale = _defaultScale;
        }

        private void CacheDefaults()
        {
            if (_cached) return;
            _cached = true;
            _renderer = GetComponentInChildren<SpriteRenderer>(true);
            _defaultScale = transform.localScale;
            if (_renderer == null) return;
            _defaultSprite = _renderer.sprite;
            _defaultColor = _renderer.color;
        }

        private void Orient()
        {
            if (_direction.sqrMagnitude < 1e-6f) return;
            var angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg + _angleOffset + _spin * _lookElapsed;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void Awake() => CacheDefaults();

        private void Update()
        {
            if (!_flying) return;
            _lookElapsed += Time.deltaTime;
            if (_frames != null && _renderer != null) { var frame = _frames.Sample(_lookElapsed); if (frame != null) _renderer.sprite = frame; }
            if (_spin != 0f) Orient();
        }

        private void FixedUpdate() => Advance(Time.fixedDeltaTime);

        public void Advance(float deltaTime)
        {
            if (!_flying || deltaTime <= 0) return;
            var step = Mathf.Min(_remaining, _speed * deltaTime);
            var start = (Vector2)transform.position;
            var hit = CoverQuery.Sweep(start, _direction, step, Radius);
            if (hit.collider != null)
            {
                LastHit = hit.collider;
                transform.position = hit.centroid;
                HitEffect?.Invoke(hit.point);
                Finish();
                return;
            }
            transform.position = start + _direction * step;
            _remaining -= step;
            if (_remaining <= .00001f) Finish();
        }

        private void Finish()
        {
            _flying = false;
            _pool.Despawn(this);
        }

        public void OnSpawned() { _flying = false; LastHit = null; HitEffect = null; ResetLook(); }
        public void OnDespawned() { _flying = false; HitEffect = null; ResetLook(); }
    }
}
