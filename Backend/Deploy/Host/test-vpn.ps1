$ErrorActionPreference='Stop'
. "$PSScriptRoot/manage-vpn.ps1" -LibraryOnly
$count=0
function Assert($Ok,$Message) { if (-not $Ok) { throw $Message };$script:count++ }
$empty='{}' | ConvertFrom-Json
$ours='{"TCP":{"443":{"HTTPS":true}},"Web":{"host.tail.ts.net:443":{"Handlers":{"/":{"Proxy":"http://127.0.0.1:18080"}}}}}' | ConvertFrom-Json
Assert (ServeEmpty $empty) 'Empty Serve'
Assert (-not (ServeEmpty $ours)) 'Existing Serve detected'
Assert (OurServe $ours 'host.tail.ts.net') 'Owned exact route'
Assert (-not (OurServe $ours 'other.tail.ts.net')) 'Different hostname'
$ours | Add-Member -NotePropertyName AllowFunnel -NotePropertyValue ([pscustomobject]@{'host.tail.ts.net:443'=$false})
Assert (OurServe $ours 'host.tail.ts.net') 'Explicit false Funnel flag is still private Serve'
$ours.AllowFunnel.'host.tail.ts.net:443'=$true
Assert (-not (OurServe $ours 'host.tail.ts.net')) 'Public Funnel cannot be adopted'
$ours.AllowFunnel.'host.tail.ts.net:443'=$false
$other='{"TCP":{"443":{"HTTPS":true}},"Web":{"host.tail.ts.net:443":{"Handlers":{"/":{"Proxy":"http://127.0.0.1:9090"}}}}}' | ConvertFrom-Json
Assert (-not (OurServe $other 'host.tail.ts.net')) 'Other app proxy protected'
$extra='{"TCP":{"443":{"HTTPS":true}},"Web":{"host.tail.ts.net:443":{"Handlers":{"/":{"Proxy":"http://127.0.0.1:18080"},"/other":{"Proxy":"http://127.0.0.1:9090"}}}}}' | ConvertFrom-Json
Assert (-not (OurServe $extra 'host.tail.ts.net')) 'Additional route protected'
$foreground='{"Foreground":{"other":{"TCP":{"443":{"HTTPS":true}}}}}' | ConvertFrom-Json
Assert (-not (ServeEmpty $foreground)) 'Foreground Serve is not empty'
$ours | Add-Member -NotePropertyName Foreground -NotePropertyValue $foreground.Foreground
Assert (-not (OurServe $ours 'host.tail.ts.net')) 'Foreground Serve must not be overwritten'
$ours.PSObject.Properties.Remove('Foreground')
$secure=ConvertTo-SecureString 'test-fixture-only' -AsPlainText -Force;Assert ($null -ne $secure) 'Windows5 Security module loads despite inherited PS7 paths';$secure.Dispose()
$code=ConnectionCode '100.80.1.20' 'host.tail.ts.net' 7777
Assert ($code -eq 'host.tail') 'Short endpoint'
Assert ((ConnectionCode '100.80.1.20' 'host.tail.ts.net' 7788) -eq 'host.tail:7788') 'Custom port'
foreach ($case in @(@('192.168.1.1','host.tail.ts.net',7777),@('100.128.1.1','host.tail.ts.net',7777),@('100.80.1.20','evil.com',7777),@('100.80.1.20','host.tail.ts.net',0))) {
 $rejected=$false;try { $null=ConnectionCode $case[0] $case[1] $case[2] } catch { $rejected=$true }
 Assert $rejected 'Invalid endpoints rejected'
}
# A canned container inspect prevents touching any live Docker or Tailscale state.
$script:fakeInspect=@{Config=@{Labels=@{'com.docker.compose.project.working_dir'=$backend;'com.docker.compose.service'='game-server'}}} | ConvertTo-Json -Depth 8
function Native([string]$Exe,[string[]]$Arguments,[int]$Seconds=30) { if ($Arguments[0] -eq 'ps') { return 'test-container' };return ('['+$script:fakeInspect+']') }
$owner=[pscustomobject]@{backend=$backend;stackOwned=$false}
$blocked=$false;try { VerifyStack $owner } catch { $blocked=$true };Assert $blocked 'Manual stack cannot be adopted/stopped'
$owner.stackOwned=$true;Assert ((VerifyStack $owner) -eq 'test-container') 'Owned same checkout allowed'
$script:fakeInspect=@{Config=@{Labels=@{'com.docker.compose.project.working_dir'=$backend;'com.docker.compose.service'='caddy'}}} | ConvertTo-Json -Depth 8
$blocked=$false;try { VerifyStack $owner } catch { $blocked=$true };Assert $blocked 'Public proxy must not be stopped by VPN tool'
$owner.backend='D:/other';$blocked=$false;try { VerifyStack $owner } catch {$blocked=$true};Assert $blocked 'Other checkout rejected'
function Get-NetTCPConnection { param($State,$LocalPort,$ErrorAction); return [pscustomobject]@{LocalPort=$LocalPort} }
function Compose { param([string[]]$Commands,[int]$Seconds=30);return '' }
$blocked=$false;try { EnsureProxyAvailable 18080 $owner } catch { $blocked=$true };Assert $blocked 'Occupied proxy port cannot be exposed through Serve'
$parseErrors=$null;$null=[Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot 'manage-vpn.ps1'),[ref]$null,[ref]$parseErrors)
Assert ($parseErrors.Count -eq 0) 'PS5 syntax'
Write-Output "PASS: $count VPN host safety checks (no live mutations)."
