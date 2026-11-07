#if UNITY_EDITOR
using System;
using JungleMaze.Domain.MatchUi;
using JungleMaze.Presentation.UI.MatchOverlay;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace JungleMaze.Tests.PlayMode.MatchUi
{
    /// <summary>
    /// T-023 skin A parts that did not exist before: close X on both popups (no new path, it presses Tiếp tục / Ở lại),
    /// stat cards hidden with their line, gold name on the selected row, the floor behind Eliminated only, the gameplay
    /// text floor (24 px) on spectator and toast, keycap labels in ink.
    /// </summary>
    public sealed class MatchOverlaySkinATests
    {
        private static readonly Guid Match = Guid.NewGuid();
        private MatchOverlayRoot _root;

        [SetUp]
        public void SetUp() =>
            _root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(OverlayRig.PrefabPath)).GetComponent<MatchOverlayRoot>();

        [TearDown]
        public void TearDown()
        {
            if (_root != null) Object.DestroyImmediate(_root.gameObject);
        }

        private static string SpriteOf(Component c) => c.GetComponent<Image>().sprite?.name;

        [Test]
        public void CloseX_PressesResume_UnlessLocked()
        {
            var menu = _root.Menu;
            Assert.NotNull(menu.Close);
            Assert.AreEqual("kit_close_normal", SpriteOf(menu.Close));
            Assert.AreEqual("kit_icon_cross_red", menu.Close.transform.Find("Icon").GetComponent<Image>().sprite.name);
            var resumed = 0;
            menu.Resume.onClick.AddListener(() => resumed++);
            menu.Close.onClick.Invoke();
            Assert.AreEqual(1, resumed);
            menu.Resume.interactable = false; // leave in progress: the presenter locks Tiếp tục
            menu.Close.onClick.Invoke();
            Assert.AreEqual(1, resumed, "the X follows the lock on Tiếp tục");
        }

        [Test]
        public void ConfirmCloseX_PressesStay()
        {
            var confirm = _root.Confirm;
            Assert.NotNull(confirm.Close);
            Assert.AreEqual("kit_close_normal", SpriteOf(confirm.Close));
            var stayed = 0;
            confirm.Stay.onClick.AddListener(() => stayed++);
            confirm.Close.onClick.Invoke();
            Assert.AreEqual(1, stayed);
        }

        [Test]
        public void StatCards_HideWithTheirLine()
        {
            var view = _root.Eliminated;
            var killsCard = view.Kills.transform.parent;
            var survivalCard = view.Survival.transform.parent;
            Assert.AreEqual("kit_card_stat", SpriteOf(killsCard));
            Assert.AreEqual("kit_card_stat", SpriteOf(survivalCard));
            Assert.AreEqual("icon_kill", killsCard.Find("Icon").GetComponent<Image>().sprite.name);
            Assert.AreEqual("icon_zone-timer", survivalCard.Find("Icon").GetComponent<Image>().sprite.name);

            view.Show(new EliminationInfo(Match, 1, 5, 10, EliminationCause.Zone, EliminationInfo.NoNetId, null, null, float.NaN, 75f, -1));
            Assert.IsFalse(killsCard.gameObject.activeSelf, "no kills data: the card goes with the line (AC20)");
            Assert.IsTrue(survivalCard.gameObject.activeSelf);
            Assert.IsTrue(view.Survival.gameObject.activeSelf);

            view.Show(new EliminationInfo(Match, 1, 5, 10, EliminationCause.Zone, EliminationInfo.NoNetId, null, null, float.NaN, float.NaN, -1));
            Assert.IsFalse(survivalCard.gameObject.activeSelf);
        }

        // R3: value only in the stat cards (40, Chakra Petch), rank split into "#4" 96 and "/ 10" 40 muted.
        [Test]
        public void StatValuesOnly_RankSplit()
        {
            var view = _root.Eliminated;
            view.Show(new EliminationInfo(Match, 1, 4, 10, EliminationCause.Zone, EliminationInfo.NoNetId, null, null, float.NaN, 278f, 2));
            Assert.AreEqual("2", view.Kills.text);
            Assert.AreEqual("04:38", view.Survival.text);
            Assert.AreEqual(40f / 1.5f, view.Kills.fontSize, 0.01f);
            Assert.AreEqual("#4", view.Rank.text);
            Assert.AreEqual("/ 10", view.RankTotal.text);
            Assert.AreEqual(40f / 1.5f, view.RankTotal.fontSize, 0.01f);
            Assert.AreEqual(new Color(0xD6 / 255f, 0xDA / 255f, 0xD6 / 255f), view.RankTotal.color, "muted");
            Assert.AreEqual(view.Rank.transform.parent, view.RankTotal.transform.parent, "one centred line");

            view.Show(new EliminationInfo(Match, 1, 0, 0, EliminationCause.Zone, EliminationInfo.NoNetId, null, null, float.NaN, float.NaN, -1));
            Assert.IsFalse(view.Rank.gameObject.activeSelf, "no rank data: both parts hidden (AC20)");
            Assert.IsFalse(view.RankTotal.gameObject.activeSelf);
        }

        [TestCase("Nỏ", "CROSSBOW")]
        [TestCase("CROSSBOW", "CROSSBOW")]
        [TestCase("Ná cao su", "RUBBER_GUN")]
        [TestCase("Súng nước", null)]
        [TestCase(null, null)]
        public void WeaponIcon_ByNameOrId_HiddenWhenUnknown(string weapon, string sprite)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<JungleMaze.Presentation.UI.Hud.HudItemIconCatalog>("Assets/_JungleMaze/Prefabs/UI/HudItemIconCatalog.asset");
            Assert.AreEqual(sprite, WeaponIcons.Find(catalog, weapon)?.name);

            var spectator = _root.Spectator;
            spectator.SetTarget(true, new SpectatorEntry(3, "Tuan", true, 86, 100, weapon));
            Assert.AreEqual(sprite != null, spectator.CardWeaponIcon.gameObject.activeSelf, "spectator card icon");
            if (sprite != null) Assert.AreEqual(sprite, spectator.CardWeaponIcon.sprite.name);
            Assert.AreEqual(weapon != null, spectator.CardWeapon.gameObject.activeSelf, "the weapon text stays");

            var eliminated = _root.Eliminated;
            eliminated.Show(new EliminationInfo(Match, 1, 4, 10, EliminationCause.Player, 2, "Hào", weapon, 6f, 278f, 2));
            Assert.AreEqual(sprite != null, eliminated.CauseWeaponIcon.gameObject.activeSelf, "cause card icon");
            if (sprite != null) Assert.AreEqual(sprite, eliminated.CauseWeaponIcon.sprite.name);
        }

        [Test]
        public void WeaponIcon_HiddenForOtherCauses()
        {
            var eliminated = _root.Eliminated;
            eliminated.Show(new EliminationInfo(Match, 1, 4, 10, EliminationCause.Zone, EliminationInfo.NoNetId, null, "Nỏ", float.NaN, 278f, 2));
            Assert.IsFalse(eliminated.CauseWeaponIcon.gameObject.activeSelf, "only a kill names a weapon");
        }

        [Test]
        public void Eliminated_HasFloorAndKitParts_SpectatorKeepsTheWorldVisible()
        {
            var eliminated = _root.Eliminated.transform;
            Assert.AreEqual(0, eliminated.Find("Floor").GetSiblingIndex(), "floor first under the screen root");
            Assert.AreEqual("kit_card_cause", SpriteOf(eliminated.Find("Frame/CauseCard")));
            Assert.AreEqual("kit_icon_cross_red", SpriteOf(eliminated.Find("Frame/TitleLine/Icon")));
            Assert.AreEqual(96f / 1.5f, _root.Eliminated.Rank.fontSize, 0.01f, "rank H1 96");
            Assert.IsNull(_root.Spectator.transform.Find("Floor"), "the watched player's world shows through");
            Assert.IsNull(_root.Spectator.GetComponent<Image>());
        }

        [Test]
        public void SelectedRow_NameTurnsGold()
        {
            var row = _root.Spectator.Rows[0];
            row.Set(3, "Tuan", true);
            Assert.AreEqual(new Color(1f, 0.863f, 0.431f), row.Label.color);
            Assert.AreEqual(Color.white, row.GetComponent<Image>().color, "kit row drawn untinted");
            row.Set(3, "Tuan", false);
            Assert.AreEqual(new Color(0.929f, 0.949f, 0.933f), row.Label.color);
        }

        // Gameplay screens (spectator, toast): no text under 24 px; keycap letters are ink on the kaki cap.
        [Test]
        public void GameplayText_Floor24_KeycapsInk()
        {
            foreach (var root in new Component[] { _root.Spectator, _root.Notice, _root.Killcam })
                foreach (var t in root.GetComponentsInChildren<TMP_Text>(true))
                    Assert.GreaterOrEqual(Mathf.RoundToInt(t.fontSize * 1.5f), 24, t.transform.parent.name + "/" + t.name);
            foreach (var t in new[] { _root.Menu.RetryNotice, _root.Menu.MusicOffLabel, _root.Eliminated.BlockedReason })
                Assert.AreEqual(24, Mathf.RoundToInt(t.fontSize * 1.5f), t.name + " (do_co_chu gate: no 20 px text but key and slot numbers)");
            foreach (var dot in _root.Killcam.Dots)
                Assert.AreEqual(TextOverflowModes.Overflow, dot.Find("Name").GetComponent<TMP_Text>().overflowMode, dot.name + ": 24 px name never cut to an ellipsis");
            foreach (var key in new[] { "KeyA", "KeyD" })
            {
                var label = _root.Spectator.KeyBar.Find(key + "/Label").GetComponent<TMP_Text>();
                Assert.AreEqual(new Color(0x1F / 255f, 0x1A / 255f, 0x10 / 255f), label.color, key);
            }
            Assert.AreEqual("E", _root.Spectator.Killer.transform.Find("KeyE/Label").GetComponent<TMP_Text>().text);
            Assert.AreEqual(Selectable.Transition.SpriteSwap, _root.Spectator.Killer.transition);
            Assert.AreEqual("kit_keycap_disabled", _root.Spectator.Killer.spriteState.disabledSprite.name, "E greys out when there is no killer");
        }
    }
}
#endif
