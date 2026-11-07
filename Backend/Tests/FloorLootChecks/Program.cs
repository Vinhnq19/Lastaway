using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using GameServer.Simulation;
using JungleBackend.Protocol.Gameplay;
using JungleMaze.World;
using JungleMaze.World.Inventory;
using JungleMaze.World.Loot;

// Floor loot (Docs/hvu/07_Floor_Loot.vi.md): spots, per-match items, spawn fairness, ammo companions, container scatter,
// object budget, JSON compatibility and server wiring. Prints per-map statistics for section 9 of the document.
var checks = 0;
void Check(bool ok, string name) { checks++; if (!ok) throw new Exception("FAIL: " + name); Console.WriteLine("PASS: " + name); }
void Throws(Action action, string name) { try { action(); } catch (ArgumentException) { Check(true, name); return; } throw new Exception("Expected rejection: " + name); }

var root = FindRoot();
var options = new JsonSerializerOptions { IncludeFields = true, PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter() } };
var catalogJson = File.ReadAllText(Path.Combine(root, "Backend", "Shared", "GameData", "items_catalog.json"));
var document = JsonSerializer.Deserialize<ItemCatalogDocument>(catalogJson, options)!;
var catalog = document.Build();
var table = new LootTable(catalog, document.Loot);
var floor = table.Floor;
const float hero = .4f;
var rangedIds = new[] { "RUBBER_GUN", "SCEPTER", "CROSSBOW", "SHOTGUN", "PISTOL", "ASSAULT_RIFLE", "SNIPER_RIFLE" };
var floorWeaponsSeen = new HashSet<string>();
Check(rangedIds.All(id => catalog.Get(id).Enabled), "all seven ranged weapons enabled in the shipped catalog");
var chestWeaponsSeen = new HashSet<string>();
for (uint seed = 1; seed <= 2000; seed++)
    foreach (var item in table.Roll(LootSource.Chest, seed))
        chestWeaponsSeen.Add(item.ItemId);
Check(rangedIds.All(chestWeaponsSeen.Contains), "all seven ranged weapons can drop from chests");

// ---- Catalog: rarity and the Floor block ----
var baseline = ItemCatalog.CreateBaseline();
Check(catalog.Items.All(i => baseline.Get(i.Id).Rarity == i.Rarity), "items_catalog.json rarities equal CreateBaseline");
Check(catalog.Get("SHOTGUN").Rarity == ItemRarity.Epic && catalog.Get("MACE").Rarity == ItemRarity.Legendary && catalog.Get("AMMO").Rarity == ItemRarity.Common &&
      catalog.Get("CROSSBOW").Rarity == ItemRarity.Rare && catalog.Get("AXE").Rarity == ItemRarity.Uncommon, "rarity table of section 6");
Check(catalogJson.Contains("\"Rarity\": \"Epic\"") && catalogJson.Contains("\"Floor\""), "JSON stores rarity as string and has a Floor block");
Check(new ItemDefinition { Id = "X" }.Rarity == ItemRarity.Common, "rarity defaults to Common");
Throws(() => new ItemDefinition { Id = "X", Rarity = (ItemRarity)9 }.Validate(), "undefined rarity rejected");
var noFloorNode = JsonNode.Parse(catalogJson)!; noFloorNode["Loot"]!.AsObject().Remove("Floor");
var noRarityNode = JsonNode.Parse(catalogJson)!; foreach (var item in noRarityNode["Items"]!.AsArray()) item!.AsObject().Remove("Rarity");
var legacy = JsonSerializer.Deserialize<ItemCatalogDocument>(noFloorNode.ToJsonString(), options)!;
var legacyItems = JsonSerializer.Deserialize<ItemCatalogDocument>(noRarityNode.ToJsonString(), options)!;
Check(legacy.Loot.Floor != null && legacy.Loot.Floor.AreaPerSpot == new FloorLootProfile().AreaPerSpot, "JSON without Loot.Floor reads with defaults");
Check(legacyItems.Build().Items.All(i => i.Rarity == ItemRarity.Common), "JSON without Rarity reads as Common");
var nullFloor = new LootProfile { Floor = null! };
Check(new LootTable(catalog, nullFloor).Floor.SpotChance == new FloorLootProfile().SpotChance, "null Floor block falls back to defaults");
Throws(() => new LootTable(catalog, new LootProfile { Floor = new FloorLootProfile { GroupWeights = new int[5] } }), "five floor group weights rejected");
Throws(() => new LootTable(catalog, new LootProfile { Floor = new FloorLootProfile { SpotChance = 1.5f } }), "spot chance above 1 rejected");
Throws(() => new LootTable(catalog, new LootProfile { Floor = new FloorLootProfile { TierRarityMultipliers = new[] { new float[5], new float[5] } } }), "two tier rows rejected");
Throws(() => new LootTable(catalog, new LootProfile { Floor = new FloorLootProfile { TierRarityMultipliers = new[] { new[] { 1f, 1, 1, 1, float.NaN }, new float[] { 1, 1, 1, 1, 1 }, new float[] { 1, 1, 1, 1, 1 } } } }), "NaN multiplier rejected");
Throws(() => new LootTable(catalog, new LootProfile { Floor = new FloorLootProfile { ScatterMinRadius = 2, ScatterMaxRadius = 1 } }), "inverted scatter radius rejected");
Throws(() => new LootTable(catalog, new LootProfile { Floor = new FloorLootProfile { TierRarityMultipliers = new[] { new float[5], new float[5], new float[5] } } }), "tier with zero total weight rejected");
var copy = table.Floor; copy.GroupWeights[0] = 999; copy.TierRarityMultipliers[0][0] = 99;
Check(table.Floor.GroupWeights[0] != 999 && table.Floor.TierRarityMultipliers[0][0] != 99, "Floor profile is a defensive copy");
Check(FloorLootPlanner.MaxWorldObjects == WorldWire.MaxObjects, "planner object cap equals WorldWire.MaxObjects");

// ---- Maps ----
var mapFiles = new[] { "map1_thap_co", "map2_thi_tran", "map3_rung_sinh_ton" };
var stats = new List<string>();
var layouts = new Dictionary<string, MapLayout>();
foreach (var name in mapFiles)
{
    var json = File.ReadAllText(Path.Combine(root, "Assets", "_JungleMaze", "Levels", name + ".json"));
    var watch = Stopwatch.StartNew();
    var layout = JsonSerializer.Deserialize<AuthoredMapDocument>(json, options)!.Build("world-prototype-1");
    var buildMs = watch.Elapsed.TotalMilliseconds;
    layouts[name] = layout;
    var spots = FloorLootPlanner.PlanSpots(layout, table, 0, hero);
    var times = new List<double>();
    for (var i = 0; i < 5; i++) times.Add(FloorLootPlanner.PlanSpots(layout, table, 0, hero).PlanMilliseconds);
    times.Sort();
    var again = FloorLootPlanner.PlanSpots(layout, table, 0, hero);
    Check(again.Spots.Count == spots.Spots.Count && again.Spots.Zip(spots.Spots).All(z => z.First.Position.X == z.Second.Position.X && z.First.Position.Y == z.Second.Position.Y && z.First.Tier == z.Second.Tier),
        name + ": spots are deterministic from the map");
    // map3 as authored today has no object budget left (objects + worst-case container drops); section 8 slims it.
    var hasRoom = spots.Budget >= floor.MinNearSpawn * layout.Spawns.Count;
    Check(spots.Spots.Count > 0 || !hasRoom, name + ": map gets floor spots when the object budget allows (budget " + spots.Budget + ")");

    // Placement rules: walkable, off hazards, clear of portals/doors/containers, away from spawns, inside the zone, spaced.
    var center = new WorldPoint(layout.Size / 2f, layout.Size / 2f);
    var bad = 0;
    foreach (var s in spots.Spots)
    {
        var pos = s.Position;
        if (!layout.IsWalkable(pos, hero)) bad++;
        else if (layout.Objects.Any(o => o.IsHazard && o.Bounds.IntersectsCircle(pos, hero))) bad++;
        else if (layout.Objects.Any(o => (o.IsDestructible || o.Kind == WorldObjectKind.Door || o.Kind == WorldObjectKind.Portal) && o.Bounds.IntersectsCircle(pos, floor.ObjectClearance))) bad++;
        else if (layout.Spawns.Any(sp => sp.DistanceSquared(pos) < floor.SpawnClearance * floor.SpawnClearance)) bad++;
        else if (pos.DistanceSquared(center) > spots.ZoneRadius * spots.ZoneRadius) bad++;
    }
    Check(bad == 0, name + ": no spot on blocked/hazard cells, near portals/doors/containers or within 3 of a spawn");
    var spacing = 0;
    var list = spots.Spots;
    for (var i = 0; i < list.Count; i++)
        for (var j = i + 1; j < list.Count; j++)
        {
            var need = list[i].Indoor && list[j].Indoor || list[i].Fairness || list[j].Fairness ? floor.IndoorSpacing : floor.OutdoorSpacing;
            if (list[i].Position.DistanceSquared(list[j].Position) < need * need - 1e-4f) spacing++;
        }
    Check(spacing == 0, name + ": minimum spacing 3 indoor / 5 elsewhere");

    // Rolls.
    var a = FloorLootPlanner.Roll(spots, table, 12345); var b = FloorLootPlanner.Roll(spots, table, 12345); var c = FloorLootPlanner.Roll(again, table, 777);
    Check(a.Placements.Select(p => (p.Stack.ItemId, p.Stack.Count, p.Position.X, p.Position.Y)).SequenceEqual(b.Placements.Select(p => (p.Stack.ItemId, p.Stack.Count, p.Position.X, p.Position.Y))), name + ": same seed gives the same floor");
    Check(spots.Spots.Count == 0 || !a.Placements.Select(p => p.Stack.ItemId + p.Spot).SequenceEqual(c.Placements.Select(p => p.Stack.ItemId + p.Spot)), name + ": another seed gives other items on the same spots");
    Check(c.Placements.All(p => p.Spot >= 0 && p.Spot < spots.Spots.Count), name + ": every item belongs to a planned spot");

    var tierRarity = new int[3, 5]; double items = 0, ammo = 0, objects = 0, filled = 0, rollMs = 0; var maxObjects = 0; var unfair = 0; var ammoBad = 0; var overBudget = 0; var guns = 0;
    const int seeds = 200;
    for (uint seed = 1; seed <= seeds; seed++)
    {
        var watchRoll = Stopwatch.StartNew();
        var plan = FloorLootPlanner.Roll(spots, table, seed * 2654435761u);
        rollMs += watchRoll.Elapsed.TotalMilliseconds;
        objects += plan.Placements.Count; maxObjects = Math.Max(maxObjects, plan.Placements.Count); filled += plan.FilledSpots;
        if (plan.Placements.Count > spots.Budget || plan.Placements.Count > 0 && layout.Objects.Count + plan.Placements.Count + spots.ContainerReserve + floor.ReserveObjects > WorldWire.MaxObjects) overBudget++;
        foreach (var p in plan.Placements)
        {
            if (p.Companion) { ammo++; continue; }
            items++;
            var def = catalog.Get(p.Stack.ItemId); tierRarity[(int)spots.Spots[p.Spot].Tier, (int)def.Rarity]++;
            if (def.MagazineSize > 0)
            {
                floorWeaponsSeen.Add(def.Id);
                guns++;
                var companion = plan.Placements.Where(q => q.Companion && q.Spot == p.Spot).ToList();
                if (companion.Count != 1 || companion[0].Stack.ItemId != "AMMO" || companion[0].Stack.Count != floor.AmmoBoxes) { ammoBad++; continue; }
                var d = MathF.Sqrt(companion[0].Position.DistanceSquared(p.Position));
                if (d < floor.AmmoMinDistance - 1e-3f || d > floor.AmmoMaxDistance + 1e-3f || !spots.Surface.IsFree(companion[0].Position) || !layout.IsWalkable(companion[0].Position, floor.DropClearance)) ammoBad++;
            }
        }
        // Fairness: each spawn sees MinNearSpawn filled spots 6-14 cells away, one of them melee or healing.
        for (var s = 0; s < layout.Spawns.Count; s++)
        {
            var ring = spots.NearSpawn(s);
            var near = plan.Placements.Where(p => !p.Companion && ring.Contains(p.Spot)).ToList();
            if (near.Count < Math.Min(floor.MinNearSpawn, ring.Count) || ring.Count > 0 && !near.Any(p => LootTable.IsStarter(catalog.Get(p.Stack.ItemId)))) unfair++;
        }
    }
    Check(overBudget == 0, name + ": floor objects stay inside the 4096 world object budget with reserves");
    Check(unfair == 0, name + ": spawn fairness (>= MinNearSpawn filled spots 6-14 cells away, one melee/heal) over 200 seeds");
    Check(!hasRoom || Enumerable.Range(0, layout.Spawns.Count).All(s => spots.NearSpawn(s).Count >= floor.MinNearSpawn), name + ": every spawn has enough fairness spots in its ring");
    Check(ammoBad == 0, name + ": every gun has one AMMO stack 0.8-1.2 cells away on a free point (" + guns + " guns)");

    // Container drops on this map: every chest, pot and supply, broken one at a time.
    var scatterBad = 0; var scatterFallback = 0; var drops = 0; var dropAmmoBad = 0; double containerItems = 0;
    foreach (var o in layout.Objects.Where(o => o.IsDestructible))
    {
        var surface = new LootSurface(layout, floor.DropClearance, id => id == o.Id);
        var seed = (uint)o.Id * 2654435761u ^ 99u;
        var placements = LootDrops.ForContainer(table, o.Kind, o.Bounds.Center, seed, surface);
        if (placements.Count != LootDrops.CountForContainer(table, o.Kind, seed)) scatterBad++;
        containerItems += placements.Count(p => !p.Companion);
        foreach (var p in placements)
        {
            if (!surface.IsFree(p.Position)) { scatterBad++; continue; }
            if (p.Companion)
            {
                var gunNear = placements.Any(g => !g.Companion && catalog.Get(g.Stack.ItemId).MagazineSize > 0 &&
                    g.Position.DistanceSquared(p.Position) >= (floor.AmmoMinDistance - 1e-3f) * (floor.AmmoMinDistance - 1e-3f) &&
                    g.Position.DistanceSquared(p.Position) <= (floor.AmmoMaxDistance + 1e-3f) * (floor.AmmoMaxDistance + 1e-3f));
                if (!gunNear) dropAmmoBad++;
                continue;
            }
            drops++;
            var d = MathF.Sqrt(p.Position.DistanceSquared(o.Bounds.Center));
            if (d < floor.ScatterMinRadius - 1e-3f || d > floor.ScatterMaxRadius + 1e-3f) scatterFallback++;
        }
    }
    Check(scatterBad == 0, name + ": container drops land on free points and CountForContainer matches");
    Check(scatterFallback == 0, name + ": all " + drops + " container drops lie 0.9-1.4 cells from the container");
    Check(dropAmmoBad == 0, name + ": container guns get AMMO 0.8-1.2 cells away");

    // Value comparison: expected container items (rolled above per container) vs floor items.
    var chests = layout.Objects.Count(o => o.Kind == WorldObjectKind.Chest); var pots = layout.Objects.Count(o => o.Kind == WorldObjectKind.Pot); var supplies = layout.Objects.Count(o => o.Kind == WorldObjectKind.Supply);
    var expectChest = Enumerable.Range(1, 2000).Average(i => table.Roll(LootSource.Chest, (uint)i).Length);
    var expectPot = Enumerable.Range(1, 2000).Average(i => table.Roll(LootSource.Pot, (uint)i).Length);
    var expectSupply = Enumerable.Range(1, 2000).Average(i => table.Roll(LootSource.Supply, (uint)i).Length);
    var containerValue = chests * expectChest + pots * expectPot + supplies * expectSupply;
    var line = $"| {name} | {layout.Objects.Count} | {chests}/{pots}/{supplies} | {spots.WalkableCells} | {spots.Target} | {spots.Budget} | {spots.Spots.Count} ({spots.Spots.Count(s => s.Fairness)} fair, {spots.Spots.Count(s => s.Indoor)} indoor; candidates open/cover/indoor {spots.CandidateScores[1]}/{spots.CandidateScores[2]}/{spots.CandidateScores[3]}) | {items / seeds:F1} | {ammo / seeds:F1} | {layout.Objects.Count + objects / seeds:F0} (max {layout.Objects.Count + maxObjects}) | {containerValue:F0} | {containerValue + items / seeds:F0} | {times[2]:F1} (cold {spots.PlanMilliseconds:F1}) | {rollMs / seeds:F2} | {buildMs:F0} |";
    stats.Add(line);
    var rarity = new List<string>();
    for (var t = 0; t < 3; t++)
    {
        var total = Enumerable.Range(0, 5).Sum(r => tierRarity[t, r]);
        rarity.Add($"{(LootTier)t}: " + (total == 0 ? "-" : string.Join(" ", Enumerable.Range(0, 5).Select(r => $"{(ItemRarity)r} {100.0 * tierRarity[t, r] / total:F0}%"))) + $" (n={total})");
    }
    stats.Add($"|   rarity by ring ({name}) | {string.Join(" ; ", rarity)} |");
}

Check(rangedIds.All(floorWeaponsSeen.Contains), "all four ranged weapons spawn as floor loot across the authored map rolls");

// Spots do not change with the seed; fewer containers mean more floor spots (map3 with all pots removed simulates section 8).
var map3 = layouts["map3_rung_sinh_ton"];
// Simulation of section 8: no pots, one chest in three kept.
var stripped = new MapLayout { Version = map3.Version, Size = map3.Size, Seed = map3.Seed, Hash = map3.Hash + "-sim8" };
var chestIndex = 0;
stripped.Spawns.AddRange(map3.Spawns); stripped.Objects.AddRange(map3.Objects.Where(o => o.Kind != WorldObjectKind.Pot && (o.Kind != WorldObjectKind.Chest || chestIndex++ % 3 == 0)));
var full3 = FloorLootPlanner.PlanSpots(map3, table, 0, hero); var less3 = FloorLootPlanner.PlanSpots(stripped, table, 0, hero);
Check(less3.Target > full3.Target && less3.Spots.Count > full3.Spots.Count, "a map with fewer containers gets more floor spots");
var lessItems = Enumerable.Range(1, 50).Average(i => FloorLootPlanner.Roll(less3, table, (uint)i).Placements.Count(p => !p.Companion));
var strippedChests = stripped.Objects.Count(o => o.Kind == WorldObjectKind.Chest);
var strippedSupply = stripped.Objects.Count(o => o.Kind == WorldObjectKind.Supply);
var strippedContainers = strippedChests * Enumerable.Range(1, 2000).Average(i => table.Roll(LootSource.Chest, (uint)i).Length) + strippedSupply * Enumerable.Range(1, 2000).Average(i => table.Roll(LootSource.Supply, (uint)i).Length);
stats.Add($"| map3 sim. section 8 (0 pots, 1/3 chests) | {stripped.Objects.Count} | {strippedChests}/0/{strippedSupply} | {less3.WalkableCells} | {less3.Target} | {less3.Budget} | {less3.Spots.Count} | {lessItems:F1} | - | - | {strippedContainers:F0} | {strippedContainers + lessItems:F0} | - | - | - |");

// Disabled floor loot plans nothing.
Check(FloorLootPlanner.Plan(map3, new LootTable(catalog, new LootProfile { Floor = new FloorLootProfile { Enabled = false } }), 1).Placements.Count == 0, "Floor.Enabled=false plans no floor loot");

// ---- Server wiring ----
var map1 = layouts["map1_thap_co"];
var settings = new WorldSettings { Layout = map1, LayoutMode = "Authored" };
var matchA = Guid.Parse("0f7d7a52-1d4c-4c1b-9d1e-1f2a3b4c5d6e"); var matchB = Guid.Parse("8a1c2d3e-4f50-4617-8899-aabbccddeeff");
MatchWorldRuntime World(Guid id) { var registry = new Registry(); return new MatchWorldRuntime(id, settings, registry, new HealthService(registry), catalog, document.Loot); }
var w1 = World(matchA); var w1Again = World(matchA); var w2 = World(matchB);
Check(w1.Floor.Items.Count == w1.InitialLoot.Placements.Count && w1.Floor.Items.Count > 0 && w1.Revision > 1, "server spawns the floor plan into Floor at world start and bumps Revision");
Check(w1.Floor.Items.Select(f => (f.Stack.ItemId, f.X, f.Y)).SequenceEqual(w1Again.Floor.Items.Select(f => (f.Stack.ItemId, f.X, f.Y))), "same MatchId reproduces the same floor");
Check(w1.LootSeed != w2.LootSeed && !w1.Floor.Items.Select(f => f.Stack.ItemId).SequenceEqual(w2.Floor.Items.Select(f => f.Stack.ItemId)), "different MatchId changes floor items");
Check(w1.SnapshotObjects().Count <= WorldWire.MaxObjects && w1.SnapshotObjects().Count(o => o.Kind == 100) == w1.Floor.Items.Count, "snapshot carries the floor items within the wire limit");
var cachedSettings = new WorldSettings { Layout = map1, LayoutMode = "Authored", FloorSpots = FloorLootPlanner.PlanSpots(map1, table, 0, hero) };
var wCached = new MatchWorldRuntime(matchA, cachedSettings, new Registry(), new HealthService(new Registry()), catalog, document.Loot);
Check(wCached.Floor.Items.Select(f => (f.Stack.ItemId, f.X, f.Y)).SequenceEqual(w1.Floor.Items.Select(f => (f.Stack.ItemId, f.X, f.Y))), "cached spots give the same floor as planning at start");

// Server container break: scatter radius, walkable, gun ammo counted by CanSpawnLoot.
var broken = 0; var gunBreaks = 0;
foreach (var chest in map1.Objects.Where(o => o.Kind == WorldObjectKind.Chest || o.Kind == WorldObjectKind.Supply))
{
    var before = w1.Floor.Items.Select(f => f.Id).ToHashSet();
    var hp = chest.Kind == WorldObjectKind.Supply ? 150 : 30;
    Check(w1.Destructibles.DamageConfirmed(chest.Id, hp, 0, 1), "server breaks container " + chest.Id);
    var added = w1.Floor.Items.Where(f => !before.Contains(f.Id)).ToList();
    var center = chest.Bounds.Center;
    var expected = LootDrops.CountForContainer(table, chest.Kind, LootSeeds.Mix(map1.Seed ^ ((uint)chest.Id * 2654435761u), w1.LootSeed));
    var gunsHere = added.Where(f => catalog.Get(f.Stack.ItemId).MagazineSize > 0).ToList();
    if (gunsHere.Count > 0) gunBreaks++;
    Check(added.Count == expected && added.Count(f => f.Stack.ItemId == "AMMO" && f.Stack.Count == floor.AmmoBoxes) >= gunsHere.Count,
        "container " + chest.Id + " spawns exactly the CanSpawnLoot count, with an AMMO stack per gun");
    bool OnRing(FloorItem f) { var d = MathF.Sqrt(new WorldPoint(f.X, f.Y).DistanceSquared(center)); return d >= floor.ScatterMinRadius - 1e-3f && d <= floor.ScatterMaxRadius + 1e-3f; }
    bool NextToGun(FloorItem f) => gunsHere.Any(g => { var d = MathF.Sqrt(new WorldPoint(f.X, f.Y).DistanceSquared(new WorldPoint(g.X, g.Y))); return d >= floor.AmmoMinDistance - 1e-3f && d <= floor.AmmoMaxDistance + 1e-3f; });
    Check(added.All(f => OnRing(f) || f.Stack.ItemId == "AMMO" && NextToGun(f)), "container " + chest.Id + " drops lie 0.9-1.4 cells out (gun ammo 0.8-1.2 from its gun)");
    Check(added.All(f => map1.IsWalkable(new WorldPoint(f.X, f.Y), floor.DropClearance, w1.Destructibles.IsBroken, null)), "container " + chest.Id + " drops are walkable");
    broken++;
}
Check(broken > 0 && gunBreaks > 0, $"server container breaks exercised ({broken} containers, {gunBreaks} with guns)");

// Saturated floor rejects a container break that would not fit including ammo; recovery spawns the whole batch.
var w3 = World(matchB);
var pot = map1.Objects.First(o => o.Kind == WorldObjectKind.Pot);
while (w3.Floor.TrySpawn(new ItemStack("GOD_LEAF"), 1, 1, out _)) { }
Check(!w3.Destructibles.DamageConfirmed(pot.Id, 20, 0, 1) && !w3.Destructibles.IsBroken(pot.Id), "full floor rejects the lethal hit without losing loot");

Console.WriteLine();
Console.WriteLine("| map | objects | chest/pot/supply | walkable cells | area target | budget | spots | floor items/match | ammo stacks/match | world objects/match (of 4096) | container items (exp.) | total items | PlanSpots ms (median of 5) | Roll ms | map Build+Validate ms |");
Console.WriteLine("|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|");
foreach (var line in stats) Console.WriteLine(line);
Console.WriteLine();
Console.WriteLine($"FloorLootChecks: {checks} passed, 0 failed");

static string FindRoot()
{
    foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        for (var dir = new DirectoryInfo(start); dir != null; dir = dir.Parent)
            if (Directory.Exists(Path.Combine(dir.FullName, "Assets", "_JungleMaze", "Levels")) && Directory.Exists(Path.Combine(dir.FullName, "Backend", "Shared")))
                return dir.FullName;
    throw new DirectoryNotFoundException("Run from inside the Jungle Maze repository.");
}

sealed class Registry : IEntityRegistry
{
    public bool TryGet(byte netId, out IEntity? entity) { entity = null; return false; }
    public bool TryGetBySession(string sessionId, out IEntity? entity) { entity = null; return false; }
    public IEnumerable<IEntity> All => Array.Empty<IEntity>();
    public int Count => 0;
    public int AliveCount => 0;
}
