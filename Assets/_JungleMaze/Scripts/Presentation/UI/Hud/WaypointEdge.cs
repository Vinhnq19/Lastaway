using System.Collections.Generic;
using UnityEngine;

namespace JungleMaze.Presentation.UI.Hud
{
    /// <summary>
    /// W9 edge arrow placement: walk the ray from the hero to the waypoint (screen space) and stop at the first thing
    /// it meets, the screen edge inset by a margin or a HUD panel grown by that margin, so the arrow never sits on the
    /// minimap, vitals or quick bar and still points straight at the flag.
    /// </summary>
    public static class WaypointEdge
    {
        /// <summary>
        /// False when the target is in view (draw the ground flag at the target). True with <paramref name="point"/> the
        /// arrow centre otherwise. <paramref name="inset"/> keeps the arrow's half size plus the margin clear.
        /// </summary>
        public static bool Place(Vector2 origin, Vector2 target, Rect screen, IList<Rect> panels, float inset, out Vector2 point)
        {
            var d = target - origin;
            var bounds = Rect.MinMaxRect(screen.xMin + inset, screen.yMin + inset, screen.xMax - inset, screen.yMax - inset);
            var t = Exit(origin, d, bounds);
            if (panels != null)
                foreach (var p in panels)
                {
                    var grown = Rect.MinMaxRect(p.xMin - inset, p.yMin - inset, p.xMax + inset, p.yMax + inset);
                    if (grown.Contains(origin)) continue; // hero under a panel: that panel cannot block its own ray
                    if (Enter(origin, d, grown, out var e) && e < t) t = e;
                }
            point = origin + d * Mathf.Min(t, 1f);
            return t < 1f;
        }

        // Ray o + d t leaves the box at t (origin inside, clamped into it first).
        private static float Exit(Vector2 o, Vector2 d, Rect b)
        {
            var t = float.MaxValue;
            if (d.x > 0f) t = Mathf.Min(t, (b.xMax - o.x) / d.x);
            else if (d.x < 0f) t = Mathf.Min(t, (b.xMin - o.x) / d.x);
            if (d.y > 0f) t = Mathf.Min(t, (b.yMax - o.y) / d.y);
            else if (d.y < 0f) t = Mathf.Min(t, (b.yMin - o.y) / d.y);
            return Mathf.Max(0f, t);
        }

        // Slab test: first t >= 0 where the ray enters the box.
        private static bool Enter(Vector2 o, Vector2 d, Rect b, out float t)
        {
            float near = 0f, far = float.MaxValue;
            t = 0f;
            for (var axis = 0; axis < 2; axis++)
            {
                var oa = o[axis];
                var da = d[axis];
                var min = axis == 0 ? b.xMin : b.yMin;
                var max = axis == 0 ? b.xMax : b.yMax;
                if (Mathf.Abs(da) < 1e-6f)
                {
                    if (oa < min || oa > max) return false;
                    continue;
                }
                var t1 = (min - oa) / da;
                var t2 = (max - oa) / da;
                if (t1 > t2) (t1, t2) = (t2, t1);
                near = Mathf.Max(near, t1);
                far = Mathf.Min(far, t2);
                if (near > far) return false;
            }
            t = near;
            return true;
        }
    }
}
