using System;
using System.Linq;
using System.Collections.Generic;
using JungleBackend.Protocol.Gameplay;
using JungleMaze.App.Combat;
using JungleMaze.App.Hud.State;
using JungleMaze.App.Match;
using JungleMaze.App.Player;
using JungleMaze.App.Ports;
using JungleMaze.App.World;
using JungleMaze.Core.Events;
using JungleMaze.Domain.Hud;
using JungleMaze.Domain.Network;
using JungleMaze.Domain.Player;
using JungleMaze.Infrastructure.Config;
using JungleMaze.Tests.App;
using JungleMaze.World;
using JungleMaze.Presentation.Combat;
using JungleMaze.Presentation.World.Phase2;
using LiteNetLib.Utils;
using NUnit.Framework;
using UnityEngine;

namespace JungleMaze.Tests.World
{
    public sealed class OnlineWorldIntegrationTests
    {
        [Test] public void AirdropStreamRejectsDuplicatesOldMatchesAndResumesWithoutAudio()
        {
            using(var r=new Rig())
            {
                r.Ready();var events=new List<AirdropStatePacket>();r.World.AirdropChanged+=events.Add;
                var p=new AirdropStatePacket{MatchId=r.MatchId,DropId=1,Phase=1,Revision=1,X=25,Y=25,FlyTick=100,FallTick=250,LandTick=400};
                r.Network.Push(p);r.Network.Push(p);Assert.AreEqual(1,events.Count);
                var audio=new JungleMaze.App.Audio.MatchAudioEventSource(null,null,null);
                var cues=new List<JungleMaze.App.Audio.AudioCue>();audio.CueRaised+=cues.Add;audio.OnAirdrop(events[0]);
                Assert.AreEqual(JungleMaze.Domain.Audio.SfxId.DragonRoar,cues.Single().Id);
                p.Revision=2;p.Phase=3;p.Baseline=true;r.Network.Push(p);audio.OnAirdrop(events[1]);Assert.AreEqual(1,cues.Count);
                p.Revision=1;p.Baseline=false;r.Network.Push(p);Assert.AreEqual(2,events.Count);
                p.MatchId=Guid.NewGuid();p.Revision=100;r.Network.Push(p);Assert.AreEqual(2,events.Count);
                p.MatchId=r.MatchId;p.Revision=3;p.Phase=4;r.Network.Push(p);audio.OnAirdrop(events[2]);
                Assert.AreEqual(JungleMaze.Domain.Audio.SfxId.EggBreak,cues.Last().Id);Assert.IsTrue(cues.Last().HasPosition);
                r.Network.SetState(NetworkState.Disconnected,DisconnectReason.None);r.World.Tick(.04f);Assert.AreEqual(0,r.World.Airdrops.Count);
                audio.Dispose();
            }
        }
        [Test] public void Actions_AreBounded_Deduplicated_AndConfirmedWithoutOptimisticInventory()
        {
            using(var r=new Rig())
            {
                r.Ready();
                Assert.IsTrue(r.World.Send(WorldActionKind.Pickup,321));
                var request=r.Sender.Last<WorldActionRequestPacket>();
                Assert.IsTrue(r.World.IsPickupPending(321));
                Assert.IsFalse(r.World.Send(WorldActionKind.Pickup,321));
                Assert.IsFalse(r.World.Inventory.Weapons.Any(w=>w.ItemId=="KNIFE"));
                r.Network.Push(new WorldActionResultPacket{MatchId=Guid.NewGuid(),RequestId=request.RequestId,Accepted=true});
                Assert.IsTrue(r.World.IsPickupPending(321),"old match result cannot clear current pending action");
                r.Network.Push(new WorldActionResultPacket{MatchId=r.MatchId,RequestId=request.RequestId,Accepted=true});
                Assert.IsFalse(r.World.IsPickupPending(321));
                for(ushort target=400;target<420;target++)r.World.Send(WorldActionKind.Pickup,target,automatic:true);
                Assert.LessOrEqual(r.World.PendingActions,6);
                Assert.IsTrue(r.World.Send(WorldActionKind.Select,slot:0),"manual command retains room in bounded window");
                Assert.LessOrEqual(r.World.PendingActions,8);
                r.World.Tick(3.1f);
                Assert.Greater(r.World.ActionTimeouts,0);
                Assert.IsFalse(r.World.IsPickupPending(400));
            }
        }
        [Test] public void Resume_DoesNotReuseInventoryRequestSequence()
        {
            using(var r=new Rig())
            {
                r.Ready();r.World.Send(WorldActionKind.Pickup,321);
                var before=r.Sender.Last<WorldActionRequestPacket>().RequestId;
                r.Network.SetState(NetworkState.Disconnected,DisconnectReason.None);r.World.Tick(0);
                r.Network.SetState(NetworkState.Joined,DisconnectReason.None);r.StartRound();r.Ready();
                Assert.IsTrue(r.World.Send(WorldActionKind.Pickup,321));
                Assert.Greater(r.Sender.Last<WorldActionRequestPacket>().RequestId,before);
            }
        }
        [Test] public void WorldHashMatchesCoreClrGoldenVector()
        {
            var map=new MapLayout{Version="hash-vector",Seed=7,Size=50};
            map.Spawns.Add(new WorldPoint(39.562305f,35.580135f));
            Assert.AreEqual("a30edc92d586ba3453c936fa654ecc3c0c9b7a6f6688bb92b424fef31e3768fb",MapLayoutGenerator.ComputeHash(map));
        }
        private sealed class Sender : IPacketSender
        {
            public readonly List<object> Packets=new List<object>();
            public bool Send<T>(ref T packet,PacketDelivery delivery) where T:struct,INetSerializable
            { Packets.Add(packet);return true; }
            public int Count<T>() {var count=0;foreach(var p in Packets)if(p is T)count++;return count;}
            public T Last<T>() {for(var i=Packets.Count-1;i>=0;i--)if(Packets[i] is T p)return p;throw new InvalidOperationException();}
        }
        private sealed class Runtime : IClientRuntimeData
        {
            public IReadOnlyList<RuntimeCover> Covers => Array.Empty<RuntimeCover>();
            public RuntimeMapBounds Bounds => new RuntimeMapBounds{MinX=0,MinY=0,MaxX=50,MaxY=50};
            public float PlayerSpeed=>4;public float PlayerRadius=>.4f;public ZoneSchedule ZoneSchedule=>null;
            public string LoadLocal()=>null;public string Verify(string version,string hash)=>null;
        }
        private sealed class Rig : IDisposable
        {
            public readonly EventBus Bus=new EventBus();
            public readonly Sender Sender=new Sender();
            public readonly FakeNetwork Network;
            public readonly MatchSession Match;
            public readonly WorldClientSession World;
            public readonly OnlineCombatSession Combat;
            public readonly HudStateStore Hud;
            public readonly EffectStateStore Effects;
            public readonly MapLayoutConfig Config=new MapLayoutConfig();
            public readonly MapLayout Layout;
            public Guid MatchId=Guid.NewGuid();
            public bool Required=true;
            public Rig(bool authored=false)
            {
                var balance=new BalanceConfigProvider(Application.streamingAssetsPath,null);
                Network=new FakeNetwork(Bus);Hud=new HudStateStore(Bus,balance);Effects=new EffectStateStore(Bus);
                var inventory=new InventoryStateStore(Bus,balance);
                Match=new MatchSession(Network,Sender,Network,Bus,new PlayerRegistry());
                var authoredLayout=authored?WorldConfigData.Load(Application.streamingAssetsPath).Layout:null;
                World=new WorldClientSession(Match,Sender,Network,new Runtime(),new InventoryHudBinding(inventory,Hud,false),Config,()=>Required,Effects,authoredLayout);
                Combat=new OnlineCombatSession(Match,World,balance,Sender,Network,Hud);
                Match.Initialize();World.Initialize();Combat.Initialize();
                Layout=authoredLayout??MapLayoutGenerator.Generate(Config,20261028);
                Network.SetState(NetworkState.Joined,DisconnectReason.None);StartRound();
            }
            public void StartRound()
            {
                Network.Push(new MatchPhasePacket{MatchId=MatchId,Phase=MatchPhase.InGame,ServerTick=100});
                Network.Push(new ResumeTokenPacket{MatchId=MatchId,NetId=0,ResumeToken=Guid.NewGuid()});
                var snapshot=new WorldSnapshotPacket{MatchId=MatchId,ServerTick=101,PlayerCount=1};
                snapshot.SetPlayer(0,new PlayerStatePacket{NetId=0,Tick=101,X=5,Y=5,Hp=100});Network.Push(snapshot);
                World.Tick(0);Combat.Tick(0);
            }
            public void Manifest(string hash=null)
            {Network.Push(new WorldManifestPacket{MatchId=MatchId,Version=Layout.Version,Size=(ushort)Layout.Size,Seed=Layout.Seed,Hash=hash??Layout.Hash});}
            public void Page(int index)
            {
                var count=Math.Min(24,Layout.Objects.Count-index*24);var objects=new WorldObjectWire[count];
                for(var i=0;i<count;i++)
                {
                    var d=Layout.Objects[index*24+i];var b=d.Bounds;var c=b.Center;
                    objects[i]=new WorldObjectWire{Id=d.Id,Kind=(byte)d.Kind,PairId=d.PairId,X=c.X,Y=c.Y,Width=b.MaxX-b.MinX,Height=b.MaxY-b.MinY,Hp=100};
                }
                Network.Push(new WorldObjectStatePacket{MatchId=MatchId,Revision=1,Tick=101,PageIndex=(ushort)index,PageCount=(ushort)((Layout.Objects.Count+23)/24),TotalObjects=(ushort)Layout.Objects.Count,Objects=objects});
            }
            public void Pages(){for(var i=0;i<(Layout.Objects.Count+23)/24;i++)Page(i);}
            public InventoryStatePacket Items(string weapon="SHOTGUN",int slot=0,uint revision=1,int magazine=3,int reserve=9)
            {
                var packet=new InventoryStatePacket{MatchId=MatchId,Actor=0,ActiveWeapon=(byte)slot,UseSlot=255,Revision=revision,Tick=101,MovementMultiplier=1,Slots=new InventorySlotWire[8]};
                packet.Slots[slot]=new InventorySlotWire{ItemId=weapon,Count=1,MagazineKnown=true,Magazine=(ushort)magazine,Ammo=(ushort)reserve};
                return packet;
            }
            public void Ready(){Manifest();Pages();Network.Push(Items());Combat.Tick(0);}
            public void Dispose(){Combat.Dispose();World.Dispose();Match.Dispose();}
        }

        [Test] public void ThrowableSession_SendsHeldKindAndAim_AndMirrorsBellReversal()
        {
            using(var r=new Rig())
            {
                r.Ready();
                var throwables=new OnlineThrowableSession(r.Match,r.World,r.Sender,r.Network);
                throwables.Initialize();
                try
                {
                    throwables.Tick(0);
                    var seen=new List<JungleMaze.World.Throwables.ThrowableEvent>();
                    throwables.ThrowableRaised+=seen.Add;
                    Assert.IsTrue(throwables.TryUse(JungleMaze.World.Throwables.ThrowableKind.Banana,90f,3.5f));
                    var request=r.Sender.Last<ThrowableRequestPacket>();
                    Assert.AreEqual(r.MatchId,request.MatchId);
                    Assert.AreEqual(1u,request.RequestId);
                    Assert.AreEqual(1,request.Kind);
                    Assert.AreEqual(90f,CombatShotMath.DecodeAim(request.Aim),.01f);
                    Assert.AreEqual(3.5f,request.Distance,1e-4f);
                    Assert.IsTrue(throwables.TryUse(JungleMaze.World.Throwables.ThrowableKind.Banana,0f,1f));
                    Assert.AreEqual(2u,r.Sender.Last<ThrowableRequestPacket>().RequestId,"request ids increase");

                    r.Network.Push(new ThrowableEventPacket{MatchId=Guid.NewGuid(),Event=14,Item=7,Actor=3,Target=0,Seconds=10});
                    Assert.AreEqual(0,seen.Count,"events of another match are ignored");
                    r.Network.Push(new ThrowableEventPacket{MatchId=r.MatchId,Event=14,Item=7,Actor=3,Target=0,Seconds=10,Amount=15});
                    throwables.Tick(.1f);
                    Assert.AreEqual(1,seen.Count);
                    Assert.IsTrue(throwables.Reversed&&r.Match.Prediction.Reversed,"a bell hit on this player reverses predicted movement");
                    throwables.Tick(10f);
                    Assert.IsFalse(r.Match.Prediction.Reversed,"the reversal ends after its 10 s");
                }
                finally{throwables.Dispose();}
            }
        }

        [Test] public void NormalWorldUsesAuthoredTeamMapWithFixedInteractivePlacements()
        {
            using(var r=new Rig(true))
            {
                r.Ready();Assert.IsTrue(r.World.IsReady);
                var expected=WorldConfigData.Load(Application.streamingAssetsPath).Layout;
                Assert.AreEqual(expected.Hash,r.World.Layout.Hash);
                Assert.AreEqual(expected.Objects.Count,r.World.Layout.Objects.Count);
                Assert.AreEqual(expected.Seed,r.World.Layout.Seed);
                foreach(WorldObjectKind kind in Enum.GetValues(typeof(WorldObjectKind)))
                    Assert.AreEqual(expected.Objects.Count(o=>o.Kind==kind),r.World.Layout.Objects.Count(o=>o.Kind==kind),kind.ToString());
                Assert.AreEqual(10,r.World.Layout.Spawns.Count);
                foreach(var sp in r.World.Layout.Spawns)Assert.IsTrue(r.World.Layout.IsWalkable(sp,.4f));
                CollectionAssert.AreEqual(expected.Spawns.Select(sp=>sp.X),r.World.Layout.Spawns.Select(sp=>sp.X));
                CollectionAssert.AreEqual(expected.Spawns.Select(sp=>sp.Y),r.World.Layout.Spawns.Select(sp=>sp.Y));
            }
        }
        [Test] public void MovementWaitsForVerifiedMapAllPagesAndOwnInventory()
        {
            using(var r=new Rig())
            {
                r.Match.SetIntent(new PlayerIntent{MoveX=1});r.Match.Tick(.1f);
                Assert.AreEqual(0,r.Sender.Count<MatchInputPacket>());
                r.Manifest();r.Network.Push(r.Items());r.Page(0);r.Match.Tick(.1f);
                Assert.IsFalse(r.World.IsReady);Assert.AreEqual(0,r.Sender.Count<MatchInputPacket>());
                r.Pages();r.Match.Tick(.1f);
                Assert.IsTrue(r.World.IsReady);Assert.Greater(r.Sender.Count<MatchInputPacket>(),0);
            }
        }
        [Test] public void MismatchedManifestCannotUnlockMovementOrCombat()
        {
            using(var r=new Rig())
            {
                r.Manifest(new string('0',64));r.Pages();r.Network.Push(r.Items());r.Match.Tick(.1f);
                Assert.IsNotNull(r.World.Error);Assert.IsFalse(r.Combat.TryAttack(90));
                Assert.AreEqual(0,r.Sender.Count<MatchInputPacket>());Assert.AreEqual(0,r.Sender.Count<AttackRequestPacket>());
                r.Manifest();r.Pages();Assert.IsTrue(r.World.IsReady);
            }
        }
        [Test] public void OwnedWeaponDrivesRealPacketAndHudMagazineAndReserve()
        {
            using(var r=new Rig())
            {
                r.Ready();Assert.AreEqual(CombatWeaponId.Shotgun,r.Combat.Controller.Selected);
                Assert.AreEqual(3,r.Hud.Ammo.InMagazine);Assert.AreEqual(9,r.Hud.Ammo.Reserve);
                Assert.IsTrue(r.Combat.TryAttack(90));var attack=r.Sender.Last<AttackRequestPacket>();
                Assert.AreEqual(CombatWeaponId.Shotgun,attack.Weapon);
                r.Network.Push(new AttackResultPacket{NetId=0,RequestSeq=attack.RequestSeq,Weapon=attack.Weapon,Outcome=CombatOutcome.Accepted,Magazine=2,Tick=102});
                Assert.AreEqual(0,r.Combat.Controller.PendingCount);Assert.AreEqual(2,r.Hud.Ammo.InMagazine);
                r.Network.Push(r.Items("AXE",1,2,0,0));
                Assert.AreEqual(CombatWeaponId.Axe,r.Combat.Controller.Selected);Assert.AreEqual("AXE",r.Hud.HeldWeapon.ItemId);
            }
        }
        [Test] public void AttackCancelsUseBeforeSendingCombatAndSameKindSlotSwapCancelsReload()
        {
            using(var r=new Rig())
            {
                r.Ready();var use=r.Items(revision:2);use.IsUsing=true;use.UseSlot=0;use.UseItemId="GOD_LEAF";use.UseDuration=2;use.UseRemaining=2;
                r.Network.Push(use);Assert.IsTrue(r.Combat.TryAttack(0));
                var count=r.Sender.Packets.Count;
                Assert.AreEqual(WorldActionKind.CancelUse,((WorldActionRequestPacket)r.Sender.Packets[count-2]).Action);
                Assert.IsInstanceOf<AttackRequestPacket>(r.Sender.Packets[count-1]);
                r.Network.Push(r.Items(revision:3,magazine:0));r.Combat.Controller.Tick(2);
                Assert.IsTrue(r.Combat.TryReload());
                r.Network.Push(r.Items(slot:1,revision:4,magazine:1,reserve:7));
                Assert.IsFalse(r.Combat.Controller.IsReloading);Assert.IsFalse(r.Combat.Controller.IsReloadPending);
            }
        }
        [Test] public void EffectsUpdatePredictionWithoutRebuildingGeometryAndClearOnRoundChange()
        {
            using(var r=new Rig())
            {
                r.Ready();var geometry=r.Match.Prediction.World;var effect=r.Items(revision:2);
                effect.Tick=102;effect.MovementMultiplier=.6f;effect.SlowRemaining=-1;effect.PoisonRemaining=3;
                r.Network.Push(effect);
                Assert.AreSame(geometry,r.Match.Prediction.World);Assert.AreEqual(.6f,r.Match.Prediction.MovementMultiplier,.001f);
                Assert.IsTrue(r.Effects.TryGet(EffectKind.Slow,out var slow));Assert.AreEqual(0,slow.DurationSeconds);
                Assert.IsTrue(r.Effects.TryGet(EffectKind.Poison,out _));
                r.MatchId=Guid.NewGuid();r.StartRound();
                Assert.IsFalse(r.World.IsReady);Assert.IsFalse(r.Combat.CanAct);
                Assert.AreEqual(CombatWeaponId.Hand,r.Combat.Controller.Selected);Assert.AreEqual(0,r.Combat.Controller.PendingCount);
                Assert.IsFalse(r.Effects.TryGet(EffectKind.Slow,out _));Assert.IsFalse(r.Effects.TryGet(EffectKind.Poison,out _));
            }
        }
        [Test] public void LegacyProfileDoesNotWaitForWorldPackets()
        {
            using(var r=new Rig())
            {r.Required=false;r.World.Tick(0);r.Match.Tick(.1f);Assert.Greater(r.Sender.Count<MatchInputPacket>(),0);}
        }
        [Test] public void SameSlotReplacementCancelsReloadAndPortalEventsAreConfirmedAndDeduplicated()
        {
            using(var r=new Rig())
            {
                r.Ready();Assert.IsTrue(r.Combat.TryReload());
                var state=r.Items(revision:2,magazine:1);state.EquipmentGeneration=1;
                r.Network.Push(state);Assert.IsFalse(r.Combat.Controller.IsReloading);
                var cues=0;r.World.Teleported+=(source,sequence)=>cues++;
                state.Revision=3;state.PortalSourceId=1;state.PortalSequence=1;state.PortalTick=101;
                r.Network.Push(state);r.Network.Push(state);Assert.AreEqual(1,cues);
                state.PortalSequence=2;state.PortalTick=1;r.Network.Push(state);Assert.AreEqual(1,cues,"old traversal must not replay on resync");
            }
        }
        [Test] public void UnsupportedHeldItemIsDisplayedWithoutSendingHandAttackInstead()
        {
            using(var r=new Rig())
            {
                r.Ready();r.Network.Push(r.Items("BANANA",0,2,0,0));
                Assert.AreEqual("BANANA",r.Hud.HeldWeapon.ItemId);Assert.IsFalse(r.Combat.TryAttack(0));
                Assert.AreEqual(0,r.Sender.Count<AttackRequestPacket>());
            }
        }
        [Test] public void PortalDestinationUsesGroupMembershipRatherThanPairNumberAsObjectId()
        {
            var objects=new[]{new WorldObjectWire{Id=2,Kind=5},new WorldObjectWire{Id=210,Kind=11,PairId=2},new WorldObjectWire{Id=211,Kind=11,PairId=2},new WorldObjectWire{Id=212,Kind=11,PairId=3}};
            Assert.AreEqual(211,WorldSceneView.FindPortalDestination(objects,210));
            Assert.AreEqual(210,WorldSceneView.FindPortalDestination(objects,211));
            Assert.AreEqual(0,WorldSceneView.FindPortalDestination(objects,2));
        }
        [Test] public void InteractionPromptUsesCursorPriorityAndTheSameReachAsPickup()
        {
            var objects = new[]
            {
                new WorldObjectWire { Id = 1, Kind = 100, X = .5f, Y = 0 },
                new WorldObjectWire { Id = 2, Kind = 100, X = 1, Y = 0 },
                new WorldObjectWire { Id = 3, Kind = 100, X = 1.21f, Y = 0 }
            };
            var target = WorldSceneView.InteractionTarget(objects, Vector2.zero, new Vector2(1.21f, 0));
            Assert.IsTrue(target.HasValue);
            Assert.AreEqual(2, target.Value.Id, "The cursor must not select loot beyond the player's reach.");
            Assert.IsFalse(WorldSceneView.InteractionTarget(objects, new Vector2(10, 10), Vector2.zero).HasValue);
        }

        [Test] public void InteractionPromptFallsBackToUsableDoorOnlyWhenNoLootIsInReach()
        {
            var objects = new[]
            {
                new WorldObjectWire { Id = 1, Kind = 12, Hp = 100, Width = 1, Height = 1 },
                new WorldObjectWire { Id = 2, Kind = 100, X = 1, Y = 0 }
            };
            Assert.AreEqual(2, WorldSceneView.InteractionTarget(objects, Vector2.zero, Vector2.zero).Value.Id);
            objects[1].X = 20;
            Assert.AreEqual(1, WorldSceneView.InteractionTarget(objects, Vector2.zero, Vector2.zero).Value.Id);
            objects[0].Hp = 0;
            Assert.IsFalse(WorldSceneView.InteractionTarget(objects, Vector2.zero, Vector2.zero).HasValue);
            objects[0].Hp = 100;
            objects[0].Flags = 1;
            Assert.IsFalse(WorldSceneView.InteractionTarget(objects, Vector2.zero, Vector2.zero).HasValue);
        }

        [Test] public void OnlinePresenterRequiredDependenciesExcludeDevelopmentBackend()
        {
            var construct=typeof(CombatWeaponPresenter).GetMethod("Construct");Assert.IsNotNull(construct);
            foreach(var parameter in construct.GetParameters())
                Assert.AreNotEqual(typeof(JungleMaze.App.Combat.Mocks.FakeCombatBackend),parameter.ParameterType);
        }
    }
}
