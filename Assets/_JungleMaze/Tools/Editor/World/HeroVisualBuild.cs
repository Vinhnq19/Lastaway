using System.Collections.Generic;
using JungleMaze.Presentation.Combat;
using JungleMaze.Presentation.Player;
using UnityEditor;
using UnityEngine;

namespace JungleMaze.EditorTools
{
    /// <summary>
    /// Builds <see cref="HeroVisualSet"/> from the JSurviv hero clips (sprite keyframes only) and puts a
    /// <see cref="HeroSpriteAnimator"/> on the Body of the local Hero and the remote PlayerView prefabs. Also builds
    /// <see cref="HeldWeaponCatalog"/> and adds a <see cref="HeldWeaponView"/> hand to both prefabs.
    /// </summary>
    public static class HeroVisualBuild
    {
        public const string SetPath = "Assets/_JungleMaze/Art/Characters/HeroVisualSet.asset";
        private const string Clips = "Assets/_JungleMaze/Art/JSurviv/Characters/Heroes/Animations/hvu_Player/";
        /// <summary>Hero_14 first (local default). Hero_1 is left out: it is a League of Legends character.</summary>
        private static readonly int[] Order = { 14, 10, 11, 2, 4, 5, 6, 7, 8 };

        [MenuItem("Jungle Maze/Art/Build Hero Visuals")]
        public static void Build()
        {
            var set = AssetDatabase.LoadAssetAtPath<HeroVisualSet>(SetPath);
            if (set == null)
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(SetPath));
                set = ScriptableObject.CreateInstance<HeroVisualSet>();
                AssetDatabase.CreateAsset(set, SetPath);
            }
            var heroes = new List<HeroVisualSet.Hero>();
            foreach (var n in Order)
            {
                string Clip(string state, string facing) => $"{Clips}Hero_{n}/{state}/{n}_{state}_{facing}.anim";
                var hero = new HeroVisualSet.Hero
                {
                    Name = "Hero_" + n,
                    IdleBack = WorldDemoBuild.ExtractFrames(Clip("idle", "back"), true),
                    IdleSide = WorldDemoBuild.ExtractFrames(Clip("idle", "side"), true),
                    IdleFront = WorldDemoBuild.ExtractFrames(Clip("idle", "front"), true),
                    MoveBack = WorldDemoBuild.ExtractFrames(Clip("move", "back"), true),
                    MoveSide = WorldDemoBuild.ExtractFrames(Clip("move", "side"), true),
                    MoveFront = WorldDemoBuild.ExtractFrames(Clip("move", "front"), true),
                    AttackBack = WorldDemoBuild.ExtractFrames(Clip("attack", "back"), false),
                    AttackSide = WorldDemoBuild.ExtractFrames(Clip("attack", "side"), false),
                    AttackFront = WorldDemoBuild.ExtractFrames(Clip("attack", "front"), false),
                };
                if (hero.IdleFront == null || hero.IdleSide == null || hero.MoveSide == null)
                {
                    Debug.LogWarning("[HeroVisualBuild] Skipped " + hero.Name + ": idle/move clips missing.");
                    continue;
                }
                heroes.Add(hero);
            }
            if (heroes.Count == 0) throw new System.InvalidOperationException("No hero clips found under " + Clips);
            set.Heroes = heroes.ToArray();
            EditorUtility.SetDirty(set);
            AssetDatabase.SaveAssetIfDirty(set);

            var weapons = BuildWeapons();
            Wire("Assets/_JungleMaze/Prefabs/Hero.prefab", set, weapons, true);
            Wire("Assets/_JungleMaze/Prefabs/PlayerView.prefab", set, weapons, false);
            Debug.Log($"[HeroVisualBuild] {heroes.Count} heroes, {weapons.Entries.Length} held weapons; Hero and PlayerView wired.");
        }

        public const string WeaponsPath = "Assets/_JungleMaze/Art/Characters/HeldWeaponCatalog.asset";
        private const string Inventory = "Docs/Tickets/ART-04_icon_inventory.csv";

        /// <summary>
        /// Per weapon: degrees that turn the drawn handle→tip axis onto +X, held point (0..1 from the sprite's
        /// bottom-left) and world length of the longest side. Tuned by eye against each JSurviv sprite.
        /// </summary>
        private static readonly Dictionary<string, (float angle, Vector2 grip, float length)> Tuning = new Dictionary<string, (float, Vector2, float)>
        {
            { "TREE_BRANCH", (-50f, new Vector2(.10f, .05f), .70f) },
            { "KNIFE", (0f, new Vector2(.12f, .50f), .60f) },
            { "BONE", (-45f, new Vector2(.12f, .12f), .60f) },
            { "AXE", (-135f, new Vector2(.92f, .08f), .65f) },
            { "MACE", (-135f, new Vector2(.90f, .08f), .65f) },
            { "RUBBER_GUN", (-90f, new Vector2(.50f, .08f), .60f) },
            { "SCEPTER", (-90f, new Vector2(.50f, .05f), .65f) },
            { "CROSSBOW", (0f, new Vector2(.35f, .50f), .60f) },
            { "SHOTGUN", (0f, new Vector2(.25f, .45f), .70f) },
            { "BANANA", (0f, new Vector2(.50f, .50f), .40f) },
            { "SMOKE", (0f, new Vector2(.50f, .50f), .35f) },
            { "METEORITE_FRAGMENT", (0f, new Vector2(.50f, .50f), .35f) },
            { "CRYSTAL", (0f, new Vector2(.50f, .50f), .40f) },
            { "SEED", (0f, new Vector2(.50f, .50f), .25f) },
            { "ROPE", (0f, new Vector2(.10f, .50f), .60f) },
            { "BELL", (0f, new Vector2(.50f, .50f), .40f) },
            { "SAHUR_BAT", (-55f, new Vector2(.12f, .08f), .72f) },
        };

        /// <summary>Reads each weapon's source sprite and slice from the ART-04 inventory and applies <see cref="Tuning"/>.</summary>
        private static HeldWeaponCatalog BuildWeapons()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<HeldWeaponCatalog>(WeaponsPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<HeldWeaponCatalog>();
                AssetDatabase.CreateAsset(catalog, WeaponsPath);
            }
            var entries = new List<HeldWeaponCatalog.Entry>();
            var lines = System.IO.File.ReadAllLines(Inventory);
            for (var i = 1; i < lines.Length; i++)
            {
                var cells = lines[i].Split(',');
                if (cells.Length < 8 || cells[1] != "Weapon" || !Tuning.TryGetValue(cells[2], out var tune)) continue;
                var slice = cells[7].Trim('"');
                var cut = slice.IndexOf("..", System.StringComparison.Ordinal);
                if (cut >= 0) slice = slice.Substring(0, cut);
                cut = slice.IndexOf(" (", System.StringComparison.Ordinal);
                if (cut >= 0) slice = slice.Substring(0, cut);
                slice = slice.Trim();
                if (slice == "full image") slice = "";
                Sprite sprite = null, first = null;
                foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(cells[6]))
                {
                    if (!(asset is Sprite candidate)) continue;
                    if (first == null) first = candidate;
                    if (slice.Length > 0 && candidate.name == slice) { sprite = candidate; break; }
                }
                sprite = sprite != null ? sprite : first;
                if (sprite == null) { Debug.LogWarning("[HeroVisualBuild] No sprite for " + cells[2] + ": " + cells[6]); continue; }
                var min = new Vector2(float.MaxValue, float.MaxValue);
                var max = new Vector2(float.MinValue, float.MinValue);
                foreach (var v in sprite.vertices) { min = Vector2.Min(min, v); max = Vector2.Max(max, v); }
                entries.Add(new HeldWeaponCatalog.Entry
                {
                    ItemId = cells[2], Sprite = sprite, AngleOffset = tune.angle, Grip = tune.grip, Length = tune.length,
                    ContentMin = min, ContentSize = max - min,
                });
            }
            catalog.Entries = entries.ToArray();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssetIfDirty(catalog);
            return catalog;
        }

        private static void Wire(string prefabPath, HeroVisualSet set, HeldWeaponCatalog weapons, bool local)
        {
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var body = root.transform.Find("Body");
                if (body == null || !body.TryGetComponent<SpriteRenderer>(out var renderer))
                    throw new System.InvalidOperationException(prefabPath + " has no Body SpriteRenderer.");
                if (!body.TryGetComponent<HeroSpriteAnimator>(out var animator)) animator = body.gameObject.AddComponent<HeroSpriteAnimator>();
                var so = new SerializedObject(animator);
                so.FindProperty("visuals").objectReferenceValue = set;
                so.FindProperty("heroIndex").intValue = 0;
                so.ApplyModifiedPropertiesWithoutUndo();
                var first = set.Heroes[0].IdleFront.Sample(0);
                renderer.sprite = first;
                renderer.color = Color.white;
                var k = set.FrameWorldHeight / first.bounds.size.y;
                body.localScale = new Vector3(k, k, 1);

                var hand = root.transform.Find("Weapon");
                if (hand == null) { hand = new GameObject("Weapon").transform; hand.SetParent(root.transform, false); }
                var art = hand.Find("Sprite");
                if (art == null) { art = new GameObject("Sprite").transform; art.SetParent(hand, false); }
                if (!art.TryGetComponent<SpriteRenderer>(out var weaponRenderer)) weaponRenderer = art.gameObject.AddComponent<SpriteRenderer>();
                weaponRenderer.sortingLayerID = renderer.sortingLayerID;
                weaponRenderer.sortingOrder = renderer.sortingOrder + 1;
                weaponRenderer.enabled = false;
                if (!hand.TryGetComponent<HeldWeaponView>(out var held)) held = hand.gameObject.AddComponent<HeldWeaponView>();
                var heldSo = new SerializedObject(held);
                heldSo.FindProperty("catalog").objectReferenceValue = weapons;
                heldSo.FindProperty("weaponRenderer").objectReferenceValue = weaponRenderer;
                heldSo.FindProperty("body").objectReferenceValue = renderer;
                heldSo.FindProperty("bodyAnimator").objectReferenceValue = animator;
                heldSo.FindProperty("followLocalHud").boolValue = local;
                heldSo.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
}
