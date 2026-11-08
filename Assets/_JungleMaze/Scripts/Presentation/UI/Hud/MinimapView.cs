using JungleMaze.App.Hud;
using JungleMaze.App.Hud.State;
using JungleMaze.Core.DI;
using UnityEngine;
using UnityEngine.UI;

namespace JungleMaze.Presentation.UI.Hud
{
    /// <summary>
    /// Minimap (Figma 41:72): frame, faint grid and the "you are here" marker with a heading needle. Lives under its
    /// own Canvas so the marker moving every frame does not rebuild the rest of the HUD.
    /// T-018 zoom (de-xuat/minimap-chuc-nang.md 4.1): N, or the mouse wheel over the map hole (up = near, down = whole
    /// map; Huy 08/11 "Zoom trên mini map dùng cuộn chuột"), switches between the whole map and a near window that follows
    /// the hero. The world's F pickup list still reads the same notch (its file is not ours): accepted by CPO 08/11. The map
    /// texture, the map team's zone layer and the grid are cropped to the window (uvRect, so the layers keep their own
    /// clipping and px-sized lines); the marker stays px-sized. Without IHudState (tests, no Bootstrap) it stays level 1.
    /// </summary>
    public sealed class MinimapView : MonoBehaviour, IMinimapView
    {
        /// <summary>Name of the zone layer <c>ZoneOverlayView.CreateMinimap</c> (map team) adds next to the marker.</summary>
        public const string ZoneLayerName = "Zone circles";

        [SerializeField] private float sizePixels = 200f;
        [SerializeField] private Graphic background;
        [SerializeField] private Graphic[] gridLines;
        [SerializeField] private RectTransform marker;
        [SerializeField] private RectTransform heading;
        [SerializeField] private Graphic markerGraphic;
        [SerializeField] private Graphic headingGraphic;
        [SerializeField] private MinimapTuning zoomTuning = MinimapTuning.Default;
        [SerializeField] private KeyCode zoomKey = KeyCode.N;

        private readonly MinimapZoom _zoom = new MinimapZoom();
        private IHudState _hud;
        private MinimapMapCapture _capture;
        private RawImage _zoneLayer;
        private bool _markerVisible;
        private float _markerX, _markerY, _windowX = -1f, _windowY, _windowCells;

        public float SizePixels => sizePixels;
        public RectTransform Marker => marker;
        public RectTransform Heading => heading;
        public MinimapZoom Zoom => _zoom;
        public MinimapTuning ZoomTuning => zoomTuning;
        /// <summary>The map hole (marker parent): parent of every cropped layer and of the waypoint flag.</summary>
        public RectTransform Area => marker != null ? (RectTransform)marker.parent : null;

        [Inject]
        public void ConstructZoom(IHudState hud) => _hud = hud;

        /// <summary>World cells to pixels of the map hole in the current window (may fall outside [0, size] at level 2).</summary>
        public bool PointerOver(Vector2 screen)
        {
            var area = Area;
            if (area == null || !isActiveAndEnabled) return false;
            var canvas = area.GetComponentInParent<Canvas>();
            var root = canvas != null ? canvas.rootCanvas : null;
            var cam = root != null && root.renderMode != RenderMode.ScreenSpaceOverlay ? root.worldCamera : null;
            return RectTransformUtility.RectangleContainsScreenPoint(area, screen, cam);
        }

        public Vector2 WorldToPixels(float x, float y)
        {
            var map = MapCells;
            if (_hud == null || _zoom.Cells <= 0f) return new Vector2(x / map * sizePixels, y / map * sizePixels);
            return new Vector2((x - _zoom.MinX) / _zoom.Cells * sizePixels, (y - _zoom.MinY) / _zoom.Cells * sizePixels);
        }

        public void ApplyPalette(HudPalette palette)
        {
            if (background != null) background.color = palette.Surface;
            if (gridLines != null)
                foreach (var line in gridLines)
                    if (line != null) line.color = palette.Grid;
            if (markerGraphic != null) markerGraphic.color = palette.Amber;
            if (headingGraphic != null) headingGraphic.color = palette.Amber;
        }

        /// <summary><paramref name="x"/>, <paramref name="y"/>: pixels on the whole-map minimap (<see cref="MinimapProjection"/>).</summary>
        public void SetMarker(bool visible, float x, float y, float headingDegrees)
        {
            if (marker == null) return;
            if (marker.gameObject.activeSelf != visible) marker.gameObject.SetActive(visible);
            _markerVisible = visible;
            if (!visible) return;
            _markerX = x;
            _markerY = y;
            PlaceMarker();
            if (heading != null) heading.localEulerAngles = new Vector3(0f, 0f, headingDegrees);
        }

        private void Update()
        {
            if (_hud == null) return;
            if (Input.GetKeyDown(zoomKey)) _zoom.Toggle();
            var wheel = Input.mouseScrollDelta.y;
            if (wheel != 0f && PointerOver(Input.mousePosition)) _zoom.SetLevel(wheel > 0f ? MinimapZoom.Near : MinimapZoom.Full);
        }

        private void LateUpdate()
        {
            if (_hud == null) return;
            var map = MapCells;
            var hasHero = _hud.HasPose;
            var pose = hasHero ? _hud.Pose : default;
            _zoom.Tick(Time.unscaledDeltaTime, zoomTuning, map, hasHero, pose.X, pose.Y);
            if (_zoom.MinX == _windowX && _zoom.MinY == _windowY && _zoom.Cells == _windowCells && !LayersPending()) return;
            _windowX = _zoom.MinX;
            _windowY = _zoom.MinY;
            _windowCells = _zoom.Cells;
            ApplyWindow(map);
        }

        private int MapCells => _hud != null && _hud.MapSizeCells > 0 ? _hud.MapSizeCells : 1;

        // The zone layer is created by the map team on the first zone packet; crop it once it shows up.
        private bool LayersPending() => _zoneLayer == null && Area != null && Area.Find(ZoneLayerName) != null;

        private void ApplyWindow(int map)
        {
            var u = _zoom.MinX / map;
            var v = _zoom.MinY / map;
            var w = _zoom.Cells / map;
            if (_capture == null) _capture = GetComponent<MinimapMapCapture>();
            if (_capture != null)
            {
                if (_capture.MapImage != null) _capture.MapImage.uvRect = new Rect(u, v, w, w);
                if (_capture.RingImage != null) _capture.RingImage.uvRect = new Rect(u, v, w, w);
            }
            if (_zoneLayer == null && Area != null)
            {
                var layer = Area.Find(ZoneLayerName);
                if (layer != null) _zoneLayer = layer.GetComponent<RawImage>();
            }
            if (_zoneLayer != null)
            {
                // Same inset crop as ZoneOverlayView.CreateMinimap, applied inside the window.
                var f = ZoneOverlayParams.UvInset(sizePixels);
                _zoneLayer.uvRect = new Rect(u + f * w, v + f * w, w * (1f - 2f * f), w * (1f - 2f * f));
            }
            PlaceGrid(map);
            PlaceMarker();
        }

        /// <summary>Grid lines stay on the same world cells (every fifth of the map) and hide outside the window.</summary>
        private void PlaceGrid(int map)
        {
            if (gridLines == null) return;
            foreach (var line in gridLines)
            {
                if (line == null || line.name.Length < 2 || !int.TryParse(line.name.Substring(1), out var index)) continue; // V1..V4, H1..H4
                var t = line.rectTransform;
                var vertical = line.name[0] == 'V';
                var px = _zoom.ToWindow(sizePixels / 5f * index, vertical ? _zoom.MinX : _zoom.MinY, sizePixels, map);
                var inside = px >= 0f && px <= sizePixels;
                if (line.enabled != inside) line.enabled = inside;
                t.anchoredPosition = vertical ? new Vector2(px, t.anchoredPosition.y) : new Vector2(t.anchoredPosition.x, px);
            }
        }

        private void PlaceMarker()
        {
            if (marker == null || !_markerVisible) return;
            if (_hud == null || _zoom.Cells <= 0f) { marker.anchoredPosition = new Vector2(_markerX, _markerY); return; }
            var map = MapCells;
            marker.anchoredPosition = new Vector2(
                _zoom.ToWindow(_markerX, _zoom.MinX, sizePixels, map),
                _zoom.ToWindow(_markerY, _zoom.MinY, sizePixels, map));
        }
    }
}
