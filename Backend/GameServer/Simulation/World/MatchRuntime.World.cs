using JungleBackend.Protocol.Gameplay;
using JungleMaze.World;
using JungleMaze.World.Inventory;
using JungleMaze.World.Throwables;
using System.Text.Json;
using System.Text.Json.Serialization;
namespace GameServer.Simulation;

public sealed partial class MatchRuntime
{
    public MatchWorldRuntime? World {get;private set;}
    private MapData? _worldMap;
    private PhysicsSimulation? _worldPhysics;
    private WorldSettings? _worldSettings;
    private ItemCatalog? _worldCatalog;
    private LootProfile? _worldLootProfile;

    private void ConfigureWorld()
    {
        var mode = Environment.GetEnvironmentVariable("WORLD_ENABLED");
        if (mode != "1" && (mode == "0" || !_profile.WorldEnabled)) return;
        if (SelectedMap is not null && _preparedWorld is not null)
        {
            _worldSettings = _preparedWorld.ForMap(SelectedMap);
            _worldCatalog = _preparedWorld.Catalog;
            _worldLootProfile = _preparedWorld.Loot;
            return;
        }
        _worldSettings = WorldSettings.Load(GameDataLoader.ReadGameDataFile("world_settings.json"));
        if (_worldSettings.LayoutMode == "Authored")
        {
            var mapDocument = JsonSerializer.Deserialize<AuthoredMapDocument>(GameDataLoader.ReadGameDataFile("map_demo.json"),
                new JsonSerializerOptions { IncludeFields = true, PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter() } })
                ?? throw new InvalidDataException("Missing authored map.");
            _worldSettings.Layout = mapDocument.Build(_worldSettings.Version);
        }
        var options = new JsonSerializerOptions { IncludeFields = true, PropertyNameCaseInsensitive = true };
        options.Converters.Add(new JsonStringEnumConverter());
        var document = JsonSerializer.Deserialize<ItemCatalogDocument>(GameDataLoader.ReadGameDataFile("items_catalog.json"), options)
            ?? throw new InvalidDataException("Missing item catalog.");
        _worldCatalog = document.Build();
        _worldLootProfile = document.Loot;
    }

    public bool EnsureWorld(Guid matchId)
    {
        return matchId == MatchId && Phase == MatchPhase.InGame && World != null;
    }

    private void StartWorld()
    {
        if (_worldSettings == null || _worldCatalog == null) return;
        World=new MatchWorldRuntime(MatchId,_worldSettings,this,_health,_worldCatalog,_worldLootProfile);
        World.ThrowableDamage=(attacker,target,amount)=>{World.SetCombatTick(CurrentTick);return _combat.ApplyExternalDamage(attacker,target,amount,CurrentTick)?.Damage??0;};
        World.ThrowableRaised+=BroadcastThrowable;
        _worldMap=new MapData{Version=_map.Version,MinX=0,MinY=0,MaxX=World.Layout.Size,MaxY=World.Layout.Size};
        _worldPhysics=new PhysicsSimulation(_worldMap,_profile.PlayerRadius,_profile.PlayerSpeed);
        var i=0;foreach(var entity in _entities.Values){var spawn=World.Layout.Spawns[i++%World.Layout.Spawns.Count];entity.X=spawn.X;entity.Y=spawn.Y;}
        World.Tick(CurrentTick, _matchElapsedSeconds);RefreshWorldGeometry();
        _combat = CreateCombat();
    }
    public bool ProcessWorldRequest(string sessionId,in WorldActionRequestPacket request)
    {
        if(request.MatchId!=MatchId||Phase!=MatchPhase.InGame||!_bySession.TryGetValue(sessionId,out var actor)||!actor.IsAlive||actor.IsDisconnected)return false;
        if(request.Action==WorldActionKind.Resync)return EnsureWorld(request.MatchId);
        if(World==null)return false;
        return World.Request(actor.NetId,request)==ItemResult.Ok;
    }
    private void TickWorld()
    {
        if(World==null)return;
        World.Tick(CurrentTick,_matchElapsedSeconds);
        World.TickThrowables();
        RefreshWorldGeometry();
    }
    /// <summary>
    /// WPN-03: uses the held thrown or special weapon. The request names the weapon kind so a swap in flight cannot
    /// throw the wrong item; actor identity comes from the session, never from the packet.
    /// </summary>
    public ThrowableResult ProcessThrowable(string sessionId,in ThrowableRequestPacket request)
    {
        if(request.MatchId!=MatchId||Phase!=MatchPhase.InGame||World==null||!_bySession.TryGetValue(sessionId,out var actor)||actor.IsDisconnected)return ThrowableResult.Invalid;
        var degrees=request.Aim*(360f/65536f);
        return World.UseThrowable(actor.NetId,request.RequestId,(ThrowableKind)request.Kind,degrees,request.Distance);
    }
    private void BroadcastThrowable(ThrowableEvent e)
    {
        var packet=new ThrowableEventPacket{MatchId=MatchId,Tick=CurrentTick,Event=(byte)e.Kind,Item=(byte)e.Item,Actor=e.Actor,Target=e.Target,Id=e.Id,
            FromX=e.FromX,FromY=e.FromY,ToX=e.ToX,ToY=e.ToY,Seconds=e.Seconds,Size=e.Size,Amount=e.Amount,Flags=e.Flags};
        BroadcastCombatResult(ref packet);
    }
    private void RefreshWorldGeometry()
    {
        if(World==null||_worldMap==null)return;
        _worldMap.Obstacles.Clear();
        foreach(var o in World.Layout.Objects)
            if(o.IsHardCover||(o.IsDestructible&&!World.Destructibles.IsBroken(o.Id))||(o.Kind==WorldObjectKind.Door&&World.Hazards.IsDoorClosed(o.Id)))
                _worldMap.Obstacles.Add(new AABB(o.Kind+"-"+o.Id,o.Bounds.MinX,o.Bounds.MaxX,o.Bounds.MinY,o.Bounds.MaxY));
    }
    private void ResetWorld(){World=null;_worldMap=null;_worldPhysics=null;}
}
