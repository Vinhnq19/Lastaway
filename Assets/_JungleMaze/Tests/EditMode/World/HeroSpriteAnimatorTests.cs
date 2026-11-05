using System.Collections.Generic;
using JungleMaze.Presentation.Player;
using JungleMaze.Presentation.World.Phase2;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace JungleMaze.Tests.World
{
    /// <summary>Hero body art: stays upright while the root aims, faces by aim angle, switches idle/move by root speed.</summary>
    public sealed class HeroSpriteAnimatorTests
    {
        private readonly List<Object> _owned = new List<Object>();
        private GameObject _root;
        private HeroSpriteAnimator _animator;
        private SpriteRenderer _body;
        private Sprite _idleSide, _idleBack, _idleFront, _moveSide, _attackSide;

        [SetUp]
        public void SetUp()
        {
            _idleSide = Frame(); _idleBack = Frame(); _idleFront = Frame(); _moveSide = Frame(); _attackSide = Frame();
            var set = Own(ScriptableObject.CreateInstance<HeroVisualSet>());
            set.FrameWorldHeight = 3f;
            set.Heroes = new[]
            {
                new HeroVisualSet.Hero { Name = "a", IdleSide = Loop(_idleSide), IdleBack = Loop(_idleBack), IdleFront = Loop(_idleFront), MoveSide = Loop(_moveSide),
                    AttackSide = new WorldVisualSet.Frames { Sprites = new[] { _attackSide }, Times = new[] { 0f }, Duration = .3f } },
                new HeroVisualSet.Hero { Name = "b", IdleSide = Loop(_idleSide), IdleFront = Loop(_idleFront), MoveSide = Loop(_moveSide) },
            };
            _root = Own(new GameObject("hero root"));
            var bodyObject = new GameObject("Body");
            bodyObject.transform.SetParent(_root.transform, false);
            _body = bodyObject.AddComponent<SpriteRenderer>();
            _animator = bodyObject.AddComponent<HeroSpriteAnimator>();
            var so = new SerializedObject(_animator);
            so.FindProperty("visuals").objectReferenceValue = set;
            so.ApplyModifiedPropertiesWithoutUndo();
            _animator.SetHero(0);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var o in _owned) if (o != null) Object.DestroyImmediate(o);
            _owned.Clear();
        }

        [TestCase(0f, HeroFacing.Side, false)]
        [TestCase(180f, HeroFacing.Side, true)]
        [TestCase(90f, HeroFacing.Back, false)]
        [TestCase(-90f, HeroFacing.Front, false)]
        public void FacingFollowsAim_AndBodyStaysUpright(float aim, HeroFacing facing, bool flipped)
        {
            _root.transform.rotation = Quaternion.Euler(0, 0, aim);
            _animator.Step(.016f);
            Assert.AreEqual(facing, _animator.Facing);
            Assert.AreEqual(flipped, _body.flipX);
            Assert.AreEqual(0f, Mathf.DeltaAngle(0, _body.transform.eulerAngles.z), 1e-3f);
        }

        [Test]
        public void BodyIsScaledToTheConfiguredFrameHeight()
        {
            _animator.Step(.016f);
            Assert.AreEqual(3f, _body.bounds.size.y, 1e-3f);
        }

        [Test]
        public void MovingRootPlaysMoveLoop_StillRootReturnsToIdle()
        {
            _animator.Step(.016f);
            for (var i = 0; i < 20; i++) { _root.transform.position += Vector3.right * .1f; _animator.Step(.02f); }
            Assert.IsTrue(_animator.Moving);
            Assert.AreSame(_moveSide, _body.sprite);
            for (var i = 0; i < 30; i++) _animator.Step(.02f);
            Assert.IsFalse(_animator.Moving);
            Assert.AreSame(_idleSide, _body.sprite);
        }

        [Test]
        public void AttackPlaysOnceForTheFacing_ThenReturnsToIdle()
        {
            _animator.Step(.016f);
            _animator.PlayAttack();
            _animator.Step(.016f);
            Assert.IsTrue(_animator.Attacking);
            Assert.AreSame(_attackSide, _body.sprite);
            for (var i = 0; i < 20; i++) _animator.Step(.02f);
            Assert.IsFalse(_animator.Attacking);
            Assert.AreSame(_idleSide, _body.sprite);
        }

        [Test]
        public void HeroWithoutAttackClipsIgnoresAttack()
        {
            _animator.SetHero(1);
            _animator.PlayAttack();
            _animator.Step(.016f);
            Assert.IsFalse(_animator.Attacking);
            Assert.AreSame(_idleSide, _body.sprite);
        }

        [Test]
        public void MissingFacingLoopFallsBackToIdleFront()
        {
            _animator.SetHero(1);
            _root.transform.rotation = Quaternion.Euler(0, 0, 90f);
            _animator.Step(.016f);
            Assert.AreSame(_idleFront, _body.sprite);
        }

        private WorldVisualSet.Frames Loop(Sprite sprite) =>
            new WorldVisualSet.Frames { Sprites = new[] { sprite }, Times = new[] { 0f }, Duration = .5f, Loop = true };

        private Sprite Frame()
        {
            var texture = Own(new Texture2D(48, 48));
            return Own(Sprite.Create(texture, new Rect(0, 0, 48, 48), new Vector2(.5f, .5f), 8));
        }

        private T Own<T>(T o) where T : Object { _owned.Add(o); return o; }
    }
}
