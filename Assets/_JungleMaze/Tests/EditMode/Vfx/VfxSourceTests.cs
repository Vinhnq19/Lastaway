using System;
using System.Collections.Generic;
using JungleMaze.App.Hud.State;
using JungleMaze.App.Vfx;
using JungleMaze.App.Vfx.Mocks;
using JungleMaze.Core.Events;
using JungleMaze.Domain.CombatFeedback;
using JungleMaze.Domain.Hud;
using JungleMaze.Domain.Vfx;
using JungleMaze.Tests.MatchUi;
using NUnit.Framework;

namespace JungleMaze.Tests.Vfx
{
    /// <summary>T-6: the local effect store drives auras on the own body (read-only, own net id only).</summary>
    public sealed class LocalEffectStatusSourceTests
    {
        [Test]
        public void AddedRefreshedRemoved_BecomeStatusEventsForTheOwnNetId()
        {
            var bus = new EventBus();
            var seen = new List<StatusEvent>();
            bus.Subscribe<StatusEvent>(seen.Add);
            var store = new EffectStateStore(bus);
            var feed = new FakeMatchFeed { OwnNetId = 3 };
            var source = new LocalEffectStatusSource(store, bus, feed);
            source.Initialize();

            store.Apply(EffectKind.Stun, 2f, 2f);
            store.Apply(EffectKind.Stun, 2f, 1.5f);
            store.Remove(EffectKind.Stun);

            Assert.AreEqual(3, seen.Count);
            Assert.IsTrue(seen[0].Applied);
            Assert.AreEqual(3, seen[0].NetId);
            Assert.AreEqual(StatusKind.Stun, seen[0].Kind);
            Assert.AreEqual(1.5f, seen[1].RemainingSeconds);
            Assert.IsFalse(seen[2].Applied);
            source.Dispose();
            store.Apply(EffectKind.Poison, 3f, 3f);
            Assert.AreEqual(3, seen.Count, "unsubscribed");
        }

        [Test]
        public void NotInGame_OrUnknownOwnId_Nothing()
        {
            var bus = new EventBus();
            var seen = new List<StatusEvent>();
            bus.Subscribe<StatusEvent>(seen.Add);
            var store = new EffectStateStore(bus);
            var feed = new FakeMatchFeed { OwnNetId = 255 };
            var source = new LocalEffectStatusSource(store, bus, feed);
            source.Initialize();
            store.Apply(EffectKind.Stun, 2f, 2f);
            feed.OwnNetId = 0;
            feed.InGame = false;
            store.Apply(EffectKind.Poison, 2f, 2f);
            Assert.AreEqual(0, seen.Count);
        }

        [Test]
        public void ThroughTheDirector_OwnStunShowsAura()
        {
            var rig = new VfxRig();
            var store = new EffectStateStore(rig.Bus);
            var source = new LocalEffectStatusSource(store, rig.Bus, rig.Feed);
            source.Initialize();
            store.Apply(EffectKind.Stun, 2f, 2f);
            Assert.AreEqual(1, rig.Output.Live(VfxCue.AuraStun));
            store.Remove(EffectKind.Stun);
            Assert.AreEqual(0, rig.Output.Live(VfxCue.AuraStun));
        }
    }

    /// <summary>The match feed: mock first, then offline, then the online session.</summary>
    public sealed class VfxMatchFeedTests
    {
        [Test]
        public void Online_InGame_OwnIdAndMatchId()
        {
            using var harness = new LiveMatchHarness();
            var feed = new VfxMatchFeed(harness.Match, null);
            Assert.IsFalse(feed.InGame);
            harness.Start(2, 4);
            Assert.IsTrue(feed.InGame);
            Assert.IsTrue(feed.Online);
            Assert.AreEqual(2, feed.OwnNetId);
            Assert.AreEqual(harness.MatchId, feed.SessionKey);
        }

        [Test]
        public void ActiveMock_TakesPrecedence()
        {
            using var harness = new LiveMatchHarness();
            harness.Start(2, 4);
            var mock = new MockVfxMatch();
            var feed = new VfxMatchFeed(harness.Match, null, mock, () => mock.Active);
            Assert.AreEqual(harness.MatchId, feed.SessionKey);
            mock.Active = true;
            Assert.AreEqual(mock.SessionKey, feed.SessionKey);
            Assert.AreEqual(0, feed.OwnNetId);
            Assert.IsFalse(feed.Online);
            var before = feed.SessionKey;
            mock.NewSession();
            Assert.AreNotEqual(before, feed.SessionKey);
        }

        [Test]
        public void NoSessions_NotInGame_UnknownOwnId()
        {
            var feed = new VfxMatchFeed(null, null);
            Assert.IsFalse(feed.InGame);
            Assert.AreEqual(255, feed.OwnNetId);
            Assert.AreEqual(Guid.Empty, feed.SessionKey);
        }
    }
}
