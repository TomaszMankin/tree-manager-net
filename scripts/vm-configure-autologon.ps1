<#
.SYNOPSIS
    Configures unattended auto-logon for the L3 UI-test VM, using Sysinternals
    Autologon (encrypted LSA secret). Run INSIDE the VM (elevated), once.

.DESCRIPTION
    FlaUI needs the VM to boot straight into an interactive desktop with no logon
    prompt. The reliable way on modern Windows 11 is Sysinternals Autologon, which
    stores the password as an encrypted LSA secret (plain DefaultPassword in the
    registry is unreliable on current Win11).

    This script:
      1. Sets DevicePasswordLessBuildVersion = 0 (stops Win11 forcing Hello-first,
         which blocks password auto-logon).
      2. Downloads Sysinternals Autologon.
      3. Arms auto-logon for the given local account.

    IMPORTANT (the gotcha that cost us hours): auto-logon lands on the VM's CONSOLE
    session. Hyper-V "Enhanced Session" mode connects over RDP and shows its OWN
    login prompt in a SEPARATE session, hiding the auto-logged-in console. To see /
    use the auto-logon desktop (and the session the runner drives), use BASIC
    SESSION in VMConnect (View menu > Enhanced Session = OFF). Use Enhanced Session
    only when you need clipboard for setup.

.PARAMETER Username
    Local account to auto-logon. Default: the current user.

.PARAMETER Password
    Account password. If omitted you are prompted (kept out of shell history).
    Do NOT pass this literally in a committed script or CI log.

.EXAMPLE
    .\vm-configure-autologon.ps1                       # prompts for password
.EXAMPLE
    .\vm-configure-autologon.ps1 -Username user        # prompts for password
#>
[CmdletBinding()]
param(
    [string] $Username = $env:USERNAME,
    [System.Security.SecureString] $Password
)

$ErrorActionPreference = 'Stop'

function Test-Admin {
    $id = [Security.Principal.WindowsIdentity]::GetCurrent()
    return (New-Object Security.Principal.WindowsPrincipal($id)).IsInRole(
        [Security.Principal.WindowsBuiltInRole]::Administrator)
}
if (-not (Test-Admin)) { throw 'Run elevated (Administrator) inside the VM.' }

if (-not $Password) {
    $Password = Read-Host "Password for $Username" -AsSecureString
}
# Plain text needed only transiently for the Autologon CLI
$plain = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
    [Runtime.InteropServices.Marshal]::SecureStringToBSTR($Password))

# --- 1. Stop Win11 Hello-first enforcement -----------------------------------

Set-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\PasswordLess\Device' `
    DevicePasswordLessBuildVersion 0
Write-Host 'DevicePasswordLessBuildVersion = 0 (password sign-in allowed).'

# --- 2. Download Sysinternals Autologon --------------------------------------

$dir = Join-Path $env:TEMP 'AutoLogon'
$zip = Join-Path $env:TEMP 'AutoLogon.zip'
Write-Host 'Downloading Sysinternals Autologon...'
Invoke-WebRequest 'https://download.sysinternals.com/files/AutoLogon.zip' -OutFile $zip
Expand-Archive $zip -DestinationPath $dir -Force
$exe = Join-Path $dir 'Autologon64.exe'
if (-not (Test-Path $exe)) { $exe = Join-Path $dir 'Autologon.exe' }

# --- 3. Arm auto-logon (encrypted LSA secret) --------------------------------

Write-Host "Arming auto-logon for $Username ..."
& $exe $Username $env:COMPUTERNAME $plain /accepteula | Out-Null
$plain = $null

Write-Host ''
Write-Host 'Auto-logon armed. Reboot to verify.'
Write-Host 'View it in VMConnect with BASIC SESSION (View > Enhanced Session OFF) -'
Write-Host 'Enhanced Session uses RDP and shows a separate login, NOT the auto-logon console.'
