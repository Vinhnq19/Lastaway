using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using JungleBackend.Protocol.Gameplay;
using JungleMaze.App.Match;
using JungleMaze.App.Ports;
using JungleMaze.App.Testing;
using JungleMaze.Core.DI;
using JungleMaze.Domain.Network;
using JungleMaze.Domain.Player;
using JungleMaze.Infrastructure.Testing;
using JungleMaze.Presentation.Player;
using JungleMaze.Presentation.UI;
using UnityEngine;

namespace JungleMaze.Composition
{
    /// <summary>Chỉ chạy khi có -onlineTest. Kịch bản và bằng chứng dùng lại cho các feature sau.</summary>
    public sealed class OnlineScenarioRunner : MonoBehaviour
    {
        [Serializable] public sealed class Assertion { public string id, status, detail; }
        [Serializable] public sealed class Metrics
        {
            public float elapsedSeconds, connectedSeconds;
            public int snapshots, inputs, ignoredPackets, catchupDrops, maxPlayers, completedMatches;
            public double inputHz, activeInputSeconds, snapshotHz, snapshotSeconds, rttP50Ms, rttP95Ms, rttP99Ms;
            public int pings, pongs, pongTimeouts;
        }
        [Serializable] public sealed class Evidence
        {
            public int schemaVersion = 1, physicalDeviceCount = 1;
            public string scenarioId, status, reason, selectedTransport, fallbackReason, endpoint, matchId;
            public Metrics metrics = new Metrics();
            public Assertion[] assertions;
        }
        private MatchSession _match;
        private JungleMaze.App.Lobby.LobbyFlow _lobby;
        private INetworkClient _network;
        private JungleMaze.Core.Events.IEventBus _bus;
        private NetworkStats? _failureStats;
        private string _failureEndpoint, _failureReason;
        private IPacketSender _sender;
        private IPlayerHistory _history;
        private ConnectionMenuPresenter _menu;
        private MatchDemoView _view;
        private OnlineScenario _scenario;
        private readonly Evidence _evidence = new Evidence();
        private readonly Dictionary<string, bool> _observed = new Dictionary<string, bool>();
        private readonly Dictionary<string, Func<OnlineTestContext, OnlineCheckResult>> _checks = new Dictionary<string, Func<OnlineTestContext, OnlineCheckResult>>();
        private readonly Dictionary<string, OnlineCheckResult> _checkResults = new Dictionary<string, OnlineCheckResult>();
        private readonly HashSet<Guid> _rounds = new HashSet<Guid>();
        private readonly HashSet<Guid> _results = new HashSet<Guid>();
        private readonly CancellationTokenSource _stop = new CancellationTokenSource();
        private IOnlineTestDriver _driver;
        private string _output;
        private float _started, _connectedSeconds, _movementStarted = -1;
        private Vector2? _firstPosition;
        private bool _finished, _wasJoined, _historyBusy;
        private bool _capturedView, _capturedResult;
        private Guid _lastReplay, _historyMatch;
        private int _baseInputs;
        private double _baseInputSeconds;
        private double _snapshotSeconds;
        private float _nextHistory;

        [Inject] public void Construct(MatchSession match, INetworkClient network, IPacketSender sender, IPlayerHistory history, ConnectionMenuPresenter menu, JungleMaze.App.Lobby.LobbyFlow lobby, JungleMaze.Core.Events.IEventBus bus)
        {
            _lobby = lobby; _match = match; _network = network; _sender = sender; _history = history; _menu = menu;
            _bus = bus; _bus.Subscribe<NetworkStateChanged>(NetworkChanged);
        }

        private void NetworkChanged(NetworkStateChanged change)
        {
            if (_finished || !_wasJoined || _failureStats.HasValue || change.Current != NetworkState.Failed) return;
            // Reconnect can reset LastReason/stats in the same frame; retain the original transport failure.
            _failureStats = _network.Stats; _failureEndpoint = _network.Endpoint; _failureReason = change.Reason.ToString();
        }

        private IEnumerator Start()
        {
            _started = Time.realtimeSinceStartup;
            var args = Environment.GetCommandLineArgs(); string scenarioPath = null;
            for (var i = 0; i + 1 < args.Length; i++)
            { if (args[i] == "-onlineTest") scenarioPath = args[++i]; else if (args[i] == "-onlineEvidence") _output = args[++i]; }
            try
            {
                if (string.IsNullOrWhiteSpace(_output)) throw new ArgumentException("-onlineEvidence path required");
                if (!OnlineScenarioJson.TryParse(File.ReadAllText(scenarioPath), out _scenario, out var parseError)) throw new ArgumentException(parseError);
                _evidence.scenarioId = _scenario.id;
                var error = _scenario.Validate(); if (error != null) throw new ArgumentException(error);
                foreach (var id in _scenario.extraAssertions ?? Array.Empty<string>())
                {
                    if (!OnlineTestExtensions.TryGetCheck(id, out var check)) throw new ArgumentException("Unknown assertion: " + id);
                    _checks.Add(id, check); _checkResults.Add(id, new OnlineCheckResult("WAIT"));
                }
                if (_scenario.driver != "movement" && !OnlineTestExtensions.TryCreateDriver(_scenario.driver, out _driver))
                    throw new ArgumentException("Unknown test driver");
                Application.runInBackground = true; Application.targetFrameRate = 30; QualitySettings.vSyncCount = 0;
                _baseInputs = _match.InputsSent; _baseInputSeconds = _match.ActiveInputSeconds;
                _view = GetComponent<MatchDemoView>(); _view.UseExternalIntent = true;
            }
            catch (Exception) { Finish("FAIL", "Invalid scenario/output or missing feature check; inspect scenario schema and registered extensions."); yield break; }
            yield return new WaitForSecondsRealtime(1);
            // The opt-in acceptance runner uses the same account and join flow as the visible UI.
            _lobby.Account.Username = Environment.GetEnvironmentVariable("NET_USERNAME") ?? "";
            _lobby.Account.Password = Environment.GetEnvironmentVariable("NET_PASSWORD") ?? "";
            _lobby.Account.RegisterMode = false;
            if (!_lobby.Account.Submit()) { Finish("FAIL", "Account login could not start"); yield break; }
            var loginAt = Time.realtimeSinceStartup;
            while (_lobby.Account.Busy && Time.realtimeSinceStartup - loginAt < 15) yield return null;
            if (!_lobby.Account.IsValid) { Finish("FAIL", "Account login failed before join"); yield break; }
            _lobby.OpenJoin(); _lobby.Join.SetCode(_menu.RoomCode); _lobby.Join.SetName(_menu.DisplayName);
            if (!_lobby.SubmitJoin()) { Finish("FAIL", "Authenticated join form rejected input"); yield break; }
        }

        private void Update()
        {
            if (_finished || _scenario == null) return;
            var elapsed = Time.realtimeSinceStartup - _started;
            if (_failureStats.HasValue) { Finish("FAIL", "Unexpected disconnection: " + _failureReason); return; }
            if (_network.State == NetworkState.Joined)
            {
                _wasJoined = true; _connectedSeconds += Time.unscaledDeltaTime; Mark("joined");
            }
            else if (_wasJoined)
            { Finish("FAIL", "Unexpected disconnection: " + _network.LastReason); return; }
            else if (_network.State == NetworkState.Failed)
            {
                var reason = _network.LastReason;
                var blocked = reason == DisconnectReason.ConnectTimeout || reason == DisconnectReason.SocketError;
                Finish(blocked ? "BLOCKED" : "FAIL", "Connect failed: " + reason); return;
            }
            var context = new OnlineTestContext(_match, _network, _history, _sender, elapsed);
            if (_match.Phase == MatchPhase.InGame && _match.Connected)
            {
                _snapshotSeconds += Time.unscaledDeltaTime;
                _rounds.Add(_match.MatchId);
                if (_movementStarted < 0) _movementStarted = elapsed;
                if (_driver != null) _driver.Apply(context);
                else
                {
                    var moving = elapsed - _movementStarted < 3;
                    var intent = new PlayerIntent { MoveX = moving ? -.25f : 0, MoveY = moving ? .2f : 0, AimDegrees = 45 };
                    // A winner can keep full HP forever; exercise damage with real movement, never by changing HP.
                    if (_scenario.requireHpDamage && !Seen("hpDamage") && elapsed - _movementStarted > 30 &&
                        _match.Players.TryGet(_match.OwnNetId, out var self) && self.HasState)
                    {
                        var center = _match.HasZone ? new Vector2(_match.Zone.CurrentCenterX, _match.Zone.CurrentCenterY) : new Vector2(25, 25);
                        var outward = new Vector2(self.Current.X, self.Current.Y) - center;
                        var angle = outward.sqrMagnitude > .001f ? Mathf.Atan2(outward.y, outward.x) : 0;
                        for (var option = 0; option < 8; option++)
                        {
                            var turn = option == 0 ? 0 : ((option + 1) / 2) * (option % 2 == 1 ? 1 : -1);
                            var direction = new Vector2(Mathf.Cos(angle + turn * Mathf.PI / 4), Mathf.Sin(angle + turn * Mathf.PI / 4));
                            var world = _match.Prediction.World;
                            if (world != null && world.Overlaps(self.Current.X + direction.x * .65f, self.Current.Y + direction.y * .65f)) continue;
                            intent.MoveX = direction.x; intent.MoveY = direction.y;
                            break;
                        }
                    }
                    _match.SetIntent(intent);
                }
                if (_match.Players.TryGet(_match.OwnNetId, out var own) && own.HasState)
                {
                    var position = new Vector2(own.Current.X, own.Current.Y);
                    if (!_firstPosition.HasValue) _firstPosition = position;
                    if (Vector2.Distance(position, _firstPosition.Value) > .5f) Mark("movement");
                    if (own.Current.Hp < 100) Mark("hpDamage");
                }
                var visible = 0; var spawner = FindAnyObjectByType<RemotePlayerSpawner>();
                if (spawner != null && Camera.main != null) foreach (var id in _match.Players.ActiveIds)
                {
                    var view = spawner.GetView(id); if (view == null) continue;
                    var p = Camera.main.WorldToViewportPoint(view.transform.position);
                    if (p.z > 0 && p.x >= 0 && p.x <= 1 && p.y >= 0 && p.y <= 1) visible++;
                }
                _evidence.metrics.maxPlayers = Math.Max(_evidence.metrics.maxPlayers, visible);
                if (visible >= _scenario.expectedPlayers) Mark("playersVisible");
                if (!_capturedView && Seen("playersVisible") && Seen("movement"))
                { ScreenCapture.CaptureScreenshot(_output + ".png"); _capturedView = true; }
                if (_match.HasZone) Mark("zone");
            }
            if (_match.HasResult)
            {
                if (!_capturedResult) { ScreenCapture.CaptureScreenshot(_output + ".result.png"); _capturedResult = true; }
                _results.Add(_match.Result.MatchId); _historyMatch = _match.Result.MatchId;
                if (_match.Result.ParticipantCount >= _scenario.expectedPlayers) Mark("result");
                if (_scenario.requireHistory && !_historyBusy && elapsed >= _nextHistory && !Seen("history")) RefreshHistory();
                if (_scenario.requireReplay && _match.Phase == MatchPhase.Ending && _lastReplay != _match.MatchId)
                { _lastReplay = _match.MatchId; _match.Replay(); }
            }
            if (_rounds.Count >= 2) Mark("replay");
            foreach (var pair in _checks)
            {
                try { _checkResults[pair.Key] = pair.Value(context); }
                catch (Exception) { Finish("FAIL", "Feature assertion threw: " + pair.Key); return; }
                if (_checkResults[pair.Key].Status == "FAIL") { Finish("FAIL", "Feature assertion failed: " + pair.Key); return; }
            }
            var ready = Seen("joined") && _connectedSeconds >= _scenario.minConnectedSeconds
                && (!_scenario.requireMovement || Seen("movement")) && (!_scenario.requireZone || Seen("zone"))
                && (!_scenario.requireHpDamage || Seen("hpDamage")) && (!_scenario.requireResult || Seen("result"))
                && (!_scenario.requireReplay || Seen("replay")) && (!_scenario.requireHistory || Seen("history"));
            if (_scenario.expectedPlayers > 1) ready &= Seen("playersVisible");
            foreach (var check in _checkResults.Values) ready &= check.Status == "PASS";
            if (ready)
            {
                var activeSeconds = _match.ActiveInputSeconds - _baseInputSeconds;
                if (_scenario.minInputHz > 0 && activeSeconds < 1)
                { if (elapsed >= _scenario.maxDurationSeconds) Finish("INVALID", "Not enough active input samples"); return; }
                var hz = activeSeconds > 0 ? (_match.InputsSent - _baseInputs) / activeSeconds : 0;
                if (_network.Stats.PingsSent > 0 && (double)_network.Stats.PongsLost / _network.Stats.PingsSent >= _scenario.maxPongTimeoutRatio && _network.Stats.PongsLost > 0)
                { Finish("FAIL", "Pong timeout ratio reached scenario limit"); return; }
                if (_scenario.minSnapshotHz > 0 && (_snapshotSeconds < 1 || _match.Snapshots / _snapshotSeconds < _scenario.minSnapshotHz))
                { Finish("FAIL", "Snapshot receive rate below scenario limit"); return; }
                Finish(hz + .001 < _scenario.minInputHz ? "INVALID" : "PASS", hz + .001 < _scenario.minInputHz ? "Generator below requested input rate" : "All scenario assertions observed");
            }
            else if (elapsed >= _scenario.maxDurationSeconds) Finish("FAIL", "Scenario deadline: required observations missing");
        }

        private bool Seen(string id) => _observed.TryGetValue(id, out var value) && value;
        private void Mark(string id) => _observed[id] = true;
        private async void RefreshHistory()
        {
            _historyBusy = true; _nextHistory = Time.realtimeSinceStartup - _started + 2;
            var expectedMatch = _historyMatch;
            try
            {
                await _history.RefreshHistoryAsync(_menu.AccountToken);
                if (!_finished && !_stop.IsCancellationRequested) foreach (var row in _history.Rows)
                    if (row.MatchId == expectedMatch) { Mark("history"); break; }
            }
            catch (Exception) { /* Hết deadline mới fail; API tạm lỗi không tác động gameplay. */ }
            finally { _historyBusy = false; }
        }
        private void Finish(string status, string reason)
        {
            if (_finished) return; _finished = true; _stop.Cancel();
            _evidence.status = status; _evidence.reason = reason;
            if (_network != null)
            {
                var s = _failureStats ?? _network.Stats;
                _evidence.selectedTransport = s.Transport; _evidence.fallbackReason = s.FallbackReason; _evidence.endpoint = _failureEndpoint ?? _network.Endpoint;
                _evidence.metrics.rttP50Ms = s.P50RttMs; _evidence.metrics.rttP95Ms = s.P95RttMs; _evidence.metrics.rttP99Ms = s.P99RttMs;
                _evidence.metrics.pings = s.PingsSent; _evidence.metrics.pongs = s.PongsReceived; _evidence.metrics.pongTimeouts = s.PongsLost;
            }
            var m = _evidence.metrics; m.elapsedSeconds = Time.realtimeSinceStartup - _started; m.connectedSeconds = _connectedSeconds;
            if (_match != null)
            {
                m.inputs = _match.InputsSent - _baseInputs; m.activeInputSeconds = _match.ActiveInputSeconds - _baseInputSeconds;
                m.inputHz = m.activeInputSeconds > 0 ? m.inputs / m.activeInputSeconds : 0;
                m.snapshotSeconds = _snapshotSeconds; m.snapshotHz = _snapshotSeconds > 0 ? _match.Snapshots / _snapshotSeconds : 0;
                m.snapshots = _match.Snapshots; m.ignoredPackets = _match.IgnoredPackets; m.catchupDrops = _match.InputCatchupDrops;
                m.completedMatches = _results.Count; _evidence.matchId = _historyMatch.ToString();
            }
            var assertions = new List<Assertion>();
            foreach (var id in new[] { "joined", "playersVisible", "movement", "zone", "hpDamage", "result", "replay", "history" })
                assertions.Add(new Assertion { id = id, status = Seen(id) ? "PASS" : "NOT_OBSERVED", detail = "Observed during this run" });
            foreach (var pair in _checkResults) assertions.Add(new Assertion { id = pair.Key, status = pair.Value.Status, detail = pair.Value.Detail });
            _evidence.assertions = assertions.ToArray();
            try
            {
                if (!string.IsNullOrWhiteSpace(_output))
                {
                    var path = Path.GetFullPath(_output); Directory.CreateDirectory(Path.GetDirectoryName(path));
                    File.WriteAllText(path, JsonUtility.ToJson(_evidence, true));
                }
            }
            catch (Exception) { Debug.LogError("[OnlineTest] Could not write evidence"); status = "FAIL"; }
            Debug.Log("[OnlineTest] " + status + ": " + reason);
            _menu?.Disconnect();
            Application.Quit(status == "PASS" ? 0 : status == "INVALID" ? 2 : 1);
        }
        private void OnDestroy() { _bus?.Unsubscribe<NetworkStateChanged>(NetworkChanged); _stop.Cancel(); _stop.Dispose(); }
    }
}
