using GameServer.Rooms;
using JungleBackend.Protocol.Gameplay;
using LiteNetLib.Utils;

namespace GameServer.Networking;

public sealed partial class SessionHub
{
    private void SetupRoomMapSubscriptions()
    {
        _frameValidator.Subscribe<SelectRoomMapPacket>(_ => { });
        _frameValidator.Subscribe<RoomMapReadyPacket>(_ => { });
        _frameValidator.Subscribe<StartRoomMatchPacket>(_ => { });
        _gameplayRegistry.Subscribe<SelectRoomMapPacket, Session>((p, s) =>
        {
            if (s.PlayerId is null) return;
            SendRoomMapResult(s, Rooms.SelectMap(s.Room, s.PlayerId, p));
        });
        _gameplayRegistry.Subscribe<RoomMapReadyPacket, Session>((p, s) =>
        {
            if (s.PlayerId is null) return;
            SendRoomMapResult(s, Rooms.AcknowledgeMap(s.Room, s.PlayerId, p));
        });
        _gameplayRegistry.Subscribe<StartRoomMatchPacket, Session>((p, s) =>
        {
            if (s.PlayerId is null) return;
            SendRoomMapResult(s, Rooms.StartMatch(s.Room, s.PlayerId, p));
        });
        Rooms.MapStateChanged += BroadcastRoomMapState;
    }

    private void SendRoomMapResult(Session session, RoomMapError error)
    {
        var result = new RoomMapCommandResultPacket { RoomId = session.Room.Id, Error = error };
        SendWorldPacket(session, ref result);
    }

    private void BroadcastRoomMapState(RoomManager.Room room)
    {
        if (!room.Match.RequiresManualStart) return;
        var state = Rooms.GetMapState(room);
        var writer = new NetDataWriter();
        _gameplayRegistry.WriteFrame(writer, ref state);
        var bytes = writer.CopyData();
        foreach (var session in _sessions.Values)
            if (session.Room == room && session.PlayerId is not null)
                session.Peer.TrySend(bytes, true, GameplayProtocol.GameplayChannel);
    }
}
