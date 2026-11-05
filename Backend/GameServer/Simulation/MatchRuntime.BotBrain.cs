using GameServer.Diagnostics;
using System.Numerics;
using GameServer.Simulation.Bots;
using GameServer.Simulation.Combat;
using JungleBackend.Protocol.Gameplay;
using JungleMaze.World;
using JungleMaze.World.Inventory;

namespace GameServer.Simulation;

public sealed partial class MatchRuntime
{
    private sealed class BotBrain
    {
        public uint Id { get; }
        public string SessionId { get; }
        public BotState State { get; private set; } = BotState.Idle;
        public bool HasPendingPath => _search is { Finished: false };
        private StableRandom _random;
        private uint _nextThink;
        private ushort _inputSequence, _combatSequence;
        private uint _itemSequence;
        private byte _enemy = 255;
        private double _seenAt = -100, _reactAt, _repathAt, _progressAt, _ignoreUntil;
        private Vector2 _lastSeen, _goal, _progressPosition, _previousPosition;
        private Vector2? _ignoredGoal;
        private BotPathSearch? _search;
        private int _pathIndex, _failedPaths;
        private bool _hasGoal, _thinkInitialised, _hasPreviousPosition;
        // Cached combat intent: decided at ThinkHz, executed every tick so authoritative combat rules apply at the tick rate.
        private bool _attackWanted;
        private CombatWeaponId _attackWeapon;

        public BotBrain(uint id, string session, BotSettings settings) { Id = id; SessionId = session; Reset(id); }
        public void Reset(uint seed)
        {
            _random = new StableRandom(seed); State = BotState.Idle; _nextThink = 0;
            _inputSequence = _combatSequence = 0; _itemSequence = 0; _enemy = 255;
            _seenAt = -100; _reactAt = _repathAt = _progressAt = _ignoreUntil = 0;
            _hasGoal = false; _search = null; _failedPaths = 0; _ignoredGoal = null;
            _pathIndex = 0; _thinkInitialised = false; _hasPreviousPosition = false; _previousPosition = default;
            _attackWanted = false; _attackWeapon = CombatWeaponId.Hand;
        }

        public void Tick(MatchRuntime match, Entity actor, BotSettings settings)
        {
            if (!actor.IsAlive) { State = BotState.Dead; _attackWanted = false; return; }
            if (match.World is null || match.SelectedMap is null) return;
            var now = match.ElapsedSeconds;
            var position = new Vector2(actor.X, actor.Y);
            // A bot already standing in a hazard may traverse hazards to escape; normal pathing still rejects them.
            var allowHazards = Dangerous(match.World, position);
            // Large displacement invalidates stale path; drop it without touching position.
            if (_hasPreviousPosition && Vector2.DistanceSquared(position, _previousPosition) > 4f)
            {
                _search = null; _pathIndex = 0; _repathAt = now;
                _progressPosition = position; _progressAt = now;
            }
            _previousPosition = position; _hasPreviousPosition = true;

            var period = (uint)Math.Max(1, 30 / settings.ThinkHz);
            if (!_thinkInitialised)
            {
                _thinkInitialised = true;
                _nextThink = match.CurrentTick + Id % period;
            }
            if (match.CurrentTick >= _nextThink)
            {
                _nextThink = match.CurrentTick + period;
                using (new ServerMetrics.StageTiming(match.PerformanceMetrics, TickWorkStage.BotDecision)) Think(match, actor, position, settings);
            }

            var move = Vector2.Zero;
            if (_hasGoal && !match.World.Using(actor.NetId))
            {
                // Container attacks begin at .25 cells, so arrival must allow reaching that range instead of stopping at ~.387 cells.
                if (Vector2.DistanceSquared(position, _goal) < .04f) _hasGoal = false;
                else
                {
                    if (_search is null && now >= _repathAt)
                    {
                        _search = new BotPathSearch(match.SelectedMap.Navigation, position, _goal);
                        _pathIndex = 0; _repathAt = now + settings.RepathSeconds;
                    }
                    if (_search is { Finished: false })
                        using (new ServerMetrics.StageTiming(match.PerformanceMetrics, TickWorkStage.BotPath))
                        _search.Advance(match.NavigationBudget, settings.PathNodesPerTick, actor.Radius,
                            p => Walkable(match, p, actor.Radius, allowHazards),
                            (a,b) => SegmentWalkable(match,a,b,actor.Radius,allowHazards), p => Penalty(match,p));
                    if (_search is { Finished: true, ReachedGoal: false })
                    { FailPath(now, settings); }
                    else if (_search is { ReachedGoal: true })
                    {
                        var path = _search.Path;
                        while (_pathIndex < path.Count && Vector2.DistanceSquared(position,path[_pathIndex]) < .09f) _pathIndex++;
                        var next = _pathIndex < path.Count ? path[_pathIndex] : _goal;
                        if (SegmentWalkable(match,position,next,actor.Radius,allowHazards)) move = Direction(next-position);
                        else { _search = null; _repathAt = now; }
                    }
                    if (Vector2.DistanceSquared(position,_progressPosition) > .16f)
                    { _progressPosition = position; _progressAt = now; _failedPaths = 0; }
                    else if (now-_progressAt >= settings.StuckSeconds) { _progressAt = now; FailPath(now,settings); }
                }
            }
            // Keep normal input validation/normalisation and input timeout; never write position directly.
            var input = new PlayerInputPacket { Tick = match.CurrentTick, InputSeq = ++_inputSequence,
                MoveX = (sbyte)Math.Clamp((int)(move.X*127),-127,127), MoveY = (sbyte)Math.Clamp((int)(move.Y*127),-127,127), Aim = actor.Rotation };
            match.ProcessInput(SessionId,input);

            // Combat intent cached by Think is replayed every tick, so the server validates cooldown, ammo,
            // range and collision at the tick rate instead of once per decision.
            if (_attackWanted && actor.IsAlive && !match.World.Using(actor.NetId))
            {
                match.TryProcessAttack(SessionId,new AttackRequestPacket { RequestSeq=++_combatSequence,Weapon=_attackWeapon,Aim=actor.Rotation },out var attackResult);
                if (attackResult.Outcome == CombatOutcome.NoAmmo)
                    match.TryProcessReload(SessionId,new ReloadRequestPacket { RequestSeq=++_combatSequence,Weapon=_attackWeapon },out _);
            }
        }

        private void Think(MatchRuntime match, Entity actor, Vector2 position, BotSettings settings)
        {
            var world = match.World!; var now = match.ElapsedSeconds;
            // Every non-combat path leaves the cached intent cleared, so attacks stop as soon as the decision changes.
            _attackWanted = false;
            var enemy = FindEnemy(match,actor,position,settings);
            if (enemy != null)
            {
                if (_enemy != enemy.NetId || now-_seenAt > settings.MemorySeconds) _reactAt = now+settings.ReactionSeconds;
                _enemy = enemy.NetId; _seenAt = now; _lastSeen = new Vector2(enemy.X,enemy.Y);
            }
            var zone = match.Zone;
            var zoneState = zone?.GetStatePacket(match.CurrentTick) ?? default;
            var outside = zone != null && zone.IsOutside(position.X,position.Y);
            var unsafeSoon = zoneState.InWarning && Vector2.Distance(position,new Vector2(zoneState.TargetCenterX,zoneState.TargetCenterY)) > zoneState.TargetRadius-actor.Radius;
            // Leave the current hazard before planning the longer route back into the zone.
            if (Dangerous(world,position))
            {
                State = BotState.EscapeZone;
                var radius = actor.Radius; var margin = radius + .1f;
                var exitDistance = float.MaxValue; var exit = position;
                void Consider(Vector2 candidate)
                {
                    var distance = Vector2.DistanceSquared(position,candidate);
                    if (distance >= exitDistance) return;
                    if (!Walkable(match,candidate,radius) || !SegmentWalkable(match,position,candidate,radius,true)) return;
                    exitDistance = distance; exit = candidate;
                }
                foreach (var hazard in world.Layout.Objects)
                {
                    if (hazard.Kind != WorldObjectKind.Lava && hazard.Kind != WorldObjectKind.Poison) continue;
                    var bounds = hazard.Bounds;
                    if (!bounds.Contains(new WorldPoint(position.X,position.Y))) continue;
                    Consider(new Vector2(bounds.MinX-margin,position.Y));
                    Consider(new Vector2(bounds.MaxX+margin,position.Y));
                    Consider(new Vector2(position.X,bounds.MinY-margin));
                    Consider(new Vector2(position.X,bounds.MaxY+margin));
                }
                if (exitDistance < float.MaxValue) { Goal(exit,now,settings); return; }
                SeekSafe(match,actor,position,settings); return;
            }
            if (outside || unsafeSoon)
            {
                State = BotState.EscapeZone;
                var center = zoneState.InWarning ? new Vector2(zoneState.TargetCenterX,zoneState.TargetCenterY) : new Vector2(zoneState.CurrentCenterX,zoneState.CurrentCenterY);
                SeekSafe(match,actor,center,settings); return;
            }
            var healthFraction = actor.Hp/(float)match.Profile.PlayerHp;
            if (enemy != null && now >= _reactAt && healthFraction <= settings.RetreatHpFraction)
            {
                State = BotState.Retreat;
                SeekSafe(match,actor,position+Direction(position-_lastSeen)*5,settings); return;
            }
            if (enemy != null && now < _reactAt) { State=BotState.Idle; _hasGoal=false; return; }
            if (enemy != null && now >= _reactAt)
            {
                world.Inventory.CancelForAction(actor.NetId,ItemCancelAction.Attack);
                var weapon = SelectWeapon(match,actor,Vector2.Distance(position,_lastSeen));
                var rule = CombatWeaponRules.Value.TryGet(weapon,out var r) ? r : throw new InvalidDataException("Missing weapon.");
                var distance = Vector2.Distance(position,_lastSeen);
                var aim = MathF.Atan2(_lastSeen.Y-position.Y,_lastSeen.X-position.X)*180/MathF.PI;
                aim += (_random.Range(-1000,1001)/1000f)*settings.AimErrorDegrees;
                actor.Rotation = unchecked((ushort)(int)(aim/360*65536));
                if (distance > rule.Range*.85f)
                { State = BotState.Chase; Goal(_lastSeen,now,settings); }
                else
                {
                    State = BotState.Combat;
                    _hasGoal = false;
                    if (rule.IsRanged && distance < Math.Min(3,rule.Range*.4f)) Goal(position+Direction(position-_lastSeen)*2,now,settings);
                    // Cache the intent; the tick loop executes it at 30Hz against authoritative combat rules.
                    _attackWanted = true; _attackWeapon = weapon;
                }
                return;
            }
            if (world.Using(actor.NetId)) { State = BotState.Heal; _hasGoal = false; return; }
            if (healthFraction <= settings.HealHpFraction && (enemy == null && now-_seenAt > settings.MemorySeconds))
            {
                var inventory = world.Inventory.GetSnapshot(actor.NetId);
                for (byte slot=0;slot<inventory.Consumables.Length;slot++)
                {
                    var stack=inventory.Consumables[slot];
                    if (!stack.IsEmpty && match._worldCatalog!.TryGet(stack.ItemId,out var definition) && definition.Heal > 0)
                    {
                        if (Item(match,WorldActionKind.Use,0,slot)) { State=BotState.Heal;_hasGoal=false;return; }
                    }
                }
            }
            if (_enemy != 255 && now-_seenAt <= settings.MemorySeconds)
            { State=BotState.Search;Goal(_lastSeen,now,settings);return; }
            _enemy=255;
            var best = float.MaxValue; FloorItem? floorTarget=null;
            foreach(var floor in world.Floor.Items)
            {
                var point=new Vector2(floor.X,floor.Y);var distance=Vector2.DistanceSquared(position,point);
                if(distance>settings.DetectRadius*settings.DetectRadius||distance>=best||!Visible(match,position,point)||!Wants(match,actor,floor.Stack))continue;
                best=distance;floorTarget=floor;
            }
            if(floorTarget.HasValue)
            {
                var floor=floorTarget.Value;State=BotState.Loot;
                if(best <= world.Inventory.PickupRadius*world.Inventory.PickupRadius)
                {
                    if(SelectReplaceSlot(match,actor,floor.Stack))Item(match,WorldActionKind.Pickup,floor.Id);
                    _hasGoal=false;
                }
                else Goal(new Vector2(floor.X,floor.Y),now,settings);
                return;
            }
            WorldObjectDefinition? container=null;best=float.MaxValue;
            foreach(var item in world.Layout.Objects)
            {
                if(!item.IsDestructible||world.Destructibles.IsBroken(item.Id))continue;
                var c=item.Bounds.Center;var distance=Vector2.DistanceSquared(position,new Vector2(c.X,c.Y));
                if(distance>=best||distance>settings.DetectRadius*settings.DetectRadius)continue;
                // The target's own bounds block the ray, so test up to its near surface.
                var edge=Approach(item.Bounds,position,actor.Radius+.1f);
                if(!Visible(match,position,edge))continue;
                best=distance;container=item;
            }
            if(container!=null)
            {
                State=BotState.BreakContainer;
                var center=container.Bounds.Center;var target=new Vector2(center.X,center.Y);
                var approach=Approach(container.Bounds,position,actor.Radius+.1f);
                var weapon=SelectWeapon(match,actor,0,meleeOnly:true);
                var rule=CombatWeaponRules.Value.TryGet(weapon,out var r)?r:throw new InvalidDataException("Missing weapon.");
                if(Vector2.Distance(position,approach)>.25f)Goal(approach,now,settings);
                else
                {
                    _hasGoal=false;var aim=MathF.Atan2(target.Y-position.Y,target.X-position.X);
                    actor.Rotation=unchecked((ushort)(int)(aim/(2*MathF.PI)*65536));
                    // Cache the intent; the tick loop swings at the container 30Hz with normal cooldown/range checks.
                    _attackWanted=true;_attackWeapon=weapon;
                }
                return;
            }
            if(_hasGoal) { State=BotState.Roam;return; }
            State=BotState.Idle;
            var theta=_random.Range(0,360)*MathF.PI/180;var destination=position+new Vector2(MathF.Cos(theta),MathF.Sin(theta))*_random.Range(3,9);
            SeekSafe(match,actor,destination,settings);
            if(_hasGoal)State=BotState.Roam;
        }

        private Entity? FindEnemy(MatchRuntime match,Entity actor,Vector2 position,BotSettings settings)
        {
            Entity? selected=null;var best=settings.DetectRadius*settings.DetectRadius;
            foreach(var candidate in match._entities.Values)
            {
                if(candidate.NetId==actor.NetId||!candidate.IsAlive||match.World!.InvisibleToBots(candidate.NetId))continue;
                var target=new Vector2(candidate.X,candidate.Y);var distance=Vector2.DistanceSquared(position,target);
                if(distance>best||!Visible(match,position,target))continue;
                if(distance>settings.BushRevealRadius*settings.BushRevealRadius && match.World.Layout.Objects.Any(o=>o.Kind==WorldObjectKind.Bush&&!match.World.Destructibles.IsBroken(o.Id)&&o.Bounds.Contains(new WorldPoint(target.X,target.Y))))continue;
                best=distance;selected=candidate;
            }
            return selected;
        }
        private CombatWeaponId SelectWeapon(MatchRuntime match,Entity actor,float distance,bool meleeOnly=false)
        {
            var inventory=match.World!.Inventory.GetSnapshot(actor.NetId);var selected=-1;var weapon=CombatWeaponId.Hand;
            var score=0d;
            for(byte slot=0;slot<inventory.Weapons.Length;slot++)
            {
                var stack=inventory.Weapons[slot];if(stack.IsEmpty)continue;
                for(var id=1;id<10;id++)
                {
                    var candidate=(CombatWeaponId)id;
                    if(stack.ItemId!=MatchWorldRuntime.WeaponItemId(candidate)||!CombatWeaponRules.Value.TryGet(candidate,out var rule))continue;
                    if(rule.IsRanged&&(meleeOnly||(stack.MagazineKnown&&stack.Magazine==0&&stack.Ammo==0)))continue;
                    var value=rule.Damage*rule.Pellets/rule.CooldownSeconds*(rule.IsRanged&&distance>2?2:1);
                    if(value>score){score=value;selected=slot;weapon=candidate;}
                }
            }
            if(inventory.ActiveWeapon!=selected)Item(match,WorldActionKind.Select,0,(byte)(selected<0?255:selected));
            return weapon;
        }
        private static double WeaponScore(WeaponRule rule) => rule.Damage*(double)rule.Pellets/rule.CooldownSeconds*Math.Max(1f,rule.Range);
        // Reuses the existing ItemId mapping; unknown ids yield no rule here.
        private static WeaponRule? RuleFor(string itemId)
        {
            for(var id=1;id<10;id++)
            {
                var candidate=(CombatWeaponId)id;
                if(itemId==MatchWorldRuntime.WeaponItemId(candidate)&&CombatWeaponRules.Value.TryGet(candidate,out var rule))return rule;
            }
            return null;
        }
        // A ranged weapon known to have no loaded rounds and no reserve cannot be fired.
        private static bool DryRanged(WeaponRule rule,ItemStack stack) =>
            rule.IsRanged&&stack.MagazineKnown&&stack.Magazine==0&&stack.Ammo==0;
        // Valuates a carried stack; unknown ids and dry ranged weapons are worth nothing.
        private static double CarriedWeaponScore(ItemStack stack)
        {
            if(stack.IsEmpty)return 0;
            var rule=RuleFor(stack.ItemId);
            return rule is null||DryRanged(rule,stack)?0:WeaponScore(rule);
        }
        // Floor guns keep picked-up ammo state, so a dry ranged candidate is unusable and must not displace a working weapon.
        private static double FloorWeaponScore(MatchRuntime match,ItemStack stack) =>
            match._worldCatalog!.TryGet(stack.ItemId,out var item)&&item.Enabled&&item.Kind==ItemKind.Weapon&&RuleFor(stack.ItemId) is { } rule&&!DryRanged(rule,stack)
                ?WeaponScore(rule):0;
        // Usable weapon that beats the weakest carried weapon; unusable weapons never qualify.
        private static bool BetterWeapon(MatchRuntime match,InventorySnapshot inv,ItemStack stack)
        {
            var value=FloorWeaponScore(match,stack);if(value<=0)return false;
            var weakest=double.MaxValue;
            foreach(var weapon in inv.Weapons)weakest=Math.Min(weakest,CarriedWeaponScore(weapon));
            return value>weakest;
        }
        // Swapping is handed to InventoryService, which drops the replaced weapon: no direct state writes here.
        private static int WeakestWeaponSlot(InventorySnapshot inv)
        {
            var slot=-1;var weakest=double.MaxValue;
            for(var i=0;i<inv.Weapons.Length;i++)
            {
                var value=CarriedWeaponScore(inv.Weapons[i]);
                if(value<weakest){weakest=value;slot=i;}
            }
            return slot;
        }
        // Pre-select the weakest weapon because InventoryService replaces the active slot on pickup; the Select
        // command only toggles/replaces the active slot, so a rejected Select must block the following pickup.
        // Returns true when a following pickup is safe: no swap needed, weakest active, or the Select succeeded.
        private bool SelectReplaceSlot(MatchRuntime match,Entity actor,ItemStack stack)
        {
            if(!match._worldCatalog!.TryGet(stack.ItemId,out var definition)||definition.Kind!=ItemKind.Weapon)return true;
            var inv=match.World!.Inventory.GetSnapshot(actor.NetId);
            if(inv.Weapons.Any(s=>s.IsEmpty))return true;
            if(!BetterWeapon(match,inv,stack))return false;
            var slot=WeakestWeaponSlot(inv);
            if(slot<0)return false;
            if(slot==inv.ActiveWeapon)return true;
            return Item(match,WorldActionKind.Select,0,(byte)slot);
        }
        private bool Wants(MatchRuntime match,Entity actor,ItemStack stack)
        {
            if(!match._worldCatalog!.TryGet(stack.ItemId,out var rule)||!rule.Enabled)return false;
            var inv=match.World!.Inventory.GetSnapshot(actor.NetId);
            return rule.Kind switch
            {
                // Empty slot or a strictly better weapon; SelectReplaceSlot below preselects the replaced slot.
                ItemKind.Weapon=>inv.Weapons.Any(s=>s.IsEmpty)||BetterWeapon(match,inv,stack),
                ItemKind.Ammo=>inv.Weapons.Any(s=>!s.IsEmpty&&match._worldCatalog.TryGet(s.ItemId,out var w)&&w.MagazineSize>0&&s.Ammo<w.ReserveCapacity),
                ItemKind.Helmet=>inv.Helmet.IsEmpty||rule.Reduction>match._worldCatalog.Get(inv.Helmet.ItemId).Reduction,
                ItemKind.Armor=>inv.Armor.IsEmpty||rule.Reduction>match._worldCatalog.Get(inv.Armor.ItemId).Reduction,
                ItemKind.Consumable=>inv.Consumables.Any(s=>s.IsEmpty||(s.ItemId==stack.ItemId&&s.Count<rule.MaxStack)),
                _=>false
            };
        }
        private bool Item(MatchRuntime match,WorldActionKind action,ushort target=0,byte slot=0) =>
            match.ProcessWorldRequest(SessionId,new WorldActionRequestPacket{MatchId=match.MatchId,RequestId=++_itemSequence,Action=action,TargetId=target,Slot=slot});
        private void Goal(Vector2 goal,double now,BotSettings settings)
        {
            if(_ignoredGoal.HasValue&&now<_ignoreUntil&&Vector2.DistanceSquared(goal,_ignoredGoal.Value)<1)return;
            if(!_hasGoal||Vector2.DistanceSquared(goal,_goal)>1){_search=null;_pathIndex=0;}
            _goal=goal;_hasGoal=true;
        }
        private void FailPath(double now,BotSettings settings)
        {
            _search=null;_repathAt=now+settings.RepathSeconds;_failedPaths++;
            if(_failedPaths>=settings.FailedPathLimit){_ignoredGoal=_goal;_ignoreUntil=now+settings.BlacklistSeconds;_hasGoal=false;_failedPaths=0;State=BotState.Idle;}
        }
        private void SeekSafe(MatchRuntime match,Entity actor,Vector2 desired,BotSettings settings)
        {
            var size=match.World!.Layout.Size;
            desired=Vector2.Clamp(desired,new Vector2(1),new Vector2(size-1));
            for(var i=0;i<40;i++)
            {
                var p=i==0?desired:desired+new Vector2(_random.Range(-80,81)/10f,_random.Range(-80,81)/10f);
                if(!Walkable(match,p,actor.Radius)||match.Zone?.IsOutside(p.X,p.Y)==true)continue;
                Goal(p,match.ElapsedSeconds,settings);return;
            }
            _hasGoal=false;
        }
        private static bool Walkable(MatchRuntime match,Vector2 p,float radius,bool allowHazards=false) =>
            match.World!.Layout.IsWalkable(new WorldPoint(p.X,p.Y),radius,match.World.Destructibles.IsBroken,match.World.Hazards.IsDoorClosed)&&(allowHazards||!Dangerous(match.World,p));
        private static bool Dangerous(MatchWorldRuntime world,Vector2 p) => world.Layout.Objects.Any(o=>
            (o.Kind==WorldObjectKind.Lava||o.Kind==WorldObjectKind.Poison)&&o.Bounds.Contains(new WorldPoint(p.X,p.Y)));
        private static float Penalty(MatchRuntime match,Vector2 p) => (match.Zone?.IsOutside(p.X,p.Y)==true?10:1)*
            (match.World!.Layout.Objects.Any(o=>o.Kind==WorldObjectKind.Swamp&&o.Bounds.Contains(new WorldPoint(p.X,p.Y)))?3:1)*
            (Dangerous(match.World,p)?20:1);
        private static bool SegmentWalkable(MatchRuntime match,Vector2 a,Vector2 b,float radius,bool allowHazards=false)
        {
            var steps=Math.Max(1,(int)MathF.Ceiling(Vector2.Distance(a,b)/.2f));
            for(var i=1;i<=steps;i++)if(!Walkable(match,Vector2.Lerp(a,b,i/(float)steps),radius,allowHazards))return false;
            return true;
        }
        private static bool Visible(MatchRuntime match,Vector2 a,Vector2 b)
        {
            foreach(var obstacle in match._worldMap?.Obstacles??match._map.Obstacles)
                if(CombatGeometry.SegmentIntersectsAabb(a,b,obstacle,out _))return false;
            return true;
        }
        private static Vector2 Direction(Vector2 v) => v.LengthSquared()>.0001f?Vector2.Normalize(v):Vector2.Zero;
        private static Vector2 Approach(WorldBounds b,Vector2 from,float margin)
        {
            var center=new Vector2(b.Center.X,b.Center.Y);var delta=from-center;
            if(MathF.Abs(delta.X)/(b.MaxX-b.MinX)>=MathF.Abs(delta.Y)/(b.MaxY-b.MinY))
                return new Vector2(delta.X>=0?b.MaxX+margin:b.MinX-margin,Math.Clamp(from.Y,b.MinY,b.MaxY));
            return new Vector2(Math.Clamp(from.X,b.MinX,b.MaxX),delta.Y>=0?b.MaxY+margin:b.MinY-margin);
        }
    }
}
