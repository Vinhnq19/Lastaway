using JungleBackend.Protocol.Gameplay;
using JungleMaze.Presentation.Rendering;
using UnityEngine;
namespace JungleMaze.Presentation.World.Phase2
{
    /// <summary>Only presentation; the world stream owns landed collision and HP.</summary>
    public sealed class AirdropVisual : MonoBehaviour
    {
        public AirdropVisualSet Visuals;
        private SpriteRenderer _dragon,_dragonShadow,_egg,_shadow,_ring,_healthBack,_health;
        private readonly SpriteRenderer[] _shards=new SpriteRenderer[6];
        private uint _revision;private float _effectAt=-100;
        /// <summary>World width of the gliding dragon (wingspan), in cells.</summary>
        public const float DragonWingspan=14f;
        /// <summary>Height the dragon flies above its ground shadow, in cells.</summary>
        public const float DragonAltitude=5f;
        private Sprite _glide;
        /// <summary>The dragon glides on one frame: the source frame with the widest wingspan (art faces up, +Y).</summary>
        public static Sprite GlideFrame(WorldVisualSet.Frames frames)
        {
            Sprite best=null;
            if(frames?.Sprites==null)return null;
            foreach(var sprite in frames.Sprites)if(sprite!=null&&(best==null||sprite.bounds.size.x>best.bounds.size.x))best=sprite;
            return best;
        }
        /// <summary>Z rotation that turns the up-facing art toward the flight direction.</summary>
        public static float HeadingDegrees(Vector2 entry,Vector2 exit)
        {
            var d=exit-entry;
            return d.sqrMagnitude<1e-6f?0f:Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg-90f;
        }
        private SpriteRenderer Part(string name,int order)
        {
            var go=new GameObject(name);go.transform.SetParent(transform,false);
            var renderer=go.AddComponent<SpriteRenderer>();renderer.sortingLayerID=SortingLayers.IdOf(SortingLayers.Foreground);renderer.sortingOrder=order;
            return renderer;
        }
        private void Awake()
        {
            _dragon=Part("Dragon",20);_dragonShadow=Part("Dragon shadow",0);_dragonShadow.sortingLayerID=SortingLayers.IdOf(SortingLayers.Ground);_egg=Part("Falling egg",21);_shadow=Part("Ground shadow",0);_ring=Part("Landing ring",1);
            _healthBack=Part("Egg HP background",22);_health=Part("Egg HP",23);
            for(var i=0;i<_shards.Length;i++)_shards[i]=Part("Shell "+i,20);
        }
        private static void Size(SpriteRenderer r,Sprite s,float width,float height,Color color)
        {
            r.sprite=s;r.color=color;r.enabled=s!=null;
            if(s!=null)r.transform.localScale=new Vector3(width/s.bounds.size.x,height/s.bounds.size.y,1);
        }
        public void Show(AirdropStatePacket p,float tick,bool hasGround,ushort hp)
        {
            if(Visuals==null)return;
            if(_revision!=p.Revision){_revision=p.Revision;if(!p.Baseline&&(p.Phase==3||p.Phase==4))_effectAt=Time.unscaledTime;}
            var point=new Vector3(p.X,p.Y,0);transform.position=point;
            var flying=p.Phase==1||p.Phase==2;var landing=p.Phase==3;
            _dragon.enabled=_dragonShadow.enabled=flying;
            if(flying)
            {
                // Entry/exit and timing come from the server, including resume baselines.
                var route=tick<=p.FallTick?Vector3.Lerp(new Vector3(p.EntryX,p.EntryY,0),point,Mathf.InverseLerp(p.FlyTick,p.FallTick,tick)):Vector3.Lerp(point,new Vector3(p.ExitX,p.ExitY,0),Mathf.InverseLerp(p.FallTick,p.LandTick,tick));
                if(_glide==null)_glide=GlideFrame(Visuals.Dragon);
                var sprite=_glide;
                if(sprite!=null)
                {
                    var scale=DragonWingspan/sprite.bounds.size.x;
                    var heading=Quaternion.Euler(0,0,HeadingDegrees(new Vector2(p.EntryX,p.EntryY),new Vector2(p.ExitX,p.ExitY)));
                    Size(_dragon,sprite,sprite.bounds.size.x*scale,sprite.bounds.size.y*scale,Color.white);
                    _dragon.flipX=false;_dragon.transform.rotation=heading;
                    _dragon.transform.position=route+Vector3.up*DragonAltitude-heading*(sprite.bounds.center*scale);
                    // Top-down shadow: same silhouette and heading, squashed a little, on the ground under the route.
                    var shadowScale=new Vector3(scale*.8f,scale*.8f,1);
                    Size(_dragonShadow,sprite,sprite.bounds.size.x*shadowScale.x,sprite.bounds.size.y*shadowScale.y,new Color(0,0,0,.22f));
                    _dragonShadow.flipX=false;_dragonShadow.transform.rotation=heading;
                    _dragonShadow.transform.position=route-heading*Vector3.Scale(sprite.bounds.center,shadowScale);
                }
            }
            _egg.enabled=p.Phase==2||(landing&&!hasGround);
            if(_egg.enabled)
            {
                var fall=Mathf.InverseLerp(p.FallTick,p.LandTick,tick);
                var scale=Mathf.Min(p.Size/Visuals.Egg.bounds.size.x,p.Size*WorldObjectView.MaxRise/Visuals.Egg.bounds.size.y);
                Size(_egg,Visuals.Egg,Visuals.Egg.bounds.size.x*scale,Visuals.Egg.bounds.size.y*scale,Color.white);
                _egg.transform.position=point+Vector3.up*(-p.Size*.5f-Visuals.Egg.bounds.min.y*scale+(p.Phase==2?Mathf.Lerp(6,0,fall):0));
            }
            _shadow.enabled=flying;
            if(flying){Size(_shadow,Visuals.Disc,2,0.6f,new Color(0,0,0,.32f));_shadow.transform.position=point;}
            var age=Time.unscaledTime-_effectAt;
            _ring.enabled=landing&&age<.65f;
            if(_ring.enabled){Size(_ring,Visuals.Disc,2+age*4,0.4f+age,new Color(.9f,.8f,.5f,(1-age/.65f)*.45f));_ring.transform.position=point;}
            var showHp=landing&&hp>0&&hp<p.MaxHp;
            _healthBack.enabled=_health.enabled=showHp;
            if(showHp)
            {
                Size(_healthBack,Visuals.Disc,2,.18f,new Color(.1f,.1f,.1f,.9f));_healthBack.transform.position=point+Vector3.up*2.3f;
                var fraction=Mathf.Clamp01(hp/(float)p.MaxHp);Size(_health,Visuals.Disc,1.9f*fraction,.1f,new Color(.95f,.35f,.22f));
                _health.transform.position=_healthBack.transform.position+Vector3.left*(.95f*(1-fraction));
            }
            for(var i=0;i<_shards.Length;i++)
            {
                var r=_shards[i];r.enabled=p.Phase==4&&age<.6f;
                if(!r.enabled)continue;
                var direction=new Vector3(Mathf.Cos(i*Mathf.PI/3),Mathf.Sin(i*Mathf.PI/3),0);
                Size(r,Visuals.Egg,.3f,.35f,new Color(1,.9f,.8f,1-age/.6f));r.transform.position=point+direction*(.5f+age*3);r.transform.rotation=Quaternion.Euler(0,0,i*60+age*120);
            }
        }
    }
}
