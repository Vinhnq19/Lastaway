$lanManager=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../Deploy/Host/manage-lan.ps1'))
. $lanManager -LibraryOnly
$passed=0
function Check([bool]$Condition,[string]$Name){if(-not $Condition){throw $Name};$script:passed++;Write-Output ('PASS '+$Name)}
Check (PrivateIp '192.168.5.9') 'private address accepted'
Check (-not (PrivateIp '100.80.1.1')) 'VPN address excluded'
Check (-not (PrivateIp '192.168.005.9')) 'noncanonical address excluded'
function Get-NetUDPEndpoint { [pscustomobject]@{LocalAddress='0.0.0.0'} }
$denied=$false;try {Available '192.168.5.9' 7777 'UDP' @()}catch{$denied=$true};Check $denied 'occupied port refused without stopping process'
function StackItems { [pscustomobject]@{Config=@{Labels=@{'com.docker.compose.service'='game-server'};Env=@('LASTAWAY_HOST_MODE=legacy')};State=@{Running=$true}} }
$denied=$false;try{Guard ([pscustomobject]@{stackOwned=$true})}catch{$denied=$true};Check $denied 'running other mode refused even with stale LAN ownership'
function StackItems { [pscustomobject]@{Id='fixture';Config=@{Labels=@{'com.docker.compose.service'='game-server'};Env=@('LASTAWAY_HOST_MODE=lan')};State=@{Running=$true}} }
function Native { '{"safeToDeploy":false,"humanActive":1}' }
$denied=$false;try{Guard ([pscustomobject]@{stackOwned=$true}) -Changing}catch{$denied=$true};Check $denied 'active participants prevent mode/config change'
function Native { '{"safeToDeploy":true,"humanActive":0}' }
Check (@(Guard ([pscustomobject]@{stackOwned=$true}) -Changing).Count -eq 1) 'empty owned server permits configuration change'
Write-Output ($passed.ToString()+' host guard checks passed')

# Switching to VPN can adopt only stopped, same-checkout shared containers.
. (Join-Path $PSScriptRoot '../../Deploy/Host/manage-vpn.ps1') -LibraryOnly
$script:fixtureItem=[pscustomobject]@{Id='fixture';Config=@{Labels=@{'com.docker.compose.service'='game-server';'com.docker.compose.project.working_dir'=$backend};Env=@('LASTAWAY_HOST_MODE=vpn')};State=@{Running=$false}}
$script:fixtureIdle=$false
function Native([string]$Exe,[string[]]$Arguments,[int]$Seconds=30) {
    switch($Arguments[0]) {
        'ps' {return 'fixture'}
        'inspect' {return ConvertTo-Json -InputObject @($script:fixtureItem) -Depth 8 -Compress}
        'exec' {return ('{"safeToDeploy":'+$script:fixtureIdle.ToString().ToLowerInvariant()+'}')}
        default {throw 'Unexpected command in fixture'}
    }
}
$owner=[pscustomobject]@{backend=$backend;stackOwned=$false}
Check ((VerifyStack $owner) -eq 'fixture') 'VPN accepts stopped same-checkout shared container'
$fixtureItem.State.Running=$true;$fixtureItem.Config.Env=@('LASTAWAY_HOST_MODE=lan');$owner.stackOwned=$true
$denied=$false;try{VerifyStack $owner}catch{$denied=$true};Check $denied 'VPN refuses running LAN host'
$fixtureItem.Config.Env=@('LASTAWAY_HOST_MODE=vpn');$denied=$false
try{VerifyStack $owner -Changing}catch{$denied=$true};Check $denied 'VPN change refused while participants exist'
$script:fixtureIdle=$true
Check ((VerifyStack $owner -Changing) -eq 'fixture') 'VPN change permitted for empty owned host'
Write-Output ('Total '+$passed+' host script checks passed')
