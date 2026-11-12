using System;
using JungleMaze.App.Hud;
using JungleMaze.App.Hud.Feedback;
using JungleMaze.App.Ports;
using JungleMaze.App.World;
using JungleMaze.Core.DI;
using JungleMaze.Presentation.World.Loot;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JungleMaze.Presentation.UI.Hud
{
    /// <summary>Sizes (canvas px at 1280×720), offsets and colours of the F pickup list; edit on Hud.prefab → PickupList.</summary>
    [Serializable]
    public struct PickupListLayout
    {
        [Tooltip("Panel left edge, in world cells right of the hero; just past the 1.2-cell reach so it does not sit on the items in reach.")]
        public float OffsetCells;
        [Tooltip("Panel middle, in world cells above the hero's feet.")]
        public float RaiseCells;
        public float Width;
        public float RowHeight;
        public float RowGap;
        public float Padding;
        [Tooltip("Space kept to the screen edge and to the HUD blocks in Keep Clear Of.")]
        public float Margin;
        public float MoreHeight;
        public float IconSize;
        public float StripeWidth;
        public float CountWidth;
        public float KeySize;
        public float KeyLabelWidth;
        [Tooltip("Black panel behind the rows (~55–65% alpha).")]
        public Color Background;
        public Color SelectedFill;
        [Tooltip("Alpha of rows that just left the reach (shown briefly, never picked).")]
        [Range(0f, 1f)] public float LingerAlpha;
        public float FadeSeconds;
        public Color KeyFill;
        public Color KeyBorder;
        public Color KeyText;

        public static PickupListLayout Default => new PickupListLayout
        {
            OffsetCells = 1.3f, RaiseCells = .5f, Width = 280f, RowHeight = 30f, RowGap = 2f, Padding = 5f, Margin = 12f,
            MoreHeight = 18f, IconSize = 22f, StripeWidth = 3f, CountWidth = 30f, KeySize = 22f, KeyLabelWidth = 32f,
            Background = new Color(0f, 0f, 0f, .6f), SelectedFill = new Color(1f, 1f, 1f, .12f), LingerAlpha = .45f, FadeSeconds = .12f,
            // Same key cap as the quickbar "R Nạp" hint.
            KeyFill = new Color(.122f, .2f, .153f), KeyBorder = new Color(.204f, .314f, .247f), KeyText = new Color(.953f, .933f, .859f)
        };
    }

    /// <summary>
    /// Fortnite-style pickup list (Docs/hvu/07_Floor_Loot.vi.md section 11): a translucent panel beside the hero listing the
    /// ground items in F reach, nearest first, with the rarity stripe, icon, name and count; the selected row shows "[F] Nhặt".
    /// Draws <see cref="NearbyLootPicker"/> (filled by WorldSceneView online and offline); never picks anything itself.
    /// Rows are built once at start under their own Canvas, so moving with the hero does not rebuild the whole HUD.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
    public sealed class PickupListView : MonoBehaviour
    {
        [SerializeField] private PickupListLayout layout = PickupListLayout.Default;
        [SerializeField] private TMP_FontAsset textFont;
        [SerializeField] private TMP_FontAsset numberFont;
        [Tooltip("Rounded sliced sprite of the panel and the rows.")]
        [SerializeField] private Sprite panelSprite;
        [SerializeField] private Sprite outlineSprite;
        [SerializeField] private Sprite keyCapSprite;
        [Tooltip("Rarity colours; empty loads Resources/GroundLootTuning (the floor glow colours).")]
        [SerializeField] private GroundLootTuning lootTuning;
        [Tooltip("HUD blocks the panel must stay above (bottom HUD).")]
        [SerializeField] private RectTransform[] keepClearOf = new RectTransform[0];

        private sealed class RowView
        {
            public RectTransform Root;
            public CanvasGroup Group;
            public Image Fill, Outline, Stripe, Icon;
            public TMP_Text Name, Count, KeyLabel;
            public RectTransform Key;
            public ushort Id;
            public string ItemId;
            public int Quantity = -1;
            public bool Selected, InReach = true, Shown;
        }

        private readonly char[] _buffer = new char[24];
        private readonly Vector3[] _corners = new Vector3[4];
        private NearbyLootPicker _picker;
        private WorldClientSession _world;
        private IItemIconProvider _icons;
        private HudItemLabels _labels;
        private HudPalette _palette = HudPalette.Default;
        private RectTransform _rect, _canvas;
        private CanvasGroup _group;
        private Image _background;
        private TMP_Text _more;
        private RowView[] _rows;
        private int _moreShown = -1, _rowsShown = -1;
        private bool _built;

        /// <summary>Current panel opacity (0 hidden … 1 shown).</summary>
        public float Alpha => _group != null ? _group.alpha : 0f;
        /// <summary>Rows drawn by the last update (for tests).</summary>
        public int RowsShown => Mathf.Max(0, _rowsShown);
        public RectTransform Panel => _rect;

        [Inject]
        public void Construct(NearbyLootPicker picker, IItemIconProvider icons, IBalanceConfigProvider balance)
        {
            _picker = picker;
            _icons = icons;
            _labels = balance != null ? new HudItemLabels(balance) : null;
        }

        [Inject]
        public void ConstructWorld(WorldClientSession world) => _world=world;

        private void Awake()
        {
            _rect = (RectTransform)transform;
            _group = GetComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            _group.interactable = false;
        }

        private void Start()
        {
            if (_picker == null) { enabled = false; return; } // no Bootstrap (HUD previews/tests): stay hidden
            var root = GetComponentInParent<HudRoot>();
            if (root != null) _palette = root.Palette;
            if (lootTuning == null) lootTuning = Resources.Load<GroundLootTuning>(GroundLootTuning.ResourceName);
            Build();
        }

        private void LateUpdate()
        {
            if (!_built) return;
            var hasRows = _picker.Count > 0;
            var step = layout.FadeSeconds > 0f ? Time.unscaledDeltaTime / layout.FadeSeconds : 1f;
            _group.alpha = Mathf.MoveTowards(_group.alpha, hasRows ? 1f : 0f, step);
            // Fading out keeps the last rows; nothing to draw once fully hidden.
            if (!hasRows) return;
            Draw();
            Place();
        }

        private void Build()
        {
            _canvas = _rect.parent as RectTransform;
            _rect.anchorMin = _rect.anchorMax = Vector2.zero;
            _rect.pivot = new Vector2(0f, .5f);
            if (GetComponent<Canvas>() == null) gameObject.AddComponent<Canvas>(); // own batch: moving each frame stays cheap
            _background = gameObject.GetComponent<Image>();
            if (_background == null) _background = gameObject.AddComponent<Image>();
            Sliced(_background, panelSprite, layout.Background);

            var picker = _picker;
            _rows = new RowView[picker.MaxRows];
            for (var i = 0; i < _rows.Length; i++) _rows[i] = BuildRow(i);
            _more = Text("More", _rect, numberFont, _palette.Muted, TextAlignmentOptions.Center);
            _more.gameObject.SetActive(false);
            _built = true;
        }

        private RowView BuildRow(int index)
        {
            var row = new RowView { Root = Rect("Row" + index, _rect) };
            row.Root.anchorMin = row.Root.anchorMax = row.Root.pivot = new Vector2(0f, 1f);
            row.Root.sizeDelta = new Vector2(layout.Width - layout.Padding * 2f, layout.RowHeight);
            row.Group = row.Root.gameObject.AddComponent<CanvasGroup>();
            row.Group.blocksRaycasts = false;
            row.Fill = Graphic("Fill", row.Root, panelSprite, layout.SelectedFill); Stretch(row.Fill.rectTransform);
            row.Outline = Graphic("Outline", row.Root, outlineSprite, _palette.Amber); Stretch(row.Outline.rectTransform);
            row.Stripe = Graphic("Rarity", row.Root, null, Color.white);
            Place(row.Stripe.rectTransform, 0f, 0f, layout.StripeWidth, layout.RowHeight - 8f);
            row.Icon = Graphic("Icon", row.Root, null, Color.white);
            row.Icon.preserveAspect = true;
            Place(row.Icon.rectTransform, layout.StripeWidth + 6f, 0f, layout.IconSize, layout.IconSize);
            row.Name = Text("Name", row.Root, textFont, _palette.Text, TextAlignmentOptions.MidlineLeft);
            row.Count = Text("Count", row.Root, numberFont, _palette.Muted, TextAlignmentOptions.MidlineRight);
            row.Key = Rect("Key", row.Root);
            var keyCap = Graphic("KeyCap", row.Key, keyCapSprite, layout.KeyFill);
            Place(keyCap.rectTransform, 0f, 0f, layout.KeySize, layout.KeySize);
            var border = Graphic("Border", keyCap.rectTransform, outlineSprite, layout.KeyBorder); Stretch(border.rectTransform);
            var key = Text("Label", keyCap.rectTransform, textFont, layout.KeyText, TextAlignmentOptions.Center);
            Stretch(key.rectTransform);
            key.text = "F";
            row.KeyLabel = Text("Action", row.Key, textFont, _palette.Text, TextAlignmentOptions.MidlineLeft);
            Place(row.KeyLabel.rectTransform, layout.KeySize + 4f, 0f, layout.KeyLabelWidth, layout.RowHeight);
            row.KeyLabel.text = "Nhặt";
            row.Root.gameObject.SetActive(false);
            return row;
        }

        /// <summary>Applies the picker window to the rows; texts and sprites change only when their value does.</summary>
        private void Draw()
        {
            var start = _picker.WindowStart;
            var count = _picker.WindowCount;
            var y = -layout.Padding;
            for (var i = 0; i < _rows.Length; i++)
            {
                var row = _rows[i];
                if (i >= count) { if (row.Shown) { row.Root.gameObject.SetActive(false); row.Shown = false; } continue; }
                if (!row.Shown) { row.Root.gameObject.SetActive(true); row.Shown = true; }
                var index = start + i;
                Apply(row, _picker.Row(index), index == _picker.SelectedIndex);
                row.Root.anchoredPosition = new Vector2(layout.Padding, y);
                y -= layout.RowHeight + layout.RowGap;
            }
            var hidden = _picker.HiddenCount;
            if (hidden != _moreShown)
            {
                _moreShown = hidden;
                _more.gameObject.SetActive(hidden > 0);
                if (hidden > 0)
                {
                    var n = 0; _buffer[n++] = '+';
                    n += Digits(hidden, n);
                    _more.SetCharArray(_buffer, 0, n);
                }
            }
            var height = layout.Padding * 2f + count * layout.RowHeight + Mathf.Max(0, count - 1) * layout.RowGap + (hidden > 0 ? layout.MoreHeight : 0f);
            if (hidden > 0) Place(_more.rectTransform, layout.Padding, -(height - layout.Padding - layout.MoreHeight * .5f), layout.Width - layout.Padding * 2f, layout.MoreHeight, true);
            if (_rowsShown != count || !Mathf.Approximately(_rect.sizeDelta.y, height)) _rect.sizeDelta = new Vector2(layout.Width, height);
            _rowsShown = count;
        }

        private void Apply(RowView row, in NearbyLootRow data, bool selected)
        {
            if (!string.Equals(row.ItemId, data.ItemId, StringComparison.Ordinal) || row.Id != data.Id)
            {
                row.Id = data.Id; row.ItemId = data.ItemId;
                // Vietnamese name (T-022 HudItemNames, GDD ITEM LIST) like the action bar; balance name as fallback.
                row.Name.text = HudItemNames.Get(data.ItemId, _labels != null ? _labels.DisplayName(data.ItemId) : data.ItemId);
                var icon = _icons != null && !string.IsNullOrEmpty(data.ItemId) ? _icons.GetIcon(data.ItemId) : null;
                row.Icon.sprite = icon != null ? icon : panelSprite;
                row.Icon.type = icon != null ? Image.Type.Simple : Image.Type.Sliced;
                row.Icon.color = icon != null ? Color.white : _palette.IconPlaceholder;
                var stripe = lootTuning != null ? lootTuning.StyleOf(data.Rarity).Glow : Color.white;
                stripe.a = 1f;
                row.Stripe.color = stripe;
                row.Quantity = -1; row.Selected = !selected; // force the layout pass below
            }
            var quantity = data.ShowsCount ? data.Count : 0;
            if (row.Quantity != quantity)
            {
                row.Quantity = quantity;
                row.Count.gameObject.SetActive(quantity > 0);
                if (quantity > 0) row.Count.SetCharArray(_buffer, 0, HudFeedbackFormat.Count(_buffer, quantity));
                row.Selected = !selected;
            }
            var pending=_world != null && _world.IsPickupPending(data.Id);
            row.KeyLabel.text=pending?"…":"Nhặt";
            if (row.Selected != selected)
            {
                row.Selected = selected;
                row.Fill.enabled = selected;
                row.Outline.enabled = selected;
                row.Key.gameObject.SetActive(selected);
                // Name takes what the count and the key hint leave free.
                var right = layout.Width - layout.Padding * 2f - 6f;
                var keyWidth = layout.KeySize + 4f + layout.KeyLabelWidth;
                if (selected) { Place(row.Key, right - keyWidth, 0f, keyWidth, layout.RowHeight); right -= keyWidth + 6f; }
                if (quantity > 0) { Place(row.Count.rectTransform, right - layout.CountWidth, 0f, layout.CountWidth, layout.RowHeight); right -= layout.CountWidth + 4f; }
                var left = layout.StripeWidth + 6f + layout.IconSize + 6f;
                Place(row.Name.rectTransform, left, 0f, Mathf.Max(0f, right - left), layout.RowHeight);
            }
            if (row.InReach != data.InReach)
            {
                row.InReach = data.InReach;
                row.Group.alpha = data.InReach ? 1f : layout.LingerAlpha;
            }
        }

        /// <summary>Beside the hero (<see cref="PickupListLayout.OffsetCells"/> right), clamped to the screen and above the bottom HUD.</summary>
        private void Place()
        {
            var camera = Camera.main;
            if (camera == null || _canvas == null) return;
            var screen = camera.WorldToScreenPoint(new Vector3(_picker.HeroX + layout.OffsetCells, _picker.HeroY + layout.RaiseCells, 0f));
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvas, screen, null, out var local)) return;
            var bounds = _canvas.rect;
            var size = _rect.sizeDelta;
            var x = Mathf.Clamp(local.x - bounds.xMin, layout.Margin, Mathf.Max(layout.Margin, bounds.width - layout.Margin - size.x));
            var floor = layout.Margin;
            foreach (var block in keepClearOf)
            {
                if (block == null || !block.gameObject.activeInHierarchy) continue;
                block.GetWorldCorners(_corners);
                var min = _canvas.InverseTransformPoint(_corners[0]); var max = _canvas.InverseTransformPoint(_corners[2]);
                var left = min.x - bounds.xMin; var right = max.x - bounds.xMin;
                if (right <= x || left >= x + size.x) continue; // not under the panel
                floor = Mathf.Max(floor, max.y - bounds.yMin + layout.Margin);
            }
            var y = local.y - bounds.yMin;
            var top = bounds.height - layout.Margin;
            y = Mathf.Clamp(y, floor + size.y * .5f, Mathf.Max(floor + size.y * .5f, top - size.y * .5f));
            _rect.anchoredPosition = new Vector2(x, y);
        }

        private int Digits(int value, int at)
        {
            var digits = value >= 100 ? 3 : value >= 10 ? 2 : 1;
            for (var i = digits - 1; i >= 0; i--) { _buffer[at + i] = (char)('0' + value % 10); value /= 10; }
            return digits;
        }

        private static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private static Image Graphic(string name, Transform parent, Sprite sprite, Color color)
        {
            var image = Rect(name, parent).gameObject.AddComponent<Image>();
            Sliced(image, sprite, color);
            return image;
        }

        private static void Sliced(Image image, Sprite sprite, Color color)
        {
            image.sprite = sprite;
            image.type = sprite != null && sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            image.color = color;
            image.raycastTarget = false;
        }

        private TMP_Text Text(string name, Transform parent, TMP_FontAsset font, Color color, TextAlignmentOptions alignment)
        {
            var text = Rect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) text.font = font;
            text.fontSize = 13f; // HUD rule 5: never below 13 px
            text.color = color;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            return text;
        }

        /// <summary>Top-left anchored rect, vertically centred on the row when <paramref name="top"/> is false.</summary>
        private void Place(RectTransform rect, float x, float y, float width, float height, bool top = false)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, .5f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(x, top ? y : y - layout.RowHeight * .5f);
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
