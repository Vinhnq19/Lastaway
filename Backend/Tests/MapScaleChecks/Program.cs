using System;
using System.Linq;
using GameServer.Simulation;

Console.WriteLine("=== Starting MapScaleChecks ===");

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

static MapData MakeMap(float sizeX, float sizeY, float ringRadius)
{
    var map = new MapData { MaxX = sizeX, MaxY = sizeY };
    var cx = sizeX * 0.5f;
    var cy = sizeY * 0.5f;
    for (var i = 0; i < 10; i++)
    {
        var angle = i * MathF.PI * 2f / 10f;
        map.SpawnPoints.Add((cx + ringRadius * MathF.Cos(angle), cy + ringRadius * MathF.Sin(angle)));
    }
    return map;
}

static ZoneSchedule MakeBaseSchedule()
{
    var schedule = new ZoneSchedule();
    schedule.Intervals.Add(new ZoneInterval { Index = 1, StartSecond = 90, EndSecond = 97, StartRadius = 25, EndRadius = 18 });
    schedule.Intervals.Add(new ZoneInterval { Index = 2, StartSecond = 182, EndSecond = 188, StartRadius = 18, EndRadius = 12 });
    schedule.Intervals.Add(new ZoneInterval { Index = 3, StartSecond = 268, EndSecond = 273, StartRadius = 12, EndRadius = 7 });
    schedule.Intervals.Add(new ZoneInterval { Index = 4, StartSecond = 343, EndSecond = 347, StartRadius = 7, EndRadius = 3 });
    schedule.Intervals.Add(new ZoneInterval { Index = 5, StartSecond = 387, EndSecond = 390, StartRadius = 3, EndRadius = 0 });
    schedule.Validate();
    return schedule;
}

// Base 50 map with a safe spawn ring: schedule must stay untouched.
var map50 = MakeMap(50f, 50f, 10f);
map50.Validate();
var baseSchedule = MakeBaseSchedule();

// 150 map (square150) with spawn ring 10: validates against bounds-derived circle.
var map150 = MakeMap(150f, 150f, 10f);
var map150Valid = true;
try
{
    map150.Validate();
}
catch (ArgumentException)
{
    map150Valid = false;
}
Check(map150Valid, "square150 map with ring10 spawns validates");

// ForMap scaling
var scaled150 = baseSchedule.ForMap(map150);
Check(scaled150.InitialCenter.X == 75f && scaled150.InitialCenter.Y == 75f, "scaled schedule center is map midpoint (75, 75)");
Check(MathF.Abs(scaled150.InitialRadius - 75f) < 1e-3f, "scaled initial radius is 75");
Check(MathF.Abs(scaled150.ShrinkSpeedCellsPerSecond - 3f) < 1e-3f, "scaled shrink speed is 3 (x3)");
Check(scaled150.DamagePerSecond == baseSchedule.DamagePerSecond, "damage per second preserved");
Check(scaled150.WarningDurationSeconds == baseSchedule.WarningDurationSeconds, "warning duration preserved");
Check(scaled150.Version == baseSchedule.Version, "version preserved");
Check(scaled150.Intervals.Count == baseSchedule.Intervals.Count, "interval count preserved");
Check(scaled150.Intervals.Zip(baseSchedule.Intervals).All(p => p.First.StartSecond == p.Second.StartSecond && p.First.EndSecond == p.Second.EndSecond), "interval start/end times preserved");
Check(MathF.Abs(scaled150.Intervals[0].StartRadius - 75f) < 1e-3f && MathF.Abs(scaled150.Intervals[0].EndRadius - 54f) < 1e-3f, "interval radii scaled (75 -> 54)");
Check(scaled150.Intervals[^1].EndRadius == 0f, "final scaled radius is 0");
Check(!ReferenceEquals(scaled150.Intervals[0], baseSchedule.Intervals[0]), "intervals are copied, not shared");
Check(!Throws(scaled150.Validate), "scaled schedule validates");

// Base schedule must remain unmodified after scaling.
Check(baseSchedule.InitialCenter.X == 25f && MathF.Abs(baseSchedule.InitialRadius - 25f) < 1e-3f, "base schedule center/radius unchanged after ForMap");
Check(MathF.Abs(baseSchedule.Intervals[0].StartRadius - 25f) < 1e-3f && MathF.Abs(baseSchedule.ShrinkSpeedCellsPerSecond - 1f) < 1e-3f, "base schedule radii/speed unchanged after ForMap");

// Simulation uses scaled schedule for map, base schedule is not mutated.
var simA = new ZoneSimulation(baseSchedule, 12345, 2f, map150);
Check(simA.CurrentCenter.X == 75f && simA.CurrentCenter.Y == 75f, "simulation starts at scaled center (75, 75)");
Check(MathF.Abs(simA.CurrentRadius - 75f) < 1e-3f, "simulation starts with scaled radius 75");
simA.Update(390.0, 11700);
Check(simA.CurrentRadius == 0f, "simulation reaches final radius 0 at last end time");
Check(simA.IsOutside(75f, 75f), "r0 damages whole map including center");
Check(baseSchedule.InitialCenter.X == 25f && MathF.Abs(baseSchedule.Intervals[0].StartRadius - 25f) < 1e-3f, "base schedule not mutated by constructor");

// Repeated construction from the same base must not double-scale.
var simB = new ZoneSimulation(baseSchedule, 12345, 2f, map150);
Check(MathF.Abs(simB.Schedule.InitialRadius - 75f) < 1e-3f, "second simulation from same base is not double-scaled");
Check(MathF.Abs(simB.Schedule.Intervals[0].StartRadius - 75f) < 1e-3f, "second simulation interval radii not double-scaled");
Check(MathF.Abs(baseSchedule.InitialRadius - 25f) < 1e-3f, "base schedule still base-sized after repeats");

// 50 map and null map preserve existing behavior.
Check(ReferenceEquals(baseSchedule.ForMap(map50), baseSchedule), "ForMap returns unchanged base for 50 map");
Check(ReferenceEquals(baseSchedule.ForMap(null), baseSchedule), "ForMap returns unchanged base for null map");
var sim50 = new ZoneSimulation(baseSchedule, 12345, 2f, map50);
Check(MathF.Abs(sim50.CurrentRadius - 25f) < 1e-3f && sim50.CurrentCenter.X == 25f, "50 map simulation keeps base radius 25 at (25, 25)");
var simNoMap = new ZoneSimulation(baseSchedule, 12345);
Check(MathF.Abs(simNoMap.CurrentRadius - 25f) < 1e-3f && simNoMap.CurrentCenter.X == 25f, "no-map simulation keeps base radius 25 at (25, 25)");

// Invalid inputs.
var nonSquare = new MapData { MaxX = 60f, MaxY = 50f };
for (var i = 0; i < 10; i++)
{
    var angle = i * MathF.PI * 2f / 10f;
    nonSquare.SpawnPoints.Add((30f + 10f * MathF.Cos(angle), 25f + 10f * MathF.Sin(angle)));
}
var nonSquareRejected = Throws(nonSquare.Validate) || Throws(() => baseSchedule.ForMap(nonSquare));
Check(nonSquareRejected, "non-square map is rejected");

var unsafeMap = MakeMap(150f, 150f, 10f);
unsafeMap.SpawnPoints[9] = (140f, 140f);
var unsafeRejected = Throws(unsafeMap.Validate) || Throws(() => baseSchedule.ForMap(unsafeMap));
Check(unsafeRejected, "unsafe spawn outside spawn circle is rejected");

Console.WriteLine($"\n=== Test Results: {passed} passed, {failed} failed ===");
if (failed > 0)
{
    Environment.Exit(1);
}
else
{
    Console.WriteLine("ALL MAP SCALE CHECKS PASSED SUCCESSFULLY!");
    Environment.Exit(0);
}
