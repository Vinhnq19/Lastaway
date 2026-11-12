using System;
using System.Threading.Tasks;
using JungleBackend.Hosting;
using JungleMaze.App.Config;
namespace JungleMaze.App.Ports
{
    [Serializable] public sealed class LanInterface { public string name,ip; public int index; public bool isPrivate; }
    public interface ILanHostTools
    {
        Task<LanInterface[]> GetLanInterfacesAsync();
        Task<VpnToolResult> RunLanAsync(string action,string ip);
        bool IsLanTrusted(LanConnectionCode code);
        Task<VpnToolResult> CheckLanEndpointAsync(LanConnectionCode code,bool confirm);
        Task<VpnToolResult> CheckServerAsync(ClientConfig config);
    }
}
