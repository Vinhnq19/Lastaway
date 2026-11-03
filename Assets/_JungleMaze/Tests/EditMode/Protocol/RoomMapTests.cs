using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JungleBackend.Protocol.Gameplay;
using JungleMaze.App.Match;
using JungleMaze.App.Ports;
using JungleMaze.Core.Events;
using JungleMaze.Domain.Network;
using JungleMaze.Domain.Player;
using JungleMaze.Infrastructure.Config;
using JungleMaze.Tests.App;
using LiteNetLib.Utils;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

namespace JungleMaze.Tests.Protocol
{
    public sealed class RoomMapTests
    {
        private sealed class Sender : IPacketSender
        {
            public readonly List<object> Packets = new List<object>();
            public bool Send<T>(ref T packet, PacketDelivery delivery) where T : struct, INetSerializable
            { Packets.Add(packet); return true; }
        }
        private sealed class Files : IRoomMapFiles
        {
            public readonly Dictionary<string, TaskCompletionSource<string>> Requests = new Dictionary<string, TaskCompletionSource<string>>();
            public string Activated;
            public string Failure;
            public bool Cleared;
            public Task<PublishedMaps> Catalog(CancellationToken ct) => Task.FromResult(new PublishedMaps { Generation = 1 });
            public Task<string> Map(string id, string hash, string rulesHash, CancellationToken ct)
            { var source = new TaskCompletionSource<string>(); Requests[hash] = source; return source.Task; }
            public Task<byte[]> Preview(string hash, CancellationToken ct) => Task.FromResult(Array.Empty<byte>());
            public string Activate(string json, string hash, string rulesHash) { if (Failure == null) Activated = json; return Failure; }
            public void Clear() { Cleared = true; Activated = null; }
        }
        private static RoomMapStatePacket State(Guid room, uint revision, char hash) => new RoomMapStatePacket
        { RoomId = room, SelectionRevision = revision, CatalogGeneration = 1, MapId = "map-" + hash, MapHash = new string(hash, 64), RulesHash = new string('c', 64), HostNetId = 0, PlayerCount = 2 };

        [Test]
        public async Task SupersededDownloadCannotActivateOrAcknowledgePreviousRoomSelection()
        {
            var bus = new EventBus(); var network = new FakeNetwork(bus); var sender = new Sender(); var files = new Files();
            using var match = new MatchSession(network, sender, network, bus, new PlayerRegistry()); match.Initialize();
            using var maps = new RoomMapSession(match, sender, network, files); maps.Initialize();
            network.SetState(NetworkState.Joined, DisconnectReason.None); maps.Tick(0);
            var room = Guid.NewGuid(); network.Push(State(room, 1, 'a')); network.Push(State(room, 2, 'b'));
            files.Requests[new string('b', 64)].SetResult("map-B");
            await Wait(() => maps.Verified);
            files.Requests[new string('a', 64)].SetResult("map-A"); await Task.Delay(20);
            Assert.AreEqual("map-B", files.Activated);
            var acks = sender.Packets.OfType<RoomMapReadyPacket>().ToArray();
            Assert.AreEqual(1, acks.Length); Assert.AreEqual(2, acks[0].SelectionRevision);
            network.SetState(NetworkState.Disconnected, DisconnectReason.UserRequested); maps.Tick(0);
            Assert.IsFalse(maps.Verified); Assert.IsFalse(maps.HasState); Assert.IsTrue(files.Cleared);
        }
        [Test]
        public async Task InvalidDownloadStaysUnreadyAndExplicitRetryCanRecover()
        {
            var bus = new EventBus(); var network = new FakeNetwork(bus); var sender = new Sender(); var files = new Files { Failure = "bad hash" };
            using var match = new MatchSession(network, sender, network, bus, new PlayerRegistry()); match.Initialize();
            using var maps = new RoomMapSession(match, sender, network, files); maps.Initialize();
            network.SetState(NetworkState.Joined, DisconnectReason.None);
            var state = State(Guid.NewGuid(), 1, 'a'); network.Push(state);
            files.Requests[state.MapHash].SetResult("bad"); await Wait(() => !maps.Busy);
            Assert.IsFalse(maps.Verified); Assert.IsNotNull(maps.Error); Assert.IsFalse(sender.Packets.OfType<RoomMapReadyPacket>().Any());
            files.Failure = null; maps.Retry(); files.Requests[state.MapHash].SetResult("valid"); await Wait(() => maps.Verified);
            Assert.IsNull(maps.Error); Assert.AreEqual("valid", files.Activated);
        }
        [Test]
        public void BothPublishedMapsActivateWithSameRulesAndInvalidReplacementPreservesCurrentMap()
        {
            var rules = Path.GetFullPath(Path.Combine(Application.dataPath, "../Backend/Shared/GameData"));
            var catalog = JObject.Parse(File.ReadAllText(Path.Combine(rules, "Maps/catalog.json")));
            var hash = RulesHash(rules);
            var data = new ClientRuntimeData(Application.streamingAssetsPath);
            foreach (var entry in (JArray)catalog["maps"])
            {
                var json = File.ReadAllText(Path.Combine(rules, "Maps", (string)entry["id"], (string)entry["mapHash"], "map.json"));
                Assert.IsNull(data.ActivateSelectedMap("\uFEFF" + json.Replace("\n", "\r\n"), (string)entry["mapHash"], hash));
                Assert.AreEqual((string)entry["mapHash"], data.SelectedMapHash); Assert.AreEqual(10, data.SelectedLayout.Spawns.Count);
                Assert.IsNull(data.Verify((string)JObject.Parse(File.ReadAllText(Path.Combine(rules,"match_settings.json")))["version"], hash));
                var previous = data.SelectedLayout;
                Assert.IsNotNull(data.ActivateSelectedMap("{}", (string)entry["mapHash"], hash));
                Assert.AreSame(previous, data.SelectedLayout);
            }
            Assert.IsNull(data.LoadLocal()); Assert.IsNull(data.SelectedLayout);
        }
        private static string RulesHash(string path)
        {
            using var stream = new MemoryStream();
            foreach (var file in new[] { "match_settings.json", "zone_schedule.json", "world_settings.json", "items_catalog.json", "weapons.json" })
                foreach (var bytes in new[] { Encoding.UTF8.GetBytes(file), Encoding.UTF8.GetBytes(File.ReadAllText(Path.Combine(path,file)).Replace("\r\n","\n")) }) stream.Write(bytes,0,bytes.Length);
            using var hash = SHA256.Create(); return BitConverter.ToString(hash.ComputeHash(stream.ToArray())).Replace("-", "").ToLowerInvariant();
        }
        private static async Task Wait(Func<bool> condition)
        { for (var i=0;i<100&&!condition();i++) await Task.Delay(10); Assert.IsTrue(condition(),"Async selection did not complete within one second."); }
    }
}
