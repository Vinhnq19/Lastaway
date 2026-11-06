using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JungleMaze.Presentation.UI.Hud
{
    /// <summary>
    /// One Vitals row (armor, helmet or adrenaline): label, bar and a number. HUD B (T-013) hides the label and the
    /// armor/helmet bars and shows a baked-colour icon instead, dimmed while the row has no number.
    /// </summary>
    public sealed class HudGaugeRowView : MonoBehaviour
    {
        private const float DimIconAlpha = 0.35f;

        [SerializeField] private TMP_Text label;
        [SerializeField] private HudBarView bar;
        [SerializeField] private TMP_Text value;
        [SerializeField] private Graphic icon;

        public TMP_Text Label => label;
        public TMP_Text Value => value;
        public HudBarView Bar => bar;
        public Graphic Icon => icon;

        public void SetLabel(string text, Color color)
        {
            if (label == null) return;
            if (!string.Equals(label.text, text)) label.text = text;
            label.color = color;
        }

        /// <summary>A length of 0 hides the number.</summary>
        public void SetValue(char[] text, int length, Color color)
        {
            if (value == null) return;
            var visible = length > 0;
            if (icon != null) icon.color = new Color(1f, 1f, 1f, visible ? 1f : DimIconAlpha);
            if (value.enabled != visible) value.enabled = visible;
            if (!visible) return;
            value.SetCharArray(text, 0, length);
            value.color = color;
        }

        public void SetFill(float fill)
        {
            if (bar != null) bar.SetFill(fill);
        }

        public void SetBarColors(Color fillColor, Color trackColor, Color trailColor)
        {
            if (bar != null) bar.SetColors(fillColor, trackColor, trailColor);
        }
    }
}
