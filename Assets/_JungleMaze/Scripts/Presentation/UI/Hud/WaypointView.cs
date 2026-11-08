using System.Collections.Generic;
using JungleMaze.App.Hud;
using JungleMaze.App.Hud.State;
using JungleMaze.Core.DI;
using TMPro;
using UnityEngine;

namespace JungleMaze.Presentation.UI.Hud
{
    /// <summary>
    /// T-018 stage 2, D1 personal waypoint (de-xuat/minimap-chuc-nang.md 5; icons T-029). Lives on the HUD root:
    /// with the big map open a left click on the map sets the flag (or removes it within reach), a right click removes
    /// it (the big map already blocks attacks); it then shows on the big map (24 px + 1 px line from the hero), on the
    /// minimap (18 px + line, held on the window edge at the near level) and in the world (40 px flag on the ground when
    /// on screen, else an edge arrow with the distance in cells, kept off the HUD panels). Client only, no packet.
    /// </summary>
    public sealed class WaypointView : MonoBehaviour
    {
        [SerializeField] private WaypointTuning tuning = WaypointTuning.Default;
        [SerializeField] private HudBigMapView bigMap;
        [SerializeField] private RectTransform bigArea, bigFlag, bigLine;
        [SerializeField] private MinimapView minimap;
        [SerializeField] private RectTransform miniFlag, miniLine;
        [SerializeField] private RectTransform edge, groundFlag, arrow, badge;
        [SerializeField] private TMP_Text distance;
        [Tooltip("HUD blocks the edge arrow keeps clear of (minimap, match status, vitals, quick bar).")]
        [SerializeField] private RectTransform[] panels = new RectTransform[0];

        private readonly Waypoint _waypoint = new Waypoint();
        private readonly List<Rect> _panelRects = new List<Rect>();
        private readonly Vector3[] _corners = new Vector3[4];
        private IHudState _hud;
        private int _mapCells, _shownDistance = -1;

        public Waypoint Model => _waypoint;
        public WaypointTuning Tuning => tuning;
        public RectTransform BigFlag => bigFlag;
        public RectTransform MiniFlag => miniFlag;
        public RectTransform Arrow => arrow;
        public RectTransform GroundFlag => groundFlag;
        public TMP_Text Distance => distance;

        [Inject]
        public void Construct(IHudState hud) => _hud = hud;

        private int MapCells => _hud != null && _hud.MapSizeCells > 0 ? _hud.MapSizeCells : 1;

        private void Update()
        {
            if (_hud == null) return;
            if (_mapCells != MapCells) { _mapCells = MapCells; _waypoint.Clear(); } // a new map is a new match
            if (bigMap != null && bigMap.IsOpen)
            {
                if (Input.GetMouseButtonDown(0) && TryMapPoint(Input.mousePosition, out var world))
                    _waypoint.Click(world.x, world.y, MapCells, tuning.ReachCells);
                else if (Input.GetMouseButtonDown(1) && TryMapPoint(Input.mousePosition, out _))
                    _waypoint.Clear();
            }
            var pose = _hud.HasPose ? _hud.Pose : default;
            _waypoint.Tick(_hud.Hp > 0, _hud.HasPose, pose.X, pose.Y, tuning.ReachCells);
        }

        /// <summary>Screen point to world cells when it falls on the big map square (header and legend do nothing).</summary>
        public bool TryMapPoint(Vector2 screen, out Vector2 world)
        {
            world = default;
            if (bigArea == null || !RectTransformUtility.ScreenPointToLocalPointInRectangle(bigArea, screen, UiCamera(bigArea), out var local)) return false;
            var r = bigArea.rect;
            var px = local - r.min;
            if (px.x < 0f || px.y < 0f || px.x > r.width || px.y > r.height) return false;
            world = px / r.width * MapCells;
            return true;
        }

        private void LateUpdate()
        {
            var on = _hud != null && _waypoint.Has;
            var k = edge != null ? edge.rect.height / 1080f : 1f / 1.5f; // canvas units per 1080p px
            var hero = on && _hud.HasPose ? new Vector2(_hud.Pose.X, _hud.Pose.Y) : default;
            DrawBigMap(on, hero, k);
            DrawMinimap(on, hero, k);
            DrawWorld(on && _hud.HasPose && (bigMap == null || !bigMap.IsOpen), hero, k);
        }

        private void DrawBigMap(bool on, Vector2 hero, float k)
        {
            if (!Show(bigFlag, on) || bigArea == null) { Show(bigLine, false); return; }
            var scale = bigArea.rect.width / MapCells;
            var flag = new Vector2(_waypoint.X, _waypoint.Y) * scale;
            Size(bigFlag, tuning.BigMapFlagPx * k);
            bigFlag.anchoredPosition = flag;
            if (Show(bigLine, _hud.HasPose)) Line(bigLine, hero * scale, flag, k);
        }

        private void DrawMinimap(bool on, Vector2 hero, float k)
        {
            if (!Show(miniFlag, on) || minimap == null) { Show(miniLine, false); return; }
            var size = minimap.SizePixels;
            var flagSize = tuning.MinimapFlagPx * k;
            var from = minimap.WorldToPixels(hero.x, hero.y);
            var to = minimap.WorldToPixels(_waypoint.X, _waypoint.Y);
            // W8: outside the near window the flag stays on its edge, on the line toward it.
            WaypointEdge.Place(from, to, new Rect(0f, 0f, size, size), null, flagSize / 2f, out var at);
            Size(miniFlag, flagSize);
            miniFlag.anchoredPosition = at;
            if (Show(miniLine, _hud.HasPose)) Line(miniLine, from, at, k);
        }

        private void DrawWorld(bool on, Vector2 hero, float k)
        {
            var cam = Camera.main;
            if (!on || edge == null || cam == null) { Show(groundFlag, false); Show(arrow, false); Show(badge, false); return; }
            var uiCam = UiCamera(edge);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(edge, cam.WorldToScreenPoint(new Vector3(hero.x, hero.y, 0f)), uiCam, out var origin);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(edge, cam.WorldToScreenPoint(new Vector3(_waypoint.X, _waypoint.Y, 0f)), uiCam, out var target);
            _panelRects.Clear();
            foreach (var p in panels)
            {
                if (p == null || !p.gameObject.activeInHierarchy) continue;
                p.GetWorldCorners(_corners);
                Vector2 a = edge.InverseTransformPoint(_corners[0]), b = edge.InverseTransformPoint(_corners[2]);
                _panelRects.Add(Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y)));
            }
            var arrowSize = tuning.EdgeArrowPx * k;
            var off = WaypointEdge.Place(origin, target, edge.rect, _panelRects, tuning.EdgeMarginPx * k + arrowSize / 2f, out var at);
            Show(groundFlag, !off);
            Show(arrow, off);
            Show(badge, off);
            if (!off)
            {
                Size(groundFlag, tuning.WorldFlagPx * k);
                groundFlag.anchoredPosition = target;
                return;
            }
            var dir = (target - origin).normalized;
            Size(arrow, arrowSize);
            arrow.anchoredPosition = at;
            arrow.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f); // sprite points up
            badge.anchoredPosition = at - dir * (arrowSize / 2f + badge.rect.width / 2f + 4f * k);
            var cells = Mathf.RoundToInt(_waypoint.DistanceTo(hero.x, hero.y));
            if (cells != _shownDistance && distance != null) { _shownDistance = cells; distance.SetText("{0:0}", cells); }
        }

        private static Camera UiCamera(RectTransform t)
        {
            var canvas = t.GetComponentInParent<Canvas>();
            var root = canvas != null ? canvas.rootCanvas : null;
            return root != null && root.renderMode != RenderMode.ScreenSpaceOverlay ? root.worldCamera : null;
        }

        private static bool Show(RectTransform t, bool on)
        {
            if (t != null && t.gameObject.activeSelf != on) t.gameObject.SetActive(on);
            return on && t != null;
        }

        private static void Size(RectTransform t, float side) => t.sizeDelta = new Vector2(side, side);

        // 1 px line from a to b (anchored at the parent's bottom-left, pivot at its left end).
        private static void Line(RectTransform line, Vector2 a, Vector2 b, float k)
        {
            var d = b - a;
            line.anchoredPosition = a;
            line.sizeDelta = new Vector2(d.magnitude, k);
            line.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        }
    }
}
