using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using GameServer.Simulation;
using JungleMaze.World;

namespace GameServer.Maps;

/// <summary>DTO for catalog.json. Only used for (de)serialization; snapshots never expose it.</summary>
public sealed class MapCatalogManifest
{
    public int SchemaVersion { get; set; } = 1;
    public long Generation { get; set; }
    public string DefaultMapId { get; set; } = string.Empty;
    public MapCatalogEntry[] Maps { get; set; } = Array.Empty<MapCatalogEntry>();
}

/// <summary>One catalog entry. Setters exist for serialization only; snapshots keep copies.</summary>
public sealed class MapCatalogEntry
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string MapHash { get; set; } = string.Empty;
    public string PreviewHash { get; set; } = string.Empty;

    internal MapCatalogEntry Copy() => new()
    {
        Id = Id,
        Name = Name,
        Description = Description,
        Version = Version,
        MapHash = MapHash,
        PreviewHash = PreviewHash
    };
}

/// <summary>Immutable, fully validated map content. All members are get-only.</summary>
public sealed class MapDefinition
{
    private readonly MapCatalogEntry _entry;
    public MapCatalogEntry Entry => _entry.Copy();
    public string Json { get; }
    public MapData Data { get; }
    public MapLayout Layout { get; }
    public GameServer.Simulation.Bots.BotNavigationGrid Navigation { get; }
    public byte[] Preview { get; }

    internal MapDefinition(MapCatalogEntry entry, string json, MapData data, MapLayout layout, byte[] preview)
    {
        _entry = entry.Copy();
        Json = json;
        Data = data;
        Layout = layout;
        Navigation = new GameServer.Simulation.Bots.BotNavigationGrid(layout);
        Preview = preview;
    }
}

/// <summary>Immutable catalog snapshot; built off the tick thread, safe for concurrent reads.</summary>
public sealed class MapCatalogSnapshot
{
    private const int CatalogMaxBytes = 256 * 1024;
    private const int MapMaxBytes = 2 * 1024 * 1024;
    private const int PreviewMaxBytes = 512 * 1024;

    private static readonly byte[] PngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

    private static readonly Regex IdPattern = new("^[a-z0-9][a-z0-9-]{0,31}\\z",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex HashPattern = new("^[0-9a-f]{64}\\z",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly JsonSerializerOptions ManifestJson = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly JsonSerializerOptions DocumentJson = new()
    {
        IncludeFields = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly IReadOnlyDictionary<string, MapDefinition> _maps;

    public long Generation { get; }
    public string DefaultMapId { get; }
    public IReadOnlyDictionary<string, MapDefinition> Maps => _maps;

    internal MapCatalogSnapshot(long generation, string defaultMapId, IReadOnlyDictionary<string, MapDefinition> maps)
    {
        Generation = generation;
        DefaultMapId = defaultMapId;
        var copy = new Dictionary<string, MapDefinition>(StringComparer.Ordinal);
        foreach (var pair in maps) copy.Add(pair.Key, pair.Value);
        _maps = new ReadOnlyDictionary<string, MapDefinition>(copy);
    }

    public MapDefinition? Find(string id, string? hash = null)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (!_maps.TryGetValue(id, out var definition)) return null;
        if (hash is null) return definition;
        return string.Equals(hash, definition.Entry.MapHash, StringComparison.Ordinal) ? definition : null;
    }

    /// <summary>Loads and validates the whole catalog under <paramref name="directory"/> or throws.</summary>
    public static MapCatalogSnapshot Load(string directory, string worldVersion)
    {
        if (string.IsNullOrWhiteSpace(worldVersion))
            throw new ArgumentException("World version is required to build a map catalog snapshot.");

        var root = ResolveRoot(directory);
        var manifestBytes = ReadBounded(Path.Combine(root, "catalog.json"), CatalogMaxBytes, "catalog.json");

        MapCatalogManifest? manifest;
        try
        {
            manifest = JsonSerializer.Deserialize<MapCatalogManifest>(DecodeUtf8(manifestBytes), ManifestJson);
        }
        catch (JsonException e)
        {
            throw new ArgumentException("catalog.json is not valid JSON.", e);
        }

        if (manifest is null) throw new ArgumentException("catalog.json does not contain a catalog object.");
        ValidateManifest(manifest);

        var maps = new Dictionary<string, MapDefinition>(StringComparer.Ordinal);
        foreach (var entry in manifest.Maps)
            maps.Add(entry.Id, LoadDefinition(root, entry, worldVersion));

        return new MapCatalogSnapshot(manifest.Generation, manifest.DefaultMapId, maps);
    }

    private static MapDefinition LoadDefinition(string root, MapCatalogEntry source, string worldVersion)
    {
        var entry = source.Copy();
        var mapDirectory = SafeDirectory(root, entry.Id);
        var hashDirectory = SafeDirectory(mapDirectory, entry.MapHash);

        var json = NormalizeJson(ReadBounded(Path.Combine(hashDirectory, "map.json"), MapMaxBytes,
            $"map.json for '{entry.Id}'"));
        var actualMapHash = HashHex(Encoding.UTF8.GetBytes(json));
        if (!string.Equals(actualMapHash, entry.MapHash, StringComparison.Ordinal))
            throw new ArgumentException(
                $"Map '{entry.Id}' content hash {actualMapHash} does not match manifest hash {entry.MapHash}.");

        var preview = ReadBounded(Path.Combine(hashDirectory, entry.PreviewHash + ".png"), PreviewMaxBytes,
            $"preview.png for '{entry.Id}'");
        if (!HasPngSignature(preview))
            throw new ArgumentException($"Map '{entry.Id}' preview.png is not a PNG image.");
        var actualPreviewHash = HashHex(preview);
        if (!string.Equals(actualPreviewHash, entry.PreviewHash, StringComparison.Ordinal))
            throw new ArgumentException(
                $"Map '{entry.Id}' preview hash {actualPreviewHash} does not match manifest hash {entry.PreviewHash}.");

        var (data, layout) = BuildAuthoritative(entry.Id, json, worldVersion);
        return new MapDefinition(entry, json, data, layout, preview);
    }

    /// <summary>Runs the existing authored/layout validators; no new domain fields are invented.</summary>
    private static (MapData Data, MapLayout Layout) BuildAuthoritative(string id, string json, string worldVersion)
    {
        try
        {
            var document = JsonSerializer.Deserialize<AuthoredMapDocument>(json, DocumentJson)
                ?? throw new ArgumentException("Authored map document is empty.");
            // Existing authored builder validates geometry, counts, hazards and connectivity.
            var layout = document.Build(worldVersion);
            var data = MapData.Load(json);
            if (data.ReachableCells().Count == 0)
                throw new ArgumentException("Authored map has no reachable cells.");
            if (data.SpawnPoints.Count != 10 || layout.Spawns.Count != 10)
                throw new ArgumentException("Authored map requires exactly 10 spawn points.");
            return (data, layout);
        }
        catch (ArgumentException e)
        {
            throw new ArgumentException($"Map '{id}' failed authored geometry validation: {e.Message}", e);
        }
        catch (InvalidOperationException e)
        {
            throw new ArgumentException($"Map '{id}' failed authored geometry validation: {e.Message}", e);
        }
        catch (JsonException e)
        {
            throw new ArgumentException($"Map '{id}' is not a valid authored map document: {e.Message}", e);
        }
    }

    private static void ValidateManifest(MapCatalogManifest manifest)
    {
        if (manifest.SchemaVersion != 1)
            throw new ArgumentException($"Unsupported catalog schema version {manifest.SchemaVersion}.");
        if (manifest.Generation <= 0)
            throw new ArgumentException("Catalog generation must be greater than zero.");
        if (manifest.Maps is null || manifest.Maps.Length == 0)
            throw new ArgumentException("Catalog must contain at least one map.");
        if (string.IsNullOrEmpty(manifest.DefaultMapId))
            throw new ArgumentException("Catalog defaultMapId is required.");

        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in manifest.Maps)
        {
            ValidateEntry(entry);
            if (!ids.Add(entry.Id))
                throw new ArgumentException($"Duplicate map id '{entry.Id}' in catalog.");
        }

        if (!ids.Contains(manifest.DefaultMapId))
            throw new ArgumentException($"Catalog defaultMapId '{manifest.DefaultMapId}' is not present.");
    }

    private static void ValidateEntry(MapCatalogEntry? entry)
    {
        if (entry is null) throw new ArgumentException("Catalog contains a null map entry.");
        if (entry.Id is null || !IdPattern.IsMatch(entry.Id))
            throw new ArgumentException("Map id must match ^[a-z0-9][a-z0-9-]{0,31}$.");
        if (entry.Name is null || entry.Name.Length is < 1 or > 64 || string.IsNullOrWhiteSpace(entry.Name))
            throw new ArgumentException($"Map '{entry.Id}' name must be 1..64 characters.");
        if (entry.Description is null || entry.Description.Length > 256)
            throw new ArgumentException($"Map '{entry.Id}' description must be at most 256 characters.");
        if (entry.Version is null || entry.Version.Length is < 1 or > 32)
            throw new ArgumentException($"Map '{entry.Id}' version must be 1..32 characters.");
        if (entry.MapHash is null || !HashPattern.IsMatch(entry.MapHash))
            throw new ArgumentException($"Map '{entry.Id}' mapHash must be 64 lowercase hex characters.");
        if (entry.PreviewHash is null || !HashPattern.IsMatch(entry.PreviewHash))
            throw new ArgumentException($"Map '{entry.Id}' previewHash must be 64 lowercase hex characters.");
    }

    private static string ResolveRoot(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory))
            throw new ArgumentException("Map catalog directory is required.");
        string full;
        try
        {
            full = Path.GetFullPath(directory);
        }
        catch (Exception e)
        {
            throw new ArgumentException("Map catalog directory is invalid.", e);
        }

        if (!Directory.Exists(full))
            throw new ArgumentException($"Map catalog directory does not exist: {full}");
        if ((File.GetAttributes(full) & FileAttributes.ReparsePoint) != 0)
            throw new ArgumentException("Map catalog directory must not be a symlink or reparse point.");
        return Path.TrimEndingDirectorySeparator(full);
    }

    private static string SafeDirectory(string parent, string child)
    {
        if (string.IsNullOrEmpty(child)) throw new ArgumentException("Catalog path component is empty.");
        var basePath = Path.TrimEndingDirectorySeparator(parent);
        var prefix = basePath + Path.DirectorySeparatorChar;
        var full = Path.GetFullPath(Path.Combine(basePath, child));
        if (!full.StartsWith(prefix, StringComparison.Ordinal))
            throw new ArgumentException("Catalog path escapes the catalog directory.");
        if (!Directory.Exists(full))
            throw new ArgumentException($"Catalog directory is missing: {full}");
        if ((File.GetAttributes(full) & FileAttributes.ReparsePoint) != 0)
            throw new ArgumentException("Catalog paths must not contain symlink or reparse point components.");
        return full;
    }

    private static byte[] ReadBounded(string path, int maxBytes, string what)
    {
        var info = new FileInfo(path);
        if (!info.Exists) throw new ArgumentException($"{what} is missing.");
        if ((info.Attributes & FileAttributes.ReparsePoint) != 0)
            throw new ArgumentException($"{what} must not be a symlink or reparse point.");
        // Size is rejected before any content is read into memory.
        if (info.Length <= 0 || info.Length > maxBytes)
            throw new ArgumentException($"{what} size {info.Length} is outside 1..{maxBytes} bytes.");

        using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (file.Length <= 0 || file.Length > maxBytes)
            throw new ArgumentException($"{what} size changed while reading.");
        var bytes = new byte[(int)file.Length];
        file.ReadExactly(bytes);
        if (file.ReadByte() != -1) throw new ArgumentException($"{what} grew while reading.");
        return bytes;
    }

    private static string DecodeUtf8(byte[] bytes)
    {
        var start = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF ? 3 : 0;
        var text = new UTF8Encoding(false, true).GetString(bytes, start, bytes.Length - start);
        return text.Length > 0 && text[0] == '\uFEFF' ? text.Substring(1) : text;
    }

    private static string NormalizeJson(byte[] bytes) => DecodeUtf8(bytes).Replace("\r\n", "\n");

    private static bool HasPngSignature(byte[] bytes)
    {
        if (bytes.Length < PngSignature.Length) return false;
        for (var i = 0; i < PngSignature.Length; i++)
            if (bytes[i] != PngSignature[i]) return false;
        return true;
    }

    private static string HashHex(byte[] data) =>
        Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant();
}
