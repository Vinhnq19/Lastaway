using JungleMaze.App.World;
using JungleMaze.Presentation.Rendering;
using JungleMaze.World.Inventory;
using UnityEngine;
using UnityEngine.Rendering;

namespace JungleMaze.Presentation.World.Loot
{
    /// <summary>What a ground item draws: sprite, uniform scale, tilt and the light it gets.</summary>
    public struct GroundLootArt
    {
        public Sprite Sprite;
        public float Scale;
        public float Angle;
        /// <summary>Visual centre of the sprite content relative to its pivot, in sprite units.</summary>
        public Vector2 Center;
        public GroundLootShape Shape;
        public ItemRarity Rarity;
        /// <summary>True when only the blank fallback square exists (tinted like the old marker).</summary>
        public bool Placeholder;
    }

    /// <summary>
    /// One floating ground item (WorldObjectWire kind 100): shadow and rarity halo on the Ground layer, light column, item
    /// and motes in a Y-sorted group on WorldDynamic, so canopies (Foreground) cover it like any other world object.
    /// Pooled by <see cref="GroundLootLayer"/>: <see cref="Show"/> resets every animated value, nothing is destroyed per snapshot.
    /// Presentation only; never decides pickup or loot.
    /// </summary>
    public sealed class GroundLootView : MonoBehaviour
    {
        private const int MaxSparkles = 6;

        private GroundLootTuning _tuning;
        private SpriteRenderer _shadow, _halo, _beam, _item;
        private readonly SpriteRenderer[] _sparkles = new SpriteRenderer[MaxSparkles];
        private SortingGroup _body;
        private Transform _bodyTransform, _itemTransform;

        private GroundLootArt _art;
        private GroundLootTuning.RarityStyle _style;
        private GroundLootArrival _arrival;
        private Vector2 _ground;
        private double _shownAt;
        private int _sparkleCount;
        private Color _itemColor;
        private bool _lit;

        public ushort WorldId { get; private set; }
        public string ItemId { get; private set; }
        public GroundLootArt Art => _art;
        public GroundLootEntry Entry => _arrival.Entry;
        /// <summary>Visible (non-zero alpha) light column, for tests.</summary>
        public bool HasBeam => _beam != null && _beam.enabled;
        public int SparkleCount => _sparkleCount;
        public SpriteRenderer ItemRenderer => _item;
        public SpriteRenderer HaloRenderer => _halo;
        public SpriteRenderer BeamRenderer => _beam;
        public SortingGroup Body => _body;

        /// <summary>Creates the child renderers once; called by the pool factory.</summary>
        public void Build(GroundLootTuning tuning)
        {
            _tuning = tuning;
            var ground = SortingLayers.IdOf(SortingLayers.Ground);
            _shadow = Child("Shadow", transform, ground, tuning.GroundOrder, true);
            _halo = Child("Halo", transform, ground, tuning.GroundOrder + 1, true);
            var body = new GameObject("Body");
            _bodyTransform = body.transform;
            _bodyTransform.SetParent(transform, false);
            _body = body.AddComponent<SortingGroup>();
            _body.sortingLayerID = SortingLayers.IdOf(SortingLayers.WorldDynamic);
            _beam = Child("Beam", _bodyTransform, 0, 0, true);
            _item = Child("Item", _bodyTransform, 0, 2, false);
            _itemTransform = _item.transform;
            for (var i = 0; i < MaxSparkles; i++) _sparkles[i] = Child("Mote", _bodyTransform, 0, 3, true);
            _shadow.sprite = tuning.Halo;
            _halo.sprite = tuning.Halo;
            _beam.sprite = tuning.Beam;
            foreach (var s in _sparkles) s.sprite = tuning.Sparkle;
        }

        private SpriteRenderer Child(string name, Transform parent, int layer, int order, bool light)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var r = go.AddComponent<SpriteRenderer>();
            if (layer != 0) r.sortingLayerID = layer;
            r.sortingOrder = order;
            if (light && _tuning.LightMaterial != null) r.sharedMaterial = _tuning.LightMaterial;
            return r;
        }

        /// <summary>Starts presenting one item: entry animation from <paramref name="arrival"/>, then hover.</summary>
        public void Show(ushort id, string itemId, float x, float y, GroundLootArt art, GroundLootArrival arrival, double now)
        {
            WorldId = id; ItemId = itemId; _art = art; _arrival = arrival; _shownAt = now;
            _ground = new Vector2(x, y);
            _lit = GroundLootLook.HasRarityLight(art.Shape);
            _style = _tuning.StyleOf(art.Rarity);
            _sparkleCount = _lit ? Mathf.Clamp(_style.Sparkles, 0, MaxSparkles) : 0;

            _item.sprite = art.Sprite;
            _itemColor = art.Placeholder ? WorldObjectViewColor : Color.white;
            _itemTransform.localScale = new Vector3(art.Scale, art.Scale, 1f);
            _halo.transform.localScale = Vector3.one * (_lit ? _style.HaloSize : _tuning.AmmoHaloSize);
            _beam.enabled = _lit && _style.BeamHeight > 0f;
            _beam.transform.localScale = new Vector3(_style.BeamWidth, _style.BeamHeight, 1f);
            for (var i = 0; i < MaxSparkles; i++)
            {
                _sparkles[i].enabled = i < _sparkleCount;
                _sparkles[i].transform.localScale = Vector3.one * _tuning.SparkleSize;
            }
            name = "Loot " + itemId + " #" + id;
            Render(now);
        }

        /// <summary>Authoritative position changed (rare for floor items); the hover continues from there.</summary>
        public void Move(float x, float y) => _ground = new Vector2(x, y);

        private void Update() => Render(Time.unscaledTimeAsDouble);

        /// <summary>Draws the item at <paramref name="now"/> (seconds on the same clock as <see cref="Show"/>); exposed for tests.</summary>
        public void Render(double now)
        {
            if (_tuning == null) return;
            var t = (float)(now - _shownAt);
            Vector2 point = _ground; float lift = 0f, appear = 1f, light, settle, spin = 0f;
            if (_arrival.Entry == GroundLootEntry.Launch)
            {
                var flight = t - _arrival.Delay;
                if (flight < 0f)
                {
                    // Waiting for its turn inside the container: hidden at the launch origin.
                    transform.position = new Vector3(_arrival.FromX, _arrival.FromY, 0f);
                    SetAlpha(0f, 0f, 0f);
                    return;
                }
                var pose = GroundLootMotion.Flight(flight, _arrival.FromX, _arrival.FromY, _ground.x, _ground.y,
                    _tuning.LaunchHeight, _tuning.LaunchSeconds, _tuning.BounceHeight, _tuning.BounceShare);
                point = new Vector2(pose.X, pose.Y); lift = pose.Lift;
                var since = flight - _tuning.LaunchSeconds;
                settle = pose.Landed ? Fade(since, _tuning.LightFadeSeconds) : 0f;
                light = settle;
                if (!pose.Landed) spin = _tuning.LaunchSpinDegrees * (1f - Mathf.Clamp01(flight / Mathf.Max(.01f, _tuning.LaunchSeconds)));
            }
            else
            {
                appear = Fade(t, _tuning.FadeSeconds);
                light = appear; settle = 1f;
            }

            transform.position = new Vector3(point.x, point.y, 0f);
            _body.sortingOrder = DepthSortingSystem.ComputeOrder(point.y, DepthSortingSystem.DefaultPrecision);

            var bob = GroundLootMotion.Bob(now, WorldId, _tuning.BobAmplitude, _tuning.BobPeriod) * settle;
            var height = _tuning.FloatHeight + bob + lift;
            var rotation = Quaternion.Euler(0f, 0f, _art.Angle + spin);
            var center = rotation * (Vector3)(_art.Center * _art.Scale);
            _itemTransform.localRotation = rotation;
            _itemTransform.localPosition = new Vector3(-center.x, height - center.y, 0f);

            var shadow = Mathf.Lerp(1f, _tuning.ShadowMinScale, Mathf.Clamp01((height - _tuning.FloatHeight) / Mathf.Max(.01f, _tuning.LaunchHeight)));
            _shadow.transform.localScale = new Vector3(_tuning.ShadowSize.x * shadow, _tuning.ShadowSize.y * shadow, 1f);

            var pulse = 1f - _tuning.BeamPulse * .5f * (1f + Mathf.Sin((float)(now / Mathf.Max(.1f, _tuning.BeamPulsePeriod)) * Mathf.PI * 2f + GroundLootMotion.Phase(WorldId) * 6.283f));
            SetAlpha(appear, light, pulse);
            for (var i = 0; i < _sparkleCount; i++) Mote(i, now, light);
        }

        private void SetAlpha(float item, float light, float pulse)
        {
            var c = _itemColor; c.a *= item; _item.color = c;
            var shadow = new Color(0f, 0f, 0f, _tuning.ShadowAlpha * item);
            _shadow.color = shadow;
            var glow = _lit ? _style.Glow : _tuning.AmmoGlow; glow.a *= light; _halo.color = glow;
            var beam = _style.Beam; beam.a *= light * pulse; _beam.color = beam;
            if (light <= 0f) for (var i = 0; i < _sparkleCount; i++) _sparkles[i].color = Color.clear;
        }

        /// <summary>Motes rise through the lower column and twinkle, each on its own phase.</summary>
        private void Mote(int i, double now, float light)
        {
            var period = Mathf.Max(.2f, _tuning.SparklePeriod);
            var phase = (float)((now / period + (i + GroundLootMotion.Phase(WorldId)) / Mathf.Max(1, _sparkleCount)) % 1.0);
            var side = ((i * 0.37f + GroundLootMotion.Phase(WorldId)) % 1f - .5f) * _style.BeamWidth * .7f;
            var r = _sparkles[i];
            r.transform.localPosition = new Vector3(side, phase * _style.BeamHeight * .85f, 0f);
            var c = _style.Glow; c.a = Mathf.Sin(phase * Mathf.PI) * light;
            r.color = c;
            r.transform.localRotation = Quaternion.Euler(0f, 0f, phase * 180f);
        }

        private static float Fade(float elapsed, float seconds) => seconds > 0f ? Mathf.Clamp01(elapsed / seconds) : 1f;

        /// <summary>Old WorldObjectView marker colour for items with no art at all.</summary>
        private static readonly Color WorldObjectViewColor = new Color(1f, .85f, .25f);
    }
}
