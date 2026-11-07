using System;
using JungleMaze.Domain.Hud;

namespace JungleMaze.App.Hud.Feedback
{
    /// <summary>
    /// Display lifetime of the local reload on the action bar and the ring above the hero. Mirrors
    /// <see cref="UseActionFeedback"/>: Using while the source reports a reload, then a short Completed or Cancelled
    /// linger. A reload that stops without a completion (weapon switch, death, leaving the match) reads as cancelled.
    /// </summary>
    public sealed class ReloadProgressFeedback
    {
        private readonly HudFeedbackTuning _t;
        private float _linger;

        public ReloadProgressFeedback(HudFeedbackTuning tuning) { _t = tuning; }

        public ItemUsePhase Phase { get; private set; }
        public bool Visible { get; private set; }
        /// <summary>1 − remaining/total, clamped to [0, 1]; 1 once completed.</summary>
        public float Fill { get; private set; }
        public float RemainingSeconds { get; private set; }
        /// <summary>The ring above the hero follows the reload and its completion, and hides at once when cancelled.</summary>
        public bool RingVisible => Visible && Phase != ItemUsePhase.Cancelled;

        /// <summary>Item use keeps the action bar; the reload only takes it when no use is shown.</summary>
        public bool ShowsOnBar(bool useVisible) => Visible && !useVisible;

        /// <summary>Ends a running reload from the source's own result (completed or rejected).</summary>
        public void Finish(bool completed)
        {
            if (Phase != ItemUsePhase.Using) return;
            Phase = completed ? ItemUsePhase.Completed : ItemUsePhase.Cancelled;
            _linger = completed ? _t.UseCompletedLinger : _t.UseCancelledLinger;
            if (completed) { Fill = 1f; RemainingSeconds = 0f; }
        }

        /// <summary>Samples the source once per frame and advances the linger.</summary>
        public void Tick(float dt, in ReloadSample sample)
        {
            if (sample.Active)
            {
                Phase = ItemUsePhase.Using;
                Visible = true;
                RemainingSeconds = Math.Max(0f, sample.RemainingSeconds);
                Fill = Ratio(sample);
                return;
            }
            if (Phase == ItemUsePhase.Using) { Finish(false); return; }
            if (!Visible) return;
            _linger -= Math.Max(0f, dt);
            if (_linger > .00001f) return;
            Visible = false;
            Phase = ItemUsePhase.Idle;
        }

        private static float Ratio(in ReloadSample s) =>
            s.TotalSeconds > 0f ? Math.Max(0f, Math.Min(1f, 1f - s.RemainingSeconds / s.TotalSeconds)) : 0f;
    }
}
