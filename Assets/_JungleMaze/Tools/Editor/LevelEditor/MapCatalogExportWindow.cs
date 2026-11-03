using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using JungleMaze.LevelEditor;
using JungleMaze.World;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace JungleMaze.EditorTools
{
    public sealed class MapCatalogExportWindow : EditorWindow
    {
        private string _id = "map-moi", _name = "Map mới", _description = "", _version = "1.0.0", _file = "", _status = "";
        private bool _default;
        [MenuItem("Jungle Maze/Level Editor/Export Published Map")]
        public static void Open() => GetWindow<MapCatalogExportWindow>("Xuất map phát hành");
        private void OnGUI()
        {
            EditorGUILayout.HelpBox("Xuất bản đồ đang mở trong Level Editor (Play), hoặc chọn JSON. Dữ liệu nguồn và scene không bị ghi đè. Publish/reload server là bước riêng.", MessageType.Info);
            _id = EditorGUILayout.TextField("Map ID ổn định", _id);
            _name = EditorGUILayout.TextField("Tên", _name);
            _description = EditorGUILayout.TextField("Mô tả", _description);
            _version = EditorGUILayout.TextField("Phiên bản", _version);
            _default = EditorGUILayout.Toggle("Map mặc định", _default);
            _file = EditorGUILayout.TextField("JSON (khi không Play)", _file);
            if (GUILayout.Button("Chọn JSON…")) _file = EditorUtility.OpenFilePanel("Map JSON", Application.dataPath, "json");
            if (GUILayout.Button("Kiểm tra và xuất map + preview vào catalog"))
            {
                try
                {
                    var controller = Application.isPlaying ? FindAnyObjectByType<LevelEditorController>() : null;
                    var json = controller != null ? controller.Document.ToJson() : File.ReadAllText(_file);
                    // Work on a copy: exporting another version never mutates the level being edited.
                    // Grid placements need catalog footprints/foot colliders; a default parse would turn them into 1x1 blocks.
                    var catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>("Assets/_JungleMaze/Art/LevelEditor/LevelEditorCatalog.asset");
                    if (catalog == null) throw new InvalidOperationException("Thiếu Level Editor catalog; không thể xác minh footprint.");
                    var document = LevelDocument.FromJson(json, catalog.BaseSizeOf, catalog.ShapeOf); document.Version = _version;
                    var root = Path.GetFullPath(Path.Combine(Application.dataPath, "../Backend/Shared/GameData/Maps"));
                    Export(document, root, _id, _name, _description, _default);
                    _status = "Đã xuất. Chạy validate → publish → reload; client sẽ tải bản mới trước trận tiếp theo.";
                }
                catch (Exception e) { _status = "Không xuất: " + e.Message; }
            }
            EditorGUILayout.HelpBox(_status, MessageType.None);
        }

        public static void Export(LevelDocument document, string directory, string id, string name, string description, bool makeDefault)
        {
            if (!Regex.IsMatch(id ?? "", @"^[a-z0-9][a-z0-9-]{0,31}\z")) throw new ArgumentException("Map ID chỉ gồm chữ thường, số, dấu -, tối đa 32 ký tự.");
            if (string.IsNullOrWhiteSpace(name) || name.Length > 64 || (description ?? "").Length > 256 ||
                string.IsNullOrWhiteSpace(document.Version) || document.Version.Length > 32) throw new ArgumentException("Tên/mô tả/phiên bản không hợp lệ.");
            if (document.Width != 50 || document.Height != 50) throw new ArgumentException("Map phát hành hiện phải là 50×50.");
            var error = document.Validate(out _); if (error != null) throw new ArgumentException(error);
            var json = document.ToJson().Replace("\r\n", "\n");
            var authored = JsonConvert.DeserializeObject<AuthoredMapDocument>(json);
            var layout = authored.Build(LevelDocument.GameWorldVersion);
            if (layout.Spawns.Count != 10) throw new ArgumentException("Map cần đúng 10 spawn.");
            var mapBytes = Encoding.UTF8.GetBytes(json); if (mapBytes.Length > 2 * 1024 * 1024) throw new ArgumentException("Map vượt 2 MiB.");
            var preview = Preview(layout);
            var mapHash = Hash(mapBytes); var previewHash = Hash(preview);
            Directory.CreateDirectory(directory);
            var catalogFile = Path.Combine(directory, "catalog.json");
            var catalog = File.Exists(catalogFile) ? JObject.Parse(File.ReadAllText(catalogFile)) : new JObject { ["schemaVersion"] = 1, ["generation"] = 0, ["maps"] = new JArray() };
            var entries = (JArray)catalog["maps"];
            foreach (var old in entries.Where(e => (string)e["id"] == id).ToArray()) old.Remove();
            entries.Add(new JObject { ["id"] = id, ["name"] = name, ["description"] = description ?? "", ["version"] = document.Version, ["mapHash"] = mapHash, ["previewHash"] = previewHash });
            if (makeDefault || string.IsNullOrEmpty((string)catalog["defaultMapId"])) catalog["defaultMapId"] = id;
            if (!entries.Any(e => (string)e["id"] == (string)catalog["defaultMapId"])) throw new ArgumentException("Catalog thiếu map mặc định.");
            catalog["generation"] = checked((long)catalog["generation"] + 1);
            var target = Path.Combine(directory, id, mapHash); Directory.CreateDirectory(target);
            ImmutableWrite(Path.Combine(target, "map.json"), mapBytes);
            ImmutableWrite(Path.Combine(target, previewHash + ".png"), preview);
            // The backend CLI runs its full authoritative validator before activation; export is only an authoring source.
            var temp = Path.Combine(directory, ".catalog-" + Guid.NewGuid().ToString("N"));
            try
            {
                File.WriteAllText(temp, catalog.ToString(Formatting.Indented) + "\n", new UTF8Encoding(false));
                if (File.Exists(catalogFile)) File.Replace(temp, catalogFile, null); else File.Move(temp, catalogFile);
            }
            finally { if (File.Exists(temp)) File.Delete(temp); }
        }
        private static void ImmutableWrite(string path, byte[] bytes)
        {
            if (File.Exists(path)) { if (!File.ReadAllBytes(path).SequenceEqual(bytes)) throw new IOException("Phiên bản bất biến đã có dữ liệu khác."); return; }
            File.WriteAllBytes(path, bytes);
        }
        private static string Hash(byte[] bytes)
        { using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant(); }
        public static byte[] Preview(MapLayout layout)
        {
            const int size = 512;
            var pixels = Enumerable.Repeat((Color32)new Color(.22f, .3f, .22f), size * size).ToArray();
            foreach (var item in layout.Objects)
            {
                var b = item.Bounds;
                var color = (byte)item.Kind <= 4 ? new Color(.5f, .52f, .46f) : item.Kind == WorldObjectKind.Portal ? Color.magenta :
                    item.Kind == WorldObjectKind.Lava ? new Color(.9f, .25f, .08f) : item.Kind == WorldObjectKind.Swamp || item.Kind == WorldObjectKind.Poison ? new Color(.23f, .5f, .3f) : new Color(.65f, .48f, .2f);
                for (var y = Mathf.Clamp((int)(b.MinY / layout.Size * size), 0, size - 1); y < Mathf.Clamp(Mathf.CeilToInt(b.MaxY / layout.Size * size), 0, size); y++)
                    for (var x = Mathf.Clamp((int)(b.MinX / layout.Size * size), 0, size - 1); x < Mathf.Clamp(Mathf.CeilToInt(b.MaxX / layout.Size * size), 0, size); x++) pixels[y * size + x] = color;
            }
            foreach (var spawn in layout.Spawns)
                for (var y = -3; y <= 3; y++) for (var x = -3; x <= 3; x++)
                    pixels[Mathf.Clamp((int)(spawn.Y / layout.Size * size) + y, 0, size - 1) * size + Mathf.Clamp((int)(spawn.X / layout.Size * size) + x, 0, size - 1)] = Color.cyan;
            var image = new Texture2D(size, size, TextureFormat.RGBA32, false);
            try { image.SetPixels32(pixels); image.Apply(); return image.EncodeToPNG(); }
            finally { DestroyImmediate(image); }
        }
    }
}
