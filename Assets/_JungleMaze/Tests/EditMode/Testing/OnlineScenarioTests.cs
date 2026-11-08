using JungleMaze.App.Testing;
using JungleMaze.Infrastructure.Testing;
using NUnit.Framework;

namespace JungleMaze.Tests.Testing
{
    public sealed class OnlineScenarioTests
    {
        [Test] public void QualitySummaryCountsStallsAndNearestRankPercentiles()
        {
            System.Type type = null;
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                type = type ?? assembly.GetType("JungleMaze.Composition.OnlineScenarioRunner+FrameQuality");
            Assert.NotNull(type);
            var samples = new System.Collections.Generic.List<double>();
            for (int i = 0; i < 98; i++) samples.Add(16);
            samples.Add(120); samples.Add(200);
            var summary = type.GetMethod("From").Invoke(null, new object[]{"InGame", samples});
            Assert.AreEqual(16d, type.GetField("p95Ms").GetValue(summary));
            Assert.AreEqual(120d, type.GetField("p99Ms").GetValue(summary));
            Assert.AreEqual(2, type.GetField("stalls100Ms").GetValue(summary));
            Assert.AreEqual(100000d / 1888d, (double)type.GetField("fps").GetValue(summary), .001);
        }

        [Test] public void MainCanWaitForFullRoundWithoutAcceleratingServer()
        { var s = new OnlineScenario { id = "main6", expectedPlayers = 6, maxDurationSeconds = 600, minConnectedSeconds = 300, requireResult = true }; Assert.IsNull(s.Validate()); }
        [TestCase(0)] [TestCase(11)] public void RefusesImpossiblePlayerCount(int players)
        { Assert.IsNotNull(new OnlineScenario { expectedPlayers = players }.Validate()); }
        [Test] public void DeadlineMustAllowConnectionWindow()
        { Assert.IsNotNull(new OnlineScenario { maxDurationSeconds = 30, minConnectedSeconds = 30 }.Validate()); }
        [Test] public void CannotClaimReplayWithoutRequiringResult()
        { Assert.IsNotNull(new OnlineScenario { requireReplay = true, requireResult = false }.Validate()); }
        [Test] public void CannotSilentlyDuplicateFutureFeatureCheck()
        { Assert.IsNotNull(new OnlineScenario { extraAssertions = new[] { "combat.damage", "combat.damage" } }.Validate()); }
        [Test] public void MissingFeatureIsNotAutomaticallyPassed()
        { Assert.IsFalse(OnlineTestExtensions.TryGetCheck("not-implemented.feature", out _)); }
        [Test] public void ExtensionHasExplicitPendingAndFailureStates()
        { Assert.AreEqual("WAIT", new OnlineCheckResult("WAIT").Status); Assert.AreEqual("FAIL", new OnlineCheckResult("FAIL", "damage not authoritative").Status); Assert.Throws<System.ArgumentException>(() => new OnlineCheckResult("SKIP_AS_PASS")); }
        [Test] public void UnknownFutureCombatObjectMustNotBeSilentlyIgnored()
        { Assert.IsFalse(OnlineScenarioJson.TryParse("{\"schemaVersion\":1,\"id\":\"combat\",\"extensions\":{\"combat\":{\"requireKills\":1}}}", out _, out _)); }
        [Test] public void MissingOptionalFieldsRetainExplicitDefaults()
        { Assert.IsTrue(OnlineScenarioJson.TryParse("{\"schemaVersion\":1,\"id\":\"defaults\"}", out var s, out _)); Assert.AreEqual("movement", s.driver); Assert.AreEqual(28.5f, s.minInputHz); }
    }
}
