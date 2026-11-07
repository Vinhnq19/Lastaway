using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JungleMaze.Presentation.UI.Lobby
{
    /// <summary>Reconnect overlay (Figma v2 43:131): message, grace seconds left (about), home (AC38). References only.</summary>
    public sealed class ReconnectView : LobbyPanelView
    {
        [SerializeField] private TMP_Text message;
        [SerializeField] private TMP_Text seconds;
        [SerializeField] private Button home;
        [Tooltip("Skin A progress fill (kit_progress_fill), stretched by SetCountdown.")]
        [SerializeField] private RectTransform progressFill;
        private int _max;

        public TMP_Text Message => message;
        public TMP_Text Seconds => seconds;
        public Button Home => home;

        /// <summary>The bar shows the grace left (B8: full at 15 s, empty when it runs out).</summary>
        public void SetCountdown(int seconds)
        {
            if (seconds < 0 || seconds > _max) _max = Mathf.Max(seconds, 0);
            SetFill(progressFill, _max > 0 ? (float)seconds / _max : 0f);
        }
    }
}
