using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using JungleMaze.Tests.Airdrops;
using JungleMaze.World.Airdrops;

Console.WriteLine("=== Starting AirdropChecks ===");

var passed = 0;
var failed = 0;

void Check(bool condition, string testName)
{
    if (condition)
    {
        Console.WriteLine($"[PASS] {testName}");
        passed++;
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[FAIL] {testName}");
        Console.ResetColor();
        failed++;
    }
}

bool Throws(Action action)
{
    try
    {
        action();
        return false;
    }
    catch (ArgumentException)
    {
        return true;
    }
}

var options = new JsonSerializerOptions
{
    IncludeFields = true,
    PropertyNameCaseInsensitive = true,
};

static string? FindScheduleJson()
{
    var directory = new DirectoryInfo(AppContext.BaseDirectory);
    while (directory is not null)
    {
        var candidate = Path.Combine(directory.FullName, "Shared", "GameData", "airdrop_schedule.json");
        if (File.Exists(candidate))
        {
            return candidate;
        }
        var backendCandidate = Path.Combine(directory.FullName, "Backend", "Shared", "GameData", "airdrop_schedule.json");
        if (File.Exists(backendCandidate))
        {
            return backendCandidate;
        }
        directory = directory.Parent;
    }
    return null;
}

// Production defaults must validate as shipped.
var defaults = new AirdropSettings();
var defaultValid = true;
try
{
    defaults.Validate();
}
catch (ArgumentException)
{
    defaultValid = false;
}
Check(defaultValid, "production defaults validate");
Check(defaults.Version == "1.0.0", "default version is 1.0.0");
Check(defaults.Enabled, "default enabled is true");
Check(MathF.Abs(defaults.EggSize - 2f) < 1e-4f, "default egg size is 2");
Check(defaults.EggHp == 150, "default egg hp is 150");
Check(MathF.Abs(defaults.EdgeMargin - 3f) < 1e-4f, "default edge margin is 3");
Check(MathF.Abs(defaults.RetrySeconds - 1f) < 1e-4f, "default retry seconds is 1");
Check(defaults.MaxRetries == 5, "default max retries is 5");
Check(defaults.CandidateLimit == 64, "default candidate limit is 64");
Check(defaults.ReservedEggs == 3, "default reserved eggs is 3");
Check(defaults.ReservedLootStacks == 24, "default reserved loot stacks is 24");
Check(defaults.Drops.Length == 3, "default drop count is 3");
Check(defaults.Drops[0].Id == 1 && defaults.Drops[1].Id == 2 && defaults.Drops[2].Id == 3, "default drop ids are 1,2,3");
Check(MathF.Abs(defaults.Drops[0].FlyAt - 110f) < 1e-4f && MathF.Abs(defaults.Drops[0].LandAt - 120f) < 1e-4f, "default drop 1 flies 110 lands 120");
Check(MathF.Abs(defaults.Drops[1].FlyAt - 230f) < 1e-4f && MathF.Abs(defaults.Drops[1].LandAt - 240f) < 1e-4f, "default drop 2 flies 230 lands 240");
Check(MathF.Abs(defaults.Drops[2].FlyAt - 320f) < 1e-4f && MathF.Abs(defaults.Drops[2].LandAt - 330f) < 1e-4f, "default drop 3 flies 320 lands 330");

// Production JSON must deserialize into the exact same shipped values.
var schedulePath = FindScheduleJson();
Check(schedulePath is not null, "airdrop_schedule.json located relative to AppContext.BaseDirectory");
if (schedulePath is not null)
{
    var json = File.ReadAllText(schedulePath);
    var fromJson = JsonSerializer.Deserialize<AirdropSettings>(json, options)!;
    var jsonValid = true;
    try
    {
        fromJson.Validate();
    }
    catch (ArgumentException)
    {
        jsonValid = false;
    }
    Check(jsonValid, "production JSON settings validate");
    Check(fromJson.Version == defaults.Version, "JSON version mirrors defaults");
    Check(fromJson.Enabled == defaults.Enabled, "JSON enabled mirrors defaults");
    Check(MathF.Abs(fromJson.EggSize - defaults.EggSize) < 1e-4f, "JSON egg size mirrors defaults");
    Check(fromJson.EggHp == defaults.EggHp, "JSON egg hp mirrors defaults");
    Check(MathF.Abs(fromJson.EdgeMargin - defaults.EdgeMargin) < 1e-4f, "JSON edge margin mirrors defaults");
    Check(MathF.Abs(fromJson.RetrySeconds - defaults.RetrySeconds) < 1e-4f, "JSON retry seconds mirrors defaults");
    Check(fromJson.MaxRetries == defaults.MaxRetries, "JSON max retries mirrors defaults");
    Check(fromJson.CandidateLimit == defaults.CandidateLimit, "JSON candidate limit mirrors defaults");
    Check(fromJson.ReservedEggs == defaults.ReservedEggs, "JSON reserved eggs mirrors defaults");
    Check(fromJson.ReservedLootStacks == defaults.ReservedLootStacks, "JSON reserved loot stacks mirrors defaults");
    Check(fromJson.Drops.Length == defaults.Drops.Length, "JSON drop count mirrors defaults");
    Check(fromJson.Drops.Zip(defaults.Drops).All(p => p.First.Id == p.Second.Id
        && MathF.Abs(p.First.FlyAt - p.Second.FlyAt) < 1e-4f
        && MathF.Abs(p.First.LandAt - p.Second.LandAt) < 1e-4f), "JSON drops mirror defaults");
}

// An explicitly empty drop list must not validate.
var emptyDrops = JsonSerializer.Deserialize<AirdropSettings>("{ \"version\": \"1.0.0\", \"drops\": [] }", options)!;
Check(Throws(emptyDrops.Validate), "empty drop array is rejected");

// Shortening the schedule keeps the reservations sufficient.
var shortened = new AirdropSettings
{
    Drops = new[]
    {
        new AirdropDropSettings { Id = 1, FlyAt = 110f, LandAt = 120f },
        new AirdropDropSettings { Id = 2, FlyAt = 230f, LandAt = 240f },
    },
    ReservedEggs = 3,
    ReservedLootStacks = 24,
};
Check(!Throws(shortened.Validate), "two drops with reservations 3/24 remain valid");

// Invalid models must all be rejected by Validate.
var duplicateId = new AirdropSettings
{
    Drops = new[]
    {
        new AirdropDropSettings { Id = 1, FlyAt = 110f, LandAt = 120f },
        new AirdropDropSettings { Id = 1, FlyAt = 230f, LandAt = 240f },
    },
};
Check(Throws(duplicateId.Validate), "duplicate drop ids are rejected");

var negativeTime = new AirdropSettings
{
    Drops = new[] { new AirdropDropSettings { Id = 1, FlyAt = -1f, LandAt = 120f } },
    ReservedEggs = 3,
    ReservedLootStacks = 24,
};
Check(Throws(negativeTime.Validate), "negative fly time is rejected");

var nanTime = new AirdropSettings
{
    Drops = new[] { new AirdropDropSettings { Id = 1, FlyAt = float.NaN, LandAt = 120f } },
    ReservedEggs = 3,
    ReservedLootStacks = 24,
};
Check(Throws(nanTime.Validate), "NaN fly time is rejected");

var overlapping = new AirdropSettings
{
    Drops = new[]
    {
        new AirdropDropSettings { Id = 1, FlyAt = 110f, LandAt = 200f },
        new AirdropDropSettings { Id = 2, FlyAt = 150f, LandAt = 240f },
    },
};
Check(Throws(overlapping.Validate), "overlapping flights are rejected");

var unordered = new AirdropSettings
{
    Drops = new[]
    {
        new AirdropDropSettings { Id = 1, FlyAt = 230f, LandAt = 240f },
        new AirdropDropSettings { Id = 2, FlyAt = 110f, LandAt = 120f },
    },
};
Check(Throws(unordered.Validate), "out-of-order flights are rejected");

var zeroHp = new AirdropSettings { EggHp = 0 };
Check(Throws(zeroHp.Validate), "egg hp 0 is rejected");

var reservedEggs = new AirdropSettings { ReservedEggs = 2 };
Check(Throws(reservedEggs.Validate), "reserved eggs below drop count are rejected");

var reservedLoot = new AirdropSettings { ReservedLootStacks = 8 };
Check(Throws(reservedLoot.Validate), "reserved loot stacks below eight per drop are rejected");

var emptyVersion = new AirdropSettings { Version = "  " };
Check(Throws(emptyVersion.Validate), "blank version is rejected");

var longVersion = new AirdropSettings { Version = new string('v', 49) };
Check(Throws(longVersion.Validate), "version longer than 48 characters is rejected");

// Deterministic airdrop lifecycle clock checks.
var runtimeResults = JungleMaze.Tests.Airdrops.RuntimeChecks.Run();
passed += runtimeResults.Passed;
failed += runtimeResults.Failed;

var placementResults = JungleMaze.Tests.Airdrops.PlacementChecks.Run();
passed += placementResults.Passed;
failed += placementResults.Failed;
JungleMaze.Tests.Airdrops.ServerChecks.Run(Check);
await JungleMaze.Tests.Airdrops.SocketChecks.Run(Check);
Console.WriteLine($"\n=== Test Results: {passed} passed, {failed} failed ===");
if (failed > 0)
{
    Environment.Exit(1);
}
else
{
    Console.WriteLine("ALL AIRDROP CHECKS PASSED SUCCESSFULLY!");
    Environment.Exit(0);
}
