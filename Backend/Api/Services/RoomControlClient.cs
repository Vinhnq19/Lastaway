using System.Net.Http.Json;
using System.Text;
using JungleBackend.Shared;

namespace Api.Services;

/// <summary>Chỉ API gọi HTTP private. User JWT không gửi tới UDP hoặc control service.</summary>
public sealed class RoomControlClient(HttpClient client, IConfiguration configuration)
{
    public async Task<(int Status, string? Json)> Send(HttpMethod method, string path, object? body, CancellationToken ct)
    {
        var secret = configuration["ROOM_CONTROL_TOKEN"] ?? "";
        if (Encoding.UTF8.GetByteCount(secret) < 32) return (503, "{\"error\":\"RoomServiceNotConfigured\"}");
        var baseUrl = configuration["RoomControl:BaseUrl"] ?? "http://127.0.0.1:7780";
        using var message = new HttpRequestMessage(method, new Uri(new Uri(baseUrl), path));
        message.Headers.Add("X-Room-Control-Key", secret);
        if (body is not null) message.Content = JsonContent.Create(body);
        try
        {
            using var response = await client.SendAsync(message, ct);
            // Không log JSON trả về: create/list có mã phòng của chủ sở hữu.
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized) return (503, "{\"error\":\"RoomServiceMisconfigured\"}");
            return ((int)response.StatusCode, response.StatusCode == System.Net.HttpStatusCode.NoContent
                ? null : await response.Content.ReadAsStringAsync(ct));
        }
        catch (Exception error) when (error is HttpRequestException or TaskCanceledException)
        { return (503, "{\"error\":\"RoomServiceUnavailable\"}"); }
    }

    public async Task<(int Status, string? Json)> RegisterGameTicketAsync(RegisterGameTicketCommand command, CancellationToken ct)
    {
        return await Send(HttpMethod.Post, "/internal/game-tickets", command, ct);
    }

    public async Task<(int Status, byte[]? Bytes)> Download(string path, int maximumBytes, CancellationToken ct)
    {
        var secret = configuration["ROOM_CONTROL_TOKEN"] ?? "";
        if (Encoding.UTF8.GetByteCount(secret) < 32) return (503, null);
        var baseUrl = configuration["RoomControl:BaseUrl"] ?? "http://127.0.0.1:7780";
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(new Uri(baseUrl), path));
        request.Headers.Add("X-Room-Control-Key", secret);
        try
        {
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized) return (503, null);
            if (!response.IsSuccessStatusCode) return ((int)response.StatusCode, null);
            if (response.Content.Headers.ContentLength > maximumBytes) return (502, null);
            await using var input = await response.Content.ReadAsStreamAsync(ct);
            using var output = new MemoryStream();
            var buffer = new byte[16384];
            int count;
            while ((count = await input.ReadAsync(buffer, ct)) > 0)
            {
                if (output.Length + count > maximumBytes) return (502, null);
                await output.WriteAsync(buffer.AsMemory(0, count), ct);
            }
            return (200, output.ToArray());
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or IOException)
        { return (503, null); }
    }
}
