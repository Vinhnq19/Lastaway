using System;
using System.Collections.Generic;
using JungleMaze.World;
using JungleMaze.World.Airdrops;
namespace JungleMaze.Tests.Airdrops
{
    /// <summary>Deterministic lifecycle checks for AirdropRuntime with a fake host.</summary>
    public static class RuntimeChecks
    {
        private sealed class FakeHost : IAirdropHost
        {
            public int ChooseCalls;
            public int LandCalls;
            public int BrokenChecks;
            public int FailChooseCalls;
            public int FailLandCalls;
            public bool ReturnZeroWorldId;
            public ushort NextWorldId = 1;
            public readonly List<int> ChooseDropIds = new List<int>();
            public readonly List<uint> ChooseSeeds = new List<uint>();
            public readonly List<float> ChooseLandTimes = new List<float>();
            public readonly List<int> LandDropIds = new List<int>();
            public readonly List<float> LandSizes = new List<float>();
            public readonly List<int> LandHps = new List<int>();
            public readonly HashSet<ushort> BrokenWorldIds = new HashSet<ushort>();

            public bool TryChooseLanding(int dropId, uint seed, float landAt, out WorldPoint position)
            {
                ChooseCalls++;
                ChooseDropIds.Add(dropId);
                ChooseSeeds.Add(seed);
                ChooseLandTimes.Add(landAt);
                if (ChooseCalls <= FailChooseCalls)
                {
                    position = default(WorldPoint);
                    return false;
                }
                position = new WorldPoint(seed % 1000u + dropId, ChooseCalls);
                return true;
            }

            public bool TryLand(int dropId, WorldPoint position, float size, int hp, out ushort worldId)
            {
                LandCalls++;
                LandDropIds.Add(dropId);
                LandSizes.Add(size);
                LandHps.Add(hp);
                if (ReturnZeroWorldId)
                {
                    worldId = 0;
                    return true;
                }
                if (LandCalls <= FailLandCalls)
                {
                    worldId = 0;
                    return false;
                }
                worldId = NextWorldId++;
                return true;
            }

            public bool IsBroken(ushort worldId)
            {
                BrokenChecks++;
                return BrokenWorldIds.Contains(worldId);
            }
        }

        public static (int Passed, int Failed) Run()
        {
            var passed = 0;
            var failed = 0;

            void Check(bool condition, string testName)
            {
                if (condition)
                {
                    Console.WriteLine($"[PASS] {testName}");
                    passed++;
                }
                else
                {
                    Console.WriteLine($"[FAIL] {testName}");
                    failed++;
                }
            }

            bool Throws<T>(Action action) where T : Exception
            {
                try
                {
                    action();
                    return false;
                }
                catch (T)
                {
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            bool Near(float a, float b)
            {
                return MathF.Abs(a - b) < 1e-4f;
            }

            AirdropState StateOf(AirdropRuntime runtime, int index)
            {
                return runtime.States[index];
            }

            // --- default timing and launch gating ---
            var host = new FakeHost();
            var runtime = new AirdropRuntime(new AirdropSettings(), 12345u, host);
            var events = new List<AirdropState>();
            runtime.Changed += s => events.Add(s);
            Check(runtime.States.Count == 3, "runtime exposes one state per scheduled drop");
            var first = StateOf(runtime, 0);
            Check(first.DropId == 1 && first.Phase == AirdropPhase.Scheduled, "runtime starts drop 1 scheduled");
            Check(first.Revision == 0u && first.WorldId == 0, "runtime default revision and world id are zero");
            Check(Near(first.FlyAt, 110f) && Near(first.FallAt, 115f) && Near(first.LandAt, 120f),
                "runtime default timing is 110/115/120");

            runtime.Tick(109.9f);
            Check(host.ChooseCalls == 0 && events.Count == 0, "runtime performs no host work before fly time");
            runtime.Tick(110f);
            Check(host.ChooseCalls == 1, "runtime chooses landing at fly time");
            first = StateOf(runtime, 0);
            Check(first.Phase == AirdropPhase.Flying && first.Revision == 1u, "runtime enters flying once");
            Check(Near(first.FlyAt, 110f) && Near(first.FallAt, 115f) && Near(first.LandAt, 120f),
                "runtime keeps schedule when selection succeeds immediately");

            runtime.Tick(119.9f);
            first = StateOf(runtime, 0);
            Check(first.Phase == AirdropPhase.Falling && host.LandCalls == 0, "runtime does not land early");
            runtime.Tick(120f);
            first = StateOf(runtime, 0);
            Check(first.Phase == AirdropPhase.Landed && first.WorldId == 1 && host.LandCalls == 1,
                "runtime lands once at LandAt with a valid world id");
            Check(Near(host.LandSizes[0], 2f) && host.LandHps[0] == 150, "runtime passes configured egg size and hp");
            Check(host.LandDropIds[0] == 1, "runtime lands with the scheduled drop id");

            var landedRevision = first.Revision;
            var eventCount = events.Count;
            runtime.Tick(120f);
            first = StateOf(runtime, 0);
            Check(host.LandCalls == 1 && host.ChooseCalls == 1 && first.Phase == AirdropPhase.Landed
                && first.Revision == landedRevision && events.Count == eventCount,
                "runtime repeated tick is idempotent");

            // --- clock jump traverses phases exactly once ---
            host = new FakeHost();
            runtime = new AirdropRuntime(new AirdropSettings(), 777u, host);
            events = new List<AirdropState>();
            runtime.Changed += s => events.Add(s);
            runtime.Tick(130f);
            first = StateOf(runtime, 0);
            Check(first.Phase == AirdropPhase.Landed && first.WorldId != 0, "runtime jump to 130 lands drop 1");
            Check(host.ChooseCalls == 1 && host.LandCalls == 1, "runtime jump creates exactly one egg");
            Check(events.Count == 3 && events[0].Phase == AirdropPhase.Flying
                && events[1].Phase == AirdropPhase.Falling && events[2].Phase == AirdropPhase.Landed,
                "runtime jump emits flying falling landed in order");
            Check(StateOf(runtime, 1).Phase == AirdropPhase.Scheduled, "runtime jump leaves later drops scheduled");
            runtime.Tick(130f);
            runtime.Tick(130.5f);
            first = StateOf(runtime, 0);
            Check(host.LandCalls == 1 && first.Revision == 3u, "runtime skip tick then repeat never duplicates");

            // --- seed determinism across drops and matches ---
            var hostA = new FakeHost();
            var runtimeA = new AirdropRuntime(new AirdropSettings(), 4242u, hostA);
            runtimeA.Tick(1000f);
            var hostB = new FakeHost();
            var runtimeB = new AirdropRuntime(new AirdropSettings(), 4242u, hostB);
            runtimeB.Tick(1000f);
            var hostC = new FakeHost();
            var runtimeC = new AirdropRuntime(new AirdropSettings(), 4243u, hostC);
            runtimeC.Tick(1000f);
            var distinctDrops = new HashSet<int>(hostA.ChooseDropIds);
            var distinctSeeds = new HashSet<uint>(hostA.ChooseSeeds);
            Check(distinctDrops.Count == 3 && hostA.ChooseDropIds.Count == 3,
                "runtime lands three drops with distinct ids");
            Check(distinctSeeds.Count == 3, "runtime derives a distinct stable seed per drop");
            var sameSeed = true;
            for (var i = 0; i < 3; i++)
                sameSeed &= hostA.ChooseSeeds[i] == hostB.ChooseSeeds[i]
                    && Near(runtimeA.States[i].Position.X, runtimeB.States[i].Position.X)
                    && Near(runtimeA.States[i].Position.Y, runtimeB.States[i].Position.Y);
            Check(sameSeed, "runtime is deterministic for the same match seed");
            Check(hostA.ChooseSeeds[0] != hostC.ChooseSeeds[0], "runtime seed depends on the match seed");

            // --- broken is reported once and is terminal ---
            host = new FakeHost();
            runtime = new AirdropRuntime(new AirdropSettings(), 9u, host);
            events = new List<AirdropState>();
            runtime.Changed += s => events.Add(s);
            runtime.Tick(130f);
            var worldId = StateOf(runtime, 0).WorldId;
            host.BrokenWorldIds.Add(worldId);
            runtime.Tick(131f);
            first = StateOf(runtime, 0);
            Check(first.Phase == AirdropPhase.Broken && first.WorldId == worldId, "runtime reports broken landing");
            var brokenRevision = first.Revision;
            var brokenEvents = events.Count;
            runtime.Tick(200f);
            first = StateOf(runtime, 0);
            Check(first.Phase == AirdropPhase.Broken && first.Revision == brokenRevision
                && events.Count == brokenEvents, "runtime broken is terminal and emitted once");

            // --- selection failures: bounded one initial plus max retries ---
            host = new FakeHost { FailChooseCalls = int.MaxValue };
            runtime = new AirdropRuntime(new AirdropSettings { MaxRetries = 3, RetrySeconds = 1f }, 11u, host);
            events = new List<AirdropState>();
            runtime.Changed += s => events.Add(s);
            runtime.Tick(109.5f);
            Check(host.ChooseCalls == 0, "runtime selection waits for fly time");
            runtime.Tick(110f);
            Check(host.ChooseCalls == 1, "runtime selection attempts once at fly time");
            runtime.Tick(110.5f);
            Check(host.ChooseCalls == 1, "runtime selection waits for the retry window");
            runtime.Tick(130f);
            first = StateOf(runtime, 0);
            Check(host.ChooseCalls == 4, "runtime failed selection cancels after one plus MaxRetries attempts");
            Check(first.Phase == AirdropPhase.Cancelled && first.Revision == 1u, "runtime failed selection cancels once");
            Check(events.Count == 1 && events[0].Phase == AirdropPhase.Cancelled, "runtime cancel emits once");
            Check(host.LandCalls == 0, "runtime cancelled selection never lands");
            Check(Near(host.ChooseLandTimes[0], 120f) && Near(host.ChooseLandTimes[1], 121f)
                && Near(host.ChooseLandTimes[3], 123f), "runtime selection retries shift the predicted land time");
            runtime.Tick(130.5f);
            runtime.Tick(131f);
            Check(host.ChooseCalls == 4, "runtime cancelled selection has no floating retries");

            host = new FakeHost { FailChooseCalls = 1 };
            runtime = new AirdropRuntime(new AirdropSettings { MaxRetries = 2, RetrySeconds = 1f }, 12u, host);
            runtime.Tick(111f);
            first = StateOf(runtime, 0);
            Check(host.ChooseCalls == 2 && first.Phase == AirdropPhase.Flying,
                "runtime recovers selection on the retry attempt");
            Check(Near(first.FlyAt, 111f) && Near(first.FallAt, 116f) && Near(first.LandAt, 121f),
                "runtime shifts all times by the selection retry delay");

            // --- landing failures: retry once per retry window, never a second launch ---
            host = new FakeHost { FailLandCalls = 2 };
            runtime = new AirdropRuntime(new AirdropSettings(), 21u, host);
            events = new List<AirdropState>();
            runtime.Changed += s => events.Add(s);
            runtime.Tick(120f);
            first = StateOf(runtime, 0);
            Check(host.LandCalls == 1 && first.Phase == AirdropPhase.Falling && first.WorldId == 0,
                "runtime failed landing stays falling without a landed state");
            Check(Near(first.LandAt, 121f) && first.Revision == 3u, "runtime publishes the deferred land time");
            Check(events[events.Count - 1].Phase == AirdropPhase.Falling
                && Near(events[events.Count - 1].LandAt, 121f), "runtime deferred land time is emitted to listeners");
            runtime.Tick(120f);
            Check(host.LandCalls == 1, "runtime deferred landing is not retried on the same tick");
            runtime.Tick(120.5f);
            Check(host.LandCalls == 1, "runtime deferred landing waits for the retry window");
            runtime.Tick(121f);
            Check(host.LandCalls == 2 && Near(StateOf(runtime, 0).LandAt, 122f),
                "runtime landing retries once per retry seconds");
            runtime.Tick(122f);
            first = StateOf(runtime, 0);
            Check(first.Phase == AirdropPhase.Landed && first.WorldId == 1 && host.LandCalls == 3,
                "runtime recovers landing on a later attempt");
            Check(host.ChooseCalls == 1 && host.ChooseDropIds.Count == 1 && host.ChooseDropIds[0] == 1,
                "runtime landing retry never re-chooses or relaunches");
            Check(StateOf(runtime, 1).Phase == AirdropPhase.Scheduled, "runtime landing retry does not shift other drops");
            Check(events.FindAll(e => e.Phase == AirdropPhase.Flying).Count == 1,
                "runtime emits flying exactly once after landing failures");

            host = new FakeHost { FailLandCalls = int.MaxValue };
            runtime = new AirdropRuntime(new AirdropSettings(), 31u, host);
            events = new List<AirdropState>();
            runtime.Changed += s => events.Add(s);
            runtime.Tick(130f);
            first = StateOf(runtime, 0);
            Check(host.LandCalls == 6, "runtime failed landing stops after one plus MaxRetries attempts");
            Check(first.Phase == AirdropPhase.Cancelled && first.WorldId == 0,
                "runtime landing failure bounded then cancelled");
            Check(host.ChooseCalls == 1, "runtime landing failure keeps the single launch");
            Check(events.FindAll(e => e.Phase == AirdropPhase.Landed).Count == 0,
                "runtime never emits landed for failed landings");
            runtime.Tick(131f);
            Check(host.LandCalls == 6, "runtime cancelled landing has no later attempts");

            host = new FakeHost { ReturnZeroWorldId = true };
            runtime = new AirdropRuntime(new AirdropSettings(), 41u, host);
            events = new List<AirdropState>();
            runtime.Changed += s => events.Add(s);
            runtime.Tick(120f);
            first = StateOf(runtime, 0);
            Check(first.Phase == AirdropPhase.Falling && first.WorldId == 0 && host.LandCalls == 1,
                "runtime rejects TryLand success with world id zero");
            runtime.Tick(130f);
            first = StateOf(runtime, 0);
            Check(first.Phase == AirdropPhase.Cancelled && host.LandCalls == 6,
                "runtime zero world id is retried and then cancelled");
            Check(events.FindAll(e => e.Phase == AirdropPhase.Landed).Count == 0,
                "runtime zero world id never emits landed");

            // --- disabled settings do nothing ---
            host = new FakeHost();
            runtime = new AirdropRuntime(new AirdropSettings { Enabled = false }, 51u, host);
            events = new List<AirdropState>();
            runtime.Changed += s => events.Add(s);
            runtime.Tick(0f);
            runtime.Tick(1000f);
            Check(host.ChooseCalls == 0 && host.LandCalls == 0 && host.BrokenChecks == 0,
                "runtime disabled makes no host calls");
            Check(events.Count == 0 && StateOf(runtime, 0).Phase == AirdropPhase.Scheduled
                && StateOf(runtime, 0).Revision == 0u, "runtime disabled emits no transitions");

            // --- clock validation ---
            host = new FakeHost();
            runtime = new AirdropRuntime(new AirdropSettings(), 61u, host);
            Check(Throws<ArgumentException>(() => runtime.Tick(-1f)), "runtime rejects negative clock");
            Check(Throws<ArgumentException>(() => runtime.Tick(float.NaN)), "runtime rejects NaN clock");
            Check(Throws<ArgumentException>(() => runtime.Tick(float.PositiveInfinity)), "runtime rejects infinite clock");
            runtime.Tick(10f);
            Check(Throws<ArgumentException>(() => runtime.Tick(9.5f)), "runtime rejects backward clock");
            runtime.Tick(10f);
            Check(host.ChooseCalls == 0, "runtime equal tick is idempotent");

            // --- construction validation and settings snapshot ---
            Check(Throws<ArgumentNullException>(() => new AirdropRuntime(null, 1u, host)), "runtime rejects null settings");
            Check(Throws<ArgumentNullException>(() => new AirdropRuntime(new AirdropSettings(), 1u, null)),
                "runtime rejects null host");
            Check(Throws<ArgumentException>(() => new AirdropRuntime(new AirdropSettings { RetrySeconds = 0f }, 1u, host)),
                "runtime rejects invalid settings");
            Check(Throws<ArgumentException>(() => new AirdropRuntime(new AirdropSettings { RetrySeconds = 1.2f }, 1u, host)),
                "runtime rejects retry window above five seconds");
            Check(Throws<ArgumentException>(() => new AirdropRuntime(new AirdropSettings { RetrySeconds = 3f }, 1u, host)),
                "runtime rejects overflow-prone retry window");
            Check(!Throws<ArgumentException>(() => new AirdropRuntime(new AirdropSettings { RetrySeconds = 1f }, 1u, host)),
                "runtime accepts a five second retry window");

            var mutableSettings = new AirdropSettings();
            var snapshotHost = new FakeHost();
            var snapshot = new AirdropRuntime(mutableSettings, 71u, snapshotHost);
            mutableSettings.Enabled = false;
            mutableSettings.EggSize = 9f;
            mutableSettings.EggHp = 1;
            mutableSettings.RetrySeconds = 4f;
            mutableSettings.MaxRetries = 0;
            mutableSettings.Drops[0].FlyAt = 1f;
            mutableSettings.Drops[0].LandAt = 2f;
            mutableSettings.Drops = new[] { new AirdropDropSettings { Id = 99, FlyAt = 5f, LandAt = 6f } };
            Check(snapshot.States.Count == 3, "runtime copies the drop list at construction");
            var snapshotFirst = snapshot.States[0];
            Check(snapshotFirst.DropId == 1 && Near(snapshotFirst.FlyAt, 110f)
                && Near(snapshotFirst.FallAt, 115f) && Near(snapshotFirst.LandAt, 120f),
                "runtime copies drop times at construction");
            snapshot.Tick(121f);
            snapshotFirst = snapshot.States[0];
            Check(snapshotFirst.Phase == AirdropPhase.Landed,
                "runtime settings mutations after constructor do not disable the match");
            Check(Near(snapshotHost.LandSizes[0], 2f) && snapshotHost.LandHps[0] == 150,
                "runtime settings mutations after constructor keep egg size and hp");

            var retryHost = new FakeHost { FailLandCalls = 1 };
            var retrySettings = new AirdropSettings();
            var retryRuntime = new AirdropRuntime(retrySettings, 81u, retryHost);
            retrySettings.RetrySeconds = 0.25f;
            retrySettings.MaxRetries = 5;
            retryRuntime.Tick(120f);
            retryRuntime.Tick(120.5f);
            Check(retryHost.LandCalls == 1 && Near(retryRuntime.States[0].LandAt, 121f),
                "runtime copies retry settings at construction");

            var readOnly = snapshot.States as IList<AirdropState>;
            Check(readOnly != null && readOnly.IsReadOnly, "runtime states view is read only");
            Check(readOnly != null && Throws<NotSupportedException>(() => readOnly.Add(default(AirdropState))),
                "runtime states view rejects mutation");

            return (passed, failed);
        }
    }
}
