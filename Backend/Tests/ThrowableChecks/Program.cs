using System.Text.Json;
using JungleMaze.World;
using JungleMaze.World.Inventory;
using JungleMaze.World.Throwables;

// WPN-03 checks: the eight thrown/special weapons against a fake world with the real InventoryService and the
// packaged weapons.json. Run from repository root: dotnet run --project Backend/Tests/ThrowableChecks/ThrowableChecks.csproj
var passed = 0;
void Check(bool value, string label) { if (!value) throw new Exception("FAIL " + label); passed++; Console.WriteLine("PASS " + label); }
void Near(float actual, float expected, float tolerance, string label) => Check(Math.Abs(actual - expected) <= tolerance, $"{label} ({actual:0.###} ≈ {expected:0.###})");

// --- config -------------------------------------------------------------------------------------------------
var table = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "GameData", "weapons.json"))).RootElement;
var loaded = new List<ThrowableRule>();
foreach (var row in table.EnumerateArray())
{
    var category = row.GetProperty("category").GetString();
    if (category != "Thrown" && category != "Special") continue;
    var id = row.GetProperty("id").GetString();
    loaded.Add(ThrowableRules.FromTable(id, key => row.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : null));
}
var rules = new ThrowableRules(loaded);
Check(rules.Count == 8, "weapons.json gives all 8 WPN-03 weapons");
rules.TryGet(ThrowableKind.Seed, out var seedRule);
Check(seedRule.RootSeconds == 1.5 && seedRule.StunSeconds == 0, "seed column stunSeconds is a 1.5 s root, not a stun");
rules.TryGet(ThrowableKind.Bat, out var batRule);
Check(batRule.Charges == 3 && batRule.KnockbackCells == 3 && batRule.StunSeconds == 2 && batRule.ArcDegrees == 90, "bat: 3 charges, 3 cell knockback, 2 s stun, 90° arc");
rules.TryGet(ThrowableKind.Smoke, out var smokeRule);
Check(smokeRule.Damage == 0 && smokeRule.AreaWidth == 7 && smokeRule.SmokeSeconds == 10, "smoke: no damage, 7×7, 10 s");
var tuned = ThrowableRules.FromTable("BANANA", key => key == "damage" ? 99 : null);
Check(tuned.Damage == 99 && tuned.StunSeconds == 1 && tuned.TrapSeconds == 20, "config overrides one number, GDD defaults fill the rest");
var threw = false; try { new ThrowableRules(new[] { tuned, tuned }); } catch (ArgumentException) { threw = true; }
Check(threw, "duplicate throwable rejected");
threw = false; try { ThrowableRules.FromTable("KNIFE", _ => null); } catch (ArgumentException) { threw = true; }
Check(threw, "non-throwable id rejected");

// --- helpers ------------------------------------------------------------------------------------------------
Scene NewScene() => new Scene(rules);

// --- banana: fuse, area damage, trap -----------------------------------------------------------------------
{
    var s = NewScene();
    s.Give(0, "BANANA", 3);
    s.Place(1, 14, 10);
    Check(s.Use(0, 1, ThrowableKind.Banana, 0, 4, 0) == ThrowableResult.Ok, "banana thrown");
    Check(s.Inventory.HeldWeapon(0).Count == 2, "throw spends one banana from the stack");
    Check(s.Service.PendingCount == 1 && s.Hits.Count == 0, "no damage before the 1.5 s fuse");
    s.Tick(1.4);
    Check(s.Hits.Count == 0, "still no damage at 1.4 s");
    s.Tick(1.5);
    Check(s.Hits.Count == 1 && s.Hits[0] == (0, 1, 30), "detonation deals 30 to the target in 3×3");
    Check(s.Service.TrapCount == 1 && s.Events.Any(e => e.Kind == ThrowableEventKind.TrapArmed), "banana leaves a trap");
    s.Tick(1.9);
    Check(s.Effects.Count == 0, "trap needs 0.5 s to arm, blast victims can step off");
    s.Tick(2.0);
    Check(s.Effects.Count(e => e.Target == 1 && e.Effect == ThrowableEffect.Stun && e.Seconds == 1) == 1, "standing on the armed peel stuns 1 s");
    Check(s.Service.TrapCount == 0, "trap is spent after one trigger");
    s.Tick(2.5);
    Check(s.Effects.Count(e => e.Effect == ThrowableEffect.Stun) == 1, "a spent trap never stuns twice");
}

// --- thrower is spared, outside area untouched, trap expires -----------------------------------------------
{
    var s = NewScene();
    s.Give(0, "BANANA", 1);
    s.Place(1, 16, 10);
    s.Use(0, 1, ThrowableKind.Banana, 0, 1, 0);
    s.Tick(2);
    Check(s.Hits.Count == 0, "thrower and players outside 3×3 take no blast damage");
    Check(s.Inventory.HeldWeapon(0).IsEmpty, "last banana empties the slot");
    s.Tick(22.1);
    Check(s.Service.TrapCount == 0 && s.Events.Any(e => e.Kind == ThrowableEventKind.TrapExpired), "untouched trap expires after 20 s");
    s.Place(1, 11, 10);
    s.Tick(22.2);
    Check(s.Effects.Count == 0, "expired trap stuns nobody");
}

// --- duplicate and stale requests ----------------------------------------------------------------------------
{
    var s = NewScene();
    s.Give(0, "METEORITE_FRAGMENT", 3);
    s.Place(1, 14, 10);
    Check(s.Use(0, 5, ThrowableKind.Meteorite, 0, 4, 0) == ThrowableResult.Ok, "meteorite thrown");
    Check(s.Use(0, 5, ThrowableKind.Meteorite, 0, 4, 2) == ThrowableResult.Duplicate, "same request id is a duplicate");
    Check(s.Use(0, 4, ThrowableKind.Meteorite, 0, 4, 2) == ThrowableResult.Duplicate, "older request id is rejected");
    s.Tick(3);
    Check(s.Inventory.HeldWeapon(0).Count == 2 && s.Hits.Count == 1 && s.Hits[0].Amount == 35, "retries spend nothing and do not double the 35 damage");
    Check(s.Use(0, 6, ThrowableKind.Meteorite, 0, 4, 3.5) == ThrowableResult.Ok, "a new id after the 1 s cooldown is accepted");
    Check(s.Use(0, 7, ThrowableKind.Meteorite, 0, 4, 3.6) == ThrowableResult.Cooldown, "cooldown enforced");
}

// --- cover stops throws and shields players --------------------------------------------------------------------
{
    var s = NewScene();
    s.Walls.Add(new WorldBounds(13, 10, 1, 6));
    s.Give(0, "CRYSTAL", 2);
    s.Place(1, 14.5f, 10);
    s.Use(0, 1, ThrowableKind.Crystal, 0, 7, 0);
    var thrown = s.Events.First(e => e.Kind == ThrowableEventKind.Thrown);
    Check(thrown.ToX < 12.5f - ThrowableService.CoverStandOff + .01f, "crystal lands in front of the wall");
    s.Tick(2);
    Check(s.Hits.Count == 0, "crystal does not hurt through the wall");
    s.Walls.Clear();
    s.Use(0, 2, ThrowableKind.Crystal, 0, 4.5f, 2);
    s.Tick(4);
    Check(s.Hits.Count == 1 && s.Hits[0].Amount == 45, "crystal deals 45 with a clear line");
    Check(s.Inventory.HeldWeapon(0).IsEmpty, "two crystals (carry 2) are both spent");
    s.Give(0, "CRYSTAL", 1);
    Check(s.Use(0, 3, ThrowableKind.Crystal, 0, 20, 5) == ThrowableResult.Ok, "crystal thrown past its range");
    var far = s.Events.Last(e => e.Kind == ThrowableEventKind.Thrown);
    Near(far.ToX - far.FromX, 7, .01f, "throw distance clamped to crystal range 7");
}

// --- smoke ---------------------------------------------------------------------------------------------------
{
    var s = NewScene();
    s.Give(0, "SMOKE", 2);
    s.Place(1, 14, 10);
    s.Use(0, 1, ThrowableKind.Smoke, 0, 4, 0);
    s.Tick(1);
    Check(s.Service.SmokeCount == 1 && s.Service.IsInSmoke(14, 10) && s.Service.IsInSmoke(17.4f, 13.4f), "smoke covers 7×7 around the landing");
    Check(!s.Service.IsInSmoke(17.6f, 10), "outside the cloud is clear");
    Check(s.Hits.Count == 0 && s.Effects.Count == 0, "smoke deals no damage or control");
    s.Tick(10.9);
    Check(s.Service.SmokeCount == 1, "cloud lasts the full 10 s");
    s.Tick(11);
    Check(s.Service.SmokeCount == 0 && s.Events.Any(e => e.Kind == ThrowableEventKind.SmokeEnded), "cloud ends after 10 s");
}

// --- seed root ------------------------------------------------------------------------------------------------
{
    var s = NewScene();
    s.Give(0, "SEED", 3);
    s.Place(1, 14, 10);
    s.Use(0, 1, ThrowableKind.Seed, 0, 4, 0);
    s.Tick(1.5);
    Check(s.Hits.Count == 1 && s.Hits[0].Amount == 20, "seed deals 20");
    Check(s.Effects.Single(e => e.Target == 1).Effect == ThrowableEffect.Root && s.Effects[0].Seconds == 1.5, "seed roots 1.5 s (root, not stun: the target can still attack)");
}

// --- rope -------------------------------------------------------------------------------------------------------
{
    var s = NewScene();
    s.Give(0, "ROPE", 1);
    s.Place(1, 16, 10);
    Check(s.Use(0, 1, ThrowableKind.Rope, 0, 0, 0) == ThrowableResult.Ok, "rope cast");
    var p = s.Actors[1];
    Near(p.X, 10 + .4f + .4f + .05f, .11f, "rope pulls the enemy next to the user");
    Check(s.Hits.Single() == (0, 1, 15), "rope deals 15");
    Check(s.Inventory.HeldWeapon(0).Magazine == 1 && s.Inventory.HeldWeapon(0).MagazineKnown, "rope keeps 1 of 2 charges");

    s.Place(1, 30, 30);
    s.Walls.Add(new WorldBounds(16, 10, 1, 4));
    Check(s.Use(0, 2, ThrowableKind.Rope, 0, 0, 2.5) == ThrowableResult.Ok, "rope cast at a wall");
    Near(s.Actors[0].X, 15.5f - .4f - .05f, .11f, "rope pulls the user to the cover");
    Check(s.Inventory.HeldWeapon(0).IsEmpty, "second charge used up the rope");
    Check(s.Use(0, 3, ThrowableKind.Rope, 0, 0, 5) == ThrowableResult.WrongItem, "no rope left");
}
{
    var s = NewScene();
    s.Give(0, "ROPE", 1);
    s.Walls.Add(new WorldBounds(14, 10, 1, 4));
    s.Place(1, 16, 10);
    s.Use(0, 1, ThrowableKind.Rope, 0, 0, 0);
    Check(s.Actors[1].X == 16 && s.Hits.Count == 0, "rope cannot grab an enemy behind cover");
    Check(s.Events.Any(e => e.Kind == ThrowableEventKind.RopePulledSelf), "it pulls the user to that cover instead");
}
{
    var s = NewScene();
    s.Give(0, "ROPE", 1);
    s.Use(0, 1, ThrowableKind.Rope, 90, 0, 0);
    Check(s.Events.Any(e => e.Kind == ThrowableEventKind.RopeMissed) && s.Actors[0].Y == 10, "rope into open space misses and moves nobody");
}

// --- bell -------------------------------------------------------------------------------------------------------
{
    var s = NewScene();
    s.Give(0, "BELL", 1);
    s.Place(1, 13, 12);
    s.Place(2, 14, 10);
    Check(s.Use(0, 1, ThrowableKind.Bell, 0, 0, 0) == ThrowableResult.Ok, "bell rung");
    Check(s.Effects.Count(e => e.Effect == ThrowableEffect.Reverse && e.Seconds == 10 && e.Target == 1) == 1, "player inside 7×7 has movement reversed 10 s");
    Check(s.Effects.All(e => e.Target != 2), "player outside 7×7 is not affected");
    Check(s.Hits.Single() == (0, 1, 15), "bell deals 15 in the area");
    Check(s.Effects.All(e => e.Target != 0), "the ringer is not reversed");
    Check(s.Inventory.HeldWeapon(0).IsEmpty, "bell has one charge");
}

// --- bat --------------------------------------------------------------------------------------------------------
{
    var s = NewScene();
    s.Give(0, "SAHUR_BAT", 1);
    s.Place(1, 11.5f, 10);
    s.Place(2, 10, 12.5f);
    Check(s.Use(0, 1, ThrowableKind.Bat, 0, 0, 0) == ThrowableResult.Ok, "bat swung");
    Near(s.Actors[1].X, 14.5f, .01f, "bat knocks the target back 3 cells");
    Check(s.Effects.All(e => e.Target != 1), "free knockback does not stun");
    Check(s.Actors[2].X == 10 && s.Actors[2].Y == 12.5f, "players outside the 90° arc are untouched");
    Check(s.Hits.Single() == (0, 1, 25), "bat deals 25");
    Check(s.Inventory.HeldWeapon(0).Magazine == 2, "bat keeps 2 of 3 charges");

    s.Place(1, 11.5f, 10);
    s.Walls.Add(new WorldBounds(13, 10, 1, 4));
    Check(s.Use(0, 2, ThrowableKind.Bat, 0, 0, 1) == ThrowableResult.Cooldown, "bat cooldown 1.5 s");
    Check(s.Use(0, 3, ThrowableKind.Bat, 0, 0, 1.6) == ThrowableResult.Ok, "bat swung again");
    Check(s.Actors[1].X < 12.5f - .4f + .01f, "knockback stops at the wall");
    Check(s.Effects.Single(e => e.Target == 1).Effect == ThrowableEffect.Stun && s.Effects[0].Seconds == 2, "hitting the wall stuns 2 s");
}
{
    var s = NewScene();
    s.Give(0, "SAHUR_BAT", 1);
    s.Place(1, 11.5f, 10);
    s.Place(2, 13.2f, 10);
    s.Use(0, 1, ThrowableKind.Bat, 0, 0, 0);
    Check(s.Effects.Any(e => e.Target == 1 && e.Effect == ThrowableEffect.Stun), "knocked into another player: stunned 2 s");
}

// --- user state ---------------------------------------------------------------------------------------------------
{
    var s = NewScene();
    s.Give(0, "BANANA", 3);
    s.Stunned.Add(0);
    Check(s.Use(0, 1, ThrowableKind.Banana, 0, 3, 0) == ThrowableResult.Controlled, "stunned players cannot throw");
    Check(s.Inventory.HeldWeapon(0).Count == 3, "a refused throw spends nothing");
    s.Stunned.Clear();
    Check(s.Use(0, 2, ThrowableKind.Seed, 0, 3, 0) == ThrowableResult.WrongItem, "request must match the held weapon");
    s.Actors[0] = s.Actors[0] with { Alive = false };
    Check(s.Use(0, 3, ThrowableKind.Banana, 0, 3, 0) == ThrowableResult.Dead, "dead players cannot throw");
    Check(s.Use(0, 4, ThrowableKind.Banana, float.NaN, 3, 0) == ThrowableResult.Invalid, "NaN aim rejected");
}

Console.WriteLine($"ThrowableChecks: {passed} passed");
return 0;

/// <summary>Fake match: players are circles, walls are boxes, inventory is the real service.</summary>
sealed class Scene : IThrowableWorld, IItemGameplayPort
{
    public readonly Dictionary<byte, ThrowActor> Actors = new();
    public readonly List<WorldBounds> Walls = new();
    public readonly HashSet<byte> Stunned = new();
    public readonly List<(byte Attacker, byte Target, int Amount)> Hits = new();
    public readonly List<(byte Target, ThrowableEffect Effect, double Seconds)> Effects = new();
    public readonly List<ThrowableEvent> Events = new();
    public readonly InventoryService Inventory;
    public readonly ThrowableService Service;
    private readonly Guid _match = Guid.NewGuid();
    private readonly MemoryItemWorld _floor;
    private ushort _nextFloor = 100;
    private uint _requests;

    public Scene(ThrowableRules rules)
    {
        _floor = new MemoryItemWorld(() => ++_nextFloor);
        Inventory = new InventoryService(_match, ItemCatalog.CreateBaseline(), this, _floor);
        Service = new ThrowableService(rules, this);
        Service.Raised += e => Events.Add(e);
        Place(0, 10, 10);
    }

    public void Place(byte id, float x, float y)
    {
        var known = Actors.TryGetValue(id, out var old);
        Actors[id] = new ThrowActor(id, x, y, .4f, !known || old.Alive);
        if (!known) Inventory.Register(id);
        Inventory.SetActorState(id, x, y, true);
    }

    public void Give(byte id, string item, int count)
    {
        if (!_floor.TrySpawn(new ItemStack(item, count), Actors[id].X, Actors[id].Y, out var floorId)) throw new Exception("spawn");
        if (Inventory.Pickup(_match, id, ++_requests, floorId) != ItemResult.Ok) throw new Exception("pickup " + item);
        var snapshot = Inventory.GetSnapshot(id);
        var slot = Array.FindIndex(snapshot.Weapons, w => w.ItemId == item);
        if (snapshot.ActiveWeapon != slot && Inventory.Select(_match, id, ++_requests, slot) != ItemResult.Ok) throw new Exception("select " + item);
    }

    public ThrowableResult Use(byte actor, uint request, ThrowableKind kind, float aim, float distance, double now) =>
        Service.Use(actor, request, kind, aim, distance, now);

    public void Tick(double now) => Service.Tick(now);

    public int ActorCount => Actors.Count;
    public ThrowActor ActorAt(int index) => Actors.Values.ElementAt(index);
    public bool TryGetActor(byte id, out ThrowActor actor) => Actors.TryGetValue(id, out actor);
    public ThrowableKind HeldThrowable(byte actor) => ThrowableRules.KindOf(Inventory.HeldWeapon(actor).ItemId);
    public bool CanAct(byte actor) => !Stunned.Contains(actor);
    public bool TryConsume(byte actor, ThrowableRule rule) => Inventory.TryConsumeHeldUse(actor, rule.ItemId, rule.IsSpecial ? rule.Charges : 0);

    public float CoverFraction(float x0, float y0, float x1, float y1)
    {
        var best = 1f;
        foreach (var w in Walls)
            if (Segment(x0, y0, x1, y1, w, out var t) && t < best) best = t;
        return best;
    }

    public bool IsWalkable(float x, float y, float radius) => !Walls.Any(w => w.IntersectsCircle(new WorldPoint(x, y), radius));
    public int Damage(byte attacker, byte target, int amount) { Hits.Add((attacker, target, amount)); return amount; }
    public void ApplyEffect(byte target, ThrowableEffect effect, double seconds) => Effects.Add((target, effect, seconds));
    public void Move(byte actor, float x, float y) { var a = Actors[actor]; a.X = x; a.Y = y; Actors[actor] = a; }

    private static bool Segment(float x0, float y0, float x1, float y1, WorldBounds b, out float t)
    {
        float tMin = 0, tMax = 1;
        t = 0;
        if (!Clip(x0, x1 - x0, b.MinX, b.MaxX, ref tMin, ref tMax) || !Clip(y0, y1 - y0, b.MinY, b.MaxY, ref tMin, ref tMax)) return false;
        t = tMin;
        return true;
    }

    private static bool Clip(float o, float d, float min, float max, ref float tMin, ref float tMax)
    {
        if (Math.Abs(d) < 1e-6f) return o >= min && o <= max;
        var a = (min - o) / d; var b = (max - o) / d;
        if (a > b) (a, b) = (b, a);
        tMin = Math.Max(tMin, a); tMax = Math.Min(tMax, b);
        return tMin <= tMax;
    }

    // IItemGameplayPort: inventory never needs these in throwable checks.
    public void Heal(byte actor, float amount) { }
    public void ApplyEffect(byte actor, ItemEffect effect, double seconds) { }
    public void Rescue(byte actor, float hp, double invulnerableSeconds) { }
}
