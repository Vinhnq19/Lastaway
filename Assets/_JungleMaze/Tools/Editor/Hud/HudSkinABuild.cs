using System.IO;
using System.Text.RegularExpressions;
using JungleMaze.Presentation.UI.Hud;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace JungleMaze.EditorTools
{
    /// <summary>
    /// T-016: skin A "carved stone" (Huy 06/11/2026) on top of HUD layout B. Sprites come from
    /// projects/lastaway/art/inbox/T-015 (asset-list.md, meta.json): the @2x files at 300 PPU, so 1 sprite px = 1 px at
    /// 1080p and borders land in the designed reference units. Each block keeps its own background Image (tinted by
    /// HudPalette at runtime) but disabled; the stone draws from a white "Skin" child, so no palette code changes.
    /// Called by <see cref="HudLayoutBBuild.Apply"/> after the layout; idempotent. New art (same file names) only needs
    /// a re-run: 9-slice borders are read from meta.json, and any border that would not fit its rect is scaled down.
    /// </summary>
    public static class HudSkinABuild
    {
        public const string SpriteFolder = "Assets/_JungleMaze/Art/Hud/SkinA";
        private static string SpriteSource => HudLayoutBBuild.ArtInbox + "/T-015";
        private const float SpritePpu = 300f;
        public static readonly Color OutlineColor = new Color(0x0C / 255f, 0x14 / 255f, 0x17 / 255f, 1f);

        private static readonly string[] Sprites =
        {
            "hud_panel_vitals", "hud_panel_quickbar", "hud_panel_match", "hud_minimap_frame", "hud_minimap_overlay",
            "hud_slot_empty", "hud_slot_selected", "hud_bar_track_hp", "hud_bar_track_thin", "hud_bar_fill_white",
        };

        /// <summary>
        /// @2x 9-slice border [L, R, T, B] of one sprite from meta.json. Reads "border_LRTB_2x" (round 2 format) or
        /// else the @1x "border_LRTB" doubled (round 3 format). No entry, null or no border = 0 (stretched whole).
        /// </summary>
        public static Vector4 BorderLRTB(string metaJson, string sprite)
        {
            var block = Regex.Match(metaJson, "\"" + Regex.Escape(sprite) + "\"\\s*:\\s*\\{([^}]*)\\}");
            if (!block.Success) return Vector4.zero;
            const string four = "\\s*:\\s*\\[\\s*(\\d+)\\s*,\\s*(\\d+)\\s*,\\s*(\\d+)\\s*,\\s*(\\d+)";
            var b = Regex.Match(block.Groups[1].Value, "\"border_LRTB_2x\"" + four);
            var scale = 1;
            if (!b.Success) { b = Regex.Match(block.Groups[1].Value, "\"border_LRTB\"" + four); scale = 2; }
            if (!b.Success) return Vector4.zero;
            return new Vector4(int.Parse(b.Groups[1].Value), int.Parse(b.Groups[2].Value), int.Parse(b.Groups[3].Value), int.Parse(b.Groups[4].Value)) * scale;
        }

        /// <summary>
        /// Transparent hole of the minimap frame in @1x px (x, y from the sprite's top-left, y down) from meta.json
        /// "hole": [x0, y0, x1, y1], inclusive (r11: 24, 24, 292, 291 = 269×268).
        /// </summary>
        public static Rect MinimapHole()
        {
            var meta = File.ReadAllText(Path.Combine(SpriteSource, "meta.json"));
            var block = Regex.Match(meta, @"""hud_minimap_frame""\s*:\s*\{([^}]*)\}");
            var h = Regex.Match(block.Groups[1].Value, @"""hole""\s*:\s*\[\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)");
            if (!h.Success) throw new InvalidDataException("meta.json: hud_minimap_frame has no \"hole\"");
            int x0 = int.Parse(h.Groups[1].Value), y0 = int.Parse(h.Groups[2].Value);
            return new Rect(x0, y0, int.Parse(h.Groups[3].Value) - x0 + 1, int.Parse(h.Groups[4].Value) - y0 + 1);
        }

        public static void ImportSprites()
        {
            Directory.CreateDirectory(SpriteFolder);
            var meta = File.ReadAllText(Path.Combine(SpriteSource, "meta.json"));
            // Check every source first so a half-finished art drop never leaves a mixed set in Assets.
            foreach (var name in Sprites)
            {
                var src = Path.Combine(SpriteSource, name + "@2x.png");
                if (!File.Exists(src)) throw new FileNotFoundException("Skin A sprite missing, nothing imported", src);
            }
            foreach (var name in Sprites)
            {
                var border = BorderLRTB(meta, name);
                var src = Path.Combine(SpriteSource, name + "@2x.png");
                var dst = SpriteFolder + "/" + name + ".png";
                File.Copy(src, dst, true);
                AssetDatabase.ImportAsset(dst, ImportAssetOptions.ForceUpdate);
                var importer = (TextureImporter)AssetImporter.GetAtPath(dst);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = SpritePpu;
                importer.spriteBorder = new Vector4(border.x, border.w, border.y, border.z); // Unity: left, bottom, right, top
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
                // Self-check of the meta.json parse: the imported sprite must carry exactly the declared border.
                var imported = AssetDatabase.LoadAssetAtPath<Sprite>(dst);
                if (imported.border != importer.spriteBorder)
                    throw new InvalidDataException(name + ": border " + imported.border + " != meta.json " + border);
            }
        }

        private static Sprite Load(string name)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteFolder + "/" + name + ".png");
            if (sprite == null) throw new System.InvalidOperationException("Skin A sprite not imported: " + name);
            return sprite;
        }

        public static void Apply(Transform hud)
        {
            foreach (var block in new[] { "Vitals", "QuickBar", "Minimap", "MatchStatus" })
                OldFrameOff(hud.Find(block));
            Panel(hud.Find("Vitals"), "hud_panel_vitals");
            Panel(hud.Find("QuickBar"), "hud_panel_quickbar");
            Panel(hud.Find("MatchStatus"), "hud_panel_match");
            NewTokens(hud);

            var vitals = hud.Find("Vitals").GetComponent<VitalsView>();
            // Fill sits in the track's dark groove: 6 px inside the HP track, 5 px inside the thin one (1080p).
            Bar(vitals.HpBar, "hud_bar_track_hp", 6f / 1.5f);
            Bar(vitals.AdrenalineRow.Bar, "hud_bar_track_thin", 5f / 1.5f);

            foreach (var slot in hud.Find("QuickBar").GetComponentsInChildren<QuickbarSlotView>(true)) Slot(slot);
            Minimap(hud.Find("Minimap"));
        }

        /// <summary>
        /// The prefab's serialized palette predates <see cref="HudPalette.Cream"/> and <see cref="HudPalette.Gold"/>: fill
        /// them once from Default. A value already set on the prefab (inspector) is kept, so each colour changes in one place.
        /// </summary>
        private static void NewTokens(Transform hud)
        {
            var so = new SerializedObject(hud.GetComponent<HudRoot>());
            foreach (var (name, value) in new[] { ("Cream", HudPalette.Default.Cream), ("Gold", HudPalette.Default.Gold), ("HpFill", HudPalette.Default.HpFill) })
            {
                var token = so.FindProperty("palette." + name);
                if (token.colorValue.a == 0f) token.colorValue = value;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Old direction-A edge/bevel overlays go off; the stone frame replaces them.</summary>
        private static void OldFrameOff(Transform block)
        {
            foreach (var name in new[] { "ArtEdge", "ArtBevel" })
            {
                var t = block.Find(name);
                if (t != null) t.gameObject.SetActive(false);
            }
        }

        private static void Panel(Transform block, string sprite)
        {
            block.GetComponent<Image>().enabled = false;
            var skin = Child(block, "Skin", 0);
            Stretch(skin, 0, 0);
            Simple(skin, sprite);
        }

        /// <summary>The bar rect is the fill area; the stone track sits around it, <paramref name="inset"/> u wider per side.</summary>
        private static void Bar(HudBarView bar, string trackSprite, float inset)
        {
            bar.TrackImage.enabled = false;
            var skin = Child(bar.transform, "TrackSkin", 0);
            Stretch(skin, -inset, inset);
            Sliced(skin, trackSprite);
            // N-FILL: fill and trail are as tall as the bar rect (fill area); judge the border on that height only,
            // since their width follows the value.
            var height = ((RectTransform)bar.transform).rect.height;
            foreach (var image in new[] { bar.FillImage, bar.TrailImage })
            {
                if (image == null) continue;
                image.sprite = Load("hud_bar_fill_white");
                image.type = Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = FitMultiplier(image.sprite, float.MaxValue, height);
            }
        }

        /// <summary>
        /// pixelsPerUnitMultiplier that keeps both border pairs inside the rect (1 when they already fit). Without it
        /// Unity squashes the caps when left+right or top+bottom is wider than the rect.
        /// </summary>
        public static float FitMultiplier(Sprite sprite, float width, float height)
        {
            var b = sprite.border / sprite.pixelsPerUnit * 100f; // reference units at multiplier 1
            return Mathf.Max(1f, (b.x + b.z) / width, (b.y + b.w) / height);
        }

        private static void Slot(QuickbarSlotView slot)
        {
            OldFrameOff(slot.transform);
            var so = new SerializedObject(slot);
            so.FindProperty("defaultBorder").objectReferenceValue = Load("hud_slot_empty");
            so.FindProperty("emptyBorder").objectReferenceValue = Load("hud_slot_empty");
            so.FindProperty("selectedBorder").objectReferenceValue = Load("hud_slot_selected");
            so.FindProperty("skinSprites").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
            slot.Border.sprite = Load("hud_slot_empty");
            slot.Border.type = Image.Type.Sliced;
            slot.Border.fillCenter = true;
            slot.Border.color = Color.white;
            var rect = ((RectTransform)slot.transform).rect;
            slot.Border.pixelsPerUnitMultiplier = FitMultiplier(slot.Border.sprite, rect.width, rect.height);
            KeyBadge(slot);
            slot.KeyLabel.fontSharedMaterial = KeyOutline(slot.KeyLabel.font);
            slot.CountLabel.fontSharedMaterial = KeyOutline(slot.CountLabel.font); // ×n also sits over the icon
        }

        /// <summary>
        /// T-022 (T-028 pack): kit_badge_key (48x48 @1x, border 14) drawn 30x30 behind the key number in the slot's
        /// bottom-right corner, same rect as the Key text (<see cref="HudLayoutBBuild.KeyBadge"/>).
        /// </summary>
        private static void KeyBadge(QuickbarSlotView slot)
        {
            var key = slot.KeyLabel.rectTransform;
            var badge = slot.transform.Find("KeyBadge") as RectTransform;
            if (badge == null) badge = UiKit.Node((RectTransform)slot.transform, "KeyBadge");
            var target = key.GetSiblingIndex(); // just under the key; only the added badge moves (slot is a nested prefab)
            if (badge.GetSiblingIndex() < target) target--;
            badge.SetSiblingIndex(target);
            var r = HudLayoutBBuild.KeyBadge;
            UiKit.Place(badge, r.x, r.y, r.width, r.height);
            UiKit.FitHeight(UiKit.Image(badge, "kit_badge_key"), r.height);
        }

        private const string KeyMaterialPath = "Assets/_JungleMaze/Art/Fonts/TMP/HudTextBold SDF - Key Outline.mat";

        /// <summary>
        /// Slot key numbers: dark 3 px outline (asset-list r11) so the gold digit reads over an item icon. One shared
        /// material preset of the font's own material, rebuilt from it on every run.
        /// </summary>
        private static Material KeyOutline(TMPro.TMP_FontAsset font)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(KeyMaterialPath);
            if (material == null)
            {
                material = new Material(font.material);
                AssetDatabase.CreateAsset(material, KeyMaterialPath);
            }
            material.shader = font.material.shader;
            material.CopyPropertiesFromMaterial(font.material);
            material.EnableKeyword("OUTLINE_ON");
            material.SetColor("_OutlineColor", OutlineColor);
            material.SetFloat("_OutlineWidth", KeyOutlineWidth);
            material.SetFloat("_FaceDilate", KeyOutlineWidth / 2f); // grow outwards: the digit keeps its weight
            TMPro.ShaderUtilities.UpdateShaderRatios(material); // TMP ignores outline values until the ratios match them
            EditorUtility.SetDirty(material);
            return material;
        }

        /// <summary>
        /// SDF units. shortcut: HudTextBold SDF has padding 6 at 48 pt, i.e. at most ~2 px of distance field around a
        /// 16 px (1080p) digit, so the asset-list's 3 px outline is capped near 2 px; a 3 px one needs the atlas rebuilt
        /// with padding >= 9.
        /// </summary>
        public static float KeyOutlineWidth = 0.6f;

        /// <summary>
        /// Map (terrain) → dark overlay → zone rings with a 1 px dark outline → grid, marker → stone frame on top.
        /// Rings come from their own RenderTexture (MinimapMapCapture) so the overlay darkens terrain only.
        /// </summary>
        private static void Minimap(Transform minimap)
        {
            minimap.GetComponent<Image>().enabled = false; // the old Surface square showed in the frame's chamfered corners
            var area = minimap.Find("MapArea");
            // Clip map and rings to the hole so the rings never show past the frame.
            var mask = area.GetComponent<RectMask2D>();
            if (mask == null) mask = area.gameObject.AddComponent<RectMask2D>();
            mask.padding = Vector4.zero; // MapArea is the frame's hole (r11); the frame's own rim covers the edge
            var overlay = Child(area, "Overlay", 1);
            Stretch(overlay, 0, 0);
            var overlayImage = overlay.GetComponent<Image>();
            overlayImage.sprite = Load("hud_minimap_overlay");
            overlayImage.type = Image.Type.Simple;
            overlayImage.color = Color.white;
            overlayImage.raycastTarget = false;

            var ring = area.Find("Rings");
            if (ring == null)
            {
                ring = new GameObject("Rings", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage), typeof(Outline)).transform;
                ring.SetParent(area, false);
            }
            ring.SetSiblingIndex(2);
            Stretch((RectTransform)ring, 0, 0);
            var raw = ring.GetComponent<RawImage>();
            raw.raycastTarget = false;
            raw.color = Color.white;
            raw.enabled = false;
            var outline = ring.GetComponent<Outline>();
            outline.effectColor = OutlineColor;
            // 1 u = 1.5 px at 1080p. Measured T-016: 1 px left the red ring at p10 2.6:1 against its surroundings,
            // 1.5 px gives p10 3.0:1, median 5.1:1.
            outline.effectDistance = new Vector2(1f, 1f);
            outline.useGraphicAlpha = true;

            var capture = minimap.GetComponent<MinimapMapCapture>();
            var so = new SerializedObject(capture);
            so.FindProperty("ringImage").objectReferenceValue = raw;
            so.ApplyModifiedPropertiesWithoutUndo();
            // T-022 R3 (CPO 07/11): the captured red/yellow lines stay out of the terrain (split render) but are not shown;
            // the minimap draws the same orange/white sprite rings as the big map, stroke at minimap scale.
            ring.gameObject.SetActive(false);
            var rings = ZoneRings((RectTransform)area, MinimapRingPx);
            rings.ZoneRing.transform.SetSiblingIndex(3);
            rings.SafeRing.transform.SetSiblingIndex(4);

            var frame = Child(minimap, "Frame", minimap.childCount - 1);
            frame.SetAsLastSibling();
            Stretch(frame, 0, 0);
            Simple(frame, "hud_minimap_frame");
        }

        /// <summary>Zone ring stroke on the minimap, 1080p px (measured on T-015/dung-thu-hud-A.png: about 2 px).</summary>
        public const float MinimapRingPx = 2f;

        /// <summary>
        /// Band of the ring art in the imported @2x sprites, fraction of the sprite size from its centre (measured on
        /// T-028 files/new: map_ring_zone@2x 1024 px, band 6 px at the edge; map_ring_safe@2x 512 px, band 20 px).
        /// </summary>
        public static readonly Vector2 ZoneBand = new Vector2(505f / 1024f, 511f / 1024f), SafeBand = new Vector2(236f / 512f, 255.5f / 512f);

        /// <summary>
        /// Orange current zone and white next safe zone as <see cref="RingGraphic"/> (fixed stroke) under <paramref name="area"/>,
        /// driven by a <see cref="HudZoneRings"/> on the area. Idempotent: reuses the rings and component it finds.
        /// </summary>
        public static HudZoneRings ZoneRings(RectTransform area, float strokePx)
        {
            var rings = area.GetComponent<HudZoneRings>();
            if (rings == null) rings = area.gameObject.AddComponent<HudZoneRings>();
            var so = new SerializedObject(rings);
            so.FindProperty("area").objectReferenceValue = area;
            so.FindProperty("zoneRing").objectReferenceValue = Ring(area, "ZoneRing", "map_ring_zone", ZoneBand, strokePx);
            so.FindProperty("safeRing").objectReferenceValue = Ring(area, "SafeRing", "map_ring_safe", SafeBand, strokePx);
            so.ApplyModifiedPropertiesWithoutUndo();
            return rings;
        }

        private static RingGraphic Ring(RectTransform area, string name, string sprite, Vector2 band, float strokePx)
        {
            var t = area.Find(name) as RectTransform;
            if (t == null) t = UiKit.Node(area, name);
            t.anchorMin = t.anchorMax = Vector2.zero; // positioned from the map's bottom-left, like the minimap marker
            t.pivot = new Vector2(0.5f, 0.5f);
            var g = t.GetComponent<RingGraphic>();
            if (g == null) g = t.gameObject.AddComponent<RingGraphic>();
            var so = new SerializedObject(g);
            so.FindProperty("sprite").objectReferenceValue = UiKit.Sprite(sprite);
            so.FindProperty("thickness").floatValue = UiKit.Px(strokePx);
            so.FindProperty("uvInner").floatValue = band.x;
            so.FindProperty("uvOuter").floatValue = band.y;
            so.ApplyModifiedPropertiesWithoutUndo();
            g.color = Color.white; // colour comes from the art
            g.raycastTarget = false;
            t.gameObject.SetActive(false);
            return g;
        }

        private static RectTransform Child(Transform parent, string name, int index)
        {
            var t = parent.Find(name);
            if (t == null)
            {
                t = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).transform;
                t.SetParent(parent, false);
            }
            t.SetSiblingIndex(index);
            return (RectTransform)t;
        }

        private static void Stretch(RectTransform rt, float min, float max)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(min, min);
            rt.offsetMax = new Vector2(max, max);
        }

        /// <summary>Asset-list r11: frames are drawn Simple, the rect is the sprite's own 1080p size.</summary>
        private static void Simple(RectTransform t, string sprite)
        {
            var image = t.GetComponent<Image>();
            image.sprite = Load(sprite);
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.color = Color.white; // colours are in the stone art
            image.raycastTarget = false;
        }

        private static void Sliced(RectTransform t, string sprite)
        {
            var image = t.GetComponent<Image>();
            image.sprite = Load(sprite);
            image.type = Image.Type.Sliced;
            image.fillCenter = true;
            image.pixelsPerUnitMultiplier = FitMultiplier(image.sprite, t.rect.width, t.rect.height);
            image.color = Color.white; // colours are in the stone art
            image.raycastTarget = false;
        }
    }
}
