using JungleBackend.Protocol.Gameplay;
using JungleMaze.App.Combat;
using JungleMaze.App.Hud.State;
using JungleMaze.App.World;
using JungleMaze.Core.DI;
using JungleMaze.Domain.Hud;
using JungleMaze.Presentation.Player;
using UnityEngine;

namespace JungleMaze.Presentation.Combat
{
    /// <summary>
    /// Draws the hero's hands and the weapon they hold: the weapon sits at reach along the aim, rotated with it,
    /// mirrored when aiming left and behind the body when the hero faces away. The main hand grips the handle; the
    /// off hand rests beside the body, or holds the front of two-handed weapons. Hands use the current hero skin's
    /// hand sprite. A melee attack swings the held weapon through its arc (wind-up, strike, hold, return, as in the
    /// JSurviv player); bare hands punch along the aim instead. Ranged and thrown attacks kick the weapon back,
    /// reloads dip it. Everything hides while dead. Presentation only.
    /// The local hero follows <see cref="IHudState.HeldWeapon"/>, or the consumable in hand
    /// (<see cref="LocalConsumableHold"/>), drawn from its item icon; remote players follow the server snapshot
    /// (<see cref="ShowServerCode"/>), falling back to what they last attacked with on servers that do not send it.
    /// </summary>
    [DisallowMultipleComponent]
    // After the player view has moved this frame (it moves in LateUpdate), so the hands never trail the body.
    [DefaultExecutionOrder(500)]
    public sealed class HeldWeaponView : MonoBehaviour
    {
        [SerializeField] private HeldWeaponCatalog catalog;
        [SerializeField] private SpriteRenderer weaponRenderer;
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private HeroSpriteAnimator bodyAnimator;
        /// <summary>Hand sprites; empty fields simply draw no hands.</summary>
        [SerializeField] private SpriteRenderer mainHand, offHand;
        /// <summary>Only the local Hero mirrors the HUD's held weapon.</summary>
        [SerializeField] private bool followLocalHud;
        /// <summary>
        /// The main hand moves on an ellipse around the shoulders (<see cref="shoulderDrop"/> below the hero centre):
        /// <see cref="sideReach"/> sideways, <see cref="verticalReach"/> up and down, so it stays at the torso edge
        /// whichever way the hero aims.
        /// </summary>
        [SerializeField] private float sideReach = .38f;
        [SerializeField] private float verticalReach = .22f;
        [SerializeField] private float shoulderDrop = .18f;
        /// <summary>Resting off hand, relative to the hero centre, on the side away from the aim.</summary>
        [SerializeField] private Vector2 restHand = new Vector2(.42f, -.26f);
        [SerializeField] private float kickDistance = .12f;
        [SerializeField] private float kickSeconds = .12f;
        /// <summary>How far the hand dips at the middle of a reload.</summary>
        [SerializeField] private float reloadDrop = .15f;
        /// <summary>How far a bare-hand punch reaches past the guard position.</summary>
        [SerializeField] private float punchReach = .3f;

        /// <summary>sin of the weapon angle above which hands and weapon are drawn behind the body (about 40°..140°).</summary>
        private const float BehindAbove = .65f;

        private IHudState _hud;
        private HeldWeaponCatalog.Entry _entry;
        private bool _hasEntry;
        private float _kick, _reloadLeft, _reloadTotal;
        private float _swingLeft, _swingTotal, _swingArc, _tipReach;

        public HeldWeaponCatalog Catalog => catalog;
        public string ItemId { get; private set; }
        public bool Visible => weaponRenderer != null && weaponRenderer.enabled;
        /// <summary>True while a melee swing or punch is playing.</summary>
        public bool Swinging => _swingLeft > 0f;
        /// <summary>World position of the main hand after the last <see cref="Apply"/>.</summary>
        public Vector3 MainHandPosition => mainHand != null ? mainHand.transform.position : transform.position;
        public bool HandsVisible => mainHand != null && mainHand.enabled;
        /// <summary>World position of the held weapon's tip (the barrel for guns), for muzzle flashes.</summary>
        public Vector3 MuzzlePosition => _hasEntry && _entry.Muzzle != Vector2.zero && weaponRenderer != null
            ? weaponRenderer.transform.TransformPoint(_entry.MuzzleLocal)
            : transform.position + transform.right * _tipReach;

        /// <summary>True when this view draws <paramref name="itemId"/> itself (in hand), so no separate swing visual is needed.</summary>
        public bool Draws(string itemId) => catalog != null && catalog.TryGet(itemId, out _);

        /// <summary>Item icon for IDs without in-hand art (consumables); set by the world view from the HUD icon table.</summary>
        public static System.Func<string, Sprite> IconFallback;

        /// <summary>World length of an item drawn from its icon (a potion, a leaf) in the hand.</summary>
        public const float IconLength = .5f;

        [Inject]
        public void Construct(IHudState hud)
        {
            if (!followLocalHud) return;
            if (_hud != null) _hud.Changed -= OnHudChanged;
            LocalConsumableHold.Changed -= OnHoldChanged;
            _hud = hud;
            if (_hud == null) return;
            _hud.Changed += OnHudChanged;
            LocalConsumableHold.Changed += OnHoldChanged;
            ShowLocal();
        }

        private void OnDestroy()
        {
            if (_hud != null) _hud.Changed -= OnHudChanged;
            LocalConsumableHold.Changed -= OnHoldChanged;
        }

        private void OnHudChanged(HudStateChange change) => ShowLocal();
        private void OnHoldChanged() => ShowLocal();
        private void ShowLocal() => Show(LocalConsumableHold.Active ? LocalConsumableHold.ItemId : _hud.HeldWeapon.ItemId);

        /// <summary>Holds this item; unknown IDs and bare hands hide the weapon.</summary>
        public void Show(string itemId)
        {
            if (itemId == ItemId && (_hasEntry || string.IsNullOrEmpty(itemId))) return;
            ItemId = itemId;
            _kick = 0f;
            _hasEntry = catalog != null && catalog.TryGet(itemId, out _entry) || TryIconEntry(itemId, out _entry);
            if (weaponRenderer == null) return;
            weaponRenderer.sprite = _hasEntry ? _entry.Sprite : null;
            if (!_hasEntry) return;
            var scale = _entry.Scale;
            var t = weaponRenderer.transform;
            t.localScale = new Vector3(scale, scale, 1f);
            t.localRotation = Quaternion.Euler(0f, 0f, _entry.AngleOffset);
            t.localPosition = -(Vector3)(t.localRotation * (Vector3)(_entry.GripLocal * scale));
            _tipReach = TipReach(_entry, t.localRotation, t.localPosition);
            Apply();
        }

        /// <summary>
        /// The consumable held in hand, drawn as its upright item icon held at the bottom centre. Only that item: bare
        /// hands ("HAND") and other IDs without weapon art keep an empty hand.
        /// </summary>
        private static bool TryIconEntry(string itemId, out HeldWeaponCatalog.Entry entry)
        {
            var held = !string.IsNullOrEmpty(itemId) && LocalConsumableHold.Active && itemId == LocalConsumableHold.ItemId;
            var icon = held ? IconFallback?.Invoke(itemId) : null;
            entry = icon == null ? default : new HeldWeaponCatalog.Entry
            {
                ItemId = itemId, Sprite = icon, Grip = new Vector2(.5f, .15f), Length = IconLength, AngleOffset = 0f
            };
            return icon != null;
        }

        /// <summary>Farthest point of the content box along the aim (+X), measured from the grip.</summary>
        private static float TipReach(HeldWeaponCatalog.Entry entry, Quaternion rotation, Vector3 offset)
        {
            var b = entry.Sprite.bounds;
            var min = entry.ContentSize.x > 0f ? entry.ContentMin : (Vector2)b.min;
            var size = entry.ContentSize.x > 0f ? entry.ContentSize : (Vector2)b.size;
            var scale = entry.Scale;
            var best = 0f;
            for (var i = 0; i < 4; i++)
            {
                var corner = new Vector3(min.x + (i & 1) * size.x, min.y + (i >> 1) * size.y, 0f) * scale;
                best = Mathf.Max(best, (rotation * corner + offset).x);
            }
            return best;
        }

        /// <summary>
        /// Follows the server's held-weapon code for a remote player: 0 (older server) changes nothing, so the last
        /// attack keeps driving the hand; 1..13 holds that combat weapon; 15 holds <paramref name="otherItemId"/> from the
        /// server's held-items list (a throwable or special item), or hides the hand while that ID is not known yet.
        /// </summary>
        public void ShowServerCode(byte code, string otherItemId = null)
        {
            if (code == PlayerStatePacket.HeldUnknown) return;
            if (PlayerStatePacket.TryDecodeHeld(code, out var weapon)) Show(CombatWeaponIds.ToJsonId(weapon));
            else Show(string.IsNullOrEmpty(otherItemId) ? null : otherItemId);
        }

        /// <summary>
        /// Melee: swings the held weapon through <paramref name="arcDegrees"/> over <paramref name="swingSeconds"/>,
        /// or punches when the hands are empty. Otherwise: a short recoil.
        /// </summary>
        public void OnAttack(bool melee, float swingSeconds, float arcDegrees = 120f)
        {
            if (!melee) { _kick = KickDuration; Apply(); return; }
            _swingTotal = Mathf.Max(.12f, swingSeconds);
            _swingLeft = _swingTotal;
            _swingArc = Mathf.Clamp(arcDegrees, 60f, 200f);
        }

        /// <summary>A reload dips the hand toward the body and back up over <paramref name="seconds"/>.</summary>
        public void OnReload(float seconds)
        {
            if (seconds <= 0f) return;
            _reloadTotal = seconds;
            _reloadLeft = seconds;
        }

        /// <summary>Hand dip for a reload in progress: 0 at both ends, <paramref name="drop"/> at the midpoint.</summary>
        public static float ReloadDip(float remaining, float total, float drop)
        {
            if (total <= 0f || remaining <= 0f || remaining >= total) return 0f;
            var progress = 1f - remaining / total;
            return Mathf.Sin(progress * Mathf.PI) * drop;
        }

        /// <summary>
        /// Weapon angle offset from the aim during a swing, for progress 0..1: wind-up to +arc/2 (raised back), fast
        /// strike to −arc/2, a short hold, then back to the aim. Same beats as the JSurviv melee animation.
        /// </summary>
        public static float SwingOffset(float progress, float arcDegrees)
        {
            var half = arcDegrees * .5f;
            if (progress <= 0f || progress >= 1f) return 0f;
            if (progress < .3f) return half * EaseOut(progress / .3f);
            if (progress < .55f) return Mathf.Lerp(half, -half, EaseInOut((progress - .3f) / .25f));
            if (progress < .75f) return -half;
            return Mathf.Lerp(-half, 0f, EaseInOut((progress - .75f) / .25f));
        }

        /// <summary>Bare-hand punch extension 0..1 for progress 0..1: quick jab out, short hold, pull back.</summary>
        public static float PunchExtend(float progress)
        {
            if (progress <= 0f || progress >= 1f) return 0f;
            if (progress < .35f) return EaseOut(progress / .35f);
            if (progress < .55f) return 1f;
            return 1f - EaseInOut((progress - .55f) / .45f);
        }

        private static float EaseOut(float t) => 1f - (1f - t) * (1f - t);
        private static float EaseInOut(float t) => t * t * (3f - 2f * t);

        private void LateUpdate()
        {
            var dt = Time.deltaTime;
            _kick = Mathf.Max(0f, _kick - dt);
            _reloadLeft = Mathf.Max(0f, _reloadLeft - dt);
            _swingLeft = Mathf.Max(0f, _swingLeft - dt);
            Apply();
        }

        /// <summary>Advances the timed motions without waiting for frames; for deterministic presentation tests.</summary>
        public void Step(float seconds)
        {
            _kick = Mathf.Max(0f, _kick - seconds);
            _reloadLeft = Mathf.Max(0f, _reloadLeft - seconds);
            _swingLeft = Mathf.Max(0f, _swingLeft - seconds);
            Apply();
        }

        /// <summary>Places hands and weapon for the root's current aim; exposed for deterministic presentation tests.</summary>
        public void Apply()
        {
            var root = transform.parent != null ? transform.parent : transform;
            var aim = Mathf.DeltaAngle(0f, root.eulerAngles.z);
            var aimingLeft = Mathf.Abs(aim) > 90f;
            var side = aimingLeft ? -1f : 1f;
            var progress = _swingTotal > 0f && _swingLeft > 0f ? 1f - _swingLeft / _swingTotal : 0f;
            var swing = _swingLeft > 0f && _hasEntry ? SwingOffset(progress, _swingArc) * side : 0f;
            var punch = _swingLeft > 0f && !_hasEntry ? PunchExtend(progress) * punchReach : 0f;
            var angle = aim + swing;
            var direction = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad), 0f);
            var recoil = KickDuration > 0f ? (_hasEntry && _entry.RecoilDistance > 0f ? _entry.RecoilDistance : kickDistance) * (_kick / KickDuration) : 0f;
            var dip = ReloadDip(_reloadLeft, _reloadTotal, reloadDrop);
            var orbit = new Vector3(direction.x * sideReach, direction.y * verticalReach, 0f);
            var grip = root.position + orbit + direction * (punch - recoil) + Vector3.down * (shoulderDrop + dip);
            transform.SetPositionAndRotation(grip, Quaternion.Euler(0f, 0f, angle));
            transform.localScale = new Vector3(1f, aimingLeft ? -1f : 1f, 1f);

            var bodyVisible = body == null || body.enabled;
            // Pointing up the screen (facing away, or a weapon raised for a swing) goes behind the head and body.
            var behind = direction.y > BehindAbove || (bodyAnimator != null && bodyAnimator.Facing == HeroFacing.Back);
            if (weaponRenderer != null)
            {
                if (_hasEntry) weaponRenderer.sprite = _entry.FireSprite != null &&
                    _kick > Mathf.Max(0f, KickDuration - _entry.FireSeconds) ? _entry.FireSprite : _entry.Sprite;
                weaponRenderer.enabled = _hasEntry && bodyVisible;
                if (body != null)
                {
                    weaponRenderer.sortingLayerID = body.sortingLayerID;
                    weaponRenderer.sortingOrder = body.sortingOrder + (behind ? -2 : 1);
                }
            }

            var handSprite = bodyAnimator != null ? bodyAnimator.Hand : null;
            var handScale = bodyAnimator != null ? Mathf.Abs(bodyAnimator.transform.lossyScale.x) : 1f;
            PlaceHand(mainHand, handSprite, handScale, grip, bodyVisible, behind ? -1 : 2);
            var twoHanded = _hasEntry && _entry.Support > 0f;
            var off = twoHanded
                ? grip + direction * (_entry.Length * _entry.Support)
                : root.position + new Vector3(-side * restHand.x, restHand.y, 0f);
            // A resting hand hangs at the body side: in front unless the hero faces away.
            var offBehind = twoHanded ? behind : bodyAnimator != null && bodyAnimator.Facing == HeroFacing.Back;
            PlaceHand(offHand, handSprite, handScale, off, bodyVisible, offBehind ? -1 : 2);
        }

        private void PlaceHand(SpriteRenderer hand, Sprite sprite, float scale, Vector3 position, bool bodyVisible, int order)
        {
            if (hand == null) return;
            hand.enabled = bodyVisible && sprite != null;
            if (!hand.enabled) return;
            hand.sprite = sprite;
            var t = hand.transform;
            t.SetPositionAndRotation(position, Quaternion.identity);
            var parent = t.parent != null ? t.parent.lossyScale : Vector3.one;
            t.localScale = new Vector3(scale / NonZero(parent.x), scale / NonZero(parent.y), 1f);
            if (body != null)
            {
                hand.sortingLayerID = body.sortingLayerID;
                hand.sortingOrder = body.sortingOrder + order;
            }
        }

        private float KickDuration => _hasEntry && _entry.RecoilSeconds > 0f ? _entry.RecoilSeconds : kickSeconds;
        private static float NonZero(float value) => Mathf.Abs(value) < 1e-5f ? 1f : value;
    }
}
