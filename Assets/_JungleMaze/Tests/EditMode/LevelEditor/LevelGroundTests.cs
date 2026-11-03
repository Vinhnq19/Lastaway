using System.Collections.Generic;
using JungleMaze.LevelEditor;
using NUnit.Framework;

namespace JungleMaze.Tests.LevelEditor
{
    /// <summary>Ground sinh theo seed phải tất định; đường đá tự nối theo ô lân cận; tường mép đủ hai hàng.</summary>
    public sealed class LevelGroundTests
    {
        private static int[] Ground(int seed, int size)
        {
            var cells = new int[size * size];
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++) cells[y * size + x] = LevelGround.GrassCell(seed, x, y);
            return cells;
        }

        private static int Mask(int tile) => LevelGround.MaskOf(tile);

        [Test] public void SameSeedSameGround() => CollectionAssert.AreEqual(Ground(42, 50), Ground(42, 50));

        [Test] public void DifferentSeedDifferentGround()
        {
            var a = Ground(1, 50);
            var b = Ground(2, 50);
            var differ = 0;
            for (var i = 0; i < a.Length; i++) if (a[i] != b[i]) differ++;
            Assert.Greater(differ, a.Length * 3 / 10);
        }

        [Test] public void GroundUsesOnlyGrassRowsAndHasFlowerClusters()
        {
            var flowers = 0;
            foreach (var tile in Ground(7, 50))
            {
                Assert.Less(tile, 32, "hàng 0–3 của tileset cỏ");
                if (tile % 8 >= 4) flowers++;
            }
            Assert.Greater(flowers, 50);
            Assert.Less(flowers, 50 * 50 / 3, "hoa/sỏi là điểm nhấn, không phủ kín");
        }

        [Test] public void NegativeAndHugeSeedsAreDeterministic()
        {
            CollectionAssert.AreEqual(Ground(-1, 20), Ground(-1, 20));
            CollectionAssert.AreEqual(Ground(int.MaxValue, 20), Ground(int.MaxValue, 20));
            CollectionAssert.AreEqual(Ground(int.MinValue, 20), Ground(int.MinValue, 20));
        }

        [Test] public void Hash01StaysInRange()
        {
            for (var i = -500; i < 500; i++)
            {
                var v = LevelGround.Hash01(i * 7919, i, -i, 3);
                Assert.GreaterOrEqual(v, 0f);
                Assert.Less(v, 1f);
            }
        }

        [Test] public void PathCellAlwaysHasStone()
        {
            var path = new HashSet<(int, int)> { (5, 5) };
            for (var seed = 0; seed < 40; seed++)
                Assert.GreaterOrEqual(LevelGround.StoneCell(seed, 5, 5, (x, y) => path.Contains((x, y))), 32);
        }

        [Test] public void NorthNeighbourOfPathOnlyUsesBottomStones()
        {
            var path = new HashSet<(int, int)> { (5, 5) };
            for (var seed = 0; seed < 40; seed++)
            {
                var tile = LevelGround.StoneCell(seed, 5, 6, (x, y) => path.Contains((x, y)));
                if (tile < 0) continue;
                Assert.AreEqual(0, Mask(tile) & (LevelGround.TL | LevelGround.TR), $"seed {seed}: viên ở nửa trên không giáp đường");
            }
        }

        [Test] public void DiagonalNeighbourUsesSingleCornerStone()
        {
            var path = new HashSet<(int, int)> { (5, 5) };
            for (var seed = 0; seed < 40; seed++)
            {
                var tile = LevelGround.StoneCell(seed, 6, 6, (x, y) => path.Contains((x, y)));
                if (tile >= 0) Assert.AreEqual(LevelGround.BL, Mask(tile), "ô đông-bắc chỉ có viên góc tây-nam");
            }
        }

        [Test] public void CellFarFromPathHasNoStone() =>
            Assert.AreEqual(-1, LevelGround.StoneCell(3, 10, 10, (x, y) => x == 5 && y == 5));

        [Test] public void FringeStonesAppearForSomeSeeds()
        {
            var path = new HashSet<(int, int)> { (5, 5) };
            var seen = 0;
            for (var seed = 0; seed < 40; seed++)
                if (LevelGround.StoneCell(seed, 5, 6, (x, y) => path.Contains((x, y))) >= 0) seen++;
            Assert.Greater(seen, 10, "viền đường có viên đá lẻ");
        }

        [Test] public void EveryMaskMapsToARealTile()
        {
            for (var mask = 1; mask < 16; mask++)
            {
                var tile = LevelGround.TileForMask(mask, .37f);
                Assert.AreEqual(mask & Mask(tile), Mask(tile), $"mask {mask}: tile không được thêm viên ngoài mask");
                Assert.AreNotEqual(0, Mask(tile));
            }
        }

        [Test] public void BorderHasTwoWallRowsBelowSouthEdge()
        {
            var border = LevelGround.Border(10, 10, 5);
            var below = new Dictionary<int, int>();
            foreach (var cell in border)
                if (cell.Y < 0) below[cell.Y] = below.TryGetValue(cell.Y, out var n) ? n + 1 : 1;
            Assert.AreEqual(10, below[-1]);
            Assert.AreEqual(10, below[-2]);
            Assert.AreEqual(2, below.Count);
            Assert.IsTrue(border.Exists(c => c.X == 0 && c.Y == 9 && c.Tile == LevelGround.WallTile(1, 1)), "góc trên-trái");
        }
    }
}
