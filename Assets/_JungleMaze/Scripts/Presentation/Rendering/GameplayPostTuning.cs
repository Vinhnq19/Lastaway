using UnityEngine;

namespace JungleMaze.Presentation.Rendering
{
    /// <summary>
    /// Look of the gameplay post-processing stack (bloom, colour grade, vignette). Values are presentation only and are
    /// applied live while playing, so the asset can be tuned in the Inspector.
    /// </summary>
    [CreateAssetMenu(menuName = "Jungle Maze/Gameplay post tuning")]
    public sealed class GameplayPostTuning : ScriptableObject
    {
        public const string ResourcePath = "GameplayPostTuning";

        [Header("Stack")]
        public bool Enabled = true;
        [Tooltip("Scenes that get the stack (menus and the Level Editor stay untouched).")]
        public string[] Scenes = { "Game" };

        [Header("Bloom")]
        [Tooltip("Linear brightness where glow starts; lower = more of the scene glows.")]
        [Min(0f)] public float BloomThreshold = .6f;
        [Min(0f)] public float BloomIntensity = 1f;
        [Range(0f, 1f)] public float BloomScatter = .68f;
        public Color BloomTint = new Color(1f, .92f, .76f);
        [Tooltip("Caps how bright a pixel can feed the glow, so HDR highlights never blow out the screen.")]
        [Min(0f)] public float BloomClamp = 6f;
        public bool BloomHighQuality = false;

        [Header("Colour")]
        [Tooltip("Exposure in EV added before grading.")]
        public float PostExposure = .08f;
        [Range(-100f, 100f)] public float Contrast = 14f;
        [Range(-100f, 100f)] public float Saturation = 24f;
        public Color ColorFilter = Color.white;
        [Range(-100f, 100f)] public float Temperature = 12f;
        [Range(-100f, 100f)] public float Tint = 0f;

        [Header("Sun grade (warm light, cool shade)")]
        [Tooltip("Multiplier on the bright tones (r, g, b); > 1 warms the sunlit parts.")]
        public Color HighlightTint = new Color(1.08f, 1.03f, .88f);
        [Tooltip("Multiplier on the dark tones; a little blue keeps shade cool against the sun.")]
        public Color ShadowTint = new Color(.94f, .98f, 1.06f);

        [Header("Map lights (Point Light 2D on the map)")]
        public bool MapLightsEnabled = true;
        [Tooltip("Direction of the sun shafts in degrees (0 = vertical; positive leans them towards the top-left).")]
        public float SunPatchAngle = 32f;
        [Tooltip("Distance in cells between neighbouring shaft lines.")]
        public float SunPatchSpacing = 20f;
        [Tooltip("Shaft width range in cells.")]
        public Vector2 SunPatchWidth = new Vector2(5f, 10f);
        [Tooltip("Shaft length range in cells.")]
        public Vector2 SunPatchLength = new Vector2(20f, 38f);
        [Tooltip("Gap range in cells between shaft segments on the same line.")]
        public Vector2 SunPatchGap = new Vector2(6f, 18f);
        [Tooltip("Share of segments left dark so the light falls in patches.")]
        [Range(0f, 1f)] public float SunPatchSkip = .3f;
        public Color SunPatchColor = new Color(1f, .86f, .55f);
        public Vector2 SunPatchIntensity = new Vector2(.4f, .65f);
        public Color LanternColor = new Color(1f, .68f, .32f);
        public float LanternRadius = 2.6f;
        public float LanternIntensity = .9f;
        public Color RuneColor = new Color(.5f, .85f, 1f);
        public float RuneRadius = 2f;
        public float RuneIntensity = .55f;
        public Color PortalColor = new Color(.55f, .75f, 1f);
        public float PortalRadius = 3.2f;
        public float PortalIntensity = .9f;

        [Header("Vignette")]
        [Range(0f, 1f)] public float VignetteIntensity = .26f;
        [Range(.01f, 1f)] public float VignetteSmoothness = .42f;
        public Color VignetteColor = new Color(.04f, .06f, .03f);
    }
}
