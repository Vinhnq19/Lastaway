using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JungleMaze.Presentation.UI.Lobby
{
    /// <summary>Blocking error with a reason and 'Về menu' (load error, lost connection: AC25, AC39, AC40). References only.</summary>
    public sealed class NoticeView : LobbyPanelView
    {
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text message;
        [SerializeField] private Button home;
        [Tooltip("Skin A: cross for a load error (A7), disconnect icon for a lost match (B8).")]
        [SerializeField] private GameObject loadErrorIcon;
        [SerializeField] private GameObject disconnectIcon;

        public TMP_Text Title => title;
        public TMP_Text Message => message;
        public Button Home => home;

        public void ShowKind(bool loadError)
        {
            if (loadErrorIcon != null) loadErrorIcon.SetActive(loadError);
            if (disconnectIcon != null) disconnectIcon.SetActive(!loadError);
        }
    }
}
