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
    /// Server → room, ReliableOrdered in its own frame: the item each player holds, so others can draw items that have no
    /// <see cref="CombatWeaponId"/> (snapshot code <see cref="PlayerStatePacket.HeldOther"/>). Sent when a held item
    /// changes and again every couple of seconds for late joiners. A full list: a missing NetId holds nothing special.
    /// Peers that do not know this packet drop the frame and keep working, so no gameplay version bump is needed.
    /// </summary>
    public struct HeldItemsPacket : INetSerializable
    {
        public const int MaxPlayers = 10;
        public const int MaxItemIdLength = 32;

        public Guid MatchId;
        public uint Tick;
        public byte Count;
        private HeldItemWire _p0, _p1, _p2, _p3, _p4, _p5, _p6, _p7, _p8, _p9;

        public HeldItemWire Get(int index)
        {
            switch (index)
            {
                case 0: return _p0; case 1: return _p1; case 2: return _p2; case 3: return _p3; case 4: return _p4;
                case 5: return _p5; case 6: return _p6; case 7: return _p7; case 8: return _p8; case 9: return _p9;
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        /// <summary>Appends one player's held item; false when the list is full.</summary>
        public bool TryAdd(byte netId, string itemId)
        {
            if (Count >= MaxPlayers) return false;
            var wire = new HeldItemWire { NetId = netId, ItemId = itemId ?? "" };
            switch (Count)
            {
                case 0: _p0 = wire; break; case 1: _p1 = wire; break; case 2: _p2 = wire; break; case 3: _p3 = wire; break;
                case 4: _p4 = wire; break; case 5: _p5 = wire; break; case 6: _p6 = wire; break; case 7: _p7 = wire; break;
                case 8: _p8 = wire; break; default: _p9 = wire; break;
            }
            Count++;
            return true;
        }

        public void Serialize(NetDataWriter writer)
        {
            MatchWire.WriteGuid(writer, MatchId);
            writer.Put(Tick);
            writer.Put(Count);
            for (var i = 0; i < Count; i++)
            {
                var wire = Get(i);
                writer.Put(wire.NetId);
                writer.Put(wire.ItemId ?? "");
            }
        }

        public void Deserialize(NetDataReader reader)
        {
            MatchId = MatchWire.ReadGuid(reader);
            Tick = reader.GetUInt();
            var count = reader.GetByte();
            if (count > MaxPlayers) throw new InvalidOperationException("Too many held items.");
            Count = 0;
            for (var i = 0; i < count; i++) TryAdd(reader.GetByte(), MatchWire.ReadString(reader, MaxItemIdLength));
        }
    }

    /// <summary>One player's held item ID ("" for nothing).</summary>
    public struct HeldItemWire
    {
        public byte NetId;
        public string ItemId;
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

        /// <summary>Marks that the player holds an item with no combat id; its ID travels in <see cref="HeldItemsPacket"/>.</summary>
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
