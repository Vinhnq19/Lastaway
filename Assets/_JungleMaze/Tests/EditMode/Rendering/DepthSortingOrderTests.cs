using JungleMaze.Presentation.Rendering;
using NUnit.Framework;

namespace JungleMaze.Tests.Rendering
{
    /// <summary>Công thức Y-sort thuần: cao hơn (y lớn) thì order nhỏ hơn, tức vẽ phía sau.</summary>
    public sealed class DepthSortingOrderTests
    {
        [Test] public void HigherYGetsLowerOrder()
        {
            Assert.Less(DepthSortingSystem.ComputeOrder(10.5f, 100f), DepthSortingSystem.ComputeOrder(10.4f, 100f));
            Assert.AreEqual(-1050, DepthSortingSystem.ComputeOrder(10.5f, 100f));
            Assert.AreEqual(250, DepthSortingSystem.ComputeOrder(-2.5f, 100f));
        }

        [Test] public void PrecisionSeparatesCentimetres()
        {
            Assert.AreNotEqual(DepthSortingSystem.ComputeOrder(3.00f, 100f), DepthSortingSystem.ComputeOrder(3.01f, 100f));
            Assert.AreEqual(DepthSortingSystem.ComputeOrder(3.00f, 10f), DepthSortingSystem.ComputeOrder(3.01f, 10f));
        }

        [Test] public void OrderIsClampedToSixteenBitRange()
        {
            Assert.AreEqual(short.MinValue, DepthSortingSystem.ComputeOrder(1000f, 100f));
            Assert.AreEqual(short.MaxValue, DepthSortingSystem.ComputeOrder(-1000f, 100f));
        }
    }
}
