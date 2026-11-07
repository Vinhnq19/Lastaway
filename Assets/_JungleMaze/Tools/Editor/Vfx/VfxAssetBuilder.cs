using System;
using System.IO;
using System.Text;
using JungleMaze.Domain.CombatFeedback;
using JungleMaze.Domain.Vfx;
using JungleMaze.Presentation.Vfx;
using TMPro;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace JungleMaze.EditorTools
{
    /// <summary>
    /// Repeatable build of the VFX-01 assets (plan 4.12.4): the geometric fuse sprites, the nine prefabs in
    /// <c>Prefabs/Vfx</c> (copies of the JSurviv/Epic Toon sources, cleaned by <see cref="VfxPrefabSanitizer"/> and
    /// fitted to the 48 px cell), the catalog and the runtime set. Sources in <c>Assets/Imported/</c> are only read.
    /// Rebuilding overwrites the outputs in place, so GUIDs and references stay.
    /// </summary>
    public static class VfxAssetBuilder
    {
        public const string PrefabFolder = "Assets/_JungleMaze/Prefabs/Vfx";
        public const string ArtFolder = "Assets/_JungleMaze/Art/Vfx";
        public const string SpriteFolder = ArtFolder + "/Sprites";
        public const string CatalogPath = ArtFolder + "/VfxCatalog.asset";
        public const string RuntimeSetPath = ArtFolder + "/Resources/VfxRuntimeSet.asset";
        public const string FramePath = SpriteFolder + "/vfx_area_frame.png";
        public const string FillPath = SpriteFolder + "/vfx_area_fill.png";
        public const string MaterialFolder = ArtFolder + "/Materials";
        private const string ToonMaterials = "Assets/Imported/JSurviv/Epic Toon FX/Materials/";

        private const string Jsurviv = "Assets/Imported/JSurviv/Prefabs/VFX/";
        private const string Toon2D = "Assets/Imported/JSurviv/Epic Toon FX/Prefabs 2D/Explosions/";
        private const string NumberFont = "Assets/_JungleMaze/Art/Fonts/TMP/HudNumber SDF.asset";
        private const string Body = "Default", Warning = "VFX";
        private const int AuraOrder = 100, SparkOrder = 105, NumberOrder = 110, FuseOrder = 10;

        [MenuItem("Jungle Maze/VFX/Build VFX-01 Assets")]
        public static void BuildAll()
        {
            var report = new StringBuilder();
            EnsureFolder(PrefabFolder);
            EnsureFolder(SpriteFolder);
            EnsureFolder(ArtFolder + "/Resources");
            EnsureFolder(MaterialFolder);
            BuildSprites();
            var spark = Build("Vfx_Hit_Spark", Toon2D + "SparkExplosion2D.prefab", .25f, Body, SparkOrder, report, FitSpark);
            var puff = Build("Vfx_Attack_Puff", Toon2D + "SparkExplosion2D.prefab", .15f, Body, SparkOrder, report, FitPuff);
            var stun = Build("Vfx_Status_Stun", Jsurviv + "StunnedVfx.prefab", .6f, Body, AuraOrder, report, FitStun);
            var reverse = Build("Vfx_Status_Reverse", Jsurviv + "ReverseVfx.prefab", 1f, Body, AuraOrder, report, FitReverse);
            var shield = Build("Vfx_Status_Shield", Jsurviv + "ImmortalVfx.prefab", .45f, Body, AuraOrder, report, FitShield);
            var invisible = Build("Vfx_Status_Invisible", Jsurviv + "ImmortalVfx.prefab", .55f, Body, AuraOrder, report, FitInvisible);
            var poison = Build("Vfx_Status_Poison", Jsurviv + "PoisonsVfx.prefab", .45f, Body, AuraOrder, report, FitPoison);
            var number = BuildNumber();
            var fuse = BuildFuse();

            var catalog = LoadOrCreate<VfxCatalog>(CatalogPath);
            catalog.Set(new[]
            {
                Cue(VfxCue.HitSpark, spark, 8, VfxPriority.Low, .4f, false, Body, new Vector2(0f, .3f)),
                Cue(VfxCue.DamageNumber, number, 8, VfxPriority.Low, 0f, false, Body, new Vector2(0f, .6f)),
                Cue(VfxCue.AttackPuff, puff, 6, VfxPriority.Low, .25f, false, Body, Vector2.zero),
                Cue(VfxCue.FuseWarning, fuse, 4, VfxPriority.Critical, 0f, true, Warning, Vector2.zero),
                Cue(VfxCue.AuraStun, stun, 2, VfxPriority.Medium, 0f, true, Body, new Vector2(0f, .75f)),
                Cue(VfxCue.AuraReverse, reverse, 2, VfxPriority.Medium, 0f, true, Body, new Vector2(0f, .55f)),
                Cue(VfxCue.AuraCcImmune, shield, 2, VfxPriority.Medium, 0f, true, Body, Vector2.zero),
                Cue(VfxCue.AuraInvulnerable, shield, 2, VfxPriority.Medium, 0f, true, Body, Vector2.zero),
                Cue(VfxCue.AuraInvisible, invisible, 2, VfxPriority.Medium, 0f, true, Body, Vector2.zero),
                Cue(VfxCue.AuraPoison, poison, 2, VfxPriority.Medium, 0f, true, Body, new Vector2(0f, .1f))
            }, new[]
            {
                Status(StatusKind.Stun, VfxCue.AuraStun),
                Status(StatusKind.Reverse, VfxCue.AuraReverse),
                Status(StatusKind.CcImmune, VfxCue.AuraCcImmune, 5),
                Status(StatusKind.Invulnerable, VfxCue.AuraInvulnerable, 4),
                Status(StatusKind.Invisible, VfxCue.AuraInvisible),
                Status(StatusKind.Poison, VfxCue.AuraPoison),
                // Known, deliberately without a VFX-01 visual: vines (ThrowableView), swamp slow, adrenaline (Q9).
                Status(StatusKind.Root, VfxCue.None),
                Status(StatusKind.Slow, VfxCue.None),
                Status(StatusKind.Adrenaline, VfxCue.None)
            }, new[]
            {
                Color.white,                     // 0: unused (prefab colours)
                Color.white,                     // 1: number dealt by me
                new Color(1f, .36f, .3f, 1f),    // 2: number taken by me
                new Color(.82f, .82f, .82f, 1f), // 3: number between others
                new Color(1f, .82f, .25f, 1f),   // 4: invulnerable (gold)
                new Color(1f, 1f, 1f, 1f)        // 5: CC immune (white, Equipments 13)
            });
            EditorUtility.SetDirty(catalog);

            var set = LoadOrCreate<VfxRuntimeSet>(RuntimeSetPath);
            set.Set(catalog);
            EditorUtility.SetDirty(set);
            // Save only what this tool owns: a global SaveAssets would also write imported materials that URP
            // re-validates on load, and Assets/Imported/ must stay untouched (AC1).
            AssetDatabase.SaveAssetIfDirty(catalog);
            AssetDatabase.SaveAssetIfDirty(set);
            foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { MaterialFolder }))
                AssetDatabase.SaveAssetIfDirty(AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid)));
            Debug.Log("[VFX] Built VFX-01 assets.\n" + report);
        }

        private static VfxCatalog.CueEntry Cue(VfxCue cue, VfxParticleEffect prefab, int prewarm, VfxPriority priority, float lifetime,
            bool persistent, string layer, Vector2 offset) => new VfxCatalog.CueEntry
        {
            cue = cue, prefab = prefab, prewarm = prewarm, priority = priority, lifetimeSeconds = lifetime, persistent = persistent,
            sortingLayer = layer, anchorOffset = offset
        };

        private static VfxCatalog.StatusEntry Status(StatusKind kind, VfxCue cue, byte tint = 0) =>
            new VfxCatalog.StatusEntry { kind = kind, cue = cue, tint = tint };

        // ---- prefab recipes (run before the sanitizer) ----

        private static VfxParticleEffect Build(string name, string source, float fit, string layer, int order, StringBuilder report,
            Action<GameObject> recipe)
        {
            var sourceAsset = AssetDatabase.LoadAssetAtPath<GameObject>(source);
            if (sourceAsset == null) throw new InvalidOperationException("Missing VFX source " + source);
            var root = new GameObject(name);
            try
            {
                var holder = new GameObject("Fx");
                holder.transform.SetParent(root.transform, false);
                holder.transform.localScale = new Vector3(fit, fit, 1f);
                // A plain clone: no prefab link to the imported asset (plan 4.5 step 1, copy not variant).
                var copy = (GameObject)Object.Instantiate(sourceAsset, holder.transform, false);
                copy.name = sourceAsset.name;
                copy.transform.localPosition = Vector3.zero;
                copy.transform.localRotation = Quaternion.identity;
                copy.transform.localScale = Vector3.one;
                foreach (var ps in copy.GetComponentsInChildren<ParticleSystem>(true))
                    if (ps.gameObject != copy) ps.transform.localPosition = Vector3.zero;
                recipe(copy);
                Brighten(root);
                report.AppendLine(name + " ← " + source + "\n" + VfxPrefabSanitizer.Sanitize(root, layer, order));
                return Save(root, name);
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static VfxParticleEffect Save(GameObject root, string name)
        {
            var path = PrefabFolder + "/" + name + ".prefab";
            var saved = PrefabUtility.SaveAsPrefabAsset(root, path, out var ok);
            if (!ok || saved == null) throw new InvalidOperationException("Could not save " + path);
            return saved.GetComponent<VfxParticleEffect>();
        }

        private static ParticleSystem Find(GameObject root, string name)
        {
            foreach (var ps in root.GetComponentsInChildren<ParticleSystem>(true))
                if (ps.name == name) return ps;
            return null;
        }

        private static void Remove(GameObject root, string name)
        {
            var ps = Find(root, name);
            if (ps != null) Object.DestroyImmediate(ps.gameObject);
        }

        private static void Life(ParticleSystem ps, float lifetime, float speed)
        {
            var main = ps.main;
            main.startLifetime = lifetime;
            main.startSpeed = speed;
        }

        /// <summary>≈ 0.6 cell, ≤ 0.4 s: short glow + a few sparks that stay near the body.</summary>
        private static void FitSpark(GameObject copy)
        {
            var sparks = copy.GetComponent<ParticleSystem>();
            Life(sparks, .3f, 6f);
            var glow = Find(copy, "Glow");
            if (glow != null) { var m = glow.main; m.startLifetime = .2f; }
            foreach (var ps in copy.GetComponentsInChildren<ParticleSystem>(true)) { var m = ps.main; m.duration = .4f; m.loop = false; }
        }

        /// <summary>≈ 0.4 cell, ≤ 0.25 s, pale dust.</summary>
        private static void FitPuff(GameObject copy)
        {
            var sparks = copy.GetComponent<ParticleSystem>();
            Life(sparks, .2f, 5f);
            var glow = Find(copy, "Glow");
            if (glow != null) { var m = glow.main; m.startLifetime = .15f; m.startColor = new Color(1f, 1f, 1f, .55f); }
            foreach (var ps in copy.GetComponentsInChildren<ParticleSystem>(true))
            {
                var m = ps.main;
                m.duration = .25f;
                m.loop = false;
                if (ps != glow) m.startColor = new Color(.95f, .92f, .82f, .85f);
            }
        }

        /// <summary>Ring + orbiting stars above the head; the ETFX rotation script goes, the ring spins by itself.</summary>
        private static void FitStun(GameObject copy)
        {
            var ring = copy.GetComponent<ParticleSystem>();
            var rot = ring.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(Mathf.PI * 2f);
            var ringMain = ring.main;
            ringMain.startColor = new Color(1f, .92f, .4f, 1f);
            foreach (var ps in copy.GetComponentsInChildren<ParticleSystem>(true)) { var m = ps.main; m.loop = true; m.simulationSpace = ParticleSystemSimulationSpace.Local; }
            var stars = Find(copy, "Stars");
            if (stars != null)
            {
                var m = stars.main;
                m.startSize = .45f;
                m.startSpeed = 0f;
                m.startColor = new Color(1f, .95f, .2f, 1f);
                // Stars circle the head instead of flying away.
                var v = stars.velocityOverLifetime;
                v.enabled = true;
                v.space = ParticleSystemSimulationSpace.Local;
                v.orbitalZ = new ParticleSystem.MinMaxCurve(4f);
                var r = stars.GetComponent<ParticleSystemRenderer>();
                var add = AssetDatabase.LoadAssetAtPath<Material>(ToonMaterials + "Misc/star_ADD.mat");
                if (r != null && add != null) r.sharedMaterial = add;
            }
        }

        /// <summary>Orange swirl over the head (different from the white/gold shield and the pale invisible sparkles).</summary>
        private static void FitReverse(GameObject copy)
        {
            var orange = new Color(1f, .55f, .1f, 1f);
            foreach (var ps in copy.GetComponentsInChildren<ParticleSystem>(true))
            {
                var m = ps.main;
                m.loop = true;
                m.simulationSpace = ParticleSystemSimulationSpace.Local;
                m.startColor = orange;
            }
            var swirl = copy.GetComponent<ParticleSystem>();
            var sm = swirl.main;
            sm.startSize = .22f;
            var se = swirl.emission;
            se.rateOverTime = 14f;
        }

        /// <summary>Flat magic ring (billboard instead of the 3D mesh) + sparkles; tinted gold or white by the catalog.</summary>
        private static void FitShield(GameObject copy)
        {
            // The source ring texture is gold; the neutral ring of the stun source lets the catalog tint it gold or white.
            var stunSource = AssetDatabase.LoadAssetAtPath<GameObject>(Jsurviv + "StunnedVfx.prefab");
            var stunRenderer = stunSource != null ? stunSource.GetComponent<ParticleSystemRenderer>() : null;
            var neutral = stunRenderer != null ? stunRenderer.sharedMaterial : null;
            var ringRenderer = copy.GetComponent<ParticleSystemRenderer>();
            if (neutral != null && ringRenderer != null) ringRenderer.sharedMaterial = neutral;
            foreach (var ps in copy.GetComponentsInChildren<ParticleSystem>(true))
            {
                var m = ps.main;
                m.loop = true;
                m.simulationSpace = ParticleSystemSimulationSpace.Local;
                m.startColor = Color.white;
                WhiteOverLifetime(ps);
            }
        }

        /// <summary>Keeps the fade (alpha keys) of colour-over-lifetime but drops its hue, so the catalog tint decides the colour.</summary>
        private static void WhiteOverLifetime(ParticleSystem ps)
        {
            var col = ps.colorOverLifetime;
            if (!col.enabled) return;
            var source = col.color.gradient;
            if (source == null) { col.enabled = false; return; }
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, source.alphaKeys);
            col.color = new ParticleSystem.MinMaxGradient(g);
        }

        /// <summary>Only the sparkles of the shield source, faint and pale blue: no ring, so it never reads as a shield.</summary>
        private static void FitInvisible(GameObject copy)
        {
            var ring = copy.GetComponent<ParticleSystem>();
            if (ring != null)
            {
                var r = copy.GetComponent<ParticleSystemRenderer>();
                if (r != null) Object.DestroyImmediate(r);
                Object.DestroyImmediate(ring);
            }
            foreach (var ps in copy.GetComponentsInChildren<ParticleSystem>(true))
            {
                var m = ps.main;
                m.loop = true;
                m.simulationSpace = ParticleSystemSimulationSpace.Local;
                m.startColor = new Color(.75f, .92f, 1f, .75f);
                var e = ps.emission;
                e.rateOverTime = 16f;
            }
        }

        /// <summary>Green cloud + droplets around the body; the skull (off style) goes, the burst becomes a slow loop.</summary>
        private static void FitPoison(GameObject copy)
        {
            Remove(copy, "Skull");
            foreach (var ps in copy.GetComponentsInChildren<ParticleSystem>(true))
            {
                var m = ps.main;
                m.loop = true;
                m.duration = 1f;
                m.simulationSpace = ParticleSystemSimulationSpace.Local;
                var e = ps.emission;
                e.SetBursts(Array.Empty<ParticleSystem.Burst>());
            }
            var cloud = copy.GetComponent<ParticleSystem>();
            Life(cloud, 1f, .5f);
            var cm = cloud.main;
            cm.startSize = 1.6f;
            var ce = cloud.emission;
            ce.rateOverTime = 4f;
            var drops = Find(copy, "Droplets");
            if (drops != null)
            {
                Life(drops, .35f, 2.5f);
                var de = drops.emission;
                de.rateOverTime = 5f;
            }
        }

        /// <summary>
        /// The Epic Toon materials carry base colour 0.5/0.5 (made for the legacy doubled particle shaders), so in URP
        /// Particles/Unlit they read at half strength. Each one used here is copied once into Art/Vfx/Materials with a
        /// white base colour (plan 4.5 step 3: copy, never edit the imported material).
        /// </summary>
        private static void Brighten(GameObject root)
        {
            foreach (var r in root.GetComponentsInChildren<ParticleSystemRenderer>(true))
            {
                var mats = r.sharedMaterials;
                for (var i = 0; i < mats.Length; i++) mats[i] = BrightCopy(mats[i]);
                r.sharedMaterials = mats;
                if (r.trailMaterial != null) r.trailMaterial = BrightCopy(r.trailMaterial);
            }
        }

        private static Material BrightCopy(Material source)
        {
            if (source == null) return null;
            var path = AssetDatabase.GetAssetPath(source);
            if (!path.StartsWith("Assets/Imported/")) return source;
            var target = MaterialFolder + "/" + source.name.Replace(" ", "_").Replace("(", "").Replace(")", "") + "_vfx.mat";
            var copy = AssetDatabase.LoadAssetAtPath<Material>(target);
            if (copy == null)
            {
                copy = new Material(source);
                AssetDatabase.CreateAsset(copy, target);
            }
            else copy.CopyPropertiesFromMaterial(source);
            if (copy.HasProperty("_BaseColor")) copy.SetColor("_BaseColor", Color.white);
            EditorUtility.SetDirty(copy);
            return copy;
        }

        // ---- new prefabs ----

        private static VfxParticleEffect BuildNumber()
        {
            var root = new GameObject("Vfx_DamageNumber");
            try
            {
                root.AddComponent<VfxParticleEffect>();
                var labelGo = new GameObject("Label", typeof(RectTransform));
                labelGo.transform.SetParent(root.transform, false);
                var label = labelGo.AddComponent<TextMeshPro>();
                label.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(NumberFont);
                label.fontSize = 4.5f;
                label.alignment = TextAlignmentOptions.Center;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.raycastTarget = false;
                label.text = "0";
                ((RectTransform)labelGo.transform).sizeDelta = new Vector2(2f, 1f);
                var number = root.AddComponent<DamageNumberVfx>();
                var so = new SerializedObject(number);
                so.FindProperty("label").objectReferenceValue = label;
                so.ApplyModifiedPropertiesWithoutUndo();
                VfxPrefabSanitizer.Sanitize(root, Body, NumberOrder);
                return Save(root, "Vfx_DamageNumber");
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static VfxParticleEffect BuildFuse()
        {
            var root = new GameObject("Vfx_FuseWarning");
            try
            {
                root.AddComponent<VfxParticleEffect>();
                var fill = Sprite("Fill", root.transform, AssetDatabase.LoadAssetAtPath<Sprite>(FillPath));
                var frame = Sprite("Frame", root.transform, AssetDatabase.LoadAssetAtPath<Sprite>(FramePath));
                var fuse = root.AddComponent<FuseWarningVfx>();
                var so = new SerializedObject(fuse);
                so.FindProperty("frame").objectReferenceValue = frame;
                so.FindProperty("fill").objectReferenceValue = fill;
                so.ApplyModifiedPropertiesWithoutUndo();
                VfxPrefabSanitizer.Sanitize(root, Warning, FuseOrder);
                fill.sortingOrder = FuseOrder - 1;
                return Save(root, "Vfx_FuseWarning");
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static SpriteRenderer Sprite(string name, Transform parent, Sprite sprite)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = sprite;
            r.drawMode = SpriteDrawMode.Sliced;
            r.size = new Vector2(3f, 3f);
            return r;
        }

        // ---- geometry sprites (single-colour shapes made by the tool, not art) ----

        private static void BuildSprites()
        {
            // 32 px frame, 3 px border, 9-slice border 4 px, 32 px per unit = 1 cell.
            WriteSprite(FramePath, 32, (x, y) => x < 3 || y < 3 || x >= 29 || y >= 29, 32, new Vector4(4, 4, 4, 4));
            WriteSprite(FillPath, 8, (x, y) => true, 8, Vector4.zero);
        }

        private static void WriteSprite(string path, int size, Func<int, int, bool> on, float ppu, Vector4 border)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
                tex.SetPixel(x, y, on(x, y) ? Color.white : new Color(1f, 1f, 1f, 0f));
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
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.spriteBorder = border;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        private static bool Same(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            for (var i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
            return true;
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
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
