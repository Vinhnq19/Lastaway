using JungleMaze.App.MatchUi;
using JungleMaze.Domain.MatchUi;
using JungleMaze.Presentation.UI.Hud;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JungleMaze.Presentation.UI.MatchOverlay
{
    /// <summary>
    /// Eliminated screen (T-021 b2-bi-loai, skin A): panel M, rank, cause card, stat cards, "Về sảnh" and "Theo dõi".
    /// A line whose data is missing is switched off with its card rather than printed as 0/null (AC20); text comes
    /// from <see cref="EliminationText"/>.
    /// </summary>
    public sealed class EliminatedView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private RectTransform frame;
        [SerializeField] private TMP_Text title;
        [Tooltip("'#4' (96) of the rank line; the '/ 10' part goes to rankTotal (40, muted), as in b2-bi-loai.")]
        [SerializeField] private TMP_Text rank;
        [SerializeField] private TMP_Text rankTotal;
        [SerializeField] private TMP_Text cause;
        [SerializeField] private TMP_Text kills;
        [SerializeField] private TMP_Text survival;
        [Tooltip("Stat card around the kills line; hidden with it, the other card recentres (layout group).")]
        [SerializeField] private GameObject killsCard;
        [SerializeField] private GameObject survivalCard;
        [Tooltip("Why spectating is not possible (AC21).")]
        [SerializeField] private TMP_Text blockedReason;
        [SerializeField] private Button home;
        [SerializeField] private Button spectate;
        [Tooltip("Icon in the cause card's left slot, on the kaki plate (ink icons) or alone and larger (colour icons).")]
        [SerializeField] private Image causeIcon;
        [Tooltip("Sprite per EliminationCause: Unknown, Player, Zone, LeftMatch, Disconnected.")]
        [SerializeField] private Sprite[] causeIcons = new Sprite[5];
        [SerializeField] private Image causePlate;
        [Tooltip("Per cause: the icon sits on the plate; false = no plate, icon scaled 1.5 to the plate's size.")]
        [SerializeField] private bool[] causeOnPlate = new bool[0];
        [Tooltip("Weapon of the killer, beside the cause text; hidden when the weapon has no icon.")]
        [SerializeField] private Image causeWeaponIcon;
        [SerializeField] private HudItemIconCatalog weaponIcons;

        public CanvasGroup Group => group;
        public RectTransform Frame => frame;
        public TMP_Text Rank => rank;
        public TMP_Text RankTotal => rankTotal;
        public Image CauseWeaponIcon => causeWeaponIcon;
        public TMP_Text Cause => cause;
        public TMP_Text Kills => kills;
        public TMP_Text Survival => survival;
        public TMP_Text BlockedReason => blockedReason;
        public Button Home => home;
        public Button Spectate => spectate;
        public Image CauseIcon => causeIcon;

        public void Show(in EliminationInfo info)
        {
            if (title != null) title.text = EliminationText.Title;
            // Display split only: '#4 / 10' -> '#4' + '/ 10'; 'Hạ gục: 2' -> '2' (the icon says what it is, mock b2).
            var rankLine = EliminationText.Rank(info);
            var cut = rankLine != null ? rankLine.IndexOf(" / ", System.StringComparison.Ordinal) : -1;
            Line(rank, cut < 0 ? rankLine : rankLine.Substring(0, cut));
            Line(rankTotal, cut < 0 ? null : rankLine.Substring(cut + 1));
            if (rankTotal != null) LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)rank.transform.parent);
            Line(cause, EliminationText.Cause(info));
            PlaceCauseIcon(info.Cause);
            var weapon = info.Cause == EliminationCause.Player ? WeaponIcons.Find(weaponIcons, info.WeaponName) : null;
            if (causeWeaponIcon != null)
            {
                causeWeaponIcon.sprite = weapon;
                causeWeaponIcon.gameObject.SetActive(weapon != null);
            }
            Line(kills, ValueOf(EliminationText.Kills(info)), killsCard);
            Line(survival, ValueOf(EliminationText.Survival(info)), survivalCard);
            if (killsCard != null) LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)killsCard.transform.parent);
        }

        public void SetSpectateAvailable(bool available, string reason)
        {
            spectate.interactable = available;
            Line(blockedReason, available ? null : reason);
        }

        // Once per elimination: picks the cause sprite; the layout group places icon + text, rebuilt now so it is
        // right on the first frame.
        private void PlaceCauseIcon(EliminationCause value)
        {
            if (causeIcon == null || cause == null || causeIcons == null || causeIcons.Length == 0) return;
            var index = (int)value;
            causeIcon.sprite = index < causeIcons.Length && causeIcons[index] != null ? causeIcons[index] : causeIcons[0];
            causeIcon.gameObject.SetActive(cause.gameObject.activeSelf);
            var plated = causeOnPlate == null || index >= causeOnPlate.Length || causeOnPlate[index];
            if (causePlate != null) causePlate.enabled = plated;
            causeIcon.rectTransform.localScale = plated ? Vector3.one : Vector3.one * 1.5f; // 40 on the 60 plate, or 60 alone
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)cause.transform.parent);
        }

        /// <summary>"Nhãn: giá trị" -> "giá trị"; any other string unchanged; null stays null.</summary>
        public static string ValueOf(string line)
        {
            var i = line != null ? line.IndexOf(": ", System.StringComparison.Ordinal) : -1;
            return i < 0 ? line : line.Substring(i + 2);
        }

        private static void Line(TMP_Text text, string value, GameObject card = null)
        {
            if (text == null) return;
            var show = value != null;
            if (show) text.text = value;
            if (text.gameObject.activeSelf != show) text.gameObject.SetActive(show);
            if (card != null && card.activeSelf != show) card.SetActive(show);
        }
    }
}
