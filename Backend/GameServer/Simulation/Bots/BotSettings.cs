using System.Text.Json;

namespace GameServer.Simulation.Bots;

public sealed class BotSettings
{
    public string Version { get; init; } = "bots-normal-2";
    public int ThinkHz { get; init; } = 5;
    public float DetectRadius { get; init; } = 12;
    public float BushRevealRadius { get; init; } = 2;
    public double ReactionSeconds { get; init; } = .35;
    public double MemorySeconds { get; init; } = 3;
    public float AimErrorDegrees { get; init; } = 6;
    public float HealHpFraction { get; init; } = .4f;
    public float RetreatHpFraction { get; init; } = .25f;
    public float GridStep { get; init; } = .5f;
    public int PathNodesPerTick { get; init; } = 500;
    public int GlobalPathNodesPerTick { get; init; } = 1500;
    public double GlobalPathMillisecondsPerTick { get; init; } = 4;
    public double EscapeLeadSeconds { get; init; } = 2;
    public double RepathSeconds { get; init; } = 1;
    public double StuckSeconds { get; init; } = 2;
    public int FailedPathLimit { get; init; } = 3;
    public double BlacklistSeconds { get; init; } = 5;
    public static BotSettings Load(string json)
    {
        var v = JsonSerializer.Deserialize<BotSettings>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidDataException("Missing bot settings.");
        if (string.IsNullOrWhiteSpace(v.Version) || v.Version.Length > 32 || v.ThinkHz is < 1 or > 30 ||
            !float.IsFinite(v.DetectRadius) || v.DetectRadius is <= 0 or > 50 ||
            !float.IsFinite(v.BushRevealRadius) || v.BushRevealRadius <= 0 || v.BushRevealRadius > v.DetectRadius ||
            !double.IsFinite(v.ReactionSeconds) || v.ReactionSeconds is < .35 or > 5 ||
            !double.IsFinite(v.MemorySeconds) || v.MemorySeconds is <= 0 or > 30 ||
            !float.IsFinite(v.AimErrorDegrees) || v.AimErrorDegrees is < 0 or > 45 ||
            !float.IsFinite(v.HealHpFraction) || v.HealHpFraction is <= 0 or > 1 ||
            !float.IsFinite(v.RetreatHpFraction) || v.RetreatHpFraction <= 0 || v.RetreatHpFraction > v.HealHpFraction ||
            v.GridStep != .5f || v.PathNodesPerTick is < 1 or > 500 || v.GlobalPathNodesPerTick is < 1 or > 1500 ||
            !double.IsFinite(v.GlobalPathMillisecondsPerTick) || v.GlobalPathMillisecondsPerTick is <= 0 or > 10 ||
            !double.IsFinite(v.EscapeLeadSeconds) || v.EscapeLeadSeconds is < 0 or > 10 ||
            !double.IsFinite(v.RepathSeconds) || v.RepathSeconds is < 1 or > 30 ||
            !double.IsFinite(v.StuckSeconds) || v.StuckSeconds is <= 0 or > 30 || v.FailedPathLimit is < 1 or > 10 ||
            !double.IsFinite(v.BlacklistSeconds) || v.BlacklistSeconds is <= 0 or > 60)
            throw new InvalidDataException("Invalid bot settings.");
        return v;
    }
}

public enum BotState : byte { Idle, Roam, Loot, BreakContainer, Chase, Combat, Search, Retreat, Heal, EscapeZone, Dead }
