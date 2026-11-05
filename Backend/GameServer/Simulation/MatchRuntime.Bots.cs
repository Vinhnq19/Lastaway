using System.Numerics;
using GameServer.Simulation.Bots;
using JungleBackend.Protocol.Gameplay;

namespace GameServer.Simulation;

public sealed partial class MatchRuntime
{
    private readonly Dictionary<uint, BotBrain> _bots = new();
    private static readonly Lazy<BotSettings> Settings = new(() => BotSettings.Load(GameDataLoader.ReadGameDataFile("bot_settings.json")));
    public BotNavigationBudget NavigationBudget { get; internal set; } = new();
    private int _botCursor;
    public int BotCount => _bots.Count;
    public bool BotsSupported => SelectedMap is not null && _worldSettings is not null;
    public int PendingBotPaths => _bots.Values.Count(b => b.HasPendingPath);
    public IReadOnlyDictionary<uint, BotState> BotStates => _bots.ToDictionary(p => p.Key, p => p.Value.State);

    internal bool AddBot(uint botId)
    {
        if (!BotsSupported || Phase != MatchPhase.Waiting || botId == 0 || _bots.ContainsKey(botId)) return false;
        var session = $"bot:{RoomId}:{botId}";
        if (!RegisterParticipant(session, "BOT " + botId, botId, out _)) return false;
        _bots.Add(botId, new BotBrain(botId, session, Settings.Value));
        SetPlayerReady(session);
        return true;
    }

    internal bool RemoveBot(uint botId)
    {
        if (Phase != MatchPhase.Waiting || !_bots.Remove(botId, out var brain)) return false;
        MarkDisconnected(brain.SessionId);
        return true;
    }

    internal void ClearBots()
    {
        foreach (var id in _bots.Keys.ToArray()) RemoveBot(id);
    }

    private void ResetBotBrains()
    {
        foreach (var brain in _bots.Values)
            brain.Reset(unchecked((uint)MatchId.GetHashCode() ^ brain.Id));
    }

    private bool HasHumanAudience() => _entities.Values.Any(e => !e.IsBot && (!e.IsDisconnected ||
        (e.IsAlive && _matchElapsedSeconds - (e.DisconnectedAtSecond ?? 0) < _profile.DisconnectGraceSeconds)));

    private void TickBots()
    {
        if (_bots.Count == 0) return;
        if (!HasHumanAudience())
        {
            EndMatch(MatchEndReason.NoHumans, 255);
            return;
        }
        if (!NavigationBudget.Shared) NavigationBudget.Reset(Settings.Value.GlobalPathNodesPerTick);
        var brains = _bots.Values.ToArray();
        for (var i = 0; i < brains.Length; i++)
        {
            var brain = brains[(i + _botCursor) % brains.Length];
            if (!_bySession.TryGetValue(brain.SessionId, out var entity)) continue;
            brain.Tick(this, entity, Settings.Value);
        }
        _botCursor = (_botCursor + 1) % brains.Length;
    }
}
