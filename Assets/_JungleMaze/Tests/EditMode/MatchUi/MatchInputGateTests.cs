using System;
using JungleMaze.App.Flow;
using JungleMaze.App.MatchUi;
using JungleMaze.Core.Events;
using JungleMaze.Presentation.UI.Kit;
using JungleMaze.Presentation.UI.MatchOverlay;
using NUnit.Framework;

namespace JungleMaze.Tests.MatchUi
{
    /// <summary>
    /// T-018 M1: with the big map open (a layer pushed with blocksActions) gameplay readers see BlocksGameplayInput, as
    /// with the ESC menu; the bag does not block; IsOpen and OpenChanged stay the menu's own (the input guard and the
    /// offline movement switch react to the menu only).
    /// </summary>
    public sealed class MatchInputGateTests
    {
        private MatchMenuController _menu;
        private MatchInputGate _gate;

        [SetUp]
        public void SetUp()
        {
            UiLayerStack.Clear();
            _menu = new MatchMenuController(new EventBus(), () => 0d, () => Guid.NewGuid(), AppScreen.InMatch);
            _gate = new MatchInputGate(_menu);
        }

        [TearDown]
        public void TearDown()
        {
            UiLayerStack.Clear();
            _gate.Dispose();
            _menu.Dispose();
        }

        [Test]
        public void BigMapOpen_BlocksActions_OneHundredOpenCloseCycles()
        {
            var map = new object();
            for (var i = 0; i < 100; i++)
            {
                UiLayerStack.Push(map, null, blocksActions: true);
                Assert.IsTrue(_gate.BlocksGameplayInput, "open " + i);
                Assert.IsFalse(_gate.IsOpen, "the menu itself stays shut");
                UiLayerStack.Remove(map);
                Assert.IsFalse(_gate.BlocksGameplayInput, "closed " + i);
            }
        }

        [Test]
        public void Bag_DoesNotBlock()
        {
            UiLayerStack.Push("bag", null);
            Assert.IsFalse(_gate.BlocksGameplayInput);
        }

        [Test]
        public void EscClosingTheMap_ReleasesTheBlock()
        {
            var map = new object();
            UiLayerStack.Push(map, () => UiLayerStack.Remove(map), blocksActions: true);
            Assert.IsTrue(UiLayerStack.TryCloseTop());
            Assert.IsFalse(_gate.BlocksGameplayInput);
        }

        [Test]
        public void Menu_StillBlocks_AndOpenChangedIsTheMenus()
        {
            var events = 0;
            _gate.OpenChanged += _ => events++;
            _menu.PressEscape();
            Assert.IsTrue(_gate.IsOpen);
            Assert.IsTrue(_gate.BlocksGameplayInput);
            Assert.AreEqual(1, events);
            Assert.IsTrue(MatchInputGate.MenuOpen, "the big map does not open over the menu");
        }

        [Test]
        public void OutsideAMatch_TheMapLayerDoesNotBlock()
        {
            var lobby = new MatchMenuController(new EventBus(), () => 0d, () => Guid.NewGuid(), AppScreen.MainMenu);
            using (var gate = new MatchInputGate(lobby))
            {
                UiLayerStack.Push("map", null, blocksActions: true);
                Assert.IsFalse(gate.BlocksGameplayInput);
            }
            lobby.Dispose();
        }
    }
}
