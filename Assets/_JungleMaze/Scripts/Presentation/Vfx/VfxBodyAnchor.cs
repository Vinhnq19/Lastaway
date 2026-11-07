using JungleMaze.Core.DI;
using JungleMaze.Presentation.Player;
using UnityEngine;

namespace JungleMaze.Presentation.Vfx
{
    /// <summary>
    /// Marks a drawn body for VFX-01 (on <c>PlayerView.prefab</c>, <c>Hero.prefab</c> and preview dummies): which net id it
    /// is, where VFX attach, and the hit flash. The flash keeps the body colour taken when it starts; a flash on top only
    /// extends it, and the end, <see cref="CancelFlash"/> and OnDisable all restore that colour, so it never sticks.
    /// Registers itself with <see cref="VfxAnchorRegistry"/> while enabled.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class VfxBodyAnchor : MonoBehaviour
    {
        [Tooltip("Sprite tinted by the hit flash; the HeroSpriteAnimator body is used when empty.")]
        [SerializeField] private SpriteRenderer body;
        [Tooltip("True on the Hero prefab: the own body in offline play (online the own PlayerView is used).")]
        [SerializeField] private bool localHero;
        [Tooltip("Preview dummies only: fixed net id; -1 = take it from PlayerView.")]
        [SerializeField] private int previewNetId = -1;
        [SerializeField] private Color flashTint = new Color(1f, .55f, .5f, 1f);

        private VfxAnchorRegistry _registry;
        private PlayerView _view;
        private bool _registered, _flashing;
        private float _flashRemaining;
        private Color _baseColor;

        public bool LocalHero => localHero;
        public bool IsFlashing => _flashing;
        public SpriteRenderer Body => body;

        /// <summary>Net id of this body; 255 when unknown (unbound view).</summary>
        public byte NetId => previewNetId >= 0 && previewNetId < 255 ? (byte)previewNetId : _view != null ? _view.NetId : (byte)255;

        /// <summary>False while the body is hidden (dead) or inactive: nothing is drawn on it.</summary>
        public bool IsShowing => enabled && gameObject.activeInHierarchy && (body == null || body.enabled);

        [Inject]
        public void Construct(VfxAnchorRegistry registry)
        {
            if (_registered && _registry != registry) Unregister();
            _registry = registry;
            if (enabled && gameObject.activeInHierarchy) Register();
        }

        /// <summary>Set up a body created in code (preview dummies, tests).</summary>
        public void Configure(SpriteRenderer bodyRenderer, bool isLocalHero, int netId)
        {
            body = bodyRenderer;
            localHero = isLocalHero;
            previewNetId = netId;
        }

        private void Awake()
        {
            TryGetComponent(out _view);
            if (body == null)
            {
                var hero = GetComponentInChildren<HeroSpriteAnimator>(true);
                if (hero != null) body = hero.GetComponent<SpriteRenderer>();
            }
        }

        private void OnEnable() => Register();

        private void OnDisable()
        {
            CancelFlash();
            Unregister();
        }

        /// <summary>Tints the body for <paramref name="seconds"/>; while flashing, only extends the time.</summary>
        public void Flash(float seconds)
        {
            if (body == null || !(seconds > 0f)) return;
            if (!_flashing)
            {
                _baseColor = body.color;
                _flashing = true;
            }
            _flashRemaining = Mathf.Max(_flashRemaining, seconds);
            body.color = _baseColor * flashTint;
        }

        /// <summary>Ends a flash now and restores the colour taken when it started.</summary>
        public void CancelFlash()
        {
            if (!_flashing) return;
            _flashing = false;
            _flashRemaining = 0f;
            if (body != null) body.color = _baseColor;
        }

        private void Update() => Advance(Time.deltaTime);

        /// <summary>Counts the flash down; public so tests can drive it frame by frame.</summary>
        public void Advance(float dt)
        {
            if (!_flashing) return;
            _flashRemaining -= dt;
            if (_flashRemaining <= 0f) CancelFlash();
        }

        private void Register()
        {
            if (_registered || _registry == null) return;
            _registry.Add(this);
            _registered = true;
        }

        private void Unregister()
        {
            if (!_registered) return;
            _registry?.Remove(this);
            _registered = false;
        }
    }
}
