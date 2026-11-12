param(
    [ValidateSet('Setup','Start','Status','Stop')][string]$Action='Status',
    [string]$LanIp='', [string]$ResultFile='', [switch]$LibraryOnly
)
$ErrorActionPreference='Stop'
# Reuse native execution/quoting/environment isolation from the existing host tool.
$lanInvocation=@{Action=$Action;ResultFile=$ResultFile;LibraryOnly=[bool]$LibraryOnly;Ip=$LanIp}
. (Join-Path $PSScriptRoot 'manage-vpn.ps1') -LibraryOnly
$Action=$lanInvocation.Action;$ResultFile=$lanInvocation.ResultFile;$LibraryOnly=$lanInvocation.LibraryOnly;$LanIp=$lanInvocation.Ip
$privateConfig=Join-Path $PSScriptRoot '.env.host.lan'
$statePath=Join-Path $PSScriptRoot '.lan-state.json'
$tlsPath=Join-Path $backend 'data/lan-tls'
function Compose([string[]]$Commands,[int]$Seconds=30) {
    Native 'docker' (@('compose','--project-name','junglemaze-host','--project-directory',$backend,'-f',(Join-Path $backend 'compose.host.yaml'),'--env-file',$privateConfig,'--profile','lan')+$Commands) $Seconds
}
function PrivateIp([string]$Ip) {
    $parsed=$null
    if (-not [Net.IPAddress]::TryParse($Ip,[ref]$parsed) -or $parsed.AddressFamily -ne 'InterNetwork' -or $parsed.ToString() -ne $Ip) { return $false }
    $b=$parsed.GetAddressBytes();return $b[0] -eq 10 -or ($b[0] -eq 172 -and $b[1] -ge 16 -and $b[1] -le 31) -or ($b[0] -eq 192 -and $b[1] -eq 168)
}
function SelectedInterface([string]$Ip) {
    if (-not (PrivateIp $Ip)) { Fail 'Chọn kết nối Wi-Fi hoặc Ethernet có địa chỉ mạng riêng.' }
    $found=@(Get-NetAdapter -Physical | Where-Object Status -eq 'Up' | ForEach-Object {
        $adapter=$_;Get-NetIPAddress -InterfaceIndex $adapter.ifIndex -AddressFamily IPv4 -ErrorAction SilentlyContinue | Where-Object IPAddress -eq $Ip
    })
    if ($found.Count -ne 1) { Fail 'Kết nối LAN đã thay đổi. Chọn lại Wi-Fi/Ethernet rồi thử lại.' }
    $profile=Get-NetConnectionProfile -InterfaceIndex $found[0].InterfaceIndex -ErrorAction Stop
    if ($profile.NetworkCategory -ne 'Private') { Fail 'Mạng này chưa được đặt là mạng riêng. Chỉ dùng mạng bạn tin cậy; đổi sang Private trong Windows rồi thiết lập lại.' }
    return $found[0]
}
function Setting([string]$Name,[string]$Default='') {
    $line=Get-Content -LiteralPath $privateConfig | Where-Object { $_.StartsWith($Name+'=') } | Select-Object -First 1
    if ($line) { return ($line -split '=',2)[1].Trim("'") };return $Default
}
function FirewallReady([string]$Ip) {
    foreach($entry in @(@{Name='LASTAWAY-LAN-Game';Port='7777';Protocol='UDP'},@{Name='LASTAWAY-LAN-Safe';Port='8443';Protocol='TCP'})) {
        $rules=@(Get-NetFirewallRule -Name $entry.Name -ErrorAction SilentlyContinue)
        if($rules.Count -ne 1 -or $rules[0].Enabled -ne 'True' -or $rules[0].Profile -ne 'Private' -or $rules[0].Action -ne 'Allow' -or $rules[0].Direction -ne 'Inbound'){return $false}
        $address=$rules[0]|Get-NetFirewallAddressFilter;$port=$rules[0]|Get-NetFirewallPortFilter
        if(@($address.LocalAddress).Count -ne 1 -or $address.LocalAddress -ne $Ip -or @($address.RemoteAddress).Count -ne 1 -or $address.RemoteAddress -ne 'LocalSubnet' -or $port.LocalPort -ne $entry.Port -or $port.Protocol -ne $entry.Protocol){return $false}
    }
    return $true
}
function ConfigFingerprint {
    $hashes=@($privateConfig,(Join-Path $backend 'compose.host.yaml'),(Join-Path $PSScriptRoot 'Caddyfile.lan')) | ForEach-Object {(Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash}
    return $hashes -join ':'
}
function State {
    if (Test-Path -LiteralPath $statePath) { return Get-Content -LiteralPath $statePath -Raw -Encoding UTF8 | ConvertFrom-Json }
    return [pscustomobject]@{schemaVersion=1;backend=$backend;stackOwned=$false;ip='';connectionCode='';fingerprint='';expiresAt=''}
}
function Save($Owner) { [IO.File]::WriteAllText($statePath,($Owner | ConvertTo-Json),[Text.UTF8Encoding]::new($false)) }
function StackItems {
    $ids=Native 'docker' @('ps','-aq','--filter','label=com.docker.compose.project=junglemaze-host')
    if (-not $ids) { return @() }
    $items=Native 'docker' (@('inspect')+@($ids -split '\s+')) | ConvertFrom-Json
    foreach ($item in $items) {
        $dir=$item.Config.Labels.'com.docker.compose.project.working_dir'
        if (-not $dir -or $dir.Replace('\','/').TrimEnd('/') -ine $backend.Replace('\','/').TrimEnd('/')) { Fail 'Có máy chủ ở source khác. Không can thiệp; hãy tự dừng hoặc chọn đúng source.' }
    }
    return @($items)
}
function Guard($Owner,[switch]$Changing) {
    $items=@(StackItems)
    foreach ($item in $items | Where-Object {$_.State.Running}) {
        $service=$item.Config.Labels.'com.docker.compose.service'
        if (($service -eq 'game-server' -and $item.Config.Env -notcontains 'LASTAWAY_HOST_MODE=lan') -or -not $Owner.stackOwned -or $service -notin @('mongo','api','game-server','caddy-lan')) { Fail 'Đang có máy chủ khác chạy. Hãy dừng chế độ hiện tại trước khi bật LAN.' }
    }
    $game=@($items | Where-Object {$_.Config.Labels.'com.docker.compose.service' -eq 'game-server' -and $_.State.Running})
    if ($game.Count -and $Changing) {
        $status=Native 'docker' @('exec',$game[0].Id,'dotnet','GameServer.dll','--lan-status') | ConvertFrom-Json
        if ($status.safeToDeploy -ne $true) { Fail 'Máy chủ còn người chơi hoặc trận chưa kết thúc. Kết thúc buổi hiện tại trước khi thay cấu hình.' }
    }
    return $items
}
function ProtectDirectory([string]$Path) {
    $sid=[Security.Principal.WindowsIdentity]::GetCurrent().User;$acl=Get-Acl -LiteralPath $Path
    $acl.SetAccessRuleProtection($true,$false);$acl.SetOwner($sid)
    foreach ($identity in @($sid,[Security.Principal.SecurityIdentifier]::new('S-1-5-18'))) {
        $acl.AddAccessRule([Security.AccessControl.FileSystemAccessRule]::new($identity,'FullControl','ContainerInherit,ObjectInherit','None','Allow'))
    }
    Set-Acl -LiteralPath $Path -AclObject $acl
}
function Available([string]$Ip,[int]$Port,[string]$Protocol,$Items) {
    $occupied=if ($Protocol -eq 'UDP') { @(Get-NetUDPEndpoint -LocalPort $Port -ErrorAction SilentlyContinue) } else { @(Get-NetTCPConnection -State Listen -LocalPort $Port -ErrorAction SilentlyContinue) }
    $occupied=@($occupied | Where-Object {$_.LocalAddress -eq $Ip -or $_.LocalAddress -in @('0.0.0.0','::')})
    if (-not $occupied.Count) { return }
    $service=if ($Protocol -eq 'UDP') {'game-server'} else {'caddy-lan'}
    $portKey=if($Protocol -eq 'UDP') {'7777/udp'} else {'8443/tcp'}
    $owned=@($Items | Where-Object {$_.State.Running -and $_.Config.Labels.'com.docker.compose.service' -eq $service -and @($_.HostConfig.PortBindings.$portKey | Where-Object {$_.HostIp -eq $Ip -and $_.HostPort -eq [string]$Port}).Count -eq 1})
    if (-not $owned.Count) { Fail ('Cổng '+$Port+' đang được ứng dụng khác dùng. Không tự dừng ứng dụng đó.') }
}
if ($LibraryOnly) { return }
$result=[ordered]@{success=$false;ready=$false;state='NeedsAttention';message='';connectionCode=''}
$lock=$null
try {
    $lock=[IO.File]::Open((Join-Path $PSScriptRoot '.host.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
    if($Action -eq 'Setup' -and -not (Get-Command docker -ErrorAction SilentlyContinue)) {
        Start-Process 'https://www.docker.com/products/docker-desktop/'
        Fail 'Cần cài Docker Desktop trước. Đã mở hướng dẫn cài đặt; hoàn tất rồi thử Thiết lập lại.'
    }
    $null=Native 'docker' @('info','--format','{{.ServerVersion}}')
    $owner=State
    if ($owner.backend -ne $backend) { Fail 'Cấu hình host thuộc source khác; không can thiệp.' }
    if ($Action -eq 'Setup') {
        $null=SelectedInterface $LanIp
        $null=Guard $owner -Changing
        if (-not (Test-Path -LiteralPath $privateConfig)) {
            & (Join-Path $PSScriptRoot 'configure-host.ps1') -Mode lan
            Add-Content -LiteralPath $privateConfig -Encoding UTF8 -Value @('HOST_MODE=lan',('HOST_LAN_IP='+$LanIp),'HOST_LAN_HTTPS_PORT=8443')
            $text=[IO.File]::ReadAllText($privateConfig);$text=[regex]::Replace($text,'(?m)^HOST_GAME_UDP_BIND=.*$',('HOST_GAME_UDP_BIND='+$LanIp))
            [IO.File]::WriteAllText($privateConfig,$text,[Text.UTF8Encoding]::new($false))
        }
        $text=[IO.File]::ReadAllText($privateConfig)
        foreach($entry in @{PUBLIC_HOSTING='true';ALLOW_LOCAL_HTTP='false';HOST_MODE='lan'}.GetEnumerator()) {
            if($text -match ('(?m)^'+$entry.Key+'=')){$text=[regex]::Replace($text,'(?m)^'+$entry.Key+'=.*$',($entry.Key+'='+$entry.Value))}else{$text+="`n"+$entry.Key+'='+$entry.Value+"`n"}
        }
        [IO.File]::WriteAllText($privateConfig,$text,[Text.UTF8Encoding]::new($false))
        New-Item -ItemType Directory -Force -Path $tlsPath | Out-Null;ProtectDirectory $tlsPath
        $null=Compose @('build','api','game-server') 900
        # Only the narrowly scoped firewall change requests elevation. No CA is installed on any PC.
        $fw=Join-Path $PSScriptRoot 'setup-lan-firewall.ps1'
        $args='-NoProfile -ExecutionPolicy Bypass -File "'+$fw+'" -LanIp '+$LanIp
        $admin=([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
        if ($admin) { & $fw -LanIp $LanIp } else { $p=Start-Process powershell.exe -Verb RunAs -ArgumentList $args -PassThru -Wait;if ($p.ExitCode -ne 0) { Fail 'Chưa cấp quyền mở kết nối LAN. Thiết lập được giữ; thử lại khi sẵn sàng.' } }
        $result.success=$true;$result.state='Configured';$result.message='Thiết lập LAN xong. Bấm Bật máy chủ để chơi.'
    } elseif (-not (Test-Path -LiteralPath $privateConfig)) { Fail 'Bấm Thiết lập máy chủ LAN lần đầu trước.' }
    elseif ($Action -eq 'Stop') {
        $null=Guard $owner -Changing
        if ($owner.stackOwned) { $null=Compose @('stop','caddy-lan','game-server','api','mongo') 90 }
        $result.success=$true;$result.state='Stopped';$result.message='Đã dừng máy chủ LAN; tài khoản và bản đồ được giữ.'
    } else {
        if (-not $LanIp) { $LanIp=Setting 'HOST_LAN_IP' }
        $null=SelectedInterface $LanIp
        $expires=if($owner.PSObject.Properties['expiresAt'] -and $owner.expiresAt){[DateTime]::Parse($owner.expiresAt).ToUniversalTime()}else{[DateTime]::MinValue}
        $changing=$Action -eq 'Start' -and ($owner.ip -ne $LanIp -or $owner.fingerprint -ne (ConfigFingerprint) -or $expires -le [DateTime]::UtcNow.AddDays(1))
        $items=@(Guard $owner -Changing:$changing)
        if ($Action -eq 'Start') {
            if(-not (FirewallReady $LanIp)){Fail 'Cần Thiết lập lại để cho phép địa chỉ LAN đã chọn. Công cụ chỉ mở kết nối trên mạng Private.'}
            Available $LanIp 7777 'UDP' $items;Available $LanIp 8443 'TCP' $items
            $text=[IO.File]::ReadAllText($privateConfig)
            foreach ($entry in @{HOST_GAME_UDP_BIND=$LanIp;HOST_LAN_IP=$LanIp}.GetEnumerator()) {
                if($text -match ('(?m)^'+$entry.Key+'=')){$text=[regex]::Replace($text,'(?m)^'+$entry.Key+'=.*$',($entry.Key+'='+$entry.Value))}else{$text+="`n"+$entry.Key+'='+$entry.Value+"`n"}
            }
            [IO.File]::WriteAllText($privateConfig,$text,[Text.UTF8Encoding]::new($false))
            # An unchanged running host keeps its certificate, sessions and room codes.
            $game=@($items | Where-Object {$_.State.Running -and $_.Config.Labels.'com.docker.compose.service' -eq 'game-server'})
            if (-not $game.Count -or $changing) {
                $cert=Compose @('run','--rm','--no-deps','-v',($tlsPath+':/lan-tls'),'game-server','--lan-cert','/lan-tls',$LanIp,'7777','8443') 60 | ConvertFrom-Json
                $previousCode=$owner.connectionCode
                $owner.connectionCode=$cert.connectionCode;$owner.ip=$LanIp;$owner.stackOwned=$true
                if(-not $owner.PSObject.Properties['expiresAt']){$owner|Add-Member -NotePropertyName expiresAt -NotePropertyValue ''}
                $owner.expiresAt=$cert.expiresAt;Save $owner
                $null=Compose @('up','-d','--no-build','mongo','api','game-server','caddy-lan') 180
                if($previousCode -and $previousCode -ne $owner.connectionCode){$null=Compose @('up','-d','--no-deps','--force-recreate','caddy-lan') 60}
                $owner.fingerprint=ConfigFingerprint;Save $owner
            }
        }
        $gameId=Compose @('ps','-q','game-server')
        $ready=$false
        if ($owner.connectionCode -and $gameId) {
            for($attempt=0;$attempt -lt 4;$attempt++) {
                try { $probe=Native 'docker' @('exec',$gameId,'dotnet','GameServer.dll','--lan-probe',$owner.connectionCode) 15 | ConvertFrom-Json;$ready=$probe.ready -eq $true } catch { $ready=$false }
                if($ready -or $Action -ne 'Start'){break};Start-Sleep -Seconds 2
            }
        }
        $result.success=$true;$result.ready=$ready;$result.state=if($ready){'Ready'}else{'NotReady'}
        $result.message=if($ready){'Máy chủ LAN sẵn sàng. Sao chép mã gửi cho bạn; đóng game không tắt máy chủ.'}else{'Máy chủ chưa sẵn sàng. Kiểm tra Docker và địa chỉ mạng rồi thử lại.'}
        if($ready){$result.connectionCode=$owner.connectionCode}
    }
} catch {
    $result.message=if($_.Exception -is [InvalidOperationException]){$_.Exception.Message}else{'Chưa hoàn tất thiết lập LAN. Kiểm tra Docker, quyền Windows và cửa sổ thiết lập rồi thử lại.'}
} finally {
    if($lock){$lock.Dispose()}
    $json=$result|ConvertTo-Json -Compress
    if($ResultFile){[IO.File]::WriteAllText([IO.Path]::GetFullPath($ResultFile),$json,[Text.UTF8Encoding]::new($false))}else{Write-Output $json}
}
if(-not $result.success){if($Action -eq 'Setup'){$null=Read-Host 'Nhan Enter de dong cua so'};exit 1}
