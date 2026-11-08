using JungleMaze.App.Hud;
using NUnit.Framework;

namespace JungleMaze.Tests.Hud
{
    /// <summary>T-018 minimap levels, numbers from de-xuat/minimap-chuc-nang.md 4.1 and 7 (map 50 cells, near 30).</summary>
    public sealed class MinimapZoomTests
    {
        private static readonly MinimapTuning Tuning = MinimapTuning.Default;

        [Test]
        public void Defaults_AreTheDesignerNumbers()
        {
            Assert.AreEqual(30f, Tuning.NearCells);
            Assert.AreEqual(0.15f, Tuning.TweenSeconds);
        }

        [Test]
        public void Level1_IsTheWholeMap_WhereverTheHeroIs()
        {
            var zoom = new MinimapZoom();
            zoom.Tick(0.016f, Tuning, 50, true, 3f, 47f);
            Assert.AreEqual(MinimapZoom.Full, zoom.Level);
            Assert.AreEqual((0f, 0f, 50f), (zoom.MinX, zoom.MinY, zoom.Cells));
        }

        [TestCase(3f, 47f, 0f, 20f)]   // corner: window x 0-30, y 20-50 (4.1)
        [TestCase(25f, 25f, 10f, 10f)] // centre: window 10-40
        [TestCase(48f, 25f, 20f, 10f)]
        public void Level2_FollowsTheHero_ClampedInsideTheMap(float hx, float hy, float minX, float minY)
        {
            MinimapZoom.Window(30f, 50, true, hx, hy, out var x, out var y, out var side);
            Assert.AreEqual((minX, minY, 30f), (x, y, side));
        }

        [Test]
        public void Level2_CornerHero_SitsAt27x241OnA269Map()
        {
            var zoom = new MinimapZoom();
            zoom.SetLevel(MinimapZoom.Near);
            zoom.Tick(0.016f, Tuning, 50, true, 3f, 47f);
            var full = new MinimapProjection(50, 269f);
            full.Project(3f, 47f, out var fx, out var fy);
            Assert.That(zoom.ToWindow(fx, zoom.MinX, 269f, 50), Is.EqualTo(3f / 30f * 269f).Within(1e-3f));
            Assert.That(zoom.ToWindow(fy, zoom.MinY, 269f, 50), Is.EqualTo(27f / 30f * 269f).Within(1e-3f));
        }

        [Test]
        public void NoHero_NearWindowSitsMidMap()
        {
            MinimapZoom.Window(30f, 50, false, 0f, 0f, out var x, out var y, out _);
            Assert.AreEqual((10f, 10f), (x, y));
        }

        [Test]
        public void LevelChange_BlendsOverTweenSeconds_Unscaled()
        {
            var zoom = new MinimapZoom();
            zoom.Tick(0.016f, Tuning, 50, true, 25f, 25f);
            zoom.Toggle();
            var frames = 0;
            var previous = zoom.Cells;
            while (zoom.Cells != 30f && frames < 100)
            {
                zoom.Tick(0.01f, Tuning, 50, true, 25f, 25f);
                Assert.That(zoom.Cells, Is.LessThanOrEqualTo(previous));
                previous = zoom.Cells;
                frames++;
            }
            Assert.That(frames, Is.InRange(14, 16), "0.15 s at 10 ms frames");
            Assert.IsFalse(zoom.Blending);
        }

        [Test]
        public void FivePressesInOneSecond_EndOnLevel2_BlendingFromWhereItWas()
        {
            var zoom = new MinimapZoom();
            zoom.Tick(0.016f, Tuning, 50, true, 25f, 25f);
            for (var i = 0; i < 5; i++)
            {
                zoom.Toggle();
                zoom.Tick(0.05f, Tuning, 50, true, 25f, 25f); // mid blend: next press starts from the window on screen
                Assert.That(zoom.Cells, Is.InRange(30f, 50f));
            }
            Assert.AreEqual(MinimapZoom.Near, zoom.Level);
            for (var i = 0; i < 20; i++) zoom.Tick(0.016f, Tuning, 50, true, 25f, 25f);
            Assert.AreEqual((10f, 10f, 30f), (zoom.MinX, zoom.MinY, zoom.Cells));
        }

        [Test]
        public void SetLevel_SameLevel_DoesNotRestartTheBlend()
        {
            var zoom = new MinimapZoom();
            zoom.Tick(0.016f, Tuning, 50, true, 25f, 25f);
            zoom.SetLevel(MinimapZoom.Full);
            Assert.IsFalse(zoom.Blending);
        }

        [Test]
        public void NearCellsLargerThanTheMap_ShowsTheMap()
        {
            MinimapZoom.Window(30f, 20, true, 5f, 5f, out var x, out var y, out var side);
            Assert.AreEqual((0f, 0f, 20f), (x, y, side));
        }
    }
}
