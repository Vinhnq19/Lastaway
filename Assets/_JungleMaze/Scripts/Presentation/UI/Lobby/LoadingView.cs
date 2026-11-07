using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JungleMaze.Presentation.UI.Lobby
{
    /// <summary>Loading (Figma v2 42:269): steps, server time left, controls hint and a tip (AC24). References only.</summary>
    public sealed class LoadingView : LobbyPanelView
    {
        [SerializeField] private TMP_Text stepMap;
        [SerializeField] private TMP_Text stepVersion;
        [SerializeField] private TMP_Text stepPlayers;
        [SerializeField] private TMP_Text timeLeft;
        [SerializeField] private TMP_Text hint;
        [SerializeField] private TMP_Text tip;
        [SerializeField] private Button home;
        [Tooltip("Skin A progress fill (kit_progress_fill), stretched by SetCountdown.")]
        [SerializeField] private RectTransform progressFill;
        private int _max;

        public TMP_Text StepMap => stepMap;
        public TMP_Text StepVersion => stepVersion;
        public TMP_Text StepPlayers => stepPlayers;
        public TMP_Text TimeLeft => timeLeft;
        public TMP_Text Hint => hint;
        public TMP_Text Tip => tip;
        public Button Home => home;

        /// <summary>No load percentage exists, so the bar shows the share of the server's loading time already used.</summary>
        public void SetCountdown(int seconds)
        {
            if (seconds < 0 || seconds > _max) _max = Mathf.Max(seconds, 0);
            SetFill(progressFill, _max > 0 ? 1f - (float)seconds / _max : 0f);
        }
    }
}
