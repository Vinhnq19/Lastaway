namespace JungleMaze.Presentation.UI.Hud
{
    /// <summary>Pure placement rules of the B4/B5 panels (T-022), kept out of the views so EditMode tests reach them.</summary>
    public static class HudPanelMath
    {
        /// <summary>
        /// The item tooltip opens to the right of the hovered cell (arrow on its left, kit_tip_item); it flips to the left
        /// only when its right edge would pass <paramref name="limitRight"/>. Same units for every argument.
        /// </summary>
        public static bool TooltipFlipsLeft(float cellRight, float gap, float tooltipWidth, float limitRight) =>
            cellRight + gap + tooltipWidth > limitRight;

        /// <summary>Slot sprite of the bag grid: empty wins over selected; the selected one is the active quick-bar slot.</summary>
        public static SlotLook Look(bool empty, bool active) => empty ? SlotLook.Empty : active ? SlotLook.Selected : SlotLook.Normal;

        public enum SlotLook { Normal, Selected, Empty }

        /// <summary>
        /// B5 header "distance to the next safe zone": world cells from the hero to the zone edge, rounded half up; 0 when
        /// already inside (or on the edge).
        /// </summary>
        public static int DistanceOutside(float x, float y, float centreX, float centreY, float radius)
        {
            var dx = (double)x - centreX;
            var dy = (double)y - centreY;
            var outside = System.Math.Sqrt(dx * dx + dy * dy) - radius;
            return outside <= 0 ? 0 : (int)System.Math.Min(int.MaxValue, System.Math.Floor(outside + 0.5));
        }
    }
}
