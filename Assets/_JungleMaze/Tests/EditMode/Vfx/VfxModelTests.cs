using JungleMaze.Domain.CombatFeedback;
using JungleMaze.Domain.Hud;
using JungleMaze.Domain.Vfx;
using NUnit.Framework;

namespace JungleMaze.Tests.Vfx
{
    /// <summary>VFX-01 Domain models: ledger, aura table, budget, shake, status kinds (plan 4.12.9 step 1).</summary>
    public sealed class VfxModelTests
    {
        [Test]
        public void StatusKinds_MatchEffectKindOneToEight()
        {
            for (var v = 1; v <= 8; v++)
                Assert.AreEqual(v, (int)StatusKinds.FromEffect((EffectKind)v), ((EffectKind)v).ToString());
            Assert.AreEqual(StatusKind.Stun, StatusKinds.FromEffect(EffectKind.Stun));
            Assert.AreEqual(StatusKind.CcImmune, StatusKinds.FromEffect(EffectKind.CcImmune));
            Assert.AreEqual(StatusKind.None, StatusKinds.FromEffect((EffectKind)0));
            Assert.AreEqual(StatusKind.None, StatusKinds.FromEffect((EffectKind)99));
        }

        [Test]
        public void Ledger_PredictConfirmReject()
        {
            var ledger = new AttackVfxLedger();
            Assert.IsTrue(ledger.TryPredict(0, 5, 0f));
            Assert.IsFalse(ledger.TryPredict(0, 5, 0f), "same seq is not predicted twice");
            ledger.AttachHandle(0, 5, 42, 0f);
            Assert.IsTrue(ledger.Confirm(0, 5, .1f));
            Assert.AreEqual(0, ledger.Reject(0, 5, .2f), "a confirmed seq has no live prediction to release");

            Assert.IsTrue(ledger.TryPredict(0, 6, 0f));
            ledger.AttachHandle(0, 6, 43, 0f);
            Assert.AreEqual(43, ledger.Reject(0, 6, .1f));
            Assert.IsFalse(ledger.Confirm(0, 6, .2f), "late confirmation of a rejected seq");
            Assert.AreEqual(AttackVfxState.Rejected, ledger.StateOf(0, 6, .2f));
        }

        [Test]
        public void Ledger_OldSeqIsNewAgainAfterMaxAge()
        {
            var ledger = new AttackVfxLedger();
            ledger.TryPredict(0, 9, 0f);
            Assert.IsTrue(ledger.TryPredict(0, 9, AttackVfxLedger.MaxAgeSeconds + .1f), "wrapped ushort seq");
        }

        [Test]
        public void AuraTable_RefreshDoesNotStack_AndExpires()
        {
            var table = new StatusAuraTable(8);
            var i = table.Apply(3, StatusKind.Stun, VfxCue.AuraStun, 0, 2f, out var added);
            Assert.IsTrue(added);
            table.Advance(1.5f);
            Assert.AreEqual(i, table.Apply(3, StatusKind.Stun, VfxCue.AuraStun, 0, 2f, out added));
            Assert.IsFalse(added);
            Assert.AreEqual(1, table.Count);
            table.Advance(1.9f);
            Assert.IsFalse(table.IsExpired(i), "timer was reset, not added");
            table.Advance(.2f);
            Assert.IsTrue(table.IsExpired(i));
            var untimed = table.Apply(4, StatusKind.Poison, VfxCue.AuraPoison, 0, 0f, out _);
            table.Advance(100f);
            Assert.IsFalse(table.IsExpired(untimed));
        }

        [Test]
        public void Budget_EvictsOldestLowerPriority_NeverCritical()
        {
            var budget = new VfxBudget(3);
            Assert.IsTrue(budget.TryReserve(VfxPriority.Low, out _)); budget.Add(1, VfxPriority.Low);
            Assert.IsTrue(budget.TryReserve(VfxPriority.Low, out _)); budget.Add(2, VfxPriority.Low);
            Assert.IsTrue(budget.TryReserve(VfxPriority.Critical, out _)); budget.Add(3, VfxPriority.Critical);
            Assert.IsFalse(budget.TryReserve(VfxPriority.Low, out _), "nothing lower than Low");
            Assert.IsTrue(budget.TryReserve(VfxPriority.Medium, out var evict));
            Assert.AreEqual(1, evict, "oldest Low goes first");
            budget.Add(4, VfxPriority.Medium);
            Assert.IsTrue(budget.TryReserve(VfxPriority.Critical, out evict));
            Assert.AreEqual(2, evict);
            budget.Add(5, VfxPriority.Critical);
            Assert.IsTrue(budget.TryReserve(VfxPriority.Critical, out evict));
            Assert.AreEqual(4, evict, "a Critical is never evicted, the Medium is");
            Assert.AreEqual(1, budget.Refused);
        }

        [Test]
        public void Shake_PeakCapped_DecaysWithinTime_PulsesDoNotExceedCap()
        {
            var shake = new ScreenShakeModel(.15f, .3f);
            for (var i = 0; i < 6; i++) shake.Add(.15f, 1f);
            var peak = 0f;
            var t = 0f;
            while (shake.Active && t < 1f)
            {
                shake.Tick(1f / 60f);
                t += 1f / 60f;
                var m = (float)System.Math.Sqrt(shake.OffsetX * shake.OffsetX + shake.OffsetY * shake.OffsetY);
                if (m > peak) peak = m;
                Assert.LessOrEqual(shake.Amplitude, .15f + 1e-5f);
            }
            Assert.LessOrEqual(peak, .15f + 1e-5f);
            Assert.LessOrEqual(t, .3f + 1f / 60f + 1e-4f, "back to zero within 0.3 s");
            Assert.AreEqual(0f, shake.OffsetX);
            Assert.AreEqual(0f, shake.OffsetY);
        }

        [Test]
        public void Shake_ClearZeroesNow()
        {
            var shake = new ScreenShakeModel();
            shake.Add(.1f, .3f);
            shake.Tick(.01f);
            shake.Clear();
            Assert.AreEqual(0f, shake.OffsetX);
            Assert.IsFalse(shake.Active);
        }
    }
}
