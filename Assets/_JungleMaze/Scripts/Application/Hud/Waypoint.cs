using System;

namespace JungleMaze.App.Hud
{
    /// <summary>
    /// Waypoint numbers (T-018 stage 2, de-xuat/minimap-chuc-nang.md 5 D1; sizes from ux/hud-B-spec.md 5, 1080p px).
    /// Designer and art-ux values, tuned after Huy plays; serialized on the HUD, not in balance JSON.
    /// </summary>
    [Serializable]
    public struct WaypointTuning
    {
        /// <summary>W3 click-to-remove and W4 auto-remove radius, cells.</summary>
        public float ReachCells;
        public float MinimapFlagPx;
        public float BigMapFlagPx;
        /// <summary>W9 flag drawn on the ground while it is on screen.</summary>
        public float WorldFlagPx;
        public float EdgeArrowPx;
        /// <summary>Gap kept from the screen edge and from HUD panels by the edge arrow.</summary>
        public float EdgeMarginPx;

        public static WaypointTuning Default => new WaypointTuning
        {
            ReachCells = 1.5f, MinimapFlagPx = 18f, BigMapFlagPx = 24f, WorldFlagPx = 40f, EdgeArrowPx = 40f, EdgeMarginPx = 16f
        };
    }

    /// <summary>
    /// One personal waypoint, client only (W11: no packet). W1 a big-map click sets it, clamped to the map; W2 a new one
    /// replaces the old; W3 a click within <see cref="WaypointTuning.ReachCells"/> of it (or a right click) removes it;
    /// W4 the hero coming within that radius removes it; W5 no timeout; W6 the hero's death removes it.
    /// </summary>
    public sealed class Waypoint
    {
        public bool Has { get; private set; }
        public float X { get; private set; }
        public float Y { get; private set; }

        /// <summary>Left click on the big map at world (<paramref name="x"/>, <paramref name="y"/>).</summary>
        public void Click(float x, float y, int mapSizeCells, float reachCells)
        {
            x = Clamp(x, 0f, mapSizeCells);
            y = Clamp(y, 0f, mapSizeCells);
            if (Has && DistanceTo(x, y) <= reachCells) { Clear(); return; }
            Has = true;
            X = x;
            Y = y;
        }

        public void Clear() => Has = false;

        public void Tick(bool heroAlive, bool hasHero, float heroX, float heroY, float reachCells)
        {
            if (!Has) return;
            if (!heroAlive) Clear();
            else if (hasHero && DistanceTo(heroX, heroY) <= reachCells) Clear();
        }

        public float DistanceTo(float x, float y)
        {
            var dx = X - x;
            var dy = Y - y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        private static float Clamp(float v, float min, float max) => v < min ? min : v > max ? max : v;
    }
}
