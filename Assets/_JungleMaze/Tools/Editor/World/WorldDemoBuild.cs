using System.Collections.Generic;
using System.IO;
using JungleMaze.Presentation.World.Phase2;
using JungleMaze.Presentation.World;
using JungleMaze.Presentation.Combat;
using JungleMaze.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JungleMaze.EditorTools
{
    public static class WorldDemoBuild
    {
        [MenuItem("Jungle Maze/Phase 2/Build World Demo")]
        public static void Build()
        {
            BuildVisualAssets();
            var runtime="Assets/StreamingAssets/JungleMaze/runtime";Directory.CreateDirectory(runtime);
            foreach(var file in new[]{"world_settings.json","items_catalog.json"})File.Copy("Backend/Shared/GameData/"+file,runtime+"/"+file,true);
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var camera=new GameObject("Main Camera").AddComponent<Camera>();camera.tag="MainCamera";camera.orthographic=true;camera.orthographicSize=10;camera.transform.position=new Vector3(25,25,-10);camera.backgroundColor=new Color(.08f,.13f,.1f);
            var light=new GameObject("Main Light").AddComponent<Light>();light.type=LightType.Directional;
            var demo=new GameObject("World Prototype").AddComponent<WorldDemoController>();ConfigureGameVisuals(demo);
            var hud=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_JungleMaze/Prefabs/UI/Hud.prefab");if(hud!=null)PrefabUtility.InstantiatePrefab(hud);
            var note=new GameObject("Prototype instructions").AddComponent<TextMesh>();note.text="OFFLINE WORLD PREVIEW\nWASD move · F pickup · 1-4 select · 5-8 use\nLeft click attack · R reload · Backspace drop\nSpace debug hit · H debug damage";note.characterSize=.11f;note.fontSize=28;note.anchor=TextAnchor.UpperLeft;note.transform.position=new Vector3(1,49,0);note.color=Color.white;
            EditorSceneManager.SaveScene(scene,"Assets/_JungleMaze/Scenes/WorldDemo.unity");AssetDatabase.SaveAssets();
        }

        /// <summary>Read Game's existing presenter references; never save or modify the Game scene.</summary>
        public static void ConfigureGameVisuals(WorldDemoController demo)
        {
            var previous=SceneManager.GetActiveScene();var game=SceneManager.GetSceneByPath("Assets/_JungleMaze/Scenes/Game.unity");
            var opened=!game.isLoaded;
            if(opened)game=EditorSceneManager.OpenScene("Assets/_JungleMaze/Scenes/Game.unity",OpenSceneMode.Additive);
            try
            {
                CombatWeaponPresenter source=null;
                foreach(var root in game.GetRootGameObjects())
                {source=root.GetComponentInChildren<CombatWeaponPresenter>(true);if(source!=null)break;}
                if(source==null)throw new System.InvalidOperationException("Game combat presenter is missing.");
                var serialized=new SerializedObject(source);
                var projectile=serialized.FindProperty("projectilePrefab").objectReferenceValue as CoverProbeProjectile;
                var swing=serialized.FindProperty("swingPrefab").objectReferenceValue as MeleeSwingVisual;
                var array=serialized.FindProperty("weaponPrefabs");var weapons=new GameObject[array.arraySize];
                for(var i=0;i<weapons.Length;i++)weapons[i]=array.GetArrayElementAtIndex(i).objectReferenceValue as GameObject;
                var hero=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_JungleMaze/Prefabs/Hero.prefab");
                if(hero==null||projectile==null||swing==null)throw new System.InvalidOperationException("Game visual assets are missing.");
                demo.ConfigureVisualSources(hero.GetComponentInChildren<SpriteRenderer>(true),projectile,swing,weapons);
                EditorUtility.SetDirty(demo);
            }
            finally
            {
                if(opened)EditorSceneManager.CloseScene(game,true);
                if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);
            }
        }

        /// <summary>Rebuilds sprite-only world assets without replacing the active scene.</summary>
        [MenuItem("Jungle Maze/Phase 2/Build World Visual Assets")]
        public static void BuildVisualAssets()
        {
            var resources="Assets/_JungleMaze/Art/World/Resources";Directory.CreateDirectory(resources);
            var prefabDir="Assets/_JungleMaze/Prefabs/World";Directory.CreateDirectory(prefabDir);
            AssetDatabase.Refresh();
            var set=AssetDatabase.LoadAssetAtPath<WorldVisualSet>(resources+"/WorldVisualSet.asset");
            if(set==null){set=ScriptableObject.CreateInstance<WorldVisualSet>();AssetDatabase.CreateAsset(set,resources+"/WorldVisualSet.asset");}
            var entries=new List<WorldVisualSet.Entry>();var report=new System.Text.StringBuilder("# ART-03 source audit\n\nSprite references and timed sprite keyframes only. No legacy scripts, controllers, float curves or animation events are copied. Damage flashes require replicated HP decrease; broken state requires the server flag. Portal teleport cues require a confirmed source event and are not inferred from proximity.\n\n| Kind | Source | Status |\n|---|---|---|\n");
            var kinds=new[]{WorldObjectKind.Chest,WorldObjectKind.Pot,WorldObjectKind.Bush,WorldObjectKind.Swamp,WorldObjectKind.Lava,WorldObjectKind.Portal,WorldObjectKind.Door,WorldObjectKind.Poison,WorldObjectKind.Supply};
            var paths=new[]{"Destructibles/CHEST1","Destructibles/Bình Hoa","Obstacles/CanGoThrough/Bush","Obstacles/CanGoThrough/POND","Indestructibles/LAVA","Obstacles/CanGoThrough/PORTAL","","Obstacles/CanGoThrough/HUNT_TRAP","Destructibles/DragonEgg"};
            for(var i=0;i<kinds.Length;i++)
            {
                var path=paths[i].Length==0?"":"Assets/Imported/JSurviv/Prefabs/"+paths[i]+".prefab";
                var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);var sprite=source!=null?source.GetComponentInChildren<SpriteRenderer>(true):null;
                var entry=new WorldVisualSet.Entry{Kind=kinds[i],Sprite=sprite!=null?sprite.sprite:null,Source=path};
                var animationRoot="Assets/Imported/JSurviv/AnimationClips/Obstacle/";
                if(kinds[i]==WorldObjectKind.Swamp)entry.Idle=ExtractFrames(animationRoot+"POND/POND.idle.anim",true);
                if(kinds[i]==WorldObjectKind.Lava)entry.Idle=ExtractFrames(animationRoot+"LAVA/LAVA.idle.anim",true);
                if(kinds[i]==WorldObjectKind.Portal)entry.Idle=ExtractFrames(animationRoot+"PORTAL/PORTAL.idle.anim",true);
                if(kinds[i]==WorldObjectKind.Door)
                {
                    path="Assets/Imported/JSurviv/Sprites/OBSTACLES/Tàn tích bỏ hoang/Abandoned_door_";
                    entry.Open=ExtractFrames(path+"open.anim",false);entry.Close=ExtractFrames(path+"close.anim",false);
                    var idle=ExtractFrames(path+"idle.anim",false);entry.Sprite=idle?.Sample(0);entry.Source=path+"idle/open/close.anim";
                }
                if(kinds[i]==WorldObjectKind.Pot)
                {
                    var debris=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Imported/JSurviv/Prefabs/Indestructibles/Mảnh sành điệu.prefab");
                    var debrisRenderer=debris!=null?debris.GetComponentInChildren<SpriteRenderer>(true):null;
                    entry.BrokenSprite=debrisRenderer!=null?debrisRenderer.sprite:null;
                }
                if(entry.Sprite!=null)
                {
                    entries.Add(entry);
                    var go=new GameObject(kinds[i]+" Visual");var sr=go.AddComponent<SpriteRenderer>();sr.sprite=entry.Sprite;
                    PrefabUtility.SaveAsPrefabAsset(go,prefabDir+"/"+kinds[i]+"Visual.prefab");Object.DestroyImmediate(go);
                }
                var status=entry.Sprite==null?"Graybox; source missing/not verified":"Static sprite; replicated damage flash";
                if(entry.Idle!=null)status="Animated sprite curve: "+entry.Idle.Sprites.Length+" frames";
                if(entry.Open!=null&&entry.Close!=null)status="Server-state open/close sprite curves; legacy callback events discarded";
                if(kinds[i]==WorldObjectKind.Pot)status+="; verified broken-pot debris sprite only (no poison script)";
                if(kinds[i]==WorldObjectKind.Chest)status+="; broken artwork absent, hide after confirmed break";
                if(kinds[i]==WorldObjectKind.Poison)status+="; hunt-trap artwork is a provisional marker, poison-specific art unverified";
                if(kinds[i]==WorldObjectKind.Supply)status+="; DragonEgg is a provisional supply visual, DRG-01 is outside scope";
                report.Append('|').Append(kinds[i]).Append('|').Append(entry.Source).Append('|').Append(status).Append("|\n");
            }
            entries.AddRange(MapCoverVisuals());
            set.Entries=entries.ToArray();EditorUtility.SetDirty(set);
            Directory.CreateDirectory("Docs/Art");File.WriteAllText("Docs/Art/ART03_Source_Audit.vi.md",report.ToString());
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Jungle Maze/Phase 2/Sync Map Cover Visuals")]
        public static void SyncMapCoverVisuals()
        {
            var set=AssetDatabase.LoadAssetAtPath<WorldVisualSet>("Assets/_JungleMaze/Art/World/Resources/WorldVisualSet.asset");
            if(set==null)throw new System.InvalidOperationException("Missing WorldVisualSet.");
            var entries=new List<WorldVisualSet.Entry>();
            foreach(var entry in set.Entries)if((byte)entry.Kind>4)entries.Add(entry);
            entries.AddRange(MapCoverVisuals());set.Entries=entries.ToArray();EditorUtility.SetDirty(set);AssetDatabase.SaveAssetIfDirty(set);
        }
        private static WorldVisualSet.Entry[] MapCoverVisuals()
        {
            var result=new List<WorldVisualSet.Entry>();
            var kinds=new[]{WorldObjectKind.Pillar,WorldObjectKind.Wall,WorldObjectKind.Rock,WorldObjectKind.Statue};
            var guids=new[]{"e4e2483e878b2f24faf9fe89381f52ab","e4e2483e878b2f24faf9fe89381f52ab","7f085d8d25c86e64781760936da60fa1","b058f60fede58844c8207e61d7fc47e3"};
            var names=new[]{"abandoned_1","abandoned_0","",""};
            for(var i=0;i<kinds.Length;i++)
            {
                var path=AssetDatabase.GUIDToAssetPath(guids[i]);Sprite sprite=null;
                foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(path))if(asset is Sprite candidate&&(names[i].Length==0||candidate.name==names[i])){sprite=candidate;break;}
                if(sprite==null)throw new System.InvalidOperationException("Missing map cover sprite: "+path);
                result.Add(new WorldVisualSet.Entry{Kind=kinds[i],Sprite=sprite,Source=path});
            }
            return result.ToArray();
        }
        private static WorldVisualSet.Frames ExtractFrames(string path,bool loop)
        {
            var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);if(clip==null)return null;
            foreach(var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
            {
                if(binding.type!=typeof(SpriteRenderer)||binding.propertyName!="m_Sprite")continue;
                var keys=AnimationUtility.GetObjectReferenceCurve(clip,binding);var sprites=new List<Sprite>();var times=new List<float>();
                foreach(var key in keys)if(key.value is Sprite frame){sprites.Add(frame);times.Add(key.time);}
                if(sprites.Count>0)return new WorldVisualSet.Frames{Sprites=sprites.ToArray(),Times=times.ToArray(),Duration=Mathf.Max(clip.length,.01f),Loop=loop};
            }
            return null;
        }

        /// <summary>
        /// Callable Editor helper to apply selected terrain and cover art to Game geometry
        /// while strictly preserving BoxCollider2D (size/offset/trigger), transform (pos/rot/scale),
        /// tag, layer, and HardCoverTag.
        /// No scene regeneration or asset modification occurs while Editor is active; Codex executes this helper after review.
        /// </summary>
        [MenuItem("Jungle Maze/Phase 2/Apply Static Map Visuals")]
        public static void ApplyStaticMapVisuals()
        {
            var gamePath = "Assets/_JungleMaze/Scenes/Game.unity";
            var active = SceneManager.GetActiveScene();
            var opened = false;
            var scene = SceneManager.GetSceneByPath(gamePath);
            if (!scene.isLoaded)
            {
                scene = EditorSceneManager.OpenScene(gamePath, OpenSceneMode.Additive);
                opened = true;
            }
            try
            {
                ApplyStaticMapVisualsToScene(scene);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log("[WorldDemoBuild] Static terrain/cover visuals applied to Game scene.");
            }
            finally
            {
                if (opened) EditorSceneManager.CloseScene(scene, true);
                if (active.IsValid() && active.isLoaded) SceneManager.SetActiveScene(active);
            }
        }

        public static void ApplyStaticMapVisualsToScene(Scene scene)
        {
            var groundSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_JungleMaze/Art/Foundation/Ground96.png");
            if (groundSprite != null)
            {
                foreach (var root in scene.GetRootGameObjects())
                {
                    var gridView = root.GetComponentInChildren<MapGridView>(true);
                    if (gridView != null)
                    {
                        var so = new SerializedObject(gridView);
                        var prop = so.FindProperty("groundSprite");
                        if (prop != null)
                        {
                            prop.objectReferenceValue = groundSprite;
                            so.ApplyModifiedProperties();
                            EditorUtility.SetDirty(gridView);
                        }
                    }
                }
            }

            var pillarSprite = FindSprite("Assets/Imported/JSurviv/Sprites/OBSTACLES/Tàn tích bỏ hoang/abandoned.png", "abandoned_1");
            var wallSprite = FindSprite("Assets/Imported/JSurviv/Sprites/OBSTACLES/Tàn tích bỏ hoang/abandoned.png", "abandoned_0");
            var statueSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Imported/JSurviv/Sprites/OBSTACLES/19. Tượng đài/tượng.png");
            var rockSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Imported/JSurviv/Sprites/OBSTACLES/05. Đá to hơn/stone_4_1.png");

            foreach (var root in scene.GetRootGameObjects())
            {
                ApplyCoverVisualIfMatching(root, "AncientPillar", pillarSprite);
                ApplyCoverVisualIfMatching(root, "CityWall", wallSprite);
                ApplyCoverVisualIfMatching(root, "AncientStatue", statueSprite);
                ApplyCoverVisualIfMatching(root, "BoulderRock", rockSprite);
            }
        }

        private static Sprite FindSprite(string texturePath, string spriteName)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(texturePath);
            foreach (var asset in assets)
            {
                if (asset is Sprite s && s.name == spriteName) return s;
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);
        }

        private static void ApplyCoverVisualIfMatching(GameObject obj, string targetName, Sprite sprite)
        {
            if (sprite == null) return;
            foreach (var renderer in obj.GetComponentsInChildren<SpriteRenderer>(true))
            {
                var go = renderer.gameObject;
                if (!go.name.Contains(targetName)) continue;

                var collider = go.GetComponent<BoxCollider2D>();
                if (collider == null) continue;
                var initialPos = go.transform.position;
                var initialLayer = go.layer;
                var initialTag = go.tag;

                // Imported art has its own pixel size; fit a visual child without scaling the authored collider.
                var visual = go.transform.Find("Authored visual");
                if (visual == null)
                {
                    visual = new GameObject("Authored visual").transform;
                    visual.SetParent(go.transform, false);
                }
                var view = visual.GetComponent<SpriteRenderer>();
                if (view == null) view = visual.gameObject.AddComponent<SpriteRenderer>();
                view.sprite = sprite;
                view.color = Color.white;
                view.sharedMaterial = renderer.sharedMaterial;
                view.sortingLayerID = renderer.sortingLayerID;
                view.sortingOrder = renderer.sortingOrder;
                var scale = new Vector2(collider.size.x / sprite.bounds.size.x, collider.size.y / sprite.bounds.size.y);
                visual.localScale = new Vector3(scale.x, scale.y, 1);
                visual.localPosition = new Vector3(collider.offset.x - sprite.bounds.center.x * scale.x,
                    collider.offset.y - sprite.bounds.center.y * scale.y, 0);
                renderer.enabled = false;
                EditorUtility.SetDirty(renderer);
                EditorUtility.SetDirty(view);

                if (go.transform.position != initialPos || go.layer != initialLayer || go.tag != initialTag)
                    throw new System.InvalidOperationException($"Preservation invariant violated on {go.name}");
            }
        }
    }
}
