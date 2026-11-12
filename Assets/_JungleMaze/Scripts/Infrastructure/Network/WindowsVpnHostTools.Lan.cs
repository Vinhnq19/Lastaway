using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using JungleBackend.Hosting;
using JungleMaze.App.Config;
using JungleMaze.App.Ports;
using Newtonsoft.Json;
namespace JungleMaze.Infrastructure.Network
{
    public sealed partial class WindowsVpnHostTools
    {
        public async Task<LanInterface[]> GetLanInterfacesAsync()
        {
            if(Environment.OSVersion.Platform!=PlatformID.Win32NT)return Array.Empty<LanInterface>();
            // Fixed read-only command. No hostname/connection-code text is ever executed.
            const string command="$env:PSModulePath=$env:WINDIR+'\\System32\\WindowsPowerShell\\v1.0\\Modules';$ErrorActionPreference='Stop';[Console]::OutputEncoding=[Text.Encoding]::UTF8;$r=@(Get-NetAdapter -Physical | Where-Object Status -eq 'Up' | ForEach-Object {$a=$_;Get-NetIPAddress -InterfaceIndex $a.ifIndex -AddressFamily IPv4 -ErrorAction SilentlyContinue | Where-Object {$_.AddressState -eq 'Preferred' -and $_.IPAddress -notlike '169.254.*'} | ForEach-Object {$p=Get-NetConnectionProfile -InterfaceIndex $a.ifIndex -ErrorAction SilentlyContinue;[pscustomobject]@{name=$a.Name;ip=$_.IPAddress;index=$a.ifIndex;isPrivate=($p.NetworkCategory -eq 'Private')}}});ConvertTo-Json -InputObject $r -Compress";
            return await Task.Run(()=>
            {
                using(var process=Process.Start(new ProcessStartInfo("powershell.exe","-NoProfile -EncodedCommand "+Convert.ToBase64String(Encoding.Unicode.GetBytes(command)))
                {UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true,StandardOutputEncoding=Encoding.UTF8}))
                {
                    var output=process.StandardOutput.ReadToEndAsync();var error=process.StandardError.ReadToEndAsync();
                    if(!process.WaitForExit(25000)){process.Kill();return Array.Empty<LanInterface>();}
                    error.GetAwaiter().GetResult();return process.ExitCode==0?JsonConvert.DeserializeObject<LanInterface[]>(output.GetAwaiter().GetResult().Trim().Trim('\uFEFF'))??Array.Empty<LanInterface>():Array.Empty<LanInterface>();
                }
            });
        }
        public Task<VpnToolResult> RunLanAsync(string action,string ip)=>RunToolAsync(action,"manage-lan.ps1",ip);
        public bool IsLanTrusted(LanConnectionCode code)=>code!=null && LanTls.IsTrusted(LanConfig(code));
        public async Task<VpnToolResult> CheckLanEndpointAsync(LanConnectionCode code,bool confirm)
        {
            var config=LanConfig(code);
            if(!confirm && !LanTls.IsTrusted(config))return Fail("Hãy xác nhận mã nhận trực tiếp từ chủ máy trước khi kết nối.");
            var result=await Health(config,confirm);
            if(result.ready && confirm) { try {LanTls.Accept(config);}catch(Exception){return Fail("Không lưu được xác nhận máy chủ. Kiểm tra quyền ghi dữ liệu game.");} }
            return result;
        }
        public Task<VpnToolResult> CheckServerAsync(ClientConfig config)=>Health(config,false);
        private static async Task<VpnToolResult> Health(ClientConfig config,bool confirm)
        {
            try
            {
                if(config==null || config.Validate()!=null)return Fail("Địa chỉ máy chủ chưa hợp lệ.");
                using(var http=LanTls.CreateHttpClient(config,confirm))
                using(var response=await http.GetAsync(config.ApiBaseUrl.TrimEnd('/')+"/health"))
                {
                    if(!response.IsSuccessStatusCode)return Fail("Máy chủ chưa sẵn sàng. Hãy thử lại sau.");
                    return new VpnToolResult{success=true,ready=true,state="ApiReady",message="Dịch vụ tài khoản sẵn sàng. Hãy đăng nhập và vào phòng để chơi."};
                }
            }
            catch(Exception){return Fail("Không kết nối an toàn được tới máy chủ. Kiểm tra mã mới, máy host và kết nối mạng.");}
        }
        private static ClientConfig LanConfig(LanConnectionCode code)=>new ClientConfig(ConnectionMode.LAN,code.Api,code.Ip,code.GamePort,JungleBackend.Protocol.JoinAuthMode.RoomCode,4,5000,"Auto","lan-code",code.WebSocket,true,code.CertificateSha256);
    }
}
