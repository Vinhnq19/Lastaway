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
        private StableRandom _random;
        private uint _nextThink;
        private ushort _inputSequence, _combatSequence;
        private uint _itemSequence;
        private byte _enemy = 255;
        private double _seenAt = -100, _reactAt, _repathAt, _progressAt, _ignoreUntil;
        private Vector2 _lastSeen, _goal, _progressPosition;
        private Vector2? _ignoredGoal;
        private BotPathSearch? _search;
        private int _pathIndex, _failedPaths;
        private ushort _floorId, _containerId;
        private bool _hasGoal;

        public BotBrain(uint id, string session, BotSettings settings) { Id = id; SessionId = session; Reset(id); }
        public void Reset(uint seed)
        {
            _random = new StableRandom(seed); State = BotState.Idle; _nextThink = 0;
            _inputSequence = _combatSequence = 0; _itemSequence = 0; _enemy = 255;
            _seenAt = -100; _reactAt = _repathAt = _progressAt = _ignoreUntil = 0;
            _hasGoal = false; _search = null; _floorId = _containerId = 0; _failedPaths = 0; _ignoredGoal = null;
        }

        public void Tick(MatchRuntime match, Entity actor, BotSettings settings)
        {
            if (!actor.IsAlive) { State = BotState.Dead; return; }
            if (match.World is null || match.SelectedMap is null) return;
            var now = match.ElapsedSeconds;
            var position = new Vector2(actor.X, actor.Y);
            if (match.CurrentTick >= _nextThink)
            {
                _nextThink = match.CurrentTick + (uint)Math.Max(1, 30 / settings.ThinkHz);
                if (_nextThink == (uint)(30 / settings.ThinkHz) + 1) _nextThink += Id % (uint)(30 / settings.ThinkHz);
                Think(match, actor, position, settings);
            }

            var move = Vector2.Zero;
            if (_hasGoal && !match.World.Using(actor.NetId))
            {
                if (Vector2.DistanceSquared(position, _goal) < .15f) _hasGoal = false;
                else
                {
                    if (_search is null && now >= _repathAt)
                    {
                        _search = new BotPathSearch(match.SelectedMap.Navigation, position, _goal);
                        _pathIndex = 0; _repathAt = now + settings.RepathSeconds;
                    }
                    if (_search is { Finished: false })
                        _search.Advance(match.NavigationBudget, settings.PathNodesPerTick, actor.Radius,
                            p => Walkable(match, p, actor.Radius),
                            (a,b) => SegmentWalkable(match,a,b,actor.Radius), p => Penalty(match,p));
                    if (_search is { Finished: true, ReachedGoal: false })
                    { FailPath(now, settings); }
                    else if (_search is { ReachedGoal: true })
                    {
                        var path = _search.Path;
                        while (_pathIndex < path.Count && Vector2.DistanceSquared(position,path[_pathIndex]) < .09f) _pathIndex++;
                        var next = _pathIndex < path.Count ? path[_pathIndex] : _goal;
                        if (SegmentWalkable(match,position,next,actor.Radius)) move = Direction(next-position);
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
        }

        private void Think(MatchRuntime match, Entity actor, Vector2 position, BotSettings settings)
        {
            var world = match.World!; var now = match.ElapsedSeconds;
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
            if (outside || unsafeSoon || Dangerous(world,position))
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
                    match.TryProcessAttack(SessionId,new AttackRequestPacket { RequestSeq=++_combatSequence,Weapon=weapon,Aim=actor.Rotation },out var result);
                    if (result.Outcome == CombatOutcome.NoAmmo)
                        match.TryProcessReload(SessionId,new ReloadRequestPacket { RequestSeq=++_combatSequence,Weapon=weapon },out _);
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
                var floor=floorTarget.Value;State=BotState.Loot;_floorId=floor.Id;
                if(best <= world.Inventory.PickupRadius*world.Inventory.PickupRadius)
                { Item(match,WorldActionKind.Pickup,floor.Id);_hasGoal=false; }
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
                State=BotState.BreakContainer;_containerId=container.Id;
                var center=container.Bounds.Center;var target=new Vector2(center.X,center.Y);
                var approach=Approach(container.Bounds,position,actor.Radius+.1f);
                var weapon=SelectWeapon(match,actor,0,meleeOnly:true);
                var rule=CombatWeaponRules.Value.TryGet(weapon,out var r)?r:throw new InvalidDataException("Missing weapon.");
                if(Vector2.Distance(position,approach)>.25f)Goal(approach,now,settings);
                else
                {
                    _hasGoal=false;var aim=MathF.Atan2(target.Y-position.Y,target.X-position.X);
                    actor.Rotation=unchecked((ushort)(int)(aim/(2*MathF.PI)*65536));
                    match.TryProcessAttack(SessionId,new AttackRequestPacket{RequestSeq=++_combatSequence,Weapon=weapon,Aim=actor.Rotation},out _);
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
        private bool Wants(MatchRuntime match,Entity actor,ItemStack stack)
        {
            if(!match._worldCatalog!.TryGet(stack.ItemId,out var rule))return false;
            var inv=match.World!.Inventory.GetSnapshot(actor.NetId);
            return rule.Kind switch
            {
                ItemKind.Weapon=>inv.Weapons.Any(s=>s.IsEmpty),
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
            _search=null;_failedPaths++;
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
        private static bool Walkable(MatchRuntime match,Vector2 p,float radius) =>
            match.World!.Layout.IsWalkable(new WorldPoint(p.X,p.Y),radius,match.World.Destructibles.IsBroken,match.World.Hazards.IsDoorClosed)&&!Dangerous(match.World,p);
        private static bool Dangerous(MatchWorldRuntime world,Vector2 p) => world.Layout.Objects.Any(o=>
            (o.Kind==WorldObjectKind.Lava||o.Kind==WorldObjectKind.Poison)&&o.Bounds.Contains(new WorldPoint(p.X,p.Y)));
        private static float Penalty(MatchRuntime match,Vector2 p) => (match.Zone?.IsOutside(p.X,p.Y)==true?10:1)*
            (match.World!.Layout.Objects.Any(o=>o.Kind==WorldObjectKind.Swamp&&o.Bounds.Contains(new WorldPoint(p.X,p.Y)))?3:1);
        private static bool SegmentWalkable(MatchRuntime match,Vector2 a,Vector2 b,float radius)
        {
            var steps=Math.Max(1,(int)MathF.Ceiling(Vector2.Distance(a,b)/.2f));
            for(var i=1;i<=steps;i++)if(!Walkable(match,Vector2.Lerp(a,b,i/(float)steps),radius))return false;
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
