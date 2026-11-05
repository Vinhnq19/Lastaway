using System;
using System.Collections.Generic;
using JungleBackend.Protocol.Gameplay;
using JungleMaze.App.Combat;
using JungleMaze.App.Flow;
using JungleMaze.App.Hud.State;
using JungleMaze.App.Match;
using JungleMaze.App.Ports;
using JungleMaze.Core.Ticking;
using JungleMaze.Domain.Hud;
using JungleMaze.World;
using JungleMaze.World.Inventory;

namespace JungleMaze.App.World
{
    /// <summary>
    /// In-process authoritative world session for local offline play (Game scene).
    /// Runs authored map placements, destructible one-time loot drops, floor items, inventory,
    /// hazards, armor absorption, and inventory-backed combat locally without network or WorldDemoController.
    /// </summary>
    public sealed class OfflineWorldSession : ITickable, IDisposable,
        ITransactionalDestructibleLootSink, IEnvironmentDamageSink, IEnvironmentEffectSink, IEnvironmentMovementSink, IItemGameplayPort
    {
        private readonly OfflineMatchSession _match;
        private readonly MapLayout _map;
        private readonly DestructibleConfig _destructibleConfig;
        private readonly HazardConfig _hazardConfig;
        private readonly ItemCatalog _catalog;
        private readonly LootTable _lootTable;
        private readonly IBalanceConfigProvider _balance;
        private readonly IHudStateWriter _hud;
        private readonly IInventoryStateWriter _inventoryHud;
        private readonly IEffectStateWriter _effectHud;
        private readonly GameFlow _flow;
        private readonly InventoryHudBinding _binding;

        private DestructibleService _destructibles;
        private HazardService _hazards;
        private MemoryItemWorld _floor;
        private InventoryService _items;
        private WorldDemoCombatSession _combat;

        private readonly List<WorldActor> _actors = new List<WorldActor>(1);
        private readonly Dictionary<ushort, double> _slow = new Dictionary<ushort, double>();
        private readonly float[] _pelletAngles = new float[16];

        private Guid _matchId;
        private uint _nextFloorId = 10000;
        private uint _requestSeq;
        private uint _tick;
        private uint _hitSequence;
        private uint _portalSequence;
        private double _now;
        private double _fraction;
        private double _invulnerable;
        private double _poisonUntil;
        private double _poisonDue;
        private double _boostUntil;
        private WorldPoint _playerPose = new WorldPoint(25f, 25f);
        private WorldPoint? _pendingPortalDestination;
        private bool _ready;
        private bool _wasActive, _deathDropped, _dirty = true;
        private int _lastFloorCount = -1;

        public MapLayout Layout => _map;
        public WorldObjectWire[] Objects { get; private set; } = Array.Empty<WorldObjectWire>();
        public InventorySnapshot Inventory => _items != null ? _items.GetSnapshot(0) : null;
        public uint EquipmentGeneration => _items != null ? _items.EquipmentGeneration(0) : 0;
        public bool IsReady => IsActive && _ready && Layout != null && _items != null;
        public bool IsActive => _match != null && _match.IsActive && (_flow == null || _flow.IsOffline);
        public float MovementMultiplier => _items != null && _items.IsUsing(0) ? 0 : (_slow.Count > 0 ? _hazardConfig.SwampSpeedMultiplier : 1f) * (_boostUntil > _now ? 1.3f : 1f);
        public WorldDemoCombatSession Combat => _combat;
        public DestructibleService Destructibles => _destructibles;
        public HazardService Hazards => _hazards;
        public MemoryItemWorld Floor => _floor;
        public InventoryService Items => _items;

        public event Action Changed;
        public event Action<ushort, uint> Teleported;
        public event Action<CombatEvent> CombatChanged;

        public OfflineWorldSession(
            OfflineMatchSession match,
            MapLayout map, DestructibleConfig destructibles, HazardConfig hazards,
            ItemCatalog catalog,
            LootTable lootTable,
            IBalanceConfigProvider balance,
            IHudStateWriter hud,
            IInventoryStateWriter inventoryHud,
            IEffectStateWriter effectHud,
            GameFlow flow,
            InventoryHudBinding binding = null)
        {
            _match = match;
            _catalog = catalog ?? ItemCatalog.CreateBaseline();
            _lootTable = lootTable ?? new LootTable(_catalog);
            _balance = balance;
            _hud = hud;
            _inventoryHud = inventoryHud;
            _effectHud = effectHud;
            _flow = flow;
            _binding = binding ?? (inventoryHud != null && hud != null ? new InventoryHudBinding(inventoryHud, hud, false) : null);

            _destructibleConfig = destructibles ?? new DestructibleConfig();
            _hazardConfig = hazards ?? new HazardConfig();
            _map = map;

            if (_match != null)
            {
                _match.RoundStarted += Reset;
                _match.DamageInterceptor = PreventDamage;
            }

            Reset();
        }

        public void Reset()
        {
            if (_match != null && _match.IsActive && _map == null)
                throw new InvalidOperationException("Offline authored world data is missing; sync runtime data.");
            _deathDropped = false; _dirty = true; _lastFloorCount = -1;
            _matchId = _match != null && _match.MatchId != Guid.Empty ? _match.MatchId : Guid.NewGuid();
            _nextFloorId = 10000;
            _requestSeq = 0;
            _tick = 0;
            _hitSequence = 0;
            _now = 0;
            _fraction = 0;
            _invulnerable = 0;
            _poisonUntil = 0;
            _poisonDue = 0;
            _boostUntil = 0;
            _slow.Clear();
            _pendingPortalDestination = null;

            if (_map != null)
            {
                _floor = new MemoryItemWorld(() => _nextFloorId >= ushort.MaxValue || _floor.Items.Count >= 2048 ? (ushort)0 : (ushort)++_nextFloorId);
                _items = new InventoryService(_matchId, _catalog, this, _floor);
                _items.Register(0);
                _items.Changed += _ => _dirty = true;

                _destructibles = new DestructibleService(_map, _destructibleConfig, this);
                _hazards = new HazardService(_map, _hazardConfig, this, this, this, _destructibles.IsBroken);
                _destructibles.Changed += _ => _dirty = true;
                _hazards.DoorChanged += (_, __) => _dirty = true;

                if (_combat != null)
                {
                    _combat.Confirmed -= OnCombatConfirmed;
                    if (_combat.Controller != null) _combat.Controller.Changed -= OnCombatControllerChanged;
                    _combat.Dispose();
                    _combat = null;
                }

                if (_balance != null && _hud != null)
                {
                    _combat = new WorldDemoCombatSession(_balance, _hud, _items);
                    _combat.Confirmed += OnCombatConfirmed;
                    if (_combat.Controller != null) _combat.Controller.Changed += OnCombatControllerChanged;
                }

                _binding?.Reset();
                _effectHud?.Reset();

                _playerPose = _map.Spawns != null && _map.Spawns.Count > 0 ? _map.Spawns[0] : new WorldPoint(25f, 25f);
                _items.SetActorState(0, _playerPose.X, _playerPose.Y, true);
                // Hero trong scene đứng sẵn ở tâm map: đưa về spawn đầu của map (cùng đường dịch chuyển như portal).
                _pendingPortalDestination = _playerPose;

                _ready = true;
                RebuildWireObjects();
                Changed?.Invoke();
            }
        }

        public void Tick(float deltaTime)
        {
            var active = IsActive;
            if (_wasActive != active)
            {
                _wasActive = active;
                if (!active) { _binding?.Reset(); _effectHud?.Reset(); }
                Changed?.Invoke();
            }
            if (!_ready || !active || !(deltaTime > 0) || float.IsInfinity(deltaTime)) return;
            if (_match.IsDead) { DropOnDeath(); return; }

            _combat?.Tick(deltaTime);

            _fraction += deltaTime;
            while (_fraction >= 1.0 / 30.0)
            {
                _fraction -= 1.0 / 30.0;
                Step();
            }

            if (_items != null)
            {
                _items.Tick(_now);
                var snap = _items.GetSnapshot(0);
                _binding?.Apply(snap, _now);
            }

            RebuildWireObjects();
        }

        private void Step()
        {
            _now += 1.0 / 30.0;
            _tick++;

            var isAlive = _match == null || !_match.IsDead;
            _items?.SetActorState(0, _playerPose.X, _playerPose.Y, isAlive);

            if (_hazards != null)
            {
                _actors.Clear();
                _actors.Add(new WorldActor(0, _playerPose.X, _playerPose.Y, isAlive));
                _hazards.Tick(_tick, _actors);
            }

            if (_poisonUntil > _now && _poisonDue <= _now)
            {
                _poisonDue = _now + 1.0;
                RequestDamage(0, 0, _tick, _hazardConfig != null ? _hazardConfig.PoisonDamage : 5, true);
            }
        }

        public void SetPlayerPose(float x, float y)
        {
            if (!IsActive || !WorldMath.Finite(x) || !WorldMath.Finite(y)) return;
            _playerPose = new WorldPoint(x, y);
            if (_items != null)
            {
                var isAlive = _match == null || !_match.IsDead;
                _items.SetActorState(0, x, y, isAlive);
            }
            _match?.SetPlayerPosition(x, y);
        }

        public bool TryConsumePortalTeleport(out WorldPoint destination)
        {
            if (_pendingPortalDestination.HasValue)
            {
                destination = _pendingPortalDestination.Value;
                _pendingPortalDestination = null;
                return true;
            }
            destination = default;
            return false;
        }

        public bool Pickup(ushort floorId)
        {
            if (!IsActive || _items == null || _match.IsDead) return false;
            if (_floor != null && _floor.TryGet(floorId, out var item))
            {
                var res = _items.Pickup(_matchId, 0, ++_requestSeq, floorId);
                if (res == ItemResult.Ok)
                {
                    _hud?.NotifyPickup(item.Stack.ItemId, item.Stack.Count);
                    RebuildWireObjects();
                    Changed?.Invoke();
                    return true;
                }
            }
            return false;
        }

        /// <summary>Mở/đóng cửa tương tác trong tầm người chơi (cùng luật HazardService như server).</summary>
        public bool ToggleDoor(ushort doorId)
        {
            if (!IsActive || _hazards == null || _match.IsDead) return false;
            var self = new WorldActor(0, _playerPose.X, _playerPose.Y, true);
            if (!_hazards.TryToggleDoor(doorId, self.Position, new[] { self })) return false;
            RebuildWireObjects();
            Changed?.Invoke();
            return true;
        }

        public bool AutoPickup(ushort floorId)
        {
            if (!IsActive || _items == null || _match.IsDead) return false;
            if (_floor != null && _floor.TryGet(floorId, out var item))
            {
                var res = _items.AutoPickup(0, floorId);
                if (res == ItemResult.Ok)
                {
                    _hud?.NotifyPickup(item.Stack.ItemId, item.Stack.Count);
                    RebuildWireObjects();
                    Changed?.Invoke();
                    return true;
                }
            }
            return false;
        }

        public bool SelectWeapon(byte slot)
        {
            if (!IsActive || _items == null || _match.IsDead) return false;
            var target = slot >= 4 ? -1 : (int)slot;
            var res = _items.Select(_matchId, 0, ++_requestSeq, target);
            if (res == ItemResult.Ok)
            {
                _combat?.Synchronize();
                return true;
            }
            return false;
        }

        public bool UseConsumable(byte slot)
        {
            if (!IsActive || _items == null || _match.IsDead) return false;
            return _items.BeginUse(_matchId, 0, ++_requestSeq, slot, _now) == ItemResult.Ok;
        }

        public bool DropWeapon(byte slot)
        {
            if (!IsActive || _items == null || _match.IsDead) return false;
            var res = _items.Drop(_matchId, 0, ++_requestSeq, slot);
            if (res == ItemResult.Ok)
            {
                RebuildWireObjects();
                Changed?.Invoke();
                return true;
            }
            return false;
        }

        public void CancelUse()
        {
            _items?.CancelForAction(0, ItemCancelAction.Move);
        }

        public bool TryAttack(float aimDegrees)
        {
            if (!IsActive || _combat == null || _match.IsDead) return false;
            _items?.CancelForAction(0, ItemCancelAction.Attack);
            return _combat.TryAttack(aimDegrees);
        }

        public bool TryReload()
        {
            if (!IsActive || _combat == null || _match.IsDead) return false;
            return _combat.TryReload();
        }

        private void OnCombatControllerChanged(CombatEvent e)
        {
            CombatChanged?.Invoke(e);
        }

        private void OnCombatConfirmed(CombatWeaponId weapon, float aimDegrees)
        {
            if (_combat == null || _map == null || _destructibles == null) return;
            var slot = _combat.Controller.Slot(weapon);
            var stats = slot.Definition.Stats;
            var origin = new WorldPoint(_playerPose.X, _playerPose.Y);
            var aim = new WorldPoint((float)Math.Cos(aimDegrees * Math.PI / 180), (float)Math.Sin(aimDegrees * Math.PI / 180));

            if (slot.IsRanged)
            {
                var count = CombatShotMath.PelletAngles(aimDegrees, slot.Pellets, slot.SpreadDegrees, _pelletAngles);
                for (var i = 0; i < count; i++)
                {
                    var direction = new WorldPoint((float)Math.Cos(_pelletAngles[i] * Math.PI / 180), (float)Math.Sin(_pelletAngles[i] * Math.PI / 180));
                    var hit = NearestHit(origin, direction, slot.Range, out _);
                    if (hit != null && hit.IsBreakable)
                    {
                        if (_destructibles.DamageConfirmed(hit.Id, (int)Math.Round(stats.Damage), 0, ++_hitSequence))
                        {
                            RebuildWireObjects();
                            Changed?.Invoke();
                        }
                    }
                }
                return;
            }

            foreach (var target in _map.Objects)
            {
                if (!target.IsBreakable || _destructibles.IsBroken(target.Id)) continue;
                var b = target.Bounds;
                var dx = Math.Clamp(origin.X, b.MinX, b.MaxX) - origin.X;
                var dy = Math.Clamp(origin.Y, b.MinY, b.MaxY) - origin.Y;
                var length = (float)Math.Sqrt(dx * dx + dy * dy);
                var angle = length <= .000001f ? 0 : Math.Acos(Math.Clamp((aim.X * dx + aim.Y * dy) / length, -1f, 1f)) * 180 / Math.PI;
                if (length > slot.Range || angle > slot.ArcDegrees * .5f) continue;
                var direction = length <= .000001f ? aim : new WorldPoint(dx / length, dy / length);
                var blocker = NearestHit(origin, direction, length + .001f, out _);
                if (blocker != null && blocker.Id != target.Id) continue;
                var damage = stats.Damage * (weapon == CombatWeaponId.Axe ? 1.5f : 1f);
                if (_destructibles.DamageConfirmed(target.Id, (int)Math.Round(damage), 0, ++_hitSequence))
                {
                    RebuildWireObjects();
                    Changed?.Invoke();
                }
            }
        }

        private WorldObjectDefinition NearestHit(WorldPoint origin, WorldPoint direction, float range, out float distance)
        {
            distance = range;
            WorldObjectDefinition hit = null;
            if (_map == null) return null;
            foreach (var item in _map.Objects)
            {
                if (!item.IsHardCover && !(item.IsDestructible && (_destructibles == null || !_destructibles.IsBroken(item.Id))) &&
                    !(item.Kind == WorldObjectKind.Door && _hazards != null && _hazards.IsDoorClosed(item.Id))) continue;
                var near = 0f; var far = range; var b = item.Bounds;
                if (!Clip(origin.X, direction.X, b.MinX, b.MaxX, ref near, ref far) ||
                    !Clip(origin.Y, direction.Y, b.MinY, b.MaxY, ref near, ref far) || near >= distance) continue;
                distance = near; hit = item;
            }
            return hit;
        }

        private static bool Clip(float origin, float direction, float min, float max, ref float near, ref float far)
        {
            if (Math.Abs(direction) < 0.000001f) return origin >= min && origin <= max;
            var a = (min - origin) / direction; var b = (max - origin) / direction;
            if (a > b) { var swap = a; a = b; b = swap; }
            near = Math.Max(near, a); far = Math.Min(far, b);
            return near <= far;
        }

        public bool CanSpawnLoot(WorldObjectKind source, ushort worldId, uint seed)
        {
            var kind = source == WorldObjectKind.Pot ? LootSource.Pot : source == WorldObjectKind.Chest ? LootSource.Chest : LootSource.Supply;
            var count = _lootTable.Roll(kind, seed).Length;
            return _floor != null && _floor.Items.Count + count <= 2048 && _nextFloorId + count <= ushort.MaxValue && _map.Objects.Count + _floor.Items.Count + count <= WorldWire.MaxObjects;
        }

        public void SpawnLoot(WorldObjectKind source, ushort worldId, WorldPoint position, uint seed)
        {
            if (_lootTable == null || _floor == null) return;
            var lootSource = source == WorldObjectKind.Pot ? LootSource.Pot : source == WorldObjectKind.Chest ? LootSource.Chest : LootSource.Supply;
            var loot = _lootTable.Roll(lootSource, seed);
            if (loot != null)
            {
                for (var i = 0; i < loot.Length; i++)
                {
                    var p = new WorldPoint(position.X + (i % 2) * 0.3f, position.Y + (i / 2) * 0.3f);
                    _floor.TrySpawn(loot[i], p.X, p.Y, out _);
                }
            }
            RebuildWireObjects();
            Changed?.Invoke();
        }

        public void RequestDamage(byte actor, ushort source, uint tick, int amount, bool bypassArmor)
        {
            if (!IsActive || actor != 0 || _match.IsDead || amount <= 0 || _now < _invulnerable) return;
            if (!bypassArmor && _items != null)
            {
                amount = _items.AbsorbWeaponDamage(actor, amount);
                if (amount <= 0) return;
            }
            _match.ApplyDamage((byte)Math.Min(255, amount));
            if (_match.IsDead) DropOnDeath();
        }

        private bool PreventDamage(byte damage)
        {
            if (!IsActive || _items == null) return false;
            if (_now < _invulnerable) return true;
            return damage >= _match.PlayerHp && _items.TryConsumeLifePassive(0, _match.CurrentTick == 0 ? 1u : _match.CurrentTick);
        }
        private void DropOnDeath()
        {
            if (_deathDropped || _items == null) return;
            _items.SetActorState(0, _playerPose.X, _playerPose.Y, false);
            _deathDropped = _items.DeathDrop(0, _match.CurrentTick == 0 ? 1u : _match.CurrentTick) == ItemResult.Ok;
            _dirty = true; RebuildWireObjects();
        }

        public void ApplyEffect(byte actor, ushort source, string effect, uint tick, float seconds, float magnitude)
        {
            if (actor != 0) return;
            if (effect == "Slow")
            {
                _slow[source] = _now + seconds;
                _effectHud?.Apply(EffectKind.Slow, 0f, 0f);
            }
            else if (effect == "Poison")
            {
                _poisonUntil = _now + seconds;
                _effectHud?.Apply(EffectKind.Poison, seconds, seconds);
            }
        }

        public void RemoveEffect(byte actor, ushort source, string effect, uint tick)
        {
            if (actor != 0) return;
            if (effect == "Slow")
            {
                _slow.Remove(source);
                if (_slow.Count == 0) _effectHud?.Remove(EffectKind.Slow);
            }
        }

        public bool TryMove(byte actor, WorldPoint destination, ushort source, uint tick)
        {
            if (!IsActive || actor != 0 || _match.IsDead || !_map.IsWalkable(destination, _hazardConfig.HeroRadius, _destructibles.IsBroken, _hazards.IsDoorClosed)) return false;
            _pendingPortalDestination = destination;
            _playerPose = destination;
            _portalSequence++;
            Teleported?.Invoke(source, _portalSequence);
            return true;
        }

        public void Heal(byte actor, float amount)
        {
            if (actor != 0 || _match == null) return;
            _match.Heal((byte)Math.Clamp((int)Math.Round(amount), 1, 255));
        }

        public void ApplyEffect(byte actor, ItemEffect effect, double seconds)
        {
            if (actor != 0) return;
            if (effect == ItemEffect.Speed)
            {
                _boostUntil = _now + seconds;
                _effectHud?.Apply(EffectKind.Adrenaline, (float)seconds, (float)seconds);
            }
        }

        public void Rescue(byte actor, float hp, double invulnerableSeconds)
        {
            if (actor != 0 || _match == null) return;
            _match.Rescue((byte)Math.Clamp((int)Math.Round(hp), 1, 255));
            _invulnerable = _now + invulnerableSeconds;
        }

        private void RebuildWireObjects()
        {
            if (!_dirty && _floor != null && _lastFloorCount == _floor.Items.Count) return;
            _dirty = false; _lastFloorCount = _floor != null ? _floor.Items.Count : 0;
            if (_map == null)
            {
                Objects = Array.Empty<WorldObjectWire>();
                return;
            }
            var list = new List<WorldObjectWire>(_map.Objects.Count + (_floor != null ? _floor.Items.Count : 0));
            foreach (var o in _map.Objects)
            {
                DestructibleState d = null;
                _destructibles?.TryGet(o.Id, out d);
                var center = o.Bounds.Center;
                list.Add(new WorldObjectWire
                {
                    Id = o.Id,
                    Kind = (byte)o.Kind,
                    X = center.X,
                    Y = center.Y,
                    Width = o.Bounds.MaxX - o.Bounds.MinX,
                    Height = o.Bounds.MaxY - o.Bounds.MinY,
                    Flags = (byte)((d != null && d.Broken ? 1 : 0) | (o.Kind == WorldObjectKind.Door && _hazards != null && _hazards.IsDoorClosed(o.Id) ? 2 : 0)),
                    PairId = o.PairId,
                    Hp = (ushort)(d != null ? d.Hp : 0)
                });
            }
            if (_floor != null)
            {
                foreach (var f in _floor.Items)
                {
                    list.Add(new WorldObjectWire
                    {
                        Id = f.Id,
                        Kind = 100,
                        X = f.X,
                        Y = f.Y,
                        Width = 0.35f,
                        Height = 0.35f,
                        ItemId = f.Stack.ItemId,
                        Count = (ushort)f.Stack.Count
                    });
                }
            }
            Objects = list.ToArray();
            Changed?.Invoke();
        }

        public void Dispose()
        {
            if (_match != null) { _match.RoundStarted -= Reset; if (_match.DamageInterceptor == PreventDamage) _match.DamageInterceptor = null; }
            if (_combat != null)
            {
                _combat.Confirmed -= OnCombatConfirmed;
                if (_combat.Controller != null) _combat.Controller.Changed -= OnCombatControllerChanged;
                _combat.Dispose();
                _combat = null;
            }
            _ready = false;
            _wasActive = false;
        }
    }
}
