using JungleMaze.Presentation.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace JungleMaze.EditorTools
{
    /// <summary>
    /// Cấu hình Depth Sorting cho project, chạy lại bao nhiêu lần cũng ra cùng kết quả:
    /// thêm Sorting Layer WorldDynamic (ngay trước Default) và Foreground (ngay sau Default) nếu chưa có, rồi gắn
    /// SortingGroup + DepthSortable lên các prefab entity, và cho Light2D của scene dự án chiếu tới hai layer mới.
    /// Không đổi pivot sprite, transform, collider hay layer vật lý.
    /// </summary>
    public static class DepthSortingSetup
    {
        private const string TagManagerPath = "ProjectSettings/TagManager.asset";
        private const string SortPointName = "SortPoint";

        // Hero và remote player xoay theo hướng ngắm: SortPoint lệch tâm sẽ xoay theo, nên dùng root (tâm thân tròn).
        private static readonly string[] RootSortedPrefabs =
        {
            "Assets/_JungleMaze/Prefabs/Hero.prefab",
            "Assets/_JungleMaze/Prefabs/PlayerView.prefab",
        };

        // Obstacle tĩnh: SortPoint ở cạnh đáy collider (điểm tiếp xúc mặt đất).
        private static readonly string[] GroundSortedPrefabs =
        {
            "Assets/_JungleMaze/Prefabs/Obstacles/AncientPillar.prefab",
            "Assets/_JungleMaze/Prefabs/Obstacles/AncientStatue.prefab",
            "Assets/_JungleMaze/Prefabs/Obstacles/BoulderRock.prefab",
            "Assets/_JungleMaze/Prefabs/Obstacles/CityWall.prefab",
        };

        [MenuItem("Jungle Maze/Rendering/Apply Depth Sorting Setup")]
        public static void Apply()
        {
            EnsureSortingLayers();
            foreach (var path in RootSortedPrefabs) ConfigurePrefab(path, false);
            foreach (var path in GroundSortedPrefabs) ConfigurePrefab(path, true);
            foreach (var guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/_JungleMaze/Scenes" }))
                EnsureLightsTargetNewLayers(AssetDatabase.GUIDToAssetPath(guid));
            AssetDatabase.SaveAssets();
            Debug.Log("[DepthSortingSetup] Sorting layers, prefab entity và Light2D đã được cấu hình.");
        }

        /// <summary>
        /// URP 2D: Light2D chỉ chiếu các Sorting Layer trong "Target Sorting Layers"; layer mới không có ở đó sẽ đen.
        /// Light nào đang chiếu Default thì thêm WorldDynamic/Foreground. Chỉ lưu scene khi thực sự có thay đổi.
        /// </summary>
        public static void EnsureLightsTargetNewLayers(string scenePath)
        {
            var scene = SceneManager.GetSceneByPath(scenePath);
            var opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            try
            {
                var added = new[] { SortingLayer.NameToID(SortingLayers.WorldDynamic), SortingLayer.NameToID(SortingLayers.Foreground) };
                var changed = false;
                foreach (var root in scene.GetRootGameObjects())
                foreach (var light in root.GetComponentsInChildren<Behaviour>(true))
                {
                    if (light == null || light.GetType().FullName != "UnityEngine.Rendering.Universal.Light2D") continue;
                    var serialized = new SerializedObject(light);
                    var layers = serialized.FindProperty("m_ApplyToSortingLayers");
                    if (layers == null || !Contains(layers, 0)) continue;
                    foreach (var id in added)
                    {
                        if (id == 0 || Contains(layers, id)) continue;
                        layers.arraySize++;
                        layers.GetArrayElementAtIndex(layers.arraySize - 1).intValue = id;
                        changed = true;
                    }
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
                if (changed) EditorSceneManager.SaveScene(scene);
            }
            finally
            {
                if (opened) EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static bool Contains(SerializedProperty ints, int value)
        {
            for (var i = 0; i < ints.arraySize; i++)
                if (ints.GetArrayElementAtIndex(i).intValue == value) return true;
            return false;
        }

        public static void EnsureSortingLayers()
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath(TagManagerPath)[0]);
            var layers = tagManager.FindProperty("m_SortingLayers");
            var changed = false;
            if (IndexOf(layers, SortingLayers.WorldDynamic) < 0)
            {
                Insert(layers, IndexOf(layers, SortingLayers.Default), SortingLayers.WorldDynamic);
                changed = true;
            }
            if (IndexOf(layers, SortingLayers.Foreground) < 0)
            {
                Insert(layers, IndexOf(layers, SortingLayers.Default) + 1, SortingLayers.Foreground);
                changed = true;
            }
            if (changed) tagManager.ApplyModifiedPropertiesWithoutUndo();

            var ground = IndexOf(layers, SortingLayers.Ground);
            var dynamic = IndexOf(layers, SortingLayers.WorldDynamic);
            var fallback = IndexOf(layers, SortingLayers.Default);
            var foreground = IndexOf(layers, SortingLayers.Foreground);
            if (!(ground < dynamic && dynamic < fallback && fallback < foreground))
                Debug.LogWarning("[DepthSortingSetup] Thứ tự Sorting Layer cần là Ground < WorldDynamic < Default < Foreground; " +
                                 "sắp lại trong Project Settings > Tags and Layers.");
        }

        private static int IndexOf(SerializedProperty layers, string layerName)
        {
            for (var i = 0; i < layers.arraySize; i++)
                if (layers.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == layerName) return i;
            return -1;
        }

        private static void Insert(SerializedProperty layers, int index, string layerName)
        {
            index = Mathf.Clamp(index, 0, layers.arraySize);
            if (index == layers.arraySize) layers.arraySize++;
            else layers.InsertArrayElementAtIndex(index);
            var layer = layers.GetArrayElementAtIndex(index);
            layer.FindPropertyRelative("name").stringValue = layerName;
            layer.FindPropertyRelative("uniqueID").longValue = NewUniqueId(layers);
            layer.FindPropertyRelative("locked").boolValue = false;
        }

        private static uint NewUniqueId(SerializedProperty layers)
        {
            while (true)
            {
                var id = (uint)Random.Range(1, int.MaxValue);
                var taken = false;
                for (var i = 0; i < layers.arraySize && !taken; i++)
                    taken = (uint)layers.GetArrayElementAtIndex(i).FindPropertyRelative("uniqueID").longValue == id;
                if (!taken) return id;
            }
        }

        private static void ConfigurePrefab(string path, bool groundSortPoint)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (!root.TryGetComponent<SortingGroup>(out var group)) group = root.AddComponent<SortingGroup>();
                group.sortingLayerID = SortingLayer.NameToID(SortingLayers.WorldDynamic);
                group.sortingOrder = 0;
                if (!root.TryGetComponent<DepthSortable>(out var sortable)) sortable = root.AddComponent<DepthSortable>();

                Transform point = null;
                if (groundSortPoint && root.TryGetComponent<BoxCollider2D>(out var box))
                {
                    point = root.transform.Find(SortPointName);
                    if (point == null)
                    {
                        point = new GameObject(SortPointName).transform;
                        point.SetParent(root.transform, false);
                    }
                    point.localPosition = new Vector3(box.offset.x, box.offset.y - box.size.y * .5f, 0f);
                    point.localRotation = Quaternion.identity;
                    point.localScale = Vector3.one;
                }

                var serialized = new SerializedObject(sortable);
                serialized.FindProperty("sortPoint").objectReferenceValue = point;
                serialized.FindProperty("sortingGroup").objectReferenceValue = group;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
