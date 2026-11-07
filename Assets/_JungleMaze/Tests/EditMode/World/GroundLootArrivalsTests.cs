using System.Collections.Generic;
using System.Linq;
using JungleBackend.Protocol.Gameplay;
using JungleMaze.App.World;
using NUnit.Framework;

namespace JungleMaze.Tests.World
{
    /// <summary>Snapshot-only detection of loot bursting out of a broken container (Docs/hvu/07_Floor_Loot.vi.md section 5).</summary>
    public sealed class GroundLootArrivalsTests
    {
        private static WorldObjectWire Container(ushort id, byte kind, float x, float y, bool broken) =>
            new WorldObjectWire { Id = id, Kind = kind, X = x, Y = y, Width = 1, Height = 1, Flags = (byte)(broken ? 1 : 0) };

        private static WorldObjectWire Item(ushort id, float x, float y, string item = "AXE") =>
            new WorldObjectWire { Id = id, Kind = 100, X = x, Y = y, Width = .35f, Height = .35f, ItemId = item, Count = 1 };

        private static List<GroundLootArrival> Observe(GroundLootArrivals tracker, double now, params WorldObjectWire[] states)
        {
            var output = new List<GroundLootArrival>();
            tracker.Observe(states, now, output);
            return output;
        }

        [Test]
        public void FirstSnapshot_EveryItemJustAppears()
        {
            var tracker = new GroundLootArrivals();
            // Even a container that is already broken in the first snapshot does not launch anything.
            var arrivals = Observe(tracker, 0, Container(1, 5, 10, 10, true), Item(10, 10.5f, 10), Item(11, 30, 30));
            Assert.AreEqual(2, arrivals.Count);
            Assert.IsTrue(arrivals.All(a => a.Entry == GroundLootEntry.Appear));
            Assert.AreEqual(30f, arrivals.Single(a => a.Id == 11).FromX);
        }

        [Test]
        public void ItemsNearAChestThatJustBroke_LaunchFromItsCenter_WithStagger()
        {
            var tracker = new GroundLootArrivals(.5f, 2f, .05f);
            Observe(tracker, 0, Container(1, 5, 10, 10, false));
            var arrivals = Observe(tracker, 1, Container(1, 5, 10, 10, true), Item(10, 11f, 10), Item(11, 10, 11.2f), Item(12, 9, 9.2f), Item(13, 20, 20));
            var launched = arrivals.Where(a => a.Entry == GroundLootEntry.Launch).OrderBy(a => a.Delay).ToList();
            Assert.AreEqual(3, launched.Count, "three drops within 2 cells");
            CollectionAssert.AreEqual(new[] { 0f, .05f, .1f }, launched.Select(a => a.Delay).ToArray());
            Assert.IsTrue(launched.All(a => a.FromX == 10f && a.FromY == 10f), "from the container center");
            Assert.AreEqual(GroundLootEntry.Appear, arrivals.Single(a => a.Id == 13).Entry, "a far item only fades in");
        }

        [Test]
        public void AmmoNextToALaunchedGun_LaunchesToo_EvenBeyondTheRadius()
        {
            var tracker = new GroundLootArrivals(.5f, 2f, .05f, 1.25f);
            Observe(tracker, 0, Container(1, 5, 10, 10, false), Item(20, 30, 30, "KNIFE"));
            // Gun on the drop ring 1.4 cells out, its AMMO box 1.1 cells further (2.5 from the chest); a lone far item stays.
            var arrivals = Observe(tracker, .2, Container(1, 5, 10, 10, true), Item(20, 30, 30, "KNIFE"),
                Item(10, 11.4f, 10, "SHOTGUN"), Item(11, 12.5f, 10, "AMMO"), Item(12, 13.9f, 10, "AMMO"));
            Assert.AreEqual(GroundLootEntry.Launch, arrivals.Single(a => a.Id == 10).Entry);
            var ammo = arrivals.Single(a => a.Id == 11);
            Assert.AreEqual(GroundLootEntry.Launch, ammo.Entry, "companion of the launched gun");
            Assert.AreEqual(10f, ammo.FromX); Assert.AreEqual(.05f, ammo.Delay, 1e-6f);
            Assert.AreEqual(GroundLootEntry.Appear, arrivals.Single(a => a.Id == 12).Entry, "not chained from another companion");
        }

        [Test]
        public void PotsAndSupplyCratesLaunchToo_ButNotOtherKinds()
        {
            var tracker = new GroundLootArrivals();
            Observe(tracker, 0, Container(1, 6, 10, 10, false), Container(2, 13, 30, 30, false), Container(3, 12, 50, 50, false));
            var arrivals = Observe(tracker, .1, Container(1, 6, 10, 10, true), Container(2, 13, 30, 30, true), Container(3, 12, 50, 50, true),
                Item(10, 11, 10), Item(11, 31, 30), Item(12, 51, 50));
            Assert.AreEqual(GroundLootEntry.Launch, arrivals.Single(a => a.Id == 10).Entry, "pot");
            Assert.AreEqual(GroundLootEntry.Launch, arrivals.Single(a => a.Id == 11).Entry, "supply crate");
            Assert.AreEqual(30f, arrivals.Single(a => a.Id == 11).FromX);
            Assert.AreEqual(GroundLootEntry.Appear, arrivals.Single(a => a.Id == 12).Entry, "a broken door drops nothing");
        }

        [Test]
        public void ItemsUpToHalfASecondLater_StillLaunch_LaterOnesFadeIn()
        {
            var tracker = new GroundLootArrivals(.5f, 2f, .05f);
            Observe(tracker, 0, Container(1, 5, 10, 10, false));
            Observe(tracker, 1, Container(1, 5, 10, 10, true), Item(10, 11, 10));
            var late = Observe(tracker, 1.4, Container(1, 5, 10, 10, true), Item(10, 11, 10), Item(11, 9, 10));
            Assert.AreEqual(1, late.Count);
            Assert.AreEqual(GroundLootEntry.Launch, late[0].Entry);
            Assert.AreEqual(.05f, late[0].Delay, 1e-6f, "continues the container's stagger");
            var tooLate = Observe(tracker, 1.6, Container(1, 5, 10, 10, true), Item(10, 11, 10), Item(11, 9, 10), Item(12, 10, 11));
            Assert.AreEqual(GroundLootEntry.Appear, tooLate.Single().Entry);
        }

        [Test]
        public void RepeatedSnapshots_NeverReportAnItemTwice()
        {
            var tracker = new GroundLootArrivals();
            Observe(tracker, 0, Container(1, 5, 10, 10, false));
            var states = new[] { Container(1, 5, 10, 10, true), Item(10, 11, 10), Item(11, 10, 11) };
            Assert.AreEqual(2, Observe(tracker, .1, states).Count);
            for (var i = 0; i < 5; i++) Assert.IsEmpty(Observe(tracker, .1 + i * .03, states), "same snapshot again");
            Assert.AreEqual(2, tracker.Count);
        }

        [Test]
        public void PickedUpItemThatComesBack_AndReusedIds_AreNewArrivals()
        {
            var tracker = new GroundLootArrivals();
            Observe(tracker, 0, Item(10, 5, 5, "AXE"));
            Assert.IsEmpty(Observe(tracker, 1, Item(10, 5, 5, "AXE")));
            Assert.IsEmpty(Observe(tracker, 2), "picked up");
            Assert.AreEqual(GroundLootEntry.Appear, Observe(tracker, 3, Item(10, 6, 6, "AXE")).Single().Entry, "dropped again");
            Assert.AreEqual(1, Observe(tracker, 4, Item(10, 6, 6, "KNIFE")).Count, "a practice reset reused the id for another item");
        }

        [Test]
        public void Reset_TreatsTheNextSnapshotAsTheFirst()
        {
            var tracker = new GroundLootArrivals();
            Observe(tracker, 0, Container(1, 5, 10, 10, false));
            tracker.Reset();
            var arrivals = Observe(tracker, .1, Container(1, 5, 10, 10, true), Item(10, 11, 10));
            Assert.AreEqual(GroundLootEntry.Appear, arrivals.Single().Entry, "new match: no stale break");
        }
    }
}
