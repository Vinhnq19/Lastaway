#nullable enable
using System;
using LiteNetLib.Utils;

namespace JungleBackend.Protocol.Gameplay
{
    public enum RoomMapError : byte
    {
        None, NotHost, MatchLocked, UnknownMap, SelectionChanged, NotEnoughPlayers, MapsNotReady
    }

    // SelectionRevision changes only when the map changes, not when another peer finishes downloading.
    public struct RoomMapStatePacket : INetSerializable
    {
        public Guid RoomId;
        public uint SelectionRevision, LobbyRevision;
        public byte HumanCount, BotCount, MaxPlayers, MinPlayers;
        public long CatalogGeneration;
        public string MapId, MapName, MapVersion, MapHash, RulesHash;
        public byte HostNetId, ReadyCount, PlayerCount;
        public bool CanStart;
        public void Serialize(NetDataWriter w)
        {
            MatchWire.WriteGuid(w, RoomId); w.Put(SelectionRevision); w.Put(CatalogGeneration);
            w.Put(MapId ?? ""); w.Put(MapName ?? ""); w.Put(MapVersion ?? ""); w.Put(MapHash ?? ""); w.Put(RulesHash ?? "");
            w.Put(HostNetId); w.Put(ReadyCount); w.Put(PlayerCount); w.Put(CanStart); w.Put(LobbyRevision); w.Put(HumanCount); w.Put(BotCount); w.Put(MaxPlayers); w.Put(MinPlayers);
        }
        public void Deserialize(NetDataReader r)
        {
            RoomId = MatchWire.ReadGuid(r); SelectionRevision = r.GetUInt(); CatalogGeneration = r.GetLong();
            MapId = MatchWire.ReadString(r, 32); MapName = MatchWire.ReadString(r, 64); MapVersion = MatchWire.ReadString(r, 32);
            MapHash = MatchWire.ReadString(r, 64); RulesHash = MatchWire.ReadString(r, 64);
            HostNetId = r.GetByte(); ReadyCount = r.GetByte(); PlayerCount = r.GetByte(); CanStart = r.GetBool(); LobbyRevision = r.GetUInt(); HumanCount = r.GetByte(); BotCount = r.GetByte(); MaxPlayers = r.GetByte(); MinPlayers = r.GetByte();
            if (RoomId == Guid.Empty || LobbyRevision == 0 || MaxPlayers < 1 || MaxPlayers > 10 || MinPlayers < 2 || MinPlayers > 10 ||
                PlayerCount > MaxPlayers || HumanCount + BotCount != PlayerCount ||
                ReadyCount > PlayerCount || CatalogGeneration < 1 ||
                (HostNetId != 255 && HostNetId >= 10))
                throw new InvalidOperationException("Invalid room map state.");
        }
    }

    public struct SelectRoomMapPacket : INetSerializable
    {
        public Guid RoomId; public uint SelectionRevision; public string MapId, MapHash;
        public void Serialize(NetDataWriter w)
        { MatchWire.WriteGuid(w, RoomId); w.Put(SelectionRevision); w.Put(MapId ?? ""); w.Put(MapHash ?? ""); }
        public void Deserialize(NetDataReader r)
        { RoomId = MatchWire.ReadGuid(r); SelectionRevision = r.GetUInt(); MapId = MatchWire.ReadString(r, 32); MapHash = MatchWire.ReadString(r, 64); }
    }

    public struct RoomMapReadyPacket : INetSerializable
    {
        public Guid RoomId; public uint SelectionRevision; public string MapHash, RulesHash;
        public void Serialize(NetDataWriter w)
        { MatchWire.WriteGuid(w, RoomId); w.Put(SelectionRevision); w.Put(MapHash ?? ""); w.Put(RulesHash ?? ""); }
        public void Deserialize(NetDataReader r)
        { RoomId = MatchWire.ReadGuid(r); SelectionRevision = r.GetUInt(); MapHash = MatchWire.ReadString(r, 64); RulesHash = MatchWire.ReadString(r, 64); }
    }

    public struct StartRoomMatchPacket : INetSerializable
    {
        public Guid RoomId; public uint SelectionRevision;
        public void Serialize(NetDataWriter w) { MatchWire.WriteGuid(w, RoomId); w.Put(SelectionRevision); }
        public void Deserialize(NetDataReader r) { RoomId = MatchWire.ReadGuid(r); SelectionRevision = r.GetUInt(); }
    }

    public struct RoomMapCommandResultPacket : INetSerializable
    {
        public Guid RoomId; public RoomMapError Error;
        public void Serialize(NetDataWriter w) { MatchWire.WriteGuid(w, RoomId); w.Put((byte)Error); }
        public void Deserialize(NetDataReader r)
        {
            RoomId = MatchWire.ReadGuid(r); Error = (RoomMapError)r.GetByte();
            if (Error > RoomMapError.MapsNotReady) throw new InvalidOperationException("Invalid room map result.");
        }
    }
}
