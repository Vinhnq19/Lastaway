using UnityEngine;
using UnityEngine.UI;

namespace JungleMaze.Presentation.UI.Lobby
{
    /// <summary>
    /// Popup close X of skin A (kit_close): presses the panel's own cancel button when that one could be pressed, so
    /// each popup keeps a single cancel action in the flow.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class ForwardClick : MonoBehaviour
    {
        [SerializeField] private Button target;

        public Button Target => target;

        private void Awake() => GetComponent<Button>().onClick.AddListener(Press);

        private void Press()
        {
            if (target != null && target.IsActive() && target.IsInteractable()) target.onClick.Invoke();
        }
    }
}
