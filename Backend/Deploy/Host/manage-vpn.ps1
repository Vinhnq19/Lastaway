param(
    [ValidateSet('Setup','Start','Status','Stop')][string]$Action='Status',
    [string]$ResultFile='',
    [switch]$LibraryOnly
)
# Invoked only from a trusted Backend checkout; JSON results contain no configuration secrets.
$ErrorActionPreference='Stop'
# Unity may inherit PowerShell7's module path; Windows PowerShell5 must load its own security/firewall modules.
if ($PSVersionTable.PSEdition -eq 'Desktop') { $env:PSModulePath=(Join-Path $PSHOME 'Modules')+';'+(Join-Path $env:ProgramFiles 'WindowsPowerShell/Modules') }
$backend=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$privateConfig=Join-Path $PSScriptRoot '.env.host.vpn'
$statePath=Join-Path $PSScriptRoot '.vpn-state.json'
$ts=Join-Path $env:ProgramFiles 'Tailscale/tailscale.exe'
function Fail([string]$Text) { throw [InvalidOperationException]::new($Text) }
function Native([string]$Exe,[string[]]$Arguments,[int]$Seconds=30) {
    $info=[Diagnostics.ProcessStartInfo]::new();$info.FileName=$Exe
    $info.Arguments=($Arguments | ForEach-Object { '"'+([regex]::Replace(([regex]::Replace($_,'(\\*)"','$1$1\"')),'(\\+)$','$1$1'))+'"' }) -join ' '
    $info.StandardOutputEncoding=[Text.Encoding]::UTF8;$info.StandardErrorEncoding=[Text.Encoding]::UTF8
    if ($Exe -eq 'docker' -and $Arguments[0] -eq 'compose') {
        $composeText=[IO.File]::ReadAllText((Join-Path $backend 'compose.host.yaml'))
        $keys=@([regex]::Matches($composeText,'\$\{([A-Z][A-Z0-9_]*)') | ForEach-Object { $_.Groups[1].Value })+@('COMPOSE_PROFILES','COMPOSE_FILE','COMPOSE_PROJECT_NAME','COMPOSE_ENV_FILES')
        foreach ($key in $keys) { $info.EnvironmentVariables.Remove($key) }
    }
    $info.UseShellExecute=$false;$info.CreateNoWindow=$true;$info.RedirectStandardOutput=$true;$info.RedirectStandardError=$true
    $p=[Diagnostics.Process]::Start($info)
    try {
        $stdout=$p.StandardOutput.ReadToEndAsync();$stderr=$p.StandardError.ReadToEndAsync()
        if (-not $p.WaitForExit($Seconds*1000)) { $p.Kill();Fail 'Thao tác quá thời gian. Kiểm tra Docker/Tailscale rồi thử lại; server không bị tự tắt.' }
        $value=$stdout.GetAwaiter().GetResult();$null=$stderr.GetAwaiter().GetResult()
        if ($p.ExitCode -ne 0) {
            if ($Exe -eq 'docker' -and $Arguments[0] -eq 'info') { Fail 'Docker Desktop chưa sẵn sàng. Bật Docker Desktop và chờ Engine chạy rồi thử lại.' }
            Fail ('Không hoàn tất '+[IO.Path]::GetFileNameWithoutExtension($Exe)+' '+$Arguments[0]+' (mã '+$p.ExitCode+'). Kiểm tra Docker/Tailscale hoặc cửa sổ thiết lập.')
        }
        return $value.Trim()
    } finally { $p.Dispose() }
}
function Props($Object) { if ($null -eq $Object) { return ,@() };return ,@($Object.PSObject.Properties | ForEach-Object Name) }
function FunnelEnabled($Config) {
    return @($Config.AllowFunnel.PSObject.Properties | Where-Object { $_.Value -eq $true }).Count -gt 0
}
function ServeEmpty($Config) {
    if (FunnelEnabled $Config) { return $false }
    foreach ($key in @('TCP','Web','Services','Foreground')) { if ((Props $Config.$key).Count -gt 0) { return $false } }
    return $true
}
function OurServe($Config,[string]$Dns,[int]$ProxyPort=18080) {
    if ((Props $Config.TCP).Count -ne 1 -or (Props $Config.TCP)[0] -ne '443') { return $false }
    if ($Config.TCP.'443'.HTTPS -ne $true) { return $false }
    if ((Props $Config.Web).Count -ne 1 -or (Props $Config.Web)[0] -ne ($Dns+':443')) { return $false }
    $handlers=$Config.Web.($Dns+':443').Handlers
    return (Props $handlers).Count -eq 1 -and (Props $handlers)[0] -eq '/' -and $handlers.'/'.Proxy -eq ('http://127.0.0.1:'+$ProxyPort) -and (Props $Config.Services).Count -eq 0 -and (-not (FunnelEnabled $Config)) -and (Props $Config.Foreground).Count -eq 0
}
function ConnectionCode([string]$Ip,[string]$Dns,[int]$Port) {
    if ($Ip -notmatch '^100\.(\d+)\.\d+\.\d+$' -or [int]$Matches[1] -lt 64 -or [int]$Matches[1] -gt 127 -or $Port -lt 1 -or $Port -gt 65535) { Fail 'IP/cổng Tailscale không hợp lệ.' }
    $parsed=$null;if (-not [Net.IPAddress]::TryParse($Ip,[ref]$parsed) -or $parsed.ToString() -ne $Ip) { Fail 'IP VPN không hợp lệ.' }
    if ($Dns -notmatch '^(?:[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?\.){2,}ts\.net$' -or $Dns.Length -gt 253) { Fail 'Cần hostname Tailscale .ts.net hợp lệ; bật MagicDNS/HTTPS trên tailnet.' }
    $short=$Dns.Substring(0,$Dns.Length-7)
    if ($Port -eq 7777) { return $short }
    return $short+':'+$Port
}
function Compose([string[]]$Commands,[int]$Seconds=30) {
    Native 'docker' (@('compose','--project-name','junglemaze-host','--project-directory',$backend,'-f',(Join-Path $backend 'compose.host.yaml'),'--env-file',$privateConfig,'--profile','vpn')+$Commands) $Seconds
}
function ReadState { if (Test-Path -LiteralPath $statePath) { $value=Get-Content -LiteralPath $statePath -Encoding UTF8 -Raw | ConvertFrom-Json; if (-not $value.PSObject.Properties['proxyPort']) { $value | Add-Member -NotePropertyName proxyPort -NotePropertyValue 8080 };return $value };return [pscustomobject]@{schemaVersion=1;backend=$backend;stackOwned=$false;serveOwned=$false;dns='';proxyPort=18080} }
function SaveState($State) { [IO.File]::WriteAllText($statePath,($State | ConvertTo-Json),[Text.UTF8Encoding]::new($false)) }
function VerifyStack($Owner) {
    if ($Owner.backend -ne $backend) { Fail 'File quản lý host thuộc checkout khác; không can thiệp stack.' }
    $ids=Native 'docker' @('ps','-aq','--filter','label=com.docker.compose.project=junglemaze-host')
    if ($ids) {
        $items=Native 'docker' (@('inspect')+@($ids -split '\s+')) | ConvertFrom-Json
        foreach ($item in $items) {
            $service=$item.Config.Labels.'com.docker.compose.service'
            if ($service -notin @('mongo','api','game-server','caddy-vpn')) { Fail 'Stack đang có service ngoài VPN (có thể là Public). Không can thiệp.' }
            $dir=$item.Config.Labels.'com.docker.compose.project.working_dir'
            if (-not $Owner.stackOwned -or -not $dir -or $dir.Replace('\','/').TrimEnd('/') -ine $backend.Replace('\','/').TrimEnd('/')) { Fail 'Stack self-host đã được chạy ngoài công cụ hoặc ở source khác. Không tự chiếm/dừng stack.' }
        }
    }
    return $ids
}
function ProxyPort {
    $line=Get-Content -LiteralPath $privateConfig -Encoding UTF8 | Where-Object { $_ -match '^HOST_VPN_PORT=' } | Select-Object -First 1
    $port=if ($line) { [int]($line -split '=',2)[1] } else { 18080 }
    if ($port -lt 1 -or $port -gt 65535) { Fail 'HOST_VPN_PORT không hợp lệ.' };return $port
}
function EnsureProxyAvailable([int]$Port,$Owner) {
    $tcp=Get-NetTCPConnection -State Listen -LocalPort $Port -ErrorAction SilentlyContinue
    if (-not $tcp) { return }
    $proxyId=Compose @('ps','-q','caddy-vpn')
    if ($Owner.stackOwned -and $proxyId) {
        $proxy=Native 'docker' @('inspect',$proxyId) | ConvertFrom-Json
        $bindings=$proxy[0].HostConfig.PortBindings.'8080/tcp'
        if ($proxy[0].State.Running -and @($bindings | Where-Object { $_.HostIp -eq '127.0.0.1' -and $_.HostPort -eq [string]$Port }).Count -eq 1) { return }
    }
    Fail ('Cổng HTTP '+$Port+' đang có ứng dụng khác. Đổi HOST_VPN_PORT trong cấu hình host; không tự dừng ứng dụng đó.')
}
function TailState {
    if (-not (Test-Path -LiteralPath $ts)) { Fail 'Chưa cài Tailscale. Bấm Thiết lập hoặc Cài / đăng nhập VPN.' }
    $s=Native $ts @('status','--json') | ConvertFrom-Json
    if ($s.BackendState -ne 'Running') { Fail 'Tailscale chưa đăng nhập/kết nối. Hoàn tất đăng nhập rồi thử lại.' }
    return $s
}
function Health([string]$Dns) {
    # Normal OS DNS/TLS only; do not pin an IP or accept invalid certificates.
    try {
        Add-Type -AssemblyName System.Net.Http
        $handler=[Net.Http.HttpClientHandler]::new();$handler.AllowAutoRedirect=$false
        $http=[Net.Http.HttpClient]::new($handler);$http.Timeout=[TimeSpan]::FromSeconds(8)
        try { $response=$http.GetAsync('https://'+$Dns+'/health').GetAwaiter().GetResult();try{return $response.IsSuccessStatusCode}finally{$response.Dispose()} } finally { $http.Dispose() }
    } catch { return $false }
}
if ($LibraryOnly) { return }
$result=[ordered]@{success=$false;ready=$false;state='NeedsAttention';message='';connectionCode=''}
$lock=$null
try {
    $lock=[IO.File]::Open((Join-Path $PSScriptRoot '.vpn.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
    $owner=ReadState
    if ($owner.backend -ne $backend) { Fail 'Trạng thái host thuộc checkout khác; không thay stack hoặc Serve.' }
    if ($Action -eq 'Setup') {
        if (-not (Get-Command docker -ErrorAction SilentlyContinue)) { Start-Process 'https://docs.docker.com/desktop/setup/install/windows-install/';Fail 'Cài và bật Docker Desktop rồi bấm Thiết lập lại.' }
        $null=Native 'docker' @('info','--format','{{.ServerVersion}}')
        if (-not (Test-Path -LiteralPath $ts)) {
            if (-not (Get-Command winget -ErrorAction SilentlyContinue)) { Start-Process 'https://tailscale.com/download/windows';Fail 'Cài Tailscale từ trang vừa mở rồi bấm Thiết lập lại.' }
            & winget install --id Tailscale.Tailscale --exact --accept-package-agreements --accept-source-agreements
            if ($LASTEXITCODE -ne 0) { Fail 'Cài Tailscale bị hủy/chưa xong. Có thể cài thủ công rồi thử lại.' }
        }
        # Interactive setup window: user performs browser authentication; no auth key stored.
        $current=Native $ts @('status','--json') | ConvertFrom-Json
        if ($current.BackendState -ne 'Running') {
            & $ts up --timeout=120s
            if ($LASTEXITCODE -ne 0) { Fail 'Hoàn tất đăng nhập Tailscale trong trình duyệt rồi bấm Thiết lập lại.' }
        }
        $null=Native $ts @('set','--accept-dns=true')
        if (-not (Test-Path -LiteralPath $privateConfig)) {
            & (Join-Path $PSScriptRoot 'configure-host.ps1') -Mode vpn
        }
        $null=VerifyStack $owner
        $vpnPort=ProxyPort;EnsureProxyAvailable $vpnPort $owner
        # A separate UAC step adds only our VPN rule, leaving Public/other rules intact.
        $fw=Join-Path $PSScriptRoot 'setup-firewall.ps1'
        $p=Start-Process powershell.exe -ArgumentList ('-NoProfile -ExecutionPolicy Bypass -File "'+$fw+'" -Action Enable -Mode Vpn') -Verb RunAs -Wait -PassThru
        if ($p.ExitCode -ne 0) { Fail 'Chưa cấp quyền firewall VPN. Bấm Thiết lập để thử lại.' }
        $vpnPort=ProxyPort
        $tail=TailState;$dns=$tail.Self.DNSName.TrimEnd('.').ToLowerInvariant()
        $serve=Native $ts @('serve','status','--json') | ConvertFrom-Json
        if (-not (ServeEmpty $serve) -and -not ($owner.serveOwned -and (OurServe $serve $owner.dns $owner.proxyPort))) { Fail 'Serve đã có cấu hình của ứng dụng khác. Không tự thay đổi.' }
        if (ServeEmpty $serve) {
            $owner.dns=$dns;$owner.proxyPort=$vpnPort;$owner.serveOwned=$true;SaveState $owner
            # Keep the native confirmation/login URL in the interactive setup window, never Unity logs.
            & $ts serve --bg --https=443 ('http://127.0.0.1:'+$vpnPort)
            if ($LASTEXITCODE -ne 0) { Fail 'Xác nhận HTTPS ở liên kết Tailscale bên trên rồi bấm Thiết lập lại.' }
        }
        $result.success=$true;$result.state='Configured';$result.message='Thiết lập xong. Bấm Bật server; nếu được hỏi, xác nhận HTTPS của Tailscale trong cửa sổ thiết lập.'
    } else {
        if (-not (Test-Path -LiteralPath $privateConfig)) { Fail 'Chưa có cấu hình host. Bấm Thiết lập máy host trước.' }
        if ($Action -eq 'Stop') {
            if ($owner.serveOwned) {
                $current=Native $ts @('serve','status','--json') | ConvertFrom-Json
                if (OurServe $current $owner.dns $owner.proxyPort) { $null=Native $ts @('serve','--https=443','off');$owner.serveOwned=$false;SaveState $owner }
                elseif (-not (ServeEmpty $current)) { Fail 'Serve đã bị thay đổi bởi ứng dụng khác. Không tự xóa cấu hình.' }
                else { $owner.serveOwned=$false;SaveState $owner }
            }
            if ($owner.stackOwned) {
                $null=Native 'docker' @('info','--format','{{.ServerVersion}}')
                $null=VerifyStack $owner
                $null=Compose @('down') 90
            }
            $result.success=$true;$result.state='Stopped';$result.message='Đã dừng stack/Serve do công cụ quản lý. Account, map và secret được giữ.'
        } else {
            $null=Native 'docker' @('info','--format','{{.ServerVersion}}')
            $ids=VerifyStack $owner
            $vpnPort=ProxyPort
            $tail=TailState;$dns=$tail.Self.DNSName.TrimEnd('.').ToLowerInvariant()
            $ip=@($tail.Self.TailscaleIPs | Where-Object { $_ -match '^100\.' })[0]
            $portLine=Get-Content -LiteralPath $privateConfig | Where-Object { $_ -match '^HOST_GAME_UDP_PORT=' } | Select-Object -First 1
            $port=if ($portLine) { [int]($portLine -split '=',2)[1] } else { 7777 }
            $code=ConnectionCode $ip $dns $port
            $serve=Native $ts @('serve','status','--json') | ConvertFrom-Json
            $empty=ServeEmpty $serve;$ours=$owner.serveOwned -and (OurServe $serve $owner.dns $owner.proxyPort) -and $owner.dns -eq $dns
            if ($Action -eq 'Start') {
                if (-not $empty -and -not $ours) { Fail 'Tailscale Serve đang phục vụ ứng dụng khác. Không ghi đè; hãy xử lý xung đột trước.' }
                if ($ours -and $owner.proxyPort -ne $vpnPort) { Fail 'Cổng proxy đã đổi. Dừng host trước rồi Bật server để áp dụng.' }
                EnsureProxyAvailable $vpnPort $owner
                $udp=Get-NetUDPEndpoint -LocalPort $port -ErrorAction SilentlyContinue
                if ($udp -and -not $ids) { Fail 'Cổng UDP đang có server khác sử dụng. Hãy tự dừng server đó hoặc đổi cổng host.' }
                # Bind only the host VPN interface; never expose this stack on the public/LAN interface.
                $text=[IO.File]::ReadAllText($privateConfig)
                if ($text -match '(?m)^HOST_GAME_UDP_BIND=') { $text=[regex]::Replace($text,'(?m)^HOST_GAME_UDP_BIND=.*$',('HOST_GAME_UDP_BIND='+$ip)) }
                else { $text+="`nHOST_GAME_UDP_BIND=$ip`n" }
                [IO.File]::WriteAllText($privateConfig,$text,[Text.UTF8Encoding]::new($false))
                $owner.stackOwned=$true;SaveState $owner
                $null=Compose @('up','-d','--build') 720
                if (-not $ours) {
                    # Persist ownership before creating the route, so a canceled setup remains recoverable.
                    $owner.dns=$dns;$owner.proxyPort=$vpnPort;$owner.serveOwned=$true;SaveState $owner
                    try { $null=Native $ts @('serve','--bg','--https=443',('http://127.0.0.1:'+$vpnPort)) 40 }
                    catch { Fail 'Chưa bật được HTTPS/Serve. Bấm Thiết lập máy host, mở Tailscale và xác nhận HTTPS rồi thử lại; stack vẫn giữ nguyên.' }
                }
            }
            $ready=$false
            for ($attempt=0;$attempt -lt $(if ($Action -eq 'Start') {12} else {1});$attempt++) {
                if (Health $dns) { $ready=$true;break };if ($Action -eq 'Start') { Start-Sleep -Seconds 2 }
            }
            $current=Native $ts @('serve','status','--json') | ConvertFrom-Json
            $running=$false
            $gameId=Compose @('ps','-q','game-server')
            if ($gameId) {
                $game=Native 'docker' @('inspect',$gameId) | ConvertFrom-Json
                $running=$game[0].State.Running -and ((Native 'docker' @('logs','--tail','30',$gameId)) -match '\[(ready|metrics)\]')
            }
            $ready=$ready -and $running -and $owner.stackOwned -and $owner.serveOwned -and (OurServe $current $dns $vpnPort)
            $result.success=$true;$result.ready=$ready;$result.state=if ($ready) {'Ready'} else {'NotReady'}
            $result.message=if ($ready) {'Server sẵn sàng. Bấm Kiểm tra / Kết nối để chơi trên host này. Chia sẻ mã và lời mời Tailscale riêng; người chơi vẫn đăng nhập game. Đóng game không tắt server.'} else {'API/HTTPS chưa sẵn sàng. Kiểm tra Docker, MagicDNS, HTTPS Tailscale và Serve. Chưa xác nhận gameplay.'}
            if ($ready) { $result.connectionCode=$code }
        }
    }
} catch {
    if ($_.Exception -is [InvalidOperationException]) { $result.message=$_.Exception.Message }
    else { $result.message='Không hoàn tất thao tác. Kiểm tra Docker/Tailscale, quyền Windows hoặc một thao tác host khác đang chạy.' }
} finally {
    if ($lock) { $lock.Dispose() }
    $json=$result | ConvertTo-Json -Compress
    if ($ResultFile) { [IO.File]::WriteAllText([IO.Path]::GetFullPath($ResultFile),$json,[Text.UTF8Encoding]::new($false)) }
    else { Write-Output $json }
}
if (-not $result.success) { if ($Action -eq 'Setup') { Write-Host $result.message; $null=Read-Host 'Nhan Enter de dong cua so va thu lai' }; exit 1 }
