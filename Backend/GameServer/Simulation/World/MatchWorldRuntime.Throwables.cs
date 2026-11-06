using System.Text.Json;
using JungleMaze.World;
using JungleMaze.World.Throwables;

namespace GameServer.Simulation;

/// <summary>
/// WPN-03 bridge: runs <see cref="ThrowableService"/> against this match's players, inventory and map. Damage goes
/// to <see cref="ThrowableDamage"/> (WPN-02 owns HP); stun, root and reverse are Game-10 effects kept here.
/// </summary>
public sealed partial class MatchWorldRuntime : IThrowableWorld
{
    private readonly List<IEntity> _throwActors = new();
    private ThrowableService? _throwables;

    /// <summary>Set by the match: applies confirmed throwable damage through CombatService and returns the HP removed.</summary>
    public Func<byte, byte, int, int>? ThrowableDamage { get; set; }

    /// <summary>Every confirmed throwable event, for the room broadcast.</summary>
    public event Action<ThrowableEvent>? ThrowableRaised;

    public ThrowableService Throwables => _throwables ??= CreateThrowables(ThrowableRulesLoader.Default);

    /// <summary>Replaces the rule set (tests and custom profiles); clears every pending throw.</summary>
    public void UseThrowableRules(ThrowableRules rules) => _throwables = CreateThrowables(rules);

    private ThrowableService CreateThrowables(ThrowableRules rules)
    {
        var service = new ThrowableService(rules, this);
        service.Raised += e => ThrowableRaised?.Invoke(e);
        return service;
    }

    public ThrowableResult UseThrowable(byte actor, uint requestId, ThrowableKind kind, float aimDegrees, float distance)
    {
        SyncActors();
        RefreshThrowActors();
        return Throwables.Use(actor, requestId, kind, aimDegrees, distance, _now);
    }

    public void TickThrowables()
    {
        if (_throwables == null) return;
        RefreshThrowActors();
        _throwables.Tick(_now);
    }

    public bool IsReversed(byte actor) => HasEffect(actor, "Reverse");
    public bool IsRooted(byte actor) => HasEffect(actor, "Root");
    public bool IsInSmoke(float x, float y) => _throwables != null && _throwables.IsInSmoke(x, y);

    private bool HasEffect(byte actor, string effect) =>
        _effects.Any(e => e.Key.Actor == actor && e.Key.Effect == effect && e.Value.End > _now);

    private void RefreshThrowActors()
    {
        _throwActors.Clear();
        _throwActors.AddRange(_entities.All);
    }

    int IThrowableWorld.ActorCount => _throwActors.Count;

    ThrowActor IThrowableWorld.ActorAt(int index)
    {
        var e = _throwActors[index];
        return new ThrowActor(e.NetId, e.X, e.Y, e.Radius, e.IsAlive);
    }

    bool IThrowableWorld.TryGetActor(byte id, out ThrowActor actor)
    {
        if (_entities.TryGet(id, out var e) && e != null) { actor = new ThrowActor(e.NetId, e.X, e.Y, e.Radius, e.IsAlive); return true; }
        actor = default;
        return false;
    }

    ThrowableKind IThrowableWorld.HeldThrowable(byte actor) => ThrowableRules.KindOf(Inventory.HeldWeapon(actor).ItemId);

    bool IThrowableWorld.CanAct(byte actor) => Inventory.CanAttack(actor);

    bool IThrowableWorld.TryConsume(byte actor, ThrowableRule rule)
    {
        if (!Inventory.TryConsumeHeldUse(actor, rule.ItemId, rule.IsSpecial ? rule.Charges : 0)) return false;
        // Using a weapon breaks invisibility like any attack (same rule as CombatService attacks).
        AttackStarted(actor);
        return true;
    }

    float IThrowableWorld.CoverFraction(float x0, float y0, float x1, float y1)
    {
        var best = 1f;
        // Leaving the map counts as hitting its edge.
        best = Math.Min(best, ExitFraction(x0, x1, Layout.Size));
        best = Math.Min(best, ExitFraction(y0, y1, Layout.Size));
        foreach (var o in Layout.Objects)
        {
            var blocks = o.IsHardCover || (o.IsDestructible && !Destructibles.IsBroken(o.Id)) || (o.Kind == WorldObjectKind.Door && Hazards.IsDoorClosed(o.Id));
            if (!blocks) continue;
            if (SegmentEntry(x0, y0, x1, y1, o.Bounds, out var t) && t < best) best = t;
        }
        return best;
    }

    bool IThrowableWorld.IsWalkable(float x, float y, float radius) =>
        Layout.IsWalkable(new WorldPoint(x, y), radius, Destructibles.IsBroken, Hazards.IsDoorClosed);

    int IThrowableWorld.Damage(byte attacker, byte target, int amount) => ThrowableDamage?.Invoke(attacker, target, amount) ?? 0;

    void IThrowableWorld.ApplyEffect(byte target, ThrowableEffect effect, double seconds)
    {
        if (seconds <= 0) return;
        switch (effect)
        {
            case ThrowableEffect.Stun: ApplyStun(target, _tick, seconds); break;
            case ThrowableEffect.Root: ApplyControl(target, "Root", seconds); break;
            case ThrowableEffect.Reverse: ApplyControl(target, "Reverse", seconds); break;
        }
    }

    void IThrowableWorld.Move(byte actor, float x, float y)
    {
        if (!_entities.TryGet(actor, out var e) || e == null) return;
        e.X = x;
        e.Y = y;
    }

    /// <summary>Root and reverse respect the elixir's immunity, like stun.</summary>
    private void ApplyControl(byte actor, string effect, double seconds)
    {
        if (_effects.Any(e => e.Key.Actor == actor && e.Key.Effect == "Immune" && e.Value.End > _now)) return;
        _effects[(actor, 0, effect)] = (_now + seconds, 0);
    }

    private static float ExitFraction(float from, float to, float size)
    {
        if (to >= 0 && to <= size) return 1f;
        var limit = to < 0 ? 0f : size;
        var span = to - from;
        return Math.Abs(span) < 1e-6f ? 1f : Math.Clamp((limit - from) / span, 0f, 1f);
    }

    private static bool SegmentEntry(float x0, float y0, float x1, float y1, WorldBounds b, out float t)
    {
        float tMin = 0f, tMax = 1f;
        t = 0f;
        if (!Clip(x0, x1 - x0, b.MinX, b.MaxX, ref tMin, ref tMax) || !Clip(y0, y1 - y0, b.MinY, b.MaxY, ref tMin, ref tMax)) return false;
        t = tMin;
        return true;
    }

    private static bool Clip(float origin, float delta, float min, float max, ref float tMin, ref float tMax)
    {
        if (Math.Abs(delta) < 1e-6f) return origin >= min && origin <= max;
        var a = (min - origin) / delta;
        var b = (max - origin) / delta;
        if (a > b) (a, b) = (b, a);
        tMin = Math.Max(tMin, a);
        tMax = Math.Min(tMax, b);
        return tMin <= tMax;
    }
}

/// <summary>Reads the thrown and special rows of weapons.json into <see cref="ThrowableRules"/>.</summary>
public static class ThrowableRulesLoader
{
    private static ThrowableRules? _default;

    /// <summary>Rules from the packaged GameData/weapons.json, read once.</summary>
    public static ThrowableRules Default => _default ??= Parse(GameDataLoader.ReadGameDataFile("weapons.json"));

    public static ThrowableRules Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        var rules = new List<ThrowableRule>();
        foreach (var row in document.RootElement.EnumerateArray())
        {
            var category = row.TryGetProperty("category", out var c) ? c.GetString() : null;
            if (category is not ("Thrown" or "Special")) continue;
            var id = row.GetProperty("id").GetString() ?? "";
            var element = row;
            rules.Add(ThrowableRules.FromTable(id, key =>
                element.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : null));
        }
        return new ThrowableRules(rules);
    }
}
