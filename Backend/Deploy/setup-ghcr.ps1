[CmdletBinding()]
param([ValidateSet('PC','EC2')][string]$Target='PC', [string]$Username='vinhnq19', [string]$SshAlias='junglemaze')
$ErrorActionPreference='Stop'
if ($Username -notmatch '^[A-Za-z0-9-]+$' -or $SshAlias -notmatch '^[A-Za-z0-9_-]+$') { throw 'Invalid account/SSH alias.' }
if ($Target -eq 'PC') {
    $configFile=if ($env:DOCKER_CONFIG) { Join-Path $env:DOCKER_CONFIG 'config.json' } else { Join-Path $env:USERPROFILE '.docker/config.json' }
    if (-not [IO.File]::Exists($configFile)) { throw 'Start Docker Desktop first; a Docker credential store is required.' }
    $config=[IO.File]::ReadAllText($configFile) | ConvertFrom-Json
    if (-not $config.credsStore -and -not $config.credHelpers.'ghcr.io') { throw 'Enable Docker Desktop credential store before login.' }
}
Write-Host $(if ($Target -eq 'PC') {'Use a classic PAT with write:packages (includes read:packages).'} else {'Use a separate classic PAT with ONLY read:packages. EC2 must already be Running.'})
$secret=Read-Host 'GHCR token (hidden; never paste into chat)' -AsSecureString
$pointer=[IntPtr]::Zero
try {
    $pointer=[Runtime.InteropServices.Marshal]::SecureStringToBSTR($secret)
    $plain=[Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer)
    # Check scopes without persisting/logging the token or HTTP response body.
    $headers=@{Authorization=('Bearer '+$plain);'User-Agent'='Lastaway-Registry-Setup'}
    $response=Invoke-WebRequest -UseBasicParsing -Uri 'https://api.github.com/user' -Headers $headers
    $user=$response.Content | ConvertFrom-Json
    if ($user.login -ne $Username) { throw 'PAT belongs to a different GitHub user.' }
    $scopes=@($response.Headers['X-OAuth-Scopes'] -split ',\s*')
    if ($Target -eq 'EC2' -and (@($scopes | Where-Object { $_ -and $_ -ne 'read:packages' }).Count -or $scopes -notcontains 'read:packages')) { throw 'EC2 requires a token scoped only to read:packages.' }
    if ($Target -eq 'PC' -and ($scopes -notcontains 'write:packages' -or @($scopes | Where-Object { $_ -and $_ -notin @('write:packages','read:packages') }).Count)) { throw 'PC requires only write:packages and read:packages.' }
    $start=[Diagnostics.ProcessStartInfo]::new()
    $start.UseShellExecute=$false;$start.RedirectStandardInput=$true;$start.RedirectStandardOutput=$true;$start.RedirectStandardError=$true
    if ($Target -eq 'PC') { $start.FileName='docker';$start.Arguments="login ghcr.io --username $Username --password-stdin" }
    else {
        $start.FileName='ssh';$start.Arguments="-o BatchMode=yes $SshAlias sudo sh -c 'umask 077; mkdir -p /root/.docker; chmod 700 /root/.docker; docker login ghcr.io --username $Username --password-stdin; result=`$?; chmod 600 /root/.docker/config.json; exit `$result'"
    }
    $process=[Diagnostics.Process]::Start($start)
    $process.StandardInput.WriteLine($plain);$process.StandardInput.Close()
    $outTask=$process.StandardOutput.ReadToEndAsync();$errTask=$process.StandardError.ReadToEndAsync()
    if (-not $process.WaitForExit(90000)) { $process.Kill();throw 'Registry login timed out.' }
    $null=$outTask.GetAwaiter().GetResult();$null=$errTask.GetAwaiter().GetResult()
    if ($process.ExitCode -ne 0) { throw 'GHCR login failed. Check token/permissions and SSH alias. No token was printed.' }
    Write-Host "GHCR login configured for $Target. Token was not written to this project."
} catch {
    throw 'GHCR setup failed. Check classic PAT scopes, account and SSH access; no credential was printed.'
} finally {
    $plain=$null;$headers=$null;$response=$null
    if ($pointer -ne [IntPtr]::Zero) { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer) }
    $secret.Dispose()
}
