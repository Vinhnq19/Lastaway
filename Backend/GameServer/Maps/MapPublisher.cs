using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GameServer.Simulation;

namespace GameServer.Maps;

/// <summary>Immutable payloads first, catalog pointer last. A failed validation never replaces the active catalog.</summary>
public static class MapPublisher
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };
    public static long Publish(string source, string destination, string worldVersion)
    {
        var candidate = MapCatalogSnapshot.Load(source, worldVersion);
        destination = Path.GetFullPath(destination);
        Directory.CreateDirectory(destination);
        if ((File.GetAttributes(destination) & FileAttributes.ReparsePoint) != 0) throw new ArgumentException("Map destination must not be a link.");
        using var publishLock = new FileStream(Path.Combine(destination, ".publish.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var active = Path.Combine(destination, "catalog.json");
        var previous = File.Exists(active) ? MapCatalogSnapshot.Load(destination, worldVersion) : null;
        var generation = Math.Max(candidate.Generation, checked((previous?.Generation ?? 0) + 1));
        var staging = Path.Combine(destination, ".staging-" + Guid.NewGuid().ToString("N"));
        if (!string.Equals(Path.GetDirectoryName(Path.GetFullPath(staging)), destination, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Invalid staging path.");
        Directory.CreateDirectory(staging);
        try
        {
            var manifest = new MapCatalogManifest { Generation = generation, DefaultMapId = candidate.DefaultMapId, Maps = candidate.Maps.Values.Select(m => m.Entry).ToArray() };
            foreach (var map in candidate.Maps.Values)
            {
                var entry = map.Entry;
                var directory = Path.Combine(staging, entry.Id, entry.MapHash);
                Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory, "map.json"), map.Json, new UTF8Encoding(false));
                File.WriteAllBytes(Path.Combine(directory, entry.PreviewHash + ".png"), map.Preview);
            }
            File.WriteAllText(Path.Combine(staging, "catalog.json"), JsonSerializer.Serialize(manifest, Json), new UTF8Encoding(false));
            MapCatalogSnapshot.Load(staging, worldVersion);
            foreach (var map in candidate.Maps.Values)
            {
                var entry = map.Entry;
                var directory = Path.Combine(destination, entry.Id, entry.MapHash);
                foreach (var part in new[] { Path.Combine(destination, entry.Id), directory })
                {
                    Directory.CreateDirectory(part);
                    if ((File.GetAttributes(part) & FileAttributes.ReparsePoint) != 0) throw new ArgumentException("Map destination contains a link.");
                }
                foreach (var file in new[] { "map.json", entry.PreviewHash + ".png" })
                {
                    var from = Path.Combine(staging, entry.Id, entry.MapHash, file); var to = Path.Combine(directory, file);
                    if (File.Exists(to))
                    {
                        if ((File.GetAttributes(to) & FileAttributes.ReparsePoint) != 0 || !File.ReadAllBytes(from).AsSpan().SequenceEqual(File.ReadAllBytes(to)))
                            throw new IOException("Immutable map payload already exists with different bytes.");
                    }
                    else File.Move(from, to);
                }
            }
            // Same filesystem rename: clients see either the old complete package or the new complete package.
            if (previous is not null)
            {
                var history = Path.Combine(destination, "catalog-history"); Directory.CreateDirectory(history);
                if ((File.GetAttributes(history) & FileAttributes.ReparsePoint) != 0) throw new IOException("Catalog history must not be a link.");
                var backup = Path.Combine(history, previous.Generation + ".json");
                if (!File.Exists(backup)) File.Copy(active, backup);
            }
            File.Move(Path.Combine(staging, "catalog.json"), active, true);
            return generation;
        }
        finally { Directory.Delete(staging, true); }
    }

    public static async Task<int> Run(string[] args)
    {
        try
        {
            var worldVersion = WorldSettings.Load(GameDataLoader.ReadGameDataFile("world_settings.json")).Version;
            if (args.Length == 3 && args[1] == "validate")
            { var maps = MapCatalogSnapshot.Load(args[2], worldVersion); Console.WriteLine($"Valid: generation={maps.Generation} maps={maps.Maps.Count}"); return 0; }
            if (args.Length == 4 && args[1] == "publish")
            { Console.WriteLine($"Published generation={Publish(args[2], args[3], worldVersion)}. Reload through the private control endpoint."); return 0; }
            if (args.Length == 2 && args[1] == "reload")
            {
                var key = Environment.GetEnvironmentVariable("ROOM_CONTROL_TOKEN") ?? "";
                if (Encoding.UTF8.GetByteCount(key) < 32) throw new ArgumentException("ROOM_CONTROL_TOKEN needs at least 32 bytes.");
                var endpoint = Environment.GetEnvironmentVariable("MAP_CONTROL_URL") ?? "http://127.0.0.1:7780";
                if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) || uri.Scheme != "http" || !(uri.IsLoopback || uri.Host == "game-server"))
                    throw new ArgumentException("MAP_CONTROL_URL must target loopback or the private Docker game-server.");
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
                client.DefaultRequestHeaders.Add("X-Room-Control-Key", key);
                using var response = await client.PostAsync(new Uri(uri, "/internal/maps/reload"), null);
                Console.WriteLine($"Map reload: HTTP {(int)response.StatusCode}"); return response.IsSuccessStatusCode ? 0 : 1;
            }
            Console.Error.WriteLine("Usage: --maps validate <catalog-dir> | --maps publish <source> <destination> | --maps reload"); return 2;
        }
        catch (Exception e) when (e is ArgumentException or IOException or UnauthorizedAccessException or HttpRequestException or TaskCanceledException)
        { Console.Error.WriteLine("Map operation failed: " + e.Message); return 1; }
    }
}
