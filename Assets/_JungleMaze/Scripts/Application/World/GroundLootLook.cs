using JungleMaze.World.Inventory;

namespace JungleMaze.App.World
{
    /// <summary>Which art and size rule draws a ground item.</summary>
    public enum GroundLootShape : byte
    {
        /// <summary>Melee weapon or gun: in-hand sprite at its HeldWeaponCatalog length.</summary>
        Held,
        /// <summary>Throwable: its flight sprite at the ThrowableArtCatalog flight size.</summary>
        Throwable,
        /// <summary>Armor, helmet, consumable or anything without weapon art: HUD icon at the icon size.</summary>
        Icon,
        /// <summary>AMMO box: smaller icon, small white halo, no light column.</summary>
        Ammo
    }

    /// <summary>
    /// Look rules for ground items (Docs/hvu/07_Floor_Loot.vi.md section 7). Decides the shape from the item kind and the art
    /// that exists; sizes, colours and timings live in the presentation tuning asset. Pure C#.
    /// </summary>
    public static class GroundLootLook
    {
        /// <summary>
        /// AMMO → <see cref="GroundLootShape.Ammo"/>; a weapon with throwable art → Throwable (the in-hand catalog also lists
        /// throwables, the flight art is what lies on the floor); a weapon with in-hand art → Held; everything else → Icon.
        /// Unknown items (null definition) use the art that exists, then the icon.
        /// </summary>
        public static GroundLootShape ShapeOf(ItemDefinition item, bool hasHeldArt, bool hasThrowableArt)
        {
            if (item != null && item.Kind == ItemKind.Ammo) return GroundLootShape.Ammo;
            var weapon = item == null || item.Kind == ItemKind.Weapon;
            if (weapon && hasThrowableArt) return GroundLootShape.Throwable;
            if (weapon && hasHeldArt) return GroundLootShape.Held;
            return GroundLootShape.Icon;
        }

        /// <summary>Rarity glow and light column for everything except ammo (which only gets a small white halo).</summary>
        public static bool HasRarityLight(GroundLootShape shape) => shape != GroundLootShape.Ammo;

        /// <summary>Rarity of a catalog item; unknown items glow as Common.</summary>
        public static ItemRarity RarityOf(ItemDefinition item) => item != null ? item.Rarity : ItemRarity.Common;
    }
}
