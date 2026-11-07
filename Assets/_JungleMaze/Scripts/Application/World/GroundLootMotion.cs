using System;

namespace JungleMaze.App.World
{
    /// <summary>
    /// Pure curves for ground loot presentation (Docs/hvu/07_Floor_Loot.vi.md section 7): the hover bob.
    /// Every number comes from the caller's tuning; no UnityEngine dependency.
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
    }
}
