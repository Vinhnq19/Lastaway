using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using JungleMaze.App.Ports;
using System.Collections.Generic;

namespace JungleMaze.Infrastructure.Auth
{
    /// <summary>Optional HTTPS-only persistent identity; every failure leaves RoomCode gameplay available.</summary>
    public sealed class GuestHistoryClient : IPlayerHistory
    {
        private readonly GuestCredentialStore _store;
        private readonly Func<JungleMaze.App.Config.ClientConfig> _config;
        private readonly Func<string> _accountEndpoint;
        private GuestCredential _identity;
        private Uri _endpoint;
        private int _identityGeneration;
        public string Status { get; private set; } = "Lịch sử chưa kết nối";
        public string HistoryJson { get; private set; } = "";
        public bool IsReady => _identity != null;
        public IReadOnlyList<PlayerMatchHistory> Rows { get; private set; } = Array.Empty<PlayerMatchHistory>();
        private sealed class HistoryPage { public PlayerMatchHistory[] Items; }
        public GuestHistoryClient(GuestCredentialStore store, Func<string> accountEndpoint = null, Func<JungleMaze.App.Config.ClientConfig> config = null)
        { _store = store; _accountEndpoint = accountEndpoint; _config=config; }

        public async Task<TicketIssueResult> AccountTicketAsync(string baseUrl, Guid attempt, string room, string token, JungleMaze.App.Config.ClientConfig settings = null)
        {
            if (string.IsNullOrWhiteSpace(token)) return new TicketIssueResult(401);
            if (!ConfigureAccountEndpoint(baseUrl)) return new TicketIssueResult(0);
            return await RequestTicketAsync(attempt, room, token, default, settings);
        }
        private bool ConfigureAccountEndpoint(string baseUrl)
        {
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var endpoint) || !IsSecureEndpoint(endpoint))
            { Status = "Lịch sử online không khả dụng"; return false; }
            if (_endpoint == null || _endpoint != endpoint)
            { _identityGeneration++; _identity = null; Rows = Array.Empty<PlayerMatchHistory>(); HistoryJson = ""; }
            _endpoint = endpoint;
            return true;
        }

        public static bool IsSecureEndpoint(Uri endpoint) => endpoint != null &&
            string.IsNullOrEmpty(endpoint.UserInfo) && string.IsNullOrEmpty(endpoint.Query) && string.IsNullOrEmpty(endpoint.Fragment) &&
            (endpoint.Scheme == Uri.UriSchemeHttps || endpoint.Scheme == Uri.UriSchemeHttp && endpoint.IsLoopback);

        public async Task<bool> EnsureGuestAsync(string baseUrl, CancellationToken cancellation = default)
        {
            var generation = ++_identityGeneration;
            // Invalidate before the first await: a failed endpoint switch must not reuse Local proof on AWS.
            var previous = _identity; var previousEndpoint = _endpoint;
            _identity = null; _endpoint = null; Rows = Array.Empty<PlayerMatchHistory>(); HistoryJson = "";
            try
            {
                if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var endpoint) || !IsSecureEndpoint(endpoint))
                { Status = "Lịch sử cần HTTPS; Local cho phép HTTP loopback"; return false; }
                _endpoint = endpoint; // Account JWT can still obtain a ticket if optional Guest creation fails.
                var identity = previousEndpoint != null && previousEndpoint.GetLeftPart(UriPartial.Authority) == endpoint.GetLeftPart(UriPartial.Authority)
                    ? previous : _store.Load(endpoint);
                if (identity == null)
                {
                    using (var request = new HttpRequestMessage(HttpMethod.Post, new Uri(endpoint.GetLeftPart(UriPartial.Authority) + "/api/guests")))
                    {
                        request.Content = JsonBody(new { });
                        using (var http = JungleMaze.Infrastructure.Network.LanTls.CreateHttpClient(_config?.Invoke()))
                    using (var response = await http.SendAsync(request, cancellation))
                        {
                            response.EnsureSuccessStatusCode();
                            identity = JsonConvert.DeserializeObject<GuestCredential>(await response.Content.ReadAsStringAsync());
                            if (identity == null || string.IsNullOrEmpty(identity.ProfileId) || string.IsNullOrEmpty(identity.Proof))
                                throw new InvalidOperationException("Invalid Guest response");
                            if (generation != _identityGeneration) return false;
                            _store.Save(endpoint, identity);
                            identity = _store.Load(endpoint); // Another process may have atomically created the shared profile first.
                        }
                    }
                }
                if (generation != _identityGeneration) return false;
                _identity = identity; _endpoint = endpoint;
                Status = "Hồ sơ Guest đã sẵn sàng";
                return true;
            }
            catch (Exception e) when (!(e is OutOfMemoryException))
            {
                if (generation == _identityGeneration) Status = "Lịch sử không khả dụng; vẫn chơi bằng RoomCode";
                return false;
            }
        }

        public async Task<string> TicketAsync(Guid attemptId, string roomCode, string accountToken = null,
            CancellationToken cancellation = default)
        {
            var result = await RequestTicketAsync(attemptId, roomCode, accountToken, cancellation);
            return result.Success ? result.Value : null;
        }

        private async Task<TicketIssueResult> RequestTicketAsync(Guid attemptId, string roomCode, string accountToken, CancellationToken cancellation, JungleMaze.App.Config.ClientConfig settings = null)
        {
            if (_endpoint == null) return new TicketIssueResult(0);
            if (_identity == null && string.IsNullOrEmpty(accountToken)) return new TicketIssueResult(401);
            try
            {
                using (var request = new HttpRequestMessage(HttpMethod.Post, Url("api/game-tickets")))
                {
                    SetProof(request, accountToken);
                    request.Content = JsonBody(new { attemptId, roomCode });
                    using (var http = JungleMaze.Infrastructure.Network.LanTls.CreateHttpClient(settings ?? _config?.Invoke()))
                    using (var response = await http.SendAsync(request, cancellation))
                    {
                        if (!response.IsSuccessStatusCode) return new TicketIssueResult((int)response.StatusCode);
                        var data = Newtonsoft.Json.Linq.JObject.Parse(await response.Content.ReadAsStringAsync());
                        var value = data.Value<string>("ticket");
                        return string.IsNullOrWhiteSpace(value) ? new TicketIssueResult(502) : new TicketIssueResult(value);
                    }
                }
            }
            catch (JsonException)
            { Status = "Máy chủ trả dữ liệu chưa hợp lệ. Vui lòng thử lại."; return new TicketIssueResult(502); }
            catch (Exception e) when (!(e is OutOfMemoryException))
            { Status = "Chưa thể vào phòng lúc này. Kiểm tra kết nối rồi thử lại."; return new TicketIssueResult(0); }
        }

        public async Task RefreshHistoryAsync(string accountToken = null, CancellationToken cancellation = default)
        {
            if (!string.IsNullOrEmpty(accountToken) && _accountEndpoint != null && !ConfigureAccountEndpoint(_accountEndpoint())) return;
            if (_endpoint == null || _identity == null && string.IsNullOrEmpty(accountToken)) return;
            var generation = ++_identityGeneration;
            try
            {
                using (var request = new HttpRequestMessage(HttpMethod.Get, Url("api/me/matches?page=1&pageSize=20")))
                {
                    SetProof(request, accountToken);
                    using (var http = JungleMaze.Infrastructure.Network.LanTls.CreateHttpClient(_config?.Invoke()))
                    using (var response = await http.SendAsync(request, cancellation))
                    {
                        response.EnsureSuccessStatusCode();
                        var json = await response.Content.ReadAsStringAsync();
                        if (generation != _identityGeneration) return;
                        HistoryJson = json;
                        Rows = JsonConvert.DeserializeObject<HistoryPage>(HistoryJson)?.Items ?? Array.Empty<PlayerMatchHistory>();
                        Status = "Lịch sử đã cập nhật";
                    }
                }
            }
            catch (Exception e) when (!(e is OutOfMemoryException))
            { if (generation == _identityGeneration) { Rows = Array.Empty<PlayerMatchHistory>(); HistoryJson = ""; Status = "Lịch sử online không khả dụng"; } }
        }

        private void SetProof(HttpRequestMessage request, string accountToken)
        {
            if (!string.IsNullOrEmpty(accountToken)) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accountToken);
            else request.Headers.Add("X-Guest-Credential", _identity.Proof);
        }
        private Uri Url(string path) => new Uri(_endpoint.GetLeftPart(UriPartial.Authority) + "/" + path);
        private static StringContent JsonBody(object value) => new StringContent(JsonConvert.SerializeObject(value), Encoding.UTF8, "application/json");
        public void Dispose() { }
    }
}
