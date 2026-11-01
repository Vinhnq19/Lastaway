using System.Net.Http.Json;
using NetSmokeClient.Configuration;

namespace NetSmokeClient.Services;

public static class AccountLogin
{
    private sealed class LoginResponse
    {
        public string AccessToken { get; init; } = string.Empty;
    }

    public static async Task<string> LoginAsync(ClientOptions options, string username, string password,
        CancellationToken cancellation)
    {
        var baseUrl = options.ValidateApiUrl();
        using var handler = new HttpClientHandler { AllowAutoRedirect = false, UseProxy = false };
        using var http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };
        var endpoint = new Uri(baseUrl.AbsoluteUri.TrimEnd('/') + "/api/auth/login");
        // HTTPS/VPN được kiểm tra trước POST; không đưa password/token vào log hoặc URL.
        using var response = await http.PostAsJsonAsync(endpoint, new { username, password }, cancellation);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"API login trả HTTP {(int)response.StatusCode}; không kết nối UDP và không fallback RoomCode.");
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: cancellation);
        if (result is null || string.IsNullOrWhiteSpace(result.AccessToken) || result.AccessToken.Length > 2048)
            throw new InvalidOperationException("API không trả JWT hợp lệ theo hợp đồng client.");
        return result.AccessToken; // Token chỉ nằm trong memory của lần chạy.
    }
    public static async Task<string> TicketAsync(ClientOptions options, Guid attemptId, string roomCode,
        string? accessToken, CancellationToken cancellation)
    {
        if (string.IsNullOrWhiteSpace(accessToken)) throw new InvalidOperationException("Login is required before joining.");
        var baseUrl = options.ValidateApiUrl();
        using var handler = new HttpClientHandler { AllowAutoRedirect = false, UseProxy = false };
        using var http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(baseUrl.AbsoluteUri.TrimEnd('/') + "/api/game-tickets"));
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = JsonContent.Create(new { attemptId, roomCode });
        using var response = await http.SendAsync(request, cancellation);
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"Game ticket rejected: HTTP {(int)response.StatusCode}.");
        var data = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(cancellationToken: cancellation);
        var ticket = data.GetProperty("ticket").GetString();
        if (ticket is null || ticket.Length != 64 || !ticket.All(Uri.IsHexDigit))
            throw new InvalidOperationException("Invalid game ticket response.");
        return ticket;
    }

}
