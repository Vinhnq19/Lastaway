using System;
using System.Text;
using JungleMaze.App.Config;
using JungleMaze.Infrastructure.Network;
using JungleBackend.Protocol;
using NUnit.Framework;

namespace JungleMaze.Tests.EditMode
{
    public sealed class VpnConnectionCodeTests
    {
        private static string Encode(string value) => VpnConnectionCode.Prefix + Convert.ToBase64String(Encoding.UTF8.GetBytes(value)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        [TestCase("host.tail123", "host.tail123.ts.net", 7777)]
        [TestCase("host.tail123.ts.net", "host.tail123.ts.net", 7777)]
        [TestCase("HOST.tail123:7788", "host.tail123.ts.net", 7788)]
        public void ShortEndpoint(string input, string domain, int port)
        {
            Assert.True(VpnConnectionCode.TryParse(input, out var endpoint));
            Assert.AreEqual(domain, endpoint.Domain);
            Assert.AreEqual(domain, endpoint.Ip);
            Assert.AreEqual(port, endpoint.Port);
        }
        [TestCase("host")]
        [TestCase("https://host.tail.ts.net")]
        [TestCase("host.tail.ts.net/path")]
        [TestCase("host.tail:0")]
        [TestCase("host.tail:65536")]
        [TestCase("host.tail:7777:80")]
        [TestCase("user@host.tail")]
        public void RejectMalformedShortEndpoint(string input) => Assert.False(VpnConnectionCode.TryParse(input, out _));
        [Test]
        public void ValidCodeProducesAutoWithoutCredentialsAndPreservesProtocol()
        {
            Assert.True(VpnConnectionCode.TryParse(Encode("host\n100.80.1.20\n7777\nhost.tail123.ts.net"), out var endpoint));
            var old = new ClientConfig(ConnectionMode.Online, "https://junglemaze.duckdns.org", "junglemaze.duckdns.org", 7777, JoinAuthMode.RoomCode, 4, 5000, "Auto", "test", "wss://junglemaze.duckdns.org/game", true);
            var config = endpoint.Apply(old);
            Assert.Null(config.Validate());
            Assert.AreEqual("https://host.tail123.ts.net", config.ApiBaseUrl);
            Assert.AreEqual("wss://host.tail123.ts.net/game", config.WebSocketUrl);
            Assert.AreEqual("Auto", config.Transport);
            Assert.AreEqual(4, config.ProtocolVersion);
            Assert.True(config.WorldEnabled);
            Assert.AreEqual("junglemaze.duckdns.org", old.GameServerHost);
        }
        [TestCase("host\n192.168.1.1\n7777\nhost.tail.ts.net")]
        [TestCase("host\n100.128.1.1\n7777\nhost.tail.ts.net")]
        [TestCase("host\n100.63.1.1\n7777\nhost.tail.ts.net")]
        [TestCase("host\n100.80.1.20\n0\nhost.tail.ts.net")]
        [TestCase("host\n100.80.1.20\n65536\nhost.tail.ts.net")]
        [TestCase("host\n100.80.1.20\n7777\nattacker.com")]
        [TestCase("host\n100.80.1.20\n7777\nhost.tail.ts.net.attacker.com")]
        [TestCase("host\n100.80.1.20\n7777\nhost.tail.ts.net/path")]
        [TestCase("host\n100.80.1.20\n7777\nhost.tail.ts.net\nroomCode=secret")]
        [TestCase("<b>host</b>\n100.80.1.20\n7777\nhost.tail.ts.net")]
        public void RejectUntrustedPayload(string payload) => Assert.False(VpnConnectionCode.TryParse(Encode(payload), out _));
        [Test]
        public void RejectMalformedOversizedAndUnknownVersions()
        {
            foreach (var input in new[] { null, "", "LASTAWAY-VPN2.abc", "LASTAWAY-VPN1.%%%%", new string('a',1025) })
                Assert.False(VpnConnectionCode.TryParse(input, out _));
        }
        [Test]
        public void SavingVpnEndpointClearsAwsAccountOnlyAfterExplicitApply()
        {
            var bus = new JungleMaze.Core.Events.EventBus();
            var net = new JungleMaze.Tests.App.FakeNetwork(bus);
            var scenes = new JungleMaze.Tests.App.FakeScenes();
            var cfg = new JungleMaze.Tests.App.FakeConfig(new ClientConfig(ConnectionMode.Online, "https://junglemaze.duckdns.org", "junglemaze.duckdns.org",7777,JoinAuthMode.RoomCode,4,5000,"Auto","test","wss://junglemaze.duckdns.org/game",true));
            using var flow = new JungleMaze.App.Flow.GameFlow(bus,net,net,scenes,new JungleMaze.App.Player.PlayerInputService(net));
            using var menu = new JungleMaze.Presentation.UI.ConnectionMenuPresenter(cfg,new JungleMaze.App.Network.ConnectToServerUseCase(cfg,net,new JungleMaze.Tests.App.FakeAuth(),bus),net,flow,bus);
            menu.AccountToken="aws-token";var changed=0;menu.EndpointChanged+=()=>changed++;
            Assert.False(menu.ApplyVpnEndpoint(null)); Assert.AreEqual("aws-token",menu.AccountToken);Assert.AreEqual(0,changed);
            Assert.True(VpnConnectionCode.TryParse(Encode("host\n100.80.1.20\n7777\nhost.tail.ts.net"),out var endpoint));
            Assert.True(menu.ApplyVpnEndpoint(endpoint));Assert.Null(menu.AccountToken);Assert.AreEqual(1,changed);
            Assert.AreEqual("https://host.tail.ts.net",cfg.Current.ApiBaseUrl);
        }
        [Test]
        public void BackendVerificationRejectsCodeAndNetworkPaths()
        {
            Assert.False(WindowsVpnHostTools.IsBackend(@"\\server\share"));
            Assert.False(WindowsVpnHostTools.IsBackend("C:/Backend\" -Command evil"));
            Assert.False(WindowsVpnHostTools.IsBackend("LASTAWAY-VPN1.abc"));
        }
    }
}
