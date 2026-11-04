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
        private SpriteRenderer _body, _weapon, _mainHand, _offHand;
        private Sprite _fist;
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
            _fist = Art(16, 16, 8);
            set.Heroes = new[] { new HeroVisualSet.Hero { IdleSide = loop, IdleFront = loop, IdleBack = loop, MoveSide = loop, Hand = _fist } };
            Set(_animator, "visuals", set);
            _animator.SetHero(0);

            var hand = new GameObject("Weapon");
            hand.transform.SetParent(_root.transform, false);
            var art = new GameObject("Sprite");
            art.transform.SetParent(hand.transform, false);
            _weapon = art.AddComponent<SpriteRenderer>();
            _mainHand = new GameObject("MainHand").AddComponent<SpriteRenderer>();
            _mainHand.transform.SetParent(hand.transform, false);
            _offHand = new GameObject("OffHand").AddComponent<SpriteRenderer>();
            _offHand.transform.SetParent(hand.transform, false);
            _held = hand.AddComponent<HeldWeaponView>();
            var catalog = Own(ScriptableObject.CreateInstance<HeldWeaponCatalog>());
            catalog.Entries = new[]
            {
                new HeldWeaponCatalog.Entry { ItemId = "AXE", Sprite = Art(32, 16, 16), Grip = new Vector2(0f, .5f), Length = .8f },
                new HeldWeaponCatalog.Entry { ItemId = "BANANA", Sprite = Art(16, 16, 16), Grip = new Vector2(.5f, .5f), Length = .4f },
                new HeldWeaponCatalog.Entry { ItemId = "SHOTGUN", Sprite = Art(32, 8, 16), Grip = new Vector2(0f, .5f), Length = 1f, Support = .5f },
            };
            var so = new SerializedObject(_held);
            so.FindProperty("catalog").objectReferenceValue = catalog;
            so.FindProperty("weaponRenderer").objectReferenceValue = _weapon;
            so.FindProperty("body").objectReferenceValue = _body;
            so.FindProperty("bodyAnimator").objectReferenceValue = _animator;
            so.FindProperty("mainHand").objectReferenceValue = _mainHand;
            so.FindProperty("offHand").objectReferenceValue = _offHand;
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
        public void MeleeSwingKeepsTheWeaponInHand_AndSweepsItThroughTheArc()
        {
            _held.Show("AXE");
            _held.OnAttack(true, 1f, 120f);
            _held.Step(.2f);
            Assert.IsTrue(_held.Visible, "the hand swings the weapon itself");
            Assert.IsTrue(_held.Swinging);
            var windUp = Mathf.DeltaAngle(0f, _held.transform.eulerAngles.z);
            Assert.Greater(windUp, 30f, "wind-up raises the weapon above the aim");

            _held.Step(.45f);
            var strike = Mathf.DeltaAngle(0f, _held.transform.eulerAngles.z);
            Assert.AreEqual(-60f, strike, 1f, "strike ends at the far side of the arc");

            _held.Step(1f);
            Assert.IsFalse(_held.Swinging);
            Assert.AreEqual(0f, Mathf.DeltaAngle(0f, _held.transform.eulerAngles.z), .5f, "returns to the aim");
        }

        [Test]
        public void SwingMirrorsWhenAimingLeft_SoItAlwaysStartsOverhead()
        {
            _held.Show("AXE");
            _root.transform.rotation = Quaternion.Euler(0, 0, 180f);
            _held.OnAttack(true, 1f, 120f);
            _held.Step(.2f);
            var offset = Mathf.DeltaAngle(180f, _held.transform.eulerAngles.z);
            Assert.Less(offset, -30f, "aiming left, the wind-up turns clockwise (still up)");
        }

        [Test]
        public void DeadBodyHidesWeaponAndHands()
        {
            _held.Show("AXE");
            _body.enabled = false;
            _held.Apply();
            Assert.IsFalse(_held.Visible);
            Assert.IsFalse(_held.HandsVisible);
            Assert.IsFalse(_offHand.enabled);
        }

        [Test]
        public void HandsUseTheSkinFist_MainHandGripsTheHandle()
        {
            _held.Show("AXE");
            _held.Apply();
            Assert.IsTrue(_held.HandsVisible);
            Assert.AreSame(_fist, _mainHand.sprite);
            Assert.AreSame(_fist, _offHand.sprite);
            Assert.AreEqual(_held.transform.position.x, _mainHand.transform.position.x, 1e-4f);
            Assert.AreEqual(_held.transform.position.y, _mainHand.transform.position.y, 1e-4f);
            Assert.Greater(_mainHand.sortingOrder, _weapon.sortingOrder, "fist drawn over the handle");
            Assert.AreEqual(Mathf.Abs(_body.transform.lossyScale.x), _mainHand.transform.lossyScale.x, 1e-4f, "same pixel size as the body");
        }

        [Test]
        public void OffHandRestsAwayFromTheAim_OrHoldsTheFrontOfTwoHandedGuns()
        {
            _held.Show("AXE");
            _held.Apply();
            Assert.Less(_offHand.transform.position.x, 0f, "aiming right, the free hand rests on the left");

            _held.Show("SHOTGUN");
            _held.Apply();
            var expected = _held.transform.position + _held.transform.right * .5f;
            Assert.AreEqual(expected.x, _offHand.transform.position.x, 1e-3f, "support hand halfway along the gun");
            Assert.AreEqual(expected.y, _offHand.transform.position.y, 1e-3f);
            Assert.AreEqual(1f, Vector3.Distance(_held.transform.position, _held.MuzzlePosition), 1e-3f, "muzzle at the barrel tip");
        }

        [Test]
        public void BareHandsPunchAlongTheAim()
        {
            _held.Show("HAND");
            _held.Apply();
            Assert.IsFalse(_held.Visible, "no weapon sprite");
            Assert.IsTrue(_held.HandsVisible, "fists still drawn");
            var guard = _mainHand.transform.position.x;
            _held.OnAttack(true, 1f);
            _held.Step(.4f);
            Assert.Greater(_mainHand.transform.position.x, guard + .2f, "the jab reaches forward");
            _held.Step(1f);
            Assert.AreEqual(guard, _mainHand.transform.position.x, 1e-4f, "and pulls back");
        }

        [Test]
        public void SwingAndPunchCurvesStartAndEndAtRest()
        {
            Assert.AreEqual(0f, HeldWeaponView.SwingOffset(0f, 120f));
            Assert.AreEqual(0f, HeldWeaponView.SwingOffset(1f, 120f));
            Assert.AreEqual(60f, HeldWeaponView.SwingOffset(.3f, 120f), 1e-3f);
            Assert.AreEqual(-60f, HeldWeaponView.SwingOffset(.6f, 120f), 1e-3f);
            Assert.AreEqual(0f, HeldWeaponView.PunchExtend(0f));
            Assert.AreEqual(1f, HeldWeaponView.PunchExtend(.45f));
            Assert.AreEqual(0f, HeldWeaponView.PunchExtend(1f));
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
            Assert.IsNull(_held.ItemId, "other item without a held-items entry yet: hide");

            model.SetHeldItem("BANANA");
            view.Bind(model);
            Assert.AreEqual("BANANA", _held.ItemId);
            _held.Apply();
            Assert.IsTrue(_held.Visible, "the throwable from the held-items list is drawn");
        }

        [Test]
        public void ReloadDipPeaksMidReloadAndIsZeroAtBothEnds()
        {
            Assert.AreEqual(0f, HeldWeaponView.ReloadDip(2f, 2f, .15f), 1e-5f, "reload just started");
            Assert.AreEqual(0f, HeldWeaponView.ReloadDip(0f, 2f, .15f), 1e-5f, "reload finished");
            Assert.AreEqual(0f, HeldWeaponView.ReloadDip(1f, 0f, .15f), 1e-5f, "no reload time: no dip");
            Assert.AreEqual(.15f, HeldWeaponView.ReloadDip(1f, 2f, .15f), 1e-5f, "midpoint hits full drop");
            Assert.Greater(HeldWeaponView.ReloadDip(1.5f, 2f, .15f), 0f);
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
