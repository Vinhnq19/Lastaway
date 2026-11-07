using JungleBackend.Protocol.Gameplay;
using JungleMaze.App.World;
using JungleMaze.World.Inventory;
using NUnit.Framework;

namespace JungleMaze.Tests.World
{
    /// <summary>The F pickup list: reach, order, wheel selection, linger and window (Docs/hvu/07_Floor_Loot.vi.md section 11).</summary>
    public sealed class NearbyLootPickerTests
    {
        private static WorldObjectWire Item(ushort id, float x, float y, string item = "AXE", ushort count = 1) =>
            new WorldObjectWire { Id = id, Kind = 100, X = x, Y = y, Width = .35f, Height = .35f, ItemId = item, Count = count };

        private static WorldObjectWire Door(ushort id, float x, float y) =>
            new WorldObjectWire { Id = id, Kind = 12, X = x, Y = y, Width = 1, Height = 1, Hp = 10 };

        [Test]
        public void ListsOnlyGroundItemsInReach_NearestFirst_AndTargetsTheNearest()
        {
            var picker = new NearbyLootPicker(ItemCatalog.CreateBaseline());
            picker.Update(new[] { Item(1, 1f, 0), Door(2, .2f, 0), Item(3, .3f, 0), Item(4, 1.25f, 0), Item(5, 0, .6f) }, 0, 0, 0, 0);

            Assert.AreEqual(3, picker.Count);
            Assert.AreEqual(new ushort[] { 3, 5, 1 }, new[] { picker.Row(0).Id, picker.Row(1).Id, picker.Row(2).Id });
            Assert.AreEqual(3, picker.TargetId);
            Assert.AreEqual(0, picker.SelectedIndex);
            Assert.IsFalse(picker.ManualSelection);
        }

        [Test]
        public void NothingInReach_NoTarget()
        {
            var picker = new NearbyLootPicker();
            picker.Update(new[] { Item(1, 3f, 0) }, 0, 0, 1, 0);
            Assert.AreEqual(0, picker.Count);
            Assert.AreEqual(0, picker.TargetId);
            Assert.AreEqual(-1, picker.SelectedIndex);
        }

        [Test]
        public void Wheel_MovesSelectionAndWraps()
        {
            var picker = new NearbyLootPicker();
            var items = new[] { Item(1, .2f, 0), Item(2, .5f, 0), Item(3, .8f, 0) };
            picker.Update(items, 0, 0, 1, 0);
            Assert.AreEqual(2, picker.TargetId);
            picker.Update(items, 0, 0, 2, .1);
            Assert.AreEqual(1, picker.TargetId, "two steps down from the second row wraps to the first");
            picker.Update(items, 0, 0, -1, .2);
            Assert.AreEqual(3, picker.TargetId, "up from the first row wraps to the last");
            Assert.IsTrue(picker.ManualSelection);
        }

        [Test]
        public void WheelChoice_StaysByIdWhileInReach_EvenWhenOrderChanges()
        {
            var picker = new NearbyLootPicker();
            var items = new[] { Item(1, .2f, 0), Item(2, .8f, 0) };
            picker.Update(items, 0, 0, 1, 0);
            Assert.AreEqual(2, picker.TargetId);
            // Hero walks right: item 2 becomes the nearest, item 1 the farthest; the choice follows the Id.
            picker.Update(items, .7f, 0, 0, .1);
            Assert.AreEqual(2, picker.TargetId);
            Assert.AreEqual(0, picker.SelectedIndex);
            picker.Update(items, .3f, 0, 0, .2);
            Assert.AreEqual(2, picker.TargetId);
            Assert.AreEqual(1, picker.SelectedIndex);
        }

        [Test]
        public void WheelChoiceLeavesReach_FallsBackToNearest_AndFollowsItAgain()
        {
            var picker = new NearbyLootPicker(linger: 0f);
            picker.Update(new[] { Item(1, .2f, 0), Item(2, 1f, 0) }, 0, 0, 1, 0);
            Assert.AreEqual(2, picker.TargetId);
            picker.Update(new[] { Item(1, .2f, 0), Item(2, 1f, 0) }, -.5f, 0, 0, .1);
            Assert.AreEqual(1, picker.TargetId);
            Assert.IsFalse(picker.ManualSelection);
            // Without a wheel choice the selection follows whichever item is nearest now.
            picker.Update(new[] { Item(1, .2f, 0), Item(3, -.4f, 0) }, -.5f, 0, 0, .2);
            Assert.AreEqual(3, picker.TargetId);
        }

        [Test]
        public void ItemJustOutOfReach_LingersDimmedAndIsNotTargeted_ThenGoes()
        {
            var picker = new NearbyLootPicker(linger: .2f);
            var items = new[] { Item(1, 1.1f, 0) };
            picker.Update(items, 0, 0, 0, 0);
            Assert.AreEqual(1, picker.TargetId);
            picker.Update(items, -.2f, 0, 0, .1);
            Assert.AreEqual(1, picker.Count, "still listed at the reach edge, no flicker");
            Assert.IsFalse(picker.Row(0).InReach);
            Assert.AreEqual(0, picker.TargetId, "F never picks a lingering row");
            picker.Update(items, -.2f, 0, 0, .35);
            Assert.AreEqual(0, picker.Count);
        }

        [Test]
        public void PickedItem_LeavesTheListAtOnce()
        {
            var picker = new NearbyLootPicker(linger: 1f);
            picker.Update(new[] { Item(1, .2f, 0), Item(2, .4f, 0) }, 0, 0, 0, 0);
            picker.Update(new[] { Item(2, .4f, 0) }, 0, 0, 0, .05);
            Assert.AreEqual(1, picker.Count);
            Assert.AreEqual(2, picker.TargetId);
        }

        [Test]
        public void MoreThanMaxRows_WindowKeepsSelectionVisible_AndCountsTheRest()
        {
            var picker = new NearbyLootPicker(maxRows: 5);
            var items = new WorldObjectWire[7];
            for (var i = 0; i < items.Length; i++) items[i] = Item((ushort)(i + 1), .1f * (i + 1), 0);
            picker.Update(items, 0, 0, 0, 0);
            Assert.AreEqual(0, picker.WindowStart);
            Assert.AreEqual(5, picker.WindowCount);
            Assert.AreEqual(2, picker.HiddenCount);
            picker.Update(items, 0, 0, 6, .1);
            Assert.AreEqual(7, picker.TargetId);
            Assert.AreEqual(2, picker.WindowStart);
            Assert.AreEqual(5, picker.WindowCount);
        }

        [Test]
        public void Rows_CarryRarityAndCountRule_FromTheCatalog()
        {
            var picker = new NearbyLootPicker(ItemCatalog.CreateBaseline());
            picker.Update(new[] { Item(1, .1f, 0, "SHOTGUN"), Item(2, .2f, 0, "GOD_LEAF", 3), Item(3, .3f, 0, "BANANA", 2), Item(4, .4f, 0, "AMMO", 12) }, 0, 0, 0, 0);
            Assert.AreEqual(ItemRarity.Epic, picker.Row(0).Rarity);
            Assert.IsFalse(picker.Row(0).ShowsCount, "a gun shows no ×1");
            Assert.IsTrue(picker.Row(1).ShowsCount);
            Assert.IsTrue(picker.Row(2).ShowsCount, "throwables stack");
            Assert.IsTrue(picker.Row(3).ShowsCount);
            Assert.AreEqual(12, picker.Row(3).Count);
        }

        [Test]
        public void Clear_ForgetsSelection()
        {
            var picker = new NearbyLootPicker();
            var items = new[] { Item(1, .2f, 0), Item(2, .5f, 0) };
            picker.Update(items, 0, 0, 1, 0);
            picker.Clear();
            Assert.AreEqual(0, picker.Count);
            picker.Update(items, 0, 0, 0, .1);
            Assert.AreEqual(1, picker.TargetId);
        }
    }
}
