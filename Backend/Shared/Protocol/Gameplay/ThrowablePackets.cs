using System;
using LiteNetLib.Utils;

namespace JungleBackend.Protocol.Gameplay
{
    /// <summary>
    /// Client → server, ReliableOrdered (WPN-03). Uses the held thrown or special weapon. <see cref="Kind"/> must match
    /// what the server sees in the player's hand; <see cref="Distance"/> is where a thrown weapon should land (the
    /// server clamps it to range and cover). RequestId increases per player and guards against duplicates.
    /// Values of <see cref="Kind"/> follow JungleMaze.World.Throwables.ThrowableKind (1 Banana … 8 Sahur bat).
    /// </summary>
    public struct ThrowableRequestPacket : INetSerializable
    {
        public const byte MaxKind = 8;
        public Guid MatchId;
        public uint RequestId;
        public byte Kind;
        /// <summary>Aim angle in 0..65535 for 0..360 degrees, same scale as PlayerInputPacket.Aim.</summary>
        public ushort Aim;
        public float Distance;

        public void Serialize(NetDataWriter w)
        {
            WorldWire.Header(w, MatchId);
            w.Put(RequestId); w.Put(Kind); w.Put(Aim); w.Put(Distance);
        }

        public void Deserialize(NetDataReader r)
        {
            MatchId = WorldWire.Header(r);
            RequestId = r.GetUInt(); Kind = r.GetByte(); Aim = r.GetUShort(); Distance = r.GetFloat();
            WorldWire.Check(RequestId != 0 && Kind >= 1 && Kind <= MaxKind && WorldWire.Finite(Distance) && Distance >= 0 && Distance <= 64);
        }
    }

    /// <summary>
    /// Server → whole room, ReliableOrdered (WPN-03). One confirmed throwable event (throw, detonation, trap, smoke,
    /// rope pull, bell, bat hit, damage/control on a target). Clients only draw it; HP and effects arrive through the
    /// usual snapshots and inventory state. Event and item codes follow JungleMaze.World.Throwables.
    /// </summary>
    public struct ThrowableEventPacket : INetSerializable
    {
        public const byte MaxEvent = 14;
        public Guid MatchId;
        public uint Tick;
        public byte Event;
        public byte Item;
        public byte Actor;
        /// <summary>Affected player, or 255 when none.</summary>
        public byte Target;
        public ushort Id;
        public float FromX, FromY, ToX, ToY;
        public float Seconds;
        public float Size;
        public short Amount;
        public byte Flags;

        public void Serialize(NetDataWriter w)
        {
            WorldWire.Header(w, MatchId);
            w.Put(Tick); w.Put(Event); w.Put(Item); w.Put(Actor); w.Put(Target); w.Put(Id);
            w.Put(FromX); w.Put(FromY); w.Put(ToX); w.Put(ToY); w.Put(Seconds); w.Put(Size); w.Put(Amount); w.Put(Flags);
        }

        public void Deserialize(NetDataReader r)
        {
            MatchId = WorldWire.Header(r);
            Tick = r.GetUInt(); Event = r.GetByte(); Item = r.GetByte(); Actor = r.GetByte(); Target = r.GetByte(); Id = r.GetUShort();
            FromX = r.GetFloat(); FromY = r.GetFloat(); ToX = r.GetFloat(); ToY = r.GetFloat(); Seconds = r.GetFloat(); Size = r.GetFloat();
            Amount = r.GetShort(); Flags = r.GetByte();
            WorldWire.Check(Event >= 1 && Event <= MaxEvent && Item >= 1 && Item <= ThrowableRequestPacket.MaxKind &&
                (Actor < 10 || Actor == 255) && (Target < 10 || Target == 255) &&
                WorldWire.Finite(FromX) && WorldWire.Finite(FromY) && WorldWire.Finite(ToX) && WorldWire.Finite(ToY) &&
                WorldWire.Finite(Seconds) && Seconds >= 0 && WorldWire.Finite(Size) && Size >= 0);
        }
    }
}
