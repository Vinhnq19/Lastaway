using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JungleMaze.App.Config;
using JungleMaze.App.Lobby;
using JungleMaze.App.Ports;
using JungleMaze.Domain.Lobby;
using JungleMaze.Domain.Network;
using JungleMaze.Infrastructure.Api;
using JungleMaze.Infrastructure.Auth;
using JungleMaze.Infrastructure.Config;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace JungleMaze.Tests.Lobby
{
    public sealed class ConnectionFlowRegressionTests
    {
        [TestCase(DisconnectReason.AccountExpired, true)]
        [TestCase(DisconnectReason.AccessDenied, false)]
        [TestCase(DisconnectReason.InvalidRoomCode, false)]
        [TestCase(DisconnectReason.RequestLimited, false)]
        [TestCase(DisconnectReason.ServiceUnavailable, false)]
        [TestCase(DisconnectReason.TicketRejected, false)]
        [TestCase(DisconnectReason.InvalidConfig, false)]
        public void JoinFailure_OnlyAccountExpiryClearsLogin(DisconnectReason reason, bool expired)
        {
            using var rig = new LobbyRig(); var token = rig.Account.Token;
            rig.Flow.OpenJoin(); rig.Join.SetCode("ROOM01"); rig.Flow.SubmitJoin();
            rig.Connection.Set(NetworkState.Failed, reason); rig.Tick();
            Assert.AreEqual(expired ? LobbyScreen.Login : LobbyScreen.JoinForm, rig.Flow.Screen);
            Assert.AreEqual(expired ? null : token, rig.Account.Token);
            Assert.AreEqual(1, rig.Commands.Joins, "No automatic retry");
            if (!expired) { Assert.IsNotEmpty(rig.Join.Message); Assert.IsFalse(rig.Join.Busy); }
        }

        [TestCase("list", 401)] [TestCase("create", 401)] [TestCase("delete", 401)]
        [TestCase("list", 200)] [TestCase("create", 201)] [TestCase("delete", 204)]
        public void OldRoomResponse_CannotTouchNewAccountOrBusyOperation(string operation, int status)
        {
            using var rig = new LobbyRig(); var api = new DelayedApi();
            using var rooms = new RoomListModel(api, rig.Account);
            var room = new RoomInfo(Guid.NewGuid(), "old", "ABC123", false, 10, 0, 0);
            if (operation == "list") rooms.Refresh();
            else if (operation == "create") { rooms.NewName = "old"; rooms.Create(); }
            else { rooms.Refresh(); api.ListDone(ApiResult<RoomInfo[]>.Success(new[]{room})); rooms.RequestDelete(room.Id); rooms.ConfirmDelete(); }
            var oldList = api.ListDone; var oldCreate = api.CreateDone; var oldDelete = api.DeleteDone;
            rig.Account.Logout(); rig.LogIn("accountB"); var current = rig.Account.Token;
            rooms.NewName = "new"; rooms.Create(); Assert.IsTrue(rooms.Creating);
            if (operation == "list") oldList(status == 200 ? ApiResult<RoomInfo[]>.Success(new[]{room}) : ApiResult<RoomInfo[]>.Fail(status));
            if (operation == "create") oldCreate(status == 201 ? ApiResult<RoomInfo>.Success(room) : ApiResult<RoomInfo>.Fail(status));
            if (operation == "delete") oldDelete(status == 204 ? ApiResult<bool>.Success(true) : ApiResult<bool>.Fail(status));
            Assert.AreEqual(current, rig.Account.Token); Assert.IsTrue(rooms.Creating);
            Assert.AreEqual(0, rooms.Rooms.Count); Assert.IsEmpty(rooms.Message);
            api.CreateDone(ApiResult<RoomInfo>.Success(new RoomInfo(Guid.NewGuid(), "new", "NEW123", false, 10, 0, 0)));
            Assert.IsFalse(rooms.Creating); Assert.AreEqual("new", rooms.Rooms[0].Name);
        }

        [Test]
        public void Clear_InvalidatesRoomCallbackWithoutReissuingMutation()
        {
            using var rig = new LobbyRig(); var api = new DelayedApi();
            using var rooms = new RoomListModel(api, rig.Account);
            rooms.NewName = "new"; rooms.Create(); rooms.Clear();
            api.CreateDone(ApiResult<RoomInfo>.Fail(401));
            Assert.IsTrue(rig.Account.IsValid); Assert.IsFalse(rooms.Creating);
            Assert.AreEqual(ListState.Idle, rooms.State); Assert.AreEqual(0, rooms.Rooms.Count);
        }

        [Test]
        public void Leaderboard_RejectsOldMatchBackendAndClosedScreen()
        {
            var api = new DelayedApi(); var backend = "A";
            var board = new LeaderboardModel(api, () => backend);
            var first = Guid.NewGuid(); var second = Guid.NewGuid();
            board.Load(first); var old = api.BoardDone;
            board.Load(second); var current = api.BoardDone;
            old(ApiResult<LeaderboardEntry[]>.Fail(503));
            Assert.AreEqual(second, board.MatchId); Assert.AreEqual(ListState.Loading, board.State);
            current(ApiResult<LeaderboardEntry[]>.Success(Array.Empty<LeaderboardEntry>()));
            Assert.AreEqual(ListState.Empty, board.State);
            board.Load(first); old = api.BoardDone; backend = "B";
            old(ApiResult<LeaderboardEntry[]>.Fail(503)); Assert.AreEqual(ListState.Loading, board.State);
            board.Clear(); old(ApiResult<LeaderboardEntry[]>.Fail(404));
            Assert.AreEqual(Guid.Empty, board.MatchId); Assert.AreEqual(ListState.Idle, board.State); Assert.IsEmpty(board.Message);
        }

        [TestCase("GameplayVersionMismatch", DisconnectReason.GameplayVersionMismatch)]
        [TestCase("InvalidResumeToken", DisconnectReason.InvalidResume)]
        [TestCase("ServerBusy", DisconnectReason.ServerBusy)]
        [TestCase("InvalidTicket", DisconnectReason.TicketRejected)]
        [TestCase("TicketExpired", DisconnectReason.TicketRejected)]
        public void ServerReasons_HaveSafeSpecificMessages(string wire, DisconnectReason expected)
        {
            Assert.AreEqual(expected, DisconnectReasons.FromServer(wire));
            var message = JoinRejectText.For(expected); Assert.IsNotEmpty(message);
            StringAssert.DoesNotContain(wire, message); StringAssert.DoesNotContain("ticket", message.ToLowerInvariant());
            StringAssert.DoesNotContain("FutureWireReason", JoinRejectText.For(DisconnectReasons.FromServer("FutureWireReason")));
        }

        [Test]
        public void ServerClosureDuringLoading_IsNotAnElimination()
        {
            using var rig = new LobbyRig(); rig.JoinLobby(); rig.Phase(JungleBackend.Protocol.Gameplay.MatchPhase.Loading,30);
            rig.Connection.Set(NetworkState.Disconnected,DisconnectReason.ServerClosed); rig.Tick();
            Assert.AreEqual(LobbyScreen.LoadError,rig.Flow.Screen);
            Assert.AreEqual(LobbyScreenRules.LoadLost,rig.Flow.LoadErrorText); Assert.IsTrue(rig.Account.IsValid);
        }

        [Test]
        public void InvalidResume_IsNotAnElimination_AndDoesNotExpireAccount()
        {
            using var rig = new LobbyRig(); rig.InGame(); rig.Connection.CanResume = true;
            rig.Connection.Set(NetworkState.Disconnected, DisconnectReason.ConnectionLost); rig.Tick();
            rig.Connection.Set(NetworkState.Failed, DisconnectReason.InvalidResume); rig.Tick();
            Assert.AreEqual(LobbyScreen.Disconnected, rig.Flow.Screen);
            Assert.AreEqual(DisconnectReasons.Describe(DisconnectReason.InvalidResume), rig.Flow.Notice);
            Assert.IsTrue(rig.Account.IsValid); StringAssert.DoesNotContain("bị loại", rig.Flow.Notice);
            rig.Flow.GoHome(); Assert.AreEqual(LobbyScreen.MainMenu, rig.Flow.Screen);
        }

        [UnityTest]
        public IEnumerator TicketHttpStatusesAndMalformedBody_KeepFailureCategory()
        {
            foreach (var status in new[]{401,403,404,429,503,200})
            {
                using var reply = new LoopbackReply(status, status == 200 ? "{bad" : "{}");
                using var client = new GuestHistoryClient(null);
                var task = client.AccountTicketAsync(reply.Url, Guid.NewGuid(), "ROOM01", "fixture-not-secret");
                yield return Complete(task);
                Assert.IsFalse(task.Result.Success); Assert.AreEqual(status == 200 ? 502 : status, task.Result.HttpStatus);
                var expected = status == 401 ? DisconnectReason.AccountExpired : status == 403 ? DisconnectReason.AccessDenied
                    : status == 404 ? DisconnectReason.InvalidRoomCode : status == 429 ? DisconnectReason.RequestLimited : DisconnectReason.ServiceUnavailable;
                Assert.AreEqual(expected, task.Result.Failure);
            }
        }

        [UnityTest]
        public IEnumerator MapHttpErrors_ExpireOnlyCurrent401_AndKeepSpecificMessages()
        {
            foreach (var status in new[]{401,403,404,503})
            {
                using var reply = new LoopbackReply(status, "{}"); long revision = 1; var expired = 0;
                var cfg = new ClientConfig(ConnectionMode.Local, reply.Url, "127.0.0.1", 7777, JungleBackend.Protocol.JoinAuthMode.RoomCode, 4, 5000);
                var maps = new RoomMapFiles(() => cfg, () => "fixture-token", new ClientRuntimeData(Application.streamingAssetsPath),
                    System.IO.Path.GetTempPath(), Application.streamingAssetsPath, () => revision, _ => expired++);
                var task = maps.Catalog(CancellationToken.None); yield return Complete(task);
                Assert.IsTrue(task.IsFaulted); Assert.AreEqual(status == 401 ? 1 : 0, expired);
                var message = task.Exception.GetBaseException().Message;
                StringAssert.DoesNotContain("không khớp", message);
                if (status == 404) StringAssert.Contains("chưa có", message);
                if (status == 403) StringAssert.Contains("chưa được phép", message);
            }
        }

        [UnityTest]
        public IEnumerator StaleMap401_CannotExpireNewSession()
        {
            using var reply = new LoopbackReply(401, "{}", hold:true); long revision = 1; var expired = 0;
            var cfg = new ClientConfig(ConnectionMode.Local, reply.Url, "127.0.0.1", 7777, JungleBackend.Protocol.JoinAuthMode.RoomCode, 4, 5000);
            var maps = new RoomMapFiles(() => cfg, () => "fixture-token", new ClientRuntimeData(Application.streamingAssetsPath),
                System.IO.Path.GetTempPath(), Application.streamingAssetsPath, () => revision, _ => expired++);
            var task = maps.Catalog(CancellationToken.None); revision++; reply.Release(); yield return Complete(task);
            Assert.IsTrue(task.IsFaulted); Assert.AreEqual(0, expired);
        }

        private static IEnumerator Complete(Task task)
        { var deadline=Time.realtimeSinceStartup+15; while(!task.IsCompleted && Time.realtimeSinceStartup<deadline) yield return null; Assert.IsTrue(task.IsCompleted,"Local HTTP fixture did not finish"); }

        private sealed class DelayedApi : IRoomsApi, ILeaderboardApi
        {
            public Action<ApiResult<RoomInfo[]>> ListDone;
            public Action<ApiResult<RoomInfo>> CreateDone;
            public Action<ApiResult<bool>> DeleteDone;
            public Action<ApiResult<LeaderboardEntry[]>> BoardDone;
            public void List(string token, Action<ApiResult<RoomInfo[]>> done) => ListDone=done;
            public void Create(string token,string name,int capacity,Action<ApiResult<RoomInfo>> done) => CreateDone=done;
            public void Delete(string token,Guid id,Action<ApiResult<bool>> done) => DeleteDone=done;
            public void Get(Guid match,Action<ApiResult<LeaderboardEntry[]>> done) => BoardDone=done;
        }

        private sealed class LoopbackReply : IDisposable
        {
            private readonly HttpListener _listener = new HttpListener();
            private readonly TaskCompletionSource<bool> _release = new TaskCompletionSource<bool>();
            public string Url {get;}
            public LoopbackReply(int status,string body,bool hold=false)
            {
                var probe=new TcpListener(IPAddress.Loopback,0); probe.Start(); var port=((IPEndPoint)probe.LocalEndpoint).Port; probe.Stop();
                Url="http://127.0.0.1:"+port; _listener.Prefixes.Add(Url+"/"); _listener.Start();
                if(!hold) _release.SetResult(true);
                _ = Task.Run(async()=>
                {
                    try { var context=await _listener.GetContextAsync(); await _release.Task;
                        var bytes=Encoding.UTF8.GetBytes(body); context.Response.StatusCode=status; context.Response.ContentLength64=bytes.Length;
                        context.Response.ContentType="application/json"; await context.Response.OutputStream.WriteAsync(bytes,0,bytes.Length); context.Response.Close(); }
                    catch(HttpListenerException){} catch(ObjectDisposedException){}
                });
            }
            public void Release()=>_release.TrySetResult(true);
            public void Dispose(){ _release.TrySetResult(true); _listener.Close(); }
        }
    }
}
