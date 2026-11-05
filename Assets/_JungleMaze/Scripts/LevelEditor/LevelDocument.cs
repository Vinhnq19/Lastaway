using System;
using System.Collections.Generic;
using System.Globalization;
using JungleMaze.World;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace JungleMaze.LevelEditor
{
    /// <summary>Cách một loại vật thể được ghi ra JSON của map.</summary>
    public enum LevelItemGroup
    {
        /// <summary>Hard cover tĩnh → mảng "obstacles" (AncientPillar, CityWall, BoulderRock, AncientStatue).</summary>
        Cover,
        /// <summary>Vật thể tương tác/hazard → mảng "worldObjects", Id = tên <see cref="WorldObjectKind"/>.</summary>
        WorldObject,
        /// <summary>Có trong GDD nhưng game chưa hỗ trợ (sông, thảm hoa) → mảng "decorations", game bỏ qua.</summary>
        Decoration,
        /// <summary>Điểm xuất phát của Hero → mảng "spawnPoints".</summary>
        Spawn,
    }

    /// <summary>Footprint ô nguyên của một loại vật và collider chân (theo góc dưới-trái footprint, chưa lật).</summary>
    public readonly struct LevelItemShape
    {
        public readonly int Width, Height;
        /// <summary>null/rỗng = cả footprint là collider (world object, decor, vật không có collider trong prefab).</summary>
        public readonly Rect[] Colliders;

        public LevelItemShape(int width, int height, Rect[] colliders)
        {
            Width = Math.Max(1, width);
            Height = Math.Max(1, height);
            Colliders = colliders;
        }
    }

    /// <summary>
    /// Một vật thể đặt trên map. Vật mới đặt theo ô (<see cref="OnGrid"/>): footprint nguyên, góc dưới-trái tại
    /// (CellX, CellY), không scale/xoay; vùng chặn trong game là các collider chân. Vật đọc từ map cũ (lệch ô) giữ
    /// footprint = kích thước gốc × Scale, xoay theo bước 90°, cho tới khi được kéo vào ô. FlipX chỉ đổi hình và
    /// lật collider trong footprint.
    /// </summary>
    public sealed class LevelPlacement
    {
        public string Type;
        public LevelItemGroup Group;
        public float X, Y;
        public float BaseWidth = 1f, BaseHeight = 1f;
        public float Scale = 1f;
        /// <summary>Số lần xoay 90° ngược chiều kim đồng hồ, 0..3.</summary>
        public int Rotation;
        public bool FlipX;
        /// <summary>Chỉ dùng cho Portal: hai đầu cùng PairId là một cặp.</summary>
        public ushort PairId;
        public bool OnGrid;
        public int CellX, CellY;
        /// <summary>Collider chân theo góc dưới-trái footprint (chưa lật); null = cả footprint.</summary>
        public Rect[] Colliders;
        /// <summary>Vật cản "Phá được": trong game thành vật thể phá được (loại = <see cref="Loot"/>) thay vì hard cover.</summary>
        public bool Destructible;
        /// <summary>HP riêng khi phá được (rương/bình/thùng tiếp tế hoặc cover phá được); 0 = HP mặc định của loại.</summary>
        public ushort Hp;
        /// <summary>Bảng loot rơi khi vỡ (cover phá được): Pot, Chest hoặc Supply.</summary>
        public string Loot = nameof(WorldObjectKind.Pot);

        public static readonly string[] LootKinds = { nameof(WorldObjectKind.Pot), nameof(WorldObjectKind.Chest), nameof(WorldObjectKind.Supply) };

        public float Width => (Rotation % 2 == 0 ? BaseWidth : BaseHeight) * Scale;
        public float Height => (Rotation % 2 == 0 ? BaseHeight : BaseWidth) * Scale;
        public WorldBounds Bounds => new WorldBounds(X, Y, Width, Height);
        public bool IsPortal => Group == LevelItemGroup.WorldObject && Type == nameof(WorldObjectKind.Portal);
        /// <summary>HP mặc định của cửa đặt bằng Level Editor: mọi cửa như vậy là cửa tương tác (mở/đóng bằng F, đánh vỡ được).</summary>
        public const ushort DefaultDoorHp = 60;

        /// <summary>Rương, bình, thùng tiếp tế (phá được, rơi loot) và cửa (mở/đóng hoặc phá).</summary>
        public bool IsDestructibleObject => Group == LevelItemGroup.WorldObject &&
                                            (Array.IndexOf(LootKinds, Type) >= 0 || Type == nameof(WorldObjectKind.Door));

        /// <summary>HP ghi ra JSON game: HP riêng, hoặc HP mặc định của cửa; 0 = HP mặc định của loại (không ghi).</summary>
        public ushort GameHp => Hp != 0 ? Hp : Group == LevelItemGroup.WorldObject && Type == nameof(WorldObjectKind.Door) ? DefaultDoorHp : (ushort)0;
        /// <summary>
        /// Có HP/loot trong game: cover đánh dấu "Phá được" hoặc rương/bình/thùng tiếp tế. Vật phá được dùng cả footprint
        /// làm bounds (giống rương/bình) để đánh trúng được; collider chân nhỏ chỉ dành cho hard cover.
        /// </summary>
        public bool BreaksInGame => (Group == LevelItemGroup.Cover && Destructible) || IsDestructibleObject;

        public LevelPlacement Clone() => (LevelPlacement)MemberwiseClone();

        public static LevelPlacement OnCell(string type, LevelItemGroup group, int cellX, int cellY, LevelItemShape shape)
        {
            var p = new LevelPlacement { Type = type, Group = group, BaseWidth = shape.Width, BaseHeight = shape.Height, Colliders = shape.Colliders, OnGrid = true };
            p.MoveToCell(cellX, cellY);
            return p;
        }

        /// <summary>Đưa vật vào ô (vật lệch ô từ map cũ cũng được chuẩn hoá: bỏ scale/xoay, footprint làm tròn lên).</summary>
        public void MoveToCell(int cellX, int cellY)
        {
            if (!OnGrid)
            {
                var w = Math.Max(1, (int)Math.Ceiling(Width - .01f));
                var h = Math.Max(1, (int)Math.Ceiling(Height - .01f));
                BaseWidth = w; BaseHeight = h; Scale = 1f; Rotation = 0; OnGrid = true;
            }
            CellX = cellX;
            CellY = cellY;
            X = cellX + Width / 2f;
            Y = cellY + Height / 2f;
        }

        /// <summary>Vùng chặn ghi ra JSON game: từng collider chân (lật theo FlipX, dịch tới ô), hoặc cả footprint.</summary>
        public IEnumerable<WorldBounds> GameRects()
        {
            if (!OnGrid || Colliders == null || Colliders.Length == 0)
            {
                yield return Bounds;
                yield break;
            }
            foreach (var c in Colliders)
            {
                var minX = FlipX ? Width - c.xMax : c.xMin;
                var maxX = FlipX ? Width - c.xMin : c.xMax;
                yield return new WorldBounds { MinX = CellX + minX, MaxX = CellX + maxX, MinY = CellY + c.yMin, MaxY = CellY + c.yMax };
            }
        }
    }

    /// <summary>
    /// Map đang soạn. Ghi/đọc đúng định dạng <c>map_demo.json</c> (đọc bởi <see cref="AuthoredMapDocument"/> ở cả
    /// client lẫn GameServer); thông tin chỉ editor cần (scale, flip, rotation, ground) là field phụ mà game bỏ qua.
    /// </summary>
    public sealed class LevelDocument
    {
        public const float MinScale = .25f, MaxScale = 8f;
        public const int MinMapSize = 8, MaxMapSize = 256;
        public const float SpawnSize = .8f;
        /// <summary>Phiên bản world mà game dùng khi Build map (world_settings.json "version").</summary>
        public const string GameWorldVersion = "world-prototype-1";

        public string Version = "1.1.0";
        public int Width = 50, Height = 50;
        /// <summary>Seed sinh cỏ/hoa, viền đường đá và tường mép (xem <see cref="LevelGround"/>).</summary>
        public int Seed = 1;
        /// <summary>Mật độ cỏ/hoa nhỏ rải theo seed (0–1, tỉ lệ ô trống có cỏ).</summary>
        public float Decor = .12f;
        /// <summary>Ô đường đá; chỉ là hình, đi xuyên, game không đọc.</summary>
        public readonly HashSet<Vector2Int> Path = new HashSet<Vector2Int>();
        public readonly List<LevelPlacement> Placements = new List<LevelPlacement>();

        public static float ClampScale(float scale) => Math.Max(MinScale, Math.Min(MaxScale, scale));

        /// <summary>Vật thể trên cùng (đặt sau cùng) chứa điểm; -1 nếu không có.</summary>
        public int FindTopAt(float x, float y)
        {
            var point = new WorldPoint(x, y);
            for (var i = Placements.Count - 1; i >= 0; i--)
                if (Placements[i].Bounds.Contains(point)) return i;
            return -1;
        }

        /// <summary>PairId cho portal mới: ghép với portal còn lẻ, nếu không có thì mở cặp mới.</summary>
        public ushort NextPortalPair()
        {
            var counts = new Dictionary<ushort, int>();
            ushort max = 0;
            foreach (var p in Placements)
            {
                if (!p.IsPortal || p.PairId == 0) continue;
                counts[p.PairId] = counts.TryGetValue(p.PairId, out var n) ? n + 1 : 1;
                if (p.PairId > max) max = p.PairId;
            }
            for (ushort id = 1; id <= max; id++)
                if (counts.TryGetValue(id, out var n) && n == 1) return id;
            return (ushort)(max + 1);
        }

        /// <summary>
        /// Vùng ô [x, x+w) × [y, y+h) nằm trong map và không chồng vật cùng lớp. Decor là một lớp riêng (đi xuyên);
        /// cover, vật thể game và spawn chung một lớp vì game cấm chúng chồng nhau.
        /// </summary>
        public bool IsFree(int x, int y, int w, int h, bool decor, int ignore = -1)
        {
            if (x < 0 || y < 0 || x + w > Width || y + h > Height) return false;
            var area = new WorldBounds { MinX = x, MinY = y, MaxX = x + w, MaxY = y + h };
            for (var i = 0; i < Placements.Count; i++)
            {
                var p = Placements[i];
                if (i == ignore || (p.Group == LevelItemGroup.Decoration) != decor) continue;
                if (p.Bounds.Overlaps(area, -.001f)) return false;
            }
            return true;
        }

        public int Count(LevelItemGroup group, string type = null)
        {
            var n = 0;
            foreach (var p in Placements)
                if (p.Group == group && (type == null || p.Type == type)) n++;
            return n;
        }

        // ---------- JSON ----------

        public string ToJson()
        {
            var obstacles = new JArray();
            var worldObjects = new JArray();
            var decorations = new JArray();
            var spawns = new JArray();
            ushort id = 0;
            HashSet<Vector2Int> wallCells = null;
            var placements = new JArray();
            foreach (var p in Placements)
            {
                placements.Add(EditorEntry(p));
                var b = p.Bounds;
                switch (p.Group)
                {
                    case LevelItemGroup.Spawn:
                        spawns.Add(new JObject { ["x"] = R(p.X), ["y"] = R(p.Y) });
                        break;
                    case LevelItemGroup.Cover when p.Destructible:
                        // Cover phá được: một vật thể game có HP/loot (loại = bảng loot), bounds = footprint, tên hình trong "art".
                        var d = new JObject
                        {
                            ["id"] = ++id,
                            ["kind"] = Array.IndexOf(LevelPlacement.LootKinds, p.Loot) >= 0 ? p.Loot : LevelPlacement.LootKinds[0],
                            ["pairId"] = 0,
                            ["bounds"] = new JObject { ["minX"] = R(b.MinX), ["minY"] = R(b.MinY), ["maxX"] = R(b.MaxX), ["maxY"] = R(b.MaxY) },
                            ["art"] = p.Type,
                        };
                        if (p.Hp != 0) d["hp"] = p.Hp;
                        AddVisual(d, p);
                        worldObjects.Add(d);
                        break;
                    case LevelItemGroup.Cover:
                        // Tường dọc chỉ chặn ở dải đá mảnh của nó (khớp hình), không chặn cả ô.
                        var side = p.Type == LevelWalls.Id && p.OnGrid
                            ? LevelWalls.SideCollider(LevelWalls.SideOf(wallCells ??= LevelWalls.Cells(Placements), p.CellX, p.CellY))
                            : null;
                        if (side.HasValue)
                        {
                            var s = side.Value;
                            obstacles.Add(Rect(p, new WorldBounds { MinX = p.CellX + s.xMin, MaxX = p.CellX + s.xMax, MinY = p.CellY + s.yMin, MaxY = p.CellY + s.yMax }));
                        }
                        else
                            foreach (var rect in p.GameRects()) obstacles.Add(Rect(p, rect));
                        break;
                    case LevelItemGroup.Decoration:
                        decorations.Add(Rect(p, b));
                        break;
                    case LevelItemGroup.WorldObject:
                        var o = new JObject
                        {
                            ["id"] = ++id,
                            ["kind"] = p.Type,
                            ["pairId"] = p.PairId,
                            ["bounds"] = new JObject { ["minX"] = R(b.MinX), ["minY"] = R(b.MinY), ["maxX"] = R(b.MaxX), ["maxY"] = R(b.MaxY) },
                        };
                        if (p.IsDestructibleObject && p.GameHp != 0) o["hp"] = p.GameHp;
                        AddVisual(o, p);
                        worldObjects.Add(o);
                        break;
                }
            }
            var root = new JObject
            {
                ["version"] = Version,
                ["mapBounds"] = new JObject { ["minX"] = 0f, ["minY"] = 0f, ["maxX"] = (float)Width, ["maxY"] = (float)Height },
                ["obstacles"] = obstacles,
                ["spawnPoints"] = spawns,
                ["worldObjects"] = worldObjects,
                ["decorations"] = decorations,
                ["editor"] = new JObject
                {
                    ["tool"] = "JungleMaze.LevelEditor",
                    ["seed"] = Seed,
                    ["decor"] = R(Decor),
                    ["path"] = PathJson(),
                    ["placements"] = placements,
                },
            };
            return root.ToString(Formatting.Indented);
        }

        /// <summary>
        /// Đọc file do editor ghi hoặc <c>map_demo.json</c> gốc. <paramref name="baseSize"/> trả kích thước gốc của
        /// một loại (từ catalog) để tính lại Scale; trả null thì coi footprint trong file là kích thước gốc.
        /// </summary>
        public static LevelDocument FromJson(string json, Func<string, (float width, float height)?> baseSize = null,
            Func<string, LevelItemShape?> shape = null)
        {
            var root = JObject.Parse(json);
            var doc = new LevelDocument { Version = (string)root["version"] ?? "1.0.0" };
            var bounds = root["mapBounds"] as JObject ?? throw new FormatException("Thiếu mapBounds.");
            doc.Width = (int)Math.Round(F(bounds, "maxX") - F(bounds, "minX"));
            doc.Height = (int)Math.Round(F(bounds, "maxY") - F(bounds, "minY"));
            if (root["editor"] is JObject editor)
            {
                doc.Seed = (int?)editor["seed"] ?? doc.Seed;
                doc.Decor = (float?)editor["decor"] ?? doc.Decor;
                if (editor["path"] is JArray path)
                    foreach (var cell in path)
                        if (cell is JArray xy && xy.Count == 2) doc.Path.Add(new Vector2Int((int)xy[0], (int)xy[1]));
                // File do editor mới ghi: danh sách placement là nguồn sự thật (một cover có thể ra nhiều obstacle).
                if (editor["placements"] is JArray entries)
                {
                    foreach (var token in entries)
                        if (token is JObject o) doc.Placements.Add(FromEditorEntry(o, shape));
                    return doc;
                }
            }

            foreach (var o in Items(root, "obstacles"))
                doc.Placements.Add(FromRect(o, LevelItemGroup.Cover, (string)o["name"], baseSize));
            foreach (var o in Items(root, "decorations"))
                doc.Placements.Add(FromRect(o, LevelItemGroup.Decoration, (string)o["name"], baseSize));
            foreach (var o in Items(root, "worldObjects"))
            {
                var kind = (string)o["kind"];
                if (int.TryParse(kind, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number))
                    kind = ((WorldObjectKind)number).ToString();
                var b = o["bounds"] as JObject ?? throw new FormatException("worldObject thiếu bounds.");
                var minX = F(b, "minX"); var minY = F(b, "minY"); var maxX = F(b, "maxX"); var maxY = F(b, "maxY");
                var p = Make(LevelItemGroup.WorldObject, kind, (minX + maxX) / 2f, (minY + maxY) / 2f, maxX - minX, maxY - minY, o, baseSize);
                p.PairId = (ushort?)o["pairId"] ?? 0;
                p.Hp = (ushort?)o["hp"] ?? 0;
                doc.Placements.Add(p);
            }
            foreach (var o in Items(root, "spawnPoints"))
                doc.Placements.Add(new LevelPlacement
                {
                    Type = "Spawn", Group = LevelItemGroup.Spawn, X = F(o, "x"), Y = F(o, "y"),
                    BaseWidth = SpawnSize, BaseHeight = SpawnSize,
                });
            return doc;
        }

        /// <summary>
        /// Kiểm tra bằng đúng đường game đọc map: JSON → <see cref="AuthoredMapDocument"/> → Build (chồng lấn, cặp
        /// portal, 10 spawn, spawn bị chặn/quá gần/không thông nhau...). Trả null nếu hợp lệ.
        /// </summary>
        public string Validate(out string hash)
        {
            hash = null;
            if (Width != Height) return $"Game chỉ nhận map vuông; hiện {Width}×{Height}.";
            foreach (var p in Placements)
            {
                var b = p.Bounds;
                if (b.MinX < 0 || b.MinY < 0 || b.MaxX > Width || b.MaxY > Height)
                    return $"{p.Type} tại ({p.X:0.##}, {p.Y:0.##}) nằm ngoài map.";
            }
            try
            {
                var authored = JsonConvert.DeserializeObject<AuthoredMapDocument>(ToJson());
                hash = authored.Build(GameWorldVersion).Hash;
                return null;
            }
            catch (Exception e) when (e is ArgumentException || e is JsonException || e is InvalidOperationException)
            {
                return e.Message;
            }
        }

        private JArray PathJson()
        {
            var cells = new List<Vector2Int>();
            foreach (var c in Path)
                if (c.x >= 0 && c.y >= 0 && c.x < Width && c.y < Height) cells.Add(c);
            cells.Sort((a, b) => a.y != b.y ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x));
            var array = new JArray();
            foreach (var c in cells) array.Add(new JArray(c.x, c.y));
            return array;
        }

        private static JObject EditorEntry(LevelPlacement p)
        {
            var o = new JObject { ["type"] = p.Type, ["group"] = p.Group.ToString() };
            if (p.OnGrid)
            {
                o["cellX"] = p.CellX;
                o["cellY"] = p.CellY;
            }
            else
            {
                o["x"] = R(p.X); o["y"] = R(p.Y);
                o["baseWidth"] = R(p.BaseWidth); o["baseHeight"] = R(p.BaseHeight);
                o["scale"] = R(p.Scale); o["rotation"] = p.Rotation;
            }
            if (p.FlipX) o["flipX"] = true;
            if (p.PairId != 0) o["pairId"] = p.PairId;
            if (p.Group == LevelItemGroup.Cover && p.Destructible)
            {
                o["destructible"] = true;
                o["loot"] = p.Loot;
            }
            if (p.BreaksInGame && p.Hp != 0) o["hp"] = p.Hp;
            return o;
        }

        private static LevelPlacement FromEditorEntry(JObject o, Func<string, LevelItemShape?> shape)
        {
            var type = (string)o["type"];
            if (string.IsNullOrEmpty(type)) throw new FormatException("Placement thiếu type.");
            if (!Enum.TryParse<LevelItemGroup>((string)o["group"], out var group)) throw new FormatException($"Placement {type} sai group.");
            LevelPlacement p;
            if (o["cellX"] != null)
                p = LevelPlacement.OnCell(type, group, (int)o["cellX"], (int)o["cellY"], shape?.Invoke(type) ?? new LevelItemShape(1, 1, null));
            else
                p = new LevelPlacement
                {
                    Type = type, Group = group, X = F(o, "x"), Y = F(o, "y"),
                    BaseWidth = F(o, "baseWidth"), BaseHeight = F(o, "baseHeight"),
                    Scale = (float?)o["scale"] ?? 1f, Rotation = ((int?)o["rotation"] ?? 0) & 3,
                };
            p.FlipX = (bool?)o["flipX"] ?? false;
            p.PairId = (ushort?)o["pairId"] ?? 0;
            p.Destructible = group == LevelItemGroup.Cover && ((bool?)o["destructible"] ?? false);
            p.Loot = (string)o["loot"] ?? p.Loot;
            p.Hp = (ushort?)o["hp"] ?? 0;
            return p;
        }

        private static JObject Rect(LevelPlacement p, WorldBounds b)
        {
            var o = new JObject
            {
                ["name"] = p.Type,
                ["centerX"] = R((b.MinX + b.MaxX) / 2f), ["centerY"] = R((b.MinY + b.MaxY) / 2f),
                ["sizeX"] = R(b.MaxX - b.MinX), ["sizeY"] = R(b.MaxY - b.MinY),
                ["minX"] = R(b.MinX), ["maxX"] = R(b.MaxX), ["minY"] = R(b.MinY), ["maxY"] = R(b.MaxY),
            };
            AddVisual(o, p);
            return o;
        }

        private static void AddVisual(JObject o, LevelPlacement p)
        {
            o["scale"] = R(p.Scale);
            o["rotation"] = p.Rotation;
            o["flipX"] = p.FlipX;
        }

        private static LevelPlacement FromRect(JObject o, LevelItemGroup group, string type,
            Func<string, (float, float)?> baseSize)
        {
            float x, y, w, h;
            if (o["sizeX"] != null) { x = F(o, "centerX"); y = F(o, "centerY"); w = F(o, "sizeX"); h = F(o, "sizeY"); }
            else
            {
                var minX = F(o, "minX"); var maxX = F(o, "maxX"); var minY = F(o, "minY"); var maxY = F(o, "maxY");
                x = (minX + maxX) / 2f; y = (minY + maxY) / 2f; w = maxX - minX; h = maxY - minY;
            }
            return Make(group, type, x, y, w, h, o, baseSize);
        }

        private static LevelPlacement Make(LevelItemGroup group, string type, float x, float y, float width, float height,
            JObject o, Func<string, (float width, float height)?> baseSize)
        {
            if (string.IsNullOrEmpty(type)) throw new FormatException("Vật thể thiếu tên/kind.");
            var p = new LevelPlacement
            {
                Type = type, Group = group, X = x, Y = y,
                Rotation = ((int?)o["rotation"] ?? 0) & 3,
                FlipX = (bool?)o["flipX"] ?? false,
            };
            // Footprint trong file đã xoay; đưa về hướng gốc rồi suy ra scale từ kích thước gốc của catalog.
            var w = p.Rotation % 2 == 0 ? width : height;
            var h = p.Rotation % 2 == 0 ? height : width;
            var known = baseSize?.Invoke(type);
            var scale = (float?)o["scale"] ?? (known.HasValue && known.Value.width > 0 ? w / known.Value.width : 1f);
            scale = scale > 0 ? scale : 1f;
            p.Scale = scale;
            p.BaseWidth = w / scale;
            p.BaseHeight = h / scale;
            return p;
        }

        private static IEnumerable<JObject> Items(JObject root, string key)
        {
            if (!(root[key] is JArray array)) yield break;
            foreach (var token in array)
                if (token is JObject o) yield return o;
        }

        private static float F(JObject o, string key) =>
            (float?)o[key] ?? throw new FormatException($"Thiếu trường '{key}'.");

        // Làm tròn 4 chữ số: file gọn, và khớp độ chính xác hash của MapLayoutGenerator.
        private static float R(float v) => (float)Math.Round(v, 4);
    }
}
