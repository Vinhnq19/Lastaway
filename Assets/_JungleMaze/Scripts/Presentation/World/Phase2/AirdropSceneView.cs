using System;
using System.Collections.Generic;
using JungleBackend.Protocol.Gameplay;
using JungleMaze.App.Flow;
using JungleMaze.App.Match;
using JungleMaze.App.World;
using JungleMaze.Presentation.UI.Hud;
using UnityEngine;
using UnityEngine.UI;
namespace JungleMaze.Presentation.World.Phase2
{
    /// <summary>One set of views per Game scene, discarded on replay/map change.</summary>
    public sealed class AirdropSceneView : MonoBehaviour
    {
        private WorldClientSession _online;private MatchSession _match;private OfflineWorldSession _offline;private GameFlow _flow;
        private Guid _round;private bool _wasOffline;private MinimapView _minimap;
        private readonly Dictionary<ushort,AirdropVisual> _views=new Dictionary<ushort,AirdropVisual>();
        private readonly Dictionary<ushort,RectTransform> _markers=new Dictionary<ushort,RectTransform>();
        private AirdropVisual _prefab;
        public void Construct(WorldClientSession online,MatchSession match,OfflineWorldSession offline,GameFlow flow)
        {_online=online;_match=match;_offline=offline;_flow=flow;_prefab=Resources.Load<AirdropVisual>("AirdropVisual");}
        private void LateUpdate()
        {
            var offline=_flow?.IsOffline==true;var active=offline?_offline?.IsReady==true:_online?.IsReady==true;
            var id=active?(offline?_offline.MatchId:_match.MatchId):Guid.Empty;
            if(id!=_round||offline!=_wasOffline){Clear();_round=id;_wasOffline=offline;}
            if(!active||_prefab==null)return;
            var objects=offline?_offline.Objects:_online.Objects;
            if(offline&&_offline.Airdrops!=null)
                foreach(var state in _offline.Airdrops.Runtime.States)
                {
                    var p=new AirdropStatePacket{MatchId=id,DropId=(ushort)state.DropId,WorldId=state.WorldId,Phase=(byte)state.Phase,Revision=state.Revision,Size=_offline.Airdrops.EggSize,MaxHp=_offline.Airdrops.EggHp,
                        X=state.Position.X,Y=state.Position.Y,FlyTick=(uint)(state.FlyAt*30),FallTick=(uint)(state.FallAt*30),LandTick=(uint)(state.LandAt*30),
                        EntryX=-8,EntryY=state.Position.Y,ExitX=_offline.Layout.Size+8,ExitY=state.Position.Y};
                    Draw(p,_offline.ElapsedSeconds*30,objects,_offline.Layout.Size);
                }
            else if(!offline)foreach(var p in _online.Airdrops.Values)Draw(p,_match.ServerTick,objects,_online.Layout.Size);
        }
        private void Draw(AirdropStatePacket p,float tick,WorldObjectWire[] objects,int size)
        {
            if(p.Phase==5)
            {
                if(_views.TryGetValue(p.DropId,out var cancelled)){Destroy(cancelled.gameObject);_views.Remove(p.DropId);}
                if(_markers.TryGetValue(p.DropId,out var cancelledMarker)){Destroy(cancelledMarker.gameObject);_markers.Remove(p.DropId);}
                return;
            }
            if(p.Phase==0)return;
            if(!_views.TryGetValue(p.DropId,out var view)){view=Instantiate(_prefab,transform);view.name="Dragon drop "+p.DropId;_views.Add(p.DropId,view);}
            ushort hp=p.MaxHp;var hasGround=false;
            foreach(var o in objects)if(o.Id==p.WorldId){hasGround=true;hp=o.Hp;break;}
            view.Show(p,tick,hasGround,hp);
            if(_minimap==null)_minimap=FindFirstObjectByType<MinimapView>();
            if(_minimap==null||_minimap.Marker==null)return;
            if(!_markers.TryGetValue(p.DropId,out var marker))
            {
                var go=new GameObject("Egg marker "+p.DropId,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
                marker=go.GetComponent<RectTransform>();marker.SetParent(_minimap.Marker.parent,false);marker.sizeDelta=new Vector2(8,8);
                var image=go.GetComponent<Image>();image.raycastTarget=false;image.sprite=view.Visuals.Egg;
                _markers.Add(p.DropId,marker);
            }
            marker.gameObject.SetActive(p.Phase>=1&&p.Phase<=3);
            var pixels=_minimap.SizePixels-6;
            marker.anchorMin=marker.anchorMax=new Vector2(.5f,.5f);marker.pivot=new Vector2(.5f,.5f);
            marker.anchoredPosition=new Vector2((p.X/size-.5f)*pixels,(p.Y/size-.5f)*pixels);
            marker.GetComponent<Image>().color=p.Phase==3?Color.green:Color.yellow;
        }
        private void Clear(){foreach(var v in _views.Values)if(v!=null)Destroy(v.gameObject);foreach(var m in _markers.Values)if(m!=null)Destroy(m.gameObject);_views.Clear();_markers.Clear();}
        private void OnDestroy()=>Clear();
    }
}
