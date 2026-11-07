using System;
using System.Collections.Generic;
using JungleBackend.Protocol.Gameplay;
using JungleMaze.App.Combat;
using JungleMaze.App.Ports;
using JungleMaze.App.Visibility;
using JungleMaze.Core.DI;
using JungleMaze.Domain.CombatFeedback;
using JungleMaze.Domain.Vfx;
using NUnit.Framework;

namespace JungleMaze.Tests.Vfx
{
    /// <summary>AC3: the same event key plays once, for every event type (ZoneState removed by D1).</summary>
    public sealed class VfxDedupeTests
    {
        [Test]
        public void Hit_TwiceAndAgainAfterOneSecond_OneEffect()
        {
            var rig = new VfxRig();
            rig.Hit(77, 3);
            rig.Hit(77, 3);
            rig.Tick(1f);
            rig.Hit(77, 3);
            Assert.AreEqual(1, rig.Output.Count(VfxCue.HitSpark));
            Assert.AreEqual(1, rig.Output.Count(VfxCue.DamageNumber));
            Assert.AreEqual(1, rig.Output.Flashes.Count);
        }

        [Test]
        public void ExplosionFuse_TwiceAndAgainAfterOneSecond_OneWarning()
        {
            var rig = new VfxRig();
            rig.Fuse(5, 20f, 20f, seconds: 3f);
            rig.Fuse(5, 20f, 20f, seconds: 3f);
            rig.Tick(1f);
            rig.Fuse(5, 20f, 20f, seconds: 3f);
            Assert.AreEqual(1, rig.Output.Count(VfxCue.FuseWarning));
        }

        [Test]
        public void Status_TwiceAndAgainAfterOneSecond_OneAura()
        {
            var rig = new VfxRig();
            rig.Status(9, 4, StatusKind.Stun, 5f);
            rig.Status(9, 4, StatusKind.Stun, 5f);
            rig.Tick(1f);
            rig.Status(9, 4, StatusKind.Stun, 5f);
            Assert.AreEqual(1, rig.Output.Count(VfxCue.AuraStun));
        }

        [Test]
        public void RemoteAttack_SameSeqTwiceAndAgainAfterOneSecond_OnePuff()
        {
            var rig = new VfxRig();
            var e = new CombatEvent(CombatEventKind.RemoteAttackConfirmed, CombatWeaponId.Knife, 4, 11, 0f, CombatOutcome.Accepted);
            rig.Director.OnCombat(e);
            rig.Director.OnCombat(e);
            rig.Tick(1f);
            rig.Director.OnCombat(e);
            Assert.AreEqual(1, rig.Output.Count(VfxCue.AttackPuff));
        }

        [Test]
        public void DifferentKeys_EachPlay()
        {
            var rig = new VfxRig();
            rig.Hit(1, 3);
            rig.Hit(2, 3);
            Assert.AreEqual(2, rig.Output.Count(VfxCue.DamageNumber));
        }
    }

    /// <summary>AC4: own attacks are predicted and reconciled by RequestSeq.</summary>
    public sealed class AttackReconcileTests
    {
        private static CombatEvent Own(CombatEventKind kind, ushort seq, CombatOutcome outcome = CombatOutcome.Accepted) =>
            new CombatEvent(kind, CombatWeaponId.Hand, 0, seq, 0f, outcome);

        [Test]
        public void PredictedThenConfirmed_ExactlyOne()
        {
            var rig = new VfxRig();
            rig.Director.OnCombat(Own(CombatEventKind.AttackPredicted, 1));
            rig.Director.OnCombat(Own(CombatEventKind.AttackConfirmed, 1));
            Assert.AreEqual(1, rig.Output.Count(VfxCue.AttackPuff));
        }

        private static IEnumerable<CombatOutcome> Rejections()
        {
            foreach (CombatOutcome o in Enum.GetValues(typeof(CombatOutcome)))
                if (o != CombatOutcome.Accepted) yield return o;
        }

        [TestCaseSource(nameof(Rejections))]
        public void PredictedThenRejected_ReleasedInTheSameCall(CombatOutcome outcome)
        {
            var rig = new VfxRig();
            rig.Director.OnCombat(Own(CombatEventKind.AttackPredicted, 2));
            var handle = rig.Output.LastHandle(VfxCue.AttackPuff);
            Assert.IsTrue(rig.Output.IsLive(handle));
            rig.Director.OnCombat(Own(CombatEventKind.AttackRejected, 2, outcome));
            Assert.IsFalse(rig.Output.IsLive(handle), "back to the pool before the next frame");
        }

        [Test]
        public void RejectedThenLateConfirmation_NeverPlaysAgain()
        {
            var rig = new VfxRig();
            rig.Director.OnCombat(Own(CombatEventKind.AttackPredicted, 3));
            rig.Director.OnCombat(Own(CombatEventKind.AttackRejected, 3, CombatOutcome.Cooldown));
            rig.Director.OnCombat(Own(CombatEventKind.AttackConfirmed, 3));
            rig.Director.OnCombat(Own(CombatEventKind.AttackPredicted, 3));
            Assert.AreEqual(1, rig.Output.Count(VfxCue.AttackPuff));
            Assert.AreEqual(0, rig.Output.Live(VfxCue.AttackPuff));
        }

        [Test]
        public void RejectionWithoutPrediction_IsHarmless()
        {
            var rig = new VfxRig();
            Assert.DoesNotThrow(() => rig.Director.OnCombat(Own(CombatEventKind.AttackRejected, 9, CombatOutcome.NoAmmo)));
            Assert.AreEqual(0, rig.Output.Spawns.Count);
        }
    }

    /// <summary>AC5: auras refresh without stacking, are removed by event or by time, and unknown kinds warn once.</summary>
    public sealed class StatusLifetimeTests
    {
        [Test]
        public void ApplyAgain_OneInstance_TimerRefreshed()
        {
            var rig = new VfxRig();
            rig.Status(0, 3, StatusKind.Stun, 2f);
            rig.Tick(1.5f);
            rig.Status(0, 3, StatusKind.Stun, 2f);
            rig.Tick(1.9f);
            Assert.AreEqual(1, rig.Output.Count(VfxCue.AuraStun));
            Assert.AreEqual(1, rig.Output.Live(VfxCue.AuraStun), "refreshed timer is still running");
            rig.Tick(.2f);
            Assert.AreEqual(0, rig.Output.Live(VfxCue.AuraStun));
        }

        [Test]
        public void RemoveEvent_ReleasesNow()
        {
            var rig = new VfxRig();
            rig.Status(0, 3, StatusKind.Reverse, 10f);
            Assert.AreEqual(1, rig.Output.Live(VfxCue.AuraReverse));
            rig.Status(0, 3, StatusKind.Reverse, 0f, applied: false);
            Assert.AreEqual(0, rig.Output.Live(VfxCue.AuraReverse));
        }

        [TestCase(1f)]
        [TestCase(2f)]
        [TestCase(5f)]
        public void Expiry_WithinDurationPlusMinusTenthSecond(float duration)
        {
            var rig = new VfxRig();
            rig.Status(0, 5, StatusKind.Stun, duration);
            rig.Tick(duration - .1f);
            Assert.AreEqual(1, rig.Output.Live(VfxCue.AuraStun));
            rig.Tick(.2f);
            Assert.AreEqual(0, rig.Output.Live(VfxCue.AuraStun));
        }

        [Test]
        public void UnknownKind_NoException_OneWarning()
        {
            var rig = new VfxRig();
            Assert.DoesNotThrow(() =>
            {
                rig.Status(0, 3, (StatusKind)42, 2f);
                rig.Status(0, 4, (StatusKind)42, 2f);
            });
            Assert.AreEqual(1, rig.Log.Warnings.Count);
            Assert.AreEqual(0, rig.Output.Spawns.Count);
        }

        [Test]
        public void NoVisualKinds_NoWarning_NoSpawn()
        {
            var rig = new VfxRig();
            rig.Status(0, 3, StatusKind.Slow, 2f);
            rig.Status(0, 3, StatusKind.Root, 2f);
            rig.Status(0, 3, StatusKind.Adrenaline, 2f);
            Assert.AreEqual(0, rig.Log.Warnings.Count);
            Assert.AreEqual(0, rig.Output.Spawns.Count);
        }

        [Test]
        public void UntimedStatus_StaysUntilRemoved()
        {
            var rig = new VfxRig();
            rig.Status(0, 2, StatusKind.Poison, 0f);
            rig.Tick(10f);
            Assert.AreEqual(1, rig.Output.Live(VfxCue.AuraPoison));
        }
    }

    /// <summary>Plan 4.12.3: no crowd-control aura on a body that has the CcImmune aura.</summary>
    public sealed class StatusCcImmuneGuardTests
    {
        [Test]
        public void Immune_StunAndReverseShowNothing()
        {
            var rig = new VfxRig();
            rig.Status(0, 3, StatusKind.CcImmune, 7f);
            rig.Status(0, 3, StatusKind.Stun, 2f);
            rig.Status(0, 3, StatusKind.Reverse, 2f);
            Assert.AreEqual(0, rig.Output.Count(VfxCue.AuraStun));
            Assert.AreEqual(0, rig.Output.Count(VfxCue.AuraReverse));
            Assert.AreEqual(1, rig.Output.Count(VfxCue.AuraCcImmune));
        }

        [Test]
        public void ImmunityRemoved_ThenStun_OneAura()
        {
            var rig = new VfxRig();
            rig.Status(0, 3, StatusKind.CcImmune, 7f);
            rig.Status(0, 3, StatusKind.CcImmune, 0f, applied: false);
            rig.Status(0, 3, StatusKind.Stun, 2f);
            Assert.AreEqual(1, rig.Output.Live(VfxCue.AuraStun));
        }

        [Test]
        public void ImmunityOnAnotherBody_DoesNotBlock()
        {
            var rig = new VfxRig();
            rig.Status(0, 2, StatusKind.CcImmune, 7f);
            rig.Status(0, 3, StatusKind.Stun, 2f);
            Assert.AreEqual(1, rig.Output.Live(VfxCue.AuraStun));
        }
    }

    /// <summary>AC6: a new session, leaving InGame or ResetAll releases everything and forgets the keys.</summary>
    public sealed class VfxDirectorResetTests
    {
        private static void Fill(VfxRig rig)
        {
            rig.Hit(1, 3);
            rig.Status(2, 4, StatusKind.Stun, 5f);
            rig.Fuse(3, 20f, 20f, seconds: 3f);
            rig.Director.OnCombat(new CombatEvent(CombatEventKind.AttackPredicted, CombatWeaponId.Hand, 0, 1, 0f, CombatOutcome.Accepted));
            Assert.Greater(rig.Output.ActiveCount, 0);
        }

        [Test]
        public void NewSessionKey_ReleasesAll_InTheSameTick_AndKeysAreForgotten()
        {
            var rig = new VfxRig();
            Fill(rig);
            rig.Feed.SessionKey = Guid.NewGuid();
            rig.Director.Tick(VfxRig.Dt);
            Assert.AreEqual(0, rig.Output.ActiveCount);
            Assert.AreEqual(0, rig.Director.LiveCount);
            Assert.AreEqual(0, rig.Director.AuraCount);
            Assert.AreEqual(0, rig.Director.FuseCount);
            rig.Hit(1, 3);
            Assert.AreEqual(2, rig.Output.Count(VfxCue.DamageNumber), "same key plays again in the new match");
        }

        [Test]
        public void LeavingInGame_ReleasesAll_AndIgnoresEvents()
        {
            var rig = new VfxRig();
            Fill(rig);
            rig.Feed.InGame = false;
            rig.Director.Tick(VfxRig.Dt);
            Assert.AreEqual(0, rig.Output.ActiveCount);
            rig.Hit(50, 3);
            Assert.AreEqual(1, rig.Output.Count(VfxCue.DamageNumber), "nothing plays outside InGame");
        }

        [Test]
        public void ResetAll_CancelsRunningFlashes()
        {
            var rig = new VfxRig();
            rig.Hit(1, 3);
            rig.Director.ResetAll();
            Assert.Contains((byte)3, rig.Output.CancelledFlashes);
            Assert.AreEqual(0, rig.Output.ActiveCount);
        }
    }

    /// <summary>AC8: the visibility seam hides every sign of a hidden target, at once and without leftovers.</summary>
    public sealed class VisibilitySeamTests
    {
        [Test]
        public void HiddenTarget_HitAndAttack_NothingSpawned_NoFlash_NoShake()
        {
            var rig = new VfxRig();
            rig.Visibility.SetHidden(3, true);
            rig.Hit(1, 3);
            rig.Director.OnCombat(new CombatEvent(CombatEventKind.RemoteAttackConfirmed, CombatWeaponId.Knife, 3, 1, 0f, CombatOutcome.Accepted));
            rig.Director.Tick(VfxRig.Dt);
            Assert.AreEqual(0, rig.Output.Spawns.Count);
            Assert.AreEqual(0, rig.Output.Flashes.Count);
            Assert.AreEqual(0f, rig.Director.Shake.Amplitude);
        }

        [Test]
        public void HiddenCentre_FuseNotDrawn_DetonateDoesNotShake()
        {
            var rig = new VfxRig();
            // Own body at (10, 10): a visible blast at (14, 10) would shake; hidden, it must not.
            rig.Visibility.HideRect(12f, 5f, 20f, 15f);
            rig.Fuse(1, 14f, 10f);
            Assert.AreEqual(0, rig.Output.Count(VfxCue.FuseWarning));
            rig.Detonate(1, 14f, 10f);
            rig.Director.Tick(VfxRig.Dt);
            Assert.AreEqual(0f, rig.Director.Shake.Amplitude);
        }

        [Test]
        public void HiddenTarget_StatusGetsNoAura()
        {
            var rig = new VfxRig();
            rig.Visibility.SetHidden(4, true);
            rig.Status(1, 4, StatusKind.Stun, 5f);
            Assert.AreEqual(0, rig.Output.Count(VfxCue.AuraStun));
        }

        [Test]
        public void ShownAuraThenHidden_ReleasedInTheTick_ShownAgain_ExactlyOne()
        {
            var rig = new VfxRig();
            rig.Status(1, 4, StatusKind.Reverse, 10f);
            Assert.AreEqual(1, rig.Output.Live(VfxCue.AuraReverse));
            rig.Visibility.SetHidden(4, true);
            rig.Director.Tick(VfxRig.Dt);
            Assert.AreEqual(0, rig.Output.Live(VfxCue.AuraReverse));
            rig.Visibility.SetHidden(4, false);
            rig.Director.Tick(VfxRig.Dt);
            rig.Director.Tick(VfxRig.Dt);
            Assert.AreEqual(1, rig.Output.Live(VfxCue.AuraReverse));
            Assert.AreEqual(2, rig.Output.Count(VfxCue.AuraReverse));
        }

        [Test]
        public void TargetHidden_EveryLiveEffectOfItGoes_FlashCancelled()
        {
            var rig = new VfxRig();
            rig.Hit(1, 4);
            rig.Status(2, 4, StatusKind.Poison, 3f);
            Assert.AreEqual(3, rig.Output.ActiveCount);
            rig.Visibility.SetHidden(4, true);
            rig.Director.Tick(VfxRig.Dt);
            Assert.AreEqual(0, rig.Output.ActiveCount, "spark, number and aura all gone");
            Assert.Contains((byte)4, rig.Output.CancelledFlashes);
        }

        [Test]
        public void LocalPlayer_AlwaysShown()
        {
            var rig = new VfxRig();
            rig.Visibility.SetHidden(0, true);
            rig.Hit(1, 0);
            rig.Status(2, 0, StatusKind.Stun, 2f);
            Assert.AreEqual(1, rig.Output.Count(VfxCue.DamageNumber));
            Assert.AreEqual(1, rig.Output.Count(VfxCue.AuraStun));
        }

        [Test]
        public void AlwaysVisiblePolicy_SameCountsAsNoSeam()
        {
            var a = new VfxRig();
            a.Hit(1, 3);
            a.Status(2, 4, StatusKind.Stun, 2f);
            a.Fuse(3, 20f, 20f);
            a.Tick(.5f);
            var b = new VfxRig();
            b.Visibility.SetHidden(9, true); // unrelated body
            b.Hit(1, 3);
            b.Status(2, 4, StatusKind.Stun, 2f);
            b.Fuse(3, 20f, 20f);
            b.Tick(.5f);
            Assert.AreEqual(a.Output.Spawns.Count, b.Output.Spawns.Count);
            Assert.AreEqual(a.Output.ActiveCount, b.Output.ActiveCount);
        }
    }

    /// <summary>
    /// Review round 1, suggestion 2: the VFX default visibility is not registered, so FOW-01 can register its policy after
    /// VFX is composed without a duplicate-type error, and VFX then follows that policy.
    /// </summary>
    public sealed class DeferredVisibilityPolicyTests
    {
        private sealed class HideAll : IVisibilityPolicy
        {
            public bool IsVisible(float x, float y, byte netId) => false;
        }

        private static DeferredVisibilityPolicy Deferred(ServiceRegistry registry) =>
            new DeferredVisibilityPolicy(() => registry.TryResolve<IVisibilityPolicy>(out var fow) ? fow : null);

        [Test]
        public void NothingRegistered_EverythingVisible_AndTheRegistryStaysEmpty()
        {
            var registry = new ServiceRegistry();
            var policy = Deferred(registry);
            Assert.IsTrue(policy.IsVisible(1f, 2f, 3));
            Assert.IsTrue(policy.IsVisible(1f, 2f, VisibilityQuery.PositionOnly));
            Assert.IsFalse(policy.IsResolved);
            Assert.IsFalse(registry.IsRegistered(typeof(IVisibilityPolicy)));
        }

        [Test]
        public void FowRegisteredAfterVfx_NoThrow_AndVfxFollowsIt()
        {
            var registry = new ServiceRegistry();
            var policy = Deferred(registry);
            var rig = new VfxRig(visibility: policy);
            rig.Status(1, 4, StatusKind.Stun, 5f);
            Assert.AreEqual(1, rig.Output.Live(VfxCue.AuraStun));
            Assert.DoesNotThrow(() => registry.Register<IVisibilityPolicy>(new HideAll()));
            rig.Director.Tick(VfxRig.Dt);
            Assert.IsTrue(policy.IsResolved);
            Assert.AreEqual(0, rig.Output.Live(VfxCue.AuraStun), "the late FOW policy hides the aura");
        }

        [Test]
        public void LookupReturningItself_IsIgnored()
        {
            DeferredVisibilityPolicy policy = null;
            policy = new DeferredVisibilityPolicy(() => policy);
            Assert.IsTrue(policy.IsVisible(0f, 0f, 1));
            Assert.IsFalse(policy.IsResolved);
        }

        [Test]
        public void ResolvedLookup_IsNotAskedAgain()
        {
            var calls = 0;
            var hide = new HideAll();
            var policy = new DeferredVisibilityPolicy(() => { calls++; return hide; });
            Assert.IsFalse(policy.IsVisible(0f, 0f, 1));
            Assert.IsFalse(policy.IsVisible(0f, 0f, 2));
            Assert.AreEqual(1, calls);
        }
    }

    /// <summary>AC12: one number per event showing the damage, no number for zero damage, nothing on a missing body.</summary>
    public sealed class HitFeedbackTests
    {
        [Test]
        public void Number_EqualsDamage_OnePerEvent_LivesNumberSeconds()
        {
            var rig = new VfxRig();
            rig.Hit(1, 3, 37);
            Assert.AreEqual(1, rig.Output.Count(VfxCue.DamageNumber));
            var spawn = rig.Output.Spawns.Find(s => s.Cue == VfxCue.DamageNumber).Spawn;
            Assert.AreEqual(37, spawn.Number);
            Assert.AreEqual(3, spawn.AnchorNetId);
            rig.Tick(.65f);
            Assert.AreEqual(1, rig.Output.Live(VfxCue.DamageNumber));
            rig.Tick(.1f);
            Assert.AreEqual(0, rig.Output.Live(VfxCue.DamageNumber));
        }

        [Test]
        public void ZeroDamage_NoNumberNoFlash()
        {
            var rig = new VfxRig();
            rig.Hit(1, 3, 0);
            rig.Hit(2, 3, -5);
            Assert.AreEqual(0, rig.Output.Spawns.Count);
            Assert.AreEqual(0, rig.Output.Flashes.Count);
        }

        [Test]
        public void MissingBody_NothingAndNoError()
        {
            var rig = new VfxRig();
            rig.Targets.Remove(6);
            Assert.DoesNotThrow(() => rig.Hit(1, 6));
            Assert.AreEqual(0, rig.Output.Spawns.Count);
            Assert.AreEqual(0, rig.Output.Flashes.Count);
            Assert.AreEqual(0, rig.Log.Errors.Count);
        }

        [Test]
        public void NumberColour_TakenDealtOther()
        {
            var rig = new VfxRig();
            rig.Hit(1, 0, 5, source: 3);
            rig.Hit(2, 3, 5, source: 0);
            rig.Hit(3, 4, 5, source: 3);
            var tints = rig.Output.Spawns.FindAll(s => s.Cue == VfxCue.DamageNumber).ConvertAll(s => s.Spawn.Tint);
            CollectionAssert.AreEqual(new[] { VfxTints.NumberTaken, VfxTints.NumberDealt, VfxTints.NumberOther }, tints);
        }

        [Test]
        public void BodyDisappears_ItsEffectsGo()
        {
            var rig = new VfxRig();
            rig.Hit(1, 3);
            rig.Targets.Remove(3);
            rig.Director.Tick(VfxRig.Dt);
            Assert.AreEqual(0, rig.Output.ActiveCount);
        }
    }

    /// <summary>AC13 (director part): shake only when on, InGame, and for the own hit or a near visible blast.</summary>
    public sealed class VfxShakeRuleTests
    {
        [Test]
        public void OwnHit_Shakes_OtherHitDoesNot()
        {
            var rig = new VfxRig();
            rig.Hit(1, 3);
            rig.Director.Tick(VfxRig.Dt);
            Assert.AreEqual(0f, rig.Director.Shake.Amplitude);
            rig.Hit(2, 0);
            rig.Director.Tick(VfxRig.Dt);
            Assert.Greater(rig.Director.Shake.Amplitude, 0f);
        }

        [Test]
        public void NearBlast_Shakes_FarBlastDoesNot()
        {
            var rig = new VfxRig(); // own body at (10, 10)
            rig.Detonate(1, 40f, 40f);
            rig.Director.Tick(VfxRig.Dt);
            Assert.AreEqual(0f, rig.Director.Shake.Amplitude);
            rig.Detonate(2, 12f, 10f);
            rig.Director.Tick(VfxRig.Dt);
            Assert.Greater(rig.Director.Shake.Amplitude, 0f);
            Assert.LessOrEqual(rig.Director.Shake.Amplitude, .15f + 1e-5f);
        }

        [Test]
        public void SettingOff_OffsetZeroEveryTick_EvenMidShake()
        {
            var rig = new VfxRig();
            rig.Hit(1, 0);
            rig.Director.Tick(VfxRig.Dt);
            Assert.Greater(rig.Director.Shake.Amplitude, 0f);
            rig.Settings.ScreenShakeEnabled = false;
            for (var i = 0; i < 10; i++)
            {
                rig.Director.Tick(VfxRig.Dt);
                Assert.AreEqual(0f, rig.Director.Shake.OffsetX);
                Assert.AreEqual(0f, rig.Director.Shake.OffsetY);
                rig.Hit((ulong)(10 + i), 0);
            }
        }

        [Test]
        public void NotInGame_NoOffset()
        {
            var rig = new VfxRig();
            rig.Hit(1, 0);
            rig.Director.Tick(VfxRig.Dt);
            rig.Feed.InGame = false;
            rig.Director.Tick(VfxRig.Dt);
            Assert.AreEqual(0f, rig.Director.Shake.OffsetX);
            Assert.AreEqual(0f, rig.Director.Shake.Amplitude);
        }
    }

    /// <summary>
    /// Fuse warning lifetime (AC2 part): from Fuse until Detonate in the same call, or until the fuse ends plus the
    /// tuning grace (review round 1).
    /// </summary>
    public sealed class FuseWarningTests
    {
        [Test]
        public void Detonate_RemovesTheWarningInTheSameCall()
        {
            var rig = new VfxRig();
            rig.Fuse(4, 20f, 20f, 3f, 1.5f);
            var spawn = rig.Output.Spawns.Find(s => s.Cue == VfxCue.FuseWarning).Spawn;
            Assert.AreEqual(3f, spawn.AreaWidth);
            Assert.AreEqual(1.5f, spawn.Seconds, 1e-4f);
            rig.Tick(.5f);
            Assert.AreEqual(1, rig.Output.Live(VfxCue.FuseWarning));
            rig.Detonate(4, 20f, 20f);
            Assert.AreEqual(0, rig.Output.Live(VfxCue.FuseWarning));
        }

        [Test]
        public void NoDetonate_WarningEndsAfterTheFusePlusTheGrace()
        {
            var rig = new VfxRig();
            Assert.AreEqual(.15f, VfxTuningValues.Default.FuseGraceSeconds, 1e-6f, "review round 1: grace from the tuning");
            rig.Fuse(4, 20f, 20f, 3f, 1.5f);
            rig.Director.Tick(1.45f);
            Assert.AreEqual(1, rig.Output.Live(VfxCue.FuseWarning));
            rig.Director.Tick(.1f); // 0.05 s past the fuse: still waiting for a late Detonated
            Assert.AreEqual(1, rig.Output.Live(VfxCue.FuseWarning));
            Assert.AreEqual(1, rig.Director.FuseCount);
            rig.Director.Tick(.11f); // 0.16 s past the fuse: gone
            Assert.AreEqual(0, rig.Output.Live(VfxCue.FuseWarning));
            Assert.AreEqual(0, rig.Director.FuseCount);
        }

        [Test]
        public void LateDetonate_InsideTheGrace_StillRemovesTheWarningInTheSameCall()
        {
            var rig = new VfxRig();
            rig.Fuse(4, 20f, 20f, 3f, 1.5f);
            rig.Director.Tick(1.6f);
            Assert.AreEqual(1, rig.Output.Live(VfxCue.FuseWarning));
            rig.Detonate(4, 20f, 20f);
            Assert.AreEqual(0, rig.Output.Live(VfxCue.FuseWarning));
            Assert.AreEqual(0, rig.Director.FuseCount);
        }

        [Test]
        public void ZeroGrace_WarningEndsExactlyWithTheFuse()
        {
            var d = VfxTuningValues.Default;
            var tuning = new VfxTuningValues(d.FlashSeconds, d.NumberSeconds, d.NumberRiseCells, d.ShakeMaxCells, d.ShakeSeconds,
                d.ShakeRadiusCells, d.DedupeSeconds, d.DedupeCapacity, d.Budget, d.HitShakeCells, 0f);
            var rig = new VfxRig(tuning);
            rig.Fuse(4, 20f, 20f, 3f, 1.5f);
            rig.Director.Tick(1.45f);
            Assert.AreEqual(1, rig.Output.Live(VfxCue.FuseWarning));
            rig.Director.Tick(.1f);
            Assert.AreEqual(0, rig.Output.Live(VfxCue.FuseWarning));
        }

        [Test]
        public void HiddenWhenTheFuseRunsOut_NotDrawnAgainDuringTheGrace()
        {
            var rig = new VfxRig();
            rig.Fuse(4, 20f, 20f, 3f, 1.5f, owner: 1);
            rig.Visibility.HideRect(15f, 15f, 25f, 25f);
            rig.Director.Tick(1.55f);
            Assert.AreEqual(0, rig.Output.Live(VfxCue.FuseWarning));
            rig.Visibility.ClearRect();
            rig.Director.Tick(VfxRig.Dt);
            Assert.AreEqual(0, rig.Output.Live(VfxCue.FuseWarning), "no new warning after its timer ran out");
            Assert.AreEqual(1, rig.Output.Count(VfxCue.FuseWarning), "only the first spawn");
        }
    }

    /// <summary>AC15 (rules): the 64 cap evicts lower priorities first and never refuses a fuse warning for hits.</summary>
    public sealed class VfxBudgetTests
    {
        [Test]
        public void Full_OfHits_FuseWarningStillSpawns_ByEvictingOldestLow()
        {
            var rig = new VfxRig();
            for (ulong k = 1; k <= 32; k++) rig.Hit(k, (byte)(1 + k % 10));
            Assert.AreEqual(64, rig.Director.LiveCount);
            var oldest = rig.Output.Spawns[0].Handle;
            rig.Fuse(999, 30f, 30f);
            Assert.AreEqual(1, rig.Output.Live(VfxCue.FuseWarning));
            Assert.IsFalse(rig.Output.IsLive(oldest));
            Assert.AreEqual(64, rig.Director.LiveCount);
        }

        [Test]
        public void Full_OfHits_AnotherHitIsRefused_WithOneWarning()
        {
            var rig = new VfxRig();
            for (ulong k = 1; k <= 34; k++) rig.Hit(k, (byte)(1 + k % 10));
            Assert.AreEqual(64, rig.Director.LiveCount);
            Assert.Greater(rig.Director.BudgetRefused, 0);
            Assert.AreEqual(1, rig.Log.Warnings.Count);
            Assert.LessOrEqual(rig.Output.ActiveCount, 64);
        }
    }
}
