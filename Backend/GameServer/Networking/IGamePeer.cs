namespace GameServer.Networking;

/// <summary>Transport không sở hữu room/identity. Chỉ SessionHub thay đổi dữ liệu phiên.</summary>
public interface IGamePeer
{
    string Transport { get; }
    bool IsOpen { get; }
    bool TrySend(byte[] message, bool reliable);
    bool TrySend(byte[] message, bool reliable, byte channel) => TrySend(message, reliable);
    // Bulk streams yield to reliable control traffic; false means retry the same packet later.
    bool TrySendBulk(byte[] message, byte channel) => TrySend(message, true, channel);
    void Close(string reason);
}
