using JungleMaze.Presentation.World.Phase2;
using UnityEngine;

namespace JungleMaze.Presentation.Player
{
    /// <summary>
    /// Draws the hero body from <see cref="HeroVisualSet"/> frames. Sits on the body child: the root keeps rotating to
    /// the aim angle (gameplay and tests read it), while the body stays upright, picks a facing from that angle and
    /// switches idle/move from how far the root moved. Presentation only; it never moves the hero.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class HeroSpriteAnimator : MonoBehaviour
    {
        [SerializeField] private HeroVisualSet visuals;
        [SerializeField] private int heroIndex;
        /// <summary>Root speed (cells/s) above which the move loop plays.</summary>
        [SerializeField] private float moveThreshold = 0.25f;

        private SpriteRenderer _renderer;
        private HeroVisualSet.Hero _hero;
        private WorldVisualSet.Frames _frames;
        private Vector3 _lastRootPosition;
        private float _elapsed;
        private float _speed;
        private bool _hasLast;

        public bool HasVisuals => _hero != null;
        public HeroFacing Facing { get; private set; } = HeroFacing.Front;
        public bool Moving { get; private set; }

        /// <summary>Chooses the look; the index wraps over the set.</summary>
        public void SetHero(int index)
        {
            heroIndex = index;
            Resolve();
        }

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            Resolve();
        }

        private void OnEnable() => _hasLast = false;

        private void Resolve()
        {
            if (_renderer == null) _renderer = GetComponent<SpriteRenderer>();
            _hero = visuals != null ? visuals.At(heroIndex) : null;
            _frames = null;
            if (_hero == null) return;
            var sample = _hero.IdleFront != null ? _hero.IdleFront.Sample(0) : null;
            if (sample != null && sample.bounds.size.y > 0)
            {
                var scale = visuals.FrameWorldHeight / sample.bounds.size.y;
                transform.localScale = new Vector3(scale, scale, 1f);
            }
            Step(0f);
        }

        private void LateUpdate() => Step(Time.deltaTime);

        /// <summary>Advances the body; exposed for deterministic presentation tests.</summary>
        public void Step(float seconds)
        {
            if (_hero == null || _renderer == null) return;
            var root = transform.parent != null ? transform.parent : transform;
            var position = root.position;
            if (_hasLast && seconds > 0f)
            {
                var instant = (position - _lastRootPosition).magnitude / seconds;
                _speed = Mathf.Lerp(_speed, instant, 1f - Mathf.Exp(-20f * seconds));
            }
            _lastRootPosition = position;
            _hasLast = true;
            transform.rotation = Quaternion.identity;

            var angle = Mathf.DeltaAngle(0f, root.eulerAngles.z);
            Facing = angle > 45f && angle < 135f ? HeroFacing.Back : angle < -45f && angle > -135f ? HeroFacing.Front : HeroFacing.Side;
            Moving = _speed > moveThreshold;
            var next = _hero.Get(Moving, Facing) ?? _hero.IdleFront;
            if (next != _frames) { _frames = next; _elapsed = 0f; }
            else _elapsed += Mathf.Max(0f, seconds);
            _renderer.flipX = Facing == HeroFacing.Side && Mathf.Abs(angle) > 90f;
            var frame = _frames?.Sample(_elapsed);
            if (frame != null) _renderer.sprite = frame;
        }
    }
}
