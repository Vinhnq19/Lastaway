using System;
using JungleMaze.Presentation.UI.Hud;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Tier = JungleMaze.EditorTools.UiKit.Tier;

namespace JungleMaze.EditorTools
{
    /// <summary>
    /// T-022: B4 bag (HudInventory.prefab) and B5 big map (HudBigMap.prefab), skin A kit, nested in Hud.prefab as the last
    /// children so they draw over the HUD and receive [Inject] with it. Coordinates are 1080p px from mocks
    /// T-021/b/b4-tui-do*.png and b5-ban-do-lon.png and the T-028 pack tables (2-hud-balo-ban-do/*.md); placed relative to
    /// each panel's top-left with UiKit.Place. Both start closed; the views toggle on Tab/I and M. Run after
    /// "Build Feedback Layer". Idempotent.
    /// </summary>
    public static class HudPanelsBuild
    {
        private const string Ui = "Assets/_JungleMaze/Prefabs/UI/";
        private const string HudPath = Ui + "Hud.prefab";
        private const string Icons = "Assets/_JungleMaze/Art/UI/Icons/";
        private const string ZoneIcon = "Assets/_JungleMaze/Art/Hud/StatusIcons/icon_zone-timer.png";
        /// <summary>Zone ring stroke on the big map, 1080p px (mock b5: orange and white rings both 4 px).</summary>
        public const float BigMapRingPx = 4f;
        public const float BigMapMargin = 60f; // map square inset in the 1000 px big-map panel, same on all four sides

        /// <summary>
        /// Outside-the-zone fill of the main ZoneOverlay on the minimap, read from Resources/ZoneOverlay.shader (map team,
        /// read only): rgb (0.48, 0.035, 0.12) at _FillAlpha 0.62.
        /// </summary>
        public static readonly Color OutsideZoneColor = new Color(0.48f, 0.035f, 0.12f, 0.62f);
        private const string MouseIcon = "Assets/_JungleMaze/Art/UI/MatchOverlay/IconMouse.png";
        public const string InventoryName = "HudInventory";
        public const string BigMapName = "HudBigMap";

        [MenuItem("Jungle Maze/HUD/Build Bag + Big Map (T-022)")]
        public static void Build()
        {
            BuildInventory();
            BuildBigMap();
            var hud = PrefabUtility.LoadPrefabContents(HudPath);
            try
            {
                var root = (RectTransform)hud.transform;
                foreach (var name in new[] { InventoryName, BigMapName })
                {
                    var old = root.Find(name);
                    if (old != null) Object.DestroyImmediate(old.gameObject);
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>(Ui + name + ".prefab"), root);
                    UiKit.Stretch((RectTransform)instance.transform).SetAsLastSibling();
                }
                var map = root.Find(BigMapName).GetComponent<HudBigMapView>();
                Set(map, "capture", hud.GetComponent<HudRoot>().Minimap.GetComponent<MinimapMapCapture>());
                Set(map.ZoneOverlay, "minimap", hud.GetComponent<HudRoot>().Minimap); // crop ratio of the main minimap layer
                Waypoint(hud, map);
                foreach (var graphic in hud.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false; // HUD never catches clicks
                UiKit.FaceAll(hud); // pressed K2 face sinks icon + label (Huy 07/11)
                PrefabUtility.SaveAsPrefabAsset(hud, HudPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(hud);
            }
            AssetDatabase.SaveAssets();
        }

        // ---------- T-018 D1 waypoint ----------

        /// <summary>
        /// <see cref="WaypointView"/> on the HUD root: big-map flag and line (built in the big map), minimap flag (18 px,
        /// ux/hud-B-spec.md 5) and line (alpha 0.6), and a full-screen layer under the panels for the ground flag (40 px)
        /// and the edge arrow (40 px) with its distance badge (kit_badge_key 64x36, 24 px number). Sizes are set at run
        /// time from WaypointTuning; everything starts hidden.
        /// </summary>
        private static void Waypoint(GameObject hud, HudBigMapView map)
        {
            var root = (RectTransform)hud.transform;
            var core = hud.GetComponent<HudRoot>();
            var view = hud.GetComponent<WaypointView>() ?? hud.AddComponent<WaypointView>();
            RectTransform marks = null;
            foreach (var t in map.GetComponentsInChildren<RectTransform>(true)) if (t.name == "Marks") marks = t;

            var area = core.Minimap.Area;
            foreach (var n in new[] { "WaypointLine", "WaypointFlag" }) { var old = area.Find(n); if (old != null) Object.DestroyImmediate(old.gameObject); }
            var miniLine = WaypointLine(area, new Color32(0xBD, 0xE6, 0xF8, 153));
            var miniFlag = MapMark(area, "WaypointFlag", "wp_flag_mini", 18, 18);
            var marker = core.Minimap.Marker.GetSiblingIndex(); // the hero marker stays on top
            miniLine.SetSiblingIndex(marker);
            miniFlag.SetSiblingIndex(marker + 1);

            var oldEdge = root.Find("Waypoint");
            if (oldEdge != null) Object.DestroyImmediate(oldEdge.gameObject);
            var edge = UiKit.Stretch(UiKit.Node(root, "Waypoint"));
            edge.SetSiblingIndex(root.Find(InventoryName).GetSiblingIndex()); // under bag and big map
            var ground = Centre(edge, "GroundFlag", "wp_flag", 40, 40);
            ground.pivot = new Vector2(0.5f, 0f); // stands on its world point
            var arrow = Centre(edge, "Arrow", "wp_arrow_edge", 40, 40);
            var badge = Centre(edge, "Badge", "kit_badge_key", 64, 36);
            var text = UiKit.Text(badge, "Distance", Tier.B2, "33", new Color32(0xF1, 0xE7, 0xC8, 255), TextAlignmentOptions.Center);
            UiKit.Stretch(text.rectTransform);
            foreach (var t in new[] { ground, arrow, badge }) t.gameObject.SetActive(false);

            Set(view, "bigMap", map);
            Set(view, "bigArea", marks);
            Set(view, "bigFlag", marks.Find("WaypointFlag"));
            Set(view, "bigLine", marks.Find("WaypointLine"));
            Set(view, "minimap", core.Minimap);
            Set(view, "miniFlag", miniFlag);
            Set(view, "miniLine", miniLine);
            Set(view, "edge", edge);
            Set(view, "groundFlag", ground);
            Set(view, "arrow", arrow);
            Set(view, "badge", badge);
            Set(view, "distance", text);
            var so = new SerializedObject(view);
            var list = so.FindProperty("panels");
            var names = new[] { "Minimap", "MatchStatus", "Vitals", "QuickBar" };
            list.arraySize = names.Length;
            for (var i = 0; i < names.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = root.Find(names[i]);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Plain 1 px line image anchored at the map's bottom-left, pivot at its left end, hidden.</summary>
        private static RectTransform WaypointLine(RectTransform parent, Color color)
        {
            var t = UiKit.Node(parent, "WaypointLine");
            t.anchorMin = t.anchorMax = Vector2.zero;
            t.pivot = new Vector2(0f, 0.5f);
            var image = t.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            t.gameObject.SetActive(false);
            return t;
        }

        /// <summary>Kit sprite anchored at the parent's centre (the view moves it in that space), size in 1080p px.</summary>
        private static RectTransform Centre(RectTransform parent, string name, string sprite, float wPx, float hPx)
        {
            var t = UiKit.Node(parent, name);
            t.anchorMin = t.anchorMax = new Vector2(0.5f, 0.5f);
            t.sizeDelta = new Vector2(UiKit.Px(wPx), UiKit.Px(hPx));
            UiKit.Image(t, sprite);
            return t;
        }

        // ---------- B4 bag ----------

        private static void BuildInventory()
        {
            var root = Root(InventoryName);
            try
            {
                var view = root.gameObject.AddComponent<HudInventoryView>();
                var content = Content(root, 200f / 255f);
                var panel = UiKit.PlaceCentred(UiKit.Node(content, "Panel"), 130, 70, 1660, 860);
                UiKit.Panel(panel, UiKit.PanelKind.L);
                InkIcon(panel, "Bag", "kit_ico_bag", 235 - 130, 150 - 70, 44);
                KeyCap(panel, "Tab", 1640 - 130, 124 - 70, 70);
                UiKit.Image(UiKit.Place(UiKit.Node(panel, "Divider"), 190 - 130, 188 - 70, 1460, 14), "kit_divider");

                // Left column: items nearby / on the ground. No data source yet: header icon only (mock b4-tui-do-trong).
                var ground = SubPanel(panel, "Ground", 180, 215, 440, 590);
                InkIcon(ground, "Header", "icon_ground", 52, 70, 30);

                // Middle column: weapons 1-4 then consumables 5-8, cells 130x140 on a 150 step.
                var slots = SubPanel(panel, "Slots", 650, 215, 680, 590);
                Icon(slots, "WeaponHeader", Icons + "Weapons/CROSSBOW.png", 33, 48, 44, 44);
                Icon(slots, "ConsumableHeader", Icons + "Equipments/GOD_LEAF.png", 33, 268, 44, 44);
                var so = new SerializedObject(view);
                var cells = so.FindProperty("cells");
                cells.arraySize = 8;
                for (var i = 0; i < 8; i++) Cell(slots, i, cells.GetArrayElementAtIndex(i));
                so.ApplyModifiedPropertiesWithoutUndo();

                // Right column: helmet, armour, adrenaline, ammo (icon 72 + number 30 right-aligned at 305).
                var gear = SubPanel(panel, "Gear", 1360, 215, 390, 590);
                Icon(gear, "Header", Icons + "Equipments/ARMOR.png", 28, 48, 44, 44);
                Set(view, "helmetValue", GearRow(gear, 0, "Helmet", Load<Sprite>(Icons + "Equipments/WOODEN_HELMET.png"), "18"));
                Set(view, "armorValue", GearRow(gear, 1, "Armor", Load<Sprite>(Icons + "Equipments/ARMOR.png"), "48"));
                Set(view, "adrenalineValue", GearRow(gear, 2, "Adrenaline", Load<Sprite>(Icons + "Equipments/ADRENALINE.png"), "55"));
                Set(view, "ammoValue", GearRow(gear, 3, "Ammo", UiKit.Sprite("icon_ammo"), "4/90"));

                Tooltip(panel, view);
                Set(view, "content", content.gameObject);
                Set(view, "slotNormal", UiKit.Sprite("kit_slot_item_normal"));
                Set(view, "slotSelected", UiKit.Sprite("kit_slot_item_selected"));
                Set(view, "slotEmpty", UiKit.Sprite("kit_slot_item_empty"));
                SetFloat(view, "tooltipGap", UiKit.Px(8));
                Save(root, InventoryName);
            }
            finally
            {
                Object.DestroyImmediate(root.gameObject);
            }
        }

        /// <summary>Cell i of the bag grid (mock: x 55 + 150 i, y 100 or 320 in the middle column): number 20, icon 104, ×n.</summary>
        private static void Cell(RectTransform slots, int i, SerializedProperty cell)
        {
            var t = UiKit.Place(UiKit.Node(slots, "Cell" + (i + 1)), 55 + i % 4 * 150, i < 4 ? 100 : 320, 130, 140);
            var frame = UiKit.Image(t, "kit_slot_item_empty");
            var key = UiKit.Text(t, "Key", Tier.C, (i + 1).ToString(), UiKit.Colors.Muted);
            UiKit.Place(key.rectTransform, 16, 8, 30, 28);
            var icon = UiKit.Place(UiKit.Node(t, "Icon"), 13, 5, 104, 104).gameObject.AddComponent<Image>();
            icon.preserveAspect = true;
            icon.enabled = false;
            var count = UiKit.Text(t, "Count", Tier.C, "×1", UiKit.Colors.Text, TextAlignmentOptions.MidlineRight);
            UiKit.Place(count.rectTransform, 58, 8, 60, 28);
            count.enabled = false;
            cell.FindPropertyRelative("Root").objectReferenceValue = t;
            cell.FindPropertyRelative("Frame").objectReferenceValue = frame;
            cell.FindPropertyRelative("Icon").objectReferenceValue = icon;
            cell.FindPropertyRelative("Count").objectReferenceValue = count;
        }

        private static TMP_Text GearRow(RectTransform gear, int i, string name, Sprite icon, string sample)
        {
            var row = UiKit.Place(UiKit.Node(gear, name), 35, 105 + i * 98, 320, 88);
            UiKit.Image(row, "kit_row_normal");
            var image = UiKit.Place(UiKit.Node(row, "Icon"), 20, 8, 72, 72).gameObject.AddComponent<Image>();
            image.sprite = icon;
            image.preserveAspect = true;
            var value = UiKit.Text(row, "Value", Tier.B1, sample, UiKit.Colors.Text, TextAlignmentOptions.MidlineRight);
            UiKit.Place(value.rectTransform, 160, 0, 145, 88);
            return value;
        }

        /// <summary>
        /// kit_tip_item 390x190 (mock b4-tui-do-tooltip): icon 44, name 24 Gold, divider, kind 24. The source line ("Tiếp tế,
        /// rương gỗ, xác") has no runtime data and is left out. Anchored at the panel centre: the view moves it by
        /// localPosition and flips its background when it would leave the panel.
        /// </summary>
        private static void Tooltip(RectTransform panel, HudInventoryView view)
        {
            var tip = UiKit.Node(panel, "Tooltip");
            tip.anchorMin = tip.anchorMax = new Vector2(0.5f, 0.5f);
            tip.pivot = new Vector2(0f, 1f);
            tip.sizeDelta = new Vector2(UiKit.Px(390), UiKit.Px(190));
            var background = UiKit.Stretch(UiKit.Node(tip, "Background"));
            UiKit.Image(background, "kit_tip_item");
            var icon = UiKit.Place(UiKit.Node(tip, "Icon"), 28, 33, 44, 44).gameObject.AddComponent<Image>();
            icon.preserveAspect = true;
            var name = UiKit.Text(tip, "Name", Tier.B2, "Shotgun", UiKit.Colors.Gold);
            UiKit.Place(name.rectTransform, 90, 37, 280, 36);
            name.overflowMode = TextOverflowModes.Ellipsis;
            UiKit.Image(UiKit.Place(UiKit.Node(tip, "Divider"), 35, 87, 290, 14), "kit_divider");
            var kind = UiKit.Text(tip, "Kind", Tier.B2, HudInventoryView.WeaponKind, UiKit.Colors.Text);
            UiKit.Place(kind.rectTransform, 35, 103, 320, 36);
            tip.gameObject.SetActive(false);
            Set(view, "tooltip", tip);
            Set(view, "tooltipBackground", background);
            Set(view, "tooltipIcon", icon);
            Set(view, "tooltipName", name);
            Set(view, "tooltipKind", kind);
        }

        // ---------- B5 big map ----------

        private static void BuildBigMap()
        {
            var root = Root(BigMapName);
            try
            {
                var view = root.gameObject.AddComponent<HudBigMapView>();
                var content = Content(root, 215f / 255f);
                var panel = UiKit.PlaceCentred(UiKit.Node(content, "Panel"), 50, 40, 1000, 1000); // square, the map fills it (CPO 07/11 "BIGMAP FIT")
                UiKit.Panel(panel, UiKit.PanelKind.L);

                // Map 840 px square at (110, 100) (T-028 round 2): kit_map_bg frame, the minimap's map texture inside its
                // 5 px margin, map_grid (840, 10x10 tiled with the same margin) over it, then the zone rings (RingGraphic
                // from map_ring_zone orange / map_ring_safe white, 4 px stroke at any radius, mock b5) and the hero pin.
                var area = UiKit.Place(UiKit.Node(panel, "MapArea"), BigMapMargin, BigMapMargin, 1000 - 2 * BigMapMargin, 1000 - 2 * BigMapMargin); // 880 square, centred
                UiKit.Image(area, "kit_map_bg");
                var inner = UiKit.Stretch(UiKit.Node(area, "Inner"), 5);
                var map = Raw(inner, "Map");
                // Main-style zone layer (ZoneOverlay shader, minimap mode) over the map, under grid and pin (CPO 07/11).
                var overlay = Raw(inner, "ZoneOverlay").gameObject.AddComponent<HudZoneOverlay>();
                var grid = UiKit.Image(UiKit.Stretch(UiKit.Node(area, "Grid")), "map_grid");
                grid.preserveAspect = false;
                var marks = UiKit.Stretch(UiKit.Node(area, "Marks"), 5); // projection space = the map texture's rect
                var zoneRings = HudSkinABuild.ZoneRings(marks, BigMapRingPx);
                // T-018 D1 waypoint (W7): 1 px line hero to flag (alpha 0.5), flag 24 px; under the hero pin.
                WaypointLine(marks, new Color32(0xBD, 0xE6, 0xF8, 128));
                MapMark(marks, "WaypointFlag", "wp_flag", 24, 24);
                var player = MapMark(marks, "Player", "fx_map_player", 49, 66);

                // Header (mock b5; meaning from the pre-icon mock "Pha 3 · Bo thu sau 01:24", "cách bo tiếp theo 420"):
                // warn + zone phase (Gold), hourglass + countdown, pin + distance to the next safe zone. All 40 px.
                var header = SubPanel(panel, "Header", 1170, 40, 700, 150);
                Centred(header, "PhaseIcon", "kit_icon_warn", 1220 - 1170, 75, 50, 44);
                var phase = UiKit.Text(header, "PhaseValue", Tier.H2, "3", UiKit.Colors.Gold);
                UiKit.Place(phase.rectTransform, 1255 - 1170, 45, 80, 60);
                Icon(header, "ZoneIcon", ZoneIcon, 1370 - 1170 - 28, 75 - 28, 56, 56);
                var zone = UiKit.Text(header, "ZoneValue", Tier.H2, "01:24", UiKit.Colors.Text);
                UiKit.Place(zone.rectTransform, 1410 - 1170, 45, 180, 60);
                Centred(header, "DistanceIcon", "fx_map_player", 1610 - 1170, 75, 38, 52);
                var distance = UiKit.Text(header, "DistanceValue", Tier.H2, "420", UiKit.Colors.Text);
                UiKit.Place(distance.rectTransform, 1645 - 1170, 45, 100, 60);

                // Legend: icons only, the same shapes as on the map (mock y 275). Zone in the main style (Huy 07/11): a flat
                // "outside the zone" swatch in the ZoneOverlay minimap colour and the white dashed next ring, so 6 slots on a
                // 114 px step instead of the mock's 5 on 140.
                var legend = SubPanel(panel, "Legend", 1170, 210, 700, 130);
                Centred(legend, "Danger", "kit_icon_warn", 65, 65, 45, 40);
                // White base sprite so the tint shows the shader colour (kit_dim is dark and multiplied it to near black).
                var outside = UiKit.Place(UiKit.Node(legend, "OutsideZone"), 65 + 114 - 16, 65 - 16, 32, 32).gameObject.AddComponent<Image>();
                outside.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_JungleMaze/Art/Hud/SkinA/hud_bar_fill_white.png");
                outside.type = Image.Type.Simple;
                outside.color = new Color32(167, 55, 80, 255); // the outside-zone pink as it shows on the minimap (ZoneOverlay fill over terrain, measured 07/11)
                outside.raycastTarget = false;
                Centred(legend, "NextZone", "legend_ring_next_dashed", 65 + 228, 65, 32, 32); // T-028 dashed next ring
                Centred(legend, "Drop", "fx_map_drop", 65 + 342, 65, 41, 52);
                Centred(legend, "Mark", "wp_flag", 65 + 456, 65, 44, 44); // T-029 m3: the waypoint flag replaces the pin
                Centred(legend, "Player", "fx_map_player", 65 + 570, 65, 38, 52);

                // Controls, 4 rows as the mock: mouse → mark, mouse → zoom, mouse → pan, M → close. Only M works today
                // (mark, zoom, pan not built yet: noted in qa/T-022/index.md).
                var controls = SubPanel(panel, "Controls", 1170, 360, 700, 360);
                for (var i = 0; i < 4; i++)
                {
                    var y = 70 + i * 75;
                    var row = UiKit.Place(UiKit.Node(controls, "Row" + (i + 1)), 0, y - 37, 700, 75);
                    const float cy = 37;
                    if (i < 3) Icon(row, "Mouse", MouseIcon, 106 - 26, cy - 26, 52, 52);
                    else KeyCap(row, "M", 80, cy - 26, 52);
                    InkIcon(row, "Then", "kit_ico_arrow_right", 250, cy, 26);
                    if (i == 0) Centred(row, "Result", "wp_flag", 390, cy, 44, 44); // left click sets the waypoint (T-018)
                    if (i == 1) InkIcon(row, "Result", "kit_ico_plus", 390, cy, 30);
                    if (i == 2)
                    {
                        InkIcon(row, "Result", "kit_ico_arrow_left", 390, cy, 30);
                        InkIcon(row, "Result2", "kit_ico_arrow_right", 470, cy, 30);
                    }
                    if (i == 3) Centred(row, "Result", "kit_icon_cross_red", 390, cy, 36, 36);
                }

                Set(view, "content", content.gameObject);
                Set(view, "mapArea", marks);
                Set(view, "zoneRings", zoneRings);
                Set(view, "zoneOverlay", overlay);
                Set(view, "phaseValue", phase);
                Set(view, "distanceValue", distance);
                Set(view, "map", map);
                Set(view, "player", player);
                Set(view, "zoneValue", zone);
                Save(root, BigMapName);
            }
            finally
            {
                Object.DestroyImmediate(root.gameObject);
            }
        }

        // ---------- helpers ----------

        private static RectTransform Root(string name)
        {
            var t = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            return UiKit.Stretch(t);
        }

        /// <summary>Closed-by-default content: full-screen dim (pack: #0A0A08 at the mock alpha), no click catching.</summary>
        private static RectTransform Content(RectTransform root, float dimAlpha)
        {
            var content = UiKit.Stretch(UiKit.Node(root, "Content"));
            var dim = UiKit.Dim(content);
            dim.color = new Color(0x0A / 255f, 0x0A / 255f, 0x08 / 255f, dimAlpha);
            dim.raycastTarget = false; // the HUD canvas has no raycaster; the panels are display only
            content.gameObject.SetActive(false);
            return content;
        }

        /// <summary>kit_panel_s at absolute mock (x, y) inside a panel placed at its own absolute position.</summary>
        private static RectTransform SubPanel(RectTransform panel, string name, float x, float y, float w, float h)
        {
            var origin = PanelOrigin(panel);
            var t = UiKit.Place(UiKit.Node(panel, name), x - origin.x, y - origin.y, w, h);
            UiKit.Panel(t, UiKit.PanelKind.S);
            return t;
        }

        /// <summary>Top-left of a PlaceCentred panel back in mock px (its anchoredPosition is the centre offset).</summary>
        private static Vector2 PanelOrigin(RectTransform panel)
        {
            var size = panel.sizeDelta * UiKit.Scale;
            var centre = panel.anchoredPosition * UiKit.Scale;
            return new Vector2(centre.x + 960f - size.x / 2f, 540f - centre.y - size.y / 2f);
        }

        /// <summary>Dark ink kit icon on its khaki round plate (plate = icon + 20 px, icon 2 px up), centred at (cx, cy).</summary>
        private static void InkIcon(RectTransform parent, string name, string sprite, float cx, float cy, float size)
        {
            var plate = size + 20;
            var t = UiKit.Place(UiKit.Node(parent, name), cx - plate / 2, cy - plate / 2, plate, plate);
            UiKit.Image(t, "kit_iconbtn_round_normal");
            UiKit.Image(UiKit.Place(UiKit.Node(t, "Icon"), 10, 8, size, size), sprite);
        }

        private static void KeyCap(RectTransform parent, string key, float x, float y, float w)
        {
            var t = UiKit.Place(UiKit.Node(parent, "Key" + key), x, y, w, 52);
            UiKit.Image(t, "kit_keycap_normal");
            var label = UiKit.Text(t, "Label", Tier.B2, key, UiKit.Colors.Ink, TextAlignmentOptions.Center);
            UiKit.Place(label.rectTransform, 0, 0, w, 46); // the cap's face, above its 6 px base
        }

        private static void Centred(RectTransform parent, string name, string sprite, float cx, float cy, float w, float h) =>
            UiKit.Image(UiKit.Place(UiKit.Node(parent, name), cx - w / 2, cy - h / 2, w, h), sprite);

        private static void Icon(RectTransform parent, string name, string path, float x, float y, float w, float h)
        {
            var image = UiKit.Place(UiKit.Node(parent, name), x, y, w, h).gameObject.AddComponent<Image>();
            image.sprite = Load<Sprite>(path);
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        /// <summary>Sprite anchored at the map's bottom-left (the view moves it by projected position), hidden until data.</summary>
        private static RectTransform MapMark(RectTransform marks, string name, string sprite, float wPx, float hPx)
        {
            var t = UiKit.Node(marks, name);
            t.anchorMin = t.anchorMax = Vector2.zero;
            t.pivot = new Vector2(0.5f, 0.5f);
            t.sizeDelta = new Vector2(UiKit.Px(wPx), UiKit.Px(hPx));
            UiKit.Image(t, sprite).preserveAspect = false; // rings are sized to the diameter by the view
            t.gameObject.SetActive(false);
            return t;
        }

        private static RawImage Raw(RectTransform parent, string name)
        {
            var raw = UiKit.Stretch(UiKit.Node(parent, name)).gameObject.AddComponent<RawImage>();
            raw.raycastTarget = false;
            raw.enabled = false;
            return raw;
        }

        private static void Save(RectTransform root, string name) { UiKit.FaceAll(root.gameObject); PrefabUtility.SaveAsPrefabAsset(root.gameObject, Ui + name + ".prefab"); }

        private static T Load<T>(string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) throw new InvalidOperationException("Asset missing: " + path);
            return asset;
        }

        private static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field) ?? throw new InvalidOperationException(target.GetType().Name + " has no field " + field);
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(Object target, string field, float value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field) ?? throw new InvalidOperationException(target.GetType().Name + " has no field " + field);
            p.floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
