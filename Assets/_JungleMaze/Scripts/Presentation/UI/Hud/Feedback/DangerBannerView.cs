using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JungleMaze.Presentation.UI.Hud.Feedback
{
    /// <summary>Single source-selected warning banner.</summary>
    public sealed class DangerBannerView:MonoBehaviour,IDangerBannerView
    {
        [SerializeField] private CanvasGroup group;[SerializeField] private TMP_Text label,timer,suffix;[SerializeField] private Image background,border;
        // Text area inside the banner (canvas units, from its top-left); the builder sets the skin A box (T-022).
        [SerializeField] private float textLeft=6,textTop=8,textWidth=289,textHeight=24;
        public CanvasGroup Group=>group;public TMP_Text Label=>label;public TMP_Text Timer=>timer;public TMP_Text Suffix=>suffix;
        // Skin A: kit_toast drawn untinted (neutral toast rule); the red stays on the seconds only.
        public void ApplyPalette(HudPalette p){label.color=p.Text;timer.color=p.Red;suffix.color=p.Muted;background.color=Color.white;border.color=p.Red;}
        public void Show(bool visible,char[] text,int length,float alpha)
        {
            group.alpha=visible?alpha:0;if(!visible)return;
            var skip=length>=2&&text[0]=='!'&&text[1]==' '?2:0; // the warn icon stands in for the leading "! "
            int start=-1,end=-1;
            for(var i=skip;i<length;i++)if(text[i]>='0'&&text[i]<='9'){var j=i;while(j<length&&text[j]>='0'&&text[j]<='9')j++;if(j<length&&text[j]=='s'){start=i;end=j+1;break;}}
            timer.enabled=start>=0;suffix.enabled=start>=0;
            label.rectTransform.anchoredPosition=new Vector2(textLeft,-textTop);
            if(start<0){label.rectTransform.sizeDelta=new Vector2(textWidth,textHeight);label.SetCharArray(text,skip,length-skip);}
            else{
                label.SetCharArray(text,skip,start-skip);timer.SetCharArray(text,start,end-start);suffix.SetCharArray(text,end,length-end);
                var prefixWidth=Mathf.Ceil(label.preferredWidth);var timerWidth=Mathf.Ceil(timer.preferredWidth);
                label.rectTransform.sizeDelta=new Vector2(prefixWidth,textHeight);timer.rectTransform.anchoredPosition=new Vector2(textLeft+prefixWidth,-textTop);timer.rectTransform.sizeDelta=new Vector2(timerWidth,textHeight);
                suffix.rectTransform.anchoredPosition=new Vector2(textLeft+prefixWidth+timerWidth,-textTop);suffix.rectTransform.sizeDelta=new Vector2(Mathf.Max(1,textWidth-prefixWidth-timerWidth),textHeight);
            }
        }
    }
}
