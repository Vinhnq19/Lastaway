[CmdletBinding()]
param(
    [ValidateSet('PrepareOnly','Deploy','Rollback')][string]$Mode='PrepareOnly',
    [string]$Release='', [string]$ReleaseId='', [string]$Player='', [string]$LocalEvidence='',
    [ValidateSet('Auto','Full')][string]$TestMode='Auto',
    [switch]$VerifyLocal, [switch]$ConfirmEmptyMigration,
    [string]$Domain='junglemaze.duckdns.org', [string]$LocalApi='http://127.0.0.1:5000',
    [string]$AwsCli='D:/AWS/ConnectionSetup/AWSCLI-portable/aws.exe',
    [string]$Profile='junglemaze-admin', [string]$Region='ap-southeast-2',
    [string]$InstanceId='i-0e617f8072fbb9878', [string]$SshAlias='junglemaze',
    [ValidateRange(5,20)][int]$BudgetMinutes=20,
    [string]$OutputDirectory='', [string]$Python='python'
)
$ErrorActionPreference='Stop'
$arguments=@((Join-Path $PSScriptRoot 'deploy_registry.py'),'--mode',$Mode,'--test-mode',$TestMode,
    '--domain',$Domain,'--local-api',$LocalApi,'--aws-cli',$AwsCli,'--profile',$Profile,'--region',$Region,
    '--instance-id',$InstanceId,'--ssh-alias',$SshAlias,'--budget-minutes',"$BudgetMinutes")
foreach ($entry in @(@('--release',$Release),@('--release-id',$ReleaseId),@('--player',$Player),
    @('--local-evidence',$LocalEvidence),@('--output',$OutputDirectory))) {
    if ($entry[1]) { $arguments += $entry }
}
if ($VerifyLocal) { $arguments += '--verify-local' }
if ($ConfirmEmptyMigration) { $arguments += '--confirm-empty-migration' }
& $Python @arguments
if ($LASTEXITCODE -ne 0) { throw 'Registry deploy did not pass. Read the report; EC2 lifecycle was not changed.' }
