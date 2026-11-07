using System;
using System.Collections.Generic;
using JungleMaze.App.Flow;
using JungleMaze.App.MatchUi;
using JungleMaze.Core.Events;
using JungleMaze.Presentation.UI.Kit;
using JungleMaze.Presentation.UI.MatchOverlay;
using NUnit.Framework;
using UnityEngine;

namespace JungleMaze.Tests.MatchUi
{
    /// <summary>
    /// CPO 07/11: with the bag or big map open, ESC closes that panel first and the menu stays shut; the next ESC opens
    /// the menu. Once the menu is open, ESC keeps closing the menu's own layer and leaves the panel stack alone.
    /// </summary>
    public sealed class MatchOverlayEscLayerTests
    {
        private MatchMenuController _menu;
        private MatchOverlayInput _input;
        private readonly HashSet<KeyCode> _keys = new HashSet<KeyCode>();

        [SetUp]
        public void SetUp()
        {
            UiLayerStack.Clear();
            _menu = new MatchMenuController(new EventBus(), () => 0d, () => Guid.NewGuid(), AppScreen.InMatch);
            _input = new MatchOverlayInput(_menu, null, _keys.Contains);
        }

        [TearDown]
        public void TearDown()
        {
            UiLayerStack.Clear();
            _menu?.Dispose();
        }

        private void Escape()
        {
            _keys.Clear();
            _keys.Add(KeyCode.Escape);
            _input.Poll();
        }

        [Test]
        public void OpenLayer_EscClosesIt_MenuStaysShut_NextEscOpensMenu()
        {
            var closed = 0;
            var bag = new object();
            UiLayerStack.Push(bag, () => closed++);

            Escape();
            Assert.AreEqual(1, closed, "the bag closes");
            Assert.AreEqual(0, UiLayerStack.Count);
            Assert.IsFalse(_menu.IsOpen, "ESC swallowed by the panel");

            Escape();
            Assert.IsTrue(_menu.IsOpen, "no panel left: ESC opens the menu");
            Assert.AreEqual(1, closed);
        }

        [Test]
        public void TwoLayers_TopClosesFirst()
        {
            var order = new List<string>();
            UiLayerStack.Push("bag", () => order.Add("bag"));
            UiLayerStack.Push("map", () => order.Add("map"));
            Escape();
            Escape();
            CollectionAssert.AreEqual(new[] { "map", "bag" }, order);
            Assert.IsFalse(_menu.IsOpen);
            Escape();
            Assert.IsTrue(_menu.IsOpen);
        }

        [Test]
        public void NoLayer_EscOpensMenu()
        {
            Escape();
            Assert.IsTrue(_menu.IsOpen);
        }

        [Test]
        public void MenuOpen_EscClosesMenu_LeavesLayers()
        {
            Escape();
            Assert.IsTrue(_menu.IsOpen);
            var closed = 0;
            UiLayerStack.Push("map", () => closed++);
            Escape();
            Assert.IsFalse(_menu.IsOpen, "old behaviour: ESC closes one layer of the menu");
            Assert.AreEqual(0, closed);
            Assert.AreEqual(1, UiLayerStack.Count);
        }
    }
}
