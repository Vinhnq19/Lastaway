using System;
using UnityEngine;

namespace JungleMaze.LevelEditor
{
    /// <summary>Một loại vật thể trong bảng chọn của Level Editor. Số liệu mặc định lấy từ GDD sheet Obstacles.</summary>
    [Serializable]
    public sealed class LevelItemType
    {
        [Tooltip("Tên ghi ra JSON: tên cover, tên WorldObjectKind, tên decoration hoặc 'Spawn'.")]
        public string Id;
        public string DisplayName;
        public LevelItemGroup Group;
        [Tooltip("Footprint gốc (world unit = cell) trước khi scale/xoay.")]
        public Vector2 BaseSize = Vector2.one;
        public Sprite Sprite;
        [Tooltip("Màu khi không có sprite và màu viền footprint.")]
        public Color Tint = Color.white;
        [Tooltip("Vẽ dưới các vật thể đứng (hazard, thảm, sông, portal).")]
        public bool Flat;
        [TextArea] public string Note;
    }

    /// <summary>Danh sách vật thể và ground tile cho Level Editor. Sinh bởi menu Jungle Maze/Level Editor.</summary>
    [CreateAssetMenu(menuName = "Jungle Maze/Level Editor Catalog", fileName = "LevelEditorCatalog")]
    public sealed class LevelCatalog : ScriptableObject
    {
        public LevelItemType[] Items = Array.Empty<LevelItemType>();
        public Sprite[] GroundTiles = Array.Empty<Sprite>();
        [Tooltip("Material không cần Light2D (Sprite-Unlit-Default) cho mọi renderer của editor.")]
        public Material SpriteMaterial;

        public LevelItemType Find(string id)
        {
            foreach (var item in Items)
                if (item != null && item.Id == id) return item;
            return null;
        }

        /// <summary>Kích thước gốc theo Id, dùng khi đọc file để suy ra scale.</summary>
        public (float width, float height)? BaseSizeOf(string id)
        {
            var item = Find(id);
            return item == null ? ((float, float)?)null : (item.BaseSize.x, item.BaseSize.y);
        }
    }
}
