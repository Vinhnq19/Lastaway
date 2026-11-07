using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JungleMaze.Presentation.UI.MatchOverlay
{
    /// <summary>"Thoát trận? Bạn sẽ bị loại khỏi trận" with Thoát / Ở lại (Q7, AC15). References only.</summary>
    public sealed class LeaveConfirmView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private TMP_Text message;
        [SerializeField] private Button confirm;
        [SerializeField] private Button stay;
        [Tooltip("X at the popup's top right (skin A): same as Ở lại.")]
        [SerializeField] private Button close;

        public CanvasGroup Group => group;
        public TMP_Text Message => message;
        public Button Confirm => confirm;
        public Button Stay => stay;
        public Button Close => close;

        private void Awake()
        {
            if (close != null && stay != null) close.onClick.AddListener(() => { if (stay.interactable) stay.onClick.Invoke(); });
        }
    }
}
