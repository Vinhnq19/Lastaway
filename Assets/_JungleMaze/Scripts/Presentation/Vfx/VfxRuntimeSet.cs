using System;
using JungleMaze.Domain.Vfx;
using UnityEngine;

namespace JungleMaze.Presentation.Vfx
{
    /// <summary>
    /// Everything VFX-01 loads at composition (Resources/<see cref="ResourcePath"/>): the catalog and the presentation
    /// numbers (not balance data; agreed at G1, spec 2.5 and T-8).
    /// </summary>
    [CreateAssetMenu(menuName = "Jungle Maze/VFX/Runtime Set", fileName = "VfxRuntimeSet")]
    public sealed class VfxRuntimeSet : ScriptableObject
    {
        public const string ResourcePath = "VfxRuntimeSet";

        [Serializable]
        public sealed class Tuning
        {
            [Min(0f)] public float flashSeconds = .16f;
            [Min(0f)] public float numberSeconds = .7f;
            [Min(0f)] public float numberRiseCells = .6f;
            [Min(0f)] public float shakeMaxCells = .15f;
            [Min(0f)] public float shakeSeconds = .3f;
            [Min(0f)] public float shakeRadiusCells = 6f;
            [Min(0f)] public float hitShakeCells = .08f;
            [Min(0f)] public float dedupeSeconds = 5f;
            [Min(1)] public int dedupeCapacity = 256;
            [Min(1)] public int budget = 64;

            public VfxTuningValues ToValues() => new VfxTuningValues(flashSeconds, numberSeconds, numberRiseCells, shakeMaxCells,
                shakeSeconds, shakeRadiusCells, dedupeSeconds, dedupeCapacity, budget, hitShakeCells);
        }

        [SerializeField] private VfxCatalog catalog;
        [SerializeField] private Tuning tuning = new Tuning();

        public VfxCatalog Catalog => catalog;
        public VfxTuningValues TuningValues => (tuning ?? new Tuning()).ToValues();
        public bool IsComplete => catalog != null;

        /// <summary>Editor/tests: replaces the content.</summary>
        public void Set(VfxCatalog vfxCatalog, Tuning values = null)
        {
            catalog = vfxCatalog;
            tuning = values ?? new Tuning();
        }
    }
}
