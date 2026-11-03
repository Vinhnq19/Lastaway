using JungleMaze.App.Ports;
using JungleMaze.Core.Pooling;
using JungleMaze.Domain.Player;
using JungleMaze.Presentation.Combat;
using JungleMaze.Presentation.Rendering;
using UnityEngine;

namespace JungleMaze.Presentation.Player
{
    /// <summary>
    /// Hiển thị một người chơi (không đọc packet). Vị trí lấy từ <see cref="IPlayerPoseSource"/>: prediction cho nhân vật
    /// của mình (MOV-02), interpolation cho người khác (MOV-03). Chưa có pose thì đặt thẳng theo snapshot mới nhất.
    /// Được lấy từ pool nên reset trạng thái trong OnSpawned/OnDespawned.
    /// Body có <see cref="HeroSpriteAnimator"/> thì mỗi NetId nhận một hero riêng (không tô màu, animator giữ scale).
    /// </summary>
    public sealed class PlayerView : MonoBehaviour, IPoolable
    {
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private float followSharpness = 15f;
        /// <summary>In-hand weapon that follows the server's held-weapon code; found among the children when empty.</summary>
        [SerializeField] private HeldWeaponView heldWeapon;
        private int _appliedHeldCode = -1;

        private PlayerModel _model;
        private IPlayerPoseSource _poses;
        private bool _snapNext;
        private DepthSortable _depth;
        private Vector3 _lastDepthPosition;

        public byte NetId => _model?.NetId ?? 0;

        public void Bind(PlayerModel model, IPlayerPoseSource poses = null)
        {
            _model = model;
            _poses = poses;
            _snapNext = true;
            if (body != null && body.TryGetComponent<HeroSpriteAnimator>(out var hero) && hero.enabled)
            {
                body.color = Color.white;
                hero.SetHero(model.NetId);
                if (hero.HasVisuals) { Refresh(); return; }
            }
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

        /// <summary>Applies the latest snapshot's held-weapon code once per change.</summary>
        private void SyncHeldWeapon()
        {
            if (heldWeapon == null || !_model.HasState) return;
            var code = _model.Current.HeldCode;
            if (code == _appliedHeldCode) return;
            _appliedHeldCode = code;
            heldWeapon.ShowServerCode(code);
        }

        private void Refresh()
        {
            if (_model == null) return;
            SyncHeldWeapon();
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
