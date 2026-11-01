using System;
using System.Linq;
using System.Collections.Generic;
using JungleBackend.Protocol.Gameplay;
using JungleMaze.App.Combat;
using JungleMaze.Tests.App;
using JungleMaze.App.Flow;
using JungleMaze.App.Hud.State;
using JungleMaze.App.Match;
using JungleMaze.App.Player;
using JungleMaze.App.Ports;
using JungleMaze.App.World;
using JungleMaze.Core.Events;
using JungleMaze.Domain.Hud;
using JungleMaze.Domain.Network;
using JungleMaze.Domain.Player;
using JungleMaze.Infrastructure.Config;
using JungleMaze.World;
using JungleMaze.World.Inventory;
using LiteNetLib.Utils;
using NUnit.Framework;
using UnityEngine;

namespace JungleMaze.Tests.World
{
    public sealed class OfflineWorldSessionTests
    {
        private sealed class DummyPacketSender : IPacketSender
        {
            public bool Send<T>(ref T packet, PacketDelivery delivery) where T : struct, INetSerializable => true;
        }

        private sealed class DummyRuntimeData : IClientRuntimeData
        {
            public IReadOnlyList<RuntimeCover> Covers => Array.Empty<RuntimeCover>();
            public RuntimeMapBounds Bounds => new RuntimeMapBounds { MinX = 0, MinY = 0, MaxX = 50, MaxY = 50 };
            public float PlayerSpeed => 4f;
            public float PlayerRadius => 0.4f;
            public ZoneSchedule ZoneSchedule => null;
            public string LoadLocal() => null;
            public string Verify(string version, string hash) => null;
        }

        private sealed class DummyScenes : ISceneLoader
        {
            public string ActiveScene { get; private set; } = "Boot";
            public void Load(string sceneName, Action completed = null) { ActiveScene = sceneName; completed?.Invoke(); }
        }

        private sealed class TestRig : IDisposable
        {
            public readonly EventBus Bus;
            public readonly FakeNetwork Network;
            public readonly DummyScenes Scenes;
            public readonly PlayerInputService Input;
            public readonly GameFlow Flow;
            public readonly BalanceConfigProvider Balance;
            public readonly HudStateStore Hud;
            public readonly EffectStateStore Effects;
            public readonly InventoryStateStore InventoryState;
            public readonly InventoryHudBinding Binding;
            public readonly OfflineMatchSession Match;
            public readonly ItemCatalog Catalog;
            public readonly LootTable Loot;
            public readonly MapLayout Layout;
            public readonly WorldConfigData ConfigData;
            public readonly OfflineWorldSession Session;

            public TestRig()
            {
                Bus = new EventBus();
                Network = new FakeNetwork(Bus);
                Scenes = new DummyScenes();
                Input = new PlayerInputService(Network);
                Flow = new GameFlow(Bus, Network, Network, Scenes, Input, new GameFlowSettings { ResultsSeconds = 1f });
                Flow.Initialize();
                Flow.IsOffline = true;

                Balance = new BalanceConfigProvider(Application.streamingAssetsPath, null);
                Hud = new HudStateStore(Bus, Balance);
                Effects = new EffectStateStore(Bus);
                InventoryState = new InventoryStateStore(Bus, Balance);
                Binding = new InventoryHudBinding(InventoryState, Hud, false);

                var runtime = new DummyRuntimeData();
                Match = new OfflineMatchSession(Balance, null, runtime, Hud, Bus, Flow, Hud);
                Match.StartRound();

                Catalog = ItemCatalog.CreateBaseline();
                Loot = new LootTable(Catalog);

                Layout = new MapLayout { Version = "test-layout", Seed = 20261028, Size = 50 };
                Layout.Spawns.Add(new WorldPoint(25f, 25f));
                Layout.Objects.Add(new WorldObjectDefinition
                {
                    Id = 101,
                    Kind = WorldObjectKind.Chest,
                    Bounds = new WorldBounds(25f, 25f, 1f, 1f)
                });

                ConfigData = new WorldConfigData
                {
                    Layout = Layout,
                    Map = new MapLayoutConfig { Size = 50 },
                    Destructibles = new DestructibleConfig { ChestHp = 30, PotHp = 20, SupplyHp = 150 },
                    Hazards = new HazardConfig { PoisonDamage = 5 }
                };

                Session = new OfflineWorldSession(Match, Layout, ConfigData.Destructibles, ConfigData.Hazards, Catalog, Loot, Balance, Hud, InventoryState, Effects, Flow, Binding);
            }

            public void Dispose()
            {
                Session.Dispose();
                Match.Dispose();
                Flow.Dispose();
            }
        }

        [Test]
        public void WalkingAudio_UsesActualPoseAndSurface_NotIdleOrPortalJump()
        {
            using var rig = new TestRig();
            rig.Layout.Objects.Add(new WorldObjectDefinition { Id = 102, Kind = WorldObjectKind.Swamp, Bounds = new WorldBounds(27, 25, 2, 2) });
            rig.Layout.Objects.Add(new WorldObjectDefinition { Id = 103, Kind = WorldObjectKind.Bush, Bounds = new WorldBounds(27, 25, 2, 2) });
            rig.Session.Reset();
            rig.Session.Tick(.034f);
            var audio = new JungleMaze.App.Audio.MatchAudioEventSource(null,
                new JungleMaze.App.Audio.MatchSessionAudioFeed(null, rig.Match), null, rig.Match);
            using (audio)
            {
                var cues = new List<JungleMaze.App.Audio.AudioCue>(); audio.CueRaised += cues.Add;
                audio.BindCombat(null, rig.Session, null, rig.Hud);
                rig.Hud.SetPose(25, 25, 0); audio.Tick(.034f);
                for (var i=0; i<50; i++) audio.Tick(.034f);
                Assert.AreEqual(0, cues.Count);
                rig.Hud.SetPose(26.1f, 25, 0); audio.Tick(.034f);
                Assert.IsTrue(cues.Any(c=>c.Id==JungleMaze.Domain.Audio.SfxId.FootstepSwamp));
                Assert.AreEqual(1, cues.Count(c=>c.Id==JungleMaze.Domain.Audio.SfxId.BushEnter));
                audio.Tick(.034f);
                var count = cues.Count;
                rig.Hud.SetPose(40, 40, 0); audio.Tick(.034f);
                Assert.AreEqual(count, cues.Count, "Teleport is not a walking step");
                rig.Match.Leave();rig.Hud.SetPose(41.1f,40,0);audio.Tick(.034f);
                Assert.AreEqual(count, cues.Count, "Menu cannot continue footsteps");
            }
        }

        [Test]
        public void RealOfflineAttack_RebindingAudioDoesNotDuplicate_AndDisposeUnsubscribes()
        {
            using var rig = new TestRig();
            var feed = new JungleMaze.App.Audio.MatchSessionAudioFeed(null, rig.Match);
            var audio = new JungleMaze.App.Audio.MatchAudioEventSource(null, feed, null, rig.Match);
            var cues = new List<JungleMaze.App.Audio.AudioCue>();
            audio.CueRaised += cues.Add;
            audio.BindCombat(null, rig.Session); audio.BindCombat(null, rig.Session);
            rig.Session.Tick(.034f);
            Assert.IsTrue(rig.Session.TryAttack(0));
            Assert.AreEqual(1, cues.Count);
            Assert.AreEqual(JungleMaze.Domain.Audio.SfxId.MeleeSwing, cues[0].Id);
            audio.Dispose();
            rig.Session.Tick(2);
            Assert.IsTrue(rig.Session.TryAttack(0));
            Assert.AreEqual(1, cues.Count, "Null packet receiver must not prevent combat unsubscription");
        }

        [Test]
        public void ZoneDeathDropsInventory_AndMenuCannotKeepDamagingOrAttacking()
        {
            using var rig = new TestRig();
            rig.Session.Floor.TrySpawn(new ItemStack("AXE"),25,25,out var id);
            Assert.IsTrue(rig.Session.Pickup(id));
            rig.Match.ApplyDamage(100); // Same sole HP owner that receives safe-zone damage.
            rig.Session.Tick(.034f);
            Assert.IsTrue(rig.Match.HasResult);
            Assert.IsTrue(rig.Session.Floor.Items.Any(i=>i.Stack.ItemId=="AXE"));
            rig.Match.Replay();rig.Match.Leave();rig.Flow.ReturnToMenu();
            var hp=rig.Match.PlayerHp;
            rig.Session.Tick(2);rig.Session.RequestDamage(0,1,99,100,true);
            Assert.AreEqual(hp,rig.Match.PlayerHp);
            Assert.IsFalse(rig.Session.TryAttack(0));
            Assert.IsFalse(rig.Session.IsActive);
        }

        [Test]
        public void SoloZoneDamageUsesLifePassive_AndSlowHasOneMovementMultiplier()
        {
            using var rig = new TestRig();
            rig.Session.Floor.TrySpawn(new ItemStack("ELIXIR_OF_LIFE"),25,25,out var id);
            Assert.IsTrue(rig.Session.Pickup(id));
            rig.Match.ApplyDamage(150);
            Assert.IsFalse(rig.Match.IsDead);Assert.AreEqual(50,rig.Match.PlayerHp);
            rig.Session.ApplyEffect(0,101,"Slow",1,float.PositiveInfinity,.6f);
            rig.Session.ApplyEffect(0,102,"Slow",1,float.PositiveInfinity,.6f);
            Assert.AreEqual(.6f,rig.Session.MovementMultiplier,.001f);
            rig.Session.RemoveEffect(0,101,"Slow",2);
            Assert.AreEqual(.6f,rig.Session.MovementMultiplier,.001f);
            rig.Session.RemoveEffect(0,102,"Slow",2);
            Assert.AreEqual(1f,rig.Session.MovementMultiplier,.001f);
        }

        [Test]
        public void DestroyOnce_SpawnsLootOnlyFirstTime_CannotDestroyBrokenTwice()
        {
            using var rig = new TestRig();
            var session = rig.Session;

            Assert.IsFalse(session.Destructibles.IsBroken(101));
            Assert.AreEqual(0, session.Floor.Items.Count);

            // Hit 1: 30 damage breaks the chest
            var broken = session.Destructibles.DamageConfirmed(101, 30, 0, 1);
            Assert.IsTrue(broken);
            Assert.IsTrue(session.Destructibles.IsBroken(101));

            var itemCount = session.Floor.Items.Count;
            Assert.Greater(itemCount, 0, "Breaking chest rolled and spawned one-time loot onto floor.");

            // Hit 2: Attempting to damage an already broken object must fail and not spawn loot again
            var brokenAgain = session.Destructibles.DamageConfirmed(101, 30, 0, 2);
            Assert.IsFalse(brokenAgain);
            Assert.AreEqual(itemCount, session.Floor.Items.Count, "Broken chest cannot be destroyed twice.");
        }

        [Test]
        public void Inventory_PickupSelectUseDrop_WorksCorrectly()
        {
            using var rig = new TestRig();
            var session = rig.Session;

            Assert.IsTrue(session.Floor.TrySpawn(new ItemStack("AXE"), 25.2f, 25f, out var axeFloorId));
            Assert.IsTrue(session.Floor.TrySpawn(new ItemStack("ELIXIR", 1), 25f, 25.2f, out var elixirFloorId));

            // Pickup Axe
            Assert.IsTrue(session.Pickup(axeFloorId));
            Assert.AreEqual("AXE", session.Inventory.Weapons[0].ItemId);
            Assert.IsFalse(session.Floor.TryGet(axeFloorId, out _));

            // Select Axe
            Assert.IsTrue(session.SelectWeapon(0));
            Assert.AreEqual(0, session.Inventory.ActiveWeapon);

            // Pickup Elixir
            Assert.IsTrue(session.Pickup(elixirFloorId));
            Assert.AreEqual("ELIXIR", session.Inventory.Consumables[0].ItemId);

            // Use Elixir
            Assert.IsTrue(session.UseConsumable(0));
            Assert.IsTrue(session.Inventory.IsUsing);

            // Drop Axe
            Assert.IsTrue(session.DropWeapon(0));
            Assert.IsTrue(session.Inventory.Weapons[0].IsEmpty);
            Assert.AreEqual(1, session.Floor.Items.Count(i => i.Stack.ItemId == "AXE"));
        }

        [Test]
        public void Hazards_Armor_HpDamage_AndLifePassiveRescue()
        {
            using var rig = new TestRig();
            var session = rig.Session;
            var match = rig.Match;

            // Equip Armor (MAGIC_ARMOR has 0.5 reduction)
            Assert.IsTrue(session.Floor.TrySpawn(new ItemStack("MAGIC_ARMOR", 1, 100), 25.1f, 25f, out var armorId));
            Assert.IsTrue(session.Pickup(armorId));
            Assert.AreEqual("MAGIC_ARMOR", session.Inventory.Armor.ItemId);

            // Initial HP is 100
            Assert.AreEqual(100, match.PlayerHp);

            // Non-bypassing damage: 20 raw damage absorbed by 50% = 10 damage dealt
            session.RequestDamage(0, 999, 1, 20, false);
            Assert.AreEqual(90, match.PlayerHp);
            Assert.Less(session.Inventory.Armor.Durability, 100f, "Armor took wear from absorbed damage.");

            // Give player ELIXIR_OF_LIFE passive
            Assert.IsTrue(session.Floor.TrySpawn(new ItemStack("ELIXIR_OF_LIFE", 1), 25.1f, 25f, out var elixirLifeId));
            Assert.IsTrue(session.Pickup(elixirLifeId));

            // Fatal damage (200 damage, bypassing armor)
            session.RequestDamage(0, 999, 2, 200, true);
            Assert.IsFalse(match.IsDead, "Elixir of Life consumed to rescue hero from lethal damage.");
            Assert.Greater(match.PlayerHp, 0, "Hero has restored HP from rescue.");
        }

        [Test]
        public void DeathDrop_DropsInventoryOnZeroHp_AndDisablesInput()
        {
            using var rig = new TestRig();
            var session = rig.Session;
            var match = rig.Match;

            Assert.IsTrue(session.Floor.TrySpawn(new ItemStack("AXE"), 25.1f, 25f, out var axeId));
            Assert.IsTrue(session.Pickup(axeId));
            Assert.AreEqual("AXE", session.Inventory.Weapons[0].ItemId);

            // Deal lethal damage with no life passive
            session.RequestDamage(0, 999, 1, 150, true);
            Assert.IsTrue(match.IsDead);
            Assert.AreEqual(0, match.PlayerHp);

            // Items dropped on death
            Assert.IsTrue(session.Floor.Items.Any(i => i.Stack.ItemId == "AXE"), "Inventory dropped to floor on death.");

            // Disabled inputs when dead
            Assert.IsFalse(session.TryAttack(0f));
            Assert.IsFalse(session.TryReload());
            Assert.IsFalse(session.Pickup(999));
            Assert.IsFalse(session.SelectWeapon(0));
            Assert.IsFalse(session.UseConsumable(0));
            Assert.IsFalse(session.DropWeapon(0));
        }

        [Test]
        public void Replay_ResetClearsStaleState_NewMatchId()
        {
            using var rig = new TestRig();
            var session = rig.Session;
            var match = rig.Match;

            var match1 = match.MatchId;
            Assert.AreNotEqual(Guid.Empty, match1);

            Assert.IsTrue(session.Floor.TrySpawn(new ItemStack("AXE"), 25.1f, 25f, out var axeId));
            Assert.IsTrue(session.Pickup(axeId));
            Assert.AreEqual("AXE", session.Inventory.Weapons[0].ItemId);

            session.RequestDamage(0, 999, 1, 30, true);
            Assert.AreEqual(70, match.PlayerHp);

            // Trigger Replay
            match.Replay();

            var match2 = match.MatchId;
            Assert.AreNotEqual(match1, match2, "Replay generates a fresh MatchId.");
            Assert.AreEqual(100, match.PlayerHp, "HP reset to full.");
            Assert.IsFalse(match.IsDead);

            Assert.IsTrue(session.Inventory.Weapons[0].IsEmpty, "Inventory reset on replay without stale items.");
            Assert.AreEqual(0, session.Floor.Items.Count, "Floor reset without stale items.");
        }
    }
}
