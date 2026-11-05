using System.Collections.Generic;
using UnityEngine;

namespace JungleMaze.LevelEditor
{
    /// <summary>Vị trí dải đá của một ô tường dọc trong ô (theo phía phòng mà nó khép lại).</summary>
    public enum WallSide { None, Left, Right, Middle }

    /// <summary>
    /// Tường gạch đặt theo ô (loại <see cref="Id"/>): hình tự nối theo ô tường kề nhau, dùng <c>TX Tileset Wall</c>.
    /// Tường ngang (có ô tường trái/phải, hoặc đứng một mình) vẽ mặt gạch (hàng 7, cột 1–4) tại ô và gờ (hàng 6) ở ô phía
    /// trên, theo góc nhìn 3/4. Tường dọc (chỉ nối trên/dưới) là cạnh bên của phòng: vẽ dải đá mảnh như viền map (hàng 2,
    /// cột 1 = dải sát mép trái, cột 3 = dải sát mép phải) và vùng chặn trong game chỉ là dải đó (<see cref="SideCollider"/>).
    /// Logic thuần, dùng chung cho Level Editor, JSON game và hình trong trận.
    /// </summary>
    public static class LevelWalls
    {
        public const string Id = "Wall";
        private const int LedgeRow = 6, BrickRow = 7, SideRow = 2, SideLeftCol = 1, SideRightCol = 3;
        // Phần đục của dải đá trong ô 32 px: ô (2,1) px 0–9, ô (2,3) px 24–31.
        private const float LeftStrip = 10f / 32f, RightStrip = 8f / 32f;
        /// <summary>Dịch ô (2,1) để dải đá nằm giữa ô (tường dọc là vách giữa hai phòng).</summary>
        private const float MiddleShift = .5f - LeftStrip / 2f;

        /// <summary>Các ô đang có tường (vật đặt theo ô).</summary>
        public static HashSet<Vector2Int> Cells(IEnumerable<LevelPlacement> placements)
        {
            var cells = new HashSet<Vector2Int>();
            foreach (var p in placements)
                if (p.Type == Id && p.OnGrid) cells.Add(new Vector2Int(p.CellX, p.CellY));
            return cells;
        }

        private static bool Horizontal(HashSet<Vector2Int> walls, int x, int y) =>
            walls.Contains(new Vector2Int(x - 1, y)) || walls.Contains(new Vector2Int(x + 1, y));

        /// <summary>
        /// Ô tường dọc: không có tường trái/phải nhưng có tường trên hoặc dưới. Phía dải đá theo chỗ đoạn tường dọc nối vào
        /// tường ngang ở hai đầu: nối sang phải (góc trái phòng) → dải sát mép trái; nối sang trái → mép phải; còn lại
        /// (chữ T, hai đầu khác phía, không nối) → giữa ô.
        /// </summary>
        public static WallSide SideOf(HashSet<Vector2Int> walls, int x, int y)
        {
            if (Horizontal(walls, x, y)) return WallSide.None;
            if (!walls.Contains(new Vector2Int(x, y + 1)) && !walls.Contains(new Vector2Int(x, y - 1))) return WallSide.None;
            bool left = false, right = false;
            foreach (var dir in new[] { -1, 1 })
            {
                var cy = y + dir;
                while (walls.Contains(new Vector2Int(x, cy)) && !Horizontal(walls, x, cy)) cy += dir;
                if (!walls.Contains(new Vector2Int(x, cy))) continue;
                if (walls.Contains(new Vector2Int(x + 1, cy))) left = true;
                if (walls.Contains(new Vector2Int(x - 1, cy))) right = true;
            }
            return left && !right ? WallSide.Left : right && !left ? WallSide.Right : WallSide.Middle;
        }

        /// <summary>Vùng chặn của ô tường dọc theo góc dưới-trái ô (khớp phần đục của dải đá); null = cả ô.</summary>
        public static Rect? SideCollider(WallSide side)
        {
            switch (side)
            {
                case WallSide.Left: return new Rect(0f, 0f, LeftStrip, 1f);
                case WallSide.Right: return new Rect(1f - RightStrip, 0f, RightStrip, 1f);
                case WallSide.Middle: return new Rect(MiddleShift, 0f, LeftStrip, 1f);
                default: return null;
            }
        }

        /// <summary>Phần hình của một ô tường theo các ô tường kề (trái/phải chọn đầu tường, trên quyết định có gờ).</summary>
        public static LevelVisualPart[] Parts(Sprite[] wallGrid, HashSet<Vector2Int> walls, int x, int y)
        {
            var side = SideOf(walls, x, y);
            if (side != WallSide.None)
                return new[]
                {
                    new LevelVisualPart
                    {
                        Sprite = At(wallGrid, SideRow, side == WallSide.Right ? SideRightCol : SideLeftCol),
                        Offset = new Vector2(.5f + (side == WallSide.Middle ? MiddleShift : 0f), .5f), Rank = 1,
                    },
                };
            var left = walls.Contains(new Vector2Int(x - 1, y));
            var right = walls.Contains(new Vector2Int(x + 1, y));
            // Gờ vẽ ở ô trên trừ khi ô trên là tường ngang (tường dày 2 hàng); dải đá dọc nối vào gờ của góc.
            var above = walls.Contains(new Vector2Int(x, y + 1)) && SideOf(walls, x, y + 1) == WallSide.None;
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
