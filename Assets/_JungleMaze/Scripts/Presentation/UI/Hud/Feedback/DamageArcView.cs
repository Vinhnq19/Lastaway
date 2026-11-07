using UnityEngine;
using UnityEngine.UI;

namespace JungleMaze.Presentation.UI.Hud.Feedback
{
    /// <summary>One solid incoming damage arc.</summary>
    public sealed class DamageArcView:MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;[SerializeField] private Image image;
        public CanvasGroup Group=>group;public Image Image=>image;
        public void ApplyPalette(HudPalette p){image.color=Color.white;} // fx_damage_arc carries its own red (Huy 07/11 keeps red)
        public void Show(bool visible,float x,float y,float rotation,float alpha){group.alpha=visible?alpha:0;var r=(RectTransform)transform;r.anchoredPosition=new Vector2(x,y);r.localEulerAngles=new Vector3(0,0,rotation);
            // The art bulges toward local +x; mirror it on the left (rotation 0) and bottom (rotation 90) edges so it always bulges away from the hero.
            r.localScale=new Vector3((rotation==0?x:y)<0?-1:1,1,1);}
    }
    /// <summary>Fixed-size arc collection adapter.</summary>
    public sealed class DamageArcViewGroup:IDamageArcView
    {
        private readonly DamageArcView[] _views;public DamageArcViewGroup(DamageArcView[] views){_views=views;}
        public void SetArc(int index,bool visible,float x,float y,float rotation,float alpha){if(index>=0&&index<_views.Length&&_views[index]!=null)_views[index].Show(visible,x,y,rotation,alpha);}
    }
}
