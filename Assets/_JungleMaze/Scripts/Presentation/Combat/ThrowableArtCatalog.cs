using System;
using JungleMaze.Presentation.World.Phase2;
using UnityEngine;

namespace JungleMaze.Presentation.Combat
{
    /// <summary>
    /// JSurviv art for the WPN-03 weapons, built by Jungle Maze → Art → Build Throwable Art. Every clip stores the
    /// longest drawn side of its frames so presentation can size it in world cells without knowing the source PPU.
    /// Loaded from Resources, so no scene wiring is needed.
    /// </summary>
    [CreateAssetMenu(menuName = "Jungle Maze/Throwable art catalog")]
    public sealed class ThrowableArtCatalog : ScriptableObject
    {
        public const string ResourceName = "ThrowableArtCatalog";

        [Serializable]
        public struct Clip
        {
            public WorldVisualSet.Frames Frames;
            /// <summary>Longest drawn side over all frames, in sprite units.</summary>
            public float Content;

            public bool IsSet => Frames != null && Frames.Sprites != null && Frames.Sprites.Length > 0;
            /// <summary>Uniform scale that draws the clip <paramref name="worldSize"/> cells long.</summary>
            public float ScaleFor(float worldSize) => Content > 0f ? worldSize / Content : 1f;
        }

        [Serializable]
        public struct Entry
        {
            public string ItemId;
            /// <summary>The item while it flies and waits for its fuse.</summary>
            public Clip Flight;
            public float FlightSize;
            /// <summary>Blast drawn over the area; empty for smoke and special weapons.</summary>
            public Clip Blast;
            /// <summary>What stays behind: banana peel, smoke cloud, rope, seed vines on a rooted target.</summary>
            public Clip Linger;
            public float LingerSize;
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
