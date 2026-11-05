#nullable enable
using System;
using LiteNetLib.Utils;

namespace JungleBackend.Protocol.Gameplay
{
    public enum RoomBotAction : byte { Add, Remove, FillMinimum }
    public enum RoomBotError : byte { None, NotHost, MatchLocked, StaleRoom, RoomFull, ServerFull, UnknownBot, InvalidRequest }
    public struct RoomBotCommandPacket : INetSerializable
    {
        public Guid RoomId;
        public uint Revision, RequestId, BotId;
        public RoomBotAction Action;
        public void Serialize(NetDataWriter w)
        { MatchWire.WriteGuid(w, RoomId); w.Put(Revision); w.Put(RequestId); w.Put(BotId); w.Put((byte)Action); }
        public void Deserialize(NetDataReader r)
        {
            RoomId = MatchWire.ReadGuid(r); Revision = r.GetUInt(); RequestId = r.GetUInt(); BotId = r.GetUInt(); Action = (RoomBotAction)r.GetByte();
            // The wire stores the action as a byte, so only values above the last member can be rejected here.
            var invalidBot = Action == RoomBotAction.Remove ? BotId == 0 : BotId != 0;
            if (RoomId == Guid.Empty || RequestId == 0 || Action > RoomBotAction.FillMinimum || invalidBot)
                throw new InvalidOperationException("Invalid bot command.");
        }
    }
    public struct RoomBotCommandResultPacket : INetSerializable
    {
        public Guid RoomId;
        public uint RequestId;
        public RoomBotError Error;
        public void Serialize(NetDataWriter w) { MatchWire.WriteGuid(w, RoomId); w.Put(RequestId); w.Put((byte)Error); }
        public void Deserialize(NetDataReader r)
        {
            RoomId = MatchWire.ReadGuid(r); RequestId = r.GetUInt(); Error = (RoomBotError)r.GetByte();
            if (RoomId == Guid.Empty || RequestId == 0 || Error > RoomBotError.InvalidRequest)
                throw new InvalidOperationException("Invalid bot result.");
        }
    }
}
