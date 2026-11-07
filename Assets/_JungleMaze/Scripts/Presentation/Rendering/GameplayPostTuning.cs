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

        [Header("Map lights (Point Light 2D on the map)")]
        public bool MapLightsEnabled = true;
        [Tooltip("Grid step in cells between sun pool candidates (one light at most per step x step block).")]
        [Min(3)] public int SunPoolSpacing = 10;
        [Tooltip("Share of open cells (no tree, wall or prop) needed around a sun pool; sunlight lands on clearings.")]
        [Range(0f, 1f)] public float SunPoolOpen = .6f;
        public Color SunPoolColor = new Color(1f, .86f, .55f);
        [Tooltip("Outer radius range in cells.")]
        public Vector2 SunPoolRadius = new Vector2(3.5f, 6.5f);
        public Vector2 SunPoolIntensity = new Vector2(.35f, .6f);
        [Range(0f, 1f)] public float SunPoolFalloff = .7f;
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
