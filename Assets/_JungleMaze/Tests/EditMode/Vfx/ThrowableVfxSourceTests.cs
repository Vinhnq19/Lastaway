using System;
using System.Collections.Generic;
using JungleBackend.Protocol.Gameplay;
using JungleMaze.App.Combat;
using JungleMaze.App.Vfx;
using JungleMaze.App.Vfx.Mocks;
using JungleMaze.Core.Events;
using JungleMaze.Domain.CombatFeedback;
using JungleMaze.Domain.Vfx;
using JungleMaze.Tests.MatchUi;
using JungleMaze.World.Throwables;
using NUnit.Framework;

namespace JungleMaze.Tests.Vfx
{
    /// <summary>Plan 4.12.3: every WPN-03 event kind maps to the VFX contract events; dedupe by Id, never by Id 0.</summary>
    public sealed class ThrowableVfxSourceTests
    {
        private sealed class Capture
        {
            public readonly List<HitConfirmedEvent> Hits = new List<HitConfirmedEvent>();
            public readonly List<ExplosionEvent> Explosions = new List<ExplosionEvent>();
            public readonly List<StatusEvent> Statuses = new List<StatusEvent>();

            public Capture(IEventBus bus)
            {
                bus.Subscribe<HitConfirmedEvent>(Hits.Add);
                bus.Subscribe<ExplosionEvent>(Explosions.Add);
                bus.Subscribe<StatusEvent>(Statuses.Add);
            }
        }

        private EventBus _bus;
        private Capture _capture;
        private ThrowableVfxSource _source;

        [SetUp]
        public void SetUp()
        {
            _bus = new EventBus();
            _capture = new Capture(_bus);
            _source = new ThrowableVfxSource(_bus, null);
        }

        private static ThrowableEvent E(ThrowableEventKind kind, ThrowableKind item, ushort id = 7, byte target = ThrowableEvent.NoTarget,
            short amount = 0, float seconds = 0f, float size = 3f, byte flags = 0) => new ThrowableEvent
        {
            Kind = kind, Item = item, Actor = 1, Target = target, Id = id, FromX = 5, FromY = 6, ToX = 8, ToY = 9,
            Seconds = seconds, Size = size, Amount = amount, Flags = flags
        };

        [Test]
        public void Thrown_IsAFuseAtTheLanding_WithSizeAndSeconds()
        {
            _source.OnThrowable(E(ThrowableEventKind.Thrown, ThrowableKind.Banana, seconds: 1.5f));
            Assert.AreEqual(1, _capture.Explosions.Count);
            var e = _capture.Explosions[0];
            Assert.AreEqual(ExplosionPhase.Fuse, e.Phase);
            Assert.AreEqual(8f, e.CenterX);
            Assert.AreEqual(9f, e.CenterY);
            Assert.AreEqual(3f, e.AreaWidth);
            Assert.AreEqual(3f, e.AreaHeight);
            Assert.AreEqual(1.5f, e.SecondsRemaining);
            Assert.AreEqual("BANANA", e.WeaponId);
            Assert.AreEqual(1, e.OwnerNetId);
        }

        [Test]
        public void Detonated_IsTheBlastAtItsCentre_SameKeyAsTheFuse()
        {
            _source.OnThrowable(E(ThrowableEventKind.Thrown, ThrowableKind.Seed, seconds: 1.5f));
            _source.OnThrowable(E(ThrowableEventKind.Detonated, ThrowableKind.Seed));
            Assert.AreEqual(2, _capture.Explosions.Count);
            Assert.AreEqual(ExplosionPhase.Detonate, _capture.Explosions[1].Phase);
            Assert.AreEqual(5f, _capture.Explosions[1].CenterX);
            Assert.AreEqual(_capture.Explosions[0].EventKey, _capture.Explosions[1].EventKey);
        }

        [TestCase(ThrowableKind.Rope)]
        [TestCase(ThrowableKind.Bell)]
        [TestCase(ThrowableKind.Bat)]
        public void SpecialItems_NeverMakeAFuse(ThrowableKind item)
        {
            _source.OnThrowable(E(ThrowableEventKind.Thrown, item, seconds: 1f));
            Assert.AreEqual(0, _capture.Explosions.Count);
        }

        [Test]
        public void Hit_WithDamage_IsAHitOnTheTarget()
        {
            _source.OnThrowable(E(ThrowableEventKind.Hit, ThrowableKind.Meteorite, target: 4, amount: 35));
            Assert.AreEqual(1, _capture.Hits.Count);
            Assert.AreEqual(4, _capture.Hits[0].TargetNetId);
            Assert.AreEqual(1, _capture.Hits[0].SourceNetId);
            Assert.AreEqual(35, _capture.Hits[0].Damage);
            Assert.AreEqual(0, _capture.Statuses.Count);
        }

        [Test]
        public void Hit_WithoutDamage_IsNoHit()
        {
            _source.OnThrowable(E(ThrowableEventKind.Hit, ThrowableKind.Banana, target: 4, amount: 0));
            _source.OnThrowable(E(ThrowableEventKind.RopePulledTarget, ThrowableKind.Rope, id: 0, target: 4, amount: 0));
            Assert.AreEqual(0, _capture.Hits.Count);
        }

        [Test]
        public void BellHit_AddsReverseForItsSeconds()
        {
            _source.OnThrowable(E(ThrowableEventKind.Hit, ThrowableKind.Bell, id: 0, target: 4, amount: 15, seconds: 10f));
            Assert.AreEqual(1, _capture.Hits.Count);
            Assert.AreEqual(1, _capture.Statuses.Count);
            Assert.AreEqual(StatusKind.Reverse, _capture.Statuses[0].Kind);
            Assert.AreEqual(10f, _capture.Statuses[0].DurationSeconds);
            Assert.IsTrue(_capture.Statuses[0].Applied);
        }

        [Test]
        public void SeedHit_AddsRoot()
        {
            _source.OnThrowable(E(ThrowableEventKind.Hit, ThrowableKind.Seed, target: 4, amount: 20, seconds: 1.5f));
            Assert.AreEqual(StatusKind.Root, _capture.Statuses[0].Kind);
        }

        [Test]
        public void BatHit_StunOnlyWithTheFlag()
        {
            _source.OnThrowable(E(ThrowableEventKind.BatHit, ThrowableKind.Bat, id: 0, target: 4, amount: 10, seconds: 0f));
            Assert.AreEqual(1, _capture.Hits.Count);
            Assert.AreEqual(0, _capture.Statuses.Count);
            _source.OnThrowable(E(ThrowableEventKind.BatHit, ThrowableKind.Bat, id: 0, target: 4, amount: 10, seconds: 2f, flags: ThrowableEvent.StunnedFlag));
            Assert.AreEqual(2, _capture.Hits.Count);
            Assert.AreEqual(StatusKind.Stun, _capture.Statuses[0].Kind);
            Assert.AreEqual(2f, _capture.Statuses[0].DurationSeconds);
        }

        [Test]
        public void TrapTriggered_StunsTheTarget()
        {
            _source.OnThrowable(E(ThrowableEventKind.TrapTriggered, ThrowableKind.Banana, target: 6, seconds: 1f, flags: ThrowableEvent.StunnedFlag));
            Assert.AreEqual(1, _capture.Statuses.Count);
            Assert.AreEqual(6, _capture.Statuses[0].NetId);
            Assert.AreEqual(StatusKind.Stun, _capture.Statuses[0].Kind);
        }

        [Test]
        public void RopePulledTarget_WithDamage_IsAHit()
        {
            _source.OnThrowable(E(ThrowableEventKind.RopePulledTarget, ThrowableKind.Rope, id: 0, target: 4, amount: 5));
            Assert.AreEqual(1, _capture.Hits.Count);
        }

        [TestCase(ThrowableEventKind.TrapArmed)]
        [TestCase(ThrowableEventKind.TrapExpired)]
        [TestCase(ThrowableEventKind.SmokeStarted)]
        [TestCase(ThrowableEventKind.SmokeEnded)]
        [TestCase(ThrowableEventKind.BellRung)]
        [TestCase(ThrowableEventKind.BatSwing)]
        [TestCase(ThrowableEventKind.RopePulledSelf)]
        [TestCase(ThrowableEventKind.RopeMissed)]
        public void OtherKinds_PublishNothing(ThrowableEventKind kind)
        {
            _source.OnThrowable(E(kind, ThrowableKind.Smoke, target: 4, amount: 5, seconds: 10f));
            Assert.AreEqual(0, _capture.Hits.Count + _capture.Explosions.Count + _capture.Statuses.Count);
        }

        [Test]
        public void ThroughTheDirector_SameIdTwice_OneEffect_IdZeroTwice_TwoEffects()
        {
            var rig = new VfxRig();
            var source = new ThrowableVfxSource(rig.Bus, null);
            var hit = E(ThrowableEventKind.Hit, ThrowableKind.Banana, id: 12, target: 3, amount: 30);
            source.OnThrowable(hit);
            source.OnThrowable(hit);
            Assert.AreEqual(1, rig.Output.Count(VfxCue.DamageNumber), "Id ≠ 0 repeated → one");
            var bell = E(ThrowableEventKind.Hit, ThrowableKind.Bell, id: 0, target: 3, amount: 15, seconds: 10f);
            source.OnThrowable(bell);
            rig.Tick(.5f);
            source.OnThrowable(bell);
            Assert.AreEqual(3, rig.Output.Count(VfxCue.DamageNumber), "two bell hits within 1 s are two events");
        }

        [Test]
        public void ThroughTheDirector_FuseLivesFromThrownToDetonated()
        {
            var rig = new VfxRig();
            var source = new ThrowableVfxSource(rig.Bus, null);
            source.OnThrowable(E(ThrowableEventKind.Thrown, ThrowableKind.Banana, seconds: 1.5f));
            rig.Tick(1f);
            Assert.AreEqual(1, rig.Output.Live(VfxCue.FuseWarning));
            source.OnThrowable(E(ThrowableEventKind.Detonated, ThrowableKind.Banana));
            Assert.AreEqual(0, rig.Output.Live(VfxCue.FuseWarning));
        }

        [Test]
        public void SubscribesToEverySource_AndUnsubscribesOnDispose()
        {
            var fake = new FakeThrowableEventSource(ThrowableRules.Baseline(), null);
            var source = new ThrowableVfxSource(_bus, null, fake);
            source.Initialize();
            fake.Raise(E(ThrowableEventKind.Thrown, ThrowableKind.Banana, seconds: 1.5f));
            Assert.AreEqual(1, _capture.Explosions.Count);
            source.Dispose();
            fake.Raise(E(ThrowableEventKind.Thrown, ThrowableKind.Banana, id: 8, seconds: 1.5f));
            Assert.AreEqual(1, _capture.Explosions.Count);
        }

        [Test]
        public void OnlineSession_RelaysWhenNoOfflinePlayRuns()
        {
            using var harness = new LiveMatchHarness();
            harness.Start(2, 4);
            var online = new OnlineThrowableSession(harness.Match, null, harness.Network, harness.Network);
            online.Initialize();
            var source = new ThrowableVfxSource(_bus, null, online);
            source.Initialize();
            harness.Network.Push(new ThrowableEventPacket
            {
                MatchId = harness.MatchId, Event = (byte)ThrowableEventKind.Thrown, Item = (byte)ThrowableKind.Banana, Actor = 1,
                Target = ThrowableEvent.NoTarget, Id = 3, ToX = 4, ToY = 4, Seconds = 1.5f, Size = 3
            });
            Assert.AreEqual(1, _capture.Explosions.Count);
            source.Dispose();
            online.Dispose();
        }
    }
}
