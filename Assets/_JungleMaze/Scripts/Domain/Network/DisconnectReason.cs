namespace JungleMaze.Domain.Network
{
    /// <summary>Lý do thất bại/ngắt kết nối. Nhóm đầu khớp chuỗi reason của GameServer.</summary>
    public enum DisconnectReason : byte
    {
        None = 0,

        // Server từ chối (JoinRejected / dữ liệu disconnect).
        MalformedRequest,
        PayloadTooLarge,
        UnsupportedVersion,
        AuthModeMismatch,
        InvalidRoomCode,
        InvalidDisplayName,
        RoomFull,
        ServerFull,
        AuthenticationRequired,
        InvalidToken,
        AuthTimeout,
        DuplicateSession,

        // Phía client.
        UserRequested,
        ConnectTimeout,
        JoinTimeout,
        ConnectionLost,
        ServerClosed,
        SocketError,
        ProtocolError,
        InvalidConfig,
        LoginFailed,
        Unknown,
        MatchInProgress,
        LoadingTimeout,
        AccountExpired, AccessDenied, RequestLimited, ServiceUnavailable, TicketRejected, GameplayVersionMismatch, InvalidResume, ServerBusy
    }

    public static class DisconnectReasons
    {
        /// <summary>Chuỗi reason từ server → enum; chuỗi lạ thành Unknown chứ không ném lỗi.</summary>
        public static DisconnectReason FromServer(string reason)
        {
            switch (reason)
            {
                case "MalformedRequest": return DisconnectReason.MalformedRequest;
                case "PayloadTooLarge": return DisconnectReason.PayloadTooLarge;
                case "UnsupportedVersion": return DisconnectReason.UnsupportedVersion;
                case "AuthModeMismatch": return DisconnectReason.AuthModeMismatch;
                case "InvalidRoomCode": return DisconnectReason.InvalidRoomCode;
                case "InvalidDisplayName": return DisconnectReason.InvalidDisplayName;
                case "RoomFull": return DisconnectReason.RoomFull;
                case "ServerFull": return DisconnectReason.ServerFull;
                case "AuthRequired":
                case "AuthenticationRequired": return DisconnectReason.AuthenticationRequired;
                case "InvalidTicket":
                case "TicketExpired": return DisconnectReason.TicketRejected;
                case "GameplayVersionMismatch": return DisconnectReason.GameplayVersionMismatch;
                case "InvalidResumeToken": return DisconnectReason.InvalidResume;
                case "ServerBusy": return DisconnectReason.ServerBusy;
                case "InvalidToken": return DisconnectReason.InvalidToken;
                case "AuthTimeout": return DisconnectReason.AuthTimeout;
                case "DuplicateSession": return DisconnectReason.DuplicateSession;
                case "MatchInProgress": return DisconnectReason.MatchInProgress;
                case "LoadingTimeout": return DisconnectReason.LoadingTimeout;
                default: return DisconnectReason.Unknown;
            }
        }

        /// <summary>Thông báo tiếng Việt cho UI; không chứa dữ liệu nhạy cảm (mã phòng, token).</summary>
        public static string Describe(DisconnectReason reason)
        {
            switch (reason)
            {
                case DisconnectReason.None: return "";
                case DisconnectReason.AccountExpired: return "Phiên đăng nhập không còn hợp lệ. Vui lòng đăng nhập lại.";
                case DisconnectReason.AccessDenied: return "Bạn chưa được phép thực hiện thao tác này.";
                case DisconnectReason.RequestLimited: return "Bạn thao tác quá nhanh. Vui lòng chờ rồi thử lại.";
                case DisconnectReason.ServiceUnavailable: return "Chưa thể vào phòng lúc này. Kiểm tra kết nối rồi thử lại.";
                case DisconnectReason.TicketRejected: return "Lượt vào phòng không còn hợp lệ. Vui lòng thử lại.";
                case DisconnectReason.GameplayVersionMismatch: return "Bản game chưa phù hợp với máy chủ. Vui lòng cập nhật game.";
                case DisconnectReason.InvalidResume: return "Không thể tiếp tục lượt chơi này. Hãy trở về menu.";
                case DisconnectReason.ServerBusy: return "Máy chủ đang bận. Vui lòng thử lại sau.";
                case DisconnectReason.UnsupportedVersion: return "Bản game chưa phù hợp với máy chủ. Vui lòng cập nhật game.";
                case DisconnectReason.AuthModeMismatch: return "Cài đặt kết nối chưa phù hợp với máy chủ. Kiểm tra địa chỉ rồi thử lại.";
                case DisconnectReason.InvalidRoomCode: return "Mã phòng không đúng.";
                case DisconnectReason.InvalidDisplayName: return "Tên hiển thị không hợp lệ (1–32 ký tự).";
                case DisconnectReason.RoomFull: return "Phòng đã đủ người.";
                case DisconnectReason.MatchInProgress: return "Trận đang diễn ra. Hãy chờ trận tiếp theo hoặc chọn phòng khác.";
                case DisconnectReason.LoadingTimeout: return "Tải trận quá thời gian. Kiểm tra dữ liệu game và thử vào lại.";
                case DisconnectReason.ServerFull: return "Server đã đủ người.";
                case DisconnectReason.AuthenticationRequired: return "Server yêu cầu đăng nhập.";
                case DisconnectReason.InvalidToken: return "Lượt vào phòng không còn hợp lệ. Vui lòng thử lại.";
                case DisconnectReason.AuthTimeout: return "Xác thực quá thời gian.";
                case DisconnectReason.DuplicateSession: return "Tài khoản đang có phiên khác trong phòng.";
                case DisconnectReason.UserRequested: return "Đã ngắt kết nối.";
                case DisconnectReason.ConnectTimeout: return "Không kết nối được máy chủ. Kiểm tra địa chỉ và mạng rồi thử lại.";
                case DisconnectReason.JoinTimeout: return "Đã nối server nhưng không vào được phòng (hết thời gian).";
                case DisconnectReason.ConnectionLost: return "Mất kết nối tới server.";
                case DisconnectReason.ServerClosed: return "Server đã đóng kết nối.";
                case DisconnectReason.SocketError: return "Không kết nối được máy chủ. Kiểm tra mạng rồi thử lại.";
                case DisconnectReason.ProtocolError: return "Dữ liệu máy chủ trả về chưa hợp lệ. Vui lòng thử lại.";
                case DisconnectReason.InvalidConfig: return "Cấu hình kết nối không hợp lệ.";
                case DisconnectReason.LoginFailed: return "Đăng nhập thất bại.";
                case DisconnectReason.MalformedRequest:
                case DisconnectReason.PayloadTooLarge: return "Yêu cầu vào phòng không hợp lệ.";
                default: return "Lỗi không xác định.";
            }
        }
    }
}
