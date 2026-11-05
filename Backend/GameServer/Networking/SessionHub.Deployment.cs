using JungleBackend.Protocol.Gameplay;
using GameServer.Simulation;
using JungleBackend.Rooms;

namespace GameServer.Networking;

public sealed record DeploymentRoomStatus(Guid RoomId, string Phase, int ReservedHumans, int Bots, int Entities);
public sealed record DeploymentStatus(int SchemaVersion, bool SafeToDeploy, int HumanActive, int HumanPending,
    int HumanGrace, int Bots, DeploymentRoomStatus[] Rooms);

public sealed partial class SessionHub
{
    // Called only through RoomCommands: HTTP never enumerates mutable sessions itself.
    public DeploymentStatus GetDeploymentStatus()
    {
        var rooms = Rooms.AllRooms.Select(room => new DeploymentRoomStatus(room.Id,
            room.Match.Phase.ToString(), room.Peers.Count, room.Match.BotCount, room.Match.Entities.All.Count())).ToArray();
        var active = ActivePeers;
        var pending = _sessions.Count - active;
        var grace = _heldSessions.Count;
        var safe = active + pending + grace + Rooms.ReservedPeers == 0
            && rooms.All(room => room.Phase == MatchPhase.Waiting.ToString() && room.Entities == 0);
        return new(1, safe, active, pending, grace, Rooms.BotCount, rooms);
    }
}
