<#
.SYNOPSIS
    One-time provisioning for the Hyper-V UI-test VM. Run INSIDE the VM, once,
    after Windows install, as the test user (tmtest) in an elevated PowerShell.

.DESCRIPTION
    Prepares the VM to host the interactive L3 FlaUI runner:
      - kills UI animations / transparency (FlaUI flakiness + speed)
      - prevents lock / screensaver / sleep (UIA dies on a locked desktop)
      - disables services unnecessary on a dedicated test VM (less noise, fewer
        surprise reboots)

    Everything here is reversible and safe to re-run (idempotent). It does NOT
    install .NET/Git or register the runner — those are separate steps (see README).

    Effect on tests: positive only. None of these change the control tree or
    AutomationIds; they remove timing flakiness (animations) and stability risks
    (lock/sleep/reboots). Disabling animations makes runs faster and deterministic.

.PARAMETER KeepWindowsUpdate
    Leave Windows Update on Automatic. By default it is set to Manual so updates
    do not reboot the VM mid-suite. (The VM is snapshotted; patch on your schedule.)

.EXAMPLE
    .\vm-configure-test-session.ps1
#>
[CmdletBinding()]
param(
    [switch] $KeepWindowsUpdate
)

$ErrorActionPreference = 'Stop'

function Test-Admin {
    $id = [Security.Principal.WindowsIdentity]::GetCurrent()
    return (New-Object Security.Principal.WindowsPrincipal($id)).IsInRole(
        [Security.Principal.WindowsBuiltInRole]::Administrator)
}
if (-not (Test-Admin)) { throw 'Run elevated (Administrator) inside the VM as tmtest.' }

function Set-Reg($path, $name, $value, $type = 'DWord') {
    if (-not (Test-Path $path)) { New-Item -Path $path -Force | Out-Null }
    New-ItemProperty -Path $path -Name $name -Value $value -PropertyType $type -Force | Out-Null
}

# --- 1. UI animations / visual effects off (FlaUI determinism + speed) --------

Write-Host 'Disabling UI animations and visual effects...'
Set-Reg 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects' 'VisualFXSetting' 2
Set-Reg 'HKCU:\Control Panel\Desktop\WindowMetrics' 'MinAnimate' '0' 'String'
Set-Reg 'HKCU:\Control Panel\Desktop' 'MenuShowDelay' '0' 'String'
Set-Reg 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize' 'EnableTransparency' 0
# UserPreferencesMask: best-performance animation profile
Set-Reg 'HKCU:\Control Panel\Desktop' 'UserPreferencesMask' ([byte[]](0x90,0x12,0x01,0x80,0x10,0x00,0x00,0x00)) 'Binary'

# --- 2. No lock / no screensaver / no sleep (UIA needs a live desktop) --------

Write-Host 'Disabling lock, screensaver, and sleep...'
Set-Reg 'HKCU:\Control Panel\Desktop' 'ScreenSaveActive' '0' 'String'
Set-Reg 'HKCU:\Control Panel\Desktop' 'ScreenSaveTimeOut' '0' 'String'
# No lock screen / no machine inactivity lock
Set-Reg 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\Personalization' 'NoLockScreen' 1
Set-Reg 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System' 'InactivityTimeoutSecs' 0
powercfg /change monitor-timeout-ac 0
powercfg /change monitor-timeout-dc 0
powercfg /change standby-timeout-ac 0
powercfg /change standby-timeout-dc 0
powercfg /change hibernate-timeout-ac 0
powercfg /change hibernate-timeout-dc 0

# --- 3. Disable services unnecessary on a dedicated test VM -------------------

# Safe to disable on an isolated, snapshotted UI-test box. Each guarded so a
# missing service on an edition (e.g. no Xbox) does not abort the run.
$servicesToDisable = @(
    'DiagTrack',        # Connected User Experiences and Telemetry
    'SysMain',          # SuperFetch - pointless in a VM
    'WSearch',          # Windows Search indexer - app does not need it
    'MapsBroker',       # Downloaded Maps Manager
    'Fax',
    'Spooler',          # Print Spooler - no printing on a test box
    'XblAuthManager',   # Xbox Live Auth
    'XblGameSave',      # Xbox Live Game Save
    'XboxGipSvc',       # Xbox Accessory Management
    'XboxNetApiSvc',    # Xbox Live Networking
    'RetailDemo'        # Retail Demo
)
foreach ($svc in $servicesToDisable) {
    try {
        $s = Get-Service -Name $svc -ErrorAction Stop
        Stop-Service -Name $svc -Force -ErrorAction SilentlyContinue
        Set-Service  -Name $svc -StartupType Disabled
        Write-Host "  disabled: $svc"
    } catch {
        Write-Host "  skipped (absent): $svc"
    }
}

# Windows Update -> Manual so it does not reboot mid-suite (unless asked to keep)
if (-not $KeepWindowsUpdate) {
    try {
        Stop-Service -Name wuauserv -Force -ErrorAction SilentlyContinue
        Set-Service  -Name wuauserv -StartupType Manual
        Write-Host '  Windows Update set to Manual (no surprise reboots)'
    } catch { Write-Host '  could not adjust Windows Update' }
}

Write-Host ''
Write-Host 'Done. Reboot (or log off/on) to apply animation + lock settings.'
Write-Host 'Remaining manual steps (see Scripts/README.md): auto-logon (netplwiz),'
Write-Host 'install .NET SDK + Git, register the GitHub runner (label ui-tests) interactively.'
