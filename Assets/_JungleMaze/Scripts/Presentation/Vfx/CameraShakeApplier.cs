using JungleMaze.App.MatchUi;
using JungleMaze.App.Ports;
using JungleMaze.Domain.Vfx;
using JungleMaze.Presentation.World;
using UnityEngine;
using UnityEngine.Rendering;

namespace JungleMaze.Presentation.Vfx
{
    /// <summary>
    /// Adds the <see cref="ScreenShakeModel"/> offset to the main camera after every camera writer (order 1100: after
    /// <c>HeroCameraFollow</c> 900 and the spectator driver 1000), keeps the view inside the map, and puts the camera
    /// back once it has rendered. Followers therefore never read a shaken position, and after the shake the camera is
    /// exactly where they put it. Off (no offset at all) when the model is zero: the director clears it when screen
    /// shake is disabled or the match is not InGame.
    /// </summary>
    [DefaultExecutionOrder(1100)]
    public sealed class CameraShakeApplier : MonoBehaviour
    {
        private ScreenShakeModel _shake;
        private IBalanceConfigProvider _balance;
        private IClientRuntimeData _runtime;
        private Camera _cameraOverride;
        private Camera _shaken;
        private Vector3 _base, _applied;
        private bool _pending;

        /// <summary>Offset written this frame (cells); zero when nothing was applied.</summary>
        public Vector2 LastOffset { get; private set; }

        public void Bind(ScreenShakeModel shake, IBalanceConfigProvider balance, IClientRuntimeData runtime, Camera cameraOverride = null)
        {
            _shake = shake;
            _balance = balance;
            _runtime = runtime;
            _cameraOverride = cameraOverride;
        }

        private void OnEnable() => RenderPipelineManager.endCameraRendering += OnEndCameraRendering;

        private void OnDisable()
        {
            RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
            Restore();
        }

        private void LateUpdate() => Apply();

        /// <summary>Applies this frame's offset; public so PlayMode tests can drive it.</summary>
        public void Apply()
        {
            // No render happened since the last shake (hidden Game view, tests): undo it unless a follower already moved on.
            if (_pending && _shaken != null && _shaken.transform.position == _applied) Restore();
            _pending = false;
            LastOffset = Vector2.zero;
            if (_shake == null) return;
            var ox = _shake.OffsetX;
            var oy = _shake.OffsetY;
            if (ox == 0f && oy == 0f) return;
            var cam = _cameraOverride != null ? _cameraOverride : Camera.main;
            if (cam == null) return;
            var t = cam.transform;
            _base = t.position;
            var x = _base.x + ox;
            var y = _base.y + oy;
            ClampInsideMap(cam, ref x, ref y);
            _applied = new Vector3(x, y, _base.z);
            t.position = _applied;
            _shaken = cam;
            _pending = true;
            LastOffset = new Vector2(x - _base.x, y - _base.y);
        }

        /// <summary>Puts the camera back to where its followers left it.</summary>
        public void Restore()
        {
            if (!_pending || _shaken == null) { _pending = false; return; }
            _shaken.transform.position = _base;
            _pending = false;
        }

        private void OnEndCameraRendering(ScriptableRenderContext context, Camera cam)
        {
            if (_pending && cam == _shaken) Restore();
        }

        private void ClampInsideMap(Camera cam, ref float x, ref float y)
        {
            if (!cam.orthographic || _balance == null) return;
            var mapSize = _runtime != null ? MapGridView.MapSize(_runtime, _balance) : _balance.Match.MapSizeCells;
            if (!(mapSize > 0f)) return;
            var halfHeight = cam.orthographicSize;
            var halfWidth = halfHeight * cam.aspect;
            // Only a map-bound camera is kept in the map (a preview camera elsewhere is left alone).
            float bx = _base.x, by = _base.y;
            SpectatorCameraMath.ClampToMap(ref bx, ref by, halfWidth, halfHeight, mapSize);
            if (Mathf.Abs(bx - _base.x) > .001f || Mathf.Abs(by - _base.y) > .001f) return;
            SpectatorCameraMath.ClampToMap(ref x, ref y, halfWidth, halfHeight, mapSize);
        }
    }
}
