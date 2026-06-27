<#
.SYNOPSIS
    Verifies the L3 UI-test VM is fully provisioned. Run INSIDE the VM (elevated).
    Read-only — checks state, changes nothing. Prints a PASS/FAIL checklist.

.DESCRIPTION
    Confirms every required piece is in place before relying on the VM as an L3
    runner: auto-logon, no-lock/animations, disabled services, .NET 10 SDK, Git,
    and the GitHub Actions runner (registered + interactive auto-start).

.PARAMETER RunnerPath
    Folder containing the runner. Default: C:\actions-runner.

.EXAMPLE
    .\vm-verify-setup.ps1
#>
[CmdletBinding()]
param(
    [string] $RunnerPath = 'C:\actions-runner'
)

$results = @()
function Check($name, [bool]$ok, $detail = '') {
    $script:results += [pscustomobject]@{ Name = $name; Ok = $ok; Detail = $detail }
}

# --- Auto-logon --------------------------------------------------------------
$w = 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon'
$wl = Get-ItemProperty $w -ErrorAction SilentlyContinue
Check 'AutoAdminLogon = 1' ($wl.AutoAdminLogon -eq '1') "value=$($wl.AutoAdminLogon)"
Check 'DefaultUserName set' (-not [string]::IsNullOrWhiteSpace($wl.DefaultUserName)) "user=$($wl.DefaultUserName)"
Check 'No logon banner (LegalNoticeCaption empty)' ([string]::IsNullOrEmpty($wl.LegalNoticeCaption))
Check 'No AutoLogonCount gate' ($null -eq $wl.AutoLogonCount)

$pl = Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\PasswordLess\Device' -ErrorAction SilentlyContinue
Check 'DevicePasswordLessBuildVersion = 0' ($pl.DevicePasswordLessBuildVersion -eq 0) "value=$($pl.DevicePasswordLessBuildVersion)"

# --- Visual effects / no-lock ------------------------------------------------
$vfx = (Get-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects' -ErrorAction SilentlyContinue).VisualFXSetting
Check 'Animations off (VisualFXSetting = 2)' ($vfx -eq 2) "value=$vfx"
$ss = (Get-ItemProperty 'HKCU:\Control Panel\Desktop' -ErrorAction SilentlyContinue).ScreenSaveActive
Check 'Screensaver off' ($ss -eq '0' -or $null -eq $ss) "value=$ss"

# --- Disabled services -------------------------------------------------------
foreach ($svc in 'DiagTrack','SysMain','WSearch') {
    $s = Get-Service -Name $svc -ErrorAction SilentlyContinue
    if ($s) { Check "Service $svc disabled" ($s.StartType -eq 'Disabled') "startType=$($s.StartType)" }
    else    { Check "Service $svc disabled" $true 'absent' }
}

# --- Tooling -----------------------------------------------------------------
$env:Path = [Environment]::GetEnvironmentVariable('Path','Machine') + ';' + [Environment]::GetEnvironmentVariable('Path','User')
$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
$dotnetVer = if ($dotnet) { (& dotnet --version) } else { '' }
Check '.NET SDK installed (10.x)' ($dotnetVer -like '10.*') "version=$dotnetVer"
$git = Get-Command git -ErrorAction SilentlyContinue
$gitVer = if ($git) { (& git --version) } else { 'not found' }
Check 'Git installed' ($null -ne $git) $gitVer

# --- Runner ------------------------------------------------------------------
Check 'Runner folder exists' (Test-Path $RunnerPath) $RunnerPath
Check 'Runner run.cmd present' (Test-Path (Join-Path $RunnerPath 'run.cmd'))
Check 'Runner registered (.runner config)' (Test-Path (Join-Path $RunnerPath '.runner'))
$startupLnk = Join-Path ([Environment]::GetFolderPath('Startup')) 'github-actions-runner.lnk'
Check 'Runner auto-start shortcut present' (Test-Path $startupLnk) $startupLnk
$runnerSvc = Get-Service -Name 'actions.runner.*' -ErrorAction SilentlyContinue
$svcDetail = if ($runnerSvc) { 'service found - WRONG (session 0, no desktop)' } else { 'ok' }
Check 'Runner NOT installed as service' ($null -eq $runnerSvc) $svcDetail

# --- Report ------------------------------------------------------------------
Write-Host ''
foreach ($r in $results) {
    $mark = if ($r.Ok) { '[PASS]' } else { '[FAIL]' }
    $line = "{0} {1}" -f $mark, $r.Name
    if ($r.Detail) { $line += "  ($($r.Detail))" }
    if ($r.Ok) { Write-Host $line -ForegroundColor Green } else { Write-Host $line -ForegroundColor Red }
}
$failed = ($results | Where-Object { -not $_.Ok }).Count
Write-Host ''
if ($failed -eq 0) {
    Write-Host 'ALL CHECKS PASSED. VM is ready as an L3 runner.' -ForegroundColor Green
} else {
    Write-Host "$failed check(s) FAILED — see Scripts/README.md for the matching step." -ForegroundColor Red
    exit 1
}
