using GameServer.Maps;
using GameServer.Simulation;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

const string PngBase64 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==";

var checks = 0;
void Check(bool okay, string name)
{
    checks++;
    if (!okay) throw new Exception("FAIL: " + name);
    Console.WriteLine("PASS: " + name);
}
void ThrowsArg(Action action, string name)
{
    try { action(); }
    catch (ArgumentException) { Check(true, name); return; }
    catch (Exception e) { throw new Exception("FAIL: " + name + " (unexpected " + e.GetType().Name + ")"); }
    throw new Exception("FAIL: " + name + " (no exception)");
}

string DecodeUtf8(byte[] bytes)
{
    var start = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF ? 3 : 0;
    var text = Encoding.UTF8.GetString(bytes, start, bytes.Length - start);
    return text.Length > 0 && text[0] == '\uFEFF' ? text.Substring(1) : text;
}
string Normalize(byte[] bytes) => DecodeUtf8(bytes).Replace("\r\n", "\n");
string Sha(byte[] data) => Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant();

var sourcePath = Path.Combine(AppContext.BaseDirectory, "GameData", "map_demo.json");
Check(File.Exists(sourcePath), "current map_demo fixture is available to checks");
var sourceBytes = File.ReadAllBytes(sourcePath);
var canonicalText = Normalize(sourceBytes);
var pngBytes = Convert.FromBase64String(PngBase64);

var temp = Path.Combine(Path.GetTempPath(), "mapcatalog-" + Guid.NewGuid().ToString("N"));
var previousWorldMode = Environment.GetEnvironmentVariable("WORLD_ENABLED");
Directory.CreateDirectory(temp);
try
{
    string Root(string name)
    {
        var path = Path.Combine(temp, name);
        Directory.CreateDirectory(path);
        return path;
    }

    (string MapHash, string PreviewHash) WriteMap(string root, string id, byte[] mapBytes, byte[] preview)
    {
        var mapHash = Sha(Encoding.UTF8.GetBytes(Normalize(mapBytes)));
        var previewHash = Sha(preview);
        var directory = Path.Combine(root, id, mapHash);
        Directory.CreateDirectory(directory);
        File.WriteAllBytes(Path.Combine(directory, "map.json"), mapBytes);
        File.WriteAllBytes(Path.Combine(directory, previewHash + ".png"), preview);
        return (mapHash, previewHash);
    }

    MapCatalogEntry Entry(string id, string mapHash, string previewHash, string name = "Fixture map") => new()
    {
        Id = id,
        Name = name,
        Description = "Catalog check fixture",
        Version = "1.0.0",
        MapHash = mapHash,
        PreviewHash = previewHash
    };

    void WriteCatalog(string root, MapCatalogManifest manifest)
    {
        var json = JsonSerializer.Serialize(manifest,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true });
        File.WriteAllText(Path.Combine(root, "catalog.json"), json, new UTF8Encoding(false));
    }

    // ---- valid catalog: canonical fixture plus a CRLF/BOM copy of the same map ----
    var validRoot = Root("valid");
    var demo = WriteMap(validRoot, "demo", sourceBytes, pngBytes);
    var bomText = canonicalText.Replace("\n", "\r\n");
    var bomBytes = new byte[] { 0xEF, 0xBB, 0xBF }.Concat(Encoding.UTF8.GetBytes(bomText)).ToArray();
    var bom = WriteMap(validRoot, "demo-bom", bomBytes, pngBytes);
    Check(bom.MapHash == demo.MapHash, "CRLF + BOM map normalizes to the same SHA-256 map hash");
    WriteCatalog(validRoot, new MapCatalogManifest
    {
        SchemaVersion = 1,
        Generation = 7,
        DefaultMapId = "demo",
        Maps = new[]
        {
            Entry("demo", demo.MapHash, demo.PreviewHash),
            Entry("demo-bom", bom.MapHash, bom.PreviewHash, "Fixture map BOM")
        }
    });

    var snapshot = MapCatalogSnapshot.Load(validRoot, "world-prototype-1");
    Check(snapshot.Generation == 7 && snapshot.DefaultMapId == "demo", "snapshot exposes generation and default map");
    Check(snapshot.Maps.Count == 2 && snapshot.Maps.ContainsKey("demo-bom"), "snapshot exposes every catalog entry");
    var demoDefinition = snapshot.Find("demo");
    Check(demoDefinition is not null && demoDefinition.Entry.Id == "demo", "Find returns the requested map");
    Check(demoDefinition!.Data.SpawnPoints.Count == 10, "authored map keeps exactly 10 spawn points");
    Check(demoDefinition.Data.ReachableCells().Count > 0, "authored map reachable cells validated");
    Check(demoDefinition.Layout.Spawns.Count == 10 && demoDefinition.Layout.Hash?.Length == 64,
        "authoritative layout built from authored document");
    Check(demoDefinition.Json == canonicalText && !demoDefinition.Json.Contains('\r'),
        "stored JSON is BOM-free with LF line endings");
    Check(demoDefinition.Preview.Length == pngBytes.Length && demoDefinition.Entry.PreviewHash == demo.PreviewHash,
        "preview bytes retained with validated hash");
    Check(snapshot.Find("demo", demo.MapHash) is not null, "Find accepts the matching map hash");
    Check(snapshot.Find("demo", new string('0', 64)) is null, "Find rejects a mismatched map hash");
    Check(snapshot.Find("missing") is null, "Find returns null for an unknown id");
    Check(snapshot.Find("") is null, "Find returns null for an empty id");

    var readOnly = false;
    try { ((IDictionary<string, MapDefinition>)snapshot.Maps).Add("extra", demoDefinition); }
    catch (NotSupportedException) { readOnly = true; }
    Check(readOnly, "snapshot dictionary cannot be mutated");

    var otherSnapshot = MapCatalogSnapshot.Load(validRoot, "world-prototype-1");
    otherSnapshot.Find("demo")!.Entry.Name = "mutated";
    Check(otherSnapshot.Find("demo")!.Entry.Name == "Fixture map", "entry access cannot mutate the published snapshot");
    Check(snapshot.Find("demo")!.Entry.Name == "Fixture map", "each snapshot owns copies of its entries");

    // Production-shaped rooms: host authority, map acknowledgement and two independent map runtimes.
    var bundled = Path.Combine(AppContext.BaseDirectory, "GameData", "Maps");
    var live = Root("live");
    foreach (var file in Directory.EnumerateFiles(bundled, "*", SearchOption.AllDirectories))
    {
        var target = Path.Combine(live, Path.GetRelativePath(bundled, file));
        Directory.CreateDirectory(Path.GetDirectoryName(target)!); File.Copy(file, target);
    }
    var worldVersion = WorldSettings.Load(GameDataLoader.ReadGameDataFile("world_settings.json")).Version;
    var store = new MapCatalogStore(live, worldVersion);
    var bundledCount = JsonDocument.Parse(File.ReadAllText(Path.Combine(bundled, "catalog.json"))).RootElement.GetProperty("maps").GetArrayLength();
    Check(bundledCount >= 2 && store.Current.Maps.Count == bundledCount, "every bundled map passes authoritative validation");
    var profile = MatchProfile.CreateTestProfile();
    Environment.SetEnvironmentVariable("WORLD_ENABLED", "1"); // Explicitly exercise the real world with accelerated match timers.
    var options = new GameServer.Configuration.ServerOptions(System.Net.IPAddress.Loopback, 7777, 10, 30, 5, "RoomCode", "FixtureCode");
    var rooms = new GameServer.Rooms.RoomManager(options, profile: profile, catalog: store);
    var room = rooms.AllRooms.Single();
    void Join(GameServer.Rooms.RoomManager.Room r, string player, string account)
    {
        Check(r.Match.TryRegisterPlayer(player, player, out _), "register " + player);
        r.Match.TryAttachProfile(player, "account:" + account); r.Match.SetPlayerReady(player);
        rooms.RegisterMember(r, player, "account:" + account);
    }
    void Ready(GameServer.Rooms.RoomManager.Room r, string player)
    {
        var state = rooms.GetMapState(r);
        Check(rooms.AcknowledgeMap(r, player, new JungleBackend.Protocol.Gameplay.RoomMapReadyPacket
            { RoomId = r.Id, SelectionRevision = state.SelectionRevision, MapHash = state.MapHash, RulesHash = state.RulesHash }) == JungleBackend.Protocol.Gameplay.RoomMapError.None,
            "map verified for " + player);
    }
    Join(room, "p1", "owner"); Join(room, "p2", "other");
    Check(room.Match.Phase == JungleBackend.Protocol.Gameplay.MatchPhase.Waiting, "enough players do not auto-start a manual room");
    var state0 = rooms.GetMapState(room);
    var start = new JungleBackend.Protocol.Gameplay.StartRoomMatchPacket { RoomId = room.Id, SelectionRevision = state0.SelectionRevision };
    Check(rooms.StartMatch(room, "p2", start) == JungleBackend.Protocol.Gameplay.RoomMapError.NotHost, "non-host cannot start");
    Check(rooms.StartMatch(room, "p1", start) == JungleBackend.Protocol.Gameplay.RoomMapError.MapsNotReady, "host cannot start before map acknowledgements");
    Ready(room, "p1"); Ready(room, "p2");
    Check(rooms.StartMatch(room, "p1", start) == JungleBackend.Protocol.Gameplay.RoomMapError.None, "host starts verified room");
    Join(room, "p3", "third");
    Check(room.Match.Phase == JungleBackend.Protocol.Gameplay.MatchPhase.Waiting, "joining during countdown cancels it");
    room.Match.ProcessLeave("p3", room.Match.MatchId); rooms.Tick(1d/30);
    var firstMap = store.Current.Find(store.Current.DefaultMapId)!;
    var secondMap = store.Current.Maps.Values.First(map => map.Entry.Id != firstMap.Entry.Id);
    var boundaryProfile = new MatchProfile { Version = ZoneSchedule.Load(GameDataLoader.ReadGameDataFile("zone_schedule.json")).Version, MinPlayers = 2, LobbyDurationSeconds = 1d / 30 };
    var boundaryMap = store.Current.Find(store.Current.DefaultMapId)!;
    var boundaryZone = ZoneSchedule.Load(GameDataLoader.ReadGameDataFile("zone_schedule.json"));
    var boundaryMatch = new MatchRuntime("boundary", boundaryMap.Data, boundaryZone, boundaryProfile, selectedMap: boundaryMap);
    for (var i=0;i<3;i++) { boundaryMatch.TryRegisterPlayer("boundary-"+i,"Boundary",out _); boundaryMatch.SetPlayerReady("boundary-"+i); }
    Check(boundaryMatch.StartFromRoom(),"manual countdown fixture has three verified-capability players");
    boundaryMatch.MarkDisconnected("boundary-2");
    Check(boundaryMatch.Phase==JungleBackend.Protocol.Gameplay.MatchPhase.Waiting,"disconnect immediately cancels countdown even when minimum players remain");
    boundaryMatch.Tick(1d/30);
    Check(boundaryMatch.Phase==JungleBackend.Protocol.Gameplay.MatchPhase.Waiting,"disconnect on final countdown tick cannot advance to Loading");

    var legacyMap=new MapData{Version=boundaryProfile.Version,MinX=boundaryMap.Data.MinX,MaxX=boundaryMap.Data.MaxX,MinY=boundaryMap.Data.MinY,MaxY=boundaryMap.Data.MaxY};
    legacyMap.SpawnPoints.AddRange(boundaryMap.Data.SpawnPoints);legacyMap.Obstacles.AddRange(boundaryMap.Data.Obstacles);
    var legacyMatch = new MatchRuntime("legacy-boundary", legacyMap, boundaryZone, boundaryProfile);
    for (var i=0;i<3;i++) { legacyMatch.TryRegisterPlayer("legacy-"+i,"Legacy",out _); legacyMatch.SetPlayerReady("legacy-"+i); }
    legacyMatch.MarkDisconnected("legacy-2");
    Check(legacyMatch.Phase==JungleBackend.Protocol.Gameplay.MatchPhase.Lobby,"legacy fixture retains count-based lobby behavior");

    var oldClientHub = new GameServer.Networking.SessionHub(options,new GameServer.Diagnostics.ServerMetrics());
    try
    {
        var peer = new ProtocolPeer();
        var request = new JungleBackend.Protocol.JoinRequest(JungleBackend.Protocol.NetworkProtocol.Version,JungleBackend.Protocol.JoinAuthMode.RoomCode,options.RoomCode,"Old gameplay");
        Check(oldClientHub.Join(request,peer),"legacy handshake fixture reserves one slot before gameplay negotiation");
        oldClientHub.Connected(peer);
        var hello = new JungleBackend.Protocol.Gameplay.ClientHelloPacket { ProtocolVersion = 1, Ready = true };
        var oldWire = new LiteNetLib.Utils.NetDataWriter(); new JungleBackend.Protocol.Gameplay.PacketRegistry().WriteFrame(oldWire,ref hello);
        oldClientHub.Receive(peer,oldWire.CopyData(),true,true);
        Check(peer.Reason=="GameplayVersionMismatch"&&oldClientHub.ReservedPeers==0,"old gameplay version is rejected explicitly and releases its slot");
    }
    finally { oldClientHub.CloseAll(); }
    var choice = new JungleBackend.Protocol.Gameplay.SelectRoomMapPacket
        { RoomId = room.Id, SelectionRevision = state0.SelectionRevision, MapId = secondMap.Entry.Id, MapHash = secondMap.Entry.MapHash };
    Check(rooms.SelectMap(room, "p2", choice) == JungleBackend.Protocol.Gameplay.RoomMapError.NotHost, "non-host cannot select map");
    Check(rooms.SelectMap(room, "p1", choice) == JungleBackend.Protocol.Gameplay.RoomMapError.None, "host selects second map");
    Check(rooms.GetMapState(room).ReadyCount == 0, "changing map resets all readiness");
    Check(rooms.StartMatch(room, "p1", start) == JungleBackend.Protocol.Gameplay.RoomMapError.SelectionChanged, "stale start packet rejected");
    // Select the first map for the running match; publish will later remove it.
    choice.SelectionRevision = rooms.GetMapState(room).SelectionRevision; choice.MapId = firstMap.Entry.Id; choice.MapHash = firstMap.Entry.MapHash;
    rooms.SelectMap(room, "p1", choice); Ready(room, "p1"); Ready(room, "p2");
    start.SelectionRevision = rooms.GetMapState(room).SelectionRevision; rooms.StartMatch(room, "p1", start);
    for (var i=0;i<18;i++) rooms.Tick(1d/30);
    room.Match.SetPlayerLoaded("p1", room.Match.MatchId); room.Match.SetPlayerLoaded("p2", room.Match.MatchId); rooms.Tick(1d/30);
    Check(room.Match.Phase == JungleBackend.Protocol.Gameplay.MatchPhase.InGame && room.Match.World?.Layout.Hash == firstMap.Layout.Hash,
        "selected authored map supplies actual world, collision and spawns");
    Check(room.Match.Zone?.CurrentRadius == firstMap.Layout.Size / 2f, "online map starts with a safe zone sized to its bounds");
    Check(rooms.SelectMap(room, "p1", choice) == JungleBackend.Protocol.Gameplay.RoomMapError.MatchLocked, "in-game map changes rejected");
    var createdResult = rooms.Create(new JungleBackend.Rooms.CreateRoomCommand { OwnerId = "creator", Name = "Other room", MaxPlayers = 10 });
    var createdId = ((JungleBackend.Rooms.RoomView)createdResult.Body!).Id;
    var otherRoom = rooms.AllRooms.Single(r => r.Id == createdId);
    Join(otherRoom, "visitor", "visitor");
    Check(rooms.GetMapState(otherRoom).HostNetId == 255, "created room waits for creator rather than granting a visitor host");
    Join(otherRoom, "creator-session", "creator");
    Check(rooms.GetMapState(otherRoom).HostNetId == 1, "creator receives initial host authority");
    otherRoom.Match.ProcessLeave("creator-session", otherRoom.Match.MatchId); rooms.Tick(1d/30);
    Check(rooms.GetMapState(otherRoom).HostNetId == 0, "host leave transfers to earliest remaining member");
    Join(otherRoom,"creator-returned","creator");
    Check(rooms.GetMapState(otherRoom).HostNetId==0,"returning owner does not reclaim session host authority");
    var otherState=rooms.GetMapState(otherRoom);
    rooms.SelectMap(otherRoom,"visitor",new JungleBackend.Protocol.Gameplay.SelectRoomMapPacket{RoomId=otherRoom.Id,SelectionRevision=otherState.SelectionRevision,MapId=secondMap.Entry.Id,MapHash=secondMap.Entry.MapHash});
    Ready(otherRoom,"visitor"); Ready(otherRoom,"creator-returned");
    var otherStart=new JungleBackend.Protocol.Gameplay.StartRoomMatchPacket{RoomId=otherRoom.Id,SelectionRevision=rooms.GetMapState(otherRoom).SelectionRevision};
    rooms.StartMatch(otherRoom,"visitor",otherStart);
    otherRoom.Match.ProcessLeave("creator-returned",otherRoom.Match.MatchId);rooms.Tick(1d/30);
    Check(otherRoom.Match.Phase==JungleBackend.Protocol.Gameplay.MatchPhase.Waiting,"leaving during manual countdown cancels start");
    Join(otherRoom,"creator-returned-again","creator"); Ready(otherRoom,"visitor");Ready(otherRoom,"creator-returned-again");
    rooms.StartMatch(otherRoom,"visitor",otherStart);
    for(var i=0;i<18;i++)rooms.Tick(1d/30);
    otherRoom.Match.SetPlayerLoaded("visitor",otherRoom.Match.MatchId);otherRoom.Match.SetPlayerLoaded("creator-returned-again",otherRoom.Match.MatchId);rooms.Tick(1d/30);
    Check(otherRoom.Match.Phase==JungleBackend.Protocol.Gameplay.MatchPhase.InGame&&otherRoom.Match.World?.Layout.Hash==secondMap.Layout.Hash&&room.Match.World?.Layout.Hash==firstMap.Layout.Hash,
        "two concurrent rooms use different authoritative geometry, placements and hashes");
    var waitingResult=rooms.Create(new JungleBackend.Rooms.CreateRoomCommand{OwnerId="waiting-owner",Name="Waiting map fallback",MaxPlayers=10});
    var waitingRoom=rooms.AllRooms.Single(r=>r.Id==((JungleBackend.Rooms.RoomView)waitingResult.Body!).Id);
    var published = JsonSerializer.Deserialize<MapCatalogManifest>(File.ReadAllText(Path.Combine(live, "catalog.json")), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    published.Generation++; published.DefaultMapId = secondMap.Entry.Id; published.Maps = published.Maps.Where(m => m.Id == secondMap.Entry.Id).ToArray();
    WriteCatalog(live, published);
    var commands = new GameServer.Rooms.RoomCommands();
    var reload = store.Reload(commands, CancellationToken.None);
    while (!reload.IsCompleted) { commands.Drain(rooms); Thread.Sleep(1); }
    Check(reload.GetAwaiter().GetResult().Status == 200, "new valid catalog activates without restart");
    Check(room.Match.SelectedMap == firstMap && otherRoom.Match.SelectedMap?.Entry.Id == secondMap.Entry.Id&&waitingRoom.Match.SelectedMap?.Entry.Id==secondMap.Entry.Id, "running matches frozen, waiting room switches to published default");
    Check(store.FindVersion(firstMap.Entry.Id, firstMap.Entry.MapHash) is not null, "removed map version remains available to pinned matches");
    var roundIdentities = new List<(string Player, byte Id, Guid Proof)>();
    room.Match.ResumeTokenRotated += (player,id,proof) => roundIdentities.Add((player,id,proof));
    foreach (var e in room.Match.Entities.All) room.Match.Health.ApplyDamage(e.NetId, 255, 255);
    for (var i=0;i<25;i++) rooms.Tick(1d/30);
    Check(room.Match.Phase == JungleBackend.Protocol.Gameplay.MatchPhase.Waiting && room.Match.SelectedMap?.Entry.Id == secondMap.Entry.Id,
        "after results the room returns to waiting on latest map without auto-start");
    Check(roundIdentities.Count==2&&roundIdentities.All(p=>p.Proof!=Guid.Empty),"new waiting round publishes identity/proof so host can start again before InGame");

    // ---- failure cases: each throws before any snapshot is returned ----
    var publishedRoot = Root("published");
    var publishedGeneration = MapPublisher.Publish(bundled, publishedRoot, "world-prototype-1");
    Check(MapCatalogSnapshot.Load(publishedRoot,"world-prototype-1").Maps.Count==bundledCount,"publish validates and installs complete immutable payloads");
    Check(MapPublisher.Publish(bundled,publishedRoot,"world-prototype-1")>publishedGeneration,"repeat publish advances generation without replacing immutable map bytes");
    var publishedPointer=File.ReadAllBytes(Path.Combine(publishedRoot,"catalog.json"));
    var invalidPackage=Root("invalid-publish");
    File.WriteAllText(Path.Combine(invalidPackage,"catalog.json"),"{}");
    ThrowsArg(()=>MapPublisher.Publish(invalidPackage,publishedRoot,"world-prototype-1"),"invalid publish rejected before activation");
    Check(publishedPointer.SequenceEqual(File.ReadAllBytes(Path.Combine(publishedRoot,"catalog.json"))),"failed publish preserves active catalog byte for byte");
    var statePacket=rooms.GetMapState(room);
    var wire=new LiteNetLib.Utils.NetDataWriter(); statePacket.Serialize(wire);
    var roundtrip=new JungleBackend.Protocol.Gameplay.RoomMapStatePacket(); roundtrip.Deserialize(new LiteNetLib.Utils.NetDataReader(wire.CopyData()));
    Check(roundtrip.RoomId==statePacket.RoomId&&roundtrip.MapHash==statePacket.MapHash&&roundtrip.RulesHash==statePacket.RulesHash,"room map packet roundtrip preserves independent map/rules identities");

    var badHashRoot = Root("badhash");
    WriteMap(badHashRoot, "demo", sourceBytes, pngBytes);
    WriteCatalog(badHashRoot, new MapCatalogManifest
    {
        Generation = 1, DefaultMapId = "demo",
        Maps = new[] { Entry("demo", new string('0', 64), demo.PreviewHash) }
    });
    ThrowsArg(() => MapCatalogSnapshot.Load(badHashRoot, "world-prototype-1"), "map hash mismatch rejected");
    Check(snapshot.Generation == 7 && snapshot.Find("demo")!.Entry.MapHash == demo.MapHash,
        "failed load leaves the previous snapshot unchanged");

    var duplicateRoot = Root("duplicate");
    WriteCatalog(duplicateRoot, new MapCatalogManifest
    {
        Generation = 2, DefaultMapId = "demo",
        Maps = new[]
        {
            Entry("demo", demo.MapHash, demo.PreviewHash),
            Entry("demo", demo.MapHash, demo.PreviewHash, "Duplicate")
        }
    });
    ThrowsArg(() => MapCatalogSnapshot.Load(duplicateRoot, "world-prototype-1"), "duplicate map id rejected");

    var missingDefaultRoot = Root("missingdefault");
    WriteCatalog(missingDefaultRoot, new MapCatalogManifest
    {
        Generation = 3, DefaultMapId = "ghost",
        Maps = new[] { Entry("demo", demo.MapHash, demo.PreviewHash) }
    });
    ThrowsArg(() => MapCatalogSnapshot.Load(missingDefaultRoot, "world-prototype-1"), "unknown default map rejected");

    var badSchemaRoot = Root("badschema");
    WriteCatalog(badSchemaRoot, new MapCatalogManifest
    {
        SchemaVersion = 2, Generation = 1, DefaultMapId = "demo",
        Maps = new[] { Entry("demo", demo.MapHash, demo.PreviewHash) }
    });
    ThrowsArg(() => MapCatalogSnapshot.Load(badSchemaRoot, "world-prototype-1"), "unsupported schema version rejected");

    var zeroGenerationRoot = Root("zerogeneration");
    WriteCatalog(zeroGenerationRoot, new MapCatalogManifest
    {
        Generation = 0, DefaultMapId = "demo",
        Maps = new[] { Entry("demo", demo.MapHash, demo.PreviewHash) }
    });
    ThrowsArg(() => MapCatalogSnapshot.Load(zeroGenerationRoot, "world-prototype-1"), "zero generation rejected");

    var geometryText = JsonNode.Parse(canonicalText)!.DeepClone();
    var geometrySpawns = geometryText["spawnPoints"]!.AsArray();
    geometrySpawns[0]!["x"] = 1.0;
    geometrySpawns[0]!["y"] = 1.0;
    var geometryRoot = Root("geometry");
    var geometry = WriteMap(geometryRoot, "demo", Encoding.UTF8.GetBytes(geometryText.ToJsonString()), pngBytes);
    WriteCatalog(geometryRoot, new MapCatalogManifest
    {
        Generation = 4, DefaultMapId = "demo",
        Maps = new[] { Entry("demo", geometry.MapHash, geometry.PreviewHash) }
    });
    ThrowsArg(() => MapCatalogSnapshot.Load(geometryRoot, "world-prototype-1"), "invalid map geometry rejected");

    var oversizedRoot = Root("oversized");
    var oversizedPreview = pngBytes.Concat(new byte[512 * 1024 + 1 - pngBytes.Length]).ToArray();
    var oversized = WriteMap(oversizedRoot, "demo", sourceBytes, oversizedPreview);
    WriteCatalog(oversizedRoot, new MapCatalogManifest
    {
        Generation = 5, DefaultMapId = "demo",
        Maps = new[] { Entry("demo", oversized.MapHash, oversized.PreviewHash) }
    });
    ThrowsArg(() => MapCatalogSnapshot.Load(oversizedRoot, "world-prototype-1"), "preview larger than 512KiB rejected");

    var hugeMapRoot = Root("hugeMap");
    var paddedMap = canonicalText + new string(' ', 2 * 1024 * 1024 + 1 - canonicalText.Length);
    var paddedBytes = Encoding.UTF8.GetBytes(paddedMap);
    Check(paddedBytes.Length > 2 * 1024 * 1024, "oversized map fixture exceeds the 2MiB bound");
    var hugeMap = WriteMap(hugeMapRoot, "demo", paddedBytes, pngBytes);
    WriteCatalog(hugeMapRoot, new MapCatalogManifest
    {
        Generation = 8, DefaultMapId = "demo",
        Maps = new[] { Entry("demo", hugeMap.MapHash, hugeMap.PreviewHash) }
    });
    ThrowsArg(() => MapCatalogSnapshot.Load(hugeMapRoot, "world-prototype-1"), "map.json larger than 2MiB rejected");

    var traversalRoot = Root("traversal");
    WriteCatalog(traversalRoot, new MapCatalogManifest
    {
        Generation = 6, DefaultMapId = "../evil",
        Maps = new[] { Entry("../evil", demo.MapHash, demo.PreviewHash) }
    });
    ThrowsArg(() => MapCatalogSnapshot.Load(traversalRoot, "world-prototype-1"), "path traversal map id rejected");

    var invalidPngRoot = Root("invalidpng");
    var notPng = Encoding.ASCII.GetBytes("this is not a png image");
    var invalidPng = WriteMap(invalidPngRoot, "demo", sourceBytes, notPng);
    WriteCatalog(invalidPngRoot, new MapCatalogManifest
    {
        Generation = 7, DefaultMapId = "demo",
        Maps = new[] { Entry("demo", invalidPng.MapHash, invalidPng.PreviewHash) }
    });
    ThrowsArg(() => MapCatalogSnapshot.Load(invalidPngRoot, "world-prototype-1"), "non-PNG preview rejected");

    ThrowsArg(() => MapCatalogSnapshot.Load(validRoot, " "), "missing world version rejected");
    ThrowsArg(() => MapCatalogSnapshot.Load(Path.Combine(temp, "absent"), "world-prototype-1"), "missing catalog directory rejected");
}
finally
{
    Environment.SetEnvironmentVariable("WORLD_ENABLED", previousWorldMode);
    try { Directory.Delete(temp, true); } catch { }
}

Console.WriteLine($"MapCatalogChecks: {checks} passed, 0 failed");

sealed class ProtocolPeer : GameServer.Networking.IGamePeer
{
    public string Transport => "Udp";
    public bool IsOpen { get; private set; } = true;
    public string? Reason { get; private set; }
    public bool TrySend(byte[] message,bool reliable) => IsOpen;
    public void Close(string reason) { IsOpen=false; Reason=reason; }
}
