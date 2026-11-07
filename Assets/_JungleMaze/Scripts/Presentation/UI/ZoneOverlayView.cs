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
        private const float MinimapFrameInset = 3f;
        private Material _worldMaterial;
        private Material _mapMaterial;
        private Mesh _mesh;
        private MeshRenderer _renderer;
        private RawImage _map;

        public void Show(ZoneStatePacket state, uint tick, RuntimeMapBounds bounds)
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
                Apply(_mapMaterial, state, bounds, state.TargetRadius > 0);
                _map.enabled = true;
            }
        }

        private void CreateWorld()
        {
            var shader = Resources.Load<Shader>("ZoneOverlay");
            if (shader == null) { Debug.LogError("ZoneOverlay shader is missing."); return; }
            _worldMaterial = new Material(shader);
            _mapMaterial = new Material(shader);
            _mapMaterial.SetFloat("_Minimap", 1);
            _mapMaterial.SetFloat("_FillAlpha", .62f);
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
            float mapSize = minimap.SizePixels;
            float inset = Mathf.Min(MinimapFrameInset, mapSize * .25f);
            rect.anchoredPosition = Vector2.one * inset;
            rect.sizeDelta = Vector2.one * (mapSize - 2 * inset);
            // Crop the overlay rather than rescaling its map coordinates relative to the player marker.
            float uvInset = inset / mapSize;
            _map.uvRect = new Rect(uvInset, uvInset, 1 - 2 * uvInset, 1 - 2 * uvInset);
            layer.transform.SetSiblingIndex(minimap.Marker.GetSiblingIndex());
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
        }

        private void OnDestroy()
        {
            if (_map != null) Destroy(_map.gameObject);
            if (_worldMaterial != null) Destroy(_worldMaterial);
            if (_mapMaterial != null) Destroy(_mapMaterial);
            if (_mesh != null) Destroy(_mesh);
        }
    }
}
