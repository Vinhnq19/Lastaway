using System.Collections.Generic;
using JungleMaze.Infrastructure.Config;
using JungleMaze.Presentation.UI.Hud;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace JungleMaze.Tests.EditMode.Hud
{
    /// <summary>T-022 R3: every item the UI can show has its Vietnamese name (GDD ITEM LIST), data names untouched.</summary>
    public sealed class HudItemNamesTests
    {
        [Test]
        public void EveryIconCatalogId_HasAVietnameseName()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<HudItemIconCatalog>("Assets/_JungleMaze/Prefabs/UI/HudItemIconCatalog.asset");
            Assert.NotNull(catalog);
            Assert.Greater(catalog.Entries.Length, 0);
            var missing = new List<string>();
            foreach (var e in catalog.Entries) if (!HudItemNames.Has(e.Id)) missing.Add(e.Id);
            Assert.IsEmpty(missing, string.Join(", ", missing));
        }

        [Test]
        public void EveryBalanceItemAndAmmo_HasAVietnameseName()
        {
            var balance = new BalanceConfigProvider(Application.streamingAssetsPath, null);
            Assert.IsNull(balance.LastError);
            var missing = new List<string>();
            foreach (var w in balance.Weapons) if (!HudItemNames.Has(w.Id)) missing.Add(w.Id);
            foreach (var e in balance.Equipments) if (!HudItemNames.Has(e.Id)) missing.Add(e.Id);
            Assert.IsEmpty(missing, string.Join(", ", missing));
            Assert.AreEqual("Đạn", HudItemNames.Get("AMMO"));
            Assert.AreEqual(35, HudItemNames.Count, "35 rows of ITEM LIST");
        }

        [Test]
        public void KnownIdsMapToGddNames_UnknownFallsBack()
        {
            Assert.AreEqual("Lá thần mộc", HudItemNames.Get("GOD_LEAF"));
            Assert.AreEqual("Ná cao su", HudItemNames.Get("RUBBER_GUN"));
            Assert.AreEqual("Nước tăng lực", HudItemNames.Get("ADRENALINE"));
            Assert.AreEqual("Data Name", HudItemNames.Get("NOT_AN_ITEM", "Data Name"));
            Assert.IsNull(HudItemNames.Get(null));
        }
    }
}
