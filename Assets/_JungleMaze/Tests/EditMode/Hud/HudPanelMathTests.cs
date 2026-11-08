using JungleMaze.Presentation.UI.Hud;
using NUnit.Framework;

namespace JungleMaze.Tests.EditMode.Hud
{
    /// <summary>T-022 B4 bag: tooltip side and slot look rules (pure, no prefab).</summary>
    public sealed class HudPanelMathTests
    {
        [Test]
        public void Tooltip_OpensRight_WhenItFits_FlipsLeftOnlyPastTheEdge()
        {
            // Cell ends at 900, gap 5, tooltip 260 wide: 1165 fits a 1200 panel, exactly 1165 still fits, 1164 does not.
            Assert.False(HudPanelMath.TooltipFlipsLeft(900, 5, 260, 1200));
            Assert.False(HudPanelMath.TooltipFlipsLeft(900, 5, 260, 1165), "touching the edge is still inside");
            Assert.True(HudPanelMath.TooltipFlipsLeft(900, 5, 260, 1164.99f));
            Assert.True(HudPanelMath.TooltipFlipsLeft(1200, 0, 0.01f, 1200), "a cell on the edge always flips");
        }

        [Test]
        public void SlotLook_EmptyWinsOverActive()
        {
            Assert.AreEqual(HudPanelMath.SlotLook.Empty, HudPanelMath.Look(true, true));
            Assert.AreEqual(HudPanelMath.SlotLook.Empty, HudPanelMath.Look(true, false));
            Assert.AreEqual(HudPanelMath.SlotLook.Selected, HudPanelMath.Look(false, true));
            Assert.AreEqual(HudPanelMath.SlotLook.Normal, HudPanelMath.Look(false, false));
        }

        [Test]
        public void DistanceOutside_ZeroInsideAndOnTheEdge_RoundedOutside()
        {
            Assert.AreEqual(0, HudPanelMath.DistanceOutside(10, 10, 10, 10, 5), "centre");
            Assert.AreEqual(0, HudPanelMath.DistanceOutside(15, 10, 10, 10, 5), "on the edge");
            Assert.AreEqual(3, HudPanelMath.DistanceOutside(18, 10, 10, 10, 5));
            Assert.AreEqual(4, HudPanelMath.DistanceOutside(18.5f, 10, 10, 10, 5), "3.5 rounds half up");
            Assert.AreEqual(5, HudPanelMath.DistanceOutside(10, 0, 10, 10, 5), "any direction");
        }
    }
}
