using System;
using JungleBackend.Protocol.Gameplay;
using JungleMaze.App.Combat;
using JungleMaze.App.Combat.Mocks;
using JungleMaze.App.Hud.Feedback;
using JungleMaze.App.Hud.State;
using JungleMaze.Core.Events;
using JungleMaze.Domain.Hud;
using JungleMaze.Presentation.UI.Hud;
using NUnit.Framework;

namespace JungleMaze.Tests.Hud.Feedback
{
    /// <summary>Reload progress on the action bar and the ring above the hero (reload UI request, 2026-11-07).</summary>
    public sealed class ReloadProgressFeedbackTests
    {
        private sealed class FakeReloadSource:IReloadProgressSource
        {
            public ReloadSample Current {get;set;}
            public event Action<bool> Ended;
            public void End(bool completed){Current=default;Ended?.Invoke(completed);}
            public int Subscribers=>Ended?.GetInvocationList().Length??0;
        }
        private sealed class RingRecorder:IReloadRingView
        {
            public bool Visible;public float Fill,X,Y;public int Writes;
            public void Show(bool visible,float fill,float x,float y){Writes++;Visible=visible;Fill=fill;X=x;Y=y;}
        }

        private static string Label(FeedbackViewRecorder v)=>new string(v.Text,0,v.TextLength);

        [Test] public void Feedback_FillsFromRemainingOverTotal_AndCompletesWithShortLinger()
        {
            var t=HudFeedbackTuning.Default;var f=new ReloadProgressFeedback(t);
            f.Tick(.016f,new ReloadSample(true,1.5f,2f));
            Assert.True(f.Visible);Assert.AreEqual(ItemUsePhase.Using,f.Phase);Assert.AreEqual(.25f,f.Fill,1e-5f);Assert.True(f.RingVisible);
            Assert.True(f.ShowsOnBar(false));Assert.False(f.ShowsOnBar(true),"item use keeps the action bar");
            f.Finish(true);f.Tick(0,default);
            Assert.AreEqual(ItemUsePhase.Completed,f.Phase);Assert.AreEqual(1f,f.Fill);Assert.True(f.RingVisible);
            f.Tick(t.UseCompletedLinger+.01f,default);Assert.False(f.Visible);Assert.AreEqual(ItemUsePhase.Idle,f.Phase);
        }

        [Test] public void Feedback_StopWithoutCompletion_ReadsAsCancelled_RingHidesAtOnce()
        {
            var t=HudFeedbackTuning.Default;var f=new ReloadProgressFeedback(t);
            f.Tick(.016f,new ReloadSample(true,1f,2f));f.Tick(.016f,default);
            Assert.AreEqual(ItemUsePhase.Cancelled,f.Phase);Assert.True(f.Visible);Assert.False(f.RingVisible);
            f.Finish(true);Assert.AreEqual(ItemUsePhase.Cancelled,f.Phase,"a late completion does not overwrite the cancel");
            f.Tick(t.UseCancelledLinger+.01f,default);Assert.False(f.Visible);
        }

        [Test] public void Sample_UsesBalanceReloadSeconds_AndGoesInactiveOnDeathOrWeaponSwitch()
        {
            var balance=StoreTestUtil.Balance();var server=new FakeCombatBackend(balance);var peer=server.AddPeer(0);
            var c=new WeaponController(balance,peer,new HudStateStore(new EventBus(),balance),0);peer.AttackResult+=c.OnAttackResult;peer.ReloadResult+=c.OnReloadResult;
            Assert.True(c.Select(CombatWeaponId.Shotgun));Assert.True(c.TryAttack(0));server.Flush();
            Assert.False(LocalReloadSource.Sample(c,true).Active);
            Assert.True(c.TryReload());server.Flush();c.Tick(.5f);
            Assert.True(balance.TryGetWeapon("SHOTGUN",out var shotgun));var total=shotgun.Stats.ReloadSeconds.Value;
            var s=LocalReloadSource.Sample(c,true);
            Assert.True(s.Active);Assert.AreEqual(total,s.TotalSeconds,1e-5f);Assert.AreEqual(c.ReloadRemainingSeconds,s.RemainingSeconds,1e-5f);
            Assert.False(LocalReloadSource.Sample(c,false).Active,"dead players show no reload");
            Assert.False(LocalReloadSource.Sample(null,true).Active,"no controller (left the match)");
            Assert.True(c.Select(CombatWeaponId.Hand));Assert.False(LocalReloadSource.Sample(c,true).Active,"switched away from the reloading weapon");
        }

        [Test] public void Presenter_ShowsReloadOnActionBarAndRing_UseTakesPriority()
        {
            var h=new FakeHudState{HasPose=true,Pose=new HeroPose(10,20,0)};var i=new FakeInventoryState();var e=new FakeEffectState();var v=new FeedbackViewRecorder();var ring=new RingRecorder();var src=new FakeReloadSource();
            using(var p=new HudFeedbackPresenter(h,i,e,HudFeedbackTuning.Default,v,v,v,v,v,v,v,v,v,src,ring))
            {
                p.Tick(0,0,0);Assert.False(v.UseVisible);Assert.False(ring.Visible);
                src.Current=new ReloadSample(true,1.5f,2f);p.Tick(.016f,0,0);
                Assert.True(v.UseVisible);Assert.AreEqual("Đang nạp đạn",Label(v));Assert.AreEqual(.25f,v.Fill,1e-5f);Assert.False(v.Hint);
                Assert.False(v.StripVisible,"weapon strip and its R Nạp hint hide while reloading");
                Assert.True(ring.Visible);Assert.AreEqual(.25f,ring.Fill,1e-5f);Assert.AreEqual(10,ring.X);Assert.AreEqual(20,ring.Y);
                i.RaiseUse(new ItemUseStatus(ItemUsePhase.Using,"GOD_LEAF","God Leaf",4,1f,1f,true));p.Tick(.016f,0,0);
                Assert.AreEqual("Đang dùng God Leaf",Label(v),"item use is shown first");Assert.True(ring.Visible);
                i.RaiseUse(default);p.Tick(.016f,0,0);Assert.AreEqual("Đang nạp đạn",Label(v));
                src.End(true);p.Tick(0,0,0);Assert.AreEqual("Xong",Label(v));Assert.AreEqual(1f,v.Fill);
                p.Tick(HudFeedbackTuning.Default.UseCompletedLinger+.01f,0,0);Assert.False(v.UseVisible);Assert.True(v.StripVisible);Assert.False(ring.Visible);
            }
            Assert.AreEqual(0,src.Subscribers,"dispose unsubscribes");
        }
    }
}
