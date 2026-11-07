using JungleMaze.App.Ports;
using JungleMaze.Core.DI;
using UnityEngine;

namespace JungleMaze.Presentation.Player
{
    /// <summary>
    /// Follow the interpolated Hero in LateUpdate; keep the viewport inside the map. Runs after the player views have
    /// moved this frame (order 900) and before the spectator driver (1000), so the camera never trails the body by a frame.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [DefaultExecutionOrder(900)]
    public sealed class HeroCameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        private Camera _camera;
        private float _mapSize = 50f;
        private IBalanceConfigProvider _balance;
        private IClientRuntimeData _runtime;

        public Transform Target { get => target; set => target = value; }

        /// <summary>
        /// Followed instead of <see cref="Target"/> while set: online the own player is drawn by its PlayerView, and the
        /// Hero is only a hidden proxy copied from it one frame late.
        /// </summary>
        public Transform FollowOverride { get; set; }

        [Inject]
        public void Construct(IBalanceConfigProvider balance) { _balance = balance; _mapSize = balance.Match.MapSizeCells; }

        /// <summary>Kẹp camera theo map đang chơi (map offline có thể lớn hơn 50).</summary>
        [Inject]
        public void ConstructRuntime(IClientRuntimeData runtime) => _runtime = runtime;

        private void Awake() => _camera = GetComponent<Camera>();

        private void LateUpdate()
        {
            var follow = FollowOverride != null && FollowOverride.gameObject.activeInHierarchy ? FollowOverride : target;
            if (follow == null) return;
            if (_runtime != null) _mapSize = JungleMaze.Presentation.World.MapGridView.MapSize(_runtime, _balance);
            var halfHeight = _camera.orthographicSize;
            var halfWidth = halfHeight * _camera.aspect;
            var p = follow.position;
            p.x = halfWidth * 2 >= _mapSize ? _mapSize / 2 : Mathf.Clamp(p.x, halfWidth, _mapSize - halfWidth);
            p.y = halfHeight * 2 >= _mapSize ? _mapSize / 2 : Mathf.Clamp(p.y, halfHeight, _mapSize - halfHeight);
            p.z = transform.position.z;
            transform.position = p;
        }
    }
}
