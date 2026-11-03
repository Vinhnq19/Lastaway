using GameServer.Maps;
using JungleBackend.Protocol.Gameplay;

namespace GameServer.Simulation;

public sealed partial class MatchRuntime
{
    public bool ChangeRoomMap(MapDefinition map)
    {
        if (!RequiresManualStart || Phase != MatchPhase.Waiting) return false;
        SelectedMap = map;
        _map = map.Data;
        _physics = new PhysicsSimulation(_map, _profile.PlayerRadius, _profile.PlayerSpeed);
        ResetWorld();
        ConfigureWorld();
        _combat = CreateCombat();
        return true;
    }

    public bool StartFromRoom()
    {
        if (!RequiresManualStart || Phase != MatchPhase.Waiting ||
            _entities.Values.Count(e => e.Ready && !e.IsDisconnected) < _profile.MinPlayers) return false;
        TransitionTo(MatchPhase.Lobby);
        return true;
    }

    public void CancelRoomCountdown()
    {
        if (RequiresManualStart && Phase == MatchPhase.Lobby) TransitionTo(MatchPhase.Waiting);
    }
}
