using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace JungleMaze.App.Ports
{
    /// <summary>Connection-attempt identity and explicit resume; durable account credentials stay outside gameplay packets.</summary>
    public interface IMatchConnection
    {
        Guid AttemptId { get; }
        ConnectRequest LastRequest { get; }
        void Resume(Guid proof);
    }

    public sealed class PlayerMatchHistory
    {
        public Guid MatchId { get; set; }
        public string DisplayName { get; set; }
        public int Rank { get; set; }
        public int Kills { get; set; }
        public double SurvivalSeconds { get; set; }
        public DateTimeOffset EndedAt { get; set; }
        public string Reason { get; set; }
        public string MatchKind { get; set; } = "HumanOnly";
        public bool PvpEligible => MatchKind == "HumanOnly" && (Reason == "LastAlive" || Reason == "Tie");
    }

    public readonly struct TicketIssueResult
    {
        public readonly string Value;
        public readonly int HttpStatus;
        public readonly JungleMaze.Domain.Network.DisconnectReason Failure;
        public bool Success => !string.IsNullOrEmpty(Value) && Failure == JungleMaze.Domain.Network.DisconnectReason.None;
        public TicketIssueResult(string value) { Value = value; HttpStatus = 200; Failure = JungleMaze.Domain.Network.DisconnectReason.None; }
        public TicketIssueResult(int status)
        {
            Value = null; HttpStatus = status;
            Failure = status == 401 ? JungleMaze.Domain.Network.DisconnectReason.AccountExpired
                : status == 403 ? JungleMaze.Domain.Network.DisconnectReason.AccessDenied
                : status == 404 ? JungleMaze.Domain.Network.DisconnectReason.InvalidRoomCode
                : status == 429 ? JungleMaze.Domain.Network.DisconnectReason.RequestLimited
                : JungleMaze.Domain.Network.DisconnectReason.ServiceUnavailable;
        }
        public override string ToString() => "TicketIssueResult(status=" + HttpStatus + ", success=" + Success + ")";
    }

    public interface IPlayerHistory : IDisposable
    {
        string Status { get; }
        bool IsReady { get; }
        IReadOnlyList<PlayerMatchHistory> Rows { get; }
        Task<bool> EnsureGuestAsync(string baseUrl, CancellationToken cancellation = default);
        Task<string> TicketAsync(Guid attemptId, string roomCode, string accountToken = null, CancellationToken cancellation = default);
        Task RefreshHistoryAsync(string accountToken = null, CancellationToken cancellation = default);
    }
}
