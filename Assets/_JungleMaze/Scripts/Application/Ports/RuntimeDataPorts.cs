using System.Collections.Generic;
using JungleMaze.App.Match;

namespace JungleMaze.App.Ports
{
    public struct RuntimeCover
    {
        public string Name;
        public float MinX, MinY, MaxX, MaxY;
    }
    public struct RuntimeMapBounds { public float MinX, MinY, MaxX, MaxY; }

    /// <summary>File dữ liệu trận trong StreamingAssets/JungleMaze/runtime.</summary>
    public static class RuntimeDataFiles
    {
        /// <summary>Map chơi offline (Luyện tập), chỉ client dùng; thiếu thì offline dùng map_demo.json như server.</summary>
        public const string OfflineMap = "offline_map.json";
        public const string ServerMap = "map_demo.json";
    }
    public interface IClientRuntimeData
    {
        IReadOnlyList<RuntimeCover> Covers { get; }
        RuntimeMapBounds Bounds { get; }
        /// <summary>Tốc độ/bán kính server dùng cho movement; chỉ hợp lệ sau khi Verify trả null.</summary>
        float PlayerSpeed { get; }
        float PlayerRadius { get; }
        ZoneSchedule ZoneSchedule { get; }
        string LoadLocal();
        string Verify(string expectedVersion, string expectedHash);
    }
}
