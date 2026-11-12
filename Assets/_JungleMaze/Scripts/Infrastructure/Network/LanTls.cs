using System;
using System.IO;
using System.Net.Http;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using JungleBackend.Hosting;
using JungleMaze.App.Config;
using UnityEngine.Networking;

namespace JungleMaze.Infrastructure.Network
{
    /// <summary>Application-local trust only. No global certificate callbacks or changes to the Windows CA store.</summary>
    public static class LanTls
    {
        private static string _directory;
        public static void Configure(string persistentPath) => _directory=Path.Combine(persistentPath,"lan-trust");
        public static bool HasPin(ClientConfig config) => !string.IsNullOrEmpty(config?.LanCertificateSha256);
        private static string TrustFile(ClientConfig config)
        {
            using (var sha=SHA256.Create())
                return Path.Combine(_directory,BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(new Uri(config.ApiBaseUrl).GetLeftPart(UriPartial.Authority)))).Replace("-","")+".pin");
        }
        public static bool IsTrusted(ClientConfig config)
        {
            if (!HasPin(config) || config.Validate()!=null || _directory==null) return false;
            try { return File.ReadAllText(TrustFile(config)).Trim()==config.LanCertificateSha256; }
            catch(Exception e) when(e is IOException || e is UnauthorizedAccessException) { return false; }
        }
        public static void Accept(ClientConfig config)
        {
            if (!HasPin(config) || config.Validate()!=null || _directory==null) throw new InvalidOperationException("Chưa xác nhận được máy chủ LAN.");
            Directory.CreateDirectory(_directory);var path=TrustFile(config);var temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
            try { File.WriteAllText(temp,config.LanCertificateSha256); if(File.Exists(path)) File.Replace(temp,path,null);else File.Move(temp,path); }
            finally { if(File.Exists(temp))File.Delete(temp); }
        }
        public static bool Validate(ClientConfig config, byte[] der, bool confirmed=true) =>
            HasPin(config) && config.Validate()==null && (!confirmed || IsTrusted(config)) &&
            LanConnectionCode.ValidateCertificate(der,config.GameServerHost,config.LanCertificateSha256,DateTime.UtcNow);

        public static HttpClient CreateHttpClient(ClientConfig config, bool probe=false)
        {
            var handler=new HttpClientHandler {AllowAutoRedirect=false};
            if(HasPin(config))
            {
                if(config.Validate()!=null || !probe && !IsTrusted(config)) {handler.Dispose();throw new InvalidOperationException("Hãy xác nhận lại máy chủ LAN trước khi đăng nhập.");}
                var origin=new Uri(config.ApiBaseUrl);
                handler.ServerCertificateCustomValidationCallback=(request,cert,chain,errors)=>
                    request.RequestUri.Scheme=="https" && request.RequestUri.Host==origin.Host && request.RequestUri.Port==origin.Port &&
                    Validate(config,cert?.RawData,!probe);
            }
            return new HttpClient(handler) {Timeout=TimeSpan.FromSeconds(10)};
        }
        public static bool Attach(UnityWebRequest request, ClientConfig config)
        {
            if(!HasPin(config))return true;
            if(config.Validate()!=null || !IsTrusted(config))return false;
            var url=new Uri(request.url);var origin=new Uri(config.ApiBaseUrl);
            if(url.Scheme!="https" || url.Host!=origin.Host || url.Port!=origin.Port)return false;
            request.redirectLimit=0;request.certificateHandler=new PinnedCertificate(config);request.disposeCertificateHandlerOnDispose=true;return true;
        }
        public static void Attach(ClientWebSocket socket, Uri uri, ClientConfig config)
        {
            if(!HasPin(config))return;
            if(config.Validate()!=null || !IsTrusted(config) || uri.AbsoluteUri!=config.WebSocketUrl)
                throw new InvalidOperationException("Chưa xác nhận được máy chủ LAN.");
            socket.Options.RemoteCertificateValidationCallback=(sender,cert,chain,errors)=>Validate(config,cert?.GetRawCertData());
        }
        private sealed class PinnedCertificate : CertificateHandler
        {
            private readonly ClientConfig _config;
            public PinnedCertificate(ClientConfig config)=>_config=config;
            protected override bool ValidateCertificate(byte[] certificateData)=>Validate(_config,certificateData);
        }
    }
}
