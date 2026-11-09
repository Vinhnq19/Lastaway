using System;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace JungleMaze.App.Config
{
    /// <summary>Endpoint only. This is not a tailnet invitation, room code or authentication credential.</summary>
    public sealed class VpnConnectionCode
    {
        public const string Prefix = "LASTAWAY-VPN1.";
        public string Name { get; }
        public string Ip { get; }
        public int Port { get; }
        public string Domain { get; }
        public string Api => "https://" + Domain;
        public string WebSocket => "wss://" + Domain + "/game";
        private VpnConnectionCode(string name, string ip, int port, string domain)
        { Name = name; Ip = ip; Port = port; Domain = domain; }

        public static bool TryParse(string code, out VpnConnectionCode endpoint)
        {
            endpoint = null;
            if (code == null || code.Length > 1024) return false;
            code = code.Trim();
            if (!code.StartsWith(Prefix, StringComparison.Ordinal))
            {
                if (code.StartsWith("LASTAWAY-", StringComparison.OrdinalIgnoreCase)) return false;
                var parts = code.ToLowerInvariant().Split(':');
                if (parts.Length > 2) return false;
                var port = 7777;
                if (parts.Length == 2 && (!int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out port) || port < 1 || port > 65535)) return false;
                var domain = parts[0].EndsWith(".ts.net", StringComparison.Ordinal) ? parts[0] : parts[0] + ".ts.net";
                if (domain.Length > 253 || !Regex.IsMatch(domain, "^(?:[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?\\.){2,}ts\\.net$")) return false;
                // MagicDNS supplies the VPN address; HTTPS keeps the original hostname for TLS.
                endpoint = new VpnConnectionCode(domain.Split('.')[0], domain, port, domain);
                return true;
            }
            try
            {
                var encoded = code.Substring(Prefix.Length);
                if (!Regex.IsMatch(encoded, "^[A-Za-z0-9_-]+$")) return false;
                encoded = encoded.Replace('-', '+').Replace('_', '/');
                encoded = encoded.PadRight((encoded.Length + 3) / 4 * 4, '=');
                var fields = new UTF8Encoding(false, true).GetString(Convert.FromBase64String(encoded)).Split('\n');
                if (fields.Length != 4 || !Regex.IsMatch(fields[0], "^[A-Za-z0-9 _-]{1,64}$")) return false;
                if (!IPAddress.TryParse(fields[1], out var ip) || ip.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork) return false;
                var bytes = ip.GetAddressBytes();
                if (ip.ToString() != fields[1] || bytes[0] != 100 || bytes[1] < 64 || bytes[1] > 127) return false;
                if (!int.TryParse(fields[2], NumberStyles.None, CultureInfo.InvariantCulture, out var port) || port < 1 || port > 65535) return false;
                var domain = fields[3];
                if (domain.Length > 253 || !Regex.IsMatch(domain, "^(?:[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?\\.){2,}ts\\.net$")) return false;
                endpoint = new VpnConnectionCode(fields[0], fields[1], port, domain);
                return true;
            }
            catch (Exception e) when (e is FormatException || e is DecoderFallbackException || e is ArgumentException) { return false; }
        }

        public ClientConfig Apply(ClientConfig current) => current
            .WithEndpoint(Ip, Port, ConnectionMode.Online, "tailscale")
            .WithApiBaseUrl(Api, "tailscale").WithTransport("Auto", WebSocket, "tailscale");
    }
}
