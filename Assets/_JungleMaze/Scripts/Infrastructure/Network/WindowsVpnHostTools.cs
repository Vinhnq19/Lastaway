using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JungleMaze.App.Config;
using JungleMaze.App.Ports;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace JungleMaze.Infrastructure.Network
{
    public sealed class WindowsVpnHostTools : IVpnHostTools
    {
        private readonly string _settings;
        private readonly string _results;
        private bool _busy;
        public string BackendPath { get; private set; }
        private static string Tailscale => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Tailscale", "tailscale.exe");
        public WindowsVpnHostTools(string dataPath, string persistentPath)
        {
            _settings = Path.Combine(persistentPath, "vpn-backend-path.txt");
            _results = Path.Combine(persistentPath, "vpn-tools");
            if (File.Exists(_settings))
            {
                try { var saved = File.ReadAllText(_settings); if (IsBackend(saved)) BackendPath = Path.GetFullPath(saved); }
                catch (IOException) { }
            }
            if (BackendPath == null)
                for (var dir = new DirectoryInfo(dataPath); dir != null; dir = dir.Parent)
                {
                    var candidate = Path.Combine(dir.FullName, "Backend");
                    if (!IsBackend(candidate)) continue;
                    BackendPath = candidate; break;
                }
        }
        public static bool IsBackend(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || path.IndexOfAny(Path.GetInvalidPathChars()) >= 0 || !Path.IsPathRooted(path) || path.StartsWith(@"\\", StringComparison.Ordinal) || path.IndexOfAny(new[] {'"', '\r', '\n'}) >= 0) return false;
            return File.Exists(Path.Combine(path, "compose.host.yaml")) &&
                File.Exists(Path.Combine(path, "Deploy", "Host", "manage-vpn.ps1")) &&
                File.Exists(Path.Combine(path, "Api", "Api.csproj")) && File.Exists(Path.Combine(path, "GameServer", "GameServer.csproj"));
        }
        public async Task<bool> ChooseBackendAsync()
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT) return false;
            // Fixed command only: never interpolate paths from a connection code into executable text.
            const string script = "Add-Type -AssemblyName System.Windows.Forms; $d=New-Object System.Windows.Forms.FolderBrowserDialog; $d.Description='Chon thu muc Backend cua source Lastaway tin cay'; if($d.ShowDialog() -eq 'OK'){[Console]::OutputEncoding=[Text.Encoding]::UTF8;[Console]::Write($d.SelectedPath)}";
            var selected = await Task.Run(() =>
            {
                using (var p = Process.Start(new ProcessStartInfo("powershell.exe", "-NoProfile -STA -EncodedCommand " + Convert.ToBase64String(Encoding.Unicode.GetBytes(script)))
                { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, StandardOutputEncoding = Encoding.UTF8 }))
                { var value = p.StandardOutput.ReadToEnd(); p.WaitForExit(); return value.Trim().Trim('\uFEFF'); }
            });
            if (!IsBackend(selected)) return false;
            BackendPath = Path.GetFullPath(selected);
            Directory.CreateDirectory(Path.GetDirectoryName(_settings)); File.WriteAllText(_settings, BackendPath);
            return true;
        }
        public async Task<VpnToolResult> RunAsync(string action)
        {
            if (_busy) return Fail("Đang xử lý thao tác trước.");
            if (Environment.OSVersion.Platform != PlatformID.Win32NT) return Fail("Công cụ host hiện hỗ trợ Windows.");
            if (!IsBackend(BackendPath)) return Fail("Chọn thư mục Backend của source tin cậy trước.");
            if (action != "Setup" && action != "Start" && action != "Status" && action != "Stop") return Fail("Thao tác không hợp lệ.");
            _busy = true;
            Directory.CreateDirectory(_results);
            var output = Path.Combine(_results, Guid.NewGuid().ToString("N") + ".json");
            try
            {
                var script = Path.Combine(BackendPath, "Deploy", "Host", "manage-vpn.ps1");
                var args = "-NoProfile -ExecutionPolicy Bypass -File \"" + script + "\" -Action " + action + " -ResultFile \"" + output + "\"";
                var result = await Task.Run(() =>
                {
                    var interactive = action == "Setup";
                    using (var p = Process.Start(new ProcessStartInfo("powershell.exe", args)
                    { WorkingDirectory = BackendPath, UseShellExecute = interactive, CreateNoWindow = !interactive,
                        WindowStyle = interactive ? ProcessWindowStyle.Normal : ProcessWindowStyle.Hidden }))
                    {
                        if (!p.WaitForExit(900000)) return Fail("Thao tác quá thời gian; kiểm tra Docker/Tailscale. Server không bị tự tắt.");
                        if (!File.Exists(output)) return Fail("Thiết lập bị hủy hoặc không xuất trạng thái. Có thể thử lại.");
                        return JsonUtility.FromJson<VpnToolResult>(File.ReadAllText(output));
                    }
                });
                return result ?? Fail("Không đọc được trạng thái host.");
            }
            catch (Exception) { return Fail("Không chạy được công cụ host. Kiểm tra thư mục Backend và quyền Windows."); }
            finally { _busy = false; if (File.Exists(output)) File.Delete(output); }
        }
        public async Task<VpnToolResult> CheckEndpointAsync(VpnConnectionCode endpoint)
        {
            if (!File.Exists(Tailscale)) return Fail("Chưa cài Tailscale. Bấm Cài / đăng nhập VPN.");
            try
            {
                var state = await Task.Run(() =>
                {
                    using (var p = Process.Start(new ProcessStartInfo(Tailscale, "status --json") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true }))
                    {
                        var output = p.StandardOutput.ReadToEndAsync(); var error = p.StandardError.ReadToEndAsync();
                        if (!p.WaitForExit(8000)) { p.Kill(); return null; }
                        error.GetAwaiter().GetResult();
                        return p.ExitCode == 0 ? JObject.Parse(output.GetAwaiter().GetResult()) : null;
                    }
                });
                if ((string)state?["BackendState"] != "Running") return Fail("Tailscale chưa đăng nhập/kết nối. Mở Tailscale rồi thử lại.");
                var resolving = Dns.GetHostAddressesAsync(endpoint.Domain);
                if (await Task.WhenAny(resolving, Task.Delay(5000)) != resolving ||
                    !(await resolving).Any(address =>
                    {
                        var bytes = address.GetAddressBytes();
                        return bytes.Length == 4 && bytes[0] == 100 && bytes[1] >= 64 && bytes[1] <= 127 &&
                            (endpoint.Ip == endpoint.Domain || address.ToString() == endpoint.Ip);
                    }))
                    return Fail("Địa chỉ VPN chưa trỏ đúng máy chủ. Kiểm tra mã kết nối và lời mời Tailscale.");
                using (var handler = new HttpClientHandler { AllowAutoRedirect = false })
                using (var http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) })
                using (var response = await http.GetAsync(endpoint.Api + "/health", HttpCompletionOption.ResponseHeadersRead))
                {
                    if (!response.IsSuccessStatusCode) return Fail("Đã tới máy chủ nhưng dịch vụ tài khoản chưa sẵn sàng.");
                }
                return new VpnToolResult { success = true, ready = true, state = "ApiReady", message = "Dịch vụ tài khoản sẵn sàng. Hãy đăng nhập và vào phòng để kiểm tra kết nối chơi." };
            }
            catch (Exception) { return Fail("Chưa kết nối an toàn được tới máy chủ. Kiểm tra lời mời Tailscale và nhờ chủ máy kiểm tra server."); }
        }
        public void OpenClientSetup()
        {
            var gui = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Tailscale", "tailscale-ipn.exe");
            if (File.Exists(gui)) Process.Start(new ProcessStartInfo(gui) { UseShellExecute = true });
            else Application.OpenURL("https://tailscale.com/download/windows");
        }
        private static VpnToolResult Fail(string message) => new VpnToolResult { state = "NeedsAttention", message = message };
    }
}
