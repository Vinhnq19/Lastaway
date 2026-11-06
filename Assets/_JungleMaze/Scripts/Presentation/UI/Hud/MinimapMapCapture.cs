using System.Collections.Generic;
using JungleMaze.Presentation.Player;
using JungleMaze.Presentation.World;
using UnityEngine;
using UnityEngine.UI;

namespace JungleMaze.Presentation.UI.Hud
{
    /// <summary>
    /// HUD B (T-013): draws the real map under the minimap. A disabled orthographic camera renders the whole map
    /// (level art, walls, objects, zone rings) into a small RenderTexture about once a second. Players are hidden for
    /// that render so the minimap never shows anyone, and zone lines are widened so they survive the downscale.
    /// Display only; no zoom, ping or other minimap features.
    /// </summary>
    public sealed class MinimapMapCapture : MonoBehaviour
    {
        [SerializeField] private RawImage mapImage;
        [SerializeField] private int resolution = 256;
        [SerializeField] private float refreshSeconds = 1f;
        [SerializeField] private float lineWidthTexels = 2.5f;
        [SerializeField] private Color emptyColor = new Color(0.05f, 0.08f, 0.09f, 1f);

        private readonly List<Renderer> _hidden = new List<Renderer>();
        private readonly List<Canvas> _hiddenCanvases = new List<Canvas>();
        private MapGridView _grid;
        private Camera _camera;
        private RenderTexture _texture;
        private float _nextAt;

        public RawImage MapImage => mapImage;
        public RenderTexture Texture => _texture;

        private void OnEnable()
        {
            if (mapImage != null && _texture == null) mapImage.enabled = false;
        }

        private void LateUpdate()
        {
            if (mapImage == null || Time.unscaledTime < _nextAt) return;
            _nextAt = Time.unscaledTime + refreshSeconds;
            // shortcut: finds the map grid once per refresh until it exists; inject it if a scene ever has two grids.
            if (_grid == null) _grid = FindAnyObjectByType<MapGridView>();
            var size = _grid != null ? _grid.MapSizeCells : 0;
            if (size <= 0) return;
            Capture(size);
        }

        private void Capture(int size)
        {
            if (_camera == null)
            {
                var go = new GameObject("Minimap capture camera") { hideFlags = HideFlags.DontSave };
                go.transform.SetParent(transform, false);
                _camera = go.AddComponent<Camera>();
                _camera.enabled = false;
                _camera.orthographic = true;
                _camera.clearFlags = CameraClearFlags.SolidColor;
                _camera.backgroundColor = emptyColor;
                var ui = LayerMask.NameToLayer("UI");
                _camera.cullingMask = ui >= 0 ? ~(1 << ui) : ~0;
                _texture = new RenderTexture(resolution, resolution, 16) { name = "Minimap map", filterMode = FilterMode.Bilinear };
                _camera.targetTexture = _texture;
                mapImage.texture = _texture;
            }
            _camera.orthographicSize = size / 2f;
            _camera.transform.position = new Vector3(size / 2f, size / 2f, -10f);

            // shortcut: scans the scene each refresh (about once a second); cache by event if the HUD ever shows lag.
            HidePlayers<HeroController>();
            HidePlayers<PlayerView>();
            foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                if (canvas.renderMode == RenderMode.WorldSpace && canvas.enabled) { canvas.enabled = false; _hiddenCanvases.Add(canvas); }
            var lines = FindObjectsByType<LineRenderer>(FindObjectsSortMode.None);
            var widths = new float[lines.Length];
            for (var i = 0; i < lines.Length; i++) { widths[i] = lines[i].widthMultiplier; lines[i].widthMultiplier = size * lineWidthTexels / resolution; }
            try
            {
                _camera.Render();
            }
            finally
            {
                for (var i = 0; i < lines.Length; i++) if (lines[i] != null) lines[i].widthMultiplier = widths[i];
                foreach (var r in _hidden) if (r != null) r.forceRenderingOff = false;
                foreach (var c in _hiddenCanvases) if (c != null) c.enabled = true;
                _hidden.Clear();
                _hiddenCanvases.Clear();
            }
            if (!mapImage.enabled) mapImage.enabled = true;
        }

        private void HidePlayers<T>() where T : Component
        {
            foreach (var player in FindObjectsByType<T>(FindObjectsSortMode.None))
            foreach (var r in player.GetComponentsInChildren<Renderer>())
                if (!r.forceRenderingOff) { r.forceRenderingOff = true; _hidden.Add(r); }
        }

        private void OnDestroy()
        {
            if (_texture != null) { _texture.Release(); Destroy(_texture); }
        }
    }
}
