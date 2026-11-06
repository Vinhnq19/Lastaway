using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using JungleMaze.App.Ports;
using JungleMaze.World;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JungleMaze.Infrastructure.Config
{
    /// <summary>Pure validation/preparation of one selected map; built only after every check passes.</summary>
    public sealed class SelectedMapData
    {
        const int MaxBytes = 2 * 1024 * 1024, RequiredSpawns = 10;
        static readonly string[] RuleFiles = { "match_settings.json", "zone_schedule.json", "world_settings.json", "items_catalog.json", "weapons.json" };

        public MapLayout Layout { get; }
        public RuntimeMapBounds Bounds { get; }
        public IReadOnlyList<RuntimeCover> Covers { get; }
        public string MapHash { get; }
        public string RulesHash { get; }
        public string MatchVersion { get; }
        public float PlayerSpeed { get; }
        public float PlayerRadius { get; }

        SelectedMapData(MapLayout layout, RuntimeMapBounds bounds, IReadOnlyList<RuntimeCover> covers, string mapHash,
            string rulesHash, string matchVersion, float playerSpeed, float playerRadius)
        {
            Layout = layout; Bounds = bounds; Covers = covers; MapHash = mapHash; RulesHash = rulesHash;
            MatchVersion = matchVersion; PlayerSpeed = playerSpeed; PlayerRadius = playerRadius;
        }

        public static SelectedMapData Prepare(string json, string expectedMapHash, string expectedRulesHash, string rulesDirectory)
        {
            if (string.IsNullOrEmpty(rulesDirectory)) throw new InvalidDataException("Rules directory is missing.");

            var mapText = NormalizeText(json, "Map");
            var mapHash = Sha256Hex(Encoding.UTF8.GetBytes(mapText));
            if (!string.Equals(mapHash, (expectedMapHash ?? string.Empty).Trim(), StringComparison.Ordinal))
                throw new InvalidDataException("Map hash does not match the expected map hash.");

            string matchSettingsText = null, worldSettingsText = null, rulesHash;
            using (var rules = new MemoryStream())
            {
                foreach (var file in RuleFiles)
                {
                    var normalized = NormalizeText(ReadRuleFile(rulesDirectory, file), file);
                    if (file == "match_settings.json") matchSettingsText = normalized;
                    if (file == "world_settings.json") worldSettingsText = normalized;
                    foreach (var bytes in new[] { Encoding.UTF8.GetBytes(file), Encoding.UTF8.GetBytes(normalized) })
                    {
                        rules.Write(bytes, 0, bytes.Length);
                        if (rules.Length > MaxBytes) throw new InvalidDataException("Rules data exceeds 2 MiB.");
                    }
                }
                rulesHash = Sha256Hex(rules.ToArray());
            }
            if (!string.Equals(rulesHash, (expectedRulesHash ?? string.Empty).Trim(), StringComparison.Ordinal))
                throw new InvalidDataException("Rules hash does not match the expected rules hash.");

            var worldVersion = ReadString(Parse(worldSettingsText, "world_settings.json"), "version");
            var matchSettings = Parse(matchSettingsText, "match_settings.json");
            var matchVersion = ReadString(matchSettings, "version");
            var playerSpeed = ReadFloat(matchSettings, "playerSpeed");
            var playerRadius = ReadFloat(matchSettings, "playerRadius");
            if (!IsPositiveFinite(playerSpeed)) throw new InvalidDataException("playerSpeed must be a positive finite number.");
            if (!IsPositiveFinite(playerRadius)) throw new InvalidDataException("playerRadius must be a positive finite number.");

            var mapNode = Parse(mapText, "Map");
            if (mapNode["mapBounds"] == null || mapNode["obstacles"] == null)
                throw new InvalidDataException("Map JSON requires mapBounds and obstacles.");

            AuthoredMapDocument document;
            try { document = JsonConvert.DeserializeObject<AuthoredMapDocument>(mapText); }
            catch (JsonException e) { throw new InvalidDataException("Map JSON is invalid.", e); }
            if (document == null || document.MapBounds == null || document.SpawnPoints == null || document.Obstacles == null)
                throw new InvalidDataException("Map data requires bounds, spawnPoints and obstacles.");
            var size = document.MapBounds.MaxX - document.MapBounds.MinX;
            if (document.SpawnPoints.Length != RequiredSpawns || !(size >= 30 && size <= 256) || size != Math.Floor(size) ||
                document.MapBounds.MaxY - document.MapBounds.MinY != size)
                throw new InvalidDataException($"Map requires a square size 30..256 and {RequiredSpawns} spawn points.");

            MapLayout layout;
            try { layout = document.Build(worldVersion); }
            catch (ArgumentException e) { throw new InvalidDataException("Map layout is invalid: " + e.Message, e); }
            if (layout == null || layout.Size != size || layout.Spawns.Count != RequiredSpawns)
                throw new InvalidDataException($"Built map must match its bounds with {RequiredSpawns} spawns.");

            var covers = new List<RuntimeCover>(document.Obstacles.Length);
            foreach (var cover in document.Obstacles)
            {
                if (cover == null) throw new InvalidDataException("Map contains a missing cover.");
                covers.Add(new RuntimeCover { Name = cover.Name, MinX = cover.MinX, MinY = cover.MinY, MaxX = cover.MaxX, MaxY = cover.MaxY });
            }
            var mapBounds = document.MapBounds;
            var bounds = new RuntimeMapBounds { MinX = mapBounds.MinX, MinY = mapBounds.MinY, MaxX = mapBounds.MaxX, MaxY = mapBounds.MaxY };

            return new SelectedMapData(layout, bounds, covers.AsReadOnly(), mapHash, rulesHash, matchVersion, playerSpeed, playerRadius);
        }

        static string ReadRuleFile(string directory, string file)
        {
            try { return File.ReadAllText(Path.Combine(directory, file)); }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            { throw new InvalidDataException("Cannot read rules file '" + file + "'.", e); }
        }

        static string NormalizeText(string text, string label)
        {
            if (text == null) throw new InvalidDataException(label + " data is missing.");
            if (text.Length > 0 && text[0] == '\uFEFF') text = text.Substring(1);
            text = text.Replace("\r\n", "\n");
            if (Encoding.UTF8.GetByteCount(text) > MaxBytes) throw new InvalidDataException(label + " data exceeds 2 MiB.");
            return text;
        }

        static JObject Parse(string text, string label)
        {
            try { return JObject.Parse(text); }
            catch (JsonException e) { throw new InvalidDataException(label + " JSON is invalid.", e); }
        }

        static string ReadString(JObject node, string field)
        {
            if (!(node[field] is JValue value) || value.Type != JTokenType.String || string.IsNullOrWhiteSpace((string)value.Value))
                throw new InvalidDataException("JSON requires a non-empty '" + field + "'.");
            return (string)value.Value;
        }

        static float ReadFloat(JObject node, string field)
        {
            var token = node[field];
            if (token == null || token.Type != JTokenType.Float && token.Type != JTokenType.Integer)
                throw new InvalidDataException("JSON requires a numeric '" + field + "'.");
            return (float)token;
        }

        static bool IsPositiveFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && value > 0f;

        static string Sha256Hex(byte[] data)
        {
            byte[] digest;
            using (var sha = SHA256.Create()) digest = sha.ComputeHash(data);
            var chars = new char[digest.Length * 2];
            for (var i = 0; i < digest.Length; i++)
            {
                chars[i * 2] = "0123456789abcdef"[digest[i] >> 4];
                chars[i * 2 + 1] = "0123456789abcdef"[digest[i] & 0xF];
            }
            return new string(chars);
        }
    }
}
