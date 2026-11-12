using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.Json.Nodes;
using GameServer.Configuration;
using GameServer.Diagnostics;
using GameServer.Networking;
using JungleBackend.Protocol.Gameplay;
namespace JungleMaze.Tests.Airdrops;
internal static class SocketChecks
{
    public static async Task Run(Action<bool,string> check)
    {
        var saved=new Dictionary<string,string?>();
        void Env(string key,string value){saved.TryAdd(key,Environment.GetEnvironmentVariable(key));Environment.SetEnvironmentVariable(key,value);}
        var temp=Directory.CreateTempSubdirectory("lastaway-airdrop-").FullName;
        var clients=new List<WorldNetworkClient>();
        int Port(SocketType type){using var s=new Socket(AddressFamily.InterNetwork,type,type==SocketType.Stream?ProtocolType.Tcp:ProtocolType.Udp);s.Bind(new IPEndPoint(IPAddress.Loopback,0));return ((IPEndPoint)s.LocalEndPoint!).Port;}
        try
        {
            var data=Path.Combine(AppContext.BaseDirectory,"GameData");
            foreach(var file in Directory.GetFiles(data,"*.json"))File.Copy(file,Path.Combine(temp,Path.GetFileName(file)));
            var document=JsonNode.Parse(File.ReadAllText(Path.Combine(temp,"airdrop_schedule.json")))!;
            for(var i=0;i<3;i++){document["drops"]![i]!["flyAt"]=25+i*50;document["drops"]![i]!["landAt"]=50+i*50;}
            File.WriteAllText(Path.Combine(temp,"airdrop_schedule.json"),document.ToJsonString());
            Env("GAME_DATA_PATH",temp);Env("MAP_DATA_PATH",Path.Combine(data,"Maps"));Env("WORLD_ENABLED","1");Env("MATCH_PROFILE","test");Env("WS_ENABLED","true");Env("WS_BIND_ADDRESS","127.0.0.1");
            var udp=Port(SocketType.Dgram);var tcp=Port(SocketType.Stream);Env("WS_PORT",tcp.ToString());
            using var host=new NetworkHost(new ServerOptions(IPAddress.Loopback,udp,10,30,1,"RoomCode","AIRDROP_CHECK"),new ServerMetrics());host.Start();
            await using var ws=await WebSocketHost.StartFromEnvironment(host.Hub,50);
            var clock=Stopwatch.StartNew();double next=0;
            void Pump(Func<bool> done,int max=15000)
            {
                var until=clock.ElapsedMilliseconds+max;
                while(!done())
                {
                    ws!.PollEvents();host.PollEvents();foreach(var c in clients)c.Poll();
                    while(clock.Elapsed.TotalSeconds>=next){host.Hub.Tick(1d/30);next+=1d/30;}
                    if(clock.ElapsedMilliseconds>until)throw new TimeoutException("Airdrop UDP/WS fixture timeout");Thread.Sleep(1);
                }
            }
            foreach(var transport in new[]{"Udp","Wss"})
            {var c=new WorldNetworkClient(transport);clients.Add(c);await c.ConnectAsync("127.0.0.1",udp,$"ws://127.0.0.1:{tcp}/game","AIRDROP_CHECK");}
            // Keep the landing geometry fixture reproducible across runs.
            typeof(GameServer.Simulation.MatchRuntime).GetProperty("MatchId")!.SetValue(host.Hub.Rooms.Find("AIRDROP_CHECK")!.Match,new Guid(42,0,0,new byte[8]));
            Pump(()=>clients.All(c=>c.Phase==MatchPhase.InGame&&c.SnapshotCount>2));
            var match=host.Hub.Rooms.Find("AIRDROP_CHECK")!.Match;
            foreach(var c in clients){var request=new WorldActionRequestPacket{MatchId=c.MatchId,RequestId=1,Action=WorldActionKind.Resync};c.Send(ref request,true);}
            Pump(()=>clients.All(c=>c.Manifest.HasValue));
            for(ushort drop=1;drop<=3;drop++)
            {
                var id=drop;Pump(()=>clients.All(c=>c.AirdropEvents.Any(p=>p.DropId==id&&p.Phase==3&&!p.Baseline)));
                var states=clients.Select(c=>c.AirdropEvents.Last(p=>p.DropId==id&&p.Phase==3)).ToArray();
                check(states[0].X==states[1].X&&states[0].Y==states[1].Y&&states[0].WorldId==states[1].WorldId&&states[0].LandTick==states[1].LandTick,"UDP/WS same drop geometry and landing tick "+drop);
                check(clients.All(c=>c.AirdropEvents.Count(p=>p.DropId==id&&p.Phase==1&&!p.Baseline)==1),"one reliable flight per transport "+drop);
                var egg=states[0].WorldId;var world=match.World!;var before=world.Floor.Items.Select(f=>f.Id).ToHashSet();
                world.Destructibles.DamageConfirmed(egg,149,0,drop*2u);
                Pump(()=>clients.All(c=>c.WorldObjects.Any(o=>o.Id==egg&&o.Hp==1)));
                check(clients.All(c=>c.WorldObjects.Any(o=>o.Id==egg&&o.Hp==1)),"both clients see confirmed egg HP");
                world.Destructibles.DamageConfirmed(egg,1,0,drop*2u+1);
                Pump(()=>clients.All(c=>c.AirdropEvents.Any(p=>p.DropId==id&&p.Phase==4)&&c.WorldObjects.Any(o=>o.Id==egg&&(o.Flags&1)!=0)));
                var added=world.Floor.Items.Where(f=>!before.Contains(f.Id)).Select(f=>f.Id).ToArray();
                Pump(()=>clients.All(c=>added.All(item=>c.WorldObjects.Any(o=>o.Id==item))));
                check(added.Length>=6&&added.Length<=8,"complete unique loot batch streamed to UDP and WS");
            }
            var count=match.World!.Floor.Items.Count;
            foreach(var c in clients){var request=new WorldActionRequestPacket{MatchId=c.MatchId,RequestId=100,Action=WorldActionKind.Resync};c.Send(ref request,true);}
            Pump(()=>clients.All(c=>c.AirdropEvents.Any(p=>p.DropId==3&&p.Phase==4&&p.Baseline)));
            check(match.World.Floor.Items.Count==count,"resync does not create loot again");
            check(clients.All(c=>c.InvalidFrames==0),"airdrop frames valid across UDP and WS");
        }
        finally
        {
            foreach(var c in clients)await c.DisposeAsync();
            foreach(var entry in saved)Environment.SetEnvironmentVariable(entry.Key,entry.Value);
            if(Path.GetFullPath(temp).StartsWith(Path.GetFullPath(Path.GetTempPath()),StringComparison.OrdinalIgnoreCase))Directory.Delete(temp,true);
        }
    }
}
