using System;
using System.Collections.Generic;
using JungleBackend.Protocol;
using JungleBackend.Protocol.Gameplay;
using JungleMaze.App.Config;
using JungleMaze.App.Flow;
using JungleMaze.App.Hud.State;
using JungleMaze.App.Lobby;
using JungleMaze.App.Match;
using JungleMaze.App.Network;
using JungleMaze.App.Player;
using JungleMaze.App.Ports;
using JungleMaze.Core.Events;
using JungleMaze.Domain.Hud;
using JungleMaze.Domain.Network;
using JungleMaze.Presentation.UI;
using NUnit.Framework;

namespace JungleMaze.Tests.App
{
    public sealed class SoloOfflineTests
    {
        private sealed class FakeHudStore : IHudState, IHudStateWriter
        {
            public int Hp { get; private set; } = 100;
            public int MaxHp => 100;
            public int Alive { get; private set; }
            public int Total { get; private set; }
            public float ZoneCountdown { get; private set; }
            public int DamageTakenCount { get; private set; }
            public bool WasReset { get; private set; }

            public event Action<HudStateChange> Changed;
            public event Action<ItemPickup> ItemPicked;
            public event Action<HitDealt> HitDealt;
            public event Action<DamageTaken> DamageTaken;
            public event Action<DangerChange> DangerChanged;

            public int DangerCount => 0;
            public DangerWarning GetDangerAt(int index) => default;
            public GearStatus Armor => default;
            public GearStatus Helmet => default;
            public HeldWeaponStatus HeldWeapon => default;
            public AmmoStatus Ammo => default;
            public MatchStatusInfo MatchStatus => default;
            public int MapSizeCells => 50;
            public bool HasPose => false;
            public HeroPose Pose => default;

            public void SetHp(int hp) => Hp = hp;
            public void NotifyPickup(string itemId, int count) { }
            public void NotifyHitDealt(HitTargetKind target) { }
            public void NotifyDamageTaken(bool hasDirection, float directionDegrees) => DamageTakenCount++;
            public void SetDanger(int id, DangerKind kind, float remainingSeconds, bool hasSource, float sourceX, float sourceY) { }
            public void ClearDanger(int id) { }
            public void SetArmor(string equipmentId, float durability) { }
            public void SetHelmet(string equipmentId, float durability) { }
            public void SetHeldWeapon(string weaponId) { }
            public void SetAmmo(int inMagazine, int reserve) { }
            public void SetZoneCountdown(float secondsRemaining) => ZoneCountdown = secondsRemaining;
            public void SetAliveCount(int alive, int total) { Alive = alive; Total = total; }
            public void SetPose(float x, float y, float rotationDegrees) { }
            public void Reset() { WasReset = true; Hp = 100; Alive = 0; Total = 0; }
        }

        private sealed class FakeRuntimeData : IClientRuntimeData
        {
            public IReadOnlyList<RuntimeCover> Covers => Array.Empty<RuntimeCover>();
            public RuntimeMapBounds Bounds => new RuntimeMapBounds { MinX = 0, MinY = 0, MaxX = 50, MaxY = 50 };
            public float PlayerSpeed => 4f;
            public float PlayerRadius => 0.4f;
            public ZoneSchedule ZoneSchedule { get; set; } = ZoneSchedule.Default();
            public string Verify(string expectedVersion, string expectedHash) => null;
            public string LoadLocal() => null;
        }

        private sealed class FakeAccountApi : IAccountApi
        {
            public AuthToken Token = new AuthToken("mock-jwt", DateTime.UtcNow.AddMinutes(30));
            public void Register(string username, string password, Action<ApiResult<bool>> done) => done(ApiResult<bool>.Success(true, 201));
            public void Login(string username, string password, Action<ApiResult<AuthToken>> done) => done(ApiResult<AuthToken>.Success(Token));
        }

        private sealed class TestMultiProfileConfig : IConfigProvider
        {
            private readonly Dictionary<string, ClientConfig> _profiles = new Dictionary<string, ClientConfig>(StringComparer.OrdinalIgnoreCase);
            public TestMultiProfileConfig(ClientConfig initial) => Current = initial;
            public void AddProfile(string name, ClientConfig config) => _profiles[name] = config;
            public ClientConfig Current { get; set; }
            public IReadOnlyList<string> Profiles => new List<string>(_profiles.Keys);
            public string SelectProfile(string profile)
            {
                if (_profiles.TryGetValue(profile, out var cfg))
                {
                    Current = cfg;
                    return null;
                }
                return "Unknown profile";
            }
            public string SaveUserConfig(ClientConfig config)
            {
                Current = config;
                return null;
            }
            public string CommandLineRoomCode => null;
            public string CommandLineDisplayName => null;
        }

        private EventBus _bus;
        private FakeNetwork _network;
        private FakeScenes _scenes;
        private FakeConfig _config;
        private FakeAuth _auth;
        private FakeHudStore _hud;
        private FakeRuntimeData _data;
        private GameFlow _flow;
        private PlayerInputService _input;
        private ConnectToServerUseCase _connect;
        private OfflineMatchSession _offlineSession;
        private ConnectionMenuPresenter _presenter;

        [SetUp]
        public void SetUp()
        {
            _bus = new EventBus();
            _network = new FakeNetwork(_bus);
            _scenes = new FakeScenes();
            _input = new PlayerInputService(_network);
            _flow = new GameFlow(_bus, _network, _network, _scenes, _input, new GameFlowSettings { ResultsSeconds = 1f });
            _flow.Initialize();

            var clientConfig = new ClientConfig(ConnectionMode.Local, "http://127.0.0.1:5000", "127.0.0.1", 7777,
                JoinAuthMode.RoomCode, NetworkProtocol.Version, 5000, "Udp", "test");
            _config = new FakeConfig(clientConfig);
            _auth = new FakeAuth();
            _connect = new ConnectToServerUseCase(_config, _network, _auth, _bus);
            _hud = new FakeHudStore();
            _data = new FakeRuntimeData();

            _offlineSession = new OfflineMatchSession(null, _config, _data, _hud, _bus, _flow, _hud);
            _presenter = new ConnectionMenuPresenter(_config, _connect, _network, _flow, _bus, null, _offlineSession);
            _presenter.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            _presenter.Dispose();
            _offlineSession.Dispose();
            _flow.Dispose();
        }

        [Test]
        public void OfflineWarningAndElimination_UseRealAudioFeed_WithoutDuplicates()
        {
            _presenter.StartOffline();
            var cues = new List<JungleMaze.Domain.Audio.SfxId>();
            var source = new JungleMaze.App.Audio.MatchAudioEventSource(null,
                new JungleMaze.App.Audio.MatchSessionAudioFeed(null, _offlineSession), null, _offlineSession);
            source.CueRaised += cue => cues.Add(cue.Id);
            _offlineSession.SetPlayerPosition(25,25);
            _offlineSession.Tick(81f);
            source.Tick(0); source.Tick(0);
            Assert.AreEqual(1, cues.Count);
            Assert.AreEqual(JungleMaze.Domain.Audio.SfxId.ZoneBell3, cues[0]);
            _offlineSession.ApplyDamage(100);
            source.Tick(0); source.Tick(0);
            Assert.AreEqual(2, cues.Count);
            Assert.AreEqual(JungleMaze.Domain.Audio.SfxId.Eliminated, cues[1]);
        }

        [Test]
        public void FullFiveStageRound_EndsAfterZeroRadius_AndReplayClearsPosition()
        {
            _presenter.StartOffline();
            var phases = new HashSet<byte>();
            for (var i = 0; i < 12500 && !_offlineSession.IsDead; i++)
            {
                var c = _offlineSession.Zone.CurrentCenter;
                _offlineSession.SetPlayerPosition(c.X, c.Y);
                _offlineSession.Tick(1f / 30f);
                phases.Add(_offlineSession.Zone.PhaseIndex);
            }
            Assert.IsTrue(_offlineSession.HasResult);
            Assert.AreEqual(0f, _offlineSession.Zone.CurrentRadius);
            Assert.GreaterOrEqual(_offlineSession.ElapsedSeconds, 390f);
            foreach (byte phase in new byte[] { 1,2,3,4,5 }) Assert.Contains(phase, new List<byte>(phases));
            Assert.AreEqual(0, _network.Sent);
            _offlineSession.Replay();
            Assert.AreEqual(100, _offlineSession.PlayerHp);
            Assert.AreEqual(0f, _offlineSession.ElapsedSeconds);
        }

        [Test]
        public void OfflineMatch_NoNetworkRoute_DoesNotCallNetworkOrAuth()
        {
            Assert.AreEqual(AppScreen.MainMenu, _flow.Screen);
            Assert.AreEqual(NetworkState.Disconnected, _network.State);

            _presenter.StartOffline();

            Assert.AreEqual(AppScreen.InMatch, _flow.Screen);
            Assert.IsTrue(_offlineSession.IsActive);
            Assert.AreNotEqual(Guid.Empty, _offlineSession.MatchId);
            Assert.AreEqual(NetworkState.Disconnected, _network.State);
            Assert.AreEqual(0, _network.Connects.Count);
            Assert.AreEqual(0, _network.Sent);
            Assert.AreEqual(0, _auth.Calls);
            Assert.AreEqual(100, _hud.Hp);
            Assert.AreEqual(1, _hud.Alive);
            Assert.AreEqual(1, _hud.Total);
            CollectionAssert.Contains(_scenes.Loads, "Game");
        }

        [Test]
        public void SwitchingTarget_LanRadminAws_UpdatesConfigAndClearsWss()
        {
            // 1. LAN
            _presenter.SelectTarget("LAN");
            Assert.AreEqual(ConnectionMode.LAN, _presenter.Mode);
            Assert.AreEqual(ConnectionMode.LAN, _config.Current.Mode);
            Assert.AreEqual("Udp", _config.Current.Transport);
            Assert.IsNull(_config.Current.WebSocketUrl);

            // 2. Radmin / VPN
            _presenter.SelectTarget("Radmin");
            Assert.AreEqual(ConnectionMode.Online, _presenter.Mode);
            Assert.AreEqual("Radmin", _presenter.SelectedOnlineTarget);
            Assert.AreEqual(ConnectionMode.Online, _config.Current.Mode);
            Assert.AreEqual("Udp", _config.Current.Transport);
            Assert.IsNull(_config.Current.WebSocketUrl);

            // 3. AWS Cloud
            _presenter.SelectTarget("AWS");
            Assert.AreEqual(ConnectionMode.Online, _presenter.Mode);
            Assert.AreEqual("AWS", _presenter.SelectedOnlineTarget);
            Assert.AreEqual("junglemaze.duckdns.org", _presenter.Host);
            Assert.AreEqual(ConnectionMode.Online, _config.Current.Mode);
            Assert.AreEqual("Auto", _config.Current.Transport);
            Assert.AreEqual("wss://junglemaze.duckdns.org/game", _config.Current.WebSocketUrl);
            Assert.AreEqual("https://junglemaze.duckdns.org", _config.Current.ApiBaseUrl);

            // 4. Switch back to LAN: clears WSS URL
            _presenter.SelectTarget("LAN");
            Assert.AreEqual(ConnectionMode.LAN, _config.Current.Mode);
            Assert.AreEqual("Udp", _config.Current.Transport);
            Assert.IsNull(_config.Current.WebSocketUrl);
        }

        [Test]
        public void TargetSwitching_FromAwsToLan_InvalidatesSessionOnce_AndClearsAccountToken()
        {
            _presenter.SelectTarget("AWS");
            var accountApi = new FakeAccountApi();
            var session = new AccountSession(accountApi, () => DateTime.UtcNow);
            session.LoggedOut += () => _presenter.AccountToken = null;
            var endpointChangedCount = 0;
            _presenter.EndpointChanged += () =>
            {
                endpointChangedCount++;
                _presenter.AccountToken = null;
                session.Logout();
            };

            session.Username = "user";
            session.Password = "pass";
            Assert.IsTrue(session.Submit());
            _presenter.AccountToken = session.Token;
            Assert.IsTrue(session.IsValid);
            Assert.AreEqual("mock-jwt", _presenter.AccountToken);

            // Switch to LAN -> real API origin change (junglemaze.duckdns.org -> YOUR-SELFHOST.duckdns.org)
            _presenter.SelectTarget("LAN");

            Assert.AreEqual(1, endpointChangedCount, "EndpointChanged must fire exactly once on real API change");
            Assert.IsNull(_presenter.AccountToken, "AccountToken must be cleared");
            Assert.IsFalse(session.IsValid, "AccountSession must be logged out");
            Assert.IsNull(session.Token, "AccountSession token must be null");
        }

        [Test]
        public void TargetSwitching_SameTargetOrEndpoint_DoesNotInvalidateSession()
        {
            _presenter.SelectTarget("AWS");
            var accountApi = new FakeAccountApi();
            var session = new AccountSession(accountApi, () => DateTime.UtcNow);
            session.LoggedOut += () => _presenter.AccountToken = null;
            var endpointChangedCount = 0;
            _presenter.EndpointChanged += () =>
            {
                endpointChangedCount++;
                _presenter.AccountToken = null;
                session.Logout();
            };

            session.Username = "user";
            session.Password = "pass";
            Assert.IsTrue(session.Submit());
            _presenter.AccountToken = session.Token;

            // Re-select same target (AWS)
            _presenter.SelectTarget("AWS");
            Assert.AreEqual(0, endpointChangedCount, "Re-selecting same target must not fire EndpointChanged");
            Assert.AreEqual("mock-jwt", _presenter.AccountToken, "Login must be preserved");
            Assert.IsTrue(session.IsValid, "Session must remain valid");

            // Save endpoint without changing API origin
            var saved = _presenter.SaveEndpoint();
            Assert.IsTrue(saved);
            Assert.AreEqual(0, endpointChangedCount, "Saving unchanged endpoint must not fire EndpointChanged");
            Assert.AreEqual("mock-jwt", _presenter.AccountToken, "Login must be preserved");
            Assert.IsTrue(session.IsValid, "Session must remain valid");
        }

        [Test]
        public void ApiPathChangeOnSameHost_InvalidatesOldBackendSession()
        {
            _presenter.SelectTarget("LAN");
            _presenter.AccountToken = "fixture-session";
            var changed = 0; _presenter.EndpointChanged += () => changed++;
            _presenter.ApiBaseUrl = _presenter.ApiBaseUrl.TrimEnd('/') + "/another-backend";
            Assert.IsTrue(_presenter.SaveEndpoint());
            Assert.AreEqual(1,changed); Assert.IsNull(_presenter.AccountToken);
        }

        [Test]
        public void SaveEndpoint_InvalidOrFailedSave_DoesNotInvalidateSession()
        {
            _presenter.SelectTarget("AWS");
            _presenter.AccountToken = "valid-token";
            var endpointChangedCount = 0;
            _presenter.EndpointChanged += () => endpointChangedCount++;

            // Invalid port
            _presenter.Port = "not_a_number";
            var saved = _presenter.SaveEndpoint();
            Assert.IsFalse(saved);
            Assert.AreEqual(0, endpointChangedCount, "Invalid save must not fire EndpointChanged");
            Assert.AreEqual("valid-token", _presenter.AccountToken, "Login must be preserved on invalid save");
        }

        [Test]
        public void SelectProfile_SuccessfulApiChangeInvalidates_FailedOrSameProfilePreservesSession()
        {
            var awsConfig = new ClientConfig(ConnectionMode.Online, "https://junglemaze.duckdns.org", "junglemaze.duckdns.org", 7777,
                JoinAuthMode.RoomCode, NetworkProtocol.Version, 5000, "Auto", "test", "wss://junglemaze.duckdns.org/game");
            var lanConfig = new ClientConfig(ConnectionMode.LAN, "https://192.168.1.10:5000", "192.168.1.10", 7777,
                JoinAuthMode.RoomCode, NetworkProtocol.Version, 5000, "Udp", "test");
            var multiConfig = new TestMultiProfileConfig(awsConfig);
            multiConfig.AddProfile("aws", awsConfig);
            multiConfig.AddProfile("lan", lanConfig);

            var presenter = new ConnectionMenuPresenter(multiConfig, _connect, _network, _flow, _bus, null, _offlineSession);
            presenter.Initialize();
            var accountApi = new FakeAccountApi();
            var session = new AccountSession(accountApi, () => DateTime.UtcNow);
            session.LoggedOut += () => presenter.AccountToken = null;
            var endpointChangedCount = 0;
            presenter.EndpointChanged += () =>
            {
                endpointChangedCount++;
                presenter.AccountToken = null;
                session.Logout();
            };

            session.Username = "user";
            session.Password = "pass";
            Assert.IsTrue(session.Submit());
            presenter.AccountToken = session.Token;

            // 1. Select unknown profile -> fails
            presenter.SelectProfile("unknown_profile");
            Assert.AreEqual(0, endpointChangedCount, "Failed profile select must not invalidate");
            Assert.AreEqual("mock-jwt", presenter.AccountToken);
            Assert.IsTrue(session.IsValid);

            // 2. Select same profile (aws) -> no API change
            presenter.SelectProfile("aws");
            Assert.AreEqual(0, endpointChangedCount, "Selecting profile with same API origin must not invalidate");
            Assert.AreEqual("mock-jwt", presenter.AccountToken);
            Assert.IsTrue(session.IsValid);

            // 3. Select lan profile -> real API change (junglemaze.duckdns.org -> 192.168.1.10:5000)
            presenter.SelectProfile("lan");
            Assert.AreEqual(1, endpointChangedCount, "Selecting profile with new API origin must invalidate once");
            Assert.IsNull(presenter.AccountToken, "AccountToken must be cleared");
            Assert.IsFalse(session.IsValid, "Session must be logged out");

            presenter.Dispose();
        }

        [Test]
        public void Join_WhenApiUrlModified_InvalidatesSessionBeforeConnect()
        {
            _presenter.SelectTarget("LAN");
            var accountApi = new FakeAccountApi();
            var session = new AccountSession(accountApi, () => DateTime.UtcNow);
            session.LoggedOut += () => _presenter.AccountToken = null;
            var endpointChangedCount = 0;
            _presenter.EndpointChanged += () =>
            {
                endpointChangedCount++;
                _presenter.AccountToken = null;
                session.Logout();
            };

            session.Username = "user";
            session.Password = "pass";
            Assert.IsTrue(session.Submit());
            _presenter.AccountToken = session.Token;
            _presenter.RoomCode = "ROOM1";

            // User edits API Base URL in menu before clicking Join
            _presenter.ApiBaseUrl = "https://selfhost.example.com";
            _presenter.Join();

            Assert.AreEqual(1, endpointChangedCount, "Join with modified API origin must persist and invalidate session once");
            Assert.IsNull(_presenter.AccountToken, "AccountToken must be cleared before connecting");
            Assert.IsFalse(session.IsValid, "AccountSession must be logged out");
        }

        [Test]
        public void SoloOffline_NeverInstantWin_WhileAlive()
        {
            _presenter.StartOffline();
            Assert.AreEqual(1, _hud.Alive);
            Assert.AreEqual(1, _hud.Total);

            // Keep player inside safe zone center (25, 25)
            _offlineSession.SetPlayerPosition(25f, 25f);

            // Advance 10 seconds
            for (var i = 0; i < 300; i++)
            {
                _offlineSession.Tick(1f / 30f);
            }

            Assert.IsFalse(_offlineSession.IsDead);
            Assert.IsFalse(_offlineSession.HasResult);
            Assert.AreEqual(100, _offlineSession.PlayerHp);
            Assert.AreEqual(AppScreen.InMatch, _flow.Screen);
        }

        [Test]
        public void SoloOffline_OutsideZone_TakesDamageAndDies_EntersResults()
        {
            _presenter.StartOffline();
            // Position (0, 0) is ~35.3m from center (25, 25), outside the 25m initial radius
            _offlineSession.SetPlayerPosition(0f, 0f);

            // Tick 1 second (30 ticks): should take 5 damage
            for (var i = 0; i < 30; i++)
            {
                _offlineSession.Tick(1f / 30f);
            }

            Assert.AreEqual(95, _offlineSession.PlayerHp);
            Assert.AreEqual(95, _hud.Hp);
            Assert.AreEqual(1, _hud.DamageTakenCount);
            Assert.IsFalse(_offlineSession.IsDead);

            // Tick another 19 seconds: 19 * 5 = 95 damage -> 0 HP -> death
            for (var i = 0; i < 19 * 30; i++)
            {
                _offlineSession.Tick(1f / 30f);
            }

            Assert.AreEqual(0, _offlineSession.PlayerHp);
            Assert.IsTrue(_offlineSession.IsDead);
            Assert.IsTrue(_offlineSession.HasResult);
            Assert.AreEqual(AppScreen.Results, _flow.Screen);
            Assert.AreEqual(0, _hud.Alive);
            Assert.AreEqual(1, _offlineSession.Result.Participants[0].Rank);
            Assert.GreaterOrEqual(_offlineSession.SurvivalSeconds, 19f);
        }

        [Test]
        public void SoloOffline_Replay_GeneratesNewMatchId_ResetsRound()
        {
            _presenter.StartOffline();
            var firstMatchId = _offlineSession.MatchId;

            // Kill player directly
            _offlineSession.ApplyDamage(100);
            Assert.IsTrue(_offlineSession.IsDead);
            Assert.AreEqual(AppScreen.Results, _flow.Screen);

            // Replay
            _offlineSession.Replay();

            Assert.AreNotEqual(firstMatchId, _offlineSession.MatchId);
            Assert.IsFalse(_offlineSession.IsDead);
            Assert.IsFalse(_offlineSession.HasResult);
            Assert.AreEqual(100, _offlineSession.PlayerHp);
            Assert.AreEqual(100, _hud.Hp);
            Assert.AreEqual(1, _hud.Alive);
            Assert.AreEqual(AppScreen.InMatch, _flow.Screen);
        }

        [Test]
        public void SoloOffline_Leave_ResetsAllState_ReturnsToMenu()
        {
            _presenter.StartOffline();
            Assert.IsTrue(_offlineSession.IsActive);
            Assert.AreEqual(AppScreen.InMatch, _flow.Screen);

            _presenter.Disconnect();

            Assert.IsFalse(_offlineSession.IsActive);
            Assert.AreEqual(Guid.Empty, _offlineSession.MatchId);
            Assert.AreEqual(AppScreen.MainMenu, _flow.Screen);
            Assert.IsTrue(_hud.WasReset);
        }
    }
}
