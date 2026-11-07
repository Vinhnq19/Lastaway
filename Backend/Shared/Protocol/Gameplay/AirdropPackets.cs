using System;
using LiteNetLib.Utils;
namespace JungleBackend.Protocol.Gameplay
{
    /// <summary>Reliable per-drop state. Baseline restores visuals without replaying one-shot audio.</summary>
    public struct AirdropStatePacket : INetSerializable
    {
        public Guid MatchId;
        public ushort DropId, WorldId, MaxHp;
        public byte Phase;
        public uint Revision, Tick, FlyTick, FallTick, LandTick;
        public float Size, X, Y, EntryX, EntryY, ExitX, ExitY;
        public bool Baseline;
        public void Serialize(NetDataWriter w)
        {
            MatchWire.WriteGuid(w,MatchId);w.Put(DropId);w.Put(WorldId);w.Put(MaxHp);w.Put(Size);w.Put(Phase);w.Put(Revision);w.Put(Tick);
            w.Put(FlyTick);w.Put(FallTick);w.Put(LandTick);w.Put(X);w.Put(Y);w.Put(EntryX);w.Put(EntryY);w.Put(ExitX);w.Put(ExitY);w.Put(Baseline);
        }
        public void Deserialize(NetDataReader r)
        {
            MatchId=MatchWire.ReadGuid(r);DropId=r.GetUShort();WorldId=r.GetUShort();MaxHp=r.GetUShort();Size=r.GetFloat();Phase=r.GetByte();Revision=r.GetUInt();Tick=r.GetUInt();
            FlyTick=r.GetUInt();FallTick=r.GetUInt();LandTick=r.GetUInt();X=r.GetFloat();Y=r.GetFloat();EntryX=r.GetFloat();EntryY=r.GetFloat();ExitX=r.GetFloat();ExitY=r.GetFloat();Baseline=r.GetBool();
            if(MatchId==Guid.Empty||DropId==0||MaxHp==0||!WorldWire.Finite(Size)||Size<=0||Phase>5||!WorldWire.Finite(X)||!WorldWire.Finite(Y)||!WorldWire.Finite(EntryX)||!WorldWire.Finite(EntryY)||!WorldWire.Finite(ExitX)||!WorldWire.Finite(ExitY)||FallTick<FlyTick||LandTick<FallTick)
                throw new InvalidOperationException("Invalid airdrop state.");
        }
    }
}
