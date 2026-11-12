using System;
using System.Globalization;
using JungleMaze.App.Ports;
using JungleMaze.App.Config;
using JungleBackend.Protocol.Gameplay;
using JungleMaze.App.Lobby;
using JungleMaze.App.Match;
using JungleMaze.Domain.Lobby;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace JungleMaze.Presentation.UI.Lobby
{
    /// <summary>
    /// Connects <see cref="LobbyFlow"/> to the prefab panels. Buttons call the flow; each frame the visible panel is
    /// re-read and redrawn only when its data changed. One panel per <see cref="LobbyScreen"/>, swapped by CanvasGroup
    /// in the frame the screen changes; the canvas is off while the match runs (HUD and UI-03 own that screen).
    /// The join form is a modal over Home; logout confirm and server settings are modals driven by the flow flags.
    /// The endpoint picker (now in the server-settings modal) reuses <see cref="ConnectionMenuPresenter"/> as is (T-2).
    /// </summary>
    public sealed partial class LobbyUiPresenter : IDisposable
    {
        public const string EmptySlot = "Chỗ trống";
        public const string SelfTag = "Bạn";
        public const string ReplayWaiting = "Đang chờ người khác…";

        private readonly LobbyUiRoot _root;
        private readonly LobbyFlow _flow;
        private readonly ConnectionMenuPresenter _menu;
        private readonly Action _quit;
        private readonly Action<string> _copy;
        private readonly Func<bool> _settingsOpen;
        private readonly Action<string> _rememberName;
        private readonly UnityAction[] _unbind;
        private LobbyScreen _shown = (LobbyScreen)255;
        private int _drawnKey = int.MinValue;
        private bool _serverShown;
        private readonly RoomMapSession _maps;
        private readonly IVpnHostTools _vpn;
        private readonly ILanHostTools _lan;
        private bool _vpnBusy, _disposed;
        private int _vpnRequest;
        private string _vpnMessage = "Dán mã kết nối hoặc thiết lập máy host.", _vpnHostCode;


        public LobbyUiPresenter(LobbyUiRoot root, LobbyFlow flow, ConnectionMenuPresenter menu = null, Action quit = null,
            Action<string> copy = null, Func<bool> settingsOpen = null, RoomMapSession maps = null, IVpnHostTools vpn = null)
        {
            _root = root ? root : throw new ArgumentNullException(nameof(root));
            _flow = flow ?? throw new ArgumentNullException(nameof(flow));
            _menu = menu;
            _quit = quit ?? Quit;
            _copy = copy ?? (text => GUIUtility.systemCopyBuffer = text);
            _settingsOpen = settingsOpen;
            _maps = maps;
            _vpn = vpn;
            _lan = vpn as ILanHostTools;
            _unbind = BindButtons();
            BindInputs();
            FillEndpoint();
            var practiceBtn = root.Login.OfflinePractice;
            if (practiceBtn != null)
            {
                practiceBtn.gameObject.SetActive(true);
                SetLabel(practiceBtn, "Luyện tập offline");
            }
            foreach (var panel in root.Panels) panel.Show(false);
            Tick();
        }

        public LobbyScreen Shown => _shown;
        public int CopyCount { get; private set; }

        public void Tick()
        {
            var screen = _flow.Screen;
            if (screen != _shown) SwapPanels(screen);
            if (screen == LobbyScreen.Settings && _settingsOpen != null && !_settingsOpen()) _flow.SettingsClosed();
            Redraw(screen);
            DrawModals();
        }

        public void Dispose()
        {
            _disposed = true;
            _root.ServerSettings.Profile.onValueChanged.RemoveListener(SelectProfile);
            if(_root.ServerSettings.DevProfile != null)_root.ServerSettings.DevProfile.onValueChanged.RemoveListener(SelectDevProfile);
            if(_root.ServerSettings.VpnCode != null)_root.ServerSettings.VpnCode.onValueChanged.RemoveAllListeners();
            foreach (var unbind in _unbind) unbind();
            var j = _root != null ? _root.JoinForm : null;
            if (j != null)
            {
                j.DisplayName.onValueChanged.RemoveAllListeners();
                j.RoomCode.onValueChanged.RemoveAllListeners();
            }
        }

        // ---- panels --------------------------------------------------------------------------------------------

        public static LobbyPanelView PanelFor(LobbyUiRoot r, LobbyScreen s)
        {
            switch (s)
            {
                case LobbyScreen.MainMenu:
                case LobbyScreen.Settings: return r.MainMenu;
                case LobbyScreen.JoinForm: return r.JoinForm;
                case LobbyScreen.Connecting: return r.Connecting;
                case LobbyScreen.Login: return r.Login;
                case LobbyScreen.RoomManager: return r.RoomManager;
                case LobbyScreen.History: return r.History;
                case LobbyScreen.Leaderboard: return r.Leaderboard;
                case LobbyScreen.Lobby: return r.Lobby;
                case LobbyScreen.Loading: return r.Loading;
                case LobbyScreen.Results: return r.Results;
                case LobbyScreen.Reconnecting: return r.Reconnect;
                case LobbyScreen.LoadError:
                case LobbyScreen.Disconnected: return r.Notice;
                default: return null; // InGame
            }
        }

        private void SwapPanels(LobbyScreen screen)
        {
            var target = PanelFor(_root, screen);
            foreach (var panel in _root.Panels) panel.Show(panel == target);
            if (screen == LobbyScreen.JoinForm) _root.MainMenu.ShowUnder(); // modal over Home, behind its scrim
            if (_root.Canvas != null) _root.Canvas.enabled = screen != LobbyScreen.InGame;
            var splash = screen == LobbyScreen.Login || screen == LobbyScreen.MainMenu;
            _root.SetSplashForMenu(screen == LobbyScreen.MainMenu);
            if (_root.Splash != null) _root.Splash.SetActive(splash);
            if (_root.Backdrop != null) _root.Backdrop.SetActive(HasFloor(screen) && !splash); // skin A: floor behind A6, A7, B6, B7 too
            _shown = screen;
            _drawnKey = int.MinValue;
            OnEnter(screen);
        }

        /// <summary>
        /// Screens drawn on the floor backdrop (mocks A2 to A7, B6, B7, B8 hết hạn); only the in-match reconnect countdown
        /// (B8 đếm ngược) keeps the HUD behind.
        /// </summary>
        public static bool HasFloor(LobbyScreen s) =>
            LobbyScreenRules.IsMenu(s) || s == LobbyScreen.Lobby || s == LobbyScreen.Loading || s == LobbyScreen.LoadError
            || s == LobbyScreen.Results || s == LobbyScreen.Disconnected;

        private void OnEnter(LobbyScreen screen)
        {
            switch (screen)
            {
                case LobbyScreen.MainMenu:
                    _flow.Api?.Check();
                    break;
                case LobbyScreen.JoinForm:
                    _root.JoinForm.DisplayName.SetTextWithoutNotify(_flow.Join.DisplayName);
                    _root.JoinForm.RoomCode.SetTextWithoutNotify(_flow.Join.RoomCode);
                    var user = _flow.Account?.Username?.Trim();
                    _root.JoinForm.Hint.text = string.IsNullOrEmpty(user) ? "" : "Vào bằng tài khoản " + user + " · tên hiển thị lấy từ tài khoản";
                    break;
                case LobbyScreen.Login:
                    _flow.Api?.Check(); // status line + Thử lại when the account server is down
                    _root.Login.Username.SetTextWithoutNotify(_flow.Account?.Username ?? "");
                    _root.Login.Password.SetTextWithoutNotify("");
                    break;
            }
        }

        private void Redraw(LobbyScreen screen)
        {
            switch (screen)
            {
                case LobbyScreen.MainMenu:
                case LobbyScreen.Settings: DrawMainMenu(); break;
                case LobbyScreen.JoinForm: DrawJoin(); break;
                case LobbyScreen.Connecting: DrawConnecting(); break;
                case LobbyScreen.Login: DrawLogin(); break;
                case LobbyScreen.RoomManager: DrawRooms(); break;
                case LobbyScreen.History: DrawHistory(); break;
                case LobbyScreen.Leaderboard: DrawLeaderboard(); break;
                case LobbyScreen.Lobby: DrawLobby(); break;
                case LobbyScreen.Loading: DrawLoading(); break;
                case LobbyScreen.Results: DrawResults(); break;
                case LobbyScreen.Reconnecting: DrawReconnect(); break;
                case LobbyScreen.LoadError:
                case LobbyScreen.Disconnected: DrawNotice(screen); break;
            }
        }

        private bool Changed(int key)
        {
            if (key == _drawnKey) return false;
            _drawnKey = key;
            return true;
        }

        // ---- draw ----------------------------------------------------------------------------------------------

        private void DrawMainMenu()
        {
            var v = _root.MainMenu;
            var api = _flow.Api?.Text ?? ApiMessages.NoApi;
            var user = _flow.Account?.Username?.Trim() ?? "";
            var ready = _flow.Api != null && _flow.Api.Ready;
            if (!Changed(Hash(api, user, ready ? 1 : 0))) return;
            v.ApiStatus.text = api;
            v.ShowApiReady(ready);
            v.AccountStatus.text = "Đã đăng nhập";
            v.AccountName.text = user;
            SetLabel(v.Logout, "Đăng xuất");
            v.Join.interactable = true; // AC3: never blocked by the API
            if (v.OfflinePractice != null) v.OfflinePractice.interactable = true;
        }

        private void DrawModals()
        {
            var r = _root;
            r.LogoutConfirm.Show(_flow.LogoutConfirmOpen);
            if (_flow.LogoutConfirmOpen) r.LogoutConfirm.Text.text = "Đăng xuất khỏi " + (_flow.Account?.Username?.Trim() ?? "") + "?";
            var open = _flow.ServerSettingsOpen;
            if (open != _serverShown)
            {
                _serverShown = open;
                if(!open){_vpnRequest++;_pendingLan=null;}
                r.ServerSettings.Show(open);
                if (open) FillEndpoint();
                r.ServerSettings.Audio.gameObject.SetActive(_flow.Screen == LobbyScreen.MainMenu && _flow.OpenSettingsPanel != null);
            }
            if (open)
            {
                var v = r.ServerSettings;
                v.Status.text = _serverMode == 0 ? _vpnMessage : _menu?.Status ?? "";
                v.ShowMode(_serverMode, _pendingLan != null, _devSettings);
                if (v.VpnStatus != null) v.VpnStatus.text = _vpnMessage;
                if (v.VpnBackend != null) v.VpnBackend.text = _vpn?.BackendPath ?? "Chưa chọn Backend. Người chơi không cần source.";
                foreach (var button in new[] { v.VpnConnect, v.VpnClientSetup, v.VpnBrowse, v.VpnSetup, v.VpnStart, v.VpnStop, v.VpnRefresh })
                    if (button != null) button.interactable = !_vpnBusy && (_serverMode == 1 ? _lan != null : _vpn != null);
                v.Save.interactable = !_vpnBusy;
                if(v.TrustConfirm != null)v.TrustConfirm.interactable = !_vpnBusy;
                if(v.LanInterface != null)v.LanInterface.interactable = !_vpnBusy;
                if (v.VpnCopy != null) v.VpnCopy.interactable = !_vpnBusy && !string.IsNullOrEmpty(_vpnHostCode);
            }
        }

        private void DrawJoin()
        {
            var j = _flow.Join;
            if (!Changed(Hash(j.Message, j.NameErrorText, j.CodeErrorText, j.CanSubmit ? 1 : 0, j.TimedOut ? 1 : 0, j.FocusCode ? 1 : 0))) return;
            var v = _root.JoinForm;
            v.NameError.text = j.NameErrorText;
            v.CodeError.text = j.CodeErrorText;
            v.Message.text = j.Message;
            v.Join.interactable = j.CanSubmit;
            v.Retry.gameObject.SetActive(!j.Busy && (j.TimedOut || !string.IsNullOrEmpty(j.Message)));
            v.Retry.interactable = j.CanSubmit;
            if (j.FocusCode) { v.RoomCode.Select(); v.RoomCode.ActivateInputField(); }
        }

        private void DrawConnecting()
        {
            var c = _flow.Connection;
            if (!Changed(Hash(c.Step, c.RttMs, c.State))) return;
            _root.Connecting.Step.text = c.State == JungleMaze.Domain.Network.NetworkState.Authenticating
                ? "Đang kiểm tra quyền vào phòng…" : "Đang kết nối máy chủ…";
            _root.Connecting.Rtt.text = c.RttMs >= 0 ? "Độ trễ " + c.RttMs + " ms" : "";
        }

        private void DrawLogin()
        {
            var a = _flow.Account;
            var api = _flow.Api;
            var apiDown = api != null && !api.Ready && !api.Checking;
            if (a == null || !Changed(Hash(a.Message, a.RegisterMode ? 1 : 0, a.Busy ? 1 : 0, api?.Text, apiDown ? 1 : 0))) return;
            var v = _root.Login;
            v.ApiStatus.text = api == null || api.Ready ? "" : api.Text;
            v.Retry.gameObject.SetActive(apiDown);
            v.Title.text = a.RegisterMode ? "Đăng ký" : "Đăng nhập";
            v.SetTab(a.RegisterMode);
            v.Message.text = a.Message;
            v.Submit.interactable = !a.Busy;
            SetLabel(v.Submit, a.RegisterMode ? "Đăng ký" : "Đăng nhập");
        }

        private void DrawRooms()
        {
            var m = _flow.Rooms;
            if (m == null || !Changed(m.Version * 31 + (m.Creating ? 1 : 0) + (m.Deleting ? 2 : 0))) return;
            var v = _root.RoomManager;
            v.State.text = m.State == ListState.Loading ? "Đang tải…" : m.Message;
            v.Retry.gameObject.SetActive(m.State == ListState.Unavailable);
            v.Create.interactable = !m.Creating;
            for (var i = 0; i < v.Rows.Length; i++)
            {
                var row = v.Rows[i];
                var show = i < m.Rooms.Count && m.State != ListState.Loading;
                row.gameObject.SetActive(show);
                if (!show) continue;
                var room = m.Rooms[i];
                row.RoomName.text = room.Name;
                row.Occupancy.text = RoomListModel.Occupancy(room);
                row.Code.text = room.IsDefault ? "Mặc định" : room.RoomCode ?? "";
                row.Copy.gameObject.SetActive(room.RoomCode != null);
                row.Delete.gameObject.SetActive(RoomListModel.CanDelete(room));
                row.Delete.interactable = !m.Deleting;
            }
            v.ConfirmGroup.alpha = m.PendingDelete.HasValue ? 1f : 0f;
            v.ConfirmGroup.interactable = v.ConfirmGroup.blocksRaycasts = m.PendingDelete.HasValue;
            if (m.PendingDelete.HasValue) v.ConfirmText.text = "Xóa phòng «" + PendingName(m) + "»?";
        }

        private static string PendingName(RoomListModel m)
        {
            foreach (var room in m.Rooms) if (room.Id == m.PendingDelete) return room.Name;
            return "";
        }

        private void DrawHistory()
        {
            var h = _flow.History;
            if (h == null) return;
            if (!Changed(Hash(h.State, h.Rows.Count, h.Message, h.IdentityLabel))) return;
            var v = _root.History;
            v.Identity.text = h.IdentityLabel;
            v.State.text = h.State == ListState.Loading ? "Đang tải…" : h.Message;
            v.Retry.gameObject.SetActive(h.State == ListState.Unavailable);
            for (var i = 0; i < v.Rows.Length; i++)
            {
                var show = i < h.Rows.Count;
                v.Rows[i].gameObject.SetActive(show);
                if (show) v.Rows[i].Line.text = HistoryModel.Line(h.Rows[i]);
            }
        }

        private void DrawLeaderboard()
        {
            var b = _flow.Leaderboard;
            if (b == null || !Changed(Hash(b.State, b.Rows.Length, b.Message, b.MatchId))) return;
            var v = _root.Leaderboard;
            v.State.text = b.State == ListState.Loading ? "Đang tải…" : b.Message;
            v.Retry.gameObject.SetActive(b.State == ListState.Unavailable);
            for (var i = 0; i < v.Rows.Length; i++)
            {
                var show = i < b.Rows.Length;
                var row = v.Rows[i];
                row.gameObject.SetActive(show);
                if (!show) continue;
                var e = b.Rows[i];
                row.Rank.text = "#" + e.Rank;
                row.PlayerName.text = (e.IsBot ? "[BOT] " : "") + e.Name;
                row.Kills.gameObject.SetActive(_flow.Options.ShowKills);
                row.Kills.text = e.Kills.ToString(CultureInfo.InvariantCulture);
                row.Survival.text = ResultRow.Clock((float)e.SurvivalSeconds);
                row.Background.enabled = false;
            }
        }

        private void DrawLobby()
        {
            var s = _flow.Lobby;
            var slots = s.Slots;
            var key = Hash(s.MatchId, slots.Count, s.MaxPlayers, s.MinPlayers, _flow.HasCountdown ? _flow.CountdownSeconds : -1,
                _flow.Notice, _flow.ReadyRequests, s.RoomCode, _maps?.State.SelectionRevision, _maps?.State.ReadyCount,
                _maps?.State.HostNetId, _maps?.CanStart, _maps?.Busy, _maps?.Error, _maps?.Notice, _maps?.State.MapName, _maps?.State.LobbyRevision);
            for (var i = 0; i < slots.Count; i++) key = key * 31 + slots[i].NetId * 7 + (slots[i].IsSelf ? 1 : 0) + (slots[i].Ready ? 2 : 0);
            if (!Changed(key)) return;
            var v = _root.Lobby;
            var o = _flow.Options;
            v.RoomCode.text = s.RoomCode;
            v.Copy.gameObject.SetActive(!string.IsNullOrEmpty(s.RoomCode));
            var max = Mathf.Clamp(s.MaxPlayers, 1, v.Slots.Length);
            for (var i = 0; i < v.Slots.Length; i++)
            {
                var row = v.Slots[i];
                row.gameObject.SetActive(i < max);
                if (i >= max) continue;
                if (i < slots.Count)
                {
                    var slot = slots[i];
                    row.NetId = slot.NetId;
                    row.PlayerName.text = slot.Name;
                    row.Badge.text = slot.IsSelf ? SelfTag : o.ReadyEnabled && slot.HasReady && slot.Ready ? "Sẵn sàng" : "";
                    row.Tint(true, slot.IsSelf);
                }
                else
                {
                    row.NetId = 255;
                    row.PlayerName.text = EmptySlot;
                    row.Badge.text = "";
                    row.Tint(false, false);
                }
            }
            var count = _maps != null && _maps.HasState ? _maps.State.PlayerCount : s.PlayerCount > 0 ? s.PlayerCount : slots.Count;
            v.Count.text = _maps != null && _maps.HasState
                ? _maps.State.HumanCount + " người + " + _maps.State.BotCount + " bot / " + _maps.State.MaxPlayers + " · tối thiểu " + s.MinPlayers
                : count + "/" + s.MaxPlayers + " · tối thiểu " + s.MinPlayers;
            v.Status.text = _flow.LobbyStatusText;
            v.Rule.text = o.ReadyEnabled ? "" : _flow.AutoStartText;
            v.Notice.text = _flow.Notice;
            v.Ready.gameObject.SetActive(o.ReadyEnabled);
            v.Ready.interactable = _flow.ReadyRequests == 0;
            v.ReadyLabel.text = _flow.ReadyRequests > 0 ? "Đã sẵn sàng" : "Sẵn sàng";
            v.Start.gameObject.SetActive(o.StartEnabled);
            v.Start.interactable = false;
            if (_maps != null && _maps.HasState)
            {
                v.Ready.gameObject.SetActive(false);
                v.Start.gameObject.SetActive(true);
                SetLabel(v.Start, _maps.IsHost ? "Bắt đầu" : "Chờ chủ phòng");
                v.Start.interactable = _maps.CanStart;
                if (!_flow.HasCountdown)
                    v.Status.text = count < s.MinPlayers ? "Chờ đủ " + s.MinPlayers + " người" :
                        _maps.State.ReadyCount < _maps.State.PlayerCount ? "Đang chờ tải map" : "Sẵn sàng · chờ trưởng phòng";
                v.Rule.text = _maps.State.MapName + " · v" + _maps.State.MapVersion + " · Đã tải " + _maps.State.ReadyCount + "/" + _maps.State.PlayerCount;
                v.Notice.text = _maps.Error ?? _maps.Notice ?? (_maps.Busy ? "Đang tải và xác minh map…" : _maps.IsHost ? "Bạn là trưởng phòng · chọn map rồi Bắt đầu" : "Chờ chủ phòng chọn map và bắt đầu");
                for (var i = 0; i < slots.Count && i < v.Slots.Length; i++)
                    if (slots[i].NetId == _maps.State.HostNetId) v.Slots[i].Badge.text = "Trưởng phòng";
            }
        }

        private void DrawLoading()
        {
            var seconds = _flow.HasCountdown ? _flow.CountdownSeconds : -1;
            if (!Changed(Hash(seconds, _flow.Lobby.MatchId))) return;
            var v = _root.Loading;
            v.StepMap.text = "1. Tải dữ liệu bản đồ";
            v.StepVersion.text = "2. Kiểm tra phiên bản";
            v.StepPlayers.text = "3. Chờ người chơi";
            v.TimeLeft.text = seconds >= 0 ? "Còn " + seconds + " s" : "";
            v.Hint.text = _flow.Options.ControlsHint;
            var tips = _flow.Options.Tips;
            v.Tip.text = tips != null && tips.Length > 0 ? "Mẹo: " + tips[(int)((uint)_flow.Lobby.MatchId.GetHashCode() % tips.Length)] : "";
            v.SetCountdown(seconds);
        }

        private void DrawResults()
        {
            var r = _flow.Result;
            var isOffline = _flow.IsOffline || r.IsOffline;
            var apiReady = !isOffline && _flow.Api != null && _flow.Api.Ready;
            var ending = isOffline ? true : _flow.Lobby.Phase == MatchPhase.Ending;
            if (!Changed(Hash(r.MatchId, r.HasData ? 1 : 0, _flow.ReplayPending ? 1 : 0, apiReady ? 1 : 0, ending ? 1 : 0, isOffline ? 1 : 0))) return;
            var v = _root.Results;
            v.Title.text = r.HasData ? r.Title : "Đang chờ kết quả…";
            SetOptional(v.Rank, r.RankText);
            SetOptional(v.Kills, r.KillsText);
            SetOptional(v.Survival, r.SurvivalText);
            SetOptional(v.Cause, r.CauseText);
            v.ShowDecor(r.HasData && r.Headline == ResultHeadline.Victory);
            v.OnlineLine.text = isOffline ? "" : (apiReady ? "" : ApiMessages.HistoryUnavailableLine);
            v.Online.gameObject.SetActive(apiReady && r.HasData && !isOffline);
            v.Replay.interactable = ending && !_flow.ReplayPending;
            v.ReplayLabel.text = _flow.ReplayPending ? ReplayWaiting : "Chơi lại";
            for (var i = 0; i < v.Rows.Length; i++)
            {
                var row = v.Rows[i];
                var show = i < r.Rows.Count;
                row.gameObject.SetActive(show);
                if (!show) continue;
                var e = r.Rows[i];
                row.Rank.text = "#" + e.Rank;
                row.PlayerName.text = (e.IsBot ? "[BOT] " : "") + e.Name;
                row.Kills.gameObject.SetActive(_flow.Options.ShowKills);
                row.Kills.text = e.Kills.ToString(CultureInfo.InvariantCulture);
                row.Survival.text = ResultRow.Clock(e.SurvivalSeconds);
                row.Background.enabled = e.IsSelf;
            }
        }

        private void DrawReconnect()
        {
            var left = _flow.Reconnect.SecondsLeft;
            if (!Changed(left)) return;
            _root.Reconnect.Message.text = "Đang kết nối lại…";
            _root.Reconnect.Seconds.text = "Còn khoảng " + left + " s";
            _root.Reconnect.SetCountdown(left);
        }

        private void DrawNotice(LobbyScreen screen)
        {
            var text = screen == LobbyScreen.LoadError ? _flow.LoadErrorText : _flow.Notice;
            if (!Changed(Hash((int)screen, text))) return;
            var title = screen == LobbyScreen.LoadError ? "Không vào được trận" : "Mất kết nối";
            _root.Notice.Title.text = title;
            // QA T25-2: ReconnectController.LostText is the title itself; show it once (mock B8 hết hạn has no repeat).
            _root.Notice.Message.text = string.Equals(text?.Trim(), title, StringComparison.OrdinalIgnoreCase) ? "" : text;
            _root.Notice.ShowKind(screen == LobbyScreen.LoadError);
        }

        // ---- input ---------------------------------------------------------------------------------------------

        private UnityAction[] BindButtons()
        {
            var r = _root;
            var f = _flow;
            var list = new System.Collections.Generic.List<UnityAction>();
            void On(Button b, UnityAction a)
            {
                if (b == null) return;
                b.onClick.AddListener(a);
                list.Add(() => b.onClick.RemoveListener(a));
            }

            On(r.MainMenu.Join, f.OpenJoin);
            On(r.MainMenu.OfflinePractice, f.OpenOffline);
            On(r.MainMenu.Rooms, f.OpenRooms);
            On(r.MainMenu.History, f.OpenHistory);
            On(r.MainMenu.Leaderboard, f.OpenLatestLeaderboard);
            On(r.MainMenu.Settings, f.OpenServerSettings);
            On(r.MainMenu.Quit, () => _quit());
            On(r.MainMenu.Logout, f.RequestLogout);
            On(r.LogoutConfirm.Yes, f.ConfirmLogout);
            On(r.LogoutConfirm.No, f.CancelLogout);
            On(r.ServerSettings.Save, SaveEndpoint);
            On(r.ServerSettings.VpnConnect, ConnectVpn);
            On(r.ServerSettings.VpnClientSetup, ClientSetupOrPaste);
            On(r.ServerSettings.TrustConfirm, () => ConnectLan(true));
            On(r.ServerSettings.TrustCancel, () => { _pendingLan = null; _vpnRequest++; _vpnMessage = "Chưa xác nhận máy chủ. Bạn có thể dán mã khác."; });
            On(r.ServerSettings.VpnBrowse, BrowseVpnBackend);
            On(r.ServerSettings.VpnSetup, () => RunVpn("Setup"));
            On(r.ServerSettings.VpnStart, () => RunVpn("Start"));
            On(r.ServerSettings.VpnStop, () => RunVpn("Stop"));
            On(r.ServerSettings.VpnRefresh, () => RunVpn("Status"));
            On(r.ServerSettings.VpnCopy, () => { if (!string.IsNullOrEmpty(_vpnHostCode)) { _copy(_vpnHostCode); _vpnMessage = _serverMode == 1 ? "Đã sao chép mã. Gửi cho bạn cùng mạng; mã phòng gửi riêng." : "Đã sao chép mã. Gửi lời mời Tailscale và mã phòng riêng."; } });
            On(r.ServerSettings.Audio, f.OpenSettings);
            On(r.ServerSettings.Close, () => { _vpnRequest++; f.CloseServerSettings(); });
            On(r.JoinForm.Join, () => f.SubmitJoin());
            On(r.JoinForm.Retry, () => f.SubmitJoin());
            On(r.JoinForm.Back, f.Back);
            On(r.Connecting.Cancel, f.CancelJoin);
            On(r.Login.LoginTab, () => { if (f.Account != null) f.Account.RegisterMode = false; _drawnKey = int.MinValue; });
            On(r.Login.RegisterTab, () => { if (f.Account != null) f.Account.RegisterMode = true; _drawnKey = int.MinValue; });
            On(r.Login.Submit, SubmitLogin);
            On(r.Login.Advanced, f.OpenServerSettings);
            On(r.Login.Retry, () => f.Api?.Check());
            On(r.Login.OfflinePractice, f.OpenOffline);
            On(r.RoomManager.Create, CreateRoom);
            On(r.RoomManager.Retry, () => f.Rooms?.Refresh());
            On(r.RoomManager.Back, f.Back);
            On(r.RoomManager.ConfirmYes, () => f.Rooms?.ConfirmDelete());
            On(r.RoomManager.ConfirmNo, () => f.Rooms?.CancelDelete());
            for (var i = 0; i < r.RoomManager.Rows.Length; i++)
            {
                var index = i;
                var row = r.RoomManager.Rows[i];
                On(row.Copy, () => { if (TryRoom(index, out var room) && room.RoomCode != null) Copy(room.RoomCode); });
                On(row.Join, () => { if (TryRoom(index, out var room)) f.JoinRoom(room); });
                On(row.Delete, () => { if (TryRoom(index, out var room)) f.Rooms.RequestDelete(room.Id); });
            }
            On(r.Lobby.Copy, () => Copy(f.Lobby.RoomCode));
            On(r.Lobby.Ready, () => f.Ready());
            On(r.Lobby.Start, () => _maps?.Start());
            On(r.Lobby.Leave, f.GoHome);
            On(r.Loading.Home, f.GoHome);
            On(r.Results.Replay, () => f.Replay());
            On(r.Results.Home, f.GoHome);
            On(r.Results.Online, () => f.OpenLeaderboard(f.Result.MatchId));
            On(r.History.Retry, () => { _ = f.History?.Refresh(); });
            On(r.History.Back, f.Back);
            for (var i = 0; i < r.History.Rows.Length; i++)
            {
                var index = i;
                On(r.History.Rows[i].Open, () =>
                {
                    if (f.History != null && index < f.History.Rows.Count) f.OpenLeaderboard(f.History.Rows[index].MatchId);
                });
            }
            On(r.Leaderboard.Retry, () => f.Leaderboard?.Load(f.Leaderboard.MatchId));
            On(r.Leaderboard.Back, f.Back);
            On(r.Reconnect.Home, f.GoHome);
            On(r.Notice.Home, f.GoHome);
            return list.ToArray();
        }

        private void BindInputs()
        {
            var j = _root.JoinForm;
            j.DisplayName.onValueChanged.AddListener(_flow.Join.SetName);
            j.RoomCode.onValueChanged.AddListener(raw =>
            {
                _flow.Join.SetCode(raw);
                if (raw != _flow.Join.RoomCode) j.RoomCode.SetTextWithoutNotify(_flow.Join.RoomCode); // AC5: upper-case
            });
            var l = _root.Login;
            if (_flow.Account != null)
            {
                l.Username.onValueChanged.AddListener(t => _flow.Account.Username = t);
                l.Password.onValueChanged.AddListener(t => _flow.Account.Password = t);
            }
            _root.ServerSettings.Profile.onValueChanged.AddListener(SelectProfile);
            if (_root.ServerSettings.DevProfile != null) _root.ServerSettings.DevProfile.onValueChanged.AddListener(SelectDevProfile);
            if (_root.ServerSettings.VpnCode != null) _root.ServerSettings.VpnCode.onValueChanged.AddListener(_ => { _pendingLan=null; _vpnRequest++; });
        }

        private void SubmitLogin()
        {
            _flow.Account?.Submit();
            _root.Login.Password.SetTextWithoutNotify(""); // AC13: cleared even when the call fails
            _drawnKey = int.MinValue;
        }

        private void CreateRoom()
        {
            var rooms = _flow.Rooms;
            if (rooms == null) return;
            rooms.NewName = _root.RoomManager.NewName.text;
            rooms.NewCapacity = int.TryParse(_root.RoomManager.NewCapacity.text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n)
                ? n : LobbyLimits.PlayersPerRoom;
            rooms.Create();
        }

        private bool TryRoom(int index, out RoomInfo room)
        {
            var rooms = _flow.Rooms;
            room = default;
            if (rooms == null || index >= rooms.Rooms.Count) return false;
            room = rooms.Rooms[index];
            return true;
        }

        private void Copy(string code)
        {
            if (string.IsNullOrEmpty(code)) return;
            CopyCount++;
            _copy(code); // Preserve the configured default room code too.
        }

        private static void SetOptional(TMP_Text text, string value)
        {
            text.gameObject.SetActive(value != null);
            if (value != null) text.text = value;
        }

        private static void SetLabel(Button button, string text)
        {
            var label = button.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = text;
        }

        private static int Hash(params object[] parts)
        {
            unchecked
            {
                var h = 17;
                foreach (var p in parts) h = h * 31 + (p?.GetHashCode() ?? 0);
                return h;
            }
        }

        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
