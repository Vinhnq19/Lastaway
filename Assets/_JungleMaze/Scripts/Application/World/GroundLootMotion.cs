using System;

namespace JungleMaze.App.World
{
    /// <summary>Ground position and height above it of a flying item at one moment.</summary>
    public struct GroundLootPose
    {
        public float X, Y;
        /// <summary>Height above the ground point in cells (drawn as a vertical offset in top-down view).</summary>
        public float Lift;
        /// <summary>True once the flight (including the bounce) has finished.</summary>
        public bool Landed;
    }

    /// <summary>
    /// Pure curves for ground loot presentation (Docs/hvu/07_Floor_Loot.vi.md sections 5 and 7): the hover bob and the
    /// launch arc with a small bounce. Every number comes from the caller's tuning; no UnityEngine dependency.
    /// </summary>
    public static class GroundLootMotion
    {
        /// <summary>Golden-ratio phase per id, so neighbouring items never bob in step.</summary>
        public static float Phase(ushort id) => (float)(id * 0.6180339887498949 % 1.0);

        /// <summary>Hover offset in cells: <paramref name="amplitude"/> × sin over <paramref name="period"/> seconds, shifted per id.</summary>
        public static float Bob(double time, ushort id, float amplitude, float period)
        {
            if (!(period > 0) || !(amplitude > 0)) return 0f;
            return amplitude * (float)Math.Sin(2 * Math.PI * (time / period + Phase(id)));
        }

        /// <summary>Quadratic ease-out: fast start, gentle arrival.</summary>
        public static float EaseOut(float u)
        {
            u = Clamp01(u);
            return 1f - (1f - u) * (1f - u);
        }

        /// <summary>
        /// Launch pose <paramref name="elapsed"/> seconds after take-off. The ground point eases out from
        /// <paramref name="fromX"/>,<paramref name="fromY"/> to the item position over the first <c>1 - bounceShare</c> of
        /// <paramref name="duration"/> while the lift follows one arc peaking at <paramref name="height"/>; the rest of the
        /// duration is a small hop of <paramref name="bounceHeight"/> on the landing point.
        /// </summary>
        public static GroundLootPose Flight(float elapsed, float fromX, float fromY, float toX, float toY,
            float height, float duration, float bounceHeight, float bounceShare)
        {
            if (!(duration > 0) || elapsed >= duration) return new GroundLootPose { X = toX, Y = toY, Landed = true };
            if (!(elapsed > 0)) elapsed = 0f;
            bounceShare = bounceShare < 0f ? 0f : bounceShare > .9f ? .9f : bounceShare;
            var main = duration * (1f - bounceShare);
            if (elapsed < main)
            {
                var u = elapsed / main;
                var p = EaseOut(u);
                // The arc follows the eased progress so the peak arrives early and the drop is gentle.
                return new GroundLootPose { X = fromX + (toX - fromX) * p, Y = fromY + (toY - fromY) * p, Lift = 4f * height * p * (1f - p) };
            }
            var b = bounceShare > 0 ? (elapsed - main) / (duration - main) : 1f;
            return new GroundLootPose { X = toX, Y = toY, Lift = 4f * bounceHeight * b * (1f - b) };
        }

        private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
    }
}
