using JungleMaze.App.Combat;
using JungleMaze.App.Hud.State;
using JungleMaze.App.Match;
using JungleMaze.App.MatchUi;
using JungleMaze.App.Ports;
using JungleMaze.App.Vfx;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using JungleMaze.App.Vfx.Mocks;
using JungleMaze.App.Visibility.Mocks;
#endif
using JungleMaze.App.Visibility;
using JungleMaze.App.World;
using JungleMaze.Core.Events;
using JungleMaze.Infrastructure.Ticking;
using JungleMaze.Presentation.Vfx;
using UnityEngine;

namespace JungleMaze.Composition
{
    /// <summary>
    /// VFX-01 composition (plan 4.4 as changed by 4.12.5): runtime set, visibility seam, match feed, body anchors,
    /// stage over the pool, director, the local-effect and throwable adapters, camera shake and (dev) the mock match,
    /// fake throwables, scenarios and the F10 panel. Called after <c>ComposeWorld</c>, which creates the throwable
    /// sessions. A missing asset only disables VFX.
    /// </summary>
    public sealed partial class Bootstrap
    {
        private void ComposeVfx(TickRunner ticks, MatchSession match)
        {
            // Loaded once here (never per frame), like the audio runtime set.
            var set = Resources.Load<VfxRuntimeSet>(VfxRuntimeSet.ResourcePath);
            if (set == null || !set.IsComplete)
            {
                _log.Warn("[Bootstrap] VfxRuntimeSet missing or incomplete; combat VFX are off.");
                return;
            }

            var bus = _registry.Resolve<IEventBus>();
            var balance = _registry.Resolve<IBalanceConfigProvider>();
            var offlineMatch = _registry.Resolve<OfflineMatchSession>();
            var offlineWorld = _registry.Resolve<OfflineWorldSession>();
            var tuning = set.TuningValues;

            // The default is not registered: FOW-01 may register IVisibilityPolicy after this without a duplicate-type
            // error, and the deferred policy then forwards to it (review round 1, suggestion 2).
            if (!_registry.TryResolve<IVisibilityPolicy>(out var visibility))
                visibility = new DeferredVisibilityPolicy(() => _registry.TryResolve<IVisibilityPolicy>(out var fow) ? fow : null);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Fake fog of war for the preview and the F10 panel: wraps the real policy, registered by its own type.
            var toggle = new ToggleVisibilityPolicy(visibility);
            _registry.Register(toggle);
            visibility = toggle;
            var mock = new MockVfxMatch();
            _registry.Register(mock);
            var feed = new VfxMatchFeed(match, offlineMatch, mock, () => mock.Active);
#else
            var feed = new VfxMatchFeed(match, offlineMatch);
#endif
            _registry.Register<IVfxMatchFeed>(feed);
            _registry.TryResolve<IPlayerPoseSource>(out var poses);
            var anchors = new VfxAnchorRegistry(feed, poses);
            _registry.Register(anchors);
            _registry.Register<IVfxTargetLocator>(anchors);

            var stage = Attach<VfxStage>().Configure(set.Catalog, tuning);
            _registry.Register(stage);
            _registry.TryResolve<IPlayerSettings>(out var settings);
            var director = new VfxDirector(bus, feed, anchors, visibility, stage, set.Catalog.BuildRules(), tuning, settings, _log, balance);
            director.BindCombat(_registry.Resolve<OnlineCombatSession>(), offlineWorld);
            stage.SceneChanged += director.ResetAll;
            _registry.Register(director);
            ticks.Add(director);

            _registry.Register(new LocalEffectStatusSource(_registry.Resolve<IEffectState>(), bus, feed));
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var fakeThrowables = new FakeThrowableEventSource(offlineWorld.ThrowableRules, anchors);
            _registry.Register(fakeThrowables);
            ticks.Add(fakeThrowables);
            _registry.Register(new ThrowableVfxSource(bus, offlineWorld, _registry.Resolve<OnlineThrowableSession>(), offlineWorld, fakeThrowables));
#else
            _registry.Register(new ThrowableVfxSource(bus, offlineWorld, _registry.Resolve<OnlineThrowableSession>(), offlineWorld));
#endif

            Attach<CameraShakeApplier>().Bind(director.Shake, balance, _registry.Resolve<IClientRuntimeData>());
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var scenarios = new VfxScenarios(bus, director, mock, toggle, fakeThrowables, anchors);
            _registry.Register(scenarios);
            ticks.Add(scenarios);
            Attach<VfxDebugPanel>();
#endif
        }
    }
}
