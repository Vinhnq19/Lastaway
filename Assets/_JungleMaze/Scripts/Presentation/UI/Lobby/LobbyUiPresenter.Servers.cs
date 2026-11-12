using System;
using System.Collections.Generic;
using JungleBackend.Hosting;
using JungleMaze.App.Config;
using JungleMaze.App.Ports;
using UnityEngine;

namespace JungleMaze.Presentation.UI.Lobby
{
    public sealed partial class LobbyUiPresenter
    {
        private int _serverMode;
        private bool _devSettings;
        private LanConnectionCode _pendingLan;
        private LanInterface[] _interfaces = Array.Empty<LanInterface>();
        private string _lanCode = "", _tailscaleCode = "";
        private void FillEndpoint()
        {
            var v=_root.ServerSettings;
            var config=_menu?.CurrentConfig;
            _serverMode=config?.Mode==ConnectionMode.LAN?1:
                Uri.TryCreate(config?.ApiBaseUrl,UriKind.Absolute,out var api) && api.Host.EndsWith(".ts.net",StringComparison.OrdinalIgnoreCase)?2:0;
            v.Profile.ClearOptions();v.Profile.AddOptions(new List<string>{"Online · AWS","LAN · cùng mạng","VPN · Tailscale"});
            v.Profile.SetValueWithoutNotify(_serverMode);
            if(v.DevProfile != null){v.DevProfile.ClearOptions();v.DevProfile.AddOptions(new List<string>{"Dành cho dev…","Local · Docker","Internet · tự host"});v.DevProfile.SetValueWithoutNotify(0);}
            var local=config?.Mode==ConnectionMode.Local;
            var selfHost=_serverMode==0 && config!=null && Uri.TryCreate(config.ApiBaseUrl,UriKind.Absolute,out var origin) && origin.Host!="junglemaze.duckdns.org";
            _devSettings=Debug.isDebugBuild && (local || selfHost);
            if(_devSettings && v.DevProfile!=null)v.DevProfile.SetValueWithoutNotify(local?1:2);
            _vpnMessage=_devSettings ? (local?"Đang dùng Local · Docker (dev).":"Đang dùng Internet · tự host (dev).") : _serverMode==1 ? "Đang dùng máy chủ LAN. Dán mã khác nếu muốn đổi máy chủ." : _serverMode==2 ? "Đang dùng máy chủ Tailscale." : "Online · AWS. Bấm Kết nối để kiểm tra máy chủ.";
            FillEndpointFields();
            if(_serverMode==1)ReadInterfaces();
        }
        private void SelectProfile(int mode)
        {
            if(mode<0 || mode>2)return;
            if(_serverMode==1)_lanCode=_root.ServerSettings.VpnCode.text;
            if(_serverMode==2)_tailscaleCode=_root.ServerSettings.VpnCode.text;
            _serverMode=mode;_vpnRequest++;_pendingLan=null;_vpnHostCode=null;_devSettings=false;
            _root.ServerSettings.VpnCode.SetTextWithoutNotify(mode==1?_lanCode:_tailscaleCode);
            _vpnMessage=mode==0?"Online · AWS. Bấm Kết nối để sử dụng máy chủ này.":mode==1?
                "Dán mã do chủ máy cùng mạng gửi. Chỉ xác nhận mã từ người bạn tin cậy.":"Dán mã kết nối hoặc thiết lập máy chủ Tailscale.";
            if(mode==1)ReadInterfaces();
        }
        private void SelectDevProfile(int index)
        {
            if(!Debug.isDebugBuild || index<1 || index>2 || _menu==null)return;
            _vpnRequest++;_devSettings=true;_menu.SelectProfile(index==1?"phase2-local":"phase2-selfhost");
            _vpnMessage=index==1?"Đang chọn Local · Docker. Bật máy chủ trên máy này rồi bấm Lưu (dev).":"Đang chọn Internet · tự host. Kiểm tra địa chỉ rồi bấm Lưu (dev).";
            FillEndpointFields();
        }
        private async void ReadInterfaces()
        {
            if(_lan==null)return;var generation=_vpnRequest;
            var loading=_root.ServerSettings.LanInterface;
            if(loading!=null){loading.ClearOptions();loading.AddOptions(new List<string>{"Đang đọc Wi-Fi / Ethernet…"});}
            try
            {
                var found=await _lan.GetLanInterfacesAsync();
                if(_disposed || generation!=_vpnRequest || _serverMode!=1)return;
                _interfaces=Array.FindAll(found ?? Array.Empty<LanInterface>(), i=>LanConnectionCode.IsPrivateIp(i.ip));
                var drop=_root.ServerSettings.LanInterface;if(drop==null)return;
                var previous=drop.options.Count>0?drop.options[drop.value].text:null;
                var labels=new List<string>();foreach(var nic in _interfaces)labels.Add(nic.name+" · "+nic.ip+(nic.isPrivate?"":" · Mạng Public"));
                if(labels.Count==0)labels.Add("Chưa có Wi-Fi / Ethernet phù hợp");
                drop.ClearOptions();drop.AddOptions(labels);var selected=labels.IndexOf(previous ?? "");drop.SetValueWithoutNotify(selected>=0?selected:0);
            }
            catch(Exception){if(!_disposed && generation==_vpnRequest)_vpnMessage="Chưa đọc được kết nối Wi-Fi / Ethernet. Thử mở lại bảng máy chủ.";}
        }
        private void ClientSetupOrPaste()
        {
            if(_serverMode==1){_pendingLan=null;_vpnRequest++;_root.ServerSettings.VpnCode.text=GUIUtility.systemCopyBuffer.Trim();return;}
            try{_vpn?.OpenClientSetup();_vpnMessage="Đăng nhập Tailscale, nhận lời mời rồi dán mã kết nối.";}
            catch(Exception){_vpnMessage="Không mở được Tailscale; cài hoặc mở ứng dụng thủ công.";}
        }
        private async void BrowseVpnBackend()
        {
            if(_vpn==null || _vpnBusy)return;var generation=_vpnRequest;_vpnBusy=true;
            try{var ok=await _vpn.ChooseBackendAsync();if(!_disposed && generation==_vpnRequest)_vpnMessage=ok?"Đã chọn Backend. Bấm Thiết lập lần đầu.":"Chưa chọn Backend hợp lệ.";}
            catch(Exception){if(!_disposed && generation==_vpnRequest)_vpnMessage="Không chọn được thư mục Backend.";}
            finally{_vpnBusy=false;}
        }
        private async void RunVpn(string action)
        {
            if(_vpn==null || _vpnBusy)return;
            var mode=_serverMode;var request=++_vpnRequest;string ip=null;
            if(mode==1 && action!="Stop")
            {
                var drop=_root.ServerSettings.LanInterface;
                if(_lan==null || _interfaces.Length==0 || drop==null || drop.value>=_interfaces.Length){_vpnMessage="Chọn kết nối Wi-Fi hoặc Ethernet trước.";return;}
                ip=_interfaces[drop.value].ip;
            }
            _vpnBusy=true;_vpnMessage=action=="Setup"?"Hoàn tất thiết lập trong cửa sổ riêng; xác nhận quyền nếu được hỏi…":"Đang xử lý máy chủ…";
            try
            {
                var result=mode==1?await _lan.RunLanAsync(action,ip):await _vpn.RunAsync(action);
                if(_disposed || request!=_vpnRequest || mode!=_serverMode)return;
                _vpnMessage=HostMessage(result);_vpnHostCode=result.ready?result.connectionCode:null;
                if(result.ready && !string.IsNullOrEmpty(result.connectionCode))_root.ServerSettings.VpnCode.SetTextWithoutNotify(result.connectionCode);
            }
            catch(Exception){if(!_disposed && request==_vpnRequest)_vpnMessage="Không chạy được công cụ máy chủ. Kiểm tra Docker và cửa sổ thiết lập.";}
            finally{_vpnBusy=false;}
        }
        private string HostMessage(VpnToolResult result)
        {
            // Host tools return curated messages, never raw stdout/exception text.
            if(_serverMode==1)return result.message;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            return result.message;
#else
            if(result.ready)return "Máy chủ sẵn sàng. Đăng nhập và vào phòng để chơi.";
            if(result.success)return result.state=="Stopped"?"Đã dừng máy chủ; dữ liệu được giữ.":"Đã thiết lập máy chủ.";
            return "Chưa kết nối được máy chủ. Kiểm tra Tailscale và lời mời rồi thử lại.";
#endif
        }
        private void ConnectVpn()
        {
            if(_serverMode==1){ConnectLan(false);return;}ConnectTailscale();
        }
        private async void ConnectLan(bool confirm)
        {
            if(_lan==null || _vpnBusy || _menu==null)return;
            if(!LanConnectionCode.TryParse(_root.ServerSettings.VpnCode.text.Trim(),out var code)){_vpnMessage="Mã LAN không hợp lệ. Nhờ chủ máy sao chép lại mã kết nối.";return;}
            if(confirm && (_pendingLan==null || _pendingLan.CertificateSha256!=code.CertificateSha256 || _pendingLan.Api!=code.Api)){_pendingLan=null;return;}
            if(!confirm && !_lan.IsLanTrusted(code)){_pendingLan=code;_vpnMessage="Xác nhận máy chủ từ mã do bạn của bạn gửi? Không xác nhận mã không rõ nguồn.";return;}
            var request=++_vpnRequest;_vpnBusy=true;_vpnMessage="Đang kiểm tra kết nối an toàn…";
            try
            {
                var result=await _lan.CheckLanEndpointAsync(code,confirm);
                if(_disposed || request!=_vpnRequest || _serverMode!=1)return;
                _pendingLan=null;_vpnMessage=result.message;
                if(result.success && result.ready && _menu.ApplyLanEndpoint(code)){_vpnMessage="Đã kết nối máy chủ LAN. Hãy đăng nhập game.";_flow.Api?.Check();}
            }
            catch(Exception){if(!_disposed && request==_vpnRequest)_vpnMessage="Chưa kết nối được máy chủ LAN. Kiểm tra mã mới và kết nối cùng mạng.";}
            finally{_vpnBusy=false;}
        }
        private async void ConnectTailscale()
        {
            if(_vpn==null || _vpnBusy || _menu==null)return;
            if(!VpnConnectionCode.TryParse(_root.ServerSettings.VpnCode.text,out var code)){_vpnMessage="Mã kết nối không hợp lệ. Nhờ chủ máy sao chép lại mã.";return;}
            var request=++_vpnRequest;_vpnBusy=true;_vpnMessage="Đang kiểm tra kết nối an toàn…";
            try
            {
                var result=await _vpn.CheckEndpointAsync(code);
                if(_disposed || request!=_vpnRequest || _serverMode!=2)return;
                _vpnMessage=HostMessage(result);
                if(result.success && result.ready && _menu.ApplyVpnEndpoint(code)){FillEndpointFields();_flow.Api?.Check();}
            }
            catch(Exception){if(!_disposed && request==_vpnRequest)_vpnMessage="Không tới được máy chủ. Kiểm tra Tailscale và kết nối mạng.";}
            finally{_vpnBusy=false;}
        }
        private async void SaveEndpoint()
        {
            if(_menu==null || _vpnBusy)return;
            if(_devSettings && Debug.isDebugBuild)
            {
                var v=_root.ServerSettings;_menu.Host=v.Host.text;_menu.Port=v.Port.text;_menu.ApiBaseUrl=v.Api.text;_menu.SaveEndpoint();_flow.Api?.Check();return;
            }
            if(_lan==null){_vpnMessage="Chưa có công cụ kiểm tra kết nối.";return;}
            var request=++_vpnRequest;_vpnBusy=true;_vpnMessage="Đang kiểm tra kết nối an toàn…";
            try
            {
                var result=await _lan.CheckServerAsync(_menu.AwsConfig);
                if(_disposed || request!=_vpnRequest || _serverMode!=0)return;
                _vpnMessage=result.message;
                if(result.ready && result.success && _menu.ApplyCheckedServer(_menu.AwsConfig))_flow.Api?.Check();
            }
            catch(Exception){if(!_disposed && request==_vpnRequest)_vpnMessage="Chưa tới được máy chủ. Kiểm tra kết nối rồi thử lại.";}
            finally{_vpnBusy=false;}
        }
        private void FillEndpointFields()
        {
            if(_menu==null)return;var v=_root.ServerSettings;
            v.Host.SetTextWithoutNotify(_menu.Host ?? "");v.Port.SetTextWithoutNotify(_menu.Port ?? "");v.Api.SetTextWithoutNotify(_menu.ApiBaseUrl ?? "");
        }
    }
}
