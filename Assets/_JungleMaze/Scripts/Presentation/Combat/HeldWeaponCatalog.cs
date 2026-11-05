using System;
using UnityEngine;

namespace JungleMaze.Presentation.Combat
{
    /// <summary>
    /// Item ID → in-hand weapon art. Each entry normalizes a JSurviv sprite whatever its PPU or drawing angle:
    /// <see cref="Entry.AngleOffset"/> turns the handle→tip axis onto +X, <see cref="Entry.Grip"/> is the held point
    /// and <see cref="Entry.Length"/> is the world size of the sprite's longest side.
    /// </summary>
    [CreateAssetMenu(menuName = "Jungle Maze/Held weapon catalog")]
    public sealed class HeldWeaponCatalog : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public string ItemId;
            public Sprite Sprite;
            /// <summary>Degrees added to the aim so the drawn weapon points along it.</summary>
            public float AngleOffset;
            /// <summary>Held point in the drawn content box, 0..1 from its bottom-left corner.</summary>
            public Vector2 Grip;
            /// <summary>World length of the content's longest side.</summary>
            public float Length;
            /// <summary>Opaque content box in sprite units (relative to the pivot); empty means the whole rect.</summary>
            public Vector2 ContentMin, ContentSize;

            private Rect Content
            {
                get
                {
                    if (ContentSize.x > 0f && ContentSize.y > 0f) return new Rect(ContentMin, ContentSize);
                    var b = Sprite != null ? Sprite.bounds : default;
                    return new Rect(b.min, b.size);
                }
            }

            /// <summary>Uniform scale that gives the content its configured length.</summary>
            public float Scale
            {
                get
                {
                    var size = Content.size;
                    var longest = Mathf.Max(size.x, size.y);
                    return longest > 0f ? Length / longest : 1f;
                }
            }

            /// <summary>Grip point relative to the sprite pivot, in unscaled sprite units.</summary>
            public Vector2 GripLocal
            {
                get
                {
                    var content = Content;
                    return content.min + Vector2.Scale(Grip, content.size);
                }
            }
        }

        public Entry[] Entries = Array.Empty<Entry>();

        public bool TryGet(string itemId, out Entry entry)
        {
            if (!string.IsNullOrEmpty(itemId) && Entries != null)
                foreach (var candidate in Entries)
                    if (candidate.Sprite != null && candidate.ItemId == itemId) { entry = candidate; return true; }
            entry = default;
            return false;
        }
    }
}
