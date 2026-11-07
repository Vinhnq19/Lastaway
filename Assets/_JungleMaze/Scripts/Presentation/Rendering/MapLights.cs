using System.Collections.Generic;
using JungleMaze.LevelEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace JungleMaze.Presentation.Rendering
{
    /// <summary>
    /// World-space 2D lights placed on the map when its art is built: long soft shafts of warm sunlight lying diagonally
    /// across the map in patches, plus small lights on stone lanterns, rune pillars and portals. Positions come from the map
    /// (seed + placements), so every client sees the same light; numbers come from <see cref="GameplayPostTuning"/>.
    /// The map art must use a lit sprite material for these lights to show.
    /// </summary>
    public static class MapLights
    {
        /// <summary>Adds the lights under <paramref name="parent"/>; returns how many were placed.</summary>
        public static int Build(LevelDocument doc, Transform parent, HashSet<Vector2Int> occupied, GameplayPostTuning t)
        {
            if (doc == null || parent == null || t == null || !t.MapLightsEnabled) return 0;
            var root = new GameObject("Map lights").transform;
            root.SetParent(parent, false);
            var count = 0;

            // Sun patches: long soft shafts of sunlight lying across the map, all parallel (one sun), cut into segments with
            // gaps so the light falls in patches. Laid out along lines perpendicular to the sun direction, jittered by the seed.
            var shaft = ShaftSprite();
            var angle = t.SunPatchAngle * Mathf.Deg2Rad;
            var along = new Vector2(-Mathf.Sin(angle), Mathf.Cos(angle));
            var across = new Vector2(along.y, -along.x);
            var centre = new Vector2(doc.Width * .5f, doc.Height * .5f);
            var reach = Mathf.Sqrt(doc.Width * doc.Width + doc.Height * doc.Height) * .5f;
            var spacing = Mathf.Max(4f, t.SunPatchSpacing);
            var line = 0;
            for (var a = -reach; a <= reach; a += spacing, line++)
            {
                var offset = a + (LevelGround.Hash01(doc.Seed, line, 0, 601) - .5f) * spacing * .5f;
                var b = -reach - LevelGround.Hash01(doc.Seed, line, 0, 602) * t.SunPatchLength.y;
                for (var seg = 0; b <= reach; seg++)
                {
                    var length = Mathf.Lerp(t.SunPatchLength.x, t.SunPatchLength.y, LevelGround.Hash01(doc.Seed, line, seg, 603));
                    var width = Mathf.Lerp(t.SunPatchWidth.x, t.SunPatchWidth.y, LevelGround.Hash01(doc.Seed, line, seg, 604));
                    var gap = Mathf.Lerp(t.SunPatchGap.x, t.SunPatchGap.y, LevelGround.Hash01(doc.Seed, line, seg, 605));
                    var mid = centre + across * offset + along * (b + length * .5f);
                    b += length + gap;
                    if (LevelGround.Hash01(doc.Seed, line, seg, 606) < t.SunPatchSkip) continue;
                    if (mid.x < -width || mid.y < -width || mid.x > doc.Width + width || mid.y > doc.Height + width) continue;
                    var intensity = Mathf.Lerp(t.SunPatchIntensity.x, t.SunPatchIntensity.y, LevelGround.Hash01(doc.Seed, line, seg, 607));
                    AddShaft(root, shaft, mid, t.SunPatchAngle, width, length, t.SunPatchColor, intensity);
                    count++;
                }
            }

            // Glowing props from the level: a light at the visual centre of each one.
            foreach (var p in doc.Placements)
            {
                Color color; float radius, intensity;
                switch (p.Type)
                {
                    case "StoneLantern": color = t.LanternColor; radius = t.LanternRadius; intensity = t.LanternIntensity; break;
                    case "RunePillarX2": case "RunePillarX3": case "RunePillarBroken": color = t.RuneColor; radius = t.RuneRadius; intensity = t.RuneIntensity; break;
                    case "Portal": color = t.PortalColor; radius = t.PortalRadius; intensity = t.PortalIntensity; break;
                    default: continue;
                }
                var b = p.Bounds;
                Add(root, p.Type + " light", new Vector2((b.MinX + b.MaxX) * .5f, (b.MinY + b.MaxY) * .5f + .5f), color, radius, intensity, .6f);
                count++;
            }
            return count;
        }

        private static Sprite _shaft;

        /// <summary>
        /// Shape of one sunlight shaft, generated once: 1 unit across and 4 units long, soft wavy edges, faded ends and a
        /// dappled inside (light through leaves). Grey in rgb and alpha so the light colour does the tinting.
        /// </summary>
        private static Sprite ShaftSprite()
        {
            if (_shaft != null) return _shaft;
            const int w = 96, h = 384;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = "Sun shaft" };
            var px = new Color32[w * h];
            for (var y = 0; y < h; y++)
            {
                var v = (y + .5f) / h;
                // Edges wander a little along the shaft so it is not a ruler-straight band.
                var left = .14f + .08f * Mathf.PerlinNoise(3.1f, v * 5f);
                var right = .86f - .08f * Mathf.PerlinNoise(7.7f, v * 5f);
                var ends = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(Mathf.Min(v, 1f - v) / .2f));
                for (var x = 0; x < w; x++)
                {
                    var u = (x + .5f) / w;
                    var edge = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((u - left) / .16f)) * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((right - u) / .16f));
                    var dapple = .62f + .38f * Mathf.PerlinNoise(u * 3.5f + 11f, v * 14f);
                    var value = (byte)Mathf.RoundToInt(Mathf.Clamp01(edge * ends * dapple) * 255f);
                    px[y * w + x] = new Color32(value, value, value, value);
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, false);
            _shaft = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(.5f, .5f), w);
            return _shaft;
        }

        private static void AddShaft(Transform root, Sprite sprite, Vector2 position, float angle, float width, float length, Color color, float intensity)
        {
            var go = new GameObject("Sun patch");
            go.transform.SetParent(root, false);
            go.transform.position = new Vector3(position.x, position.y, 0f);
            go.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            go.transform.localScale = new Vector3(width, length / 4f, 1f); // sprite is 1 x 4 units
            var light = go.AddComponent<Light2D>();
            light.lightCookieSprite = sprite;
            light.lightType = Light2D.LightType.Sprite;
            light.color = color;
            light.intensity = intensity;
            light.shadowsEnabled = false;
        }

        private static void Add(Transform root, string name, Vector2 position, Color color, float radius, float intensity, float falloff)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            go.transform.position = new Vector3(position.x, position.y, 0f);
            var light = go.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.pointLightOuterRadius = Mathf.Max(.5f, radius);
            light.pointLightInnerRadius = Mathf.Max(0f, radius * .25f);
            light.color = color;
            light.intensity = intensity;
            light.falloffIntensity = falloff;
            light.shadowsEnabled = false;
        }
    }
}
