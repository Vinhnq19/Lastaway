using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using JungleMaze.Presentation.Combat;
using JungleMaze.Presentation.World.Phase2;
using UnityEditor;
using UnityEngine;

namespace JungleMaze.EditorTools
{
    /// <summary>
    /// Builds <see cref="ThrowableArtCatalog"/> (WPN-03) from the JSurviv projectile prefabs and weapon sheets:
    /// the flying item, the blast, and what stays behind (banana peel, smoke cloud, seed vines). Nothing in
    /// Art/JSurviv is modified; the catalog only references those sprites.
    /// </summary>
    public static class ThrowableArtBuild
    {
        public const string CatalogPath = "Assets/_JungleMaze/Art/Characters/Resources/ThrowableArtCatalog.asset";
        private const string Sheets = "Assets/_JungleMaze/Art/JSurviv/Weapons/Sprites/";

        /// <summary>Item → JSurviv sources. Blast and linger are sheets/sprites under <see cref="Sheets"/>; null skips.</summary>
        private static readonly (string id, string flight, float flightSize, string blast, float blastSeconds, string linger, bool lingerLoop, float lingerSize)[] Items =
        {
            ("BANANA", "BananaProjectile", .45f, "Chuối/banana_explosing-Sheet.png", .55f, "Chuối/banana_peel.png", false, .7f),
            ("SMOKE", "SmokeProjectile", .45f, null, 0f, "Bom khói/smoke-Sheet.png", true, 7f),
            ("METEORITE_FRAGMENT", "MeteoProjectile", .5f, "meteorite_explosion-Sheet.png", .5f, null, false, 0f),
            ("CRYSTAL", "CrystalProjectile", .45f, "meteorite_explosion-Sheet.png", .5f, null, false, 0f),
            ("SEED", "SeedProjectile", .35f, "Hạt/seed_stun.png", .5f, "Hạt/seed_stun.png", false, .9f),
            ("ROPE", "RopeProjectile", .6f, null, 0f, null, false, 0f),
            ("BELL", "BellProjectile", .55f, null, 0f, null, false, 0f),
            ("SAHUR_BAT", "Sahur_BatProjectile", .7f, null, 0f, null, false, 0f),
        };

        [MenuItem("Jungle Maze/Art/Build Throwable Art")]
        public static void Build()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ThrowableArtCatalog>(CatalogPath);
            if (catalog == null)
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(CatalogPath));
                catalog = ScriptableObject.CreateInstance<ThrowableArtCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }
            var held = AssetDatabase.LoadAssetAtPath<HeldWeaponCatalog>(HeroVisualBuild.WeaponsPath);
            var entries = new List<ThrowableArtCatalog.Entry>();
            foreach (var item in Items)
            {
                var entry = new ThrowableArtCatalog.Entry { ItemId = item.id, FlightSize = item.flightSize, LingerSize = item.lingerSize };
                entry.Flight = Clip(HeldSprite(held, item.id) ?? ProjectileArtBuild.FramesOf(item.flight, true));
                if (item.blast != null) entry.Blast = Clip(Sheet(item.blast, item.blastSeconds, false));
                if (item.linger != null) entry.Linger = Clip(Sheet(item.linger, item.lingerLoop ? .6f : 1f, item.lingerLoop));
                entries.Add(entry);
            }
            catalog.Entries = entries.ToArray();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssetIfDirty(catalog);
            Debug.Log($"[ThrowableArtBuild] {entries.Count} throwable art entries.");
        }

        /// <summary>
        /// The flying item is the same normalized sprite the hand holds (ART-04 icon source), so a thrown banana looks
        /// like the banana in hand. The JSurviv projectile prefabs' clips are generic blobs for several items.
        /// </summary>
        private static WorldVisualSet.Frames HeldSprite(HeldWeaponCatalog held, string itemId)
        {
            if (held == null || !held.TryGet(itemId, out var entry) || entry.Sprite == null) return null;
            return new WorldVisualSet.Frames { Sprites = new[] { entry.Sprite }, Times = new[] { 0f }, Duration = 1f, Loop = true };
        }

        private static ThrowableArtCatalog.Clip Clip(WorldVisualSet.Frames frames) =>
            new ThrowableArtCatalog.Clip { Frames = frames, Content = ProjectileArtBuild.Longest(frames) };

        /// <summary>All sprites sliced from one sheet, in frame order (the trailing number of each slice name).</summary>
        private static WorldVisualSet.Frames Sheet(string relative, float seconds, bool loop)
        {
            var path = Resolve(relative);
            if (!AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().Any()) path = SlicedCopy(path, FramesIn(path));
            var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
                .OrderBy(s => FrameNumber(s.name)).ThenBy(s => s.name).ToArray();
            if (sprites.Length == 0) throw new System.InvalidOperationException("No sprites in " + path);
            var times = new float[sprites.Length];
            for (var i = 0; i < times.Length; i++) times[i] = seconds * i / sprites.Length;
            return new WorldVisualSet.Frames { Sprites = sprites, Times = times, Duration = seconds, Loop = loop };
        }

        /// <summary>
        /// Finds the sheet by file name under the JSurviv weapon sprites. Folder names are Vietnamese and may be stored
        /// in a different Unicode normal form than this source file, so the path is never compared as typed.
        /// </summary>
        private static string Resolve(string relative)
        {
            var file = System.IO.Path.GetFileName(relative);
            foreach (var guid in AssetDatabase.FindAssets(System.IO.Path.GetFileNameWithoutExtension(file), new[] { Sheets.TrimEnd('/') }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileName(path) == file) return path;
            }
            throw new System.InvalidOperationException("Missing JSurviv sheet " + relative);
        }

        private const string Copies = "Assets/_JungleMaze/Art/Combat/Throwables/";

        /// <summary>
        /// Some JSurviv sheets are imported as plain textures for particle materials. Rather than change the source
        /// import, copy the sheet into Art/Combat/Throwables and slice it into square-ish frames along its width.
        /// </summary>
        /// <summary>Frame count of texture-only sheets (they are wider than one square per frame).</summary>
        private static int FramesIn(string path) => System.IO.Path.GetFileName(path) == "banana_explosing-Sheet.png" ? 8 : 0;

        private static string SlicedCopy(string source, int frames)
        {
            System.IO.Directory.CreateDirectory(Copies);
            var target = Copies + System.IO.Path.GetFileName(source);
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(target) == null)
            {
                if (!AssetDatabase.CopyAsset(source, target)) throw new System.InvalidOperationException("Copy failed: " + source);
            }
            var importer = (TextureImporter)AssetImporter.GetAtPath(target);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.isReadable = false;
            // Sheets can be wider than the default 2048 limit; a downscaled sheet would shift every frame rect.
            importer.maxTextureSize = 8192;
            importer.SaveAndReimport();
            importer.GetSourceTextureWidthAndHeight(out var sourceWidth, out var height);
            var count = frames > 0 ? frames : Mathf.Max(1, Mathf.RoundToInt(sourceWidth / (float)height));
            var width = sourceWidth / (float)count;
            importer.spritePixelsPerUnit = height;
            importer.SaveAndReimport();
            var factory = new UnityEditor.U2D.Sprites.SpriteDataProviderFactories();
            factory.Init();
            var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            var name = System.IO.Path.GetFileNameWithoutExtension(target);
            var existing = provider.GetSpriteRects();
            var rects = new UnityEditor.SpriteRect[count];
            // Neighbouring frames bleed a few pixels across the cell edges of these sheets; trim them off.
            var inset = Mathf.Round(width * .07f);
            for (var i = 0; i < count; i++)
                rects[i] = new UnityEditor.SpriteRect
                {
                    name = name + "_" + i, rect = new Rect(i * width + inset, 0, width - inset * 2, height),
                    alignment = SpriteAlignment.Center, pivot = new Vector2(.5f, .5f),
                    // Keep sprite ids stable across rebuilds so the catalog and meta file do not churn.
                    spriteID = i < existing.Length ? existing[i].spriteID : GUID.Generate()
                };
            provider.SetSpriteRects(rects);
            provider.Apply();
            importer.SaveAndReimport();
            return target;
        }

        private static int FrameNumber(string name)
        {
            var match = Regex.Match(name, @"(\d+)$");
            return match.Success ? int.Parse(match.Groups[1].Value) : 0;
        }
    }
}
