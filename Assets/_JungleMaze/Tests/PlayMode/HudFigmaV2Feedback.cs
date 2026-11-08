using UnityEngine;

namespace JungleMaze.Tests.PlayMode
{
    /// <summary>Approved Figma 48:239 geometry in the 1280 by 720 reference canvas.</summary>
    public static class HudFigmaV2Feedback
    {
        public const float Tolerance=2;
        // T-022 skin A (mock B9, 1080p px / 1.5): chips 3 per line from (32, 568), pickup toasts 308×336 (3 × kit_toast
        // 308×92, step 98, + overflow tag) ending 20 px from the right and 46 px from the bottom (R3), banner 620×60 at
        // y 36 centred, use strip 860×64 at (530, 850).
        public static readonly Rect Effects=new Rect(32/1.5f,568/1.5f,364/1.5f,240/1.5f);
        public static readonly Rect Pickup=new Rect((1920-20-308)/1.5f,(1080-46-336)/1.5f,308/1.5f,336/1.5f);
        public static readonly Rect Banner=new Rect((960-310)/1.5f,36/1.5f,620/1.5f,60/1.5f);
        public static readonly Rect Use=new Rect(530/1.5f,850/1.5f,860/1.5f,64/1.5f);
        public static readonly Rect Center=new Rect(320,180,640,360);
    }
}
