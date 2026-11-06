using JungleMaze.App.Player;
using JungleMaze.Presentation.UI.Lobby;
using JungleMaze.Presentation.World.Phase2;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JungleMaze.Presentation.Player
{
    /// <summary>
    /// Home's hero skin picker: an animated preview, the skin name and ◀ ▶ buttons, left of the menu column. Built at
    /// runtime from the menu's own button and status text so it matches their look without rebuilding the lobby
    /// prefab (same approach as <see cref="RoomMapPanel"/>). The choice is saved on this device only.
    /// </summary>
    public sealed class HeroSkinPicker : MonoBehaviour
    {
        /// <summary>Figma-space layout (1280×720, y down from the top), left of the 300 px column at x = 490.</summary>
        private const float X = 200f, Y = 208f, Width = 240f, PreviewSize = 150f, ButtonWidth = 110f;

        private IHeroSkinPreference _skins;
        private HeroVisualSet _set;
        private Image _preview;
        private TMP_Text _label;
        private Button _previous, _next;
        private WorldVisualSet.Frames _frames;

        public string Label => _label != null ? _label.text : "";
        public Sprite Preview => _preview != null ? _preview.sprite : null;

        /// <summary>Adds the picker under the Home menu; does nothing when the menu, the choice store or the art is missing.</summary>
        public static HeroSkinPicker Attach(MainMenuView menu, IHeroSkinPreference skins, HeroVisualSet set)
        {
            if (menu == null || skins == null || set == null || set.Heroes == null || set.Heroes.Length == 0) return null;
            var template = menu.OfflinePractice;
            if (template == null || !(template.transform.parent is RectTransform parent)) return null;

            var root = Rect("HeroSkinPicker", parent, X, Y, Width, 300f);
            var picker = root.gameObject.AddComponent<HeroSkinPicker>();
            picker._skins = skins;
            picker._set = set;

            var title = Text(menu.ApiStatus, root, "Title", 0f, 0f, Width, 28f);
            if (title != null) title.text = "Nhân vật";

            var frame = Rect("Preview", root, (Width - PreviewSize) * .5f, 32f, PreviewSize, PreviewSize);
            picker._preview = frame.gameObject.AddComponent<Image>();
            picker._preview.preserveAspect = true;
            picker._preview.raycastTarget = false;

            picker._label = Text(menu.ApiStatus, root, "Name", 0f, 190f, Width, 28f);
            picker._previous = Arrow(template, root, "Previous", "<", 0f, 230f);
            picker._next = Arrow(template, root, "Next", ">", Width - ButtonWidth, 230f);
            picker._previous.onClick.AddListener(() => picker.Step(-1));
            picker._next.onClick.AddListener(() => picker.Step(1));
            skins.Changed += picker.Refresh;
            picker.Refresh();
            return picker;
        }

        /// <summary>Moves the choice one step (wrapping through "automatic").</summary>
        public void Step(int direction) => _skins.Select(HeroSkinRules.Step(_skins.Selected, _set.Heroes.Length, direction));

        private void Refresh()
        {
            var selected = _skins.Selected;
            var count = _set.Heroes.Length;
            var hero = _set.At(selected >= 0 && selected < count ? selected : 0);
            _frames = hero?.IdleFront;
            if (_preview != null) _preview.sprite = _frames?.Sample(0f);
            if (_label != null)
                _label.text = selected >= 0 && selected < count ? $"Skin {selected + 1}/{count}" : "Tự động (theo phòng)";
            if (_preview != null) _preview.color = selected >= 0 && selected < count ? Color.white : new Color(1f, 1f, 1f, .55f);
        }

        private void Update()
        {
            if (_preview == null || _frames == null) return;
            var frame = _frames.Sample(Time.unscaledTime);
            if (frame != null && frame != _preview.sprite) _preview.sprite = frame;
        }

        private void OnDestroy()
        {
            if (_skins != null) _skins.Changed -= Refresh;
        }

        private static RectTransform Rect(string name, RectTransform parent, float x, float y, float w, float h)
        {
            var r = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            r.SetParent(parent, false);
            r.anchorMin = r.anchorMax = new Vector2(0f, 1f);
            r.pivot = new Vector2(0f, 1f);
            r.anchoredPosition = new Vector2(x, -y);
            r.sizeDelta = new Vector2(w, h);
            return r;
        }

        /// <summary>Copies the menu's status text so the font, size and colour match the screen.</summary>
        private static TMP_Text Text(TMP_Text template, RectTransform parent, string name, float x, float y, float w, float h)
        {
            if (template == null) return null;
            var text = Instantiate(template, parent);
            text.name = name;
            var r = text.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(0f, 1f);
            r.pivot = new Vector2(0f, 1f);
            r.anchoredPosition = new Vector2(x, -y);
            r.sizeDelta = new Vector2(w, h);
            text.alignment = TextAlignmentOptions.Center;
            text.gameObject.SetActive(true);
            return text;
        }

        /// <summary>Copies a menu button (look and hover states, no click listeners) and relabels it.</summary>
        private static Button Arrow(Button template, RectTransform parent, string name, string label, float x, float y)
        {
            var button = Instantiate(template, parent);
            button.name = name;
            button.onClick.RemoveAllListeners();
            var r = (RectTransform)button.transform;
            r.anchorMin = r.anchorMax = new Vector2(0f, 1f);
            r.pivot = new Vector2(0f, 1f);
            r.anchoredPosition = new Vector2(x, -y);
            r.sizeDelta = new Vector2(ButtonWidth, Mathf.Max(44f, r.sizeDelta.y));
            var text = button.GetComponentInChildren<TMP_Text>(true);
            if (text != null) text.text = label;
            button.gameObject.SetActive(true);
            return button;
        }
    }
}
