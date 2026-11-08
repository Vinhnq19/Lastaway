using System;
using System.IO;
using JungleMaze.Presentation.Combat;
using JungleMaze.Presentation.World.Loot;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace JungleMaze.EditorTools
{
    /// <summary>
    /// Repeatable build of the ground loot look (Docs/hvu/07_Floor_Loot.vi.md section 7): procedural halo, light column and
    /// mote sprites, an unlit URP 2D sprite material, and the <see cref="GroundLootTuning"/> asset. Rebuilding rewrites the
    /// sprites in place (same GUIDs) and only refreshes the tuning's art references, so tuned numbers and colours stay.
    /// </summary>
    public static class GroundLootAssetBuilder
    {
        public const string ArtFolder = "Assets/_JungleMaze/Art/World/Loot";
        public const string HaloPath = ArtFolder + "/loot_halo.png";
        public const string BeamPath = ArtFolder + "/loot_beam.png";
        public const string SparklePath = ArtFolder + "/loot_sparkle.png";
        public const string AmmoPath = ArtFolder + "/loot_ammo_box.png";
        public const string MaterialPath = ArtFolder + "/GroundLootLight.mat";
        /// <summary>Brightness multiplier of the loot light material; the sprite colours stay LDR, this lifts them into bloom.</summary>
        public const float LightHdrBoost = 1.8f;
        public const string TuningPath = "Assets/_JungleMaze/Art/World/Resources/" + GroundLootTuning.ResourceName + ".asset";
        private const string HeldArtPath = "Assets/_JungleMaze/Art/Characters/HeldWeaponCatalog.asset";
        private const string UnlitShader = "Universal Render Pipeline/2D/Sprite-Unlit-Default";

        [MenuItem("Jungle Maze/Loot/Build Ground Loot Assets")]
        public static void Build()
        {
            EnsureFolder(ArtFolder);
            EnsureFolder(Path.GetDirectoryName(TuningPath).Replace('\\', '/'));
            const int halo = 64, beam = 64, mote = 32;
            // Soft round glow; also the (tinted black, squashed) shadow.
            WriteSprite(HaloPath, halo, halo, (u, v) =>
            {
                var r = Mathf.Sqrt((u - .5f) * (u - .5f) + (v - .5f) * (v - .5f)) * 2f;
                return Mathf.Pow(Mathf.Clamp01(1f - r), 1.8f);
            }, halo, new Vector2(.5f, .5f));
            // Column rising from its bottom pivot: gaussian across, fading out toward the top.
            WriteSprite(BeamPath, beam, beam, (u, v) =>
            {
                var across = Mathf.Exp(-Mathf.Pow((u - .5f) / .2f, 2f));
                var up = Mathf.Pow(1f - v, 1.4f) * Mathf.SmoothStep(0f, 1f, v / .06f);
                return across * up;
            }, beam, new Vector2(.5f, 0f));
            // Four-point twinkle.
            WriteSprite(SparklePath, mote, mote, (u, v) =>
            {
                float x = Mathf.Abs(u - .5f) * 2f, y = Mathf.Abs(v - .5f) * 2f;
                var star = Mathf.Clamp01(1f - (Mathf.Min(x, y) * 6f + Mathf.Max(x, y)));
                var core = Mathf.Clamp01(1f - Mathf.Sqrt(x * x + y * y) * 2.5f);
                return Mathf.Clamp01(star + core);
            }, mote, new Vector2(.5f, .5f));

            WritePixelArt(AmmoPath, AmmoBoxArt, AmmoPalette, 16);

            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            var shader = Shader.Find(UnlitShader);
            if (shader == null) throw new InvalidOperationException("Missing shader " + UnlitShader);
            if (material == null)
            {
                material = new Material(shader) { name = "GroundLootLight" };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            else if (material.shader != shader) material.shader = shader;
            // HDR tint (> 1) so the halo, beam and sparkles pass the gameplay bloom threshold (GameplayPostTuning).
            material.SetColor("_Color", new Color(LightHdrBoost, LightHdrBoost, LightHdrBoost, 1f));
            EditorUtility.SetDirty(material);

            var tuning = AssetDatabase.LoadAssetAtPath<GroundLootTuning>(TuningPath);
            if (tuning == null)
            {
                tuning = ScriptableObject.CreateInstance<GroundLootTuning>();
                AssetDatabase.CreateAsset(tuning, TuningPath);
            }
            tuning.Halo = AssetDatabase.LoadAssetAtPath<Sprite>(HaloPath);
            tuning.Beam = AssetDatabase.LoadAssetAtPath<Sprite>(BeamPath);
            tuning.Sparkle = AssetDatabase.LoadAssetAtPath<Sprite>(SparklePath);
            tuning.AmmoBox = AssetDatabase.LoadAssetAtPath<Sprite>(AmmoPath);
            tuning.LightMaterial = material;
            tuning.HeldArt = AssetDatabase.LoadAssetAtPath<HeldWeaponCatalog>(HeldArtPath);
            tuning.ThrowableArt = Resources.Load<ThrowableArtCatalog>(ThrowableArtCatalog.ResourceName);
            if (tuning.Rarities == null || tuning.Rarities.Length == 0) tuning.Rarities = GroundLootTuning.DefaultRarities();
            EditorUtility.SetDirty(material);
            EditorUtility.SetDirty(tuning);
            // Save only what this tool owns (a global SaveAssets would also rewrite imported materials).
            AssetDatabase.SaveAssetIfDirty(material);
            AssetDatabase.SaveAssetIfDirty(tuning);
            Debug.Log("[Loot] Built ground loot assets: " + TuningPath + (tuning.HeldArt == null ? " (held weapon catalog missing)" : ""));
        }

        // 16 x 14 olive ammo crate with three brass rounds (top row first). The HUD catalog has no AMMO icon.
        private static readonly string[] AmmoBoxArt =
        {
            "...c...c...c....",
            "..cbc.cbc.cbc...",
            "..bbb.bbb.bbb...",
            "..bBb.bBb.bBb...",
            "..bBb.bBb.bBb...",
            ".dddddddddddddd.",
            ".dLLLLLLLLLLLLd.",
            ".dggggggggggggd.",
            ".dggggyyyyggggd.",
            ".dggggyddyggggd.",
            ".dggggyyyyggggd.",
            ".dggggggggggggd.",
            ".dGGGGGGGGGGGGd.",
            ".dddddddddddddd."
        };

        private static readonly (char Key, Color Color)[] AmmoPalette =
        {
            ('c', new Color(.72f, .38f, .2f)), ('b', new Color(.93f, .76f, .3f)), ('B', new Color(.78f, .58f, .2f)),
            ('d', new Color(.17f, .21f, .1f)), ('L', new Color(.62f, .68f, .36f)), ('g', new Color(.42f, .49f, .22f)),
            ('G', new Color(.32f, .38f, .17f)), ('y', new Color(.95f, .82f, .3f))
        };

        private static void WritePixelArt(string path, string[] rows, (char Key, Color Color)[] palette, float ppu)
        {
            int height = rows.Length, width = rows[0].Length;
            WriteTexture(path, width, height, (x, y) =>
            {
                var key = rows[height - 1 - y][x];
                foreach (var entry in palette) if (entry.Key == key) return entry.Color;
                return new Color(0f, 0f, 0f, 0f);
            }, ppu, new Vector2(.5f, .5f), FilterMode.Point);
        }

        private static void WriteSprite(string path, int width, int height, Func<float, float, float> alpha, float ppu, Vector2 pivot) =>
            WriteTexture(path, width, height, (x, y) => new Color(1f, 1f, 1f, alpha((x + .5f) / width, (y + .5f) / height)), ppu, pivot, FilterMode.Bilinear);

        private static void WriteTexture(string path, int width, int height, Func<int, int, Color> pixel, float ppu, Vector2 pivot, FilterMode filter)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
                tex.SetPixel(x, y, pixel(x, y));
            tex.Apply();
            var full = Path.Combine(Directory.GetParent(Application.dataPath).FullName, path);
            var bytes = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);
            if (!File.Exists(full) || !Same(File.ReadAllBytes(full), bytes)) File.WriteAllBytes(full, bytes);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = ppu;
            importer.filterMode = filter;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = pivot;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        private static bool Same(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            for (var i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
            return true;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
