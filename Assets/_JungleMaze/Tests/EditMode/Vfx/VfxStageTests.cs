using System;
using System.Collections.Generic;
using JungleMaze.App.Vfx;
using JungleMaze.Domain.CombatFeedback;
using JungleMaze.Domain.Vfx;
using JungleMaze.Infrastructure.Pooling;
using JungleMaze.Presentation.Vfx;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace JungleMaze.Tests.Vfx
{
    /// <summary>A real <see cref="VfxStage"/> over a real <see cref="PoolService"/> with template prefabs built in code.</summary>
    internal sealed class StageRig : IDisposable
    {
        public readonly List<Object> Owned = new List<Object>();
        public readonly FakeLog Log = new FakeLog();
        public readonly FakeMatchFeed Feed = new FakeMatchFeed();
        public readonly PoolService Pool;
        public readonly VfxAnchorRegistry Anchors;
        public readonly VfxStage Stage;
        public readonly VfxCatalog Catalog;
        public readonly Dictionary<VfxCue, VfxParticleEffect> Prefabs = new Dictionary<VfxCue, VfxParticleEffect>();

        public static readonly VfxCue[] Cues =
        {
            VfxCue.HitSpark, VfxCue.DamageNumber, VfxCue.AttackPuff, VfxCue.FuseWarning, VfxCue.AuraStun, VfxCue.AuraReverse,
            VfxCue.AuraCcImmune, VfxCue.AuraInvisible, VfxCue.AuraInvulnerable, VfxCue.AuraPoison
        };

        public StageRig(int prewarm = 2)
        {
            var poolParent = new GameObject("PoolParent");
            Owned.Add(poolParent);
            Pool = new PoolService(null, Log, poolParent.transform);
            Anchors = new VfxAnchorRegistry(Feed);
            var entries = new List<VfxCatalog.CueEntry>();
            var templates = new GameObject("Templates");
            templates.SetActive(false);
            Owned.Add(templates);
            foreach (var cue in Cues)
            {
                var go = new GameObject("Vfx_" + cue);
                go.transform.SetParent(templates.transform, false);
                var child = new GameObject("Particles");
                child.transform.SetParent(go.transform, false);
                var ps = child.AddComponent<ParticleSystem>();
                var main = ps.main;
                main.playOnAwake = false;
                main.startColor = Color.white;
                var effect = go.AddComponent<VfxParticleEffect>();
                Prefabs[cue] = effect;
                entries.Add(new VfxCatalog.CueEntry { cue = cue, prefab = effect, prewarm = prewarm, priority = VfxPriority.Low });
            }
            Catalog = ScriptableObject.CreateInstance<VfxCatalog>();
            Owned.Add(Catalog);
            Catalog.Set(entries.ToArray(), Array.Empty<VfxCatalog.StatusEntry>(), new[] { Color.white, Color.white, Color.red, Color.gray, Color.yellow });
            var stageGo = new GameObject("Stage");
            Owned.Add(stageGo);
            Stage = stageGo.AddComponent<VfxStage>();
            Stage.Construct(Pool, Anchors);
            Stage.Configure(Catalog, VfxTuningValues.Default);
        }

        public int ActiveInPool()
        {
            var n = 0;
            foreach (var p in Prefabs.Values) n += Pool.CountActive(p);
            return n;
        }

        public int TotalInPool(VfxCue cue) => Pool.CountActive(Prefabs[cue]) + Pool.CountInactive(Prefabs[cue]);

        public VfxBodyAnchor Body(byte netId, Vector3 position, bool localHero = false)
        {
            var go = new GameObject("Body" + netId);
            Owned.Add(go);
            go.transform.position = position;
            var sprite = new GameObject("Sprite").AddComponent<SpriteRenderer>();
            sprite.transform.SetParent(go.transform, false);
            sprite.color = new Color(.2f, .4f, .9f, 1f);
            var anchor = go.AddComponent<VfxBodyAnchor>();
            anchor.Configure(sprite, localHero, localHero ? -1 : netId);
            anchor.Construct(Anchors);
            return anchor;
        }

        public void Dispose()
        {
            Stage.ReleaseAll();
            Pool.Dispose();
            foreach (var o in Owned) if (o != null) Object.DestroyImmediate(o);
        }
    }

    /// <summary>AC6/AC7 with a real pool: release all at once, reuse instances, ignore stale handles.</summary>
    public sealed class VfxPoolReuseTests
    {
        [Test]
        public void HundredCycles_PerCue_NoNewInstance_NoPoolError()
        {
            using var rig = new StageRig(prewarm: 1);
            foreach (var cue in StageRig.Cues)
            {
                var before = rig.TotalInPool(cue);
                for (var i = 0; i < 100; i++)
                {
                    var h = rig.Stage.Spawn(cue, new VfxSpawn(1f, 1f));
                    Assert.AreNotEqual(0, h, cue.ToString());
                    rig.Stage.Release(h);
                }
                Assert.AreEqual(before, rig.TotalInPool(cue), cue + ": no instance created after prewarm");
            }
            Assert.AreEqual(0, rig.Log.Errors.Count);
            Assert.AreEqual(0, rig.Log.Warnings.Count);
        }

        [Test]
        public void StaleHandle_IsIgnored_AfterTheSlotWasReused()
        {
            using var rig = new StageRig();
            var old = rig.Stage.Spawn(VfxCue.HitSpark, new VfxSpawn(0f, 0f));
            rig.Stage.Release(old);
            var fresh = rig.Stage.Spawn(VfxCue.HitSpark, new VfxSpawn(0f, 0f));
            Assert.AreEqual(old & 0xFF, fresh & 0xFF, "same slot");
            rig.Stage.Release(old);
            Assert.IsNotNull(rig.Stage.InstanceOf(fresh), "a stale release does not take the new instance back");
            Assert.AreEqual(1, rig.Stage.ActiveCount);
            Assert.AreEqual(0, rig.Log.Errors.Count);
        }

        [Test]
        public void SixtyFourSlots_ThenRefuses_WithoutError()
        {
            using var rig = new StageRig();
            for (var i = 0; i < VfxStage.SlotCount; i++) Assert.AreNotEqual(0, rig.Stage.Spawn(VfxCue.HitSpark, new VfxSpawn(0f, 0f)));
            Assert.AreEqual(0, rig.Stage.Spawn(VfxCue.HitSpark, new VfxSpawn(0f, 0f)));
            Assert.AreEqual(VfxStage.SlotCount, rig.ActiveInPool());
        }

        [Test]
        public void TintAndAreaScale_AreRestoredOnDespawn()
        {
            using var rig = new StageRig();
            var h = rig.Stage.Spawn(VfxCue.AuraInvulnerable, new VfxSpawn(0f, 0f, tint: 4));
            var instance = rig.Stage.InstanceOf(h);
            Assert.AreEqual(Color.yellow, instance.Systems[0].main.startColor.color);
            rig.Stage.Release(h);
            Assert.AreEqual(Color.white, instance.Systems[0].main.startColor.color);
        }
    }

    /// <summary>AC6 with a real pool: after a room change nothing of the old match is active and nothing logs.</summary>
    public sealed class VfxResetTests
    {
        [Test]
        public void SessionChange_EveryCueBackToPool_InTheSameTick_NoDoubleDespawn()
        {
            using var rig = new StageRig();
            var bus = new JungleMaze.Core.Events.EventBus();
            for (byte i = 0; i < 6; i++) rig.Body(i, new Vector3(5 + i, 5, 0), localHero: i == 0);
            var director = new VfxDirector(bus, rig.Feed, rig.Anchors, null, rig.Stage, VfxRig.Rules(), VfxTuningValues.Default, null, rig.Log);
            director.Initialize();
            director.Tick(VfxRig.Dt);
            bus.Publish(new HitConfirmedEvent(1, 1, 3, 20, 0f, 0f));
            bus.Publish(new StatusEvent(2, 4, StatusKind.Stun, true, 5f, 5f));
            bus.Publish(new StatusEvent(3, 0, StatusKind.Poison, true, 3f, 3f));
            bus.Publish(new ExplosionEvent(4, "BANANA", ExplosionPhase.Fuse, 8f, 8f, 3f, 3f, 1.5f, 1));
            Assert.Greater(rig.ActiveInPool(), 3);

            rig.Feed.SessionKey = Guid.NewGuid();
            director.Tick(VfxRig.Dt);
            Assert.AreEqual(0, rig.ActiveInPool());
            Assert.AreEqual(0, rig.Stage.ActiveCount);
            director.Tick(1f);
            Assert.AreEqual(0, rig.Log.Errors.Count, "no 'despawned twice' from the pool");

            bus.Publish(new HitConfirmedEvent(1, 1, 3, 20, 0f, 0f));
            Assert.AreEqual(2, rig.ActiveInPool(), "same key spawns once in the new match");
            LogAssert.NoUnexpectedReceived();
        }
    }

    /// <summary>Plan 4.12.1 (R3): own body online = own PlayerView, offline = Hero; others = their PlayerView.</summary>
    public sealed class VfxAnchorRegistryTests
    {
        [Test]
        public void Online_OwnIsThePlayerView_NotTheHero()
        {
            using var rig = new StageRig();
            rig.Feed.OwnNetId = 2;
            rig.Feed.Online = true;
            var hero = rig.Body(0, new Vector3(1, 1, 0), localHero: true);
            var view = rig.Body(2, new Vector3(9, 9, 0));
            Assert.IsTrue(rig.Anchors.TryGetAnchor(2, out var a));
            Assert.AreSame(view, a);
            Assert.AreNotSame(hero, a);
        }

        [Test]
        public void Offline_OwnIsTheHero()
        {
            using var rig = new StageRig();
            rig.Feed.OwnNetId = 0;
            rig.Feed.Online = false;
            var view = rig.Body(0, new Vector3(9, 9, 0));
            var hero = rig.Body(0, new Vector3(1, 1, 0), localHero: true);
            Assert.IsTrue(rig.Anchors.TryGetAnchor(0, out var a));
            Assert.AreSame(hero, a);
            Assert.IsTrue(rig.Anchors.TryGetPosition(0, out var x, out _));
            Assert.AreEqual(1f, x);
        }

        [Test]
        public void Others_AreTheirView_HiddenOrMissingIsNotFound()
        {
            using var rig = new StageRig();
            rig.Feed.OwnNetId = 0;
            var other = rig.Body(5, new Vector3(4, 4, 0));
            Assert.IsTrue(rig.Anchors.TryGetAnchor(5, out var a));
            Assert.AreSame(other, a);
            other.Body.enabled = false; // dead body
            Assert.IsFalse(rig.Anchors.TryGetPosition(5, out _, out _));
            Assert.IsFalse(rig.Anchors.TryGetPosition(7, out _, out _));
            Assert.IsFalse(rig.Anchors.TryGetPosition(VfxSpawn.NoAnchor, out _, out _));
        }

        [Test]
        public void AnchoredInstance_FollowsTheBody_WithTheCatalogOffset()
        {
            using var rig = new StageRig();
            rig.Catalog.Cues[Array.FindIndex(rig.Catalog.Cues, c => c.cue == VfxCue.AuraStun)].anchorOffset = new Vector2(0f, .9f);
            rig.Stage.Configure(rig.Catalog, VfxTuningValues.Default);
            var body = rig.Body(3, new Vector3(4, 4, 0));
            var h = rig.Stage.Spawn(VfxCue.AuraStun, new VfxSpawn(0f, 0f, anchorNetId: 3));
            var instance = rig.Stage.InstanceOf(h);
            Assert.AreEqual(new Vector3(4f, 4.9f, 0f), instance.transform.position);
            body.transform.position = new Vector3(6, 2, 0);
            rig.Stage.Drive();
            Assert.AreEqual(new Vector3(6f, 2.9f, 0f), instance.transform.position);
        }
    }

    /// <summary>AC12 (flash part): 0.16 s ± one frame, colour always restored, never stuck after many hits.</summary>
    public sealed class HitFlashTests
    {
        [Test]
        public void Flash_LastsFlashSeconds_ThenRestoresTheColour()
        {
            using var rig = new StageRig();
            var body = rig.Body(3, Vector3.zero);
            var original = body.Body.color;
            rig.Stage.Flash(3, .16f);
            Assert.AreNotEqual(original, body.Body.color);
            var frames = 0;
            while (body.IsFlashing && frames < 100) { body.Advance(1f / 60f); frames++; }
            Assert.That(frames, Is.InRange(9, 11), "0.16 s at 60 fps ≈ 10 frames ± 1");
            Assert.AreEqual(original, body.Body.color);
        }

        [Test]
        public void HundredHitsInARow_ColourBackToOriginal()
        {
            using var rig = new StageRig();
            var body = rig.Body(3, Vector3.zero);
            var original = body.Body.color;
            for (var i = 0; i < 100; i++)
            {
                rig.Stage.Flash(3, .16f);
                body.Advance(1f / 60f);
            }
            for (var i = 0; i < 20; i++) body.Advance(1f / 60f);
            Assert.AreEqual(original, body.Body.color);
            rig.Stage.Flash(3, .16f);
            rig.Stage.CancelFlash(3);
            Assert.AreEqual(original, body.Body.color);
        }
    }
}
