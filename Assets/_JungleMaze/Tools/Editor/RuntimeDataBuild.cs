using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace JungleMaze.EditorTools
{
    /// <summary>Canonical backend data is copied explicitly, then verified before every player build.</summary>
    public sealed class RuntimeDataBuild : IPreprocessBuildWithReport
    {
        private static readonly string[] Files = { "map_demo.json", "match_settings.json", "zone_schedule.json", "world_settings.json", "items_catalog.json", "weapons.json" };
        private static string Source => Path.GetFullPath(Path.Combine(Application.dataPath, "../Backend/Shared/GameData"));
        private static string Target => Path.Combine(Application.streamingAssetsPath, "JungleMaze/runtime");
        public int callbackOrder => -1000;

        /// <summary>Tên file level (trong Assets/_JungleMaze/Levels) dùng cho Luyện tập offline; trống = offline dùng map_demo.</summary>
        private const string OfflineMapSetting = "Assets/_JungleMaze/Levels/offline_map.txt";
        private const string OfflineMapFile = "offline_map.json";

        [MenuItem("Jungle Maze/Phase 2/Sync Runtime Data")]
        public static void Sync()
        {
            Directory.CreateDirectory(Target);
            foreach (var name in Files) File.Copy(Path.Combine(Source, name), Path.Combine(Target, name), true);
            File.WriteAllText(Path.Combine(Target, "manifest.json"), Manifest(), new UTF8Encoding(false));
            var offline = OfflineMapSource();
            var target = Path.Combine(Target, OfflineMapFile);
            if (offline != null) File.Copy(offline, target, true);
            else if (File.Exists(target)) File.Delete(target);
            AssetDatabase.Refresh();
            Debug.Log("[RuntimeData] Synced canonical match/map/zone and SHA256 manifest." + (offline != null ? " Offline map: " + offline : ""));
        }

        /// <summary>Chọn file level JSON trong Project rồi dùng menu này: Luyện tập offline sẽ chơi map đó (server vẫn dùng map_demo).</summary>
        [MenuItem("Jungle Maze/Level Editor/Use Selected Level As Offline Map")]
        public static void UseSelectedAsOfflineMap()
        {
            var path = Selection.activeObject != null ? AssetDatabase.GetAssetPath(Selection.activeObject) : null;
            if (string.IsNullOrEmpty(path) || !path.EndsWith(".json") || !path.StartsWith("Assets/_JungleMaze/Levels/"))
            { Debug.LogWarning("[RuntimeData] Chọn một file .json trong Assets/_JungleMaze/Levels trước."); return; }
            File.WriteAllText(OfflineMapSetting, Path.GetFileNameWithoutExtension(path) + "\n", new UTF8Encoding(false));
            Sync();
        }

        /// <summary>Đường dẫn file level offline đã chọn, hoặc null nếu chưa chọn.</summary>
        private static string OfflineMapSource()
        {
            if (!File.Exists(OfflineMapSetting)) return null;
            var name = File.ReadAllText(OfflineMapSetting).Trim();
            if (name.Length == 0) return null;
            var path = Path.Combine("Assets/_JungleMaze/Levels", name + ".json");
            if (!File.Exists(path)) throw new BuildFailedException("Offline map không tồn tại: " + path);
            return path;
        }

        [MenuItem("Jungle Maze/Phase 2/Verify Runtime Data")]
        public static void Verify()
        {
            foreach (var name in Files)
                if (!File.Exists(Path.Combine(Target, name)) || !File.ReadAllBytes(Path.Combine(Source, name)).SequenceEqual(File.ReadAllBytes(Path.Combine(Target, name))))
                    throw new BuildFailedException("Runtime data is outdated. Use Jungle Maze > Phase 2 > Sync Runtime Data.");
            if (!File.Exists(Path.Combine(Target, "manifest.json")) || File.ReadAllText(Path.Combine(Target, "manifest.json")) != Manifest())
                throw new BuildFailedException("Runtime data manifest is outdated; sync runtime data first.");
            var offline = OfflineMapSource();
            var target = Path.Combine(Target, OfflineMapFile);
            if (offline != null && (!File.Exists(target) || !File.ReadAllBytes(offline).SequenceEqual(File.ReadAllBytes(target))))
                throw new BuildFailedException("Offline map is outdated; sync runtime data first.");
        }

        public void OnPreprocessBuild(BuildReport report) => Verify();

        private static string Manifest()
        {
            var settings = JsonUtility.FromJson<VersionInfo>(File.ReadAllText(Path.Combine(Source, "match_settings.json")));
            var text = new StringBuilder("{\n  \"version\": \"").Append(settings.version).Append("\",\n  \"files\": {\n");
            using (var sha = SHA256.Create())
                for (var i = 0; i < Files.Length; i++)
                {
                    var hash = BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(Path.Combine(Source, Files[i])))).Replace("-", "").ToLowerInvariant();
                    text.Append("    \"").Append(Files[i]).Append("\": \"").Append(hash).Append(i == Files.Length - 1 ? "\"\n" : "\",\n");
                }
            return text.Append("  }\n}\n").ToString();
        }
        [Serializable] private sealed class VersionInfo { public string version; }
    }
}
