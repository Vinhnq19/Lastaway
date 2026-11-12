using System;
using System.IO;
using JungleBackend.Hosting;
using JungleBackend.Protocol;
using JungleMaze.App.Config;
using JungleMaze.Infrastructure.Config;
using JungleMaze.Infrastructure.Network;
using NUnit.Framework;
namespace JungleMaze.Tests.Config
{
    public sealed class LanTrustTests
    {
        private string _path;
        [SetUp]public void Setup(){_path=Path.Combine(Path.GetTempPath(),"lastaway-trust-"+Guid.NewGuid().ToString("N"));LanTls.Configure(_path);}
        [TearDown]public void Cleanup(){if(Directory.Exists(_path))Directory.Delete(_path,true);LanTls.Configure(UnityEngine.Application.persistentDataPath);}
        private static ClientConfig Config(string ip="192.168.1.20",string pin=null,ConnectionMode mode=ConnectionMode.LAN)=>new ClientConfig(mode,"https://"+ip+":8443",ip,7777,JoinAuthMode.RoomCode,4,5000,"Auto","user:client.json","wss://"+ip+":8443/game",true,pin??new string('a',64));
        [Test]public void SavedConfigAndEveryCopyKeepPin()
        {
            var c=Config().WithEndpoint("192.168.1.20",7777,ConnectionMode.LAN,"copy").WithApiBaseUrl("https://192.168.1.20:8443","copy").WithWebSocketUrl("wss://192.168.1.20:8443/game").WithTransport("Auto","wss://192.168.1.20:8443/game");
            Assert.IsNull(c.Validate());Assert.True(ClientConfigJson.TryParse(ClientConfigJson.Serialize(c),"saved",out var loaded,out var error),error);
            Assert.AreEqual(c.LanCertificateSha256,loaded.LanCertificateSha256);Assert.False(LanTls.IsTrusted(loaded));
        }
        [Test]public void TrustIsLocalToOriginAndExactPin()
        {
            var c=Config();Assert.False(LanTls.IsTrusted(c));LanTls.Accept(c);Assert.True(LanTls.IsTrusted(c));
            Assert.False(LanTls.IsTrusted(Config(pin:new string('b',64))));Assert.False(LanTls.IsTrusted(Config("192.168.1.21")));
            Assert.NotNull(Config(mode:ConnectionMode.Online).Validate());Assert.NotNull(Config("8.8.8.8").Validate());
        }
        [Test]public void UnconfirmedConfigCannotCreateCredentialHttpClient()
        {
            Assert.Throws<InvalidOperationException>(()=>LanTls.CreateHttpClient(Config()));
            using(var probe=LanTls.CreateHttpClient(Config(),true))Assert.NotNull(probe);
            Assert.False(LanTls.Validate(Config(),new byte[]{1,2,3}));
        }
        [Test]public void CompactCodeRejectsUrlsPublicAddressesAndExtraFields()
        {
            var text=LanConnectionCode.Create("10.1.2.3",7777,8443,new byte[]{1,2,3});Assert.AreEqual(61,text.Length);
            Assert.True(LanConnectionCode.TryParse(text,out var c));Assert.AreEqual("10.1.2.3",c.Ip);
            Assert.False(LanConnectionCode.TryParse(text+"&token=secret",out _));Assert.False(LanConnectionCode.TryParse("https://10.1.2.3",out _));
            Assert.Throws<ArgumentException>(()=>LanConnectionCode.Create("100.64.1.1",7777,8443,new byte[]{1}));
        }
    }
}
