using System.Collections.Generic;
using JungleMaze.App.Player;
using JungleMaze.Presentation.Combat;
using JungleMaze.Presentation.World;
using JungleMaze.Presentation.World.Phase2;
using NUnit.Framework;
using UnityEngine;

namespace JungleMaze.Tests.World
{
    /// <summary>Shots face their flight, wear per-weapon art without distortion, and muzzle flashes start at the barrel.</summary>
    public sealed class ProjectileArtTests
    {
        private readonly List<Object> _owned = new List<Object>();
        private CoverProbeProjectile _shot;
        private SpriteRenderer _renderer;
        private Sprite _tracer;

        [SetUp]
        public void SetUp()
        {
            var go = Own(new GameObject("shot"));
            go.transform.localScale = new Vector3(.18f, .08f, 1f);
            _renderer = go.AddComponent<SpriteRenderer>();
            _tracer = Art(48, 48, 48);
            _renderer.sprite = _tracer;
            _renderer.color = new Color(1f, .85f, .25f);
            _shot = go.AddComponent<CoverProbeProjectile>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var o in _owned) if (o != null) Object.DestroyImmediate(o);
            _owned.Clear();
        }

        [TestCase(0f)]
        [TestCase(90f)]
        [TestCase(-135f)]
        public void DefaultTracerFacesItsFlightDirection(float degrees)
        {
            var shot = default(ConfiguredShot);
            _shot.Launch(in shot, Direction(degrees));
            Assert.AreEqual(0f, Mathf.DeltaAngle(degrees, _shot.transform.eulerAngles.z), .01f);
        }

        [Test]
        public void WeaponArtIsUniformlyScaled_AndTurnedOntoTheFlight()
        {
            var arrow = Art(15, 15, 32);
            var frames = new WorldVisualSet.Frames { Sprites = new[] { arrow }, Times = new[] { 0f }, Duration = 1f, Loop = true };
            _shot.SetLook(frames, 1.07f, -45f, 0f, Color.white);
            var shot = default(ConfiguredShot);
            _shot.Launch(in shot, Direction(30f));
            Assert.AreSame(arrow, _renderer.sprite);
            Assert.AreEqual(_shot.transform.localScale.x, _shot.transform.localScale.y, 1e-6f, "uniform");
            Assert.AreEqual(0f, Mathf.DeltaAngle(30f - 45f, _shot.transform.eulerAngles.z), .01f);
            Assert.AreEqual(Color.white, _renderer.color);
        }

        [Test]
        public void TracerOnlyArtKeepsTheTracerButTintsIt_AndResetRestoresThePrefabLook()
        {
            var orange = new Color(1f, .62f, .22f);
            _shot.SetLook(null, 1f, 0f, 0f, orange);
            Assert.AreSame(_tracer, _renderer.sprite);
            Assert.AreEqual(orange, _renderer.color);
            Assert.AreEqual(new Vector3(.18f, .08f, 1f), _shot.transform.localScale);

            _shot.SetLook(new WorldVisualSet.Frames { Sprites = new[] { Art(8, 8, 8) }, Times = new[] { 0f }, Duration = 1f }, .3f, 0f, 0f, Color.white);
            _shot.ResetLook();
            Assert.AreSame(_tracer, _renderer.sprite);
            Assert.AreEqual(new Color(1f, .85f, .25f), _renderer.color);
            Assert.AreEqual(new Vector3(.18f, .08f, 1f), _shot.transform.localScale);
        }

        [Test]
        public void MuzzleFlashStartsAtTheBarrel_PointsAlongAim_AndEnds()
        {
            var root = Own(new GameObject("flash"));
            var child = new GameObject("Sprite");
            child.transform.SetParent(root.transform, false);
            child.AddComponent<SpriteRenderer>();
            var flash = root.AddComponent<ShotFlashVisual>();
            var cone = Art(64, 64, 32);
            var frames = new WorldVisualSet.Frames { Sprites = new[] { cone }, Times = new[] { 0f }, Duration = .24f };
            var barrel = new Vector3(5f, 5f, 0f);
            flash.Play(frames, barrel, 0f, .7f);
            var renderer = child.GetComponent<SpriteRenderer>();
            Assert.IsTrue(renderer.enabled);
            Assert.AreEqual(barrel.x, renderer.bounds.min.x, 1e-3f, "back edge at the barrel");
            Assert.AreEqual(barrel.y, renderer.bounds.center.y, 1e-3f);
            flash.Advance(.3f);
            Assert.IsFalse(renderer.enabled);
        }

        private static Vector2 Direction(float degrees) => new Vector2(Mathf.Cos(degrees * Mathf.Deg2Rad), Mathf.Sin(degrees * Mathf.Deg2Rad));

        private Sprite Art(int w, int h, float ppu)
        {
            var texture = Own(new Texture2D(w, h));
            return Own(Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(.5f, .5f), ppu));
        }

        private T Own<T>(T o) where T : Object { _owned.Add(o); return o; }
    }
}
