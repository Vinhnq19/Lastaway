using System;
using UnityEngine;

namespace JungleMaze.LevelEditor
{
    /// <summary>Một sprite con của prefab Cainos (thân, tán, bóng...), vị trí tính từ góc dưới-trái footprint.</summary>
    [Serializable]
    public sealed class LevelVisualPart
    {
        public Sprite Sprite;
        public Vector2 Offset;
        public Vector2 Scale = Vector2.one;
        public bool FlipX;
        [Tooltip("Thứ tự vẽ trong vật: bóng < thân < tán.")]
        public int Rank;
        public bool Shadow;
    }

    /// <summary>Một mẫu hình của vật (Pot 01–03, Bush 01–06 dùng chung một loại trong game).</summary>
    [Serializable]
    public sealed class LevelVisualVariant
    {
        public LevelVisualPart[] Parts = Array.Empty<LevelVisualPart>();
    }

    /// <summary>Một loại vật thể trong bảng chọn của Level Editor. Hình và collider chân lấy từ prefab Cainos.</summary>
    [Serializable]
    public sealed class LevelItemType
    {
        [Tooltip("Tên ghi ra JSON: tên cover, tên WorldObjectKind, tên decoration hoặc 'Spawn'.")]
        public string Id;
        public string DisplayName;
        public LevelItemGroup Group;
        [Tooltip("Số ô chiếm trên map (đặt theo ô).")]
        public Vector2Int Footprint = Vector2Int.one;
        [Tooltip("Collider chân theo góc dưới-trái footprint; rỗng = cả footprint.")]
        public Rect[] Colliders = Array.Empty<Rect>();
        [Tooltip("Các mẫu hình; editor chọn theo seed và ô nên map đa dạng mà file không đổi.")]
        public LevelVisualVariant[] Variants = Array.Empty<LevelVisualVariant>();
        [Tooltip("Ảnh đại diện ở bảng chọn.")]
        public Sprite Sprite;
        [Tooltip("Màu viền footprint và màu khối khi không có hình.")]
        public Color Tint = Color.white;
        [Tooltip("Vẽ dưới các vật thể đứng (spawn).")]
        public bool Flat;
        [TextArea] public string Note;

        /// <summary>Kích thước gốc cho map cũ (lệch ô) = footprint.</summary>
        public Vector2 BaseSize => Footprint;
        public LevelItemShape Shape => new LevelItemShape(Footprint.x, Footprint.y, Colliders);
    }

    /// <summary>Danh sách vật thể, tile ground và tile tường cho Level Editor. Sinh bởi menu Jungle Maze/Level Editor.</summary>
    [CreateAssetMenu(menuName = "Jungle Maze/Level Editor Catalog", fileName = "LevelEditorCatalog")]
    public sealed class LevelCatalog : ScriptableObject
    {
        public LevelItemType[] Items = Array.Empty<LevelItemType>();
        [Tooltip("TX Tileset Grass theo vị trí: index = hàng (từ trên) × 8 + cột.")]
        public Sprite[] GrassGrid = Array.Empty<Sprite>();
        [Tooltip("TX Tileset Wall theo vị trí: index = hàng (từ trên) × 16 + cột.")]
        public Sprite[] WallGrid = Array.Empty<Sprite>();
        [Tooltip("Sprite cỏ/hoa nhỏ rải theo seed.")]
        public Sprite[] Sprinkles = Array.Empty<Sprite>();
        [Tooltip("Material không cần Light2D (Sprite-Unlit-Default) cho mọi renderer của editor.")]
        public Material SpriteMaterial;

        public LevelItemType Find(string id)
        {
            foreach (var item in Items)
                if (item != null && item.Id == id) return item;
            return null;
        }

        /// <summary>Kích thước gốc theo Id, dùng khi đọc map cũ để suy ra scale.</summary>
        public (float width, float height)? BaseSizeOf(string id)
        {
            var item = Find(id);
            return item == null ? ((float, float)?)null : (item.BaseSize.x, item.BaseSize.y);
        }

        public LevelItemShape? ShapeOf(string id) => Find(id)?.Shape;
    }
}
