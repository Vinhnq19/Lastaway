#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using JungleMaze.App.Hud;
using JungleMaze.App.Hud.State;
using JungleMaze.Core.Events;
using JungleMaze.Infrastructure.Config;
using JungleMaze.Presentation.UI.Hud;
using JungleMaze.Presentation.UI.Kit;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace JungleMaze.Tests.PlayMode
{
    /// <summary>
    /// T-018 stage 1 on the real Hud.prefab (de-xuat/minimap-chuc-nang.md 4.1, 10 S): two minimap levels cropping the
    /// map texture, marker kept on the hero and inside the window, 1024 texture, cell sizes at 1080p.
    /// </summary>
    public sealed class MinimapZoomPlayTests
    {
        private const string HudPath = "Assets/_JungleMaze/Prefabs/UI/Hud.prefab";
        private const float Px = 1.5f; // 1080p px per canvas unit
        private readonly List<Object> _objects = new List<Object>();
        private HudStateStore _hud;
        private HudRoot _core;
        private MinimapView _minimap;
        private HudBigMapView _map;
        private float _timeScale;

        [SetUp]
        public void SetUp()
        {
            _timeScale = Time.timeScale;
            Time.timeScale = 0; // the blend runs on unscaled time
            UiLayerStack.Clear();
            var balance = new BalanceConfigProvider(Application.streamingAssetsPath, null);
            _hud = new HudStateStore(new EventBus(), balance);
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = _timeScale;
            UiLayerStack.Clear();
            foreach (var o in _objects) if (o != null) Object.DestroyImmediate(o);
            _objects.Clear();
        }

        private IEnumerator Spawn(float x, float y)
        {
            var go = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(HudPath));
            _objects.Add(go);
            _core = go.GetComponent<HudRoot>();
            _minimap = _core.Minimap;
            _map = go.GetComponentInChildren<HudBigMapView>(true);
            var bus = new EventBus();
            var balance = new BalanceConfigProvider(Application.streamingAssetsPath, null);
            _core.Construct(_hud, new InventoryStateStore(bus, balance), new EffectStateStore(bus), null);
            _minimap.ConstructZoom(_hud);
            _map.Construct(_hud);
            _hud.SetPose(x, y, 0f);
            yield return null;
            yield return null;
        }

        private IEnumerator Settle()
        {
            yield return new WaitForSecondsRealtime(0.3f);
            Canvas.ForceUpdateCanvases();
        }

        private RawImage MapImage => _minimap.GetComponent<MinimapMapCapture>().MapImage;

        [UnityTest]
        public IEnumerator Level1_WholeMap_Level2_CornerHeroClampedInsideTheMap()
        {
            yield return Spawn(3f, 47f);
            var cells = _hud.MapSizeCells;
            Assert.AreEqual(50, cells, "balance MapSizeCells drives this test; the offline map resizes the HUD to 150 at run time");
            Assert.AreEqual(MinimapZoom.Full, _minimap.Zoom.Level, "a match starts on the whole map");
            Assert.AreEqual(new Rect(0, 0, 1, 1), MapImage.uvRect);

            _minimap.Zoom.SetLevel(MinimapZoom.Near);
            yield return Settle();
            var uv = MapImage.uvRect;
            Assert.That(uv.x, Is.EqualTo(0f).Within(1e-4f), "window x 0-30");
            Assert.That(uv.y, Is.EqualTo(20f / 50f).Within(1e-4f), "window y 20-50");
            Assert.That(uv.width, Is.EqualTo(30f / 50f).Within(1e-4f));
            var size = _minimap.SizePixels;
            var marker = _minimap.Marker.anchoredPosition;
            Assert.That(marker.x, Is.EqualTo(3f / 30f * size).Within(0.05f), "hero 3 cells from the left edge");
            Assert.That(marker.y, Is.EqualTo(27f / 30f * size).Within(0.05f), "hero 27 cells up the window");

            _minimap.Zoom.Toggle();
            yield return Settle();
            Assert.AreEqual(new Rect(0, 0, 1, 1), MapImage.uvRect, "back to the whole map");
            Assert.That(_minimap.Marker.anchoredPosition.x, Is.EqualTo(3f / 50f * size).Within(0.05f));
        }

        [UnityTest]
        public IEnumerator Level2_CentreHero_SitsMidMinimap_GridFollowsCells()
        {
            yield return Spawn(25f, 25f);
            _minimap.Zoom.SetLevel(MinimapZoom.Near);
            yield return Settle();
            var size = _minimap.SizePixels;
            Assert.That(_minimap.Marker.anchoredPosition.x, Is.EqualTo(size / 2f).Within(0.05f));
            Assert.That(_minimap.Marker.anchoredPosition.y, Is.EqualTo(size / 2f).Within(0.05f));
            Assert.That(MapImage.uvRect.x, Is.EqualTo(10f / 50f).Within(1e-4f), "window 10-40");
        }

        [UnityTest]
        public IEnumerator CellSizesAt1080p_AndTexture1024()
        {
            yield return Spawn(25f, 25f);
            Assert.AreEqual(1024, _minimap.GetComponent<MinimapMapCapture>().Resolution, "shared by minimap and big map");
            var hole = _minimap.Area.rect.width * Px;
            Assert.That(hole / MinimapTuning.Default.NearCells, Is.GreaterThanOrEqualTo(8.9f), "level 2 px a cell (269 / 30), any map size");
            _map.Open();
            yield return null;
            Canvas.ForceUpdateCanvases();
            // px a cell on the big map depends on the map size (spec 17 px assumed 50 cells; the played map is 150):
            // what the texture owes is no upscale, texels >= screen px across the whole map.
            var bigMap = _map.Map.rectTransform.rect.width * Px;
            Assert.That(1024f, Is.GreaterThanOrEqualTo(bigMap), "big map drawn without upscaling the 1024 texture");
            Assert.IsTrue(UiLayerStack.BlocksActions, "big map open blocks attack (M1)");
            _map.Close();
            Assert.IsFalse(UiLayerStack.BlocksActions);
        }

        /// <summary>Main 809a59b7 danger fill (HudZoneRings, after the first shrink) is cropped to the zoom window like the terrain.</summary>
        [UnityTest]
        public IEnumerator DangerFill_FollowsTheZoomWindow()
        {
            yield return Spawn(25f, 25f);
            var rings = _minimap.GetComponentInChildren<HudZoneRings>(true);
            Assert.NotNull(rings, "minimap zone rings");
            rings.SetZone(1, 25f, 25f, 20f, 30f, 28f, 8f);
            rings.Apply(50);
            var fill = _minimap.Area.Find("Map danger fill").GetComponent<RawImage>();
            var full = fill.uvRect;
            Assert.That(full.width, Is.GreaterThan(0.95f), "level 1: whole map");
            _minimap.Zoom.SetLevel(MinimapZoom.Near);
            yield return Settle();
            rings.Apply(50);
            var uv = fill.uvRect;
            Assert.That(uv.width, Is.EqualTo(full.width * 30f / 50f).Within(1e-4f), "30-cell window");
            Assert.That(uv.x, Is.EqualTo(10f / 50f + full.x * 30f / 50f).Within(1e-4f), "window 10-40, frame inset kept");
        }

        /// <summary>Huy 08/11 "Zoom trên mini map dùng cuộn chuột": the wheel zooms only with the pointer over the map hole.</summary>
        [UnityTest]
        public IEnumerator PointerOver_IsTheMapHoleOnly()
        {
            yield return Spawn(25f, 25f);
            var area = _minimap.Area;
            var canvas = area.GetComponentInParent<Canvas>().rootCanvas;
            var cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var c = new Vector3[4];
            area.GetWorldCorners(c);
            Assert.IsTrue(_minimap.PointerOver(RectTransformUtility.WorldToScreenPoint(cam, (c[0] + c[2]) / 2f)), "wheel over the map zooms");
            Assert.IsFalse(_minimap.PointerOver(new Vector2(10f, 10f)), "wheel elsewhere does not");
        }

        /// <summary>The click-to-cell mapping reads the live map size (offline map 150 cells), never the balance 50.</summary>
        [UnityTest]
        public IEnumerator Waypoint_ClickPoint_UsesTheLiveMapSize_150()
        {
            _hud.MapSizeSource = () => 150;
            yield return Spawn(75f, 75f);
            var view = _core.GetComponent<WaypointView>();
            view.Construct(_hud);
            _map.Open();
            yield return null;
            Canvas.ForceUpdateCanvases();
            var big = (RectTransform)view.BigFlag.parent;
            var c = new Vector3[4];
            big.GetWorldCorners(c);
            var canvas = big.GetComponentInParent<Canvas>().rootCanvas;
            var cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            Assert.IsTrue(view.TryMapPoint(RectTransformUtility.WorldToScreenPoint(cam, (c[0] + c[2]) / 2f), out var mid));
            Assert.That(mid.x, Is.EqualTo(75f).Within(0.1f), "centre of a 150-cell map is cell 75");
            Assert.That(mid.y, Is.EqualTo(75f).Within(0.1f));
            Assert.IsTrue(view.TryMapPoint(RectTransformUtility.WorldToScreenPoint(cam, Vector3.Lerp(c[0], c[2], 0.9f)), out var nearCorner));
            Assert.That(nearCorner.x, Is.EqualTo(135f).Within(0.1f), "90 % across = cell 135");
            view.Model.Click(mid.x, mid.y, _hud.MapSizeCells, view.Tuning.ReachCells);
            view.Model.Click(140f, 20f, _hud.MapSizeCells, view.Tuning.ReachCells);
            yield return null;
            yield return null;
            Assert.That(view.BigFlag.anchoredPosition.x, Is.EqualTo(140f / 150f * big.rect.width).Within(0.05f), "flag drawn on the 150-cell scale");
            _map.Close();
        }

        /// <summary>T-018 stage 2 D1: the builder wires the waypoint; a flag shows on the big map and the minimap at its cell.</summary>
        [UnityTest]
        public IEnumerator Waypoint_ShowsOnBigMapAndMinimap_ClickPointMapsToCells()
        {
            yield return Spawn(25f, 25f);
            var view = _core.GetComponent<WaypointView>();
            Assert.NotNull(view, "WaypointView on the HUD root");
            view.Construct(_hud);
            Assert.NotNull(view.BigFlag, "big map flag");
            Assert.NotNull(view.MiniFlag, "minimap flag");
            Assert.NotNull(view.Arrow, "edge arrow");
            Assert.AreEqual("wp_flag", view.BigFlag.GetComponent<Image>().sprite.name);
            Assert.AreEqual("wp_flag_mini", view.MiniFlag.GetComponent<Image>().sprite.name);
            Assert.AreEqual("wp_arrow_edge", view.Arrow.GetComponent<Image>().sprite.name);
            yield return null;
            Assert.IsFalse(view.BigFlag.gameObject.activeSelf || view.MiniFlag.gameObject.activeSelf, "no flag before a click");

            _map.Open();
            view.Model.Click(40f, 35f, 50, view.Tuning.ReachCells);
            yield return null;
            yield return null;
            Canvas.ForceUpdateCanvases();
            Assert.IsTrue(view.BigFlag.gameObject.activeSelf && view.MiniFlag.gameObject.activeSelf);
            var big = (RectTransform)view.BigFlag.parent;
            Assert.That(view.BigFlag.anchoredPosition.x, Is.EqualTo(40f / 50f * big.rect.width).Within(0.05f));
            Assert.That(view.BigFlag.anchoredPosition.y, Is.EqualTo(35f / 50f * big.rect.width).Within(0.05f));
            Assert.That(view.MiniFlag.anchoredPosition.x, Is.EqualTo(40f / 50f * _minimap.SizePixels).Within(0.05f), "level 1: flag at its cell");

            var c = new Vector3[4];
            big.GetWorldCorners(c);
            var canvas = big.GetComponentInParent<Canvas>().rootCanvas;
            var cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            Assert.IsTrue(view.TryMapPoint(RectTransformUtility.WorldToScreenPoint(cam, (c[0] + c[2]) / 2f), out var mid));
            Assert.That(mid.x, Is.EqualTo(25f).Within(0.05f), "a click at the map centre is cell 25");
            Assert.IsFalse(view.TryMapPoint(new Vector2(5f, 5f), out _), "outside the map square: nothing");
            _map.Close();
        }
    }
}
#endif
