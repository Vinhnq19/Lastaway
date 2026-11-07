using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace JungleMaze.Presentation.UI.Kit
{
    /// <summary>
    /// K2 raised button (T-026, ui-kit.md mục 11): the icon + label group is centred on the button FACE, not on the
    /// whole sprite. The face sits above a base that is 11 % of the height (normal, hover), 4 % while pressed (the face
    /// sinks) and 0 when disabled. This moves <see cref="content"/>'s bottom padding to follow the sprite state.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class KitButtonFace : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public const float BaseRatio = 0.11f, PressedBaseRatio = 0.04f;

        [SerializeField] private Selectable button;
        [SerializeField] private RectTransform content;
        private bool _down;
        private float _applied = -1f;

        public RectTransform Content => content;

        public void Bind(Selectable target, RectTransform group)
        {
            button = target;
            content = group;
            Apply();
        }

        public void OnPointerDown(PointerEventData eventData) { _down = true; Apply(); }
        public void OnPointerUp(PointerEventData eventData) { _down = false; Apply(); }
        public void OnPointerExit(PointerEventData eventData) { if (_down) { _down = false; Apply(); } }

        private void OnEnable() { _down = false; Apply(); }

        private void LateUpdate() => Apply(); // interactable can flip from any presenter; cheap compare, no layout when unchanged

        /// <summary>Bottom padding in the parent's units for the current state.</summary>
        public float BottomPadding(float height)
        {
            if (button != null && !button.IsInteractable()) return 0f;
            return Mathf.Round(height * (_down ? PressedBaseRatio : BaseRatio) * 100f) / 100f;
        }

        private void Apply()
        {
            if (content == null) return;
            var height = ((RectTransform)transform).rect.height;
            var pad = BottomPadding(height);
            if (Mathf.Approximately(pad, _applied)) return;
            _applied = pad;
            content.offsetMin = new Vector2(content.offsetMin.x, pad);
        }
    }
}
