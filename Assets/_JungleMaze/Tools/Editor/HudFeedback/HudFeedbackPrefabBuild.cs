using System;
using JungleMaze.Presentation.UI.Hud;
using JungleMaze.Presentation.UI.Hud.Feedback;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Tier = JungleMaze.EditorTools.UiKit.Tier;

namespace JungleMaze.EditorTools
{
    /// <summary>
    /// HUD feedback layer (B9, T-022 skin A): hit marker, damage arcs, offscreen danger arrows with seconds, effect chips
    /// (max 6), pickup list, "Đang dùng" strip, bomb/trap banner and the low-HP vignette under the HUD. Geometry is 1080p
    /// px from mock T-021/b/b9-phan-hoi-hud.png and the T-028 pack table (b9-phan-hoi-hud.md); UiKit.Px divides by 1.5.
    /// Damage arc, danger arrow and vignette keep their own red (Huy 07/11). Run after "Apply layout B + skin A".
    /// Idempotent: removes and rebuilds only the feedback nodes.
    /// </summary>
    public static class HudFeedbackPrefabBuild
    {
        private const string Ui = "Assets/_JungleMaze/Prefabs/UI/";
        private const string HudPath = Ui + "Hud.prefab";
        private const string ZoneIconPath = "Assets/_JungleMaze/Art/Hud/StatusIcons/icon_zone-timer.png";

#pragma warning disable 67
        /// <summary>
        /// Kept so UiArtAssetBuild still compiles; no longer raised (T-022): its direction A pass (ToastBg, EffectFrame,
        /// DangerBanner sprites, "QuickBar/WeaponStrip" frame) would overwrite the kit skin built here.
        /// </summary>
        public static event Action AfterBuild;
#pragma warning restore 67

        /// <summary>
        /// Stacked fx_vignette layers at low HP (art alpha 0.38 at the edge: 1 layer 0.38, 2 layers 1-(1-0.38)^2 = 0.62; the
        /// centre stays clear either way). 2 = proposal after the Play check (22% HP faint on bright grass); set 1 to compare.
        /// </summary>
        public static int VignetteLayers = 2;

        [MenuItem("Jungle Maze/HUD/Build Feedback Layer")]
        public static void Build()
        {
            BuildChipPrefab();
            BuildToastPrefab();
            BuildArcPrefab();
            BuildArrowPrefab();
            BuildCatalog();
            BuildSlotPrefab();
            KeyCapTypeScale();

            var go = PrefabUtility.LoadPrefabContents(HudPath);
            try
            {
                var core = go.GetComponent<HudRoot>();
                var root = (RectTransform)go.transform;
                var old = go.GetComponent<HudFeedbackRoot>();
                if (old != null) Object.DestroyImmediate(old);
                foreach (var name in new[] { "EffectsRow", "PickupFeed", "DangerBanner", "FeedbackOverlay", "HudKillFeed", "PickupList" }) Remove(root, name);
                for (var child = root.Find("LowHpVignette"); child != null; child = root.Find("LowHpVignette")) Object.DestroyImmediate(child.gameObject);
                Remove(core.Quickbar.transform, "UseActionStrip");

                SetPalette(core);
                WeaponStripGroup(core);
                SlotInUseFrames(core);

                var feedback = go.AddComponent<HudFeedbackRoot>();
                Vignette(root, core);
                Set(feedback, "effectRow", EffectRow(root));
                Set(feedback, "pickupFeed", PickupFeed(root));
                PickupList(root);
                Set(feedback, "useAction", UseStrip(core));
                Set(feedback, "dangerBanner", Banner(root));
                Overlay(root, feedback);
                KillFeed(root);
                Set(feedback, "catalog", AssetDatabase.LoadAssetAtPath<HudEffectCatalog>(Ui + "HudEffectCatalog.asset"));

                // Modal panels (HudPanelsBuild) stay above the feedback layer.
                foreach (var name in new[] { "HudInventory", "HudBigMap" })
                {
                    var panel = root.Find(name);
                    if (panel != null) panel.SetAsLastSibling();
                }
                foreach (var graphic in go.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
                core.ApplyPalette();
                PrefabUtility.SaveAsPrefabAsset(go, HudPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(go);
            }
            AssetDatabase.SaveAssets();
        }

        // ---------- Hud.prefab parts ----------

        private static void SetPalette(HudRoot core)
        {
            var palette = new SerializedObject(core).FindProperty("palette");
            palette.FindPropertyRelative("Green").colorValue = HudPalette.Default.Green;
            palette.FindPropertyRelative("HitMarker").colorValue = Color.white;
            palette.FindPropertyRelative("DangerFill").colorValue = HudPalette.Default.DangerFill;
            palette.serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WeaponStripGroup(HudRoot core)
        {
            var strip = core.Quickbar.WeaponStrip;
            var group = strip.GetComponent<CanvasGroup>();
            if (group == null) group = strip.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 1;
            Set(core.Quickbar, "weaponStripGroup", group);
        }

        /// <summary>The slot prefab is nested in Hud; sync each instance's in-use overlay reference.</summary>
        private static void SlotInUseFrames(HudRoot core)
        {
            for (var i = 0; i < core.Quickbar.SlotViewCount; i++)
            {
                var slot = core.Quickbar.GetSlotView(i);
                var frame = slot.transform.Find("InUseFrame");
                if (frame == null)
                {
                    var rect = Node(slot.transform, "InUseFrame");
                    rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
                    rect.sizeDelta = new Vector2(64, 64);
                    LegacyImage(rect, "HudRound6Stroke2").enabled = false;
                    frame = rect;
                }
                Set(slot, "inUseFrame", frame.GetComponent<Image>());
            }
        }

        /// <summary>
        /// fx_vignette over the whole screen, <see cref="VignetteLayers"/> stacked, first children so they draw under every
        /// HUD block; VitalsView turns them on at low HP.
        /// </summary>
        private static void Vignette(RectTransform root, HudRoot core)
        {
            var layers = new Image[Mathf.Max(1, VignetteLayers)];
            for (var i = 0; i < layers.Length; i++)
            {
                var t = UiKit.Stretch(UiKit.Node(root, "LowHpVignette"));
                t.SetSiblingIndex(i);
                layers[i] = UiKit.Image(t, "fx_vignette");
                layers[i].preserveAspect = false; // 1920x1080 art stretched to the screen like the mock
                layers[i].enabled = false;
            }
            Set(core.Vitals, "lowHpVignette", layers[0]);
            var extra = new Object[layers.Length - 1];
            for (var i = 1; i < layers.Length; i++) extra[i - 1] = layers[i];
            SetArray(core.Vitals, "lowHpVignetteExtra", extra);
        }

        /// <summary>
        /// Kill feed top-left (mock: kit_killfeed_panel at (15, 34), rows killer 24 px + weapon icon 32 + victim, 44 px
        /// apart, text from x 37). Not wired to data (no all-players kill source yet); hidden until <c>Push</c>.
        /// </summary>
        private static void KillFeed(RectTransform root)
        {
            var host = UiKit.Stretch(UiKit.Node(root, "HudKillFeed"));
            host.SetSiblingIndex(Mathf.Max(1, VignetteLayers)); // above the vignette, under the other blocks
            var view = host.gameObject.AddComponent<HudKillFeedView>();
            var panel = UiKit.Place(UiKit.Node(host, "Panel"), 15, 34, 270, 154);
            UiKit.Image(panel, "kit_killfeed_panel");
            var so = new SerializedObject(view);
            var rows = so.FindProperty("rows");
            rows.arraySize = 3;
            for (var i = 0; i < 3; i++)
            {
                var row = UiKit.Place(UiKit.Node(panel, "Row" + (i + 1)), 0, 11 + i * 44, 270, 44);
                var killer = UiKit.Text(row, "Killer", Tier.B2, "Hào", UiKit.Colors.Text);
                UiKit.Place(killer.rectTransform, 22, 0, 60, 44);
                var weapon = UiKit.Place(UiKit.Node(row, "Weapon"), 94, 6, 32, 32).gameObject.AddComponent<Image>();
                weapon.preserveAspect = true;
                var victim = UiKit.Text(row, "Victim", Tier.B2, "Linh", UiKit.Colors.Text);
                UiKit.Place(victim.rectTransform, 138, 0, 60, 44);
                var e = rows.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("Root").objectReferenceValue = row;
                e.FindPropertyRelative("Killer").objectReferenceValue = killer;
                e.FindPropertyRelative("Weapon").objectReferenceValue = weapon;
                e.FindPropertyRelative("Victim").objectReferenceValue = victim;
                row.gameObject.SetActive(false);
            }
            so.FindProperty("panel").objectReferenceValue = panel;
            foreach (var (field, px) in new[] { ("textLeft", 22f), ("iconGap", 12f), ("iconSize", 32f), ("rowHeight", 44f), ("padTop", 11f), ("minWidth", 270f), ("extraWidth", 70f) })
                so.FindProperty(field).floatValue = UiKit.Px(px);
            so.ApplyModifiedPropertiesWithoutUndo();
            panel.gameObject.SetActive(false);
        }

        /// <summary>
        /// Chips 116 px on a 124 px step, icon 76 + seconds tag 60×36 under it (mock: first chip centre x 90, icon y 730,
        /// tag y 790). Three per line so six never reach the clear centre (x 480): chips 0-2 on the mock line, 3-5 above.
        /// </summary>
        private static EffectRowView EffectRow(RectTransform root)
        {
            var row = Anchored(UiKit.Node(root, "EffectsRow"), new Vector2(0, 0), 32, 1080 - 808, 3 * 124 - 8, 2 * 116 + 8);
            row.gameObject.AddComponent<Canvas>();
            var view = row.gameObject.AddComponent<EffectRowView>();
            var chips = new Object[6];
            for (var i = 0; i < chips.Length; i++)
            {
                var chip = InstantiatePrefab("HudEffectChip", row);
                UiKit.Place(chip, i % 3 * 124, (1 - i / 3) * 124, 116, 116);
                chips[i] = chip.GetComponent<EffectChipView>();
            }
            SetArray(view, "chips", chips);
            return view;
        }

        /// <summary>
        /// Pickup toasts, bottom-right (T-022 R3, T-028 b9 "toast nhặt": kit_toast 308×92, icon 72, name + ×n): step 98,
        /// newest at the bottom, 46 px above the screen bottom and 20 px from the right; "+n món khác" tag above. The tag
        /// is 180 wide, not the mock's 90: the logic text carries "món khác".
        /// </summary>
        private static PickupFeedView PickupFeed(RectTransform root)
        {
            var feed = Anchored(UiKit.Node(root, "PickupFeed"), new Vector2(1, 0), -20, 46, PickupW, PickupH);
            var view = feed.gameObject.AddComponent<PickupFeedView>();
            var rows = new Object[3];
            for (var i = 0; i < rows.Length; i++)
            {
                var row = InstantiatePrefab("HudPickupToast", feed);
                row.anchorMin = row.anchorMax = row.pivot = new Vector2(1, 0);
                row.anchoredPosition = new Vector2(0, (2 - i) * UiKit.Px(ToastStep));
                rows[i] = row.GetComponent<PickupToastView>();
            }
            SetArray(view, "rows", rows);
            SetFloat(view, "rowStep", UiKit.Px(ToastStep));

            var overflow = UiKit.Node(feed, "Overflow");
            overflow.anchorMin = overflow.anchorMax = overflow.pivot = Vector2.one;
            overflow.anchoredPosition = Vector2.zero;
            overflow.sizeDelta = new Vector2(UiKit.Px(180), UiKit.Px(36));
            var tag = UiKit.Image(overflow, "kit_tag_neutral");
            var group = Group(overflow);
            var label = UiKit.Text(overflow, "Label", Tier.B2, "+2 món khác", UiKit.Colors.Text, TextAlignmentOptions.Center);
            UiKit.Stretch(label.rectTransform);
            Set(view, "overflowGroup", group);
            Set(view, "overflowLabel", label);
            Set(view, "overflowBackground", tag);
            return view;
        }

        /// <summary>
        /// "Đang dùng" strip above the quick bar, centred on the screen (mock: toast 620×64 at y 850, icon 52, progress
        /// 360×24, seconds Gold). Widened to 860: the UI-01 label and the "Di chuyển để hủy" hint stay readable (tests).
        /// </summary>
        private static UseActionView UseStrip(HudRoot core)
        {
            // Quick bar top-left is (668, 950); the strip's top-left (530, 850) relative to it.
            var use = UiKit.Place(UiKit.Node((RectTransform)core.Quickbar.transform, "UseActionStrip"), 530 - 668, 850 - 950, 860, 64);
            use.gameObject.AddComponent<Canvas>();
            var background = UiKit.Image(use, "kit_toast");
            var group = Group(use);
            var view = use.gameObject.AddComponent<UseActionView>();
            var icon = UiKit.Place(UiKit.Node(use, "Icon"), 14, 6, 52, 52).gameObject.AddComponent<Image>();
            icon.preserveAspect = true;
            icon.enabled = false;
            var label = UiKit.Text(use, "Label", Tier.B2, "Đang dùng", UiKit.Colors.Text);
            UiKit.Place(label.rectTransform, 78, 0, 340, 64); // R3: Vietnamese names ("Đang dùng Lá thần mộc" renders ~300 px in TMP)
            label.overflowMode = TextOverflowModes.Ellipsis;
            var track = UiKit.Place(UiKit.Node(use, "Track"), 428, 20, 120, 24);
            UiKit.Image(track, "kit_progress_track");
            var area = UiKit.Stretch(UiKit.Node(track, "FillArea"));
            area.offsetMin = new Vector2(UiKit.Px(6), UiKit.Px(4));
            area.offsetMax = new Vector2(-UiKit.Px(6), -UiKit.Px(4));
            var fill = UiKit.Node(area, "Fill");
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = Vector2.one;
            fill.offsetMin = fill.offsetMax = Vector2.zero;
            UiKit.Image(fill, "kit_progress_fill");
            var seconds = UiKit.Text(use, "Seconds", Tier.B2, "0.5s", UiKit.Colors.Gold);
            UiKit.Place(seconds.rectTransform, 556, 0, 70, 64); // "10.0s" ≈ 65 px
            var hint = UiKit.Text(use, "Hint", Tier.B2, "Di chuyển để hủy", UiKit.Colors.Muted);
            UiKit.Place(hint.rectTransform, 632, 0, 220, 64); // ≈ 204 px
            Set(view, "group", group);
            Set(view, "label", label);
            Set(view, "seconds", seconds);
            Set(view, "hint", hint);
            Set(view, "fill", fill);
            Set(view, "background", background);
            Set(view, "icon", icon);
            return view;
        }

        /// <summary>
        /// Bomb/trap banner, top centre: kit_toast 620×60 at y 36 with kit_icon_warn (29×26) in place of "! ". The mock's
        /// 340 px icon-only banner cannot hold the logic text ("Bom nổ sau 12s · hướng Đông Bắc, 12 ô +15" ≈ 514 px at 24).
        /// </summary>
        private static DangerBannerView Banner(RectTransform root)
        {
            var banner = UiKit.Node(root, "DangerBanner");
            banner.anchorMin = banner.anchorMax = banner.pivot = new Vector2(0.5f, 1);
            banner.anchoredPosition = new Vector2(0, -UiKit.Px(36));
            banner.sizeDelta = new Vector2(UiKit.Px(620), UiKit.Px(60));
            var background = UiKit.Image(banner, "kit_toast");
            var group = Group(banner);
            var view = banner.gameObject.AddComponent<DangerBannerView>();
            var border = UiKit.Stretch(UiKit.Node(banner, "Border")).gameObject.AddComponent<Image>();
            border.enabled = false; // red stays off the banner frame (colour rule); the view still sets its colour
            UiKit.Image(UiKit.Place(UiKit.Node(banner, "Icon"), 14, 17, 29, 26), "kit_icon_warn");
            TMP_Text Part(string name, string value)
            {
                var t = UiKit.Text(banner, name, Tier.B2, value, UiKit.Colors.Text);
                UiKit.Place(t.rectTransform, 50, 12, 120, 36);
                return t;
            }
            Set(view, "group", group);
            Set(view, "label", Part("Label", "Bom nổ sau "));
            Set(view, "timer", Part("Seconds", "8s"));
            Set(view, "suffix", Part("Direction", " · hướng Tây, 4 ô"));
            Set(view, "background", background);
            Set(view, "border", border);
            SetFloat(view, "textLeft", UiKit.Px(50));
            SetFloat(view, "textTop", UiKit.Px(12));
            SetFloat(view, "textWidth", UiKit.Px(620 - 50 - 14));
            SetFloat(view, "textHeight", UiKit.Px(36));
            return view;
        }

        /// <summary>
        /// F pickup list beside the hero (Vũ, 0ecca9e8; logic in PickupListView, rows built at runtime). Kept here so a
        /// rebuild does not drop it. Skin A: Be Vietnam Pro Bold for name and count; panel, outline and key cap stay
        /// Vũ's HudRound6 / KeyCap (the key cap matches the quickbar "R Nạp" hint).
        /// </summary>
        private static void PickupList(RectTransform root)
        {
            var list = UiKit.Node(root, "PickupList");
            list.anchorMin = list.anchorMax = Vector2.zero;
            list.pivot = new Vector2(0f, 0.5f);
            list.anchoredPosition = new Vector2(700f, 360f); // moved next to the hero every frame by the view
            list.sizeDelta = new Vector2(280f, 40f);
            Group(list);
            var view = list.gameObject.AddComponent<PickupListView>();
            Set(view, "textFont", UiKit.Body);
            Set(view, "numberFont", UiKit.Body);
            Set(view, "panelSprite", Load<Sprite>("Assets/_JungleMaze/Art/Hud/HudRound6.png"));
            Set(view, "outlineSprite", Load<Sprite>("Assets/_JungleMaze/Art/Hud/HudRound6Stroke1.png"));
            Set(view, "keyCapSprite", Load<Sprite>("Assets/_JungleMaze/Art/Hud/KeyCap.png"));
            // lootTuning stays empty: the view loads Resources/GroundLootTuning at start, and a prefab reference would pull
            // ThrowableArtCatalog's JSurviv weapon sprites into Hud.prefab (Art04 AC17: no image under Imported/JSurviv).
            var keepClear = new Object[] { Find(root, "Vitals"), Find(root, "QuickBar"), Find(root, "EffectsRow"), Find(root, "PickupFeed") };
            SetArray(view, "keepClearOf", keepClear);
            var feed = root.Find("PickupFeed");
            if (feed != null) list.SetSiblingIndex(feed.GetSiblingIndex() + 1);
        }

        private static RectTransform Find(RectTransform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<RectTransform>(true)) if (t.name == name) return t;
            throw new InvalidOperationException("Hud.prefab has no " + name);
        }

        /// <summary>
        /// Reload ring above the hero (Vũ, a9c178e7): HudRing128 track and a radial fill from the top. HudFeedbackRoot hides
        /// it at start (CombatWeaponPresenter draws the world ring), so it is wired only to keep main's references.
        /// </summary>
        private static ReloadRingView ReloadRing(RectTransform overlay)
        {
            var ring = UiKit.Node(overlay, "ReloadRing");
            ring.anchorMin = ring.anchorMax = ring.pivot = new Vector2(0.5f, 0.5f);
            ring.sizeDelta = new Vector2(30f, 30f);
            var group = Group(ring);
            var sprite = Load<Sprite>("Assets/_JungleMaze/Art/Hud/HudRing128.png");
            var track = UiKit.Stretch(UiKit.Node(ring, "Track")).gameObject.AddComponent<Image>();
            track.sprite = sprite;
            var fill = UiKit.Stretch(UiKit.Node(ring, "Fill")).gameObject.AddComponent<Image>();
            fill.sprite = sprite;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Radial360;
            fill.fillOrigin = (int)Image.Origin360.Top;
            fill.fillClockwise = true;
            fill.fillAmount = 0f;
            var view = ring.gameObject.AddComponent<ReloadRingView>();
            Set(view, "group", group);
            Set(view, "track", track);
            Set(view, "fill", fill);
            return view;
        }

        /// <summary>Hit marker (fx_hitmarker 101×96 in a 96 px box at the aim point), 4 damage arcs, 3 danger arrows.</summary>
        private static void Overlay(RectTransform root, HudFeedbackRoot feedback)
        {
            var overlay = UiKit.Node(root, "FeedbackOverlay");
            overlay.anchorMin = overlay.anchorMax = overlay.pivot = new Vector2(0.5f, 0.5f);
            overlay.sizeDelta = new Vector2(1280, 720);
            overlay.gameObject.AddComponent<Canvas>();
            Set(feedback, "reloadRing", ReloadRing(overlay));

            var hit = Centred(UiKit.Node(overlay, "HitMarker"), 96, 96);
            var hitGroup = Group(hit);
            var hitView = hit.gameObject.AddComponent<HitMarkerView>();
            var mark = UiKit.Image(UiKit.Stretch(UiKit.Node(hit, "Mark")), "fx_hitmarker");
            Set(hitView, "group", hitGroup);
            SetArray(hitView, "ticks", new Object[] { mark });

            var arcs = new Object[4];
            for (var i = 0; i < arcs.Length; i++) arcs[i] = Centred(InstantiatePrefab("HudDamageArc", overlay), 54, 180).GetComponent<DamageArcView>();
            var arrows = new Object[3];
            for (var i = 0; i < arrows.Length; i++) arrows[i] = Centred(InstantiatePrefab("HudDangerArrow", overlay), 45, 45).GetComponent<DangerArrowView>();
            Set(feedback, "hitMarker", hitView);
            SetArray(feedback, "damageArcs", arcs);
            SetArray(feedback, "dangerArrows", arrows);
        }

        // ---------- Child prefabs ----------

        /// <summary>Effect chip 116×116: icon 76 (catalog art) centred on top, seconds tag 60×36 (24 px) under it.</summary>
        private static void BuildChipPrefab()
        {
            var chip = PrefabRoot("HudEffectChip", 116, 116);
            try
            {
                var group = Group(chip);
                var view = chip.gameObject.AddComponent<EffectChipView>();
                var box = UiKit.Place(UiKit.Node(chip, "Box"), 0, 0, 116, 76);
                var border = UiKit.Stretch(UiKit.Node(box, "Border")).gameObject.AddComponent<Image>();
                border.enabled = false; // polarity colour kept for the view; green/amber only on the icon glyphs (colour rule)
                var icon = UiKit.Place(UiKit.Node(box, "Icon"), 20, 0, 76, 76).gameObject.AddComponent<Image>();
                icon.preserveAspect = true;
                icon.enabled = false;
                var label = UiKit.Text(box, "Label", Tier.B2, "Độc", UiKit.Colors.Text, TextAlignmentOptions.Center);
                UiKit.Stretch(label.rectTransform);
                label.overflowMode = TextOverflowModes.Ellipsis;
                var tag = UiKit.Place(UiKit.Node(chip, "SecondsTag"), 28, 80, 60, 36);
                var plate = UiKit.Image(tag, "kit_tag_neutral");
                var seconds = UiKit.Text(tag, "Seconds", Tier.B2, "3s", UiKit.Colors.Text, TextAlignmentOptions.Center); // "12s" about 39 px; has "s", so gameplay floor 24
                UiKit.Stretch(seconds.rectTransform);
                // The view drives a fill ratio; mock B9 shows no bar, so the track stays hidden.
                var track = UiKit.Place(UiKit.Node(chip, "Track"), 28, 112, 60, 4);
                track.gameObject.SetActive(false);
                var fill = UiKit.Stretch(UiKit.Node(track, "Fill"));
                fill.gameObject.AddComponent<Image>();
                Set(view, "group", group);
                Set(view, "border", border);
                Set(view, "icon", icon);
                Set(view, "label", label);
                Set(view, "seconds", seconds);
                Set(view, "fill", fill);
                Set(view, "secondsPlate", plate);
                Save(chip, "HudEffectChip");
            }
            finally
            {
                Object.DestroyImmediate(chip.gameObject);
            }
        }

        public const float ToastW = 308, ToastH = 92, ToastStep = 98, PickupW = ToastW, PickupH = 3 * ToastH + 2 * (ToastStep - ToastH) + 48;

        /// <summary>Pickup toast 308×92 (kit_toast): "+" at 16, item icon 72 at 40, Vietnamese name (2 lines) at 122, ×n right.</summary>
        private static void BuildToastPrefab()
        {
            var toast = PrefabRoot("HudPickupToast", ToastW, ToastH);
            try
            {
                var background = UiKit.Image(toast, "kit_toast");
                var group = Group(toast);
                var view = toast.gameObject.AddComponent<PickupToastView>();
                var plus = UiKit.Text(toast, "Plus", Tier.B2, "+", UiKit.Colors.Text);
                UiKit.Place(plus.rectTransform, 16, 0, 20, ToastH);
                // Placeholder until the ID resolves (ART-04); the view swaps in the item icon, Simple + preserveAspect.
                var icon = UiKit.Image(UiKit.Place(UiKit.Node(toast, "Icon"), 40, 10, 72, 72), "kit_slot_item_empty");
                var name = UiKit.Text(toast, "Name", Tier.B2, "Lá thần mộc", UiKit.Colors.Text);
                UiKit.Place(name.rectTransform, 122, 6, 126, 80);
                name.textWrappingMode = TextWrappingModes.Normal; // "Lông vũ phượng hoàng lửa" takes two lines
                name.overflowMode = TextOverflowModes.Ellipsis;
                name.lineSpacing = -20;
                var count = UiKit.Text(toast, "Count", Tier.B2, "×2", UiKit.Colors.Muted, TextAlignmentOptions.MidlineRight);
                UiKit.Place(count.rectTransform, 250, 0, 44, ToastH);
                Set(view, "background", background);
                Set(view, "group", group);
                Set(view, "plus", plus);
                Set(view, "icon", icon);
                Set(view, "label", name);
                Set(view, "count", count);
                Save(toast, "HudPickupToast");
            }
            finally
            {
                Object.DestroyImmediate(toast.gameObject);
            }
        }

        /// <summary>
        /// Damage arc: fx_damage_arc in 54×180 px (mock 60×200; 10% smaller so it intrudes at most 8 u into the clear
        /// centre from the 330×190 u ring, as before). The view mirrors it so it bulges away from the hero.
        /// </summary>
        private static void BuildArcPrefab()
        {
            var arc = Centred(PrefabRoot("HudDamageArc", 54, 180), 54, 180);
            try
            {
                var image = UiKit.Image(UiKit.Stretch(UiKit.Node(arc, "Graphic")), "fx_damage_arc");
                var group = Group(arc);
                var view = arc.gameObject.AddComponent<DamageArcView>();
                Set(view, "group", group);
                Set(view, "image", image);
                Save(arc, "HudDamageArc");
            }
            finally
            {
                Object.DestroyImmediate(arc.gameObject);
            }
        }

        /// <summary>
        /// Danger arrow: root 45 px at the ring point. The "Arrow" (fx_danger_arrow 45×63, mock 64×90 scaled so its side
        /// stays within 8 u of the clear centre) has its pivot on the inner end and is mirrored, so the left-pointing art
        /// points along the view's angle and sits outside the ring. Timer: zone icon 34 + tag 70×36 with the seconds to the
        /// right (mock: icon +66, tag +130 px from the arrow), hidden for untimed traps.
        /// </summary>
        private static void BuildArrowPrefab()
        {
            var arrow = Centred(PrefabRoot("HudDangerArrow", 45, 45), 45, 45);
            try
            {
                var group = Group(arrow);
                var view = arrow.gameObject.AddComponent<DangerArrowView>();
                var shape = UiKit.Node(arrow, "Arrow");
                shape.anchorMin = shape.anchorMax = new Vector2(0.5f, 0.5f);
                shape.pivot = new Vector2(1, 0.5f);
                shape.anchoredPosition = Vector2.zero;
                shape.sizeDelta = new Vector2(UiKit.Px(45), UiKit.Px(63));
                shape.localScale = new Vector3(-1, 1, 1);
                UiKit.Image(shape, "fx_danger_arrow");

                var timer = UiKit.Node(arrow, "Timer");
                timer.anchorMin = timer.anchorMax = new Vector2(0.5f, 0.5f);
                timer.pivot = new Vector2(0, 0.5f);
                timer.anchoredPosition = new Vector2(UiKit.Px(66 - 17), 0);
                timer.sizeDelta = new Vector2(UiKit.Px(116), UiKit.Px(36));
                var zone = UiKit.Place(UiKit.Node(timer, "ZoneIcon"), 0, 1, 34, 34).gameObject.AddComponent<Image>();
                zone.sprite = Load<Sprite>(ZoneIconPath);
                zone.preserveAspect = true;
                var tag = UiKit.Place(UiKit.Node(timer, "Tag"), 46, 0, 70, 36);
                UiKit.Image(tag, "kit_tag_neutral");
                var seconds = UiKit.Text(tag, "Seconds", Tier.B2, "8s", UiKit.Colors.Text, TextAlignmentOptions.Center);
                UiKit.Stretch(seconds.rectTransform);
                Set(view, "group", group);
                Set(view, "arrow", shape);
                Set(view, "seconds", seconds);
                Set(view, "timerGroup", timer.gameObject);
                Save(arrow, "HudDangerArrow");
            }
            finally
            {
                Object.DestroyImmediate(arrow.gameObject);
            }
        }

        /// <summary>Kind and label per effect (icons are bound by the UiArt catalog build and kept here).</summary>
        private static void BuildCatalog()
        {
            var path = Ui + "HudEffectCatalog.asset";
            var catalog = AssetDatabase.LoadAssetAtPath<HudEffectCatalog>(path);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<HudEffectCatalog>();
                AssetDatabase.CreateAsset(catalog, path);
            }
            var so = new SerializedObject(catalog);
            var entries = so.FindProperty("entries");
            var labels = new[] { "Adrenaline", "Độc", "Chậm", "Ẩn", "Ngất", "Trói", "Đảo", "Miễn" };
            entries.arraySize = labels.Length;
            for (var i = 0; i < labels.Length; i++)
            {
                var e = entries.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("Kind").enumValueIndex = i + 1;
                e.FindPropertyRelative("Label").stringValue = labels[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Amber in-use frame on the quick-bar slot prefab (off until an item is being used).</summary>
        private static void BuildSlotPrefab()
        {
            var slot = PrefabUtility.LoadPrefabContents(Ui + "HudSlot.prefab");
            try
            {
                Remove(slot.transform, "InUseFrame");
                var rect = Node(slot.transform, "InUseFrame");
                rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
                rect.sizeDelta = new Vector2(64, 64);
                var img = LegacyImage(rect, "HudRound6Stroke2");
                img.color = HudPalette.Default.Amber;
                img.enabled = false;
                var view = slot.GetComponent<QuickbarSlotView>();
                Set(view, "inUseFrame", img);
                // T-027 gate reads the source prefab too: key and count numbers 20, the (hidden) name 24.
                UiKit.ApplyTier(view.KeyLabel, Tier.C);
                UiKit.ApplyTier(view.CountLabel, Tier.C);
                UiKit.ApplyTier(view.NameLabel, Tier.B2);
                PrefabUtility.SaveAsPrefabAsset(slot, Ui + "HudSlot.prefab");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(slot);
            }
        }

        /// <summary>The reload key cap prefab ("R"): a key on a key cap, 20 px (T-027 gate on the source prefab).</summary>
        private static void KeyCapTypeScale()
        {
            var cap = PrefabUtility.LoadPrefabContents(Ui + "HudKeyCap.prefab");
            try
            {
                foreach (var text in cap.GetComponentsInChildren<TMP_Text>(true)) UiKit.ApplyTier(text, Tier.C);
                PrefabUtility.SaveAsPrefabAsset(cap, Ui + "HudKeyCap.prefab");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(cap);
            }
        }

        // ---------- helpers ----------

        private static RectTransform PrefabRoot(string name, float wPx, float hPx)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var t = (RectTransform)go.transform;
            t.sizeDelta = new Vector2(UiKit.Px(wPx), UiKit.Px(hPx));
            return t;
        }

        private static void Save(RectTransform root, string name) => PrefabUtility.SaveAsPrefabAsset(root.gameObject, Ui + name + ".prefab");

        private static RectTransform InstantiatePrefab(string name, Transform parent) =>
            (RectTransform)((GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>(Ui + name + ".prefab"), parent)).transform;

        private static RectTransform Node(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void Remove(Transform parent, string name)
        {
            var child = parent.Find(name);
            if (child != null) Object.DestroyImmediate(child.gameObject);
        }

        /// <summary>Anchor and pivot at <paramref name="anchor"/>; offset and size in 1080p px.</summary>
        private static RectTransform Anchored(RectTransform t, Vector2 anchor, float xPx, float yPx, float wPx, float hPx)
        {
            t.anchorMin = t.anchorMax = t.pivot = anchor;
            t.anchoredPosition = new Vector2(UiKit.Px(xPx), UiKit.Px(yPx));
            t.sizeDelta = new Vector2(UiKit.Px(wPx), UiKit.Px(hPx));
            return t;
        }

        private static RectTransform Centred(RectTransform t, float wPx, float hPx)
        {
            t.anchorMin = t.anchorMax = t.pivot = new Vector2(0.5f, 0.5f);
            t.anchoredPosition = Vector2.zero;
            t.sizeDelta = new Vector2(UiKit.Px(wPx), UiKit.Px(hPx));
            return t;
        }

        private static CanvasGroup Group(RectTransform t)
        {
            var g = t.gameObject.AddComponent<CanvasGroup>();
            g.alpha = 0;
            g.interactable = false;
            g.blocksRaycasts = false;
            return g;
        }

        /// <summary>Direction A sprite still used by the quick-bar in-use frame (not part of B9).</summary>
        private static Image LegacyImage(RectTransform t, string sprite)
        {
            var img = t.gameObject.AddComponent<Image>();
            img.raycastTarget = false;
            img.sprite = Load<Sprite>("Assets/_JungleMaze/Art/Hud/" + sprite + ".png");
            img.type = Image.Type.Sliced;
            return img;
        }

        private static T Load<T>(string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) throw new InvalidOperationException("Asset missing: " + path);
            return asset;
        }

        private static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field) ?? throw new InvalidOperationException(target.GetType().Name + " has no field " + field);
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(Object target, string field, float value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field) ?? throw new InvalidOperationException(target.GetType().Name + " has no field " + field);
            p.floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetArray(Object target, string field, Object[] values)
        {
            var so = new SerializedObject(target);
            var array = so.FindProperty(field) ?? throw new InvalidOperationException(target.GetType().Name + " has no field " + field);
            array.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
