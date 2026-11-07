using GameServer.Maps;
using GameServer.Simulation;
using JungleBackend.Protocol.Gameplay;

namespace GameServer.Rooms;

public sealed partial class RoomManager
{
    public event Action<Room>? MapStateChanged;
    private MatchRuntime NewRuntime(string code, Guid id)
    {
        var snapshot = Catalog?.Current;
        var selected = snapshot?.Find(snapshot.DefaultMapId);
        return new MatchRuntime(code, selected?.Data ?? _map, _zoneSchedule, _profile, id.ToString(), selected, _worldSetup) { NavigationBudget = _botNavigationBudget };
    }

    public void RegisterMember(Room room, string playerId, string? profile)
    {
        if (room.Match.SelectedMap is null || !room.Match.Entities.TryGetBySession(playerId, out var entity) || entity is null) return;
        if (room.MemberOrder.Contains(entity.NetId)) return;
        room.LobbyRevision++;
        room.MemberOrder.Add(entity.NetId);
        room.MemberProfiles[entity.NetId] = profile ?? "";
        room.Match.CancelRoomCountdown();
        if (room.HostNetId == 255 && (room.IsDefault || room.HostAssigned || profile == "account:" + room.Owner))
        { room.HostNetId = entity.NetId; room.HostAssigned = true; }
        MapStateChanged?.Invoke(room);
    }

    private void RefreshMembers(Room room)
    {
        var changed = false;
        for (var i = room.MemberOrder.Count - 1; i >= 0; i--)
        {
            var id = room.MemberOrder[i];
            if (room.Match.Entities.TryGet(id, out var entity) && entity is not null &&
                (!entity.IsDisconnected || room.Match.Phase == MatchPhase.InGame)) continue;
            room.MemberOrder.RemoveAt(i); room.MemberProfiles.Remove(id); room.MapReady.Remove(id);
            
            if (room.HostNetId == id) room.HostNetId = 255;
            changed = true;
        }
        if (!changed) return;
        foreach (var key in room.BotRequests.Keys.ToArray())
            if (!room.Match.Entities.TryGetBySession(key, out _)) room.BotRequests.Remove(key);
        room.LobbyRevision++;
        room.Match.CancelRoomCountdown();
        if (room.HostNetId == 255 && room.HostAssigned && room.MemberOrder.Count > 0) room.HostNetId = room.MemberOrder[0];
        MapStateChanged?.Invoke(room);
    }

    public RoomMapError SelectMap(Room room, string playerId, in SelectRoomMapPacket request)
    {
        if (!IsHost(room, playerId)) return RoomMapError.NotHost;
        if (room.Match.Phase != MatchPhase.Waiting) return RoomMapError.MatchLocked;
        if (request.RoomId != room.Id || request.SelectionRevision != room.SelectionRevision) return RoomMapError.SelectionChanged;
        var map = Catalog?.Current.Find(request.MapId, request.MapHash);
        if (map is null) return RoomMapError.UnknownMap;
        if (room.Match.SelectedMap?.Entry.MapHash != map.Entry.MapHash || room.Match.SelectedMap?.Entry.Id != map.Entry.Id)
            SetMap(room, map);
        return RoomMapError.None;
    }

    public RoomMapError AcknowledgeMap(Room room, string playerId, in RoomMapReadyPacket request)
    {
        if (request.RoomId != room.Id || request.SelectionRevision != room.SelectionRevision ||
            room.Match.SelectedMap?.Entry.MapHash != request.MapHash || Catalog?.RulesHash != request.RulesHash)
            return RoomMapError.SelectionChanged;
        if (!room.Match.Entities.TryGetBySession(playerId, out var entity) || entity is null ||
            entity.IsDisconnected || !room.MemberOrder.Contains(entity.NetId)) return RoomMapError.MapsNotReady;
        if (room.MapReady.Add(entity.NetId)) MapStateChanged?.Invoke(room);
        return RoomMapError.None;
    }

    public RoomMapError StartMatch(Room room, string playerId, in StartRoomMatchPacket request)
    {
        if (!IsHost(room, playerId)) return RoomMapError.NotHost;
        if (request.RoomId != room.Id || request.SelectionRevision != room.SelectionRevision) return RoomMapError.SelectionChanged;
        if (room.Match.Phase != MatchPhase.Waiting) return RoomMapError.MatchLocked;
        if (room.MemberOrder.Count + room.Match.BotCount < _profile.MinPlayers) return RoomMapError.NotEnoughPlayers;
        if (room.MemberOrder.Any(id => !room.MapReady.Contains(id))) return RoomMapError.MapsNotReady;
        if (!room.Match.StartFromRoom()) return RoomMapError.NotEnoughPlayers;
        MapStateChanged?.Invoke(room);
        return RoomMapError.None;
    }

    private static bool IsHost(Room room, string playerId) => room.HostNetId != 255 &&
        room.Match.Entities.TryGetBySession(playerId, out var entity) && entity is not null &&
        !entity.IsDisconnected && entity.NetId == room.HostNetId;

    public RoomMapStatePacket GetMapState(Room room)
    {
        var entry = room.Match.SelectedMap?.Entry;
        return new RoomMapStatePacket
        {
            RoomId = room.Id, SelectionRevision = room.SelectionRevision, CatalogGeneration = Catalog?.Current.Generation ?? 1,
            MapId = entry?.Id ?? "", MapName = entry?.Name ?? "", MapVersion = entry?.Version ?? "",
            MapHash = entry?.MapHash ?? "", RulesHash = Catalog?.RulesHash ?? "", HostNetId = room.HostNetId,
            PlayerCount = (byte)(room.MemberOrder.Count + room.Match.BotCount), ReadyCount = (byte)(room.Match.BotCount + room.MemberOrder.Count(id => room.MapReady.Contains(id))),
            MaxPlayers = (byte)room.MaxPlayers, MinPlayers = (byte)_profile.MinPlayers, HumanCount = (byte)room.MemberOrder.Count, BotCount = (byte)room.Match.BotCount, LobbyRevision = room.LobbyRevision,
            CanStart = room.Match.Phase == MatchPhase.Waiting && room.HostNetId != 255 &&
                room.MemberOrder.Count + room.Match.BotCount >= _profile.MinPlayers && room.MemberOrder.All(id => room.MapReady.Contains(id))
        };
    }

    public void ApplyCatalog(MapCatalogSnapshot snapshot)
    {
        // Floor-loot spots of new maps are planned off the tick thread; a match that starts first plans them itself.
        if (_worldSetup is not null) { var setup = _worldSetup; _ = Task.Run(() => setup.Prewarm(snapshot.Maps.Values)); }
        foreach (var room in _byCode.Values)
        {
            if (room.Match.Phase == MatchPhase.Lobby) room.Match.CancelRoomCountdown();
            if (room.Match.Phase == MatchPhase.Waiting) ReconcileMap(room, snapshot);
            MapStateChanged?.Invoke(room);
        }
    }

    private void ReconcileMap(Room room, MapCatalogSnapshot snapshot)
    {
        var selected = room.Match.SelectedMap;
        var next = snapshot.Find(selected?.Entry.Id ?? snapshot.DefaultMapId) ?? snapshot.Find(snapshot.DefaultMapId)!;
        if (ReferenceEquals(selected, next)) return;
        if (selected?.Entry.MapHash == next.Entry.MapHash && selected.Entry.Id == next.Entry.Id)
        { room.Match.ChangeRoomMap(next); MapStateChanged?.Invoke(room); return; }
        SetMap(room, next);
    }

    private void SetMap(Room room, MapDefinition map)
    {
        if (!room.Match.ChangeRoomMap(map)) return;
        room.SelectionRevision++; room.LobbyRevision++; room.MapReady.Clear();
        MapStateChanged?.Invoke(room);
    }
}
