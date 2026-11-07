using UnityEngine;
using UnityEngine.UI;

namespace JungleMaze.Presentation.UI.Hud.Feedback
{
    /// <summary>
    /// Thin radial reload ring drawn on the HUD above the local hero's head. The presenter gives the fill and the own
    /// HUD pose; this view only projects that world point after the camera has moved (late execution order).
    /// </summary>
    [DefaultExecutionOrder(1200)]
    public sealed class ReloadRingView:MonoBehaviour,IReloadRingView
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Image track;
        [SerializeField] private Image fill;
        /// <summary>World units from the hero's pivot to the ring centre (above the head).</summary>
        [SerializeField] private float headOffset=.9f;
        private Camera _camera;
        private bool _visible;
        private Vector3 _world;
        public CanvasGroup Group=>group;public Image Track=>track;public Image Fill=>fill;public float HeadOffset=>headOffset;
        public void ApplyPalette(HudPalette palette){if(track!=null)track.color=palette.Surface;if(fill!=null)fill.color=palette.Amber;}
        public void Show(bool visible,float ratio,float worldX,float worldY)
        {
            _visible=visible;group.alpha=visible?1:0;if(!visible)return;
            fill.fillAmount=ratio;_world=new Vector3(worldX,worldY+headOffset,0);Place();
        }
        private void LateUpdate(){if(_visible)Place();}
        private void Place()
        {
            if(_camera==null||!_camera.isActiveAndEnabled)_camera=Camera.main;
            var parent=transform.parent as RectTransform;if(_camera==null||parent==null)return;
            var screen=_camera.WorldToScreenPoint(_world);
            if(RectTransformUtility.ScreenPointToLocalPointInRectangle(parent,screen,null,out var local))((RectTransform)transform).anchoredPosition=local;
        }
    }
}
