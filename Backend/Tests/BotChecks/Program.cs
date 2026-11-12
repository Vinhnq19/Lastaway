using System.Net;
using GameServer.Configuration;
using GameServer.Maps;
using GameServer.Rooms;
using GameServer.Simulation;
using GameServer.Simulation.Bots;
using JungleBackend.Protocol.Gameplay;
using JungleMaze.World.Inventory;
using JungleMaze.World;
using System.Numerics;
using GameServer.Simulation.Combat;
using System.Diagnostics;
using GameServer.Diagnostics;
using JungleBackend.Rooms;

var checks = 0;
void Check(bool ok, string message) { if (!ok) throw new Exception("FAIL: " + message); Console.WriteLine("PASS: " + message); checks++; }
Environment.SetEnvironmentVariable("WORLD_ENABLED", "1");
var settings = BotSettings.Load(GameDataLoader.ReadGameDataFile("bot_settings.json"));
Check(settings.ThinkHz == 5 && settings.ReactionSeconds >= .35, "versioned fair bot settings");
var maps = new MapCatalogStore(Path.Combine(AppContext.BaseDirectory,"FixtureMaps"),"world-prototype-1");
RoomManager Manager(MatchProfile? profile = null, int total = 50) => new(new ServerOptions(IPAddress.Loopback,7777,10,30,5,"RoomCode","BotsTest",Rooms:new RoomOptions(MaxTotalPlayers:total)),profile:profile ?? MatchProfile.CreateTestProfile(),catalog:maps);
uint sequence = 0;
void Human(RoomManager manager,RoomManager.Room room,string id,int peer)
{
    manager.Reserve(room,peer,true);
    Check(room.Match.TryRegisterPlayer(id,id,out _),"register human " + id);
    room.Match.TryAttachProfile(id,"account:"+id);room.Match.SetPlayerReady(id);manager.RegisterMember(room,id,"account:"+id);
    var state=manager.GetMapState(room);
    Check(manager.AcknowledgeMap(room,id,new RoomMapReadyPacket{RoomId=room.Id,SelectionRevision=state.SelectionRevision,MapHash=state.MapHash,RulesHash=state.RulesHash})==RoomMapError.None,"human verifies map");
}
RoomBotCommandPacket Command(RoomManager.Room room,RoomManager manager,RoomBotAction action,uint bot=0) =>
    new(){RoomId=room.Id,Revision=manager.GetMapState(room).LobbyRevision,RequestId=++sequence,Action=action,BotId=bot};
void Start(RoomManager manager,RoomManager.Room room,string host)
{
    Check(manager.StartMatch(room,host,new StartRoomMatchPacket{RoomId=room.Id,SelectionRevision=manager.GetMapState(room).SelectionRevision})==RoomMapError.None,"host starts human plus bots");
    for(var i=0;i<20;i++) { room.Match.SetPlayerLoaded(host,room.Match.MatchId);manager.Tick(1d/30); }
    Check(room.Match.Phase==MatchPhase.InGame,"bots load without sockets or acknowledgements");
}
var rooms=Manager();var room=rooms.AllRooms.Single();Human(rooms,room,"human",1);
var cmd=Command(room,rooms,RoomBotAction.Add);
Check(rooms.ManageBots(room,"intruder",cmd)==RoomBotError.NotHost,"non-host cannot add bot");
Check(rooms.ManageBots(room,"human",cmd)==RoomBotError.None,"host adds bot");
Check(rooms.ManageBots(room,"human",cmd)==RoomBotError.None&&room.Match.BotCount==1,"duplicate request does not add twice");
Check(rooms.ReservedPeers==2&&room.Match.GetRosterPacket().Players!.Count(p=>p.IsBot)==1,"bot occupies actor slot but has no peer");
Check(room.Peers.Count==1,"no simulated transport session");
for(var i=0;i<8;i++)Check(rooms.ManageBots(room,"human",Command(room,rooms,RoomBotAction.Add))==RoomBotError.None,"fill room bot slot");
Check(room.Match.BotCount==9&&rooms.CapacityError(room)=="RoomFull","1 human plus 9 bots is full");
Check(rooms.ManageBots(room,"human",Command(room,rooms,RoomBotAction.Add))==RoomBotError.RoomFull,"cannot exceed ten actors");
var first=room.Match.GetRosterPacket().Players!.First(p=>p.IsBot);
Check(rooms.ManageBots(room,"human",Command(room,rooms,RoomBotAction.Remove,first.BotId))==RoomBotError.None&&rooms.CapacityError(room)==null,"removing bot frees slot");
var stale=Command(room,rooms,RoomBotAction.Add);stale.Revision--;
Check(rooms.ManageBots(room,"human",stale)==RoomBotError.StaleRoom,"old lobby revision rejected");
Start(rooms,room,"human");
Check(rooms.ManageBots(room,"human",Command(room,rooms,RoomBotAction.Add))==RoomBotError.MatchLocked,"cannot modify bots in match");
var before=room.Match.Entities.All.Where(e=>e.PlayerSessionId.StartsWith("bot:")).ToDictionary(e=>e.NetId,e=>(e.X,e.Y));
for(var i=0;i<180;i++)rooms.Tick(1d/30);
Check(room.Match.Entities.All.Any(e=>before.TryGetValue(e.NetId,out var p)&&Math.Abs(e.X-p.X)+Math.Abs(e.Y-p.Y)>.1),"bots generate real movement input");
Check(room.Match.BotStates.Values.Any(s=>s!=BotState.Idle),"bot state machine advances");
var small=Manager(total:2);var smallRoom=small.AllRooms.Single();Human(small,smallRoom,"small",1);
Check(small.ManageBots(smallRoom,"small",Command(smallRoom,small,RoomBotAction.Add))==RoomBotError.None,"bot fits server remaining slot");
Check(small.ManageBots(smallRoom,"small",Command(smallRoom,small,RoomBotAction.Add))==RoomBotError.ServerFull,"bot respects global server cap");
small.Release(smallRoom,1,"small");small.Tick(1d/30);
Check(smallRoom.Match.BotCount==0&&small.ReservedPeers==0,"last human leaving waiting room clears bot slots");
foreach(var publishedMap in new MapCatalogStore(Path.Combine(AppContext.BaseDirectory,"GameData","Maps"),"world-prototype-1").Current.Maps.Values)
{
    var layout=publishedMap.Layout;var nav=publishedMap.Navigation;var rng=new Random(123);
    for(var sample=0;sample<1000;sample++)
    {
        var point=new Vector2((float)rng.NextDouble()*layout.Size,(float)rng.NextDouble()*layout.Size);
        var radius=sample%2==0?.4f:.8f;
        if(nav.WalkableWorld(layout,point,radius,id=>id%2==0,id=>id%3==0)!=layout.IsWalkable(new WorldPoint(point.X,point.Y),radius,id=>id%2==0,id=>id%3==0))throw new Exception("Spatial query differs from canonical collision");
    }
    Check(true,"navigation spatial query matches canonical collision for "+publishedMap.Entry.Id);
}
var main = MatchProfile.Current;
var mainRooms=Manager(main);var mainRoom=mainRooms.AllRooms.Single();Human(mainRooms,mainRoom,"main",1);
Check(mainRooms.ManageBots(mainRoom,"main",Command(mainRoom,mainRooms,RoomBotAction.FillMinimum))==RoomBotError.None&&mainRoom.Match.BotCount==main.MinPlayers-1,"fill minimum follows profile");
Check(mainRooms.GetMapState(mainRoom).CanStart,"bots satisfy minimum only after human map verification");
var budget=new BotNavigationBudget();budget.Reset(3);
Check(budget.Take()&&budget.Take()&&budget.Take()&&!budget.Take()&&budget.Used==3,"global navigation budget is bounded");
var pickProfile=new MatchProfile{Version=main.Version,MinPlayers=2,MaxPlayers=10,LobbyDurationSeconds=.1,LoadingTimeoutSeconds=1,DisconnectGraceSeconds=1,EndingDurationSeconds=.5,ZoneTimeScale=1,WorldEnabled=true};
var pickRooms=Manager(pickProfile);var pickRoom=pickRooms.AllRooms.Single();Human(pickRooms,pickRoom,"loot-human",1);
Check(pickRooms.ManageBots(pickRoom,"loot-human",Command(pickRoom,pickRooms,RoomBotAction.Add))==RoomBotError.None,"loot room adds one bot");
Check(pickRooms.StartMatch(pickRoom,"loot-human",new StartRoomMatchPacket{RoomId=pickRoom.Id,SelectionRevision=pickRooms.GetMapState(pickRoom).SelectionRevision})==RoomMapError.None,"loot room match starts");
for(var i=0;i<60&&pickRoom.Match.Phase!=MatchPhase.InGame;i++){pickRoom.Match.SetPlayerLoaded("loot-human",pickRoom.Match.MatchId);pickRooms.Tick(1d/30);}
Check(pickRoom.Match.Phase==MatchPhase.InGame,"loot room reaches in game");
if (!pickRoom.Match.Entities.TryGetBySession("loot-human", out var human) || human is null) throw new Exception("FAIL: human entity missing");
var botPlayer=pickRoom.Match.GetRosterPacket().Players!.Single(p=>p.IsBot);
if (!pickRoom.Match.Entities.TryGet(botPlayer.NetId, out var bot) || bot is null) throw new Exception("FAIL: bot entity missing");
human.X=.5f;human.Y=.5f;pickRoom.Match.World!.Floor.Clear();
// Fake clock expires initial spawn memory without letting the bot roam, break containers or collect other weapons.
typeof(MatchRuntime).GetField("_matchElapsedSeconds",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.SetValue(pickRoom.Match,4d);
pickRoom.Match.World.SyncActors();
Check(pickRoom.Match.World.Inventory.GetSnapshot(bot.NetId).Weapons.All(s=>s.IsEmpty),"pickup fixture starts with an empty weapon bag");
// The production catalog now enables RUBBER_GUN; disable it explicitly only for this fixture.
var pickupCatalog=(ItemCatalog)typeof(MatchRuntime).GetField("_worldCatalog",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.GetValue(pickRoom.Match)!;
var pickupDefinitions=(Dictionary<string,ItemDefinition>)typeof(ItemCatalog).GetField("_items",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.GetValue(pickupCatalog)!;
var disabledItem=pickupDefinitions["RUBBER_GUN"];var previousEnabled=disabledItem.Enabled;
try
{
    disabledItem.Enabled=false;
    Check(!pickupCatalog.Get("RUBBER_GUN").Enabled,"fixture catalog explicitly disables test item");
    Check(pickRoom.Match.World.Floor.TrySpawn(new ItemStack("RUBBER_GUN",1),bot.X,bot.Y,out var disabledId),"disabled item fixture");
    Check(pickRoom.Match.World.Floor.TrySpawn(new ItemStack("KNIFE",1),bot.X,bot.Y,out var knifeId),"knife spawns on floor at bot");
    for(var i=0;i<30&&pickRoom.Match.World.Floor.TryGet(knifeId,out _);i++)pickRooms.Tick(1d/30);
    if(!pickRoom.Match.World.Inventory.GetSnapshot(bot.NetId).Weapons.Any(s=>s.ItemId=="KNIFE"))Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new {bot.X,bot.Y,bot.Hp,alive=bot.IsAlive,phase=pickRoom.Match.Phase.ToString(),elapsed=pickRoom.Match.ElapsedSeconds,state=pickRoom.Match.BotStates,bag=pickRoom.Match.World.Inventory.GetSnapshot(bot.NetId).Weapons.Select(s=>s.ItemId),usingItem=pickRoom.Match.World.Using(bot.NetId),floor=pickRoom.Match.World.Floor.Items.Select(f=>new{f.Id,f.X,f.Y,item=f.Stack.ItemId})}));
    Check(pickRoom.Match.World.Inventory.GetSnapshot(bot.NetId).Weapons.Count(s=>s.ItemId=="KNIFE")==1,"bot picks up one knife through brain");
    Check(!pickRoom.Match.World.Floor.TryGet(knifeId,out _),"picked up knife leaves the floor");
    Check(pickRoom.Match.World.Floor.TryGet(disabledId,out _)&&!pickRoom.Match.World.Inventory.GetSnapshot(bot.NetId).Weapons.Any(w=>w.ItemId=="RUBBER_GUN"),"bot ignores items disabled by current catalog");
}
finally { disabledItem.Enabled=previousEnabled; }

// Fixture positioning belongs only to tests; every action under test is generated by the real bot brain.
(RoomManager Manager, RoomManager.Room Room, IEntity Human, IEntity Bot) Fixture(int botCount=1)
{
    var m=Manager(pickProfile);var r=m.AllRooms.Single();Human(m,r,"fixture",1);
    // Geometry-specific perception fixtures use the stable ruins map, not the developer's catalog default.
    var map=maps.Current.Find("di-tich-co")!;var selection=m.GetMapState(r);
    Check(m.SelectMap(r,"fixture",new SelectRoomMapPacket{RoomId=r.Id,SelectionRevision=selection.SelectionRevision,MapId=map.Entry.Id,MapHash=map.Entry.MapHash})==RoomMapError.None,"fixture selects deterministic geometry");
    selection=m.GetMapState(r);
    Check(m.AcknowledgeMap(r,"fixture",new RoomMapReadyPacket{RoomId=r.Id,SelectionRevision=selection.SelectionRevision,MapHash=selection.MapHash,RulesHash=selection.RulesHash})==RoomMapError.None,"fixture verifies selected geometry");
    for(var i=0;i<botCount;i++)Check(m.ManageBots(r,"fixture",Command(r,m,RoomBotAction.Add))==RoomBotError.None,"fixture adds bot");
    Check(m.StartMatch(r,"fixture",new StartRoomMatchPacket{RoomId=r.Id,SelectionRevision=m.GetMapState(r).SelectionRevision})==RoomMapError.None,"fixture starts");
    // Clock/perception geometry fixtures use a stable round seed before zone/world/brains are initialized.
    typeof(MatchRuntime).GetProperty(nameof(MatchRuntime.MatchId))!.SetValue(r.Match,new Guid(42,0,0,new byte[8]));
    for(var t=0;t<60&&r.Match.Phase!=MatchPhase.InGame;t++){r.Match.SetPlayerLoaded("fixture",r.Match.MatchId);m.Tick(1d/30);}
    if(r.Match.Phase!=MatchPhase.InGame||!r.Match.Entities.TryGetBySession("fixture",out var h)||h is null)throw new Exception("Fixture failed to load human");
    var bp=r.Match.GetRosterPacket().Players!.First(p=>p.IsBot);
    if(!r.Match.Entities.TryGet(bp.NetId,out var b)||b is null)throw new Exception("Fixture failed to load bot");
    r.Match.World!.Floor.Clear();h.X=.5f;h.Y=.5f;r.Match.World.SyncActors();return(m,r,h,b);
}
bool Safe(MatchRuntime m,Vector2 p) => m.World!.Layout.IsWalkable(new WorldPoint(p.X,p.Y),.4f,m.World.Destructibles.IsBroken,m.World.Hazards.IsDoorClosed)
    && !m.World.Layout.Objects.Any(o=>(o.IsHazard||o.Kind==WorldObjectKind.Portal)&&o.Bounds.Contains(new WorldPoint(p.X,p.Y),.5f))
    && m.Zone?.IsOutside(p.X,p.Y)!=true;
(Vector2 A,Vector2 B) Lane(MatchRuntime m,float distance)
{
    for(float y=8;y<42;y+=.5f)for(float x=8;x<42-distance;x+=.5f)
    {
        var a=new Vector2(x,y);var b=a+new Vector2(distance,0);
        if(Enumerable.Range(0,21).All(i=>Safe(m,Vector2.Lerp(a,b,i/20f))))return(a,b);
    }
    throw new Exception("No safe fixture lane");
}
void Position(MatchRuntime m,IEntity e,Vector2 p){e.X=p.X;e.Y=p.Y;m.World!.SyncActors();}
void Pump(RoomManager m,int ticks){for(var t=0;t<ticks;t++)m.Tick(1d/30);}

var fight=Fixture();var lane=Lane(fight.Room.Match,1.05f);
Position(fight.Room.Match,fight.Bot,lane.A);Position(fight.Room.Match,fight.Human,lane.B);
Pump(fight.Manager,10);
Check(fight.Human.Hp==100,"new enemy does not receive damage before 350ms reaction");
Check(Vector2.Distance(new Vector2(fight.Bot.X,fight.Bot.Y),lane.A)<.01f,"new enemy does not trigger movement before reaction");
var movedTooFast=false;
for(var t=0;t<60&&fight.Room.Match.Phase==MatchPhase.InGame;t++)
{
    var p=new Vector2(fight.Bot.X,fight.Bot.Y);fight.Manager.Tick(1d/30);
    movedTooFast|=Vector2.Distance(p,new Vector2(fight.Bot.X,fight.Bot.Y))>pickProfile.PlayerSpeed/30+.002f;
}
Check(fight.Human.Hp<100,"bot causes actual melee damage after reaction");Check(!movedTooFast,"bot obeys normal movement speed");

var hidden=Fixture();lane=Lane(hidden.Room.Match,1.05f);
Position(hidden.Room.Match,hidden.Bot,lane.A);Position(hidden.Room.Match,hidden.Human,lane.B);
hidden.Room.Match.World!.ApplyEffect(hidden.Human.NetId,ItemEffect.Invisible,30);Pump(hidden.Manager,24);
Check(hidden.Room.Match.BotStates.Values.All(s=>s is not (BotState.Combat or BotState.Chase or BotState.Search)),"invisible opponent is not detected");

var healing=Fixture();var world=healing.Room.Match.World!;healing.Bot.Hp=35;
Check(world.Floor.TrySpawn(new ItemStack("PHOENIX_FEATHERS",1),healing.Bot.X,healing.Bot.Y,out _),"healing item fixture");
Pump(healing.Manager,24);Check(world.Inventory.GetSnapshot(healing.Bot.NetId).IsUsing,"bot begins timed healing via inventory");
var healingPosition=new Vector2(healing.Bot.X,healing.Bot.Y);Pump(healing.Manager,24);
Check(Vector2.Distance(healingPosition,new Vector2(healing.Bot.X,healing.Bot.Y))<.01f,"bot stands still while healing");
Pump(healing.Manager,60);Check(healing.Bot.Hp>35,"timed inventory use restores HP");

var upgrade=Fixture();world=upgrade.Room.Match.World!;
for(var i=0;i<4;i++)Check(world.Floor.TrySpawn(new ItemStack("TREE_BRANCH",1),upgrade.Bot.X,upgrade.Bot.Y,out _),"weapon slot fixture");
for(var t=0;t<120&&world.Inventory.GetSnapshot(upgrade.Bot.NetId).Weapons.Count(w=>!w.IsEmpty)<4;t++)upgrade.Manager.Tick(1d/30);
Check(world.Inventory.GetSnapshot(upgrade.Bot.NetId).Weapons.Count(w=>!w.IsEmpty)==4,"bot fills four weapon slots without fake inventory commands");
Check(world.Floor.TrySpawn(new ItemStack("SHOTGUN",1),upgrade.Bot.X,upgrade.Bot.Y,out _),"upgrade fixture");
for(var t=0;t<60&&!world.Inventory.GetSnapshot(upgrade.Bot.NetId).Weapons.Any(w=>w.ItemId=="SHOTGUN");t++)upgrade.Manager.Tick(1d/30);
Check(world.Inventory.GetSnapshot(upgrade.Bot.NetId).Weapons.Any(w=>w.ItemId=="SHOTGUN")&&world.Floor.Items.Any(i=>i.Stack.ItemId=="TREE_BRANCH"),"full inventory upgrades weapon and drops replaced item");

var reload=Fixture();world=reload.Room.Match.World!;
Check(world.Floor.TrySpawn(new ItemStack("SHOTGUN",1,0,10,1),reload.Bot.X,reload.Bot.Y,out _),"partly loaded gun fixture");
for(var t=0;t<30&&!world.Inventory.GetSnapshot(reload.Bot.NetId).Weapons.Any(w=>w.ItemId=="SHOTGUN");t++)reload.Manager.Tick(1d/30);
Check(world.Inventory.GetSnapshot(reload.Bot.NetId).Weapons.Any(w=>w.ItemId=="SHOTGUN"),"bot acquires reload fixture gun before opponent is introduced");
// Keep the opponent inside firing range: this assertion measures reload, not A* travel time.
lane=Lane(reload.Room.Match,2.5f);Position(reload.Room.Match,reload.Bot,lane.A);Position(reload.Room.Match,reload.Human,lane.B);
var reloaded=false;
for(var t=0;t<150&&reload.Room.Match.Phase==MatchPhase.InGame;t++)
{
    reload.Manager.Tick(1d/30);reloaded|=world.Inventory.GetSnapshot(reload.Bot.NetId).Weapons.Any(w=>w.ItemId=="SHOTGUN"&&w.Ammo<10);
    if(reloaded)break;
}
if(!reloaded)
{
    var items=string.Join(',',world.Inventory.GetSnapshot(reload.Bot.NetId).Weapons.Select(w=>$"{w.ItemId}:{w.Magazine}:{w.Ammo}"));
    Console.WriteLine($"Reload debug state={string.Join(',',reload.Room.Match.BotStates.Values)} hp={reload.Human.Hp} inv={items} bush={world.Layout.Objects.Any(o=>o.Kind==WorldObjectKind.Bush&&o.Bounds.Contains(new WorldPoint(reload.Human.X,reload.Human.Y)))}");
}
Check(reloaded,"bot reload consumes reserve ammo through real combat service");

var grace=Fixture();MatchResult? ended=null;grace.Room.Match.MatchCompleted+=r=>ended=r;
grace.Room.Match.GraceExpired+=_=>grace.Manager.ReleaseSlot(grace.Room,1);
grace.Room.Match.PlayerDropped+=_=>grace.Manager.ReleaseSlot(grace.Room,1);
grace.Manager.Release(grace.Room,1,"fixture");Pump(grace.Manager,10);
Check(grace.Room.Match.Phase==MatchPhase.InGame&&grace.Manager.ReservedPeers==2,"human grace preserves match and both slots");
Check(!grace.Room.Match.IsValidResumeToken(grace.Bot.ResumeToken,out _),"bot has no resumable human proof");
Pump(grace.Manager,90);
Check(ended?.Reason=="NoHumans"&&ended.MatchKind=="WithBots","no audience after grace ends match without win");
Check(grace.Room.Match.BotCount==0&&grace.Manager.ReservedPeers==0,"NoHumans cleanup returns all bot and human slots");

var victory=Fixture();ended=null;victory.Room.Match.MatchCompleted+=r=>ended=r;
victory.Room.Match.Health.ApplyDamage(victory.Human.NetId,255,victory.Bot.NetId);victory.Manager.Tick(1d/30);
Check(ended?.Reason=="LastAlive"&&ended.MatchKind=="WithBots"&&ended.Participants.Single(p=>p.IsBot).Rank==1,"bot can win while connected eliminated human watches");
Check(ended!.Participants.Where(p=>p.IsBot).All(p=>p.ProfileKey is null),"bot results never carry account profiles");
var oldMatch=victory.Room.Match.MatchId;victory.Room.Match.ProcessReplayAck("fixture",oldMatch);Pump(victory.Manager,4);
Check(victory.Room.Match.Phase==MatchPhase.Waiting&&victory.Room.Match.BotCount==1&&victory.Room.Match.MatchId!=oldMatch,"human replay ack alone resets match and retains bot configuration");

var dead=Fixture(2);dead.Room.Match.Health.ApplyDamage(dead.Bot.NetId,255,dead.Human.NetId);
var inputAtDeath=dead.Bot.LastProcessedInputSeq;Pump(dead.Manager,2);
Check(dead.Room.Match.BotStates[1]==BotState.Dead&&dead.Bot.LastProcessedInputSeq==inputAtDeath,"dead bot stops decisions and input while other actors continue");

bool Line(MatchRuntime m,Vector2 a,Vector2 b) => !m.World!.Layout.Objects.Any(o=>
    (o.IsHardCover||o.IsDestructible&&!m.World.Destructibles.IsBroken(o.Id)||o.Kind==WorldObjectKind.Door&&m.World.Hazards.IsDoorClosed(o.Id))&&
    CombatGeometry.SegmentIntersectsAabb(a,b,new AABB("fixture",o.Bounds.MinX,o.Bounds.MaxX,o.Bounds.MinY,o.Bounds.MaxY),out _));
var sight=Fixture();world=sight.Room.Match.World!;
var cover=world.Layout.Objects.First(o=>o.IsHardCover&&o.Bounds.MaxY-o.Bounds.MinY>=3&&
    Safe(sight.Room.Match,new Vector2(o.Bounds.MinX-.6f,o.Bounds.Center.Y))&&Safe(sight.Room.Match,new Vector2(o.Bounds.MaxX+.6f,o.Bounds.Center.Y)));
Position(sight.Room.Match,sight.Bot,new Vector2(cover.Bounds.MinX-.6f,cover.Bounds.Center.Y));
Position(sight.Room.Match,sight.Human,new Vector2(cover.Bounds.MaxX+.6f,cover.Bounds.Center.Y));
world.ApplyEffect(sight.Bot.NetId,0,"Stun",sight.Room.Match.CurrentTick,30,1);Pump(sight.Manager,24);
Check(sight.Room.Match.BotStates.Values.All(s=>s is not (BotState.Combat or BotState.Chase or BotState.Search)),"cover blocks bot perception");
lane=Lane(sight.Room.Match,1.05f);Position(sight.Room.Match,sight.Bot,lane.A);Position(sight.Room.Match,sight.Human,lane.B);Pump(sight.Manager,24);
Check(sight.Room.Match.BotStates.Values.Any(s=>s is BotState.Combat or BotState.Chase),"unobstructed nearby opponent is detected");
Position(sight.Room.Match,sight.Human,new Vector2(.5f,.5f));Pump(sight.Manager,6);
Check(sight.Room.Match.BotStates.Values.Contains(BotState.Search),"lost opponent is searched at last seen position");Pump(sight.Manager,100);
Check(!sight.Room.Match.BotStates.Values.Contains(BotState.Search),"target memory expires after three seconds");

var bush=Fixture();world=bush.Room.Match.World!;
var bushObject=world.Layout.Objects.First(o=>o.Kind==WorldObjectKind.Bush&&Safe(bush.Room.Match,new Vector2(o.Bounds.Center.X-3,o.Bounds.Center.Y))&&
    Safe(bush.Room.Match,new Vector2(o.Bounds.Center.X-1.2f,o.Bounds.Center.Y))&&Line(bush.Room.Match,new Vector2(o.Bounds.Center.X-3,o.Bounds.Center.Y),new Vector2(o.Bounds.Center.X,o.Bounds.Center.Y)));
Position(bush.Room.Match,bush.Human,new Vector2(bushObject.Bounds.Center.X,bushObject.Bounds.Center.Y));
Position(bush.Room.Match,bush.Bot,new Vector2(bushObject.Bounds.Center.X-3,bushObject.Bounds.Center.Y));
world.ApplyEffect(bush.Bot.NetId,0,"Stun",bush.Room.Match.CurrentTick,30,1);Pump(bush.Manager,24);
Check(bush.Room.Match.BotStates.Values.All(s=>s is not (BotState.Combat or BotState.Chase or BotState.Search)),"bush hides opponent beyond two cells");
Position(bush.Room.Match,bush.Bot,new Vector2(bushObject.Bounds.Center.X-1.2f,bushObject.Bounds.Center.Y));Pump(bush.Manager,24);
Check(bush.Room.Match.BotStates.Values.Any(s=>s is BotState.Combat or BotState.Chase),"bush opponent is revealed within two cells without cover");

var breaking=Fixture();world=breaking.Room.Match.World!;
var chest=world.Layout.Objects.First(o=>o.Kind==WorldObjectKind.Chest&&Safe(breaking.Room.Match,new Vector2(o.Bounds.MinX-.5f,o.Bounds.Center.Y))&&
    world.Layout.Objects.Where(c=>c.IsDestructible&&c.Id!=o.Id).All(c=>c.Bounds.Center.DistanceSquared(new WorldPoint(o.Bounds.MinX-.5f,o.Bounds.Center.Y))>=o.Bounds.Center.DistanceSquared(new WorldPoint(o.Bounds.MinX-.5f,o.Bounds.Center.Y))));
Position(breaking.Room.Match,breaking.Bot,new Vector2(chest.Bounds.MinX-.5f,chest.Bounds.Center.Y));
var breaks=0;world.Destructibles.Changed+=s=>{if(s.Id==chest.Id&&s.Broken)breaks++;};
for(var t=0;t<300&&!world.Destructibles.IsBroken(chest.Id);t++)breaking.Manager.Tick(1d/30);
Check(world.Destructibles.IsBroken(chest.Id)&&breaks==1,"bot breaks a real chest through combat");
Check(world.Floor.Items.Count>0||world.Inventory.GetSnapshot(breaking.Bot.NetId).Weapons.Any(w=>!w.IsEmpty),"broken chest produces authoritative loot");Pump(breaking.Manager,60);
Check(breaks==1,"continued bot attacks do not break or spawn chest loot twice");

var zoneEscape=Fixture();var escapeRuntime=zoneEscape.Room.Match;
Position(escapeRuntime,zoneEscape.Bot,new Vector2(1.25f,1.25f));
escapeRuntime.World!.ApplyEffect(zoneEscape.Human.NetId,ItemEffect.Invisible,30);
var distanceBefore=Vector2.Distance(new Vector2(zoneEscape.Bot.X,zoneEscape.Bot.Y),new Vector2(25,25));
Pump(zoneEscape.Manager,90);
if(Vector2.Distance(new Vector2(zoneEscape.Bot.X,zoneEscape.Bot.Y),new Vector2(25,25))>=distanceBefore-1)
    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { zoneEscape.Bot.X,zoneEscape.Bot.Y,zoneEscape.Bot.Hp,phase=escapeRuntime.Phase.ToString(),state=escapeRuntime.BotStates,elapsed=escapeRuntime.ElapsedSeconds,budget=escapeRuntime.NavigationBudget.Used }));
Check(zoneEscape.Bot.IsAlive&&Vector2.Distance(new Vector2(zoneEscape.Bot.X,zoneEscape.Bot.Y),new Vector2(25,25))<distanceBefore-1,"bot moves back toward safe zone from outside");

// No warning flag is sent during an active shrink: the bot must still move toward the published target.
var shrinking=Fixture();var shrink=shrinking.Room.Match;
typeof(MatchRuntime).GetField("_matchElapsedSeconds",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.SetValue(shrink,93d);
shrink.Zone!.Update(93,shrink.CurrentTick);
var published=shrink.Zone.GetStatePacket(shrink.CurrentTick);Vector2 shrinkPoint=default;var foundShrinkPoint=false;
for(float y=2;y<48&&!foundShrinkPoint;y+=.5f)for(float x=2;x<48&&!foundShrinkPoint;x+=.5f)
{
    var point=new Vector2(x,y);
    if(Safe(shrink,point)&&Vector2.Distance(point,new Vector2(published.TargetCenterX,published.TargetCenterY))>published.TargetRadius+1)
    {shrinkPoint=point;foundShrinkPoint=true;}
}
Check(foundShrinkPoint&&!published.InWarning,"active shrink fixture is inside current circle but outside target without warning flag");
Position(shrink,shrinking.Bot,shrinkPoint);shrink.World!.ApplyEffect(shrinking.Human.NetId,ItemEffect.Invisible,30);
Pump(shrinking.Manager,6);
Check(shrink.BotStates.Values.Contains(BotState.EscapeZone),"bot keeps escaping while the zone is actively shrinking");
var starving=Fixture();var starve=starving.Room.Match;
Position(starve,starving.Bot,new Vector2(1.25f,1.25f));starve.World!.ApplyEffect(starving.Human.NetId,ItemEffect.Invisible,30);
typeof(BotNavigationBudget).GetField("Shared",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.SetValue(starve.NavigationBudget,true);
for(var t=0;t<240;t++){starve.NavigationBudget.Reset(0);starve.Tick(1d/30);}
Check(starve.PendingBotPaths==1,"waiting for a shared budget does not repeatedly discard the path as stuck");
var starveStart=new Vector2(starving.Bot.X,starving.Bot.Y);
for(var t=0;t<100;t++){starve.NavigationBudget.Reset(1500);starve.Tick(1d/30);}
Check(Vector2.Distance(starveStart,new Vector2(starving.Bot.X,starving.Bot.Y))>1,"bot resumes movement when navigation budget becomes available");

// Recovery must rearm its deadline rather than cancelling every route on the next tick.
var recovery=Fixture();var recoveryMatch=recovery.Room.Match;
var brains=(System.Collections.IDictionary)typeof(MatchRuntime).GetField("_bots",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.GetValue(recoveryMatch)!;
var recoveryBrain=brains.Values.Cast<object>().Single();var brainType=recoveryBrain.GetType();
var privateFields=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
brainType.GetField("_lastMotionAt",privateFields)!.SetValue(recoveryBrain,-100d);
brainType.GetField("_pathWorkSinceMotion",privateFields)!.SetValue(recoveryBrain,true);
brainType.GetMethod("FailPath",privateFields)!.Invoke(recoveryBrain,new object[]{recoveryMatch.ElapsedSeconds,settings,true});
Check((double)brainType.GetField("_lastMotionAt",privateFields)!.GetValue(recoveryBrain)! == recoveryMatch.ElapsedSeconds&&
    !(bool)brainType.GetField("_pathWorkSinceMotion",privateFields)!.GetValue(recoveryBrain)!,"recovery rearms the actor deadline and permits new path work");

// Slow real movement accumulates across ticks; it must not be judged against one tick's displacement.
var slow=Fixture();var slowMatch=slow.Room.Match;var slowLane=Lane(slowMatch,3);
Position(slowMatch,slow.Bot,slowLane.A);slowMatch.World!.ApplyEffect(slow.Human.NetId,ItemEffect.Invisible,30);
slowMatch.World.ApplyEffect(slow.Bot.NetId,65000,"Slow",slowMatch.CurrentTick,12,.1f);
var slowBrains=(System.Collections.IDictionary)typeof(MatchRuntime).GetField("_bots",privateFields)!.GetValue(slowMatch)!;
var slowBrain=slowBrains.Values.Cast<object>().Single();
brainType.GetField("_hasGoal",privateFields)!.SetValue(slowBrain,true);
brainType.GetField("_goal",privateFields)!.SetValue(slowBrain,slowLane.B);
brainType.GetField("_directGoal",privateFields)!.SetValue(slowBrain,true);
brainType.GetField("_repathAt",privateFields)!.SetValue(slowBrain,slowMatch.ElapsedSeconds+20);
brainType.GetField("_thinkInitialised",privateFields)!.SetValue(slowBrain,true);
brainType.GetField("_nextThink",privateFields)!.SetValue(slowBrain,uint.MaxValue);
Pump(slow.Manager,210);
Check(Vector2.Distance(slowLane.A,new Vector2(slow.Bot.X,slow.Bot.Y))>1&&
    (double)brainType.GetField("_ignoreUntil",privateFields)!.GetValue(slowBrain)! == 0,"slow moving bot progresses without blacklisting its valid goal");

// Regression: a portal/server displacement may leave a bot inside an otherwise avoided hazard.
foreach(var hazardKind in new[]{WorldObjectKind.Lava,WorldObjectKind.Poison})
{
    var escape=Fixture();var runtime=escape.Room.Match;var layout=runtime.World!.Layout;
    var hazard=layout.Objects.First(o=>o.Kind==hazardKind&&layout.IsWalkable(o.Bounds.Center,escape.Bot.Radius,runtime.World.Destructibles.IsBroken,runtime.World.Hazards.IsDoorClosed));
    Position(runtime,escape.Bot,new Vector2(hazard.Bounds.Center.X,hazard.Bounds.Center.Y));
    runtime.World.ApplyEffect(escape.Human.NetId,ItemEffect.Invisible,30);
    var escaped=false;var sawEscape=false;var exceededSpeed=false;
    for(var t=0;t<90&&escape.Bot.IsAlive&&runtime.Phase==MatchPhase.InGame;t++)
    {
        var previous=new Vector2(escape.Bot.X,escape.Bot.Y);escape.Manager.Tick(1d/30);
        var current=new Vector2(escape.Bot.X,escape.Bot.Y);
        exceededSpeed|=Vector2.Distance(previous,current)>pickProfile.PlayerSpeed/30+.002f;
        sawEscape|=runtime.BotStates.Values.Contains(BotState.EscapeZone);
        escaped=!layout.Objects.Any(o=>(o.Kind==WorldObjectKind.Lava||o.Kind==WorldObjectKind.Poison)&&o.Bounds.Contains(new WorldPoint(current.X,current.Y)));
        if(escaped)break;
    }
    if(!escaped)Console.WriteLine($"Hazard debug {hazardKind} bounds={hazard.Bounds.MinX},{hazard.Bounds.MinY}..{hazard.Bounds.MaxX},{hazard.Bounds.MaxY} actor={escape.Bot.X},{escape.Bot.Y} hp={escape.Bot.Hp} state={string.Join(',',runtime.BotStates.Values)} paths={runtime.PendingBotPaths}");
    Check(sawEscape&&escaped&&escape.Bot.IsAlive,"bot escapes from inside "+hazardKind+" through normal movement");
    Check(!exceededSpeed,"hazard escape does not teleport or exceed movement speed");
}

var navLayout=new MapLayout{Size=10};navLayout.Objects.Add(new WorldObjectDefinition{Id=1,Kind=WorldObjectKind.Wall,Bounds=new WorldBounds(5,2.5f,1,5)});
var grid=new BotNavigationGrid(navLayout);var job=new BotPathSearch(grid,new Vector2(1.25f,2.25f),new Vector2(8.25f,2.25f));
bool NavWalk(Vector2 p)=>navLayout.IsWalkable(new WorldPoint(p.X,p.Y),.4f);
bool NavSegment(Vector2 a,Vector2 b)=>Enumerable.Range(0,21).All(i=>NavWalk(Vector2.Lerp(a,b,i/20f)));
for(var t=0;t<100&&!job.Finished;t++){budget.Reset(7);job.Advance(budget,500,.4f,NavWalk,NavSegment,_=>1);Check(budget.Used<=7,"resumable navigation respects tick slice");}
Check(job.ReachedGoal&&job.Expanded<=500,"bounded A star routes around cover");
Check(job.Path.Zip(job.Path.Skip(1)).All(pair=>NavSegment(pair.First,pair.Second)),"path segments do not cut cover corners");

// One huge centered wall leaves only narrow edge corridors at 0.5-grid resolution and radius .4.
var longLayout=new MapLayout{Size=150};
longLayout.Objects.Add(new WorldObjectDefinition{Id=100,Kind=WorldObjectKind.Wall,Bounds=new WorldBounds(75,74,146,146)});
var longGrid=new BotNavigationGrid(longLayout);
var longStart=new Vector2(1.25f,1.25f);var longGoal=new Vector2(148.75f,1.25f);
var longJob=new BotPathSearch(longGrid,longStart,longGoal);
bool LongWalk(Vector2 p)=>longLayout.IsWalkable(new WorldPoint(p.X,p.Y),.4f);
bool LongSegment(Vector2 a,Vector2 b)=>Enumerable.Range(0,21).All(i=>LongWalk(Vector2.Lerp(a,b,i/20f)));
var longCalls=0;var longFrontierSurvived=false;
while(!longJob.Finished&&longCalls<2000)
{
    budget.Reset(7);longJob.Advance(budget,500,.4f,LongWalk,LongSegment,_=>1);
    Check(budget.Used<=7,"long navigation respects shared tick slice");
    longCalls++;
    if(longJob.Expanded>=500&&!longJob.Finished)longFrontierSurvived=true;
}
Check(longFrontierSurvived,"bounded A star frontier survives more than five hundred expansions");
Check(longJob.ReachedGoal&&longJob.Expanded>500&&longJob.Expanded<BotPathSearch.MaxDiscoveredNodes&&longCalls<=2000,"narrow edge corridor reaches its goal within bounded calls");
Check(longJob.Path.Any(p=>p.Y>147.4f),"narrow corridor route passes above the wall because bottom clearance is blocked");
Check(longJob.DiscoveredNodes<=BotPathSearch.MaxDiscoveredNodes,"long navigation respects discovered node cap");
Check(longJob.Path.Count>1&&longJob.Path.Zip(longJob.Path.Skip(1)).All(pair=>LongSegment(pair.First,pair.Second)),"long route segments stay clear of cover");

// Four overlapping walls form a sealed cage around the goal; search must end with a shorter partial path.
var cageLayout=new MapLayout{Size=150};
cageLayout.Objects.Add(new WorldObjectDefinition{Id=200,Kind=WorldObjectKind.Wall,Bounds=new WorldBounds(75,70,12,1)});
cageLayout.Objects.Add(new WorldObjectDefinition{Id=201,Kind=WorldObjectKind.Wall,Bounds=new WorldBounds(75,80,12,1)});
cageLayout.Objects.Add(new WorldObjectDefinition{Id=202,Kind=WorldObjectKind.Wall,Bounds=new WorldBounds(70,75,1,10)});
cageLayout.Objects.Add(new WorldObjectDefinition{Id=203,Kind=WorldObjectKind.Wall,Bounds=new WorldBounds(80,75,1,10)});
var cageGrid=new BotNavigationGrid(cageLayout);
var cageStart=new Vector2(1.25f,1.25f);var cageGoal=new Vector2(75.25f,75.25f);
var cageJob=new BotPathSearch(cageGrid,cageStart,cageGoal);
bool CageWalk(Vector2 p)=>cageLayout.IsWalkable(new WorldPoint(p.X,p.Y),.4f);
bool CageSegment(Vector2 a,Vector2 b)=>Enumerable.Range(0,21).All(i=>CageWalk(Vector2.Lerp(a,b,i/20f)));
var cageCalls=0;
while(!cageJob.Finished&&cageCalls<3000)
{
    budget.Reset(7);cageJob.Advance(budget,500,.4f,CageWalk,CageSegment,_=>1);
    Check(budget.Used<=7,"sealed navigation respects shared tick slice");
    cageCalls++;
}
Check(cageJob.Finished&&!cageJob.ReachedGoal&&cageCalls<=3000,"sealed goal is not reachable");
Check(cageJob.Expanded<=BotPathSearch.MaxDiscoveredNodes&&cageJob.DiscoveredNodes<=BotPathSearch.MaxDiscoveredNodes,"sealed search obeys discovered node cap");
Check(cageJob.HasPartialPath&&cageJob.Path.Count>0,"sealed search returns a partial path");
var cageInitialDistance=Vector2.Distance(cageStart,cageGoal);
var cageFinalDistance=Vector2.Distance(cageJob.Path[cageJob.Path.Count-1],cageGoal);
Check(cageFinalDistance<cageInitialDistance,"sealed search partial path strictly improves distance to goal");
Check(cageJob.Path.Zip(cageJob.Path.Skip(1)).All(pair=>CageSegment(pair.First,pair.Second)),"sealed partial path segments do not cut cover");

// A bot should start along a verified partial route before the global node cap is reached.
var earlyPartial=new BotPathSearch(cageGrid,cageStart,cageGoal);
budget.Reset(120);earlyPartial.Advance(budget,500,.4f,CageWalk,CageSegment,_=>1);
Check(!earlyPartial.Finished&&earlyPartial.DiscoveredNodes<BotPathSearch.MaxDiscoveredNodes,"partial fixture is still searching below its node cap");
Check(earlyPartial.TryFinishPartial()&&earlyPartial.Path.Count>0&&!earlyPartial.ReachedGoal,"planning deadline can yield a reachable progress route");
Check(Vector2.Distance(earlyPartial.Path[^1],cageGoal)<cageInitialDistance&&earlyPartial.Path.Zip(earlyPartial.Path.Skip(1)).All(pair=>CageSegment(pair.First,pair.Second)),"early partial route progresses without crossing cover");
var deferredPartial=new BotPathSearch(grid,new Vector2(1.25f,2.25f),new Vector2(8.25f,2.25f));
budget.Reset(0);deferredPartial.Advance(budget,500,.4f,NavWalk,NavSegment,_=>1);
Check(!deferredPartial.TryFinishPartial()&&!deferredPartial.Finished,"no-budget planning keeps its frontier and does not invent movement");
for(var t=0;t<100&&!deferredPartial.Finished;t++){budget.Reset(7);deferredPartial.Advance(budget,500,.4f,NavWalk,NavSegment,_=>1);}
Check(deferredPartial.ReachedGoal,"deferred planning resumes after navigation budget returns");

foreach (var invalidMs in new[] { 0, 11 })
{
    var rejected = false;
    try { BotSettings.Load("{\"GlobalPathMillisecondsPerTick\":" + invalidMs + "}"); }
    catch (InvalidDataException) { rejected = true; }
    Check(rejected, "invalid production navigation wall-time budget rejected");
}

// A time slice can expire during endpoint selection, not only during node expansion.
var sliced = new BotPathSearch(grid,new Vector2(1.25f,2.25f),new Vector2(8.25f,2.25f));
var probes = 0;
budget.Reset(500, 1);
sliced.Advance(budget,500,.4f,p=>{probes++;Thread.Sleep(2);return NavWalk(p);},NavSegment,_=>1);
Check(!sliced.Finished && sliced.Expanded == 0 && probes <= 1,"endpoint preparation yields when wall-time slice expires");
for(var t=0;t<100&&!sliced.Finished;t++){budget.Reset(7);sliced.Advance(budget,500,.4f,NavWalk,NavSegment,_=>1);}
Check(sliced.ReachedGoal && sliced.Path.SequenceEqual(job.Path),"time-sliced endpoints preserve deterministic path/frontier");
budget.Reset(3,1);Thread.Sleep(3);
Check(!budget.CanWork&&!budget.Take()&&budget.Used==0,"expired time budget cannot consume a node");

// Fake clock only in this fixture; all decisions, combat and pickups are the production bot brain.
{
    var fixture=Fixture();var match=fixture.Room.Match;var supplyWorld=match.World!;
    typeof(MatchRuntime).GetField("_matchElapsedSeconds",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.SetValue(match,110d);
    fixture.Manager.Tick(1d/30);
    var drop=supplyWorld.Airdrops!.Runtime.States[0];
    Check(drop.Phase==JungleMaze.World.Airdrops.AirdropPhase.Flying,"bot supply fixture announces a real scheduled drop");
    var safeHuman=false;
    for(float y=2;y<supplyWorld.Layout.Size-2&&!safeHuman;y++)for(float x=2;x<supplyWorld.Layout.Size-2&&!safeHuman;x++)
        if(Safe(match,new Vector2(x,y))&&Vector2.Distance(new Vector2(x,y),new Vector2(drop.Position.X,drop.Position.Y))>16)
        {fixture.Human.X=x;fixture.Human.Y=y;safeHuman=true;}
    Check(safeHuman,"human stays safe outside bot perception for supply fixture");
    for(var i=0;i<330&&supplyWorld.Airdrops.Runtime.States[0].Phase!=JungleMaze.World.Airdrops.AirdropPhase.Landed;i++)fixture.Manager.Tick(1d/30);
    drop=supplyWorld.Airdrops.Runtime.States[0];Check(drop.Phase==JungleMaze.World.Airdrops.AirdropPhase.Landed,"bot target egg lands");
    supplyWorld.Floor.Clear();var egg=supplyWorld.Layout.Objects.Single(o=>o.Id==drop.WorldId);var placed=false;
    for(var i=0;i<4;i++){var a=i*MathF.PI/2;var point=new Vector2(drop.Position.X-MathF.Cos(a)*1.45f,drop.Position.Y-MathF.Sin(a)*1.45f);if(!Safe(match,point))continue;fixture.Bot.X=point.X;fixture.Bot.Y=point.Y;placed=true;break;}
    Check(placed,"bot has an accessible egg approach");supplyWorld.SyncActors();
    var hpChanges=0;supplyWorld.Destructibles.Changed+=state=>{if(state.Id==drop.WorldId&&state.Hp<150)hpChanges++;};
    for(var i=0;i<600&&!supplyWorld.Destructibles.IsBroken(drop.WorldId);i++)fixture.Manager.Tick(1d/30);
    Check(supplyWorld.Destructibles.IsBroken(drop.WorldId)&&hpChanges>0,"bot destroys dynamic egg through real combat intents");
    for(var i=0;i<300;i++)fixture.Manager.Tick(1d/30);
    var inventory=supplyWorld.Inventory.GetSnapshot(fixture.Bot.NetId);
    Check(inventory.Weapons.Any(w=>w.ItemId=="MACE")||inventory.Consumables.Any(w=>w.ItemId=="COMPASS"),"bot picks up scheduled supply loot through inventory");
}
if(args.Contains("--stress"))
{
    int StressOption(string name,int fallback,int max){var index=Array.IndexOf(args,name);if(index<0)return fallback;if(index+1>=args.Length||!int.TryParse(args[index+1],out var value)||value<1||value>max)throw new ArgumentException(name);return value;}
    var stressRooms=StressOption("--stress-rooms",5,5);var stressSeconds=StressOption("--stress-seconds",60,600);
    var stressProfile=new MatchProfile{Version=main.Version,MinPlayers=6,LobbyDurationSeconds=.1,LoadingTimeoutSeconds=1,ZoneTimeScale=1,WorldEnabled=true};
    var stressCatalog=args.Contains("--authored")?new MapCatalogStore(Path.Combine(AppContext.BaseDirectory,"GameData","Maps"),"world-prototype-1"):maps;
    var stress=new RoomManager(new ServerOptions(IPAddress.Loopback,7777,10,30,5,"RoomCode","BotsTest",Rooms:new RoomOptions(MaxTotalPlayers:50)),profile:stressProfile,catalog:stressCatalog);var hosted=new List<(RoomManager.Room Room,string Human)>();
    for(var i=0;i<stressRooms;i++)
    {
        RoomManager.Room r;
        if(i==0)r=stress.AllRooms.Single();
        else{var created=stress.Create(new CreateRoomCommand{OwnerId="stress"+i,Name="stress"+i,MaxPlayers=10});Check(created.Status==201,"create isolated stress room");r=stress.AllRooms.Single(x=>x.Id==((RoomView)created.Body!).Id);}
        var id="stress"+i;Human(stress,r,id,i+1);
        if(i%2==1)
        {
            var other=stressCatalog.Current.Maps.Values.Last();var state=stress.GetMapState(r);
            Check(stress.SelectMap(r,id,new SelectRoomMapPacket{RoomId=r.Id,SelectionRevision=state.SelectionRevision,MapId=other.Entry.Id,MapHash=other.Entry.MapHash})==RoomMapError.None,"stress alternate room map");
            state=stress.GetMapState(r);stress.AcknowledgeMap(r,id,new RoomMapReadyPacket{RoomId=r.Id,SelectionRevision=state.SelectionRevision,MapHash=state.MapHash,RulesHash=state.RulesHash});
        }
        for(var b=0;b<9;b++)Check(stress.ManageBots(r,id,Command(r,stress,RoomBotAction.Add))==RoomBotError.None,"stress bot slot");
        if(args.Contains("--diagnose"))
            typeof(MatchRuntime).GetProperty(nameof(MatchRuntime.MatchId))!.SetValue(r.Match,
                i%2==0?new Guid("fb9db6ce-2f53-4e27-8fef-452abc13f29b"):new Guid("48d7f49a-e8b5-45b0-809c-0553ee6f1c39"));
        hosted.Add((r,id));
    }
    Check(stress.ReservedPeers==stressRooms*10&&stress.BotCount==stressRooms*9,"five rooms reserve exactly fifty human and bot slots");
    Console.WriteLine("Stress catalog: "+string.Join(",",stressCatalog.Current.Maps.Values.Select(m=>m.Entry.Id)));
    var metrics=new ServerMetrics();foreach(var entry in hosted)typeof(MatchRuntime).GetProperty("PerformanceMetrics",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.SetValue(entry.Room.Match,metrics);var maxNodes=0;using var stop=new CancellationTokenSource(TimeSpan.FromSeconds(stressSeconds));
    var diagnose=args.Contains("--diagnose");
    var diagnostics=new Dictionary<(string Room,Guid Match,byte Id),(double Alive,float Travel,string State,bool Goal,int Expanded,float Speed,bool Using,bool Finished,bool Partial,int PathCount)>();
    var observed=new Dictionary<(string Room,Guid Match,byte Id),Vector2>();var moved=new HashSet<(string Room,Guid Match,byte Id)>();
    var loop=new FixedTickLoop(30,metrics);
    loop.Run(()=>
    {
        foreach(var entry in hosted)
        {
            var m=entry.Room.Match;
            if(m.Phase==MatchPhase.Waiting)stress.StartMatch(entry.Room,entry.Human,new StartRoomMatchPacket{RoomId=entry.Room.Id,SelectionRevision=stress.GetMapState(entry.Room).SelectionRevision});
            if(m.Phase==MatchPhase.Loading)m.SetPlayerLoaded(entry.Human,m.MatchId);
            if(m.Phase==MatchPhase.Ending)m.ProcessReplayAck(entry.Human,m.MatchId);
        }
    },dt=>{stress.Tick(dt);
        foreach(var entry in hosted.Where(e=>e.Room.Match.Phase==MatchPhase.InGame))foreach(var actor in entry.Room.Match.Entities.All.Where(a=>a.PlayerSessionId.StartsWith("bot:")))
        {
            var key=(entry.Room.Match.RoomId,entry.Room.Match.MatchId,actor.NetId);var point=new Vector2(actor.X,actor.Y);
            if(!observed.TryGetValue(key,out var initial)){observed[key]=point;initial=point;}
            else if(Vector2.Distance(initial,point)>.5f)moved.Add(key);
            if(diagnose)
            {
                diagnostics.TryGetValue(key,out var data);
                var state=data.State;var goal=data.Goal;var expanded=data.Expanded;var speed=1f;var usingItem=false;var finished=false;var partial=false;var pathCount=0;
                if(entry.Room.Match.CurrentTick%30==0)
                {
                    var roster=entry.Room.Match.GetRosterPacket().Players!.First(p=>p.NetId==actor.NetId);
                    var brains=(System.Collections.IDictionary)typeof(MatchRuntime).GetField("_bots",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.GetValue(entry.Room.Match)!;
                    var brain=brains[roster.BotId]!;var type=brain.GetType();
                    state=type.GetProperty("State")!.GetValue(brain)!.ToString()!;
                    goal=(bool)type.GetField("_hasGoal",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.GetValue(brain)!;
                    var search=(BotPathSearch?)type.GetField("_search",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.GetValue(brain);
                    expanded=search?.Expanded??0;
                    if(actor.NetId==3&&entry.Room==hosted[0].Room&&entry.Room.Match.CurrentTick%180==0)
                    {
                        object? Field(string name)=>type.GetField(name,System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.GetValue(brain);
                        Console.WriteLine("[bot-trace] "+System.Text.Json.JsonSerializer.Serialize(new{t=entry.Room.Match.ElapsedSeconds,x=actor.X,y=actor.Y,state,goal=Field("_goal")!.ToString(),lastMotion=Field("_lastMotionAt"),work=Field("_pathWorkSinceMotion"),ignore=Field("_ignoredGoal")?.ToString(),until=Field("_ignoreUntil"),repath=Field("_repathAt"),expanded,startWalkable=entry.Room.Match.SelectedMap!.Navigation.WalkableWorld(entry.Room.Match.World!.Layout,point,actor.Radius,entry.Room.Match.World.Destructibles.IsBroken,entry.Room.Match.World.Hazards.IsDoorClosed)}));
                    }

                    speed=entry.Room.Match.World!.SpeedMultiplier(actor.NetId);usingItem=entry.Room.Match.World.Using(actor.NetId);
                    finished=search?.Finished??false;partial=search?.HasPartialPath??false;pathCount=search?.Path.Count??0;
                }
                diagnostics[key]=(data.Alive+(actor.IsAlive?dt:0),Math.Max(data.Travel,Vector2.Distance(initial,point)),state,goal,expanded,speed,usingItem,finished,partial,pathCount);
            }
        }
        maxNodes=Math.Max(maxNodes,stress.BotPathNodes);metrics.RecordBotWork(stress.BotPathNodes,stress.PendingBotPaths);},
        ()=>metrics.ReportIfDue(stressRooms,stressRooms,stressRooms,0,stress.Count,stress.Occupancy(),stress.BotCount),stop.Token);
    if(diagnose)
    {
        var data=diagnostics.Select(p=>new{p.Key.Room,p.Key.Match,p.Key.Id,aliveSeconds=p.Value.Alive,maxDisplacement=p.Value.Travel,state=p.Value.State,hasGoal=p.Value.Goal,expanded=p.Value.Expanded,speed=p.Value.Speed,usingItem=p.Value.Using,searchFinished=p.Value.Finished,partialPath=p.Value.Partial,pathCount=p.Value.PathCount}).ToArray();
        File.WriteAllText(".utmp/bot-immobile-diagnostics.json",System.Text.Json.JsonSerializer.Serialize(data,new System.Text.Json.JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine("Stationary lives >=15s: "+data.Count(p=>p.aliveSeconds>=15&&p.maxDisplacement<.5f));
        Check(data.All(p=>p.aliveSeconds<15||p.maxDisplacement>=.5f),"every bot alive fifteen seconds moves after spawn; aggregate movement cannot mask a stationary bot");
    }
    Console.WriteLine($"Movement observed: {moved.Count}/{observed.Count} bot lives moved more than half a cell");
    Check(moved.Count>=stressRooms*8,"at least eight bots per authored room actually move during stress");
    Check(maxNodes<=1500&&stress.ReservedPeers==stressRooms*10,"shared navigation budget and slots remain bounded during five-room stress");
}
Console.WriteLine($"BotChecks: {checks} passed, 0 failed");
