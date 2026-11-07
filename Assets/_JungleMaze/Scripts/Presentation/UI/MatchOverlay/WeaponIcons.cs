using System;
using JungleMaze.Presentation.UI.Hud;
using UnityEngine;

namespace JungleMaze.Presentation.UI.MatchOverlay
{
    /// <summary>
    /// Weapon -> icon for the spectator target card and the eliminated cause card (T-028 weapons-icons.md). The UI-03
    /// sources carry the weapon as a balance ID or as its Vietnamese name; both resolve through the HUD's own tables
    /// (<see cref="HudItemIconCatalog"/>, <see cref="HudItemNames"/>, read only). Unknown or missing: null, and the view
    /// hides the icon and keeps the text. Called when the weapon changes, not per frame.
    /// </summary>
    public static class WeaponIcons
    {
        public static Sprite Find(HudItemIconCatalog catalog, string weapon)
        {
            if (catalog == null || string.IsNullOrEmpty(weapon)) return null;
            var byId = catalog.GetIcon(weapon);
            if (byId != null) return byId;
            foreach (var entry in catalog.Entries)
                if (string.Equals(HudItemNames.Get(entry.Id), weapon, StringComparison.OrdinalIgnoreCase)) return entry.Icon;
            return null;
        }
    }
}
