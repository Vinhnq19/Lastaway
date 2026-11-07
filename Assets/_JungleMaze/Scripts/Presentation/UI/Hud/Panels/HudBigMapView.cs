using JungleMaze.App.Hud;
using JungleMaze.App.Hud.State;
using JungleMaze.Core.DI;
using JungleMaze.Presentation.UI.Kit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JungleMaze.Presentation.UI.Hud
{
    /// <summary>
    /// B5 big map (T-022, mock T-021/b/b5-ban-do-lon.png): the minimap's own map texture (<see cref="MinimapMapCapture"/>)
    /// drawn large under map_grid, the zone rings (<see cref="HudZoneRings"/>, same look as the minimap), the hero pin at
    /// its pose and heading, and the header: zone phase, zone countdown, distance to the next safe zone (world cells,
    /// 0 inside it). Mock source of the header: "Pha 3 · Bo thu sau 01:24", "cách bo tiếp theo 420". Display only
    /// (no mark, zoom or pan yet). M toggles; ESC closes it first through <see cref="UiLayerStack"/>.
    /// </summary>
    public sealed class HudBigMapView : MonoBehaviour
    {
        [SerializeField] private GameObject content;
        [SerializeField] private RectTransform mapArea;
        [SerializeField] private RawImage map;
        [SerializeField] private MinimapMapCapture capture;
        [SerializeField] private HudZoneRings zoneRings;
        [SerializeField] private RectTransform player;
        [SerializeField] private TMP_Text zoneValue, phaseValue, distanceValue;
        [SerializeField] private KeyCode toggleKey = KeyCode.M;

        private IHudState _hud;
        private readonly char[] _text = new char[16];

        public bool IsOpen => content != null && content.activeSelf;
        public RectTransform Player => player;
        public RawImage Map => map;
        public TMP_Text ZoneValue => zoneValue;
        public TMP_Text PhaseValue => phaseValue;
        public TMP_Text DistanceValue => distanceValue;
        public HudZoneRings ZoneRings => zoneRings;
        public RectTransform ZoneRing => zoneRings != null && zoneRings.ZoneRing != null ? zoneRings.ZoneRing.rectTransform : null;
        public RectTransform SafeRing => zoneRings != null && zoneRings.SafeRing != null ? zoneRings.SafeRing.rectTransform : null;

        [Inject]
        public void Construct(IHudState hud) => _hud = hud;

        /// <summary>Same as <see cref="HudZoneRings.SetZone"/> (phase unknown = 0).</summary>
        public void SetZone(float x, float y, float radius, float nextX, float nextY, float nextRadius)
        {
            if (zoneRings != null) zoneRings.SetZone(0, x, y, radius, nextX, nextY, nextRadius);
        }

        public void Open()
        {
            if (content == null) return;
            content.SetActive(true);
            UiLayerStack.Push(this, Close); // ESC closes the top panel before the match menu (CPO 07/11)
            Refresh();
        }

        public void Close()
        {
            UiLayerStack.Remove(this);
            if (content != null) content.SetActive(false);
        }

        /// <summary>A disabled HUD (scene unload, spectator teardown) never leaves a stale ESC layer behind.</summary>
        private void OnDisable() => UiLayerStack.Remove(this);

        public void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey)) Toggle();
            if (IsOpen) Refresh();
        }

        public void Refresh()
        {
            var cells = _hud != null ? _hud.MapSizeCells : 1;
            if (capture != null)
            {
                if (map.texture != capture.Texture) map.texture = capture.Texture;
                map.enabled = map.texture != null;
            }
            if (zoneRings != null) zoneRings.Apply(cells);
            var projection = new MinimapProjection(cells, mapArea.rect.width);
            var known = _hud != null && _hud.HasPose;
            if (player != null)
            {
                if (player.gameObject.activeSelf != known) player.gameObject.SetActive(known);
                if (known)
                {
                    var pose = _hud.Pose;
                    projection.Project(pose.X, pose.Y, out var x, out var y);
                    player.anchoredPosition = new Vector2(x, y);
                    player.localEulerAngles = new Vector3(0f, 0f, MinimapProjection.HeadingDegrees(pose.RotationDegrees));
                }
            }
            if (zoneValue != null)
            {
                var status = _hud != null ? _hud.MatchStatus : default;
                var length = status.HasZoneCountdown ? HudFormat.MinutesSeconds(_text, status.ZoneSecondsRemaining) : HudFormat.ClockPlaceholder(_text);
                zoneValue.SetCharArray(_text, 0, length);
            }
            var zone = zoneRings != null && zoneRings.HasZone;
            // No data: the number stays empty (no invented value); the icon remains as in the mock.
            if (phaseValue != null)
                phaseValue.SetCharArray(_text, 0, zone ? HudFormat.Integer(_text, zoneRings.Phase) : 0);
            if (distanceValue != null)
            {
                var hasDistance = zone && known && zoneRings.SafeRadius > 0f;
                var distance = hasDistance ? HudPanelMath.DistanceOutside(_hud.Pose.X, _hud.Pose.Y, zoneRings.SafeCentre.x, zoneRings.SafeCentre.y, zoneRings.SafeRadius) : 0;
                distanceValue.SetCharArray(_text, 0, hasDistance ? HudFormat.Integer(_text, distance) : 0);
            }
        }
    }
}
