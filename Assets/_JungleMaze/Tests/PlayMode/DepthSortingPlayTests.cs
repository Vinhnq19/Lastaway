using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using JungleMaze.Presentation.Player;
using JungleMaze.Presentation.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;
using Is =UnityEngine.TestTools.Constraints.Is;
using Object = UnityEngine.Object;

namespace JungleMaze.Tests.PlayMode
{
    /// <summary>
    /// Depth Sorting trên object Unity thật: vòng đời OnEnable/OnDisable/Destroy, dirty queue một lần mỗi frame,
    /// Trunk/Canopy tách group, Body+Weapon chung group và Hero dùng Rigidbody2D interpolation. Không load scene;
    /// test tự dựng system như Bootstrap và gọi Construct thay cho [Inject].
    /// </summary>
    public sealed class DepthSortingPlayTests
    {
        private const float Precision = DepthSortingSystem.DefaultPrecision;
        private readonly List<Object> _objects = new List<Object>();
        private DepthSortingSystem _system;

        [SetUp] public void SetUp()
        {
            _system = Track(new GameObject("DepthSortingSystem")).AddComponent<DepthSortingSystem>();
        }

        [TearDown] public void TearDown()
        {
            foreach (var o in _objects) if (o != null) Object.Destroy(o);
            _objects.Clear();
        }

        [UnityTest] public IEnumerator StaticEntitySortsOnceOnRegisterThenStaysIdle()
        {
            var rock = Entity("Rock", new Vector2(4, 3));
            Assert.AreEqual(1, _system.DirtyCount);
            yield return null;
            Assert.AreEqual(-300, rock.SortingOrder);
            Assert.AreEqual(-300, rock.SortingGroup.sortingOrder);
            Assert.AreEqual(0, _system.DirtyCount);
            yield return null;
            Assert.AreEqual(0, _system.DirtyCount, "entity đứng yên không được xếp lại");
        }

        [UnityTest] public IEnumerator PlayerWalkingUpGoesBehindTrunkAndDownComesInFront()
        {
            var trunk = Entity("Trunk", new Vector2(5, 5));
            var player = Entity("Player", new Vector2(5, 4));
            yield return null;
            Assert.Greater(player.SortingOrder, trunk.SortingOrder, "đứng dưới gốc cây: vẽ trước thân");

            player.transform.position = new Vector3(5, 6, 0);
            player.MarkDirty();
            yield return null;
            Assert.Less(player.SortingOrder, trunk.SortingOrder, "đi lên qua gốc cây: vẽ sau thân");

            player.transform.position = new Vector3(5, 4.99f, 0);
            player.MarkDirty();
            yield return null;
            Assert.Greater(player.SortingOrder, trunk.SortingOrder);
        }

        [UnityTest] public IEnumerator DiagonalMovementSortsByYOnly()
        {
            var player = Entity("Player", Vector2.zero);
            for (var i = 1; i <= 5; i++)
            {
                player.transform.position = new Vector3(i * .37f, i * -.53f, 0);
                player.MarkDirty();
                yield return null;
                Assert.AreEqual(DepthSortingSystem.ComputeOrder(i * -.53f, Precision), player.SortingOrder);
            }
        }

        [UnityTest] public IEnumerator NpcAndEnemySwapOrderWhenTheyCross()
        {
            var npc = Entity("Npc", new Vector2(0, 2));
            var enemy = Entity("Enemy", new Vector2(.3f, 3));
            yield return null;
            Assert.Greater(npc.SortingOrder, enemy.SortingOrder);
            npc.transform.position = new Vector3(0, 3.5f, 0); npc.MarkDirty();
            enemy.transform.position = new Vector3(.3f, 2.5f, 0); enemy.MarkDirty();
            yield return null;
            Assert.Less(npc.SortingOrder, enemy.SortingOrder);
        }

        [UnityTest] public IEnumerator SortPointOverridesSpriteCentre()
        {
            var statue = Entity("Statue", new Vector2(0, 10));
            var foot = Track(new GameObject("SortPoint")).transform;
            foot.SetParent(statue.transform, false);
            foot.localPosition = new Vector3(0, -1.5f, 0);
            SetField(statue, "sortPoint", foot);
            statue.MarkDirty();
            yield return null;
            Assert.AreEqual(-850, statue.SortingOrder);
        }

        [UnityTest] public IEnumerator CanopyIsOutsideTrunkGroupAndStaysInForeground()
        {
            var tree = Track(new GameObject("Tree")).transform;
            var trunk = Entity("Trunk", new Vector2(2, 2));
            trunk.transform.SetParent(tree, true);
            Renderer(trunk.transform, "TrunkSprite", 0);
            var canopy = Renderer(tree, "Canopy", 0);
            canopy.transform.position = new Vector3(2, 3.5f, 0);
            canopy.sortingLayerID = SortingLayers.IdOf(SortingLayers.Foreground);
            var player = Entity("Player", new Vector2(2, 1));
            yield return null;

            Assert.IsNull(canopy.GetComponentInParent<SortingGroup>(), "canopy không được nằm trong group của trunk");
            Assert.Greater(player.SortingOrder, trunk.SortingOrder, "player trước thân cây");
            Assert.AreEqual(SortingLayers.IdOf(SortingLayers.Foreground), canopy.sortingLayerID);
            Assert.AreEqual(0, canopy.sortingOrder, "system không đụng tới canopy");
            Assert.Greater(SortingLayer.GetLayerValueFromID(canopy.sortingLayerID),
                SortingLayer.GetLayerValueFromID(player.SortingGroup.sortingLayerID), "canopy vẫn che player");
        }

        [UnityTest] public IEnumerator BodyAndWeaponStayGroupedUnderOneOrder()
        {
            var player = Entity("Player", new Vector2(0, 1));
            var body = Renderer(player.transform, "Body", 0);
            var weapon = Renderer(player.transform, "Weapon", 1);
            player.transform.position = new Vector3(0, 7, 0);
            player.MarkDirty();
            yield return null;
            Assert.AreEqual(-700, player.SortingGroup.sortingOrder);
            Assert.AreSame(player.SortingGroup, body.GetComponentInParent<SortingGroup>());
            Assert.AreSame(player.SortingGroup, weapon.GetComponentInParent<SortingGroup>());
            Assert.AreEqual(0, body.sortingOrder, "thứ tự trong group giữ nguyên");
            Assert.AreEqual(1, weapon.sortingOrder);
        }

        [Test] public void RepeatedMarkDirtyIsQueuedOnceAndUnchangedOrderIsNotWritten()
        {
            var player = Entity("Player", new Vector2(0, 2));
            _system.ProcessDirty();
            for (var i = 0; i < 5; i++) player.MarkDirty();
            Assert.AreEqual(1, _system.DirtyCount);
            _system.ProcessDirty();
            Assert.AreEqual(0, _system.DirtyCount);
            Assert.IsFalse(player.ApplySortingOrder(-200), "order không đổi thì không ghi SortingGroup");
        }

        [Test] public void DisabledEntityLeavesQueueAndIgnoresMarkDirtyUntilReEnabled()
        {
            var player = Entity("Player", new Vector2(0, 2));
            Assert.AreEqual(1, _system.DirtyCount);
            player.gameObject.SetActive(false);
            Assert.AreEqual(0, _system.DirtyCount);
            Assert.AreEqual(0, _system.RegisteredCount);
            player.MarkDirty();
            Assert.AreEqual(0, _system.DirtyCount);
            player.gameObject.SetActive(true);
            Assert.AreEqual(1, _system.RegisteredCount);
            Assert.AreEqual(1, _system.DirtyCount, "bật lại thì xếp lại theo vị trí mới");
        }

        [Test] public void RegisterIsIdempotent()
        {
            var player = Entity("Player", new Vector2(0, 2));
            _system.Register(player);
            player.Construct(_system);
            Assert.AreEqual(1, _system.RegisteredCount);
            Assert.AreEqual(1, _system.DirtyCount);
        }

        [UnityTest] public IEnumerator DestroyedEntityIsDroppedWithoutErrors()
        {
            var player = Entity("Player", new Vector2(0, 2));
            var other = Entity("Other", new Vector2(0, 3));
            Object.Destroy(player.gameObject);
            yield return null;
            Assert.AreEqual(1, _system.RegisteredCount);
            other.MarkDirty();
            Object.DestroyImmediate(other.gameObject);
            Assert.DoesNotThrow(() => _system.ProcessDirty());
            Assert.AreEqual(0, _system.RegisteredCount);
            Assert.AreEqual(0, _system.DirtyCount);
        }

        [Test] public void SteadyStateSortingDoesNotAllocate()
        {
            var a = Entity("A", new Vector2(0, 1));
            var b = Entity("B", new Vector2(0, 2));
            _system.ProcessDirty();
            var y = 0f;
            Assert.That(() =>
            {
                y += .01f;
                a.transform.position = new Vector3(0, y, 0);
                a.MarkDirty(); a.MarkDirty(); b.MarkDirty();
                _system.ProcessDirty();
            }, Is.Not.AllocatingGCMemory());
        }

        [UnityTest] public IEnumerator HeroWithInterpolatedRigidbodySortsByFinalFramePose()
        {
            var go = Track(new GameObject("Hero"));
            go.transform.position = new Vector3(10, 10, 0);
            var depth = go.AddComponent<DepthSortable>(); // trước HeroController: Awake của nó tìm DepthSortable
            go.AddComponent<HeroController>();
            var body = go.GetComponent<Rigidbody2D>();
            depth.Construct(_system);
            Assert.AreEqual(RigidbodyInterpolation2D.Interpolate, body.interpolation);

            for (var i = 0; i < 30; i++)
            {
                body.MovePosition(body.position + new Vector2(.02f, .05f));
                yield return new WaitForEndOfFrame(); // sau LateUpdate: pose đã nội suy và order đã ghi
                Assert.AreEqual(DepthSortingSystem.ComputeOrder(go.transform.position.y, Precision), depth.SortingGroup.sortingOrder,
                    $"frame {i}: order phải khớp pose hiển thị của frame");
            }
            // MovePosition chỉ có hiệu lực theo bước physics (ít hơn số frame), nên chỉ kiểm tra là đã đi được một đoạn.
            Assert.Greater(go.transform.position.y, 10.2f, "Hero đã thực sự di chuyển");
        }

        private DepthSortable Entity(string name, Vector2 position)
        {
            var go = Track(new GameObject(name));
            go.transform.position = position;
            var sortable = go.AddComponent<DepthSortable>();
            sortable.SortingGroup.sortingLayerID = SortingLayers.IdOf(SortingLayers.WorldDynamic);
            sortable.Construct(_system);
            return sortable;
        }

        private SpriteRenderer Renderer(Transform parent, string name, int order)
        {
            var go = Track(new GameObject(name));
            go.transform.SetParent(parent, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = order;
            return renderer;
        }

        private GameObject Track(GameObject go)
        {
            _objects.Add(go);
            return go;
        }

        private static void SetField(object target, string field, object value) =>
            target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }
}
