using System.Collections.Generic;
using JungleMaze.App.Player;
using JungleMaze.Domain.Player;
using JungleMaze.Infrastructure.Settings;
using JungleMaze.Presentation.Player;
using JungleMaze.Presentation.UI.Lobby;
using JungleMaze.Presentation.World.Phase2;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace JungleMaze.Tests.World
{
    /// <summary>Home skin choice: pure rules, PlayerPrefs storage, own-character skin online and the Home picker.</summary>
    public sealed class HeroSkinChoiceTests
    {
        private const string TestKey = "jm.test.hero.skin";
        private readonly List<Object> _owned = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey(TestKey);
            foreach (var o in _owned) if (o != null) Object.DestroyImmediate(o);
            _owned.Clear();
        }

        [Test]
        public void OwnCharacterUsesTheChoice_OthersAndAutomaticUseTheNetIdSkin()
        {
            Assert.AreEqual(4, HeroSkinRules.Resolve(4, true, 2));
            Assert.AreEqual(2, HeroSkinRules.Resolve(4, false, 2));
            Assert.AreEqual(2, HeroSkinRules.Resolve(HeroSkinRules.Automatic, true, 2));
            Assert.AreEqual(0, HeroSkinRules.Offline(HeroSkinRules.Automatic));
            Assert.AreEqual(5, HeroSkinRules.Offline(5));
        }

        [Test]
        public void StepWrapsThroughAutomaticBothWays()
        {
            Assert.AreEqual(0, HeroSkinRules.Step(HeroSkinRules.Automatic, 9, 1));
            Assert.AreEqual(8, HeroSkinRules.Step(HeroSkinRules.Automatic, 9, -1));
            Assert.AreEqual(HeroSkinRules.Automatic, HeroSkinRules.Step(8, 9, 1));
            Assert.AreEqual(HeroSkinRules.Automatic, HeroSkinRules.Step(0, 9, -1));
            Assert.AreEqual(4, HeroSkinRules.Step(3, 9, 1));
            Assert.AreEqual(0, HeroSkinRules.Step(20, 9, 1), "a stale index past the set restarts from automatic");
            Assert.AreEqual(HeroSkinRules.Automatic, HeroSkinRules.Step(3, 0, 1));
        }

        [Test]
        public void PreferencePersists_AndRaisesChangedOnlyOnARealChange()
        {
            PlayerPrefs.DeleteKey(TestKey);
            var first = new PlayerPrefsHeroSkinPreference(TestKey);
            Assert.AreEqual(HeroSkinRules.Automatic, first.Selected);
            var changes = 0;
            first.Changed += () => changes++;
            first.Select(3);
            first.Select(3);
            Assert.AreEqual(1, changes);
            Assert.AreEqual(3, new PlayerPrefsHeroSkinPreference(TestKey).Selected, "saved on the device");
            first.Select(-7);
            Assert.AreEqual(HeroSkinRules.Automatic, first.Selected);
            Assert.AreEqual(2, changes);
        }

        [Test]
        public void OnlineOwnPlayerViewWearsTheChoice_OthersKeepTheirNetIdSkin()
        {
            var (set, idle) = Set(4);
            var prefs = new FakeSkins { Selected = 3 };
            using var live = new JungleMaze.Tests.MatchUi.LiveMatchHarness();
            live.Start(1, 3);

            var own = View(set, prefs, live.Match, 1, out var ownBody, out var ownModel);
            View(set, prefs, live.Match, 2, out var otherBody, out _);
            Assert.AreSame(idle[3], ownBody.sprite, "own character (NetId 1) wears the chosen skin 3");
            Assert.AreSame(idle[2], otherBody.sprite, "another player keeps the NetId skin");

            prefs.Selected = HeroSkinRules.Automatic;
            own.Bind(ownModel);
            Assert.AreSame(idle[1], ownBody.sprite, "automatic falls back to the NetId skin");
        }

        [Test]
        public void HomePickerStepsAndSavesTheChoice()
        {
            var (set, idle) = Set(3);
            var prefs = new FakeSkins { Selected = HeroSkinRules.Automatic };
            var menuObject = Own(new GameObject("MainMenu", typeof(RectTransform)));
            var menu = menuObject.AddComponent<MainMenuView>();
            var buttonObject = new GameObject("OfflinePractice", typeof(RectTransform));
            buttonObject.transform.SetParent(menuObject.transform, false);
            var button = buttonObject.AddComponent<Button>();
            var labelObject = new GameObject("Label", typeof(RectTransform));
            labelObject.transform.SetParent(buttonObject.transform, false);
            labelObject.AddComponent<TextMeshProUGUI>();
            var statusObject = new GameObject("ApiStatus", typeof(RectTransform));
            statusObject.transform.SetParent(menuObject.transform, false);
            var status = statusObject.AddComponent<TextMeshProUGUI>();
            var so = new SerializedObject(menu);
            so.FindProperty("offlinePractice").objectReferenceValue = button;
            so.FindProperty("apiStatus").objectReferenceValue = status;
            so.ApplyModifiedPropertiesWithoutUndo();

            var picker = HeroSkinPicker.Attach(menu, prefs, set);
            Assert.NotNull(picker);
            StringAssert.Contains("Tự động", picker.Label);
            picker.Step(1);
            Assert.AreEqual(0, prefs.Selected);
            Assert.AreEqual("Skin 1/3", picker.Label);
            Assert.AreSame(idle[0], picker.Preview);
            picker.Step(-1);
            picker.Step(-1);
            Assert.AreEqual(2, prefs.Selected, "◀ from automatic wraps to the last skin");
            Assert.AreSame(idle[2], picker.Preview);
        }

        private PlayerView View(HeroVisualSet set, IHeroSkinPreference prefs, JungleMaze.App.Match.MatchSession match, byte netId, out SpriteRenderer body, out PlayerModel model)
        {
            var root = Own(new GameObject("player " + netId));
            var bodyObject = new GameObject("Body");
            bodyObject.transform.SetParent(root.transform, false);
            body = bodyObject.AddComponent<SpriteRenderer>();
            var animator = bodyObject.AddComponent<HeroSpriteAnimator>();
            var animatorSo = new SerializedObject(animator);
            animatorSo.FindProperty("visuals").objectReferenceValue = set;
            animatorSo.ApplyModifiedPropertiesWithoutUndo();
            var view = root.AddComponent<PlayerView>();
            var viewSo = new SerializedObject(view);
            viewSo.FindProperty("body").objectReferenceValue = body;
            viewSo.ApplyModifiedPropertiesWithoutUndo();
            view.ConfigureSkin(prefs, match);
            model = new PlayerModel(netId);
            model.Apply(new PlayerSnapshot { Tick = 1, Hp = 100 });
            view.Bind(model);
            return view;
        }

        private (HeroVisualSet set, Sprite[] idle) Set(int count)
        {
            var set = Own(ScriptableObject.CreateInstance<HeroVisualSet>());
            var heroes = new HeroVisualSet.Hero[count];
            var idle = new Sprite[count];
            for (var i = 0; i < count; i++)
            {
                var texture = Own(new Texture2D(48, 48));
                idle[i] = Own(Sprite.Create(texture, new Rect(0, 0, 48, 48), new Vector2(.5f, .5f), 8));
                var loop = new WorldVisualSet.Frames { Sprites = new[] { idle[i] }, Times = new[] { 0f }, Duration = .5f, Loop = true };
                heroes[i] = new HeroVisualSet.Hero { Name = "Hero_" + i, IdleSide = loop, IdleFront = loop, IdleBack = loop, MoveSide = loop };
            }
            set.Heroes = heroes;
            return (set, idle);
        }

        private T Own<T>(T o) where T : Object { _owned.Add(o); return o; }

        private sealed class FakeSkins : IHeroSkinPreference
        {
            public int Selected { get; set; }
            public event System.Action Changed;
            public void Select(int skin) { Selected = skin < 0 ? HeroSkinRules.Automatic : skin; Changed?.Invoke(); }
        }
    }
}
