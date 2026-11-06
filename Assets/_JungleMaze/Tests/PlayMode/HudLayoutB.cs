using TMPro;
using UnityEngine;

namespace JungleMaze.Tests.PlayMode
{
    /// <summary>
    /// HUD layout B (T-013; projects/lastaway/ux/hud-B-spec.md, chosen by Huy 06/11/2026) in the 1280×720 reference
    /// canvas, origin top-left. Replaces the Figma v2 block and child numbers; v2 constants that B keeps (margins,
    /// killfeed, centre) stay in <see cref="HudFigmaV2"/>.
    /// </summary>
    internal static class HudLayoutB
    {
        public static readonly Rect Vitals = new Rect(24, 581, 207, 115);
        public static readonly Rect QuickBar = new Rect(442.5f, 612, 395, 84);
        public static readonly Rect Minimap = new Rect(1096, 24, 160, 160);
        public static readonly Rect MatchStatus = new Rect(1096, 192, 160, 59);

        // Children, relative to their block's top-left corner.
        public static readonly Rect HpIcon = new Rect(10, 10, 26, 26);
        public static readonly Rect HpBar = new Rect(42, 16, 109, 14);
        public static readonly Rect ArmorIcon = new Rect(10, 46, 26, 26);
        public static readonly Rect HelmetIcon = new Rect(104, 46, 26, 26);
        public static readonly Rect AdrenalineIcon = new Rect(12, 80, 24, 24);
        public static readonly Rect AdrenalineBar = new Rect(42, 88, 109, 8);
        public static readonly Rect WeaponStrip = new Rect(10, 8, 375, 22);
        public const float SlotSize = 42f;
        public const float SlotStep = 46f;
        public const float SlotRowX = 10f;
        public const float SlotRowY = 34f;
        public const float ConsumableGroupX = 195f;
        public const float MinimapSize = 160f;

        /// <summary>Slot row of the quick bar in canvas coordinates (feedback must not cover it).</summary>
        public static Rect SlotRow => new Rect(QuickBar.x + SlotRowX, QuickBar.y + SlotRowY, 375, SlotSize);

        /// <summary>
        /// UI-01 asks for 13 u text. Huy approved two exceptions for B (DECISIONS 2026-11-06): 12 u for the adrenaline
        /// number, reserve ammo and weapon name; 10 u for the slot key numbers. Everything else stays at 13 u or more.
        /// </summary>
        public static float MinFontSize(TMP_Text text)
        {
            var parent = text.transform.parent != null ? text.transform.parent.name : "";
            if (text.name == "Key" && parent.StartsWith("Slot")) return 10f;
            if (text.name == "Key" && parent == "HudSlot") return 10f;
            if (text.name == "Reserve") return 12f;
            if (text.name == "Name" && parent == "WeaponStrip") return 12f;
            if (text.name == "Value" && parent == "AdrenalineRow") return 12f;
            return 13f;
        }
    }
}
