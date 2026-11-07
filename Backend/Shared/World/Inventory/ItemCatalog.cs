#nullable disable
using System;
using System.Collections.Generic;

namespace JungleMaze.World.Inventory
{
    public enum ItemKind : byte { Unarmed, Weapon, Consumable, Helmet, Armor, Ammo }
    [Flags] public enum LootSource : byte { None = 0, Pot = 1, Chest = 2, Supply = 4, Death = 8 }
    public enum ItemEffect : byte { None, Speed, Cleanse, Invisible, LifePassive, Compass }
    /// <summary>Display/loot tier. Floor loot scales item weights per zone ring by rarity; clients color the glow by it.</summary>
    public enum ItemRarity : byte { Common, Uncommon, Rare, Epic, Legendary }
    /// <summary>Floor-loot ring by distance to the map center: Outer (r &gt; 0.66R), Middle, Inner (r &lt; 0.33R).</summary>
    public enum LootTier : byte { Outer, Middle, Inner }

    /// <summary>Machine-readable item rules. These values remain a tentative GDD profile.</summary>
    [Serializable]
    public sealed class ItemDefinition
    {
        public string Id = "";
        public ItemKind Kind;
        public int MaxStack = 1;
        public bool Enabled = true;
        public LootSource Sources;
        public int Weight = 1;
        public ItemRarity Rarity;
        public int MagazineSize;
        public int ReserveCapacity;
        public float Reduction;
        public float Durability;
        public float Heal;
        public double UseSeconds;
        public ItemEffect Effect;
        public double EffectSeconds;
        public bool UseWhileControlled;
        public bool IgnoreStunCancel;
        public bool EffectAtStart;

        public ItemDefinition Copy() => (ItemDefinition)MemberwiseClone();

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(Id) || Id.Length > 40 || MaxStack < 1 || MaxStack > 999 || Weight < 0 || Weight > 100000)
                throw new ArgumentException("Invalid item identity, stack or weight.");
            if (!Finite(Reduction) || Reduction < 0 || Reduction > 1 || !Finite(Durability) || Durability < 0 ||
                !Finite(Heal) || Heal < 0 || !Finite(UseSeconds) || UseSeconds < 0 || !Finite(EffectSeconds) || EffectSeconds < 0 ||
                MagazineSize < 0 || ReserveCapacity < 0 || MagazineSize > 999 || ReserveCapacity > 9999)
                throw new ArgumentException("Invalid item numeric rule: " + Id);
            if (!Enum.IsDefined(typeof(ItemKind), Kind) || !Enum.IsDefined(typeof(ItemEffect), Effect) || !Enum.IsDefined(typeof(ItemRarity), Rarity) || ((byte)Sources & ~15) != 0)
                throw new ArgumentException("Invalid item rule enum: " + Id);
            if ((MagazineSize == 0) != (ReserveCapacity == 0)) throw new ArgumentException("Gun needs magazine and reserve: " + Id);
            if (Kind == ItemKind.Unarmed && Sources != LootSource.None) throw new ArgumentException("HAND cannot drop.");
        }
        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }

    [Serializable]
    public struct ItemStack
    {
        public string ItemId;
        public int Count;
        public float Durability;
        public int Ammo;
        /// <summary>Loaded rounds belong to the weapon instance, separately from reserve Ammo.</summary>
        public int Magazine;
        public bool MagazineKnown;
        public ItemStack(string itemId, int count = 1, float durability = 0, int ammo = 0, int magazine = -1)
        { ItemId = itemId; Count = count; Durability = durability; Ammo = ammo; Magazine = Math.Max(0, magazine); MagazineKnown = magazine >= 0; }
        public bool IsEmpty => string.IsNullOrEmpty(ItemId) || Count <= 0;
    }

    public sealed class ItemCatalog
    {
        private readonly Dictionary<string, ItemDefinition> _items = new Dictionary<string, ItemDefinition>(StringComparer.Ordinal);
        public string Version { get; }
        public IReadOnlyCollection<ItemDefinition> Items
        {
            get { var copy = new List<ItemDefinition>(); foreach (var item in _items.Values) copy.Add(item.Copy()); return copy.AsReadOnly(); }
        }
        public ItemCatalog(string version, IEnumerable<ItemDefinition> items)
        {
            if (string.IsNullOrWhiteSpace(version) || items == null) throw new ArgumentException("Catalog/version required.");
            Version = version;
            foreach (var item in items)
            {
                if (item == null) throw new ArgumentException("Null catalog item.");
                item.Validate();
                if (_items.ContainsKey(item.Id)) throw new ArgumentException("Duplicate catalog ID: " + item.Id);
                _items.Add(item.Id, item.Copy());
            }
            if (!_items.ContainsKey("HAND") || !_items.ContainsKey("AMMO")) throw new ArgumentException("Catalog requires HAND and AMMO.");
        }
        public bool TryGet(string id, out ItemDefinition item)
        {
            var found = _items.TryGetValue(id ?? "", out var value); item = found ? value.Copy() : null; return found;
        }
        public ItemDefinition Get(string id) => _items.TryGetValue(id ?? "", out var value) ? value.Copy() : throw new ArgumentException("Unknown item: " + id);

        /// <summary>Fixture/demo default; production should load the versioned items JSON via an adapter.</summary>
        public static ItemCatalog CreateBaseline()
        {
            var all = new List<ItemDefinition>();
            var pc = LootSource.Pot | LootSource.Chest | LootSource.Death;
            var cs = LootSource.Chest | LootSource.Supply | LootSource.Death;
            var c = LootSource.Chest | LootSource.Death;
            all.Add(new ItemDefinition { Id = "HAND", Kind = ItemKind.Unarmed });
            foreach (var id in new[] { "TREE_BRANCH", "KNIFE", "BONE", "AXE", "MACE" })
                all.Add(new ItemDefinition { Id = id, Kind = ItemKind.Weapon, Sources = id == "MACE" ? LootSource.Supply | LootSource.Death : id == "AXE" ? c : pc, Weight = 25 });
            var guns = new[] { "RUBBER_GUN", "SCEPTER", "CROSSBOW", "SHOTGUN", "PISTOL", "ASSAULT_RIFLE", "SNIPER_RIFLE" };
            var magazines = new[] { 12, 10, 6, 4, 12, 24, 5 }; var reserve = new[] { 60, 50, 30, 20, 60, 96, 20 };
            var gunWeights = new[] { 25, 25, 25, 8, 14, 8, 3 };
            for (var i = 0; i < guns.Length; i++) all.Add(new ItemDefinition { Id = guns[i], Kind = ItemKind.Weapon, Sources = i >= 3 ? cs : c, Weight = gunWeights[i], Enabled = i >= 3, MagazineSize = magazines[i], ReserveCapacity = reserve[i] });
            var thrown = new[] { "BANANA", "SMOKE", "METEORITE_FRAGMENT", "CRYSTAL", "SEED", "ROPE", "BELL", "SAHUR_BAT" };
            var caps = new[] { 3, 2, 3, 2, 3, 1, 1, 1 }; var weights = new[] { 25, 20, 20, 15, 20, 12, 10, 12 };
            for (var i = 0; i < thrown.Length; i++) all.Add(new ItemDefinition { Id = thrown[i], Kind = ItemKind.Weapon, MaxStack = caps[i], Sources = i == 0 || i == 4 ? pc : c, Weight = weights[i] });
            var armor = new[] { "WOODEN_HELMET", "SPIKE_HELMET", "MAGIC_HELMET", "WOODEN_ARMOR", "ARMOR", "MAGIC_ARMOR" };
            var reductions = new[] { .1f, .2f, .3f, .2f, .35f, .5f }; var durability = new[] { 30f, 50f, 70f, 50f, 80f, 120f };
            var armorWeight = new[] { 40, 25, 12, 35, 22, 10 };
            for (var i = 0; i < armor.Length; i++) all.Add(new ItemDefinition { Id = armor[i], Kind = i < 3 ? ItemKind.Helmet : ItemKind.Armor, Reduction = reductions[i], Durability = durability[i], Weight = armorWeight[i], Sources = i == 0 || i == 3 ? pc : i == 2 || i == 5 ? cs : c });
            var healIds = new[] { "GOD_LEAF", "SPIRIT_TEAR", "MERMAID_TAIL", "PHOENIX_FEATHERS", "UNICORN_BLOOD" };
            var heals = new[] { 10f, 15f, 25f, 45f, 100f }; var seconds = new[] { .5, .8, 1.2, 2.0, 3.5 }; var carry = new[] { 6, 5, 4, 3, 2 }; var hw = new[] { 25, 20, 15, 10, 6 };
            for (var i = 0; i < healIds.Length; i++) all.Add(new ItemDefinition { Id = healIds[i], Kind = ItemKind.Consumable, MaxStack = carry[i], Heal = heals[i], UseSeconds = seconds[i], Weight = hw[i], Sources = i < 2 ? pc : i == 4 ? cs : c });
            all.Add(new ItemDefinition { Id = "ADRENALINE", Kind = ItemKind.Consumable, MaxStack = 4, UseSeconds = 1.5, Effect = ItemEffect.Speed, EffectSeconds = 8, Weight = 12, Sources = pc });
            all.Add(new ItemDefinition { Id = "ELIXIR", Kind = ItemKind.Consumable, MaxStack = 2, Heal = 5, UseSeconds = 1, Effect = ItemEffect.Cleanse, EffectSeconds = 7, UseWhileControlled = true, IgnoreStunCancel = true, EffectAtStart = true, Weight = 6, Sources = cs });
            all.Add(new ItemDefinition { Id = "MAGIC", Kind = ItemKind.Consumable, MaxStack = 2, UseSeconds = 1, Effect = ItemEffect.Invisible, EffectSeconds = 5, Weight = 6, Sources = c });
            all.Add(new ItemDefinition { Id = "ELIXIR_OF_LIFE", Kind = ItemKind.Consumable, Heal = 50, Effect = ItemEffect.LifePassive, EffectSeconds = 2, Sources = LootSource.Supply | LootSource.Death });
            all.Add(new ItemDefinition { Id = "COMPASS", Kind = ItemKind.Consumable, UseSeconds = 2, Effect = ItemEffect.Compass, EffectSeconds = 5, Sources = LootSource.Supply | LootSource.Death });
            all.Add(new ItemDefinition { Id = "AMMO", Kind = ItemKind.Ammo, Sources = pc });
            foreach (var item in all) item.Rarity = BaselineRarity(item.Id);
            return new ItemCatalog("gdd-v0.3-proposal", all);
        }

        /// <summary>Docs/hvu/07_Floor_Loot.vi.md section 6. Unlisted IDs stay Common.</summary>
        private static ItemRarity BaselineRarity(string id)
        {
            switch (id)
            {
                case "AXE": case "SMOKE": case "METEORITE_FRAGMENT": case "SPIKE_HELMET": case "ARMOR": case "MERMAID_TAIL": case "ADRENALINE": case "RUBBER_GUN":
                    return ItemRarity.Uncommon;
                case "CRYSTAL": case "ROPE": case "SAHUR_BAT": case "PHOENIX_FEATHERS": case "MAGIC": case "CROSSBOW": case "ASSAULT_RIFLE":
                    return ItemRarity.Rare;
                case "SHOTGUN": case "BELL": case "MAGIC_HELMET": case "MAGIC_ARMOR": case "ELIXIR": case "UNICORN_BLOOD": case "SCEPTER": case "SNIPER_RIFLE":
                    return ItemRarity.Epic;
                case "MACE": case "ELIXIR_OF_LIFE": case "COMPASS":
                    return ItemRarity.Legendary;
                default:
                    return ItemRarity.Common;
            }
        }
    }

    [Serializable]
    public sealed class ItemCatalogDocument
    {
        public string Version = "";
        public ItemDefinition[] Items = Array.Empty<ItemDefinition>();
        public LootProfile Loot = new LootProfile();
        public ItemCatalog Build() => new ItemCatalog(Version, Items);
    }
    [Serializable]
    public sealed class SupplyDropRule
    {
        public string ItemId = "";
        public int ChancePercent = 100;
        public int MinCount = 1, MaxCount = 1;
        public int Ammo;
    }
    [Serializable]
    public sealed class LootProfile
    {
        public int ChestMin = 3, ChestMax = 4, PotMin = 1, PotMax = 2;
        public int[] ChestGroupWeights = { 15, 20, 25, 15, 15, 10 };
        public int[] PotGroupWeights = { 25, 8, 30, 12, 20, 5 };
        public SupplyDropRule[] Supply = {
            new SupplyDropRule { ItemId="MACE" }, new SupplyDropRule { ItemId="COMPASS" },
            new SupplyDropRule { ItemId="SHOTGUN",ChancePercent=70,Ammo=20 },
            new SupplyDropRule { ItemId="MAGIC_ARMOR",ChancePercent=60 },
            new SupplyDropRule { ItemId="MAGIC_HELMET",ChancePercent=50 },
            new SupplyDropRule { ItemId="ELIXIR_OF_LIFE",ChancePercent=35 },
            new SupplyDropRule { ItemId="UNICORN_BLOOD",ChancePercent=80,MaxCount=2 },
            new SupplyDropRule { ItemId="ELIXIR",ChancePercent=60 }
        };
        /// <summary>Floor loot spread at match start, gun ammo companions and container drop scatter. Missing in JSON = defaults.</summary>
        public FloorLootProfile Floor = new FloorLootProfile();
    }
    /// <summary>
    /// Floor loot (Docs/hvu/07_Floor_Loot.vi.md). Spots are fixed per map geometry; what lies on a spot is rolled per match.
    /// Distances are in map cells. See section 9 of that document for the calibration of these defaults.
    /// </summary>
    [Serializable]
    public sealed class FloorLootProfile
    {
        public bool Enabled = true;
        /// <summary>Reachable walkable cells inside the start zone per floor spot, before subtracting containers.</summary>
        public float AreaPerSpot = 55;
        /// <summary>Spots a pre-placed container replaces (expected items / SpotChance keeps total loot similar on any container mix).</summary>
        public float ChestSpots = 4.5f, PotSpots = 2, SupplySpots = 0;
        /// <summary>Chance that a spot has an item this match.</summary>
        public float SpotChance = .75f;
        /// <summary>Six LootTable groups: melee, gun, ammo, throwable, consumable, armor.</summary>
        public int[] GroupWeights = { 22, 8, 3, 22, 25, 20 };
        /// <summary>[LootTier Outer, Middle, Inner][ItemRarity Common..Legendary] multipliers on item weight.</summary>
        public float[][] TierRarityMultipliers = {
            new[] { 1.3f, 1f, .45f, .2f, .1f },
            new[] { 1f, 1.1f, 1f, .6f, .3f },
            new[] { .6f, 1f, 1.5f, 1f, 1f }
        };
        public float InnerTierFraction = .33f, OuterTierFraction = .66f;
        /// <summary>Poisson spacing between spots: indoor (cover within IndoorProbe in 7 of 8 directions) and elsewhere.</summary>
        public float IndoorSpacing = 3, OutdoorSpacing = 5;
        public int IndoorProbe = 4, CoverProbe = 2;
        public float SpawnClearance = 3, ObjectClearance = 1;
        /// <summary>Each spawn gets at least MinNearSpawn filled spots between NearSpawnMin and NearSpawnMax, one of them melee or healing.</summary>
        public int MinNearSpawn = 2;
        public float NearSpawnMin = 6, NearSpawnMax = 14;
        /// <summary>AMMO count placed next to every gun with a magazine, AmmoMin..AmmoMax cells away.</summary>
        public int AmmoBoxes = 2;
        public float AmmoMinDistance = .8f, AmmoMaxDistance = 1.2f;
        /// <summary>World objects kept free for death drops and swaps; worst-case container drops are reserved on top.</summary>
        public int ReserveObjects = 160;
        /// <summary>Container drops fan out ScatterMin..ScatterMax cells around the broken container.</summary>
        public float ScatterMinRadius = .9f, ScatterMaxRadius = 1.4f;
        /// <summary>Clearance radius of a dropped item from blocking geometry, hazards and portals.</summary>
        public float DropClearance = .3f;

        public FloorLootProfile Copy()
        {
            var copy = (FloorLootProfile)MemberwiseClone();
            copy.GroupWeights = GroupWeights == null ? null : (int[])GroupWeights.Clone();
            if (TierRarityMultipliers != null)
            {
                copy.TierRarityMultipliers = new float[TierRarityMultipliers.Length][];
                for (var i = 0; i < TierRarityMultipliers.Length; i++)
                    copy.TierRarityMultipliers[i] = TierRarityMultipliers[i] == null ? null : (float[])TierRarityMultipliers[i].Clone();
            }
            return copy;
        }

        public void Validate()
        {
            if (!Range(AreaPerSpot, 1, 100000) || !Range(ChestSpots, 0, 1000) || !Range(PotSpots, 0, 1000) || !Range(SupplySpots, 0, 1000) ||
                !Range(SpotChance, 0, 1) || !Range(InnerTierFraction, 0, 1) || !Range(OuterTierFraction, InnerTierFraction, 1) ||
                !Range(IndoorSpacing, 1, 32) || !Range(OutdoorSpacing, 1, 32) || IndoorProbe < 1 || IndoorProbe > 16 || CoverProbe < 0 || CoverProbe > 16 ||
                !Range(SpawnClearance, 0, 64) || !Range(ObjectClearance, 0, 8) || MinNearSpawn < 0 || MinNearSpawn > 16 ||
                !Range(NearSpawnMin, 0, 128) || !Range(NearSpawnMax, NearSpawnMin, 128) || AmmoBoxes < 1 || AmmoBoxes > 99 ||
                !Range(AmmoMinDistance, .3f, 4) || !Range(AmmoMaxDistance, AmmoMinDistance, 4) || ReserveObjects < 0 || ReserveObjects > 4096 ||
                !Range(ScatterMinRadius, .3f, 4) || !Range(ScatterMaxRadius, ScatterMinRadius, 4) || !Range(DropClearance, .05f, .5f))
                throw new ArgumentException("Invalid floor loot profile.");
            if (GroupWeights == null || GroupWeights.Length != 6) throw new ArgumentException("Floor loot needs six group weights.");
            var total = 0;
            foreach (var weight in GroupWeights) { if (weight < 0 || weight > 100000) throw new ArgumentException("Invalid floor loot group weight."); total += weight; }
            if (Enabled && total == 0) throw new ArgumentException("Floor loot group weights are all zero.");
            if (TierRarityMultipliers == null || TierRarityMultipliers.Length != 3) throw new ArgumentException("Floor loot needs three tier rows.");
            foreach (var row in TierRarityMultipliers)
            {
                if (row == null || row.Length != 5) throw new ArgumentException("Floor loot tier row needs five rarity multipliers.");
                foreach (var value in row) if (!Range(value, 0, 1000)) throw new ArgumentException("Invalid floor loot rarity multiplier.");
            }
        }
        private static bool Range(float value, float min, float max) => !float.IsNaN(value) && !float.IsInfinity(value) && value >= min && value <= max;
    }
    /// <summary>Versioned group weights and counts. Sampling conditions on feasible enabled groups, so reroll cannot hang.</summary>
    public sealed class LootTable
    {
        private readonly ItemCatalog _catalog;
        private readonly LootProfile _profile;
        public LootTable(ItemCatalog catalog, LootProfile profile = null)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            var source = profile ?? new LootProfile();
            _profile = new LootProfile { ChestMin = source.ChestMin, ChestMax = source.ChestMax, PotMin = source.PotMin, PotMax = source.PotMax,
                ChestGroupWeights = source.ChestGroupWeights == null ? null : (int[])source.ChestGroupWeights.Clone(),
                PotGroupWeights = source.PotGroupWeights == null ? null : (int[])source.PotGroupWeights.Clone(),
                Supply = source.Supply == null ? null : new SupplyDropRule[source.Supply.Length],
                Floor = source.Floor == null ? new FloorLootProfile() : source.Floor.Copy() };
            if (source.Supply != null) for (var i = 0; i < source.Supply.Length; i++)
            {
                var rule = source.Supply[i];
                if (rule != null) _profile.Supply[i] = new SupplyDropRule { ItemId = rule.ItemId, ChancePercent = rule.ChancePercent, MinCount = rule.MinCount, MaxCount = rule.MaxCount, Ammo = rule.Ammo };
            }
            if (_profile.ChestMin < 1 || _profile.ChestMax < _profile.ChestMin || _profile.ChestMax > 64 ||
                _profile.PotMin < 1 || _profile.PotMax < _profile.PotMin || _profile.PotMax > 64)
                throw new ArgumentException("Invalid loot counts.");
            foreach (var weights in new[] { _profile.ChestGroupWeights, _profile.PotGroupWeights })
            {
                if (weights == null || weights.Length != 6) throw new ArgumentException("Loot profile needs six group weights.");
                foreach (var weight in weights) if (weight < 0 || weight > 100000) throw new ArgumentException("Invalid loot group weight.");
            }
            if (_profile.Supply == null || _profile.Supply.Length > 64) throw new ArgumentException("Invalid supply table.");
            foreach (var rule in _profile.Supply)
                if (rule == null || !_catalog.TryGet(rule.ItemId, out _) || rule.ChancePercent < 0 || rule.ChancePercent > 100 || rule.MinCount < 1 || rule.MaxCount < rule.MinCount || rule.MaxCount > 999 || rule.Ammo < 0)
                    throw new ArgumentException("Invalid supply drop rule.");
            _profile.Floor.Validate();
            ValidateSources();
            if (_profile.Floor.Enabled) BuildFloorSamplers();
        }
        public ItemCatalog Catalog => _catalog;
        /// <summary>Defensive copy of the validated floor profile.</summary>
        public FloorLootProfile Floor => _profile.Floor.Copy();
        internal FloorLootProfile FloorProfile => _profile.Floor;
        /// <summary>Upper bound of items a container source can roll (chest/pot max, or one per supply rule).</summary>
        public int MaxRoll(LootSource source) => source == LootSource.Chest ? _profile.ChestMax : source == LootSource.Pot ? _profile.PotMax : _profile.Supply.Length;
        public ItemStack[] Roll(LootSource source, uint seed)
        {
            if (source != LootSource.Pot && source != LootSource.Chest && source != LootSource.Supply) throw new ArgumentException("Unsupported loot source.");
            var rng = new ItemRandom(seed);
            if (source == LootSource.Supply) return Supply(rng);
            var min = source == LootSource.Chest ? _profile.ChestMin : _profile.PotMin;
            var max = source == LootSource.Chest ? _profile.ChestMax : _profile.PotMax;
            var count = min + rng.Next(max - min + 1);
            var output = new ItemStack[count];
            for (var i = 0; i < count; i++)
            {
                var item = Sample(source, rng); output[i] = new ItemStack(item.Id, 1, item.Durability);
            }
            return output;
        }
        public void ValidateSources() { Sample(LootSource.Pot, new ItemRandom(1)); Sample(LootSource.Chest, new ItemRandom(1)); }
        private ItemDefinition Sample(LootSource source, ItemRandom rng)
        {
            var groups = new List<List<ItemDefinition>>(); var groupWeights = new List<int>();
            int[] weights = source == LootSource.Chest ? _profile.ChestGroupWeights : _profile.PotGroupWeights;
            for (var group = 0; group < 6; group++)
            {
                var options = new List<ItemDefinition>();
                foreach (var item in _catalog.Items) if (item.Enabled && item.Weight > 0 && (item.Sources & source) != 0 && Group(item) == group) options.Add(item);
                options.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
                if (options.Count > 0 && weights[group] > 0) { groups.Add(options); groupWeights.Add(weights[group]); }
            }
            if (groups.Count == 0) throw new InvalidOperationException("Loot source has no enabled eligible items: " + source);
            var selected = groups[Weighted(groupWeights, rng)]; var itemWeights = new List<int>();
            foreach (var item in selected) itemWeights.Add(item.Weight);
            return selected[Weighted(itemWeights, rng)];
        }
        private ItemStack[] Supply(ItemRandom rng)
        {
            var result = new List<ItemStack>();
            foreach (var rule in _profile.Supply)
            {
                var item = _catalog.Get(rule.ItemId);
                if (item.Enabled && (item.Sources & LootSource.Supply) != 0 && rng.Next(100) < rule.ChancePercent)
                    result.Add(new ItemStack(item.Id, rule.MinCount + rng.Next(rule.MaxCount - rule.MinCount + 1), item.Durability, rule.Ammo));
            }
            return result.ToArray();
        }
        private FloorSampler[] _floorAll, _floorNoGuns, _floorStarter;
        private sealed class FloorSampler
        {
            public readonly List<ItemDefinition> Items = new List<ItemDefinition>();
            public readonly List<double> Cumulative = new List<double>();
            public double Total;
            public void Add(ItemDefinition item, double weight) { if (weight <= 0) return; Total += weight; Items.Add(item); Cumulative.Add(Total); }
            public ItemDefinition Sample(ItemRandom rng)
            {
                if (Total <= 0) return null;
                var roll = rng.NextDouble() * Total;
                for (var i = 0; i < Cumulative.Count; i++) if (roll < Cumulative[i]) return Items[i];
                return Items[Items.Count - 1];
            }
        }
        /// <summary>
        /// Floor weight of an item = GroupWeights[group] * Weight * rarityMultiplier / (sum of Weight in its group). With all multipliers 1 this
        /// is exactly "pick group, then item by Weight"; multipliers can therefore also shift a group whose items are all rare (guns).
        /// </summary>
        private void BuildFloorSamplers()
        {
            var floor = _profile.Floor; var eligible = new List<ItemDefinition>(); var groupWeight = new double[6];
            foreach (var item in _catalog.Items)
                if (item.Enabled && item.Weight > 0 && item.Kind != ItemKind.Unarmed && (item.Sources & (LootSource.Pot | LootSource.Chest)) != 0) eligible.Add(item);
            eligible.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            foreach (var item in eligible) groupWeight[Group(item)] += item.Weight;
            _floorAll = new FloorSampler[3]; _floorNoGuns = new FloorSampler[3]; _floorStarter = new FloorSampler[3];
            for (var tier = 0; tier < 3; tier++)
            {
                _floorAll[tier] = new FloorSampler(); _floorNoGuns[tier] = new FloorSampler(); _floorStarter[tier] = new FloorSampler();
                var row = floor.TierRarityMultipliers[tier];
                foreach (var item in eligible)
                {
                    var group = Group(item); var multiplier = (double)row[(int)item.Rarity];
                    var weight = floor.GroupWeights[group] * (double)item.Weight * multiplier / groupWeight[group];
                    _floorAll[tier].Add(item, weight);
                    if (group != 1) _floorNoGuns[tier].Add(item, weight);
                    if (IsStarter(item)) _floorStarter[tier].Add(item, item.Weight * multiplier);
                }
                if (_floorAll[tier].Total <= 0) throw new ArgumentException("Floor loot tier has no eligible items: " + (LootTier)tier);
            }
        }
        /// <summary>Null only when no item fits (e.g. guns excluded and the tier offers only guns).</summary>
        internal ItemDefinition SampleFloor(LootTier tier, bool allowGuns, ItemRandom rng)
        {
            if (_floorAll == null) throw new InvalidOperationException("Floor loot is disabled.");
            return (allowGuns ? _floorAll : _floorNoGuns)[(int)tier].Sample(rng);
        }
        internal ItemDefinition SampleStarter(LootTier tier, ItemRandom rng) => _floorStarter == null ? null : _floorStarter[(int)tier].Sample(rng);
        /// <summary>Spawn fairness item: a melee weapon or a healing consumable.</summary>
        public static bool IsStarter(ItemDefinition item) => item != null && (Group(item) == 0 || item.Kind == ItemKind.Consumable && item.Heal > 0);
        public static bool IsGun(ItemDefinition item) => item != null && item.MagazineSize > 0;
        private static int Group(ItemDefinition i)
        {
            if (i.Kind == ItemKind.Ammo) return 2;
            if (i.Kind == ItemKind.Helmet || i.Kind == ItemKind.Armor) return 5;
            if (i.Kind == ItemKind.Consumable) return 4;
            if (i.MagazineSize > 0) return 1;
            return i.Id == "TREE_BRANCH" || i.Id == "KNIFE" || i.Id == "BONE" || i.Id == "AXE" || i.Id == "MACE" ? 0 : 3;
        }
        private static int Weighted(List<int> weights, ItemRandom rng)
        {
            var total = 0; foreach (var weight in weights) checked { total += weight; }
            var roll = rng.Next(total);
            for (var i = 0; i < weights.Count; i++) { roll -= weights[i]; if (roll < 0) return i; }
            throw new InvalidOperationException("Invalid loot weights.");
        }
    }
    internal sealed class ItemRandom
    {
        private uint _state;
        public ItemRandom(uint seed) { _state = seed == 0 ? 0x9e3779b9u : seed; }
        public int Next(int max) { if (max <= 0) throw new ArgumentOutOfRangeException(nameof(max)); _state ^= _state << 13; _state ^= _state >> 17; _state ^= _state << 5; return (int)(_state % (uint)max); }
        /// <summary>Uniform in [0, 1).</summary>
        public double NextDouble() { _state ^= _state << 13; _state ^= _state >> 17; _state ^= _state << 5; return _state / 4294967296.0; }
    }
}
