using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using JungleBackend.Hosting;
using GameServer.Configuration;

var passed=0;
void Check(bool value,string name){if(!value)throw new Exception(name);Console.WriteLine("PASS "+name);passed++;}
var directory=Path.Combine(Path.GetTempPath(),"lastaway-lan-checks-"+Guid.NewGuid().ToString("N"));
try
{
    var created=JsonSerializer.SerializeToElement(LanHostTools.EnsureCertificate(directory,"192.168.1.20",7777,8443));
    var text=created.GetProperty("connectionCode").GetString()!;
    Check(text.Length==61,"compact code is 61 characters");
    Check(LanConnectionCode.TryParse(text,out var code) && code.Ip=="192.168.1.20" && code.GamePort==7777 && code.HttpsPort==8443,"code address and ports roundtrip");
    using var cert=X509Certificate2.CreateFromPemFile(Path.Combine(directory,"server.crt"),Path.Combine(directory,"server.key"));
    Check(cert.HasPrivateKey,"host PEM key pairs with certificate");
    Check(LanConnectionCode.ValidateCertificate(cert.RawData,code.Ip,code.CertificateSha256,DateTime.UtcNow),"generated certificate passes exact IP pin and validity");
    Check(!LanConnectionCode.ValidateCertificate(cert.RawData,"192.168.1.21",code.CertificateSha256,DateTime.UtcNow),"SAN rejects different IP");
    Check(!LanConnectionCode.ValidateCertificate(cert.RawData,code.Ip,new string('0',64),DateTime.UtcNow),"reject incorrect fingerprint");
    Check(!LanConnectionCode.ValidateCertificate(cert.RawData,code.Ip,code.CertificateSha256,cert.NotAfter.ToUniversalTime()),"reject expired certificate at boundary");
    Check(!LanConnectionCode.ValidateCertificate(cert.RawData,code.Ip,code.CertificateSha256,cert.NotBefore.ToUniversalTime().AddSeconds(-1)),"reject not-yet-valid certificate");
    Check(cert.NotAfter.ToUniversalTime()<DateTime.UtcNow.AddDays(367),"certificate validity at most a year");
    var reused=JsonSerializer.SerializeToElement(LanHostTools.EnsureCertificate(directory,code.Ip,7777,8443));
    Check(reused.GetProperty("connectionCode").GetString()==text,"setup/start reuses valid key and code");
    foreach(var ip in new[]{"127.0.0.1","0.0.0.0","8.8.8.8","100.70.1.1","169.254.1.1","::1","192.168.001.1","192.168.1.1:8443","https://192.168.1.1"})Check(!LanConnectionCode.IsPrivateIp(ip),"reject address "+ip);
    foreach(var ip in new[]{"10.0.0.1","172.16.0.1","172.31.255.254","192.168.1.1"})Check(LanConnectionCode.IsPrivateIp(ip),"accept RFC1918 "+ip);
    foreach(var bad in new[]{"",text+"x",text.Replace("LWLAN1.","LWLAN2."),text+"/api",text[..^1]+"!",text[..^1]})Check(!LanConnectionCode.TryParse(bad,out _),"reject malformed code length="+bad.Length);
    var data=Convert.FromBase64String(text[7..].Replace('-','+').Replace('_','/')+"==");data[4]=data[5]=0;
    Check(!LanConnectionCode.TryParse("LWLAN1."+Convert.ToBase64String(data).TrimEnd('=').Replace('+','-').Replace('/','_'),out _),"reject zero port");
    var rotated=JsonSerializer.SerializeToElement(LanHostTools.EnsureCertificate(directory,"10.0.0.5",7777,8443));
    Check(rotated.GetProperty("connectionCode").GetString()!=text,"IP change yields new certificate/code");
    foreach(var der in new[]{Array.Empty<byte>(),new byte[]{0x30,0xff,0},RandomNumberGenerator.GetBytes(64)})
    {
        var raw=LanConnectionCode.Create(code.Ip,7777,8443,der);LanConnectionCode.TryParse(raw,out var invalid);
        Check(!LanConnectionCode.ValidateCertificate(der,code.Ip,invalid.CertificateSha256,DateTime.UtcNow),"malformed DER rejected safely");
    }
    await ClientTlsChecks.Run(directory,Check);
    Console.WriteLine($"{passed} LAN host and TLS checks passed");return 0;
}
finally{if(Directory.Exists(directory))Directory.Delete(directory,true);}
