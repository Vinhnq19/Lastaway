using JungleMaze.Presentation.UI.Hud;
using NUnit.Framework;

namespace JungleMaze.Tests.EditMode.Hud
{
    /// <summary>T-022 kill feed rows: capacity, order, expiry (pure, no prefab).</summary>
    public sealed class KillFeedLogTests
    {
        [Test]
        public void Push_KeepsNewestAtTheBottom_DropsOldestWhenFull()
        {
            var log = new KillFeedLog<int>(3, 5f);
            Assert.AreEqual(0, log.Count);
            log.Push("A", 1, "B"); log.Push("C", 2, "D"); log.Push("E", 3, "F");
            Assert.AreEqual(3, log.Count);
            log.Push("G", 4, "H");
            Assert.AreEqual(3, log.Count);
            Assert.AreEqual("C", log[0].Killer);
            Assert.AreEqual("G", log[2].Killer);
            Assert.AreEqual(4, log[2].Weapon);
            Assert.Throws<System.ArgumentOutOfRangeException>(() => { var _ = log[3]; });
        }

        [Test]
        public void Tick_RemovesOnlyExpired_AtExactlyTheTimeout()
        {
            var log = new KillFeedLog<int>(3, 5f);
            log.Push("A", 0, "B");
            Assert.False(log.Tick(2f));
            log.Push("C", 0, "D");
            Assert.False(log.Tick(2.5f), "4.5 s is still shown");
            Assert.True(log.Tick(0.5f), "exactly 5 s expires the first row");
            Assert.AreEqual(1, log.Count);
            Assert.AreEqual("C", log[0].Killer);
            Assert.True(log.Tick(10f));
            Assert.AreEqual(0, log.Count);
            Assert.False(log.Tick(1f), "empty log never reports a change");
        }

        [Test]
        public void Capacity_MustBePositive_ClearEmpties()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new KillFeedLog<int>(0, 5f));
            var log = new KillFeedLog<int>(1, 5f);
            log.Push("A", 0, "B"); log.Push("C", 0, "D");
            Assert.AreEqual("C", log[0].Killer);
            log.Clear();
            Assert.AreEqual(0, log.Count);
        }
    }
}
