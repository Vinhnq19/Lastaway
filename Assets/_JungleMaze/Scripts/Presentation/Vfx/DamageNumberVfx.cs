using JungleMaze.App.Vfx;
using TMPro;
using UnityEngine;

namespace JungleMaze.Presentation.Vfx
{
    /// <summary>
    /// Floating damage number (VFX-01 Q6): world-space TextMeshPro that rises and fades over the tuning time, one number
    /// per event. <c>SetText("{0}", n)</c> does not allocate. Reset when the instance returns to the pool.
    /// </summary>
    public sealed class DamageNumberVfx : MonoBehaviour, IVfxSpawnListener
    {
        [SerializeField] private TMP_Text label;

        private float _elapsed, _seconds = .7f, _rise = .6f;
        private Color _color = Color.white;
        private Vector3 _base;
        private bool _running;

        public int Shown { get; private set; }
        public TMP_Text Label => label;

        public void OnVfxBegin(in VfxSpawn spawn, Color tint, bool hasTint, VfxStage stage)
        {
            if (label == null) label = GetComponentInChildren<TMP_Text>(true);
            if (stage != null)
            {
                _seconds = stage.Tuning.NumberSeconds > 0f ? stage.Tuning.NumberSeconds : .7f;
                _rise = stage.Tuning.NumberRiseCells;
            }
            Shown = spawn.Number;
            _color = hasTint ? tint : Color.white;
            _elapsed = 0f;
            _running = true;
            if (label == null) return;
            _base = label.transform.localPosition;
            label.SetText("{0}", spawn.Number);
            label.color = _color;
        }

        public void OnVfxEnd()
        {
            _running = false;
            if (label == null) return;
            label.transform.localPosition = _base;
            label.color = _color;
        }

        private void LateUpdate() => Advance(Time.deltaTime);

        /// <summary>Moves the number one step; public so tests can drive it.</summary>
        public void Advance(float dt)
        {
            if (!_running || label == null) return;
            _elapsed += dt;
            var t = _seconds > 0f ? Mathf.Clamp01(_elapsed / _seconds) : 1f;
            label.transform.localPosition = _base + Vector3.up * (_rise * (1f - (1f - t) * (1f - t)));
            var c = _color;
            c.a = _color.a * (t < .5f ? 1f : 1f - (t - .5f) * 2f);
            label.color = c;
        }
    }
}
