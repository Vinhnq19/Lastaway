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
        public Color BloomTint = new Color(1f, .96f, .88f);
        [Min(0f)] public float BloomClamp = 65472f;
        public bool BloomHighQuality = false;

        [Header("Colour")]
        [Tooltip("Exposure in EV added before grading.")]
        public float PostExposure = .08f;
        [Range(-100f, 100f)] public float Contrast = 14f;
        [Range(-100f, 100f)] public float Saturation = 24f;
        public Color ColorFilter = Color.white;
        [Range(-100f, 100f)] public float Temperature = 6f;
        [Range(-100f, 100f)] public float Tint = 0f;

        [Header("Vignette")]
        [Range(0f, 1f)] public float VignetteIntensity = .26f;
        [Range(.01f, 1f)] public float VignetteSmoothness = .42f;
        public Color VignetteColor = new Color(.04f, .06f, .03f);
    }
}
