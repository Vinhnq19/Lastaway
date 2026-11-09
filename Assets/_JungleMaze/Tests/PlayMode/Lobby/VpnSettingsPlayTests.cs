#if UNITY_EDITOR
using System.Collections;
using System.Threading.Tasks;
using JungleMaze.App.Ports;
using JungleMaze.App.Config;
using JungleMaze.Presentation.UI.Lobby;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace JungleMaze.Tests.PlayMode.Lobby
{
    public sealed class VpnSettingsPlayTests
    {
        private sealed class Tools : IVpnHostTools
        {
            public string BackendPath => "D:/trusted/Backend";
            public readonly TaskCompletionSource<VpnToolResult> Pending = new TaskCompletionSource<VpnToolResult>();
            public string Action; public int Stops;
            public Task<bool> ChooseBackendAsync() => Task.FromResult(true);
            public Task<VpnToolResult> RunAsync(string action) { Action=action;if(action=="Stop")Stops++;return Pending.Task; }
            public Task<VpnToolResult> CheckEndpointAsync(VpnConnectionCode endpoint) => Pending.Task;
            public void OpenClientSetup() { }
        }
        [UnityTest]
        public IEnumerator StartingHostDoesNotBlockFramesOrStopWhenPanelCloses()
        {
            var tools=new Tools();
            using var rig=new LobbyUiRig(vpn:tools);
            yield return null;
            rig.Flow.OpenServerSettings(); yield return null;
            var v=rig.Root.ServerSettings;v.ShowVpn(true);
            rig.Click(v.VpnStart);Assert.AreEqual("Start",tools.Action);
            yield return null;Assert.False(v.VpnStart.interactable,"pending operation must not start twice");
            rig.Flow.CloseServerSettings();
            tools.Pending.SetResult(new VpnToolResult{success=true,ready=true,message="Ready",connectionCode="endpoint-only"});
            yield return null;Assert.AreEqual(0,tools.Stops,"closing panel never owns server lifetime");
            Assert.AreEqual("endpoint-only",v.VpnCode.text);
        }
        [UnityTest]
        public IEnumerator VpnPanelFitsAndRestoresNormalEndpointForm()
        {
            using var rig = new LobbyUiRig();
            yield return null;
            var v=rig.Root.ServerSettings;
            Assert.NotNull(v.VpnConnect);Assert.NotNull(v.VpnSetup);Assert.NotNull(v.VpnStart);Assert.NotNull(v.VpnStop);Assert.NotNull(v.VpnCopy);
            var panel=(RectTransform)v.transform.Find("Panel");var before=panel.sizeDelta;
            rig.Flow.OpenServerSettings();rig.Presenter.Tick();
            v.ShowVpn(true);Canvas.ForceUpdateCanvases();
            Assert.True(v.VpnCode.gameObject.activeInHierarchy);
            Assert.False(v.Host.gameObject.activeInHierarchy);
            Assert.Greater(panel.rect.width,before.x);
            var corners=new Vector3[4];
            foreach(var control in new Component[]{v.VpnCode,v.VpnConnect,v.VpnClientSetup,v.VpnBrowse,v.VpnSetup,v.VpnStart,v.VpnStop,v.VpnRefresh,v.VpnCopy,v.VpnStatus,v.Close})
            {
                ((RectTransform)control.transform).GetWorldCorners(corners);
                foreach(var c in corners)Assert.True(panel.rect.Contains(panel.InverseTransformPoint(c)),control.name+" inside popup");
            }
            // Capture the real kit layout while the ordinary frame driver is paused for this fixture only.
            rig.Root.enabled=false;
            ScreenCapture.CaptureScreenshot(System.IO.Path.GetFullPath("Backend/out/tailscale-ui/vpn-panel.png"));
            yield return null;
            v.ShowVpn(false);
            Assert.AreEqual(before,panel.sizeDelta);Assert.True(v.Host.gameObject.activeInHierarchy);Assert.False(v.VpnCode.gameObject.activeInHierarchy);
        }
    }
}
#endif
