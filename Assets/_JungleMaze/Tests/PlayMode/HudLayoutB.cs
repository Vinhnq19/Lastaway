using TMPro;
using UnityEngine;

namespace JungleMaze.Tests.PlayMode
{
    /// <summary>
    /// HUD in the 1280×720 reference canvas, origin top-left. Since T-016 r11 (Huy 07/11/2026) every block and child
    /// follows projects/lastaway/art/inbox/T-015/asset-list.md section 3, measured on the 1920×1080 concept (1080p px =
    /// 1.5 × these units); this replaced the HUD B rects. Icon boxes are the icon PNG canvas (@2x / 2) centred on the
    /// asset-list centre. The class name stays for the tests that use it; v2 constants (killfeed, centre) stay in
    /// <see cref="HudFigmaV2"/>.
    /// </summary>
    internal static class HudLayoutB
    {
        public static readonly Rect Vitals = Px(30, 857, 422, 206);
        public static readonly Rect QuickBar = Px(668, 950, 593, 112);
        public static readonly Rect Minimap = Px(1577, 30, 318, 316);
        public static readonly Rect MatchStatus = Px(1577, 350, 318, 90);
        /// <summary>Smallest screen margin of the four blocks (Vitals bottom, 17 px).</summary>
        public const float MinMargin = 17f / 1.5f;

        // Children, relative to their block's top-left corner.
        public static readonly Rect HpIcon = Px(42, 20, 44, 44);
        /// <summary>Fill area of the HP bar, 6 px inside the 224×32 px stone track at (91, 26).</summary>
        public static readonly Rect HpBar = Px(97, 32, 212, 20);
        public static readonly Rect ArmorIcon = Px(40, 75, 50, 50);
        public static readonly Rect HelmetIcon = Px(222, 77, 46, 46);
        public static readonly Rect AdrenalineIcon = Px(43, 136, 44, 44);
        /// <summary>Fill area, 5 px inside the 242×24 px thin track at (95, 146).</summary>
        public static readonly Rect AdrenalineBar = Px(100, 151, 232, 14);
        public const float SlotSize = 54f / 1.5f;
        public const float SlotStep = 60f / 1.5f;
        public const float SlotRowX = 50f / 1.5f;
        public const float SlotRowY = 29f / 1.5f;
        public const float ConsumableGroupX = 260f / 1.5f;
        public const float SlotRowWidth = 494f / 1.5f;
        /// <summary>Map area = the frame sprite's transparent hole (meta.json "hole").</summary>
        public static readonly Rect MapHole = Px(24, 24, 269, 268);

        /// <summary>Slot row of the quick bar in canvas coordinates (feedback must not cover it).</summary>
        public static Rect SlotRow => new Rect(QuickBar.x + SlotRowX, QuickBar.y + SlotRowY, SlotRowWidth, SlotSize);

        private static Rect Px(float x, float y, float w, float h) => new Rect(x / 1.5f, y / 1.5f, w / 1.5f, h / 1.5f);

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
