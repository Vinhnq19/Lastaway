using System;
using System.Collections.Generic;

namespace JungleMaze.Presentation.UI.Hud
{
    /// <summary>
    /// Vietnamese item names shown by the UI (pickup toast and list, bag tooltip, kill feed), T-022 R3 (CPO 07/11).
    /// Source: GDD sheet "ITEM LIST" (column "Item name cũ"), matched to ids by its Ref column (Weapons › n,
    /// Equipments › n = order of balance/weapons.json, equipments.json). Display only: the ITM data (English
    /// displayName, Quang Huy) is not changed. Read with <c>python -I Tools/read_gdd.py "ITEM LIST"</c> when the sheet moves.
    /// </summary>
    public static class HudItemNames
    {
        private static readonly Dictionary<string, string> Names = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            // Weapons › 1..18
            { "HAND", "Tay không" }, { "TREE_BRANCH", "Cành cây" }, { "KNIFE", "Dao" }, { "BONE", "Xương" },
            { "AXE", "Rìu" }, { "MACE", "Chuỳ gai" }, { "RUBBER_GUN", "Ná cao su" }, { "SCEPTER", "Quyền trượng" },
            { "CROSSBOW", "Nỏ" }, { "SHOTGUN", "Shotgun" }, { "PISTOL", "Súng lục" },
            { "ASSAULT_RIFLE", "Súng trường" }, { "SNIPER_RIFLE", "Súng ngắm" }, { "BANANA", "Chuối" }, { "SMOKE", "Bom khói" },
            { "METEORITE_FRAGMENT", "Mảnh thiên thạch" }, { "CRYSTAL", "Tinh thể" }, { "SEED", "Hạt" }, { "ROPE", "Dây thừng" },
            { "BELL", "Chuông" }, { "SAHUR_BAT", "Gậy đánh bóng" },
            // Weapons › Guns
            { "AMMO", "Đạn" },
            // Equipments › 1..16
            { "WOODEN_HELMET", "Mũ gỗ" }, { "SPIKE_HELMET", "Mũ gai" }, { "MAGIC_HELMET", "Mũ phù thủy" },
            { "WOODEN_ARMOR", "Giáp gỗ" }, { "ARMOR", "Giáp gai" }, { "MAGIC_ARMOR", "Giáp phù thủy" },
            { "GOD_LEAF", "Lá thần mộc" }, { "SPIRIT_TEAR", "Nước mắt tinh linh" }, { "MERMAID_TAIL", "Vảy đuôi tiên cá" },
            { "PHOENIX_FEATHERS", "Lông vũ phượng hoàng lửa" }, { "UNICORN_BLOOD", "Máu kỳ lân" }, { "ADRENALINE", "Nước tăng lực" },
            { "ELIXIR", "Thần dược" }, { "MAGIC", "Gậy thần" }, { "ELIXIR_OF_LIFE", "Tiên dược sự sống" }, { "COMPASS", "La bàn" },
        };

        public static int Count => Names.Count;

        /// <summary>Vietnamese name of <paramref name="itemId"/>, or <paramref name="fallback"/> (the data name) when unknown.</summary>
        public static string Get(string itemId, string fallback = null) =>
            itemId != null && Names.TryGetValue(itemId, out var name) ? name : fallback;

        public static bool Has(string itemId) => itemId != null && Names.ContainsKey(itemId);
    }
}
