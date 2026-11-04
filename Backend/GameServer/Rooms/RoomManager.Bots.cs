using JungleBackend.Protocol.Gameplay;

namespace GameServer.Rooms;

public sealed partial class RoomManager
{
    public RoomBotError ManageBots(Room room, string playerId, in RoomBotCommandPacket request)
    {
        if (!IsHost(room, playerId)) return RoomBotError.NotHost;
        if (request.RoomId != room.Id || request.RequestId == 0 || request.Action > RoomBotAction.FillMinimum) return RoomBotError.InvalidRequest;
        if (room.BotRequests.TryGetValue(playerId, out var previous))
        {
            if (previous.RequestId == request.RequestId) return previous.Error;
            if (unchecked((int)(request.RequestId - previous.RequestId)) <= 0) return RoomBotError.InvalidRequest;
        }
        var error = ApplyBotCommand(room, request);
        room.BotRequests[playerId] = (request.RequestId, error);
        return error;
    }

    private RoomBotError ApplyBotCommand(Room room, in RoomBotCommandPacket request)
    {
        if (room.Match.Phase != MatchPhase.Waiting) return RoomBotError.MatchLocked;
        if (request.Revision != room.LobbyRevision) return RoomBotError.StaleRoom;
        if (request.Action == RoomBotAction.Remove)
        {
            if (!room.Match.RemoveBot(request.BotId)) return RoomBotError.UnknownBot;
        }
        else
        {
            var count = request.Action == RoomBotAction.FillMinimum ? Math.Max(0, _profile.MinPlayers - room.MemberOrder.Count - room.Match.BotCount) : 1;
            // Validate the complete fill before adding any actor, including pending and grace slots.
            if (room.Peers.Count + room.Match.BotCount + count > room.MaxPlayers) return RoomBotError.RoomFull;
            if (ReservedPeers + count > _options.MaxTotalPlayers) return RoomBotError.ServerFull;
            for (var i = 0; i < count; i++)
                if (!room.Match.AddBot(++room.NextBotId)) throw new InvalidOperationException("Bot reservation and runtime capacity diverged.");
        }
        room.LobbyRevision++;
        MapStateChanged?.Invoke(room);
        return RoomBotError.None;
    }

    private void ClearOrphanBots(Room room)
    {
        if (room.Match.Phase != MatchPhase.Waiting || room.MemberOrder.Count != 0 || room.Match.BotCount == 0) return;
        room.Match.ClearBots();
        room.LobbyRevision++;
        room.EmptySince = _time.GetUtcNow();
        MapStateChanged?.Invoke(room);
    }
}
