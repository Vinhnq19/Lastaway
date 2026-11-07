using JungleMaze.Domain.CombatFeedback;

namespace JungleMaze.Domain.Vfx
{
    /// <summary>Every visual the VFX layer can spawn. Each cue maps to one pooled prefab in the catalog.</summary>
    /// <remarks>
    /// Blasts, smoke, traps, vines and the bell ring are drawn by <c>ThrowableView</c> (WPN-03), so they have no cue
    /// here (VFX-01 plan 4.12, D2). Values are explicit so assets keep their mapping when cues are added.
    /// </remarks>
    public enum VfxCue : byte
    {
        None = 0,
        HitSpark = 1,
        DamageNumber = 2,
        AttackPuff = 3,
        FuseWarning = 4,
        AuraStun = 12,
        AuraReverse = 14,
        AuraCcImmune = 15,
        AuraInvisible = 16,
        AuraInvulnerable = 17,
        AuraPoison = 18,
        /// <summary>P2, no prefab by default (Q9).</summary>
        AuraAdrenaline = 19
    }

    /// <summary>Budget priority: when the cap is reached an older, strictly lower priority instance makes room.</summary>
    public enum VfxPriority : byte { Low, Medium, High, Critical }

    /// <summary>Fixed tint indices the director chooses itself; catalog palettes add their own from 4 upwards.</summary>
    public static class VfxTints
    {
        public const byte None = 0;
        public const byte NumberDealt = 1;
        public const byte NumberTaken = 2;
        public const byte NumberOther = 3;
    }

    /// <summary>How one cue behaves.</summary>
    public readonly struct VfxRule
    {
        public readonly VfxPriority Priority;
        /// <summary>Seconds before the director releases it; 0 = decided by the event (status, fuse).</summary>
        public readonly float LifetimeSeconds;
        /// <summary>Kept as logical state (status/fuse) so it can be spawned again after a visibility change.</summary>
        public readonly bool Persistent;

        public VfxRule(VfxPriority priority, float lifetimeSeconds, bool persistent)
        {
            Priority = priority;
            LifetimeSeconds = lifetimeSeconds;
            Persistent = persistent;
        }
    }

    /// <summary>
    /// Cue rules plus the status → cue map, built once from the catalog asset.
    /// A status mapped to <see cref="VfxCue.None"/> is a deliberate "no visual" (Slow, Adrenaline).
    /// </summary>
    public sealed class VfxRuleTable
    {
        private const int CueCount = 256;
        private readonly VfxRule[] _rules = new VfxRule[CueCount];
        private readonly bool[] _hasRule = new bool[CueCount];
        private readonly VfxCue[] _statusCue = new VfxCue[256];
        private readonly byte[] _statusTint = new byte[256];
        private readonly bool[] _hasStatus = new bool[256];
        public void Set(VfxCue cue, in VfxRule rule)
        {
            _rules[(int)cue] = rule;
            _hasRule[(int)cue] = cue != VfxCue.None;
        }

        public bool TryGet(VfxCue cue, out VfxRule rule)
        {
            rule = _rules[(int)cue];
            return _hasRule[(int)cue];
        }

        /// <summary>Maps a status to a cue; <see cref="VfxCue.None"/> means "known, no visual".</summary>
        public void MapStatus(StatusKind kind, VfxCue cue, byte tint = 0)
        {
            _statusCue[(byte)kind] = cue;
            _statusTint[(byte)kind] = tint;
            _hasStatus[(byte)kind] = kind != StatusKind.None;
        }

        /// <summary>False for an unmapped (unknown) status; true with cue None for a deliberate "no visual".</summary>
        public bool TryGetStatus(StatusKind kind, out VfxCue cue, out byte tint)
        {
            cue = _statusCue[(byte)kind];
            tint = _statusTint[(byte)kind];
            return _hasStatus[(byte)kind];
        }
    }

    /// <summary>Presentation numbers of the VFX layer (not balance data): read from the runtime set asset.</summary>
    public readonly struct VfxTuningValues
    {
        public readonly float FlashSeconds;
        public readonly float NumberSeconds;
        public readonly float NumberRiseCells;
        public readonly float ShakeMaxCells;
        public readonly float ShakeSeconds;
        public readonly float ShakeRadiusCells;
        public readonly float DedupeSeconds;
        public readonly int DedupeCapacity;
        public readonly int Budget;
        /// <summary>Shake added when the local player is hit; explosions use <see cref="ShakeMaxCells"/>.</summary>
        public readonly float HitShakeCells;
        /// <summary>
        /// Extra seconds a fuse warning stays after its timer runs out while waiting for <c>Detonated</c> (a late or
        /// lagging blast); <c>Detonated</c> still removes it at once.
        /// </summary>
        public readonly float FuseGraceSeconds;

        public VfxTuningValues(float flashSeconds, float numberSeconds, float numberRiseCells, float shakeMaxCells,
            float shakeSeconds, float shakeRadiusCells, float dedupeSeconds, int dedupeCapacity, int budget,
            float hitShakeCells, float fuseGraceSeconds = DefaultFuseGraceSeconds)
        {
            FlashSeconds = flashSeconds;
            NumberSeconds = numberSeconds;
            NumberRiseCells = numberRiseCells;
            ShakeMaxCells = shakeMaxCells;
            ShakeSeconds = shakeSeconds;
            ShakeRadiusCells = shakeRadiusCells;
            DedupeSeconds = dedupeSeconds;
            DedupeCapacity = dedupeCapacity;
            Budget = budget;
            HitShakeCells = hitShakeCells;
            FuseGraceSeconds = fuseGraceSeconds > 0f ? fuseGraceSeconds : 0f;
        }

        /// <summary>Grace after a fuse runs out (review round 1, suggestion 5).</summary>
        public const float DefaultFuseGraceSeconds = .15f;

        /// <summary>Values agreed at G1 (spec 2.5, T-8).</summary>
        public static VfxTuningValues Default =>
            new VfxTuningValues(.16f, .7f, .6f, .15f, .3f, 6f, 5f, 256, 64, .08f, DefaultFuseGraceSeconds);
    }
}
