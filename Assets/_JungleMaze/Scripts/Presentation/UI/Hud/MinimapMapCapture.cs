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
    /// Skin A (T-016): with <see cref="ringImage"/> set, zone rings render into a second, transparent texture so the
    /// HUD can put a dark overlay between terrain and rings.
    /// </summary>
    public sealed class MinimapMapCapture : MonoBehaviour
    {
        [SerializeField] private RawImage mapImage;
        [SerializeField] private RawImage ringImage;
        [SerializeField] private int resolution = 256;
        [SerializeField] private float refreshSeconds = 1f;
        [SerializeField] private float lineWidthTexels = 2.5f;
        [SerializeField] private Color emptyColor = new Color(0.05f, 0.08f, 0.09f, 1f);

        private readonly List<Renderer> _hidden = new List<Renderer>();
        private readonly List<Canvas> _hiddenCanvases = new List<Canvas>();
        private MapGridView _grid;
        private Camera _camera;
        private RenderTexture _texture, _ringTexture;
        private int _worldMask;
        // shortcut: borrows unnamed layer 31 for the ring render only; give it a name if the project ever uses 31.
        private const int RingLayer = 31;
        private float _nextAt;

        public RawImage MapImage => mapImage;
        public RenderTexture Texture => _texture;
        public RawImage RingImage => ringImage;
        public RenderTexture RingTexture => _ringTexture;

        private void OnEnable()
        {
            if (mapImage != null && _texture == null) mapImage.enabled = false;
            if (ringImage != null && _ringTexture == null) ringImage.enabled = false;
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
                _worldMask = (ui >= 0 ? ~(1 << ui) : ~0) & ~(1 << RingLayer);
                _texture = new RenderTexture(resolution, resolution, 16) { name = "Minimap map", filterMode = FilterMode.Bilinear };
                _camera.targetTexture = _texture;
                mapImage.texture = _texture;
                if (ringImage != null)
                {
                    _ringTexture = new RenderTexture(resolution, resolution, 16) { name = "Minimap rings", filterMode = FilterMode.Bilinear };
                    ringImage.texture = _ringTexture;
                }
            }
            _camera.orthographicSize = size / 2f;
            _camera.transform.position = new Vector3(size / 2f, size / 2f, -10f);

            // shortcut: scans the scene each refresh (about once a second); cache by event if the HUD ever shows lag.
            HidePlayers<HeroController>();
            HidePlayers<PlayerView>();
            // Both HUD maps draw their own authoritative zone. Never bake the world tint into terrain.
            foreach (var overlay in FindObjectsByType<ZoneOverlayView>(FindObjectsSortMode.None))
            {
                var renderer = overlay.WorldRenderer;
                if (renderer != null && !renderer.forceRenderingOff)
                {
                    renderer.forceRenderingOff = true;
                    _hidden.Add(renderer);
                }
            }
            foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                if (canvas.renderMode == RenderMode.WorldSpace && canvas.enabled) { canvas.enabled = false; _hiddenCanvases.Add(canvas); }
            var lines = FindObjectsByType<LineRenderer>(FindObjectsSortMode.None);
            var widths = new float[lines.Length];
            var layers = new int[lines.Length];
            var forced = new bool[lines.Length];
            for (var i = 0; i < lines.Length; i++)
            {
                widths[i] = lines[i].widthMultiplier; lines[i].widthMultiplier = size * lineWidthTexels / resolution;
                layers[i] = lines[i].gameObject.layer;
                forced[i] = lines[i].forceRenderingOff;
            }
            var split = _ringTexture != null;
            try
            {
                if (split) foreach (var line in lines) line.forceRenderingOff = true;
                _camera.targetTexture = _texture;
                _camera.cullingMask = _worldMask;
                _camera.backgroundColor = emptyColor;
                _camera.Render();
                if (split)
                {
                    for (var i = 0; i < lines.Length; i++) { lines[i].forceRenderingOff = forced[i]; lines[i].gameObject.layer = RingLayer; }
                    _camera.targetTexture = _ringTexture;
                    _camera.cullingMask = 1 << RingLayer;
                    _camera.backgroundColor = Color.clear;
                    _camera.Render();
                }
            }
            finally
            {
                for (var i = 0; i < lines.Length; i++)
                    if (lines[i] != null) { lines[i].widthMultiplier = widths[i]; lines[i].gameObject.layer = layers[i]; lines[i].forceRenderingOff = forced[i]; }
                foreach (var r in _hidden) if (r != null) r.forceRenderingOff = false;
                foreach (var c in _hiddenCanvases) if (c != null) c.enabled = true;
                _hidden.Clear();
                _hiddenCanvases.Clear();
            }
            if (!mapImage.enabled) mapImage.enabled = true;
            if (split && !ringImage.enabled) ringImage.enabled = true;
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
            if (_ringTexture != null) { _ringTexture.Release(); Destroy(_ringTexture); }
        }
    }
}
