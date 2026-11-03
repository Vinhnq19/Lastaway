using System.Text.Json;
using System.Text.Json.Serialization;
using GameServer.Maps;
using JungleMaze.World.Inventory;

namespace GameServer.Simulation;

/// <summary>Prepared once before the application loop; creating/switching rooms performs no filesystem access.</summary>
public sealed class MatchWorldSetup
{
    private readonly WorldSettings _template;
    internal ItemCatalog Catalog { get; }
    internal LootProfile Loot { get; }
    public string RulesHash { get; }
    private MatchWorldSetup(WorldSettings template, ItemCatalog catalog, LootProfile loot, string rulesHash)
    { _template = template; Catalog = catalog; Loot = loot; RulesHash = rulesHash; }

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
        Map = _template.Map, Destructibles = _template.Destructibles, Hazards = _template.Hazards
    };
}
