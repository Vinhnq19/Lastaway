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
        [Min(0f)] public float BloomThreshold = .78f;
        [Min(0f)] public float BloomIntensity = 1.1f;
        [Range(0f, 1f)] public float BloomScatter = .72f;
        public Color BloomTint = new Color(1f, .92f, .76f);
        [Min(0f)] public float BloomClamp = 65472f;
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

        [Header("Sunlight overlay")]
        public bool SunEnabled = true;
        [Tooltip("Colour of the shafts, glow and dust (warm sunlight).")]
        public Color SunColor = new Color(1f, .87f, .55f);
        [Tooltip("HDR brightness of the overlay so it blooms.")]
        [Min(0f)] public float SunHdrBoost = 1.35f;
        [Tooltip("Shaft tilt in degrees; positive leans the light from the top-left.")]
        public float SunAngle = 32f;
        [Range(0, 8)] public int SunRays = 5;
        [Range(0f, 1f)] public float SunRayAlpha = .085f;
        [Tooltip("Shaft width range in cells.")]
        public Vector2 SunRayWidth = new Vector2(1.2f, 3.4f);
        [Tooltip("Sideways sway of each shaft in cells, and its speed.")]
        public float SunSway = .6f;
        public float SunSwaySpeed = .22f;
        [Tooltip("Where the sun glow sits on screen (-1..1 from the centre; top-left = (-1, 1)).")]
        public Vector2 SunCorner = new Vector2(-.95f, 1f);
        [Tooltip("Sun glow diameter as a fraction of the screen height.")]
        public float SunGlowSize = 1.5f;
        [Range(0f, 1f)] public float SunGlowAlpha = .2f;
        [Range(0, 96)] public int DustCount = 40;
        [Tooltip("Dust mote size in cells.")]
        public float DustSize = .12f;
        [Range(0f, 1f)] public float DustAlpha = .55f;
        [Tooltip("Dust drift in cells per second.")]
        public float DustSpeed = .35f;

        [Header("Vignette")]
        [Range(0f, 1f)] public float VignetteIntensity = .26f;
        [Range(.01f, 1f)] public float VignetteSmoothness = .42f;
        public Color VignetteColor = new Color(.04f, .06f, .03f);
    }
}
