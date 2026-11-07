using System.Linq;
using JungleBackend.Protocol.Gameplay;
using JungleMaze.App.World;
using JungleMaze.Presentation.Rendering;
using JungleMaze.Presentation.UI.Hud;
using JungleMaze.Presentation.World.Loot;
using JungleMaze.World.Inventory;
using NUnit.Framework;
using UnityEngine;

namespace JungleMaze.Tests.World
{
    /// <summary>Ground loot look: shape and size per item, rarity light, hover/launch curves and pooled views.</summary>
    public sealed class GroundLootViewTests
    {
        private static ItemDefinition Def(ItemKind kind, ItemRarity rarity = ItemRarity.Common) => new ItemDefinition { Id = "X", Kind = kind, Rarity = rarity };

        [Test]
        public void ShapeFollowsKindAndArt()
        {
            Assert.AreEqual(GroundLootShape.Ammo, GroundLootLook.ShapeOf(Def(ItemKind.Ammo), true, true));
            Assert.AreEqual(GroundLootShape.Throwable, GroundLootLook.ShapeOf(Def(ItemKind.Weapon), true, true), "flight art wins for throwables");
            Assert.AreEqual(GroundLootShape.Held, GroundLootLook.ShapeOf(Def(ItemKind.Weapon), true, false));
            Assert.AreEqual(GroundLootShape.Icon, GroundLootLook.ShapeOf(Def(ItemKind.Weapon), false, false));
            Assert.AreEqual(GroundLootShape.Icon, GroundLootLook.ShapeOf(Def(ItemKind.Armor), true, true), "armor never uses weapon art");
            Assert.AreEqual(GroundLootShape.Icon, GroundLootLook.ShapeOf(Def(ItemKind.Consumable), false, false));
            Assert.IsFalse(GroundLootLook.HasRarityLight(GroundLootShape.Ammo));
            Assert.IsTrue(GroundLootLook.HasRarityLight(GroundLootShape.Icon));
            Assert.AreEqual(ItemRarity.Common, GroundLootLook.RarityOf(null));
            Assert.AreEqual(ItemRarity.Epic, GroundLootLook.RarityOf(Def(ItemKind.Weapon, ItemRarity.Epic)));
        }

        [Test]
        public void BobStaysWithinAmplitude_AndPhaseDiffersPerId()
        {
            for (var t = 0.0; t < 3.2; t += .05)
                Assert.LessOrEqual(Mathf.Abs(GroundLootMotion.Bob(t, 7, .08f, 1.6f)), .08f + 1e-6f);
            Assert.AreEqual(GroundLootMotion.Bob(.3, 7, .08f, 1.6f), GroundLootMotion.Bob(1.9, 7, .08f, 1.6f), 1e-5f, "1.6 s period");
            Assert.AreNotEqual(GroundLootMotion.Phase(7), GroundLootMotion.Phase(8));
        }

        [Test]
        public void FlightArcs_EasesOut_AndLandsOnTheItem()
        {
            const float h = .8f, d = .45f, bounce = .1f, share = .22f;
            var start = GroundLootMotion.Flight(0, 10, 10, 11, 10, h, d, bounce, share);
            Assert.AreEqual(10f, start.X, 1e-5f); Assert.AreEqual(0f, start.Lift, 1e-5f);
            var main = d * (1 - share);
            var half = GroundLootMotion.Flight(main * .5f, 10, 10, 11, 10, h, d, bounce, share);
            Assert.Greater(half.X, 10.5f, "ease-out: more than half way at half time");
            var peak = Enumerable.Range(0, 100).Max(i => GroundLootMotion.Flight(main * i / 100f, 10, 10, 11, 10, h, d, bounce, share).Lift);
            Assert.AreEqual(h, peak, .01f, "arc about 0.8 cells high");
            var hop = GroundLootMotion.Flight(main + (d - main) * .5f, 10, 10, 11, 10, h, d, bounce, share);
            Assert.AreEqual(11f, hop.X, 1e-5f); Assert.AreEqual(bounce, hop.Lift, 1e-4f, "small bounce on landing");
            var end = GroundLootMotion.Flight(d, 10, 10, 11, 10, h, d, bounce, share);
            Assert.IsTrue(end.Landed); Assert.AreEqual(0f, end.Lift);
        }

        [Test]
        public void TuningAsset_ColoursEachRarity_AndRarerColumnsAreTaller()
        {
            var tuning = Resources.Load<GroundLootTuning>(GroundLootTuning.ResourceName);
            Assert.IsNotNull(tuning, "run Jungle Maze/Loot/Build Ground Loot Assets");
            Assert.IsNotNull(tuning.Halo); Assert.IsNotNull(tuning.Beam); Assert.IsNotNull(tuning.Sparkle);
            Assert.IsNotNull(tuning.HeldArt); Assert.IsNotNull(tuning.ThrowableArt); Assert.IsNotNull(tuning.AmmoBox);
            Assert.IsNotNull(tuning.LightMaterial);
            Assert.AreEqual("Universal Render Pipeline/2D/Sprite-Unlit-Default", tuning.LightMaterial.shader.name, "valid URP 2D shader (never pink)");
            var styles = System.Enum.GetValues(typeof(ItemRarity)).Cast<ItemRarity>().Select(tuning.StyleOf).ToArray();
            CollectionAssert.AreEqual(System.Enum.GetValues(typeof(ItemRarity)), styles.Select(s => s.Rarity).ToArray(), "one row per rarity");
            for (var i = 1; i < styles.Length; i++) Assert.Greater(styles[i].BeamHeight, styles[i - 1].BeamHeight, "taller column for rarer items");
            Color common = styles[0].Glow, uncommon = styles[1].Glow, rare = styles[2].Glow, epic = styles[3].Glow, legendary = styles[4].Glow;
            Assert.That(Mathf.Min(common.r, common.g, common.b), Is.GreaterThan(.9f), "Common white");
            Assert.That(uncommon.g, Is.GreaterThan(uncommon.r + .3f).And.GreaterThan(uncommon.b + .3f), "Uncommon green");
            Assert.That(rare.b, Is.GreaterThan(rare.r + .3f).And.GreaterThan(rare.g), "Rare blue");
            Assert.That(epic.r, Is.GreaterThan(epic.g + .2f)); Assert.That(epic.b, Is.GreaterThan(epic.g + .2f), "Epic purple");
            Assert.That(legendary.r, Is.GreaterThan(legendary.g).And.GreaterThan(legendary.b + .5f), "Legendary gold-orange");
            Assert.AreEqual(0, styles[0].Sparkles + styles[1].Sparkles + styles[2].Sparkles);
            Assert.Greater(styles[3].Sparkles, 0); Assert.Greater(styles[4].Sparkles, 0);
        }

        private sealed class Icons : IItemIconProvider
        {
            public readonly Sprite Sprite;
            public Icons(Sprite sprite) => Sprite = sprite;
            public Sprite GetIcon(string itemId) => Sprite;
        }

        private static WorldObjectWire Item(ushort id, string item, float x = 10, float y = 10) =>
            new WorldObjectWire { Id = id, Kind = 100, X = x, Y = y, Width = .35f, Height = .35f, ItemId = item, Count = 1 };

        [Test]
        public void Layer_SizesByArt_PoolsViews_AndLaunchesFromABrokenChest()
        {
            var tuning = Resources.Load<GroundLootTuning>(GroundLootTuning.ResourceName);
            Assert.IsNotNull(tuning);
            var texture = new Texture2D(40, 20);
            var icon = Sprite.Create(texture, new Rect(0, 0, 40, 20), new Vector2(.5f, .5f), 20); // 2 x 1 units
            var root = new GameObject("loot test root");
            try
            {
                var catalog = ItemCatalog.CreateBaseline();
                var layer = new GroundLootLayer(root.transform, tuning, new Icons(icon), catalog, null);

                // Sizes: gun at its in-hand length, throwable at its flight size, armor icon ~0.8, ammo ~0.55.
                Assert.IsTrue(tuning.HeldArt.TryGet("SHOTGUN", out var shotgun));
                var gun = layer.ResolveArt("SHOTGUN");
                Assert.AreEqual(GroundLootShape.Held, gun.Shape);
                Assert.AreEqual(shotgun.Scale * tuning.HeldScale, gun.Scale, 1e-5f);
                Assert.AreEqual(shotgun.AngleOffset + tuning.HeldAngle, gun.Angle, 1e-5f);
                Assert.AreEqual(ItemRarity.Epic, gun.Rarity);
                Assert.IsTrue(tuning.ThrowableArt.TryGet("BANANA", out var banana));
                var thrown = layer.ResolveArt("BANANA");
                Assert.AreEqual(GroundLootShape.Throwable, thrown.Shape);
                Assert.AreEqual(banana.Flight.ScaleFor(banana.FlightSize * tuning.ThrowableScale), thrown.Scale, 1e-5f);
                Assert.AreEqual(GroundLootShape.Icon, layer.ResolveArt("ROPE").Shape, "a 5:1 rope stroke falls back to the HUD icon");
                var armor = layer.ResolveArt("WOODEN_ARMOR");
                Assert.AreEqual(GroundLootShape.Icon, armor.Shape);
                Assert.AreEqual(tuning.IconSize, 2f * armor.Scale, 1e-4f, "longest side = icon size");
                var ammo = layer.ResolveArt("AMMO");
                Assert.AreEqual(GroundLootShape.Ammo, ammo.Shape);
                Assert.AreEqual(tuning.AmmoSize, 2f * ammo.Scale, 1e-4f);

                var chest = new WorldObjectWire { Id = 1, Kind = 5, X = 10, Y = 10, Width = 1, Height = 1 };
                layer.Apply(new[] { chest, Item(10, "SHOTGUN", 20, 20), Item(11, "AMMO", 21, 20) }, 0);
                Assert.AreEqual(2, layer.ActiveCount);
                Assert.IsTrue(layer.TryGetView(10, out var view10));
                Assert.AreEqual(GroundLootEntry.Appear, view10.Entry, "items present at match start fade in");
                Assert.IsTrue(view10.HasBeam, "Epic shotgun has a column");
                Assert.AreEqual(tuning.StyleOf(ItemRarity.Epic).Sparkles, view10.SparkleCount);
                Assert.IsTrue(layer.TryGetView(11, out var view11));
                Assert.IsFalse(view11.HasBeam, "ammo: small white halo only");
                Assert.AreEqual(SortingLayers.IdOf(SortingLayers.WorldDynamic), view10.Body.sortingLayerID, "same Y-sorted layer as chests and heroes");
                Assert.AreEqual(DepthSortingSystem.ComputeOrder(20, DepthSortingSystem.DefaultPrecision), view10.Body.sortingOrder, "Y-sorted at its ground point");
                Assert.AreEqual(0, view10.gameObject.layer, "GameObject layer Default");

                // Fade-in: transparent at first, opaque after FadeSeconds.
                view10.Render(0);
                Assert.AreEqual(0f, view10.ItemRenderer.color.a, 1e-4f);
                view10.Render(tuning.FadeSeconds + .01f);
                Assert.AreEqual(1f, view10.ItemRenderer.color.a, 1e-4f);

                // Repeating the snapshot reuses the same views; nothing new is created.
                var created = layer.Created;
                for (var i = 0; i < 5; i++) layer.Apply(new[] { chest, Item(10, "SHOTGUN", 20, 20), Item(11, "AMMO", 21, 20) }, .1 + i * .03);
                Assert.AreEqual(created, layer.Created);
                Assert.IsTrue(layer.TryGetView(10, out var same) && ReferenceEquals(same, view10));

                // Picking the shotgun up returns its view to the pool (inactive, not destroyed).
                layer.Apply(new[] { chest, Item(11, "AMMO", 21, 20) }, 1);
                Assert.AreEqual(1, layer.ActiveCount); Assert.AreEqual(1, layer.PooledCount);
                Assert.IsTrue(view10 != null && !view10.gameObject.activeSelf);

                // The chest breaks and two items appear next to it: the pooled view is reused for a launch.
                chest.Flags = 1;
                layer.Apply(new[] { chest, Item(11, "AMMO", 21, 20), Item(12, "KNIFE", 11, 10), Item(13, "GOD_LEAF", 10, 11.2f) }, 2);
                Assert.AreEqual(created + 1, layer.Created, "one new view, one reused");
                Assert.IsTrue(layer.TryGetView(12, out var knife));
                Assert.IsTrue(layer.TryGetView(13, out var leaf));
                Assert.AreEqual(GroundLootEntry.Launch, knife.Entry); Assert.AreEqual(GroundLootEntry.Launch, leaf.Entry);
                knife.Render(2);
                Assert.AreEqual(10f, knife.transform.position.x, 1e-4f, "starts at the chest center");
                knife.Render(2 + tuning.LaunchSeconds * .3f);
                Assert.Greater(knife.ItemRenderer.transform.localPosition.y, tuning.FloatHeight + .3f, "in the air");
                knife.Render(2 + tuning.LaunchSeconds + tuning.LightFadeSeconds + .01f);
                Assert.AreEqual(11f, knife.transform.position.x, 1e-4f, "landed on its authoritative position");
                Assert.Greater(knife.HaloRenderer.color.a, 0f, "glow after landing");

                layer.Clear();
                Assert.AreEqual(0, layer.ActiveCount);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(icon);
                Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void Layer_SelectHighlightsOnlyThePickupTarget_AndForgetsPickedItems()
        {
            var tuning = Resources.Load<GroundLootTuning>(GroundLootTuning.ResourceName);
            Assert.IsNotNull(tuning);
            var root = new GameObject("loot select test root");
            try
            {
                var layer = new GroundLootLayer(root.transform, tuning, null, ItemCatalog.CreateBaseline(), null);
                layer.Apply(new[] { Item(10, "SHOTGUN", 20, 20), Item(11, "KNIFE", 21, 20) }, 0);
                Assert.IsTrue(layer.TryGetView(10, out var gun));
                Assert.IsTrue(layer.TryGetView(11, out var knife));
                gun.Render(1);
                var normalHalo = gun.HaloRenderer.transform.localScale.x;
                var normalGlow = gun.HaloRenderer.color.a;

                layer.Select(10);
                Assert.AreEqual(10, layer.SelectedId);
                Assert.IsTrue(gun.Selected); Assert.IsFalse(knife.Selected);
                gun.Render(1 + tuning.SelectedBlendSeconds + .01);
                Assert.AreEqual(1f, gun.SelectBlend, 1e-4f);
                Assert.Greater(gun.HaloRenderer.transform.localScale.x, normalHalo * 1.15f, "larger halo on the F target");
                Assert.Greater(gun.HaloRenderer.color.a, normalGlow, "brighter glow on the F target");

                layer.Select(11);
                Assert.IsFalse(gun.Selected); Assert.IsTrue(knife.Selected);

                // Picked up: the view goes back to the pool and is not highlighted when it is reused.
                layer.Apply(new[] { Item(10, "SHOTGUN", 20, 20) }, 2);
                Assert.AreEqual(0, layer.SelectedId);
                layer.Apply(new[] { Item(10, "SHOTGUN", 20, 20), Item(12, "AXE", 22, 20) }, 3);
                Assert.IsTrue(layer.TryGetView(12, out var axe));
                Assert.IsFalse(axe.Selected);
                layer.Select(0);
                Assert.IsFalse(gun.Selected);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
