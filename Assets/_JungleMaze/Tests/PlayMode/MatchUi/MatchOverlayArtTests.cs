#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using JungleMaze.App.MatchUi.Mocks;
using JungleMaze.Domain.MatchUi;
using JungleMaze.Presentation.UI.MatchOverlay;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace JungleMaze.Tests.PlayMode.MatchUi
{
    /// <summary>
    /// UI-03 art binding: the 19 overlay sprites are still imported per asset_mapping.json (the killcam uses them); since
    /// T-023 the other panels wear the skin A kit of mocks B1-B3. Icons never cover text or push it into an ellipsis;
    /// the cause icon follows the cause, the killer mark the killer.
    /// </summary>
    public sealed class MatchOverlayArtTests
    {
        private const string Art = "Assets/_JungleMaze/Art/UI/MatchOverlay/";
        private static readonly Guid Match = Guid.NewGuid();

        // Name → 9-slice border (px, all sides), from asset_mapping.json.
        private static readonly Dictionary<string, int> Sprites = new Dictionary<string, int>
        {
            { "SliderHandle", 0 }, { "ToggleBox", 12 }, { "ToggleCheck", 0 }, { "ZoneRing", 0 }, { "KillcamGrid", 0 },
            { "PlayerDot", 0 }, { "PlayerDotOutline", 0 }, { "KillerMark", 0 }, { "CausePlayer", 0 }, { "CauseZone", 0 },
            { "CauseLeft", 0 }, { "CauseDisconnect", 0 }, { "CauseUnknown", 0 }, { "IconVolume", 0 }, { "IconMusic", 0 },
            { "IconMouse", 0 }, { "IconShake", 0 }, { "IconExit", 0 }, { "IconSpectate", 0 }
        };

        private MatchOverlayRoot _root;
        private OverlayRig _rig;

        [TearDown]
        public void TearDown()
        {
            if (_root != null) Object.DestroyImmediate(_root.gameObject);
            _rig?.Dispose();
            _rig = null;
        }

        private MatchOverlayRoot Spawn()
        {
            _root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(OverlayRig.PrefabPath)).GetComponent<MatchOverlayRoot>();
            var canvas = _root.Canvas;
            canvas.renderMode = RenderMode.WorldSpace;
            var rt = (RectTransform)_root.transform;
            rt.sizeDelta = new Vector2(1280, 720);
            rt.position = Vector3.zero;
            rt.localScale = Vector3.one;
            Canvas.ForceUpdateCanvases();
            return _root;
        }

        private static string SpriteOf(Component c) => c.GetComponent<Image>().sprite?.name;
        private static Image Child(Component c, string path) => c.transform.Find(path).GetComponent<Image>();

        [Test]
        public void OverlaySprites_ImportSettings()
        {
            Assert.AreEqual(19, Sprites.Count);
            foreach (var pair in Sprites)
            {
                var path = Art + pair.Key + ".png";
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                Assert.NotNull(importer, path);
                Assert.AreEqual(TextureImporterType.Sprite, importer.textureType, pair.Key);
                Assert.AreEqual(SpriteImportMode.Single, importer.spriteImportMode, pair.Key);
                Assert.AreEqual(100f, importer.spritePixelsPerUnit, pair.Key);
                Assert.AreEqual(FilterMode.Point, importer.filterMode, pair.Key);
                Assert.IsFalse(importer.mipmapEnabled, pair.Key);
                Assert.AreEqual(TextureImporterCompression.Uncompressed, importer.textureCompression, pair.Key);
                Assert.AreEqual(pair.Key == "KillcamGrid" ? TextureWrapMode.Repeat : TextureWrapMode.Clamp, importer.wrapMode, pair.Key);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                Assert.NotNull(sprite, path);
                Assert.AreEqual(Vector4.one * pair.Value, sprite.border, pair.Key + " border");
            }
        }

        // T-023 skin A: the ESC menu, confirm, eliminated, spectator and notice wear the kit (T-020) sprites of mocks B1-B3;
        // the killcam keeps its asset_mapping.json art (no mock).
        [Test]
        public void Prefab_BindsMappedSprites()
        {
            var root = Spawn();
            var menu = root.Menu;
            foreach (var slider in new[] { menu.Master, menu.Sfx, menu.Music, menu.Sensitivity })
            {
                Assert.AreEqual("kit_knob", SpriteOf(slider.handleRect), slider.name);
                Assert.AreEqual("kit_slider_fill", SpriteOf(slider.fillRect), slider.name);
                Assert.AreEqual("kit_slider_track", SpriteOf(slider.transform.Find("Background")), slider.name);
            }
            foreach (var toggle in new[] { menu.MusicEnabled, menu.ScreenShake })
            {
                Assert.AreEqual("kit_toggle_off", ((Image)toggle.targetGraphic).sprite.name, toggle.name);
                Assert.AreEqual("kit_toggle_on", ((Image)toggle.graphic).sprite.name, toggle.name);
            }
            var icons = new Dictionary<string, string>
            {
                { "MasterRow", "kit_ico_sound" }, { "SfxRow", "kit_ico_sound" }, { "MusicRow", "kit_ico_music" },
                { "MusicToggleRow", "kit_ico_music" }, { "ShakeRow", "kit_ico_vibrate" }
            };
            foreach (var pair in icons)
            {
                Assert.AreEqual("kit_iconbtn_round_normal", Child(menu, "Panel/" + pair.Key + "/IconPlate").sprite.name, pair.Key);
                Assert.AreEqual(pair.Value, Child(menu, "Panel/" + pair.Key + "/IconPlate/Icon").sprite.name, pair.Key);
            }
            Assert.IsNull(menu.transform.Find("Panel/SensitivityRow/IconPlate"), "Chuột has no icon in the mock");
            Assert.AreEqual("kit_icon_cross_red", Child(menu.Leave, "Face/Group/Icon").sprite.name);
            Assert.AreEqual("kit_icon_cross_red", Child(root.Confirm.Confirm, "Face/Group/Icon").sprite.name);
            Assert.AreEqual("kit_ico_eye", Child(root.Spectator.WatchingBlock, "IconPlate/Icon").sprite.name);
            Assert.NotNull(root.Eliminated.CauseIcon);

            Assert.AreEqual("kit_panel_popup", SpriteOf(menu.transform.Find("Panel")));
            Assert.AreEqual("kit_panel_popup", SpriteOf(root.Confirm.transform.Find("Box")));
            Assert.AreEqual("kit_panel_s", SpriteOf(root.Eliminated.Frame), "panel M = 9-sliced S source");
            Assert.AreEqual("kit_card_stat", SpriteOf(root.Spectator.Card));
            Assert.AreEqual("kit_card_room_selected", SpriteOf(root.Spectator.WatchingBlock));
            Assert.AreEqual("kit_keycap_normal", SpriteOf(root.Spectator.KeyBar.Find("KeyA")));
            Assert.AreEqual("kit_keycap_normal", SpriteOf(root.Spectator.Killer.transform.Find("KeyE")));
            Assert.AreEqual("kit_toast", SpriteOf(root.Notice));
            var row = root.Spectator.Rows[0];
            Assert.AreEqual("kit_row_normal", SpriteOf(row));
            Assert.AreEqual("kit_row_alt", SpriteOf(root.Spectator.Rows[1]), "odd rows alternate");
            Assert.AreEqual("kit_row_selected", Child(row, "Selected").sprite.name);
            Assert.AreEqual("kit_progress_track", SpriteOf(root.Spectator.CardHp.transform.Find("Track")));

            foreach (var b in new[] { menu.Resume, root.Eliminated.Spectate, root.Confirm.Stay })
                Assert.AreEqual("kit_btn_primary_normal", SpriteOf(b), b.name);
            foreach (var b in new[] { menu.Defaults, root.Eliminated.Home, menu.Leave, root.Confirm.Confirm }) // Exit = secondary face + red X
                Assert.AreEqual("kit_btn_secondary_normal", SpriteOf(b), b.name);
            Assert.AreEqual("kit_iconbtn_square_normal", SpriteOf(root.Spectator.Home));
            Assert.AreEqual("ButtonSecondary", SpriteOf(root.Killcam.Skip), "killcam keeps its art (no mock)");

            var killcam = root.Killcam;
            var grid = killcam.Board.GetComponent<Image>();
            Assert.AreEqual("KillcamGrid", grid.sprite.name);
            Assert.AreEqual(Image.Type.Tiled, grid.type, "tiled, not one stretched tile");
            Assert.IsFalse(grid.raycastTarget);
            Assert.AreEqual("ZoneRing", SpriteOf(killcam.Board.Find("Zone")));
            Assert.AreEqual(KillcamClip.MaxPlayers, killcam.KillerMarks.Length);
            for (var i = 0; i < killcam.Dots.Length; i++)
            {
                Assert.AreEqual("PlayerDot", SpriteOf(killcam.Dots[i]), "dot " + i);
                Assert.AreEqual("PlayerDotOutline", Child(killcam.Dots[i], "Outline").sprite.name, "dot " + i);
                Assert.AreEqual("KillerMark", killcam.KillerMarks[i].GetComponent<Image>().sprite.name, "dot " + i);
                Assert.IsFalse(killcam.KillerMarks[i].activeSelf, "mark hidden until a killer is drawn");
                Assert.AreEqual("BarFill", SpriteOf(killcam.HpFills[i]), "dot " + i);
            }
        }

        private static Rect WorldRect(RectTransform r)
        {
            var c = new Vector3[4];
            r.GetWorldCorners(c);
            return Rect.MinMaxRect(c[0].x, c[0].y, c[2].x, c[2].y);
        }

        private static Rect TextRect(TMP_Text text)
        {
            text.ForceMeshUpdate(true);
            var b = text.textBounds;
            var min = text.transform.TransformPoint(b.min);
            var max = text.transform.TransformPoint(b.max);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        // The icons sit in space the text already left free: no label is truncated and no icon covers a glyph.
        [Test]
        public void Icons_CoverNoText_NoEllipsis()
        {
            var root = Spawn();
            var checks = new List<(Image icon, TMP_Text text, float size)>();
            foreach (var row in new[] { "MasterRow", "SfxRow", "MusicRow", "MusicToggleRow", "ShakeRow" })
            {
                var r = root.Menu.transform.Find("Panel/" + row);
                checks.Add((r.Find("IconPlate").GetComponent<Image>(), r.Find("Label").GetComponent<TMP_Text>(), 44f / 1.5f));
            }
            foreach (var b in new[] { root.Menu.Leave, root.Confirm.Confirm, root.Menu.Resume, root.Menu.Defaults, root.Confirm.Stay })
                checks.Add((Child(b, "Face/Group/Icon"), b.GetComponentInChildren<TMP_Text>(), 30f / 1.5f));
            var watching = root.Spectator.WatchingBlock;
            checks.Add((Child(watching, "IconPlate"), root.Spectator.WatchingName, 50f / 1.5f));
            checks.Add((Child(watching, "AliveIcon"), root.Spectator.AliveCount, 36f / 1.5f));
            Canvas.ForceUpdateCanvases();

            foreach (var (icon, text, size) in checks)
            {
                var label = text.transform.parent.name + "/" + text.name;
                var glyphs = TextRect(text);
                Assert.IsFalse(text.isTextTruncated, label + " truncated");
                Assert.IsFalse(WorldRect(icon.rectTransform).Overlaps(glyphs), label + " under its icon");
                var box = WorldRect(text.rectTransform);
                Assert.IsTrue(glyphs.xMin >= box.xMin - 0.5f && glyphs.xMax <= box.xMax + 0.5f, label + " runs past its rect");
                Assert.AreEqual(size, icon.rectTransform.rect.width, 0.01f, label + " icon size");
            }
        }

        // Skin A: the cause icon sits on a kaki plate in the card's left slot (110 px border), the text right of it.
        [TestCase(EliminationCause.Player, "kit_ico_skull")]
        [TestCase(EliminationCause.Zone, "icon_zone_cause")]
        [TestCase(EliminationCause.LeftMatch, "kit_ico_door")]
        [TestCase(EliminationCause.Disconnected, "kit_ico_wifi")]
        [TestCase(EliminationCause.Unknown, "kit_icon_warn")]
        public void CauseIcon_FollowsCause_InTheCardSlot(EliminationCause cause, string sprite)
        {
            var root = Spawn();
            var view = root.Eliminated;
            view.Show(MockEliminationSource.Sample(cause, Match));
            Assert.AreEqual(sprite, view.CauseIcon.sprite.name);
            Assert.IsTrue(view.CauseIcon.gameObject.activeInHierarchy);
            var icon = WorldRect(view.CauseIcon.rectTransform);
            var card = WorldRect((RectTransform)view.Cause.transform.parent);
            var text = TextRect(view.Cause);
            Assert.AreEqual("CauseCard", view.Cause.transform.parent.name);
            Assert.LessOrEqual(icon.center.x, card.xMin + 110f / 1.5f, "icon centred in the card's left slot");
            var plated = cause != EliminationCause.Zone;
            Assert.AreEqual(plated, view.CauseIcon.transform.parent.GetComponent<Image>().enabled, "plate only under ink icons");
            Assert.AreEqual((plated ? 40f : 60f) / 1.5f, icon.width, 0.5f, "40 on the plate, 60 alone (T-028 b2-bi-loai-bo)");
            Assert.LessOrEqual(icon.xMax, text.xMin, "icon left of the text");
            Assert.AreEqual(card.center.y, icon.center.y, 3f, "icon on the card's middle line");
            Assert.IsFalse(view.Cause.isTextTruncated, view.Cause.text);
        }

        // The killer mark is on the killer's dot only; the selected spectator row shows its outline, the others none.
        [UnityTest]
        public IEnumerator KillerMark_OnKillerOnly_SelectedRowOutlined()
        {
            _rig = new OverlayRig(killcam: true);
            _rig.Eliminate();
            Assert.AreEqual(MatchOverlayPhase.Killcam, _rig.Flow.Phase);
            for (var i = 0; i < 10; i++) yield return null;
            var killcam = _rig.Root.Killcam;
            var killer = MockEliminationSource.KillerNetId;
            Assert.IsTrue(killcam.Dots[killer].gameObject.activeSelf, "killer drawn");
            for (var i = 0; i < killcam.KillerMarks.Length; i++)
                Assert.AreEqual(i == killer, killcam.KillerMarks[i].activeSelf, "mark on dot " + i);

            _rig.Flow.SkipKillcam();
            Assert.IsTrue(_rig.Flow.Spectate());
            yield return null;
            var selected = 0;
            foreach (var row in _rig.Root.Spectator.Rows)
            {
                if (!row.gameObject.activeSelf) continue;
                var outline = row.transform.Find("Selected").GetComponent<Image>();
                Assert.AreEqual(row.Selected, outline.enabled, "row " + row.NetId);
                if (row.Selected) selected++;
            }
            Assert.AreEqual(1, selected);
        }
    }
}
#endif
