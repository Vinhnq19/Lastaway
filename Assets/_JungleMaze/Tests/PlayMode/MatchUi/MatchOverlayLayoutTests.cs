#if UNITY_EDITOR
using System.Collections.Generic;
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
    /// AC44–AC47 on the 1280×720 reference canvas. The prefab's canvas is switched to world space at 1 unit = 1 canvas
    /// pixel, so rects are measured independent of the Game view size; values are top-left based. Since T-023 (skin A)
    /// the expected rects are the 1920×1080 px of mocks B1-B3 (T-028 pack), divided by 1.5. Tolerance 20 canvas px.
    /// </summary>
    public sealed class MatchOverlayLayoutTests
    {
        private const float Tolerance = 20f;
        private MatchOverlayRoot _root;

        [SetUp]
        public void SetUp() => _root = Spawn();

        [TearDown]
        public void TearDown()
        {
            if (_root != null) Object.DestroyImmediate(_root.gameObject);
        }

        private static MatchOverlayRoot Spawn()
        {
            var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(OverlayRig.PrefabPath)).GetComponent<MatchOverlayRoot>();
            var canvas = root.Canvas;
            canvas.renderMode = RenderMode.WorldSpace;
            var rt = (RectTransform)root.transform;
            rt.sizeDelta = new Vector2(1280, 720);
            rt.position = Vector3.zero;
            rt.localScale = Vector3.one;
            Canvas.ForceUpdateCanvases();
            return root;
        }

        // The layout tests below switch the canvas to world space, which turns the scaler off; the shipped prefab's
        // scaler is checked here on the asset itself (review lát 7–11, Gợi ý 3), with the sort order above the HUD.
        [Test]
        public void Prefab_CanvasScaler_1280x720_Expand()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(OverlayRig.PrefabPath);
            var canvas = prefab.GetComponent<MatchOverlayRoot>().Canvas;
            Assert.NotNull(canvas);
            var scaler = canvas.GetComponent<CanvasScaler>();
            Assert.NotNull(scaler);
            Assert.AreEqual(RenderMode.ScreenSpaceOverlay, canvas.renderMode);
            Assert.AreEqual(20, canvas.sortingOrder, "above the HUD (10)");
            Assert.AreEqual(CanvasScaler.ScaleMode.ScaleWithScreenSize, scaler.uiScaleMode);
            Assert.AreEqual(new Vector2(1280, 720), scaler.referenceResolution);
            Assert.AreEqual(CanvasScaler.ScreenMatchMode.Expand, scaler.screenMatchMode);
        }

        /// <summary>Rect in canvas pixels, origin top-left, y down.</summary>
        private static Rect Measure(RectTransform r)
        {
            var c = new Vector3[4];
            r.GetWorldCorners(c);
            return new Rect(c[0].x + 640f, 360f - c[1].y, c[2].x - c[0].x, c[1].y - c[0].y);
        }

        private static void Near(RectTransform r, float x, float y, float w, float h, string what)
        {
            var m = Measure(r);
            Assert.AreEqual(x, m.x, Tolerance, what + " x");
            Assert.AreEqual(y, m.y, Tolerance, what + " y");
            Assert.AreEqual(w, m.width, Tolerance, what + " w");
            Assert.AreEqual(h, m.height, Tolerance, what + " h");
        }

        /// <summary>Same as <see cref="Near"/> with 1080p mock px.</summary>
        private static void NearPx(RectTransform r, float x, float y, float w, float h, string what) =>
            Near(r, x / 1.5f, y / 1.5f, w / 1.5f, h / 1.5f, what);

        private static readonly int[] Scale = { 96, 40, 30, 24, 20 };

        private static string Codes(uint[] missing) =>
            missing == null ? "" : string.Join(" ", System.Array.ConvertAll(missing, c => "U+" + c.ToString("X4")));

        private static IEnumerable<T> All<T>(Component root) where T : Component => root.GetComponentsInChildren<T>(true);

        // AC44 on skin A (b1-esc-cai-dat): popup 820×700 px at (550,180), 10 px above centre as in the mock; rows 48 px
        // (kit slider and toggle), buttons kit M 64 px and the close X 52 px; every text on the T-027 scale in its tier font.
        [Test]
        public void EscPanel()
        {
            var panel = (RectTransform)_root.Menu.transform.Find("Panel");
            var m = Measure(panel);
            Assert.AreEqual(820f / 1.5f, m.width, 1f);
            Assert.AreEqual(700f / 1.5f, m.height, 1f);
            Assert.AreEqual(640f, m.center.x, 1f, "centred horizontally");
            Assert.AreEqual(530f / 1.5f, m.center.y, 1f, "mock: 10 px above the vertical centre");
            Assert.LessOrEqual(m.height, 720f * 0.65f, "≤ 65 % of the screen height (mock 64.8 %)");
            Assert.GreaterOrEqual(m.xMin, 0f);
            Assert.LessOrEqual(m.yMax, 720f);
            foreach (var slider in new[] { _root.Menu.Master, _root.Menu.Sfx, _root.Menu.Music, _root.Menu.Sensitivity })
                Assert.GreaterOrEqual(Measure((RectTransform)slider.transform.parent).height, 48f / 1.5f - 0.01f, slider.name + " row");
            Assert.GreaterOrEqual(Measure((RectTransform)_root.Menu.ScreenShake.transform.parent).height, 48f / 1.5f - 0.01f);
            foreach (var button in All<Button>(_root.Menu))
                Assert.GreaterOrEqual(Measure((RectTransform)button.transform).height, 48f / 1.5f - 0.01f, button.name);
            var chakra = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_JungleMaze/Art/Fonts/TMP/ChakraPetch-Bold SDF.asset");
            var body = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_JungleMaze/Art/Fonts/TMP/HudTextBold SDF.asset");
            foreach (var text in All<TMP_Text>(_root))
            {
                var px = Mathf.RoundToInt(text.fontSize * 1.5f);
                CollectionAssert.Contains(Scale, px, text.name + " on the 96/40/30/24/20 scale");
                Assert.AreEqual(px >= 40 ? chakra : body, text.font, text.name + " font of its tier (no L buttons here)");
            }
        }

        // TR9: every character the overlay prints exists in its own font, so TMP never falls back to (and writes
        // glyphs into) the JSurviv LiberationSans fallback asset. Skin A prints in Be Vietnam Pro Bold (HudTextBold)
        // and Chakra Petch Bold (titles, rank, countdown); runtime lines are checked in both.
        [Test]
        public void AllText_IsInTheHudFonts()
        {
            var fonts = new[]
            {
                AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_JungleMaze/Art/Fonts/TMP/HudTextBold SDF.asset"),
                AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_JungleMaze/Art/Fonts/TMP/ChakraPetch-Bold SDF.asset")
            };
            var runtime = new[]
            {
                JungleMaze.App.MatchUi.EliminationText.Title, "Bị Hào hạ bằng Nỏ · cách 6 ô", "Bị bo thu hẹp loại", "Bạn đã rời trận",
                "Mất kết nối quá lâu", "Không rõ nguyên nhân", "Hạ gục: 2", "Sống sót: 05:12", "#4 / 10",
                "Không còn người chơi nào để theo dõi", "Trận đã kết thúc", "Lan đã bị loại", "Còn sống 9/10",
                "Killcam không khả dụng: dữ liệu chưa đủ 5 giây, chờ dữ liệu quá lâu, máy chủ chưa có dữ liệu",
                "dữ liệu không thuộc trận này, dữ liệu không phải của bạn, không rõ kẻ hạ, trận đã kết thúc",
                "Killcam · góc nhìn của Thảo Ngọc Tùng Khoa Minh Vũ", "Killcam 3,2 s", MatchOverlayPresenter.MusicOffText,
                MatchOverlayPresenter.RetryText, "Người chơi 10", "100%", "1,5", "80/100"
            };
            foreach (var text in All<TMP_Text>(_root))
            {
                Assert.IsTrue(text.font.HasCharacters(text.text, out var missing, false, false),
                    text.name + " misses " + Codes(missing));
            }
            foreach (var font in fonts)
                foreach (var line in runtime)
                    Assert.IsTrue(font.HasCharacters(line, out var missing, false, false),
                        font.name + ": " + line + " misses " + Codes(missing));
        }

        // AC45 on skin A (b2-bi-loai): panel M 920×860 at (500,110), buttons kit M at y 804 (mock 72 tall, kit M 64).
        [Test]
        public void EliminatedScreen()
        {
            NearPx(_root.Eliminated.Frame, 500, 110, 920, 860, "frame");
            NearPx((RectTransform)_root.Eliminated.Home.transform, 560, 804, 360, 64, "Về sảnh");
            NearPx((RectTransform)_root.Eliminated.Spectate.transform, 1000, 804, 360, 64, "Theo dõi");
        }

        // AC46 on skin A (b3-spectator): watching card, keycaps, home square, target card, 9 rows 56 px apart.
        [Test]
        public void Spectator()
        {
            var v = _root.Spectator;
            NearPx(v.WatchingBlock, 660, 20, 600, 120, "Đang theo dõi");
            NearPx(v.KeyBar, 540, 990, 184, 52, "A D");
            NearPx((RectTransform)v.Killer.transform, 780, 990, 124, 52, "E kẻ hạ bạn");
            NearPx((RectTransform)v.Home.transform, 1620, 976, 64, 64, "Về sảnh");
            NearPx(v.Card, 30, 770, 460, 180, "thẻ mục tiêu");
            Assert.AreEqual(9, v.Rows.Length);
            for (var i = 0; i < v.Rows.Length; i++)
                NearPx((RectTransform)v.Rows[i].transform, 30, 190 + i * 56, 300, 48, "row " + (i + 1));
            // The HUD killfeed area (15,34) 270×154 px of the mock stays empty.
            var killfeed = new Rect(15 / 1.5f, 34 / 1.5f, 270 / 1.5f, 154 / 1.5f);
            foreach (var g in All<Graphic>(v))
                Assert.IsFalse(killfeed.Overlaps(Measure(g.rectTransform)), g.name + " in the killfeed area");
        }

        // AC47: swapping colors, sprites and fonts on the prefab copy changes no measured rect.
        [Test]
        public void ArtSwapNeedsPrefabOnly()
        {
            var watched = new List<RectTransform>
            {
                (RectTransform)_root.Menu.transform.Find("Panel"), _root.Eliminated.Frame, (RectTransform)_root.Eliminated.Home.transform,
                (RectTransform)_root.Eliminated.Spectate.transform, _root.Spectator.WatchingBlock, _root.Spectator.KeyBar,
                (RectTransform)_root.Spectator.Home.transform, _root.Spectator.Card
            };
            var before = new List<Rect>();
            foreach (var r in watched) before.Add(Measure(r));

            var knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            // Swap to the other HUD font that covers every glyph the overlay prints (AllText_IsInTheHudFonts), so the
            // test never makes TMP write glyphs into the JSurviv fallback asset.
            var otherFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_JungleMaze/Art/Fonts/TMP/HudText SDF.asset");
            foreach (var image in All<Image>(_root)) { image.sprite = knob; image.color = Color.magenta; }
            foreach (var text in All<TMP_Text>(_root)) { if (text.font != otherFont) text.font = otherFont; text.fontSize += 2; text.color = Color.cyan; }
            Canvas.ForceUpdateCanvases();

            for (var i = 0; i < watched.Count; i++)
            {
                var after = Measure(watched[i]);
                Assert.AreEqual(before[i].position, after.position, watched[i].name);
                Assert.AreEqual(before[i].size, after.size, watched[i].name);
            }
        }
    }
}
#endif
