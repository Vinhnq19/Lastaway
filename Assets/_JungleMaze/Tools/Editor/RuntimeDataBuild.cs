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
    public sealed class RuntimeDataBuild : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        private static readonly string[] Files = { "map_demo.json", "match_settings.json", "zone_schedule.json", "world_settings.json", "items_catalog.json", "weapons.json", "airdrop_schedule.json" };
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

        private static string _registrySourceHash;

        public void OnPreprocessBuild(BuildReport report)
        {
            Verify();
            if (report.summary.platform == BuildTarget.StandaloneWindows64) _registrySourceHash = RegistrySourceHash();
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.StandaloneWindows64) return;
            // Unity may still hold temporary build settings here; Local/publish gates compare these pre-build inputs after the Editor restores them.
            if (string.IsNullOrEmpty(_registrySourceHash))
                throw new BuildFailedException("Missing pre-build source fingerprint; rebuild before registry acceptance.");
            var player = Path.GetFullPath(report.summary.outputPath);
            var root = Path.GetDirectoryName(player);
            var files = new System.Collections.Generic.List<string> { player };
            foreach (var name in new[] { "UnityPlayer.dll", "UnityCrashHandler64.exe" })
                if (File.Exists(Path.Combine(root, name))) files.Add(Path.Combine(root, name));
            var data = Path.Combine(root, Path.GetFileNameWithoutExtension(player) + "_Data");
            if (Directory.Exists(data)) files.AddRange(Directory.GetFiles(data, "*", SearchOption.AllDirectories));
            var evidence = new RegistryBuildInfo { schemaVersion = 1, sourceHash = _registrySourceHash,
                playerFingerprint = RegistryHash(root, files.ToArray()), unityVersion = Application.unityVersion };
            File.WriteAllText(Path.Combine(root, "registry-build.json"), JsonUtility.ToJson(evidence, true), new UTF8Encoding(false));
        }

        private static string RegistrySourceHash()
        {
            var root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var files = new System.Collections.Generic.List<string>();
            var extensions = new System.Collections.Generic.HashSet<string>(new[] { ".cs", ".asmdef", ".asmref", ".unity", ".prefab", ".asset", ".json", ".shader", ".hlsl", ".cginc", ".uss", ".uxml", ".txt", ".bytes", ".csv", ".xml", ".dll" });
            files.AddRange(Directory.GetFiles(Path.Combine(root, "Assets"), "*", SearchOption.AllDirectories)
                .Where(path => extensions.Contains(Path.GetExtension(path)) || path.EndsWith(".cs.meta") || path.EndsWith(".asmdef.meta") || path.EndsWith(".asmref.meta")));
            foreach (var folder in new[] { "Packages", "ProjectSettings", "Backend/Shared" })
                files.AddRange(Directory.GetFiles(Path.Combine(root, folder), "*", SearchOption.AllDirectories)
                    .Where(path => !path.Replace('\\', '/').Split('/').Any(part => part == "bin" || part == "obj" || part == "__pycache__" || part == ".git")));
            return RegistryHash(root, files.ToArray());
        }

        private static byte[] RegistryBytes(string file)
        {
            var bytes = File.ReadAllBytes(file);
            if (!file.EndsWith(".meta", StringComparison.Ordinal)) return bytes;
            // These disabled non-Windows64 platform entries are added by Unity while building; they cannot affect this player.
            var text = Encoding.UTF8.GetString(bytes).TrimStart('\uFEFF').Replace("\r\n", "\n");
            text = System.Text.RegularExpressions.Regex.Replace(text,
                @"  - serializedVersion: 4\n    buildTarget: (?:WebGL|WindowsStoreApps)\n(?:    [^\n]*\n)*",
                match => match.Value.Contains("    overridden: 0\n") ? "" : match.Value);
            return Encoding.UTF8.GetBytes(text);
        }

        private static string RegistryHash(string root, string[] files)
        {
            Array.Sort(files, StringComparer.Ordinal);
            var text = new StringBuilder();
            using (var sha = SHA256.Create())
            {
                foreach (var file in files)
                {
                    var relative = file.Substring(root.TrimEnd(Path.DirectorySeparatorChar).Length + 1).Replace('\\', '/');
                    text.Append(relative).Append(':').Append(BitConverter.ToString(sha.ComputeHash(RegistryBytes(file))).Replace("-", "").ToLowerInvariant()).Append('\n');
                }
                return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text.ToString()))).Replace("-", "").ToLowerInvariant();
            }
        }

        [Serializable] private sealed class RegistryBuildInfo
        {
            public int schemaVersion;
            public string sourceHash, playerFingerprint, unityVersion;
        }

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
