#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using JungleMaze.App.Ports;
using JungleMaze.App.Vfx;
using JungleMaze.App.Vfx.Mocks;
using JungleMaze.App.Visibility.Mocks;
using JungleMaze.Core.Events;
using JungleMaze.Core.Logging;
using JungleMaze.Domain.CombatFeedback;
using JungleMaze.Domain.Vfx;
using JungleMaze.Infrastructure.Config;
using JungleMaze.Infrastructure.Pooling;
using JungleMaze.Presentation.Combat;
using JungleMaze.Presentation.Player;
using JungleMaze.Presentation.Vfx;
using JungleMaze.World.Throwables;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;
using Object = UnityEngine.Object;

namespace JungleMaze.Tests.PlayMode.Vfx
{
    internal sealed class PlayFeed : IVfxMatchFeed
    {
        public Guid SessionKey { get; set; } = Guid.NewGuid();
        public bool InGame { get; set; } = true;
        public byte OwnNetId { get; set; }
        public bool Online { get; set; }
    }

    internal sealed class CountingLog : ILog
    {
        public int Warnings, Errors;
        public readonly List<string> Lines = new List<string>();
        public void Info(string message) { }
        public void Warn(string message) { Warnings++; Lines.Add(message); }
        public void Error(string message) { Errors++; Lines.Add(message); }
    }

    /// <summary>
    /// The real VFX-01 runtime without the composition root: pool, stage with the shipped catalog, director, the
    /// throwable adapter and a ThrowableView on the same fake WPN-03 source, plus 11 dummy bodies (0 = own).
    /// </summary>
    internal sealed class VfxPlayRig : IDisposable
    {
        public const float Dt = 1f / 60f;
        public readonly List<Object> Owned = new List<Object>();
        public readonly CountingLog Log = new CountingLog();
        public readonly EventBus Bus = new EventBus();
        public readonly PlayFeed Feed = new PlayFeed();
        public readonly ToggleVisibilityPolicy Visibility = new ToggleVisibilityPolicy();
        public readonly PoolService Pool;
        public readonly VfxAnchorRegistry Anchors;
        public readonly VfxStage Stage;
        public readonly VfxDirector Director;
        public readonly FakeThrowableEventSource Throwables;
        public readonly ThrowableVfxSource Adapter;
        public readonly ThrowableView View;
        public readonly VfxRuntimeSet Set;
        public readonly VfxBodyAnchor[] Bodies = new VfxBodyAnchor[11];

        /// <param name="tuning">Overrides the shipped tuning (e.g. long lifetimes for steady-state measurements).</param>
        public VfxPlayRig(VfxTuningValues? tuning = null)
        {
            Set = Resources.Load<VfxRuntimeSet>(VfxRuntimeSet.ResourcePath);
            Assert.IsNotNull(Set, "Resources/VfxRuntimeSet");
            Assert.IsTrue(Set.IsComplete);
            var values = tuning ?? Set.TuningValues;
            var poolParent = new GameObject("PoolParent");
            Owned.Add(poolParent);
            Pool = new PoolService(null, Log, poolParent.transform);
            Anchors = new VfxAnchorRegistry(Feed);
            for (byte i = 0; i <= 10; i++)
            {
                var go = new GameObject("Dummy" + i);
                Owned.Add(go);
                go.transform.position = new Vector3(10 + i * 3, 10, 0);
                var sprite = new GameObject("Body").AddComponent<SpriteRenderer>();
                sprite.transform.SetParent(go.transform, false);
                var anchor = go.AddComponent<VfxBodyAnchor>();
                anchor.Configure(sprite, i == 0, i == 0 ? -1 : i);
                anchor.Construct(Anchors);
                Bodies[i] = anchor;
            }
            var stageGo = new GameObject("Stage");
            Owned.Add(stageGo);
            Stage = stageGo.AddComponent<VfxStage>();
            Stage.Construct(Pool, Anchors);
            Stage.Configure(Set.Catalog, values);
            Director = new VfxDirector(Bus, Feed, Anchors, Visibility, Stage, Set.Catalog.BuildRules(), values, null, Log);
            Director.Initialize();
            Stage.SceneChanged += Director.ResetAll;
            Throwables = new FakeThrowableEventSource(ThrowableRules.Baseline(), Anchors);
            Adapter = new ThrowableVfxSource(Bus, null, Throwables);
            Adapter.Initialize();
            var viewGo = new GameObject("ThrowableView");
            Owned.Add(viewGo);
            View = viewGo.AddComponent<ThrowableView>();
            View.Bind(Bodies[0].transform, null, null, Throwables);
            Director.Tick(Dt);
        }

        /// <summary>One simulated frame of everything VFX-related.</summary>
        public void Step()
        {
            Throwables.Tick(Dt);
            Director.Tick(Dt);
            View.Advance(Dt);
            Stage.Drive();
            foreach (var b in Bodies) b.Advance(Dt);
        }

        public void Steps(float seconds)
        {
            for (var t = 0f; t < seconds - 1e-4f; t += Dt) Step();
        }

        public int Active(VfxCue cue)
        {
            Set.Catalog.TryGetEntry(cue, out var entry);
            return entry != null && entry.prefab != null ? Pool.CountActive(entry.prefab) : 0;
        }

        /// <summary>Renderers of VFX-01 instances that would draw this frame.</summary>
        public int VisibleRenderers()
        {
            var n = 0;
            foreach (var r in Stage.Root.GetComponentsInChildren<Renderer>(false))
                if (r.enabled && r.gameObject.activeInHierarchy) n++;
            return n;
        }

        public void Dispose()
        {
            Adapter.Dispose();
            Director.Dispose();
            Stage.ReleaseAll();
            Pool.Dispose();
            foreach (var o in Owned) if (o != null) Object.Destroy(o);
        }
    }

    /// <summary>AC2, AC6, AC8, AC10 on the shipped prefabs and catalog (no Game scene, no network).</summary>
    public sealed class VfxPreviewAcceptanceTests
    {
        private VfxPlayRig _rig;

        [SetUp] public void SetUp() => _rig = new VfxPlayRig();
        [TearDown] public void TearDown() => _rig.Dispose();

        [UnityTest]
        public IEnumerator Hit_FlashNumberSpark_ThenBackWithinOneSecond()
        {
            _rig.Bus.Publish(new HitConfirmedEvent(1, 0, 3, 27, 0f, 0f));
            yield return null;
            Assert.Greater(_rig.Active(VfxCue.HitSpark), 0);
            Assert.Greater(_rig.Active(VfxCue.DamageNumber), 0);
            Assert.IsTrue(_rig.Bodies[3].IsFlashing);
            var number = _rig.Stage.Root.GetComponentInChildren<DamageNumberVfx>();
            Assert.AreEqual(27, number.Shown);
            Assert.AreEqual("27", number.Label.text);
            _rig.Steps(1f);
            yield return null;
            Assert.AreEqual(0, _rig.Active(VfxCue.HitSpark));
            Assert.AreEqual(0, _rig.Active(VfxCue.DamageNumber));
            Assert.IsFalse(_rig.Bodies[3].IsFlashing);
        }

        [UnityTest]
        public IEnumerator Auras_StunReverseShield_LiveForTheirDuration()
        {
            _rig.Bus.Publish(new StatusEvent(1, 3, StatusKind.Stun, true, 2f, 2f));
            _rig.Bus.Publish(new StatusEvent(2, 4, StatusKind.Reverse, true, 2f, 2f));
            _rig.Bus.Publish(new StatusEvent(3, 5, StatusKind.Invulnerable, true, 2f, 2f));
            _rig.Bus.Publish(new StatusEvent(4, 6, StatusKind.CcImmune, true, 2f, 2f));
            yield return null;
            Assert.AreEqual(1, _rig.Active(VfxCue.AuraStun));
            Assert.AreEqual(1, _rig.Active(VfxCue.AuraReverse));
            Assert.AreEqual(2, _rig.Active(VfxCue.AuraInvulnerable), "one shield prefab serves both tints");
            _rig.Steps(1.8f);
            Assert.AreEqual(1, _rig.Active(VfxCue.AuraStun));
            _rig.Steps(.4f);
            yield return null;
            Assert.AreEqual(0, _rig.Active(VfxCue.AuraStun));
            Assert.AreEqual(0, _rig.Active(VfxCue.AuraReverse));
            Assert.AreEqual(0, _rig.Active(VfxCue.AuraInvulnerable));
        }

        [UnityTest]
        public IEnumerator BananaThrow_FuseFromThrownToDetonated_ThrowableViewDrawsTheBlast()
        {
            var target = _rig.Bodies[4].transform.position;
            var detonatedFrame = -1;
            var fuseGoneFrame = -1;
            var frame = 0;
            var blastSeen = false;
            _rig.Throwables.ThrowableRaised += e => { if (e.Kind == ThrowableEventKind.Detonated) detonatedFrame = frame; };
            _rig.Throwables.Throw(ThrowableKind.Banana, 0, 10f, 10f, target.x, target.y);
            yield return null;
            Assert.AreEqual(1, _rig.Active(VfxCue.FuseWarning), "warning appears with Thrown");
            var fuse = _rig.Stage.Root.GetComponentInChildren<FuseWarningVfx>();
            Assert.AreEqual(3f, fuse.Size.x, .15f, "3×3 frame ±5%");
            Assert.AreEqual(3f, fuse.Size.y, .15f);
            for (frame = 0; frame < 300; frame++)
            {
                _rig.Step();
                if (fuseGoneFrame < 0 && _rig.Active(VfxCue.FuseWarning) == 0) fuseGoneFrame = frame;
                if (detonatedFrame >= 0 && _rig.View.ActiveEffects > 0) blastSeen = true;
                if (detonatedFrame >= 0 && frame > detonatedFrame + 5) break;
            }
            yield return null;
            Assert.GreaterOrEqual(detonatedFrame, 0, "the fake fuse ran out");
            Assert.AreEqual(detonatedFrame, fuseGoneFrame, "warning removed in the same frame as Detonated");
            Assert.IsTrue(blastSeen, "ThrowableView draws the blast and the peel");
            Assert.Greater(_rig.Active(VfxCue.DamageNumber), 0, "the blast hit the body under it");
        }

        [UnityTest]
        public IEnumerator SmokeThrow_ThrowableViewDrawsTheCloud_VfxDrawsOnlyTheWarning()
        {
            var target = _rig.Bodies[8].transform.position;
            _rig.Throwables.Throw(ThrowableKind.Smoke, 0, 10f, 10f, target.x, target.y);
            var fuse = _rig.Stage.Root.GetComponentInChildren<FuseWarningVfx>();
            Assert.AreEqual(7f, fuse.Size.x, .35f, "7×7 frame ±5%");
            _rig.Steps(1.2f);
            yield return null;
            Assert.AreEqual(0, _rig.Active(VfxCue.FuseWarning));
            Assert.Greater(_rig.View.ActiveEffects, 0, "smoke cloud");
            Assert.AreEqual(0, _rig.Active(VfxCue.DamageNumber), "smoke deals no damage → no number");
        }

        [UnityTest]
        public IEnumerator RoomChange_NothingOfTheOldMatchDrawsNextFrame()
        {
            _rig.Bus.Publish(new HitConfirmedEvent(1, 0, 3, 27, 0f, 0f));
            _rig.Bus.Publish(new StatusEvent(2, 4, StatusKind.Poison, true, 5f, 5f));
            _rig.Throwables.Throw(ThrowableKind.Seed, 0, 10f, 10f, 20f, 10f);
            yield return null;
            Assert.Greater(_rig.VisibleRenderers(), 0);
            _rig.Feed.SessionKey = Guid.NewGuid();
            _rig.Step();
            yield return null;
            Assert.AreEqual(0, _rig.VisibleRenderers());
            Assert.AreEqual(0, _rig.Stage.ActiveCount);
            Assert.IsFalse(_rig.Bodies[3].IsFlashing);
            Assert.AreEqual(0, _rig.Log.Errors, string.Join("\n", _rig.Log.Lines));
        }

        [UnityTest]
        public IEnumerator Seam_HiddenTarget_RendererGoesInTheNextTick_ShownAgainOnce()
        {
            _rig.Bus.Publish(new StatusEvent(1, 5, StatusKind.Stun, true, 10f, 10f));
            yield return null;
            Assert.AreEqual(1, _rig.Active(VfxCue.AuraStun));
            _rig.Visibility.SetHidden(5, true);
            _rig.Step();
            yield return null;
            Assert.AreEqual(0, _rig.Active(VfxCue.AuraStun));
            Assert.AreEqual(0, _rig.VisibleRenderers());
            _rig.Visibility.SetHidden(5, false);
            _rig.Step();
            _rig.Step();
            Assert.AreEqual(1, _rig.Active(VfxCue.AuraStun));
        }

        [UnityTest]
        public IEnumerator Aura_StaysWithinOneCellOfTheBody()
        {
            foreach (var kind in new[] { StatusKind.Stun, StatusKind.Reverse, StatusKind.Invulnerable, StatusKind.Invisible, StatusKind.Poison })
                _rig.Bus.Publish(new StatusEvent((ulong)kind, 7, kind, true, 5f, 5f));
            _rig.Bodies[7].transform.position = new Vector3(30f, 12f, 0f);
            _rig.Step();
            yield return null;
            var body = _rig.Bodies[7].transform.position;
            var effects = _rig.Stage.Root.GetComponentsInChildren<VfxParticleEffect>(false);
            Assert.AreEqual(5, effects.Length);
            foreach (var e in effects)
                Assert.LessOrEqual(Vector2.Distance(e.transform.position, body), 1f, e.name);
        }

        [UnityTest]
        public IEnumerator SteadyState_TwentyEffects_NoGcPerFrame()
        {
            for (byte i = 1; i <= 10; i++)
            {
                _rig.Bus.Publish(new StatusEvent(i, i, StatusKind.Poison, true, 0f, 0f));
                _rig.Bus.Publish(new StatusEvent(100u + i, i, StatusKind.Invisible, true, 0f, 0f));
            }
            for (var i = 0; i < 120; i++) { _rig.Step(); if (i % 30 == 0) yield return null; }
            Assert.AreEqual(20, _rig.Stage.ActiveCount);
            Assert.That(() => { _rig.Director.Tick(VfxPlayRig.Dt); _rig.Stage.Drive(); }, Is.Not.AllocatingGCMemory());
        }

        [UnityTest]
        public IEnumerator AllEffects_TenSeconds_ConsoleClean()
        {
            var scenarios = new VfxScenarios(_rig.Bus, _rig.Director, null, _rig.Visibility, _rig.Throwables, _rig.Anchors);
            scenarios.StartLoad(10f);
            for (var i = 0; i < 600; i++)
            {
                scenarios.Tick(VfxPlayRig.Dt);
                _rig.Step();
                if (i % 60 == 0) yield return null;
            }
            Assert.AreEqual(0, _rig.Log.Errors, string.Join("\n", _rig.Log.Lines));
            LogAssert.NoUnexpectedReceived();
        }
    }

    /// <summary>AC15: ten fake sources for 60 simulated seconds never exceed the 64-instance cap and log no error.</summary>
    public sealed class VfxLoadPlayTests
    {
        [UnityTest, Category("Perf")]
        public IEnumerator TenSources_SixtySeconds_CapHolds()
        {
            using var rig = new VfxPlayRig();
            var scenarios = new VfxScenarios(rig.Bus, rig.Director, null, rig.Visibility, rig.Throwables, rig.Anchors);
            scenarios.StartLoad(60f);
            var max = 0;
            var start = Time.realtimeSinceStartup;
            for (var i = 0; i < 3600; i++)
            {
                scenarios.Tick(VfxPlayRig.Dt);
                rig.Step();
                if (rig.Stage.ActiveCount > max) max = rig.Stage.ActiveCount;
                if (i % 120 == 0) yield return null;
            }
            var ms = (Time.realtimeSinceStartup - start) * 1000f / 3600f;
            Debug.Log($"[VFX] load 60 s simulated: max active {max}, budget {rig.Director.BudgetCapacity}, refused {rig.Director.BudgetRefused}, {ms:0.000} ms per simulated frame (Editor, VFX logic only)");
            Assert.LessOrEqual(max, 64);
            Assert.Greater(max, 0);
            Assert.AreEqual(0, rig.Log.Errors, string.Join("\n", rig.Log.Lines));
        }
    }

    /// <summary>AC13: the shake offset stays inside the map and the camera returns exactly to where followers put it.</summary>
    public sealed class CameraShakePlayTests
    {
        private readonly List<Object> _owned = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var o in _owned) if (o != null) Object.Destroy(o);
            _owned.Clear();
        }

        private (Camera cam, HeroCameraFollow follow, Transform target, CameraShakeApplier shaker) Rig(ScreenShakeModel model)
        {
            var camGo = new GameObject("ShakeCam");
            _owned.Add(camGo);
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 8f;
            camGo.transform.position = new Vector3(0, 0, -10);
            var follow = camGo.AddComponent<HeroCameraFollow>();
            var target = new GameObject("Target").transform;
            _owned.Add(target.gameObject);
            follow.Target = target;
            var shakerGo = new GameObject("Shaker");
            _owned.Add(shakerGo);
            var shaker = shakerGo.AddComponent<CameraShakeApplier>();
            shaker.Bind(model, new BalanceConfigProvider(Application.streamingAssetsPath, null), null, cam);
            return (cam, follow, target, shaker);
        }

        [UnityTest]
        public IEnumerator AtTheMapEdge_StaysInside_AndReturnsToTheFollowPosition()
        {
            var model = new ScreenShakeModel(.15f, .3f);
            var (cam, _, target, shaker) = Rig(model);
            target.position = new Vector3(.5f, .5f, 0f); // bottom-left corner: the follower clamps the view
            yield return null;
            var followed = cam.transform.position;
            var halfH = cam.orthographicSize;
            var halfW = halfH * cam.aspect;
            model.Add(.15f, .3f);
            var shookOnce = false;
            for (var i = 0; i < 30; i++)
            {
                model.Tick(1f / 60f);
                yield return null;
                if (shaker.LastOffset != Vector2.zero) shookOnce = true;
                var p = cam.transform.position;
                Assert.GreaterOrEqual(p.x, halfW - .001f);
                Assert.GreaterOrEqual(p.y, halfH - .001f);
            }
            Assert.IsTrue(shookOnce, "the shake was applied at least once");
            shaker.Restore();
            Assert.AreEqual(0f, model.Amplitude);
            Assert.LessOrEqual(Vector3.Distance(cam.transform.position, followed), .001f);
        }

        [UnityTest]
        public IEnumerator FollowOverride_IsRespected_NoDriftAfterShake()
        {
            var model = new ScreenShakeModel(.15f, .3f);
            var (cam, follow, target, shaker) = Rig(model);
            var proxy = new GameObject("OwnPlayerView").transform;
            _owned.Add(proxy.gameObject);
            proxy.position = new Vector3(20f, 20f, 0f);
            target.position = new Vector3(40f, 40f, 0f);
            follow.FollowOverride = proxy;
            model.Add(.15f, .3f);
            for (var i = 0; i < 30; i++) { model.Tick(1f / 60f); yield return null; }
            yield return null;
            shaker.Restore();
            Assert.AreEqual(20f, cam.transform.position.x, .001f);
            Assert.AreEqual(20f, cam.transform.position.y, .001f);
        }

        [UnityTest]
        public IEnumerator ZeroModel_NoOffsetAtAll()
        {
            var model = new ScreenShakeModel(.15f, .3f);
            var (cam, _, target, shaker) = Rig(model);
            target.position = new Vector3(25f, 25f, 0f);
            for (var i = 0; i < 10; i++)
            {
                yield return null;
                Assert.AreEqual(Vector2.zero, shaker.LastOffset);
                Assert.AreEqual(25f, cam.transform.position.x, .001f);
            }
        }
    }
}
#endif
