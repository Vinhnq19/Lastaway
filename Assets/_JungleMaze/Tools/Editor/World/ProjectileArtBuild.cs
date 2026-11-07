using System.Collections.Generic;
using JungleMaze.Presentation.Combat;
using JungleMaze.Presentation.World.Phase2;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JungleMaze.EditorTools
{
    /// <summary>
    /// Builds <see cref="ProjectileArtCatalog"/> from the JSurviv projectile prefabs each ranged weapon fired, makes
    /// the pooled <see cref="ShotFlashVisual"/> prefab and wires both into the Game scene's combat presenter.
    /// </summary>
    public static class ProjectileArtBuild
    {
        public const string CatalogPath = "Assets/_JungleMaze/Art/Characters/ProjectileArtCatalog.asset";
        public const string FlashPath = "Assets/_JungleMaze/Prefabs/Combat/ShotFlashVisual.prefab";
        private const string Projectiles = "Assets/Imported/JSurviv/Prefabs/Projectiles/";
        private const string GamePath = "Assets/_JungleMaze/Scenes/Game.unity";

        /// <summary>
        /// Weapon → JSurviv projectile prefab and tuning. The shotgun's JSurviv "projectile" is a spreading blast
        /// cone, so it becomes a muzzle flash and the individual pellets stay tinted tracers.
        /// </summary>
        private static readonly (string id, string flight, string muzzle, float angle, float length, float spin, Color tracer, float muzzleLength)[] Weapons =
        {
            ("RUBBER_GUN", "BowProjectile", null, 0f, .28f, 0f, Color.white, 0f),
            ("SCEPTER", "ScepterProjectile", null, 0f, .30f, 0f, Color.white, 0f),
            ("CROSSBOW", "Arrow", null, -45f, .50f, 0f, Color.white, 0f),
            ("SHOTGUN", null, "ShotgunProjectile", 0f, 0f, 0f, new Color(1f, .62f, .22f, 1f), 1.4f),
        };

        [MenuItem("Jungle Maze/Art/Build Projectile Art")]
        public static void Build()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ProjectileArtCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<ProjectileArtCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }
            var entries = new List<ProjectileArtCatalog.Entry>();
            foreach (var w in Weapons)
            {
                var entry = new ProjectileArtCatalog.Entry
                {
                    ItemId = w.id, AngleOffset = w.angle, Length = w.length, Spin = w.spin, TracerColor = w.tracer,
                    MuzzleLength = w.muzzleLength,
                };
                if (w.flight != null)
                {
                    entry.Flight = FramesOf(w.flight, true);
                    entry.FlightContent = Longest(entry.Flight);
                }
                if (w.muzzle != null)
                {
                    entry.Muzzle = FramesOf(w.muzzle, false);
                    entry.Muzzle.Duration *= MuzzleTimeScale;
                    for (var i = 0; i < entry.Muzzle.Times.Length; i++) entry.Muzzle.Times[i] *= MuzzleTimeScale;
                    entry.MuzzleContent = Longest(entry.Muzzle);
                }
                entries.Add(entry);
            }
            catalog.Entries = entries.ToArray();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssetIfDirty(catalog);

            var flash = BuildFlashPrefab();
            WireGame(catalog, flash);
            Debug.Log($"[ProjectileArtBuild] {entries.Count} ranged weapons; Game presenter wired.");
        }

        /// <summary>Frames of the prefab's animator clip, or its single sprite when it has no clip.</summary>
        internal static WorldVisualSet.Frames FramesOf(string prefabName, bool loop)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Projectiles + prefabName + ".prefab");
            if (prefab == null) throw new System.InvalidOperationException("Missing JSurviv projectile " + prefabName);
            var animator = prefab.GetComponentInChildren<Animator>(true);
            if (animator != null && animator.runtimeAnimatorController != null)
                foreach (var clip in animator.runtimeAnimatorController.animationClips)
                {
                    var frames = WorldDemoBuild.ExtractFrames(AssetDatabase.GetAssetPath(clip), loop);
                    if (frames != null) return frames;
                }
            var renderer = prefab.GetComponentInChildren<SpriteRenderer>(true);
            if (renderer == null || renderer.sprite == null) throw new System.InvalidOperationException(prefabName + " has no sprite");
            return new WorldVisualSet.Frames { Sprites = new[] { renderer.sprite }, Times = new[] { 0f }, Duration = 1f, Loop = loop };
        }

        /// <summary>Longest drawn side over all frames (Tight mesh vertices), in sprite units, so a growing clip never outgrows its length.</summary>
        internal static float Longest(WorldVisualSet.Frames frames)
        {
            var longest = 0f;
            foreach (var sprite in frames.Sprites)
            {
                var min = new Vector2(float.MaxValue, float.MaxValue);
                var max = new Vector2(float.MinValue, float.MinValue);
                foreach (var v in sprite.vertices) { min = Vector2.Min(min, v); max = Vector2.Max(max, v); }
                var size = max - min;
                longest = Mathf.Max(longest, Mathf.Max(size.x, size.y));
            }
            return longest;
        }

        /// <summary>Muzzle flashes play faster than the JSurviv clip, which lingered for 0.6 s.</summary>
        private const float MuzzleTimeScale = .4f;

        private static ShotFlashVisual BuildFlashPrefab()
        {
            var root = new GameObject("ShotFlashVisual");
            try
            {
                var art = new GameObject("Sprite");
                art.transform.SetParent(root.transform, false);
                var renderer = art.AddComponent<SpriteRenderer>();
                renderer.enabled = false;
                renderer.sortingOrder = 5;
                var flash = root.AddComponent<ShotFlashVisual>();
                var so = new SerializedObject(flash);
                so.FindProperty("spriteRenderer").objectReferenceValue = renderer;
                so.ApplyModifiedPropertiesWithoutUndo();
                return PrefabUtility.SaveAsPrefabAsset(root, FlashPath).GetComponent<ShotFlashVisual>();
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static void WireGame(ProjectileArtCatalog catalog, ShotFlashVisual flash)
        {
            var active = SceneManager.GetActiveScene();
            var scene = SceneManager.GetSceneByPath(GamePath);
            var opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(GamePath, OpenSceneMode.Additive);
            try
            {
                var wired = 0;
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var presenter in root.GetComponentsInChildren<CombatWeaponPresenter>(true))
                    {
                        var so = new SerializedObject(presenter);
                        so.FindProperty("projectileArt").objectReferenceValue = catalog;
                        so.FindProperty("flashPrefab").objectReferenceValue = flash;
                        so.ApplyModifiedPropertiesWithoutUndo();
                        wired++;
                    }
                if (wired == 0) throw new System.InvalidOperationException("Game scene has no CombatWeaponPresenter.");
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            finally
            {
                if (opened) EditorSceneManager.CloseScene(scene, true);
                if (active.IsValid() && active.isLoaded) SceneManager.SetActiveScene(active);
            }
        }
    }
}
