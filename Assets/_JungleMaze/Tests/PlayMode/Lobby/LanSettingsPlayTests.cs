#if UNITY_EDITOR
using System.Collections;
using System.Threading.Tasks;
using JungleBackend.Hosting;
using JungleMaze.App.Config;
using JungleMaze.App.Ports;
using NUnit.Framework;
using UnityEngine.TestTools;
namespace JungleMaze.Tests.PlayMode.Lobby
{
    public sealed class LanSettingsPlayTests
    {
        private sealed class Tools : IVpnHostTools, ILanHostTools
        {
            public string BackendPath=>"test/Backend";
            public readonly TaskCompletionSource<VpnToolResult> Pending=new TaskCompletionSource<VpnToolResult>();
            public string LastIp;public int Stops;public bool NoInterfaces;
            public Task<bool> ChooseBackendAsync()=>Task.FromResult(true);
            public void OpenClientSetup(){}
            public Task<VpnToolResult> RunAsync(string action)=>Pending.Task;
            public Task<VpnToolResult> CheckEndpointAsync(VpnConnectionCode code)=>Pending.Task;
            public Task<LanInterface[]> GetLanInterfacesAsync()=>Task.FromResult(NoInterfaces?System.Array.Empty<LanInterface>():new[]{new LanInterface{name="Wi-Fi",ip="192.168.1.20",isPrivate=true},new LanInterface{name="Ethernet",ip="10.1.1.20",isPrivate=true}});
            public Task<VpnToolResult> RunLanAsync(string action,string ip){LastIp=ip;if(action=="Stop")Stops++;return Pending.Task;}
            public bool IsLanTrusted(LanConnectionCode code)=>false;
            public Task<VpnToolResult> CheckLanEndpointAsync(LanConnectionCode code,bool confirm)=>Pending.Task;
            public Task<VpnToolResult> CheckServerAsync(ClientConfig config)=>Pending.Task;
        }
        [UnityTest]public IEnumerator PickerHasThreeChoicesAndOpeningLanKeepsAccount()
        {
            using var rig=new LobbyUiRig(vpn:new Tools());yield return null;rig.Flow.OpenServerSettings();yield return null;
            var v=rig.Root.ServerSettings;Assert.AreEqual(3,v.Profile.options.Count);Assert.AreEqual("Online · AWS",v.Profile.options[0].text);
            Assert.AreEqual(0,v.Profile.value);v.Profile.value=1;yield return null;
            Assert.True(v.VpnCode.gameObject.activeInHierarchy);Assert.NotNull(v.TrustConfirm);Assert.NotNull(v.LanInterface);
            Assert.AreEqual(2,v.LanInterface.options.Count);Assert.True(rig.Flow.Account.IsValid,"preview does not logout");
        }
        [UnityTest]public IEnumerator DevPickerRemainsAboveFooterBeforeSelectingLocal()
        {
            using var rig=new LobbyUiRig(vpn:new Tools());yield return null;
            rig.Flow.OpenServerSettings();yield return null;
            var v=rig.Root.ServerSettings;
            Assert.True(v.DevProfile.gameObject.activeInHierarchy);
            Assert.AreEqual("Local · Docker",v.DevProfile.options[1].text);
            var corners=new UnityEngine.Vector3[4];
            ((UnityEngine.RectTransform)v.DevProfile.transform).GetWorldCorners(corners);
            var pickerBottom=corners[0].y;
            ((UnityEngine.RectTransform)v.Close.transform).GetWorldCorners(corners);
            Assert.Greater(pickerBottom,corners[1].y,"dev picker must not sit behind the Close footer");
        }
        [UnityTest]public IEnumerator StoppingHostStillWorksAfterNetworkAdapterDisappears()
        {
            var tools=new Tools{NoInterfaces=true};using var rig=new LobbyUiRig(vpn:tools);yield return null;
            rig.Flow.OpenServerSettings();yield return null;var v=rig.Root.ServerSettings;v.Profile.value=1;yield return null;
            rig.Click(v.VpnStop);Assert.AreEqual(1,tools.Stops);Assert.IsNull(tools.LastIp);
            tools.Pending.SetResult(new VpnToolResult{success=true,state="Stopped"});yield return null;
        }
        [UnityTest]public IEnumerator OldHostCompletionCannotChangeNewMode()
        {
            var tools=new Tools();using var rig=new LobbyUiRig(vpn:tools);yield return null;rig.Flow.OpenServerSettings();yield return null;
            var v=rig.Root.ServerSettings;v.Profile.value=1;yield return null;v.LanInterface.value=1;rig.Click(v.VpnStart);
            Assert.AreEqual("10.1.1.20",tools.LastIp);v.Profile.value=2;
            tools.Pending.SetResult(new VpnToolResult{success=true,ready=true,connectionCode="old-lan"});yield return null;
            Assert.AreEqual("",v.VpnCode.text);Assert.AreEqual(0,tools.Stops);
        }
    }
}
#endif
