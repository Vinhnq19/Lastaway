using System;
using System.Collections.Generic;
using JungleBackend.Protocol.Gameplay;
using JungleMaze.App.Match;
using JungleMaze.App.Player;
using JungleMaze.App.Ports;
using JungleMaze.App.Hud.State;
using JungleMaze.Domain.Hud;
using JungleMaze.Core.DI;
using JungleMaze.Core.Ticking;
using JungleMaze.World;
using JungleMaze.World.Inventory;

namespace JungleMaze.App.World
{
    /// <summary>Opt-in world stream. State pages are committed atomically; no client item simulation online.</summary>
    public sealed class WorldClientSession : IInitializable, IDisposable, ITickable
    {
        private readonly MatchSession _match;
        private readonly IPacketSender _sender;
        private readonly IPacketReceiver _receiver;
        private readonly IClientRuntimeData _runtime;
        private readonly InventoryHudBinding _hud;
        private readonly MapLayoutConfig _config;
        private readonly MapLayout _authored;
        private readonly Func<MapLayout> _selectedMap;
        private readonly Func<bool> _enabled;
        private readonly IEffectStateWriter _effects;
        private uint _effectsTick;
        private bool _hasEffects;
        private float _movementMultiplier=1f;
        private uint _portalSequence;
        private readonly Dictionary<ushort, WorldObjectStatePacket> _pages = new Dictionary<ushort, WorldObjectStatePacket>();
        private Guid _requested;
        private uint _request, _stagingRevision, _revision, _inventoryRevision;
        private ushort _pageCount, _total;
        private bool _hasRevision, _hasInventory;
        private float _retry;
        private string _collisionKey;
        private readonly Dictionary<uint,(WorldActionKind Kind,ushort Target,float Age)> _pendingActions = new Dictionary<uint,(WorldActionKind,ushort,float)>();
        private readonly List<uint> _pendingKeys = new List<uint>(8);
        public int PendingActions => _pendingActions.Count;
        public int ActionTimeouts { get; private set; }
        public string ActionMessage { get; private set; }
        public bool IsPickupPending(ushort target)
        { foreach(var action in _pendingActions.Values)if(action.Kind==WorldActionKind.Pickup&&action.Target==target)return true;return false; }
        private void ActionResult(WorldActionResultPacket p)
        {
            if(p.MatchId!=_requested||p.MatchId!=_match.MatchId||!_pendingActions.Remove(p.RequestId))return;
            ActionMessage=p.Accepted?null:p.Reason=="Busy"?"Máy chủ đang bận. Vui lòng thử lại.":"Chưa thực hiện được thao tác. Hãy kiểm tra rồi thử lại.";
            Changed?.Invoke();
        }

        public MapLayout Layout { get; private set; }
        public WorldObjectWire[] Objects { get; private set; } = Array.Empty<WorldObjectWire>();
        public InventorySnapshot Inventory { get; private set; }
        public uint EquipmentGeneration { get; private set; }
        public string Error { get; private set; }
        public bool Enabled { get; set; } = true;
        /// <summary>Only a verified layout, complete state and own inventory unlock generated-world gameplay.</summary>
        public bool IsReady => Enabled && (_enabled == null || _enabled()) && _requested == _match.MatchId &&
            Layout != null && _hasRevision && _hasInventory && Error == null;
        public bool Required => Enabled && (_enabled == null || _enabled());
        public readonly Dictionary<ushort,AirdropStatePacket> Airdrops=new Dictionary<ushort,AirdropStatePacket>();
        public event Action<AirdropStatePacket> AirdropChanged;
        public event Action Changed;
        /// <summary>A recent, deduplicated portal traversal confirmed by the server.</summary>
        public event Action<ushort,uint> Teleported;

        public WorldClientSession(MatchSession match, IPacketSender sender, IPacketReceiver receiver,
            IClientRuntimeData runtime, InventoryHudBinding hud, MapLayoutConfig config, Func<bool> enabled = null, IEffectStateWriter effects = null, MapLayout authored = null, Func<MapLayout> selectedMap = null)
        { _match=match; _sender=sender; _receiver=receiver; _runtime=runtime; _hud=hud; _config=config; _enabled=enabled; _effects=effects; _authored=authored; _selectedMap=selectedMap; }
        public void Initialize()
        { _receiver.Subscribe<WorldActionResultPacket>(ActionResult); _match.GameplayReady = GameplayReady; _receiver.Subscribe<AirdropStatePacket>(Airdrop); _receiver.Subscribe<WorldManifestPacket>(Manifest); _receiver.Subscribe<WorldObjectStatePacket>(Page); _receiver.Subscribe<InventoryStatePacket>(Items); }
        public void Dispose()
        { _receiver.Unsubscribe<WorldActionResultPacket>(ActionResult); if (_match.GameplayReady == GameplayReady) _match.GameplayReady=null; _receiver.Unsubscribe<AirdropStatePacket>(Airdrop); _receiver.Unsubscribe<WorldManifestPacket>(Manifest); _receiver.Unsubscribe<WorldObjectStatePacket>(Page); _receiver.Unsubscribe<InventoryStatePacket>(Items); }
        private bool GameplayReady() => !Required || IsReady;
        public void Tick(float dt)
        {
            if (!Required || !_match.Connected || _match.Phase != MatchPhase.InGame || _match.OwnNetId == 255)
            { if (_requested != Guid.Empty) Reset(); return; }
            if (_requested != _match.MatchId) { Reset(); _requested = _match.MatchId; _retry=0; }
            _pendingKeys.Clear();
            foreach(var id in _pendingActions.Keys)_pendingKeys.Add(id);
            foreach(var id in _pendingKeys)
            {
                var action=_pendingActions[id];action.Age+=Math.Max(0,dt);
                if(action.Age>=3)
                { _pendingActions.Remove(id);ActionTimeouts++;ActionMessage="Thao tác chưa được xác nhận. Hãy kiểm tra rồi thử lại.";Changed?.Invoke(); }
                else _pendingActions[id]=action;
            }
            _retry -= dt;
            if (!IsReady && _retry <= 0) { Send(WorldActionKind.Resync); _retry=2; }
        }
        public bool Send(WorldActionKind kind, ushort target=0, byte slot=255, bool automatic=false)
        {
            if (!Required || !_match.Connected || _match.Phase!=MatchPhase.InGame || _requested!=_match.MatchId ||
                (kind != WorldActionKind.Resync && !IsReady)) return false;
            if(_pendingActions.Count>=(automatic?6:8))return false;
            if(kind==WorldActionKind.Pickup||kind==WorldActionKind.CancelUse||kind==WorldActionKind.Resync)
                foreach(var pending in _pendingActions.Values)if(pending.Kind==kind&&pending.Target==target)return false;
            if (++_request==0) _request=1;
            var packet=new WorldActionRequestPacket{MatchId=_requested,RequestId=_request,Action=kind,TargetId=target,Slot=slot};
            if(!_sender.Send(ref packet,PacketDelivery.ReliableOrdered))return false;
            _pendingActions[_request]=(kind,target,0);ActionMessage=null;Changed?.Invoke();return true;
        }
        private void Reset()
        {
            if(Layout!=null&&_runtime.PlayerRadius>0&&_runtime.PlayerSpeed>0)
                _match.Prediction.SetWorld(new MovementSimulation(_runtime.Bounds,_runtime.Covers,_runtime.PlayerRadius,_runtime.PlayerSpeed));
            Airdrops.Clear();_requested=Guid.Empty;Layout=null;Objects=Array.Empty<WorldObjectWire>();Inventory=null;EquipmentGeneration=0;_pages.Clear();_hasRevision=_hasInventory=_hasEffects=false;_portalSequence=0;_pendingActions.Clear();ActionMessage=null;_collisionKey=null;Error=null;_movementMultiplier=1;_match.Prediction.MovementMultiplier=1;ClearEffects();_hud.Reset();Changed?.Invoke();
        }
        private void Airdrop(AirdropStatePacket p)
        {
            if(!Required||!_match.Connected||p.MatchId!=_match.MatchId||p.MatchId==Guid.Empty)return;
            if(Airdrops.TryGetValue(p.DropId,out var old)&&unchecked((int)(p.Revision-old.Revision))<=0)return;
            Airdrops[p.DropId]=p;AirdropChanged?.Invoke(p);
        }
        private void Manifest(WorldManifestPacket p)
        {
            if (p.MatchId!=_requested || p.MatchId==Guid.Empty) return;
            try
            {
                var authored = _selectedMap != null ? _selectedMap() : _authored;
                if(_selectedMap != null && authored == null){Reject("Selected map has not been verified.");return;}
                if(_config==null||(authored!=null?authored.Version:_config.Version)!=p.Version||(authored!=null?authored.Size:_config.Size)!=p.Size){Reject("World config version/size differs from server.");return;}
                if(authored!=null&&p.Seed!=authored.Seed){Reject("Authored map seed differs from server.");return;}
                var layout=authored??MapLayoutGenerator.Generate(_config,p.Seed);
                if (!string.Equals(layout.Hash,p.Hash,StringComparison.OrdinalIgnoreCase))
                { Reject("World layout/config differs from server. Update the client data."); return; }
                if (Layout != null && string.Equals(Layout.Hash, layout.Hash, StringComparison.OrdinalIgnoreCase)) return;
                Layout=layout;Error=null;Changed?.Invoke();
            }
            catch(Exception e){Reject("World manifest rejected: "+e.Message);}
        }
        private void Reject(string error)
        { Error=error; Layout=null; Objects=Array.Empty<WorldObjectWire>(); _pages.Clear(); _hasRevision=false; Changed?.Invoke(); }
        private void Page(WorldObjectStatePacket p)
        {
            if (p.MatchId!=_requested || Layout==null || p.Objects==null || p.TotalObjects>WorldWire.MaxObjects ||
                p.PageCount==0 || p.PageCount>(WorldWire.MaxObjects+23)/24 || p.PageIndex>=p.PageCount || p.Objects.Length>24) return;
            if (_hasRevision && unchecked((int)(p.Revision-_revision))<=0) return;
            if (_pages.Count!=0 && unchecked((int)(p.Revision-_stagingRevision))<0) return;
            if (_pages.Count==0 || p.Revision!=_stagingRevision)
            { _pages.Clear();_stagingRevision=p.Revision;_pageCount=p.PageCount;_total=p.TotalObjects; }
            if (_pageCount!=p.PageCount || _total!=p.TotalObjects || p.PageCount!=Math.Max(1,(p.TotalObjects+23)/24) ||
                p.Objects.Length!=(p.PageIndex+1<p.PageCount?24:p.TotalObjects-p.PageIndex*24)) return;
            _pages[p.PageIndex]=p;
            if (_pages.Count!=_pageCount) return;
            var all=new List<WorldObjectWire>(_total);var ids=new HashSet<ushort>();
            for(ushort i=0;i<_pageCount;i++)foreach(var o in _pages[i].Objects)
            {
                if(o.Id==0 || !ids.Add(o.Id) || !WorldWire.Finite(o.X) || !WorldWire.Finite(o.Y) || o.X<0 || o.Y<0 ||
                    o.X>Layout.Size || o.Y>Layout.Size || o.Width<=0 || o.Height<=0) { _pages.Clear();return; }
                all.Add(o);
            }
            if(all.Count!=_total){_pages.Clear();return;}
            foreach(var definition in Layout.Objects)
            {
                var found=false;foreach(var state in all)if(state.Id==definition.Id)
                {
                    var b=definition.Bounds;var c=b.Center;
                    if(state.Kind!=(byte)definition.Kind||state.PairId!=definition.PairId||Math.Abs(state.X-c.X)>.001f||Math.Abs(state.Y-c.Y)>.001f||
                        Math.Abs(state.Width-(b.MaxX-b.MinX))>.001f||Math.Abs(state.Height-(b.MaxY-b.MinY))>.001f){_pages.Clear();return;}
                    found=true;break;
                }
                if(!found){_pages.Clear();return;}
            }
            Objects=all.ToArray();_revision=p.Revision;_hasRevision=true;_pages.Clear();
            var covers=new List<RuntimeCover>();var key=new System.Text.StringBuilder();
            foreach(var o in Objects)
            {
                var solid=o.Kind<=4 || ((o.Kind==5||o.Kind==6||o.Kind==13)&&(o.Flags&1)==0) || (o.Kind==12&&(o.Flags&2)!=0);
                if(!solid)continue;
                covers.Add(new RuntimeCover{Name="World "+o.Id,MinX=o.X-o.Width/2,MaxX=o.X+o.Width/2,MinY=o.Y-o.Height/2,MaxY=o.Y+o.Height/2});
                key.Append(o.Id).Append(':').Append(o.Flags).Append(';');
            }
            var newKey=key.ToString();
            if(_collisionKey!=newKey)
            {
                _collisionKey=newKey;
                _match.Prediction.SetWorld(new MovementSimulation(new RuntimeMapBounds{MinX=0,MinY=0,MaxX=Layout.Size,MaxY=Layout.Size},covers,_runtime.PlayerRadius,_runtime.PlayerSpeed));
                _match.Prediction.MovementMultiplier=_movementMultiplier;
            }
            Changed?.Invoke();
        }
        private void Items(InventoryStatePacket p)
        {
            if(p.MatchId!=_requested || p.Actor!=_match.OwnNetId || p.Slots==null || p.Slots.Length!=8 ||
                (_hasInventory&&unchecked((int)(p.Revision-_inventoryRevision))<0))return;
            if (_hasEffects && unchecked((int)(p.Tick-_effectsTick))<0) return;
            _hasEffects=true; _effectsTick=p.Tick; _movementMultiplier=p.MovementMultiplier;
            _match.Prediction.MovementMultiplier=_movementMultiplier;
            ApplyEffect(EffectKind.Slow,p.SlowRemaining);ApplyEffect(EffectKind.Poison,p.PoisonRemaining);ApplyEffect(EffectKind.Stun,p.StunRemaining);
            ApplyEffect(EffectKind.Adrenaline,p.SpeedRemaining);ApplyEffect(EffectKind.Invisible,p.InvisibleRemaining);ApplyEffect(EffectKind.CcImmune,p.ImmuneRemaining);
            var s=new InventorySnapshot{Actor=p.Actor,ActiveWeapon=p.ActiveWeapon==255?-1:p.ActiveWeapon,IsUsing=p.IsUsing,
                UseSlot=p.UseSlot==255?-1:p.UseSlot,UseItemId=p.UseItemId,UseDuration=p.UseDuration,UseEnd=p.Tick/30.0+p.UseRemaining,Revision=p.Revision};
            for(var i=0;i<4;i++){s.Weapons[i]=Stack(p.Slots[i]);s.Consumables[i]=Stack(p.Slots[i+4]);}
            s.Helmet=Stack(p.Helmet);s.Armor=Stack(p.Armor);Inventory=s;_inventoryRevision=p.Revision;_hasInventory=true;
            EquipmentGeneration=p.EquipmentGeneration;
            _hud.Apply(s,_match.ServerTick/30.0);Changed?.Invoke();
            if(p.PortalSequence!=0&&(_portalSequence==0||unchecked((int)(p.PortalSequence-_portalSequence))>0))
            {
                _portalSequence=p.PortalSequence;
                var age=unchecked((int)(p.Tick-p.PortalTick));
                if(age>=0&&age<=30&&p.PortalSourceId!=0&&IsReady)Teleported?.Invoke(p.PortalSourceId,p.PortalSequence);
            }
        }
        private void ApplyEffect(EffectKind kind,float remaining)
        {
            if (_effects==null)return;
            if (remaining<0) {_effects.Apply(kind,0,0);return;}
            var elapsed=Math.Max(0,unchecked((int)(_match.ServerTick-_effectsTick)))/30f;
            var left=Math.Max(0,remaining-elapsed);
            if(left>0)_effects.Apply(kind,remaining,left);else _effects.Remove(kind);
        }
        private void ClearEffects()
        {
            if(_effects==null)return;
            _effects.Remove(EffectKind.Slow);_effects.Remove(EffectKind.Poison);_effects.Remove(EffectKind.Stun);
            _effects.Remove(EffectKind.Adrenaline);_effects.Remove(EffectKind.Invisible);_effects.Remove(EffectKind.CcImmune);
        }
        private static ItemStack Stack(InventorySlotWire s)=>new ItemStack(s.ItemId,s.Count,s.Durability,s.Ammo,s.MagazineKnown?s.Magazine:-1);
    }
}
