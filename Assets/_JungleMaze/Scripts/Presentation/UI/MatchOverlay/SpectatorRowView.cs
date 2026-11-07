using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JungleMaze.Presentation.UI.MatchOverlay
{
    /// <summary>One row of the spectator list (SpectatorRow.prefab, kit row 300×48 px, G-12). Colors are prefab data (AC47).</summary>
    public sealed class SpectatorRowView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image background;
        [SerializeField] private TMP_Text label;
        [Tooltip("Outline shown on the selected row only.")]
        [SerializeField] private Image selectedOutline;
        [Tooltip("Tint of the kit row sprite; white keeps the art as drawn (skin A).")]
        [SerializeField] private Color normal = Color.white;
        [SerializeField] private Color selected = Color.white;
        [Tooltip("Name color: text on a normal row, gold on the selected one (b3-spectator).")]
        [SerializeField] private Color labelNormal = new Color(0.929f, 0.949f, 0.933f);
        [SerializeField] private Color labelSelected = new Color(1f, 0.863f, 0.431f);

        private byte _netId = 255;
        private string _name;

        public Button Button => button;
        public TMP_Text Label => label;
        public byte NetId => _netId;
        public bool Selected { get; private set; }

        public void Set(byte netId, string name, bool isSelected)
        {
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            if (_netId != netId || !ReferenceEquals(_name, name))
            {
                _netId = netId;
                _name = name;
                label.text = name ?? "Người chơi " + (netId + 1);
            }
            Selected = isSelected;
            background.color = isSelected ? selected : normal;
            label.color = isSelected ? labelSelected : labelNormal;
            if (selectedOutline != null && selectedOutline.enabled != isSelected) selectedOutline.enabled = isSelected;
        }

        public void Hide()
        {
            _netId = 255;
            Selected = false;
            if (gameObject.activeSelf) gameObject.SetActive(false);
        }
    }
}
