using GameServer.Simulation;
using JungleBackend.Protocol.Gameplay;
using LiteNetLib.Utils;
namespace GameServer.Networking;

public sealed partial class SessionHub
{
    private void SetupWorldSubscriptions()
    {
        _frameValidator.Subscribe<WorldActionRequestPacket>(_=>{});
        _frameValidator.Subscribe<ThrowableRequestPacket>(_=>{});
        // WPN-03: thrown and special weapons. Duplicates are rejected by request id inside ThrowableService.
        _gameplayRegistry.Subscribe<ThrowableRequestPacket,Session>((request,session)=>
        {
            if(session.PlayerId==null||!session.WorldEnabled||request.MatchId!=session.Room.Match.MatchId)return;
            if(session.Room.Match.ProcessThrowable(session.PlayerId,request)==JungleMaze.World.Throwables.ThrowableResult.Ok)
                SendWorldState(session,false);
        });
        _gameplayRegistry.Subscribe<WorldActionRequestPacket,Session>((request,session)=>
        {
            if(session.PlayerId==null||request.MatchId!=session.Room.Match.MatchId)return;
            if(session.WorldRequestMatch!=request.MatchId)
            { session.WorldRequests.Clear(); session.WorldRequestMatch=request.MatchId; }
            if(session.WorldRequests.Count>=8)
            { ReplyWorldAction(session,request,false,"Busy"); return; }
            session.WorldRequests.Enqueue(request);
        });
    }
    private void ReplyWorldAction(Session session,WorldActionRequestPacket request,bool accepted,string reason)
    {
        var result=new WorldActionResultPacket{MatchId=request.MatchId,RequestId=request.RequestId,Accepted=accepted,Reason=reason};
        SendWorldPacket(session,ref result);
    }
    private void TickWorldRequests()
    {
        foreach(var session in _sessions.Values)
        {
            if(session.WorldRequests.Count==0||session.PlayerId==null)continue;
            var request=session.WorldRequests.Dequeue();
            if(request.MatchId!=session.Room.Match.MatchId)
            { session.WorldRequests.Clear(); continue; }
            var resync=request.Action==WorldActionKind.Resync;
            if(resync&&session.WorldEnabled&&session.Room.Match.CurrentTick-session.LastWorldResyncTick<30)
            { ReplyWorldAction(session,request,false,"Busy"); continue; }
            if(!resync&&!session.WorldEnabled)
            { ReplyWorldAction(session,request,false,"WorldDisabled"); continue; }
            // Process one ordered action after movement, using the authoritative current position.
            var accepted=session.Room.Match.ProcessWorldRequest(session.PlayerId,request);
            if(resync&&accepted)
            { session.WorldEnabled=true; session.LastWorldResyncTick=session.Room.Match.CurrentTick; }
            ReplyWorldAction(session,request,accepted,accepted?"Ok":session.Room.Match.World?.LastResult.ToString()??"WorldDisabled");
            if(accepted)SendWorldState(session,resync);
        }
    }
    private void TickWorldStreams()
    {
        foreach(var session in _sessions.Values)
            if(session.WorldEnabled&&session.PlayerId!=null&&session.Room.Match.World!=null)
            {
                var match=session.Room.Match;
                if(!match.Entities.TryGetBySession(session.PlayerId,out var actor)||actor==null)continue;
                var key=match.World.OwnerEffectKey(actor.NetId);
                if(match.CurrentTick-session.LastWorldInventoryTick>=15||key!=session.LastWorldEffectKey)
                {SendWorldState(session,false);session.LastWorldEffectKey=key;}
            }
    }
    private void SendWorldPacket<T>(Session session,ref T packet) where T:struct,INetSerializable
    {
        var writer=new NetDataWriter();_gameplayRegistry.WriteFrame(writer,ref packet);
        session.Peer.TrySend(writer.CopyData(),true,GameplayProtocol.GameplayChannel);
    }
    private readonly Dictionary<string,(Guid Match,uint Revision,List<byte[]> Packets)> _worldPackets=new();
    private List<byte[]> WorldPackets(MatchRuntime match,MatchWorldRuntime world)
    {
        if(_worldPackets.TryGetValue(match.RoomId,out var cached)&&cached.Match==match.MatchId&&cached.Revision==world.Revision)return cached.Packets;
        if(_worldPackets.Count>=16)
            foreach(var id in _worldPackets.Keys.Where(id=>!Rooms.AllRooms.Any(r=>r.Id.ToString()==id)).ToArray())_worldPackets.Remove(id);
        var packets=new List<byte[]>();
        void Add<T>(ref T packet) where T:struct,INetSerializable
        { var writer=new NetDataWriter();_gameplayRegistry.WriteFrame(writer,ref packet);packets.Add(writer.CopyData()); }
        var manifest=new WorldManifestPacket{MatchId=match.MatchId,Seed=world.Layout.Seed,Version=world.Layout.Version,Hash=world.Layout.Hash,Size=(ushort)world.Layout.Size,Revision=world.Revision};
        Add(ref manifest);
        var all=world.SnapshotObjects();var pages=Math.Max(1,(all.Count+WorldWire.PageSize-1)/WorldWire.PageSize);
        for(var page=0;page<pages;page++)
        {
            var count=Math.Min(WorldWire.PageSize,all.Count-page*WorldWire.PageSize);
            var packet=new WorldObjectStatePacket{MatchId=match.MatchId,Revision=world.Revision,Tick=match.CurrentTick,PageIndex=(ushort)page,PageCount=(ushort)pages,TotalObjects=(ushort)all.Count,Objects=all.GetRange(page*WorldWire.PageSize,count).ToArray()};
            Add(ref packet);
        }
        _worldPackets[match.RoomId]=(match.MatchId,world.Revision,packets);return packets;
    }
    private void SendWorldState(Session session,bool force)
    {
        var match=session.Room.Match;var world=match.World;
        if(world==null||session.PlayerId==null||!match.Entities.TryGetBySession(session.PlayerId,out var actor)||actor==null)return;
        if(force&&world.Airdrops!=null)
            foreach(var state in world.Airdrops.Runtime.States)
            {var drop=match.AirdropPacket(state,true);SendWorldPacket(session,ref drop);}
        var inventory=world.SnapshotInventory(actor.NetId);
        SendWorldPacket(session,ref inventory);
        if(force||((session.LastWorldRevision!=world.Revision||session.LastWorldMatch!=match.MatchId)&&match.CurrentTick-session.LastWorldSyncTick>=15))
        {
            var cache=WorldPackets(match,world);
            foreach(var bytes in cache)session.Peer.TrySend(bytes,true,GameplayProtocol.GameplayChannel);
            session.LastWorldRevision=world.Revision;session.LastWorldMatch=match.MatchId;
            session.LastWorldSyncTick=match.CurrentTick;
        }
        session.LastWorldInventoryTick=match.CurrentTick;
        session.LastWorldEffectKey=world.OwnerEffectKey(actor.NetId);
    }
}
