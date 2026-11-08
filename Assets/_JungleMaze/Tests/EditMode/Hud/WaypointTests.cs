using JungleMaze.App.Hud;
using JungleMaze.Presentation.UI.Hud;
using NUnit.Framework;
using UnityEngine;

namespace JungleMaze.Tests.Hud
{
    /// <summary>T-018 D1 waypoint, the numeric examples of de-xuat/minimap-chuc-nang.md 5 (map 50 cells, reach 1.5).</summary>
    public sealed class WaypointTests
    {
        private const float Reach = 1.5f;

        [Test]
        public void Defaults()
        {
            var t = WaypointTuning.Default;
            Assert.AreEqual((1.5f, 18f, 24f, 40f, 40f), (t.ReachCells, t.MinimapFlagPx, t.BigMapFlagPx, t.WorldFlagPx, t.EdgeArrowPx));
        }

        [Test]
        public void W1_ClickSetsTheFlag_ClampedToTheMap()
        {
            var w = new Waypoint();
            w.Click(40.2f, 35.7f, 50, Reach);
            Assert.AreEqual((true, 40.2f, 35.7f), (w.Has, w.X, w.Y));
            w.Click(-3f, 55f, 50, Reach);
            Assert.AreEqual((0f, 50f), (w.X, w.Y));
        }

        [Test]
        public void W2_NewFlagReplacesTheOld()
        {
            var w = new Waypoint();
            w.Click(40f, 35f, 50, Reach);
            w.Click(12f, 8f, 50, Reach);
            Assert.AreEqual((true, 12f, 8f), (w.Has, w.X, w.Y));
        }

        [Test]
        public void W3_ClickWithin1Point5Cells_RemovesIt_RightClickClears()
        {
            var w = new Waypoint();
            w.Click(40f, 35f, 50, Reach);
            w.Click(41f, 35.5f, 50, Reach); // 1.1 cells
            Assert.IsFalse(w.Has);
            w.Click(40f, 35f, 50, Reach);
            w.Click(41.6f, 35f, 50, Reach); // 1.6 cells: a new flag there
            Assert.AreEqual((true, 41.6f), (w.Has, w.X));
            w.Clear();
            Assert.IsFalse(w.Has);
        }

        [Test]
        public void W4_HeroWithin1Point5Cells_RemovesIt()
        {
            var w = new Waypoint();
            w.Click(40f, 35f, 50, Reach);
            w.Tick(true, true, 10f, 10f, Reach);
            Assert.That(w.DistanceTo(10f, 10f), Is.EqualTo(39.05f).Within(0.01f));
            Assert.IsTrue(w.Has, "39.1 cells away");
            w.Tick(true, true, 39f, 34f, Reach);
            Assert.IsFalse(w.Has, "1.4 cells: removed");
        }

        [Test]
        public void W5_W6_NoTimeout_DeathRemoves_NoPoseKeeps()
        {
            var w = new Waypoint();
            w.Click(40f, 35f, 50, Reach);
            for (var i = 0; i < 10000; i++) w.Tick(true, false, 0f, 0f, Reach);
            Assert.IsTrue(w.Has, "no timeout, no pose (reconnect) keeps it");
            w.Tick(false, true, 0f, 0f, Reach);
            Assert.IsFalse(w.Has, "hero dead");
        }

        private static readonly Rect Screen = new Rect(-960f, -540f, 1920f, 1080f);

        [Test]
        public void Edge_TargetOnScreen_NoArrow()
        {
            Assert.IsFalse(WaypointEdge.Place(Vector2.zero, new Vector2(300f, 200f), Screen, null, 36f, out var p));
            Assert.AreEqual(new Vector2(300f, 200f), p);
        }

        [Test]
        public void Edge_OffScreenLeft_ArrowOnTheLeftEdge_SameDirection()
        {
            Assert.IsTrue(WaypointEdge.Place(Vector2.zero, new Vector2(-3000f, -600f), Screen, null, 36f, out var p));
            Assert.That(p.x, Is.EqualTo(-960f + 36f).Within(1e-3f));
            Assert.That(Mathf.Atan2(p.y, p.x), Is.EqualTo(Mathf.Atan2(-600f, -3000f)).Within(1e-4f), "points at the flag");
        }

        [Test]
        public void Edge_KeepsOffAPanel_StillPointingAtTheFlag()
        {
            var vitals = new Rect(-940f, -520f, 420f, 200f); // bottom-left block
            Assert.IsTrue(WaypointEdge.Place(Vector2.zero, new Vector2(-2000f, -1000f), Screen, new[] { vitals }, 16f, out var p));
            var grown = Rect.MinMaxRect(vitals.xMin - 16f, vitals.yMin - 16f, vitals.xMax + 16f, vitals.yMax + 16f);
            Assert.IsFalse(grown.Contains(p + (p.normalized * 0.01f)) && grown.Contains(p - p.normalized * 0.5f), "stops at the panel's border, outside it");
            Assert.That(Mathf.Atan2(p.y, p.x), Is.EqualTo(Mathf.Atan2(-1000f, -2000f)).Within(1e-4f));
            Assert.That(p.magnitude, Is.LessThan(new Vector2(-960f + 16f, -480f + 8f).magnitude), "stops before the screen edge");
        }
    }
}
