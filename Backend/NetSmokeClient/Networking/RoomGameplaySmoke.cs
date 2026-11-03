#nullable enable
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using JungleBackend.Protocol.Gameplay;
using JungleMaze.World;
using LiteNetLib.Utils;
using NetSmokeClient.Configuration;

namespace NetSmokeClient.Networking;

/// <summary>
/// Optional room-gameplay smoke driver. It validates the room lifecycle (map download, geometry build,
/// ready/start/load/replay acknowledgements) only: no fake input, no bots, no client rules bundle claim.
/// All sends happen on the poll thread; the download task returns a value or an exception and never touches the room.
/// </summary>
public sealed class RoomGameplaySmoke : IDisposable
{
    public const string RulesValidation = "server-provided; Unity performs full bundle verification";
    private const int CatalogMaxBytes = 256 * 1024;
    private const int MapMaxBytes = 2 * 1024 * 1024;
    private const string LayoutVersion = "smoke-validation";
    private const int CatalogSchemaVersion = 1;
    private const int MaxMapVersionLength = 32;
    private const int MaxMapNameLength = 64;

    private static readonly Regex IdPattern = new("^[a-z0-9][a-z0-9-]{0,31}\\z", RegexOptions.CultureInvariant);
    private static readonly Regex HashPattern = new("^[0-9a-f]{64}\\z", RegexOptions.CultureInvariant);
    private static readonly JsonSerializerOptions DocumentJson = new()
    {
        IncludeFields = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private sealed record CatalogMap(string Id, string Name, string Version, string MapHash);
    private readonly record struct Verification(Guid RoomId, uint Revision, string MapHash, bool Verified, string? Error, CatalogMap? Target);

    private readonly ClientOptions _options;
    private readonly string _accountToken;
    private readonly Action<byte[], bool, byte> _send;
    private readonly Action<string> _fail;
    private readonly PacketRegistry _registry = new();
    private readonly NetDataWriter _writer = new();
    private readonly CancellationTokenSource _cancellation = new();
    private readonly string? _environmentMapId;
    private Task<Verification>? _verification;
    private CatalogMap? _target;
    private Guid _roomId, _matchId, _loadAckMatch, _replayAckMatch, _ackMatch;
    private uint _selectionRevision, _selectRevision = uint.MaxValue;
    private uint? _ackTick;
    private string _mapId = "", _mapHash = "", _mapVersion = "", _mapName = "", _rulesHash = "";
    private byte _hostNetId = 255;
    private int _ownNetId = -1;
    private MatchPhase? _phase;
    private bool _canStart, _verified, _roomReadySent, _startRequested, _started, _disposed, _failed;
    private string? _failureReason, _lastMapError;

    public string MapId => _mapId;
    public string MapHash => _mapHash;
    public string? Phase => _phase?.ToString();
    public int Snapshots { get; private set; }
    public int Results { get; private set; }
    public int Starts { get; private set; }
    public bool MapVerified => _verified;

    public RoomGameplaySmoke(ClientOptions options, string accountToken, Action<byte[], bool, byte> send, Action<string> fail)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        if (string.IsNullOrWhiteSpace(accountToken) || accountToken.Length > 2048)
            throw new ArgumentException("Account token is required.", nameof(accountToken));
        _accountToken = accountToken;
        _send = send ?? throw new ArgumentNullException(nameof(send));
        _fail = fail ?? throw new ArgumentNullException(nameof(fail));
        var requestedMap = Environment.GetEnvironmentVariable("NET_SMOKE_MAP_ID");
        if (!string.IsNullOrEmpty(requestedMap))
        {
            if (!IdPattern.IsMatch(requestedMap)) throw new ArgumentException("NET_SMOKE_MAP_ID is not a valid map id.");
            _environmentMapId = requestedMap;
        }
        _registry.Subscribe<RoomMapStatePacket>(OnRoomMapState);
        _registry.Subscribe<MatchPhasePacket>(OnMatchPhase);
        _registry.Subscribe<ResumeTokenPacket>(OnResumeToken);
        _registry.Subscribe<WorldSnapshotPacket>(OnSnapshot);
        _registry.Subscribe<MatchResultPacket>(OnResult);
        _registry.Subscribe<RoomMapCommandResultPacket>(OnRoomMapResult);
        RegisterIgnoredPackets();
    }

    public void Start()
    {
        if (_disposed || _started) return;
        _started = true;
        var hello = new ClientHelloPacket { ProtocolVersion = MatchProtocol.Version, Ready = true };
        Emit(ref hello, true, GameplayProtocol.GameplayChannel);
    }

    /// <summary>Parses one transport datagram. Non-gameplay frames and unknown packets are ignored, not failed.</summary>
    public void Receive(byte[] bytes)
    {
        if (_disposed || bytes is null || bytes.Length == 0) return;
        _registry.TryReadFrame(new NetDataReader(bytes));
    }

    /// <summary>Called on the same poll thread as Receive; never blocks on network I/O.</summary>
    public void Tick()
    {
        if (_disposed) return;
        ApplyVerification();
        if (_disposed) return;
        TrySelectEnvironmentMap();
        if (_verified && !_roomReadySent && SelectionSettled()) EmitRoomReady();
        EmitSnapshotAck();
        ApplyPhaseAcks();
        if (!_canStart) _startRequested = false;
        if (_canStart && _verified && _hostNetId != 255 && _ownNetId == _hostNetId && !_startRequested) EmitStart();
    }

    public object Report => new
    {
        mapId = MapId, mapHash = MapHash, phase = Phase, snapshots = Snapshots, results = Results,
        starts = Starts, mapVerified = MapVerified, canStart = _canStart,
        selectionRevision = _selectionRevision, ownNetId = _ownNetId >= 0 ? (int?)_ownNetId : null,
        lastMapError = _lastMapError, rulesValidation = RulesValidation,
        failed = _failed, failureReason = _failureReason
    };

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        var pending = _verification;
        _verification = null;
        try { _cancellation.Cancel(); }
        catch (Exception e) when (e is ObjectDisposedException or AggregateException) { }
        // Observe any late fault so a disposed driver never leaves an unobserved task exception.
        if (pending is not null)
            _ = pending.ContinueWith(static task => _ = task.Exception, CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        _cancellation.Dispose();
    }

    private void OnRoomMapState(RoomMapStatePacket packet)
    {
        _hostNetId = packet.HostNetId;
        _canStart = packet.CanStart;
        _rulesHash = packet.RulesHash ?? "";
        if (packet.RoomId == _roomId && packet.SelectionRevision == _selectionRevision &&
            string.Equals(packet.MapHash, _mapHash, StringComparison.Ordinal)) return;
        _roomId = packet.RoomId;
        _selectionRevision = packet.SelectionRevision;
        _mapId = packet.MapId ?? "";
        _mapHash = packet.MapHash ?? "";
        _mapVersion = packet.MapVersion ?? "";
        _mapName = packet.MapName ?? "";
        _verified = false;
        _roomReadySent = false;
        _startRequested = false;
        _selectRevision = uint.MaxValue;
        _target = null;
        BeginVerification();
    }

    private void OnMatchPhase(MatchPhasePacket packet)
    {
        if (packet.MatchId != _matchId)
        {
            _matchId = packet.MatchId;
            _loadAckMatch = Guid.Empty;
            _replayAckMatch = Guid.Empty;
            _startRequested = false;
        }
        _phase = packet.Phase;
        if (packet.Phase != MatchPhase.Waiting) _startRequested = false;
    }

    private void OnResumeToken(ResumeTokenPacket packet)
    {
        _ownNetId = packet.NetId;
        if (_matchId == Guid.Empty) _matchId = packet.MatchId;
    }

    private void OnSnapshot(WorldSnapshotPacket packet)
    {
        Snapshots++;
        _ackMatch = packet.MatchId;
        _ackTick = packet.ServerTick;
    }

    private void OnResult(MatchResultPacket packet)
    {
        Results++;
        if (_verified && !string.IsNullOrEmpty(packet.MapHash) &&
            !string.Equals(packet.MapHash, _mapHash, StringComparison.Ordinal))
            FailOnce("ResultMapHashMismatch");
    }

    private void OnRoomMapResult(RoomMapCommandResultPacket packet)
    {
        if (packet.Error != RoomMapError.None) _lastMapError = packet.Error.ToString();
    }

    private void ApplyVerification()
    {
        var task = _verification;
        if (task is null || !task.IsCompleted) return;
        _verification = null;
        if (task.IsCanceled) return;
        if (task.IsFaulted) { FailOnce("MapValidationFailed"); return; }
        var result = task.Result;
        if (result.RoomId != _roomId || result.Revision != _selectionRevision ||
            !string.Equals(result.MapHash, _mapHash, StringComparison.Ordinal))
        {
            if (!_verified) BeginVerification();
            return;
        }
        _target = result.Target;
        if (!result.Verified)
        {
            if (result.Error is not null) FailOnce(result.Error);
            return;
        }
        _verified = true;
    }

    private void BeginVerification()
    {
        if (_disposed || _verification is not null) return;
        if (!IdPattern.IsMatch(_mapId) || !HashPattern.IsMatch(_mapHash)) { FailOnce("InvalidMapMetadata"); return; }
        var canChoose = _environmentMapId is not null && _ownNetId >= 0 && _ownNetId == _hostNetId;
        // Snapshot the phase on the poll thread; the verification task must not read live state.
        var pinned = _phase is MatchPhase.InGame or MatchPhase.Loading or MatchPhase.Ending;
        _verification = VerifyAsync(_roomId, _selectionRevision, _mapId, _mapVersion, _mapName, _mapHash, canChoose, pinned);
    }

    private void TrySelectEnvironmentMap()
    {
        if (_environmentMapId is null || _target is null || _disposed) return;
        if (_target.Id != _environmentMapId || _ownNetId < 0 || _ownNetId != _hostNetId) return;
        if (_selectRevision == _selectionRevision) return;
        if (string.Equals(_target.MapHash, _mapHash, StringComparison.Ordinal)) return;
        var select = new SelectRoomMapPacket
        {
            RoomId = _roomId, SelectionRevision = _selectionRevision,
            MapId = _target.Id, MapHash = _target.MapHash
        };
        Emit(ref select, true, GameplayProtocol.GameplayChannel);
        _selectRevision = _selectionRevision;
    }

    private bool SelectionSettled() =>
        _environmentMapId is null || _target is null || string.Equals(_target.MapHash, _mapHash, StringComparison.Ordinal);

    private void EmitRoomReady()
    {
        var ready = new RoomMapReadyPacket
        {
            RoomId = _roomId, SelectionRevision = _selectionRevision, MapHash = _mapHash, RulesHash = _rulesHash
        };
        Emit(ref ready, true, GameplayProtocol.GameplayChannel);
        _roomReadySent = true;
    }

    private void EmitSnapshotAck()
    {
        if (_ackTick is not { } tick || _disposed) return;
        _ackTick = null;
        var ack = new SnapshotAckPacket { MatchId = _ackMatch, AckTick = tick };
        Emit(ref ack, false, GameplayProtocol.ControlChannel);
    }

    private void ApplyPhaseAcks()
    {
        if (_matchId == Guid.Empty) return;
        if (_phase == MatchPhase.Loading && _verified && _loadAckMatch != _matchId)
        {
            _loadAckMatch = _matchId;
            var load = new LoadAckPacket { MatchId = _matchId, Ready = true };
            Emit(ref load, true, GameplayProtocol.GameplayChannel);
        }
        if (_phase == MatchPhase.Ending && _replayAckMatch != _matchId)
        {
            _replayAckMatch = _matchId;
            var replay = new ReplayAckPacket { MatchId = _matchId };
            Emit(ref replay, true, GameplayProtocol.GameplayChannel);
        }
    }

    private void EmitStart()
    {
        if (!SelectionSettled()) return;
        _startRequested = true;
        var start = new StartRoomMatchPacket { RoomId = _roomId, SelectionRevision = _selectionRevision };
        Emit(ref start, true, GameplayProtocol.GameplayChannel);
        Starts++;
    }

    private void Emit<T>(ref T packet, bool reliable, byte channel) where T : struct, INetSerializable
    {
        if (_disposed) return;
        PacketRegistry.BeginFrame(_writer);
        _registry.Write(_writer, ref packet);
        _send(_writer.CopyData(), reliable, channel);
    }

    private void FailOnce(string reason)
    {
        if (_failed || _disposed) return;
        _failed = true;
        _failureReason = reason;
        _fail(reason);
    }

    /// <summary>Off-poll download/validation. Returns a bound value or an error code; never sends or mutates room state.</summary>
    private async Task<Verification> VerifyAsync(Guid roomId, uint revision, string mapId, string mapVersion,
        string mapName, string mapHash, bool canChoose, bool pinned)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(_cancellation.Token);
        timeout.CancelAfter(TimeSpan.FromSeconds(15));
        var ct = timeout.Token;
        try
        {
            using var http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false, UseProxy = false })
            { Timeout = TimeSpan.FromSeconds(15) };
            var api = RequireSecureApiUrl(_options.ValidateApiUrl()).AbsoluteUri.TrimEnd('/');
            var requested = _environmentMapId;
            var catalogBytes = await GetBoundedAsync(http, _accountToken, api + "/api/maps", CatalogMaxBytes, ct);
            using var catalog = JsonDocument.Parse(DecodeUtf8(catalogBytes));
            if (catalog.RootElement.ValueKind != JsonValueKind.Object) throw new InvalidOperationException("Catalog is not an object.");
            ValidateCatalog(catalog.RootElement);
            var entry = FindMap(catalog.RootElement, mapId);
            var target = requested is null ? null : FindMap(catalog.RootElement, requested);
            if (_environmentMapId is not null && target is null)
                return new(roomId, revision, mapHash, false, "MapMissingFromCatalog", null);
            var switching = !pinned && canChoose && target is not null &&
                !string.Equals(target.MapHash, mapHash, StringComparison.Ordinal);
            if (entry is null)
            {
                // A pinned immutable version may be pruned from the live catalog while a match is in flight;
                // the content is still fetched by id/hash from the authenticated API. Anything else rejects.
                if (!pinned) return new(roomId, revision, mapHash, false, "MapCatalogMismatch", target);
            }
            else if (!MatchesCatalog(entry, mapId, mapHash, mapVersion, mapName))
            {
                // While the host is about to switch maps, a stale catalog mismatch is not a failure.
                return switching
                    ? new(roomId, revision, mapHash, false, null, target)
                    : new(roomId, revision, mapHash, false, "MapCatalogMismatch", target);
            }
            var bytes = await GetBoundedAsync(http, _accountToken,
                api + "/api/maps/" + mapId + "/" + mapHash, MapMaxBytes, ct);
            var normalized = NormalizeJson(bytes);
            var actual = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized))).ToLowerInvariant();
            if (!string.Equals(actual, mapHash, StringComparison.Ordinal))
                return new(roomId, revision, mapHash, false, "MapContentHashMismatch", target);
            var document = JsonSerializer.Deserialize<AuthoredMapDocument>(normalized, DocumentJson)
                ?? throw new InvalidOperationException("Authored map document is empty.");
            // Existing authored builder validates geometry, object counts, hazards and spawn connectivity.
            var layout = document.Build(LayoutVersion);
            if (layout is null || layout.Spawns.Count != 10 || layout.Size != 50)
                throw new InvalidOperationException("Authored map geometry is outside supported bounds.");
            return new(roomId, revision, mapHash, true, null, target);
        }
        catch (OperationCanceledException) when (!_cancellation.IsCancellationRequested)
        { return new(roomId, revision, mapHash, false, "MapFetchTimeout", null); }
        catch (OperationCanceledException) { throw; }
        catch (HttpRequestException) { return new(roomId, revision, mapHash, false, "MapFetchHttpError", null); }
        catch (JsonException) { return new(roomId, revision, mapHash, false, "MapJsonInvalid", null); }
        catch (Exception e) when (e is InvalidOperationException or ArgumentException or FormatException or IOException)
        { return new(roomId, revision, mapHash, false, "MapValidationFailed", null); }
    }

    private static Uri RequireSecureApiUrl(Uri uri)
    {
        if (uri is null || !uri.IsAbsoluteUri ||
            !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
            throw new InvalidOperationException("MapApiUrlInvalid");
        var allowed = uri.Scheme == Uri.UriSchemeHttps || (uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback);
        if (!allowed) throw new InvalidOperationException("MapApiUrlInvalid");
        return uri;
    }

    private static async Task<byte[]> GetBoundedAsync(HttpClient http, string token, string endpoint, int maxBytes, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(endpoint, UriKind.Absolute));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException("MapApiHttpError");
        if (response.Content.Headers.ContentLength is long declared && (declared <= 0 || declared > maxBytes))
            throw new InvalidOperationException("MapApiBodyTooLarge");
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        var buffer = new byte[8192];
        using var output = new MemoryStream();
        int read;
        while ((read = await stream.ReadAsync(buffer, ct)) > 0)
        {
            if (output.Length + read > maxBytes) throw new InvalidOperationException("MapApiBodyTooLarge");
            output.Write(buffer, 0, read);
        }
        if (output.Length == 0) throw new InvalidOperationException("MapApiBodyEmpty");
        return output.ToArray();
    }

    private static void ValidateCatalog(JsonElement root)
    {
        if (!root.TryGetProperty("schemaVersion", out var schema) || schema.ValueKind != JsonValueKind.Number ||
            !schema.TryGetInt32(out var schemaVersion) || schemaVersion != CatalogSchemaVersion)
            throw new InvalidOperationException("CatalogSchemaVersion");
        if (!root.TryGetProperty("generation", out var generation) || generation.ValueKind != JsonValueKind.Number ||
            !generation.TryGetInt64(out var generationValue) || generationValue <= 0)
            throw new InvalidOperationException("CatalogGeneration");
        if (!root.TryGetProperty("maps", out var maps) || maps.ValueKind != JsonValueKind.Array ||
            maps.GetArrayLength() == 0)
            throw new InvalidOperationException("CatalogMaps");
    }

    private static CatalogMap? FindMap(JsonElement root, string id)
    {
        if (!root.TryGetProperty("maps", out var maps) || maps.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException("Catalog has no map array.");
        foreach (var item in maps.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object) continue;
            var entryId = Text(item, "id");
            if (!string.Equals(entryId, id, StringComparison.Ordinal)) continue;
            var name = Text(item, "name");
            var version = Text(item, "version");
            var mapHash = Text(item, "mapHash");
            if (!IdPattern.IsMatch(entryId) || !HashPattern.IsMatch(mapHash) ||
                version.Length is < 1 or > MaxMapVersionLength || name.Length is < 1 or > MaxMapNameLength)
                throw new InvalidOperationException("CatalogMapMalformed");
            return new(entryId, name, version, mapHash);
        }
        return null;
    }

    private static string Text(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? "" : "";

    private static bool MatchesCatalog(CatalogMap? entry, string mapId, string mapHash, string mapVersion, string mapName) =>
        entry is not null && string.Equals(entry.Id, mapId, StringComparison.Ordinal) &&
        HashPattern.IsMatch(entry.MapHash) && string.Equals(entry.MapHash, mapHash, StringComparison.Ordinal) &&
        (mapVersion.Length == 0 || string.Equals(entry.Version, mapVersion, StringComparison.Ordinal)) &&
        (mapName.Length == 0 || string.Equals(entry.Name, mapName, StringComparison.Ordinal));

    private static string DecodeUtf8(byte[] bytes)
    {
        var start = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF ? 3 : 0;
        var text = new UTF8Encoding(false, true).GetString(bytes, start, bytes.Length - start);
        return text.Length > 0 && text[0] == '\uFEFF' ? text.Substring(1) : text;
    }

    private static string NormalizeJson(byte[] bytes) => DecodeUtf8(bytes).Replace("\r\n", "\n");

    /// <summary>Registers inert handlers for every other wire struct so unrelated world/combat packets never fail the poll.</summary>
    private void RegisterIgnoredPackets()
    {
        var known = new HashSet<Type>
        {
            typeof(RoomMapStatePacket), typeof(MatchPhasePacket), typeof(ResumeTokenPacket),
            typeof(WorldSnapshotPacket), typeof(MatchResultPacket), typeof(RoomMapCommandResultPacket)
        };
        var register = typeof(RoomGameplaySmoke).GetMethod(nameof(RegisterIgnore),
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        foreach (var type in typeof(ClientHelloPacket).Assembly.GetTypes())
        {
            if (!type.IsValueType || type.IsEnum || type.IsGenericTypeDefinition) continue;
            if (!typeof(INetSerializable).IsAssignableFrom(type) || known.Contains(type)) continue;
            register.MakeGenericMethod(type).Invoke(this, null);
        }
    }

    private void RegisterIgnore<T>() where T : struct, INetSerializable => _registry.Subscribe<T>(_ => { });
}
