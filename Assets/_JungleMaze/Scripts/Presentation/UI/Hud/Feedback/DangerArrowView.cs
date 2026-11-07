using JungleMaze.App.Hud.Feedback;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JungleMaze.Presentation.UI.Hud.Feedback
{
    /// <summary>One outlined offscreen danger arrow; timer stays upright.</summary>
    public sealed class DangerArrowView:MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;[SerializeField] private RectTransform arrow;[SerializeField] private TMP_Text seconds;[SerializeField] private GameObject timerGroup;
        private readonly char[] _buffer=new char[16];
        public CanvasGroup Group=>group;public RectTransform Arrow=>arrow;public TMP_Text Seconds=>seconds;
        public void ApplyPalette(HudPalette p){arrow.GetComponent<Image>().color=Color.white;seconds.color=p.Text;} // fx_danger_arrow carries its red; the seconds sit on a neutral tag
        public void Show(bool visible,float x,float y,float degrees,bool timed,int left,float alpha){group.alpha=visible?alpha:0;((RectTransform)transform).anchoredPosition=new Vector2(x,y);arrow.localEulerAngles=new Vector3(0,0,degrees);seconds.enabled=timed;if(timerGroup!=null&&timerGroup.activeSelf!=timed)timerGroup.SetActive(timed);if(visible&&timed)seconds.SetCharArray(_buffer,0,HudFeedbackFormat.Seconds(_buffer,left));}
    }
    /// <summary>Fixed-size offscreen arrow collection adapter.</summary>
    public sealed class DangerArrowViewGroup:IDangerArrowView
    {
        private readonly DangerArrowView[] _views;public DangerArrowViewGroup(DangerArrowView[] views){_views=views;}
        public void SetArrow(int index,bool visible,float x,float y,float degrees,bool timed,int seconds,float alpha){if(index>=0&&index<_views.Length&&_views[index]!=null)_views[index].Show(visible,x,y,degrees,timed,seconds,alpha);}
    }
}
