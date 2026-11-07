using System;
using System.Collections.Generic;
using JungleMaze.App.MatchUi;
using JungleMaze.App.Ports;
using JungleMaze.App.Vfx;
using JungleMaze.App.Visibility.Mocks;
using JungleMaze.Core.Events;
using JungleMaze.Core.Logging;
using JungleMaze.Domain.CombatFeedback;
using JungleMaze.Domain.MatchUi;
using JungleMaze.Domain.Vfx;

namespace JungleMaze.Tests.Vfx
{
    /// <summary>Counts what the director asks the stage to draw (VFX-01 plan 4.7).</summary>
    internal sealed class FakeVfxOutput : IVfxOutput
    {
        private readonly Dictionary<int, VfxCue> _live = new Dictionary<int, VfxCue>();
        private int _next;

        public readonly List<(VfxCue Cue, VfxSpawn Spawn, int Handle)> Spawns = new List<(VfxCue, VfxSpawn, int)>();
        public readonly List<int> Releases = new List<int>();
        public readonly List<(byte NetId, float Seconds)> Flashes = new List<(byte, float)>();
        public readonly List<byte> CancelledFlashes = new List<byte>();
        public int ReleaseAllCalls;
        /// <summary>Cues that fail to spawn (no prefab).</summary>
        public readonly HashSet<VfxCue> Missing = new HashSet<VfxCue>();

        public int ActiveCount => _live.Count;

        public int Spawn(VfxCue cue, in VfxSpawn spawn)
        {
            if (Missing.Contains(cue)) return 0;
            var handle = ++_next;
            _live[handle] = cue;
            Spawns.Add((cue, spawn, handle));
            return handle;
        }

        public void Release(int handle)
        {
            if (_live.Remove(handle)) Releases.Add(handle);
        }

        public void ReleaseAll()
        {
            ReleaseAllCalls++;
            _live.Clear();
        }

        public void Flash(byte netId, float seconds) => Flashes.Add((netId, seconds));
        public void CancelFlash(byte netId) => CancelledFlashes.Add(netId);

        public bool IsLive(int handle) => _live.ContainsKey(handle);

        public int Count(VfxCue cue)
        {
            var n = 0;
            foreach (var s in Spawns) if (s.Cue == cue) n++;
            return n;
        }

        public int Live(VfxCue cue)
        {
            var n = 0;
            foreach (var c in _live.Values) if (c == cue) n++;
            return n;
        }

        public int LastHandle(VfxCue cue)
        {
            for (var i = Spawns.Count - 1; i >= 0; i--) if (Spawns[i].Cue == cue) return Spawns[i].Handle;
            return 0;
        }
    }

    internal sealed class FakeMatchFeed : IVfxMatchFeed
    {
        public Guid SessionKey { get; set; } = Guid.NewGuid();
        public bool InGame { get; set; } = true;
        public byte OwnNetId { get; set; }
        public bool Online { get; set; }
    }

    internal sealed class FakeLocator : IVfxTargetLocator
    {
        private readonly Dictionary<byte, (float X, float Y)> _bodies = new Dictionary<byte, (float, float)>();

        public void Put(byte netId, float x, float y) => _bodies[netId] = (x, y);
        public void Remove(byte netId) => _bodies.Remove(netId);

        public bool TryGetPosition(byte netId, out float x, out float y)
        {
            if (_bodies.TryGetValue(netId, out var p)) { x = p.X; y = p.Y; return true; }
            x = y = 0f;
            return false;
        }
    }

    internal sealed class FakeLog : ILog
    {
        public readonly List<string> Warnings = new List<string>();
        public readonly List<string> Errors = new List<string>();
        public void Info(string message) { }
        public void Warn(string message) => Warnings.Add(message);
        public void Error(string message) => Errors.Add(message);
    }

    internal sealed class FakeSettings : IPlayerSettings
    {
        public float MouseSensitivity => 1f;
        public bool ScreenShakeEnabled { get; set; } = PlayerSettingsRules.DefaultScreenShake;
        public event Action<PlayerSettingsChanged> Changed { add { } remove { } }
    }

    /// <summary>A director over fakes with the catalog rules of plan 4.12.4 (no prefab, only rules).</summary>
    internal sealed class VfxRig
    {
        public const float Dt = 1f / 60f;

        public readonly EventBus Bus = new EventBus();
        public readonly FakeMatchFeed Feed = new FakeMatchFeed();
        public readonly FakeLocator Targets = new FakeLocator();
        public readonly ToggleVisibilityPolicy Visibility = new ToggleVisibilityPolicy();
        public readonly FakeVfxOutput Output = new FakeVfxOutput();
        public readonly FakeLog Log = new FakeLog();
        public readonly FakeSettings Settings = new FakeSettings();
        public readonly VfxDirector Director;

        public VfxRig(VfxTuningValues? tuning = null, VfxRuleTable rules = null)
        {
            for (byte i = 0; i <= 10; i++) Targets.Put(i, 10f + i * 2f, 10f);
            Director = new VfxDirector(Bus, Feed, Targets, Visibility, Output, rules ?? Rules(), tuning ?? VfxTuningValues.Default,
                Settings, Log);
            Director.Initialize();
            Director.Tick(Dt);
        }

        public static VfxRuleTable Rules()
        {
            var t = new VfxRuleTable();
            t.Set(VfxCue.HitSpark, new VfxRule(VfxPriority.Low, .4f, false));
            t.Set(VfxCue.DamageNumber, new VfxRule(VfxPriority.Low, 0f, false));
            t.Set(VfxCue.AttackPuff, new VfxRule(VfxPriority.Low, .25f, false));
            t.Set(VfxCue.FuseWarning, new VfxRule(VfxPriority.Critical, 0f, true));
            foreach (var cue in new[] { VfxCue.AuraStun, VfxCue.AuraReverse, VfxCue.AuraCcImmune, VfxCue.AuraInvisible, VfxCue.AuraInvulnerable, VfxCue.AuraPoison })
                t.Set(cue, new VfxRule(VfxPriority.Medium, 0f, true));
            t.MapStatus(StatusKind.Stun, VfxCue.AuraStun);
            t.MapStatus(StatusKind.Reverse, VfxCue.AuraReverse);
            t.MapStatus(StatusKind.CcImmune, VfxCue.AuraCcImmune, 5);
            t.MapStatus(StatusKind.Invulnerable, VfxCue.AuraInvulnerable, 4);
            t.MapStatus(StatusKind.Invisible, VfxCue.AuraInvisible);
            t.MapStatus(StatusKind.Poison, VfxCue.AuraPoison);
            t.MapStatus(StatusKind.Root, VfxCue.None);
            t.MapStatus(StatusKind.Slow, VfxCue.None);
            t.MapStatus(StatusKind.Adrenaline, VfxCue.None);
            return t;
        }

        public void Tick(float seconds)
        {
            for (var t = 0f; t < seconds - 1e-4f; t += Dt) Director.Tick(Dt);
        }

        public void Hit(ulong key, byte target, int damage = 10, byte source = 1) =>
            Bus.Publish(new HitConfirmedEvent(key, source, target, damage, 0f, 0f));

        public void Status(ulong key, byte netId, StatusKind kind, float seconds, bool applied = true) =>
            Bus.Publish(new StatusEvent(key, netId, kind, applied, seconds, seconds));

        public void Fuse(ulong key, float x, float y, float size = 3f, float seconds = 1.5f, byte owner = 1) =>
            Bus.Publish(new ExplosionEvent(key, "BANANA", ExplosionPhase.Fuse, x, y, size, size, seconds, owner));

        public void Detonate(ulong key, float x, float y, float size = 3f, byte owner = 1) =>
            Bus.Publish(new ExplosionEvent(key, "BANANA", ExplosionPhase.Detonate, x, y, size, size, 0f, owner));
    }
}
