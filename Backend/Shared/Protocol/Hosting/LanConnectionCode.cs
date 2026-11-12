#nullable disable
using System;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace JungleBackend.Hosting
{
    /// <summary>Public LAN address + exact certificate pin. Never an account credential or room code.</summary>
    public sealed class LanConnectionCode
    {
        public const string Prefix = "LWLAN1.";
        public string Ip { get; private set; }
        public int GamePort { get; private set; }
        public int HttpsPort { get; private set; }
        public string CertificateSha256 { get; private set; }
        public string Api => "https://" + Ip + ":" + HttpsPort;
        public string WebSocket => "wss://" + Ip + ":" + HttpsPort + "/game";
        private LanConnectionCode() { }
        public static bool IsPrivateIp(string value)
        {
            if (!IPAddress.TryParse(value, out var address) || address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork || address.ToString() != value) return false;
            var b = address.GetAddressBytes();
            return b[0] == 10 || b[0] == 172 && b[1] >= 16 && b[1] <= 31 || b[0] == 192 && b[1] == 168;
        }
        public static bool IsPin(string pin)
        {
            if (pin == null || pin.Length != 64) return false;
            foreach (var c in pin) if (!(c >= '0' && c <= '9' || c >= 'a' && c <= 'f')) return false;
            return true;
        }
        public static string Create(string ip, int gamePort, int httpsPort, byte[] certificate)
        {
            if (!IsPrivateIp(ip) || gamePort < 1 || gamePort > 65535 || httpsPort < 1 || httpsPort > 65535) throw new ArgumentException("Invalid LAN address");
            var data = new byte[40]; Array.Copy(IPAddress.Parse(ip).GetAddressBytes(), data, 4);
            data[4] = (byte)(gamePort >> 8); data[5] = (byte)gamePort;
            data[6] = (byte)(httpsPort >> 8); data[7] = (byte)httpsPort;
            using (var sha = SHA256.Create()) Array.Copy(sha.ComputeHash(certificate), 0, data, 8, 32);
            return Prefix + Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }
        public static bool TryParse(string value, out LanConnectionCode code)
        {
            code = null; value = value?.Trim();
            if (value == null || value.Length != Prefix.Length + 54 || !value.StartsWith(Prefix, StringComparison.Ordinal)) return false;
            foreach (var c in value.Substring(Prefix.Length)) if (!(c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z' || c >= '0' && c <= '9' || c == '-' || c == '_')) return false;
            try
            {
                var b = Convert.FromBase64String(value.Substring(Prefix.Length).Replace('-', '+').Replace('_', '/') + "==");
                var ip = new IPAddress(new byte[] {b[0],b[1],b[2],b[3]}).ToString(); var game = b[4] * 256 + b[5]; var https = b[6] * 256 + b[7];
                if (b.Length != 40 || !IsPrivateIp(ip) || game == 0 || https == 0) return false;
                // Reject alternate encodings of the same payload (unused trailing base64 bits).
                if (Prefix + Convert.ToBase64String(b).TrimEnd('=').Replace('+','-').Replace('/','_') != value) return false;
                var pin = new byte[32];Array.Copy(b,8,pin,0,32);
                code = new LanConnectionCode {Ip=ip,GamePort=game,HttpsPort=https,CertificateSha256=BitConverter.ToString(pin).Replace("-", "").ToLowerInvariant()};return true;
            }
            catch (FormatException) { return false; }
        }
        /// <summary>Exact out-of-band trust anchor: also enforce validity, server EKU and the advertised IPv4 SAN.</summary>
        public static bool ValidateCertificate(byte[] der, string ip, string pin, DateTime utcNow)
        {
            if (!IsPrivateIp(ip) || !IsPin(pin) || der == null) return false;
            try
            {
                using (var sha = SHA256.Create())
                    if (BitConverter.ToString(sha.ComputeHash(der)).Replace("-", "").ToLowerInvariant() != pin) return false;
                using (var cert = new X509Certificate2(der))
                {
                    if (utcNow < cert.NotBefore.ToUniversalTime() || utcNow >= cert.NotAfter.ToUniversalTime()) return false;
                    var san = cert.Extensions["2.5.29.17"]; if (san == null || !HasIp(san.RawData, IPAddress.Parse(ip).GetAddressBytes())) return false;
                    var usage = cert.Extensions["2.5.29.37"]; if (usage == null) return false;
                    var eku = new X509EnhancedKeyUsageExtension(usage, usage.Critical);
                    foreach (var oid in eku.EnhancedKeyUsages) if (oid.Value == "1.3.6.1.5.5.7.3.1") return true;
                    return false;
                }
            }
            catch (Exception e) when (e is CryptographicException || e is ArgumentException || e is IndexOutOfRangeException) { return false; }
        }
        private static bool HasIp(byte[] data, byte[] expected)
        {
            var offset=0;
            if (!Read(data, ref offset, out var tag, out var length) || tag != 0x30 || offset+length != data.Length) return false;
            while (offset < data.Length)
            {
                if (!Read(data,ref offset,out tag,out length)) return false;
                if (tag == 0x87 && length == 4 && data[offset]==expected[0] && data[offset+1]==expected[1] && data[offset+2]==expected[2] && data[offset+3]==expected[3]) return true;
                offset += length;
            }
            return false;
        }
        private static bool Read(byte[] data, ref int offset, out int tag, out int length)
        {
            tag=length=0; if (offset+2 > data.Length) return false; tag=data[offset++];var first=data[offset++];
            if (first < 128) length=first;
            else { var count=first&127;if(count==0 || count>2 || offset+count>data.Length)return false;while(count-->0)length=(length<<8)|data[offset++]; }
            return length <= data.Length-offset;
        }
    }
}
