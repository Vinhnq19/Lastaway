using UnityEngine;

namespace JungleMaze.Tests.PlayMode
{
    /// <summary>Approved Figma 48:239 geometry in the 1280 by 720 reference canvas.</summary>
    public static class HudFigmaV2Feedback
    {
        public const float Tolerance=2;
        // T-013 layout B: the effects row sits 8 u above the smaller Vitals (y 147 from the bottom), the use strip
        // takes the weapon strip's place inside the 395×84 quick bar.
        public static readonly Rect Effects=new Rect(24,515,294,58);
        public static readonly Rect Pickup=new Rect(1115,564,141,132);
        public static readonly Rect Banner=new Rect(490,24,301,40);
        public static readonly Rect Use=new Rect(452.5f,620,375,22);
        public static readonly Rect Center=new Rect(320,180,640,360);
    }
}
