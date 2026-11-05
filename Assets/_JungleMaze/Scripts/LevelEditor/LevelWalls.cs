using System.Collections.Generic;
using UnityEngine;

namespace JungleMaze.LevelEditor
{
    /// <summary>
    /// Tường gạch đặt theo ô (loại <see cref="Id"/>): hình tự nối theo ô tường kề nhau, dùng dải tường 4×2 ô của
    /// <c>TX Tileset Wall</c> (cột 1–4, hàng 6 = gờ trên, hàng 7 = mặt gạch). Mỗi ô vẽ mặt gạch tại ô và gờ ở ô phía trên
    /// (tường cao 2 ô theo góc nhìn 3/4); ô có tường ngay phía trên chỉ vẽ mặt gạch nên tường dọc liền một khối.
    /// Logic thuần, dùng chung cho Level Editor và hình trong trận.
    /// </summary>
    public static class LevelWalls
    {
        public const string Id = "Wall";
        private const int LedgeRow = 6, BrickRow = 7;

        /// <summary>Các ô đang có tường (vật đặt theo ô).</summary>
        public static HashSet<Vector2Int> Cells(IEnumerable<LevelPlacement> placements)
        {
            var cells = new HashSet<Vector2Int>();
            foreach (var p in placements)
                if (p.Type == Id && p.OnGrid) cells.Add(new Vector2Int(p.CellX, p.CellY));
            return cells;
        }

        /// <summary>Phần hình của một ô tường theo các ô tường kề (trái/phải chọn đầu tường, trên quyết định có gờ).</summary>
        public static LevelVisualPart[] Parts(Sprite[] wallGrid, HashSet<Vector2Int> walls, int x, int y)
        {
            var left = walls.Contains(new Vector2Int(x - 1, y));
            var right = walls.Contains(new Vector2Int(x + 1, y));
            var above = walls.Contains(new Vector2Int(x, y + 1));
            // Đầu trái = cột 1, đầu phải = cột 4, giữa xen kẽ cột 2/3 cho đỡ lặp; tường đứng một mình dùng cột giữa.
            var col = left && right ? 2 + (x & 1) : right ? 1 : left ? 4 : 2;
            var parts = new List<LevelVisualPart>(2)
            {
                new LevelVisualPart { Sprite = At(wallGrid, BrickRow, col), Offset = new Vector2(.5f, .5f), Rank = 1 },
            };
            if (!above) parts.Add(new LevelVisualPart { Sprite = At(wallGrid, LedgeRow, col), Offset = new Vector2(.5f, 1.5f), Rank = 2 });
            return parts.ToArray();
        }

        private static Sprite At(Sprite[] grid, int row, int col)
        {
            var i = row * LevelGround.WallColumns + col;
            return grid != null && i >= 0 && i < grid.Length ? grid[i] : null;
        }
    }
}
