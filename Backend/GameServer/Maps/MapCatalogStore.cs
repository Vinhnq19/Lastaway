using System.Collections.Concurrent;
using GameServer.Rooms;
using GameServer.Simulation;
using JungleBackend.Rooms;

namespace GameServer.Maps;

/// <summary>Validate off-thread, then commit one generation on the application loop. Existing matches retain their map.</summary>
public sealed class MapCatalogStore
{
    private MapCatalogSnapshot _current;
    private readonly string _directory, _worldVersion;
    private readonly ConcurrentDictionary<(string Id, string Hash), MapDefinition> _versions = new();
    private readonly ConcurrentDictionary<string, byte[]> _previews = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim _reload = new(1, 1);
    public MapCatalogSnapshot Current => Volatile.Read(ref _current);
    public string RulesHash { get; }

    public MapCatalogStore(string directory, string worldVersion)
    {
        _directory = directory; _worldVersion = worldVersion;
        RulesHash = GameDataLoader.RulesHash;
        _current = MapCatalogSnapshot.Load(directory, worldVersion);
        Remember(_current);
    }

    public static MapCatalogStore FromEnvironment()
    {
        var root = Environment.GetEnvironmentVariable("MAP_DATA_PATH") ?? Path.Combine(AppContext.BaseDirectory, "GameData", "Maps");
        var world = WorldSettings.Load(GameDataLoader.ReadGameDataFile("world_settings.json"));
        if (!File.Exists(Path.Combine(root, "catalog.json")))
            MapPublisher.Publish(Path.Combine(AppContext.BaseDirectory, "GameData", "Maps"), root, world.Version);
        return new MapCatalogStore(root, world.Version);
    }

    public MapDefinition? FindVersion(string id, string hash) => _versions.GetValueOrDefault((id, hash));
    public byte[]? FindPreview(string hash) => _previews.GetValueOrDefault(hash);
    public MapCatalogView View()
    {
        var current = Current;
        return new MapCatalogView
        {
            Generation = current.Generation, DefaultMapId = current.DefaultMapId, RulesHash = RulesHash,
            Maps = current.Maps.Values.Select(m => new PublishedMapView
            {
                Id = m.Entry.Id, Name = m.Entry.Name, Description = m.Entry.Description,
                Version = m.Entry.Version, MapHash = m.Entry.MapHash, PreviewHash = m.Entry.PreviewHash
            }).ToArray()
        };
    }

    public async Task<RoomResult> Reload(RoomCommands commands, CancellationToken ct)
    {
        if (!await _reload.WaitAsync(0, ct)) return new(409, new RoomError("MapReloadBusy"));
        try
        {
            var candidate = await Task.Run(() => MapCatalogSnapshot.Load(_directory, _worldVersion), ct);
            if (candidate.Generation <= Current.Generation) return new(409, new RoomError("CatalogGenerationMustIncrease"));
            return await commands.Submit(rooms =>
            {
                Remember(candidate);
                Volatile.Write(ref _current, candidate);
                rooms.ApplyCatalog(candidate);
                return new RoomResult(200, new { candidate.Generation, Maps = candidate.Maps.Count });
            }, ct);
        }
        catch (Exception e) when (e is ArgumentException or IOException or System.Text.Json.JsonException or UnauthorizedAccessException)
        { return new(400, new RoomError("InvalidMapCatalog")); }
        finally { _reload.Release(); }
    }

    private void Remember(MapCatalogSnapshot snapshot)
    {
        foreach (var map in snapshot.Maps.Values)
        {
            _versions.TryAdd((map.Entry.Id, map.Entry.MapHash), map);
            _previews.TryAdd(map.Entry.PreviewHash, map.Preview);
        }
    }
}
