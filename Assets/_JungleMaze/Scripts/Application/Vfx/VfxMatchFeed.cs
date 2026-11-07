using System;
using JungleBackend.Protocol.Gameplay;
using JungleMaze.App.Match;

namespace JungleMaze.App.Vfx
{
    /// <summary>
    /// <see cref="IVfxMatchFeed"/> over the real sessions: offline practice when it is active, otherwise the online
    /// <see cref="MatchSession"/>. A dev mock, when given and active, takes precedence (VfxPreview, F10 panel).
    /// </summary>
    public sealed class VfxMatchFeed : IVfxMatchFeed
    {
        private readonly MatchSession _online;
        private readonly OfflineMatchSession _offline;
        private readonly IVfxMatchFeed _mock;
        private readonly Func<bool> _mockActive;

        public VfxMatchFeed(MatchSession online, OfflineMatchSession offline, IVfxMatchFeed mock = null, Func<bool> mockActive = null)
        {
            _online = online;
            _offline = offline;
            _mock = mock;
            _mockActive = mockActive;
        }

        private bool UseMock => _mock != null && _mockActive != null && _mockActive();
        private bool UseOffline => _offline != null && _offline.IsActive;

        public Guid SessionKey => UseMock ? _mock.SessionKey : UseOffline ? _offline.MatchId : _online != null ? _online.MatchId : Guid.Empty;

        public bool InGame => UseMock ? _mock.InGame : UseOffline ||
                              _online != null && _online.Connected && _online.Phase == MatchPhase.InGame;

        public byte OwnNetId => UseMock ? _mock.OwnNetId : UseOffline ? (byte)0 : _online != null ? _online.OwnNetId : (byte)255;

        public bool Online => UseMock ? _mock.Online : !UseOffline && _online != null && _online.Connected;
    }
}
