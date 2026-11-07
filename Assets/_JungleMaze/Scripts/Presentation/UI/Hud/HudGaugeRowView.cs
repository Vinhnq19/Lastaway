using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JungleMaze.Presentation.UI.Hud
{
    /// <summary>
    /// One Vitals row (armor, helmet or adrenaline): label, bar and a number. HUD B (T-013) hides the label and the
    /// armor/helmet bars and shows a baked-colour icon instead. T-019 (Huy 07/11 "Theo ref"): the icon keeps full
    /// brightness and an empty row reads "0", as in the approved mock.
    /// </summary>
    public sealed class HudGaugeRowView : MonoBehaviour
    {
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
