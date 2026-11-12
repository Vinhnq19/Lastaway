param([Parameter(Mandatory=$true)][string]$LanIp)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'manage-lan.ps1') -LibraryOnly -LanIp $LanIp
$nic=SelectedInterface $LanIp
$principal=[Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()
if(-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)){throw 'Cần quyền Administrator cho kết nối LAN.'}
foreach($rule in @(@{Name='LASTAWAY-LAN-Game';Protocol='UDP';Port=7777},@{Name='LASTAWAY-LAN-Safe';Protocol='TCP';Port=8443})) {
    $existing=@(Get-NetFirewallRule -Name $rule.Name -ErrorAction SilentlyContinue)
    if(@($existing | Where-Object {$_.Group -ne 'LASTAWAY-LAN' -or $_.Profile -ne 'Private'}).Count) {
        throw 'Có rule firewall trùng tên nhưng không thuộc cấu hình LAN riêng. Giữ nguyên rule; nhờ quản trị viên xử lý xung đột.'
    }
    $existing | Remove-NetFirewallRule
    New-NetFirewallRule -Name $rule.Name -DisplayName $rule.Name -Group 'LASTAWAY-LAN' -Profile Private -Direction Inbound -Action Allow -Protocol $rule.Protocol -LocalPort $rule.Port -LocalAddress $LanIp -RemoteAddress LocalSubnet -InterfaceAlias $nic.InterfaceAlias | Out-Null
}
