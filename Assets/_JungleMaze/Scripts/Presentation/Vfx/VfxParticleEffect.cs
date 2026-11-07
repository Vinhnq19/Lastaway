using JungleMaze.App.Vfx;
using JungleMaze.Core.Pooling;
using UnityEngine;

namespace JungleMaze.Presentation.Vfx
{
    /// <summary>Extra behaviour on a VFX prefab (number text, fuse frame) told about each spawn by <see cref="VfxParticleEffect"/>.</summary>
    public interface IVfxSpawnListener
    {
        /// <summary>Called right after the instance was taken from the pool and placed.</summary>
        void OnVfxBegin(in VfxSpawn spawn, Color tint, bool hasTint, VfxStage stage);
        /// <summary>Called when the instance goes back to the pool.</summary>
        void OnVfxEnd();
    }

    /// <summary>
    /// Root of every VFX-01 prefab (pooled through CLI-03). Caches its particle systems, restarts them on each spawn,
    /// stops and clears them on despawn, scales area effects from <see cref="NominalCells"/> and tints through
    /// <c>MainModule.startColor</c> (no material instance). It never destroys itself: the director decides lifetime.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class VfxParticleEffect : MonoBehaviour, IPoolable
    {
        [Tooltip("Side of the area the prefab covers at scale 1, in cells; 0 = never scaled by area.")]
        [SerializeField] private float nominalCells;

        private ParticleSystem[] _systems;
        private ParticleSystem.MinMaxGradient[] _startColors;
        private IVfxSpawnListener[] _listeners;
        private bool _tinted;

        public float NominalCells => nominalCells;
        public ParticleSystem[] Systems { get { Cache(); return _systems; } }

        /// <summary>Places, scales, tints and plays this instance (called by <see cref="VfxStage"/> after Spawn).</summary>
        public void Begin(in VfxSpawn spawn, Color tint, bool hasTint, VfxStage stage)
        {
            Cache();
            if (nominalCells > 0f && spawn.AreaWidth > 0f && spawn.AreaHeight > 0f)
                transform.localScale = new Vector3(spawn.AreaWidth / nominalCells, spawn.AreaHeight / nominalCells, 1f);
            else transform.localScale = Vector3.one;
            for (var i = 0; i < _systems.Length; i++)
            {
                var main = _systems[i].main;
                if (hasTint) main.startColor = Multiply(_startColors[i], tint);
                else if (_tinted) main.startColor = _startColors[i];
            }
            _tinted = hasTint;
            for (var i = 0; i < _systems.Length; i++)
            {
                _systems[i].Clear(true);
                _systems[i].Play(false);
            }
            for (var i = 0; i < _listeners.Length; i++) _listeners[i].OnVfxBegin(spawn, tint, hasTint, stage);
        }

        public void OnSpawned() => Cache();

        public void OnDespawned()
        {
            Cache();
            for (var i = 0; i < _systems.Length; i++)
            {
                _systems[i].Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                if (_tinted)
                {
                    var main = _systems[i].main;
                    main.startColor = _startColors[i];
                }
            }
            _tinted = false;
            transform.localScale = Vector3.one;
            for (var i = 0; i < _listeners.Length; i++) _listeners[i].OnVfxEnd();
        }

        private void Cache()
        {
            if (_systems != null) return;
            _systems = GetComponentsInChildren<ParticleSystem>(true);
            _startColors = new ParticleSystem.MinMaxGradient[_systems.Length];
            for (var i = 0; i < _systems.Length; i++) _startColors[i] = _systems[i].main.startColor;
            _listeners = GetComponentsInChildren<IVfxSpawnListener>(true);
        }

        private static ParticleSystem.MinMaxGradient Multiply(ParticleSystem.MinMaxGradient source, Color tint)
        {
            switch (source.mode)
            {
                case ParticleSystemGradientMode.Color:
                    return new ParticleSystem.MinMaxGradient(source.color * tint);
                case ParticleSystemGradientMode.TwoColors:
                    return new ParticleSystem.MinMaxGradient(source.colorMin * tint, source.colorMax * tint);
                default:
                    // Gradients keep their shape; a flat tint is enough to tell auras apart.
                    return new ParticleSystem.MinMaxGradient(tint);
            }
        }
    }
}
