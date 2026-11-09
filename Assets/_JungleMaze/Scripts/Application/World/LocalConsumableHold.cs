using System;
using JungleMaze.World.Inventory;

namespace JungleMaze.App.World
{
    /// <summary>
    /// The consumable the local player holds in hand (client side, 08/11). A consumable key (5-8) takes that item in
    /// hand instead of using it at once; right click uses it, Q drops it, a weapon key or the same key puts it away.
    /// The server still only receives Use / Drop of a slot and keeps the selected weapon, so other players keep seeing
    /// that weapon. Cleared automatically when the held stack runs out.
    /// </summary>
    public static class LocalConsumableHold
    {
        /// <summary>Consumable slot 0..3 in hand, -1 when nothing is held.</summary>
        public static int Slot { get; private set; } = -1;
        public static string ItemId { get; private set; }
        public static bool Active => Slot >= 0;
        public static event Action Changed;

        /// <summary>Takes <paramref name="slot"/> in hand, or puts it away when it is already held. False when the slot is empty.</summary>
        public static bool Toggle(int slot, InventorySnapshot state)
        {
            if (Slot == slot) { Release(); return true; }
            if (state == null || slot < 0 || slot >= state.Consumables.Length || state.Consumables[slot].IsEmpty) return false;
            Set(slot, state.Consumables[slot].ItemId);
            return true;
        }

        public static void Release() => Set(-1, null);

        /// <summary>Follows the authoritative bag: the hold ends when its slot empties and tracks the item now in it.</summary>
        public static void Sync(InventorySnapshot state)
        {
            if (!Active) return;
            if (state == null || Slot >= state.Consumables.Length || state.Consumables[Slot].IsEmpty) { Release(); return; }
            var id = state.Consumables[Slot].ItemId;
            if (id != ItemId) Set(Slot, id);
        }

        private static void Set(int slot, string itemId)
        {
            if (Slot == slot && ItemId == itemId) return;
            Slot = slot;
            ItemId = itemId;
            Changed?.Invoke();
        }
    }
}
