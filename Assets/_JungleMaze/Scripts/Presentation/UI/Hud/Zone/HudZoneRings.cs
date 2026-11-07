using JungleBackend.Protocol.Gameplay;
using JungleMaze.App.Hud;
using JungleMaze.App.Hud.State;
using JungleMaze.App.Match;
using JungleMaze.App.Ports;
using JungleMaze.Core.DI;
using UnityEngine;

namespace JungleMaze.Presentation.UI.Hud
{
    /// <summary>
    /// Current zone (orange, map_ring_zone) and next safe zone (white, map_ring_safe) on a map rect, shared by the minimap
    /// and the big map (CPO 07/11: one look on both). Reads the online <see cref="MatchSession"/> zone in game, else the
    /// offline <see cref="OfflineMatchSession"/> zone; read-only. <see cref="SetZone"/> overrides for tests and other sources.
    /// Rings sit in <see cref="area"/>, anchored at its bottom-left like the minimap marker (same projection).
    /// </summary>
    public sealed class HudZoneRings : MonoBehaviour
    {
        [SerializeField] private RectTransform area;
        [SerializeField] private RingGraphic zoneRing, safeRing;
        [Tooltip("Draw the sprite rings. Off = main style (Huy 07/11): the map shows Quang Huy's ZoneOverlay layer; data still computed.")]
        [SerializeField] private bool draw = true;

        private IHudState _hud;
        private MatchSession _online;
        private OfflineMatchSession _offline;

        public bool HasZone { get; private set; }
        public int Phase { get; private set; }
        public Vector2 ZoneCentre { get; private set; }
        public float ZoneRadius { get; private set; }
        public Vector2 SafeCentre { get; private set; }
        public float SafeRadius { get; private set; }
        public RingGraphic ZoneRing => zoneRing;
        public RingGraphic SafeRing => safeRing;
        public bool Draw => draw;
        /// <summary>Map rect in world cells (runtime map data), for the big map's ZoneOverlay layer (_MapBounds).</summary>
        public bool HasBounds { get; private set; }
        public RuntimeMapBounds Bounds { get; private set; }

        [Inject]
        public void Construct(IHudState hud, MatchSession online, OfflineMatchSession offline)
        {
            _hud = hud;
            _online = online;
            _offline = offline;
        }

        /// <summary>Same source as the main ZoneOverlay (<c>MatchDemoView</c> passes <c>IClientRuntimeData.Bounds</c>).</summary>
        [Inject]
        public void BindMapData(IClientRuntimeData data)
        {
            if (data != null) SetBounds(data.Bounds);
        }

        public void SetBounds(RuntimeMapBounds bounds)
        {
            Bounds = bounds;
            HasBounds = bounds.MaxX > bounds.MinX && bounds.MaxY > bounds.MinY;
        }

        /// <summary>World cells (same space as the hero pose). Radius 0 hides that ring.</summary>
        public void SetZone(int phase, float x, float y, float radius, float nextX, float nextY, float nextRadius)
        {
            HasZone = true;
            Phase = phase;
            ZoneCentre = new Vector2(x, y);
            ZoneRadius = radius;
            SafeCentre = new Vector2(nextX, nextY);
            SafeRadius = nextRadius;
        }

        private void LateUpdate()
        {
            if (_hud != null) Apply(_hud.MapSizeCells);
        }

        /// <summary>Reads the session zone (if any) and places both rings for a map of <paramref name="mapSizeCells"/>.</summary>
        public void Apply(int mapSizeCells)
        {
            Read();
            var projection = new MinimapProjection(mapSizeCells, area != null ? area.rect.width : 0f);
            Place(zoneRing, projection, ZoneCentre, ZoneRadius);
            Place(safeRing, projection, SafeCentre, SafeRadius);
        }

        private void Read()
        {
            if (_online != null && _online.HasZone && _online.Phase == MatchPhase.InGame)
            {
                var z = _online.Zone;
                SetZone(z.PhaseIndex, z.CurrentCenterX, z.CurrentCenterY, z.CurrentRadius, z.TargetCenterX, z.TargetCenterY, z.TargetRadius);
            }
            else if (_offline != null && _offline.Zone != null)
            {
                var z = _offline.Zone;
                SetZone(z.PhaseIndex, z.CurrentCenter.X, z.CurrentCenter.Y, z.CurrentRadius, z.NextCenter.X, z.NextCenter.Y, z.NextRadius);
            }
        }

        /// <summary>Rect = the ring's centre-line diameter; the stroke stays <see cref="RingGraphic.Thickness"/>.</summary>
        private void Place(RingGraphic ring, MinimapProjection projection, Vector2 centre, float radius)
        {
            if (ring == null) return;
            var visible = draw && HasZone && radius > 0f;
            if (ring.gameObject.activeSelf != visible) ring.gameObject.SetActive(visible);
            if (!visible) return;
            projection.Project(centre.x, centre.y, out var px, out var py);
            var t = ring.rectTransform;
            var diameter = 2f * radius * projection.PixelsPerCell;
            var position = new Vector2(px, py);
            if (t.anchoredPosition != position) t.anchoredPosition = position;
            if (!Mathf.Approximately(t.sizeDelta.x, diameter)) t.sizeDelta = new Vector2(diameter, diameter);
        }
    }
}
