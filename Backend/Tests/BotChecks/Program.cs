using System.Net;
using GameServer.Configuration;
using GameServer.Maps;
using GameServer.Rooms;
using GameServer.Simulation;
using GameServer.Simulation.Bots;
using JungleBackend.Protocol.Gameplay;

var checks = 0;
void Check(bool ok, string message) { if (!ok) throw new Exception("FAIL: " + message); Console.WriteLine("PASS: " + message); checks++; }
Environment.SetEnvironmentVariable("WORLD_ENABLED", "1");
var settings = BotSettings.Load(GameDataLoader.ReadGameDataFile("bot_settings.json"));
Check(settings.ThinkHz == 5 && settings.ReactionSeconds >= .35, "versioned fair bot settings");
var maps = new MapCatalogStore(Path.Combine(AppContext.BaseDirectory,"GameData","Maps"),"world-prototype-1");
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
var main = MatchProfile.Current;
var mainRooms=Manager(main);var mainRoom=mainRooms.AllRooms.Single();Human(mainRooms,mainRoom,"main",1);
Check(mainRooms.ManageBots(mainRoom,"main",Command(mainRoom,mainRooms,RoomBotAction.FillMinimum))==RoomBotError.None&&mainRoom.Match.BotCount==main.MinPlayers-1,"fill minimum follows profile");
Check(mainRooms.GetMapState(mainRoom).CanStart,"bots satisfy minimum only after human map verification");
var budget=new BotNavigationBudget();budget.Reset(3);
Check(budget.Take()&&budget.Take()&&budget.Take()&&!budget.Take()&&budget.Used==3,"global navigation budget is bounded");
Console.WriteLine($"BotChecks: {checks} passed, 0 failed");
