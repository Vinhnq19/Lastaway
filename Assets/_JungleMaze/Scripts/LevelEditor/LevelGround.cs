using System;
using System.Collections.Generic;
using UnityEngine;

namespace JungleMaze.LevelEditor
{
    /// <summary>
    /// Chọn tile ground theo seed, auto-tile đường đá và tường mép map. Logic thuần, trả index tile:
    /// <c>row * 8 + col</c> trong <c>TX Tileset Grass</c> (row đếm từ trên) và <c>row * 16 + col</c> trong
    /// <c>TX Tileset Wall</c>. Kết quả của một ô chỉ phụ thuộc (seed, x, y) và các ô đường đá kề nó, nên sửa map ở
    /// chỗ này không làm cỏ chỗ khác đổi theo.
    /// </summary>
    public static class LevelGround
    {
        public const int GrassColumns = 8, WallColumns = 16;

        /// <summary>Bit của 4 viên đá 16 px trong một tile 32 px.</summary>
        public const int TL = 8, TR = 4, BL = 2, BR = 1;

        /// <summary>
        /// Mask viên đá của tile hàng 4–7 trong <c>TX Tileset Grass</c>, đo bằng pixel xám từng góc 16×16
        /// (xem Docs/superpowers/plans/2026-11-03-level-editor-cainos.md). 0 = tile không dùng.
        /// </summary>
        private static readonly int[] StoneMasks =
        {
            15, 15, 5, 10, 3, 3, 3, 3,
            15, 15, 5, 10, 12, 12, 12, 12,
            15, 15, 5, 10, 8, 4, 2, 1,
            14, 13, 4, 10, 7, 11, 0, 0,
        };

        private static readonly Dictionary<int, int[]> TilesByMask = BuildMaskTable();

        public static int WallTile(int col, int row) => row * WallColumns + col;

        public static int MaskOf(int grassTile) =>
            grassTile >= 32 && grassTile < 64 ? StoneMasks[grassTile - 32] : 0;

        /// <summary>Hash số nguyên tất định → [0, 1). Không dùng System.Random hay GetHashCode (khác nhau giữa runtime).</summary>
        public static float Hash01(int seed, int x, int y, int salt)
        {
            unchecked
            {
                var h = (uint)seed * 0x9E3779B1u;
                h ^= (uint)x * 0x85EBCA77u;
                h = ((h << 13) | (h >> 19)) * 0xC2B2AE3Du;
                h ^= (uint)y * 0x27D4EB2Fu;
                h = ((h << 15) | (h >> 17)) * 0x165667B1u;
                h ^= (uint)salt * 0x61C88647u;
                h ^= h >> 15; h *= 0x2C1B3C6Du;
                h ^= h >> 12; h *= 0x297A2D39u;
                h ^= h >> 15;
                return (h >> 8) * (1f / 16777216f);
            }
        }

        // ---------- Cỏ ----------

        private const float FlowerClusterSize = 5f, FlowerClusterThreshold = .66f;

        /// <summary>Tile cỏ (hàng 0–3). Cột 0–3 là cỏ trơn/có ngọn; cột 4–7 là hoa/sỏi, mọc thành cụm theo value noise.</summary>
        public static int GrassCell(int seed, int x, int y)
        {
            var cluster = ValueNoise(seed, x / FlowerClusterSize, y / FlowerClusterSize);
            var flower = cluster > FlowerClusterThreshold ? Hash01(seed, x, y, 2) < .55f : Hash01(seed, x, y, 5) < .015f;
            var pick = Hash01(seed, x, y, 3);
            if (flower)
            {
                var i = Mathf.Min(15, (int)(pick * 16f));
                return i / 4 * GrassColumns + 4 + i % 4;
            }
            if (pick < .45f) return 0; // cỏ trơn chiếm phần lớn, nền không bị "rỗ"
            var j = 1 + Mathf.Min(14, (int)((pick - .45f) / .55f * 15f));
            return j / 4 * GrassColumns + j % 4;
        }

        private static float ValueNoise(int seed, float x, float y)
        {
            var x0 = Mathf.FloorToInt(x);
            var y0 = Mathf.FloorToInt(y);
            var tx = Smooth(x - x0);
            var ty = Smooth(y - y0);
            var a = Mathf.Lerp(Hash01(seed, x0, y0, 1), Hash01(seed, x0 + 1, y0, 1), tx);
            var b = Mathf.Lerp(Hash01(seed, x0, y0 + 1, 1), Hash01(seed, x0 + 1, y0 + 1, 1), tx);
            return Mathf.Lerp(a, b, ty);
        }

        private static float Smooth(float t) => t * t * (3f - 2f * t);

        // ---------- Đường đá ----------

        private const float PathGapChance = .12f, FringeStoneChance = .45f;

        /// <summary>
        /// Tile đá cho ô (x, y), -1 nếu ô không có đá. Ô thuộc đường: lát kín (đôi khi thiếu một viên). Ô cỏ kề đường:
        /// chỉ những viên quay về phía đường được phép, mỗi viên giữ theo hash → viền đường tự nối và vỡ tự nhiên.
        /// </summary>
        public static int StoneCell(int seed, int x, int y, Func<int, int, bool> isPath)
        {
            if (isPath(x, y))
            {
                var mask = 15;
                if (Hash01(seed, x, y, 11) < PathGapChance) mask &= ~(1 << (int)(Hash01(seed, x, y, 12) * 4f));
                return TileForMask(mask, Hash01(seed, x, y, 13));
            }

            var allowed = 0;
            if (isPath(x, y + 1)) allowed |= TL | TR;
            if (isPath(x, y - 1)) allowed |= BL | BR;
            if (isPath(x - 1, y)) allowed |= TL | BL;
            if (isPath(x + 1, y)) allowed |= TR | BR;
            if (isPath(x - 1, y + 1)) allowed |= TL;
            if (isPath(x + 1, y + 1)) allowed |= TR;
            if (isPath(x - 1, y - 1)) allowed |= BL;
            if (isPath(x + 1, y - 1)) allowed |= BR;
            if (allowed == 0) return -1;

            var kept = 0;
            for (var bit = 0; bit < 4; bit++)
                if ((allowed & (1 << bit)) != 0 && Hash01(seed, x, y, 20 + bit) < FringeStoneChance) kept |= 1 << bit;
            return kept == 0 ? -1 : TileForMask(kept, Hash01(seed, x, y, 14));
        }

        /// <summary>Tile có đúng mask; mask không có tile (hai viên chéo) thì bỏ bớt viên tới khi có.</summary>
        public static int TileForMask(int mask, float pick)
        {
            if (TilesByMask.TryGetValue(mask, out var tiles)) return tiles[Mathf.Min(tiles.Length - 1, (int)(pick * tiles.Length))];
            var options = new List<int>();
            for (var bit = 0; bit < 4; bit++)
                if ((mask & (1 << bit)) != 0) options.Add(mask & ~(1 << bit));
            return TileForMask(options[Mathf.Min(options.Count - 1, (int)(pick * options.Count))], pick);
        }

        private static Dictionary<int, int[]> BuildMaskTable()
        {
            var lists = new Dictionary<int, List<int>>();
            for (var i = 0; i < StoneMasks.Length; i++)
            {
                if (StoneMasks[i] == 0) continue;
                if (!lists.TryGetValue(StoneMasks[i], out var list)) lists[StoneMasks[i]] = list = new List<int>();
                list.Add(32 + i);
            }
            var table = new Dictionary<int, int[]>();
            foreach (var pair in lists) table[pair.Key] = pair.Value.ToArray();
            return table;
        }

        // ---------- Tường mép map ----------

        public struct BorderCell
        {
            public int X, Y, Tile;
            public BorderCell(int x, int y, int tile) { X = x; Y = y; Tile = tile; }
        }

        private const float WindowChance = .12f;

        /// <summary>
        /// Viền đá mảnh vẽ đè lên các ô mép trong map (trên/trái/phải) và mặt tường dày 2 hàng ngay dưới mép nam
        /// (gờ đá ở y = -1, gạch ở y = -2, cửa sổ xen theo hash). Lấy từ khối hộp cột 1–3, hàng 1–4 của
        /// <c>TX Tileset Wall</c>; cửa sổ là cột 6 hàng 7.
        /// </summary>
        public static List<BorderCell> Border(int width, int height, int seed)
        {
            var cells = new List<BorderCell>();
            for (var x = 0; x < width; x++)
            {
                var col = x == 0 ? 1 : x == width - 1 ? 3 : 2;
                cells.Add(new BorderCell(x, height - 1, WallTile(col, 1)));
                cells.Add(new BorderCell(x, -1, WallTile(col, 3)));
            }
            for (var y = 0; y < height - 1; y++)
            {
                cells.Add(new BorderCell(0, y, WallTile(1, 2)));
                cells.Add(new BorderCell(width - 1, y, WallTile(3, 2)));
            }
            var lastWindow = -10;
            for (var x = 0; x < width; x++)
            {
                var col = x == 0 ? 1 : x == width - 1 ? 3 : 2;
                var window = col == 2 && x > 1 && x < width - 2 && x - lastWindow > 3 && Hash01(seed, x, -2, 30) < WindowChance;
                if (window) lastWindow = x;
                cells.Add(new BorderCell(x, -2, window ? WallTile(6, 7) : WallTile(col, 4)));
            }
            return cells;
        }

        // ---------- Cỏ rải ----------

        /// <summary>Vị trí sprite cỏ/hoa nhỏ rải trên ô trống (không vật chặn, không đường, không spawn).</summary>
        public static List<(Vector2 position, int sprite)> Sprinkles(int seed, int width, int height,
            Func<int, int, bool> blocked, float density, int spriteCount)
        {
            var result = new List<(Vector2, int)>();
            if (spriteCount <= 0 || density <= 0f) return result;
            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                if (Hash01(seed, x, y, 40) >= density || blocked(x, y)) continue;
                var px = x + .15f + .7f * Hash01(seed, x, y, 41);
                var py = y + .15f + .7f * Hash01(seed, x, y, 42);
                result.Add((new Vector2(px, py), Mathf.Min(spriteCount - 1, (int)(Hash01(seed, x, y, 43) * spriteCount))));
            }
            return result;
        }
    }
}
