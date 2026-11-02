using UnityEngine;

namespace JungleMaze.Presentation.Rendering
{
    /// <summary>
    /// Sorting Layer của world, từ sau ra trước: Ground (nền, hazard phẳng) → WorldDynamic (entity Y-sort) →
    /// Default (nội dung cũ chưa Y-sort: projectile, swing, item rơi, vật thể world) → Foreground (tán cây, mái
    /// che player). WorldDynamic/Foreground do menu "Jungle Maze/Rendering/Apply Depth Sorting Setup" tạo.
    /// </summary>
    public static class SortingLayers
    {
        public const string Ground = "Ground";
        public const string WorldDynamic = "WorldDynamic";
        public const string Default = "Default";
        public const string Foreground = "Foreground";

        /// <summary>ID của layer; layer chưa tồn tại thì rơi về Default (0) thay vì lỗi.</summary>
        public static int IdOf(string layerName)
        {
            var id = SortingLayer.NameToID(layerName);
            return SortingLayer.IsValid(id) ? id : 0;
        }
    }
}
