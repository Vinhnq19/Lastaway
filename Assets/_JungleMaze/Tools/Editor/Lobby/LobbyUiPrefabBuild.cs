using System.IO;
using JungleMaze.Presentation.UI.Lobby;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Tier = JungleMaze.EditorTools.UiKit.Tier;
using Kind = JungleMaze.EditorTools.UiKit.ButtonKind;
using Size = JungleMaze.EditorTools.UiKit.ButtonSize;
using C = JungleMaze.EditorTools.UiKit.Colors;

namespace JungleMaze.EditorTools
{
    /// <summary>
    /// Repeatable assembly of LobbyUi.prefab and LobbyUiSet.asset (UI-02), skin A (T-024, T-025). Every rect is the
    /// 1080p px of the T-021 mocks (screens_a.py / screens_b.py, T-028 tables): screen-level parts are absolute mock
    /// rects centred on the canvas (<see cref="UiKit.PlaceCentred"/>), parts inside a panel are px from the panel's top
    /// left (<see cref="UiKit.Place"/>). Sprites, buttons and the 5-step text scale come from <see cref="UiKit"/>;
    /// art is only ever assigned here (AC50). Rows are built inside the prefab (10 lobby slots, 5 rooms, 10
    /// result/leaderboard rows, 20 history lines). Field names of every view are unchanged.
    /// </summary>
    public static class LobbyUiPrefabBuild
    {
        public const string PrefabFolder = "Assets/_JungleMaze/Prefabs/UI/Lobby/";
        public const string PrefabPath = PrefabFolder + "LobbyUi.prefab";
        public const string SetFolder = "Assets/_JungleMaze/Art/UI/Lobby/Resources/";
        public const string SetPath = SetFolder + "LobbyUiSet.asset";
        private const string MenuArt = "Assets/_JungleMaze/Art/UI/Menu/";
        private const string StatusIcons = "Assets/_JungleMaze/Art/Hud/StatusIcons/";
        private const float W = 1920f, H = 1080f, TitleBar = 62f, InputHeight = 56f;
        private const TextAlignmentOptions Left = TextAlignmentOptions.MidlineLeft, Centre = TextAlignmentOptions.Midline,
            Right = TextAlignmentOptions.MidlineRight;

        private static Sprite Menu(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(MenuArt + name + ".png");

        [MenuItem("Jungle Maze/UI-02/Build Lobby UI")]
        [MenuItem("Jungle Maze/UI-02/Bind Offline Practice Buttons")]
        public static void Build()
        {
            Directory.CreateDirectory(PrefabFolder);
            Directory.CreateDirectory(SetFolder);
            var go = new GameObject("LobbyUi", typeof(RectTransform));
            try
            {
                var canvas = go.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 15; // above the HUD (10), below the UI-03 overlay (20) so its settings panel sits on top
                var scaler = go.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1280, 720);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
                go.AddComponent<GraphicRaycaster>();
                var root = go.AddComponent<LobbyUiRoot>();
                var rt = (RectTransform)go.transform;

                var backdrop = UiKit.Floor(rt, 0.66f); // tiled stone floor + wash (T-028 bg), shown on menu, lobby, loading, results
                backdrop.name = "Backdrop";
                Set(root, "canvas", canvas);
                Set(root, "backdrop", backdrop.gameObject);
                BuildSplash(rt, root);
                Set(root, "mainMenu", BuildMainMenu(rt));
                Set(root, "joinForm", BuildJoin(rt));
                Set(root, "connecting", BuildConnecting(rt));
                Set(root, "login", BuildLogin(rt));
                Set(root, "roomManager", BuildRooms(rt));
                Set(root, "history", BuildHistory(rt));
                Set(root, "leaderboard", BuildLeaderboard(rt));
                Set(root, "lobby", BuildLobby(rt));
                Set(root, "loading", BuildLoading(rt));
                Set(root, "results", BuildResults(rt));
                Set(root, "reconnect", BuildReconnect(rt));
                Set(root, "notice", BuildNotice(rt));
                Set(root, "serverSettings", BuildServerSettings(rt)); // modals last: drawn over Home and Login
                Set(root, "logoutConfirm", BuildLogoutConfirm(rt));

                UiKit.FaceAll(go); // pressed K2, close and icon buttons sink their content with the face (Integrator, KitButtonFace)
                var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
                var set = AssetDatabase.LoadAssetAtPath<LobbyUiSet>(SetPath);
                if (set == null)
                {
                    set = ScriptableObject.CreateInstance<LobbyUiSet>();
                    AssetDatabase.CreateAsset(set, SetPath);
                }
                Set(set, "uiPrefab", prefab.GetComponent<LobbyUiRoot>());
                EditorUtility.SetDirty(set);
                AssetDatabase.SaveAssets();
            }
            finally { Object.DestroyImmediate(go); }
        }

        // ---------- screens ----------

        /// <summary>A2 Sảnh chính: logo, six L buttons (420×80, step 100), API line with check, version, account chip (kit_toast, 96 tall).</summary>
        public static void BuildSplash(RectTransform rt, LobbyUiRoot root)
        {
                var splash = new GameObject("Splash", typeof(RectTransform), typeof(Image), typeof(AspectRatioFitter));
                splash.transform.SetParent(rt, false);
                var sr = (RectTransform)splash.transform;
                sr.anchorMin = sr.anchorMax = sr.pivot = new Vector2(.5f, .5f);
                var splashImage = splash.GetComponent<Image>();
                splashImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_JungleMaze/Art/UI/Splash/lastaway_splash.png");
                splashImage.raycastTarget = false;
                var fitter = splash.GetComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fitter.aspectRatio = splashImage.sprite != null ? splashImage.sprite.rect.width / splashImage.sprite.rect.height : 16f / 9f;
                var shade = new GameObject("Shade", typeof(RectTransform), typeof(Image));
                shade.transform.SetParent(sr, false);
                var shadeRect = (RectTransform)shade.transform;
                shadeRect.anchorMin = Vector2.zero; shadeRect.anchorMax = Vector2.one;
                shadeRect.offsetMin = shadeRect.offsetMax = Vector2.zero;
                shade.GetComponent<Image>().color = new Color(0f, 0f, 0f, .18f);
                shade.GetComponent<Image>().raycastTarget = false;
                Set(root, "splash", splash);
                var menu = Object.Instantiate(splash, rt);
                menu.name = "MainMenuSplash";
                menu.GetComponent<Image>().sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_JungleMaze/Art/UI/Splash/lastaway_menu.png");
                var menuSprite = menu.GetComponent<Image>().sprite;
                if (menuSprite != null) menu.GetComponent<AspectRatioFitter>().aspectRatio = menuSprite.rect.width / menuSprite.rect.height;
                menu.SetActive(false);
                Set(root, "mainMenuSplash", menu);
        }

        private static MainMenuView BuildMainMenu(RectTransform rt)
        {
            var v = Screen<MainMenuView>(rt, "MainMenu");
            var r = Rt(v);
            Logo(r, 960, 60, 760);
            Set(v, "join", Btn(r, "Join", Kind.Primary, Size.L, 750, 340, 420, "kit_ico_play", "Vào phòng", true));
            Set(v, "rooms", Btn(r, "Rooms", Kind.Secondary, Size.L, 750, 440, 420, "kit_ico_plus", "Tạo phòng", true));
            Set(v, "history", Btn(r, "History", Kind.Secondary, Size.L, 750, 540, 420, "kit_ico_clock", "Lịch sử", true));
            Set(v, "leaderboard", Btn(r, "Leaderboard", Kind.Secondary, Size.L, 750, 640, 420, "kit_ico_trophy", "Bảng xếp hạng", true));
            Set(v, "settings", Btn(r, "Settings", Kind.Secondary, Size.L, 750, 740, 420, "kit_ico_gear", "Cài đặt", true));
            Set(v, "quit", Btn(r, "Quit", Kind.Secondary, Size.L, 750, 840, 420, "kit_ico_door", "Thoát", true));
            // API line: check + text centred as one group (A2); the check shows only while the API is ready.
            var api = Abs(r, "ApiRow", 560, 944, 800, 28);
            var apiRow = api.gameObject.AddComponent<HorizontalLayoutGroup>();
            apiRow.spacing = UiKit.Px(10);
            apiRow.childAlignment = TextAnchor.MiddleCenter;
            apiRow.childControlWidth = true;
            apiRow.childControlHeight = false;
            apiRow.childForceExpandWidth = apiRow.childForceExpandHeight = false;
            var check = UiKit.Image(Rel(api, "ApiReady", 0, 3, 28, 22), "kit_icon_check");
            check.gameObject.AddComponent<LayoutElement>().preferredWidth = UiKit.Px(28);
            Set(v, "apiReadyIcon", Hidden(check).gameObject);
            var apiText = Txt(api, "ApiStatus", Tier.C, "", C.Text, 0, 14, 800);
            apiText.overflowMode = TextOverflowModes.Overflow;
            Set(v, "apiStatus", apiText);
            Set(v, "version", Txt(r, "Version", Tier.C, "v", C.Muted, 1560, 1030, 300, Right, true)); // MainMenuView fills Application.version
            // Not on the mock: offline practice stays reachable from Home, as a small button bottom left.
            Set(v, "offlinePractice", Btn(r, "OfflinePractice", Kind.Secondary, Size.S, 60, 1002, 340, "kit_ico_practice", "Luyện tập offline", true));
            // Account chip: kept on the mock's right edge (1884) and grown left by its own layout, so "Đã đăng nhập" and
            // the account name are never cut (QA T24-1).
            var card = Box(r, "AccountCard", "kit_toast", 1480, 36, 404, 96, true);
            card.pivot = new Vector2(1f, 0.5f);
            card.anchoredPosition = new Vector2(UiKit.Px(1884 - 960), -UiKit.Px(84 - 540));
            var chip = card.gameObject.AddComponent<HorizontalLayoutGroup>();
            chip.padding = new RectOffset(Mathf.RoundToInt(UiKit.Px(12)), Mathf.RoundToInt(UiKit.Px(16)), 0, 0);
            chip.spacing = UiKit.Px(10);
            chip.childAlignment = TextAnchor.MiddleLeft;
            chip.childControlWidth = true;
            chip.childControlHeight = false;
            chip.childForceExpandWidth = chip.childForceExpandHeight = false;
            card.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            PlateIcon(card, "Avatar", "kit_ico_person", 36, 48, 28).gameObject.AddComponent<LayoutElement>().preferredWidth = UiKit.Px(48);
            var who = Rel(card, "Who", 0, 13, 160, 70);
            var lines = who.gameObject.AddComponent<VerticalLayoutGroup>();
            lines.childAlignment = TextAnchor.MiddleLeft;
            lines.childControlWidth = true;
            lines.childControlHeight = false;
            lines.childForceExpandWidth = lines.childForceExpandHeight = false;
            var status = Txt(who, "Status", Tier.C, "Đã đăng nhập", C.Muted, 0, 14, 160);
            var name = Txt(who, "Username", Tier.B1, "", C.Gold, 0, 49, 160);
            status.overflowMode = name.overflowMode = TextOverflowModes.Overflow;
            Set(v, "accountStatus", status);
            Set(v, "accountName", name);
            var logout = Btn(card, "Logout", Kind.Secondary, Size.S, 0, 24, 180, "kit_ico_logout", "Đăng xuất");
            logout.gameObject.AddComponent<LayoutElement>().preferredWidth = UiKit.Px(180);
            Set(v, "logout", logout);
            return v;
        }

        /// <summary>Logout confirm, same box as A4 Xóa phòng (popup 720×400) but no red: logout is not destructive.</summary>
        private static LogoutConfirmView BuildLogoutConfirm(RectTransform rt)
        {
            var v = Screen<LogoutConfirmView>(rt, "LogoutConfirm");
            var r = Rt(v);
            UiKit.Dim(r);
            var p = Popup(r, 720, 400, "Đăng xuất");
            Set(v, "text", Txt(p, "Text", Tier.B2, "Đăng xuất?", C.Text, 40, 142, 640, Centre));
            Txt(p, "Body", Tier.C, "Bạn sẽ quay về màn đăng nhập.", C.Muted, 40, 180, 640, Centre);
            var by = Footer(p, 720, 400);
            var no = Btn(p, "No", Kind.Secondary, Size.M, 70, by, 240, "kit_icon_cross_ink", "Hủy");
            Set(v, "no", no);
            Set(v, "yes", Btn(p, "Yes", Kind.Primary, Size.M, 410, by, 240, "kit_ico_logout", "Đăng xuất"));
            Close(p, 720, no);
            return v;
        }

        /// <summary>
        /// Server settings modal (no mock; kit rules, ui-kit.md mục 2, 4): popup 820×720 sized to its content, inner margin
        /// 60 (rim 24 + 36), form rows of label 20 + field 56 on a 100 px pitch, host and port side by side, Lưu / Âm thanh
        /// (M) in one row 24 apart, then the footer with Đóng.
        /// </summary>
        private static ServerSettingsView BuildServerSettings(RectTransform rt)
        {
            const float w = 820, h = 720;
            var v = Screen<ServerSettingsView>(rt, "ServerSettings");
            var r = Rt(v);
            UiKit.Dim(r);
            var p = Popup(r, w, h, "Cài đặt máy chủ");
            Txt(p, "Advanced", Tier.C, "Nâng cao: chỉ đổi khi chơi LAN/VPN hoặc được hướng dẫn.", C.Muted, 60, 124, 700);
            var profile = Drop(p, "Profile", 60, 182, 700);
            FieldLabel((RectTransform)profile.transform, "Kết nối");
            Set(v, "profile", profile);
            Set(v, "popup", p);
            var normal = Rel(p, "NormalFields", 0, 0, w, h);
            Set(v, "normalFields", normal.gameObject);
            var host = Input(normal, "Host", "Host", 60, 282, 456);
            FieldLabel((RectTransform)host.transform, "Máy chủ");
            Set(v, "host", host);
            var port = Input(normal, "Port", "Port", 540, 282, 220);
            FieldLabel((RectTransform)port.transform, "Cổng");
            Set(v, "port", port);
            var api = Input(normal, "Api", "https://…", 60, 382, 700);
            FieldLabel((RectTransform)api.transform, "URL API");
            Set(v, "api", api);
            Set(v, "status", Txt(normal, "Status", Tier.C, "", C.Muted, 60, 466, 700));
            Set(v, "save", Btn(normal, "Save", Kind.Secondary, Size.M, 60, 496, 260, "kit_ico_save", "Lưu endpoint"));
            Set(v, "audioSettings", Btn(normal, "Audio", Kind.Secondary, Size.M, 344, 496, 416, "kit_ico_sound", "Âm thanh & điều khiển"));
            var close = Btn(p, "Close", Kind.Secondary, Size.M, 260, Footer(p, w, h), 300, "kit_icon_cross_ink", "Đóng");
            Set(v, "close", close);
            Close(p, w, close);
            Set(v, "popupFooter", (RectTransform)p.Find("Footer"));
            Set(v, "popupTitle", (RectTransform)p.Find("Title"));
            Set(v, "popupClose", (RectTransform)p.GetComponentInChildren<ForwardClick>().transform);
            var vpn = Rel(p, "VpnPanel", 0, 0, 1220, 1040);
            Set(v, "vpnPanel", vpn.gameObject);
            Para(vpn, "ClientTitle", Tier.B2, "Chơi cùng bạn", C.Muted, 60, 245, 520, 42);
            Set(v, "vpnCode", Input(vpn, "VpnCode", "VD: may-host.tail123", 60, 316, 520));
            Set(v, "vpnConnect", Btn(vpn, "VpnConnect", Kind.Primary, Size.M, 60, 400, 520, "kit_ico_save", "Kiểm tra / Kết nối"));
            Set(v, "vpnClientSetup", Btn(vpn, "VpnClientSetup", Kind.Secondary, Size.M, 60, 480, 520, "kit_ico_gear", "Cài / đăng nhập VPN"));
            Para(vpn, "ClientHelp", Tier.C, "1. Nhận lời mời Tailscale từ host.\n2. Đăng nhập Tailscale của bạn.\n3. Dán mã kết nối rồi đăng nhập game.\nMã này không phải mã phòng.", C.Muted, 60, 575, 520, 165);
            Para(vpn, "HostTitle", Tier.B2, "Tự làm host · máy dev", C.Muted, 640, 245, 520, 42);
            Set(v, "vpnBackend", Para(vpn, "VpnBackend", Tier.C, "Chọn thư mục Backend của source tin cậy", C.Muted, 640, 305, 520, 65));
            Set(v, "vpnBrowse", Btn(vpn, "VpnBrowse", Kind.Secondary, Size.M, 640, 390, 520, "kit_ico_gear", "Chọn thư mục Backend"));
            Set(v, "vpnSetup", Btn(vpn, "VpnSetup", Kind.Secondary, Size.M, 640, 470, 248, "kit_ico_gear", "Thiết lập"));
            Set(v, "vpnStart", Btn(vpn, "VpnStart", Kind.Primary, Size.M, 912, 470, 248, "kit_ico_play", "Bật server"));
            Set(v, "vpnStop", Btn(vpn, "VpnStop", Kind.Secondary, Size.M, 640, 550, 248, "kit_icon_cross_ink", "Dừng server"));
            Set(v, "vpnRefresh", Btn(vpn, "VpnRefresh", Kind.Secondary, Size.M, 912, 550, 248, "kit_ico_refresh", "Trạng thái"));
            Set(v, "vpnCopy", Btn(vpn, "VpnCopy", Kind.Secondary, Size.M, 640, 630, 520, "kit_ico_copy", "Sao chép mã kết nối"));
            Para(vpn, "HostHelp", Tier.C, "Cần Docker + source. Đóng game không tắt server.\nLời mời Tailscale gửi riêng, không nằm trong mã.", C.Muted, 640, 715, 520, 65);
            Set(v, "vpnStatus", Para(vpn, "VpnStatus", Tier.C, "Dán mã hoặc thiết lập máy host để bắt đầu.", C.Muted, 60, 800, 1100, 88));
            v.VpnCode.characterLimit = 1024;
            v.VpnCode.textComponent.richText = false;
            vpn.gameObject.SetActive(false);
            return v;
        }

        /// <summary>Caption 20 above a form field, as a child so it hides with the field (the presenter hides Profile without a menu).</summary>
        private static void FieldLabel(RectTransform field, string label) =>
            Txt(field, "FieldLabel", Tier.C, label, C.Muted, 0, -16, field.sizeDelta.x * UiKit.Scale);

        /// <summary>A3 Vào phòng: popup 800×620 over Home behind the dim (the account chip stays under it).</summary>
        private static JoinFormView BuildJoin(RectTransform rt)
        {
            var v = Screen<JoinFormView>(rt, "JoinForm");
            var r = Rt(v);
            UiKit.Dim(r);
            var p = Popup(r, 800, 620, "Vào phòng");
            Txt(p, "NameLabel", Tier.C, "Tên hiển thị", C.Muted, 60, 150, 680);
            Set(v, "displayName", Input(p, "DisplayName", "Tên của bạn (1 đến 32 ký tự)", 60, 170, 680));
            Set(v, "nameError", Txt(p, "NameError", Tier.C, "", C.DangerText, 60, 244, 680));
            Txt(p, "CodeLabel", Tier.C, "Mã phòng (phòng mới: 6 ký tự)", C.Muted, 60, 276, 680);
            // One kit input, not six code boxes: configured default codes are longer than 6 (JungleDev123).
            var code = Input(p, "RoomCode", "VD: K7M2Q9", 60, 296, 680, Tier.B1);
            code.textComponent.characterSpacing = 18;
            Set(v, "roomCode", code);
            Set(v, "codeError", Txt(p, "CodeError", Tier.C, "", C.DangerText, 60, 370, 680));
            Set(v, "message", Txt(p, "Message", Tier.C, "", C.Gold, 60, 400, 680));
            Set(v, "hint", Txt(p, "Hint", Tier.C, "", C.Muted, 60, 436, 680));
            var by = Footer(p, 800, 620);
            var back = Btn(p, "Back", Kind.Secondary, Size.M, 60, by, 220, "kit_icon_cross_ink", "Hủy");
            Set(v, "back", back);
            Set(v, "retry", Btn(p, "Retry", Kind.Secondary, Size.M, 290, by, 220, "kit_ico_refresh", "Thử lại"));
            Set(v, "join", Btn(p, "Join", Kind.Primary, Size.M, 520, by, 220, "kit_ico_play", "Vào phòng"));
            Close(p, 800, back);
            return v;
        }

        /// <summary>A3 đang kết nối: its own screen in the flow, drawn as the join popup with a bar and Hủy.</summary>
        private static ConnectingView BuildConnecting(RectTransform rt)
        {
            var v = Screen<ConnectingView>(rt, "Connecting");
            var r = Rt(v);
            UiKit.Dim(r);
            var p = Popup(r, 800, 420, "Đang kết nối");
            Set(v, "step", Txt(p, "Step", Tier.B2, "Đang kết nối", C.Text, 60, 150, 680));
            Progress(p, 60, 190, 680, 0.44f); // static as on the mock: the flow has no connect progress
            Set(v, "rtt", Txt(p, "Rtt", Tier.C, "", C.Muted, 60, 250, 680));
            var cancel = Btn(p, "Cancel", Kind.Secondary, Size.M, 275, Footer(p, 800, 420), 250, "kit_icon_cross_ink", "Hủy");
            Set(v, "cancel", cancel);
            Close(p, 800, cancel);
            return v;
        }

        /// <summary>A3b Đăng nhập: kit_panel_m 700×650, tabs, two inputs, Đăng nhập (M) and Máy chủ (S).</summary>
        private static LoginView BuildLogin(RectTransform rt)
        {
            var v = Screen<LoginView>(rt, "Login");
            var r = Rt(v);
            Logo(r, 960, 55, 700);
            var p = Box(r, "Panel", "kit_panel_m", 610, 220, 700, 650, true);
            Set(v, "title", Txt(p, "Title", Tier.H2, "Đăng nhập", C.Gold, 60, 74, 580));
            Set(v, "loginTab", Tab(p, "LoginTab", "Đăng nhập", 60, 100, out var loginOn));
            Set(v, "registerTab", Tab(p, "RegisterTab", "Đăng ký", 360, 100, out var registerOn));
            registerOn.SetActive(false);
            Set(v, "loginTabOn", loginOn);
            Set(v, "registerTabOn", registerOn);
            SetFloat(v, "idleTabAlpha", 1f); // the tabs differ by sprite height now, not by alpha
            SetColor(v, "idleTabText", C.Text);
            SetColor(v, "activeTabText", C.Ink);
            Txt(p, "UserLabel", Tier.C, "Tên đăng nhập", C.Muted, 60, 215, 580);
            Set(v, "username", Input(p, "Username", "Tên đăng nhập (3 đến 32 ký tự)", 60, 237, 580));
            Txt(p, "PassLabel", Tier.C, "Mật khẩu", C.Muted, 60, 321, 580);
            var pass = Input(p, "Password", "Mật khẩu", 60, 343, 580);
            pass.contentType = TMP_InputField.ContentType.Password;
            Set(v, "password", pass);
            Set(v, "message", Txt(p, "Message", Tier.C, "", C.Gold, 60, 429, 580));
            Set(v, "submit", Btn(p, "Submit", Kind.Primary, Size.M, 60, 470, 580, "kit_ico_key", "Đăng nhập"));
            Set(v, "advanced", Btn(p, "Advanced", Kind.Secondary, Size.S, 60, 550, 580, "kit_ico_server", "Máy chủ (nâng cao)"));
            // Under the panel (not on the mock): account-server status with Thử lại (review M1), offline practice.
            Set(v, "apiStatus", Txt(r, "ApiStatus", Tier.C, "", C.Gold, 610, 892, 700, Centre, true));
            Set(v, "retry", Btn(r, "Retry", Kind.Secondary, Size.S, 610, 920, 340, "kit_ico_refresh", "Thử lại", true));
            var offline = Btn(r, "OfflinePractice", Kind.Secondary, Size.S, 970, 920, 340, "kit_ico_practice", "Luyện tập offline", true);
            Set(v, "offlinePractice", offline);
            Set(v, "devSkip", offline);
            return v;
        }

        /// <summary>A4 Phòng của tôi: header, five room cards 1680×120 (step 140), create row at the bottom, A4 xóa phòng popup.</summary>
        private static RoomManagerView BuildRooms(RectTransform rt)
        {
            var v = Screen<RoomManagerView>(rt, "RoomManager");
            var r = Rt(v);
            var h = Header(r, "Phòng của tôi");
            Txt(h, "Right", Tier.B2, "2 phòng bổ sung tối đa", C.Muted, 1260, 54, 500, Right);
            Set(v, "back", BackButton(h));
            Set(v, "state", Txt(r, "State", Tier.B2, "", C.Muted, 120, 162, 1300, Left, true));
            Set(v, "retry", Btn(r, "Retry", Kind.Secondary, Size.S, 1560, 138, 240, "kit_ico_refresh", "Thử lại", true));
            var rows = new RoomRowView[5];
            for (var i = 0; i < rows.Length; i++) rows[i] = RoomRow(r, i, 190 + i * 140);
            SetArray(v, "rows", rows);
            // A5 is a popup over Home in the mock; the flow creates rooms from this screen, so the form is a bottom row.
            Txt(r, "NameLabel", Tier.C, "Tên phòng (1 đến 48 ký tự)", C.Muted, 120, 906, 760, Left, true);
            Set(v, "newName", Input(r, "NewName", "Tên phòng", 120, 928, 760, Tier.B2, true));
            Txt(r, "CapacityLabel", Tier.C, "Sức chứa", C.Muted, 900, 906, 200, Left, true);
            var cap = Input(r, "NewCapacity", "10", 900, 928, 160, Tier.B2, true);
            cap.contentType = TMP_InputField.ContentType.IntegerNumber;
            cap.text = "10";
            Set(v, "newCapacity", cap);
            Set(v, "create", Btn(r, "Create", Kind.Primary, Size.M, 1520, 924, 280, "kit_ico_plus", "Tạo phòng", true));

            var overlay = UiKit.Stretch(UiKit.Node(r, "Confirm")); // AES-01: centred modal over a dim, never over a row
            var group = overlay.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = group.blocksRaycasts = false;
            UiKit.Dim(overlay);
            var p = Popup(overlay, 720, 400, "Xóa phòng?");
            Set(v, "confirmGroup", group);
            Set(v, "confirmText", Txt(p, "Text", Tier.B2, "Xóa phòng?", C.Text, 40, 142, 640, Centre));
            Txt(p, "Body", Tier.C, "Không hoàn tác. Chỉ xóa được phòng đang trống.", C.Muted, 40, 180, 640, Centre);
            var by = Footer(p, 720, 400);
            var no = Btn(p, "No", Kind.Secondary, Size.M, 70, by, 240, "kit_icon_cross_ink", "Hủy");
            Set(v, "confirmNo", no);
            Set(v, "confirmYes", Btn(p, "Yes", Kind.Danger, Size.M, 410, by, 240, "kit_ico_trash", "Xóa phòng"));
            Close(p, 720, no);
            return v;
        }

        /// <summary>A4 room card (kit_card_room_normal): name, n/max, code or "Mặc định", Sao chép / Vào / Xóa (S).</summary>
        private static RoomRowView RoomRow(RectTransform parent, int index, float y)
        {
            var card = Box(parent, "Room" + index, "kit_card_room_normal", 120, y, 1680, 120, true);
            var row = card.gameObject.AddComponent<RoomRowView>();
            Set(row, "roomName", Txt(card, "Name", Tier.B1, "", C.Text, 50, 60, 560));
            Set(row, "occupancy", Txt(card, "Occupancy", Tier.B2, "", C.Text, 640, 60, 180));
            Set(row, "code", Txt(card, "Code", Tier.B2, "", C.Gold, 840, 60, 160));
            Set(row, "copy", Btn(card, "Copy", Kind.Secondary, Size.S, 1010, 36, 190, "kit_ico_copy", "Sao chép"));
            Set(row, "join", Btn(card, "Join", Kind.Primary, Size.S, 1220, 36, 110, "kit_ico_play", "Vào"));
            Set(row, "delete", Btn(card, "Delete", Kind.Secondary, Size.S, 1350, 36, 110, "kit_ico_trash", "Xóa"));
            return row;
        }

        /// <summary>History (no mock): A4 header and back, 20 kit rows of 48 px scrolling in a 720 px list.</summary>
        private static HistoryView BuildHistory(RectTransform rt)
        {
            var v = Screen<HistoryView>(rt, "History");
            var r = Rt(v);
            var h = Header(r, "Lịch sử trận");
            Set(v, "back", BackButton(h));
            Set(v, "identity", Txt(h, "Identity", Tier.B2, "", C.Muted, 1260, 54, 500, Right));
            Set(v, "state", Txt(r, "State", Tier.B2, "", C.Muted, 120, 162, 1300, Left, true));
            Set(v, "retry", Btn(r, "Retry", Kind.Secondary, Size.S, 1560, 138, 240, "kit_ico_refresh", "Thử lại", true));
            const float line = 48f;
            var view = Abs(r, "List", 120, 200, 1680, 720);
            view.gameObject.AddComponent<RectMask2D>();
            var content = Rel(view, "Content", 0, 0, 1680, 20 * line);
            var scroll = view.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = view;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = UiKit.Px(line);
            view.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0); // catches the wheel between lines
            var rows = new HistoryRowView[20];
            for (var i = 0; i < rows.Length; i++)
            {
                var lineRect = Box(content, "Line" + i, i % 2 == 0 ? "kit_row_normal" : "kit_row_alt", 0, i * line, 1680, line);
                var image = lineRect.GetComponent<Image>();
                image.raycastTarget = true;
                var row = lineRect.gameObject.AddComponent<HistoryRowView>();
                var button = lineRect.gameObject.AddComponent<Button>();
                button.targetGraphic = image;
                Set(row, "open", button);
                Set(row, "line", Txt(lineRect, "Text", Tier.B2, "", C.Text, 20, line / 2f, 1640));
                rows[i] = row;
            }
            SetArray(v, "rows", rows);
            return v;
        }

        /// <summary>Leaderboard (no mock): header and back, ten rows as in the B6 table.</summary>
        private static LeaderboardView BuildLeaderboard(RectTransform rt)
        {
            var v = Screen<LeaderboardView>(rt, "Leaderboard");
            var r = Rt(v);
            var h = Header(r, "Bảng xếp hạng trận (online)");
            Set(v, "back", BackButton(h));
            Set(v, "state", Txt(r, "State", Tier.B2, "", C.Muted, 120, 162, 1300, Left, true));
            Set(v, "retry", Btn(r, "Retry", Kind.Secondary, Size.S, 1560, 138, 240, "kit_ico_refresh", "Thử lại", true));
            var rows = new ResultRowView[10];
            for (var i = 0; i < rows.Length; i++) rows[i] = ResultRow(r, i, 600, 200 + i * 68, 720, true);
            SetArray(v, "rows", rows);
            return v;
        }

        /// <summary>
        /// A6 Phòng chờ: header with code + Chép, ten slots in two columns (440×96, 470 × 112), info panel_m on the right
        /// (status, players, notice, rule / map line, map cards), bot buttons under the slots, Rời phòng / Sẵn sàng / Bắt đầu.
        /// </summary>
        private static LobbyView BuildLobby(RectTransform rt)
        {
            var v = Screen<LobbyView>(rt, "Lobby");
            var r = Rt(v);
            var h = Header(r, "Phòng chờ");
            Txt(h, "CodeLabel", Tier.B2, "Mã phòng", C.Text, 1290, 54, 140, Right);
            Set(v, "roomCode", Txt(h, "RoomCode", Tier.B2, "", C.Gold, 1440, 54, 200));
            Set(v, "copy", Btn(h, "Copy", Kind.Secondary, Size.S, 1655, 28, 130, "kit_ico_copy", "Chép"));

            var slots = new LobbySlotView[10];
            for (var i = 0; i < slots.Length; i++) slots[i] = Slot(r, i, 60 + i / 5 * 470, 170 + i % 5 * 112);
            SetArray(v, "slots", slots);

            var info = Box(r, "Info", "kit_panel_m", 1030, 170, 830, 540, true);
            Set(v, "status", Txt(info, "Status", Tier.H2, "", C.Gold, 60, 86, 710));
            PlateIcon(info, "PlayerIcon", "kit_ico_person", 76, 150, 26);
            Set(v, "count", Txt(info, "Count", Tier.B2, "", C.Text, 118, 150, 652));
            Set(v, "notice", Para(info, "Notice", Tier.B2, "", C.Gold, 60, 188, 710, 54));
            Set(v, "rule", Para(info, "Rule", Tier.B2, "", C.Muted, 60, 250, 710, 40));
            var cards = MapCards(info, 60, 300, 710, 180, out var template);
            Set(v, "mapCards", cards);
            Set(v, "mapCardTemplate", template);
            cards.gameObject.SetActive(false); // RoomMapPanel shows it when maps are in play

            Set(v, "addBot", Hidden(Btn(r, "AddBot", Kind.Secondary, Size.S, 60, 744, 240, "kit_ico_plus", "Thêm bot", true)));
            Set(v, "fillBots", Hidden(Btn(r, "FillBots", Kind.Secondary, Size.S, 320, 744, 240, "kit_ico_fill", "Điền đủ 6", true)));
            Set(v, "retryMap", Hidden(Btn(r, "RetryMap", Kind.Secondary, Size.S, 580, 744, 320, "kit_ico_refresh", "Thử tải map lại", true)));

            Set(v, "leave", Btn(r, "Leave", Kind.Exit, Size.M, 60, 900, 320, null, "Rời phòng", true));
            var ready = Btn(r, "Ready", Kind.Secondary, Size.M, 900, 900, 420, "kit_ico_check", "Sẵn sàng", true);
            Set(v, "ready", ready);
            Set(v, "readyLabel", ready.GetComponentInChildren<TMP_Text>());
            Set(v, "start", Btn(r, "Start", Kind.Primary, Size.M, 1340, 900, 520, "kit_ico_play", "Bắt đầu (máy chủ chưa hỗ trợ)", true));
            return v;
        }

        /// <summary>A6 slot: kit_card_room_full under kit_slot_player_notready; Tint fades the player face out on empty slots.</summary>
        private static LobbySlotView Slot(RectTransform parent, int index, float x, float y)
        {
            var s = Abs(parent, "Slot" + index, x, y, 440, 96);
            var slot = s.gameObject.AddComponent<LobbySlotView>();
            Box(s, "EmptyCard", "kit_card_room_full", 0, 0, 440, 96);
            Set(slot, "background", UiKit.Image(Rel(s, "Background", 0, 0, 440, 96), "kit_slot_player_notready"));
            SetColor(slot, "selfColor", new Color(1f, 0.9f, 0.62f));
            SetColor(slot, "filledColor", Color.white);
            SetColor(slot, "emptyColor", new Color(1f, 1f, 1f, 0f));
            Set(slot, "playerName", Txt(s, "Name", Tier.B2, "", C.Text, 112, 48, 170));
            Set(slot, "badge", Txt(s, "Badge", Tier.C, "", C.Gold, 286, 48, 142, Right));
            Set(slot, "removeBot", Hidden(IconButton(s, "RemoveBot", 380, 24, 48, "kit_iconbtn_square", "kit_ico_trash", 30)));
            return slot;
        }

        /// <summary>Horizontal map strip for RoomMapPanel: viewport, empty content, and an inactive 340×180 card template.</summary>
        private static ScrollRect MapCards(RectTransform parent, float x, float y, float w, float h, out Button template)
        {
            var view = Rel(parent, "MapCards", x, y, w, h);
            view.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0); // drag target between cards
            view.gameObject.AddComponent<RectMask2D>();
            var scroll = view.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = view;
            scroll.content = Rel(view, "Content", 0, 0, w, h);
            scroll.horizontal = true;
            scroll.vertical = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 28;
            var card = Box(view, "MapCardTemplate", "kit_card_room_normal", 0, 0, 340, h); // outside Content: Rebuild clears Content
            var image = card.GetComponent<Image>();
            image.raycastTarget = true;
            template = card.gameObject.AddComponent<Button>();
            template.targetGraphic = image;
            Box(card, "Selected", "kit_card_room_selected", 0, 0, 340, h).gameObject.SetActive(false);
            Rel(card, "Preview", 16, 12, 308, 84).gameObject.AddComponent<RawImage>().raycastTarget = false;
            Para(card, "Title", Tier.B2, "", C.Text, 16, 100, 308, 38);
            Para(card, "Description", Tier.C, "", C.Muted, 16, 138, 308, 36);
            card.gameObject.SetActive(false);
            return scroll;
        }

        /// <summary>A7 Đang tải: logo, bar (time used, no load % exists), steps, controls panel_s, tip toast.</summary>
        private static LoadingView BuildLoading(RectTransform rt)
        {
            var v = Screen<LoadingView>(rt, "Loading");
            var r = Rt(v);
            Logo(r, 960, 110, 440);
            Txt(r, "Title", Tier.B1, "Đang tải trận", C.Text, 560, 280, 800, Centre, true);
            Set(v, "progressFill", Progress(r, 680, 330, 560, 0f, true));
            Txt(r, "BarLabel", Tier.B2, "Đang tải bản đồ…", C.Muted, 680, 385, 320, Left, true);
            Set(v, "timeLeft", Txt(r, "TimeLeft", Tier.B2, "", C.Text, 1000, 385, 240, Right, true));
            var steps = new[] { "stepMap", "stepVersion", "stepPlayers" };
            var names = new[] { "StepMap", "StepVersion", "StepPlayers" };
            for (var i = 0; i < steps.Length; i++)
            {
                var y = 440 + i * 40;
                UiKit.Image(Abs(r, names[i] + "Dot", 689, y - 11, 22, 22), "kit_icon_dot");
                Set(v, steps[i], Txt(r, names[i], Tier.B2, "", C.Text, 730, y, 510, Left, true));
            }
            var controls = Box(r, "Controls", "kit_panel_s", 480, 640, 960, 200, true);
            Txt(controls, "ControlsTitle", Tier.C, "Điều khiển", C.Gold, 50, 56, 860);
            Set(v, "hint", Para(controls, "Hint", Tier.B2, "", C.Text, 50, 78, 860, 72));
            var tip = Box(r, "TipBox", "kit_toast", 460, 880, 1000, 64, true);
            Set(v, "tip", Txt(tip, "Tip", Tier.B2, "", C.Text, 30, 32, 940));
            Set(v, "home", Btn(r, "Home", Kind.Secondary, Size.S, 60, 1002, 320, "kit_ico_home", "Về trang chủ", true));
            return v;
        }

        /// <summary>
        /// B6 Thắng / B7 Thua: left column stacked by a vertical layout (emblem 230, title 96, rank, cause card, two stat
        /// cards) so hidden lines collapse; Về sảnh / Chơi lại (M); table in kit_panel_m 820×820 with ten 720×60 rows.
        /// </summary>
        private static ResultsView BuildResults(RectTransform rt)
        {
            var v = Screen<ResultsView>(rt, "Results");
            var r = Rt(v);
            var col = Abs(r, "Summary", 120, 135, 720, 680);
            var stack = col.gameObject.AddComponent<VerticalLayoutGroup>();
            stack.spacing = UiKit.Px(10);
            stack.childAlignment = TextAnchor.UpperCenter;
            stack.childControlWidth = stack.childControlHeight = false;
            stack.childForceExpandWidth = stack.childForceExpandHeight = false;

            var emblem = Rel(col, "Emblem", 245, 0, 230, 230);
            var win = UiKit.Image(UiKit.Stretch(UiKit.Node(emblem, "EmblemWin")), "fx_emblem_win");
            var lose = UiKit.Image(UiKit.Stretch(UiKit.Node(emblem, "EmblemLose")), "fx_emblem_lose");
            lose.gameObject.SetActive(false);
            Set(v, "winEmblem", win.gameObject);
            Set(v, "loseEmblem", lose.gameObject);
            var title = Txt(col, "Title", Tier.H1, "", C.Gold, 0, 55, 720, Centre);
            title.textWrappingMode = TextWrappingModes.Normal; // long headlines (tie, · CÓ BOT) wrap instead of cutting
            title.overflowMode = TextOverflowModes.Overflow;
            ((RectTransform)title.transform).sizeDelta = new Vector2(UiKit.Px(720), UiKit.Px(110));
            Set(v, "title", title);
            SetColor(v, "victoryTitle", C.Gold);
            SetColor(v, "otherTitle", C.Text);
            Set(v, "rank", Txt(col, "Rank", Tier.H2, "", C.Text, 0, 28, 720, Centre));

            var cause = Box(col, "CauseCard", "kit_card_cause", 0, 0, 720, 110);
            PlateIcon(cause, "Skull", "kit_ico_skull", 70, 55, 36);
            Set(v, "cause", Txt(cause, "Cause", Tier.B1, "", C.Text, 140, 55, 560));
            Set(v, "causeCard", cause.gameObject);

            var stats = Rel(col, "Stats", 0, 0, 780, 130); // wider than the column: two long cards (340 + 20 + ~370 px) still fit
            var row = stats.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = UiKit.Px(20);
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlWidth = true; // each card as wide as its text (Sống sót: 05:12), never cut; one card stays centred
            row.childControlHeight = false;
            row.childForceExpandWidth = row.childForceExpandHeight = false;
            Set(v, "statsRow", stats.gameObject);
            Set(v, "kills", StatCard(stats, "KillsCard", "icon_kill", out var killsCard));
            Set(v, "killsCard", killsCard);
            Set(v, "survival", StatCard(stats, "SurvivalCard", "icon_zone-timer", out var survivalCard));
            Set(v, "survivalCard", survivalCard);

            Set(v, "home", Btn(r, "Home", Kind.Secondary, Size.M, 120, 834, 340, "kit_ico_home", "Về sảnh", true));
            var replay = Btn(r, "Replay", Kind.Primary, Size.M, 480, 834, 340, "kit_ico_refresh", "Chơi lại", true);
            Set(v, "replay", replay);
            Set(v, "replayLabel", replay.GetComponentInChildren<TMP_Text>());
            Set(v, "online", Btn(r, "Online", Kind.Secondary, Size.S, 120, 920, 700, "kit_ico_globe", "Xem online", true));
            Set(v, "onlineLine", Txt(r, "OnlineLine", Tier.C, "", C.Muted, 120, 995, 700, Left, true));

            var table = Box(r, "Table", "kit_panel_m", 1000, 120, 820, 820, true);
            PlateIcon(table, "Trophy", "kit_ico_trophy", 75, 70, 36);
            Txt(table, "Header", Tier.B1, "Xếp hạng", C.Gold, 130, 70, 500);
            var rows = new ResultRowView[10];
            for (var i = 0; i < rows.Length; i++) rows[i] = ResultRow(table, i, 50, 110 + i * 68, 720);
            SetArray(v, "rows", rows);
            return v;
        }

        /// <summary>
        /// B6 stat card (kit_card_stat, at least 340×130): HUD status icon 56 + 12 + value text, grown by its own layout to
        /// the text's width ("Sống sót: 05:12" needs about 370 px), content centred.
        /// </summary>
        private static TMP_Text StatCard(RectTransform parent, string name, string icon, out GameObject card)
        {
            var c = Box(parent, name, "kit_card_stat", 0, 0, 340, 130);
            var fit = c.gameObject.AddComponent<HorizontalLayoutGroup>();
            fit.padding = new RectOffset(Mathf.RoundToInt(UiKit.Px(26)), Mathf.RoundToInt(UiKit.Px(30)), 0, 0);
            fit.spacing = UiKit.Px(12);
            fit.childAlignment = TextAnchor.MiddleCenter;
            fit.childControlWidth = true;
            fit.childControlHeight = false;
            fit.childForceExpandWidth = fit.childForceExpandHeight = false;
            c.gameObject.AddComponent<LayoutElement>().minWidth = UiKit.Px(340);
            var pic = Rel(c, "Icon", 26, 37, 56, 56);
            pic.gameObject.AddComponent<LayoutElement>().preferredWidth = UiKit.Px(56);
            var img = pic.gameObject.AddComponent<Image>();
            img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(StatusIcons + icon + ".png");
            img.preserveAspect = true;
            img.raycastTarget = false;
            card = c.gameObject;
            var value = Txt(c, "Value", Tier.B1, "", C.Text, 94, 65, 230);
            value.overflowMode = TextOverflowModes.Overflow; // width comes from the layout (preferred width): nothing to cut
            return value;
        }

        /// <summary>B6 table row 60 px: kit_row_normal/alt, kit_row_selected for the own row, rank, name, kills with icon, survival.</summary>
        private static ResultRowView ResultRow(RectTransform parent, int index, float x, float y, float w, bool abs = false)
        {
            var r = abs ? Abs(parent, "Row" + index, x, y, w, 60) : Rel(parent, "Row" + index, x, y, w, 60);
            Box(r, "Base", index % 2 == 1 ? "kit_row_alt" : "kit_row_normal", 0, 0, w, 60);
            var row = r.gameObject.AddComponent<ResultRowView>();
            var highlight = UiKit.Image(Rel(r, "Highlight", 0, 0, w, 60), "kit_row_selected");
            highlight.enabled = false;
            Set(row, "background", highlight);
            Set(row, "rank", Txt(r, "Rank", Tier.B2, "", C.Muted, 25, 30, 80));
            Set(row, "playerName", Txt(r, "Name", Tier.B2, "", C.Text, 110, 30, w - 300));
            var kills = Txt(r, "Kills", Tier.B2, "", C.Muted, w - 170, 30, 50, Right);
            var icon = Rel((RectTransform)kills.transform, "Icon", -44, 1, 32, 32).gameObject.AddComponent<Image>(); // hides with the text
            icon.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(StatusIcons + "icon_kill.png");
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            Set(row, "kills", kills);
            Set(row, "survival", Txt(r, "Survival", Tier.B2, "", C.Muted, w - 115, 30, 90, Right));
            return row;
        }

        /// <summary>B8 Mất kết nối, đếm 15 s: over the HUD behind a dim; panel_m 760×420, icon, bar of the grace left, clock + seconds.</summary>
        private static ReconnectView BuildReconnect(RectTransform rt)
        {
            var v = Screen<ReconnectView>(rt, "Reconnect");
            var r = Rt(v);
            UiKit.Dim(r);
            var p = Box(r, "Panel", "kit_panel_m", 580, 330, 760, 420, true);
            UiKit.Image(Rel(p, "Icon", 342, 58, 75, 64), "fx_icon_disconnect");
            Set(v, "message", Txt(p, "Message", Tier.H2, "Đang kết nối lại…", C.Text, 40, 170, 680, Centre));
            Set(v, "progressFill", Progress(p, 70, 230, 620, 0f));
            PlateIcon(p, "Clock", "kit_ico_clock", 83, 290, 26);
            Set(v, "seconds", Txt(p, "Seconds", Tier.B2, "", C.Gold, 120, 290, 500));
            Set(v, "home", Btn(p, "Home", Kind.Secondary, Size.M, 230, 310, 300, "kit_ico_home", "Về sảnh"));
            return v;
        }

        /// <summary>B8 hết hạn (lost) and A7 lỗi (load error): one panel_m 760×420, icon by kind, title, reason, Về trang chủ.</summary>
        private static NoticeView BuildNotice(RectTransform rt)
        {
            var v = Screen<NoticeView>(rt, "Notice");
            var r = Rt(v);
            UiKit.Dim(r);
            var p = Box(r, "Panel", "kit_panel_m", 580, 330, 760, 420, true);
            var disconnect = UiKit.Image(Rel(p, "DisconnectIcon", 342, 58, 75, 64), "fx_icon_disconnect");
            var loadError = UiKit.Image(Rel(p, "LoadErrorIcon", 360, 70, 40, 40), "kit_icon_cross");
            loadError.gameObject.SetActive(false);
            Set(v, "disconnectIcon", disconnect.gameObject);
            Set(v, "loadErrorIcon", loadError.gameObject);
            Set(v, "title", Txt(p, "Title", Tier.H2, "", C.Text, 40, 170, 680, Centre));
            Set(v, "message", Para(p, "Message", Tier.B2, "", C.Text, 40, 206, 680, 80, TextAlignmentOptions.Top));
            Set(v, "home", Btn(p, "Home", Kind.Primary, Size.M, 220, 310, 320, "kit_ico_home", "Về trang chủ"));
            return v;
        }

        // ---------- kit parts used by several screens ----------

        /// <summary>Full-screen panel with its CanvasGroup (shown/hidden by alpha only) and view component.</summary>
        private static T Screen<T>(RectTransform parent, string name) where T : LobbyPanelView
        {
            var r = UiKit.Stretch(UiKit.Node(parent, name));
            var group = r.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = group.blocksRaycasts = false;
            var view = r.gameObject.AddComponent<T>();
            Set(view, "group", group);
            return view;
        }

        private static RectTransform Rt(Component c) => (RectTransform)c.transform;

        /// <summary>Absolute mock rect, centred on the parent (screen-level parts).</summary>
        private static RectTransform Abs(RectTransform parent, string name, float x, float y, float w, float h) =>
            UiKit.PlaceCentred(UiKit.Node(parent, name), x, y, w, h);

        /// <summary>Rect in px from the parent's top left (parts inside a panel).</summary>
        private static RectTransform Rel(RectTransform parent, string name, float x, float y, float w, float h) =>
            UiKit.Place(UiKit.Node(parent, name), x, y, w, h);

        private static RectTransform Box(RectTransform parent, string name, string sprite, float x, float y, float w, float h, bool abs = false)
        {
            var t = abs ? Abs(parent, name, x, y, w, h) : Rel(parent, name, x, y, w, h);
            UiKit.Image(t, sprite);
            return t;
        }

        private static T Hidden<T>(T c) where T : Component
        {
            c.gameObject.SetActive(false);
            return c;
        }

        /// <summary>One line of text whose middle sits at <paramref name="yMid"/> (the mock's "lm"/"mm"/"rm" anchors).</summary>
        private static TextMeshProUGUI Txt(RectTransform parent, string name, Tier tier, string value, Color color,
            float x, float yMid, float w, TextAlignmentOptions align = Left, bool abs = false)
        {
            var t = UiKit.Text(parent, name, tier, value, color, align);
            var h = Mathf.Ceil((int)tier * 1.4f);
            var r = (RectTransform)t.transform;
            if (abs) UiKit.PlaceCentred(r, x, yMid - h / 2f, w, h); else UiKit.Place(r, x, yMid - h / 2f, w, h);
            t.overflowMode = TextOverflowModes.Ellipsis;
            return t;
        }

        /// <summary>Wrapped text block from its top edge.</summary>
        private static TextMeshProUGUI Para(RectTransform parent, string name, Tier tier, string value, Color color,
            float x, float yTop, float w, float h, TextAlignmentOptions align = TextAlignmentOptions.TopLeft)
        {
            var t = UiKit.Text(parent, name, tier, value, color, align);
            UiKit.Place((RectTransform)t.transform, x, yTop, w, h);
            t.textWrappingMode = TextWrappingModes.Normal;
            t.overflowMode = TextOverflowModes.Ellipsis;
            return t;
        }

        private static Button Btn(RectTransform parent, string name, Kind kind, Size size, float x, float y, float w,
            string icon, string label, bool abs = false)
        {
            var b = UiKit.Button(parent, name, kind, size, w, icon, label);
            var t = (RectTransform)b.transform;
            if (abs) UiKit.PlaceCentred(t, x, y, w, (int)size); else UiKit.Place(t, x, y, w, (int)size);
            return b;
        }

        /// <summary>Icon-only button: kit_iconbtn_square_* or kit_close_* (sprite swap), ink or red icon on the face.</summary>
        private static Button IconButton(RectTransform parent, string name, float x, float y, float s, string face, string icon, float iconPx)
        {
            var t = Rel(parent, name, x, y, s, s);
            var img = UiKit.Image(t, face + "_normal");
            img.raycastTarget = true;
            var b = t.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            b.transition = Selectable.Transition.SpriteSwap;
            b.spriteState = new SpriteState
            {
                highlightedSprite = UiKit.Sprite(face + "_hover"),
                selectedSprite = UiKit.Sprite(face + "_normal"),
                pressedSprite = UiKit.Sprite(face + "_pressed"),
                disabledSprite = UiKit.Sprite(face + "_disabled"),
            };
            var faceMiddle = s * (1f - 0.11f) / 2f; // centre of the face above the 11 % base
            UiKit.Image(Rel(t, "Icon", (s - iconPx) / 2f, faceMiddle - iconPx / 2f, iconPx, iconPx), icon);
            return b;
        }

        /// <summary>Ink icon on a light round plate (ink icons are only drawn on light faces, ui-kit.md mục 10).</summary>
        private static RectTransform PlateIcon(RectTransform parent, string name, string icon, float cx, float cy, float s)
        {
            var plate = s + 20f;
            var t = Box(parent, name, "kit_iconbtn_round_normal", cx - plate / 2f, cy - plate / 2f, plate, plate);
            UiKit.Image(Rel(t, "Icon", 10, 8, s, s), icon); // 2 px up, as the mock's ico()
            return t;
        }

        /// <summary>kit_panel_popup centred on the screen, gold title in the 62 px title bar (mock popup()).</summary>
        private static RectTransform Popup(RectTransform parent, float w, float h, string title)
        {
            var p = Box(parent, "Panel", "kit_panel_popup", (W - w) / 2f, (H - h) / 2f, w, h, true);
            Txt(p, "Title", Tier.H2, title, C.Gold, 90, 24 + TitleBar / 2f, w - 180, Centre);
            return p;
        }

        /// <summary>kit_close X at the popup's top right; it presses the popup's own cancel button.</summary>
        private static void Close(RectTransform popup, float w, Button cancel)
        {
            var x = IconButton(popup, "Close", w - 52 - 34, 29, 52, "kit_close", "kit_icon_cross_red", 26);
            Set(x.gameObject.AddComponent<ForwardClick>(), "target", cancel);
        }

        /// <summary>kit_dialog_footer at the popup's bottom; returns the y of its 64 px buttons (16 below the footer top).</summary>
        private static float Footer(RectTransform popup, float w, float h)
        {
            var top = h - 47 - 96;
            Box(popup, "Footer", "kit_dialog_footer", 47, top, w - 94, 96);
            return top + 16;
        }

        /// <summary>kit_screen_header 1800×104 at (60, 30) with the gold title in the middle.</summary>
        private static RectTransform Header(RectTransform parent, string title)
        {
            var h = Box(parent, "Header", "kit_screen_header", 60, 30, 1800, 104, true);
            Txt(h, "Title", Tier.H2, title, C.Gold, 300, 54, 1200, Centre);
            return h;
        }

        private static Button BackButton(RectTransform header) =>
            IconButton(header, "Back", 36, 20, 64, "kit_iconbtn_square", "kit_ico_arrow_left", 40);

        /// <summary>kit_progress_track with the fill inset 6 px; the fill is stretched to <paramref name="share"/> of the track.</summary>
        private static RectTransform Progress(RectTransform parent, float x, float y, float w, float share, bool abs = false)
        {
            var track = Box(parent, "Progress", "kit_progress_track", x, y, w, 28, abs);
            var area = Rel(track, "FillArea", 6, 6, w - 12, 16);
            var fill = UiKit.Stretch(UiKit.Node(area, "Fill"));
            UiKit.Image(fill, "kit_progress_fill");
            fill.anchorMax = new Vector2(share, 1f);
            fill.gameObject.SetActive(share > 0.01f);
            return fill;
        }

        /// <summary>A3b tab: kit_tab_off face (hover swap) with the taller kit_tab_on drawn over it when active.</summary>
        private static Button Tab(RectTransform parent, string name, string label, float x, float y, out GameObject on)
        {
            var t = Box(parent, name, "kit_tab_off", x, y, 280, 64);
            var img = t.GetComponent<Image>();
            img.raycastTarget = true;
            var b = t.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            b.transition = Selectable.Transition.SpriteSwap;
            b.spriteState = new SpriteState { highlightedSprite = UiKit.Sprite("kit_tab_hover"), selectedSprite = UiKit.Sprite("kit_tab_off") };
            on = Box(t, "On", "kit_tab_on", 0, 0, 280, 64).gameObject;
            Txt(t, "Label", Tier.B2, label, C.Ink, 0, 40, 280, Centre);
            return b;
        }

        /// <summary>Place the visible title at (cx, y); account for the generated PNG's transparent padding.</summary>
        private static void Logo(RectTransform parent, float cx, float y, float w)
        {
            var k = w / 2124f;
            var img = Abs(parent, "Logo", cx - w / 2f - 31f * k, y - 148f * k, 2172f * k, 724f * k).gameObject.AddComponent<Image>();
            img.sprite = Menu("Logo");
            img.preserveAspect = true;
            img.raycastTarget = false;
        }

        private static TMP_InputField Input(RectTransform parent, string name, string placeholder, float x, float y, float w,
            Tier tier = Tier.B2, bool abs = false)
        {
            var go = TMP_DefaultControls.CreateInputField(new TMP_DefaultControls.Resources());
            go.name = name;
            var r = (RectTransform)go.transform;
            r.SetParent(parent, false);
            if (abs) UiKit.PlaceCentred(r, x, y, w, InputHeight); else UiKit.Place(r, x, y, w, InputHeight);
            UiKit.Image(r, "kit_input_normal").raycastTarget = true;
            var field = go.GetComponent<TMP_InputField>();
            field.transition = Selectable.Transition.SpriteSwap;
            field.spriteState = new SpriteState
            {
                highlightedSprite = UiKit.Sprite("kit_input_focus"),
                selectedSprite = UiKit.Sprite("kit_input_focus"),
                pressedSprite = UiKit.Sprite("kit_input_focus"),
            };
            field.textViewport.offsetMin = new Vector2(UiKit.Px(20), UiKit.Px(4));
            field.textViewport.offsetMax = new Vector2(-UiKit.Px(20), -UiKit.Px(4));
            field.pointSize = UiKit.Px((int)tier);
            Style(field.textComponent, tier, C.Text);
            var hint = (TMP_Text)field.placeholder;
            hint.text = placeholder;
            Style(hint, tier, C.Muted);
            return field;
        }

        private static void Style(TMP_Text t, Tier tier, Color color)
        {
            UiKit.ApplyTier(t, tier);
            t.color = color;
            t.verticalAlignment = VerticalAlignmentOptions.Middle; // UX review: text and hint centred
        }

        /// <summary>
        /// Kit dropdown (ui-kit.md mục 2): kit_dropdown_closed 56 tall (border L19 R68 T19 B19, the right 68 px hold the
        /// arrow box), kit_arrow_down 16×10 centred in that box, the value in the left part; while the list is open the
        /// header swaps to kit_dropdown_open (selected state). The list is kit rows 48 px: kit_row_alt behind, kit_row_normal
        /// per item, kit_row_selected for the current one, kit_scroll_track/thumb. Component, options and listeners stay TMP's.
        /// </summary>
        private static TMP_Dropdown Drop(RectTransform parent, string name, float x, float y, float w)
        {
            const float arrowBox = 68f, row = 48f, rows = 6f;
            var go = TMP_DefaultControls.CreateDropdown(new TMP_DefaultControls.Resources());
            go.name = name;
            var r = (RectTransform)go.transform;
            r.SetParent(parent, false);
            UiKit.Place(r, x, y, w, InputHeight);
            UiKit.Image(r, "kit_dropdown_closed").raycastTarget = true;
            var drop = go.GetComponent<TMP_Dropdown>();
            drop.transition = Selectable.Transition.SpriteSwap;
            drop.spriteState = new SpriteState
            {
                highlightedSprite = UiKit.Sprite("kit_dropdown_closed"),
                pressedSprite = UiKit.Sprite("kit_dropdown_open"),
                selectedSprite = UiKit.Sprite("kit_dropdown_open"),
            };
            foreach (var t in go.GetComponentsInChildren<TMP_Text>(true))
            {
                UiKit.ApplyTier(t, Tier.B2);
                t.color = C.Text; // header and list rows are both dark stone
                t.verticalAlignment = VerticalAlignmentOptions.Middle;
            }
            var label = (RectTransform)r.Find("Label");
            label.offsetMin = new Vector2(UiKit.Px(20), 0f);
            label.offsetMax = new Vector2(-UiKit.Px(arrowBox + 8), 0f);
            var arrow = (RectTransform)r.Find("Arrow");
            arrow.anchorMin = arrow.anchorMax = arrow.pivot = new Vector2(1f, 0.5f);
            arrow.anchoredPosition = new Vector2(-UiKit.Px(arrowBox / 2f - 8f), 0f); // centre of the 68 px box from the right edge
            arrow.sizeDelta = new Vector2(UiKit.Px(16), UiKit.Px(10));
            UiKit.Image(arrow, "kit_arrow_down");

            var template = (RectTransform)r.Find("Template");
            template.sizeDelta = new Vector2(template.sizeDelta.x, UiKit.Px(rows * row));
            UiKit.Image(template, "kit_row_alt");
            var viewport = (RectTransform)template.Find("Viewport");
            viewport.offsetMax = new Vector2(-UiKit.Px(14), 0f); // room for the 14 px scrollbar
            var item = (RectTransform)viewport.Find("Content/Item");
            item.sizeDelta = new Vector2(item.sizeDelta.x, UiKit.Px(row));
            var content = (RectTransform)viewport.Find("Content");
            content.sizeDelta = new Vector2(content.sizeDelta.x, UiKit.Px(row));
            var itemBack = UiKit.Image((RectTransform)item.Find("Item Background"), "kit_row_normal");
            itemBack.raycastTarget = true;
            var check = UiKit.Stretch((RectTransform)item.Find("Item Checkmark"));
            UiKit.Image(check, "kit_row_selected"); // the toggle graphic: the chosen row gets the gold-edged row
            var itemLabel = (RectTransform)item.Find("Item Label");
            itemLabel.offsetMin = new Vector2(UiKit.Px(20), 0f);
            itemLabel.offsetMax = new Vector2(-UiKit.Px(8), 0f);
            var bar = (RectTransform)template.Find("Scrollbar");
            bar.sizeDelta = new Vector2(UiKit.Px(14), bar.sizeDelta.y);
            UiKit.Image(bar, "kit_scroll_track").raycastTarget = true;
            var area = (RectTransform)bar.Find("Sliding Area");
            area.offsetMin = area.offsetMax = Vector2.zero;
            var handle = (RectTransform)area.Find("Handle");
            handle.offsetMin = handle.offsetMax = Vector2.zero;
            UiKit.Image(handle, "kit_scroll_thumb").raycastTarget = true;
            drop.captionText.alignment = TextAlignmentOptions.MidlineLeft;
            drop.captionText.textWrappingMode = TextWrappingModes.NoWrap;
            drop.captionText.overflowMode = TextOverflowModes.Ellipsis;
            drop.ClearOptions();
            return drop;

        }

        // ---------- serialized refs ----------

        private static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetColor(Object target, string field, Color value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).colorValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(Object target, string field, float value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetArray(Object target, string field, Object[] values)
        {
            var so = new SerializedObject(target);
            var array = so.FindProperty(field);
            array.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
