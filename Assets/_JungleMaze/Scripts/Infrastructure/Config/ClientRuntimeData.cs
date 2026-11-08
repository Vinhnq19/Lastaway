using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using JungleMaze.App.Match;
using JungleMaze.App.Ports;
using Newtonsoft.Json.Linq;

namespace JungleMaze.Infrastructure.Config
{
    /// <summary>Validate the packaged copy against both its manifest and the server's canonical hash before LoadingAck.</summary>
    public sealed class ClientRuntimeData : IClientRuntimeData
    {
        private readonly string _directory;
        private SelectedMapData _selected;
        public JungleMaze.World.MapLayout SelectedLayout => _selected?.Layout;
        public string SelectedMapHash => _selected?.MapHash;
        public void ClearSelectedMap() => _selected = null;
        public string ActivateSelectedMap(string json, string mapHash, string rulesHash)
        {
            try
            {
                var prepared = SelectedMapData.Prepare(json, mapHash, rulesHash, _directory);
                var oldZone = ZoneSchedule;
                try { TryLoadZoneSchedule(); }
                catch { ZoneSchedule = oldZone; throw; }
                Bounds = prepared.Bounds; Covers = prepared.Covers; PlayerSpeed = prepared.PlayerSpeed; PlayerRadius = prepared.PlayerRadius;
                _selected = prepared;
                return null;
            }
            catch (Exception e) when (!(e is OutOfMemoryException)) { return SelectedMapData.ValidationMessage(e); }
        }
        public IReadOnlyList<RuntimeCover> Covers { get; private set; } = Array.Empty<RuntimeCover>();
        public RuntimeMapBounds Bounds { get; private set; }
        public float PlayerSpeed { get; private set; }
        public float PlayerRadius { get; private set; }
        public ZoneSchedule ZoneSchedule { get; private set; }

        public ClientRuntimeData(string streamingAssets)
        {
            _directory = Path.Combine(streamingAssets, "JungleMaze", "runtime");
        }

        public string Verify(string expectedVersion, string expectedHash)
        {
            if (_selected != null)
                return _selected.MatchVersion == expectedVersion && _selected.RulesHash == expectedHash ? null : "Map/luật đã chọn không khớp server.";
            try
            {
                var manifest = JObject.Parse(File.ReadAllText(Path.Combine(_directory, "manifest.json")));
                if ((string)manifest["version"] != expectedVersion) return "Dữ liệu trận khác phiên bản server; cập nhật client.";
                using (var aggregate = SHA256.Create())
                using (var stream = new MemoryStream())
                {
                    foreach (var name in new[] { "map_demo.json", "match_settings.json", "zone_schedule.json" })
                    {
                        var bytes = File.ReadAllBytes(Path.Combine(_directory, name));
                        using (var fileHash = SHA256.Create())
                            if (Hex(fileHash.ComputeHash(bytes)) != (string)manifest["files"][name]) return "Dữ liệu trận bị thay đổi; đóng gói lại client.";
                        var label = Encoding.UTF8.GetBytes(name);
                        stream.Write(label, 0, label.Length);
                        // Canonical aggregate is independent of Windows/Linux line endings and UTF-8 BOM.
                        var normalized = Encoding.UTF8.GetBytes(File.ReadAllText(Path.Combine(_directory, name)).Replace("\r\n", "\n"));
                        stream.Write(normalized, 0, normalized.Length);
                    }
                    if (Hex(aggregate.ComputeHash(stream.ToArray())) != expectedHash) return "Map/luật trận không khớp server; cập nhật client.";
                }
                var covers = ReadGeometry(Path.Combine(_directory, RuntimeDataFiles.ServerMap), out var mapBounds);
                Bounds = mapBounds;
                var settings = JObject.Parse(File.ReadAllText(Path.Combine(_directory, "match_settings.json")));
                var speed = (float)settings["playerSpeed"];
                var radius = (float)settings["playerRadius"];
                if (!(speed > 0) || !(radius > 0) || float.IsInfinity(speed) || float.IsInfinity(radius))
                    return "Tốc độ/bán kính người chơi trong dữ liệu trận không hợp lệ.";
                Covers = covers;
                PlayerSpeed = speed;
                PlayerRadius = radius;
                TryLoadZoneSchedule();
                return null;
            }
            catch (Exception e) when (!(e is OutOfMemoryException)) { return "Thiếu hoặc sai dữ liệu trận; chạy công cụ đồng bộ dữ liệu trước khi build."; }
        }

        public string LoadLocal()
        {
            ClearSelectedMap();
            try
            {
                var manifest = JObject.Parse(File.ReadAllText(Path.Combine(_directory, "manifest.json")));
                using (var hash = SHA256.Create())
                using (var stream = new MemoryStream())
                {
                    foreach (var name in new[] { "map_demo.json", "match_settings.json", "zone_schedule.json" })
                    {
                        var label = Encoding.UTF8.GetBytes(name);
                        var bytes = Encoding.UTF8.GetBytes(File.ReadAllText(Path.Combine(_directory, name)).Replace("\r\n", "\n"));
                        stream.Write(label, 0, label.Length); stream.Write(bytes, 0, bytes.Length);
                    }
                    var error = Verify((string)manifest["version"], Hex(hash.ComputeHash(stream.ToArray())));
                    if (error != null) return error;
                }
                // Luyện tập offline chơi map offline riêng (nếu có): ranh giới và cover theo map đó, luật trận giữ nguyên.
                var offline = Path.Combine(_directory, RuntimeDataFiles.OfflineMap);
                if (File.Exists(offline))
                {
                    Covers = ReadGeometry(offline, out var bounds);
                    Bounds = bounds;
                }
                return null;
            }
            catch (Exception e) when (!(e is OutOfMemoryException)) { return "Invalid offline data; Sync Runtime Data before playing."; }
        }

        /// <summary>Ranh giới map và các hard cover ("obstacles") của một file map.</summary>
        private static List<RuntimeCover> ReadGeometry(string path, out RuntimeMapBounds bounds)
        {
            var map = JObject.Parse(File.ReadAllText(path));
            var b = map["mapBounds"];
            bounds = new RuntimeMapBounds { MinX = (float)b["minX"], MinY = (float)b["minY"], MaxX = (float)b["maxX"], MaxY = (float)b["maxY"] };
            var covers = new List<RuntimeCover>();
            foreach (var item in (JArray)map["obstacles"])
                covers.Add(new RuntimeCover { Name = (string)item["name"], MinX = (float)item["minX"], MinY = (float)item["minY"],
                    MaxX = (float)item["maxX"], MaxY = (float)item["maxY"] });
            return covers;
        }

        private void TryLoadZoneSchedule()
        {
            try
            {
                var path = Path.Combine(_directory, "zone_schedule.json");
                if (!File.Exists(path)) throw new FileNotFoundException("Missing zone data.");
                var root = JObject.Parse(File.ReadAllText(path));
                var s = new ZoneSchedule
                {
                    Version = (string)root["version"] ?? "1.0.0",
                    InitialRadius = (float)root["initialRadius"],
                    WarningDurationSeconds = (double)root["warningDurationSeconds"],
                    ShrinkSpeedCellsPerSecond = (float)root["shrinkSpeedCellsPerSecond"],
                    DamagePerSecond = (int)root["damagePerSecond"]
                };
                if (root["initialCenter"] != null)
                {
                    s.InitialCenter = (
                        (float)root["initialCenter"]["x"],
                        (float)root["initialCenter"]["y"]
                    );
                }
                if (root["intervals"] is JArray intervals)
                {
                    foreach (var elem in intervals)
                    {
                        s.Intervals.Add(new ZoneInterval
                        {
                            Index = (int)elem["index"],
                            StartSecond = (double)elem["startSecond"],
                            EndSecond = (double)elem["endSecond"],
                            StartRadius = (float)elem["startRadius"],
                            EndRadius = (float)elem["endRadius"]
                        });
                    }
                }
                s.Validate();
                ZoneSchedule = s;
            }
            catch (Exception)
            {
                throw new InvalidDataException("Invalid zone data.");
            }
        }

        private static string Hex(byte[] bytes) => BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
    }
}
