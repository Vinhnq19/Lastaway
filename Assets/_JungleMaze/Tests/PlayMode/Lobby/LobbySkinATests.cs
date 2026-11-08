#if UNITY_EDITOR
using System.Collections;
using JungleMaze.App.Lobby;
using JungleMaze.App.Lobby.Mocks;
using JungleMaze.Domain.Lobby;
using JungleMaze.Presentation.UI.Lobby;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace JungleMaze.Tests.PlayMode.Lobby
{
    /// <summary>
    /// T-024 / T-025 skin A on LobbyUi.prefab: floor backdrop on the lobby, loading and results screens, kit sprites
    /// on the new parts (map strip, bot buttons, bars, emblems, notice icons), text on the T-027 scale and font tiers.
    /// </summary>
    public sealed class LobbySkinATests
    {
        [UnityTest]
        public IEnumerator SplashIsNonInteractiveAndScopedToMenu()
        {
            using var rig = new LobbyUiRig();
            yield return null;
            Assert.NotNull(rig.Root.Splash);
            var image = rig.Root.Splash.GetComponent<Image>();
            Assert.AreEqual("lastaway_menu", rig.Root.Splash.GetComponent<Image>().sprite.name);
            rig.Root.SetSplashForMenu(false);
            Assert.AreEqual("lastaway_splash", rig.Root.Splash.GetComponent<Image>().sprite.name);
            rig.Root.SetSplashForMenu(true);
            Assert.AreEqual("lastaway_menu", rig.Root.Splash.GetComponent<Image>().sprite.name);
            Assert.False(image.raycastTarget);
            var fitter = image.GetComponent<AspectRatioFitter>();
            Assert.AreEqual(AspectRatioFitter.AspectMode.EnvelopeParent, fitter.aspectMode);
            Assert.That(fitter.aspectRatio, Is.EqualTo(16f / 9f).Within(.02f));
            Assert.True(rig.Root.Splash.activeSelf);
            rig.Flow.OpenJoin(); yield return null;
            Assert.False(rig.Root.Splash.activeSelf);
        }

        [UnityTest]
        public IEnumerator DropdownAndLobbyTextFitTheirFrames()
        {
            using var rig = new LobbyUiRig();
            yield return null;
            var dropdown = rig.Root.ServerSettings.Profile;
            var sprite = dropdown.GetComponent<Image>().sprite;
            Assert.Less(sprite.border.y + sprite.border.w, sprite.rect.height, "vertical borders must not exceed texture height");
            Assert.Greater(sprite.border.z, sprite.border.w, "arrow cap belongs on the right");
            var title = rig.Root.Lobby.MapCardTemplate.transform.Find("Title").GetComponent<TMP_Text>();
            title.text = "Thị trấn · v1.1.0"; title.ForceMeshUpdate(true);
            Assert.Greater(title.rectTransform.rect.width, 180);
            var notice = rig.Root.Lobby.Notice.rectTransform;
            var rule = rig.Root.Lobby.Rule.rectTransform;
            Assert.LessOrEqual(-notice.anchoredPosition.y + notice.rect.height, -rule.anchoredPosition.y);
        }

        private static string SpriteOf(Component c) => c.GetComponent<Image>().sprite.name;

        [Test]
        public void HasFloor_MenuLobbyLoadingResultsLost_NotInMatchOverlays()
        {
            foreach (var s in new[] { LobbyScreen.MainMenu, LobbyScreen.RoomManager, LobbyScreen.Lobby, LobbyScreen.Loading, LobbyScreen.LoadError,
                         LobbyScreen.Results, LobbyScreen.Disconnected })
                Assert.IsTrue(LobbyUiPresenter.HasFloor(s), s.ToString());
            foreach (var s in new[] { LobbyScreen.InGame, LobbyScreen.Reconnecting })
                Assert.IsFalse(LobbyUiPresenter.HasFloor(s), s.ToString());
        }

        /// <summary>Integrator 07/11: the lobby, loading and results screens no longer show the world or HUD behind.</summary>
        [UnityTest]
        public IEnumerator Backdrop_OnLobbyLoadingResults_OffInGame()
        {
            using var rig = new LobbyUiRig();
            yield return null;
            Assert.AreEqual("bg_floor_tile", SpriteOf(rig.Root.Backdrop.transform));
            rig.JoinForm();
            rig.Click(rig.Root.JoinForm.Join);
            yield return rig.Until(() => rig.Visible(rig.Root.Lobby), 2f, "lobby");
            Assert.IsTrue(rig.Root.Backdrop.activeSelf, "floor behind Phòng chờ (A6)");
            yield return rig.Until(() => rig.Visible(rig.Root.Loading), 3f, "loading");
            Assert.IsTrue(rig.Root.Backdrop.activeSelf, "floor behind Đang tải (A7)");
            yield return rig.Until(() => rig.Flow.Screen == LobbyScreen.InGame, 3f, "in game");
            yield return null;
            Assert.IsFalse(rig.Root.Backdrop.activeSelf, "the match keeps its own screen");
            yield return rig.Until(() => rig.Visible(rig.Root.Results), 5f, "results");
            Assert.IsTrue(rig.Root.Backdrop.activeSelf, "floor behind Kết quả (B6, B7)");
        }

        /// <summary>RoomMapPanel's controls are prefab parts on kit sprites, hidden until it binds (no flat boxes).</summary>
        [UnityTest]
        public IEnumerator MapAndBotControls_KitSprites_HiddenUntilRoomMapPanel()
        {
            using var rig = new LobbyUiRig();
            yield return null;
            var l = rig.Root.Lobby;
            foreach (var b in new[] { l.AddBot, l.FillBots, l.RetryMap })
            {
                Assert.IsFalse(b.gameObject.activeSelf, b.name + " hidden without maps");
                StringAssert.StartsWith("kit_btn_secondary", SpriteOf(b), b.name);
                Assert.AreEqual(32f, ((RectTransform)b.transform).sizeDelta.y, 0.01f, b.name + " is size S (48 px)");
            }
            Assert.IsFalse(l.MapCards.gameObject.activeSelf);
            Assert.AreEqual(0, l.MapCards.content.childCount, "Rebuild clears Content, so the template lives outside it");
            Assert.AreEqual("kit_card_room_normal", SpriteOf(l.MapCardTemplate));
            Assert.AreEqual("kit_card_room_selected", SpriteOf(l.MapCardTemplate.transform.Find("Selected")));
            Assert.IsNotNull(l.MapCardTemplate.transform.Find("Preview").GetComponent<RawImage>());
            Assert.AreEqual(24f, l.MapCardTemplate.transform.Find("Title").GetComponent<TMP_Text>().fontSize * 1.5f, 0.05f);
            Assert.AreEqual(20f, l.MapCardTemplate.transform.Find("Description").GetComponent<TMP_Text>().fontSize * 1.5f, 0.05f);
            foreach (var slot in l.Slots)
            {
                Assert.IsFalse(slot.RemoveBot.gameObject.activeSelf, slot.name);
                Assert.AreEqual("kit_iconbtn_square_normal", SpriteOf(slot.RemoveBot));
                Assert.AreEqual("kit_ico_trash", SpriteOf(slot.RemoveBot.transform.Find("Face/Icon")));
            }
        }

        /// <summary>New skin parts exist with their kit / fx sprites; popup X presses the popup's own cancel.</summary>
        [UnityTest]
        public IEnumerator SkinParts_KitSprites_CloseForwardsToCancel()
        {
            using var rig = new LobbyUiRig();
            yield return null;
            var r = rig.Root;
            Assert.AreEqual("kit_progress_fill", SpriteOf(r.Loading.transform.Find("Progress/FillArea/Fill")));
            Assert.AreEqual("kit_progress_track", SpriteOf(r.Reconnect.transform.Find("Panel/Progress")));
            Assert.AreEqual("kit_panel_m", SpriteOf(r.Reconnect.transform.Find("Panel")));
            Assert.AreEqual("fx_icon_disconnect", SpriteOf(r.Reconnect.transform.Find("Panel/Icon")));
            Assert.AreEqual("fx_icon_disconnect", SpriteOf(r.Notice.transform.Find("Panel/DisconnectIcon")));
            Assert.AreEqual("kit_icon_cross", SpriteOf(r.Notice.transform.Find("Panel/LoadErrorIcon")));
            Assert.AreEqual("fx_emblem_win", SpriteOf(r.Results.transform.Find("Summary/Emblem/EmblemWin")));
            Assert.AreEqual("fx_emblem_lose", SpriteOf(r.Results.transform.Find("Summary/Emblem/EmblemLose")));
            Assert.AreEqual("kit_card_cause", SpriteOf(r.Results.transform.Find("Summary/CauseCard")));
            Assert.AreEqual("kit_card_stat", SpriteOf(r.Results.transform.Find("Summary/Stats/KillsCard")));
            Assert.AreEqual("kit_panel_popup", SpriteOf(r.JoinForm.transform.Find("Panel")));
            Assert.AreEqual("kit_screen_header", SpriteOf(r.RoomManager.transform.Find("Header")));
            Assert.AreEqual("kit_card_room_normal", SpriteOf(r.RoomManager.Rows[0]));
            Assert.AreEqual("kit_toast", SpriteOf(r.MainMenu.transform.Find("AccountCard")));
            StringAssert.StartsWith("kit_btn_primary", SpriteOf(r.MainMenu.Join));
            StringAssert.StartsWith("kit_btn_danger", SpriteOf(r.RoomManager.ConfirmYes));
            Assert.AreSame(r.JoinForm.Back, r.JoinForm.transform.Find("Panel/Close").GetComponent<ForwardClick>().Target);
            Assert.AreSame(r.RoomManager.ConfirmNo, r.RoomManager.transform.Find("Confirm/Panel/Close").GetComponent<ForwardClick>().Target);

            rig.JoinForm();
            yield return null;
            r.JoinForm.transform.Find("Panel/Close").GetComponent<Button>().onClick.Invoke();
            yield return null;
            Assert.IsTrue(rig.Visible(r.MainMenu), "X closes the join popup like Hủy");
        }

        /// <summary>T-027: titles and big numbers (40, 96) in Chakra Petch, everything else and every button label in Be Vietnam Pro.</summary>
        [UnityTest]
        public IEnumerator Texts_FontTierFollowsSize()
        {
            using var rig = new LobbyUiRig();
            yield return null;
            foreach (var t in rig.Root.GetComponentsInChildren<TMP_Text>(true))
            {
                var px = Mathf.Round(t.fontSize * 1.5f);
                var title = (px == 40f || px == 96f) && t.GetComponentInParent<Button>(true) == null;
                StringAssert.StartsWith(title ? "ChakraPetch-Bold SDF" : "HudTextBold SDF", t.font.name, t.name + " " + px + " px");
            }
        }

        /// <summary>B6: victory shows the win emblem and a gold title; each stat card follows its text (kills off by default, T-4).</summary>
        [UnityTest]
        public IEnumerator Results_Victory_WinEmblem_CardsFollowTexts()
        {
            using var rig = new LobbyUiRig();
            yield return null;
            rig.JoinForm();
            rig.Click(rig.Root.JoinForm.Join);
            yield return rig.Until(() => rig.Visible(rig.Root.Results), 6f, "results");
            yield return null;
            var v = rig.Root.Results;
            var summary = v.transform.Find("Summary");
            Assert.AreEqual(ResultModel.VictoryText, v.Title.text);
            Assert.IsTrue(summary.Find("Emblem/EmblemWin").gameObject.activeSelf);
            Assert.IsFalse(summary.Find("Emblem/EmblemLose").gameObject.activeSelf);
            Assert.AreEqual(new Color32(0xFF, 0xDC, 0x6E, 0xFF), (Color32)v.Title.color);
            Assert.AreEqual(v.Kills.gameObject.activeSelf, summary.Find("Stats/KillsCard").gameObject.activeSelf);
            Assert.AreEqual(v.Survival.gameObject.activeSelf, summary.Find("Stats/SurvivalCard").gameObject.activeSelf);
            Assert.AreEqual(v.Cause.gameObject.activeSelf, summary.Find("CauseCard").gameObject.activeSelf);
            var stats = (RectTransform)summary.Find("Stats");
            var survival = (RectTransform)summary.Find("Stats/SurvivalCard");
            Assert.GreaterOrEqual(survival.rect.width + 0.5f, v.Survival.preferredWidth + (26f + 56f + 12f + 30f) / 1.5f, "card fits " + v.Survival.text);
            Assert.GreaterOrEqual(((RectTransform)v.Survival.transform).rect.width + 0.5f, v.Survival.preferredWidth, "no cut text");
            if (!v.Kills.gameObject.activeSelf)
            {
                var s = new Vector3[4]; var c = new Vector3[4];
                stats.GetWorldCorners(s); survival.GetWorldCorners(c);
                Assert.AreEqual((s[0].x + s[2].x) / 2f, (c[0].x + c[2].x) / 2f, 1f, "a single stat card is centred");
            }
        }

        /// <summary>QA T24-1, T24-2: the account chip shows both lines whole; version bottom right; API check only while ready.</summary>
        [UnityTest]
        public IEnumerator Home_ChipNotCut_Version_ApiCheckOnlyWhenReady()
        {
            using var rig = new LobbyUiRig();
            yield return rig.Until(() => rig.Flow.Api != null && rig.Flow.Api.Ready, 2f, "api ready");
            yield return null;
            yield return null; // chip layout and the ready redraw
            var m = rig.Root.MainMenu;
            foreach (var t in new[] { m.AccountStatus, m.AccountName })
                Assert.GreaterOrEqual(((RectTransform)t.transform).rect.width + 0.5f, t.preferredWidth, t.text + " not cut");
            Assert.AreEqual("Đã đăng nhập", m.AccountStatus.text);
            var chip = (RectTransform)m.transform.Find("AccountCard");
            Assert.AreEqual(1f, chip.pivot.x, "chip grows left from the mock's right edge");
            Assert.AreEqual((1884f - 960f) / 1.5f, chip.anchoredPosition.x, 0.5f);
            Assert.AreEqual("v" + Application.version, m.Version.text);
            Assert.AreEqual(20f, m.Version.fontSize * 1.5f, 0.05f);
            Assert.IsTrue(m.ApiReadyIcon.activeSelf, "check beside API sẵn sàng");
            Assert.AreEqual("kit_icon_check", SpriteOf(m.ApiReadyIcon.transform));
            rig.Scenarios.Api.Configured = false;
            rig.Flow.Api.Check();
            yield return rig.Until(() => !rig.Flow.Api.Ready, 2f, "api down");
            yield return null;
            Assert.IsFalse(m.ApiReadyIcon.activeSelf, "no check while the API is not ready");
        }

        /// <summary>T-028 đợt 3: approved icons replace the four placeholders.</summary>
        [UnityTest]
        public IEnumerator ApprovedIcons_OnTheFourButtons()
        {
            using var rig = new LobbyUiRig();
            yield return null;
            var r = rig.Root;
            string Icon(Button b) => SpriteOf(b.transform.Find("Face/Group/Icon"));
            Assert.AreEqual("kit_ico_practice", Icon(r.MainMenu.OfflinePractice));
            Assert.AreEqual("kit_ico_practice", Icon(r.Login.OfflinePractice));
            Assert.AreEqual("kit_ico_save", Icon(r.ServerSettings.Save));
            Assert.AreEqual("kit_ico_globe", Icon(r.Results.Online));
            Assert.AreEqual("kit_ico_fill", Icon(r.Lobby.FillBots));
        }

        /// <summary>QA T25-1, T25-2: the lost-match notice sits on the floor and says "Mất kết nối" once.</summary>
        [UnityTest]
        public IEnumerator Disconnected_OnFloor_TitleNotRepeated()
        {
            using var rig = new LobbyUiRig(LobbyCase.Disconnect, options: new LobbyOptions { ReconnectEnabled = false });
            yield return null;
            rig.JoinForm();
            rig.Click(rig.Root.JoinForm.Join);
            yield return rig.Until(() => rig.Flow.Screen == LobbyScreen.Disconnected, 4f, "disconnected");
            yield return null;
            var n = rig.Root.Notice;
            Assert.IsTrue(rig.Visible(n));
            Assert.IsTrue(rig.Root.Backdrop.activeSelf, "floor behind B8 hết hạn");
            Assert.AreEqual("Mất kết nối", n.Title.text);
            Assert.AreEqual(ReconnectController.LostText, rig.Flow.Notice, "flow string unchanged");
            Assert.AreEqual("", n.Message.text, "the body does not repeat the title");
            Assert.IsTrue(n.transform.Find("Panel/DisconnectIcon").gameObject.activeSelf);
        }

        /// <summary>Huy 07/11 (Cài đặt máy chủ): every field has a 20 px caption; the dropdown is the kit one with its arrow and kit list.</summary>
        [UnityTest]
        public IEnumerator ServerSettings_FieldLabels_KitDropdownWithArrow()
        {
            using var rig = new LobbyUiRig();
            yield return null;
            var s = rig.Root.ServerSettings;
            var fields = new (Component field, string label)[] { (s.Profile, "Kết nối"), (s.Host, "Máy chủ"), (s.Port, "Cổng"), (s.Api, "URL API") };
            foreach (var (field, expected) in fields)
            {
                var caption = field.transform.Find("FieldLabel").GetComponent<TMP_Text>();
                Assert.AreEqual(expected, caption.text, field.name);
                Assert.AreEqual(20f, caption.fontSize * 1.5f, 0.05f, field.name);
                Assert.AreEqual(56f / 1.5f, ((RectTransform)field.transform).sizeDelta.y, 0.01f, field.name + " is 56 px");
            }
            foreach (var input in new[] { s.Host, s.Port, s.Api })
            {
                Assert.AreEqual("kit_input_normal", SpriteOf(input), input.name);
                Assert.AreEqual("kit_input_focus", input.spriteState.selectedSprite.name, input.name);
            }
            Assert.AreEqual("kit_dropdown_closed", SpriteOf(s.Profile));
            Assert.AreEqual(Image.Type.Sliced, s.Profile.image.type);
            Assert.AreEqual("kit_dropdown_open", s.Profile.spriteState.selectedSprite.name, "open list: header swaps to the open sprite");
            var arrow = (RectTransform)s.Profile.transform.Find("Arrow");
            Assert.IsTrue(arrow.gameObject.activeSelf);
            Assert.AreEqual("kit_arrow_down", SpriteOf(arrow));
            Assert.AreEqual(Image.Type.Simple, arrow.GetComponent<Image>().type);
            Assert.AreEqual(new Vector2(16f, 10f) / 1.5f, arrow.sizeDelta);
            Assert.AreEqual(-34f / 1.5f, arrow.anchoredPosition.x - arrow.sizeDelta.x / 2f, 0.01f, "centred in the right 68 px box");
            var label = (RectTransform)s.Profile.transform.Find("Label");
            Assert.LessOrEqual(label.offsetMax.x, -68f / 1.5f, "value text stays left of the arrow box");
            var template = s.Profile.transform.Find("Template");
            Assert.AreEqual("kit_row_alt", SpriteOf(template));
            Assert.AreEqual("kit_row_normal", SpriteOf(template.Find("Viewport/Content/Item/Item Background")));
            Assert.AreEqual("kit_row_selected", SpriteOf(template.Find("Viewport/Content/Item/Item Checkmark")));
            Assert.AreEqual("kit_scroll_track", SpriteOf(template.Find("Scrollbar")));
            Assert.AreEqual("kit_scroll_thumb", SpriteOf(template.Find("Scrollbar/Sliding Area/Handle")));
        }

        /// <summary>No overlapping 9-slice anywhere in LobbyUi: every Sliced image is at least as large as its borders.</summary>
        [UnityTest]
        public IEnumerator SlicedImages_NeverSmallerThanTheirBorders()
        {
            using var rig = new LobbyUiRig();
            yield return null;
            yield return null;
            foreach (var img in rig.Root.GetComponentsInChildren<Image>(true))
            {
                if (img.sprite == null || img.type != Image.Type.Sliced) continue;
                var rect = ((RectTransform)img.transform).rect;
                if (rect.width <= 0f || rect.height <= 0f) continue; // empty (hidden) bar fill, or a scroll handle laid out at run time
                var b = img.sprite.border / (img.pixelsPerUnit * img.pixelsPerUnitMultiplier); // left, bottom, right, top in canvas units
                var path = img.name;
                for (var t = img.transform.parent; t != null && t != rig.Root.transform; t = t.parent) path = t.name + "/" + path;
                Assert.GreaterOrEqual(rect.width + 0.5f, b.x + b.z, path + " (" + img.sprite.name + ") narrower than left + right border");
                Assert.GreaterOrEqual(rect.height + 0.5f, b.y + b.w, path + " (" + img.sprite.name + ") lower than top + bottom border");
            }
        }

        /// <summary>B8: the bar shows the grace left (full when the overlay opens).</summary>
        [UnityTest]
        public IEnumerator Reconnect_BarShowsGraceLeft()
        {
            using var rig = new LobbyUiRig(LobbyCase.Disconnect);
            yield return null;
            rig.JoinForm();
            rig.Click(rig.Root.JoinForm.Join);
            yield return rig.Until(() => rig.Visible(rig.Root.Reconnect), 4f, "reconnect overlay");
            yield return null;
            var fill = (RectTransform)rig.Root.Reconnect.transform.Find("Panel/Progress/FillArea/Fill");
            Assert.IsTrue(fill.gameObject.activeSelf, "grace left > 0");
            Assert.That(fill.anchorMax.x, Is.InRange(0.5f, 1f));
            Assert.IsFalse(rig.Root.Backdrop.activeSelf, "B8 sits over the HUD, not on the floor");
        }
    }
}
#endif
