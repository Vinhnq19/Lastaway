using System;
using JungleMaze.Domain.CombatFeedback;
using JungleMaze.Domain.Vfx;
using UnityEngine;

namespace JungleMaze.Presentation.Vfx
{
    /// <summary>
    /// VFX-01 catalog: cue → pooled prefab with its rule (priority, lifetime, persistent), the sorting layer it is
    /// allowed to use (T-2: body feedback on Default under the canopy, warnings on VFX), where it sits on the body,
    /// status → cue (a status mapped to None is a deliberate "no visual", e.g. Slow, Root, Adrenaline) and the tint
    /// palette. Blasts and smoke are not here: <c>ThrowableView</c> draws them (plan 4.12, D2).
    /// </summary>
    [CreateAssetMenu(menuName = "Jungle Maze/VFX/Catalog", fileName = "VfxCatalog")]
    public sealed class VfxCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class CueEntry
        {
            public VfxCue cue;
            public VfxParticleEffect prefab;
            [Min(0)] public int prewarm = 4;
            public VfxPriority priority = VfxPriority.Low;
            [Tooltip("Seconds before the director releases it; 0 = decided by the event.")]
            [Min(0f)] public float lifetimeSeconds;
            [Tooltip("Logical state that is drawn again after a visibility change (auras, fuse warning).")]
            public bool persistent;
            [Tooltip("Sorting layer every renderer of the prefab must use (checked by the audit test).")]
            public string sortingLayer = "Default";
            [Tooltip("Offset from the body anchor, in cells (above the head, at the feet).")]
            public Vector2 anchorOffset;
        }

        [Serializable]
        public sealed class StatusEntry
        {
            public StatusKind kind;
            [Tooltip("None = known status without a visual.")]
            public VfxCue cue;
            [Tooltip("Palette index; 0 = prefab colours.")]
            public byte tint;
        }

        [SerializeField] private CueEntry[] cues = Array.Empty<CueEntry>();
        [SerializeField] private StatusEntry[] statuses = Array.Empty<StatusEntry>();
        [Tooltip("Index 1..3 = damage number dealt / taken / other (VfxTints); 4+ = aura tints.")]
        [SerializeField] private Color[] palette = Array.Empty<Color>();

        public CueEntry[] Cues => cues;
        public StatusEntry[] Statuses => statuses;
        public Color[] Palette => palette;

        public bool TryGetEntry(VfxCue cue, out CueEntry entry)
        {
            foreach (var e in cues)
                if (e != null && e.cue == cue) { entry = e; return true; }
            entry = null;
            return false;
        }

        /// <summary>Palette colour for a tint index; false for 0 or an index outside the palette.</summary>
        public bool TryGetTint(byte index, out Color color)
        {
            if (index > 0 && index < palette.Length) { color = palette[index]; return true; }
            color = Color.white;
            return false;
        }

        /// <summary>Rules for the director; cues without a prefab are left out (the director skips them).</summary>
        public VfxRuleTable BuildRules()
        {
            var table = new VfxRuleTable();
            foreach (var e in cues)
                if (e != null && e.cue != VfxCue.None && e.prefab != null)
                    table.Set(e.cue, new VfxRule(e.priority, e.lifetimeSeconds, e.persistent));
            foreach (var s in statuses)
                if (s != null && s.kind != StatusKind.None)
                    table.MapStatus(s.kind, s.cue, s.tint);
            return table;
        }

        /// <summary>Editor/tests: replaces the content.</summary>
        public void Set(CueEntry[] cueEntries, StatusEntry[] statusEntries, Color[] colors)
        {
            cues = cueEntries ?? Array.Empty<CueEntry>();
            statuses = statusEntries ?? Array.Empty<StatusEntry>();
            palette = colors ?? Array.Empty<Color>();
        }
    }
}
