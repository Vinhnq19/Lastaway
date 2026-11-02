using System.Collections.Generic;
using System.IO;
using JungleMaze.LevelEditor;
using JungleMaze.Presentation.World.Phase2;
using JungleMaze.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace JungleMaze.EditorTools
{
    /// <summary>
    /// Sinh catalog (số liệu theo GDD sheet Obstacles, sprite có sẵn trong project) và scene LevelEditor.
    /// Chạy lại bao nhiêu lần cũng ghi đè cùng một catalog/scene, không tạo bản trùng.
    /// </summary>
    public static class LevelEditorBuild
    {
        private const string ScenePath = "Assets/_JungleMaze/Scenes/LevelEditor.unity";
        private const string CatalogDirectory = "Assets/_JungleMaze/Art/LevelEditor";
        private const string CatalogPath = CatalogDirectory + "/LevelEditorCatalog.asset";
        private const string LevelsDirectory = "Assets/_JungleMaze/Levels";
        private const string Obstacles = "Assets/Imported/JSurviv/Sprites/OBSTACLES/";

        [MenuItem("Jungle Maze/Level Editor/Build Level Editor Scene")]
        public static void Build()
        {
            var catalog = BuildCatalog();
            if (!AssetDatabase.IsValidFolder(LevelsDirectory)) AssetDatabase.CreateFolder("Assets/_JungleMaze", "Levels");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("Main Camera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.orthographic = true;
            camera.orthographicSize = 30;
            camera.transform.position = new Vector3(25, 25, -10);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.07f, .09f, .1f);

            var editor = new GameObject("Level Editor").AddComponent<LevelEditorController>();
            var serialized = new SerializedObject(editor);
            serialized.FindProperty("catalog").objectReferenceValue = catalog;
            serialized.FindProperty("viewCamera").objectReferenceValue = camera;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[LevelEditorBuild] Đã tạo " + ScenePath + " và " + CatalogPath + ". Bấm Play để dùng.");
        }

        [MenuItem("Jungle Maze/Level Editor/Open Level Editor Scene")]
        public static void Open()
        {
            if (!File.Exists(ScenePath)) Build();
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(ScenePath);
        }

        public static LevelCatalog BuildCatalog()
        {
            Directory.CreateDirectory(CatalogDirectory);
            AssetDatabase.Refresh();
            var catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<LevelCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            var world = AssetDatabase.LoadAssetAtPath<WorldVisualSet>("Assets/_JungleMaze/Art/World/Resources/WorldVisualSet.asset");
            Sprite WorldSprite(WorldObjectKind kind) => world != null ? world.Find((byte)kind) : null;
            var hardCover = "Hard cover: chặn đi, đạn, vụ nổ, tầm nhìn.";
            var items = new List<LevelItemType>
            {
                Item("AncientPillar", "Cột đá cổ đại", LevelItemGroup.Cover, 1, 2, FindSprite(Obstacles + "Tàn tích bỏ hoang/abandoned.png", "abandoned_1"),
                    Grey, false, hardCover + " Vật che chắn cơ bản, rải đều toàn map."),
                Item("CityWall", "Tường thành", LevelItemGroup.Cover, 1, 4, FindSprite(Obstacles + "Tàn tích bỏ hoang/abandoned.png", "abandoned_0"),
                    Grey, false, hardCover + " Tạo hành lang và ngõ cụt. R để xoay ngang."),
                Item("BoulderRock", "Tảng đá lớn", LevelItemGroup.Cover, 4, 4, FindSprite(Obstacles + "05. Đá to hơn/stone_4_1.png", null),
                    Grey, false, hardCover + " Điểm mốc, vùng khuất lớn."),
                Item("AncientStatue", "Tượng cổ", LevelItemGroup.Cover, 3, 3, FindSprite(Obstacles + "19. Tượng đài/tượng.png", null),
                    Grey, false, hardCover + " Đánh dấu nút giao mê cung."),

                Item(nameof(WorldObjectKind.Chest), "Rương gỗ", LevelItemGroup.WorldObject, 1, 1, WorldSprite(WorldObjectKind.Chest),
                    new Color(.75f, .45f, .16f), false, "Destructible, HP 30. Nguồn loot chính, rơi 3–4 món. Không respawn."),
                Item(nameof(WorldObjectKind.Pot), "Chum gốm", LevelItemGroup.WorldObject, 1, 1, WorldSprite(WorldObjectKind.Pot),
                    new Color(.65f, .37f, .3f), false, "Destructible, HP 20. Rơi 1–2 món; đặt dày ở rìa map."),
                Item(nameof(WorldObjectKind.Supply), "Vật chứa tiếp tế", LevelItemGroup.WorldObject, 2, 2, WorldSprite(WorldObjectKind.Supply),
                    new Color(1f, .85f, .25f), false, "Destructible, HP 150, rơi 6–8 món. GDD: chỉ xuất hiện qua airdrop."),
                Item(nameof(WorldObjectKind.Bush), "Bụi rậm", LevelItemGroup.WorldObject, 1.5f, 1.5f, WorldSprite(WorldObjectKind.Bush),
                    new Color(.2f, .5f, .22f), false, "Đi xuyên, bắn xuyên. Đứng lọt trong bụi thì vô hình; bắn ra là lộ."),
                Item(nameof(WorldObjectKind.Door), "Cửa đá", LevelItemGroup.WorldObject, 1, 2, WorldSprite(WorldObjectKind.Door),
                    new Color(.55f, .5f, .45f), false, "Hard cover + tương tác: mở 20 s / đóng 20 s."),
                Item(nameof(WorldObjectKind.Portal), "Portal", LevelItemGroup.WorldObject, 1, 1, WorldSprite(WorldObjectKind.Portal),
                    new Color(.2f, .65f, 1f), true, "Đi theo cặp (tự ghép theo thứ tự đặt; đổi số cặp ở cột phải). Cooldown 3 s."),
                Item(nameof(WorldObjectKind.Swamp), "Đầm lầy", LevelItemGroup.WorldObject, 4, 4, WorldSprite(WorldObjectKind.Swamp),
                    new Color(.25f, .4f, .25f), true, "Hazard: giảm 40% tốc độ khi đứng trong."),
                Item(nameof(WorldObjectKind.Lava), "Hồ Lava", LevelItemGroup.WorldObject, 3, 3, WorldSprite(WorldObjectKind.Lava),
                    new Color(1f, .3f, .08f), true, "Hazard: 10 HP / 0.5 s, giáp không giảm."),
                Item(nameof(WorldObjectKind.Poison), "Ổ gai", LevelItemGroup.WorldObject, 1, 1, WorldSprite(WorldObjectKind.Poison),
                    new Color(.65f, .22f, .7f), true, "Hazard: Poison 5 HP/s trong 3 s, đứng trong thì reset."),

                Item("River", "Sông", LevelItemGroup.Decoration, 4, 1, null,
                    new Color(.15f, .45f, .85f), true, "GDD: không đi qua, bắn xuyên. Game CHƯA hỗ trợ: chỉ lưu trong \"decorations\"."),
                Item("FlowerBed", "Thảm hoa / nấm", LevelItemGroup.Decoration, 1, 1, FindSprite(Obstacles + "03. Hoa anh túc/hoa anh tuc.png", null),
                    new Color(.9f, .5f, .7f), true, "Thuần trang trí. Game CHƯA hỗ trợ: chỉ lưu trong \"decorations\"."),

                Item("Spawn", "Điểm spawn", LevelItemGroup.Spawn, LevelDocument.SpawnSize, LevelDocument.SpawnSize, null,
                    new Color(.3f, 1f, .45f), false, "Game cần đúng 10 spawn, cách nhau ≥ 6, trong bo đầu, không bị chặn, đi tới được nhau."),
            };
            catalog.Items = items.ToArray();
            catalog.GroundTiles = new[]
            {
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_JungleMaze/Art/Foundation/Ground96.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_JungleMaze/Art/Foundation/Cell48.png"),
            };
            catalog.SpriteMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat");
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();

            foreach (var item in items)
                if (item.Sprite == null && item.Group != LevelItemGroup.Spawn)
                    Debug.Log($"[LevelEditorBuild] {item.Id}: chưa có sprite, dùng khối màu.");
            return catalog;
        }

        private static readonly Color Grey = new Color(.6f, .62f, .6f);

        private static LevelItemType Item(string id, string name, LevelItemGroup group, float w, float h, Sprite sprite,
            Color tint, bool flat, string note) =>
            new LevelItemType { Id = id, DisplayName = name, Group = group, BaseSize = new Vector2(w, h), Sprite = sprite, Tint = tint, Flat = flat, Note = note };

        private static Sprite FindSprite(string texturePath, string spriteName)
        {
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(texturePath))
                if (asset is Sprite s && (spriteName == null || s.name == spriteName)) return s;
            return null;
        }
    }
}
