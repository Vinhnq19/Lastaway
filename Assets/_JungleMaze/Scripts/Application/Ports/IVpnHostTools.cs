using System;
using System.Threading.Tasks;
using JungleMaze.App.Config;

namespace JungleMaze.App.Ports
{
    [Serializable]
    public sealed class VpnToolResult
    {
        public bool success;
        public bool ready;
        public string state;
        public string message;
        public string connectionCode;
    }

    /// <summary>Local Windows tools only; never owns AWS or the lifetime of the game process.</summary>
    public interface IVpnHostTools
    {
        string BackendPath { get; }
        Task<bool> ChooseBackendAsync();
        Task<VpnToolResult> RunAsync(string action);
        Task<VpnToolResult> CheckEndpointAsync(VpnConnectionCode endpoint);
        void OpenClientSetup();
    }
}
