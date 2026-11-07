using System;
using JungleBackend.Protocol.Gameplay;
using JungleMaze.App.Audio;
using JungleMaze.App.Combat;
using JungleMaze.App.MatchUi;
using JungleMaze.App.Ports;
using JungleMaze.App.World;
using JungleMaze.Core.DI;
using JungleMaze.Core.Events;
using JungleMaze.Core.Logging;
using JungleMaze.Core.Ticking;
using JungleMaze.Domain.CombatFeedback;
using JungleMaze.Domain.MatchUi;
using JungleMaze.Domain.Vfx;

namespace JungleMaze.App.Vfx
{
    /// <summary>
    /// The one place that decides which VFX play (VFX-01 plan 4.1 and 4.12): dedupes repeated events, reconciles
    /// predicted attacks with the server answer, asks the visibility seam (again every tick for live instances), keeps
    /// logical state for statuses and fuse warnings, enforces the instance budget, and resets everything when the
    /// match/room changes. Blasts, smoke and the safe zone are drawn elsewhere (<c>ThrowableView</c>,
    /// <c>ZoneOverlayView</c>). Draws only through <see cref="IVfxOutput"/>; never writes HUD, HP or gameplay state.
    /// </summary>
    public sealed class VfxDirector : IInitializable, ITickable, IDisposable
    {
        private const byte KeyAttack = 3, KeyHit = 11, KeyExplosion = 12, KeyStatus = 13;
        private const int MaxFuses = 32, MaxFlash = 16;
        private const float DefaultShortLifetime = 1f, DefaultFuseSeconds = 1.5f;

        private struct Live
        {
            public int Handle;
            public float Remaining;
            public byte Anchor;          // VfxSpawn.NoAnchor = stationary
            public byte SeamNetId;       // PositionOnly for area effects
            public bool Exempt;          // own effects are never hidden
            public float X, Y;
        }

        private struct Fuse
        {
            public ulong Key;
            public float X, Y, W, H;
            public float Remaining;
            public byte Owner;
            public int Handle;
        }

        private struct FlashEntry
        {
            public byte NetId;
            public float Remaining;
        }

        private readonly IEventBus _bus;
        private readonly IVfxMatchFeed _feed;
        private readonly IVfxTargetLocator _targets;
        private readonly IVisibilityPolicy _visibility;
        private readonly IVfxOutput _output;
        private readonly VfxRuleTable _rules;
        private readonly VfxTuningValues _tuning;
        private readonly IPlayerSettings _settings;
        private readonly IBalanceConfigProvider _balance;
        private readonly ILog _log;

        private readonly SfxDeduper _dedupe;
        private readonly AttackVfxLedger _ledger = new AttackVfxLedger();
        private readonly StatusAuraTable _auras;
        private readonly VfxBudget _budget;
        private readonly Live[] _live;
        private int _liveCount;
        private readonly Fuse[] _fuses = new Fuse[MaxFuses];
        private int _fuseCount;
        private readonly FlashEntry[] _flashes = new FlashEntry[MaxFlash];
        private int _flashCount;
        private readonly bool[] _warnedStatus = new bool[256];
        private readonly Action<HitConfirmedEvent> _onHit;
        private readonly Action<ExplosionEvent> _onExplosion;
        private readonly Action<StatusEvent> _onStatus;

        private OnlineCombatSession _online;
        private OfflineWorldSession _offline;
        private Guid _session;
        private bool _inGame, _subscribed, _warnedBudget;
        private float _time;

        public VfxDirector(IEventBus bus, IVfxMatchFeed feed, IVfxTargetLocator targets, IVisibilityPolicy visibility,
            IVfxOutput output, VfxRuleTable rules, VfxTuningValues tuning, IPlayerSettings settings, ILog log,
            IBalanceConfigProvider balance = null)
        {
            _bus = bus;
            _feed = feed ?? throw new ArgumentNullException(nameof(feed));
            _targets = targets;
            _visibility = visibility;
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _rules = rules ?? new VfxRuleTable();
            _tuning = tuning;
            _settings = settings;
            _balance = balance;
            _log = log ?? NullLog.Instance;
            _dedupe = new SfxDeduper(tuning.DedupeSeconds, tuning.DedupeCapacity);
            var budget = tuning.Budget > 0 ? tuning.Budget : 64;
            _budget = new VfxBudget(budget);
            _live = new Live[budget];
            _auras = new StatusAuraTable(budget);
            Shake = new ScreenShakeModel(tuning.ShakeMaxCells, tuning.ShakeSeconds);
            _onHit = OnHit;
            _onExplosion = OnExplosion;
            _onStatus = OnStatus;
        }

        public ScreenShakeModel Shake { get; }
        /// <summary>Instances counted by the budget (short-lived, auras, fuses).</summary>
        public int LiveCount => _budget.Count;
        public int BudgetCapacity => _budget.Capacity;
        public int BudgetRefused => _budget.Refused;
        public int AuraCount => _auras.Count;
        public int FuseCount => _fuseCount;
        public int Resets { get; private set; }

        public void Initialize()
        {
            if (_subscribed || _bus == null) return;
            _bus.Subscribe(_onHit);
            _bus.Subscribe(_onExplosion);
            _bus.Subscribe(_onStatus);
            _subscribed = true;
        }

        public void Dispose()
        {
            BindCombat(null, null);
            if (_subscribed)
            {
                _bus.Unsubscribe(_onHit);
                _bus.Unsubscribe(_onExplosion);
                _bus.Unsubscribe(_onStatus);
            }
            _subscribed = false;
        }

        /// <summary>Listens to attack results; online events only count while offline play is not active (as AUD-01).</summary>
        public void BindCombat(OnlineCombatSession online, OfflineWorldSession offline)
        {
            if (_online != null) _online.Changed -= OnOnlineCombat;
            if (_offline != null) _offline.CombatChanged -= OnOfflineCombat;
            _online = online;
            _offline = offline;
            if (_online != null) _online.Changed += OnOnlineCombat;
            if (_offline != null) _offline.CombatChanged += OnOfflineCombat;
        }

        private void OnOnlineCombat(CombatEvent value) { if (_offline?.IsActive != true) OnCombat(value); }
        private void OnOfflineCombat(CombatEvent value) { if (_offline?.IsActive == true) OnCombat(value); }

        /// <summary>Attack effects: own attacks are predicted and reconciled by RequestSeq, others' follow the server.</summary>
        public void OnCombat(CombatEvent e)
        {
            if (!EnsureSession()) return;
            var own = e.NetId == _feed.OwnNetId;
            switch (e.Kind)
            {
                case CombatEventKind.AttackPredicted:
                    if (!own || e.Outcome != CombatOutcome.Accepted || !_ledger.TryPredict(e.NetId, e.RequestSeq, _time)) return;
                    if (_targets == null || !_targets.TryGetPosition(e.NetId, out var ox, out var oy)) return;
                    var handle = SpawnShort(VfxCue.AttackPuff, new VfxSpawn(ox, oy, anchorNetId: e.NetId), e.NetId, true);
                    if (handle != 0) _ledger.AttachHandle(e.NetId, e.RequestSeq, handle, _time);
                    return;
                case CombatEventKind.AttackConfirmed:
                    if (own) _ledger.Confirm(e.NetId, e.RequestSeq, _time);
                    return;
                case CombatEventKind.AttackRejected:
                    if (!own) return;
                    var rejected = _ledger.Reject(e.NetId, e.RequestSeq, _time);
                    if (rejected != 0) ReleaseHandle(rejected);
                    return;
                case CombatEventKind.RemoteAttackConfirmed:
                    if (own || e.Outcome != CombatOutcome.Accepted) return;
                    if (_dedupe.IsDuplicate(SfxDeduper.Key(KeyAttack, e.NetId, e.RequestSeq), _time)) return;
                    if (_targets == null || !_targets.TryGetPosition(e.NetId, out var rx, out var ry)) return;
                    if (!Visible(rx, ry, e.NetId)) return;
                    SpawnShort(VfxCue.AttackPuff, new VfxSpawn(rx, ry, anchorNetId: e.NetId), e.NetId, false);
                    return;
            }
        }

        private void OnHit(HitConfirmedEvent e)
        {
            // No damage (immortal target, smoke): no number and no flash (AC12).
            if (e.Damage <= 0 || !EnsureSession()) return;
            if (_dedupe.IsDuplicate(SfxDeduper.Key(KeyHit, (uint)e.EventKey, (uint)(e.EventKey >> 32)), _time)) return;
            var target = e.TargetNetId;
            // Dead or despawned target: nothing to draw on (AC12).
            if (_targets == null || !_targets.TryGetPosition(target, out var x, out var y)) return;
            var own = _feed.OwnNetId;
            var exempt = target == own;
            if (!exempt && !Visible(x, y, target)) return;
            var tint = target == own ? VfxTints.NumberTaken : e.SourceNetId == own ? VfxTints.NumberDealt : VfxTints.NumberOther;
            SpawnShort(VfxCue.HitSpark, new VfxSpawn(x, y, anchorNetId: target), target, exempt);
            SpawnShort(VfxCue.DamageNumber, new VfxSpawn(x, y, anchorNetId: target, number: e.Damage, tint: tint), target, exempt);
            StartFlash(target);
            if (target == own) AddShake(_tuning.HitShakeCells);
        }

        private void OnExplosion(ExplosionEvent e)
        {
            if (!EnsureSession()) return;
            if (_dedupe.IsDuplicate(SfxDeduper.Key(KeyExplosion, (uint)e.EventKey, (uint)(e.EventKey >> 32), (uint)e.Phase), _time)) return;
            var exempt = e.OwnerNetId == _feed.OwnNetId;

            if (e.Phase == ExplosionPhase.Fuse)
            {
                var w = e.AreaWidth;
                var h = e.AreaHeight;
                WeaponArea(e.WeaponId, ref w, ref h);
                var seconds = e.SecondsRemaining;
                if (!(seconds > 0f) && _balance != null && _balance.TryGetWeapon(e.WeaponId, out var weapon) && weapon.Stats.FuseSeconds > 0f)
                    seconds = weapon.Stats.FuseSeconds.Value;
                if (!(seconds > 0f)) seconds = DefaultFuseSeconds;
                AddFuse(e.EventKey, e.CenterX, e.CenterY, w, h, seconds, e.OwnerNetId);
                return;
            }

            // The blast itself belongs to ThrowableView: the warning goes in this call, then the camera may shake.
            RemoveFuse(e.EventKey);
            if (!exempt && !Visible(e.CenterX, e.CenterY, VisibilityQuery.PositionOnly)) return;
            if (_targets != null && _targets.TryGetPosition(_feed.OwnNetId, out var px, out var py))
            {
                var dx = px - e.CenterX;
                var dy = py - e.CenterY;
                if (dx * dx + dy * dy <= _tuning.ShakeRadiusCells * _tuning.ShakeRadiusCells) AddShake(_tuning.ShakeMaxCells);
            }
        }

        private void OnStatus(StatusEvent e)
        {
            if (!EnsureSession()) return;
            if (e.EventKey != 0 && _dedupe.IsDuplicate(SfxDeduper.Key(KeyStatus, (uint)e.EventKey, (uint)(e.EventKey >> 32), e.Applied ? 1u : 0u), _time)) return;
            if (!_rules.TryGetStatus(e.Kind, out var cue, out var tint))
            {
                if (!_warnedStatus[(byte)e.Kind])
                {
                    _warnedStatus[(byte)e.Kind] = true;
                    _log.Warn($"[VFX] No aura mapped for status {e.Kind}; it is not drawn.");
                }
                return;
            }
            if (cue == VfxCue.None) return; // deliberate "no visual" (Slow, Adrenaline)

            if (!e.Applied)
            {
                var index = _auras.IndexOf(e.NetId, e.Kind);
                if (index < 0) return;
                var handle = _auras.HandleAt(index);
                _auras.RemoveAt(index);
                if (handle != 0) ReleaseHandle(handle);
                return;
            }

            // A crowd-control aura never shows on a body that is immune to crowd control (plan 4.12.3, R16).
            if (IsCrowdControl(e.Kind) && _auras.IndexOf(e.NetId, StatusKind.CcImmune) >= 0) return;
            var seconds = e.RemainingSeconds > 0f ? e.RemainingSeconds : e.DurationSeconds;
            var i = _auras.Apply(e.NetId, e.Kind, cue, tint, seconds, out var added);
            if (i < 0 || !added) return; // refresh only resets the timer
            TrySpawnAura(i);
        }

        public void Tick(float deltaTime)
        {
            if (!(deltaTime > 0f) || float.IsInfinity(deltaTime)) deltaTime = 0f;
            _time += deltaTime;
            if (!EnsureSession()) { Shake.Clear(); return; }

            TickLive(deltaTime);
            TickAuras(deltaTime);
            TickFuses(deltaTime);
            TickFlashes(deltaTime);

            var shakeOn = _settings?.ScreenShakeEnabled ?? PlayerSettingsRules.DefaultScreenShake;
            if (shakeOn) Shake.Tick(deltaTime);
            else Shake.Clear();
        }

        /// <summary>Releases every instance and forgets all logical state and dedupe keys (room/match change).</summary>
        public void ResetAll()
        {
            _output.ReleaseAll();
            for (var i = 0; i < _flashCount; i++) _output.CancelFlash(_flashes[i].NetId);
            _flashCount = 0;
            _liveCount = 0;
            _fuseCount = 0;
            for (var i = 0; i < _fuses.Length; i++) _fuses[i] = default;
            _auras.Clear();
            _ledger.Clear();
            _budget.Clear();
            _dedupe.Clear();
            Shake.Clear();
            _warnedBudget = false;
            Resets++;
        }

        /// <summary>Resets on a new session key or InGame change; returns whether effects may play now.</summary>
        private bool EnsureSession()
        {
            var key = _feed.SessionKey;
            var inGame = _feed.InGame && key != Guid.Empty;
            if (key != _session || inGame != _inGame)
            {
                ResetAll();
                _session = key;
                _inGame = inGame;
            }
            return _inGame;
        }

        private void WeaponArea(string weaponId, ref float w, ref float h)
        {
            if (w > 0f && h > 0f) return;
            if (_balance != null && _balance.TryGetWeapon(weaponId, out var weapon))
            {
                if (!(w > 0f) && weapon.Stats.AreaWidth > 0f) w = weapon.Stats.AreaWidth.Value;
                if (!(h > 0f) && weapon.Stats.AreaHeight > 0f) h = weapon.Stats.AreaHeight.Value;
            }
            if (!(w > 0f)) w = h > 0f ? h : 1f;
            if (!(h > 0f)) h = w;
        }

        private bool Visible(float x, float y, byte netId) => _visibility == null || _visibility.IsVisible(x, y, netId);

        private void AddShake(float cells)
        {
            if (_settings != null && !_settings.ScreenShakeEnabled) return;
            Shake.Add(cells, _tuning.ShakeSeconds);
        }

        private int Reserve(VfxCue cue, out VfxRule rule)
        {
            if (!_rules.TryGet(cue, out rule)) return -1;
            if (!_budget.TryReserve(rule.Priority, out var evict))
            {
                if (!_warnedBudget)
                {
                    _warnedBudget = true;
                    _log.Warn($"[VFX] Budget of {_budget.Capacity} instances full; {cue} skipped.");
                }
                return -1;
            }
            if (evict != 0) ReleaseHandle(evict);
            return 0;
        }

        private int SpawnRaw(VfxCue cue, in VfxSpawn spawn)
        {
            if (Reserve(cue, out var rule) < 0) return 0;
            var handle = _output.Spawn(cue, spawn);
            if (handle != 0) _budget.Add(handle, rule.Priority);
            return handle;
        }

        private int SpawnShort(VfxCue cue, in VfxSpawn spawn, byte seamNetId, bool exempt)
        {
            if (!_rules.TryGet(cue, out var rule)) return 0;
            var handle = SpawnRaw(cue, spawn);
            if (handle == 0) return 0;
            if (_liveCount >= _live.Length) { ReleaseHandle(handle); return 0; }
            _live[_liveCount++] = new Live
            {
                Handle = handle,
                Remaining = rule.LifetimeSeconds > 0f ? rule.LifetimeSeconds : cue == VfxCue.DamageNumber ? _tuning.NumberSeconds : DefaultShortLifetime,
                Anchor = spawn.AnchorNetId,
                SeamNetId = seamNetId,
                Exempt = exempt,
                X = spawn.X,
                Y = spawn.Y
            };
            return handle;
        }

        /// <summary>Releases one instance and clears every reference to it.</summary>
        private void ReleaseHandle(int handle)
        {
            if (handle == 0) return;
            _output.Release(handle);
            _budget.Free(handle);
            for (var i = _liveCount - 1; i >= 0; i--)
                if (_live[i].Handle == handle) { _live[i] = _live[--_liveCount]; break; }
            _auras.ForgetHandle(handle);
            for (var i = 0; i < _fuseCount; i++)
                if (_fuses[i].Handle == handle) _fuses[i].Handle = 0;
            _ledger.ForgetHandle(handle);
        }

        private void TickLive(float dt)
        {
            for (var i = _liveCount - 1; i >= 0; i--)
            {
                ref var live = ref _live[i];
                live.Remaining -= dt;
                var keep = live.Remaining > 0f;
                if (keep && live.Anchor != VfxSpawn.NoAnchor)
                {
                    // Anchor gone (dead/despawned) or hidden: nothing of that body stays on screen (AC8e).
                    keep = _targets != null && _targets.TryGetPosition(live.Anchor, out var ax, out var ay) &&
                           (live.Exempt || Visible(ax, ay, live.Anchor));
                }
                else if (keep && !live.Exempt) keep = Visible(live.X, live.Y, live.SeamNetId);
                if (!keep) ReleaseHandle(live.Handle);
            }
        }

        private void TickAuras(float dt)
        {
            _auras.Advance(dt);
            var own = _feed.OwnNetId;
            for (var i = _auras.Count - 1; i >= 0; i--)
            {
                var handle = _auras.HandleAt(i);
                if (_auras.IsExpired(i))
                {
                    _auras.RemoveAt(i);
                    if (handle != 0) ReleaseHandle(handle);
                    continue;
                }
                var netId = _auras.NetIdAt(i);
                var shown = _targets != null && _targets.TryGetPosition(netId, out var x, out var y) &&
                            (netId == own || Visible(x, y, netId));
                if (!shown && handle != 0) ReleaseHandle(handle);
                else if (shown && handle == 0) TrySpawnAura(i);
            }
        }

        private void TrySpawnAura(int index)
        {
            var netId = _auras.NetIdAt(index);
            if (_targets == null || !_targets.TryGetPosition(netId, out var x, out var y)) return;
            if (netId != _feed.OwnNetId && !Visible(x, y, netId)) return;
            var handle = SpawnRaw(_auras.CueAt(index), new VfxSpawn(x, y, anchorNetId: netId, tint: _auras.TintAt(index)));
            _auras.SetHandle(index, handle);
        }

        private static bool IsCrowdControl(StatusKind kind) =>
            kind == StatusKind.Stun || kind == StatusKind.Root || kind == StatusKind.Reverse;

        private void AddFuse(ulong key, float x, float y, float w, float h, float seconds, byte owner)
        {
            if (!_rules.TryGet(VfxCue.FuseWarning, out _)) return;
            var i = -1;
            for (var k = 0; k < _fuseCount; k++)
                if (_fuses[k].Key == key) { i = k; break; }
            if (i < 0)
            {
                if (_fuseCount >= _fuses.Length) return;
                i = _fuseCount++;
                _fuses[i] = new Fuse { Key = key, X = x, Y = y, W = w, H = h, Owner = owner };
            }
            _fuses[i].Remaining = seconds;
            if (_fuses[i].Handle == 0) TrySpawnFuse(i);
        }

        private void RemoveFuse(ulong key)
        {
            for (var i = _fuseCount - 1; i >= 0; i--)
            {
                if (_fuses[i].Key != key) continue;
                var handle = _fuses[i].Handle;
                _fuses[i] = _fuses[--_fuseCount];
                _fuses[_fuseCount] = default;
                if (handle != 0) ReleaseHandle(handle);
            }
        }

        private void TrySpawnFuse(int i)
        {
            ref var f = ref _fuses[i];
            if (f.Owner != _feed.OwnNetId && !Visible(f.X, f.Y, VisibilityQuery.PositionOnly)) return;
            f.Handle = SpawnRaw(VfxCue.FuseWarning, new VfxSpawn(f.X, f.Y, f.W, f.H, seconds: f.Remaining));
        }

        private void TickFuses(float dt)
        {
            for (var i = _fuseCount - 1; i >= 0; i--)
            {
                _fuses[i].Remaining -= dt;
                var handle = _fuses[i].Handle;
                if (_fuses[i].Remaining <= -_tuning.FuseGraceSeconds)
                {
                    // No Detonated arrived (lost, or later than the grace): the warning still ends.
                    _fuses[i] = _fuses[--_fuseCount];
                    _fuses[_fuseCount] = default;
                    if (handle != 0) ReleaseHandle(handle);
                    continue;
                }
                var shown = _fuses[i].Owner == _feed.OwnNetId || Visible(_fuses[i].X, _fuses[i].Y, VisibilityQuery.PositionOnly);
                if (!shown && handle != 0) ReleaseHandle(handle);
                // A warning hidden when its timer ran out is not drawn again during the grace.
                else if (shown && handle == 0 && _fuses[i].Remaining > 0f) TrySpawnFuse(i);
            }
        }

        private void StartFlash(byte netId)
        {
            for (var i = 0; i < _flashCount; i++)
                if (_flashes[i].NetId == netId)
                {
                    _flashes[i].Remaining = _tuning.FlashSeconds;
                    _output.Flash(netId, _tuning.FlashSeconds);
                    return;
                }
            if (_flashCount >= _flashes.Length) return;
            _flashes[_flashCount++] = new FlashEntry { NetId = netId, Remaining = _tuning.FlashSeconds };
            _output.Flash(netId, _tuning.FlashSeconds);
        }

        private void TickFlashes(float dt)
        {
            var own = _feed.OwnNetId;
            for (var i = _flashCount - 1; i >= 0; i--)
            {
                _flashes[i].Remaining -= dt;
                var netId = _flashes[i].NetId;
                var expired = _flashes[i].Remaining <= 0f;
                var hidden = !expired && netId != own &&
                             (_targets == null || !_targets.TryGetPosition(netId, out var x, out var y) || !Visible(x, y, netId));
                if (!expired && !hidden) continue;
                if (hidden) _output.CancelFlash(netId);
                _flashes[i] = _flashes[--_flashCount];
            }
        }
    }
}
