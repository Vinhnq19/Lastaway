using System.Collections.Generic;
using System.Reflection;
using JungleMaze.Presentation.Combat;
using JungleMaze.Presentation.Player;
using JungleMaze.Presentation.UI.MatchOverlay;
using NUnit.Framework;
using UnityEngine;

namespace JungleMaze.Tests.World
{
    /// <summary>
    /// Smooth motion: the camera follows the body the player sees (the own PlayerView online) after it moved this
    /// frame, and hands/body art read the root after the view moved, so nothing trails the character by a frame.
    /// </summary>
    public sealed class CameraFollowOrderTests
    {
        private readonly List<Object> _owned = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var o in _owned) if (o != null) Object.DestroyImmediate(o);
            _owned.Clear();
        }

        [Test]
        public void CameraFollowsTheOverride_ThenFallsBackToTheHero()
        {
            var camera = Own(new GameObject("camera")).AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 2f;
            camera.aspect = 1f;
            var follow = camera.gameObject.AddComponent<HeroCameraFollow>();
            var hero = Own(new GameObject("hero")).transform;
            var view = Own(new GameObject("own view")).transform;
            hero.position = new Vector3(10f, 10f, 0f);
            view.position = new Vector3(20f, 15f, 0f);
            follow.Target = hero;

            Step(follow);
            Assert.AreEqual(new Vector2(10f, 10f), (Vector2)camera.transform.position, "offline: the hero");

            follow.FollowOverride = view;
            Step(follow);
            Assert.AreEqual(new Vector2(20f, 15f), (Vector2)camera.transform.position, "online: the own PlayerView");

            view.gameObject.SetActive(false);
            Step(follow);
            Assert.AreEqual(new Vector2(10f, 10f), (Vector2)camera.transform.position, "a despawned view hands the camera back");
        }

        [Test]
        public void LateUpdateOrder_ViewsThenBodyThenHandsThenCameraThenSpectator()
        {
            var view = Order(typeof(PlayerView));
            var body = Order(typeof(HeroSpriteAnimator));
            var hands = Order(typeof(HeldWeaponView));
            var camera = Order(typeof(HeroCameraFollow));
            var spectator = Order(typeof(MatchOverlayCameraDriver));
            Assert.Less(view, body);
            Assert.Less(body, hands);
            Assert.Less(hands, camera);
            Assert.Less(camera, spectator);
        }

        private static int Order(System.Type type)
        {
            var attribute = type.GetCustomAttribute<DefaultExecutionOrder>();
            return attribute != null ? attribute.order : 0;
        }

        private static void Step(HeroCameraFollow follow)
        {
            var awake = typeof(HeroCameraFollow).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance);
            awake.Invoke(follow, null);
            typeof(HeroCameraFollow).GetMethod("LateUpdate", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(follow, null);
        }

        private GameObject Own(GameObject go) { _owned.Add(go); return go; }
    }
}
