using System;
using System.Threading;
using System.Threading.Tasks;
using JungleBackend.Protocol.Gameplay;
using JungleMaze.App.Ports;
using JungleMaze.Core.DI;
using JungleMaze.Core.Ticking;

namespace JungleMaze.App.Match
{
    public sealed class PublishedMap
    {
        public string Id, Name, Description, Version, MapHash, PreviewHash;
    }
    public sealed class PublishedMaps
    {
        public long Generation;
        public string DefaultMapId, RulesHash;
        public PublishedMap[] Maps = Array.Empty<PublishedMap>();
    }
    public interface IRoomMapFiles
    {
        Task<PublishedMaps> Catalog(CancellationToken token);
        Task<string> Map(string id, string hash, string rulesHash, CancellationToken token);
        Task<byte[]> Preview(string hash, CancellationToken token);
        string Activate(string json, string mapHash, string rulesHash);
        void Clear();
    }

    /// <summary>Readiness belongs to one room selection, never to a completed HTTP request from an older room.</summary>
    public sealed class RoomMapSession : IInitializable, ITickable, IDisposable
    {
        private readonly MatchSession _match;
        private readonly IPacketSender _sender;
        private readonly IPacketReceiver _receiver;
        private readonly IRoomMapFiles _files;
        private CancellationTokenSource _download;
        private int _generation;
        private bool _wasConnected;
        private uint _botRequest;
        private float _botWait;
        public bool BotCommandPending { get; private set; }
        public bool CanManageBots => HasState && IsHost && _match.Phase == MatchPhase.Waiting && !BotCommandPending;
        public RoomMapStatePacket State { get; private set; }
        public PublishedMaps Catalog { get; private set; }
        public bool HasState { get; private set; }
        public bool Busy { get; private set; }
        public bool Verified { get; private set; }
        public string VerifiedMapJson { get; private set; }
        public string Error { get; private set; }
        public string Notice { get; private set; }
        public bool IsHost => HasState && _match.OwnNetId != 255 && _match.OwnNetId == State.HostNetId;
        public bool CanStart => IsHost && State.CanStart && Verified && !Busy;
        public event Action Changed;

        public RoomMapSession(MatchSession match, IPacketSender sender, IPacketReceiver receiver, IRoomMapFiles files)
        { _match = match; _sender = sender; _receiver = receiver; _files = files; }
        public void Initialize()
        { _receiver.Subscribe<RoomBotCommandResultPacket>(OnBotResult); _receiver.Subscribe<RoomMapStatePacket>(OnState); _receiver.Subscribe<RoomMapCommandResultPacket>(OnResult); }
        public void Dispose()
        { _receiver.Unsubscribe<RoomBotCommandResultPacket>(OnBotResult); _receiver.Unsubscribe<RoomMapStatePacket>(OnState); _receiver.Unsubscribe<RoomMapCommandResultPacket>(OnResult); Reset(); }
        public void Tick(float dt)
        {
            if (BotCommandPending && (_botWait -= dt) <= 0) { BotCommandPending = false; Notice = "Chưa nhận phản hồi bot; kiểm tra phòng rồi thử lại."; Changed?.Invoke(); }
            if (_wasConnected && !_match.Connected) Reset();
            _wasConnected = _match.Connected;
        }
        private void Reset()
        {
            _generation++; _download?.Cancel(); _download?.Dispose(); _download = null;
            BotCommandPending = false;
            HasState = Busy = Verified = false; VerifiedMapJson = null; Catalog = null; Error = Notice = null; _files.Clear(); Changed?.Invoke();
        }
        private void OnState(RoomMapStatePacket packet)
        {
            if (!_match.Connected) return;
            if (HasState && packet.RoomId == State.RoomId && unchecked((int)(packet.SelectionRevision - State.SelectionRevision)) < 0) return;
            var selectionChanged = !HasState || packet.RoomId != State.RoomId || packet.SelectionRevision != State.SelectionRevision || packet.MapHash != State.MapHash;
            var catalogChanged = !HasState || packet.CatalogGeneration != State.CatalogGeneration;
            State = packet; HasState = true;
            if (selectionChanged || catalogChanged) BeginDownload(selectionChanged);
            Changed?.Invoke();
        }
        private void OnResult(RoomMapCommandResultPacket packet)
        {
            if (!HasState || packet.RoomId != State.RoomId) return;
            Notice = packet.Error == RoomMapError.None ? null
                : packet.Error == RoomMapError.NotHost ? "Chỉ trưởng phòng được chọn bản đồ và bắt đầu."
                : packet.Error == RoomMapError.MatchLocked ? "Trận đã bắt đầu; hãy chờ trở lại phòng chờ."
                : packet.Error == RoomMapError.UnknownMap ? "Bản đồ này hiện chưa có trên máy chủ. Hãy tải lại danh sách."
                : packet.Error == RoomMapError.NotEnoughPlayers ? "Chưa đủ người để bắt đầu."
                : packet.Error == RoomMapError.MapsNotReady ? "Hãy chờ tất cả người chơi tải xong bản đồ."
                : "Lựa chọn trong phòng đã đổi. Kiểm tra lại rồi thử lại.";
            Changed?.Invoke();
        }
        public void ManageBots(RoomBotAction action, uint botId = 0)
        {
            if (!CanManageBots) return;
            var packet = new RoomBotCommandPacket { RoomId = State.RoomId, Revision = State.LobbyRevision, RequestId = ++_botRequest, Action = action, BotId = botId };
            if (_sender.Send(ref packet, PacketDelivery.ReliableOrdered)) { BotCommandPending = true; _botWait = 5; }
            else Notice = "Không gửi được lệnh bot.";
            Changed?.Invoke();
        }
        private void OnBotResult(RoomBotCommandResultPacket packet)
        {
            if (!HasState || packet.RoomId != State.RoomId || packet.RequestId != _botRequest) return;
            BotCommandPending = false;
            Notice = packet.Error == RoomBotError.None ? null
                : packet.Error == RoomBotError.NotHost ? "Chỉ trưởng phòng được thêm hoặc xóa bot."
                : packet.Error == RoomBotError.MatchLocked ? "Chỉ có thể sửa bot khi đang ở phòng chờ."
                : packet.Error == RoomBotError.RoomFull ? "Phòng đã đủ người."
                : packet.Error == RoomBotError.ServerFull ? "Máy chủ đã đầy. Vui lòng thử lại sau."
                : "Phòng đã thay đổi. Kiểm tra danh sách bot rồi thử lại.";
            Changed?.Invoke();
        }
        public void Select(PublishedMap map)
        {
            if (!IsHost || _match.Phase != MatchPhase.Waiting || map == null) return;
            var packet = new SelectRoomMapPacket { RoomId = State.RoomId, SelectionRevision = State.SelectionRevision, MapId = map.Id, MapHash = map.MapHash };
            _sender.Send(ref packet, PacketDelivery.ReliableOrdered);
        }
        public void Start()
        {
            if (!CanStart) return;
            var packet = new StartRoomMatchPacket { RoomId = State.RoomId, SelectionRevision = State.SelectionRevision };
            _sender.Send(ref packet, PacketDelivery.ReliableOrdered);
        }
        public void Retry() { if (HasState && !Busy) BeginDownload(true); }
        public Task<byte[]> Preview(PublishedMap map, CancellationToken token) => _files.Preview(map.PreviewHash, token);
        private async void BeginDownload(bool resetReadiness)
        {
            _download?.Cancel(); _download?.Dispose(); _download = new CancellationTokenSource();
            var token = _download.Token; var generation = ++_generation; var selected = State;
            Busy = true; Error = null; if (resetReadiness) { Verified = false; VerifiedMapJson = null; }
            try
            {
                var catalog = await _files.Catalog(token);
                if (generation != _generation || token.IsCancellationRequested) return;
                if (catalog.Generation < selected.CatalogGeneration) throw new InvalidOperationException("Danh sách bản đồ đang cập nhật. Hãy thử tải lại.");
                Catalog = catalog;
                if (!Verified)
                {
                    var json = await _files.Map(selected.MapId, selected.MapHash, selected.RulesHash, token);
                    if (generation != _generation || token.IsCancellationRequested) return;
                    var error = _files.Activate(json, selected.MapHash, selected.RulesHash);
                    if (error != null) throw new InvalidOperationException("Dữ liệu bản đồ không hợp lệ. Thử tải lại; nếu vẫn lỗi, cập nhật game.");
                    VerifiedMapJson = json;
                    Verified = true;
                    var ready = new RoomMapReadyPacket { RoomId = selected.RoomId, SelectionRevision = selected.SelectionRevision, MapHash = selected.MapHash, RulesHash = selected.RulesHash };
                    if (!_sender.Send(ref ready, PacketDelivery.ReliableOrdered)) throw new InvalidOperationException("Không gửi được xác nhận map; thử lại.");
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception e) when (!(e is OutOfMemoryException))
            { if (generation == _generation) { Verified = false; VerifiedMapJson = null; Error = e is InvalidOperationException ? e.Message : "Không tải được map; kiểm tra kết nối rồi thử lại."; } }
            finally { if (generation == _generation) { Busy = false; Changed?.Invoke(); } }
        }
    }
}
