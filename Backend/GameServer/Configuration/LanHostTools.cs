using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using JungleBackend.Hosting;
using GameServer.Networking;

namespace GameServer.Configuration;

/// <summary>Host-only maintenance commands. Never opens a gameplay listener or prints credentials.</summary>
public static class LanHostTools
{
    public static async Task<int> Run(string[] args)
    {
        try
        {
            switch(args[0])
            {
                case "--lan-cert":
                    if(args.Length!=5)throw new ArgumentException();
                    Console.WriteLine(JsonSerializer.Serialize(EnsureCertificate(args[1],args[2],int.Parse(args[3]),int.Parse(args[4]))));return 0;
                case "--lan-probe":
                    if(args.Length!=2 || !LanConnectionCode.TryParse(args[1],out var code))throw new ArgumentException();
                    using(var handler=new HttpClientHandler {AllowAutoRedirect=false})
                    {
                        handler.ServerCertificateCustomValidationCallback=(req,cert,chain,errors)=>req.RequestUri?.Host==code.Ip && req.RequestUri.Port==code.HttpsPort &&
                            LanConnectionCode.ValidateCertificate(cert?.RawData,code.Ip,code.CertificateSha256,DateTime.UtcNow);
                        using var client=new HttpClient(handler){Timeout=TimeSpan.FromSeconds(8)};
                        using var response=await client.GetAsync(code.Api+"/health");
                        Console.WriteLine(JsonSerializer.Serialize(new {ready=response.IsSuccessStatusCode}));return response.IsSuccessStatusCode?0:1;
                    }
                case "--lan-status":
                    var secret=Environment.GetEnvironmentVariable("ROOM_CONTROL_TOKEN");
                    if(string.IsNullOrEmpty(secret))throw new InvalidOperationException();
                    using(var client=new HttpClient{Timeout=TimeSpan.FromSeconds(5)})
                    using(var request=new HttpRequestMessage(HttpMethod.Get,"http://127.0.0.1:7780/internal/deployment-status"))
                    {
                        request.Headers.Add("X-Room-Control-Key",secret);
                        using var response=await client.SendAsync(request);response.EnsureSuccessStatusCode();
                        var status=JsonSerializer.Deserialize<DeploymentStatus>(await response.Content.ReadAsStringAsync(),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;
                        Console.WriteLine(JsonSerializer.Serialize(new {safeToDeploy=status.SafeToDeploy,humanActive=status.HumanActive,humanPending=status.HumanPending,humanGrace=status.HumanGrace,bots=status.Bots}));return 0;
                    }
                default:throw new ArgumentException();
            }
        }
        catch(Exception e) when(e is not OutOfMemoryException)
        { Console.Error.WriteLine("LAN maintenance command failed ("+e.GetType().Name+"). No private data written to logs.");return 1; }
    }
    public static object EnsureCertificate(string directory,string ip,int gamePort,int httpsPort)
    {
        if(!LanConnectionCode.IsPrivateIp(ip) || gamePort is <1 or >65535 || httpsPort is <1 or >65535)throw new ArgumentException("Invalid LAN address");
        directory=Path.GetFullPath(directory);Directory.CreateDirectory(directory);
        if(!OperatingSystem.IsWindows()) File.SetUnixFileMode(directory,UnixFileMode.UserRead|UnixFileMode.UserWrite|UnixFileMode.UserExecute);
        var certPath=Path.Combine(directory,"server.crt");var keyPath=Path.Combine(directory,"server.key");
        if(File.Exists(certPath) && File.Exists(keyPath))
        {
            try
            {
                using var current=X509Certificate2.CreateFromPemFile(certPath,keyPath);
                var oldCode=LanConnectionCode.Create(ip,gamePort,httpsPort,current.RawData);LanConnectionCode.TryParse(oldCode,out var old);
                if(current.NotAfter.ToUniversalTime()>DateTime.UtcNow.AddDays(1) && LanConnectionCode.ValidateCertificate(current.RawData,ip,old.CertificateSha256,DateTime.UtcNow))
                    return new {connectionCode=oldCode,expiresAt=current.NotAfter.ToUniversalTime(),ip};
            }
            catch(CryptographicException) { }
        }
        using var key=RSA.Create(2048);
        var request=new CertificateRequest("CN=LASTAWAY LAN",key,HashAlgorithmName.SHA256,RSASignaturePadding.Pkcs1);
        var san=new SubjectAlternativeNameBuilder();san.AddIpAddress(IPAddress.Parse(ip));request.CertificateExtensions.Add(san.Build());
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false,false,0,true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature|X509KeyUsageFlags.KeyEncipherment,true));
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection{new("1.3.6.1.5.5.7.3.1")},true));
        using var cert=request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5),DateTimeOffset.UtcNow.AddYears(1));
        var tempKey=keyPath+".tmp";var tempCert=certPath+".tmp";
        File.WriteAllText(tempKey,key.ExportPkcs8PrivateKeyPem());
        if(!OperatingSystem.IsWindows())File.SetUnixFileMode(tempKey,UnixFileMode.UserRead|UnixFileMode.UserWrite);
        File.WriteAllText(tempCert,cert.ExportCertificatePem());
        File.Move(tempKey,keyPath,true);File.Move(tempCert,certPath,true);
        return new {connectionCode=LanConnectionCode.Create(ip,gamePort,httpsPort,cert.RawData),expiresAt=cert.NotAfter.ToUniversalTime(),ip};
    }
}
