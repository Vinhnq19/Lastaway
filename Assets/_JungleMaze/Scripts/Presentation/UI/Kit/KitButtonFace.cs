using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace JungleMaze.Presentation.UI.Kit
{
    /// <summary>
    /// K2 raised button (T-026, ui-kit.md mục 9, 11): the face sits on a base about 11 % of the button height
    /// (7 px on a 64 px button at 1080p). While pressed the pressed sprite drops the face onto the base, so the whole
    /// <see cref="content"/> (icon and label) moves down by that base; released, it comes back; disabled (flat face,
    /// no base) it never moves (Huy 07/11: "đế nhấn nhưng icon không dịch xuống").
    /// <see cref="padNormal"/>: the content is also centred on the face at rest (bottom padding = base), as
    /// <c>UiKit.Button</c> builds it; off for buttons whose icon was already placed on the face by hand.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class KitButtonFace : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public const float BaseRatio = 0.11f;

        [SerializeField] private Selectable button;
        [SerializeField] private RectTransform content;
        [SerializeField] private bool padNormal = true;
        private bool _down;
        private int _applied = -1;

        public RectTransform Content => content;
        public bool Pressed => _down;

        public void Bind(Selectable target, RectTransform group, bool centreOnFace = true)
        {
            button = target;
            content = group;
            padNormal = centreOnFace;
            _applied = -1;
            Apply();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData != null && eventData.button != PointerEventData.InputButton.Left) return;
            _down = true;
            Apply();
        }

        public void OnPointerUp(PointerEventData eventData) { _down = false; Apply(); }
        public void OnPointerExit(PointerEventData eventData) { if (_down) { _down = false; Apply(); } }

        private void OnEnable() { _down = false; _applied = -1; Apply(); }

        private void LateUpdate() => Apply(); // interactable can flip from any presenter; cheap compare, no layout when unchanged

        /// <summary>Base height in the button's own units (11 % of its height, to 0.01).</summary>
        public float Base => Mathf.Round(((RectTransform)transform).rect.height * BaseRatio * 100f) / 100f;

        /// <summary>Vertical offset of the content centre from its rest position: 0 at rest, -Base while pressed, 0 disabled.</summary>
        public float PressOffset => State == 1 ? -Base : 0f;

        private int State => button != null && !button.IsInteractable() ? 2 : _down ? 1 : 0;

        private void Apply()
        {
            if (content == null) return;
            var state = State;
            if (state == _applied) return;
            _applied = state;
            var b = Base;
            var restMin = padNormal ? b : 0f;
            float min, max;
            if (state == 2) { min = 0f; max = 0f; }           // flat disabled face
            else if (state == 1) { min = restMin - b; max = -b; } // whole content down by the base
            else { min = restMin; max = 0f; }
            content.offsetMin = new Vector2(content.offsetMin.x, min);
            content.offsetMax = new Vector2(content.offsetMax.x, max);
        }
    }
}
