using System.Text.RegularExpressions;
using Api.Services;
using JungleBackend.Rooms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController, Authorize, Route("api/maps")]
public sealed class MapsController(RoomControlClient control) : ControllerBase
{
    private static bool Hash(string value) => Regex.IsMatch(value, "^[0-9a-f]{64}\\z", RegexOptions.CultureInvariant);

    /// <summary>Danh sách map được server phát hành; số map theo catalog, không cố định trong client.</summary>
    [HttpGet, ProducesResponseType(typeof(MapCatalogView), 200), ProducesResponseType(401), ProducesResponseType(503)]
    public async Task<ActionResult> Catalog(CancellationToken ct)
    {
        var result = await control.Send(HttpMethod.Get, "/internal/maps", null, ct);
        return result.Json is null ? StatusCode(result.Status) : ContentResult(result.Status, result.Json);
    }

    /// <summary>Tải JSON đúng ID/hash, tối đa 2 MiB. Hash cũ còn phục vụ các trận đã chốt phiên bản.</summary>
    [HttpGet("{id}/{hash}"), ProducesResponseType(200), ProducesResponseType(401), ProducesResponseType(404)]
    public async Task<ActionResult> Map(string id, string hash, CancellationToken ct)
    {
        if (!Hash(hash) || !Regex.IsMatch(id, "^[a-z0-9][a-z0-9-]{0,31}\\z", RegexOptions.CultureInvariant)) return BadRequest();
        var result = await control.Download($"/internal/maps/{id}/{hash}/map", 2 * 1024 * 1024, ct);
        return result.Bytes is null ? StatusCode(result.Status) : File(result.Bytes, "application/json");
    }

    /// <summary>Ảnh preview PNG theo hash, tối đa 512 KiB.</summary>
    [HttpGet("previews/{hash}"), ProducesResponseType(200), ProducesResponseType(401), ProducesResponseType(404)]
    public async Task<ActionResult> Preview(string hash, CancellationToken ct)
    {
        if (!Hash(hash)) return BadRequest();
        var result = await control.Download($"/internal/maps/previews/{hash}", 512 * 1024, ct);
        return result.Bytes is null ? StatusCode(result.Status) : File(result.Bytes, "image/png");
    }

    private static ContentResult ContentResult(int status, string json) => new()
    { StatusCode = status, ContentType = "application/json", Content = json };
}
