#nullable disable
using System;
using System.Collections.Generic;

namespace JungleMaze.World.Throwables
{
    /// <summary>The five thrown and three special weapons of WPN-03. Values are stable wire codes; append only.</summary>
    public enum ThrowableKind : byte
    {
        None = 0,
        Banana = 1,
        Smoke = 2,
        Meteorite = 3,
        Crystal = 4,
        Seed = 5,
        Rope = 6,
        Bell = 7,
        Bat = 8
    }

    /// <summary>
    /// Rules for one thrown or special weapon. Numbers come from weapons.json; fields the table does not carry yet
    /// fall back to the GDD text (see <see cref="ThrowableRules.Baseline"/>), so adding them to the JSON later needs
    /// no code change.
    /// </summary>
    public sealed class ThrowableRule
    {
        public ThrowableKind Kind;
        public string ItemId = "";
        public int Damage;
        public double CooldownSeconds;
        /// <summary>Maximum throw distance, rope length, or bat reach, in cells.</summary>
        public float Range;
        /// <summary>Seconds from the throw to the detonation.</summary>
        public double FuseSeconds;
        /// <summary>Square blast or cloud size in cells (3×3 grenades, 7×7 smoke and bell).</summary>
        public float AreaWidth, AreaHeight;
        /// <summary>Uses per special weapon instance; thrown weapons spend one stack item per throw instead.</summary>
        public int Charges;
        /// <summary>Banana trap stun, bat stun on impact.</summary>
        public double StunSeconds;
        /// <summary>Seed root: cannot move, can still attack.</summary>
        public double RootSeconds;
        /// <summary>Bell: movement input reversed.</summary>
        public double ReverseSeconds;
        /// <summary>Smoke cloud lifetime.</summary>
        public double SmokeSeconds;
        /// <summary>How long an untouched banana trap stays armed.</summary>
        public double TrapSeconds;
        /// <summary>Trap trigger radius around its centre.</summary>
        public float TrapRadius;
        /// <summary>Bat knockback distance in cells.</summary>
        public float KnockbackCells;
        /// <summary>Bat swing arc.</summary>
        public float ArcDegrees;

        public bool IsThrown => Kind >= ThrowableKind.Banana && Kind <= ThrowableKind.Seed;
        public bool IsSpecial => Kind >= ThrowableKind.Rope && Kind <= ThrowableKind.Bat;

        public void Validate()
        {
            if (Kind == ThrowableKind.None || Kind > ThrowableKind.Bat || string.IsNullOrEmpty(ItemId))
                throw new ArgumentException("Invalid throwable kind or id.");
            if (Damage < 0 || !Finite(CooldownSeconds) || CooldownSeconds < 0 || !Finite(Range) || Range < 0 ||
                !Finite(FuseSeconds) || FuseSeconds < 0 || !Finite(AreaWidth) || AreaWidth < 0 || !Finite(AreaHeight) || AreaHeight < 0 ||
                !Finite(StunSeconds) || StunSeconds < 0 || !Finite(RootSeconds) || RootSeconds < 0 || !Finite(ReverseSeconds) || ReverseSeconds < 0 ||
                !Finite(SmokeSeconds) || SmokeSeconds < 0 || !Finite(TrapSeconds) || TrapSeconds < 0 || !Finite(TrapRadius) || TrapRadius < 0 ||
                !Finite(KnockbackCells) || KnockbackCells < 0 || !Finite(ArcDegrees) || ArcDegrees < 0 || ArcDegrees > 360)
                throw new ArgumentException("Invalid throwable numbers: " + ItemId);
            if (IsThrown && (Range <= 0 || AreaWidth <= 0 || AreaHeight <= 0)) throw new ArgumentException("Thrown weapon needs range and area: " + ItemId);
            if (IsSpecial && Charges <= 0) throw new ArgumentException("Special weapon needs charges: " + ItemId);
        }

        public ThrowableRule Copy() => (ThrowableRule)MemberwiseClone();

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }

    /// <summary>Validated set of the eight WPN-03 rules, keyed by kind and by item id.</summary>
    public sealed class ThrowableRules
    {
        private readonly Dictionary<ThrowableKind, ThrowableRule> _byKind = new Dictionary<ThrowableKind, ThrowableRule>();
        private readonly Dictionary<string, ThrowableRule> _byId = new Dictionary<string, ThrowableRule>();

        public ThrowableRules(IEnumerable<ThrowableRule> rules)
        {
            if (rules == null) throw new ArgumentNullException(nameof(rules));
            foreach (var source in rules)
            {
                var rule = source.Copy();
                rule.Validate();
                if (_byKind.ContainsKey(rule.Kind) || _byId.ContainsKey(rule.ItemId)) throw new ArgumentException("Duplicate throwable: " + rule.ItemId);
                _byKind[rule.Kind] = rule;
                _byId[rule.ItemId] = rule;
            }
        }

        public int Count => _byKind.Count;
        public bool TryGet(ThrowableKind kind, out ThrowableRule rule) => _byKind.TryGetValue(kind, out rule);
        public bool TryGet(string itemId, out ThrowableRule rule)
        {
            if (string.IsNullOrEmpty(itemId)) { rule = null; return false; }
            return _byId.TryGetValue(itemId, out rule);
        }

        /// <summary>Item id → kind for the eight WPN-03 weapons; None for anything else.</summary>
        public static ThrowableKind KindOf(string itemId)
        {
            switch (itemId)
            {
                case "BANANA": return ThrowableKind.Banana;
                case "SMOKE": return ThrowableKind.Smoke;
                case "METEORITE_FRAGMENT": return ThrowableKind.Meteorite;
                case "CRYSTAL": return ThrowableKind.Crystal;
                case "SEED": return ThrowableKind.Seed;
                case "ROPE": return ThrowableKind.Rope;
                case "BELL": return ThrowableKind.Bell;
                case "SAHUR_BAT": return ThrowableKind.Bat;
                default: return ThrowableKind.None;
            }
        }

        public static string IdOf(ThrowableKind kind)
        {
            switch (kind)
            {
                case ThrowableKind.Banana: return "BANANA";
                case ThrowableKind.Smoke: return "SMOKE";
                case ThrowableKind.Meteorite: return "METEORITE_FRAGMENT";
                case ThrowableKind.Crystal: return "CRYSTAL";
                case ThrowableKind.Seed: return "SEED";
                case ThrowableKind.Rope: return "ROPE";
                case ThrowableKind.Bell: return "BELL";
                case ThrowableKind.Bat: return "SAHUR_BAT";
                default: return "";
            }
        }

        /// <summary>
        /// Builds one rule from a weapons.json row. Missing optional numbers take the GDD defaults of
        /// <see cref="Baseline"/>; the Seed's <c>stunSeconds</c> column is its root time (the GDD says it can still attack).
        /// </summary>
        public static ThrowableRule FromTable(string itemId, Func<string, double?> number)
        {
            var kind = KindOf(itemId);
            if (kind == ThrowableKind.None) throw new ArgumentException("Not a throwable: " + itemId);
            var rule = BaselineRule(kind);
            double Read(string key, double fallback) { var v = number(key); return v ?? fallback; }
            rule.Damage = (int)Math.Round(Read("damage", rule.Damage));
            rule.CooldownSeconds = Read("cooldownSeconds", rule.CooldownSeconds);
            rule.Range = (float)Read("range", rule.Range);
            rule.FuseSeconds = Read("fuseSeconds", rule.FuseSeconds);
            rule.AreaWidth = (float)Read("areaWidth", rule.AreaWidth);
            rule.AreaHeight = (float)Read("areaHeight", rule.AreaHeight);
            rule.Charges = (int)Math.Round(Read("charges", rule.Charges));
            rule.ArcDegrees = (float)Read("arcDegrees", rule.ArcDegrees);
            var stun = number("stunSeconds");
            if (kind == ThrowableKind.Seed) rule.RootSeconds = Read("rootSeconds", stun ?? rule.RootSeconds);
            else rule.StunSeconds = stun ?? rule.StunSeconds;
            rule.ReverseSeconds = Read("reverseSeconds", rule.ReverseSeconds);
            rule.SmokeSeconds = Read("smokeSeconds", rule.SmokeSeconds);
            rule.TrapSeconds = Read("trapSeconds", rule.TrapSeconds);
            rule.TrapRadius = (float)Read("trapRadius", rule.TrapRadius);
            rule.KnockbackCells = (float)Read("knockbackCells", rule.KnockbackCells);
            return rule;
        }

        /// <summary>
        /// GDD baseline for all eight weapons (Weapons sheet rows 11–18). Trap lifetime (20 s), trap radius (0.6 cell)
        /// and bell/smoke durations not in the table are documented assumptions in Docs/Tickets/WPN-03.md.
        /// </summary>
        public static ThrowableRules Baseline()
        {
            var all = new List<ThrowableRule>();
            for (var k = ThrowableKind.Banana; k <= ThrowableKind.Bat; k++) all.Add(BaselineRule(k));
            return new ThrowableRules(all);
        }

        private static ThrowableRule BaselineRule(ThrowableKind kind)
        {
            var r = new ThrowableRule { Kind = kind, ItemId = IdOf(kind), CooldownSeconds = 1, FuseSeconds = 1.5, AreaWidth = 3, AreaHeight = 3 };
            switch (kind)
            {
                case ThrowableKind.Banana: r.Damage = 30; r.Range = 8; r.StunSeconds = 1; r.TrapSeconds = 20; r.TrapRadius = .6f; break;
                case ThrowableKind.Smoke: r.Damage = 0; r.Range = 6; r.FuseSeconds = 1; r.AreaWidth = 7; r.AreaHeight = 7; r.SmokeSeconds = 10; break;
                case ThrowableKind.Meteorite: r.Damage = 35; r.Range = 8; break;
                case ThrowableKind.Crystal: r.Damage = 45; r.Range = 7; break;
                case ThrowableKind.Seed: r.Damage = 20; r.Range = 7; r.RootSeconds = 1.5; break;
                case ThrowableKind.Rope: r.Damage = 15; r.CooldownSeconds = 2; r.Range = 8; r.Charges = 2; r.FuseSeconds = 0; r.AreaWidth = r.AreaHeight = 0; break;
                case ThrowableKind.Bell: r.Damage = 15; r.CooldownSeconds = 0; r.Range = 0; r.Charges = 1; r.FuseSeconds = 0; r.AreaWidth = 7; r.AreaHeight = 7; r.ReverseSeconds = 10; break;
                case ThrowableKind.Bat: r.Damage = 25; r.CooldownSeconds = 1.5; r.Range = 2.5f; r.Charges = 3; r.FuseSeconds = 0; r.AreaWidth = r.AreaHeight = 0; r.ArcDegrees = 90; r.StunSeconds = 2; r.KnockbackCells = 3; break;
            }
            return r;
        }
    }
}
