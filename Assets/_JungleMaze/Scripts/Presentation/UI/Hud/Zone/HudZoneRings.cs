using JungleBackend.Protocol.Gameplay;
using JungleMaze.App.Hud;
using JungleMaze.App.Hud.State;
using JungleMaze.App.Match;
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

        private IHudState _hud;
        private MatchSession _online;
        private OfflineMatchSession _offline;
        private UnityEngine.UI.RawImage _danger;
        private Material _dangerMaterial;

        public bool HasZone { get; private set; }
        public int Phase { get; private set; }
        public Vector2 ZoneCentre { get; private set; }
        public float ZoneRadius { get; private set; }
        public Vector2 SafeCentre { get; private set; }
        public float SafeRadius { get; private set; }
        public RingGraphic ZoneRing => zoneRing;
        public RingGraphic SafeRing => safeRing;

        [Inject]
        public void Construct(IHudState hud, MatchSession online, OfflineMatchSession offline)
        {
            _hud = hud;
            _online = online;
            _offline = offline;
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
            ApplyDanger(mapSizeCells);
            Place(zoneRing, projection, ZoneCentre, Phase > 0 ? ZoneRadius : 0f);
            Place(safeRing, projection, SafeCentre, SafeRadius);
        }

        private void ApplyDanger(int mapSizeCells)
        {
            var visible = HasZone && Phase > 0 && mapSizeCells > 0;
            if (visible && _danger == null && area != null)
            {
                var shader = Resources.Load<Shader>("ZoneOverlay");
                if (shader == null) return;
                _dangerMaterial = new Material(shader);
                _dangerMaterial.SetFloat("_Minimap", 1f);
                _dangerMaterial.SetFloat("_FillAlpha", .62f);
                var layer = new GameObject("Map danger fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.RawImage));
                layer.transform.SetParent(area, false);
                _danger = layer.GetComponent<UnityEngine.UI.RawImage>();
                _danger.raycastTarget = false;
                _danger.material = _dangerMaterial;
                if (zoneRing != null) layer.transform.SetSiblingIndex(zoneRing.transform.GetSiblingIndex());
                else if (safeRing != null) layer.transform.SetSiblingIndex(safeRing.transform.GetSiblingIndex());
            }
            if (_danger == null) return;
            _danger.enabled = visible;
            if (!visible) return;
            // Crop the fill inside the frame without shifting its world-to-map coordinates.
            var size = area.rect.size;
            float inset = Mathf.Min(3f, Mathf.Min(size.x, size.y) * .25f);
            var rect = _danger.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.one * inset;
            rect.offsetMax = -Vector2.one * inset;
            _danger.uvRect = new Rect(inset / Mathf.Max(size.x, 1f), inset / Mathf.Max(size.y, 1f),
                1f - 2f * inset / Mathf.Max(size.x, 1f), 1f - 2f * inset / Mathf.Max(size.y, 1f));
            _dangerMaterial.SetVector("_Current", new Vector4(ZoneCentre.x, ZoneCentre.y, ZoneRadius, 0f));
            _dangerMaterial.SetVector("_MapBounds", new Vector4(0f, 0f, mapSizeCells, mapSizeCells));
        }

        private void OnDestroy()
        {
            if (_danger != null) Destroy(_danger.gameObject);
            if (_dangerMaterial != null) Destroy(_dangerMaterial);
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
            var visible = HasZone && radius > 0f;
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
