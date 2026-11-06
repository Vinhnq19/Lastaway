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
    /// Skin art (ArtEdge/ArtBevel, fonts) is kept; only rects, font sizes, visibility and the new icons change.
    /// </summary>
    public static class HudLayoutBBuild
    {
        private const string PrefabPath = "Assets/_JungleMaze/Prefabs/UI/Hud.prefab";
        public const string IconFolder = "Assets/_JungleMaze/Art/Hud/StatusIcons";
        private const string IconSource = "projects/lastaway/art/inbox/hud-icons/png";
        private static readonly string[] IconNames = { "hp", "armor", "helmet", "adrenaline", "alive", "zone-timer", "kill", "helmet-alt" };

        [MenuItem("Jungle Maze/HUD/Apply layout B (T-013)")]
        public static void Apply()
        {
            ImportIcons();
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                Vitals(root.transform.Find("Vitals"));
                QuickBar(root.transform.Find("QuickBar"));
                Minimap(root.transform.Find("Minimap"));
                MatchStatus(root.transform.Find("MatchStatus"));
                Rect(root.transform.Find("EffectsRow"), new Vector2(24, 147));
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            Debug.Log("[HUD B] Hud.prefab updated (T-013).");
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

        // ---- Vitals 207×115, padding 10 ----
        private static void Vitals(Transform vitals)
        {
            Rect(vitals, new Vector2(24, 24), new Vector2(207, 115));
            var view = vitals.GetComponent<VitalsView>();
            AddIcon(vitals, "HpIcon", "hp", 10, 10, 26);
            Place(vitals.Find("HpBar"), 42, 16, 109, 14);
            var hpValue = vitals.Find("HpValue");
            Place(hpValue, 157, 8, 40, 30);
            var layout = hpValue.GetComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleRight;
            Text(view.HpNumber, 16, TextAlignmentOptions.MidlineRight);
            view.HpMax.gameObject.SetActive(false);

            Row(vitals, view.ArmorRow, "ArmorIcon", "armor", 10, 46, 26, 40, 50, TextAlignmentOptions.MidlineLeft, 13, false);
            Row(vitals, view.HelmetRow, "HelmetIcon", "helmet", 104, 46, 26, 134, 50, TextAlignmentOptions.MidlineLeft, 13, false);
            Row(vitals, view.AdrenalineRow, "AdrenalineIcon", "adrenaline", 12, 80, 24, 157, 83, TextAlignmentOptions.MidlineRight, 12, true);
            Place(view.AdrenalineRow.Bar.transform, 42, 88, 109, 8);
        }

        /// <summary>The row becomes a transparent container over the whole block so children use block coordinates.</summary>
        private static void Row(Transform block, HudGaugeRowView row, string iconName, string sprite, float ix, float iy, float isize,
            float vx, float vy, TextAlignmentOptions align, float fontSize, bool keepBar)
        {
            var rt = (RectTransform)row.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = ((RectTransform)block).sizeDelta;
            row.Label.gameObject.SetActive(false);
            row.Bar.gameObject.SetActive(keepBar);
            var icon = AddIcon(row.transform, iconName, sprite, ix, iy, isize);
            Place(row.Value.transform, vx, vy, 40, 18);
            Text(row.Value, fontSize, align);
            var so = new SerializedObject(row);
            so.FindProperty("icon").objectReferenceValue = icon;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---- Quick bar 395×84 ----
        private static void QuickBar(Transform bar)
        {
            Rect(bar, new Vector2(0, 24), new Vector2(395, 84));
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
            Place(area, 10, 8, 375, 22);
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
            Text(view.WeaponName, 12, TextAlignmentOptions.MidlineLeft);
            Text(strip.Find("Ammo/Magazine").GetComponent<TMP_Text>(), 16, null);
            Text(strip.Find("Ammo/Reserve").GetComponent<TMP_Text>(), 12, null);
            strip.Find("Ammo").GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;
            var reload = strip.Find("ReloadHint");
            reload.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleRight;
            var keyCap = reload.Find("KeyCap");
            var keyLayout = keyCap.GetComponent<LayoutElement>();
            keyLayout.preferredWidth = keyLayout.preferredHeight = 18;
            Text(keyCap.Find("Label").GetComponent<TMP_Text>(), 13, null);
            Text(reload.Find("Label").GetComponent<TMP_Text>(), 13, null);

            // Slots: two groups of 4 × 42 with step 46, groups 15 apart, no group headings.
            Place(bar.Find("Slots"), 10, 34, 375, 42);
            Group(bar.Find("Slots/WeaponGroup"), 0);
            Group(bar.Find("Slots/ConsumableGroup"), 195);

            // Use-item strip takes the weapon strip's place.
            var use = bar.Find("UseActionStrip");
            Place(use, 10, 8, 375, 22);
            // "Di chuyển để hủy" needs ~108 u at 13 u and must not be ellipsized (UI-01 part 2).
            PlaceText(use.Find("Label"), 6, 2, 140, 18, 13);
            Place(use.Find("Track"), 150, 9, 64, 4);
            PlaceText(use.Find("Seconds"), 220, 2, 30, 18, 13);
            PlaceText(use.Find("Hint"), 254, 2, 117, 18, 13);
        }

        private static void Group(Transform group, float x)
        {
            Place(group, x, 0, 180, 42);
            group.Find("Label").gameObject.SetActive(false);
            for (var i = 0; i < 4; i++) Slot(group.GetChild(i + 1), i * 46f);
        }

        /// <summary>
        /// Slot 64 → 42: key 10 u top-left (approved exception), count 13 u top-right, icon 22 below both so the three
        /// never overlap (ART-04 sweep), name hidden (mock B).
        /// </summary>
        private static void Slot(Transform slot, float x)
        {
            Place(slot, x, 0, 42, 42);
            Place(slot.Find("Icon"), 10, 16, 22, 22);
            PlaceText(slot.Find("Key"), 3, 1, 12, 12, 10);
            PlaceText(slot.Find("Count"), 17, 0, 23, 16, 13);
            slot.Find("Name").gameObject.SetActive(false);
            var inUse = (RectTransform)slot.Find("InUseFrame");
            inUse.sizeDelta = new Vector2(42, 42);
        }

        // ---- Minimap 160×160 ----
        private static void Minimap(Transform minimap)
        {
            Rect(minimap, new Vector2(-24, -24), new Vector2(160, 160));
            var view = minimap.GetComponent<MinimapView>();
            var so = new SerializedObject(view);
            so.FindProperty("sizePixels").floatValue = 160f;
            so.ApplyModifiedPropertiesWithoutUndo();
            var area = minimap.Find("MapArea");
            for (var i = 1; i <= 4; i++)
            {
                ((RectTransform)area.Find("Grid/V" + i)).anchoredPosition = new Vector2(32 * i, 0);
                ((RectTransform)area.Find("Grid/H" + i)).anchoredPosition = new Vector2(0, 32 * i);
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
            captureSo.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---- Match status 160×59, one row: alive left, zone timer right ----
        private static void MatchStatus(Transform status)
        {
            Rect(status, new Vector2(-24, -192), new Vector2(160, 59));
            foreach (var rowName in new[] { "AliveRow", "ZoneRow" })
            {
                var row = (RectTransform)status.Find(rowName);
                row.anchorMin = row.anchorMax = row.pivot = new Vector2(0, 1);
                row.anchoredPosition = Vector2.zero;
                row.sizeDelta = new Vector2(160, 59);
                row.Find("Label").gameObject.SetActive(false);
            }
            // Spec put the timer at x 116 w 44, which ends on the frame (160) and clips "01:22": keep 10 u padding.
            AddIcon(status.Find("AliveRow"), "AliveIcon", "alive", 10, 17, 24);
            Place(status.Find("AliveRow/Value"), 38, 18, 38, 22);
            Text(status.Find("AliveRow/Value").GetComponent<TMP_Text>(), 16, TextAlignmentOptions.MidlineLeft);
            AddIcon(status.Find("ZoneRow"), "ZoneIcon", "zone-timer", 80, 17, 24);
            Place(status.Find("ZoneRow/Value"), 106, 18, 44, 22);
            Text(status.Find("ZoneRow/Value").GetComponent<TMP_Text>(), 16, TextAlignmentOptions.MidlineRight);
        }

        // ---- helpers ----
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

        private static void PlaceText(Transform t, float x, float y, float w, float h, float size)
        {
            Place(t, x, y, w, h);
            Text(t.GetComponent<TMP_Text>(), size, null);
        }

        private static void Text(TMP_Text text, float size, TextAlignmentOptions? align)
        {
            text.enableAutoSizing = false;
            text.fontSize = size;
            if (align.HasValue) text.alignment = align.Value;
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
