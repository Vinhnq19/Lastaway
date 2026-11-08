#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using JungleMaze.App.Hud.State;
using JungleMaze.App.Ports;
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
        public IEnumerator ZoneRings_MainStyle_NextRingVisible_DataKept()
        {
            // Huy 07/11 "kiểu main": the maps show the map team's ZoneOverlay; the T-022 sprite rings are built (switch
            // back with HudSkinABuild.RingStyle) but never drawn, while HudZoneRings still knows phase, centre and radius.
            yield return Spawn();
            _map.Open();
            var cells = _hud.MapSizeCells;
            _map.ZoneRings.SetZone(3, cells / 2f, cells / 2f, cells / 4f, cells / 2f, cells / 2f, cells / 8f);
            _map.Refresh();
            Assert.False(_map.ZoneRings.Draw);
            Assert.False(_map.ZoneRing.gameObject.activeSelf, "big map: no sprite ring");
            Assert.True(_map.SafeRing.gameObject.activeSelf, "next zone stays visible independently of main ring style");
            Assert.True(_map.ZoneRings.HasZone);
            Assert.AreEqual(3, _map.ZoneRings.Phase);
            Assert.That(_map.ZoneRings.SafeRadius, Is.EqualTo(cells / 8f).Within(0.001f));
            Assert.AreEqual("3", Parsed(_map.PhaseValue), "header still reads the zone data");
            Assert.AreEqual("map_ring_zone", _map.ZoneRing.GetComponent<RingGraphic>().Sprite.name, "art kept for the Sprite style");

            var mini = _core.Minimap.transform.Find("MapArea").GetComponent<HudZoneRings>();
            Assert.NotNull(mini, "minimap keeps the zone data component");
            Assert.False(mini.Draw);
            mini.SetZone(1, cells / 2f, cells / 2f, cells / 4f, cells / 2f, cells / 2f, cells / 8f);
            mini.Apply(cells);
            Assert.False(mini.ZoneRing.gameObject.activeSelf, "minimap: no sprite ring");
            Assert.True(mini.SafeRing.gameObject.activeSelf);
            mini.SetZone(5, 25, 25, 3, 24, 24, 0); mini.Apply(cells);
            Assert.True(mini.SafeRing.gameObject.activeSelf, "final target is a white point");
            mini.SetZone(5, 24, 24, 0, 24, 24, 0); mini.Apply(cells);
            Assert.False(mini.SafeRing.gameObject.activeSelf, "no next zone after schedule ends");
            Assert.True(mini.HasZone);

            SpriteIs(_map.transform.Find("Content/Panel/MapArea").GetComponent<Image>(), "kit_map_bg");
            { // BIGMAP FIT (CPO 07/11): square map centred in a square panel, equal margins, overlay and marks on the same rect
                var panelRect = ((RectTransform)_map.transform.Find("Content/Panel")).rect;
                var areaT = (RectTransform)_map.transform.Find("Content/Panel/MapArea");
                Assert.That(areaT.rect.width, Is.EqualTo(areaT.rect.height).Within(0.01f), "map is square");
                Assert.That(panelRect.width, Is.EqualTo(panelRect.height).Within(0.01f), "panel is square");
                var left = areaT.anchoredPosition.x; var top = -areaT.anchoredPosition.y;
                Assert.That(panelRect.width - left - areaT.rect.width, Is.EqualTo(left).Within(0.01f), "right margin = left");
                Assert.That(panelRect.height - top - areaT.rect.height, Is.EqualTo(top).Within(0.01f), "bottom margin = top");
            }
            SpriteIs(_map.transform.Find("Content/Panel/MapArea/Grid").GetComponent<Image>(), "map_grid");
        }

        [UnityTest]
        public IEnumerator BigMap_Legend_OutsideZoneSwatch_AndNextRing()
        {
            yield return Spawn();
            var legend = _map.transform.Find("Content/Panel/Legend");
            var outside = legend.Find("OutsideZone").GetComponent<Image>();
            SpriteIs(outside, "hud_bar_fill_white"); // white base so the tint is the shader colour (kit_dim darkened it to black)
            Assert.That(Vector4.Distance(outside.color, (Color)new Color32(167, 55, 80, 255)), Is.LessThan(0.001f), "outside-zone pink as displayed on the minimap");
            SpriteIs(legend.Find("NextZone").GetComponent<Image>(), "legend_ring_next_dashed");
            Assert.IsNull(legend.Find("Zone"), "old single ring symbol gone");
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

        // ---------- Main-style zone layer on the big map (CPO 07/11) ----------

        private static readonly RuntimeMapBounds TestBounds = new RuntimeMapBounds { MinX = 0, MinY = 0, MaxX = 50, MaxY = 50 };

        [UnityTest]
        public IEnumerator BigMap_ZoneOverlay_UsesMainShaderInMinimapMode()
        {
            yield return Spawn();
            var overlay = _map.ZoneOverlay;
            Assert.NotNull(overlay, "big map has the ZoneOverlay layer");
            Assert.AreEqual("Inner", overlay.transform.parent.name, "over the map texture, under grid and pin");
            Assert.Greater(overlay.transform.GetSiblingIndex(), overlay.transform.parent.Find("Map").GetSiblingIndex());
            _map.Open();
            Assert.False(overlay.Image.enabled, "no zone yet: layer off");
            _map.ZoneRings.SetBounds(TestBounds);
            _map.ZoneRings.SetZone(1, 25, 25, 20, 30, 28, 8);
            _map.Refresh();
            Assert.True(overlay.Image.enabled);
            var m = overlay.Material;
            Assert.NotNull(m);
            Assert.AreEqual(ZoneOverlayParams.ShaderName, m.shader.name);
            Assert.AreEqual(1f, m.GetFloat("_Minimap"));
            Assert.AreEqual(ZoneOverlayParams.FillAlpha, m.GetFloat("_FillAlpha"), 1e-6f);
            Assert.AreEqual(new Vector4(25, 25, 20, 0), m.GetVector("_Current"));
            Assert.AreEqual(new Vector4(30, 28, 8, 1), m.GetVector("_Next"), "next ring previewed whenever it exists (minimap rule)");
            Assert.AreEqual(new Vector4(0, 0, 50, 50), m.GetVector("_MapBounds"));
            Assert.False(overlay.Image.raycastTarget);
        }

        [UnityTest]
        public IEnumerator BigMap_ZoneOverlay_MatchesMainMinimapLayer_SameData()
        {
            yield return Spawn();
            // Main 809a59b7: the composed HUD's minimap zone is HudZoneRings' "Map danger fill" (ZoneOverlayView no longer
            // adds "Zone circles" here); the big map's ZoneOverlay layer must show the same zone data with the same shader.
            var miniRings = _core.Minimap.Area.GetComponent<HudZoneRings>();
            Assert.NotNull(miniRings, "minimap zone rings");
            foreach (var rings in new[] { miniRings, _map.ZoneRings })
            {
                rings.SetBounds(TestBounds);
                rings.SetZone(1, 25, 25, 20, 30, 28, 8);
            }
            miniRings.Apply(50);
            var layer = _core.Minimap.Area.Find("Map danger fill");
            Assert.NotNull(layer, "minimap danger fill created after the first shrink");
            var mini = layer.GetComponent<RawImage>().material;

            _map.Open();
            _map.Refresh();
            var big = _map.ZoneOverlay.Material;
            Assert.AreEqual(mini.shader, big.shader, "same shader");
            foreach (var f in new[] { "_Minimap", "_FillAlpha" }) Assert.AreEqual(mini.GetFloat(f), big.GetFloat(f), 1e-6f, f);
            foreach (var v in new[] { "_Current", "_MapBounds" }) Assert.AreEqual(mini.GetVector(v), big.GetVector(v), v);
            var a = layer.GetComponent<RawImage>().uvRect; var b = _map.ZoneOverlay.Image.uvRect;
            Assert.That(Vector4.Distance(new Vector4(a.x, a.y, a.width, a.height), new Vector4(b.x, b.y, b.width, b.height)), Is.LessThan(1e-3f), "same crop ratio");
        }

        [UnityTest]
        public IEnumerator MinimapCapture_LeavesOutWorldZoneLayer_AndRestoresIt()
        {
            var grid = Track(new GameObject("Map grid")).AddComponent<JungleMaze.Presentation.World.MapGridView>();
            grid.Build(50, 8);
            yield return Spawn();
            // The world fill is the map team's ZoneOverlayView layer (main 809a59b7 hides it through WorldRenderer); it
            // is created by Show (packet built by reflection: its type lives in the protocol assembly this test does not reference).
            var main = Track(new GameObject("Main zone overlay")).AddComponent<JungleMaze.Presentation.UI.ZoneOverlayView>();
            var show = typeof(JungleMaze.Presentation.UI.ZoneOverlayView).GetMethod("Show");
            var packetType = show.GetParameters()[0].ParameterType;
            var packet = System.Activator.CreateInstance(packetType);
            packetType.GetField("CurrentRadius").SetValue(packet, 20f);
            show.Invoke(main, new object[] { packet, 0u, TestBounds });
            var renderer = main.WorldRenderer;
            Assert.NotNull(renderer, "world fill created");
            renderer.sharedMaterial = Track(new Material(Shader.Find("Sprites/Default")) { color = Color.magenta });
            yield return null;
            yield return null;
            var capture = _core.Minimap.GetComponent<MinimapMapCapture>();
            Assert.NotNull(capture.Texture, "map captured");
            var centre = Pixel(capture.Texture, 0.5f, 0.5f);
            Assert.False(centre.r > 200 && centre.g < 60 && centre.b > 200, "world zone fill is not in the terrain image: " + centre);
            Assert.False(renderer.forceRenderingOff, "world zone fill visible again after the capture");
        }

        private static Color32 Pixel(RenderTexture rt, float u, float v)
        {
            var previous = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = previous;
            var c = tex.GetPixel((int)(u * (rt.width - 1)), (int)(v * (rt.height - 1)));
            Object.Destroy(tex);
            return c;
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
