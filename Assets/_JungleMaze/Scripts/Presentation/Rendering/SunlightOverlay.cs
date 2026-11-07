using UnityEngine;

namespace JungleMaze.Presentation.Rendering
{
    /// <summary>
    /// Warm sunlight drawn over the gameplay view: soft diagonal light shafts that sway slowly, a sun glow in one corner
    /// and dust motes drifting through the light. Everything follows the main camera's view rect; textures are generated
    /// once (no art assets). Presentation only; numbers come from <see cref="GameplayPostTuning"/>.
    /// </summary>
    public sealed class SunlightOverlay : MonoBehaviour
    {
        private const string Shader2D = "Universal Render Pipeline/2D/Sprite-Unlit-Default";
        private const int MaxRays = 8, MaxDust = 96;

        private GameplayPostTuning _t;
        private Material _material;
        private Texture2D _rayTex, _dotTex, _glowTex;
        private Sprite _ray, _dot, _glow;
        private SpriteRenderer[] _rays;
        private SpriteRenderer _sunGlow;
        private SpriteRenderer[] _dust;
        private Vector2[] _dustPos;
        private float[] _rayPhase, _rayOffset, _rayWidth, _dustPhase, _dustSpeed;
        private bool _built;

        public void Configure(GameplayPostTuning tuning)
        {
            _t = tuning;
            if (!_built) Build();
        }

        private void Build()
        {
            _built = true;
            var shader = Shader.Find(Shader2D);
            _material = shader != null ? new Material(shader) { name = "Sunlight (runtime)" } : null;
            _rayTex = MakeTexture(32, 256, (u, v) => Gauss(u, .5f, .22f) * Mathf.SmoothStep(0f, 1f, Mathf.Min(v, 1f - v) * 3f));
            _dotTex = MakeTexture(16, 16, (u, v) => Gauss(Vector2.Distance(new Vector2(u, v), new Vector2(.5f, .5f)), 0f, .22f));
            _glowTex = MakeTexture(128, 128, (u, v) => Mathf.Pow(Mathf.Clamp01(1f - Vector2.Distance(new Vector2(u, v), new Vector2(.5f, .5f)) * 2f), 2.2f));
            _ray = Sprite.Create(_rayTex, new Rect(0, 0, 32, 256), new Vector2(.5f, .5f), 256f);   // 1 unit tall, 1/8 wide
            _dot = Sprite.Create(_dotTex, new Rect(0, 0, 16, 16), new Vector2(.5f, .5f), 16f);     // 1 unit
            _glow = Sprite.Create(_glowTex, new Rect(0, 0, 128, 128), new Vector2(.5f, .5f), 128f); // 1 unit

            var random = new System.Random(7741);
            _rays = new SpriteRenderer[MaxRays]; _rayPhase = new float[MaxRays]; _rayOffset = new float[MaxRays]; _rayWidth = new float[MaxRays];
            for (var i = 0; i < MaxRays; i++)
            {
                _rays[i] = Child("Sun ray " + i, _ray, 0);
                _rayPhase[i] = (float)random.NextDouble() * 10f;
                _rayOffset[i] = Mathf.Lerp(-.9f, .9f, (i + (float)random.NextDouble() * .6f) / MaxRays);
                _rayWidth[i] = (float)random.NextDouble();
            }
            _sunGlow = Child("Sun glow", _glow, -1);
            _dust = new SpriteRenderer[MaxDust]; _dustPos = new Vector2[MaxDust]; _dustPhase = new float[MaxDust]; _dustSpeed = new float[MaxDust];
            for (var i = 0; i < MaxDust; i++)
            {
                _dust[i] = Child("Dust " + i, _dot, 1);
                _dustPos[i] = new Vector2((float)random.NextDouble(), (float)random.NextDouble());
                _dustPhase[i] = (float)random.NextDouble() * 6.283f;
                _dustSpeed[i] = .6f + (float)random.NextDouble() * .8f;
            }
        }

        private SpriteRenderer Child(string name, Sprite sprite, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = sprite;
            if (_material != null) r.sharedMaterial = _material;
            // Above every world layer (tree canopies included); the HUD is a screen overlay canvas and stays on top.
            r.sortingLayerID = SortingLayers.IdOf(SortingLayers.Vfx);
            r.sortingOrder = 900 + order;
            r.enabled = false;
            return r;
        }

        /// <summary>Lays the light out over <paramref name="camera"/>'s view; hides everything when <paramref name="visible"/> is false.</summary>
        public void Render(Camera camera, bool visible, float time, float dt)
        {
            if (!_built || _t == null) return;
            var show = visible && _t.SunEnabled && camera != null && camera.orthographic;
            if (_material != null) _material.SetColor("_Color", new Color(_t.SunHdrBoost, _t.SunHdrBoost, _t.SunHdrBoost, 1f));
            var rayCount = show ? Mathf.Clamp(_t.SunRays, 0, MaxRays) : 0;
            var dustCount = show ? Mathf.Clamp(_t.DustCount, 0, MaxDust) : 0;
            for (var i = 0; i < MaxRays; i++) _rays[i].enabled = i < rayCount;
            for (var i = 0; i < MaxDust; i++) _dust[i].enabled = i < dustCount;
            _sunGlow.enabled = show && _t.SunGlowAlpha > 0f;
            if (!show) return;

            var halfH = camera.orthographicSize; var halfW = halfH * camera.aspect;
            var center = (Vector2)camera.transform.position;
            var angle = _t.SunAngle * Mathf.Deg2Rad;
            var along = new Vector2(Mathf.Sin(angle), -Mathf.Cos(angle)); // shafts fall from the sun corner
            var across = new Vector2(-along.y, along.x);
            var reach = Mathf.Sqrt(halfW * halfW + halfH * halfH);
            var sun = _t.SunColor;

            for (var i = 0; i < rayCount; i++)
            {
                var sway = Mathf.Sin(time * _t.SunSwaySpeed + _rayPhase[i]) * _t.SunSway;
                var pos = center + across * (_rayOffset[i] * reach + sway);
                var width = Mathf.Lerp(_t.SunRayWidth.x, _t.SunRayWidth.y, _rayWidth[i]);
                var t = _rays[i].transform;
                t.position = new Vector3(pos.x, pos.y, 0f);
                t.rotation = Quaternion.Euler(0f, 0f, _t.SunAngle);
                t.localScale = new Vector3(width * 8f, reach * 2.2f, 1f); // sprite is 1/8 unit wide, 1 unit tall
                var flicker = .7f + .3f * Mathf.Sin(time * .45f + _rayPhase[i] * 1.7f);
                _rays[i].color = new Color(sun.r, sun.g, sun.b, _t.SunRayAlpha * flicker);
            }

            var corner = center + new Vector2(_t.SunCorner.x * halfW, _t.SunCorner.y * halfH);
            _sunGlow.transform.position = new Vector3(corner.x, corner.y, 0f);
            var glowSize = halfH * 2f * _t.SunGlowSize;
            _sunGlow.transform.localScale = new Vector3(glowSize, glowSize, 1f);
            _sunGlow.color = new Color(sun.r, sun.g, sun.b, _t.SunGlowAlpha);

            var drift = (along * .35f + Vector2.up * .65f) * _t.DustSpeed / (halfH * 2f);
            for (var i = 0; i < dustCount; i++)
            {
                var p = _dustPos[i] + drift * (_dustSpeed[i] * dt);
                p.x += Mathf.Sin(time * .7f + _dustPhase[i]) * .0006f;
                p.x -= Mathf.Floor(p.x); p.y -= Mathf.Floor(p.y);
                _dustPos[i] = p;
                var t = _dust[i].transform;
                t.position = new Vector3(center.x + (p.x - .5f) * 2f * halfW, center.y + (p.y - .5f) * 2f * halfH, 0f);
                var size = _t.DustSize * (.6f + .4f * _dustSpeed[i]);
                t.localScale = new Vector3(size, size, 1f);
                var twinkle = .45f + .55f * Mathf.Max(0f, Mathf.Sin(time * 1.3f * _dustSpeed[i] + _dustPhase[i]));
                _dust[i].color = new Color(sun.r, sun.g, sun.b, _t.DustAlpha * twinkle);
            }
        }

        private static float Gauss(float x, float mean, float sigma) { var d = (x - mean) / sigma; return Mathf.Exp(-.5f * d * d); }

        private static Texture2D MakeTexture(int w, int h, System.Func<float, float, float> alpha)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = "Sunlight" };
            var px = new Color32[w * h];
            for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
                px[y * w + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(alpha((x + .5f) / w, (y + .5f) / h)) * 255f));
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return tex;
        }

        private void OnDestroy()
        {
            if (_material != null) Destroy(_material);
            foreach (var o in new Object[] { _ray, _dot, _glow, _rayTex, _dotTex, _glowTex }) if (o != null) Destroy(o);
        }
    }
}
