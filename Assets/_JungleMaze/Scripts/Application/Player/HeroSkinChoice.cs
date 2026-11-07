using System;

namespace JungleMaze.App.Player
{
    /// <summary>
    /// The hero skin the player picked on Home, kept on this device. <see cref="HeroSkinRules.Automatic"/> means "not
    /// chosen": offline uses the default hero, online the skin the room assigns by NetId. Only the local player's own
    /// character uses the choice for now; other players still see the NetId skin until the server relays choices.
    /// </summary>
    public interface IHeroSkinPreference
    {
        /// <summary>Chosen skin index into the hero visual set, or <see cref="HeroSkinRules.Automatic"/>.</summary>
        int Selected { get; }

        /// <summary>Stores a choice (clamped to Automatic for negatives) and raises <see cref="Changed"/> when it differs.</summary>
        void Select(int skin);

        event Action Changed;
    }

    /// <summary>Pure skin rules shared by the picker, the offline hero and player views.</summary>
    public static class HeroSkinRules
    {
        public const int Automatic = -1;

        /// <summary>Skin to draw for a player: the local choice on your own character, otherwise the NetId skin.</summary>
        public static int Resolve(int selected, bool isOwnCharacter, byte netId) =>
            isOwnCharacter && selected >= 0 ? selected : netId;

        /// <summary>Skin for the offline hero: the choice, or the default (0) when none was made.</summary>
        public static int Offline(int selected) => selected >= 0 ? selected : 0;

        /// <summary>
        /// Steps through Automatic, 0 .. count-1 and wraps both ways, so ◀ from Automatic goes to the last skin.
        /// A stale index beyond <paramref name="count"/> (fewer skins after an art update) restarts from Automatic.
        /// </summary>
        public static int Step(int selected, int count, int direction)
        {
            if (count <= 0) return Automatic;
            var slots = count + 1;
            var slot = selected < 0 || selected >= count ? 0 : selected + 1;
            slot = ((slot + Math.Sign(direction)) % slots + slots) % slots;
            return slot - 1;
        }
    }
}
