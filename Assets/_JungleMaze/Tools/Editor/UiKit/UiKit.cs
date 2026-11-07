using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using JungleMaze.Presentation.UI.Kit;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace JungleMaze.EditorTools
{
    /// <summary>
    /// Shared skin A kit for every UI builder (contract T-022..T-025, docs/CONTRACT_UI_V1.md). Coordinates and sizes
    /// are 1080p px as in the T-021 mocks; the canvases are 1280×720 reference, so one canvas unit = 1.5 px.
    /// Sprites: projects/lastaway/art/inbox/T-020 (kit_*, @2x at 300 PPU, 9-slice from meta.json) and
    /// T-021/b/assets (fx_*, source size, Simple + preserveAspect). Text: the 5-step scale of T-027.
    /// Every UI asset is GPT art; this file only imports, slices and places it (Huy 07/11).
    /// </summary>
    public static class UiKit
    {
        public const string KitFolder = "Assets/_JungleMaze/Art/UI/Kit";
        public const string FxFolder = KitFolder + "/Fx";
        public const string ArtInbox = "projects/lastaway/art/inbox";
        public const string ChakraPath = "Assets/_JungleMaze/Art/Fonts/TMP/ChakraPetch-Bold SDF.asset";
        public const string BodyPath = "Assets/_JungleMaze/Art/Fonts/TMP/HudTextBold SDF.asset";
        private const string ChakraTtf = "Assets/_JungleMaze/Art/JSurviv/Fonts/ChakraPetch-Bold.ttf";
        private const string Charset = "Assets/_JungleMaze/Art/Fonts/TMP/hud_charset.txt";
        public const float Scale = 1.5f;

        /// <summary>1080p px to canvas units.</summary>
        public static float Px(float px) => px / Scale;

        // ---------- Tokens (ui-kit.md mục 4, 9, 11; so-hien-hanh) ----------
        public static class Colors
        {
            public static readonly Color Text = Hex(0xED, 0xF2, 0xEE), Muted = Hex(0xD6, 0xDA, 0xD6), Gold = Hex(0xFF, 0xDC, 0x6E);
            public static readonly Color Ink = Hex(0x1F, 0x1A, 0x10), ExitRed = Hex(0x55, 0x14, 0x0F), DangerText = Hex(0xFF, 0x8A, 0x78);
            public static readonly Color Outline = Hex(0x0A, 0x0C, 0x0C), DisabledText = Hex(0xEB, 0xEB, 0xE6), Dim = Hex(0x1F, 0x1D, 0x18, 0.8f);
            // Dim 0.8, not the kit's 0.6: the project blends in Linear space, where 0.6 only darkens like ~0.38 does in the
            // sRGB mocks (measured: world 114 -> 73 in Unity vs 114 -> 53 in mock b1). 0.8 lands on the mock.
        }

        internal static Color Hex(byte r, byte g, byte b, float a = 1f) => new Color(r / 255f, g / 255f, b / 255f, a);

        // ---------- Text scale (T-027, ui-kit.md mục 13) ----------
        public enum Tier { H1 = 96, H2 = 40, B1 = 30, B2 = 24, C = 20 }
        private static readonly Tier[] Tiers = { Tier.C, Tier.B2, Tier.B1, Tier.H2, Tier.H1 };

        public static TMP_FontAsset Chakra => Load<TMP_FontAsset>(ChakraPath);
        public static TMP_FontAsset Body => Load<TMP_FontAsset>(BodyPath);

        /// <summary>Chakra Petch Bold for 96 and 40 (titles, big numbers); Be Vietnam Pro Bold for the rest and every button label.</summary>
        public static TMP_FontAsset FontFor(Tier tier, bool buttonLabel = false) =>
            !buttonLabel && (tier == Tier.H1 || tier == Tier.H2) ? Chakra : Body;

        /// <summary>Nearest step of the scale to a 1080p px size; a tie goes down; never below <paramref name="floorPx"/> (20, gameplay 24).</summary>
        public static Tier Snap(float px, float floorPx = 20f)
        {
            var best = Tier.C;
            var bestDistance = float.MaxValue;
            foreach (var t in Tiers)
            {
                if ((int)t < floorPx) continue;
                var d = Mathf.Abs(px - (int)t);
                if (d < bestDistance - 0.01f) { best = t; bestDistance = d; } // ascending order: a tie keeps the smaller step
            }
            return (int)best < floorPx ? Tiers[System.Array.FindIndex(Tiers, t => (int)t >= floorPx)] : best;
        }

        public static void ApplyTier(TMP_Text text, Tier tier, bool buttonLabel = false)
        {
            text.enableAutoSizing = false;
            text.fontSize = Px((int)tier);
            var font = FontFor(tier, buttonLabel);
            if (text.font != font) { text.font = font; text.fontSharedMaterial = font.material; }
            text.fontStyle = FontStyles.Normal; // the face is already Bold; faux bold would double it
        }

        public static TextMeshProUGUI Text(RectTransform parent, string name, Tier tier, string value, Color color,
            TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft, bool buttonLabel = false)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            ApplyTier(t, tier, buttonLabel);
            t.text = value;
            t.color = color;
            t.alignment = align;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            t.raycastTarget = false;
            return t;
        }

        /// <summary>
        /// Moves every text under <paramref name="root"/> onto the scale (the 206/353 off-scale texts of T-027). Size
        /// snaps to the nearest step at or above the floor; the font follows the tier. Texts named in
        /// <paramref name="keep"/> are left alone. Returns how many changed.
        /// </summary>
        public static int SnapAll(GameObject root, float floorPx = 20f, ICollection<string> keep = null)
        {
            var changed = 0;
            foreach (var t in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (keep != null && keep.Contains(t.name)) continue;
                var before = t.fontSize;
                var beforeFont = t.font;
                ApplyTier(t, Snap(t.fontSize * Scale, floorPx));
                if (!Mathf.Approximately(before, t.fontSize) || beforeFont != t.font) changed++;
            }
            return changed;
        }

        // ---------- Font (Chakra Petch Bold SDF, Vietnamese set of hud_charset.txt) ----------
        [MenuItem("Jungle Maze/UI Kit/Build Chakra Petch SDF")]
        public static TMP_FontAsset BuildChakraFont()
        {
            var ttf = Load<Font>(ChakraTtf);
            var chars = File.ReadAllText(Charset).Trim('\r', '\n');
            var fa = TMP_FontAsset.CreateFontAsset(ttf, 72, 9, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, false);
            if (!fa.TryAddCharacters(chars, out var missing) && !string.IsNullOrEmpty(missing.Trim()))
                throw new InvalidDataException("Chakra Petch is missing glyphs: " + missing);
            fa.atlasPopulationMode = AtlasPopulationMode.Static;
            fa.name = Path.GetFileNameWithoutExtension(ChakraPath);
            AssetDatabase.DeleteAsset(ChakraPath);
            AssetDatabase.CreateAsset(fa, ChakraPath);
            fa.atlasTexture.name = fa.name + " Atlas";
            fa.material.name = fa.name + " Material";
            AssetDatabase.AddObjectToAsset(fa.atlasTexture, fa);
            AssetDatabase.AddObjectToAsset(fa.material, fa);
            EditorUtility.SetDirty(fa);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(ChakraPath);
            var saved = Load<TMP_FontAsset>(ChakraPath);
            foreach (var c in chars) // self-check: every glyph of the set made it into the saved static atlas
                if (!char.IsWhiteSpace(c) && !saved.HasCharacter(c, false, false))
                    throw new InvalidDataException("Chakra Petch SDF lacks '" + c + "' after save");
            return saved;
        }

        // ---------- Sprites ----------
        [MenuItem("Jungle Maze/UI Kit/Import Kit Sprites")]
        public static int ImportKit()
        {
            Directory.CreateDirectory(KitFolder);
            Directory.CreateDirectory(FxFolder);
            var metaPath = Path.Combine(ArtInbox, "T-020", "meta.json");
            var meta = File.ReadAllText(metaPath);
            var names = new List<string>();
            foreach (Match m in Regex.Matches(meta, "\"(kit_[a-z0-9_]+)\"\\s*:\\s*\\{")) names.Add(m.Groups[1].Value);
            var fx = Directory.GetFiles(Path.Combine(ArtInbox, "T-021", "b", "assets"), "fx_*.png");
            foreach (var n in names) // check first: a half art drop never leaves a mixed set in Assets
                if (!File.Exists(Path.Combine(ArtInbox, "T-020", n + "@2x.png"))) throw new FileNotFoundException("kit sprite missing, nothing imported", n);
            foreach (var n in names)
                Copy(Path.Combine(ArtInbox, "T-020", n + "@2x.png"), KitFolder + "/" + n + ".png", 300f, Border2x(meta, n));
            foreach (var f in fx)
                Copy(f, FxFolder + "/" + Path.GetFileName(f), 100f, Vector4.zero);
            // T-028 pack (art-ux, CPO duyệt 07/11): new icons and badge (@2x, border from its meta.json), floor tile.
            var pack = Path.Combine(ArtInbox, "T-028-unity-pack", "files");
            var newMeta = File.ReadAllText(Path.Combine(pack, "new", "meta.json"));
            var extra = 0;
            foreach (Match m in Regex.Matches(newMeta, @"""([a-z0-9_]+)""\s*:\s*\{")) // every approved file of files/new
            {
                var n = m.Groups[1].Value;
                Copy(Path.Combine(pack, "new", n + "@2x.png"), KitFolder + "/" + n + ".png", 300f, Border2x(newMeta, n));
                extra++;
            }
            Copy(Path.Combine(pack, "bg", "nen-gach-tile.png"), KitFolder + "/" + FloorTile + ".png", 100f, Vector4.zero, true);
            return names.Count + fx.Length + extra + 1;
        }

        /// <summary>@2x border [L, R, T, B] from T-020 meta.json ("border_LRTB" is @1x).</summary>
        public static Vector4 Border2x(string meta, string sprite)
        {
            var block = Regex.Match(meta, "\"" + Regex.Escape(sprite) + "\"\\s*:\\s*\\{([^}]*)\\}");
            var b = Regex.Match(block.Groups[1].Value, @"""border_LRTB""\s*:\s*\[\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)");
            if (!b.Success) return Vector4.zero;
            return new Vector4(int.Parse(b.Groups[1].Value), int.Parse(b.Groups[2].Value), int.Parse(b.Groups[3].Value), int.Parse(b.Groups[4].Value)) * 2f;
        }

        public const string FloorTile = "bg_floor_tile";

        private static void Copy(string src, string dst, float ppu, Vector4 lrtb, bool tile = false)
        {
            if (!File.Exists(src)) throw new FileNotFoundException("UI kit source missing", src);
            File.Copy(src, dst, true);
            AssetDatabase.ImportAsset(dst, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(dst);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = ppu;
            importer.spriteBorder = new Vector4(lrtb.x, lrtb.w, lrtb.y, lrtb.z); // Unity order: left, bottom, right, top
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            if (tile)
            {
                importer.wrapMode = TextureWrapMode.Repeat;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect; // Image.Type.Tiled needs a full-rect mesh to repeat
                importer.SetTextureSettings(settings);
            }
            importer.SaveAndReimport();
        }

        public static Sprite Sprite(string name)
        {
            var folder = name.StartsWith("fx_") ? FxFolder : KitFolder;
            return Load<Sprite>(folder + "/" + name + ".png");
        }

        private static T Load<T>(string path) where T : Object
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a == null) throw new System.InvalidOperationException("UI kit asset missing (run Jungle Maze/UI Kit menu): " + path);
            return a;
        }

        // ---------- Layout ----------
        public static RectTransform Node(RectTransform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        /// <summary>Top-left of the parent, 1080p px, y down (mock coordinates relative to the parent).</summary>
        public static RectTransform Place(RectTransform t, float x, float y, float w, float h)
        {
            t.anchorMin = t.anchorMax = t.pivot = new Vector2(0f, 1f);
            t.anchoredPosition = new Vector2(Px(x), -Px(y));
            t.sizeDelta = new Vector2(Px(w), Px(h));
            return t;
        }

        /// <summary>Absolute 1920×1080 mock rect, anchored at the parent's centre so it stays centred on any aspect.</summary>
        public static RectTransform PlaceCentred(RectTransform t, float x, float y, float w, float h)
        {
            t.anchorMin = t.anchorMax = t.pivot = new Vector2(0.5f, 0.5f);
            t.anchoredPosition = new Vector2(Px(x + w / 2f - 960f), -Px(y + h / 2f - 540f));
            t.sizeDelta = new Vector2(Px(w), Px(h));
            return t;
        }

        public static RectTransform Stretch(RectTransform t, float inset = 0f)
        {
            t.anchorMin = Vector2.zero;
            t.anchorMax = Vector2.one;
            t.pivot = new Vector2(0.5f, 0.5f);
            t.offsetMin = new Vector2(Px(inset), Px(inset));
            t.offsetMax = new Vector2(-Px(inset), -Px(inset));
            return t;
        }

        /// <summary>Image of a kit or fx sprite: Sliced when the sprite has a border, else Simple with preserveAspect.</summary>
        public static Image Image(RectTransform t, string sprite, Color? tint = null)
        {
            var img = t.GetComponent<Image>() ?? t.gameObject.AddComponent<Image>();
            img.sprite = Sprite(sprite);
            img.color = tint ?? Color.white;
            var sliced = img.sprite.border != Vector4.zero;
            img.type = sliced ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
            img.preserveAspect = !sliced;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>Scales the 9-slice so the sprite's border keeps its proportion at another height (K2 S 48, L 80).</summary>
        public static void FitHeight(Image img, float heightPx)
        {
            var nativeUnits = img.sprite.rect.height * 100f / img.sprite.pixelsPerUnit; // canvas referencePixelsPerUnit = 100
            img.pixelsPerUnitMultiplier = nativeUnits / Px(heightPx);
        }

        // ---------- Panels ----------
        public enum PanelKind { S, M, L, Popup }

        /// <summary>S, M, L are all the 9-sliced S source (border 47, same corners); Popup has the 88 px title bar.</summary>
        public static Image Panel(RectTransform t, PanelKind kind) => Image(t, kind == PanelKind.Popup ? "kit_panel_popup" : "kit_panel_s");

        /// <summary>Full-screen scrim behind a modal (kit_dim, alpha 0.6). Blocks clicks to what is under it.</summary>
        public static Image Dim(RectTransform parent)
        {
            var t = Stretch(Node(parent, "Dim"));
            var img = Image(t, "kit_dim", Colors.Dim);
            img.preserveAspect = false; // kit_dim has no border, so Image() would keep it square in the middle
            img.raycastTarget = true;
            return img;
        }

        /// <summary>
        /// Full-screen stone floor of the mocks (T-028 bg, 510x280 tile at 1080p) with the dark wash on top
        /// (alpha 0.62 to 0.7 per bang-tong.md). Put it first under the screen root.
        /// </summary>
        public static RectTransform Floor(RectTransform parent, float washAlpha = 0.66f)
        {
            var t = Stretch(Node(parent, "Floor"));
            var img = Image(t, FloorTile);
            img.type = UnityEngine.UI.Image.Type.Tiled;
            img.preserveAspect = false;
            img.pixelsPerUnitMultiplier = 1.5f; // tile is 1080p px at 100 PPU; canvas units are 1.5 px
            var wash = Image(Stretch(Node(t, "Wash")), "kit_dim", new Color(0f, 0f, 0f, washAlpha));
            wash.type = UnityEngine.UI.Image.Type.Simple;
            wash.preserveAspect = false;
            img.raycastTarget = true; // a full-screen backdrop also stops clicks reaching the world
            return t;
        }

        // ---------- K2 buttons (T-026; ui-kit.md mục 9, 11) ----------
        public enum ButtonKind { Primary, Secondary, Danger, Exit }
        public enum ButtonSize { S = 48, M = 64, L = 80 }

        public static Tier LabelTier(ButtonSize size) => size == ButtonSize.S ? Tier.B2 : size == ButtonSize.M ? Tier.B1 : Tier.H2;

        /// <summary>
        /// Raised K2 button: sprite swap normal/hover/pressed/disabled, icon (ink height = label size) + 10 px + label,
        /// the group centred on the face (bottom padding = base, <see cref="KitButtonFace"/>). Exit = secondary face
        /// with the red X (Thoát trận, Rời phòng). Place it with <see cref="Place"/> or <see cref="PlaceCentred"/>.
        /// </summary>
        public static Button Button(RectTransform parent, string name, ButtonKind kind, ButtonSize size, float widthPx,
            string icon, string label)
        {
            var t = Node(parent, name);
            t.sizeDelta = new Vector2(Px(widthPx), Px((int)size));
            var face = kind == ButtonKind.Exit ? "secondary" : kind.ToString().ToLowerInvariant();
            var img = Image(t, "kit_btn_" + face + "_normal");
            img.raycastTarget = true;
            FitHeight(img, (int)size);
            var b = t.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            b.transition = Selectable.Transition.SpriteSwap;
            b.spriteState = new SpriteState
            {
                highlightedSprite = Sprite("kit_btn_" + face + "_hover"),
                selectedSprite = Sprite("kit_btn_" + face + "_normal"),
                pressedSprite = Sprite("kit_btn_" + face + "_pressed"),
                disabledSprite = Sprite("kit_btn_" + face + "_disabled"),
            };

            var content = Stretch(Node(t, "Face"));
            var group = Node(content, "Group");
            group.anchorMin = group.anchorMax = group.pivot = new Vector2(0.5f, 0.5f);
            var row = group.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = Px(10);
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = false;
            var fit = group.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var tier = LabelTier(size);
            if (!string.IsNullOrEmpty(icon) || kind == ButtonKind.Exit)
            {
                var ico = Node(group, "Icon");
                Image(ico, kind == ButtonKind.Exit ? "kit_icon_cross_red" : icon);
                var le = ico.gameObject.AddComponent<LayoutElement>();
                le.preferredWidth = le.preferredHeight = Px((int)tier);
            }
            if (!string.IsNullOrEmpty(label))
            {
                var text = Text(group, "Label", tier, label, Colors.Ink, TextAlignmentOptions.Capline, true);
                text.gameObject.AddComponent<KitButtonLabel>().Bind(b, Colors.Ink, Colors.DisabledText);
            }
            t.gameObject.AddComponent<KitButtonFace>().Bind(b, content);
            return b;
        }
    }
}
