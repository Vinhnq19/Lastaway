using System.Collections.Generic;
using JungleMaze.LevelEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace JungleMaze.Presentation.Rendering
{
    /// <summary>
    /// World-space 2D lights placed on the map when its art is built: warm "sun pools" where sunlight breaks through the
    /// canopy onto open ground, plus small lights on stone lanterns, rune pillars and portals. Positions come from the map
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

            // Sun pools: one candidate per grid cell, jittered by the map seed, kept only on mostly open ground.
            var step = Mathf.Max(3, t.SunPoolSpacing);
            for (var gy = 0; gy < doc.Height; gy += step)
            for (var gx = 0; gx < doc.Width; gx += step)
            {
                var cx = gx + Mathf.FloorToInt(LevelGround.Hash01(doc.Seed, gx, gy, 501) * step);
                var cy = gy + Mathf.FloorToInt(LevelGround.Hash01(doc.Seed, gx, gy, 502) * step);
                if (cx < 2 || cy < 2 || cx >= doc.Width - 2 || cy >= doc.Height - 2) continue;
                if (OpenRatio(occupied, cx, cy, 2) < t.SunPoolOpen) continue;
                var k = LevelGround.Hash01(doc.Seed, cx, cy, 503);
                var radius = Mathf.Lerp(t.SunPoolRadius.x, t.SunPoolRadius.y, k);
                var intensity = Mathf.Lerp(t.SunPoolIntensity.x, t.SunPoolIntensity.y, LevelGround.Hash01(doc.Seed, cx, cy, 504));
                Add(root, "Sun pool", new Vector2(cx + .5f, cy + .5f), t.SunPoolColor, radius, intensity, t.SunPoolFalloff);
                count++;
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

        private static float OpenRatio(HashSet<Vector2Int> occupied, int cx, int cy, int r)
        {
            if (occupied == null) return 1f;
            int open = 0, total = 0;
            for (var y = cy - r; y <= cy + r; y++)
            for (var x = cx - r; x <= cx + r; x++) { total++; if (!occupied.Contains(new Vector2Int(x, y))) open++; }
            return total > 0 ? (float)open / total : 1f;
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
