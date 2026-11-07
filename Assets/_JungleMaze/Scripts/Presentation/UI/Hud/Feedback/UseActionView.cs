using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JungleMaze.Presentation.UI.Hud.Feedback
{
    /// <summary>Only renders the use action; all clocks live in the model.</summary>
    public sealed class UseActionView:MonoBehaviour,IUseActionView,IUseActionIconView
    {
        [SerializeField] private CanvasGroup group;[SerializeField] private TMP_Text label,seconds,hint;[SerializeField] private RectTransform fill;[SerializeField] private Image background;[SerializeField] private Image icon;
        private HudPalette _palette;private IItemIconProvider _icons;
        public CanvasGroup Group=>group;public TMP_Text Label=>label;public TMP_Text Seconds=>seconds;public TMP_Text Hint=>hint;public RectTransform Fill=>fill;public Image Icon=>icon;
        // Skin A kit (T-022): toast and progress sprites carry their colours; the seconds read in Gold as in mock B9.
        public void ApplyPalette(HudPalette palette){_palette=palette;if(background!=null)background.color=Color.white;fill.GetComponent<Image>().color=Color.white;seconds.color=palette.Gold.a>0?palette.Gold:palette.Amber;hint.color=palette.Muted;}
        /// <summary>ID→sprite provider for the item icon (null hides the icon).</summary>
        public void ConfigureIcons(IItemIconProvider icons){_icons=icons;}
        public void SetItem(string itemId)
        {
            if(icon==null)return;var sprite=_icons!=null&&!string.IsNullOrEmpty(itemId)?_icons.GetIcon(itemId):null;
            if(icon.sprite!=sprite)icon.sprite=sprite;icon.enabled=sprite!=null;
        }
        public void Show(bool visible,char[] text,int length,char[] time,int timeLength,float ratio,bool showHint,bool cancelled)
        {
            group.alpha=visible?1:0;if(!visible)return;label.SetCharArray(text,0,length);label.color=cancelled?_palette.Faint:_palette.Text;
            seconds.SetCharArray(time,0,timeLength);seconds.enabled=!cancelled;hint.enabled=showHint;fill.anchorMax=new Vector2(ratio,1);
        }
    }
}
