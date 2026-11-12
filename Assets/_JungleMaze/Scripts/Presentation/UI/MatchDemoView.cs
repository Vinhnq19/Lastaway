using System;
using System.Globalization;
using JungleBackend.Protocol.Gameplay;
using JungleMaze.App.Match;
using JungleMaze.App.MatchUi;
using JungleMaze.App.Player;
using JungleMaze.App.Ports;
using JungleMaze.App.World;
using JungleMaze.Core.DI;
using JungleMaze.Domain.MatchUi;
using JungleMaze.Presentation.Combat;
using JungleMaze.Presentation.Controls;
using JungleMaze.Presentation.Player;
using JungleMaze.Presentation.World;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JungleMaze.Presentation.UI
{
    /// <summary>
    /// Small Phase-2 inspection UI and scene adapter. Gives MOV-02 prediction the verified map (same data the server
    /// simulates) and aims from the predicted Hero; full HUD/FOW remain separate tasks.
    /// Supports both authoritative online match and solo offline play.
    /// </summary>
    public sealed class MatchDemoView : MonoBehaviour
    {
        /// <summary>Explicit acceptance runner only; ordinary play always reads WASD/mouse.</summary>
        public bool UseExternalIntent { get; set; }
        private MatchSession _session;
        private OfflineMatchSession _offlineSession;
        private Guid _offlineRound;
        private INetworkClient _network;
        private IMatchConnection _connection;
        private IPlayerHistory _history;
        private IClientRuntimeData _data;
        private IPlayerPoseSource _poses;
        private WorldClientSession _generatedWorld;
        private RoomMapSession _roomMaps;
        private IMatchMenuState _menuState;
        private MatchOverlayFlow _overlayFlow;
        private string _worldHash;
        private HeroController _hero;
        private HeroSpriteAnimator _heroBody;
        private IHeroSkinPreference _skins;
        private int _offlineSkin = int.MinValue;
        private RemotePlayerSpawner _spawner;
        private Camera _camera;
        private HeroCameraFollow _follow;
        private ZoneOverlayView _zoneOverlay;


        private Guid _loadedAck;
        private string _dataError;
        private bool _historyVisible;
        private bool _showDetails;
        private bool _busyHistory;
        private GUIStyle _label;
        private IVisibilityPolicy _worldLabelVisibility;

        [Inject] public void BindWorld(WorldClientSession world) => _generatedWorld=world;
        [Inject] public void BindMaps(RoomMapSession maps) => _roomMaps = maps;

        [Inject]
        public void Construct(MatchSession session, INetworkClient network, IMatchConnection connection,
            IPlayerHistory history, IClientRuntimeData data, IPlayerPoseSource poses, OfflineMatchSession offline = null)
        {
            _session = session;
            _network = network;
            _connection = connection;
            _history = history;
            _data = data;
            _poses = poses;
            _offlineSession = offline;
            if (offline != null) offline.RoundStarted += ResetOfflineHero;
        }

        public void BindOverlay(IMatchMenuState menuState, MatchOverlayFlow overlayFlow)
        {
            _menuState = menuState;
            _overlayFlow = overlayFlow;
        }

        /// <summary>Optional per-target label visibility (null keeps the default-visible behaviour).</summary>
        public void BindWorldLabelVisibility(IVisibilityPolicy policy) => _worldLabelVisibility = policy;

        public bool DetailsVisible => Debug.isDebugBuild && _showDetails && _menuState?.IsOpen != true;

        public bool ShouldHideWorldLabels
        {
            get
            {
                if (_session?.HasResult == true) return true;
                if (_menuState != null && _menuState.IsOpen) return true;
                if (_overlayFlow != null && (_overlayFlow.Phase == MatchOverlayPhase.Killcam ||
                                             _overlayFlow.Phase == MatchOverlayPhase.Spectating ||
                                             _overlayFlow.Phase == MatchOverlayPhase.Eliminated)) return true;
                return false;
            }
        }

        private void Update()
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (Input.GetKeyDown(KeyCode.F3)) _showDetails = !_showDetails;
#else
            _showDetails = false;
#endif
            var inScene = SceneManager.GetActiveScene().name == "Game";
            if (_offlineSession != null && _offlineSession.IsActive && inScene)
            {
                if (_hero == null) _hero = FindAnyObjectByType<HeroController>();
                if (_hero != null)
                {
                    if (_offlineRound != _offlineSession.MatchId) ResetOfflineHero();
                    _hero.enabled = !_offlineSession.IsDead && !(_menuState?.BlocksGameplayInput ?? false);
                    SyncOfflineSkin();
                }
                FollowOwnView(null);
                if (_offlineSession.Zone != null)
                {
                    ShowZone(_offlineSession.Zone.GetStatePacket(_offlineSession.CurrentTick), _offlineSession.CurrentTick);
                }
                else HideRings();
                return;
            }

            if (_session == null) return;
            if (!_session.Connected || !inScene) { HideRings(); return; }
            PrepareScene();
            if (_roomMaps?.HasState == true && !_roomMaps.Verified)
            { _dataError = _roomMaps.Error; HideRings(); return; }
            var mapKey = _session.PhaseState.ConfigHash + "/" + (_roomMaps?.HasState == true ? _roomMaps.State.MapHash : "");
            if (_worldHash != mapKey) _dataError = null;
            if (_session.Phase != MatchPhase.Loading) _loadedAck = Guid.Empty;
            if (_session.Phase == MatchPhase.Loading && _loadedAck != _session.MatchId)
            {
                _dataError = _data.Verify(_session.PhaseState.ConfigVersion, _session.PhaseState.ConfigHash);
                if (_dataError == null && _roomMaps?.HasState != true) _dataError = VerifyGeometry();
                if (_dataError == null) { UsePredictionWorld(); _session.ConfirmLoaded(); _loadedAck = _session.MatchId; }
            }
            // Resume thẳng vào InGame (bỏ qua Loading) vẫn cần map cho prediction; chỉ kiểm, không gửi LoadAck.
            if (_session.Phase == MatchPhase.InGame && _worldHash != mapKey && _dataError == null)
            {
                _dataError = _data.Verify(_session.PhaseState.ConfigVersion, _session.PhaseState.ConfigHash);
                if (_dataError == null) UsePredictionWorld();
            }
            var intent = new JungleMaze.Domain.Player.PlayerIntent();
            intent.MoveX = Input.GetAxisRaw("Horizontal"); intent.MoveY = Input.GetAxisRaw("Vertical");
            var origin = _hero != null ? _hero.transform.position : Vector3.zero;
            // Ngắm từ vị trí đang vẽ (đã dự đoán) để hướng chuột khớp với nhân vật người chơi nhìn thấy.
            if (_session.OwnNetId != 255 && _poses.TryGetPose(_session.OwnNetId, out var ownPose))
                origin = new Vector3(ownPose.X, ownPose.Y, 0);
            else if (_session.Players.TryGet(_session.OwnNetId, out var own) && own.HasState)
                origin = new Vector3(own.Current.X, own.Current.Y, 0);
            if (HeroAim.TryGetTargetDegrees(_camera, origin, Input.mousePosition, out var degrees)) intent.AimDegrees = degrees;
            if (!UseExternalIntent) _session.SetIntent(intent);
            if (_hero != null && _session.OwnNetId != 255)
            {
                var view = _spawner != null ? _spawner.GetView(_session.OwnNetId) : null;
                _hero.transform.position = view != null ? view.transform.position : origin;
                FollowOwnView(view != null ? view.transform : null);
            }
            if (_session.HasZone && _session.Phase == MatchPhase.InGame)
            {
                ShowZone(_session.Zone, _session.ServerTick);
            }
            else HideRings();
        }

        /// <summary>Points the camera at the own PlayerView online (null: back to the Hero).</summary>
        private void FollowOwnView(Transform view)
        {
            if (_follow == null)
            {
                if (_camera == null) _camera = Camera.main;
                if (_camera == null || !_camera.TryGetComponent(out _follow)) return;
            }
            _follow.FollowOverride = view;
        }

        private void PrepareScene()
        {
            if (_roomMaps?.HasState == true)
                foreach (var collider in FindObjectsByType<BoxCollider2D>(FindObjectsSortMode.None))
                    if (collider.gameObject.layer == LayerMask.NameToLayer("HardCover") && !collider.name.StartsWith("World collision ", StringComparison.Ordinal))
                    {
                        collider.enabled = false;
                        foreach (var visual in collider.GetComponentsInChildren<SpriteRenderer>()) visual.enabled = false;
                    }
            if (_camera == null) _camera = Camera.main;
            if (_spawner == null) _spawner = FindAnyObjectByType<RemotePlayerSpawner>();
            if (_hero != null) return;
            _hero = FindAnyObjectByType<HeroController>();
            if (_hero == null) return;
            // Two controllers must never write the same transform: the local foundation is now only a camera proxy.
            _hero.enabled = false;
            if (_hero.TryGetComponent<Rigidbody2D>(out var body)) body.simulated = false;
            foreach (var renderer in _hero.GetComponentsInChildren<SpriteRenderer>()) renderer.enabled = false;
            foreach (var input in FindObjectsByType<PlayerInputReader>(FindObjectsSortMode.None)) input.enabled = false;
            foreach (var probe in FindObjectsByType<LocalWeaponProbe>(FindObjectsSortMode.None)) probe.enabled = false;
            // CombatWeaponPresenter routes connected matches through OnlineCombatSession.
        }

        /// <summary>Map đã khớp hash server → mô phỏng movement client giống hệt server (MOV-02).</summary>
        private void UsePredictionWorld()
        {
            var mapKey = _session.PhaseState.ConfigHash + "/" + (_roomMaps?.HasState == true ? _roomMaps.State.MapHash : "");
            if (_worldHash == mapKey && _session.Prediction.HasWorld) return;
            if (_generatedWorld==null || !_generatedWorld.IsReady)
                _session.Prediction.SetWorld(new MovementSimulation(_data.Bounds, _data.Covers, _data.PlayerRadius, _data.PlayerSpeed));
            _worldHash = mapKey;
        }

        private string VerifyGeometry()
        {
            var grid = FindAnyObjectByType<MapGridView>();
            var map = _data.Bounds;
            if (grid == null || Mathf.Abs(grid.MapSizeCells - (map.MaxX - map.MinX)) > .001f ||
                Mathf.Abs(grid.MapSizeCells - (map.MaxY - map.MinY)) > .001f ||
                Vector3.Distance(grid.transform.position, new Vector3(map.MinX, map.MinY, 0)) > .001f ||
                Vector3.Distance(grid.transform.lossyScale, Vector3.one) > .001f)
                return "Kích thước/vị trí map trong scene khác server; khôi phục map demo.";
            var colliders = FindObjectsByType<BoxCollider2D>(FindObjectsSortMode.None);
            foreach (var cover in _data.Covers)
            {
                var found = false;
                foreach (var collider in colliders)
                {
                    if (collider.name != cover.Name || collider.gameObject.layer != LayerMask.NameToLayer("HardCover")) continue;
                    var bounds = collider.bounds;
                    if (Mathf.Abs(bounds.min.x - cover.MinX) > .002f || Mathf.Abs(bounds.max.x - cover.MaxX) > .002f ||
                        Mathf.Abs(bounds.min.y - cover.MinY) > .002f || Mathf.Abs(bounds.max.y - cover.MaxY) > .002f)
                        return "Vật cản trong scene khác dữ liệu server; đồng bộ lại map.";
                    found = true; break;
                }
                if (!found) return "Scene thiếu vật cản của map server; cập nhật client.";
            }
            return null;
        }

        private void ShowZone(ZoneStatePacket state, uint tick)
        {
            if (_zoneOverlay == null) _zoneOverlay = gameObject.AddComponent<ZoneOverlayView>();
            _zoneOverlay.Show(state, tick, _data.Bounds);
        }
        /// <summary>Home's skin choice for the offline hero (default hero when none). Optional: views built in tests skip it.</summary>
        [Inject] public void BindSkins(IHeroSkinPreference skins) => _skins = skins;

        /// <summary>
        /// Draws the offline hero with the chosen skin; only touches the animator when the skin changes. A reloaded Game
        /// scene brings a fresh Hero with the prefab's default skin, so finding a new body forgets the applied skin.
        /// </summary>
        private void SyncOfflineSkin()
        {
            if (_hero == null) return;
            if (_heroBody == null)
            {
                _heroBody = _hero.GetComponentInChildren<HeroSpriteAnimator>(true);
                _offlineSkin = int.MinValue;
                if (_heroBody == null) return;
            }
            var skin = HeroSkinRules.Offline(_skins?.Selected ?? HeroSkinRules.Automatic);
            if (skin == _offlineSkin) return;
            _offlineSkin = skin;
            _heroBody.SetHero(skin);
        }

        private void ResetOfflineHero()
        {
            if (_hero == null) _hero = FindAnyObjectByType<HeroController>();
            if (_hero == null) return;
            _offlineRound = _offlineSession.MatchId;
            var c = _offlineSession.Zone.CurrentCenter;
            _hero.transform.position = new Vector3(c.X, c.Y, 0);
            if (_hero.TryGetComponent<Rigidbody2D>(out var rb)) { rb.simulated = true; rb.position = new Vector2(c.X, c.Y); rb.linearVelocity = Vector2.zero; }
            foreach (var renderer in _hero.GetComponentsInChildren<SpriteRenderer>(true)) renderer.enabled = true;
            if (_hero.TryGetComponent<LocalHeroPoseReporter>(out var reporter)) reporter.Report();
        }

        private void HideRings()
        { if (_zoneOverlay != null) _zoneOverlay.Hide(); }
        private void OnDestroy() { if (_offlineSession != null) _offlineSession.RoundStarted -= ResetOfflineHero; if (_zoneOverlay != null) Destroy(_zoneOverlay); }

        /// <summary>
        /// Safe short label for an overhead name: control characters (newlines) and angle brackets are suppressed so the
        /// label cannot break layout or inject markup, whitespace is trimmed, and long names are truncated by text element
        /// (grapheme) count so surrogate pairs and combining marks are never split.
        /// </summary>
        public static string ShortOverheadName(string name)
        {
            const int maxElements = 16;
            const string fallback = "Người chơi";
            if (string.IsNullOrEmpty(name)) return fallback;
            var builder = new System.Text.StringBuilder(name.Length);
            foreach (var c in name)
            {
                if (char.IsControl(c) || c == '<' || c == '>') continue;
                builder.Append(c);
            }
            var clean = builder.ToString().Trim();
            if (clean.Length == 0) return fallback;
            var info = new StringInfo(clean);
            if (info.LengthInTextElements <= maxElements) return clean;
            var shortened = info.SubstringByTextElements(0, maxElements).TrimEnd();
            return shortened.Length == 0 ? fallback : shortened + "…";
        }

        private void OnGUI()
        {
            _label ??= new GUIStyle(GUI.skin.label) { wordWrap = true, richText = false };

            if (_offlineSession != null && _offlineSession.IsActive)
            {
                if (!DetailsVisible) return;
                GUILayout.BeginArea(new Rect(10, 52, 370, Mathf.Min(Screen.height - 62, 220)), GUI.skin.box);
                GUILayout.Label("Lastaway · Solo Offline", _label);
                GUILayout.Label($"Trận {_offlineSession.MatchId.ToString().Substring(0, 8)} · 1 người chơi", _label);
                GUILayout.Label($"HP: {_offlineSession.PlayerHp} · Thời gian: {_offlineSession.ElapsedSeconds:F1}s", _label);
                if (_offlineSession.Zone != null)
                    GUILayout.Label($"Bo {_offlineSession.Zone.PhaseIndex}: r={_offlineSession.Zone.CurrentRadius:F1}" + (_offlineSession.Zone.InWarning ? " · Sắp co" : ""), _label);
                GUILayout.Label("WASD di chuyển, chuột chọn hướng. Đi ra ngoài bo sẽ mất máu.", _label);
                if (_offlineSession.HasResult)
                {
                    GUILayout.Label("KẾT QUẢ SINH TỒN (SOLO)", _label);
                    GUILayout.Label($"#1 {_offlineSession.DisplayName} · Sống sót: {_offlineSession.SurvivalSeconds:F1}s", _label);
                    if (GUILayout.Button("Sẵn sàng chơi lại")) _offlineSession.Replay();
                }
                GUILayout.EndArea();
                return;
            }

            if (_session == null) return;
            if (!_session.Connected && !_session.CanResume) return;
            if (_session.Connected && _camera != null && _spawner != null && !ShouldHideWorldLabels)
                foreach (var id in _session.Players.ActiveIds)
                {
                    var view = _spawner.GetView(id);
                    if (id == _session.OwnNetId) continue;
                    if (view == null || !view.gameObject.activeInHierarchy || !view.isActiveAndEnabled) continue;
                    if (!_session.Players.TryGet(id, out var player) || !player.HasState) continue;
                    if (player.Current.IsDead || player.Current.Hp == 0) continue;
                    if (_worldLabelVisibility != null &&
                        !_worldLabelVisibility.IsVisible(view.transform.position.x, view.transform.position.y, id)) continue;
                    var point = _camera.WorldToScreenPoint(view.transform.position);
                    if (point.z <= 0) continue;
                    var name = "Player " + id;
                    foreach (var row in _session.Roster) if (row.NetId == id) { name = row.DisplayName; break; }
                    GUI.Label(new Rect(point.x - 50, Screen.height - point.y - 30, 150, 25), ShortOverheadName(name), _label);
                }
            var worldStatus = _generatedWorld != null && _generatedWorld.Required && _session.Phase == MatchPhase.InGame && !_generatedWorld.IsReady
                ? (_generatedWorld.Error != null ? "Không thể tải bản đồ. Dữ liệu client và server không khớp; hãy cập nhật game." : "Đang đồng bộ bản đồ và túi đồ…") : null;
            if (!DetailsVisible)
            {
                // Normal play uses the composed Lobby/HUD/results UI; only critical load errors stay visible.
                if (_session.Phase == MatchPhase.InGame && (_dataError != null || worldStatus != null))
                    GUI.Box(new Rect(10, 52, 370, 62), _dataError != null
                        ? (_roomMaps?.Error ?? "Dữ liệu trận chưa hợp lệ. Hãy cập nhật game rồi thử lại.") : worldStatus, _label);
                return;
            }
            GUILayout.BeginArea(new Rect(_session.Connected ? 10 : 420, 52, 370, Mathf.Min(Screen.height - 62, 590)), GUI.skin.box);
            GUILayout.Label("Lastaway · Demo Phase 2", _label);
            if (!_session.Connected)
            {
                GUILayout.Label("Mất kết nối. Có thể khôi phục nếu phiên còn grace.", _label);
                if (GUILayout.Button("Khôi phục nhân vật")) _connection.Resume(_session.ResumeProof);
                GUILayout.EndArea(); return;
            }
            GUILayout.Label($"{_session.Phase} · {_session.PhaseState.PlayerCount}/{_session.PhaseState.MaxPlayers} người", _label);
            if (_session.Phase == MatchPhase.Lobby || _session.Phase == MatchPhase.Loading)
                GUILayout.Label($"Còn {_session.CountdownSeconds:F1}s · tối thiểu {_session.PhaseState.MinPlayers} người", _label);
            if (_session.Phase == MatchPhase.Waiting || _session.Phase == MatchPhase.Lobby || _session.Phase == MatchPhase.Loading)
                foreach (var row in _session.Roster) GUILayout.Label(row.DisplayName, _label);
            GUILayout.Label($"HP: {_session.OwnHp} · Tick server: {_session.ServerTick} · Snapshot: {_session.Snapshots}", _label);
            if (_dataError != null) GUILayout.Label(_dataError, _label);
            if (worldStatus != null) GUILayout.Label(worldStatus, _label);
            if (_session.HasZone) GUILayout.Label($"Bo {_session.Zone.PhaseIndex}: r={_session.Zone.CurrentRadius:F1}" + (_session.Zone.InWarning ? " · Sắp co" : ""), _label);
            var p = _session.Prediction.Stats;
            GUILayout.Label($"WASD di chuyển, chuột trái đánh, R nạp đạn, F nhặt đồ; 1–4 chọn vũ khí, 5–8 dùng dược. Prediction: {(_session.Prediction.IsActive ? "bật" : "chờ")} · sửa {p.Corrections} · lệch max {p.MaxErrorMeters:F2}.", _label);
            if (_session.HasResult)
            {
                GUILayout.Label(_session.Result.Reason == MatchEndReason.Tie ? "HÒA — không có người thắng" : "KẾT QUẢ TRẬN", _label);
                foreach (var row in _session.Result.Participants ?? Array.Empty<MatchParticipantState>())
                    GUILayout.Label($"#{row.Rank} {row.DisplayName} · hạ gục {row.Kills} · {row.SurvivalTicks / 30f:F1}s", _label);
                if (_session.Phase == MatchPhase.Ending && GUILayout.Button("Sẵn sàng chơi lại")) _session.Replay();
            }
            GUILayout.Label(_history.Status, _label);
            GUI.enabled = !_busyHistory;
            if (GUILayout.Button("Xem / cập nhật lịch sử")) RefreshHistory();
            GUI.enabled = true;
            if (_historyVisible)
                foreach (var row in _history.Rows)
                    GUILayout.Label($"{row.EndedAt.ToLocalTime():dd/MM HH:mm} · #{row.Rank} · {row.SurvivalSeconds:F1}s · {row.Reason}", _label);
            GUILayout.EndArea();
        }

        private async void RefreshHistory()
        {
            _busyHistory = true;
            try { await _history.RefreshHistoryAsync(_connection.LastRequest.AccessToken); _historyVisible = true; }
            finally { _busyHistory = false; }
        }
    }
}
