using GameServer.Maps;
using GameServer.Simulation;
using JungleBackend.Protocol.Gameplay;
using JungleMaze.World;
using JungleMaze.World.Airdrops;
using LiteNetLib.Utils;
namespace JungleMaze.Tests.Airdrops;
internal static class ServerChecks
{
    public static void Run(Action<bool,string> check)
    {
        var store=new MapCatalogStore(Path.Combine(AppContext.BaseDirectory,"GameData","Maps"),GameDataLoader.RulesHash);
        var setup=MatchWorldSetup.Load();
        var schedule=ZoneSchedule.Load(GameDataLoader.ReadGameDataFile("zone_schedule.json"));
        foreach(var definition in store.Current.Maps.Values)
        {
            var original=definition.Layout.Objects.Count;
            var profile=new MatchProfile{Version=schedule.Version,MinPlayers=2,LobbyDurationSeconds=.01,WorldEnabled=true};
            var match=new MatchRuntime("test",definition.Data,schedule.ForMap(definition.Data),profile,selectedMap:definition,worldSetup:setup);
            match.TryRegisterPlayer("a","A",out _);match.TryRegisterPlayer("b","B",out _);match.SetPlayerReady("a");match.SetPlayerReady("b");
            match.StartFromRoom();for(var i=0;i<4;i++)match.Tick(1d/30);
            match.SetPlayerLoaded("a",match.MatchId);match.SetPlayerLoaded("b",match.MatchId);match.Tick(1d/30);
            check(match.Phase==MatchPhase.InGame&&match.World?.Airdrops!=null,definition.Entry.Id+" starts airdrop world");
            var world=match.World!;var drops=world.Airdrops!;
            check(drops.ReservedLootRemaining==24,"24 supply stacks reserved");
            uint action=1;
            foreach(var time in new[]{120f,240f,330f})
            {
                world.Tick((uint)(time*30),time);
                var state=drops.Runtime.States[(int)action-1];
                check(state.Phase==AirdropPhase.Landed,definition.Entry.Id+" landed at "+time);
                check(world.Layout.Objects.Count==original+action&&definition.Layout.Objects.Count==original,"dynamic egg does not mutate cached map");
                var egg=world.Layout.Objects.Single(o=>o.Id==state.WorldId);
                check(egg.Hp==150&&egg.Bounds.MaxX-egg.Bounds.MinX==2,"egg footprint and HP");
                var before=world.Floor.Items.Select(i=>i.Id).ToHashSet();
                check(world.Destructibles.DamageConfirmed(egg.Id,149,0,action*2),"nonlethal hit confirmed");
                check(world.Destructibles.TryGet(egg.Id,out var damaged)&&damaged.Hp==1,"149 hit leaves 1 HP");
                check(world.Destructibles.DamageConfirmed(egg.Id,1,0,action*2+1),"lethal hit confirmed");
                var loot=world.Floor.Items.Where(i=>!before.Contains(i.Id)).ToArray();
                check(loot.Length>=6&&loot.Length<=8&&loot.Select(i=>i.Stack.ItemId).Distinct().Count()==loot.Length,"6-8 unique physical stacks");
                check(loot.Any(i=>i.Stack.ItemId=="MACE")&&loot.Any(i=>i.Stack.ItemId=="COMPASS"),"guaranteed mace and compass");
                check(!world.Destructibles.DamageConfirmed(egg.Id,150,1,999)&&world.Floor.Items.Count==before.Count+loot.Length,"simultaneous duplicate cannot spawn again");
                world.Tick((uint)(time*30+1),time+.04);
                check(drops.Runtime.States[(int)action-1].Phase==AirdropPhase.Broken,"broken phase follows loot commit");
                var packet=match.AirdropPacket(drops.Runtime.States[(int)action-1],true);
                AirdropStatePacket received=default;var registry=new PacketRegistry();registry.Subscribe<AirdropStatePacket>(p=>received=p);
                var writer=new NetDataWriter();registry.WriteFrame(writer,ref packet);
                check(registry.TryReadFrame(new NetDataReader(writer.CopyData()))&&received.Baseline&&received.WorldId==egg.Id&&received.MatchId==match.MatchId,"resume baseline packet roundtrip");
                for(var length=1;length<writer.Length;length++)
                    if(registry.TryReadFrame(new NetDataReader(writer.CopyData().Take(length).ToArray())))throw new Exception("Truncated airdrop accepted");
                action++;
            }
            check(drops.ReservedLootRemaining==0,"all three loot reservations released once");
        }
    }
}
