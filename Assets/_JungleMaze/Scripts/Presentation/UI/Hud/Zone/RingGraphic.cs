using UnityEngine;
using UnityEngine.UI;

namespace JungleMaze.Presentation.UI.Hud
{
    /// <summary>
    /// Zone ring with a fixed stroke at any radius (T-022 R3, CPO 07/11): an annulus mesh whose centre line is the rect's
    /// inscribed circle, <see cref="Thickness"/> canvas units wide, textured from the ring band of a map_ring_* sprite
    /// (UV runs along the band, so the art's colour and soft edge stay; scaling the sprite would scale its stroke).
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RingGraphic : MaskableGraphic
    {
        [SerializeField] private Sprite sprite;
        [SerializeField] private float thickness = 4f / 1.5f;
        [Tooltip("Inner and outer radius of the sprite's ring band, as a fraction of the sprite size (0..0.5).")]
        [SerializeField] private float uvInner = 0.46f, uvOuter = 0.5f;
        [SerializeField] private int segments = 128;

        public Sprite Sprite => sprite;
        public float Thickness => thickness;

        public override Texture mainTexture => sprite != null ? sprite.texture : s_WhiteTexture;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = rectTransform.rect;
            var radius = Mathf.Min(r.width, r.height) / 2f;
            if (radius <= 0f || thickness <= 0f || segments < 3) return;
            var inner = Mathf.Max(0f, radius - thickness / 2f);
            var outer = radius + thickness / 2f;
            var uv = sprite != null ? UnityEngine.Sprites.DataUtility.GetOuterUV(sprite) : new Vector4(0f, 0f, 1f, 1f);
            var uvCentre = new Vector2((uv.x + uv.z) / 2f, (uv.y + uv.w) / 2f);
            var uvSize = new Vector2(uv.z - uv.x, uv.w - uv.y);
            var centre = r.center;
            Color32 tint = color;
            for (var i = 0; i <= segments; i++)
            {
                var a = i * 2f * Mathf.PI / segments;
                var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                vh.AddVert(centre + d * inner, tint, uvCentre + Vector2.Scale(d * uvInner, uvSize));
                vh.AddVert(centre + d * outer, tint, uvCentre + Vector2.Scale(d * uvOuter, uvSize));
                if (i == 0) continue;
                var k = i * 2;
                vh.AddTriangle(k - 2, k - 1, k + 1);
                vh.AddTriangle(k - 2, k + 1, k);
            }
        }
    }
}
