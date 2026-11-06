#nullable disable
using System;
using System.Collections.Generic;

namespace JungleMaze.World.Throwables
{
    /// <summary>Crowd control the throwables ask the effect owner (Game-10) to apply.</summary>
    public enum ThrowableEffect : byte { Stun = 1, Root = 2, Reverse = 3 }

    public enum ThrowableResult : byte { Ok = 0, Duplicate = 1, Dead = 2, Controlled = 3, WrongItem = 4, Cooldown = 5, Missing = 6, Invalid = 7 }

    /// <summary>What happened, for presentation and network broadcast. Values are wire codes; append only.</summary>
    public enum ThrowableEventKind : byte
    {
        Thrown = 1,
        Detonated = 2,
        TrapArmed = 3,
        TrapTriggered = 4,
        TrapExpired = 5,
        SmokeStarted = 6,
        SmokeEnded = 7,
        RopePulledTarget = 8,
        RopePulledSelf = 9,
        RopeMissed = 10,
        BellRung = 11,
        BatSwing = 12,
        BatHit = 13,
        Hit = 14
    }

    /// <summary>
    /// One confirmed throwable event. <see cref="Id"/> names a pending blast, trap or smoke cloud so later events
    /// refer to the same instance. From/To are the throw origin and landing, a pull or knockback path, or a centre.
    /// </summary>
    public struct ThrowableEvent
    {
        public ThrowableEventKind Kind;
        public ThrowableKind Item;
        public byte Actor;
        /// <summary>Affected player, or 255 when none.</summary>
        public byte Target;
        public ushort Id;
        public float FromX, FromY, ToX, ToY;
        /// <summary>Fuse, trap, smoke or crowd-control duration in seconds.</summary>
        public float Seconds;
        /// <summary>Square area side in cells (blast, smoke, bell).</summary>
        public float Size;
        /// <summary>Damage dealt by a <see cref="ThrowableEventKind.Hit"/> or <see cref="ThrowableEventKind.BatHit"/>.</summary>
        public short Amount;
        /// <summary>Bit 0: the target was stunned (bat impact, banana trap).</summary>
        public byte Flags;

        public const byte NoTarget = 255;
        public const byte StunnedFlag = 1;
    }

    /// <summary>Position and state of one player as seen by <see cref="ThrowableService"/>.</summary>
    public struct ThrowActor
    {
        public byte Id;
        public float X, Y, Radius;
        public bool Alive;
        public ThrowActor(byte id, float x, float y, float radius, bool alive = true) { Id = id; X = x; Y = y; Radius = radius; Alive = alive; }
    }

    /// <summary>
    /// What the throwable rules need from the match. The server implements it over its world runtime, the offline
    /// client over its local world. Damage and effects are requests: HP stays with WPN-02 and effects with Game-10.
    /// </summary>
    public interface IThrowableWorld
    {
        int ActorCount { get; }
        ThrowActor ActorAt(int index);
        bool TryGetActor(byte id, out ThrowActor actor);
        /// <summary>The throwable the player is holding, or None.</summary>
        ThrowableKind HeldThrowable(byte actor);
        /// <summary>False while stunned. Root and reverse still allow attacks.</summary>
        bool CanAct(byte actor);
        /// <summary>Spends one thrown item from the held stack, or one charge of the held special weapon.</summary>
        bool TryConsume(byte actor, ThrowableRule rule);
        /// <summary>Fraction 0..1 of the segment travelled before it enters blocking geometry (1 = clear).</summary>
        float CoverFraction(float x0, float y0, float x1, float y1);
        bool IsWalkable(float x, float y, float radius);
        /// <summary>Requests damage; returns the HP actually removed (after armor, immunity or a life-save).</summary>
        int Damage(byte attacker, byte target, int amount);
        void ApplyEffect(byte target, ThrowableEffect effect, double seconds);
        void Move(byte actor, float x, float y);
    }

    /// <summary>
    /// Authoritative rules for WPN-03 (banana, smoke, meteorite fragment, crystal, seed, rope, bell, sahur bat).
    /// Requests carry a per-player monotonic id, so a repeated or older request never spends a charge, arms a trap,
    /// stuns or damages twice. Single simulation thread only; no Unity types.
    /// </summary>
    public sealed class ThrowableService
    {
        /// <summary>Blasts and clouds stop this far in front of the cover they hit.</summary>
        public const float CoverStandOff = .3f;
        private const float MinThrow = .5f;
        private const float SlideStep = .1f;
        /// <summary>A fresh banana trap arms this long after the blast, so blast victims can step off it.</summary>
        public const double TrapArmSeconds = .5;

        private readonly ThrowableRules _rules;
        private readonly IThrowableWorld _world;
        private readonly Dictionary<byte, ActorState> _actors = new Dictionary<byte, ActorState>();
        private readonly List<Pending> _pending = new List<Pending>();
        private readonly List<Trap> _traps = new List<Trap>();
        private readonly List<Cloud> _smokes = new List<Cloud>();
        private ushort _nextId;

        private sealed class ActorState
        {
            public uint LastRequest;
            public readonly double[] ReadyAt = new double[(int)ThrowableKind.Bat + 1];
        }

        private struct Pending { public ushort Id; public ThrowableRule Rule; public byte Actor; public float X, Y; public double At; }
        private struct Trap { public ushort Id; public ThrowableRule Rule; public byte Actor; public float X, Y; public double ArmedAt, Until; }
        private struct Cloud { public ushort Id; public float X, Y, Half; public double Until; }

        public ThrowableService(ThrowableRules rules, IThrowableWorld world)
        {
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _world = world ?? throw new ArgumentNullException(nameof(world));
        }

        public event Action<ThrowableEvent> Raised;

        public int PendingCount => _pending.Count;
        public int TrapCount => _traps.Count;
        public int SmokeCount => _smokes.Count;
        public ThrowableRules Rules => _rules;

        /// <summary>Clears every throw, trap, cloud, cooldown and request high-water mark (new match).</summary>
        public void Reset()
        {
            _actors.Clear(); _pending.Clear(); _traps.Clear(); _smokes.Clear(); _nextId = 0;
        }

        /// <summary>True when the point is inside an active smoke cloud (FOW-01 decides what that hides).</summary>
        public bool IsInSmoke(float x, float y)
        {
            foreach (var s in _smokes)
                if (Math.Abs(x - s.X) <= s.Half && Math.Abs(y - s.Y) <= s.Half) return true;
            return false;
        }

        /// <summary>
        /// Uses the held throwable. Thrown weapons fly toward <paramref name="distance"/> along the aim (clamped to range,
        /// stopped by cover) and detonate after the fuse; special weapons act at once.
        /// </summary>
        public ThrowableResult Use(byte actor, uint requestId, ThrowableKind kind, float aimDegrees, float distance, double now)
        {
            if (requestId == 0 || !Finite(aimDegrees) || !Finite(distance) || !_rules.TryGet(kind, out var rule)) return ThrowableResult.Invalid;
            var state = State(actor);
            if (requestId <= state.LastRequest) return ThrowableResult.Duplicate;
            state.LastRequest = requestId;
            if (!_world.TryGetActor(actor, out var self) || !self.Alive) return ThrowableResult.Dead;
            if (!_world.CanAct(actor)) return ThrowableResult.Controlled;
            if (_world.HeldThrowable(actor) != kind) return ThrowableResult.WrongItem;
            if (now < state.ReadyAt[(int)kind]) return ThrowableResult.Cooldown;
            if (!_world.TryConsume(actor, rule)) return ThrowableResult.Missing;
            state.ReadyAt[(int)kind] = now + rule.CooldownSeconds;

            var radians = aimDegrees * Math.PI / 180.0;
            var dx = (float)Math.Cos(radians);
            var dy = (float)Math.Sin(radians);
            switch (kind)
            {
                case ThrowableKind.Rope: Rope(self, rule, dx, dy); break;
                case ThrowableKind.Bell: Bell(self, rule); break;
                case ThrowableKind.Bat: Bat(self, rule, dx, dy); break;
                default: Throw(self, rule, dx, dy, distance, now); break;
            }
            return ThrowableResult.Ok;
        }

        /// <summary>Detonates due throws, triggers and expires traps, and ends smoke clouds.</summary>
        public void Tick(double now)
        {
            for (var i = 0; i < _pending.Count; i++)
            {
                if (_pending[i].At > now) continue;
                var due = _pending[i];
                _pending.RemoveAt(i--);
                Detonate(due, now);
            }
            for (var i = 0; i < _traps.Count; i++)
            {
                var trap = _traps[i];
                if (trap.Until <= now)
                {
                    _traps.RemoveAt(i--);
                    Raise(new ThrowableEvent { Kind = ThrowableEventKind.TrapExpired, Item = trap.Rule.Kind, Actor = trap.Actor, Target = ThrowableEvent.NoTarget, Id = trap.Id, FromX = trap.X, FromY = trap.Y, ToX = trap.X, ToY = trap.Y });
                    continue;
                }
                if (trap.ArmedAt > now || !TryFindTrapVictim(trap, out var victim)) continue;
                _traps.RemoveAt(i--);
                _world.ApplyEffect(victim.Id, ThrowableEffect.Stun, trap.Rule.StunSeconds);
                Raise(new ThrowableEvent { Kind = ThrowableEventKind.TrapTriggered, Item = trap.Rule.Kind, Actor = trap.Actor, Target = victim.Id, Id = trap.Id, FromX = trap.X, FromY = trap.Y, ToX = victim.X, ToY = victim.Y, Seconds = (float)trap.Rule.StunSeconds, Flags = ThrowableEvent.StunnedFlag });
            }
            for (var i = 0; i < _smokes.Count; i++)
            {
                if (_smokes[i].Until > now) continue;
                var cloud = _smokes[i];
                _smokes.RemoveAt(i--);
                Raise(new ThrowableEvent { Kind = ThrowableEventKind.SmokeEnded, Item = ThrowableKind.Smoke, Actor = ThrowableEvent.NoTarget, Target = ThrowableEvent.NoTarget, Id = cloud.Id, FromX = cloud.X, FromY = cloud.Y, ToX = cloud.X, ToY = cloud.Y, Size = cloud.Half * 2 });
            }
        }

        private void Throw(ThrowActor self, ThrowableRule rule, float dx, float dy, float distance, double now)
        {
            var reach = Math.Max(MinThrow, Math.Min(rule.Range, distance));
            var endX = self.X + dx * reach;
            var endY = self.Y + dy * reach;
            var clear = _world.CoverFraction(self.X, self.Y, endX, endY);
            if (clear < 1f)
            {
                var stop = Math.Max(0f, reach * clear - CoverStandOff);
                endX = self.X + dx * stop;
                endY = self.Y + dy * stop;
            }
            var id = NextId();
            _pending.Add(new Pending { Id = id, Rule = rule, Actor = self.Id, X = endX, Y = endY, At = now + rule.FuseSeconds });
            Raise(new ThrowableEvent { Kind = ThrowableEventKind.Thrown, Item = rule.Kind, Actor = self.Id, Target = ThrowableEvent.NoTarget, Id = id, FromX = self.X, FromY = self.Y, ToX = endX, ToY = endY, Seconds = (float)rule.FuseSeconds, Size = rule.AreaWidth });
        }

        private void Detonate(Pending blast, double now)
        {
            var rule = blast.Rule;
            Raise(new ThrowableEvent { Kind = ThrowableEventKind.Detonated, Item = rule.Kind, Actor = blast.Actor, Target = ThrowableEvent.NoTarget, Id = blast.Id, FromX = blast.X, FromY = blast.Y, ToX = blast.X, ToY = blast.Y, Size = rule.AreaWidth });
            if (rule.Kind == ThrowableKind.Smoke)
            {
                _smokes.Add(new Cloud { Id = blast.Id, X = blast.X, Y = blast.Y, Half = rule.AreaWidth / 2f, Until = now + rule.SmokeSeconds });
                Raise(new ThrowableEvent { Kind = ThrowableEventKind.SmokeStarted, Item = rule.Kind, Actor = blast.Actor, Target = ThrowableEvent.NoTarget, Id = blast.Id, FromX = blast.X, FromY = blast.Y, ToX = blast.X, ToY = blast.Y, Seconds = (float)rule.SmokeSeconds, Size = rule.AreaWidth });
                return;
            }
            var halfW = rule.AreaWidth / 2f;
            var halfH = rule.AreaHeight / 2f;
            for (var i = 0; i < _world.ActorCount; i++)
            {
                var target = _world.ActorAt(i);
                // The thrower is not hurt by their own throw (documented assumption, WPN-03 open decision).
                if (!target.Alive || target.Id == blast.Actor) continue;
                if (!OverlapsSquare(target, blast.X, blast.Y, halfW, halfH)) continue;
                if (_world.CoverFraction(blast.X, blast.Y, target.X, target.Y) < 1f) continue;
                var dealt = rule.Damage > 0 ? _world.Damage(blast.Actor, target.Id, rule.Damage) : 0;
                if (rule.RootSeconds > 0) _world.ApplyEffect(target.Id, ThrowableEffect.Root, rule.RootSeconds);
                Raise(new ThrowableEvent { Kind = ThrowableEventKind.Hit, Item = rule.Kind, Actor = blast.Actor, Target = target.Id, Id = blast.Id, FromX = blast.X, FromY = blast.Y, ToX = target.X, ToY = target.Y, Amount = (short)dealt, Seconds = (float)rule.RootSeconds });
            }
            if (rule.Kind == ThrowableKind.Banana && rule.TrapSeconds > 0)
            {
                _traps.Add(new Trap { Id = blast.Id, Rule = rule, Actor = blast.Actor, X = blast.X, Y = blast.Y, ArmedAt = now + TrapArmSeconds, Until = now + rule.TrapSeconds });
                Raise(new ThrowableEvent { Kind = ThrowableEventKind.TrapArmed, Item = rule.Kind, Actor = blast.Actor, Target = ThrowableEvent.NoTarget, Id = blast.Id, FromX = blast.X, FromY = blast.Y, ToX = blast.X, ToY = blast.Y, Seconds = (float)rule.TrapSeconds, Size = rule.TrapRadius * 2 });
            }
        }

        private void Rope(ThrowActor self, ThrowableRule rule, float dx, float dy)
        {
            var endX = self.X + dx * rule.Range;
            var endY = self.Y + dy * rule.Range;
            var coverDistance = _world.CoverFraction(self.X, self.Y, endX, endY) * rule.Range;
            var hitDistance = float.MaxValue;
            var hit = default(ThrowActor);
            for (var i = 0; i < _world.ActorCount; i++)
            {
                var target = _world.ActorAt(i);
                if (!target.Alive || target.Id == self.Id) continue;
                if (RayHitsCircle(self.X, self.Y, dx, dy, rule.Range, target.X, target.Y, target.Radius, out var d) && d < hitDistance)
                { hitDistance = d; hit = target; }
            }
            if (hitDistance <= coverDistance)
            {
                var gap = self.Radius + hit.Radius + .05f;
                Slide(hit, hit.X, hit.Y, self.X + dx * gap, self.Y + dy * gap, self.Id, out var toX, out var toY);
                _world.Move(hit.Id, toX, toY);
                var dealt = rule.Damage > 0 ? _world.Damage(self.Id, hit.Id, rule.Damage) : 0;
                Raise(new ThrowableEvent { Kind = ThrowableEventKind.RopePulledTarget, Item = rule.Kind, Actor = self.Id, Target = hit.Id, FromX = hit.X, FromY = hit.Y, ToX = toX, ToY = toY, Amount = (short)dealt });
            }
            else if (coverDistance < rule.Range)
            {
                var stop = Math.Max(0f, coverDistance - self.Radius - .05f);
                Slide(self, self.X, self.Y, self.X + dx * stop, self.Y + dy * stop, ThrowableEvent.NoTarget, out var toX, out var toY);
                _world.Move(self.Id, toX, toY);
                Raise(new ThrowableEvent { Kind = ThrowableEventKind.RopePulledSelf, Item = rule.Kind, Actor = self.Id, Target = ThrowableEvent.NoTarget, FromX = self.X, FromY = self.Y, ToX = toX, ToY = toY });
            }
            else
            {
                Raise(new ThrowableEvent { Kind = ThrowableEventKind.RopeMissed, Item = rule.Kind, Actor = self.Id, Target = ThrowableEvent.NoTarget, FromX = self.X, FromY = self.Y, ToX = endX, ToY = endY });
            }
        }

        private void Bell(ThrowActor self, ThrowableRule rule)
        {
            Raise(new ThrowableEvent { Kind = ThrowableEventKind.BellRung, Item = rule.Kind, Actor = self.Id, Target = ThrowableEvent.NoTarget, FromX = self.X, FromY = self.Y, ToX = self.X, ToY = self.Y, Seconds = (float)rule.ReverseSeconds, Size = rule.AreaWidth });
            for (var i = 0; i < _world.ActorCount; i++)
            {
                var target = _world.ActorAt(i);
                if (!target.Alive || target.Id == self.Id) continue;
                if (!OverlapsSquare(target, self.X, self.Y, rule.AreaWidth / 2f, rule.AreaHeight / 2f)) continue;
                var dealt = rule.Damage > 0 ? _world.Damage(self.Id, target.Id, rule.Damage) : 0;
                if (rule.ReverseSeconds > 0) _world.ApplyEffect(target.Id, ThrowableEffect.Reverse, rule.ReverseSeconds);
                Raise(new ThrowableEvent { Kind = ThrowableEventKind.Hit, Item = rule.Kind, Actor = self.Id, Target = target.Id, FromX = self.X, FromY = self.Y, ToX = target.X, ToY = target.Y, Amount = (short)dealt, Seconds = (float)rule.ReverseSeconds });
            }
        }

        private void Bat(ThrowActor self, ThrowableRule rule, float dx, float dy)
        {
            Raise(new ThrowableEvent { Kind = ThrowableEventKind.BatSwing, Item = rule.Kind, Actor = self.Id, Target = ThrowableEvent.NoTarget, FromX = self.X, FromY = self.Y, ToX = self.X + dx * rule.Range, ToY = self.Y + dy * rule.Range, Size = rule.ArcDegrees });
            var halfArcCos = Math.Cos(rule.ArcDegrees * .5 * Math.PI / 180.0);
            for (var i = 0; i < _world.ActorCount; i++)
            {
                var target = _world.ActorAt(i);
                if (!target.Alive || target.Id == self.Id) continue;
                var ox = target.X - self.X;
                var oy = target.Y - self.Y;
                var dist = (float)Math.Sqrt(ox * ox + oy * oy);
                if (dist > rule.Range + target.Radius) continue;
                if (dist > target.Radius && (ox * dx + oy * dy) / dist < halfArcCos) continue;
                if (_world.CoverFraction(self.X, self.Y, target.X, target.Y) < 1f) continue;
                var px = dist > 1e-4f ? ox / dist : dx;
                var py = dist > 1e-4f ? oy / dist : dy;
                var blocked = Slide(target, target.X, target.Y, target.X + px * rule.KnockbackCells, target.Y + py * rule.KnockbackCells, self.Id, out var toX, out var toY);
                _world.Move(target.Id, toX, toY);
                var dealt = rule.Damage > 0 ? _world.Damage(self.Id, target.Id, rule.Damage) : 0;
                if (blocked && rule.StunSeconds > 0) _world.ApplyEffect(target.Id, ThrowableEffect.Stun, rule.StunSeconds);
                Raise(new ThrowableEvent { Kind = ThrowableEventKind.BatHit, Item = rule.Kind, Actor = self.Id, Target = target.Id, FromX = target.X, FromY = target.Y, ToX = toX, ToY = toY, Amount = (short)dealt, Seconds = blocked ? (float)rule.StunSeconds : 0f, Flags = blocked ? ThrowableEvent.StunnedFlag : (byte)0 });
            }
        }

        /// <summary>
        /// Moves <paramref name="mover"/> from (fx, fy) toward (tx, ty) in small steps while the spot is walkable and
        /// clear of other living players (except <paramref name="ignore"/>). Returns true when something stopped it early.
        /// </summary>
        private bool Slide(ThrowActor mover, float fx, float fy, float tx, float ty, byte ignore, out float x, out float y)
        {
            x = fx; y = fy;
            var lx = tx - fx;
            var ly = ty - fy;
            var length = (float)Math.Sqrt(lx * lx + ly * ly);
            if (length < 1e-4f) return false;
            var steps = (int)Math.Ceiling(length / SlideStep);
            for (var s = 1; s <= steps; s++)
            {
                var t = Math.Min(1f, s * SlideStep / length);
                var nx = fx + lx * t;
                var ny = fy + ly * t;
                if (!_world.IsWalkable(nx, ny, mover.Radius) || Bumps(mover, nx, ny, ignore)) return true;
                x = nx; y = ny;
            }
            return false;
        }

        private bool Bumps(ThrowActor mover, float x, float y, byte ignore)
        {
            for (var i = 0; i < _world.ActorCount; i++)
            {
                var other = _world.ActorAt(i);
                if (!other.Alive || other.Id == mover.Id || other.Id == ignore) continue;
                var gap = mover.Radius + other.Radius;
                var ox = other.X - x;
                var oy = other.Y - y;
                if (ox * ox + oy * oy < gap * gap) return true;
            }
            return false;
        }

        private bool TryFindTrapVictim(Trap trap, out ThrowActor victim)
        {
            for (var i = 0; i < _world.ActorCount; i++)
            {
                var a = _world.ActorAt(i);
                // The thrower knows where their own peel is and does not slip on it (documented assumption).
                if (!a.Alive || a.Id == trap.Actor) continue;
                var reach = trap.Rule.TrapRadius + a.Radius;
                var ox = a.X - trap.X;
                var oy = a.Y - trap.Y;
                if (ox * ox + oy * oy <= reach * reach) { victim = a; return true; }
            }
            victim = default;
            return false;
        }

        private static bool OverlapsSquare(ThrowActor a, float cx, float cy, float halfW, float halfH)
        {
            var nx = Math.Max(cx - halfW, Math.Min(cx + halfW, a.X));
            var ny = Math.Max(cy - halfH, Math.Min(cy + halfH, a.Y));
            var ox = nx - a.X;
            var oy = ny - a.Y;
            return ox * ox + oy * oy <= a.Radius * a.Radius;
        }

        /// <summary>Nearest entry distance of a ray into a circle within [0, max].</summary>
        public static bool RayHitsCircle(float ox, float oy, float dx, float dy, float max, float cx, float cy, float radius, out float distance)
        {
            distance = 0f;
            var tx = cx - ox;
            var ty = cy - oy;
            var along = tx * dx + ty * dy;
            var perp = tx * tx + ty * ty - along * along;
            var r2 = radius * radius;
            if (perp > r2) return false;
            var entry = along - (float)Math.Sqrt(r2 - perp);
            if (entry < 0f)
            {
                if (along + (float)Math.Sqrt(r2 - perp) < 0f) return false;
                entry = 0f;
            }
            if (entry > max) return false;
            distance = entry;
            return true;
        }

        private ActorState State(byte actor)
        {
            if (!_actors.TryGetValue(actor, out var state)) _actors[actor] = state = new ActorState();
            return state;
        }

        private ushort NextId()
        {
            if (++_nextId == 0) _nextId = 1;
            return _nextId;
        }

        private void Raise(ThrowableEvent value) => Raised?.Invoke(value);

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
