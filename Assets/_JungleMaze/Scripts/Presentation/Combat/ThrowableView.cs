using System;
using System.Collections.Generic;
using JungleMaze.App.Combat;
using JungleMaze.App.Match;
using JungleMaze.Presentation.Player;
using JungleMaze.Presentation.World;
using JungleMaze.World.Throwables;
using UnityEngine;

namespace JungleMaze.Presentation.Combat
{
    /// <summary>
    /// Draws confirmed WPN-03 events: the item arcing to its landing and waiting for the fuse, the blast, the banana
    /// peel until it is stepped on or expires, the smoke cloud, the rope, the bell's ring, seed vines on rooted
    /// players, and the user's throw/swing animation. Presentation only: it never moves players or decides hits.
    /// Effects are recycled from a small free list.
    /// </summary>
    public sealed class ThrowableView : MonoBehaviour
    {
        private enum FxKind { Flight, Blast, Trap, Smoke, Rope, Ring, Vines, Bell }

        private sealed class Fx
        {
            public GameObject Root;
            public SpriteRenderer Sprite;
            public LineRenderer Line;
            public FxKind Kind;
            public ushort Id;
            public ThrowableArtCatalog.Clip Clip;
            public float Scale, Elapsed, Duration, Flight;
            public Vector3 From, To;
            public byte Follow;
            public Color Tint;
        }

        private const int SortAbovePlayers = 20, SortUnderPlayers = -5;
        private const float RopeSeconds = .3f, RingSeconds = .45f;

        private readonly List<Fx> _active = new List<Fx>();
        private readonly Stack<Fx> _free = new Stack<Fx>();
        private readonly Vector3[] _ring = new Vector3[5];
        private readonly List<IThrowableEventSource> _sources = new List<IThrowableEventSource>();
        private ThrowableArtCatalog _art;
        private Transform _hero;
        private HeroSpriteAnimator _heroBody;
        private HeldWeaponView _heroHands;
        private RemotePlayerSpawner _remotes;
        private MatchSession _match;
        private Material _lineMaterial;
        private int _sortingLayer;

        /// <summary>Hooks the view to the local hero, the remote players and the event sources.</summary>
        public void Bind(Transform hero, RemotePlayerSpawner remotes, MatchSession match, params IThrowableEventSource[] sources)
        {
            _hero = hero;
            _heroBody = hero != null ? hero.GetComponentInChildren<HeroSpriteAnimator>(true) : null;
            _heroHands = hero != null ? hero.GetComponentInChildren<HeldWeaponView>(true) : null;
            _remotes = remotes;
            _match = match;
            _art = Resources.Load<ThrowableArtCatalog>(ThrowableArtCatalog.ResourceName);
            var body = _heroBody != null ? _heroBody.GetComponent<SpriteRenderer>() : null;
            _sortingLayer = body != null ? body.sortingLayerID : 0;
            foreach (var source in sources)
            {
                if (source == null) continue;
                source.ThrowableRaised += OnEvent;
                _sources.Add(source);
            }
        }

        public int ActiveEffects => _active.Count;

        private void OnDestroy()
        {
            foreach (var source in _sources) source.ThrowableRaised -= OnEvent;
            _sources.Clear();
            if (_lineMaterial != null) Destroy(_lineMaterial);
        }

        /// <summary>Applies one event; public so presentation tests can drive it without a match.</summary>
        public void OnEvent(ThrowableEvent e)
        {
            var itemId = ThrowableRules.IdOf(e.Item);
            var art = default(ThrowableArtCatalog.Entry);
            if (_art != null) _art.TryGet(itemId, out art);
            var from = new Vector3(e.FromX, e.FromY, 0f);
            var to = new Vector3(e.ToX, e.ToY, 0f);
            switch (e.Kind)
            {
                case ThrowableEventKind.Thrown:
                    AnimateUser(e.Actor, itemId, false);
                    if (art.Flight.IsSet)
                    {
                        var fx = Spawn(FxKind.Flight, e.Id, art.Flight, art.Flight.ScaleFor(art.FlightSize), from, to);
                        fx.Flight = Mathf.Clamp(Vector3.Distance(from, to) / 12f, .25f, .6f);
                        fx.Duration = Mathf.Max(fx.Flight, e.Seconds) + .5f;
                    }
                    break;
                case ThrowableEventKind.Detonated:
                    Release(FxKind.Flight, e.Id);
                    if (art.Blast.IsSet)
                    {
                        var fx = Spawn(FxKind.Blast, e.Id, art.Blast, art.Blast.ScaleFor(Mathf.Max(1f, e.Size)), from, from);
                        fx.Duration = art.Blast.Frames.Duration;
                        fx.Tint = BlastTint(e.Item);
                    }
                    break;
                case ThrowableEventKind.TrapArmed:
                    if (art.Linger.IsSet)
                    {
                        var fx = Spawn(FxKind.Trap, e.Id, art.Linger, art.Linger.ScaleFor(art.LingerSize), from, from);
                        fx.Duration = e.Seconds;
                    }
                    break;
                case ThrowableEventKind.TrapTriggered:
                case ThrowableEventKind.TrapExpired:
                    Release(FxKind.Trap, e.Id);
                    break;
                case ThrowableEventKind.SmokeStarted:
                    if (art.Linger.IsSet)
                    {
                        var fx = Spawn(FxKind.Smoke, e.Id, art.Linger, art.Linger.ScaleFor(Mathf.Max(1f, e.Size) * 1.15f), from, from);
                        fx.Duration = e.Seconds;
                    }
                    break;
                case ThrowableEventKind.SmokeEnded:
                    Release(FxKind.Smoke, e.Id);
                    break;
                case ThrowableEventKind.RopePulledTarget:
                    AnimateUser(e.Actor, itemId, false);
                    SpawnRope(ActorPosition(e.Actor, from), from, to);
                    break;
                case ThrowableEventKind.RopePulledSelf:
                    AnimateUser(e.Actor, itemId, false);
                    SpawnRope(to, from, to, true);
                    break;
                case ThrowableEventKind.RopeMissed:
                    AnimateUser(e.Actor, itemId, false);
                    SpawnRope(from, to, from);
                    break;
                case ThrowableEventKind.BellRung:
                    AnimateUser(e.Actor, itemId, false);
                    var ring = Spawn(FxKind.Ring, 0, default, 1f, from, from);
                    ring.Duration = RingSeconds;
                    ring.Scale = Mathf.Max(1f, e.Size);
                    ring.Tint = new Color(1f, .86f, .35f, 1f);
                    PrepareLine(ring, .08f, 5, true);
                    if (art.Flight.IsSet)
                    {
                        var bell = Spawn(FxKind.Bell, 0, art.Flight, art.Flight.ScaleFor(art.FlightSize), from, from);
                        bell.Duration = RingSeconds + .2f;
                        bell.Follow = e.Actor;
                    }
                    break;
                case ThrowableEventKind.BatSwing:
                    AnimateUser(e.Actor, itemId, true, e.Size);
                    break;
                case ThrowableEventKind.Hit:
                    if (e.Item == ThrowableKind.Seed && e.Seconds > 0f && e.Target != ThrowableEvent.NoTarget && art.Linger.IsSet)
                    {
                        var vines = Spawn(FxKind.Vines, e.Id, art.Linger, art.Linger.ScaleFor(art.LingerSize), to, to);
                        vines.Duration = e.Seconds;
                        vines.Follow = e.Target;
                    }
                    break;
            }
        }

        private void LateUpdate() => Advance(Time.deltaTime);

        /// <summary>Advances every effect; exposed for deterministic presentation tests and previews.</summary>
        public void Advance(float dt)
        {
            for (var i = _active.Count - 1; i >= 0; i--)
            {
                var fx = _active[i];
                fx.Elapsed += dt;
                if (fx.Elapsed >= fx.Duration) { Recycle(i); continue; }
                Draw(fx);
            }
        }

        private void Draw(Fx fx)
        {
            switch (fx.Kind)
            {
                case FxKind.Flight:
                {
                    var t = fx.Flight > 0f ? Mathf.Clamp01(fx.Elapsed / fx.Flight) : 1f;
                    var height = Mathf.Sin(t * Mathf.PI) * (.25f + Vector3.Distance(fx.From, fx.To) * .08f);
                    fx.Root.transform.position = Vector3.Lerp(fx.From, fx.To, t) + Vector3.up * height;
                    // Spins in the air, then sits still and blinks faster as the fuse runs out.
                    fx.Root.transform.rotation = Quaternion.Euler(0f, 0f, t < 1f ? -fx.Elapsed * 720f : 0f);
                    var left = fx.Duration - .5f - fx.Elapsed;
                    fx.Sprite.enabled = t < 1f || left > .6f || Mathf.Repeat(fx.Elapsed * (left > .3f ? 6f : 12f), 1f) < .6f;
                    SetFrame(fx, fx.Elapsed, Color.white);
                    break;
                }
                case FxKind.Blast:
                {
                    // Animated sheets carry their own fade; a single still (seed vines bursting) grows and fades out.
                    var single = fx.Clip.Frames != null && fx.Clip.Frames.Sprites.Length == 1;
                    var t = Mathf.Clamp01(fx.Elapsed / fx.Duration);
                    var tint = fx.Tint;
                    if (single)
                    {
                        tint.a = 1f - t * t;
                        fx.Root.transform.localScale = Vector3.one * fx.Scale * Mathf.Lerp(.6f, 1f, Mathf.Sqrt(t));
                    }
                    SetFrame(fx, fx.Elapsed, tint);
                    break;
                }
                case FxKind.Trap:
                    SetFrame(fx, 0f, Color.white);
                    break;
                case FxKind.Smoke:
                {
                    var fade = Mathf.Min(1f, fx.Elapsed / .4f, (fx.Duration - fx.Elapsed) / .6f);
                    SetFrame(fx, fx.Elapsed, new Color(1f, 1f, 1f, .92f * Mathf.Clamp01(fade)));
                    break;
                }
                case FxKind.Bell:
                    // Rings above the ringer's head, swinging side to side.
                    fx.Root.transform.position = ActorPosition(fx.Follow, fx.From) + Vector3.up * .95f;
                    fx.Root.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(fx.Elapsed * 40f) * 25f * (1f - fx.Elapsed / fx.Duration));
                    SetFrame(fx, 0f, Color.white);
                    break;
                case FxKind.Vines:
                    fx.Root.transform.position = ActorPosition(fx.Follow, fx.From) + Vector3.down * .25f;
                    SetFrame(fx, 0f, Color.white);
                    break;
                case FxKind.Rope:
                {
                    // Out to the hook point in the first half, then reels in toward the end point.
                    var t = Mathf.Clamp01(fx.Elapsed / fx.Duration);
                    var head = t < .5f ? Vector3.Lerp(fx.From, fx.To, t * 2f) : fx.To;
                    var tail = t < .5f ? fx.From : Vector3.Lerp(fx.From, fx.To, (t - .5f) * 2f);
                    fx.Line.SetPosition(0, tail);
                    fx.Line.SetPosition(1, head);
                    break;
                }
                case FxKind.Ring:
                {
                    var t = Mathf.Clamp01(fx.Elapsed / fx.Duration);
                    var half = Mathf.Lerp(.4f, fx.Scale * .5f, 1f - (1f - t) * (1f - t));
                    _ring[0] = fx.From + new Vector3(-half, -half);
                    _ring[1] = fx.From + new Vector3(half, -half);
                    _ring[2] = fx.From + new Vector3(half, half);
                    _ring[3] = fx.From + new Vector3(-half, half);
                    _ring[4] = _ring[0];
                    fx.Line.SetPositions(_ring);
                    var color = fx.Tint; color.a = 1f - t;
                    fx.Line.startColor = fx.Line.endColor = color;
                    break;
                }
            }
        }

        private static void SetFrame(Fx fx, float elapsed, Color tint)
        {
            var sprite = fx.Clip.Frames != null ? fx.Clip.Frames.Sample(elapsed) : null;
            if (sprite != null) fx.Sprite.sprite = sprite;
            fx.Sprite.color = tint;
        }

        private void SpawnRope(Vector3 anchor, Vector3 hook, Vector3 end, bool selfPull = false)
        {
            var fx = Spawn(FxKind.Rope, 0, default, 1f, selfPull ? hook : anchor, selfPull ? end : hook);
            fx.Duration = RopeSeconds;
            fx.Tint = new Color(.42f, .33f, .16f, 1f);
            PrepareLine(fx, .09f, 2, false);
            fx.Line.startColor = fx.Line.endColor = fx.Tint;
        }

        private Fx Spawn(FxKind kind, ushort id, ThrowableArtCatalog.Clip clip, float scale, Vector3 from, Vector3 to)
        {
            var fx = _free.Count > 0 ? _free.Pop() : Create();
            fx.Kind = kind; fx.Id = id; fx.Clip = clip; fx.Scale = scale; fx.From = from; fx.To = to;
            fx.Elapsed = 0f; fx.Duration = 1f; fx.Flight = 0f; fx.Follow = ThrowableEvent.NoTarget; fx.Tint = Color.white;
            fx.Root.SetActive(true);
            fx.Root.transform.SetPositionAndRotation(from, Quaternion.identity);
            fx.Root.transform.localScale = new Vector3(scale, scale, 1f);
            fx.Sprite.enabled = clip.IsSet;
            fx.Sprite.sortingLayerID = _sortingLayer;
            fx.Sprite.sortingOrder = kind == FxKind.Trap || kind == FxKind.Vines ? SortUnderPlayers : SortAbovePlayers;
            fx.Line.enabled = false;
            _active.Add(fx);
            if (clip.IsSet) SetFrame(fx, 0f, Color.white);
            return fx;
        }

        private void PrepareLine(Fx fx, float width, int points, bool loop)
        {
            fx.Root.transform.localScale = Vector3.one;
            fx.Sprite.enabled = false;
            if (_lineMaterial == null) _lineMaterial = new Material(Shader.Find("Sprites/Default"));
            var line = fx.Line;
            line.enabled = true;
            line.sharedMaterial = _lineMaterial;
            line.useWorldSpace = true;
            line.loop = false;
            line.positionCount = points;
            line.startWidth = line.endWidth = width;
            line.sortingLayerID = _sortingLayer;
            line.sortingOrder = SortAbovePlayers;
            line.numCapVertices = loop ? 0 : 2;
            for (var i = 0; i < points; i++) line.SetPosition(i, fx.From);
        }

        private Fx Create()
        {
            var root = new GameObject("ThrowableFx");
            root.transform.SetParent(transform, false);
            var fx = new Fx { Root = root, Sprite = root.AddComponent<SpriteRenderer>(), Line = root.AddComponent<LineRenderer>() };
            fx.Line.enabled = false;
            return fx;
        }

        private void Release(FxKind kind, ushort id)
        {
            for (var i = _active.Count - 1; i >= 0; i--)
                if (_active[i].Kind == kind && _active[i].Id == id) Recycle(i);
        }

        private void Recycle(int index)
        {
            var fx = _active[index];
            _active.RemoveAt(index);
            fx.Root.SetActive(false);
            _free.Push(fx);
        }

        /// <summary>Plays the user's attack: body swing plus the held weapon (a full swing for the bat, a flick otherwise).</summary>
        private void AnimateUser(byte actor, string itemId, bool swing, float arcDegrees = 90f)
        {
            HeroSpriteAnimator body;
            HeldWeaponView hands;
            var view = ViewOf(actor);
            if (view != null)
            {
                body = view.GetComponentInChildren<HeroSpriteAnimator>();
                hands = view.GetComponentInChildren<HeldWeaponView>();
            }
            else if (IsLocal(actor)) { body = _heroBody; hands = _heroHands; }
            else return;
            if (body != null) body.PlayAttack();
            if (hands == null) return;
            hands.Show(itemId);
            hands.OnAttack(swing, swing ? .35f : 0f, arcDegrees);
        }

        private Vector3 ActorPosition(byte actor, Vector3 fallback)
        {
            var view = ViewOf(actor);
            if (view != null) return view.transform.position;
            if (IsLocal(actor)) return _hero != null ? _hero.position : fallback;
            return fallback;
        }

        /// <summary>
        /// The networked view drawing this player, own player included (online the Hero is only a hidden camera proxy).
        /// Null offline.
        /// </summary>
        private PlayerView ViewOf(byte actor)
        {
            if (_match == null || !_match.Connected || _remotes == null || actor == ThrowableEvent.NoTarget) return null;
            var view = _remotes.GetView(actor);
            return view != null && view.isActiveAndEnabled ? view : null;
        }

        /// <summary>Offline the local player is actor 0; online it is the server-assigned NetId.</summary>
        private bool IsLocal(byte actor) => _match != null && _match.Connected ? actor == _match.OwnNetId : actor == 0;

        private static Color BlastTint(ThrowableKind kind)
        {
            switch (kind)
            {
                case ThrowableKind.Crystal: return new Color(.62f, .9f, 1f, 1f);
                default: return Color.white;
            }
        }
    }
}
