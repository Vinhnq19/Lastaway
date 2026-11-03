using System.Collections.Generic;
using JungleBackend.Protocol.Gameplay;
using JungleMaze.Presentation.Combat;
using JungleMaze.Presentation.Player;
using JungleMaze.Presentation.World.Phase2;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace JungleMaze.Tests.World
{
    /// <summary>In-hand weapon: normalized size, grip at the hand, mirrored aiming left, behind a back-facing body.</summary>
    public sealed class HeldWeaponViewTests
    {
        private readonly List<Object> _owned = new List<Object>();
        private GameObject _root;
        private SpriteRenderer _body, _weapon;
        private HeroSpriteAnimator _animator;
        private HeldWeaponView _held;

        [SetUp]
        public void SetUp()
        {
            _root = Own(new GameObject("hero"));
            var bodyObject = new GameObject("Body");
            bodyObject.transform.SetParent(_root.transform, false);
            _body = bodyObject.AddComponent<SpriteRenderer>();
            _animator = bodyObject.AddComponent<HeroSpriteAnimator>();
            var set = Own(ScriptableObject.CreateInstance<HeroVisualSet>());
            var frame = Art(48, 48, 8);
            var loop = new WorldVisualSet.Frames { Sprites = new[] { frame }, Times = new[] { 0f }, Duration = .5f, Loop = true };
            set.Heroes = new[] { new HeroVisualSet.Hero { IdleSide = loop, IdleFront = loop, IdleBack = loop, MoveSide = loop } };
            Set(_animator, "visuals", set);
            _animator.SetHero(0);

            var hand = new GameObject("Weapon");
            hand.transform.SetParent(_root.transform, false);
            var art = new GameObject("Sprite");
            art.transform.SetParent(hand.transform, false);
            _weapon = art.AddComponent<SpriteRenderer>();
            _held = hand.AddComponent<HeldWeaponView>();
            var catalog = Own(ScriptableObject.CreateInstance<HeldWeaponCatalog>());
            catalog.Entries = new[]
            {
                new HeldWeaponCatalog.Entry { ItemId = "AXE", Sprite = Art(32, 16, 16), Grip = new Vector2(0f, .5f), Length = .8f },
            };
            var so = new SerializedObject(_held);
            so.FindProperty("catalog").objectReferenceValue = catalog;
            so.FindProperty("weaponRenderer").objectReferenceValue = _weapon;
            so.FindProperty("body").objectReferenceValue = _body;
            so.FindProperty("bodyAnimator").objectReferenceValue = _animator;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var o in _owned) if (o != null) Object.DestroyImmediate(o);
            _owned.Clear();
        }

        [Test]
        public void BareHandsAndUnknownItemsHideTheWeapon()
        {
            _held.Show("HAND");
            _held.Apply();
            Assert.IsFalse(_held.Visible);
            _held.Show("NOT_A_WEAPON");
            _held.Apply();
            Assert.IsFalse(_held.Visible);
        }

        [Test]
        public void KnownWeaponHasItsConfiguredLength_AndItsGripSitsInTheHand()
        {
            _held.Show("AXE");
            _held.Apply();
            Assert.IsTrue(_held.Visible);
            Assert.AreEqual(.8f, _weapon.bounds.size.x, 1e-3f);
            var gripWorld = new Vector3(_weapon.bounds.min.x, _weapon.bounds.center.y, 0f);
            Assert.AreEqual(_held.transform.position.x, gripWorld.x, 1e-3f);
            Assert.AreEqual(_held.transform.position.y, gripWorld.y, 1e-3f);
        }

        [TestCase(0f, 1f)]
        [TestCase(180f, -1f)]
        [TestCase(120f, -1f)]
        [TestCase(-60f, 1f)]
        public void WeaponPointsAlongAim_AndIsMirroredWhenAimingLeft(float aim, float mirror)
        {
            _held.Show("AXE");
            _root.transform.rotation = Quaternion.Euler(0, 0, aim);
            _held.Apply();
            Assert.AreEqual(mirror, _held.transform.localScale.y);
            var direction = (_weapon.bounds.center - _held.transform.position).normalized;
            Assert.AreEqual(0f, Mathf.DeltaAngle(aim, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg), .5f);
        }

        [Test]
        public void WeaponGoesBehindTheBodyOnlyWhenTheHeroFacesAway()
        {
            _held.Show("AXE");
            _root.transform.rotation = Quaternion.Euler(0, 0, 90f);
            _animator.Step(.016f);
            _held.Apply();
            Assert.Less(_weapon.sortingOrder, _body.sortingOrder);
            _root.transform.rotation = Quaternion.Euler(0, 0, -90f);
            _animator.Step(.016f);
            _held.Apply();
            Assert.Greater(_weapon.sortingOrder, _body.sortingOrder);
        }

        [Test]
        public void MeleeSwingHidesTheHeldWeapon_DeadBodyHidesIt()
        {
            _held.Show("AXE");
            _held.OnAttack(true, .3f);
            _held.Apply();
            Assert.IsFalse(_held.Visible);

            _held.OnAttack(false, 0f);
            _body.enabled = false;
            _held.Apply();
            Assert.IsFalse(_held.Visible);
        }

        [Test]
        public void ServerCodeDrivesRemoteHands_UnknownLeavesTheLastAttackInPlace()
        {
            _held.ShowServerCode((byte)(CombatWeaponId.Axe + 1));
            _held.Apply();
            Assert.AreEqual("AXE", _held.ItemId);
            Assert.IsTrue(_held.Visible);

            _held.ShowServerCode(PlayerStatePacket.HeldUnknown);
            Assert.AreEqual("AXE", _held.ItemId, "older servers send 0: keep what the attack showed");

            _held.ShowServerCode(PlayerStatePacket.HeldOther);
            _held.Apply();
            Assert.IsFalse(_held.Visible, "an item without combat art hides the hand");

            _held.ShowServerCode((byte)(CombatWeaponId.Hand + 1));
            _held.Apply();
            Assert.AreEqual("HAND", _held.ItemId);
            Assert.IsFalse(_held.Visible);
        }

        [Test]
        public void RemotePlayerViewFollowsTheSnapshotHeldWeapon()
        {
            var view = _root.AddComponent<PlayerView>();
            var so = new SerializedObject(view);
            so.FindProperty("heldWeapon").objectReferenceValue = _held;
            so.ApplyModifiedPropertiesWithoutUndo();
            var model = new JungleMaze.Domain.Player.PlayerModel(4);
            model.Apply(new JungleMaze.Domain.Player.PlayerSnapshot { Tick = 1, Hp = 100, HeldCode = (byte)(CombatWeaponId.Axe + 1) });
            view.Bind(model);
            Assert.AreEqual("AXE", _held.ItemId);

            model.Apply(new JungleMaze.Domain.Player.PlayerSnapshot { Tick = 2, Hp = 100, HeldCode = PlayerStatePacket.HeldOther });
            view.Bind(model);
            Assert.IsNull(_held.ItemId);
        }

        private Sprite Art(int w, int h, float ppu)
        {
            var texture = Own(new Texture2D(w, h));
            return Own(Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(.5f, .5f), ppu));
        }

        private static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private T Own<T>(T o) where T : Object { _owned.Add(o); return o; }
    }
}
