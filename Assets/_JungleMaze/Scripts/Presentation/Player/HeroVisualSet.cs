using System;
using JungleMaze.Presentation.World.Phase2;
using UnityEngine;

namespace JungleMaze.Presentation.Player
{
    /// <summary>
    /// Sprite-only hero looks extracted from the JSurviv hero clips (no Animator, no legacy scripts).
    /// Each hero has idle and move loops for three facings; "side" frames face right and are mirrored for left.
    /// </summary>
    [CreateAssetMenu(menuName = "Jungle Maze/Hero visual set")]
    public sealed class HeroVisualSet : ScriptableObject
    {
        /// <summary>One hero's frames. Frame sprites share one size so the body never jumps between states.</summary>
        [Serializable]
        public sealed class Hero
        {
            public string Name = "";
            public WorldVisualSet.Frames IdleBack, IdleSide, IdleFront, MoveBack, MoveSide, MoveFront;
            /// <summary>One-shot attack swings per facing; null keeps the idle/move loop during an attack.</summary>
            public WorldVisualSet.Frames AttackBack, AttackSide, AttackFront;
            /// <summary>This hero's fist, drawn at the same pixel scale as the body; null draws no hands.</summary>
            public Sprite Hand;

            /// <summary>Attack clip for a facing, falling back to the side clip.</summary>
            public WorldVisualSet.Frames GetAttack(HeroFacing facing)
            {
                var clip = facing == HeroFacing.Back ? AttackBack : facing == HeroFacing.Front ? AttackFront : AttackSide;
                return Has(clip) ? clip : Has(AttackSide) ? AttackSide : null;
            }

            /// <summary>Unity deserializes a missing clip as an empty instance, so emptiness counts as missing.</summary>
            public static bool Has(WorldVisualSet.Frames clip) => clip != null && clip.Sprites != null && clip.Sprites.Length > 0;

            /// <summary>Loop for a state and facing; null when the source clip was missing.</summary>
            public WorldVisualSet.Frames Get(bool moving, HeroFacing facing)
            {
                WorldVisualSet.Frames clip;
                switch (facing)
                {
                    case HeroFacing.Back: clip = moving ? MoveBack : IdleBack; break;
                    case HeroFacing.Front: clip = moving ? MoveFront : IdleFront; break;
                    default: clip = moving ? MoveSide : IdleSide; break;
                }
                return Has(clip) ? clip : Has(IdleFront) ? IdleFront : null;
            }
        }

        /// <summary>World height of one source frame. JSurviv frames are 48 px with the character about 16 px tall.</summary>
        public float FrameWorldHeight = 3f;
        public Hero[] Heroes = Array.Empty<Hero>();

        /// <summary>Hero for an index, wrapping so every NetId maps to a valid look.</summary>
        public Hero At(int index) => Heroes == null || Heroes.Length == 0 ? null : Heroes[((index % Heroes.Length) + Heroes.Length) % Heroes.Length];
    }

    /// <summary>Which way the body is drawn. Back = looking up the screen, Front = looking down.</summary>
    public enum HeroFacing { Side, Back, Front }
}
