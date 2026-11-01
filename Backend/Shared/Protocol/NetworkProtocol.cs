#nullable enable
using System;
using LiteNetLib.Utils;

// Package com.junglemaze.protocol: GameServer/NetSmokeClient Compile Link, Unity nạp qua file: package.
// Giữ cú pháp C# 9 (Unity 6) — không file-scoped namespace, không API chỉ có trên net8.
namespace JungleBackend.Protocol
{
    public enum JoinAuthMode : byte { RoomCode = 0, Jwt = 1 }

    public sealed record JoinRequest(ushort Version, JoinAuthMode Mode, string RoomCode, string DisplayName, Guid AttemptId = default, Guid ResumeToken = default, string Ticket = "")
    {
        // Không để record.ToString() vô tình in mã phòng, ticket hoặc proof ra log.
        public override string ToString() => $"JoinRequest(version={Version}, mode={Mode})";
    }

    /// <summary>Hợp đồng wire dùng chung qua Compile Link; thay thế hoàn toàn key smoke SRV-01.</summary>
    public static class NetworkProtocol
    {
        public const byte ExplicitQuit = 0x7e;
        public const ushort Version = 1;
        public const ushort LatestVersion = 2;
        public const ushort Version3 = 3;
        public const ushort Version4 = 4;
        public const byte Ping = 1, Pong = 2, Auth = 3, JoinAccepted = 4, JoinRejected = 5, AuthRequired = 6;
        public const int PacketBytes = 5, MaxConnectBytes = 256, MaxConnectV4Bytes = 512, MaxAuthBytes = 4096;
        public const int MaxNameChars = 32, MaxRoomChars = 64, MaxTokenChars = 2048, MaxTicketChars = 64;

        private static bool IsValidField(string? value, int maxChars)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > maxChars) return false;
            foreach (var ch in value)
                if (char.IsControl(ch)) return false;
            return true;
        }

        private static bool IsValidTicket(string? ticket)
        {
            if (ticket is null || ticket.Length != MaxTicketChars) return false;
            foreach (var ch in ticket)
            {
                if ((ch >= '0' && ch <= '9') || (ch >= 'a' && ch <= 'f') || (ch >= 'A' && ch <= 'F'))
                    continue;
                return false;
            }
            return true;
        }

        public static NetDataWriter WriteConnect(JoinAuthMode mode, string roomCode, string displayName,
            ushort version = Version, Guid attemptId = default, Guid resumeToken = default, string ticket = "")
        {
            if (!IsValidField(roomCode, MaxRoomChars) || !IsValidField(displayName, MaxNameChars))
                throw new ArgumentException("Mã phòng cần 1–64 ký tự, tên hiển thị cần 1–32 ký tự.");
            if (version is not (Version or LatestVersion or Version3 or Version4))
                throw new ArgumentException("Protocol version không được hỗ trợ.");
            if (version is (LatestVersion or Version3 or Version4) && attemptId == Guid.Empty)
                throw new ArgumentException("Protocol v2/v3/v4 cần mã lượt kết nối ngẫu nhiên, không rỗng.");
            if (version == Version4 && mode != JoinAuthMode.RoomCode)
                throw new ArgumentException("Protocol v4 chỉ chấp nhận RoomCode mode.");
            if (version == Version4 && !IsValidTicket(ticket))
                throw new ArgumentException("Protocol v4 cần ticket đúng 64 ký tự ASCII hex.");

            var writer = new NetDataWriter();
            writer.Put(version);
            writer.Put((byte)mode);
            writer.Put(roomCode);
            writer.Put(displayName);
            if (version is (LatestVersion or Version3 or Version4)) writer.Put(attemptId.ToByteArray());
            if (version is (Version3 or Version4)) writer.Put(resumeToken.ToByteArray());
            if (version == Version4) writer.Put(ticket);

            // Chặn byte thực tế: Unicode có thể chiếm nhiều byte dù số ký tự vẫn đúng.
            var maxBytes = version == Version4
                ? MaxConnectV4Bytes
                : MaxConnectBytes + (version == Version3 ? 32 : (version == LatestVersion ? 16 : 0));
            if (writer.Length > maxBytes) throw new ArgumentException("Connection request quá dài.");
            return writer;
        }

        public static bool TryReadConnect(NetDataReader reader, out JoinRequest? request, out string reason)
        {
            request = null;
            reason = "MalformedRequest";
            try
            {
                var length = reader.AvailableBytes;
                if (length > MaxConnectV4Bytes) { reason = "PayloadTooLarge"; return false; }
                if (!reader.TryGetUShort(out var version)) return false;

                int maxAllowed;
                switch (version)
                {
                    case Version: maxAllowed = MaxConnectBytes; break;
                    case LatestVersion: maxAllowed = MaxConnectBytes + 16; break;
                    case Version3: maxAllowed = MaxConnectBytes + 32; break;
                    case Version4: maxAllowed = MaxConnectV4Bytes; break;
                    default:
                        if (!reader.TryGetByte(out _) || !reader.TryGetString(out _) || !reader.TryGetString(out _)) return false;
                        reason = "UnsupportedVersion"; return false;
                }
                if (length > maxAllowed) { reason = "PayloadTooLarge"; return false; }

                if (!reader.TryGetByte(out var mode)) return false;
                if (mode > (byte)JoinAuthMode.Jwt
                    || (version == Version4 && mode != (byte)JoinAuthMode.RoomCode))
                {
                    reason = "AuthModeMismatch";
                    return false;
                }

                if (!reader.TryGetString(out var room) || !reader.TryGetString(out var name)) return false;
                if (!IsValidField(room, MaxRoomChars)) { reason = "InvalidRoomCode"; return false; }
                if (!IsValidField(name, MaxNameChars)) { reason = "InvalidDisplayName"; return false; }

                var attempt = Guid.Empty;
                var resume = Guid.Empty;
                if (version != Version)
                {
                    if (version == Version4 && reader.AvailableBytes < 32) return false;
                    if (version == LatestVersion && reader.AvailableBytes < 16) return false;
                    if (version == Version3 && reader.AvailableBytes < 32) return false;

                    var attemptBytes = new byte[16];
                    reader.GetBytes(attemptBytes, 16);
                    attempt = new Guid(attemptBytes);
                    if (attempt == Guid.Empty) return false;

                    if (version is (Version3 or Version4))
                    {
                        var resumeBytes = new byte[16];
                        reader.GetBytes(resumeBytes, 16);
                        resume = new Guid(resumeBytes);
                    }
                }

                var ticket = string.Empty;
                if (version == Version4)
                {
                    if (!reader.TryGetString(out ticket)) return false;
                    if (!IsValidTicket(ticket)) { reason = "InvalidTicket"; return false; }
                }
                if (reader.AvailableBytes != 0) return false;

                request = new JoinRequest(version, (JoinAuthMode)mode, room, name.Trim(), attempt, resume, ticket);
                return true;
            }
            catch (Exception error) when (error is ArgumentException || error is IndexOutOfRangeException || error is InvalidOperationException)
            {
                request = null;
                reason = "MalformedRequest";
                return false;
            }
        }

        public static void Write(NetDataWriter writer, byte type, int sequence)
        {
            writer.Reset();
            writer.Put(type);
            writer.Put(sequence);
        }

        public static bool TryRead(NetDataReader reader, byte expectedType, out int sequence)
        {
            sequence = 0;
            // Ping/Pong phải đúng 5 byte và sequence > 0; không đọc packet thiếu/thừa.
            return reader.AvailableBytes == PacketBytes && reader.TryGetByte(out var type)
                && type == expectedType && reader.TryGetInt(out sequence) && sequence > 0;
        }

        public static NetDataWriter StringMessage(byte type, string value)
        {
            var writer = new NetDataWriter();
            writer.Put(type);
            writer.Put(value);
            return writer;
        }

        public static bool TryReadStringMessage(NetDataReader reader, byte expectedType, int maxBytes,
            int maxChars, out string value)
        {
            value = string.Empty;
            return reader.AvailableBytes <= maxBytes && reader.TryGetByte(out var type) && type == expectedType
                && reader.TryGetString(out value) && !string.IsNullOrWhiteSpace(value)
                && value.Length <= maxChars && reader.AvailableBytes == 0;
        }

        public static NetDataWriter WriteAuth(string token)
        {
            if (string.IsNullOrWhiteSpace(token) || token.Length > MaxTokenChars)
                throw new ArgumentException("JWT thiếu hoặc vượt 2048 ký tự.");
            var writer = StringMessage(Auth, token);
            if (writer.Length > MaxAuthBytes) throw new ArgumentException("Auth packet vượt 4096 byte.");
            return writer;
        }
    }
}
