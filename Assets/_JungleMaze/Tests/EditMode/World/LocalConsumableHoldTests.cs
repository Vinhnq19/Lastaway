using JungleMaze.App.Hud.State;
using JungleMaze.App.World;
using JungleMaze.Core.Events;
using JungleMaze.Infrastructure.Config;
using JungleMaze.World.Inventory;
using NUnit.Framework;
using UnityEngine;

namespace JungleMaze.Tests.World
{
    public sealed class LocalConsumableHoldTests
    {
        [SetUp] public void SetUp() => LocalConsumableHold.Release();
        [TearDown] public void TearDown() => LocalConsumableHold.Release();

        [Test] public void ConsumableKeyHoldsTheItemAndTheSameKeyPutsItAway()
        {
            var state = new InventorySnapshot { ActiveWeapon = 0 };
            state.Consumables[1] = new ItemStack("GOD_LEAF", 3);
            Assert.IsFalse(LocalConsumableHold.Toggle(0, state), "an empty slot cannot be held");
            Assert.IsTrue(LocalConsumableHold.Toggle(1, state));
            Assert.AreEqual(1, LocalConsumableHold.Slot);
            Assert.AreEqual("GOD_LEAF", LocalConsumableHold.ItemId);
            LocalConsumableHold.Toggle(1, state);
            Assert.IsFalse(LocalConsumableHold.Active);
        }

        [Test] public void HoldEndsWhenTheStackRunsOutAndQuickbarFollowsIt()
        {
            var balance = new BalanceConfigProvider(Application.streamingAssetsPath, null); var bus = new EventBus();
            var inventory = new InventoryStateStore(bus, balance); var hud = new HudStateStore(bus, balance);
            var binding = new InventoryHudBinding(inventory, hud, false);
            var state = new InventorySnapshot { ActiveWeapon = 0 };
            state.Weapons[0] = new ItemStack("AXE"); state.Consumables[2] = new ItemStack("SPIRIT_TEAR", 1);
            binding.Apply(state, 0);
            Assert.AreEqual(0, inventory.ActiveSlot);
            LocalConsumableHold.Toggle(2, state);
            Assert.AreEqual(6, inventory.ActiveSlot, "the held consumable is the highlighted quickbar slot");
            state.Consumables[2] = default(ItemStack);
            binding.Apply(state, 1);
            Assert.IsFalse(LocalConsumableHold.Active, "using the last one empties the hand");
            Assert.AreEqual(0, inventory.ActiveSlot, "the weapon is highlighted again");
            binding.Reset();
        }
    }
}
