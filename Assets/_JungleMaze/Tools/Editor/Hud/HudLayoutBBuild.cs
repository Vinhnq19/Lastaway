using System;
using System.IO;
using JungleMaze.Presentation.UI.Hud;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace JungleMaze.EditorTools
{
    /// <summary>
    /// T-013: applies HUD layout B (projects/lastaway/ux/hud-B-spec.md, chosen by Huy 06/11/2026) to Hud.prefab.
    /// Units are reference units of the 1280×720 canvas. Children are placed by their top-left corner inside the block,
    /// y downwards, as in the spec tables. Idempotent: running it again gives the same prefab.
    /// T-016 r11 (Huy 07/11): block rects and content follow art/inbox/T-015/asset-list.md section 3, measured on the
    /// 1920×1080 concept (1080p px; Px() divides by 1.5), replacing the HUD B numbers; no weapon/ammo tab.
    /// <see cref="HudSkinABuild"/> then puts the carved-stone sprites on.
    /// </summary>
    public static class HudLayoutBBuild
    {
        private const string PrefabPath = "Assets/_JungleMaze/Prefabs/UI/Hud.prefab";
        public const string IconFolder = "Assets/_JungleMaze/Art/Hud/StatusIcons";
        /// <summary>
        /// Where art is read from. Default is the repo inbox; point it at a git-extracted copy (git archive of a commit)
        /// to import the exact art version qa passed while art-ux keeps editing the inbox.
        /// </summary>
        public static string ArtInbox = "projects/lastaway/art/inbox";
        private static string IconSource => ArtInbox + "/hud-icons/png";
        private static readonly string[] IconNames = { "hp", "armor", "helmet", "adrenaline", "alive", "zone-timer", "kill", "helmet-alt" };

        [MenuItem("Jungle Maze/HUD/Apply layout B + skin A (T-013, T-016)")]
        public static void Apply()
        {
            ImportIcons();
            HudSkinABuild.ImportSprites();
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                Vitals(root.transform.Find("Vitals"));
                QuickBar(root.transform.Find("QuickBar"));
                Minimap(root.transform.Find("Minimap"));
                MatchStatus(root.transform.Find("MatchStatus"));
                // T-022: feedback nodes (effects row, use strip) are placed by HudFeedbackPrefabBuild from mock B9.
                TypeScale(root.transform);
                HudSkinABuild.Apply(root.transform);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            Debug.Log("[HUD B] Hud.prefab updated (T-013 layout, T-016 skin A).");
        }

        /// <summary>Copies the @2x PNGs (baked colours) into the project; Bilinear because the art is vector, not pixel.</summary>
        public static void ImportIcons()
        {
            Directory.CreateDirectory(IconFolder);
            foreach (var name in IconNames)
            {
                var src = Path.Combine(IconSource, "icon_" + name + "@2x.png");
                var dst = IconFolder + "/icon_" + name + ".png";
                if (!File.Exists(src)) throw new FileNotFoundException("HUD B icon missing", src);
                File.Copy(src, dst, true);
                AssetDatabase.ImportAsset(dst, ImportAssetOptions.ForceUpdate);
                var importer = (TextureImporter)AssetImporter.GetAtPath(dst);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
        }

        private static Sprite Icon(string name) =>
            AssetDatabase.LoadAssetAtPath<Sprite>(IconFolder + "/icon_" + name + ".png") is Sprite s && s != null
                ? s : throw new InvalidOperationException("Icon not imported: " + name);

        // ---- Vitals 422×206 px at (30, 857) of 1920×1080 (T-015 r11 asset-list section 3) ----
        private static void Vitals(Transform vitals)
        {
            Rect(vitals, new Vector2(Px(30), Px(1080 - 857 - 206)), new Vector2(Px(422), Px(206)));
            var view = vitals.GetComponent<VitalsView>();
            AddIconAt(vitals, "HpIcon", "hp", 64, 42);
            // Bar rect = fill area, 6 px inside the 224×32 px stone track at (91, 26) that HudSkinABuild draws.
            Place(vitals.Find("HpBar"), Px(97), Px(32), Px(212), Px(20));
            var hpValue = vitals.Find("HpValue");
            Place(hpValue, Px(290), Px(41 - 22.5f), Px(90), Px(45)); // right edge x 380, centre y 41
            var layout = hpValue.GetComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleRight;
            Tier(view.HpNumber, UiKit.Tier.B1, TextAlignmentOptions.MidlineRight);
            view.HpMax.gameObject.SetActive(false);

            Row(vitals, view.ArmorRow, "ArmorIcon", "armor", 65, 100, 103, 101, 70, TextAlignmentOptions.MidlineLeft, false);
            Row(vitals, view.HelmetRow, "HelmetIcon", "helmet", 245, 100, 280, 101, 70, TextAlignmentOptions.MidlineLeft, false);
            Row(vitals, view.AdrenalineRow, "AdrenalineIcon", "adrenaline", 65, 158, 336, 158, 60, TextAlignmentOptions.Midline, true);
            // Thin track 242×24 px at (95, 146); fill area 5 px inside.
            Place(view.AdrenalineRow.Bar.transform, Px(100), Px(151), Px(232), Px(14));
        }

        /// <summary>
        /// The row becomes a transparent container over the whole block so children use block coordinates. Icon centre
        /// (icx, icy) and number box (left vx, centre y vcy, width vw) in 1080p px; numbers are 30 px (T-022 type scale).
        /// </summary>
        private static void Row(Transform block, HudGaugeRowView row, string iconName, string sprite, float icx, float icy,
            float vx, float vcy, float vw, TextAlignmentOptions align, bool keepBar)
        {
            var rt = (RectTransform)row.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = ((RectTransform)block).sizeDelta;
            row.Label.gameObject.SetActive(false);
            row.Bar.gameObject.SetActive(keepBar);
            var icon = AddIconAt(row.transform, iconName, sprite, icx, icy);
            Place(row.Value.transform, Px(vx), Px(vcy - 18), Px(vw), Px(36));
            Tier(row.Value, UiKit.Tier.B1, align);
            var so = new SerializedObject(row);
            so.FindProperty("icon").objectReferenceValue = icon;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---- Quick bar 593×112 px at (668, 950) of 1920×1080, no weapon/ammo tab (Huy 07/11) ----
        private static void QuickBar(Transform bar)
        {
            Rect(bar, new Vector2(Px(668 + 593 / 2f - 960), Px(1080 - 950 - 112)), new Vector2(Px(593), Px(112)));
            var view = bar.GetComponent<QuickbarView>();
            var strip = bar.Find("WeaponStrip") ?? bar.Find("StripArea/WeaponStrip");

            // One Surface panel for the whole bar (mock B); the strip keeps its components but draws no own frame.
            var panel = bar.GetComponent<Image>();
            if (panel == null) panel = bar.gameObject.AddComponent<Image>();
            var stripImage = strip.GetComponent<Image>();
            panel.sprite = stripImage.sprite;
            panel.type = stripImage.type;
            panel.pixelsPerUnitMultiplier = stripImage.pixelsPerUnitMultiplier;
            panel.color = stripImage.color;
            panel.raycastTarget = false;
            stripImage.enabled = false;
            if (bar.Find("ArtEdge") == null)
                foreach (var name in new[] { "ArtBevel", "ArtEdge" })
                {
                    var copy = UnityEngine.Object.Instantiate(strip.Find(name).gameObject, bar, false);
                    copy.name = name;
                    copy.transform.SetSiblingIndex(0);
                }
            foreach (var name in new[] { "ArtEdge", "ArtBevel" }) strip.Find(name).gameObject.SetActive(false);
            var frame = bar.GetComponent<HudArtFrameView>();
            if (frame == null) frame = bar.gameObject.AddComponent<HudArtFrameView>();
            var frameSo = new SerializedObject(frame);
            frameSo.FindProperty("edge").objectReferenceValue = bar.Find("ArtEdge").GetComponent<Image>();
            frameSo.FindProperty("bevel").objectReferenceValue = bar.Find("ArtBevel").GetComponent<Image>();
            frameSo.ApplyModifiedPropertiesWithoutUndo();
            var viewSo = new SerializedObject(view);
            viewSo.FindProperty("panelBackground").objectReferenceValue = panel;
            viewSo.ApplyModifiedPropertiesWithoutUndo();
            RegisterFrame(bar.root, frame, strip.GetComponent<HudArtFrameView>());

            // Weapon strip: fixed 375×22, name left, ammo middle, reload key right.
            // The strip stretches inside a fixed 375×22 area. Scenes still carry an old layout-driven override
            // "WeaponStrip m_SizeDelta.x = 0" (Game.unity, WorldDemo.unity); with stretch anchors 0 is the right value,
            // so those scenes need no edit. The fitter goes off first, otherwise it snaps the width back.
            strip.GetComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            var area = bar.Find("StripArea");
            if (area == null)
            {
                area = new GameObject("StripArea", typeof(RectTransform)).transform;
                area.SetParent(bar, false);
                area.SetSiblingIndex(strip.GetSiblingIndex());
                strip.SetParent(area, false);
            }
            Place(area, Px(40), Px(18), Px(512), Px(22));
            var stripRt = (RectTransform)strip;
            stripRt.anchorMin = Vector2.zero;
            stripRt.anchorMax = Vector2.one;
            stripRt.pivot = new Vector2(0.5f, 0.5f);
            stripRt.anchoredPosition = Vector2.zero;
            stripRt.sizeDelta = Vector2.zero;
            var stripLayout = strip.GetComponent<HorizontalLayoutGroup>();
            stripLayout.padding = new RectOffset(0, 0, 0, 0);
            stripLayout.childAlignment = TextAnchor.MiddleCenter;
            stripLayout.childForceExpandWidth = true;
            // Hidden in r11; still put on the type scale (gameplay floor 24, key 20).
            Tier(view.WeaponName, UiKit.Tier.B2, TextAlignmentOptions.MidlineLeft);
            Tier(strip.Find("Ammo/Magazine").GetComponent<TMP_Text>(), UiKit.Tier.B2, null);
            Tier(strip.Find("Ammo/Reserve").GetComponent<TMP_Text>(), UiKit.Tier.B2, null);
            strip.Find("Ammo").GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;
            var reload = strip.Find("ReloadHint");
            reload.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleRight;
            var keyCap = reload.Find("KeyCap");
            var keyLayout = keyCap.GetComponent<LayoutElement>();
            keyLayout.preferredWidth = keyLayout.preferredHeight = Px(22); // fits the 22 px strip
            Tier(keyCap.Find("Label").GetComponent<TMP_Text>(), UiKit.Tier.C, null);
            Tier(reload.Find("Label").GetComponent<TMP_Text>(), UiKit.Tier.B2, null);
            // r11 (Huy 07/11): no weapon name / ammo tab. QuickbarView still writes to it, so it stays in the prefab, hidden.
            area.gameObject.SetActive(false);

            // Slots: two groups of 4 × 54 px, step 60, groups at x 50 and 310, row centred at y 29.
            Place(bar.Find("Slots"), Px(50), Px(29), Px(494), Px(54));
            Group(bar.Find("Slots/WeaponGroup"), 0);
            Group(bar.Find("Slots/ConsumableGroup"), Px(260));
        }

        private static void Group(Transform group, float x)
        {
            Place(group, x, 0, Px(234), Px(54));
            group.Find("Label").gameObject.SetActive(false);
            for (var i = 0; i < 4; i++) Slot(group.GetChild(i + 1), Px(i * 60f));
        }

        /// <summary>
        /// Slot 54 px: item icon up to 40 px centred; key number 20 px centred on the 30x30 key badge at (31, 27), the
        /// slot's bottom-right corner (mock B9: badge centres x 764.., y 1021; badge sprite in HudSkinABuild); count 20 px
        /// top-right; name hidden.
        /// </summary>
        private static void Slot(Transform slot, float x)
        {
            var size = Px(54);
            Place(slot, x, 0, size, size);
            Place(slot.Find("Icon"), Px(7), Px(7), Px(40), Px(40));
            UiKit.CentrePivot((RectTransform)slot.Find("Icon"));
            Place(slot.Find("Key"), Px(KeyBadge.x), Px(KeyBadge.y), Px(KeyBadge.width), Px(KeyBadge.height));
            Tier(slot.Find("Key").GetComponent<TMP_Text>(), UiKit.Tier.C, TextAlignmentOptions.Center);
            // A 20 px line is ~18 u tall; the 28 px box keeps TMP's ellipsis mode from dropping it.
            Place(slot.Find("Count"), Px(12), 0, Px(40), Px(28));
            Tier(slot.Find("Count").GetComponent<TMP_Text>(), UiKit.Tier.C, TextAlignmentOptions.TopRight);
            slot.Find("Name").gameObject.SetActive(false);
            var inUse = (RectTransform)slot.Find("InUseFrame");
            inUse.sizeDelta = new Vector2(size, size);
        }

        // ---- Minimap 318×316 px at (1577, 30); the map fills the frame's hole exactly ----
        private static void Minimap(Transform minimap)
        {
            Rect(minimap, new Vector2(-Px(1920 - 1577 - 318), -Px(30)), new Vector2(Px(318), Px(316)));
            var hole = HudSkinABuild.MinimapHole(); // px inside the frame sprite, y down
            var view = minimap.GetComponent<MinimapView>();
            var so = new SerializedObject(view);
            so.FindProperty("sizePixels").floatValue = Px(hole.width);
            so.ApplyModifiedPropertiesWithoutUndo();
            var area = (RectTransform)minimap.Find("MapArea");
            area.anchorMin = Vector2.zero;
            area.anchorMax = Vector2.one;
            area.offsetMin = new Vector2(Px(hole.x), Px(316 - hole.yMax));
            area.offsetMax = new Vector2(-Px(318 - hole.xMax), -Px(hole.y));
            for (var i = 1; i <= 4; i++)
            {
                ((RectTransform)area.Find("Grid/V" + i)).anchoredPosition = new Vector2(Px(hole.width) / 5 * i, 0);
                ((RectTransform)area.Find("Grid/H" + i)).anchoredPosition = new Vector2(0, Px(hole.height) / 5 * i);
            }
            var marker = (RectTransform)area.Find("Marker");
            marker.sizeDelta = new Vector2(14, 14);
            ((RectTransform)marker.Find("Heading")).sizeDelta = new Vector2(14, 14);
            var needle = (RectTransform)marker.Find("Heading/Needle");
            needle.anchoredPosition = new Vector2(0, 10);
            needle.sizeDelta = new Vector2(2, 5);

            var map = area.Find("Map");
            if (map == null)
            {
                map = new GameObject("Map", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage)).transform;
                map.SetParent(area, false);
            }
            map.SetSiblingIndex(0);
            var mapRt = (RectTransform)map;
            mapRt.anchorMin = Vector2.zero;
            mapRt.anchorMax = Vector2.one;
            mapRt.pivot = new Vector2(0.5f, 0.5f);
            mapRt.anchoredPosition = Vector2.zero;
            mapRt.sizeDelta = Vector2.zero;
            var raw = map.GetComponent<RawImage>();
            raw.raycastTarget = false;
            raw.color = Color.white;
            raw.enabled = false;
            var capture = minimap.GetComponent<MinimapMapCapture>();
            if (capture == null) capture = minimap.gameObject.AddComponent<MinimapMapCapture>();
            var captureSo = new SerializedObject(capture);
            captureSo.FindProperty("mapImage").objectReferenceValue = raw;
            captureSo.FindProperty("resolution").intValue = 1024; // T-018: shared with the big map (840 px / 50 cells)
            captureSo.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---- Match status 318×90 px at (1577, 350): alive left, divider at x 162, zone timer right ----
        // T-022 type scale: both numbers 24 px. Widths (Be Vietnam Pro Bold 24, PIL advance): "10/10" 67 px, "00:48"
        // 75 px. Alive icon centre 71 -> 66 and value box 90..160 (70 px), divider 155 -> 162, zone icon 185 -> 188 and
        // value box 212..290 (78 px), still left of the frame corner block (x 292). Panel rect unchanged.
        private static void MatchStatus(Transform status)
        {
            Rect(status, new Vector2(-Px(1920 - 1577 - 318), -Px(350)), new Vector2(Px(318), Px(90)));
            foreach (var rowName in new[] { "AliveRow", "ZoneRow" })
            {
                var row = (RectTransform)status.Find(rowName);
                row.anchorMin = row.anchorMax = row.pivot = new Vector2(0, 1);
                row.anchoredPosition = Vector2.zero;
                row.sizeDelta = new Vector2(Px(318), Px(90));
                row.Find("Label").gameObject.SetActive(false);
            }
            AddIconAt(status.Find("AliveRow"), "AliveIcon", "alive", 66, 45);
            Place(status.Find("AliveRow/Value"), Px(90), Px(45 - 16), Px(70), Px(32));
            var alive = status.Find("AliveRow/Value").GetComponent<TMP_Text>();
            Tier(alive, UiKit.Tier.B2, TextAlignmentOptions.MidlineLeft);
            // The divider is not in the panel sprite; colour sampled from the approved mock dung-thu-hud-A.png.
            var divider = status.Find("Divider");
            if (divider == null)
            {
                divider = new GameObject("Divider", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).transform;
                divider.SetParent(status, false);
            }
            Place(divider, Px(162), Px(28), Px(2), Px(34));
            var line = divider.GetComponent<Image>();
            line.color = new Color32(0xBE, 0xB4, 0x96, 0xFF);
            line.raycastTarget = false;
            AddIconAt(status.Find("ZoneRow"), "ZoneIcon", "zone-timer", 188, 45);
            Place(status.Find("ZoneRow/Value"), Px(212), Px(45 - 16), Px(78), Px(32));
            Tier(status.Find("ZoneRow/Value").GetComponent<TMP_Text>(), UiKit.Tier.B2, TextAlignmentOptions.MidlineLeft);
        }

        // ---- helpers ----
        /// <summary>1080p pixels (skin A asset-list) to reference units of the 1280×720 canvas.</summary>
        private static float Px(float px) => px / 1.5f;

        /// <summary>Quick-bar key badge in a 54 px slot, 1080p px (x, y from the slot's top-left).</summary>
        public static readonly UnityEngine.Rect KeyBadge = new UnityEngine.Rect(31, 27, 30, 30);

        /// <summary>
        /// T-022: every text of the four core blocks onto the T-027 scale, gameplay floor 24 (UiKit, Be Vietnam Pro Bold).
        /// Slot key and count numbers and the reload key cap get 20 (numbers on a slot or key). Feedback texts are left alone.
        /// </summary>
        private static void TypeScale(Transform hud)
        {
            foreach (var block in new[] { "Vitals", "Minimap", "MatchStatus" }) UiKit.SnapAll(hud.Find(block).gameObject, 24f);
            var bar = hud.Find("QuickBar");
            var use = bar.Find("UseActionStrip");
            foreach (var text in bar.GetComponentsInChildren<TMP_Text>(true))
                if (use == null || !text.transform.IsChildOf(use)) UiKit.ApplyTier(text, UiKit.Snap(text.fontSize * UiKit.Scale, 24f));
            foreach (var slot in bar.GetComponentsInChildren<QuickbarSlotView>(true))
            {
                UiKit.ApplyTier(slot.KeyLabel, UiKit.Tier.C);
                UiKit.ApplyTier(slot.CountLabel, UiKit.Tier.C);
            }
            var key = bar.Find("StripArea/WeaponStrip/ReloadHint/KeyCap/Label");
            if (key != null) UiKit.ApplyTier(key.GetComponent<TMP_Text>(), UiKit.Tier.C);
        }

        private static void Tier(TMP_Text text, UiKit.Tier tier, TextAlignmentOptions? align)
        {
            UiKit.ApplyTier(text, tier);
            if (align.HasValue) text.alignment = align.Value;
        }

        /// <summary>Icon centred at (cx, cy) 1080p px; the box is the icon's own canvas (@2x / 2), so art keeps its size.</summary>
        private static Image AddIconAt(Transform parent, string name, string sprite, float cx, float cy)
        {
            var canvas = Icon(sprite).rect.width / 2f;
            return AddIcon(parent, name, sprite, Px(cx - canvas / 2f), Px(cy - canvas / 2f), Px(canvas));
        }

        private static void Rect(Transform t, Vector2 position, Vector2? size = null)
        {
            var rt = (RectTransform)t;
            rt.anchoredPosition = position;
            if (size.HasValue) rt.sizeDelta = size.Value;
        }

        /// <summary>Top-left anchored rect at (x, y) from the parent's top-left corner, y downwards.</summary>
        private static void Place(Transform t, float x, float y, float w, float h)
        {
            var rt = (RectTransform)t;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
        }

        private static Image AddIcon(Transform parent, string name, string sprite, float x, float y, float size)
        {
            var t = parent.Find(name);
            if (t == null)
            {
                t = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).transform;
                t.SetParent(parent, false);
            }
            Place(t, x, y, size, size);
            var image = t.GetComponent<Image>();
            image.sprite = Icon(sprite);
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.color = Color.white; // colour is baked into the PNG
            return image;
        }

        /// <summary>The quick-bar panel frame takes the weapon strip's slot in HudRoot.artFrames: still one frame per block.</summary>
        private static void RegisterFrame(Transform hudRoot, HudArtFrameView frame, HudArtFrameView replaces)
        {
            var root = hudRoot.GetComponent<HudRoot>();
            var so = new SerializedObject(root);
            var frames = so.FindProperty("artFrames");
            var replaced = false;
            for (var i = frames.arraySize - 1; i >= 0; i--)
            {
                var element = frames.GetArrayElementAtIndex(i);
                if (element.objectReferenceValue == replaces || element.objectReferenceValue == frame)
                {
                    if (replaced) frames.DeleteArrayElementAtIndex(i);
                    else { element.objectReferenceValue = frame; replaced = true; }
                }
            }
            if (!replaced)
            {
                frames.arraySize++;
                frames.GetArrayElementAtIndex(frames.arraySize - 1).objectReferenceValue = frame;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
