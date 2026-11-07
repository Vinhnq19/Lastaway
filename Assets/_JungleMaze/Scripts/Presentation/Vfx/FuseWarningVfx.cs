using JungleMaze.App.Vfx;
using UnityEngine;

namespace JungleMaze.Presentation.Vfx
{
    /// <summary>
    /// World warning over the area of a burning fuse (VFX-01, WPN-03 hand-off): a 9-slice frame sized to the blast
    /// square (<c>Size × Size</c> cells) plus a translucent fill, blinking faster as the fuse runs out. The director
    /// removes it in the same call as <c>Detonated</c>.
    /// </summary>
    public sealed class FuseWarningVfx : MonoBehaviour, IVfxSpawnListener
    {
        [SerializeField] private SpriteRenderer frame;
        [SerializeField] private SpriteRenderer fill;
        [SerializeField] private Color color = new Color(1f, .32f, .2f, 1f);
        [SerializeField, Range(0f, 1f)] private float fillAlpha = .12f;

        private float _elapsed, _seconds;
        private bool _running;

        /// <summary>Side lengths of the frame in cells (for tests).</summary>
        public Vector2 Size => frame != null ? frame.size : Vector2.zero;
        public SpriteRenderer Frame => frame;

        public void OnVfxBegin(in VfxSpawn spawn, Color tint, bool hasTint, VfxStage stage)
        {
            var w = spawn.AreaWidth > 0f ? spawn.AreaWidth : 1f;
            var h = spawn.AreaHeight > 0f ? spawn.AreaHeight : w;
            if (frame != null) frame.size = new Vector2(w, h);
            if (fill != null) fill.size = new Vector2(w, h);
            _seconds = spawn.Seconds;
            _elapsed = 0f;
            _running = true;
            Apply(1f);
        }

        public void OnVfxEnd() => _running = false;

        private void LateUpdate() => Advance(Time.deltaTime);

        /// <summary>Blinks one step; public so tests can drive it.</summary>
        public void Advance(float dt)
        {
            if (!_running) return;
            _elapsed += dt;
            var left = _seconds > 0f ? _seconds - _elapsed : 1f;
            // Steady for most of the fuse, then 4 Hz, then 10 Hz in the last 0.4 s.
            var hz = left > 1f ? 2f : left > .4f ? 4f : 10f;
            var pulse = .55f + .45f * Mathf.Abs(Mathf.Cos(_elapsed * hz * Mathf.PI));
            Apply(pulse);
        }

        private void Apply(float pulse)
        {
            if (frame != null) frame.color = new Color(color.r, color.g, color.b, color.a * pulse);
            if (fill != null) fill.color = new Color(color.r, color.g, color.b, fillAlpha * pulse);
        }
    }
}
