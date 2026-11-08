using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using JungleMaze.App.Config;
using JungleMaze.App.Match;
using JungleMaze.Infrastructure.Config;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.Networking;

namespace JungleMaze.Infrastructure.Api
{
    /// <summary>
    /// MAP-01 downloads for one room selection: the published catalog, one published map document and one preview PNG.
    /// Every request is GET only, strict-HTTPS (HTTP only on loopback, even when <see cref="ClientConfig.ValidateApiUrl"/>
    /// would allow a VPN HTTP base), no redirects, 15 s, bounded bodies read through a
    /// <see cref="DownloadHandlerScript"/> chunk buffer (never <c>DownloadHandlerBuffer</c>), and the token from
    /// <see cref="Func{String}"/> is sent as a bearer header only — it is never logged, stored or put in an error message.
    /// The map/preview cache lives under <c>cacheRoot/&lt;sha256(apiBase)&gt;/&lt;hash&gt;.{json,png}</c>; a cached file is
    /// reused only after <see cref="SelectedMapData.Prepare"/> accepts the schema and hashes, otherwise that one file is
    /// removed and downloaded again. Disk, hashing and schema work runs on the thread pool; Unity APIs stay on the main
    /// thread and every await resumes on the captured Unity context.
    /// </summary>
    public sealed class RoomMapFiles : IRoomMapFiles
    {
        private const int TimeoutSeconds = 15;
        private const int CatalogLimit = 256 * 1024;
        private const int MapLimit = 2 * 1024 * 1024;
        private const int PreviewLimit = 512 * 1024;
        private const int ReceiveBufferSize = 16384;
        private const int PngHeaderSize = 33;

        private static readonly Regex IdPattern = new Regex(@"^[a-z0-9][a-z0-9-]{0,31}\z", RegexOptions.CultureInvariant);
        private static readonly Regex HashPattern = new Regex(@"^[0-9a-f]{64}\z", RegexOptions.CultureInvariant);
        private static readonly UTF8Encoding StrictUtf8Encoding = new UTF8Encoding(false, true);

        private readonly Func<ClientConfig> _config;
        private readonly Func<string> _token;
        private readonly ClientRuntimeData _runtime;
        private readonly string _cacheRoot;
        private readonly string _rulesDirectory;

        /// <param name="config">Current client config; <c>ApiBaseUrl</c> is read on every call, never cached.</param>
        /// <param name="token">Current JWT (may be null/empty for guests); read per request, never logged.</param>
        /// <param name="runtime">Owns the verified selection; activation/clear are delegated, never reimplemented.</param>
        /// <param name="cacheRoot">Writable directory; the per-endpoint subdirectory is created on demand.</param>
        /// <param name="streamingAssets">Unity <c>Application.streamingAssetsPath</c>, for the bundled rules directory.</param>
        public RoomMapFiles(Func<ClientConfig> config, Func<string> token, ClientRuntimeData runtime, string cacheRoot,
            string streamingAssets)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _token = token ?? throw new ArgumentNullException(nameof(token));
            _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            if (string.IsNullOrWhiteSpace(cacheRoot)) throw new ArgumentException("Cache root is required.", nameof(cacheRoot));
            if (string.IsNullOrWhiteSpace(streamingAssets)) throw new ArgumentException("StreamingAssets path is required.", nameof(streamingAssets));
            _cacheRoot = cacheRoot;
            // Same rules directory ClientRuntimeData uses for the packaged copy.
            _rulesDirectory = Path.Combine(streamingAssets, "JungleMaze", "runtime");
        }

        public async Task<PublishedMaps> Catalog(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            ResolveEndpoint(out var urlPrefix, out _);
            var authorization = Authorization();
            var bytes = await Download(urlPrefix + "/api/maps", authorization, CatalogLimit, token);
            token.ThrowIfCancellationRequested();
            return await Task.Run(() => ParseCatalog(bytes, token), token);
        }

        public async Task<string> Map(string id, string hash, string rulesHash, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (!IsValidId(id)) throw Fail("Mã map không hợp lệ; thử lại sau.");
            if (!IsValidHash(hash)) throw Fail("Mã băm map không hợp lệ; cập nhật client rồi thử lại.");
            if (!IsValidHash(rulesHash)) throw Fail("Mã băm luật không hợp lệ; cập nhật client rồi thử lại.");
            ResolveEndpoint(out var urlPrefix, out var cacheDirectory);
            var authorization = Authorization();
            var path = Path.Combine(cacheDirectory, hash + ".json");

            var cached = await Task.Run(() => ReadMapCache(path, hash, rulesHash, token), token);
            if (cached != null) return cached;

            var bytes = await Download(urlPrefix + "/api/maps/" + id + "/" + hash, authorization, MapLimit, token);
            token.ThrowIfCancellationRequested();
            var json = await Task.Run(() => VerifyMap(bytes, hash, rulesHash, token), token);
            token.ThrowIfCancellationRequested();
            var normalized = Encoding.UTF8.GetBytes(json);
            await Task.Run(() => CommitCache(path, normalized, token), token);
            return json;
        }

        public async Task<byte[]> Preview(string hash, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (!IsValidHash(hash)) throw Fail("Mã băm ảnh xem trước không hợp lệ; cập nhật client rồi thử lại.");
            ResolveEndpoint(out var urlPrefix, out var cacheDirectory);
            var authorization = Authorization();
            var path = Path.Combine(cacheDirectory, hash + ".png");

            var cached = await Task.Run(() => ReadPreviewCache(path, hash, token), token);
            if (cached != null) return cached;

            var bytes = await Download(urlPrefix + "/api/maps/previews/" + hash, authorization, PreviewLimit, token);
            token.ThrowIfCancellationRequested();
            if (!IsPng(bytes) || !string.Equals(Sha256Hex(bytes), hash, StringComparison.Ordinal))
                throw Fail("Ảnh xem trước tải về không hợp lệ; thử lại sau.");
            await Task.Run(() => CommitCache(path, bytes, token), token);
            return bytes;
        }

        /// <summary>Delegates to the runtime owner; returns its message instead of throwing so one room stays retryable.</summary>
        public string Activate(string json, string mapHash, string rulesHash)
        {
            try { return _runtime.ActivateSelectedMap(json, mapHash, rulesHash); }
            catch (Exception e) when (!(e is OutOfMemoryException))
            { return SelectedMapData.ValidationMessage(e); }
        }

        public void Clear()
        {
            try { _runtime.ClearSelectedMap(); }
            catch (Exception e) when (!(e is OutOfMemoryException)) { }
        }

        private string Authorization()
        {
            var value = _token();
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        /// <summary>Base URL prefix and its per-endpoint cache directory; strict HTTPS, loopback HTTP only.</summary>
        private void ResolveEndpoint(out string urlPrefix, out string cacheDirectory)
        {
            var apiBaseUrl = _config()?.ApiBaseUrl?.Trim();
            if (string.IsNullOrEmpty(apiBaseUrl) || !Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var baseUri))
                throw Fail("Chưa cấu hình API để tải map; kiểm tra cài đặt mạng.");
            var https = string.Equals(baseUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
            var loopbackHttp = string.Equals(baseUri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) && baseUri.IsLoopback;
            if (!https && !loopbackHttp)
                throw Fail("API tải map cần HTTPS; HTTP chỉ được dùng trên loopback.");
            if (!string.IsNullOrEmpty(baseUri.UserInfo) || !string.IsNullOrEmpty(baseUri.Query) || !string.IsNullOrEmpty(baseUri.Fragment))
                throw Fail("API tải map không hợp lệ; kiểm tra cài đặt mạng.");
            urlPrefix = baseUri.AbsoluteUri.TrimEnd('/');
            if (urlPrefix.Length == 0) throw Fail("Chưa cấu hình API để tải map; kiểm tra cài đặt mạng.");
            cacheDirectory = Path.Combine(_cacheRoot, Sha256Hex(Encoding.UTF8.GetBytes(urlPrefix)));
        }

        private async Task<byte[]> Download(string url, string authorization, int limit, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            var handler = new BoundedDownloadHandler(limit);
            var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET)
            {
                downloadHandler = handler,
                timeout = TimeoutSeconds,
                redirectLimit = 0
            };
            var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            try
            {
                if (!string.IsNullOrEmpty(authorization) && authorization.IndexOf('\r') < 0 && authorization.IndexOf('\n') < 0)
                    request.SetRequestHeader("Authorization", "Bearer " + authorization);
                // ct is cancelled on Unity's main thread (RoomMapSession), where Abort is legal.
                using (token.Register(() => Abort(request)))
                {
                    token.ThrowIfCancellationRequested();
                    request.SendWebRequest().completed += _ => completion.TrySetResult(true);
                    await completion.Task;
                }
                if (token.IsCancellationRequested) throw new OperationCanceledException(token);
                if (handler.Exceeded) throw Fail("Dữ liệu map tải về vượt quá giới hạn; thử lại sau.");
                if (request.result != UnityWebRequest.Result.Success) throw TransportError(request);
                var data = handler.Copy();
                if (data.Length == 0) throw Fail("Máy chủ trả dữ liệu rỗng; thử lại sau.");
                if (data.Length > limit) throw Fail("Dữ liệu map tải về vượt quá giới hạn; thử lại sau.");
                return data;
            }
            catch (OperationCanceledException) { throw new OperationCanceledException(token); }
            finally
            {
                request.Dispose();
                handler.Release();
            }
        }

        private static void Abort(UnityWebRequest request)
        {
            try { request.Abort(); }
            catch (Exception e) when (!(e is OutOfMemoryException)) { }
        }

        /// <summary>Friendly transport/HTTP failure: status-based wording only, never a URL, token or response body.</summary>
        private static InvalidOperationException TransportError(UnityWebRequest request)
        {
            var status = (int)request.responseCode;
            switch (request.result)
            {
                case UnityWebRequest.Result.ProtocolError:
                    if (status == 401 || status == 403) return Fail("Phiên đăng nhập không hợp lệ hoặc đã hết hạn; đăng nhập lại rồi thử lại.");
                    if (status == 404) return Fail("Máy chủ chưa có map này; cập nhật client rồi thử lại.");
                    if (status == 429) return Fail("Máy chủ đang bận; thử lại sau.");
                    if (status >= 500) return Fail("Máy chủ map đang gặp lỗi; thử lại sau.");
                    if (status >= 300 && status < 400) return Fail("Máy chủ map chuyển hướng không hợp lệ; kiểm tra cài đặt mạng.");
                    return Fail("Không tải được map từ máy chủ; thử lại sau.");
                case UnityWebRequest.Result.DataProcessingError:
                    return Fail("Dữ liệu map tải về bị lỗi; thử lại sau.");
                default:
                    return Fail("Không kết nối được máy chủ map; kiểm tra mạng rồi thử lại.");
            }
        }

        private static PublishedMaps ParseCatalog(byte[] bytes, CancellationToken token)
        {
            try { return ParseCatalogCore(bytes, token); }
            catch (InvalidOperationException) { throw; }
            catch (OperationCanceledException) { throw; }
            catch (Exception e) when (!(e is OutOfMemoryException))
            { throw Fail("Danh sách map tải về không hợp lệ; thử lại sau."); }
        }

        private static PublishedMaps ParseCatalogCore(byte[] bytes, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            var json = StrictUtf8(bytes);
            if (json.Length > 0 && json[0] == '\uFEFF') json = json.Substring(1);
            var root = JObject.Parse(json);

            if (!ReadInteger(Field(root, "schemaVersion"), out var schemaVersion) || schemaVersion != 1)
                throw Fail("Danh sách map sai phiên bản; cập nhật client rồi thử lại.");
            if (!ReadInteger(Field(root, "generation"), out var generation) || generation <= 0)
                throw Fail("Danh sách map thiếu phiên bản nội dung; thử lại sau.");

            var catalog = JsonConvert.DeserializeObject<PublishedMaps>(json);
            if (catalog == null || catalog.Maps == null || catalog.Maps.Length == 0)
                throw Fail("Danh sách map trống; thử lại sau.");
            if (catalog.Generation != generation)
                throw Fail("Danh sách map không nhất quán; thử lại sau.");

            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var map in catalog.Maps)
            {
                if (map == null) throw Fail("Danh sách map có mục thiếu dữ liệu; thử lại sau.");
                if (!IsValidId(map.Id)) throw Fail("Danh sách map có mã không hợp lệ; thử lại sau.");
                if (!ids.Add(map.Id)) throw Fail("Danh sách map trùng mã; thử lại sau.");
                if (!IsValidHash(map.MapHash) || !IsValidHash(map.PreviewHash))
                    throw Fail("Danh sách map có mã băm không hợp lệ; thử lại sau.");
                if (!IsFilledText(map.Name, 64) || !IsOptionalText(map.Description, 256) || !IsFilledText(map.Version, 32))
                    throw Fail("Danh sách map có tên/mô tả/phiên bản không hợp lệ; thử lại sau.");
            }
            if (!IsValidId(catalog.DefaultMapId) || !ids.Contains(catalog.DefaultMapId))
                throw Fail("Danh sách map thiếu map mặc định; thử lại sau.");
            if (!IsValidHash(catalog.RulesHash))
                throw Fail("Danh sách map có mã luật không hợp lệ; thử lại sau.");
            token.ThrowIfCancellationRequested();
            return catalog;
        }

        private static JToken Field(JObject root, string name)
        {
            foreach (var property in root.Properties())
                if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase)) return property.Value;
            return null;
        }

        /// <summary>Integral JSON value only; a float or string authorises nothing.</summary>
        private static bool ReadInteger(JToken value, out long number)
        {
            number = 0;
            if (value == null || value.Type != JTokenType.Integer) return false;
            try { number = value.Value<long>(); return true; }
            catch (Exception e) when (!(e is OutOfMemoryException)) { return false; }
        }

        /// <summary>Cached map: only after the size cap, strict decode, hash and full schema/rules preparation pass.</summary>
        private string ReadMapCache(string path, string hash, string rulesHash, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            byte[] bytes;
            try { bytes = ReadCacheBytes(path, MapLimit); }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is NotSupportedException
                || e is System.Security.SecurityException) { return null; }
            if (bytes == null) { RemoveCacheFile(path); return null; }
            token.ThrowIfCancellationRequested();
            try
            {
                var json = NormalizeMapText(StrictUtf8(bytes));
                if (!string.Equals(Sha256Hex(Encoding.UTF8.GetBytes(json)), hash, StringComparison.Ordinal))
                    throw new InvalidDataException("Cached map hash does not match.");
                SelectedMapData.Prepare(json, hash, rulesHash, _rulesDirectory);
                return json;
            }
            catch (Exception e) when (!(e is OutOfMemoryException))
            {
                // Only this file is suspect (stale hash, corrupt bytes or rules changed); never touch the rest of the cache.
                RemoveCacheFile(path);
                return null;
            }
        }

        private string VerifyMap(byte[] bytes, string hash, string rulesHash, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            string json;
            try { json = NormalizeMapText(StrictUtf8(bytes)); }
            catch (InvalidDataException) { throw Fail("Dữ liệu map tải về không hợp lệ; thử lại sau."); }
            if (!string.Equals(Sha256Hex(Encoding.UTF8.GetBytes(json)), hash, StringComparison.Ordinal))
                throw Fail("Map tải về không khớp mã băm; thử lại sau.");
            PrepareMap(json, hash, rulesHash, token);
            return json;
        }

        private void PrepareMap(string json, string hash, string rulesHash, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            try { SelectedMapData.Prepare(json, hash, rulesHash, _rulesDirectory); }
            catch (Exception e) when (!(e is OutOfMemoryException))
            { throw Fail(SelectedMapData.ValidationMessage(e)); }
        }

        private static byte[] ReadPreviewCache(string path, string hash, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            byte[] bytes;
            try { bytes = ReadCacheBytes(path, PreviewLimit); }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is NotSupportedException
                || e is System.Security.SecurityException) { return null; }
            if (bytes == null) { RemoveCacheFile(path); return null; }
            token.ThrowIfCancellationRequested();
            if (!string.Equals(Sha256Hex(bytes), hash, StringComparison.Ordinal) || !IsPng(bytes))
            {
                RemoveCacheFile(path);
                return null;
            }
            return bytes;
        }

        /// <summary>Reads at most <paramref name="limit"/> bytes using the open stream's length, with an exact read loop.</summary>
        private static byte[] ReadCacheBytes(string path, int limit)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var length = stream.Length;
                if (length <= 0 || length > limit) return null;
                var bytes = new byte[(int)length];
                var offset = 0;
                while (offset < bytes.Length)
                {
                    var read = stream.Read(bytes, offset, bytes.Length - offset);
                    if (read <= 0) return null;
                    offset += read;
                }
                return stream.ReadByte() < 0 ? bytes : null;
            }
        }

        /// <summary>PNG signature plus the first IHDR chunk's declared width/height, both within 1..1024.</summary>
        private static bool IsPng(byte[] bytes)
        {
            if (bytes == null || bytes.Length < PngHeaderSize) return false;
            if (bytes[0] != 0x89 || bytes[1] != 0x50 || bytes[2] != 0x4E || bytes[3] != 0x47
                || bytes[4] != 0x0D || bytes[5] != 0x0A || bytes[6] != 0x1A || bytes[7] != 0x0A) return false;
            var chunkLength = ((uint)bytes[8] << 24) | ((uint)bytes[9] << 16) | ((uint)bytes[10] << 8) | bytes[11];
            if (chunkLength != 13) return false;
            if (bytes[12] != (byte)'I' || bytes[13] != (byte)'H' || bytes[14] != (byte)'D' || bytes[15] != (byte)'R') return false;
            var width = ((uint)bytes[16] << 24) | ((uint)bytes[17] << 16) | ((uint)bytes[18] << 8) | bytes[19];
            var height = ((uint)bytes[20] << 24) | ((uint)bytes[21] << 16) | ((uint)bytes[22] << 8) | bytes[23];
            return width >= 1 && width <= 1024 && height >= 1 && height <= 1024;
        }

        /// <summary>Temp file with a fresh name, then an atomic move/replace; a verified download still loads if the cache write fails.</summary>
        private static void CommitCache(string path, byte[] bytes, CancellationToken token)
        {
            try
            {
                token.ThrowIfCancellationRequested();
                var directory = Path.GetDirectoryName(path);
                if (string.IsNullOrEmpty(directory)) return;
                Directory.CreateDirectory(directory);
                var temp = Path.Combine(directory, Guid.NewGuid().ToString("N") + ".tmp");
                try
                {
                    using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    {
                        stream.Write(bytes, 0, bytes.Length);
                        stream.Flush(true);
                    }
                    token.ThrowIfCancellationRequested();
                    if (File.Exists(path))
                    {
                        try { File.Replace(temp, path, null, true); }
                        catch (Exception e) when (e is NotSupportedException || e is IOException || e is UnauthorizedAccessException)
                        {
                            // Keep the previous verified cache; the temp file is dropped in the finally block.
                        }
                    }
                    else File.Move(temp, path);
                }
                finally
                {
                    try { if (File.Exists(temp)) File.Delete(temp); }
                    catch (Exception e) when (e is IOException || e is UnauthorizedAccessException) { }
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is NotSupportedException
                || e is System.Security.SecurityException)
            {
                // Cache is an optimization: the caller already holds a verified document.
            }
        }

        private static void RemoveCacheFile(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is NotSupportedException) { }
        }

        private static string StrictUtf8(byte[] bytes)
        {
            if (bytes == null) throw new InvalidDataException("Downloaded data is missing.");
            try { return StrictUtf8Encoding.GetString(bytes); }
            catch (DecoderFallbackException e) { throw new InvalidDataException("Downloaded data is not valid UTF-8.", e); }
        }

        /// <summary>Same canonical form SelectedMapData hashes: UTF-8 strict, BOM stripped, CRLF to LF, at most 2 MiB.</summary>
        private static string NormalizeMapText(string text)
        {
            if (text == null) throw new InvalidDataException("Map data is missing.");
            if (text.Length > 0 && text[0] == '\uFEFF') text = text.Substring(1);
            text = text.Replace("\r\n", "\n");
            if (Encoding.UTF8.GetByteCount(text) > MapLimit) throw new InvalidDataException("Map data exceeds 2 MiB.");
            return text;
        }

        private static bool IsValidId(string id) =>
            !string.IsNullOrEmpty(id) && IdPattern.IsMatch(id);

        private static bool IsValidHash(string hash) =>
            !string.IsNullOrEmpty(hash) && HashPattern.IsMatch(hash);

        private static bool IsFilledText(string value, int max) =>
            !string.IsNullOrWhiteSpace(value) && value.Length <= max;

        private static bool IsOptionalText(string value, int max) =>
            value == null || value.Length <= max;

        private static string Sha256Hex(byte[] data)
        {
            byte[] digest;
            using (var sha = SHA256.Create()) digest = sha.ComputeHash(data);
            var chars = new char[digest.Length * 2];
            for (var i = 0; i < digest.Length; i++)
            {
                chars[i * 2] = "0123456789abcdef"[digest[i] >> 4];
                chars[i * 2 + 1] = "0123456789abcdef"[digest[i] & 0xF];
            }
            return new string(chars);
        }

        private static InvalidOperationException Fail(string message) => new InvalidOperationException(message);

        /// <summary>Chunked, size-capped body sink; returns false (Unity aborts the request) before writing past the cap.</summary>
        private sealed class BoundedDownloadHandler : DownloadHandlerScript
        {
            private readonly int _limit;
            private readonly MemoryStream _buffer;
            private bool _exceeded;

            public BoundedDownloadHandler(int limit) : base(new byte[ReceiveBufferSize])
            {
                _limit = limit;
                _buffer = new MemoryStream(Math.Min(limit, 64 * 1024));
            }

            public bool Exceeded => _exceeded;

            public byte[] Copy() => _buffer.ToArray();

            public void Release() => _buffer.Dispose();

            protected override bool ReceiveData(byte[] data, int dataLength)
            {
                if (_exceeded) return false;
                if (data == null || dataLength <= 0) return true;
                if (dataLength > _limit - _buffer.Length) { _exceeded = true; return false; }
                try { _buffer.Write(data, 0, dataLength); return true; }
                catch (Exception e) when (!(e is OutOfMemoryException)) { return false; }
            }

            protected override void CompleteContent() { }

            protected override byte[] GetData() => _buffer.ToArray();

            protected override string GetText() => null;
        }
    }
}
