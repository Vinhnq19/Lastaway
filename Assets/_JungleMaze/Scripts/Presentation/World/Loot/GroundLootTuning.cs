using System;
using JungleMaze.Presentation.Combat;
using JungleMaze.World.Inventory;
using UnityEngine;

namespace JungleMaze.Presentation.World.Loot
{
    /// <summary>
    /// Every number and colour of the ground loot look (Docs/hvu/07_Floor_Loot.vi.md section 7): item size, hover, rarity
    /// glow and light column, fade-in and the launch arc. Built with defaults and generated sprites by
    /// Jungle Maze → Loot → Build Ground Loot Assets; rebuilding only refreshes the art references, so edits here stay.
    /// Loaded from Resources, so no scene wiring is needed.
    /// </summary>
    [CreateAssetMenu(menuName = "Jungle Maze/Ground loot tuning")]
    public sealed class GroundLootTuning : ScriptableObject
    {
        public const string ResourceName = "GroundLootTuning";

        /// <summary>Light of one rarity.</summary>
        [Serializable]
        public struct RarityStyle
        {
            public ItemRarity Rarity;
            /// <summary>Soft halo on the ground under the item.</summary>
            public Color Glow;
            public float HaloSize;
            /// <summary>Vertical light column; taller for rarer items.</summary>
            public Color Beam;
            public float BeamHeight;
            public float BeamWidth;
            /// <summary>Twinkling motes rising in the column (Epic and Legendary).</summary>
            public int Sparkles;
        }

        [Header("Art (refreshed by the builder)")]
        public HeldWeaponCatalog HeldArt;
        public ThrowableArtCatalog ThrowableArt;
        public Sprite Halo;
        public Sprite Beam;
        public Sprite Sparkle;
        /// <summary>AMMO box art used when the HUD icon catalog has no AMMO icon.</summary>
        public Sprite AmmoBox;
        /// <summary>Unlit sprite material for the glow, column, motes and shadow (light-independent).</summary>
        public Material LightMaterial;

        [Header("Item size (cells) and tilt (degrees)")]
        [Tooltip("Multiplier on HeldWeaponCatalog Length for melee weapons and guns.")]
        public float HeldScale = 1f;
        [Tooltip("Multiplier on ThrowableArtCatalog FlightSize for throwables.")]
        public float ThrowableScale = 1.25f;
        [Tooltip("Flight art longer than this width:height (the ROPE line) reads as a thin stroke; such items use their HUD icon.")]
        public float MaxThrowableAspect = 3f;
        [Tooltip("Longest side of armor, helmet and consumable icons.")]
        public float IconSize = .8f;
        [Tooltip("Longest side of the AMMO box icon.")]
        public float AmmoSize = .55f;
        [Tooltip("Weapon axis (handle to tip) above +X; ~30 reads well top-down.")]
        public float HeldAngle = 30f;
        public float ThrowableAngle = 30f;
        public float IconAngle = 0f;

        [Header("Hover")]
        [Tooltip("Rest height of the item above its ground point.")]
        public float FloatHeight = .22f;
        public float BobAmplitude = .08f;
        public float BobPeriod = 1.6f;
        public Vector2 ShadowSize = new Vector2(.55f, .2f);
        [Range(0f, 1f)] public float ShadowAlpha = .35f;
        [Tooltip("Shadow scale when the item is one LaunchHeight above the ground.")]
        [Range(.1f, 1f)] public float ShadowMinScale = .55f;

        [Header("Entry")]
        [Tooltip("Fade-in of items already on the floor or dropped without a container.")]
        public float FadeSeconds = .35f;
        public float LaunchHeight = .8f;
        public float LaunchSeconds = .45f;
        public float BounceHeight = .1f;
        [Tooltip("Share of LaunchSeconds spent on the landing hop.")]
        [Range(0f, .9f)] public float BounceShare = .22f;
        public float LaunchSpinDegrees = 360f;
        [Tooltip("Delay between items bursting out of the same container.")]
        public float LaunchStagger = .05f;
        [Tooltip("An item new within this many seconds after a container broke flies out of it...")]
        public float LaunchWindow = .5f;
        [Tooltip("...when the container is at most this many cells away.")]
        public float LaunchRadius = 2f;
        [Tooltip("A new item this close to one launched in the same update (AMMO next to its gun) launches too.")]
        public float LaunchCompanionRadius = 1.25f;
        [Tooltip("Glow and column fade in after landing.")]
        public float LightFadeSeconds = .25f;

        [Header("Light")]
        public RarityStyle[] Rarities = DefaultRarities();
        public Color AmmoGlow = new Color(1f, 1f, 1f, .45f);
        public float AmmoHaloSize = .6f;
        [Tooltip("Column brightness pulse (0 = steady).")]
        [Range(0f, 1f)] public float BeamPulse = .2f;
        public float BeamPulsePeriod = 2.2f;
        public float SparkleSize = .14f;
        public float SparklePeriod = 1.3f;

        [Header("Selected by the F pickup list (section 11)")]
        [Tooltip("Halo size multiplier of the item F would pick up.")]
        public float SelectedHaloScale = 1.4f;
        [Tooltip("Glow and column alpha multiplier of the selected item (clamped to 1).")]
        public float SelectedGlowBoost = 1.6f;
        [Tooltip("Item art scale multiplier of the selected item.")]
        public float SelectedItemScale = 1.1f;
        [Tooltip("Halo breathing of the selected item (0 = steady).")]
        [Range(0f, 1f)] public float SelectedPulse = .15f;
        public float SelectedPulsePeriod = .9f;
        [Tooltip("Seconds to blend in and out of the selected look.")]
        public float SelectedBlendSeconds = .1f;

        [Header("Sorting")]
        [Tooltip("Order of shadow and halo in the Ground sorting layer (above floor decor at -80).")]
        public int GroundOrder = 5;

        /// <summary>Style of <paramref name="rarity"/>; the first entry when the table has no row for it.</summary>
        public RarityStyle StyleOf(ItemRarity rarity)
        {
            if (Rarities != null)
            {
                foreach (var style in Rarities) if (style.Rarity == rarity) return style;
                if (Rarities.Length > 0) return Rarities[0];
            }
            return default;
        }

        /// <summary>Common white, Uncommon green, Rare blue, Epic purple, Legendary gold-orange; columns grow with rarity.</summary>
        public static RarityStyle[] DefaultRarities() => new[]
        {
            Style(ItemRarity.Common, new Color(1f, 1f, 1f), .9f, .55f, .45f, 0),
            Style(ItemRarity.Uncommon, new Color(.35f, 1f, .35f), 1f, .9f, .5f, 0),
            Style(ItemRarity.Rare, new Color(.3f, .62f, 1f), 1.05f, 1.3f, .55f, 0),
            Style(ItemRarity.Epic, new Color(.78f, .38f, 1f), 1.15f, 1.75f, .6f, 3),
            Style(ItemRarity.Legendary, new Color(1f, .68f, .12f), 1.25f, 2.25f, .66f, 5)
        };

        private static RarityStyle Style(ItemRarity rarity, Color color, float halo, float height, float width, int sparkles) => new RarityStyle
        {
            Rarity = rarity, Glow = new Color(color.r, color.g, color.b, .65f), HaloSize = halo,
            Beam = new Color(color.r, color.g, color.b, .5f), BeamHeight = height, BeamWidth = width, Sparkles = sparkles
        };
    }
}
