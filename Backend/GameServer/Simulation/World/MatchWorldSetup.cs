using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using GameServer.Maps;
using JungleMaze.World;
using JungleMaze.World.Inventory;
using JungleMaze.World.Loot;

namespace GameServer.Simulation;

/// <summary>Prepared once before the application loop; creating/switching rooms performs no filesystem access.</summary>
public sealed class MatchWorldSetup
{
    private readonly WorldSettings _template;
    internal ItemCatalog Catalog { get; }
    internal LootProfile Loot { get; }
    public string RulesHash { get; }
    private MatchWorldSetup(WorldSettings template, ItemCatalog catalog, LootProfile loot, string rulesHash)
    { _template = template; Catalog = catalog; Loot = loot; RulesHash = rulesHash; _floorTable = new LootTable(catalog, loot); }

    public static MatchWorldSetup Load()
    {
        var settings = WorldSettings.Load(GameDataLoader.ReadGameDataFile("world_settings.json"));
        var options = new JsonSerializerOptions { IncludeFields = true, PropertyNameCaseInsensitive = true };
        options.Converters.Add(new JsonStringEnumConverter());
        var items = JsonSerializer.Deserialize<ItemCatalogDocument>(GameDataLoader.ReadGameDataFile("items_catalog.json"), options)
            ?? throw new InvalidDataException("Missing item catalog.");
        return new(settings, items.Build(), items.Loot, GameDataLoader.RulesHash);
    }

    internal WorldSettings ForMap(MapDefinition definition) => new()
    {
        Version = _template.Version, Seed = _template.Seed, LayoutMode = "Authored", Layout = definition.Layout,
        Map = _template.Map, Destructibles = _template.Destructibles, Hazards = _template.Hazards,
        FloorSpots = FloorSpotsFor(definition.Layout)
    };

    // Spots depend only on map geometry and the loot profile: plan once per map hash, off the tick thread where possible.
    private readonly ConcurrentDictionary<string, FloorLootSpots> _floorSpots = new(StringComparer.Ordinal);
    private readonly LootTable _floorTable;
    internal FloorLootSpots FloorSpotsFor(MapLayout layout)
    {
        if (_floorSpots.TryGetValue(layout.Hash, out var cached) && ReferenceEquals(cached.Layout, layout)) return cached;
        var planned = FloorLootPlanner.PlanSpots(layout, _floorTable, 0, _template.Map.HeroRadius);
        _floorSpots[layout.Hash] = planned;
        return planned;
    }
    /// <summary>Plans floor-loot spots for every catalog map; safe to run on a worker thread.</summary>
    public void Prewarm(IEnumerable<MapDefinition> maps) { foreach (var map in maps) FloorSpotsFor(map.Layout); }
}
