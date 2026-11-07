using System.Collections.Generic;
using System.IO;
using JungleMaze.App.MatchUi;
using JungleMaze.Presentation.UI.Hud;
using JungleMaze.Presentation.UI.MatchOverlay;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace JungleMaze.EditorTools
{
    /// <summary>
    /// Repeatable assembly of MatchOverlay.prefab, SpectatorRow.prefab and MatchOverlaySet.asset (UI-03), run from the
    /// menu or Unity MCP. Skin A (T-023, docs/CONTRACT_UI_V1.md): the ESC menu, leave confirm, eliminated, spectator and
    /// notice follow the T-021 mocks B1-B3 in 1080p px through <see cref="UiKit"/>. The killcam keeps its flat board
    /// (no mock): only its text moves onto the T-027 scale. Field names on the views are unchanged.
    /// </summary>
    public static class MatchOverlayPrefabBuild
    {
        public const string PrefabFolder = "Assets/_JungleMaze/Prefabs/UI/MatchOverlay/";
        public const string PrefabPath = PrefabFolder + "MatchOverlay.prefab";
        public const string RowPrefabPath = PrefabFolder + "SpectatorRow.prefab";
        public const string SetFolder = "Assets/_JungleMaze/Art/UI/MatchOverlay/Resources/";
        public const string SetPath = SetFolder + "MatchOverlaySet.asset";

        public const string OverlayArt = "Assets/_JungleMaze/Art/UI/MatchOverlay/";
        private const string HudArt = "Assets/_JungleMaze/Art/Hud/";
        private const string MenuArt = "Assets/_JungleMaze/Art/UI/Menu/";
        /// <summary>Existing status icons the B2/B3 mocks use (man-hinh.md "Nguồn icon", not generated).</summary>
        private const string StatusIcons = HudArt + "StatusIcons/";

        /// <summary>The 19 overlay sprites with their 9-slice border (px, all sides), from asset_mapping.json.</summary>
        public static readonly IReadOnlyDictionary<string, int> OverlaySprites = new Dictionary<string, int>
        {
            { "SliderHandle", 0 }, { "ToggleBox", 12 }, { "ToggleCheck", 0 }, { "ZoneRing", 0 }, { "KillcamGrid", 0 },
            { "PlayerDot", 0 }, { "PlayerDotOutline", 0 }, { "KillerMark", 0 }, { "CausePlayer", 0 }, { "CauseZone", 0 },
            { "CauseLeft", 0 }, { "CauseDisconnect", 0 }, { "CauseUnknown", 0 }, { "IconVolume", 0 }, { "IconMusic", 0 },
            { "IconMouse", 0 }, { "IconShake", 0 }, { "IconExit", 0 }, { "IconSpectate", 0 }
        };

        /// <summary>Tiled across the killcam board, so it repeats instead of clamping.</summary>
        public const string TiledSprite = "KillcamGrid";

        /// <summary>Cause icon per EliminationCause (Unknown, Player, Zone, LeftMatch, Disconnected).</summary>
        public static readonly string[] CauseIcons = { "kit_icon_warn", "kit_ico_skull", "icon_zone_cause", "kit_ico_door", "kit_ico_wifi" };
        /// <summary>Ink icons sit 40 px on the 60 px kaki plate; the colour Bo thu icon (T-028 b2-bi-loai-bo) is 60 px alone.</summary>
        public static readonly bool[] CauseOnPlate = { true, true, false, true, true };
        public const string WeaponCatalogPath = "Assets/_JungleMaze/Prefabs/UI/HudItemIconCatalog.asset";

        private const int TextSize = 15;
        private static readonly HudPalette P = HudPalette.Default;
        private static TMP_FontAsset TextFont => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_JungleMaze/Art/Fonts/TMP/HudText SDF.asset");
        private static TMP_FontAsset NumberFont => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_JungleMaze/Art/Fonts/TMP/HudTextBold SDF.asset");

        [MenuItem("Jungle Maze/UI-03/Build Match Overlay")]
        public static void Build()
        {
            ConfigureImporters();
            Directory.CreateDirectory(PrefabFolder);
            Directory.CreateDirectory(SetFolder);
            BuildRowPrefab();
            var root = new GameObject("MatchOverlay", typeof(RectTransform));
            try
            {
                var canvas = root.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 20; // above the HUD (10)
                var scaler = root.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1280, 720);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
                root.AddComponent<GraphicRaycaster>();
                var overlay = root.AddComponent<MatchOverlayRoot>();
                var rt = (RectTransform)root.transform;

                BuildPanels(rt, overlay);

                var events = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                events.transform.SetParent(rt, false);
                events.SetActive(false); // switched on at composition only when the scene has none
                Set(overlay, "canvas", canvas);
                Set(overlay, "eventSystem", events);

                UiKit.FaceAll(root); // pressed K2 face sinks icon + label (Huy 07/11)
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                var set = AssetDatabase.LoadAssetAtPath<MatchOverlaySet>(SetPath);
                if (set == null)
                {
                    set = ScriptableObject.CreateInstance<MatchOverlaySet>();
                    AssetDatabase.CreateAsset(set, SetPath);
                }
                Set(set, "overlayPrefab", prefab.GetComponent<MatchOverlayRoot>());
                EditorUtility.SetDirty(set);
                AssetDatabase.SaveAssets();
            }
            finally { Object.DestroyImmediate(root); }
        }

        /// <summary>Sprite (2D and UI), Single, PPU 100, Point, no mipmaps, uncompressed, pivot centre, FullRect, border per table.</summary>
        [MenuItem("Jungle Maze/UI-03/Import Overlay Sprites")]
        public static void ConfigureImporters()
        {
            foreach (var pair in OverlaySprites)
            {
                var path = OverlayArt + pair.Key + ".png";
                if (!(AssetImporter.GetAtPath(path) is TextureImporter importer))
                {
                    Debug.LogError("[UI-03] Missing overlay sprite " + path);
                    continue;
                }
                var border = new Vector4(pair.Value, pair.Value, pair.Value, pair.Value);
                var wrap = pair.Key == TiledSprite ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                var dirty = importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single
                    || importer.filterMode != FilterMode.Point || importer.mipmapEnabled || importer.textureCompression != TextureImporterCompression.Uncompressed
                    || !Mathf.Approximately(importer.spritePixelsPerUnit, 100) || importer.wrapMode != wrap || settings.spriteBorder != border
                    || settings.spriteMeshType != SpriteMeshType.FullRect || settings.spriteAlignment != (int)SpriteAlignment.Center;
                if (!dirty) continue;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.alphaIsTransparency = true;
                importer.wrapMode = wrap;
                importer.spritePixelsPerUnit = 100;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.Center;
                settings.spritePivot = new Vector2(0.5f, 0.5f);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                settings.spriteBorder = border;
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }
        }

        private static void BuildPanels(RectTransform rt, MatchOverlayRoot overlay)
        {
            // Sibling order is draw order: killcam board at the back, the ESC menu and its confirm box on top.
            Set(overlay, "killcam", BuildKillcam(rt));
            Set(overlay, "spectator", BuildSpectator(rt));
            Set(overlay, "eliminated", BuildEliminated(rt));
            Set(overlay, "notice", BuildNotice(rt));
            Set(overlay, "menu", BuildMenu(rt));
            Set(overlay, "confirm", BuildConfirm(rt));
        }

        private static RectTransform Screen(string name, RectTransform parent)
        {
            var r = Rect(name, parent, 0, 0, 1280, 720);
            Stretch(r);
            return r;
        }

        // ---------- B2 Eliminated (T-021 b2-bi-loai*, panel M 920×860 at (500,110)) ----------

        private static EliminatedView BuildEliminated(RectTransform parent)
        {
            var root = Screen("Eliminated", parent);
            var group = Group(root);
            UiKit.Floor(root); // stone floor + wash (Integrator 07/11, man-hinh.md: HUD canvas is off after elimination)
            var view = root.gameObject.AddComponent<EliminatedView>();
            var frame = UiKit.PlaceCentred(UiKit.Node(root, "Frame"), 500, 110, 920, 860);
            UiKit.Panel(frame, UiKit.PanelKind.M);

            // Red X + title, centred as one group.
            var titleLine = HRow(frame, "TitleLine", 0, 60, 920, 40, 10);
            Size(UiKit.Image(UiKit.Node(titleLine, "Icon"), "kit_icon_cross_red"), 30, 30);
            var title = UiKit.Text(titleLine, "Title", UiKit.Tier.B1, EliminationText.Title, UiKit.Colors.Text, TextAlignmentOptions.Center);
            // "#4" 96 + "/ 10" 40 muted, bottoms aligned, centred as one group (b2-bi-loai).
            var rankLine = HRow(frame, "RankLine", 0, 135, 920, 110, 18);
            rankLine.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.LowerCenter;
            var rank = UiKit.Text(rankLine, "Rank", UiKit.Tier.H1, "#4", UiKit.Colors.Text, TextAlignmentOptions.Center);
            var rankTotal = UiKit.Text(rankLine, "RankTotal", UiKit.Tier.H2, "/ 10", UiKit.Colors.Muted, TextAlignmentOptions.Center);

            // Cause card: icon in the card's left slot (kaki plate, ink icon), text to its right.
            var causeCard = Kit(frame, "CauseCard", "kit_card_cause", 60, 280, 800, 150).rectTransform;
            var causePlate = Kit(causeCard, "IconPlate", "kit_iconbtn_round_normal", 58, 45, 60, 60);
            // Centre pivot, 2 px up on the plate face: the view scales it 1.5 (no plate) for the colour Bo thu icon.
            var causeIcon = UiKit.Image(UiKit.Node(causePlate.rectTransform, "Icon"), "kit_ico_skull");
            var ci = causeIcon.rectTransform;
            ci.anchorMin = ci.anchorMax = ci.pivot = new Vector2(0.5f, 0.5f);
            ci.anchoredPosition = new Vector2(0f, UiKit.Px(2));
            ci.sizeDelta = Vector2.one * UiKit.Px(40);
            var cause = Label(causeCard, "Cause", UiKit.Tier.B1, "Không rõ nguyên nhân", UiKit.Colors.Text, TextAlignmentOptions.MidlineLeft, 160, 0, 530, 150, true);
            // Killer's weapon (mock: icon 68 after the name); fixed at the card's right end so a long cause line cannot push it out.
            var causeWeapon = ArtIcon(causeCard, "WeaponIcon", null, 696, 41, 68, 68);
            causeWeapon.gameObject.SetActive(false);

            // Stat cards in a centred row: a card whose line has no data is switched off and the other recentres.
            var stats = HRow(frame, "Stats", 0, 460, 920, 130, 20);
            var killsCard = StatCard(stats, "KillsCard", StatusIcons + "icon_kill.png", "Kills", "0", out var kills);
            var survivalCard = StatCard(stats, "SurvivalCard", StatusIcons + "icon_zone-timer.png", "Survival", "00:00", out var survival);

            var home = KitButton(frame, "Home", UiKit.ButtonKind.Secondary, 60, 694, 360, "kit_ico_home", "Về sảnh");
            var spectate = KitButton(frame, "Spectate", UiKit.ButtonKind.Primary, 500, 694, 360, "kit_ico_eye", "Theo dõi");
            var blocked = Label(frame, "BlockedReason", UiKit.Tier.B2, "Không còn người chơi nào để theo dõi", UiKit.Colors.Muted, TextAlignmentOptions.Center, 60, 772, 800, 30);

            Set(view, "group", group);
            Set(view, "frame", frame);
            Set(view, "title", title);
            Set(view, "rank", rank);
            Set(view, "rankTotal", rankTotal);
            Set(view, "causePlate", causePlate);
            Set(view, "causeWeaponIcon", causeWeapon);
            Set(view, "weaponIcons", AssetDatabase.LoadAssetAtPath<HudItemIconCatalog>(WeaponCatalogPath));
            SetBools(view, "causeOnPlate", CauseOnPlate);
            Set(view, "cause", cause);
            Set(view, "kills", kills);
            Set(view, "survival", survival);
            Set(view, "killsCard", killsCard);
            Set(view, "survivalCard", survivalCard);
            Set(view, "blockedReason", blocked);
            Set(view, "home", home);
            Set(view, "spectate", spectate);
            Set(view, "causeIcon", causeIcon);
            var icons = new Object[CauseIcons.Length];
            for (var i = 0; i < icons.Length; i++) icons[i] = UiKit.Sprite(CauseIcons[i]);
            SetArray(view, "causeIcons", icons);
            return view;
        }

        private static GameObject StatCard(RectTransform row, string name, string iconPath, string textName, string value, out TMP_Text text)
        {
            var card = UiKit.Image(UiKit.Node(row, name), "kit_card_stat");
            Size(card, 300, 130);
            Layout(card.gameObject.AddComponent<HorizontalLayoutGroup>(), 14);
            Size(ArtIcon(card.rectTransform, "Icon", iconPath), 56, 56);
            text = UiKit.Text(card.rectTransform, textName, UiKit.Tier.H2, value, UiKit.Colors.Text); // value only, 40 (mock)
            return card.gameObject;
        }

        // ---------- B3 Spectator (T-021 b3-spectator*; gameplay text floor 24) ----------

        private static void BuildRowPrefab()
        {
            var go = new GameObject("SpectatorRow", typeof(RectTransform));
            var row = (RectTransform)go.transform;
            row.sizeDelta = new Vector2(UiKit.Px(300), UiKit.Px(48));
            try
            {
                var bg = UiKit.Image(row, "kit_row_normal");
                bg.raycastTarget = true;
                var outline = UiKit.Image(UiKit.Stretch(UiKit.Node(row, "Selected")), "kit_row_selected");
                outline.enabled = false;
                var button = go.AddComponent<Button>();
                button.targetGraphic = bg;
                var label = Label(row, "Label", UiKit.Tier.B2, "Người chơi", UiKit.Colors.Text, TextAlignmentOptions.MidlineLeft, 20, 0, 270, 48, true);
                var view = go.AddComponent<SpectatorRowView>();
                Set(view, "button", button);
                Set(view, "background", bg);
                Set(view, "label", label);
                Set(view, "selectedOutline", outline);
                PrefabUtility.SaveAsPrefabAsset(go, RowPrefabPath);
            }
            finally { Object.DestroyImmediate(go); }
        }

        private static SpectatorView BuildSpectator(RectTransform parent)
        {
            var root = Screen("Spectator", parent);
            var group = Group(root);
            var view = root.gameObject.AddComponent<SpectatorView>();
            // No UiKit.Floor here: the mock's floor stands in for the watched player's world, which must stay visible.

            // Target card at the top: eye, name (gold), alive icon + count.
            var block = UiKit.PlaceCentred(UiKit.Node(root, "Watching"), 660, 20, 600, 120);
            UiKit.Image(block, "kit_card_room_selected");
            PlateIcon(block, "IconPlate", "kit_ico_eye", 100, 60, 30);
            var name = Label(block, "Name", UiKit.Tier.B1, "Người chơi", UiKit.Colors.Gold, TextAlignmentOptions.MidlineLeft, 140, 0, 175, 120, true);
            ArtIcon(block, "AliveIcon", StatusIcons + "icon_alive.png", 320, 42, 36, 36);
            var alive = Label(block, "Alive", UiKit.Tier.B2, "Còn sống 9/10", UiKit.Colors.Text, TextAlignmentOptions.MidlineLeft, 366, 0, 210, 120);

            // Alive list, at most 9 rows, 56 px apart; odd rows use the alternate row face.
            var list = UiKit.Place(UiKit.Node(root, "List"), 30, 190, 300, 8 * 56 + 48);
            var rowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RowPrefabPath);
            var rows = new Object[9];
            for (var i = 0; i < rows.Length; i++)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(rowPrefab, list);
                UiKit.Place((RectTransform)go.transform, 0, i * 56, 300, 48);
                go.name = "Row" + (i + 1);
                if (i % 2 == 1) go.GetComponent<Image>().sprite = UiKit.Sprite("kit_row_alt");
                rows[i] = go.GetComponent<SpectatorRowView>();
            }

            var card = UiKit.Place(UiKit.Node(root, "TargetCard"), 30, 770, 460, 180);
            UiKit.Image(card, "kit_card_stat");
            var cardName = Label(card, "Name", UiKit.Tier.B1, "Người chơi", UiKit.Colors.Text, TextAlignmentOptions.MidlineLeft, 54, 20, 180, 40, true);
            var weapon = Label(card, "Weapon", UiKit.Tier.B2, "Súng nước", UiKit.Colors.Muted, TextAlignmentOptions.MidlineRight, 240, 20, 140, 40, true);
            var weaponIcon = ArtIcon(card, "WeaponIcon", null, 388, 18, 44, 44); // weapons-icons.md: 44 beside the name
            weaponIcon.gameObject.SetActive(false);
            var hp = UiKit.Place(UiKit.Node(card, "Hp"), 52, 84, 400, 36);
            ArtIcon(hp, "Icon", StatusIcons + "icon_hp.png", 0, 0, 36, 36);
            var track = Kit(hp, "Track", "kit_progress_track", 48, 6, 210, 24).rectTransform;
            var fillArea = UiKit.Place(UiKit.Node(track, "FillArea"), 6, 4, 198, 16);
            var fill = UiKit.Image(UiKit.Stretch(UiKit.Node(fillArea, "Fill")), "kit_progress_fill").rectTransform;
            var hpText = Label(hp, "Value", UiKit.Tier.B2, "100/100", UiKit.Colors.Text, TextAlignmentOptions.MidlineLeft, 270, 0, 110, 36);

            // Keys: A D = change player, E = the one who knocked you out (button), home = icon square.
            var keys = UiKit.PlaceCentred(UiKit.Node(root, "KeyBar"), 540, 990, 184, 52);
            Keycap(keys, "A", 0);
            Keycap(keys, "D", 62);
            PlateIcon(keys, "People", "kit_ico_people", 160, 26, 28);
            var killerRect = UiKit.PlaceCentred(UiKit.Node(root, "Killer"), 780, 990, 124, 52);
            var cap = Keycap(killerRect, "E", 0);
            cap.raycastTarget = true;
            var killer = Swap(killerRect.gameObject.AddComponent<Button>(), cap, "kit_keycap_normal", "kit_keycap_active", "kit_keycap_active", "kit_keycap_disabled");
            PlateIcon(killerRect, "Skull", "kit_ico_skull", 100, 26, 28);
            var home = IconButton(root, "Home", "kit_iconbtn_square", "kit_ico_home", 64);
            UiKit.PlaceCentred((RectTransform)home.transform, 1620, 976, 64, 64);

            Set(view, "group", group);
            Set(view, "watchingBlock", block);
            Set(view, "watchingName", name);
            Set(view, "aliveCount", alive);
            Set(view, "keyBar", keys);
            Set(view, "killer", killer);
            Set(view, "home", home);
            Set(view, "card", card);
            Set(view, "cardName", cardName);
            Set(view, "cardHp", hp.gameObject);
            Set(view, "cardHpFill", fill);
            Set(view, "cardHpText", hpText);
            Set(view, "cardWeapon", weapon);
            Set(view, "cardWeaponIcon", weaponIcon);
            Set(view, "weaponIcons", AssetDatabase.LoadAssetAtPath<HudItemIconCatalog>(WeaponCatalogPath));
            SetArray(view, "rows", rows);
            return view;
        }

        private static Image Keycap(RectTransform parent, string key, float x)
        {
            var cap = Kit(parent, "Key" + key, "kit_keycap_normal", x, 0, 52, 52);
            Label(cap.rectTransform, "Label", UiKit.Tier.B2, key, UiKit.Colors.Ink, TextAlignmentOptions.Center, 0, 0, 52, 46);
            return cap;
        }

        // ---------- Killcam (T-5: own flat board, no world; no mock: text onto the scale only) ----------

        private static KillcamView BuildKillcam(RectTransform parent)
        {
            var root = Screen("Killcam", parent);
            var group = Group(root);
            Img(root, new Color(0.047f, 0.078f, 0.090f, 0.97f), true);
            var view = root.gameObject.AddComponent<KillcamView>();
            var board = Centered("Board", root, 1280, 720);
            var grid = Img(board, P.Grid);
            grid.sprite = Overlay(TiledSprite);
            grid.type = Image.Type.Tiled; // 48 px per tile, never one stretched tile
            var zone = Centered("Zone", board, 480, 480);
            Img(zone, new Color(0x95 / 255f, 0x72 / 255f, 0xBA / 255f, 0.85f)).sprite = Overlay("ZoneRing");
            var dots = new Object[10];
            var images = new Object[10];
            var names = new Object[10];
            var fills = new Object[10];
            var marks = new Object[10];
            for (var i = 0; i < 10; i++)
            {
                var dot = Centered("Player" + i, board, 18, 18);
                var image = Img(dot, P.Text);
                image.sprite = Overlay("PlayerDot");
                Layer(dot, "Outline", Overlay("PlayerDotOutline"), P.Edge, 1f).type = Image.Type.Simple; // rim only, no overlap
                var mark = Layer(dot, "KillerMark", Overlay("KillerMark"), P.Amber, 1f);
                mark.type = Image.Type.Simple;
                mark.gameObject.SetActive(false);
                marks[i] = mark.gameObject;
                var label = Txt("Name", dot, -51, -24, 120, 18, "#" + (i + 1), 13, TextFont, TextAlignmentOptions.Center, P.Text);
                var track = Rect("Hp", dot, -3, 22, 24, 4);
                Skin(Img(track, P.Track), Hud("BarTrack"), 3f); // 4 px tall, 6 px border
                var fill = Rect("Fill", track, 0, 0, 24, 4);
                Stretch(fill);
                Skin(Img(fill, P.Red), Hud("BarFill"), 3f);
                dot.gameObject.SetActive(false);
                dots[i] = dot;
                images[i] = image;
                names[i] = label;
                fills[i] = fill;
            }
            var title = Txt("Title", root, 340, 24, 600, 28, "Killcam · góc nhìn của kẻ hạ bạn", 17, TextFont, TextAlignmentOptions.Center, P.Text);
            var countdown = Txt("Countdown", root, 490, 56, 300, 40, "Killcam 5,0 s", 26, NumberFont, TextAlignmentOptions.Center, P.Amber);
            var skip = Btn("Skip", root, 540, 640, 200, 48, "Bỏ qua (Space)", Secondary, P.Text);
            // T-027, gameplay floor 24: names 19.5 -> 24, title 25.5 -> 24, countdown 39 -> 40 (Chakra Petch), skip 22.5 -> 24;
            // rects unchanged. A 24 px name (line ~32 px) is taller than its 18-unit rect: overflow, not ellipsis, so "#10" stays whole.
            UiKit.SnapAll(root.gameObject, 24f);
            foreach (var n in names) ((TMP_Text)n).overflowMode = TextOverflowModes.Overflow;
            Set(view, "group", group);
            Set(view, "board", board);
            Set(view, "zone", zone);
            SetArray(view, "dots", dots);
            SetArray(view, "dotImages", images);
            SetArray(view, "names", names);
            SetArray(view, "hpFills", fills);
            SetArray(view, "killerMarks", marks);
            Set(view, "title", title);
            Set(view, "countdown", countdown);
            Set(view, "skip", skip);
            return view;
        }

        // ---------- notice toast (b3-spectator-chuyen: kit_toast under the target card) ----------

        private static OverlayNoticeView BuildNotice(RectTransform parent)
        {
            // 640 wide, not the mock's 440: the same toast carries the long "Killcam không khả dụng: …" lines.
            var box = UiKit.PlaceCentred(UiKit.Node(parent, "Notice"), 640, 170, 640, 70);
            var group = Group(box);
            UiKit.Image(box, "kit_toast");
            var text = Label(box, "Text", UiKit.Tier.B2, "", UiKit.Colors.Text, TextAlignmentOptions.Center, 24, 0, 592, 70, true);
            var view = box.gameObject.AddComponent<OverlayNoticeView>();
            Set(view, "group", group);
            Set(view, "text", text);
            return view;
        }

        // ---------- B1 ESC menu (b1-esc-cai-dat: popup 820×700 at (550,180), values apply at once, no Save) ----------

        private static MatchMenuView BuildMenu(RectTransform parent)
        {
            var root = Screen("Menu", parent);
            var group = Group(root);
            UiKit.Dim(root);
            var panel = UiKit.PlaceCentred(UiKit.Node(root, "Panel"), 550, 180, 820, 700);
            UiKit.Panel(panel, UiKit.PanelKind.Popup);
            var view = root.gameObject.AddComponent<MatchMenuView>();
            Label(panel, "Title", UiKit.Tier.H2, "Cài đặt", UiKit.Colors.Gold, TextAlignmentOptions.Center, 0, 24, 820, 62);
            var close = CloseButton(panel, 820);

            var master = SliderRow(panel, "Master", "Âm lượng", "kit_ico_sound", 142, 0f, 1f, false, out var masterValue);
            var sfx = SliderRow(panel, "Sfx", "Hiệu ứng", "kit_ico_sound", 206, 0f, 1f, false, out var sfxValue);
            var music = SliderRow(panel, "Music", "Nhạc nền", "kit_ico_music", 270, 0f, 1f, false, out var musicValue);
            Kit(panel, "Divider", "kit_divider", 70, 334, 680, 14);
            var musicRow = Row(panel, "MusicToggleRow", 348, "kit_ico_music", "Nhạc");
            var musicToggle = KitToggle(musicRow, "MusicEnabled");
            var musicOff = Label(musicRow, "MusicOff", UiKit.Tier.B2, MatchOverlayPresenter.MusicOffText, UiKit.Colors.Muted, TextAlignmentOptions.MidlineLeft, 290, 0, 270, 48);
            var sensitivity = SliderRow(panel, "Sensitivity", "Chuột", null, 418, 5f, 20f, true, out var sensitivityValue);
            var shakeRow = Row(panel, "ShakeRow", 482, "kit_ico_vibrate", "Rung");
            var shake = KitToggle(shakeRow, "ScreenShake");
            var retry = Label(panel, "RetryNotice", UiKit.Tier.B2, MatchOverlayPresenter.RetryText, UiKit.Colors.Gold, TextAlignmentOptions.Center, 70, 530, 680, 28);
            retry.gameObject.SetActive(false);

            Kit(panel, "Footer", "kit_dialog_footer", 47, 557, 726, 96);
            var defaults = KitButton(panel, "Defaults", UiKit.ButtonKind.Secondary, 60, 573, 200, "kit_ico_refresh", "Mặc định");
            var leave = KitButton(panel, "Leave", UiKit.ButtonKind.Exit, 280, 573, 240, null, "Thoát trận");
            var resume = KitButton(panel, "Resume", UiKit.ButtonKind.Primary, 540, 573, 220, "kit_ico_play", "Tiếp tục");

            Set(view, "group", group);
            Set(view, "master", master);
            Set(view, "sfx", sfx);
            Set(view, "music", music);
            Set(view, "sensitivity", sensitivity);
            Set(view, "masterValue", masterValue);
            Set(view, "sfxValue", sfxValue);
            Set(view, "musicValue", musicValue);
            Set(view, "sensitivityValue", sensitivityValue);
            Set(view, "musicEnabled", musicToggle);
            Set(view, "screenShake", shake);
            Set(view, "musicOffLabel", musicOff);
            Set(view, "resume", resume);
            Set(view, "defaults", defaults);
            Set(view, "leave", leave);
            Set(view, "retryNotice", retry);
            Set(view, "close", close);
            return view;
        }

        /// <summary>One 48 px setting row of the popup: icon on a kaki plate (none for Chuột, as in the mock) + label.</summary>
        private static RectTransform Row(RectTransform panel, string name, float top, string icon, string label)
        {
            var row = UiKit.Place(UiKit.Node(panel, name), 70, top, 680, 48);
            if (icon != null) PlateIcon(row, "IconPlate", icon, 14, 24, 24);
            Label(row, "Label", UiKit.Tier.B2, label, UiKit.Colors.Text, TextAlignmentOptions.MidlineLeft, 44, 0, 250, 48);
            return row;
        }

        private static Slider SliderRow(RectTransform panel, string name, string label, string icon, float top, float min, float max, bool whole, out TMP_Text value)
        {
            var row = Row(panel, name + "Row", top, icon, label);
            var slider = KitSlider(row, name, min, max, whole);
            value = Label(row, "Value", UiKit.Tier.B2, whole ? "1,0" : "100%", UiKit.Colors.Muted, TextAlignmentOptions.MidlineRight, 580, 0, 100, 48);
            return slider;
        }

        /// <summary>Track 270×16, fill 10 tall inside it, knob 31×32 centred on the value (mock slider()).</summary>
        private static Slider KitSlider(RectTransform row, string name, float min, float max, bool whole)
        {
            var r = UiKit.Place(UiKit.Node(row, name), 300, 8, 270, 32);
            Kit(r, "Background", "kit_slider_track", 0, 8, 270, 16).raycastTarget = true;
            var fillArea = UiKit.Place(UiKit.Node(r, "FillArea"), 0, 11, 270, 10);
            var fill = UiKit.Image(UiKit.Node(fillArea, "Fill"), "kit_slider_fill").rectTransform;
            var handleArea = UiKit.Stretch(UiKit.Node(r, "HandleArea"));
            var handle = UiKit.Image(UiKit.Node(handleArea, "Handle"), "kit_knob");
            handle.raycastTarget = true;
            var s = r.gameObject.AddComponent<Slider>();
            fill.anchorMin = Vector2.zero; fill.anchorMax = new Vector2(0, 1); fill.pivot = new Vector2(0, 0.5f); fill.sizeDelta = Vector2.zero; fill.anchoredPosition = Vector2.zero;
            var h = handle.rectTransform;
            h.anchorMin = Vector2.zero; h.anchorMax = new Vector2(0, 1); h.pivot = new Vector2(0.5f, 0.5f); h.sizeDelta = new Vector2(UiKit.Px(31), 0); h.anchoredPosition = Vector2.zero;
            s.fillRect = fill;
            s.handleRect = h;
            s.targetGraphic = handle;
            s.direction = Slider.Direction.LeftToRight;
            s.minValue = min;
            s.maxValue = max;
            s.wholeNumbers = whole;
            s.value = max;
            return s;
        }

        /// <summary>kit_toggle_off is the target graphic; kit_toggle_on (same size) fades in over it when on.</summary>
        private static Toggle KitToggle(RectTransform row, string name)
        {
            var off = Kit(row, name, "kit_toggle_off", 566, 0, 114, 48);
            off.raycastTarget = true;
            var on = Kit(off.rectTransform, "On", "kit_toggle_on", 0, 0, 114, 48);
            var t = off.gameObject.AddComponent<Toggle>();
            t.targetGraphic = off;
            t.graphic = on;
            t.isOn = true;
            return t;
        }

        // ---------- B1 leave confirm (b1-esc-xac-nhan-thoat: popup 700×380 at (610,350)) ----------

        private static LeaveConfirmView BuildConfirm(RectTransform parent)
        {
            var root = Screen("LeaveConfirm", parent);
            var group = Group(root);
            UiKit.Dim(root);
            var box = UiKit.PlaceCentred(UiKit.Node(root, "Box"), 610, 350, 700, 380);
            UiKit.Panel(box, UiKit.PanelKind.Popup);
            var view = root.gameObject.AddComponent<LeaveConfirmView>();
            Label(box, "Title", UiKit.Tier.H2, "Thoát trận?", UiKit.Colors.Gold, TextAlignmentOptions.Center, 0, 24, 700, 62);
            var close = CloseButton(box, 700);
            var message = Label(box, "Message", UiKit.Tier.B2, "Bạn sẽ bị loại khỏi trận.", UiKit.Colors.Text, TextAlignmentOptions.Center, 60, 150, 580, 50);
            message.textWrappingMode = TextWrappingModes.Normal;
            Kit(box, "Footer", "kit_dialog_footer", 47, 237, 606, 96);
            var confirm = KitButton(box, "Confirm", UiKit.ButtonKind.Exit, 60, 253, 270, null, "Thoát trận");
            var stay = KitButton(box, "Stay", UiKit.ButtonKind.Primary, 370, 253, 270, "kit_ico_arrow_left", "Ở lại");
            Set(view, "group", group);
            Set(view, "message", message);
            Set(view, "confirm", confirm);
            Set(view, "stay", stay);
            Set(view, "close", close);
            return view;
        }

        // ---------- kit helpers (1080p px of the mock, y down from the parent's top-left) ----------

        private static Image Kit(RectTransform parent, string name, string sprite, float x, float y, float w, float h) =>
            UiKit.Image(UiKit.Place(UiKit.Node(parent, name), x, y, w, h), sprite);

        /// <summary>An existing Art icon (not a kit sprite), Simple, aspect kept.</summary>
        private static Image ArtIcon(RectTransform parent, string name, string path, float x = 0, float y = 0, float w = 0, float h = 0)
        {
            var img = UiKit.Place(UiKit.Node(parent, name), x, y, w, h).gameObject.AddComponent<Image>();
            if (path != null) img.sprite = Load(path); // null: sprite set at runtime (weapon icons)
            img.preserveAspect = true;
            img.raycastTarget = false;
            return img;
        }

        private static TextMeshProUGUI Label(RectTransform parent, string name, UiKit.Tier tier, string value, Color color,
            TextAlignmentOptions align, float x, float y, float w, float h, bool ellipsis = false)
        {
            var t = UiKit.Text(parent, name, tier, value, color, align);
            UiKit.Place(t.rectTransform, x, y, w, h);
            if (ellipsis) t.overflowMode = TextOverflowModes.Ellipsis;
            return t;
        }

        private static Button KitButton(RectTransform parent, string name, UiKit.ButtonKind kind, float x, float y, float w, string icon, string label)
        {
            var b = UiKit.Button(parent, name, kind, UiKit.ButtonSize.M, w, icon, label);
            UiKit.Place((RectTransform)b.transform, x, y, w, (int)UiKit.ButtonSize.M);
            return b;
        }

        /// <summary>Ink icon on a round kaki plate (s + 20), 2 px up on the plate face (mock ico()). Returns the icon.</summary>
        private static Image PlateIcon(RectTransform parent, string name, string icon, float cx, float cy, float s)
        {
            var pw = s + 20f;
            var plate = Kit(parent, name, "kit_iconbtn_round_normal", cx - pw / 2f, cy - pw / 2f, pw, pw);
            return Kit(plate.rectTransform, "Icon", icon, 10, 8, s, s);
        }

        private static Button Swap(Button b, Image face, string normal, string hover, string pressed, string disabled)
        {
            b.targetGraphic = face;
            b.transition = Selectable.Transition.SpriteSwap;
            b.spriteState = new SpriteState
            {
                highlightedSprite = UiKit.Sprite(hover),
                selectedSprite = UiKit.Sprite(normal),
                pressedSprite = UiKit.Sprite(pressed),
                disabledSprite = UiKit.Sprite(disabled),
            };
            return b;
        }

        /// <summary>Square or round icon button: kit_iconbtn_* in 4 states, ink icon 62 % of the side, 3 px up (mock iconsq()).</summary>
        private static Button IconButton(RectTransform parent, string name, string family, string icon, float size)
        {
            var face = Kit(parent, name, family + "_normal", 0, 0, size, size);
            face.raycastTarget = true;
            var b = Swap(face.gameObject.AddComponent<Button>(), face, family + "_normal", family + "_hover", family + "_pressed", family + "_disabled");
            var k = Mathf.Round(size * 0.62f);
            Kit(face.rectTransform, "Icon", icon, (size - k) / 2f, (size - k) / 2f - 3f, k, k);
            return b;
        }

        /// <summary>Close (kit_close 52, red X 26 on the face) at the popup's top right (mock close_btn()).</summary>
        private static Button CloseButton(RectTransform popup, float popupWidth)
        {
            var face = Kit(popup, "Close", "kit_close_normal", popupWidth - 86f, 29, 52, 52);
            face.raycastTarget = true;
            var b = Swap(face.gameObject.AddComponent<Button>(), face, "kit_close_normal", "kit_close_hover", "kit_close_pressed", "kit_close_disabled");
            Kit(face.rectTransform, "Icon", "kit_icon_cross_red", 13, 11, 26, 26);
            return b;
        }

        private static RectTransform HRow(RectTransform parent, string name, float x, float y, float w, float h, float spacingPx)
        {
            var r = UiKit.Place(UiKit.Node(parent, name), x, y, w, h);
            Layout(r.gameObject.AddComponent<HorizontalLayoutGroup>(), spacingPx);
            return r;
        }

        private static void Layout(HorizontalLayoutGroup g, float spacingPx)
        {
            g.childAlignment = TextAnchor.MiddleCenter;
            g.spacing = UiKit.Px(spacingPx);
            g.childControlWidth = g.childControlHeight = true;
            g.childForceExpandWidth = g.childForceExpandHeight = false;
        }

        private static void Size(Component c, float wPx, float hPx)
        {
            var le = c.gameObject.AddComponent<LayoutElement>();
            le.minWidth = le.preferredWidth = UiKit.Px(wPx);
            le.minHeight = le.preferredHeight = UiKit.Px(hPx);
        }

        // ---------- killcam helpers (canvas units, y down from the parent's top-left) ----------

        internal static RectTransform Rect(string name, Transform parent, float x, float y, float w, float h)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var r = (RectTransform)go.transform;
            r.SetParent(parent, false);
            r.anchorMin = r.anchorMax = new Vector2(0, 1);
            r.pivot = new Vector2(0, 1);
            r.anchoredPosition = new Vector2(x, -y);
            r.sizeDelta = new Vector2(w, h);
            return r;
        }

        internal static RectTransform Centered(string name, Transform parent, float w, float h)
        {
            var r = Rect(name, parent, 0, 0, w, h);
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = Vector2.zero;
            return r;
        }

        internal static void Stretch(RectTransform r)
        {
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;
        }

        internal static CanvasGroup Group(RectTransform r)
        {
            var g = r.gameObject.AddComponent<CanvasGroup>();
            g.alpha = 0f;
            g.interactable = false;
            g.blocksRaycasts = false;
            return g;
        }

        internal static Image Img(RectTransform r, Color color, bool raycast = false)
        {
            var i = r.gameObject.AddComponent<Image>();
            i.color = color;
            i.raycastTarget = raycast;
            return i;
        }

        internal static TMP_Text Txt(string name, Transform parent, float x, float y, float w, float h, string text, float size,
            TMP_FontAsset font, TextAlignmentOptions align, Color color)
        {
            var r = Rect(name, parent, x, y, w, h);
            var t = r.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = font;
            t.fontSize = size;
            t.enableAutoSizing = false;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Ellipsis;
            t.raycastTarget = false;
            t.text = text;
            t.alignment = align;
            t.color = color;
            return t;
        }

        internal static Button Btn(string name, Transform parent, float x, float y, float w, float h, string label, Sprite face, Color fg)
        {
            var r = Rect(name, parent, x, y, w, h);
            var image = Img(r, Color.white, true);
            Skin(image, face, 2f); // 44–50 px tall, 32 px border
            var b = r.gameObject.AddComponent<Button>();
            b.targetGraphic = image;
            var text = Txt("Label", r, 0, 0, w, h, label, TextSize, TextFont, TextAlignmentOptions.Center, fg);
            text.fontStyle = FontStyles.Bold;
            return b;
        }

        // ---------- killcam art (asset_mapping.json) ----------

        private static Sprite Secondary => Load(MenuArt + "ButtonSecondary.png");
        private static Sprite Overlay(string name) => Load(OverlayArt + name + ".png");
        private static Sprite Hud(string name) => Load(HudArt + name + ".png");

        private static Sprite Load(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) Debug.LogError("[UI-03] Missing sprite " + path);
            return sprite;
        }

        /// <summary>Sliced sprite on an existing Image; the multiplier shrinks the border to fit short rects.</summary>
        private static Image Skin(Image image, Sprite sprite, float pixelsPerUnitMultiplier)
        {
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.fillCenter = true;
            image.pixelsPerUnitMultiplier = pixelsPerUnitMultiplier;
            return image;
        }

        /// <summary>A stretched, non-raycast overlay child over <paramref name="parent"/>'s own Image (outline, mark).</summary>
        private static Image Layer(RectTransform parent, string name, Sprite sprite, Color color, float pixelsPerUnitMultiplier)
        {
            var r = Rect(name, parent, 0, 0, 0, 0);
            Stretch(r);
            return Skin(Img(r, color), sprite, pixelsPerUnitMultiplier);
        }

        internal static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetBools(Object target, string field, bool[] values)
        {
            var so = new SerializedObject(target);
            var array = so.FindProperty(field);
            array.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).boolValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void SetArray(Object target, string field, Object[] values)
        {
            var so = new SerializedObject(target);
            var array = so.FindProperty(field);
            array.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
