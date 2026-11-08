using System;
using System.Collections.Generic;

namespace JungleMaze.Presentation.UI.Kit
{
    /// <summary>
    /// In-match panels that sit above the HUD (bag, big map). ESC closes the top one first; only when none is open
    /// does ESC reach the UI-03 menu (CPO 07/11). A panel pushes its close action when it opens and removes it when
    /// it closes; the ESC owner calls <see cref="TryCloseTop"/> before opening the menu.
    /// A layer pushed with <c>blocksActions</c> (big map, T-018 M1) stops attack, reload and item use while it is open;
    /// movement keeps going (<see cref="BlocksActions"/>, read through MatchInputGate).
    /// </summary>
    public static class UiLayerStack
    {
        private static readonly List<(object owner, Action close, bool blocks)> Layers = new List<(object, Action, bool)>();

        public static int Count => Layers.Count;

        /// <summary>True while an open layer blocks gameplay actions (not movement).</summary>
        public static bool BlocksActions
        {
            get
            {
                foreach (var layer in Layers) if (layer.blocks) return true;
                return false;
            }
        }

        public static void Push(object owner, Action close, bool blocksActions = false)
        {
            Remove(owner);
            Layers.Add((owner, close, blocksActions));
        }

        public static void Remove(object owner)
        {
            for (var i = Layers.Count - 1; i >= 0; i--)
                if (ReferenceEquals(Layers[i].owner, owner)) Layers.RemoveAt(i);
        }

        /// <summary>Closes the most recently opened panel. False when nothing was open (ESC may open the menu).</summary>
        public static bool TryCloseTop()
        {
            if (Layers.Count == 0) return false;
            var top = Layers[Layers.Count - 1];
            Layers.RemoveAt(Layers.Count - 1);
            top.close?.Invoke();
            return true;
        }

        /// <summary>Test and scene-reload hygiene.</summary>
        public static void Clear() => Layers.Clear();
    }
}
