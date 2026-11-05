using System.Collections.Generic;
using JungleMaze.LevelEditor;
using JungleMaze.World;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

namespace JungleMaze.Tests.LevelEditor
{
    /// <summary>"Phá được" trong Level Editor: cover thành vật thể game có HP riêng và rơi loot khi vỡ.</summary>
    public sealed class LevelDestructibleTests
    {
        private static readonly LevelItemShape Barrel = new LevelItemShape(1, 1, new[] { new Rect(.1f, 0f, .8f, .35f) });

        private static LevelPlacement BreakableBarrel(int x, int y, ushort hp, string loot)
        {
            var p = LevelPlacement.OnCell("Barrel", LevelItemGroup.Cover, x, y, Barrel);
            p.Destructible = true;
            p.Hp = hp;
            p.Loot = loot;
            return p;
        }

        private static MapLayout Build(string json) =>
            JsonConvert.DeserializeObject<AuthoredMapDocument>(json).Build(LevelDocument.GameWorldVersion);

        /// <summary>Map 50×50 có đủ 10 spawn hợp lệ (vòng r=18) để game chịu Build.</summary>
        private static LevelDocument NewMap()
        {
            var doc = new LevelDocument();
            for (var i = 0; i < 10; i++)
            {
                var a = i * 36f * Mathf.Deg2Rad;
                doc.Placements.Add(LevelPlacement.OnCell("Spawn", LevelItemGroup.Spawn,
                    Mathf.FloorToInt(25 + 18 * Mathf.Cos(a)), Mathf.FloorToInt(25 + 18 * Mathf.Sin(a)), new LevelItemShape(1, 1, null)));
            }
            return doc;
        }

        [Test] public void DestructibleCoverExportsAsWorldObjectWithHpAndLootKind()
        {
            var doc = NewMap();
            doc.Placements.Add(BreakableBarrel(10, 12, 15, nameof(WorldObjectKind.Chest)));
            var root = JObject.Parse(doc.ToJson());

            Assert.AreEqual(0, ((JArray)root["obstacles"]).Count, "không còn là hard cover");
            var o = (JObject)((JArray)root["worldObjects"])[0];
            Assert.AreEqual("Chest", (string)o["kind"]);
            Assert.AreEqual(15, (int)o["hp"]);
            Assert.AreEqual("Barrel", (string)o["art"]);
            Assert.AreEqual(10f, (float)o["bounds"]["minX"], 1e-4f, "bounds = footprint để đánh trúng được");
            Assert.AreEqual(13f, (float)o["bounds"]["maxY"], 1e-4f);
        }

        [Test] public void DestructibleSettingsSurviveSaveAndReopen()
        {
            var doc = NewMap();
            doc.Placements.Add(BreakableBarrel(3, 4, 40, nameof(WorldObjectKind.Supply)));
            var chest = LevelPlacement.OnCell(nameof(WorldObjectKind.Chest), LevelItemGroup.WorldObject, 6, 6, new LevelItemShape(1, 1, null));
            chest.Hp = 70;
            doc.Placements.Add(chest);

            var again = LevelDocument.FromJson(doc.ToJson(), shape: id => id == "Barrel" ? Barrel : new LevelItemShape(1, 1, null));
            var barrel = again.Placements.Find(p => p.Type == "Barrel");
            Assert.IsTrue(barrel.Destructible);
            Assert.AreEqual(40, barrel.Hp);
            Assert.AreEqual(nameof(WorldObjectKind.Supply), barrel.Loot);
            Assert.AreEqual(70, again.Placements.Find(p => p.Type == nameof(WorldObjectKind.Chest)).Hp);
        }

        [Test] public void CustomHpIsUsedAndLootDropsWhenBroken()
        {
            var doc = NewMap();
            doc.Placements.Add(BreakableBarrel(10, 10, 15, nameof(WorldObjectKind.Pot)));
            var layout = Build(doc.ToJson());
            var barrel = layout.Objects.Find(o => o.Kind == WorldObjectKind.Pot);
            Assert.AreEqual(15, barrel.Hp);

            var loot = new LootSpy();
            var service = new DestructibleService(layout, new DestructibleConfig(), loot);
            Assert.IsTrue(service.TryGet(barrel.Id, out var state));
            Assert.AreEqual(15, state.Hp, "HP riêng thay cho PotHp mặc định (20)");

            Assert.IsTrue(service.DamageConfirmed(barrel.Id, 10, 1, 1));
            Assert.AreEqual(0, loot.Drops.Count);
            Assert.IsTrue(service.DamageConfirmed(barrel.Id, 10, 1, 2));
            Assert.IsTrue(service.IsBroken(barrel.Id));
            CollectionAssert.AreEqual(new[] { WorldObjectKind.Pot }, loot.Drops, "vỡ thì rơi loot theo bảng đã chọn");
        }

        [Test] public void ZeroHpKeepsDefaultHpAndMapHash()
        {
            var plain = NewMap();
            plain.Placements.Add(LevelPlacement.OnCell(nameof(WorldObjectKind.Pot), LevelItemGroup.WorldObject, 5, 5, new LevelItemShape(1, 1, null)));
            var json = plain.ToJson();
            Assert.IsNull(JObject.Parse(json)["worldObjects"][0]["hp"], "HP 0 không ghi ra file");

            var layout = Build(json);
            var service = new DestructibleService(layout, new DestructibleConfig(), new LootSpy());
            service.TryGet(layout.Objects[0].Id, out var state);
            Assert.AreEqual(new DestructibleConfig().PotHp, state.Hp);

            var withHp = JObject.Parse(json);
            withHp["worldObjects"][0]["hp"] = 33;
            Assert.AreNotEqual(layout.Hash, Build(withHp.ToString()).Hash, "HP riêng là một phần của map: client và server phải cùng hash");
        }

        [Test] public void HpOnIndestructibleObjectIsRejected()
        {
            var doc = NewMap();
            doc.Placements.Add(LevelPlacement.OnCell(nameof(WorldObjectKind.Bush), LevelItemGroup.WorldObject, 5, 5, new LevelItemShape(1, 1, null)));
            var root = JObject.Parse(doc.ToJson());
            root["worldObjects"][0]["hp"] = 10;
            Assert.Throws<System.ArgumentException>(() => Build(root.ToString()));
        }

        private sealed class LootSpy : IDestructibleLootSink
        {
            public readonly List<WorldObjectKind> Drops = new List<WorldObjectKind>();
            public void SpawnLoot(WorldObjectKind source, ushort worldId, WorldPoint position, uint seed) => Drops.Add(source);
        }
    }
}
