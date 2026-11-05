using GameServer.Maps;
using JungleMaze.World;
using JungleBackend.Protocol.Gameplay;
using GameServer.Simulation;
using LiteNetLib.Utils;
using JungleMaze.World.Inventory;

var checks=0;




void Check(bool okay,string name){checks++;if(!okay)throw new Exception("FAIL: "+name);Console.WriteLine("PASS: "+name);}
void Throws(Action action,string name){try{action();}catch(ArgumentException){Check(true,name);return;}throw new Exception("Expected rejection: "+name);}
var config=new MapLayoutConfig();
var map=MapLayoutGenerator.Generate(config,20261028);
var hashVector = new MapLayout { Version = "hash-vector", Seed = 7, Size = 50 };
hashVector.Spawns.Add(new WorldPoint(39.562305f, 35.580135f));
Check(MapLayoutGenerator.ComputeHash(hashVector) == "a30edc92d586ba3453c936fa654ecc3c0c9b7a6f6688bb92b424fef31e3768fb", "fixed-point hash matches shared Mono/CoreCLR golden vector");
Check(map.Objects.Count(o=>o.Kind==WorldObjectKind.Chest)==60,"60 chests");
Check(map.Objects.Count(o=>o.Kind==WorldObjectKind.Pot)==90,"90 pots");
Check(map.Objects.Count(o=>o.Kind==WorldObjectKind.Bush)==45,"45 bushes");
Check(map.Objects.Count(o=>o.Kind==WorldObjectKind.Portal)==6,"3 portal pairs");
Check(map.Spawns.Count==10,"10 spawns");
Check(map.Hash==MapLayoutGenerator.Generate(config,20261028).Hash,"stable seed and hash");
Check(map.Hash!=MapLayoutGenerator.Generate(config,20261029).Hash,"different seed changes layout");
for(uint seed=1;seed<=12;seed++){var seeded=MapLayoutGenerator.Generate(config,seed);MapLayoutGenerator.Validate(seeded,config);Check(seeded.Hash.Length==64,"valid deterministic layout seed "+seed);}
Throws(()=>MapLayoutGenerator.Generate(new MapLayoutConfig{ChestCount=4000},1),"bounded impossible placement rejects");
Throws(()=>MapLayoutGenerator.Generate(new MapLayoutConfig{HeroRadius=float.NaN},1),"nonfinite radius rejects");
var loot=new FakePorts();var destructibles=new DestructibleService(map,new DestructibleConfig(),loot);var chest=map.Objects.First(o=>o.Kind==WorldObjectKind.Chest);
Check(destructibles.DamageConfirmed(chest.Id,20,0,1),"confirmed damage applied");
Check(!destructibles.DamageConfirmed(chest.Id,20,0,1),"duplicate damage ignored");
Check(destructibles.DamageConfirmed(chest.Id,20,1,1),"second attacker breaks chest");
Check(loot.LootCount==1&&destructibles.IsBroken(chest.Id),"single loot batch and broken state");
Check(!destructibles.DamageConfirmed(chest.Id,100,1,2)&&loot.LootCount==1,"broken cannot respawn loot");
var hazardMap=new MapLayout{Version="test",Size=50};
hazardMap.Objects.Add(new WorldObjectDefinition{Id=1,Kind=WorldObjectKind.Lava,Bounds=new WorldBounds(5,5,3,3)});
hazardMap.Objects.Add(new WorldObjectDefinition{Id=2,Kind=WorldObjectKind.Swamp,Bounds=new WorldBounds(10,10,4,4)});
hazardMap.Objects.Add(new WorldObjectDefinition{Id=3,Kind=WorldObjectKind.Poison,Bounds=new WorldBounds(15,15,1,1)});
hazardMap.Objects.Add(new WorldObjectDefinition{Id=4,Kind=WorldObjectKind.Portal,Bounds=new WorldBounds(20,20,1,1),PairId=1});
hazardMap.Objects.Add(new WorldObjectDefinition{Id=5,Kind=WorldObjectKind.Portal,Bounds=new WorldBounds(25,25,1,1),PairId=1});
hazardMap.Objects.Add(new WorldObjectDefinition{Id=6,Kind=WorldObjectKind.Door,Bounds=new WorldBounds(30,30,1,2)});
var hazardPorts=new FakePorts();var hazards=new HazardService(hazardMap,new HazardConfig(),hazardPorts,hazardPorts,hazardPorts);
var actors=new[]{new WorldActor(0,5,5),new WorldActor(1,10,10),new WorldActor(2,15,15),new WorldActor(3,20,20)};
hazards.Tick(0,actors);Check(hazardPorts.DamageCount==0,"lava waits half-second");Check(hazardPorts.SlowCount==1&&hazardPorts.PoisonCount==1,"slow and poison requests");Check(hazardPorts.MoveCount==1,"portal moves once");
hazards.Tick(15,actors);Check(hazardPorts.DamageCount==1,"lava damage 10 every 15 ticks");
hazards.Tick(15,actors);Check(hazardPorts.DamageCount==1,"same tick no duplicate lava");
hazards.Tick(30,actors);Check(hazardPorts.DamageCount==2,"second lava pulse");
hazards.Tick(60,Array.Empty<WorldActor>());Check(hazardPorts.Removed==1,"swamp leaves remove source");
hazards.Tick(90,new[]{new WorldActor(3,25,25)});Check(hazardPorts.MoveCount==2,"portal cooldown expires");
hazards.Tick(600,new[]{new WorldActor(0,30,30)});Check(hazards.DoorsClosed&&hazardPorts.MoveCount==3,"door closes and displaces occupant");
hazards.Tick(1200,Array.Empty<WorldActor>());Check(!hazards.DoorsClosed&&hazards.CollisionRevision==2,"door all phases use server clock");
var overlappingMap=new MapLayout{Size=50};
overlappingMap.Objects.Add(new WorldObjectDefinition{Id=1,Kind=WorldObjectKind.Swamp,Bounds=new WorldBounds(10,10,4,4)});
overlappingMap.Objects.Add(new WorldObjectDefinition{Id=2,Kind=WorldObjectKind.Swamp,Bounds=new WorldBounds(12,10,4,4)});
var overlappingPorts=new FakePorts();overlappingPorts.ApplyEffect(0,999,"Slow",0,10,.5f);
var overlapping=new HazardService(overlappingMap,new HazardConfig(),overlappingPorts,overlappingPorts,overlappingPorts);
overlapping.Tick(0,new[]{new WorldActor(0,11,10)});
Check(overlappingPorts.SlowSources.Count==3,"overlapping swamps retain separate slow sources");
overlapping.Tick(1,new[]{new WorldActor(0,9,10)});
Check(overlappingPorts.SlowSources.Count==2&&overlappingPorts.SlowSources.Contains((0,1)),"leaving one swamp preserves the other slow");
overlapping.Tick(2,new[]{new WorldActor(0,20,10)});
Check(overlappingPorts.SlowSources.Count==1&&overlappingPorts.SlowSources.Contains((0,999)),"leaving swamps never removes an unrelated slow source");
var blockedMap=new MapLayout{Size=50};
blockedMap.Objects.Add(new WorldObjectDefinition{Id=1,Kind=WorldObjectKind.Portal,PairId=1,Bounds=new WorldBounds(10,10,1,1)});
blockedMap.Objects.Add(new WorldObjectDefinition{Id=2,Kind=WorldObjectKind.Portal,PairId=1,Bounds=new WorldBounds(20,20,1,1)});
var blocker=new WorldObjectDefinition{Id=3,Kind=WorldObjectKind.Wall,Bounds=new WorldBounds(20,20,2,2)};blockedMap.Objects.Add(blocker);
var blockedPorts=new FakePorts();var blockedPortal=new HazardService(blockedMap,new HazardConfig(),blockedPorts,blockedPorts,blockedPorts);
blockedPortal.Tick(0,new[]{new WorldActor(0,10,10)});Check(blockedPorts.MoveCount==0,"portal refuses a blocked destination");
blockedMap.Objects.Remove(blocker);blockedPortal.Tick(1,new[]{new WorldActor(0,10,10)});
Check(blockedPorts.MoveCount==1,"failed portal traversal does not consume cooldown");
blockedPortal.Tick(2,new[]{new WorldActor(0,10,10)});Check(blockedPorts.MoveCount==1,"portal cooldown rejects immediate repeat traversal");
blockedPortal.Tick(91,new[]{new WorldActor(0,10,10)});Check(blockedPorts.MoveCount==2,"portal cooldown permits traversal after three seconds");
var trappedMap=new MapLayout{Size=50};
trappedMap.Objects.Add(new WorldObjectDefinition{Id=1,Kind=WorldObjectKind.Door,Bounds=new WorldBounds(25,25,1,2)});
trappedMap.Objects.Add(new WorldObjectDefinition{Id=2,Kind=WorldObjectKind.Wall,Bounds=new WorldBounds(25,25,50,50)});
var trappedPorts=new FakePorts();var trappedDoor=new HazardService(trappedMap,new HazardConfig(),trappedPorts,trappedPorts,trappedPorts);
trappedDoor.Tick(0,new[]{new WorldActor(0,25,25)});trappedDoor.Tick(600,new[]{new WorldActor(0,25,25)});
Check(!trappedDoor.DoorsClosed&&trappedDoor.BlockedDoorClosures==1&&trappedPorts.MoveCount==0,"door stays open if no safe displacement exists");
var registry=new PacketRegistry();WorldActionRequestPacket received=default;registry.Subscribe<WorldActionRequestPacket>(p=>received=p);
var request=new WorldActionRequestPacket{MatchId=Guid.NewGuid(),RequestId=1,Action=WorldActionKind.Pickup,TargetId=400,Slot=0};var writer=new NetDataWriter();registry.WriteFrame(writer,ref request);Check(registry.TryReadFrame(new NetDataReader(writer.CopyData()))&&received.TargetId==400,"world request roundtrip ushort ID");
var invalid=writer.CopyData();invalid[9]=99;Check(!registry.TryReadFrame(new NetDataReader(invalid)),"wrong world version rejects");
var priorWorld=Environment.GetEnvironmentVariable("WORLD_ENABLED");
var priorLayout=Environment.GetEnvironmentVariable("WORLD_LAYOUT_MODE");
try
{
    Environment.SetEnvironmentVariable("WORLD_ENABLED","1");
    Environment.SetEnvironmentVariable("WORLD_LAYOUT_MODE","Prototype");
    // This golden vector describes the shipped ruins map, independently of the developer-selected default.
    var definition=new MapCatalogStore(Path.Combine(AppContext.BaseDirectory,"GameData","Maps"),"world-prototype-1").Current.Find("di-tich-co")!;
    var canonical=definition.Data;
    var schedule=ZoneSchedule.Load(GameDataLoader.ReadGameDataFile("zone_schedule.json"));
    var profile=new MatchProfile{Version=canonical.Version,MinPlayers=2,LobbyDurationSeconds=.01,LoadingTimeoutSeconds=1,ZoneTimeScale=1000,TargetMatchDurationSeconds=10000};
    MatchRuntime Start(string room,byte hp=100)
    {
        var p=hp==100?profile:new MatchProfile{Version=canonical.Version,MinPlayers=2,LobbyDurationSeconds=.01,LoadingTimeoutSeconds=1,PlayerHp=hp,ZoneTimeScale=1000,TargetMatchDurationSeconds=10000};
        var m=new MatchRuntime(room,canonical,schedule,p,selectedMap:definition);
        m.TryRegisterPlayer("p0","Hero0",out _);m.TryRegisterPlayer("p1","Hero1",out _);m.SetPlayerReady("p0");m.SetPlayerReady("p1");
        m.StartFromRoom();
        for(var i=0;i<4;i++)m.Tick(1.0/30);
        m.SetPlayerLoaded("p0",m.MatchId);m.SetPlayerLoaded("p1",m.MatchId);m.Tick(1.0/30);
        return m;
    }
    var match=Start("WORLD_A");var other=Start("WORLD_B");
    Check(match.World!=null&&other.World!=null,"world initializes from server profile at match start");
    var active=match.World!;var second=other.World!;
    Environment.SetEnvironmentVariable("WORLD_LAYOUT_MODE","Authored");
    var authored=Start("TEAM_MAP");
    var authoredLayout=authored.World!.Layout;
    Check(authoredLayout.Objects.Count(o=>o.IsHardCover)==4,"authored mode preserves exact 4 hard covers");
    Check(authoredLayout.Objects.Count==215,"authored map contains 4 hard covers + 211 versioned fixed worldObjects");
    Check(authoredLayout.Objects.Count(o=>o.Kind==WorldObjectKind.Chest)==60,"authored map exact 60 chests");
    Check(authoredLayout.Objects.Count(o=>o.Kind==WorldObjectKind.Pot)==90,"authored map exact 90 pots");
    Check(authoredLayout.Objects.Count(o=>o.Kind==WorldObjectKind.Bush)==45,"authored map exact 45 bushes");
    Check(authoredLayout.Objects.Count(o=>o.Kind==WorldObjectKind.Portal)==6,"authored map exact 6 portals (3 pairs)");
    Check(authoredLayout.Objects.Count(o=>o.Kind==WorldObjectKind.Door)==2,"authored map exact 2 doors");
    Check(authoredLayout.Objects.Count(o=>o.Kind==WorldObjectKind.Swamp)==2,"authored map exact 2 swamps");
    Check(authoredLayout.Objects.Count(o=>o.Kind==WorldObjectKind.Lava)==2,"authored map exact 2 lava");
    Check(authoredLayout.Objects.Count(o=>o.Kind==WorldObjectKind.Poison)==4,"authored map exact 4 poison");
    var portalPairs=authoredLayout.Objects.Where(o=>o.Kind==WorldObjectKind.Portal).GroupBy(o=>o.PairId).ToList();
    Check(portalPairs.Count==3&&portalPairs.All(g=>g.Count()==2),"authored portals paired into 3 valid 2-endpoint groups");
    foreach(var p in authoredLayout.Objects.Where(o=>o.Kind==WorldObjectKind.Portal))
    {
        var otherP=authoredLayout.Objects.First(o=>o.Kind==WorldObjectKind.Portal&&o.PairId==p.PairId&&o.Id!=p.Id);
        Check(authoredLayout.IsWalkable(otherP.Bounds.Center,profile.PlayerRadius),"portal destination is walkable with hero radius");
    }
    for(var i=0;i<authoredLayout.Spawns.Count;i++)
    {
        var sp=authoredLayout.Spawns[i];
        Check(authoredLayout.IsWalkable(sp,profile.PlayerRadius),"spawn point is walkable");
        Check(authoredLayout.Objects.All(o=>!o.IsHazard||!o.Bounds.Contains(sp,profile.PlayerRadius)),"spawn point has no hazard overlap");
        Check(new WorldPoint(25,25).DistanceSquared(sp)<=25*25,"spawn point inside initial zone radius");
        for(var j=i+1;j<authoredLayout.Spawns.Count;j++)
            Check(sp.DistanceSquared(authoredLayout.Spawns[j])>=6*6,"pairwise spawn distance >= 6");
    }
    var reachVisited=new HashSet<int>();var reachQ=new Queue<int>();var startCell=(int)authoredLayout.Spawns[0].X+(int)authoredLayout.Spawns[0].Y*authoredLayout.Size;
    reachQ.Enqueue(startCell);reachVisited.Add(startCell);
    while(reachQ.Count>0)
    {
        var cur=reachQ.Dequeue();var cx=cur%authoredLayout.Size;var cy=cur/authoredLayout.Size;
        foreach(var d in new[]{-1,1,-authoredLayout.Size,authoredLayout.Size})
        {
            var n=cur+d;var nx=n%authoredLayout.Size;var ny=n/authoredLayout.Size;
            if(n<0||n>=authoredLayout.Size*authoredLayout.Size||Math.Abs(nx-cx)+Math.Abs(ny-cy)!=1||reachVisited.Contains(n)||!authoredLayout.IsWalkable(new WorldPoint(nx+.5f,ny+.5f),profile.PlayerRadius))continue;
            reachVisited.Add(n);reachQ.Enqueue(n);
        }
    }
    Check(authoredLayout.Spawns.All(s=>reachVisited.Contains((int)s.X+(int)s.Y*authoredLayout.Size)),"all spawns connected in single reachable component");
    Check(authoredLayout.Objects.Where(o=>o.Kind==WorldObjectKind.Portal).All(p=>reachVisited.Contains((int)p.Bounds.Center.X+(int)p.Bounds.Center.Y*authoredLayout.Size)),"all portal exits connected to reachable component");
    Check(authoredLayout.Objects.Where(o=>o.IsDestructible).All(o=>reachVisited.Any(cell=>
        {var p=new WorldPoint(cell%authoredLayout.Size+.5f,cell/authoredLayout.Size+.5f);var b=o.Bounds;
         var dx=Math.Max(b.MinX,Math.Min(b.MaxX,p.X))-p.X;var dy=Math.Max(b.MinY,Math.Min(b.MaxY,p.Y))-p.Y;
         return dx*dx+dy*dy<=1.2f*1.2f;})),"every authored loot container has a reachable interaction approach");
    Check(authoredLayout.Hash=="cb3b0a1356ac911d41f58a3ea225af67b4651de7533b94549bfcfd835b82fd2d","authored client/server hash matches golden vector");
    Check(authored.World.Layout.Spawns.Select(p=>(p.X,p.Y)).SequenceEqual(canonical.SpawnPoints),"authored mode preserves every team spawn coordinate");
    Check(authored.World.Layout.Objects.Take(4).Select(o=>(o.Bounds.MinX,o.Bounds.MaxX,o.Bounds.MinY,o.Bounds.MaxY)).SequenceEqual(canonical.Obstacles.Select(o=>(o.MinX,o.MaxX,o.MinY,o.MaxY))),"authored mode preserves every team collider exactly");
    Environment.SetEnvironmentVariable("WORLD_LAYOUT_MODE","Prototype");
    Check(canonical.Obstacles.Count==4,"world collision never mutates shared canonical map");
    Check(match.Entities.All.All(e=>!match.Zone!.IsOutside(e.X,e.Y)),"world spawns start inside actual zone schedule");
    match.Entities.TryGet(0,out var actor);var px=actor!.X;var py=actor.Y;
    Check(match.EnsureWorld(match.MatchId)&&actor.X==px&&actor.Y==py,"resync does not teleport or mutate world");
    Check(!match.EnsureWorld(Guid.NewGuid()),"resync rejects old match identity");
    active.Floor.TrySpawn(new ItemStack{ItemId="TREE_BRANCH",Count=1},px,py,out var floor);
    var pickup=new WorldActionRequestPacket{MatchId=match.MatchId,RequestId=1,Action=WorldActionKind.Pickup,TargetId=floor};
    Check(match.ProcessWorldRequest("p0",pickup),"authoritative scoped pickup");
    Check(!match.ProcessWorldRequest("p0",pickup),"pickup retry does not duplicate item");
    Check(active.Inventory.GetSnapshot(0).Weapons[0].ItemId=="TREE_BRANCH","inventory stores received item");
    var victimChest=active.Layout.Objects.First(o=>o.Kind==WorldObjectKind.Chest);
    Check(active.Destructibles.DamageConfirmed(victimChest.Id,30,0,1)&&active.Floor.Items.Count>=3,"confirmed chest break creates floor items");
    Check(!second.Destructibles.IsBroken(victimChest.Id)&&second.Floor.Items.Count==0,"room isolation of broken state and loot");
    active.Floor.TrySpawn(new ItemStack{ItemId="ELIXIR_OF_LIFE",Count=1},px,py,out var life);
    pickup.RequestId=2;pickup.TargetId=life;Check(match.ProcessWorldRequest("p0",pickup),"passive item picked into dược slot");
    match.Health.ApplyDamage(0,255,1);Check(actor.Hp==50&&actor.IsAlive,"lethal hit consumes passive and restores 50 HP");
    match.Health.ApplyDamage(0,10,1);Check(actor.Hp==50,"rescue invulnerability blocks repeat same-tick hit");
    Check(match.ProcessLeave("p0",match.MatchId)&&!actor.IsAlive,"explicit leave bypasses invulnerability and passive");
    Check(active.Floor.Items.Any(f=>f.Stack.ItemId=="TREE_BRANCH"),"leave elimination death-drops inventory immediately");
    var low=Start("HP60",60);low.Health.ApplyDamage(0,20,1);((HealthService)low.Health).Heal(0,255);Check(low.Health.GetHp(0)==60,"healing respects profile max60");
    var high=Start("HP150",150);high.Health.ApplyDamage(0,20,1);((HealthService)high.Health).Heal(0,255);Check(high.Health.GetHp(0)==150,"healing respects profile max150");
    var secondActor=other.Entities.All.First();var newItem=new ItemStack{ItemId="GOD_LEAF",Count=1};second.Floor.TrySpawn(newItem,secondActor.X,secondActor.Y,out var healId);
    var serial=second.SnapshotObjects();Check(serial.Any(o=>o.Id==healId&&o.Kind==100&&o.ItemId=="GOD_LEAF"),"world snapshot includes floor catalog identity");
    var pageObjects=serial.Take(WorldWire.PageSize).ToArray();var page=new WorldObjectStatePacket{MatchId=other.MatchId,Revision=second.Revision,PageIndex=0,PageCount=(ushort)((serial.Count+23)/24),TotalObjects=(ushort)serial.Count,Objects=pageObjects};
    var pageReader=new PacketRegistry();WorldObjectStatePacket pageReceived=default;pageReader.Subscribe<WorldObjectStatePacket>(p=>pageReceived=p);writer.Reset();pageReader.WriteFrame(writer,ref page);Check(writer.Length<=4096&&pageReader.TryReadFrame(new NetDataReader(writer.CopyData()))&&pageReceived.Objects.Length==24,"world page is bounded and roundtrips");
    var s=second.SnapshotInventory(0);var inventoryReader=new PacketRegistry();inventoryReader.Subscribe<InventoryStatePacket>(_=>{});writer.Reset();inventoryReader.WriteFrame(writer,ref s);Check(inventoryReader.TryReadFrame(new NetDataReader(writer.CopyData())),"inventory snapshot roundtrip");
    var saturated = Start("SATURATED").World!;
    var saturatedChest = saturated.Layout.Objects.First(o => o.Kind == WorldObjectKind.Chest);
    while (saturated.Floor.TrySpawn(new ItemStack("GOD_LEAF"), 1, 1, out _)) { }
    Check(!saturated.Destructibles.DamageConfirmed(saturatedChest.Id, 30, 0, 1) && !saturated.Destructibles.IsBroken(saturatedChest.Id), "full floor rejects lethal container hit without losing its loot");
    foreach (var item in saturated.Floor.Items.ToArray()) saturated.Floor.Remove(item.Id);
    Check(saturated.Destructibles.DamageConfirmed(saturatedChest.Id, 30, 0, 1) && saturated.Floor.Items.Count is >= 3 and <= 4, "capacity recovery allows the same confirmed hit to spawn one complete loot batch");
    var batchCount = saturated.Floor.Items.Count;
    Check(!saturated.Destructibles.DamageConfirmed(saturatedChest.Id, 30, 0, 1) && saturated.Floor.Items.Count == batchCount, "retry after recovered destruction cannot duplicate the loot batch");
}
finally{Environment.SetEnvironmentVariable("WORLD_ENABLED",priorWorld);Environment.SetEnvironmentVariable("WORLD_LAYOUT_MODE",priorLayout);}
Console.WriteLine($"WorldChecks: {checks} passed, 0 failed");

sealed class FakePorts:IDestructibleLootSink,IEnvironmentDamageSink,IEnvironmentEffectSink,IEnvironmentMovementSink
{
    public readonly HashSet<(byte Actor,ushort Source)> SlowSources=new();
    public int LootCount,DamageCount,SlowCount,PoisonCount,MoveCount,Removed;
    public void SpawnLoot(WorldObjectKind source,ushort id,WorldPoint p,uint seed){LootCount++;}
    public void RequestDamage(byte actor,ushort source,uint tick,int amount,bool bypass){if(amount!=10||!bypass)throw new Exception("Invalid lava damage");DamageCount++;}
    public void ApplyEffect(byte actor,ushort source,string effect,uint tick,float seconds,float magnitude){if(effect=="Slow"){SlowCount++;SlowSources.Add((actor,source));}if(effect=="Poison")PoisonCount++;}
    public void RemoveEffect(byte actor,ushort source,string effect,uint tick){Removed++;if(effect=="Slow")SlowSources.Remove((actor,source));}
    public bool TryMove(byte actor,WorldPoint destination,ushort source,uint tick){MoveCount++;return true;}
}
