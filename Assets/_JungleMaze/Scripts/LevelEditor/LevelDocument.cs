using System;
using System.Collections.Generic;
using System.Globalization;
using JungleMaze.World;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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

    /// <summary>
    /// Một vật thể đặt trên map. Vị trí tự do (không theo cell); footprint = kích thước gốc × Scale, xoay theo bước
    /// 90° nên vẫn là AABB như game yêu cầu. FlipX chỉ là hình ảnh, không đổi footprint.
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

        public float Width => (Rotation % 2 == 0 ? BaseWidth : BaseHeight) * Scale;
        public float Height => (Rotation % 2 == 0 ? BaseHeight : BaseWidth) * Scale;
        public WorldBounds Bounds => new WorldBounds(X, Y, Width, Height);
        public bool IsPortal => Group == LevelItemGroup.WorldObject && Type == nameof(WorldObjectKind.Portal);

        public LevelPlacement Clone() => (LevelPlacement)MemberwiseClone();
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
        public int GroundTile;
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
            foreach (var p in Placements)
            {
                var b = p.Bounds;
                switch (p.Group)
                {
                    case LevelItemGroup.Spawn:
                        spawns.Add(new JObject { ["x"] = R(p.X), ["y"] = R(p.Y) });
                        break;
                    case LevelItemGroup.Cover:
                        obstacles.Add(Rect(p, b));
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
                ["editor"] = new JObject { ["tool"] = "JungleMaze.LevelEditor", ["groundTile"] = GroundTile },
            };
            return root.ToString(Formatting.Indented);
        }

        /// <summary>
        /// Đọc file do editor ghi hoặc <c>map_demo.json</c> gốc. <paramref name="baseSize"/> trả kích thước gốc của
        /// một loại (từ catalog) để tính lại Scale; trả null thì coi footprint trong file là kích thước gốc.
        /// </summary>
        public static LevelDocument FromJson(string json, Func<string, (float width, float height)?> baseSize = null)
        {
            var root = JObject.Parse(json);
            var doc = new LevelDocument { Version = (string)root["version"] ?? "1.0.0" };
            var bounds = root["mapBounds"] as JObject ?? throw new FormatException("Thiếu mapBounds.");
            doc.Width = (int)Math.Round(F(bounds, "maxX") - F(bounds, "minX"));
            doc.Height = (int)Math.Round(F(bounds, "maxY") - F(bounds, "minY"));
            if (root["editor"] is JObject editor) doc.GroundTile = (int?)editor["groundTile"] ?? 0;

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

        private static JObject Rect(LevelPlacement p, WorldBounds b)
        {
            var o = new JObject
            {
                ["name"] = p.Type,
                ["centerX"] = R(p.X), ["centerY"] = R(p.Y), ["sizeX"] = R(p.Width), ["sizeY"] = R(p.Height),
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
