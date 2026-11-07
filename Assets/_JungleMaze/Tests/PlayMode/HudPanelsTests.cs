#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using JungleMaze.App.Hud.State;
using JungleMaze.Core.Events;
using JungleMaze.Domain.Hud;
using JungleMaze.Infrastructure.Config;
using JungleMaze.Presentation.UI.Hud;
using JungleMaze.Presentation.UI.Kit;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace JungleMaze.Tests.PlayMode
{
    /// <summary>
    /// T-022 skin A on the real Hud.prefab: B9 feedback sprites and low-HP vignette, the type scale of the core blocks,
    /// and the new B4 bag / B5 big map panels (exist, kit sprites, text tiers, closed by default, Open/Close/Toggle, data).
    /// </summary>
    public sealed class HudPanelsTests
    {
        private const string HudPath = "Assets/_JungleMaze/Prefabs/UI/Hud.prefab";
        private const float Px = 1.5f; // 1080p px per canvas unit
        private readonly List<Object> _objects = new List<Object>();
        private HudStateStore _hud;
        private InventoryStateStore _inventory;
        private EffectStateStore _effects;
        private HudRoot _core;
        private HudFeedbackRoot _feedback;
        private HudInventoryView _bag;
        private HudBigMapView _map;
        private FakeIcons _icons;
        private float _timeScale;

        [SetUp]
        public void SetUp()
        {
            _timeScale = Time.timeScale;
            Time.timeScale = 0;
            UiLayerStack.Clear();
            var balance = new BalanceConfigProvider(Application.streamingAssetsPath, null);
            Assert.IsNull(balance.LastError);
            var bus = new EventBus();
            _hud = new HudStateStore(bus, balance);
            _inventory = new InventoryStateStore(bus, balance);
            _effects = new EffectStateStore(bus);
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = _timeScale;
            UiLayerStack.Clear();
            for (var i = _objects.Count - 1; i >= 0; i--) if (_objects[i] != null) Object.DestroyImmediate(_objects[i]);
            _objects.Clear();
        }

        private T Track<T>(T item) where T : Object { _objects.Add(item); return item; }

        private IEnumerator Spawn()
        {
            var go = Track(Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(HudPath)));
            _core = go.GetComponent<HudRoot>();
            _feedback = go.GetComponent<HudFeedbackRoot>();
            _bag = go.GetComponentInChildren<HudInventoryView>(true);
            _map = go.GetComponentInChildren<HudBigMapView>(true);
            Assert.NotNull(_bag, "HudInventory nested in Hud.prefab");
            Assert.NotNull(_map, "HudBigMap nested in Hud.prefab");
            _icons = new FakeIcons(Track(Sprite.Create(Track(new Texture2D(64, 64)), new Rect(0, 0, 64, 64), Vector2.one * 0.5f)));
            _core.Construct(_hud, _inventory, _effects, _icons);
            _feedback.Construct(_hud, _inventory, _effects);
            _feedback.ConstructIcons(_icons);
            _bag.Construct(_hud, _inventory, _effects, _icons);
            _map.Construct(_hud);
            yield return null;
            _core.enabled = false;
            _feedback.enabled = false;
            Tick(0);
        }

        private void Tick(float dt)
        {
            _hud.Tick(dt); _inventory.Tick(dt); _effects.Tick(dt);
            _core.Presenter.Tick(dt); _feedback.Presenter.Tick(dt, 0, 0);
            Canvas.ForceUpdateCanvases();
        }

        private static string Parsed(TMP_Text text) { text.ForceMeshUpdate(); return text.GetParsedText(); }

        private static void TierIs(TMP_Text text, int px, string family)
        {
            Assert.That(text.fontSize, Is.EqualTo(px / Px).Within(0.01f), text.name + " size");
            Assert.AreEqual(family, text.font.faceInfo.familyName, text.name + " font");
            Assert.False(text.enableAutoSizing, text.name);
        }

        private static void SpriteIs(Image image, string name) => Assert.AreEqual(name, image.sprite != null ? image.sprite.name : null, image.name);

        // ---------- B9 feedback ----------

        [UnityTest]
        public IEnumerator Feedback_UsesKitAndFxSprites_RedKeptOnArcArrowVignette()
        {
            yield return Spawn();
            SpriteIs(_feedback.HitMarker.Ticks[0], "fx_hitmarker");
            Assert.AreEqual(1, _feedback.HitMarker.Ticks.Length);
            SpriteIs(_feedback.DamageArcs[0].Image, "fx_damage_arc");
            Assert.AreEqual(Color.white, _feedback.DamageArcs[0].Image.color, "arc red comes from the art, untinted");
            SpriteIs(_feedback.DangerArrows[0].Arrow.GetComponent<Image>(), "fx_danger_arrow");
            SpriteIs(_feedback.DangerBanner.GetComponent<Image>(), "kit_toast");
            SpriteIs(_feedback.UseAction.GetComponent<Image>(), "kit_toast");
            SpriteIs(_feedback.PickupFeed.Rows[0].GetComponent<Image>(), "kit_toast"); // R3: framed toast (T-028 b9)
            SpriteIs(_feedback.PickupFeed.OverflowGroup.GetComponent<Image>(), "kit_tag_neutral");
            SpriteIs(_feedback.EffectRow.Chips[0].transform.Find("SecondsTag").GetComponent<Image>(), "kit_tag_neutral");
            var vignette = (Image)_core.Vitals.LowHpVignette;
            SpriteIs(vignette, "fx_vignette");
            Assert.AreEqual(0, vignette.transform.GetSiblingIndex(), "vignette draws under every HUD block");
        }

        [UnityTest]
        public IEnumerator Vignette_OnAtLowHp_OffAfterHeal()
        {
            yield return Spawn();
            var vignette = _core.Vitals.LowHpVignette;
            var layers = new List<Graphic> { vignette };
            layers.AddRange(_core.Vitals.LowHpVignetteExtra);
            foreach (var layer in layers) Assert.False(layer.enabled);
            _hud.SetHp(_hud.MaxHp / 4); Tick(0);
            foreach (var layer in layers) Assert.True(layer.enabled, "25% HP is low (HudTuning.LowHpRatio), every stacked layer on");
            _hud.SetHp(_hud.MaxHp); Tick(0);
            foreach (var layer in layers) Assert.False(layer.enabled);
            for (var i = 0; i < layers.Count; i++)
                Assert.AreEqual(i, layers[i].transform.GetSiblingIndex(), "vignette layers are the first children (under the HUD)");
        }

        [UnityTest]
        public IEnumerator Feedback_TextTiers_GameplayFloor24_TagNumbers20()
        {
            yield return Spawn();
            TierIs(_feedback.UseAction.Label, 24, "Be Vietnam Pro");
            TierIs(_feedback.UseAction.Hint, 24, "Be Vietnam Pro");
            TierIs(_feedback.DangerBanner.Label, 24, "Be Vietnam Pro");
            TierIs(_feedback.DangerArrows[0].Seconds, 24, "Be Vietnam Pro");
            TierIs(_feedback.PickupFeed.Rows[0].Label, 24, "Be Vietnam Pro");
            TierIs(_feedback.EffectRow.Chips[0].Seconds, 24, "Be Vietnam Pro"); // "3s" carries a unit: gameplay floor 24
        }

        [UnityTest]
        public IEnumerator UseStrip_ShowsItemIcon_PickupIcon56()
        {
            yield return Spawn();
            _inventory.SetConsumableSlot(0, "GOD_LEAF", 6);
            _inventory.BeginUse(4, "GOD_LEAF", .5f, .5f, true); Tick(0);
            Assert.True(_feedback.UseAction.Icon.enabled);
            Assert.AreSame(_icons.Sprite, _feedback.UseAction.Icon.sprite);
            Assert.That(((RectTransform)_feedback.PickupFeed.Rows[0].Icon.transform).sizeDelta.x, Is.EqualTo(72 / Px).Within(0.01f));
            _feedback.PickupFeed.SetRow(0, true, "GOD_LEAF", "God Leaf", 2, 1);
            Assert.AreEqual("Lá thần mộc", Parsed(_feedback.PickupFeed.Rows[0].Label), "Vietnamese UI name, not the data name");
        }

        // ---------- Type scale of the core HUD ----------

        [UnityTest]
        public IEnumerator CoreHud_TypeScale_AndWidestNumbersFit()
        {
            yield return Spawn();
            TierIs(_core.Vitals.HpNumber, 30, "Be Vietnam Pro");
            TierIs(_core.Vitals.ArmorRow.Value, 30, "Be Vietnam Pro");
            TierIs(_core.Vitals.HelmetRow.Value, 30, "Be Vietnam Pro");
            TierIs(_core.Vitals.AdrenalineRow.Value, 30, "Be Vietnam Pro");
            TierIs(_core.MatchStatus.AliveValue, 24, "Be Vietnam Pro");
            TierIs(_core.MatchStatus.ZoneValue, 24, "Be Vietnam Pro");
            for (var i = 0; i < 8; i++)
            {
                TierIs(_core.Quickbar.GetSlotView(i).KeyLabel, 20, "Be Vietnam Pro");
                TierIs(_core.Quickbar.GetSlotView(i).CountLabel, 20, "Be Vietnam Pro");
                SpriteIs(_core.Quickbar.GetSlotView(i).transform.Find("KeyBadge").GetComponent<Image>(), "kit_badge_key");
            }
            _hud.SetAliveCount(10, 10); _hud.SetZoneCountdown(48); _hud.SetHp(100); Tick(0);
            foreach (var text in new[] { _core.MatchStatus.AliveValue, _core.MatchStatus.ZoneValue, _core.Vitals.HpNumber })
            {
                text.ForceMeshUpdate();
                Assert.That(text.preferredWidth, Is.LessThanOrEqualTo(text.rectTransform.rect.width + 0.01f), Parsed(text) + " fits its box");
            }
            Assert.AreEqual("10/10", Parsed(_core.MatchStatus.AliveValue));
        }

        // ---------- B4 bag ----------

        [UnityTest]
        public IEnumerator Bag_ClosedByDefault_OpenCloseToggle()
        {
            yield return Spawn();
            Assert.False(_bag.IsOpen);
            _bag.Open(); Assert.True(_bag.IsOpen);
            _bag.Close(); Assert.False(_bag.IsOpen);
            _bag.Toggle(); Assert.True(_bag.IsOpen);
            _bag.Toggle(); Assert.False(_bag.IsOpen);
            Assert.False(_bag.Tooltip.gameObject.activeSelf);
        }

        [UnityTest]
        public IEnumerator Bag_ShowsSlotsGearAmmo_FromState()
        {
            yield return Spawn();
            _inventory.SetWeaponSlot(1, "CROSSBOW");
            _inventory.SetConsumableSlot(0, "GOD_LEAF", 6);
            _inventory.SetActiveSlot(1);
            _hud.SetHelmet("WOODEN_HELMET", 18); _hud.SetArmor("ARMOR", 48); _hud.SetAmmo(4, 90);
            _effects.Apply(EffectKind.Adrenaline, 55, 55);
            _bag.Open();
            var cells = _bag.Cells;
            Assert.AreEqual(8, cells.Length);
            SpriteIs(cells[1].Frame, "kit_slot_item_selected");
            SpriteIs(cells[3].Frame, "kit_slot_item_empty");
            SpriteIs(cells[4].Frame, "kit_slot_item_normal");
            Assert.True(cells[1].Icon.enabled); Assert.False(cells[3].Icon.enabled);
            Assert.AreEqual("×6", Parsed(cells[4].Count)); Assert.False(cells[1].Count.enabled, "weapons show no count");
            Assert.AreEqual("18", Parsed(_bag.HelmetValue));
            Assert.AreEqual("48", Parsed(_bag.ArmorValue));
            Assert.AreEqual("55", Parsed(_bag.AdrenalineValue));
            Assert.AreEqual("4/90", Parsed(_bag.AmmoValue));
            TierIs(_bag.AmmoValue, 30, "Be Vietnam Pro");
            TierIs(cells[4].Count, 20, "Be Vietnam Pro");
            SpriteIs(_bag.transform.Find("Content/Panel").GetComponent<Image>(), "kit_panel_s"); // L = the 9-sliced S source
            SpriteIs(_bag.transform.Find("Content/Panel/Gear/Ammo/Icon").GetComponent<Image>(), "icon_ammo");
            SpriteIs(_bag.transform.Find("Content/Panel/Ground/Header/Icon").GetComponent<Image>(), "icon_ground");
            foreach (var g in _bag.GetComponentsInChildren<Graphic>(true)) Assert.False(g.raycastTarget, g.name);
        }

        [UnityTest]
        public IEnumerator Bag_Tooltip_NameAndKind_EmptyHides()
        {
            yield return Spawn();
            _inventory.SetWeaponSlot(1, "CROSSBOW");
            _inventory.SetConsumableSlot(0, "GOD_LEAF", 6);
            _bag.Open();
            _bag.ShowTooltip(1);
            Assert.True(_bag.Tooltip.gameObject.activeSelf);
            Assert.AreEqual("Nỏ", _bag.TooltipName.text, "Vietnamese UI name (GDD ITEM LIST)");
            Assert.AreEqual(HudInventoryView.WeaponKind, _bag.TooltipKind.text);
            TierIs(_bag.TooltipName, 24, "Be Vietnam Pro");
            _bag.ShowTooltip(4);
            Assert.AreEqual(HudInventoryView.ConsumableKind, _bag.TooltipKind.text);
            _bag.ShowTooltip(3);
            Assert.False(_bag.Tooltip.gameObject.activeSelf, "empty slot has no tooltip");
            _bag.ShowTooltip(-1);
            Assert.False(_bag.Tooltip.gameObject.activeSelf);
        }

        // ---------- B5 big map ----------

        [UnityTest]
        public IEnumerator BigMap_ClosedByDefault_OpenCloseToggle_PinAndZone()
        {
            yield return Spawn();
            Assert.False(_map.IsOpen);
            _map.Toggle(); Assert.True(_map.IsOpen);
            Assert.False(_map.Player.gameObject.activeSelf, "no pose yet: no pin");
            _hud.SetPose(_hud.MapSizeCells / 2f, _hud.MapSizeCells / 2f, 0); _hud.SetZoneCountdown(84);
            _map.Refresh();
            Assert.True(_map.Player.gameObject.activeSelf);
            var area = (RectTransform)_map.Player.parent;
            Assert.That(Vector2.Distance(_map.Player.anchoredPosition, area.rect.size / 2f), Is.LessThan(0.5f), "map centre");
            Assert.AreEqual("01:24", Parsed(_map.ZoneValue));
            TierIs(_map.ZoneValue, 40, "Chakra Petch");
            SpriteIs(_map.Player.GetComponent<Image>(), "fx_map_player");
            foreach (var g in _map.GetComponentsInChildren<Graphic>(true)) Assert.False(g.raycastTarget, g.name);
            _map.Close(); Assert.False(_map.IsOpen);
        }

        [UnityTest]
        public IEnumerator Esc_ClosesTopPanelFirst_ThenBag_ThenNothing()
        {
            yield return Spawn();
            _bag.Open();
            _map.Open();
            Assert.AreEqual(2, UiLayerStack.Count);
            Assert.True(UiLayerStack.TryCloseTop());
            Assert.False(_map.IsOpen, "the big map, opened last, closes first");
            Assert.True(_bag.IsOpen);
            Assert.True(UiLayerStack.TryCloseTop());
            Assert.False(_bag.IsOpen);
            Assert.False(UiLayerStack.TryCloseTop(), "third ESC reaches the match menu");
            _bag.Open(); _bag.Close();
            Assert.AreEqual(0, UiLayerStack.Count, "closing by key removes the layer too");
        }

        [UnityTest]
        public IEnumerator BigMap_ZoneRings_SizedFromRadius_CapturedLinesOff()
        {
            yield return Spawn();
            _map.Open();
            Assert.False(_map.ZoneRing.gameObject.activeSelf, "no zone yet");
            var cells = _hud.MapSizeCells;
            _map.SetZone(cells / 2f, cells / 2f, cells / 4f, cells / 2f, cells / 2f, 0f);
            _map.Refresh();
            var area = (RectTransform)_map.ZoneRing.parent;
            Assert.True(_map.ZoneRing.gameObject.activeSelf);
            Assert.That(_map.ZoneRing.sizeDelta.x, Is.EqualTo(area.rect.width / 2f).Within(0.01f), "diameter = 2 r on the map scale");
            Assert.That(Vector2.Distance(_map.ZoneRing.anchoredPosition, area.rect.size / 2f), Is.LessThan(0.01f));
            Assert.False(_map.SafeRing.gameObject.activeSelf, "radius 0 hides the next ring");
            var zoneArt = _map.ZoneRing.GetComponent<RingGraphic>();
            Assert.AreEqual("map_ring_zone", zoneArt.Sprite.name);
            Assert.AreEqual("map_ring_safe", _map.SafeRing.GetComponent<RingGraphic>().Sprite.name);
            Assert.That(zoneArt.Thickness, Is.EqualTo(4 / Px).Within(0.01f), "4 px stroke (mock b5) whatever the radius");
            _map.SetZone(cells / 2f, cells / 2f, cells / 8f, cells / 2f, cells / 2f, cells / 16f);
            _map.Refresh();
            Assert.That(zoneArt.Thickness, Is.EqualTo(4 / Px).Within(0.01f), "stroke unchanged at a smaller radius");
            Assert.True(_map.SafeRing.gameObject.activeSelf);
            var mini = _core.Minimap.transform.Find("MapArea/ZoneRing").GetComponent<RingGraphic>();
            Assert.AreEqual("map_ring_zone", mini.Sprite.name, "minimap uses the same ring art");
            Assert.That(mini.Thickness, Is.EqualTo(2 / Px).Within(0.01f), "minimap stroke at its scale");
            SpriteIs(_map.transform.Find("Content/Panel/MapArea").GetComponent<Image>(), "kit_map_bg");
            SpriteIs(_map.transform.Find("Content/Panel/MapArea/Grid").GetComponent<Image>(), "map_grid");
        }

        [UnityTest]
        public IEnumerator KillFeed_HiddenWhenEmpty_PushShowsRows_PanelWidensWithText()
        {
            yield return Spawn();
            var feed = _core.GetComponentInChildren<HudKillFeedView>(true);
            Assert.NotNull(feed, "HudKillFeed in Hud.prefab");
            Assert.False(feed.Panel.gameObject.activeSelf, "empty feed hidden");
            SpriteIs(feed.Panel.GetComponent<Image>(), "kit_killfeed_panel");
            feed.Push("Hào", _icons.Sprite, "Linh");
            Assert.True(feed.Panel.gameObject.activeSelf);
            Assert.True(feed.Rows[0].Root.gameObject.activeSelf); Assert.False(feed.Rows[1].Root.gameObject.activeSelf);
            Assert.AreEqual("Linh", feed.Rows[0].Victim.text);
            TierIs(feed.Rows[0].Killer, 24, "Be Vietnam Pro");
            Assert.That(feed.Panel.sizeDelta.x, Is.EqualTo(270 / Px).Within(0.01f), "short names keep the 270 px minimum");
            feed.Push("Quang Huy Nguyễn Văn", _icons.Sprite, "Vũ Trần Thị Ngọc");
            Assert.Greater(feed.Panel.sizeDelta.x, 270 / Px, "long names widen the panel");
            feed.Push("a", null, "b"); feed.Push("c", null, "d");
            Assert.AreEqual(3, feed.Log.Count, "max 3 rows");
            Assert.AreEqual("Vũ Trần Thị Ngọc", feed.Rows[0].Victim.text, "oldest dropped, newest at the bottom");
            Assert.False(feed.Rows[2].Weapon.enabled, "no icon, no placeholder");
            feed.Clear();
            Assert.False(feed.Panel.gameObject.activeSelf);
        }

        [UnityTest]
        public IEnumerator BigMap_Header_PhaseClockDistance_EmptyWithoutData()
        {
            yield return Spawn();
            _map.Open();
            Assert.AreEqual("", Parsed(_map.PhaseValue), "no zone data: no invented number");
            Assert.AreEqual("", Parsed(_map.DistanceValue));
            var cells = _hud.MapSizeCells;
            _map.ZoneRings.SetZone(2, cells / 2f, cells / 2f, cells / 4f, cells / 2f, cells / 2f, cells / 8f);
            _hud.SetPose(0f, cells / 2f, 0f);
            _map.Refresh();
            Assert.AreEqual("2", Parsed(_map.PhaseValue));
            Assert.AreEqual(HudPanelMath.DistanceOutside(0f, cells / 2f, cells / 2f, cells / 2f, cells / 8f).ToString(), Parsed(_map.DistanceValue));
            foreach (var t in new[] { _map.PhaseValue, _map.ZoneValue, _map.DistanceValue }) TierIs(t, 40, "Chakra Petch");
            for (var i = 1; i <= 4; i++)
                Assert.True(_map.transform.Find("Content/Panel/Controls/Row" + i).gameObject.activeSelf, "all 4 control rows shown as the mock");
        }

        private sealed class FakeIcons : IItemIconProvider
        {
            public readonly Sprite Sprite;
            public FakeIcons(Sprite sprite) { Sprite = sprite; }
            public Sprite GetIcon(string itemId) => string.IsNullOrEmpty(itemId) ? null : Sprite;
        }
    }
}
#endif
