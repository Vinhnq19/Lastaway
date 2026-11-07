using JungleMaze.App.Match;
using JungleMaze.App.Player;
using JungleMaze.App.Ports;
using JungleMaze.Core.DI;
using JungleMaze.Core.Pooling;
using JungleMaze.Domain.Player;
using JungleBackend.Protocol.Gameplay;
using JungleMaze.Presentation.Combat;
using JungleMaze.Presentation.Rendering;
using UnityEngine;

namespace JungleMaze.Presentation.Player
{
    /// <summary>
    /// Hiển thị một người chơi (không đọc packet). Vị trí lấy từ <see cref="IPlayerPoseSource"/>: prediction cho nhân vật
    /// của mình (MOV-02), interpolation cho người khác (MOV-03). Chưa có pose thì đặt thẳng theo snapshot mới nhất.
    /// Được lấy từ pool nên reset trạng thái trong OnSpawned/OnDespawned.
    /// Body có <see cref="HeroSpriteAnimator"/> thì mỗi NetId nhận một hero riêng (không tô màu, animator giữ scale);
    /// riêng nhân vật của mình dùng skin đã chọn ở Home (<see cref="IHeroSkinPreference"/>) nếu có.
    /// </summary>
    public sealed class PlayerView : MonoBehaviour, IPoolable
    {
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private float followSharpness = 15f;
        /// <summary>In-hand weapon that follows the server's held-weapon code; found among the children when empty.</summary>
        [SerializeField] private HeldWeaponView heldWeapon;
        private int _appliedHeldCode = -1;
        private string _appliedHeldItem = "";
        private IHeroSkinPreference _skins;
        private MatchSession _match;
        private HeroSpriteAnimator _hero;
        private int _appliedSkin = int.MinValue;

        private PlayerModel _model;
        private IPlayerPoseSource _poses;
        private bool _snapNext;
        private DepthSortable _depth;
        private Vector3 _lastDepthPosition;

        public byte NetId => _model?.NetId ?? 0;

        /// <summary>True between <see cref="Bind"/> and despawn; <see cref="NetId"/> reads 0 while unbound.</summary>
        public bool IsBound => _model != null;

        public void Bind(PlayerModel model, IPlayerPoseSource poses = null)
        {
            _model = model;
            _poses = poses;
            _snapNext = true;
            _appliedSkin = int.MinValue;
            if (body != null && body.TryGetComponent<HeroSpriteAnimator>(out var hero) && hero.enabled)
            {
                _hero = hero;
                body.color = Color.white;
                SyncSkin();
                if (hero.HasVisuals) { Refresh(); return; }
            }
            _hero = null;
            if (body != null)
            {
                body.color = Color.HSVToRGB(model.NetId * 0.1f % 1f, 0.6f, 1f);
                // Built-in UI sprites have a very small native size. Match the server's 0.8-cell diameter.
                if (body.sprite != null)
                {
                    var size = body.sprite.bounds.size;
                    if (size.x > 0 && size.y > 0) body.transform.localScale = new Vector3(.8f / size.x, .8f / size.y, 1);
                }
            }
            Refresh();
        }

        /// <summary>Own-character skin choice; optional (tests and tools may bind views without it).</summary>
        [Inject]
        public void ConfigureSkin(IHeroSkinPreference skins, MatchSession match)
        {
            _skins = skins;
            _match = match;
            _appliedSkin = int.MinValue;
        }

        /// <summary>
        /// Applies the skin rule: the local choice on your own character (known once the server sends OwnNetId),
        /// the NetId skin for everyone else. Cheap to call every frame; it only touches the animator on a change.
        /// </summary>
        private void SyncSkin()
        {
            if (_hero == null || _model == null) return;
            var own = _match != null && _match.OwnNetId == _model.NetId;
            var skin = HeroSkinRules.Resolve(_skins?.Selected ?? HeroSkinRules.Automatic, own, _model.NetId);
            if (skin == _appliedSkin) return;
            _appliedSkin = skin;
            _hero.SetHero(skin);
        }

        private void Awake()
        {
            TryGetComponent(out _depth); // tùy chọn, xem DepthSortable
            if (heldWeapon == null) heldWeapon = GetComponentInChildren<HeldWeaponView>(true);
        }

        public void OnSpawned()
        {
            _snapNext = true;
            _appliedHeldCode = -1;
        }

        public void OnDespawned()
        {
            _model = null;
            _poses = null;
            _appliedHeldCode = -1;
            if (heldWeapon != null) heldWeapon.Show(null);
        }

        // LateUpdate: prediction/interpolation đã được tick trong Update của frame này.
        private void LateUpdate() => Refresh();

        /// <summary>Applies the latest snapshot's held-weapon code (and, for "other item", the held item ID) once per change.</summary>
        private void SyncHeldWeapon()
        {
            if (heldWeapon == null || !_model.HasState) return;
            var code = _model.Current.HeldCode;
            var item = code == PlayerStatePacket.HeldOther ? _model.HeldItemId : "";
            if (code == _appliedHeldCode && item == _appliedHeldItem) return;
            _appliedHeldCode = code;
            _appliedHeldItem = item;
            heldWeapon.ShowServerCode(code, item);
        }

        private void Refresh()
        {
            if (_model == null) return;
            SyncHeldWeapon();
            SyncSkin();
            if (_poses != null && _poses.TryGetPose(_model.NetId, out var pose))
            {
                // Pose đã mượt sẵn: đặt thẳng, không làm mượt thêm (sẽ cộng độ trễ).
                transform.SetPositionAndRotation(new Vector3(pose.X, pose.Y, 0f), Quaternion.Euler(0f, 0f, pose.RotationDegrees));
                if (body != null) body.enabled = !pose.IsDead;
                _snapNext = false;
                NotifyDepth();
                return;
            }
            if (!_model.HasState) return;
            var state = _model.Current;
            var target = new Vector3(state.X, state.Y, 0f);
            var t = _snapNext ? 1f : 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, target, t);
            transform.rotation = Quaternion.Euler(0f, 0f, state.RotationDegrees);
            if (body != null) body.enabled = !state.IsDead;
            _snapNext = false;
            NotifyDepth();
        }

        private void NotifyDepth()
        {
            if (_depth == null) return;
            var position = transform.position;
            if (position == _lastDepthPosition) return;
            _lastDepthPosition = position;
            _depth.MarkDirty();
        }
    }
}
