using JungleBackend.Protocol.Gameplay;
using JungleMaze.App.Ports;
using JungleMaze.Presentation.UI.Hud;
using UnityEngine;
using UnityEngine.UI;

namespace JungleMaze.Presentation.UI
{
    /// <summary>Draws the same authoritative circles in the world and on the HUD minimap.</summary>
    public sealed class ZoneOverlayView : MonoBehaviour
    {
        private Material _worldMaterial;
        private Material _mapMaterial;
        private Mesh _mesh;
        private MeshRenderer _renderer;
        private RawImage _map;
        private TMPro.TextMeshProUGUI _status;

        public void Show(ZoneStatePacket state, uint tick, RuntimeMapBounds bounds, Vector2 player)
        {
            if (_worldMaterial == null) CreateWorld();
            if (_worldMaterial == null) return;
            if (_map == null) CreateMinimap();
            bool preview = state.TargetRadius > 0 && (state.InWarning ||
                (unchecked((int)(tick - state.ShrinkStartTick)) >= 0 && unchecked((int)(state.ShrinkEndTick - tick)) > 0));
            Apply(_worldMaterial, state, bounds, preview);
            _renderer.enabled = true;
            if (_map != null)
            {
                Apply(_mapMaterial, state, bounds, preview);
                _map.enabled = true;
                bool outside = state.CurrentRadius <= .0001f || Vector2.Distance(player,
                    new Vector2(state.CurrentCenterX, state.CurrentCenterY)) > state.CurrentRadius;
                bool shrinking = unchecked((int)(tick - state.ShrinkStartTick)) >= 0 &&
                    unchecked((int)(state.ShrinkEndTick - tick)) > 0;
                float seconds = Mathf.Max(0, unchecked((int)((shrinking ? state.ShrinkEndTick : state.ShrinkStartTick) - tick)) / 30f);
                _status.enabled = outside || shrinking || state.InWarning;
                _status.SetText(outside ? "NGOÀI BO\n{0:0}s" : shrinking ? "BO ĐANG CO\n{0:0}s" : "BO CO SAU\n{0:0}s", Mathf.Ceil(seconds));
                _status.color = outside ? new Color(1, .65f, .9f) : Color.white;
            }
        }

        private void CreateWorld()
        {
            var shader = Resources.Load<Shader>("ZoneOverlay");
            if (shader == null) { Debug.LogError("ZoneOverlay shader is missing."); return; }
            _worldMaterial = new Material(shader);
            _mapMaterial = new Material(shader);
            _mapMaterial.SetFloat("_Minimap", 1);
            _mapMaterial.SetFloat("_FillAlpha", .55f);
            var surface = new GameObject("Zone danger overlay", typeof(MeshFilter), typeof(MeshRenderer));
            surface.transform.SetParent(transform, false);
            _mesh = new Mesh { name = "Zone overlay viewport" };
            _mesh.vertices = new[] { new Vector3(-10000,-10000), new Vector3(10000,-10000), new Vector3(10000,10000), new Vector3(-10000,10000) };
            _mesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
            _mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            surface.GetComponent<MeshFilter>().sharedMesh = _mesh;
            _renderer = surface.GetComponent<MeshRenderer>();
            _renderer.sharedMaterial = _worldMaterial;
            _renderer.sortingLayerName = "Foreground";
            _renderer.sortingOrder = 100;
        }

        private void CreateMinimap()
        {
            var minimap = FindFirstObjectByType<MinimapView>();
            if (minimap == null || minimap.Marker == null) return;
            var layer = new GameObject("Zone circles", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            layer.transform.SetParent(minimap.Marker.parent, false);
            _map = layer.GetComponent<RawImage>();
            _map.raycastTarget = false;
            _map.material = _mapMaterial;
            var rect = _map.rectTransform;
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.one * minimap.SizePixels;
            layer.transform.SetSiblingIndex(minimap.Marker.GetSiblingIndex());
            var label = new GameObject("Zone status", typeof(RectTransform), typeof(CanvasRenderer), typeof(TMPro.TextMeshProUGUI));
            label.transform.SetParent(minimap.GetComponentInParent<Canvas>().rootCanvas.transform, false);
            _status = label.GetComponent<TMPro.TextMeshProUGUI>();
            _status.raycastTarget = false;
            _status.fontSize = 16;
            _status.alignment = TMPro.TextAlignmentOptions.Center;
            _status.rectTransform.anchorMin = _status.rectTransform.anchorMax = new Vector2(.5f, 1);
            _status.rectTransform.pivot = new Vector2(.5f, 1);
            _status.rectTransform.anchoredPosition = new Vector2(0, -24);
            _status.rectTransform.sizeDelta = new Vector2(260, 48);
        }

        private static void Apply(Material material, ZoneStatePacket state, RuntimeMapBounds bounds, bool preview)
        {
            material.SetVector("_Current", new Vector4(state.CurrentCenterX, state.CurrentCenterY, state.CurrentRadius, 0));
            material.SetVector("_Next", new Vector4(state.TargetCenterX, state.TargetCenterY, state.TargetRadius, preview ? 1 : 0));
            material.SetVector("_MapBounds", new Vector4(bounds.MinX, bounds.MinY, bounds.MaxX - bounds.MinX, bounds.MaxY - bounds.MinY));
        }

        public void Hide()
        {
            if (_renderer != null) _renderer.enabled = false;
            if (_map != null) _map.enabled = false;
            if (_status != null) _status.enabled = false;
        }

        private void OnDestroy()
        {
            if (_map != null) Destroy(_map.gameObject);
            if (_status != null) Destroy(_status.gameObject);
            if (_worldMaterial != null) Destroy(_worldMaterial);
            if (_mapMaterial != null) Destroy(_mapMaterial);
            if (_mesh != null) Destroy(_mesh);
        }
    }
}
