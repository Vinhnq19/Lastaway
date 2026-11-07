using JungleMaze.App.Ports;
using UnityEngine;
using UnityEngine.UI;

namespace JungleMaze.Presentation.UI.Hud
{
    /// <summary>
    /// The map team's minimap zone layer, copied for the big map (CPO 07/11 "kiểu main"). Every value here mirrors
    /// <c>ZoneOverlayView.CreateWorld()</c> / <c>CreateMinimap()</c> / <c>Apply()</c> / <c>Show()</c>
    /// (Scripts/Presentation/UI/ZoneOverlayView.cs, Quang Huy, read only): re-check them when that file changes.
    /// </summary>
    public static class ZoneOverlayParams
    {
        /// <summary><c>Resources.Load&lt;Shader&gt;("ZoneOverlay")</c> in CreateWorld; shader name "JungleMaze/ZoneOverlay".</summary>
        public const string ShaderResource = "ZoneOverlay";
        public const string ShaderName = "JungleMaze/ZoneOverlay";
        /// <summary>CreateWorld: <c>_mapMaterial.SetFloat("_Minimap", 1)</c>, <c>SetFloat("_FillAlpha", .62f)</c>.</summary>
        public const float Minimap = 1f, FillAlpha = 0.62f;
        /// <summary>CreateMinimap: <c>MinimapFrameInset = 3</c>, capped at a quarter of the map size.</summary>
        public const float FrameInset = 3f;

        /// <summary>CreateMinimap crop: the inset as a fraction of the minimap side (uvRect inset), same ratio on any map size.</summary>
        public static float UvInset(float minimapSize) => minimapSize <= 0f ? 0f : Mathf.Min(FrameInset, minimapSize * .25f) / minimapSize;

        /// <summary>Show(): the minimap layer previews the next ring whenever it exists (<c>state.TargetRadius &gt; 0</c>).</summary>
        public static bool MapPreview(float targetRadius) => targetRadius > 0f;

        /// <summary>Apply(): _Current, _Next (w = preview), _MapBounds (min x, min y, width, height).</summary>
        public static void Apply(Material material, Vector2 current, float radius, Vector2 next, float nextRadius, RuntimeMapBounds bounds)
        {
            material.SetVector("_Current", new Vector4(current.x, current.y, radius, 0));
            material.SetVector("_Next", new Vector4(next.x, next.y, nextRadius, MapPreview(nextRadius) ? 1 : 0));
            material.SetVector("_MapBounds", new Vector4(bounds.MinX, bounds.MinY, bounds.MaxX - bounds.MinX, bounds.MaxY - bounds.MinY));
        }
    }

    /// <summary>
    /// Big-map zone layer (B5): a RawImage over the map texture, under grid and pin, drawn by the ZoneOverlay shader in
    /// minimap mode with its own material (made at run time, destroyed with the view). Data from <see cref="HudZoneRings"/>.
    /// </summary>
    [RequireComponent(typeof(RawImage))]
    public sealed class HudZoneOverlay : MonoBehaviour
    {
        [SerializeField] private MinimapView minimap;

        private RawImage _image;
        private Material _material;

        public Material Material => _material;
        public RawImage Image => _image != null ? _image : (_image = GetComponent<RawImage>());

        /// <summary>Shows the layer for the zone in <paramref name="rings"/>; hidden when it has no zone or no map bounds.</summary>
        public void Apply(HudZoneRings rings)
        {
            if (rings == null || !rings.HasZone || !rings.HasBounds) { Image.enabled = false; return; }
            Show(rings.ZoneCentre, rings.ZoneRadius, rings.SafeCentre, rings.SafeRadius, rings.Bounds);
        }

        public void Show(Vector2 current, float radius, Vector2 next, float nextRadius, RuntimeMapBounds bounds)
        {
            if (!EnsureMaterial()) return;
            ZoneOverlayParams.Apply(_material, current, radius, next, nextRadius, bounds);
            Crop();
            Image.enabled = true;
        }

        private bool EnsureMaterial()
        {
            if (_material != null) return true;
            var shader = Resources.Load<Shader>(ZoneOverlayParams.ShaderResource);
            if (shader == null) { Image.enabled = false; return false; }
            _material = new Material(shader) { name = "Big map zone overlay" };
            _material.SetFloat("_Minimap", ZoneOverlayParams.Minimap);
            _material.SetFloat("_FillAlpha", ZoneOverlayParams.FillAlpha);
            Image.material = _material;
            Image.raycastTarget = false;
            return true;
        }

        /// <summary>CreateMinimap crops the layer by its frame inset instead of rescaling map coordinates; same ratio here.</summary>
        private void Crop()
        {
            var uv = ZoneOverlayParams.UvInset(minimap != null ? minimap.SizePixels : 0f);
            var rect = Image.rectTransform;
            var parent = (RectTransform)rect.parent;
            var inset = parent.rect.width * uv;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
            Image.uvRect = new Rect(uv, uv, 1 - 2 * uv, 1 - 2 * uv);
        }

        private void OnDestroy()
        {
            if (_material != null) Destroy(_material);
        }
    }
}
