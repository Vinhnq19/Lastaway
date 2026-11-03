using System;
using LiteNetLib.Utils;

namespace JungleBackend.Protocol.Gameplay
{
    /// <summary>State bits 0-3. Bits 4-7 carry the held weapon code: use <see cref="PlayerStatePacket.HeldCode"/>, never test them as flags.</summary>
    [Flags]
    public enum PlayerStateFlags : byte
    {
        None = 0,
        Dead = 1 << 0,
        Invulnerable = 1 << 1,
        Attacking = 1 << 2,
        Dashing = 1 << 3
    }

    /// <summary>
    /// Server → client, trạng thái authoritative của một người chơi tại Tick. Unreliable/Sequenced.
    /// NetId là slot 0..9 do server cấp (cần bổ sung vào JoinAccepted ở bản server tiếp theo).
    /// </summary>
    public struct PlayerStatePacket : INetSerializable
    {
        public const int PayloadBytes = 19;
        private const float RotationScale = 65536f / 360f;

        public uint Tick;
        public byte NetId;
        public float X, Y;
        /// <summary>Góc quay 0..65535 ứng với 0..360 độ.</summary>
        public ushort Rotation;
        /// <summary>InputSeq cuối server đã xử lý; client dùng để reconcile prediction.</summary>
        public ushort LastProcessedInputSeq;
        public byte Hp;
        public PlayerStateFlags Flags;

        /// <summary>
        /// Held weapon lives in the free high nibble of <see cref="Flags"/> (bits 4-7), so the packet size and the
        /// gameplay version stay the same and older peers ignore it. 0 = unknown (server without this field),
        /// 1..10 = <see cref="CombatWeaponId"/> + 1, 15 = holding an item that has no combat id (e.g. a throwable).
        /// </summary>
        public const byte HeldUnknown = 0, HeldOther = 15;
        private const int HeldShift = 4;
        private const byte LowFlagsMask = 0x0F;

        /// <summary>Raw held-weapon code from the high nibble; see <see cref="HeldUnknown"/>.</summary>
        public byte HeldCode => (byte)((byte)Flags >> HeldShift);

        /// <summary>Stores the held combat weapon without touching the state flags in the low nibble.</summary>
        public void SetHeldWeapon(CombatWeaponId weapon) => SetHeldCode((byte)((byte)weapon + 1));

        /// <summary>Marks that the player holds an item with no combat id.</summary>
        public void SetHeldOther() => SetHeldCode(HeldOther);

        /// <summary>The combat weapon held, when the server sent one.</summary>
        public bool TryGetHeldWeapon(out CombatWeaponId weapon) => TryDecodeHeld(HeldCode, out weapon);

        /// <summary>Decodes a held code: true only for 1..10 (a <see cref="CombatWeaponId"/>).</summary>
        public static bool TryDecodeHeld(byte code, out CombatWeaponId weapon)
        {
            weapon = CombatWeaponId.Hand;
            if (code < 1 || code > (byte)CombatWeaponId.Shotgun + 1) return false;
            weapon = (CombatWeaponId)(code - 1);
            return true;
        }

        private void SetHeldCode(byte code) =>
            Flags = (PlayerStateFlags)(((byte)Flags & LowFlagsMask) | ((code & 0x0F) << HeldShift));

        public float RotationDegrees => Rotation / RotationScale;
        public bool IsDead => (Flags & PlayerStateFlags.Dead) != 0;
        public bool IsValid => !float.IsNaN(X) && !float.IsNaN(Y) && !float.IsInfinity(X) && !float.IsInfinity(Y);

        public void SetRotationDegrees(float degrees)
        {
            if (float.IsNaN(degrees) || float.IsInfinity(degrees)) degrees = 0f;
            var wrapped = degrees % 360f;
            if (wrapped < 0f) wrapped += 360f;
            Rotation = (ushort)((int)Math.Round(wrapped * RotationScale) & 0xFFFF);
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Tick);
            writer.Put(NetId);
            writer.Put(X);
            writer.Put(Y);
            writer.Put(Rotation);
            writer.Put(LastProcessedInputSeq);
            writer.Put(Hp);
            writer.Put((byte)Flags);
        }

        public void Deserialize(NetDataReader reader)
        {
            Tick = reader.GetUInt();
            NetId = reader.GetByte();
            X = reader.GetFloat();
            Y = reader.GetFloat();
            Rotation = reader.GetUShort();
            LastProcessedInputSeq = reader.GetUShort();
            Hp = reader.GetByte();
            Flags = (PlayerStateFlags)reader.GetByte();
        }
    }
}
