using System.IO;
using JungleMaze.LevelEditor;
using JungleMaze.World;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

namespace JungleMaze.Tests.LevelEditor
{
    /// <summary>File của Level Editor phải là map game đọc được: cùng định dạng, cùng hash, cùng luật kiểm tra.</summary>
    public sealed class LevelDocumentTests
    {
        private static string GameMap => File.ReadAllText(Path.GetFullPath(Path.Combine(Application.dataPath, "../Backend/Shared/GameData/map_demo.json")));

        private static string GameHash(string json) =>
            JsonConvert.DeserializeObject<AuthoredMapDocument>(json).Build(LevelDocument.GameWorldVersion).Hash;

        [Test] public void OpeningAndSavingGameMapKeepsTheSameGameHash()
        {
            var doc = LevelDocument.FromJson(GameMap);
            Assert.AreEqual(50, doc.Width);
            Assert.AreEqual(10, doc.Count(LevelItemGroup.Spawn));
            Assert.AreEqual(4, doc.Count(LevelItemGroup.Cover));
            Assert.AreEqual(211, doc.Count(LevelItemGroup.WorldObject));
            var exported = doc.ToJson();
            Assert.AreEqual(GameHash(GameMap), GameHash(exported));
            Assert.IsNull(doc.Validate(out var hash));
            Assert.AreEqual(GameHash(GameMap), hash);
        }

        [Test] public void ScaleRotationAndFlipSurviveRoundTripAndRotationSwapsFootprint()
        {
            var doc = new LevelDocument();
            doc.Placements.Add(new LevelPlacement { Type = "CityWall", Group = LevelItemGroup.Cover, X = 10, Y = 12, BaseWidth = 1, BaseHeight = 4, Scale = 1.5f, Rotation = 1, FlipX = true });
            Assert.AreEqual(6f, doc.Placements[0].Width, 1e-4f, "xoay 90° → tường nằm ngang");
            Assert.AreEqual(1.5f, doc.Placements[0].Height, 1e-4f);

            var json = JObject.Parse(doc.ToJson());
            var wall = json["obstacles"][0];
            Assert.AreEqual(7f, (float)wall["minX"], 1e-4f);
            Assert.AreEqual(13f, (float)wall["maxX"], 1e-4f);

            var back = LevelDocument.FromJson(doc.ToJson(), id => id == "CityWall" ? (1f, 4f) : ((float, float)?)null).Placements[0];
            Assert.AreEqual(1f, back.BaseWidth, 1e-4f);
            Assert.AreEqual(4f, back.BaseHeight, 1e-4f);
            Assert.AreEqual(1.5f, back.Scale, 1e-4f);
            Assert.AreEqual(1, back.Rotation);
            Assert.IsTrue(back.FlipX);
        }

        [Test] public void PlainGameMapWithoutEditorFieldsInfersScaleFromCatalogSize()
        {
            var doc = LevelDocument.FromJson(GameMap, id => id == nameof(WorldObjectKind.Swamp) ? (2f, 2f) : ((float, float)?)null);
            var swamp = doc.Placements.Find(p => p.Type == nameof(WorldObjectKind.Swamp));
            Assert.AreEqual(2f, swamp.Scale, 1e-4f, "4×4 trong file / 2×2 gốc");
            Assert.AreEqual(4f, swamp.Width, 1e-4f);
        }

        [Test] public void PortalsPairInPlacementOrder()
        {
            var doc = new LevelDocument();
            for (var i = 0; i < 3; i++)
            {
                var portal = new LevelPlacement { Type = nameof(WorldObjectKind.Portal), Group = LevelItemGroup.WorldObject, X = 5 + i * 3, Y = 5 };
                portal.PairId = doc.NextPortalPair();
                doc.Placements.Add(portal);
            }
            Assert.AreEqual(new ushort[] { 1, 1, 2 }, doc.Placements.ConvertAll(p => p.PairId).ToArray());
            doc.Placements.RemoveAt(0);
            Assert.AreEqual(1, doc.NextPortalPair(), "cặp 1 còn lẻ thì portal mới ghép vào đó");
        }

        [Test] public void ValidationUsesGameRules()
        {
            var doc = LevelDocument.FromJson(GameMap);
            Assert.IsNull(doc.Validate(out _));

            doc.Placements.Add(new LevelPlacement { Type = nameof(WorldObjectKind.Chest), Group = LevelItemGroup.WorldObject, X = 23, Y = 25 });
            StringAssert.Contains("overlap", doc.Validate(out _), "rương đè lên cột đá");
            doc.Placements.RemoveAt(doc.Placements.Count - 1);

            doc.Placements.RemoveAt(doc.Placements.FindIndex(p => p.Group == LevelItemGroup.Spawn));
            Assert.IsNotNull(doc.Validate(out _), "thiếu spawn");

            var square = LevelDocument.FromJson(GameMap);
            square.Width = 60;
            StringAssert.Contains("vuông", square.Validate(out _));
        }

        [Test] public void DecorationsAreSavedButIgnoredByTheGame()
        {
            var doc = LevelDocument.FromJson(GameMap);
            var before = GameHash(doc.ToJson());
            doc.Placements.Add(new LevelPlacement { Type = "River", Group = LevelItemGroup.Decoration, X = 2, Y = 2, BaseWidth = 4, BaseHeight = 1 });
            var json = doc.ToJson();
            Assert.AreEqual(1, ((JArray)JObject.Parse(json)["decorations"]).Count);
            Assert.AreEqual(before, GameHash(json));
        }

        [Test] public void TopmostPlacementIsPickedFirst()
        {
            var doc = new LevelDocument();
            doc.Placements.Add(new LevelPlacement { Type = "A", X = 5, Y = 5, BaseWidth = 4, BaseHeight = 4 });
            doc.Placements.Add(new LevelPlacement { Type = "B", X = 5, Y = 5, BaseWidth = 1, BaseHeight = 1 });
            Assert.AreEqual(1, doc.FindTopAt(5.2f, 5.2f));
            Assert.AreEqual(0, doc.FindTopAt(6.5f, 6.5f));
            Assert.AreEqual(-1, doc.FindTopAt(20, 20));
        }

        [Test] public void HistoryUndoesAndRedoesSnapshots()
        {
            var history = new LevelHistory();
            history.Record("a");
            history.Record("b");
            Assert.IsTrue(history.TryUndo("c", out var previous));
            Assert.AreEqual("b", previous);
            Assert.IsTrue(history.TryRedo("b", out var next));
            Assert.AreEqual("c", next);
            history.Record("d");
            Assert.IsFalse(history.CanRedo, "thay đổi mới xóa nhánh redo");
        }
    }
}
