using JungleMaze.World;
using JungleMaze.World.Airdrops;
namespace JungleMaze.Tests.Airdrops;
public static class PlacementChecks
{
    sealed class Loot : ITransactionalDestructibleLootSink
    {
        public int Spawns;
        public bool Capacity = true;
        public bool CanSpawnLoot(WorldObjectKind kind, ushort id, uint seed) => Capacity;
        public void SpawnLoot(WorldObjectKind kind, ushort id, WorldPoint point, uint seed) => Spawns++;
    }
    public static (int Passed, int Failed) Run()
    {
        int passed = 0, failed = 0;
        void Check(bool value, string message) { if(value)passed++;else failed++; Console.WriteLine((value?"[PASS] ":"[FAIL] ")+message); }
        var source = new MapLayout { Size = 50, Hash = "authored-hash" };
        source.Spawns.Add(new WorldPoint(20,20));
        var map = AirdropController.CopyLayout(source);
        var sink = new Loot();
        var damage = new DestructibleService(map,new DestructibleConfig(),sink);
        ushort id = 100;
        var config = new AirdropSettings { Drops = new[]{new AirdropDropSettings{Id=1,FlyAt=1,LandAt=2}} };
        var host = new AirdropController(config,9,map,damage,new[]{new WorldPoint(25,25)},
            _=>new AirdropZone(25,25,25),()=>id++,null);
        host.Tick(1);
        Check(host.Runtime.States[0].Phase==AirdropPhase.Flying && map.Objects.Count==0,"airborne egg has no collision");
        host.Tick(2);
        var egg = host.Runtime.States[0].WorldId;
        Check(egg==100 && map.Objects.Count==1,"landed egg uses reserved world id");
        Check(damage.TryGet(egg,out var state)&&state.Hp==150,"dynamic egg starts at150HP");
        Check(source.Objects.Count==0&&source.Hash==map.Hash,"authored map and hash remain unchanged");
        sink.Capacity=false;
        Check(!damage.DamageConfirmed(egg,150,0,1)&&!damage.IsBroken(egg),"full loot registry preserves egg for retry");
        sink.Capacity=true;
        Check(damage.DamageConfirmed(egg,150,0,1)&&sink.Spawns==1,"retry succeeds and produces loot once");
        Check(!damage.DamageConfirmed(egg,150,0,1)&&sink.Spawns==1,"duplicate hit never produces duplicate loot");
        host.Tick(3);
        Check(host.Runtime.States[0].Phase==AirdropPhase.Broken && host.ReservedLootRemaining==0,"broken egg releases reservation");
        var blockedMap=AirdropController.CopyLayout(source);
        blockedMap.Objects.Add(new WorldObjectDefinition{Id=1,Kind=WorldObjectKind.Lava,Bounds=new WorldBounds(25,25,2,2)});
        var blocked=new AirdropController(config,9,blockedMap,new DestructibleService(blockedMap,new DestructibleConfig(),sink),
            new[]{new WorldPoint(25,25)},_=>new AirdropZone(25,25,25),()=>id++,null);
        blocked.Tick(10);
        Check(blocked.Runtime.States[0].Phase==AirdropPhase.Cancelled,"hazard-overlapping candidate cancels bounded drop");
        var outside=new AirdropController(config,9,AirdropController.CopyLayout(source),new DestructibleService(source,new DestructibleConfig(),sink),
            new[]{new WorldPoint(48,25)},_=>new AirdropZone(25,25,25),()=>id++,null);
        outside.Tick(10);
        Check(outside.Runtime.States[0].Phase==AirdropPhase.Cancelled,"full footprint must remain3cells inside zone");
        var blastMap = new MapLayout { Size = 50 };
        blastMap.Objects.Add(new WorldObjectDefinition { Id = 10, Kind = WorldObjectKind.Supply,
            Bounds = new WorldBounds(10,10,2,2), Hp = 150 });
        blastMap.Objects.Add(new WorldObjectDefinition { Id = 11, Kind = WorldObjectKind.Wall,
            Bounds = new WorldBounds(7,10,1,4) });
        var blastLoot = new Loot();
        var blastDamage = new DestructibleService(blastMap,new DestructibleConfig(),blastLoot);
        blastDamage.DamageContainersInBlast(new WorldBounds(2,2,1,1),150,0,1,_=>false);
        Check(blastDamage.TryGet(10,out var missed)&&missed.Hp==150,"blast outside egg footprint cannot damage it");
        blastDamage.DamageContainersInBlast(new WorldBounds(5,10,14,8),150,0,2,_=>false);
        Check(blastDamage.TryGet(10,out var covered)&&covered.Hp==150,"hard cover blocks blast damage to supply egg");
        blastMap.Objects.RemoveAt(1);
        blastDamage.DamageContainersInBlast(new WorldBounds(5,10,14,8),150,0,3,_=>false);
        Check(blastDamage.IsBroken(10)&&blastLoot.Spawns==1,"exposed supply egg breaks through shared explosion damage");
        blastDamage.DamageContainersInBlast(new WorldBounds(5,10,14,8),150,0,3,_=>false);
        Check(blastLoot.Spawns==1,"replayed explosion cannot duplicate egg loot");
        return(passed,failed);
    }
}
