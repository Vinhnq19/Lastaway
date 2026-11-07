#if UNITY_EDITOR
using System.Collections;
using JungleMaze.Domain.CombatFeedback;
using JungleMaze.Domain.Player;
using JungleMaze.Domain.Vfx;
using JungleMaze.Presentation.Player;
using JungleMaze.Presentation.Vfx;
using NUnit.Framework;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;
using Object = UnityEngine.Object;

namespace JungleMaze.Tests.PlayMode.Vfx
{
    /// <summary>
    /// Ticks the VFX logic from real frames, like the composition's tick runner. Body anchors, the stage, damage numbers,
    /// fuse frames and particle systems run their own Update/LateUpdate. Can allocate once on request (positive control
    /// for the GC recorder).
    /// </summary>
    internal sealed class VfxFrameDriver : MonoBehaviour
    {
        public VfxPlayRig Rig;
        public int AllocateBytesOnce;
        public byte[] Sink;

        private void Update()
        {
            if (AllocateBytesOnce > 0)
            {
                Sink = new byte[AllocateBytesOnce];
                AllocateBytesOnce = 0;
            }
            if (Rig == null) return;
            var dt = Time.deltaTime;
            Rig.Throwables.Tick(dt);
            Rig.Director.Tick(dt);
        }
    }

    /// <summary>
    /// AC7, review round 1: whole-frame GC over real rendered frames, read from the "GC Allocated In Frame" profiler
    /// counter, while a damage number is rising, a fuse warning is blinking, a body is flashing and four auras emit
    /// particles. In the Editor that counter also counts the Editor and the test runner (about 16 KB every frame even
    /// with no VFX object in the scene), so the frames with VFX alive are compared with frames measured right after the
    /// same effects are released, and every VFX per-frame call is also checked exactly with <c>AllocatingGCMemory</c>.
    /// </summary>
    public sealed class VfxFrameGcPlayTests
    {
        private const int WarmupFrames = 120;
        private const int MeasuredFrames = 300;
        private const int ControlBytes = 64 * 1024;

        /// <summary>Shipped tuning with long flash/number lifetimes so they are still running after the warm-up.</summary>
        private static VfxTuningValues LongLived()
        {
            var d = VfxTuningValues.Default;
            return new VfxTuningValues(60f, 60f, d.NumberRiseCells, d.ShakeMaxCells, d.ShakeSeconds, d.ShakeRadiusCells,
                d.DedupeSeconds, d.DedupeCapacity, d.Budget, d.HitShakeCells, d.FuseGraceSeconds);
        }

        [UnityTest, Category("Perf")]
        public IEnumerator WholeFrame_NumberFuseFlashAuras_VfxAddsZeroGcAfterWarmup()
        {
            using var rig = new VfxPlayRig(LongLived());
            var driverGo = new GameObject("VfxFrameDriver");
            rig.Owned.Add(driverGo);
            var driver = driverGo.AddComponent<VfxFrameDriver>();
            // A camera on the dummies so particles, sprites and the TMP number are really rendered each frame.
            var camGo = new GameObject("VfxGcCamera");
            rig.Owned.Add(camGo);
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 8f;
            camGo.transform.position = new Vector3(25f, 10f, -10f);

            using var recorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
            yield return null;
            yield return null;
            Assert.IsTrue(recorder.Valid, "GC Allocated In Frame recorder is not valid in this Editor");

            // Positive control: the recorder must see a known allocation made in Update.
            driver.AllocateBytesOnce = ControlBytes;
            long control = 0;
            for (var i = 0; i < 4; i++)
            {
                yield return null;
                if (recorder.LastValue > control) control = recorder.LastValue;
            }
            Assert.GreaterOrEqual(control, ControlBytes, "the recorder reports allocations made in a frame");

            driver.Rig = rig;
            var body = rig.Bodies[6].transform.position;
            rig.Bus.Publish(new HitConfirmedEvent(1, 1, 3, 45, 0f, 0f));
            rig.Bus.Publish(new ExplosionEvent(2, "BANANA", ExplosionPhase.Fuse, body.x, body.y, 3f, 3f, 60f, 1));
            rig.Bus.Publish(new StatusEvent(3, 4, StatusKind.Stun, true, 0f, 0f));
            rig.Bus.Publish(new StatusEvent(4, 5, StatusKind.Poison, true, 0f, 0f));
            rig.Bus.Publish(new StatusEvent(5, 7, StatusKind.Invulnerable, true, 0f, 0f));
            rig.Bus.Publish(new StatusEvent(6, 8, StatusKind.Invisible, true, 0f, 0f));
            for (var i = 0; i < WarmupFrames; i++) yield return null;
            AssertAlive(rig);

            // Exact check of every VFX per-frame call (logic, stage, body flash, number, fuse frame).
            var number = rig.Stage.Root.GetComponentInChildren<DamageNumberVfx>();
            var fuse = rig.Stage.Root.GetComponentInChildren<FuseWarningVfx>();
            var bodies = rig.Bodies;
            Assert.That(() =>
            {
                rig.Director.Tick(VfxPlayRig.Dt);
                rig.Stage.Drive();
                for (var i = 0; i < bodies.Length; i++) bodies[i].Advance(VfxPlayRig.Dt);
                number.Advance(VfxPlayRig.Dt);
                fuse.Advance(VfxPlayRig.Dt);
            }, Is.Not.AllocatingGCMemory());

            // Values go into preallocated arrays; nothing is formatted until both runs are over.
            var withVfx = new long[MeasuredFrames];
            for (var i = 0; i < MeasuredFrames; i++)
            {
                yield return null;
                withVfx[i] = recorder.LastValue;
            }
            AssertAlive(rig);

            // Same scene, same driver, effects released: what the Editor allocates on its own.
            rig.Director.ResetAll();
            for (var i = 0; i < 30; i++) yield return null;
            Assert.AreEqual(0, rig.Stage.ActiveCount);
            var without = new long[MeasuredFrames];
            for (var i = 0; i < MeasuredFrames; i++)
            {
                yield return null;
                without[i] = recorder.LastValue;
            }

            var vfx = Describe(withVfx, out var vfxMedian, out var vfxMin);
            var editor = Describe(without, out var editorMedian, out var editorMin);
            Debug.Log($"[VFX] whole-frame GC (\"GC Allocated In Frame\", valid {recorder.Valid}, control {control} B): " +
                      $"with VFX {vfx}; after release (Editor only) {editor}; VFX share per frame {vfxMedian - editorMedian} B");
            Assert.AreEqual(0, rig.Log.Errors, string.Join("\n", rig.Log.Lines));
            Assert.LessOrEqual(vfxMedian, editorMedian, "a typical frame with VFX alive allocates more than without: " + vfx + " vs " + editor);
            Assert.LessOrEqual(vfxMin, editorMin, "the quietest frame with VFX alive allocates more than without: " + vfx + " vs " + editor);
        }

        private static void AssertAlive(VfxPlayRig rig)
        {
            Assert.AreEqual(1, rig.Active(VfxCue.DamageNumber), "a damage number is rising");
            Assert.AreEqual(1, rig.Active(VfxCue.FuseWarning), "a fuse warning is blinking");
            Assert.IsTrue(rig.Bodies[3].IsFlashing, "a body is flashing");
            Assert.AreEqual(1, rig.Active(VfxCue.AuraStun));
            Assert.AreEqual(1, rig.Active(VfxCue.AuraPoison));
            Assert.AreEqual(1, rig.Active(VfxCue.AuraInvulnerable));
            Assert.AreEqual(1, rig.Active(VfxCue.AuraInvisible));
            var particles = 0;
            foreach (var ps in rig.Stage.Root.GetComponentsInChildren<ParticleSystem>(false)) particles += ps.particleCount;
            Assert.Greater(particles, 0, "aura particles are alive");
        }

        /// <summary>Total, median, min and max of a run of per-frame counter values.</summary>
        private static string Describe(long[] values, out long median, out long min)
        {
            long total = 0;
            for (var i = 0; i < values.Length; i++) total += values[i];
            var sorted = (long[])values.Clone();
            System.Array.Sort(sorted);
            median = sorted[sorted.Length / 2];
            min = sorted[0];
            return $"{values.Length} frames, total {total} B, median {median} B, min {min} B, max {sorted[sorted.Length - 1]} B";
        }
    }

    /// <summary>Review round 1, suggestion 1: a PlayerView fresh from the pool (not bound, NetId reads 0) is not a body.</summary>
    public sealed class VfxBodyAnchorBindingPlayTests
    {
        [UnityTest]
        public IEnumerator UnboundPlayerView_IsNotAnAnchor_UntilBound_AndNotAfterDespawn()
        {
            var feed = new PlayFeed { OwnNetId = 0, Online = false };
            var registry = new VfxAnchorRegistry(feed);
            var go = new GameObject("PlayerView");
            try
            {
                var view = go.AddComponent<PlayerView>();
                var anchor = go.AddComponent<VfxBodyAnchor>();
                anchor.Construct(registry);
                yield return null;
                Assert.AreEqual(1, registry.Count);
                Assert.IsFalse(view.IsBound);
                Assert.AreEqual(255, anchor.NetId);
                Assert.IsFalse(anchor.HasIdentity);
                Assert.IsFalse(registry.TryGetAnchor(0, out _), "offline: an unbound view is not the own body");
                feed.Online = true;
                Assert.IsFalse(registry.TryGetAnchor(0, out _), "online: an unbound view is not player 0");

                view.Bind(new PlayerModel(0));
                Assert.IsTrue(registry.TryGetAnchor(0, out var bound));
                Assert.AreSame(anchor, bound);
                Assert.AreEqual(0, anchor.NetId);

                view.OnDespawned();
                Assert.IsFalse(registry.TryGetAnchor(0, out _), "a despawned view is unbound again");
            }
            finally
            {
                Object.Destroy(go);
            }
        }
    }
}
#endif
