using JungleMaze.World;
using JungleMaze.World.Airdrops;
using JungleMaze.World.Inventory;
using JungleMaze.World.Loot;
using JungleBackend.Protocol.Gameplay;
namespace GameServer.Simulation;

/// <summary>Per-match world adapter. Inventory owns items; this runtime owns all world instance IDs.
/// Existing player NetIds are never reused for world objects. No Unity types, database or HTTP calls.</summary>
public sealed partial class MatchWorldRuntime : ITransactionalDestructibleLootSink,IEnvironmentDamageSink,IEnvironmentEffectSink,IEnvironmentMovementSink,IItemGameplayPort
{
    private readonly IEntityRegistry _entities;
    private readonly HealthService _health;
    private readonly Dictionary<(byte Actor,ushort Source,string Effect),(double End,float Magnitude)> _effects=new();
    private readonly Dictionary<byte,double> _invulnerable=new();
    private readonly Dictionary<byte,double> _poisonDue=new();
    private readonly HashSet<byte> _registered=new();
    private readonly LootTable _loot;
    private LootSurface _lootSurface;
    private bool _usingSupplyReserve;
    private readonly float _airdropTimeScale;
    public AirdropController? Airdrops { get; }
    private int FloorReserve => Math.Max(0, (Airdrops?.ReservedLootRemaining ?? 0) - (_usingSupplyReserve ? 8 : 0));
    /// <summary>Floor capacity shared with <see cref="FloorLootPlanner.MaxFloorItems"/>.</summary>
    private const int FloorCapacity=FloorLootPlanner.MaxFloorItems;
    private ushort _nextId;
    private uint _tick;
    private readonly Dictionary<byte,(ushort Source,uint Sequence,uint Tick)> _portals=new();
    private uint _startTick;
    private bool _started;
    private readonly float _heroRadius;
    public ItemResult LastResult { get; private set; }
    public bool EnvironmentDamageInProgress { get; private set; }
    private double _now;
    public Guid MatchId {get;}
    public MapLayout Layout {get;}
    public WorldObjectDefinition[] NavigationHazards {get;}
    public InventoryService Inventory {get;}
    public MemoryItemWorld Floor {get;}
    public DestructibleService Destructibles {get;}
    public HazardService Hazards {get;}
    public uint Revision {get;private set;}=1;
    /// <summary>Per-match loot seed derived from <see cref="MatchId"/>: floor items and container drops differ every match.</summary>
    public uint LootSeed {get;}
    /// <summary>Floor loot created when the world started (for checks/telemetry).</summary>
    public FloorLootPlan InitialLoot {get;}
    public MatchWorldRuntime(Guid matchId,WorldSettings settings,IEntityRegistry entities,HealthService health,ItemCatalog? catalog=null,LootProfile? lootProfile=null, Func<float,AirdropZone>? forecast=null,float airdropTimeScale=1)
    {
        MatchId=matchId;_entities=entities;_health=health;_heroRadius=settings.Map.HeroRadius;
        Layout=settings.Layout ?? (settings.LayoutMode=="Prototype" ? MapLayoutGenerator.Generate(settings.Map,settings.Seed) : throw new InvalidOperationException("Authored map must be loaded before starting world services."));
        var authoredLayout = Layout;
        Layout = AirdropController.CopyLayout(authoredLayout);
        NavigationHazards=Layout.Objects.Where(o=>o.IsHazard).ToArray();
        _airdropTimeScale = airdropTimeScale;
        _nextId=(ushort)(Layout.Objects.Count+1);
        Floor=new MemoryItemWorld(AllocateId);
        catalog??=ItemCatalog.CreateBaseline();_loot=lootProfile==null?new LootTable(catalog):new LootTable(catalog,lootProfile);
        Inventory=new InventoryService(matchId,catalog,this,Floor);
        Inventory.Changed+=_=>Revision++;
        Destructibles=new DestructibleService(Layout,settings.Destructibles,this);
        Destructibles.Changed+=_=>Revision++;
        Hazards=new HazardService(Layout,settings.Hazards,this,this,this,Destructibles.IsBroken);
        Hazards.DoorChanged+=(_,_)=>Revision++;
        _health.DamageInterceptor=TryPreventDamage;
        LootSeed=LootSeeds.FromGuid(matchId);
        var floorProfile=_loot.Floor;
        _lootSurface=new LootSurface(Layout,floorProfile.DropClearance,Destructibles.IsBroken);
        // Authored maps use r = Size/2 for loot rings; the prototype generator has its own start-zone radius.
        var spots=settings.FloorSpots is {} cached&&ReferenceEquals(cached.Layout,authoredLayout)?cached:FloorLootPlanner.PlanSpots(Layout,_loot,settings.Layout==null?settings.Map.InitialZoneRadius:0,_heroRadius);
        if (settings.Airdrops?.Enabled == true)
        {
            _loot.ValidateSupply();
            if(Layout.Objects.Count+settings.Airdrops.ReservedEggs+settings.Airdrops.ReservedLootStacks>WorldWire.MaxObjects)
                throw new InvalidOperationException("Map has no capacity reserved for scheduled eggs and their loot.");
            if (forecast == null || !float.IsFinite(airdropTimeScale) || airdropTimeScale <= 0)
                throw new ArgumentException("Airdrops require a valid forecast and clock scale.");
            Airdrops = new AirdropController(settings.Airdrops, LootSeed, Layout, Destructibles,
                spots.ReachablePositions, forecast, AllocateId,
                () => { _lootSurface = new LootSurface(Layout, floorProfile.DropClearance, Destructibles.IsBroken); Revision++; },
                (point, size) => _entities.All.Any(e => e.IsAlive && new WorldBounds(point.X,point.Y,size,size)
                    .IntersectsCircle(new WorldPoint(e.X,e.Y),e.Radius)));
        }
        InitialLoot=FloorLootPlanner.Roll(spots,_loot,LootSeed);
        foreach(var placed in InitialLoot.Placements)if(!Floor.TrySpawn(placed.Stack,placed.Position.X,placed.Position.Y,out _))break;
        Revision++;
    }
    private ushort AllocateId(){if(_nextId==0||Floor.Items.Count>=Math.Min(FloorCapacity-FloorReserve,WorldWire.MaxObjects-Layout.Objects.Count-FloorReserve-(Airdrops?.ReservedEggsRemaining??0)))return 0;return _nextId++;}
    public void SyncActors()
    {
        foreach(var e in _entities.All)
        {
            if(_registered.Add(e.NetId))Inventory.Register(e.NetId);
            var controls=ItemControlFlags.None;
            foreach(var effect in _effects)if(effect.Key.Actor==e.NetId&&effect.Value.End>_now&&effect.Key.Effect=="Stun")controls|=ItemControlFlags.Stun;
            Inventory.SetActorState(e.NetId,e.X,e.Y,e.IsAlive,controls);
        }
    }
    public float SpeedMultiplier(byte actor)
    {
        var value=1f;
        // Overlapping swamp sources do not multiply the same Slow effect; leaving one keeps the other.
        var slow=1f;foreach(var e in _effects)if(e.Key.Actor==actor&&e.Value.End>_now){if(e.Key.Effect=="Slow")slow=Math.Min(slow,e.Value.Magnitude);else if(e.Key.Effect=="Speed")value*=e.Value.Magnitude;else if(e.Key.Effect=="Stun"||e.Key.Effect=="Root")return 0;}
        return value*slow;
    }
    internal bool InvisibleToBots(byte actor) => _effects.Any(e => e.Key.Actor == actor && e.Key.Effect == "Invisible" && e.Value.End > _now);
    public bool Using(byte actor){return _registered.Contains(actor)&&Inventory.IsUsing(actor);}
    public void Tick(uint tick,double now)
    {
        _tick=tick;_now=now;SyncActors();
        var actors=_entities.All.Select(e=>new WorldActor(e.NetId,e.X,e.Y,e.IsAlive)).ToArray();
        if (!_started) { _started = true; _startTick = tick; }
        Hazards.Tick(unchecked(tick - _startTick),actors);
        var expired=_effects.Where(e=>e.Value.End<=now).Select(e=>e.Key).ToArray();foreach(var k in expired)_effects.Remove(k);
        foreach(var e in _entities.All)
        {
            if(!e.IsAlive){Inventory.DeathDrop(e.NetId,tick);continue;}
            var poison=_effects.Where(p=>p.Key.Actor==e.NetId&&p.Key.Effect=="Poison"&&p.Value.End>now).Select(p=>p.Value.Magnitude).DefaultIfEmpty(0).Max();
            if(poison>0&&(!_poisonDue.TryGetValue(e.NetId,out var due)||now>=due)){_poisonDue[e.NetId]=now+1;ApplyEnvironmentalDamage(e.NetId,(int)poison);}
            else if(poison==0)_poisonDue.Remove(e.NetId);
            foreach(var floor in Floor.Items.ToArray())
                if((floor.X-e.X)*(floor.X-e.X)+(floor.Y-e.Y)*(floor.Y-e.Y)<=Inventory.PickupRadius*Inventory.PickupRadius)
                    Inventory.AutoPickup(e.NetId,floor.Id);
        }
        Inventory.Tick(now);
        Airdrops?.Tick((float)(now / _airdropTimeScale));
    }
    public ItemResult Request(byte actor,in WorldActionRequestPacket request)
    {
        SyncActors();ItemResult result;
        switch(request.Action)
        {
            case WorldActionKind.Pickup:result=Inventory.Pickup(MatchId,actor,request.RequestId,request.TargetId);break;
            case WorldActionKind.Drop:result=Inventory.Drop(MatchId,actor,request.RequestId,request.Slot);break;
            case WorldActionKind.Select:result=Inventory.Select(MatchId,actor,request.RequestId,request.Slot==255?-1:request.Slot);break;
            case WorldActionKind.Use:result=Inventory.BeginUse(MatchId,actor,request.RequestId,request.Slot,_now);break;
            case WorldActionKind.CancelUse:Inventory.CancelForAction(actor,ItemCancelAction.Move);result=ItemResult.Ok;break;
            case WorldActionKind.Interact:
                // Mở/đóng cửa tương tác trong tầm tay; server kiểm khoảng cách và người đứng trong khung cửa.
                if(!_entities.TryGet(actor,out var self)||self==null||!self.IsAlive){result=ItemResult.Invalid;break;}
                var actors=_entities.All.Select(e=>new WorldActor(e.NetId,e.X,e.Y,e.IsAlive)).ToArray();
                result=Hazards.TryToggleDoor(request.TargetId,new WorldPoint(self.X,self.Y),actors)?ItemResult.Ok:ItemResult.Invalid;break;
            default:return ItemResult.Invalid;
        }
        LastResult=result;
        if(result==ItemResult.Ok)Revision++;
        return result;
    }
    public void SpawnLoot(WorldObjectKind source,ushort worldId,WorldPoint position,uint seed)
    {
        // Drops fan out 0.9-1.4 cells around the container (broken, so its center is free) and guns bring an AMMO box.
        var placements=LootDrops.ForContainer(_loot,LootDrops.LootKind(source,Airdrops?.Owns(worldId)==true),position,LootSeeds.Mix(seed,LootSeed),_lootSurface);
        // Preflight/rollback makes a full floor registry an explicit rejection, never partial loot.
        if(Floor.Items.Count+placements.Count>FloorCapacity||_nextId+placements.Count>ushort.MaxValue)
            return;
        _usingSupplyReserve = source == WorldObjectKind.Supply && Airdrops?.Owns(worldId) == true;
        try
        {
        var spawned=new List<ushort>();
        foreach(var placed in placements)
        {
            if(!Floor.TrySpawn(placed.Stack,placed.Position.X,placed.Position.Y,out var id)){foreach(var old in spawned)Floor.Remove(old);return;}
            spawned.Add(id);
        }
        }
        finally { _usingSupplyReserve = false; }
        Revision++;
    }
    public bool CanSpawnLoot(WorldObjectKind source,ushort worldId,uint seed)
    {
        // Counts ammo companions too, so the preflight matches what SpawnLoot will create.
        var count=LootDrops.CountForContainer(_loot,LootDrops.LootKind(source,Airdrops?.Owns(worldId)==true),LootSeeds.Mix(seed,LootSeed));
        var reserve = Math.Max(0, (Airdrops?.ReservedLootRemaining ?? 0) -
            (source == WorldObjectKind.Supply && Airdrops?.Owns(worldId) == true ? 8 : 0));
        return Floor.Items.Count+count<=FloorCapacity-reserve&&_nextId+count<=ushort.MaxValue&&Layout.Objects.Count+Floor.Items.Count+count<=WorldWire.MaxObjects-reserve-(Airdrops?.ReservedEggsRemaining??0);
    }
    public void RequestDamage(byte actor,ushort source,uint tick,int amount,bool bypassArmor){ApplyEnvironmentalDamage(actor,amount);}
    private void ApplyEnvironmentalDamage(byte actor,int amount)
    {
        EnvironmentDamageInProgress = true;
        try { _health.ApplyDamage(actor,(byte)Math.Clamp(amount,0,255),255); }
        finally { EnvironmentDamageInProgress = false; }
    }
    public void ApplyEffect(byte actor,ushort source,string effect,uint tick,float seconds,float magnitude){_effects[(actor,source,effect)]=(float.IsPositiveInfinity(seconds)?double.PositiveInfinity:_now+seconds,magnitude);}
    public void RemoveEffect(byte actor,ushort source,string effect,uint tick){_effects.Remove((actor,source,effect));}
    public bool TryMove(byte actor,WorldPoint destination,ushort source,uint tick)
    {
        if(!_entities.TryGet(actor,out var e)||e==null||!e.IsAlive||!Layout.IsWalkable(destination,_heroRadius,Destructibles.IsBroken,Hazards.IsDoorClosed))return false;
        e.X=destination.X;e.Y=destination.Y;
        if(Layout.Objects.Any(o=>o.Id==source&&o.Kind==WorldObjectKind.Portal)){_portals.TryGetValue(actor,out var previous);_portals[actor]=(source,previous.Sequence+1,_tick);}
        Revision++;return true;
    }
    public void Heal(byte actor,float amount){_health.Heal(actor,(byte)Math.Clamp((int)amount,0,255));}
    public void Rescue(byte actor,float hp,double invulnerableSeconds){_health.Rescue(actor,(byte)Math.Clamp((int)hp,1,100));_invulnerable[actor]=_now+invulnerableSeconds;}
    public void ApplyEffect(byte actor,ItemEffect effect,double seconds)
    {
        if(effect==ItemEffect.Cleanse){foreach(var k in _effects.Keys.Where(k=>k.Actor==actor&&(k.Effect=="Stun"||k.Effect=="Root"||k.Effect=="Reverse")).ToArray())_effects.Remove(k);_effects[(actor,0,"Immune")]=(_now+seconds,1);}
        else if(effect==ItemEffect.Speed)_effects[(actor,0,"Speed")]=(_now+seconds,1.3f);
        else if(effect==ItemEffect.Invisible)_effects[(actor,0,"Invisible")]=(_now+seconds,1);
        else if(effect==ItemEffect.Compass&&_entities.TryGet(actor,out var origin)&&origin!=null)foreach(var other in _entities.All)if(other.NetId!=actor&&other.IsAlive&&Math.Abs(other.X-origin.X)<=4.5f&&Math.Abs(other.Y-origin.Y)<=4.5f&&!_effects.Any(e=>e.Key.Actor==other.NetId&&e.Key.Effect=="Immune"&&e.Value.End>_now))_effects[(other.NetId,0,"Stun")]=(_now+Math.Min(seconds,1.5),0);
    }
    private bool TryPreventDamage(byte actor,byte amount)
    {
        if(_invulnerable.TryGetValue(actor,out var end)&&end>_now)return true;
        return _entities.TryGet(actor,out var e)&&e!=null&&amount>=e.Hp&&_registered.Contains(actor)&&Inventory.TryConsumeLifePassive(actor,_tick==0?1:_tick);
    }
    public List<WorldObjectWire> SnapshotObjects()
    {
        var result=new List<WorldObjectWire>();
        foreach(var o in Layout.Objects){Destructibles.TryGet(o.Id,out var d);var center=o.Bounds.Center;result.Add(new WorldObjectWire{Id=o.Id,Kind=(byte)o.Kind,Flags=(byte)((d?.Broken==true?1:0)|(o.Kind==WorldObjectKind.Door&&Hazards.IsDoorClosed(o.Id)?2:0)),PairId=o.PairId,Hp=(ushort)(d?.Hp??0),X=center.X,Y=center.Y,Width=o.Bounds.MaxX-o.Bounds.MinX,Height=o.Bounds.MaxY-o.Bounds.MinY});}
        foreach(var floor in Floor.Items)result.Add(new WorldObjectWire{Id=floor.Id,Kind=100,X=floor.X,Y=floor.Y,Width=.4f,Height=.4f,ItemId=floor.Stack.ItemId,Count=(ushort)floor.Stack.Count});
        return result;
    }
    public InventoryStatePacket SnapshotInventory(byte actor)
    {
        _portals.TryGetValue(actor,out var portal);var s=Inventory.GetSnapshot(actor);var slots=new InventorySlotWire[8];for(var i=0;i<4;i++){slots[i]=Slot(s.Weapons[i]);slots[i+4]=Slot(s.Consumables[i]);}
        return new InventoryStatePacket{MatchId=MatchId,Actor=actor,Revision=s.Revision,ActiveWeapon=(byte)(s.ActiveWeapon<0?255:s.ActiveWeapon),IsUsing=s.IsUsing,UseSlot=(byte)(s.UseSlot<0?255:s.UseSlot),UseItemId=s.UseItemId,UseDuration=(float)s.UseDuration,UseRemaining=(float)Math.Max(0,s.UseEnd-_now),Tick=_tick,PortalSourceId=portal.Source,PortalSequence=portal.Sequence,PortalTick=portal.Tick,EquipmentGeneration=Inventory.EquipmentGeneration(actor),MovementMultiplier=s.IsUsing?0:SpeedMultiplier(actor),SlowRemaining=Remaining(actor,"Slow"),PoisonRemaining=Remaining(actor,"Poison"),StunRemaining=Remaining(actor,"Stun"),SpeedRemaining=Remaining(actor,"Speed"),InvisibleRemaining=Remaining(actor,"Invisible"),ImmuneRemaining=Remaining(actor,"Immune"),Slots=slots,Helmet=Slot(s.Helmet),Armor=Slot(s.Armor)};
    }
    private float Remaining(byte actor, string effect)
    {
        var remaining=0f;
        foreach(var e in _effects)if(e.Key.Actor==actor&&e.Key.Effect==effect&&e.Value.End>_now)
        {
            if(double.IsPositiveInfinity(e.Value.End))return -1;
            remaining=Math.Max(remaining,(float)Math.Min(3600,e.Value.End-_now));
        }
        return remaining;
    }
    public (uint Revision, float Speed, byte Effects, bool Using, uint Portal, uint Equipment) OwnerEffectKey(byte actor)
    {
        byte effects=0;
        if(Remaining(actor,"Slow")!=0) effects|=1;
        if(Remaining(actor,"Poison")>0) effects|=2;
        if(Remaining(actor,"Stun")>0) effects|=4;
        if(Remaining(actor,"Speed")>0) effects|=8;
        if(Remaining(actor,"Invisible")>0) effects|=16;
        if(Remaining(actor,"Immune")>0) effects|=32;
        _portals.TryGetValue(actor,out var portal);
        var usingItem=Inventory.IsUsing(actor);
        return (Inventory.Revision(actor),usingItem?0:SpeedMultiplier(actor),effects,usingItem,portal.Sequence,Inventory.EquipmentGeneration(actor));
    }
    private static InventorySlotWire Slot(ItemStack s){return new InventorySlotWire{ItemId=s.ItemId,Count=(ushort)Math.Max(0,s.Count),Ammo=(ushort)Math.Max(0,s.Ammo),Durability=s.Durability,Magazine=(ushort)Math.Max(0,s.Magazine),MagazineKnown=s.MagazineKnown};}
}
