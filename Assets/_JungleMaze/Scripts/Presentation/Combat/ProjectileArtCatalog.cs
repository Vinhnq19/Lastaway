using System;
using JungleMaze.Presentation.World.Phase2;
using UnityEngine;

namespace JungleMaze.Presentation.Combat
{
    /// <summary>
    /// Item ID → shot art for ranged weapons (JSurviv projectiles). A weapon may have flight frames (the projectile
    /// sprite), a muzzle flash played once per shot at the barrel, or neither (the default tracer, tinted).
    /// Sizes are normalized from the drawn content, so any source PPU gives the configured world length.
    /// </summary>
    [CreateAssetMenu(menuName = "Jungle Maze/Projectile art catalog")]
    public sealed class ProjectileArtCatalog : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public string ItemId;
            /// <summary>Projectile frames; empty keeps the default tracer.</summary>
            public WorldVisualSet.Frames Flight;
            /// <summary>Degrees that turn the drawn tail→tip axis onto the flight direction.</summary>
            public float AngleOffset;
            /// <summary>World length of the flight sprite's longest drawn side.</summary>
            public float Length;
            /// <summary>Visual spin in degrees per second (tumbling pebbles); 0 keeps the sprite aligned.</summary>
            public float Spin;
            /// <summary>Longest drawn side of the first flight frame, in sprite units.</summary>
            public float FlightContent;
            /// <summary>Tint for the default tracer when there are no flight frames.</summary>
            public Color TracerColor;
            /// <summary>One-shot frames at the barrel (shotgun blast); empty for none.</summary>
            public WorldVisualSet.Frames Muzzle;
            public float MuzzleLength;
            public float MuzzleContent;

            public bool HasFlight => Flight != null && Flight.Sprites != null && Flight.Sprites.Length > 0;
            public bool HasMuzzle => Muzzle != null && Muzzle.Sprites != null && Muzzle.Sprites.Length > 0;
            public float FlightScale => FlightContent > 0f ? Length / FlightContent : 1f;
            public float MuzzleScale => MuzzleContent > 0f ? MuzzleLength / MuzzleContent : 1f;
        }

        public Entry[] Entries = Array.Empty<Entry>();

        public bool TryGet(string itemId, out Entry entry)
        {
            if (!string.IsNullOrEmpty(itemId) && Entries != null)
                foreach (var candidate in Entries)
                    if (candidate.ItemId == itemId) { entry = candidate; return true; }
            entry = default;
            return false;
        }
    }
}
