using System.Collections.Generic;
using System.IO;
using JungleMaze.LevelEditor;
using JungleMaze.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace JungleMaze.EditorTools
{
    /// <summary>
    /// Sinh catalog từ prefab Cainos "Pixel Art Top Down - Basic" (bản riêng của Level Editor trong
    /// Art/LevelEditor/Cainos) và scene LevelEditor. Hình = các sprite con của prefab; vùng chặn = collider chân của
    /// prefab. Chạy lại bao nhiêu lần cũng ghi đè cùng một catalog/scene, không tạo bản trùng.
    /// </summary>
    public static class LevelEditorBuild
    {
        private const string ScenePath = "Assets/_JungleMaze/Scenes/LevelEditor.unity";
        private const string CatalogDirectory = "Assets/_JungleMaze/Art/LevelEditor";
        private const string CatalogPath = CatalogDirectory + "/LevelEditorCatalog.asset";
        private const string LevelsDirectory = "Assets/_JungleMaze/Levels";
        private const string Cainos = CatalogDirectory + "/Cainos/";
        private const float PixelsPerUnit = 32f;
        /// <summary>Collider bắt đầu cao hơn mức này (xà cổng, mái) không phải chân, không chặn đường trên map 2D.</summary>
        private const float FootColliderMaxStart = .3f;

        [MenuItem("Jungle Maze/Level Editor/Build Level Editor Scene")]
        public static void Build()
        {
            var catalog = BuildCatalog();
            if (!AssetDatabase.IsValidFolder(LevelsDirectory)) AssetDatabase.CreateFolder("Assets/_JungleMaze", "Levels");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            // NewScene(Single) dỡ asset không còn được tham chiếu; nạp lại catalog để tham chiếu không thành null.
            catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>(CatalogPath);
            var camera = new GameObject("Main Camera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.orthographic = true;
            camera.orthographicSize = 30;
            camera.transform.position = new Vector3(25, 25, -10);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.2f, .2f, .22f);

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

        private sealed class Spec
        {
            public string Id, Name, Note;
            public LevelItemGroup Group;
            public string[] Prefabs;
            public Vector2Int? Footprint;
        }

        private static Spec S(string id, string name, LevelItemGroup group, string note, params string[] prefabs) =>
            new Spec { Id = id, Name = name, Group = group, Note = note, Prefabs = prefabs };

        private static Spec Sized(this Spec spec, int w, int h) { spec.Footprint = new Vector2Int(w, h); return spec; }

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

            const string cover = "Hard cover: chặn đi, đạn, tầm nhìn. Chỉ phần chân chặn đường.";
            const string decor = "Trang trí, đi xuyên. Game bỏ qua (\"decorations\").";
            var cv = LevelItemGroup.Cover;
            var wo = LevelItemGroup.WorldObject;
            var de = LevelItemGroup.Decoration;
            var specs = new List<Spec>
            {
                S("Tree01", "Cây 1", cv, cover, "Plant - Tree 01"),
                S("Tree02", "Cây 2", cv, cover, "Plant - Tree 02"),
                S("Tree03", "Cây 3", cv, cover, "Plant - Tree 03"),
                S("Statue01", "Tượng đá", cv, cover, "Props - Statue 01"),
                S("Pillar01", "Cột đá", cv, cover, "Props - Pillar 01"),
                S("Pillar02", "Cột đá gãy", cv, cover, "Props - Pillar 02"),
                S("RunePillarX2", "Cột rune thấp", cv, cover, "Props - Rune Pillar X2"),
                S("RunePillarX3", "Cột rune cao", cv, cover, "Props - Rune Pillar X3"),
                S("RunePillarBroken", "Cột rune gãy", cv, cover, "Props - Rune Pillar Broken"),
                S("Gravestone01", "Bia mộ 1", cv, cover, "Props - Gravestone 01"),
                S("Gravestone02", "Bia mộ 2", cv, cover, "Props - Gravestone 02"),
                S("Gravestone03", "Bia mộ 3", cv, cover, "Props - Gravestone 03"),
                S("StoneCube", "Khối đá", cv, cover, "Props - Stone Cube 01"),
                S("StoneLantern", "Đèn đá", cv, cover, "Props - Stone Lantern 01"),
                S("Well", "Giếng", cv, cover, "Props - Well 01"),
                S("BenchS", "Ghế đá ngang", cv, cover, "Props - Stone Bench 01 S"),
                S("BenchE", "Ghế đá dọc (Đông)", cv, cover, "Props - Stone Bench 01 E"),
                S("BenchW", "Ghế đá dọc (Tây)", cv, cover, "Props - Stone Bench 01 W"),
                S("CoffinH", "Quan tài ngang", cv, cover, "Props - Stone Coffin 01 H"),
                S("CoffinV", "Quan tài dọc", cv, cover, "Props - Stone Coffin 01 V"),
                S("Barrel", "Thùng tròn", cv, cover, "Props - Barrel 01"),
                S("CrateSmall", "Thùng gỗ nhỏ", cv, cover, "Props - Crate 02"),
                S("Rock05", "Đá 1", cv, cover, "Props - Stone 05"),
                S("Rock06", "Đá 2", cv, cover, "Props - Stone 06"),
                S("Rock07", "Tảng đá lớn", cv, cover, "Props - Stone 07"),
                S("Gate01", "Cổng đá 1", cv, cover + " Hai chân cổng chặn, lối giữa đi qua.", "Struct - Gate 01"),
                S("Gate02", "Cổng đá 2", cv, cover + " Hai chân cổng chặn, lối giữa đi qua.", "Struct - Gate 02"),

                S(nameof(WorldObjectKind.Chest), "Rương", wo, "Destructible, HP 30. Nguồn loot chính, rơi 3–4 món.", "Props - Chest 01").Sized(1, 1),
                S(nameof(WorldObjectKind.Pot), "Bình gốm", wo, "Destructible, HP 20. Rơi 1–2 món. Mẫu hình đổi theo seed.",
                    "Props - Pot 01", "Props - Pot 02", "Props - Pot 03").Sized(1, 1),
                S(nameof(WorldObjectKind.Supply), "Thùng tiếp tế", wo, "Destructible, HP 150, rơi 6–8 món.", "Props - Crate 01").Sized(1, 1),
                S(nameof(WorldObjectKind.Bush), "Bụi rậm", wo, "Đi xuyên, bắn xuyên. Đứng trong bụi thì vô hình. Mẫu hình đổi theo seed.",
                    "Plant - Bush 01", "Plant - Bush 02", "Plant - Bush 03", "Plant - Bush 05", "Plant - Bush 06").Sized(1, 1),
                S(nameof(WorldObjectKind.Door), "Cửa gỗ", wo, "Cửa tương tác: F để mở/đóng, đánh vỡ được (HP mặc định 60, chỉnh ở cột phải). Đặt vào khe tường.", "Props - Wooden Gate 01").Sized(1, 1),
                S(nameof(WorldObjectKind.Portal), "Portal (bệ rune)", wo, "Đi theo cặp (tự ghép theo thứ tự đặt; đổi số cặp ở cột phải). Cooldown 3 s.",
                    "Props - Altar 01").Sized(3, 2),

                S("Pebble", "Đá vụn", de, decor, "Props - Stone 01", "Props - Stone 02", "Props - Stone 03", "Props - Stone 04").Sized(1, 1),
                S("Brick", "Gạch vụn", de, decor, "Props - Brick 01", "Props - Brick 02", "Props - Brick 03").Sized(1, 1),
                S("GrassTuft", "Cỏ", de, decor, GrassPrefabs()).Sized(1, 1),
                S("Flower", "Hoa", de, decor, "Plant - Flower 01").Sized(1, 1),
                S("SignE", "Biển báo (Đông)", de, decor, "Props - Road Sign 01 E").Sized(1, 1),
                S("SignW", "Biển báo (Tây)", de, decor, "Props - Road Sign 01 W").Sized(1, 1),
            };

            var items = new List<LevelItemType>();
            foreach (var spec in specs)
            {
                var item = BuildItem(spec);
                if (item != null) items.Add(item);
            }
            items.Add(new LevelItemType
            {
                Id = "Spawn", DisplayName = "Điểm spawn", Group = LevelItemGroup.Spawn, Footprint = Vector2Int.one,
                Tint = new Color(.3f, 1f, .45f), Flat = true,
                Note = "Game cần đúng 10 spawn, cách nhau ≥ 6, trong bo đầu, không bị chặn, đi tới được nhau.",
            });
            catalog.GrassGrid = SpriteGrid(Cainos + "Texture/TX Tileset Grass.png", LevelGround.GrassColumns);
            catalog.WallGrid = SpriteGrid(Cainos + "Texture/TX Tileset Wall.png", LevelGround.WallColumns);
            // Tường gạch: không có prefab Cainos, hình lấy từ tileset tường và tự nối theo ô kề (LevelWalls).
            items.Insert(0, new LevelItemType
            {
                Id = LevelWalls.Id, DisplayName = "Tường gạch", Group = LevelItemGroup.Cover, Footprint = Vector2Int.one,
                Colliders = new[] { new Rect(0f, 0f, 1f, 1f) },
                Variants = new[] { new LevelVisualVariant { Parts = LevelWalls.Parts(catalog.WallGrid, new HashSet<Vector2Int>(), 0, 0) } },
                Sprite = catalog.WallGrid[7 * LevelGround.WallColumns + 2], Tint = new Color(.75f, .55f, .45f),
                Note = "Hard cover kín cả ô. Xếp liền nhau thành tường/nhà; hình tự nối đầu trái/phải và tường dọc. Chừa ô cho Cửa gỗ.",
            });
            catalog.Items = items.ToArray();
            var sprinkles = new List<Sprite>();
            foreach (var name in GrassPrefabs())
            {
                var prefab = LoadPrefab(name);
                var renderer = prefab != null ? prefab.GetComponentInChildren<SpriteRenderer>(true) : null;
                if (renderer != null && renderer.sprite != null) sprinkles.Add(renderer.sprite);
            }
            catalog.Sprinkles = sprinkles.ToArray();
            catalog.SpriteMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat");
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            return catalog;
        }

        private static string[] GrassPrefabs()
        {
            var names = new string[15];
            for (var i = 0; i < names.Length; i++) names[i] = $"Plant - Grass {i + 1:00}";
            return names;
        }

        private static GameObject LoadPrefab(string name)
        {
            var folder = name.StartsWith("Plant") ? "Plant" : "Props";
            return AssetDatabase.LoadAssetAtPath<GameObject>($"{Cainos}Prefab/{folder}/PF {name}.prefab");
        }

        private static LevelItemType BuildItem(Spec spec)
        {
            var variants = new List<LevelVisualVariant>();
            var colliders = new List<Rect>();
            var footprint = spec.Footprint ?? Vector2Int.one;
            Sprite icon = null;
            for (var v = 0; v < spec.Prefabs.Length; v++)
            {
                var prefab = LoadPrefab(spec.Prefabs[v]);
                if (prefab == null)
                {
                    Debug.LogWarning($"[LevelEditorBuild] {spec.Id}: không thấy prefab {spec.Prefabs[v]}.");
                    continue;
                }
                var root = prefab.transform;
                var feet = FootColliders(root);
                // Footprint lấy từ collider của mẫu đầu tiên; pivot prefab (chân) đặt ở giữa cạnh đáy footprint.
                if (variants.Count == 0)
                {
                    if (spec.Footprint == null && feet.Count > 0)
                    {
                        var union = feet[0];
                        foreach (var r in feet) union = Rect.MinMaxRect(Mathf.Min(union.xMin, r.xMin), Mathf.Min(union.yMin, r.yMin), Mathf.Max(union.xMax, r.xMax), Mathf.Max(union.yMax, r.yMax));
                        footprint = new Vector2Int(Mathf.Max(1, Mathf.CeilToInt(union.width - .15f)), Mathf.Max(1, Mathf.CeilToInt(union.yMax - .15f)));
                    }
                    if (spec.Group == LevelItemGroup.Cover)
                        foreach (var r in feet)
                        {
                            var shifted = Rect.MinMaxRect(Snap(Mathf.Clamp(r.xMin + footprint.x / 2f, 0, footprint.x)), Snap(Mathf.Clamp(r.yMin, 0, footprint.y)),
                                Snap(Mathf.Clamp(r.xMax + footprint.x / 2f, 0, footprint.x)), Snap(Mathf.Clamp(r.yMax, 0, footprint.y)));
                            if (shifted.width > 0 && shifted.height > 0) colliders.Add(shifted);
                        }
                }
                var parts = new List<LevelVisualPart>();
                var bestArea = 0f;
                foreach (var renderer in prefab.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (renderer.sprite == null) continue;
                    var local = (Vector2)root.InverseTransformPoint(renderer.transform.position);
                    var scale = renderer.transform.lossyScale;
                    var rootScale = root.lossyScale;
                    var name = renderer.sprite.name;
                    var shadow = name.Contains("Shadow");
                    var rank = shadow ? 0 : name.Contains("Upper") || name.EndsWith(" T") ? 20 : name.Contains("Glow") || name.Contains("Rune ") ? 11 : 10;
                    parts.Add(new LevelVisualPart
                    {
                        Sprite = renderer.sprite,
                        Offset = local + new Vector2(footprint.x / 2f, 0f),
                        Scale = new Vector2(scale.x / rootScale.x, scale.y / rootScale.y),
                        FlipX = renderer.flipX,
                        Rank = rank * 100 + renderer.sortingOrder,
                        Shadow = shadow,
                    });
                    var area = renderer.sprite.rect.width * renderer.sprite.rect.height;
                    if (!shadow && v == 0 && area > bestArea) { bestArea = area; icon = renderer.sprite; }
                }
                // Cover đứng trên chân (pivot = giữa cạnh đáy footprint). Vật đi xuyên / vật game có footprint định sẵn
                // (bụi, bình, đá vụn, bệ portal...) thì căn hình vào giữa footprint, vì pivot prefab của chúng nằm giữa hình.
                if (spec.Group != LevelItemGroup.Cover) CenterInFootprint(parts, footprint);
                variants.Add(new LevelVisualVariant { Parts = parts.ToArray() });
            }
            if (variants.Count == 0) return null;
            if (spec.Group == LevelItemGroup.Cover && colliders.Count == 0)
                Debug.LogWarning($"[LevelEditorBuild] {spec.Id}: prefab không có collider chân, dùng cả footprint.");
            return new LevelItemType
            {
                Id = spec.Id, DisplayName = spec.Name, Group = spec.Group, Note = spec.Note,
                Footprint = footprint, Colliders = colliders.ToArray(), Variants = variants.ToArray(), Sprite = icon,
                Tint = spec.Group == LevelItemGroup.Cover ? new Color(.95f, .55f, .3f) : spec.Group == LevelItemGroup.WorldObject ? new Color(.35f, .75f, 1f) : new Color(.8f, .8f, .8f),
            };
        }

        private static void CenterInFootprint(List<LevelVisualPart> parts, Vector2Int footprint)
        {
            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);
            foreach (var part in parts)
            {
                if (part.Shadow) continue;
                var b = part.Sprite.bounds;
                var center = part.Offset + Vector2.Scale((Vector2)b.center, part.Scale);
                var half = Vector2.Scale((Vector2)b.extents, new Vector2(Mathf.Abs(part.Scale.x), Mathf.Abs(part.Scale.y)));
                min = Vector2.Min(min, center - half);
                max = Vector2.Max(max, center + half);
            }
            if (min.x > max.x) return;
            var shift = (Vector2)footprint / 2f - (min + max) / 2f;
            foreach (var part in parts) part.Offset += shift;
        }

        /// <summary>AABB của các collider không phải trigger, theo toạ độ gốc prefab; bỏ collider không chạm chân.</summary>
        private static List<Rect> FootColliders(Transform root)
        {
            var result = new List<Rect>();
            foreach (var collider in root.GetComponentsInChildren<Collider2D>(true))
            {
                if (collider.isTrigger) continue;
                var points = new List<Vector2>();
                switch (collider)
                {
                    case BoxCollider2D box:
                        var half = box.size / 2f;
                        points.Add(box.offset + new Vector2(-half.x, -half.y));
                        points.Add(box.offset + new Vector2(half.x, half.y));
                        points.Add(box.offset + new Vector2(-half.x, half.y));
                        points.Add(box.offset + new Vector2(half.x, -half.y));
                        break;
                    case PolygonCollider2D polygon:
                        for (var p = 0; p < polygon.pathCount; p++)
                            foreach (var point in polygon.GetPath(p)) points.Add(point + polygon.offset);
                        break;
                    case CircleCollider2D circle:
                        points.Add(circle.offset - Vector2.one * circle.radius);
                        points.Add(circle.offset + Vector2.one * circle.radius);
                        break;
                    case CapsuleCollider2D capsule:
                        points.Add(capsule.offset - capsule.size / 2f);
                        points.Add(capsule.offset + capsule.size / 2f);
                        break;
                    case EdgeCollider2D edge:
                        foreach (var point in edge.points) points.Add(point + edge.offset);
                        break;
                    default:
                        continue;
                }
                var min = new Vector2(float.MaxValue, float.MaxValue);
                var max = new Vector2(float.MinValue, float.MinValue);
                foreach (var point in points)
                {
                    var local = (Vector2)root.InverseTransformPoint(collider.transform.TransformPoint(point));
                    min = Vector2.Min(min, local);
                    max = Vector2.Max(max, local);
                }
                if (min.y > FootColliderMaxStart) continue;
                result.Add(Rect.MinMaxRect(min.x, min.y, max.x, max.y));
            }
            return result;
        }

        private static float Snap(float v) => Mathf.Round(v * PixelsPerUnit) / PixelsPerUnit;

        /// <summary>Sprite của tileset xếp theo vị trí ô 32 px trong texture (không theo tên): index = hàng từ trên × cột + cột.</summary>
        private static Sprite[] SpriteGrid(string texturePath, int columns)
        {
            var grid = new Sprite[columns * columns];
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(texturePath))
            {
                if (!(asset is Sprite sprite)) continue;
                var rect = sprite.rect;
                var col = Mathf.RoundToInt(rect.x / PixelsPerUnit);
                var row = Mathf.RoundToInt((sprite.texture.height - rect.yMax) / PixelsPerUnit);
                if (col < 0 || row < 0 || col >= columns || row >= columns) continue;
                if (Mathf.RoundToInt(rect.width) != (int)PixelsPerUnit || Mathf.RoundToInt(rect.height) != (int)PixelsPerUnit) continue;
                grid[row * columns + col] = sprite;
            }
            return grid;
        }
    }
}
