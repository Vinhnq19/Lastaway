using System.Collections.Generic;
using JungleMaze.App.Match;
using JungleMaze.LevelEditor;
using JungleMaze.World;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

namespace JungleMaze.Tests.LevelEditor
{
    /// <summary>Nhà trong Level Editor: tường tự nối, cửa tương tác (mở/đóng hoặc phá), và map lớn chơi offline.</summary>
    public sealed class LevelHouseTests
    {
        private static readonly LevelItemShape Cell = new LevelItemShape(1, 1, null);
        private static readonly LevelItemShape WallShape = new LevelItemShape(1, 1, new[] { new Rect(0f, 0f, 1f, 1f) });

        private static LevelDocument NewMap(int size = 50)
        {
            var doc = new LevelDocument { Width = size, Height = size };
            for (var i = 0; i < 10; i++)
            {
                var a = i * 36f * Mathf.Deg2Rad;
                doc.Placements.Add(LevelPlacement.OnCell("Spawn", LevelItemGroup.Spawn,
                    Mathf.FloorToInt(size / 2f + 18 * Mathf.Cos(a)), Mathf.FloorToInt(size / 2f + 18 * Mathf.Sin(a)), Cell));
            }
            return doc;
        }

        private static MapLayout Build(LevelDocument doc) =>
            JsonConvert.DeserializeObject<AuthoredMapDocument>(doc.ToJson()).Build(LevelDocument.GameWorldVersion);

        [Test] public void WallPartsJoinNeighbours()
        {
            var grid = new Sprite[LevelGround.WallColumns * LevelGround.WallColumns];
            for (var i = 0; i < grid.Length; i++) grid[i] = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * .5f);
            var walls = new HashSet<Vector2Int> { new Vector2Int(4, 4), new Vector2Int(5, 4), new Vector2Int(6, 4), new Vector2Int(4, 5) };
            Assert.AreSame(grid[7 * 16 + 1], LevelWalls.Parts(grid, walls, 4, 4)[0].Sprite, "đầu trái");
            Assert.AreSame(grid[7 * 16 + 4], LevelWalls.Parts(grid, walls, 6, 4)[0].Sprite, "đầu phải");
            Assert.AreEqual(2, LevelWalls.Parts(grid, walls, 4, 4).Length, "trên là tường dọc: góc vẫn có gờ để dải đá nối vào");
            Assert.AreEqual(2, LevelWalls.Parts(grid, walls, 5, 4).Length, "trên trống: có gờ");
            var thick = new HashSet<Vector2Int> { new Vector2Int(4, 4), new Vector2Int(5, 4), new Vector2Int(4, 5), new Vector2Int(5, 5) };
            Assert.AreEqual(1, LevelWalls.Parts(grid, thick, 4, 4).Length, "trên là tường ngang (tường dày): không vẽ gờ");
        }

        [Test] public void SideWallsUseStoneStrip()
        {
            var grid = new Sprite[LevelGround.WallColumns * LevelGround.WallColumns];
            for (var i = 0; i < grid.Length; i++) grid[i] = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * .5f);
            // Phòng 5×4: tường ngang y=28 và y=31, tường dọc x=28 và x=32; vách giữa x=30 nối hai tường ngang (chữ T).
            var walls = new HashSet<Vector2Int>();
            for (var x = 28; x <= 32; x++) { walls.Add(new Vector2Int(x, 28)); walls.Add(new Vector2Int(x, 31)); }
            for (var y = 29; y <= 30; y++) { walls.Add(new Vector2Int(28, y)); walls.Add(new Vector2Int(32, y)); walls.Add(new Vector2Int(30, y)); }
            Assert.AreEqual(WallSide.Left, LevelWalls.SideOf(walls, 28, 29));
            Assert.AreEqual(WallSide.Right, LevelWalls.SideOf(walls, 32, 30));
            Assert.AreEqual(WallSide.Middle, LevelWalls.SideOf(walls, 30, 29));
            Assert.AreEqual(WallSide.None, LevelWalls.SideOf(walls, 29, 28), "tường ngang");
            Assert.AreSame(grid[2 * 16 + 1], LevelWalls.Parts(grid, walls, 28, 29)[0].Sprite, "dải đá mép trái");
            Assert.AreSame(grid[2 * 16 + 3], LevelWalls.Parts(grid, walls, 32, 29)[0].Sprite, "dải đá mép phải");
            Assert.AreEqual(1, LevelWalls.Parts(grid, walls, 28, 29).Length);

            var doc = NewMap();
            foreach (var c in walls) doc.Placements.Add(LevelPlacement.OnCell(LevelWalls.Id, LevelItemGroup.Cover, c.x, c.y, WallShape));
            var layout = Build(doc);
            var left = layout.Objects.Find(o => o.IsHardCover && o.Bounds.Contains(new WorldPoint(28.1f, 29.5f)));
            Assert.IsNotNull(left);
            Assert.AreEqual(28f, left.Bounds.MinX, 1e-3f);
            Assert.Less(left.Bounds.MaxX, 28.4f, "tường dọc chỉ chặn ở dải đá");
            Assert.IsTrue(layout.IsWalkable(new WorldPoint(29.5f, 29.5f), .4f), "trong phòng sát tường dọc vẫn đi được");
            Assert.IsFalse(layout.IsWalkable(new WorldPoint(28.5f, 29.5f), .4f), "tường dọc vẫn kín, không lách qua");
        }

        [Test] public void EditorDoorsAreInteractiveWithDefaultHp()
        {
            var doc = NewMap();
            doc.Placements.Add(LevelPlacement.OnCell(nameof(WorldObjectKind.Door), LevelItemGroup.WorldObject, 20, 20, Cell));
            var door = Build(doc).Objects.Find(o => o.Kind == WorldObjectKind.Door);
            Assert.AreEqual(LevelPlacement.DefaultDoorHp, door.Hp);
            Assert.IsTrue(door.IsInteractiveDoor);
            Assert.IsTrue(door.IsBreakable);
            Assert.IsFalse(door.IsDestructible, "cửa không rơi loot và không chặn đường khi mở");
        }

        [Test] public void DoorOpensClosesAndBreaks()
        {
            var doc = NewMap();
            for (var x = 18; x <= 22; x++) if (x != 20) doc.Placements.Add(LevelPlacement.OnCell(LevelWalls.Id, LevelItemGroup.Cover, x, 20, WallShape));
            doc.Placements.Add(LevelPlacement.OnCell(nameof(WorldObjectKind.Door), LevelItemGroup.WorldObject, 20, 20, Cell));
            var layout = Build(doc);
            var door = layout.Objects.Find(o => o.Kind == WorldObjectKind.Door);
            var sink = new Sink();
            var destructibles = new DestructibleService(layout, new DestructibleConfig(), sink);
            var hazards = new HazardService(layout, new HazardConfig(), sink, sink, sink, destructibles.IsBroken);
            var near = new WorldPoint(20.5f, 21.8f);
            var actors = new[] { new WorldActor(0, near.X, near.Y) };

            Assert.IsTrue(hazards.IsDoorClosed(door.Id), "mặc định đóng");
            Assert.IsFalse(layout.IsWalkable(new WorldPoint(20.5f, 20.5f), .4f, destructibles.IsBroken, hazards.IsDoorClosed));
            Assert.IsFalse(hazards.TryToggleDoor(door.Id, new WorldPoint(20.5f, 25f), actors), "ngoài tầm tay");
            Assert.IsTrue(hazards.TryToggleDoor(door.Id, near, actors));
            Assert.IsFalse(hazards.IsDoorClosed(door.Id));
            Assert.IsTrue(layout.IsWalkable(new WorldPoint(20.5f, 20.5f), .4f, destructibles.IsBroken, hazards.IsDoorClosed), "mở thì đi qua được");

            var inside = new[] { new WorldActor(0, near.X, near.Y), new WorldActor(1, 20.5f, 20.5f) };
            Assert.IsFalse(hazards.TryToggleDoor(door.Id, near, inside), "không đóng khi có người đứng trong khung cửa");
            Assert.IsTrue(hazards.TryToggleDoor(door.Id, near, actors));

            Assert.IsTrue(destructibles.DamageConfirmed(door.Id, LevelPlacement.DefaultDoorHp, 1, 1));
            Assert.IsTrue(destructibles.IsBroken(door.Id));
            Assert.IsFalse(hazards.IsDoorClosed(door.Id), "cửa vỡ luôn thông");
            Assert.IsFalse(hazards.TryToggleDoor(door.Id, near, actors));
            Assert.AreEqual(0, sink.Loot, "cửa vỡ không rơi loot");
        }

        [Test] public void LegacyDoorKeepsSharedTimer()
        {
            var doc = NewMap();
            var json = JObject.Parse(doc.ToJson());
            ((JArray)json["worldObjects"]).Add(new JObject { ["id"] = 1, ["kind"] = "Door", ["pairId"] = 0, ["bounds"] = new JObject { ["minX"] = 20, ["minY"] = 20, ["maxX"] = 21, ["maxY"] = 22 } });
            var layout = JsonConvert.DeserializeObject<AuthoredMapDocument>(json.ToString()).Build(LevelDocument.GameWorldVersion);
            var door = layout.Objects.Find(o => o.Kind == WorldObjectKind.Door);
            Assert.IsFalse(door.IsInteractiveDoor);
            var sink = new Sink();
            var hazards = new HazardService(layout, new HazardConfig(), sink, sink, sink);
            Assert.IsFalse(hazards.TryToggleDoor(door.Id, new WorldPoint(20.5f, 23f), new WorldActor[0]), "cửa cũ không mở bằng tay");
        }

        [Test] public void LargeMapBuildsAndZoneScalesWithIt()
        {
            var doc = NewMap(150);
            Assert.IsNull(doc.Validate(out _), "map 150×150 hợp lệ (spawn trong bo tỉ lệ theo map)");
            var schedule = ZoneSchedule.Default();
            var big = schedule.ScaledTo(150);
            Assert.AreEqual(75f, big.InitialCenter.X, 1e-4f);
            Assert.AreEqual(schedule.InitialRadius * 3f, big.InitialRadius, 1e-4f);
            Assert.AreEqual(schedule.Intervals[0].EndRadius * 3f, big.Intervals[0].EndRadius, 1e-4f);
            Assert.AreSame(schedule, schedule.ScaledTo(50), "map 50 giữ nguyên lịch bo");
        }

        private sealed class Sink : IDestructibleLootSink, IEnvironmentDamageSink, IEnvironmentEffectSink, IEnvironmentMovementSink
        {
            public int Loot;
            public void SpawnLoot(WorldObjectKind source, ushort worldId, WorldPoint position, uint seed) => Loot++;
            public void RequestDamage(byte actor, ushort source, uint tick, int amount, bool bypassArmor) { }
            public void ApplyEffect(byte actor, ushort source, string effect, uint tick, float seconds, float magnitude) { }
            public void RemoveEffect(byte actor, ushort source, string effect, uint tick) { }
            public bool TryMove(byte actor, WorldPoint destination, ushort source, uint tick) => true;
        }
    }
}
