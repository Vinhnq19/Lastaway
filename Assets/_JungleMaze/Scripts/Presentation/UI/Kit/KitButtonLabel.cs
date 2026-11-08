using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JungleMaze.Presentation.UI.Kit
{
    /// <summary>K2 label colour: ink on a live button, light grey on the flat disabled face (ui-kit.md mục 9).</summary>
    [RequireComponent(typeof(TMP_Text))]
    public sealed class KitButtonLabel : MonoBehaviour
    {
        [SerializeField] private Selectable button;
        [SerializeField] private Color normal = Color.black;
        [SerializeField] private Color disabled = Color.grey;
        private TMP_Text _text;
        private int _state = -1;

        public void Bind(Selectable target, Color normalColor, Color disabledColor)
        {
            button = target;
            normal = normalColor;
            disabled = disabledColor;
            Apply();
        }

        /// <summary>Lets a builder change the live colour (Exit label, custom kinds) without losing the disabled swap.</summary>
        public void SetNormal(Color color) { normal = color; _state = -1; Apply(); }

        private void OnEnable() { _state = -1; Apply(); }
        private void LateUpdate() => Apply();

        private void Apply()
        {
            if (_text == null) _text = GetComponent<TMP_Text>();
            var on = button == null || button.IsInteractable() ? 1 : 0;
            if (on == _state) return;
            _state = on;
            _text.color = on == 1 ? normal : disabled;
        }
    }
}
