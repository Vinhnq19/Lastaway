using System;
using System.Collections.Generic;
using System.IO;
using JungleBackend.Protocol;
using JungleMaze.App.Config;
using JungleMaze.App.Ports;
using JungleMaze.Core.Logging;

namespace JungleMaze.Infrastructure.Config
{
    /// <summary>
    /// Thứ tự nạp: -clientConfig path → -profile → AWS mặc định; sau đó áp env/command line.
    /// client.json vẫn lưu được, nhưng chỉ nạp lại khi được chỉ định để lần mở thường luôn về AWS.
    /// </summary>
    public sealed class FileConfigProvider : IConfigProvider
    {
        public const string ProfileFolder = "JungleMaze";
        public const string UserFileName = "client.json";
        public const string DefaultProfile = "phase2-aws";

        private readonly string _profileDirectory;
        private readonly string _userFilePath;
        private readonly ILog _log;
        private readonly List<string> _profiles = new List<string>();

        public FileConfigProvider(string streamingAssetsPath, string persistentDataPath, string[] commandLine,
            Func<string, string> environment, ILog log)
        {
            _log = log ?? NullLog.Instance;
            _profileDirectory = Path.Combine(streamingAssetsPath, ProfileFolder);
            _userFilePath = Path.Combine(persistentDataPath, UserFileName);
            ScanProfiles();

            var overrides = ConfigOverrides.From(commandLine, environment);
            CommandLineRoomCode = overrides.RoomCode;
            CommandLineDisplayName = overrides.DisplayName;

            string error;
            ClientConfig loaded;
            if (!string.IsNullOrEmpty(overrides.ConfigPath)) error = LoadFile(overrides.ConfigPath, "cmd:" + overrides.ConfigPath, out loaded);
            else error = LoadProfile(overrides.Profile ?? DefaultProfile, out loaded);

            if (error == null) error = overrides.Apply(loaded, out loaded);
            if (error != null)
            {
                _log.Error("[Config] " + error);
                LastError = error;
                // Vẫn cấp một config để UI hiện và người dùng sửa; Validate() sẽ chặn Connect.
                loaded = loaded ?? Fallback(error);
            }
            Current = loaded;
            _log.Info($"[Config] {Current} source={Current.Source}");
        }

        public ClientConfig Current { get; private set; }
        public IReadOnlyList<string> Profiles => _profiles;
        public string CommandLineRoomCode { get; }
        public string CommandLineDisplayName { get; }
        public string LastError { get; private set; }
        public string UserFilePath => _userFilePath;

        public string SelectProfile(string profile)
        {
            var error = LoadProfile(profile, out var loaded);
            if (error != null) { LastError = error; return error; }
            Current = loaded;
            LastError = null;
            return null;
        }

        public string SaveUserConfig(ClientConfig config)
        {
            var validation = config.Validate();
            if (validation != null) return validation;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_userFilePath));
                File.WriteAllText(_userFilePath, ClientConfigJson.Serialize(config));
                Current = config.WithEndpoint(config.GameServerHost, config.GameServerPort, config.Mode, "user:client.json");
                LastError = null;
                return null;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                return "Không ghi được client.json: " + e.Message;
            }
        }

        private void ScanProfiles()
        {
            if (!Directory.Exists(_profileDirectory)) return;
            foreach (var file in Directory.GetFiles(_profileDirectory, "client.*.json"))
            {
                var name = Path.GetFileNameWithoutExtension(file).Substring("client.".Length);
                if (name.Length > 0) _profiles.Add(name);
            }
            // Keep legacy files loadable by CLI, but show one current preset per environment.
            foreach (var alias in new[] { "local", "lan", "aws", "online" })
                if (_profiles.Contains("phase2-" + (alias == "online" ? "vpn" : alias)))
                    _profiles.Remove(alias);
            _profiles.Sort(StringComparer.OrdinalIgnoreCase);
        }

        private string LoadProfile(string profile, out ClientConfig config)
        {
            config = null;
            if (string.IsNullOrWhiteSpace(profile) || profile.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                return $"Tên profile không hợp lệ '{profile}'.";
            var path = Path.Combine(_profileDirectory, $"client.{profile}.json");
            return LoadFile(path, "profile:" + profile, out config);
        }

        private static string LoadFile(string path, string source, out ClientConfig config)
        {
            config = null;
            string json;
            try { json = File.ReadAllText(path); }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException
                                      || e is NotSupportedException)
            { return $"Không đọc được config {source}: {e.Message}"; }
            return ClientConfigJson.TryParse(json, source, out config, out var error) ? null : error;
        }

        private static ClientConfig Fallback(string error) =>
            new ClientConfig(ConnectionMode.Local, null, "127.0.0.1", 7777, JoinAuthMode.RoomCode,
                NetworkProtocol.Version, 5000, "Udp", "fallback (" + error + ")");
    }
}
