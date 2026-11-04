using System;
using JungleMaze.App.Player;
using UnityEngine;

namespace JungleMaze.Infrastructure.Settings
{
    /// <summary>
    /// <see cref="IHeroSkinPreference"/> in PlayerPrefs (<c>jm.hero.skin</c>). A missing or non-integer key reads as
    /// Automatic. Writes are saved immediately: the choice changes only on a button press.
    /// </summary>
    public sealed class PlayerPrefsHeroSkinPreference : IHeroSkinPreference
    {
        public const string DefaultKey = "jm.hero.skin";
        private readonly string _key;

        /// <param name="key">Tests pass their own key so they never touch the player's real choice.</param>
        public PlayerPrefsHeroSkinPreference(string key = DefaultKey)
        {
            _key = key;
            Selected = PlayerPrefs.GetInt(_key, HeroSkinRules.Automatic);
            if (Selected < 0) Selected = HeroSkinRules.Automatic;
        }

        public int Selected { get; private set; }

        public event Action Changed;

        public void Select(int skin)
        {
            if (skin < 0) skin = HeroSkinRules.Automatic;
            if (skin == Selected) return;
            Selected = skin;
            PlayerPrefs.SetInt(_key, skin);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }
}
