using JungleBackend.Protocol.Gameplay;
using LiteNetLib.Utils;
using NUnit.Framework;

namespace JungleMaze.Tests.Protocol
{
    /// <summary>The held weapon rides in the free high nibble of PlayerStatePacket.Flags: same size, flags untouched.</summary>
    public sealed class HeldWeaponSnapshotTests
    {
        [Test]
        public void HeldWeaponRoundTripsWithoutChangingSizeOrStateFlags()
        {
            var packet = new PlayerStatePacket { Tick = 7, NetId = 3, Hp = 55, Flags = PlayerStateFlags.Dead | PlayerStateFlags.Dashing };
            packet.SetHeldWeapon(CombatWeaponId.Shotgun);
            var writer = new NetDataWriter();
            packet.Serialize(writer);
            Assert.AreEqual(PlayerStatePacket.PayloadBytes, writer.Length);

            var read = new PlayerStatePacket();
            read.Deserialize(new NetDataReader(writer.Data, 0, writer.Length));
            Assert.IsTrue(read.TryGetHeldWeapon(out var weapon));
            Assert.AreEqual(CombatWeaponId.Shotgun, weapon);
            Assert.IsTrue(read.IsDead);
            Assert.AreEqual(PlayerStateFlags.Dead | PlayerStateFlags.Dashing, read.Flags & (PlayerStateFlags)0x0F);
        }

        [Test]
        public void EveryCombatWeaponHasItsOwnCode_HandIncluded()
        {
            for (var i = 0; i <= (int)CombatWeaponId.SniperRifle; i++)
            {
                var packet = new PlayerStatePacket();
                packet.SetHeldWeapon((CombatWeaponId)i);
                Assert.AreEqual(i + 1, packet.HeldCode);
                Assert.IsTrue(packet.TryGetHeldWeapon(out var weapon));
                Assert.AreEqual((CombatWeaponId)i, weapon);
            }
        }

        [Test]
        public void OlderServersSendUnknown_AndNonCombatItemsSendOther()
        {
            var legacy = new PlayerStatePacket { Flags = PlayerStateFlags.Attacking };
            Assert.AreEqual(PlayerStatePacket.HeldUnknown, legacy.HeldCode);
            Assert.IsFalse(legacy.TryGetHeldWeapon(out _));

            var throwable = new PlayerStatePacket { Flags = PlayerStateFlags.Invulnerable };
            throwable.SetHeldOther();
            Assert.AreEqual(PlayerStatePacket.HeldOther, throwable.HeldCode);
            Assert.IsFalse(throwable.TryGetHeldWeapon(out _));
            Assert.AreEqual(PlayerStateFlags.Invulnerable, throwable.Flags & (PlayerStateFlags)0x0F);
        }

        [Test]
        public void HeldItemsPacketRoundTripsTheFullList()
        {
            var packet = new HeldItemsPacket { MatchId = System.Guid.NewGuid(), Tick = 42 };
            Assert.IsTrue(packet.TryAdd(2, "BANANA"));
            Assert.IsTrue(packet.TryAdd(5, ""));
            var writer = new NetDataWriter();
            packet.Serialize(writer);
            var read = new HeldItemsPacket();
            read.Deserialize(new NetDataReader(writer.Data, 0, writer.Length));
            Assert.AreEqual(packet.MatchId, read.MatchId);
            Assert.AreEqual(42u, read.Tick);
            Assert.AreEqual(2, read.Count);
            Assert.AreEqual("BANANA", read.Get(0).ItemId);
            Assert.AreEqual(5, read.Get(1).NetId);
            Assert.AreEqual("", read.Get(1).ItemId);
        }

        [Test]
        public void HeldItemsPacketHoldsAtMostTenPlayers()
        {
            var packet = new HeldItemsPacket();
            for (byte i = 0; i < HeldItemsPacket.MaxPlayers; i++) Assert.IsTrue(packet.TryAdd(i, "SEED"));
            Assert.IsFalse(packet.TryAdd(10, "SEED"));
        }

        [Test]
        public void MatchSessionStoresHeldItemsForKnownPlayers_AndClearsPlayersLeftOutOfTheList()
        {
            using var live = new JungleMaze.Tests.MatchUi.LiveMatchHarness();
            live.Start(0, 3);
            var first = new HeldItemsPacket { MatchId = live.MatchId, Tick = live.Tick };
            first.TryAdd(1, "BANANA");
            first.TryAdd(2, "BELL");
            first.TryAdd(7, "SMOKE");
            live.Network.Push(first);
            Assert.IsTrue(live.Players.TryGet(1, out var one));
            Assert.AreEqual("BANANA", one.HeldItemId);
            Assert.IsTrue(live.Players.TryGet(2, out var two));
            Assert.AreEqual("BELL", two.HeldItemId);
            Assert.IsFalse(live.Players.TryGet(7, out _), "an unknown NetId is not created by the held-items list");

            var older = new HeldItemsPacket { MatchId = live.MatchId, Tick = live.Tick - 1 };
            older.TryAdd(1, "SEED");
            live.Network.Push(older);
            Assert.AreEqual("BANANA", one.HeldItemId, "an older list never overwrites a newer one");

            var next = new HeldItemsPacket { MatchId = live.MatchId, Tick = live.Tick + 1 };
            next.TryAdd(1, "ROPE");
            live.Network.Push(next);
            Assert.AreEqual("ROPE", one.HeldItemId);
            Assert.AreEqual("", two.HeldItemId, "a player missing from the full list holds nothing special");

            var otherMatch = new HeldItemsPacket { MatchId = System.Guid.NewGuid(), Tick = live.Tick + 5 };
            otherMatch.TryAdd(1, "CRYSTAL");
            live.Network.Push(otherMatch);
            Assert.AreEqual("ROPE", one.HeldItemId, "another match's list is ignored");
        }

        [Test]
        public void ReEquippingReplacesTheCodeInsteadOfMixingBits()
        {
            var packet = new PlayerStatePacket();
            packet.SetHeldWeapon(CombatWeaponId.Shotgun);
            packet.SetHeldWeapon(CombatWeaponId.Knife);
            Assert.IsTrue(packet.TryGetHeldWeapon(out var weapon));
            Assert.AreEqual(CombatWeaponId.Knife, weapon);
        }
    }
}
