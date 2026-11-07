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
        {
            RoomId = room, SelectionRevision = revision, CatalogGeneration = 1, MapId = "map-" + hash,
            MapHash = new string(hash, 64), RulesHash = new string('c', 64), HostNetId = 0,
            ReadyCount = 0, PlayerCount = 2, LobbyRevision = 1, HumanCount = 2, BotCount = 0,
            MaxPlayers = 10, MinPlayers = 6, CanStart = false
        };
        private static T RoundTrip<T>(T packet) where T : struct, INetSerializable
        {
            var writer = new NetDataWriter();
            packet.Serialize(writer);
            var reader = new NetDataReader(writer.Data, 0, writer.Length);
            var copy = default(T);
            copy.Deserialize(reader);
            return copy;
        }
        private static void RejectsState(Func<RoomMapStatePacket, RoomMapStatePacket> mutate)
        {
            var packet = State(Guid.NewGuid(), 1, 'a');
            packet = mutate(packet);
            Assert.Throws<InvalidOperationException>(() => RoundTrip(packet));
        }
        private static void RejectsBotCommand(Func<RoomBotCommandPacket, RoomBotCommandPacket> mutate)
        {
            var packet = new RoomBotCommandPacket
            { RoomId = Guid.NewGuid(), Revision = 1, RequestId = 1, BotId = 0, Action = RoomBotAction.Add };
            packet = mutate(packet);
            Assert.Throws<InvalidOperationException>(() => RoundTrip(packet));
        }
        private static void RejectsBotResult(Func<RoomBotCommandResultPacket, RoomBotCommandResultPacket> mutate)
        {
            var packet = new RoomBotCommandResultPacket { RoomId = Guid.NewGuid(), RequestId = 1, Error = RoomBotError.None };
            packet = mutate(packet);
            Assert.Throws<InvalidOperationException>(() => RoundTrip(packet));
        }

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
        public void RoomGameplayPacketsRoundTripOnTheWire()
        {
            var room = Guid.NewGuid();
            var state = State(room, 3, 'a');
            state.MapName = "Map A"; state.MapVersion = "1"; state.CanStart = true;
            var stateCopy = RoundTrip(state);
            Assert.AreEqual(state.RoomId, stateCopy.RoomId);
            Assert.AreEqual(state.SelectionRevision, stateCopy.SelectionRevision);
            Assert.AreEqual(state.CatalogGeneration, stateCopy.CatalogGeneration);
            Assert.AreEqual(state.MapId, stateCopy.MapId);
            Assert.AreEqual(state.MapName, stateCopy.MapName);
            Assert.AreEqual(state.MapVersion, stateCopy.MapVersion);
            Assert.AreEqual(state.MapHash, stateCopy.MapHash);
            Assert.AreEqual(state.RulesHash, stateCopy.RulesHash);
            Assert.AreEqual(state.HostNetId, stateCopy.HostNetId);
            Assert.AreEqual(state.ReadyCount, stateCopy.ReadyCount);
            Assert.AreEqual(state.PlayerCount, stateCopy.PlayerCount);
            Assert.AreEqual(state.CanStart, stateCopy.CanStart);
            Assert.AreEqual(state.LobbyRevision, stateCopy.LobbyRevision);
            Assert.AreEqual(state.HumanCount, stateCopy.HumanCount);
            Assert.AreEqual(state.BotCount, stateCopy.BotCount);
            Assert.AreEqual(state.MaxPlayers, stateCopy.MaxPlayers);
            Assert.AreEqual(state.MinPlayers, stateCopy.MinPlayers);

            var select = new SelectRoomMapPacket { RoomId = room, SelectionRevision = 3, MapId = "map-a", MapHash = new string('a', 64) };
            var selectCopy = RoundTrip(select);
            Assert.AreEqual(select.RoomId, selectCopy.RoomId); Assert.AreEqual(select.SelectionRevision, selectCopy.SelectionRevision);
            Assert.AreEqual(select.MapId, selectCopy.MapId); Assert.AreEqual(select.MapHash, selectCopy.MapHash);

            var ready = new RoomMapReadyPacket { RoomId = room, SelectionRevision = 3, MapHash = new string('a', 64), RulesHash = new string('c', 64) };
            var readyCopy = RoundTrip(ready);
            Assert.AreEqual(ready.RoomId, readyCopy.RoomId); Assert.AreEqual(ready.SelectionRevision, readyCopy.SelectionRevision);
            Assert.AreEqual(ready.MapHash, readyCopy.MapHash); Assert.AreEqual(ready.RulesHash, readyCopy.RulesHash);

            var start = new StartRoomMatchPacket { RoomId = room, SelectionRevision = 3 };
            var startCopy = RoundTrip(start);
            Assert.AreEqual(start.RoomId, startCopy.RoomId); Assert.AreEqual(start.SelectionRevision, startCopy.SelectionRevision);

            var mapResult = new RoomMapCommandResultPacket { RoomId = room, Error = RoomMapError.SelectionChanged };
            var mapResultCopy = RoundTrip(mapResult);
            Assert.AreEqual(mapResult.RoomId, mapResultCopy.RoomId); Assert.AreEqual(mapResult.Error, mapResultCopy.Error);

            foreach (var action in new[] { RoomBotAction.Add, RoomBotAction.Remove, RoomBotAction.FillMinimum })
            {
                var command = new RoomBotCommandPacket
                {
                    RoomId = room, Revision = 4, RequestId = 2, Action = action,
                    BotId = action == RoomBotAction.Remove ? 7u : 0u
                };
                var commandCopy = RoundTrip(command);
                Assert.AreEqual(command.RoomId, commandCopy.RoomId); Assert.AreEqual(command.Revision, commandCopy.Revision);
                Assert.AreEqual(command.RequestId, commandCopy.RequestId); Assert.AreEqual(command.BotId, commandCopy.BotId);
                Assert.AreEqual(command.Action, commandCopy.Action);
            }

            var botResult = new RoomBotCommandResultPacket { RoomId = room, RequestId = 2, Error = RoomBotError.RoomFull };
            var botResultCopy = RoundTrip(botResult);
            Assert.AreEqual(botResult.RoomId, botResultCopy.RoomId); Assert.AreEqual(botResult.RequestId, botResultCopy.RequestId);
            Assert.AreEqual(botResult.Error, botResultCopy.Error);
        }
        [Test]
        public void RoomMapStateRejectsInvalidV3WireCounts()
        {
            RejectsState(p => { p.RoomId = Guid.Empty; return p; });
            RejectsState(p => { p.LobbyRevision = 0; return p; });
            RejectsState(p => { p.MaxPlayers = 0; return p; });
            RejectsState(p => { p.MaxPlayers = 11; return p; });
            RejectsState(p => { p.MinPlayers = 1; return p; });
            RejectsState(p => { p.MinPlayers = 11; return p; });
            RejectsState(p => { p.PlayerCount = 3; return p; });
            RejectsState(p => { p.BotCount = 1; return p; });
            RejectsState(p => { p.PlayerCount = 11; return p; });
            RejectsState(p => { p.ReadyCount = 3; return p; });
            RejectsState(p => { p.CatalogGeneration = 0; return p; });
            RejectsState(p => { p.HostNetId = 10; return p; });
        }
        [Test]
        public void RoomMapStateAllowsMinimumAboveMaximumForSmallRoomButNotStartable()
        {
            var small = State(Guid.NewGuid(), 1, 'a');
            small.MaxPlayers = 5; small.MinPlayers = 6; small.CanStart = false;
            var copy = RoundTrip(small);
            Assert.AreEqual(5, copy.MaxPlayers); Assert.AreEqual(6, copy.MinPlayers);
            Assert.IsFalse(copy.CanStart, "A room smaller than its minimum is valid state but cannot start.");
        }
        [Test]
        public void RoomBotPacketsRejectInvalidWireValues()
        {
            RejectsBotCommand(p => { p.RoomId = Guid.Empty; return p; });
            RejectsBotCommand(p => { p.RequestId = 0; return p; });
            RejectsBotCommand(p => { p.Action = (RoomBotAction)255; return p; });
            RejectsBotCommand(p => { p.Action = RoomBotAction.Add; p.BotId = 3; return p; });
            RejectsBotCommand(p => { p.Action = RoomBotAction.FillMinimum; p.BotId = 3; return p; });
            RejectsBotCommand(p => { p.Action = RoomBotAction.Remove; p.BotId = 0; return p; });

            RejectsBotResult(p => { p.RoomId = Guid.Empty; return p; });
            RejectsBotResult(p => { p.RequestId = 0; return p; });
            RejectsBotResult(p => { p.Error = (RoomBotError)255; return p; });
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
                var crlf = "\uFEFF" + json.Replace("\r\n", "\n").Replace("\n", "\r\n");
                Assert.IsNull(data.ActivateSelectedMap(crlf, (string)entry["mapHash"], hash));
                Assert.AreEqual((string)entry["mapHash"], data.SelectedMapHash); Assert.AreEqual(10, data.SelectedLayout.Spawns.Count);
                var bounds = JObject.Parse(json)["mapBounds"];
                Assert.AreEqual((int)bounds["maxX"] - (int)bounds["minX"], data.SelectedLayout.Size);
                Assert.IsNull(data.Verify((string)JObject.Parse(File.ReadAllText(Path.Combine(rules, "match_settings.json")))["version"], hash));
                var previous = data.SelectedLayout;
                Assert.IsNotNull(data.ActivateSelectedMap("{}", (string)entry["mapHash"], hash));
                Assert.AreSame(previous, data.SelectedLayout);
            }
            Assert.IsNull(data.LoadLocal()); Assert.IsNull(data.SelectedLayout);
        }
        private static string RulesHash(string path)
        {
            using var stream = new MemoryStream();
            foreach (var file in new[] { "match_settings.json", "zone_schedule.json", "world_settings.json", "items_catalog.json", "weapons.json", "airdrop_schedule.json" })
                foreach (var bytes in new[] { Encoding.UTF8.GetBytes(file), Encoding.UTF8.GetBytes(File.ReadAllText(Path.Combine(path,file)).Replace("\r\n","\n")) }) stream.Write(bytes,0,bytes.Length);
            using var hash = SHA256.Create(); return BitConverter.ToString(hash.ComputeHash(stream.ToArray())).Replace("-", "").ToLowerInvariant();
        }
        private static async Task Wait(Func<bool> condition)
        { for (var i=0;i<100&&!condition();i++) await Task.Delay(10); Assert.IsTrue(condition(),"Async selection did not complete within one second."); }
    }
}
