using System.Collections.Generic;
using JungleMaze.Presentation.Player;
using UnityEditor;
using UnityEngine;

namespace JungleMaze.EditorTools
{
    /// <summary>
    /// Builds <see cref="HeroVisualSet"/> from the JSurviv hero clips (sprite keyframes only) and puts a
    /// <see cref="HeroSpriteAnimator"/> on the Body of the local Hero and the remote PlayerView prefabs.
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

            Wire("Assets/_JungleMaze/Prefabs/Hero.prefab", set);
            Wire("Assets/_JungleMaze/Prefabs/PlayerView.prefab", set);
            Debug.Log($"[HeroVisualBuild] {heroes.Count} heroes; Hero and PlayerView bodies animated.");
        }

        private static void Wire(string prefabPath, HeroVisualSet set)
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
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
}
