using System.Collections.Generic;
using JungleMaze.Infrastructure.Config;
using JungleMaze.Presentation.Combat;
using JungleMaze.World.Throwables;
using NUnit.Framework;
using UnityEngine;

namespace JungleMaze.Tests.Combat
{
    /// <summary>WPN-03 client: packaged rules, art catalog and the effect lifecycle of <see cref="ThrowableView"/>.</summary>
    public sealed class ThrowableClientTests
    {
        private readonly List<Object> _owned = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var o in _owned) if (o != null) Object.DestroyImmediate(o);
            _owned.Clear();
        }

        [Test]
        public void PackagedRuntimeWeapons_GiveTheServerRulesForAllEight()
        {
            var rules = ThrowableRulesJson.Load(Application.streamingAssetsPath);
            Assert.AreEqual(8, rules.Count);
            Assert.IsTrue(rules.TryGet(ThrowableKind.Seed, out var seed) && seed.RootSeconds == 1.5);
            Assert.IsTrue(rules.TryGet("SAHUR_BAT", out var bat) && bat.Charges == 3 && bat.KnockbackCells == 3);
            Assert.IsTrue(rules.TryGet(ThrowableKind.Smoke, out var smoke) && smoke.AreaWidth == 7 && smoke.Damage == 0);
        }

        [Test]
        public void ArtCatalog_HasFlightArtForEveryWeapon_AndBlastOrCloudWhereTheRulesNeedOne()
        {
            var art = Resources.Load<ThrowableArtCatalog>(ThrowableArtCatalog.ResourceName);
            Assert.IsNotNull(art, "built by Jungle Maze → Art → Build Throwable Art");
            for (var kind = ThrowableKind.Banana; kind <= ThrowableKind.Bat; kind++)
            {
                Assert.IsTrue(art.TryGet(ThrowableRules.IdOf(kind), out var entry), kind.ToString());
                Assert.IsTrue(entry.Flight.IsSet && entry.Flight.Content > 0f, kind + " flight art");
            }
            foreach (var id in new[] { "BANANA", "METEORITE_FRAGMENT", "CRYSTAL", "SEED" })
            {
                art.TryGet(id, out var entry);
                Assert.IsTrue(entry.Blast.IsSet, id + " blast art");
            }
            art.TryGet("BANANA", out var banana);
            Assert.IsTrue(banana.Linger.IsSet, "banana peel");
            Assert.AreEqual(8, banana.Blast.Frames.Sprites.Length, "banana explosion sliced into its 8 frames");
            art.TryGet("SMOKE", out var smoke);
            Assert.IsTrue(smoke.Linger.IsSet && smoke.Linger.Frames.Loop, "looping smoke cloud");
        }

        [Test]
        public void View_FollowsEachEffectFromThrowToCleanup()
        {
            var view = NewView();
            view.OnEvent(Event(ThrowableEventKind.Thrown, ThrowableKind.Banana, 7, 0, 0, 3, 0, 1.5f, 3f));
            Assert.AreEqual(1, view.ActiveEffects, "the banana flies");
            view.Advance(1.5f);
            Assert.AreEqual(1, view.ActiveEffects, "and waits on the ground for its fuse");
            view.OnEvent(Event(ThrowableEventKind.Detonated, ThrowableKind.Banana, 7, 3, 0, 3, 0, 0f, 3f));
            view.OnEvent(Event(ThrowableEventKind.TrapArmed, ThrowableKind.Banana, 7, 3, 0, 3, 0, 20f, 1.2f));
            Assert.AreEqual(2, view.ActiveEffects, "blast replaces the item, peel stays");
            view.Advance(1f);
            Assert.AreEqual(1, view.ActiveEffects, "blast animation ends");
            view.OnEvent(Event(ThrowableEventKind.TrapTriggered, ThrowableKind.Banana, 7, 3, 0, 3, 0, 1f, 0f));
            Assert.AreEqual(0, view.ActiveEffects, "stepped-on peel disappears");

            view.OnEvent(Event(ThrowableEventKind.SmokeStarted, ThrowableKind.Smoke, 9, 5, 5, 5, 5, 10f, 7f));
            Assert.AreEqual(1, view.ActiveEffects);
            view.OnEvent(Event(ThrowableEventKind.SmokeEnded, ThrowableKind.Smoke, 9, 5, 5, 5, 5, 0f, 7f));
            Assert.AreEqual(0, view.ActiveEffects, "cloud cleared with the server's end event");

            view.OnEvent(Event(ThrowableEventKind.RopeMissed, ThrowableKind.Rope, 0, 0, 0, 8, 0, 0f, 0f));
            view.OnEvent(Event(ThrowableEventKind.BellRung, ThrowableKind.Bell, 0, 0, 0, 0, 0, 10f, 7f));
            Assert.AreEqual(3, view.ActiveEffects, "rope line, bell ring and the bell above the ringer");
            view.Advance(1f);
            Assert.AreEqual(0, view.ActiveEffects, "short effects time out on their own");
        }

        [Test]
        public void View_SeedRootDrawsVinesOnlyOnAHitTarget()
        {
            var view = NewView();
            view.OnEvent(Event(ThrowableEventKind.Hit, ThrowableKind.Seed, 4, 2, 2, 2, 2, 1.5f, 0f, target: 3));
            Assert.AreEqual(1, view.ActiveEffects);
            view.OnEvent(Event(ThrowableEventKind.Hit, ThrowableKind.Crystal, 5, 2, 2, 2, 2, 0f, 0f, target: 3));
            Assert.AreEqual(1, view.ActiveEffects, "plain damage hits leave no lingering art");
            view.Advance(1.6f);
            Assert.AreEqual(0, view.ActiveEffects, "vines last as long as the root");
        }

        [Test]
        public void View_RecyclesEffectObjects()
        {
            var view = NewView();
            for (var round = 0; round < 3; round++)
            {
                view.OnEvent(Event(ThrowableEventKind.Detonated, ThrowableKind.Meteorite, (ushort)round, 0, 0, 0, 0, 0f, 3f));
                view.Advance(1f);
            }
            Assert.AreEqual(1, view.transform.childCount, "one pooled object reused for every blast");
        }

        private ThrowableView NewView()
        {
            var go = new GameObject("throwables");
            _owned.Add(go);
            var view = go.AddComponent<ThrowableView>();
            view.Bind(null, null, null);
            return view;
        }

        private static ThrowableEvent Event(ThrowableEventKind kind, ThrowableKind item, ushort id, float fx, float fy, float tx, float ty, float seconds, float size, byte target = ThrowableEvent.NoTarget) =>
            new ThrowableEvent { Kind = kind, Item = item, Actor = 0, Target = target, Id = id, FromX = fx, FromY = fy, ToX = tx, ToY = ty, Seconds = seconds, Size = size };
    }
}
