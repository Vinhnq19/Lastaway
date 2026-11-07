using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JungleMaze.Presentation.UI.Lobby
{
    /// <summary>Results (Figma v2 43:445): headline, own block, ranked table, replay/home, optional online board (AC28–AC33). References only.</summary>
    public sealed class ResultsView : LobbyPanelView
    {
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text rank;
        [SerializeField] private TMP_Text kills;
        [SerializeField] private TMP_Text survival;
        [SerializeField] private TMP_Text cause;
        [SerializeField] private TMP_Text onlineLine;
        [SerializeField] private Button replay;
        [SerializeField] private TMP_Text replayLabel;
        [SerializeField] private Button home;
        [SerializeField] private Button online;
        [SerializeField] private ResultRowView[] rows;
        [Header("Skin A decoration (B6, B7)")]
        [SerializeField] private GameObject winEmblem;
        [SerializeField] private GameObject loseEmblem;
        [SerializeField] private GameObject causeCard;
        [SerializeField] private GameObject killsCard;
        [SerializeField] private GameObject survivalCard;
        [SerializeField] private GameObject statsRow;
        [SerializeField] private Color victoryTitle = new Color32(0xFF, 0xDC, 0x6E, 0xFF);
        [SerializeField] private Color otherTitle = new Color32(0xED, 0xF2, 0xEE, 0xFF);

        public TMP_Text Title => title;
        public TMP_Text Rank => rank;
        public TMP_Text Kills => kills;
        public TMP_Text Survival => survival;
        public TMP_Text Cause => cause;
        public TMP_Text OnlineLine => onlineLine;
        public Button Replay => replay;
        public TMP_Text ReplayLabel => replayLabel;
        public Button Home => home;
        public Button Online => online;
        public ResultRowView[] Rows => rows;

        /// <summary>Emblem and title colour by headline; each card follows the text the presenter shows or hides.</summary>
        public void ShowDecor(bool victory)
        {
            if (winEmblem != null) winEmblem.SetActive(victory);
            if (loseEmblem != null) loseEmblem.SetActive(!victory);
            title.color = victory ? victoryTitle : otherTitle;
            Follow(causeCard, cause);
            Follow(killsCard, kills);
            Follow(survivalCard, survival);
            if (statsRow != null) statsRow.SetActive(kills.gameObject.activeSelf || survival.gameObject.activeSelf);
        }

        private static void Follow(GameObject card, TMP_Text text)
        {
            if (card != null) card.SetActive(text.gameObject.activeSelf);
        }
    }
}
