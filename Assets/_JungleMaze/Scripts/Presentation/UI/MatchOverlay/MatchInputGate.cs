using System;
using JungleMaze.App.MatchUi;
using JungleMaze.Presentation.UI.Kit;

namespace JungleMaze.Presentation.UI.MatchOverlay
{
    /// <summary>
    /// T-018 M1 from the UI side: the <see cref="IMatchMenuState"/> that gameplay readers get. It is the ESC menu
    /// (<see cref="MatchMenuController"/>) plus the in-match panels that block actions (<see cref="UiLayerStack.BlocksActions"/>,
    /// the big map): attack, reload, throwables and item use stop while the big map is open, as with the menu. Movement
    /// is not read from here (MatchDemoView gets the menu itself; online movement never reads this flag), so the hero
    /// still walks with the big map open. <see cref="IsOpen"/> and <see cref="OpenChanged"/> are the menu's own.
    /// </summary>
    public sealed class MatchInputGate : IMatchMenuState, IDisposable
    {
        private readonly MatchMenuController _menu;

        public MatchInputGate(MatchMenuController menu)
        {
            _menu = menu ?? throw new ArgumentNullException(nameof(menu));
            Current = this;
        }

        /// <summary>The gate of the running match (null outside Bootstrap, e.g. HUD tests).</summary>
        public static MatchInputGate Current { get; private set; }

        /// <summary>The ESC menu is open: the big map does not open over it (de-xuat/minimap-chuc-nang.md 7).</summary>
        public static bool MenuOpen => Current != null && Current._menu.IsOpen;

        public bool IsOpen => _menu.IsOpen;

        public bool BlocksGameplayInput => _menu.BlocksGameplayInput || (_menu.InMatch && UiLayerStack.BlocksActions);

        public event Action<bool> OpenChanged
        {
            add => _menu.OpenChanged += value;
            remove => _menu.OpenChanged -= value;
        }

        public void Dispose()
        {
            if (Current == this) Current = null;
        }
    }
}
