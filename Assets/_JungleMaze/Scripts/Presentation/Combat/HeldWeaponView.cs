using JungleMaze.App.Hud.State;
using JungleMaze.Core.DI;
using JungleMaze.Domain.Hud;
using JungleMaze.Presentation.Player;
using UnityEngine;

namespace JungleMaze.Presentation.Combat
{
    /// <summary>
    /// Draws the weapon in the hero's hand: at reach along the aim, rotated with it, mirrored when aiming left and
    /// behind the body when the hero faces away. Hidden for bare hands, while dead, and during a melee swing (the
    /// swing visual draws the weapon then). Ranged and thrown attacks kick it back briefly. Presentation only.
    /// The local hero follows <see cref="IHudState.HeldWeapon"/>; remote players show what they last attacked with.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HeldWeaponView : MonoBehaviour
    {
        [SerializeField] private HeldWeaponCatalog catalog;
        [SerializeField] private SpriteRenderer weaponRenderer;
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private HeroSpriteAnimator bodyAnimator;
        /// <summary>Only the local Hero mirrors the HUD's held weapon.</summary>
        [SerializeField] private bool followLocalHud;
        /// <summary>Hand distance from the hero centre along the aim, and how far below the centre the hand sits.</summary>
        [SerializeField] private float reach = .3f;
        [SerializeField] private float handDrop = .12f;
        [SerializeField] private float kickDistance = .12f;
        [SerializeField] private float kickSeconds = .12f;

        private IHudState _hud;
        private HeldWeaponCatalog.Entry _entry;
        private bool _hasEntry;
        private float _hiddenFor, _kick;

        public HeldWeaponCatalog Catalog => catalog;
        public string ItemId { get; private set; }
        public bool Visible => weaponRenderer != null && weaponRenderer.enabled;

        [Inject]
        public void Construct(IHudState hud)
        {
            if (!followLocalHud) return;
            if (_hud != null) _hud.Changed -= OnHudChanged;
            _hud = hud;
            if (_hud == null) return;
            _hud.Changed += OnHudChanged;
            Show(_hud.HeldWeapon.ItemId);
        }

        private void OnDestroy()
        {
            if (_hud != null) _hud.Changed -= OnHudChanged;
        }

        private void OnHudChanged(HudStateChange change) => Show(_hud.HeldWeapon.ItemId);

        /// <summary>Holds this item; unknown IDs and bare hands hide the weapon.</summary>
        public void Show(string itemId)
        {
            if (itemId == ItemId && (_hasEntry || string.IsNullOrEmpty(itemId))) return;
            ItemId = itemId;
            _hasEntry = catalog != null && catalog.TryGet(itemId, out _entry);
            if (weaponRenderer == null) return;
            weaponRenderer.sprite = _hasEntry ? _entry.Sprite : null;
            if (!_hasEntry) return;
            var scale = _entry.Scale;
            var t = weaponRenderer.transform;
            t.localScale = new Vector3(scale, scale, 1f);
            t.localRotation = Quaternion.Euler(0f, 0f, _entry.AngleOffset);
            t.localPosition = -(Vector3)(t.localRotation * (Vector3)(_entry.GripLocal * scale));
            Apply();
        }

        /// <summary>Melee: hide while the swing visual draws the weapon. Otherwise: a short recoil.</summary>
        public void OnAttack(bool melee, float swingSeconds)
        {
            if (melee) _hiddenFor = Mathf.Max(_hiddenFor, swingSeconds);
            else _kick = kickSeconds;
        }

        private void LateUpdate()
        {
            _hiddenFor = Mathf.Max(0f, _hiddenFor - Time.deltaTime);
            _kick = Mathf.Max(0f, _kick - Time.deltaTime);
            Apply();
        }

        /// <summary>Places the hand for the root's current aim; exposed for deterministic presentation tests.</summary>
        public void Apply()
        {
            if (weaponRenderer == null) return;
            var root = transform.parent != null ? transform.parent : transform;
            var aim = Mathf.DeltaAngle(0f, root.eulerAngles.z);
            var direction = new Vector3(Mathf.Cos(aim * Mathf.Deg2Rad), Mathf.Sin(aim * Mathf.Deg2Rad), 0f);
            var recoil = kickSeconds > 0f ? kickDistance * (_kick / kickSeconds) : 0f;
            transform.SetPositionAndRotation(root.position + direction * (reach - recoil) + Vector3.down * handDrop,
                Quaternion.Euler(0f, 0f, aim));
            transform.localScale = new Vector3(1f, Mathf.Abs(aim) > 90f ? -1f : 1f, 1f);
            var bodyVisible = body == null || body.enabled;
            weaponRenderer.enabled = _hasEntry && bodyVisible && _hiddenFor <= 0f;
            if (body != null)
            {
                weaponRenderer.sortingLayerID = body.sortingLayerID;
                var behind = bodyAnimator != null && bodyAnimator.Facing == HeroFacing.Back;
                weaponRenderer.sortingOrder = body.sortingOrder + (behind ? -1 : 1);
            }
        }
    }
}
