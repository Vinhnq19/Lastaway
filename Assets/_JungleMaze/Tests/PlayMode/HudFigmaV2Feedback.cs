using UnityEngine;

namespace JungleMaze.Tests.PlayMode
{
    /// <summary>Approved Figma 48:239 geometry in the 1280 by 720 reference canvas.</summary>
    public static class HudFigmaV2Feedback
    {
        public const float Tolerance=2;
        // T-016 r11: the effects row sits 8 u above the Vitals (top 148.7 u from the bottom, row at y 157).
        public static readonly Rect Effects=new Rect(24,505,294,58);
        public static readonly Rect Pickup=new Rect(1115,564,141,132);
        public static readonly Rect Banner=new Rect(490,24,301,40);
        // T-016 r11: no strip row inside the quick bar any more; the use strip shows 28 px above it (1080p px).
        public static readonly Rect Use=new Rect((668+40)/1.5f,(950-28)/1.5f,512f/1.5f,22f/1.5f);
        public static readonly Rect Center=new Rect(320,180,640,360);
    }
}
