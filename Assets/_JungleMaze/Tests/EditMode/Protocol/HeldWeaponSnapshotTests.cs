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
            for (var i = 0; i <= (int)CombatWeaponId.Shotgun; i++)
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
