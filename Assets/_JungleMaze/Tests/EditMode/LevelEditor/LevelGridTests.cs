using System.Collections.Generic;
using JungleMaze.LevelEditor;
using JungleMaze.World;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

namespace JungleMaze.Tests.LevelEditor
{
    /// <summary>Đặt theo ô: footprint nguyên, collider chân, khối editor trong file, chiếm ô.</summary>
    public sealed class LevelGridTests
    {
        private static readonly Dictionary<string, LevelItemShape> Shapes = new Dictionary<string, LevelItemShape>
        {
            ["Tree01"] = new LevelItemShape(1, 1, new[] { new Rect(.375f, .1f, .25f, .2f) }),
            ["Bench"] = new LevelItemShape(3, 1, new[] { new Rect(0f, 0f, 1f, 1f) }),
            ["Gate"] = new LevelItemShape(2, 2, new[] { new Rect(0f, 0f, .5f, 1.4f), new Rect(1.5f, 0f, .5f, 1.4f) }),
            [nameof(WorldObjectKind.Portal)] = new LevelItemShape(1, 1, null),
            ["Spawn"] = new LevelItemShape(1, 1, null),
            ["Pebble"] = new LevelItemShape(1, 1, null),
        };

        private static LevelItemShape? Shape(string id) => Shapes.TryGetValue(id, out var s) ? s : (LevelItemShape?)null;

        private static LevelPlacement Make(string type, LevelItemGroup group, int x, int y) =>
            LevelPlacement.OnCell(type, group, x, y, Shape(type).Value);

        [Test] public void CoverExportsOneObstaclePerFootCollider()
        {
            var doc = new LevelDocument();
            doc.Placements.Add(Make("Tree01", LevelItemGroup.Cover, 10, 12));
            var obstacles = (JArray)JObject.Parse(doc.ToJson())["obstacles"];
            Assert.AreEqual(1, obstacles.Count);
            Assert.AreEqual(10.375f, (float)obstacles[0]["minX"], 1e-4f);
            Assert.AreEqual(12.1f, (float)obstacles[0]["minY"], 1e-4f);
            Assert.AreEqual(10.625f, (float)obstacles[0]["maxX"], 1e-4f);
            Assert.AreEqual(12.3f, (float)obstacles[0]["maxY"], 1e-4f);
        }

        [Test] public void MultiColliderCoverExportsEveryPart()
        {
            var doc = new LevelDocument();
            doc.Placements.Add(Make("Gate", LevelItemGroup.Cover, 4, 4));
            var obstacles = (JArray)JObject.Parse(doc.ToJson())["obstacles"];
            Assert.AreEqual(2, obstacles.Count, "hai chân cổng, lối giữa đi qua được");
            Assert.AreEqual(5.5f, (float)obstacles[1]["minX"], 1e-4f);
        }

        [Test] public void FlipMirrorsCollidersInsideFootprint()
        {
            var bench = Make("Bench", LevelItemGroup.Cover, 6, 2);
            bench.FlipX = true;
            var rects = new List<WorldBounds>(bench.GameRects());
            Assert.AreEqual(8f, rects[0].MinX, 1e-4f);
            Assert.AreEqual(9f, rects[0].MaxX, 1e-4f);
        }

        [Test] public void GridPlacementsRoundTripThroughEditorBlock()
        {
            var doc = new LevelDocument { Seed = 1234 };
            doc.Path.Add(new Vector2Int(1, 1));
            doc.Path.Add(new Vector2Int(2, 1));
            var tree = Make("Tree01", LevelItemGroup.Cover, 10, 12);
            tree.FlipX = true;
            doc.Placements.Add(tree);
            var portal = Make(nameof(WorldObjectKind.Portal), LevelItemGroup.WorldObject, 3, 4);
            portal.PairId = 7;
            doc.Placements.Add(portal);
            doc.Placements.Add(Make("Spawn", LevelItemGroup.Spawn, 20, 20));

            var back = LevelDocument.FromJson(doc.ToJson(), null, Shape);
            Assert.AreEqual(1234, back.Seed);
            CollectionAssert.AreEquivalent(doc.Path, back.Path);
            Assert.AreEqual(3, back.Placements.Count);
            Assert.IsTrue(back.Placements[0].OnGrid);
            Assert.AreEqual((10, 12), (back.Placements[0].CellX, back.Placements[0].CellY));
            Assert.IsTrue(back.Placements[0].FlipX);
            Assert.AreEqual(7, back.Placements[1].PairId);
            Assert.AreEqual(20.5f, back.Placements[2].X, 1e-4f, "spawn ở tâm ô");
            Assert.AreEqual(doc.ToJson(), back.ToJson());
        }

        [Test] public void PathAndDecorNeverReachGameArrays()
        {
            var doc = new LevelDocument();
            doc.Path.Add(new Vector2Int(5, 5));
            doc.Placements.Add(Make("Pebble", LevelItemGroup.Decoration, 6, 6));
            var json = JObject.Parse(doc.ToJson());
            Assert.AreEqual(0, ((JArray)json["obstacles"]).Count);
            Assert.AreEqual(0, ((JArray)json["worldObjects"]).Count);
            Assert.AreEqual(1, ((JArray)json["decorations"]).Count);
        }

        [Test] public void CellOccupancyBlocksSecondBlockingItemButNotDecor()
        {
            var doc = new LevelDocument();
            doc.Placements.Add(Make("Bench", LevelItemGroup.Cover, 6, 2));
            Assert.IsFalse(doc.IsFree(8, 2, 1, 1, false), "ô thứ ba của ghế đã có vật");
            Assert.IsTrue(doc.IsFree(9, 2, 1, 1, false));
            Assert.IsTrue(doc.IsFree(7, 2, 1, 1, true), "decor nằm lớp riêng");
            Assert.IsTrue(doc.IsFree(6, 2, 3, 1, false, 0), "bỏ qua chính nó khi kéo");
            Assert.IsFalse(doc.IsFree(doc.Width - 1, 0, 2, 1, false), "ra ngoài map");
        }

        [Test] public void ShrinkingMapDropsOutsidePathCellsOnSave()
        {
            var doc = new LevelDocument { Width = 30, Height = 30 };
            doc.Path.Add(new Vector2Int(40, 40));
            doc.Path.Add(new Vector2Int(3, 3));
            var path = (JArray)JObject.Parse(doc.ToJson())["editor"]["path"];
            Assert.AreEqual(1, path.Count);
        }

        [Test] public void GridMapPassesGameValidation()
        {
            var doc = new LevelDocument();
            var spawns = new[] { (10, 10), (20, 10), (30, 10), (40, 10), (10, 25), (40, 25), (10, 40), (20, 40), (30, 40), (40, 40) };
            foreach (var (x, y) in spawns) doc.Placements.Add(Make("Spawn", LevelItemGroup.Spawn, x - 1, y - 1));
            doc.Placements.Add(Make("Gate", LevelItemGroup.Cover, 24, 24));
            doc.Placements.Add(Make("Tree01", LevelItemGroup.Cover, 15, 15));
            Assert.IsNull(doc.Validate(out var hash));
            var authored = JsonConvert.DeserializeObject<AuthoredMapDocument>(doc.ToJson());
            Assert.AreEqual(hash, authored.Build(LevelDocument.GameWorldVersion).Hash);
        }
    }
}
