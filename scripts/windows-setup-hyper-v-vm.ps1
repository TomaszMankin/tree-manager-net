<#
.SYNOPSIS
    Creates a Hyper-V VM to host the interactive GitHub Actions runner that runs
    the L3 WPF UI-automation suite (FlaUI). See Scripts/README.md for the why.

.DESCRIPTION
    WPF UI automation needs a real, unlocked, interactive desktop session. The
    default self-hosted runner runs as NT AUTHORITY\NETWORK SERVICE in session 0
    with no desktop, so it cannot drive the app window. This script provisions an
    isolated Hyper-V VM (Gen 2 + vTPM + Secure Boot, required by Windows 11) that
    will hold a second self-hosted runner labelled 'ui-tests'. The VM's desktop is
    isolated from the host session, so the suite runs beside you without taking
    over your mouse/keyboard.

    The script is computer-agnostic: nothing is hard-coded to a specific machine.
    All paths and sizes are parameters with sensible defaults; the ISO path is
    required, and the virtual switch is auto-detected when not supplied.

    Run from an elevated (Administrator) PowerShell prompt.

.PARAMETER IsoPath
    Full path to the Windows 11 installation ISO. Required.

.PARAMETER VmName
    VM name. Default: tm-ui-tests.

.PARAMETER VmRootPath
    Directory under which the VM folder + VHDX are created.
    Default: <Hyper-V default VHD path>\<VmName>, falling back to C:\HyperV\<VmName>.

.PARAMETER MemoryGB
    Startup memory in GB. Default: 4.

.PARAMETER ProcessorCount
    Virtual CPUs. Default: 2.

.PARAMETER DiskGB
    OS disk size in GB. Default: 64.

.PARAMETER SwitchName
    Virtual switch to attach. Default: auto-detect (prefers 'Default Switch',
    else the first available external/internal switch).

.PARAMETER Force
    Recreate the VM if one with the same name already exists (removes it first).

.EXAMPLE
    .\windows-setup-hyper-v-vm.ps1 -IsoPath 'D:\iso\Windows11.iso'

.EXAMPLE
    .\windows-setup-hyper-v-vm.ps1 -IsoPath 'D:\iso\Win11.iso' -VmName ci-ui -MemoryGB 6 -ProcessorCount 4
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $IsoPath,

    [string] $VmName = 'tm-ui-tests',

    [string] $VmRootPath,

    [ValidateRange(2, 64)]
    [int] $MemoryGB = 4,

    [ValidateRange(1, 32)]
    [int] $ProcessorCount = 2,

    [ValidateRange(32, 512)]
    [int] $DiskGB = 64,

    [string] $SwitchName,

    [switch] $Force
)

$ErrorActionPreference = 'Stop'

function Test-Admin {
    $id = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($id)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

# --- Preconditions -----------------------------------------------------------

if (-not (Test-Admin)) {
    throw 'Run this script from an elevated (Administrator) PowerShell prompt.'
}

if (-not (Get-Command Get-VM -ErrorAction SilentlyContinue)) {
    throw 'Hyper-V PowerShell module not found. Enable Hyper-V first: ' +
          'Enable-WindowsOptionalFeature -Online -FeatureName Microsoft-Hyper-V -All  (then reboot).'
}

if (-not (Test-Path -LiteralPath $IsoPath)) {
    throw "ISO not found: $IsoPath"
}

# --- Resolve paths (computer-agnostic) ---------------------------------------

if (-not $VmRootPath) {
    $defaultVhd = (Get-VMHost).VirtualHardDiskPath
    $baseRoot = if ($defaultVhd) { $defaultVhd } else { 'C:\HyperV' }
    $VmRootPath = Join-Path $baseRoot $VmName
}
$vhdPath = Join-Path $VmRootPath ("$VmName.vhdx")

# --- Resolve virtual switch (auto-detect) ------------------------------------

if (-not $SwitchName) {
    $preferred = Get-VMSwitch -ErrorAction SilentlyContinue | Where-Object Name -eq 'Default Switch'
    $chosen = if ($preferred) { $preferred } else { Get-VMSwitch -ErrorAction SilentlyContinue | Select-Object -First 1 }
    if (-not $chosen) {
        throw 'No Hyper-V virtual switch found. Create one (Hyper-V Manager > Virtual Switch Manager) or pass -SwitchName.'
    }
    $SwitchName = $chosen.Name
}
Write-Host "Using virtual switch: $SwitchName"

# --- Handle existing VM ------------------------------------------------------

$existing = Get-VM -Name $VmName -ErrorAction SilentlyContinue
if ($existing) {
    if (-not $Force) {
        throw "VM '$VmName' already exists. Re-run with -Force to recreate it, or pick a different -VmName."
    }
    Write-Host "Removing existing VM '$VmName' (-Force)..."
    if ($existing.State -ne 'Off') { Stop-VM -Name $VmName -TurnOff -Force }
    Remove-VM -Name $VmName -Force
    if (Test-Path -LiteralPath $vhdPath) { Remove-Item -LiteralPath $vhdPath -Force }
}

# --- Create VM ---------------------------------------------------------------

Write-Host "Creating VM '$VmName' ($ProcessorCount vCPU, ${MemoryGB}GB RAM, ${DiskGB}GB disk)..."
New-VM -Name $VmName -Generation 2 -MemoryStartupBytes (${MemoryGB} * 1GB) `
       -Path $VmRootPath -NewVHDPath $vhdPath -NewVHDSizeBytes (${DiskGB} * 1GB) | Out-Null

Set-VM -Name $VmName -ProcessorCount $ProcessorCount -AutomaticCheckpointsEnabled $false

Connect-VMNetworkAdapter -VMName $VmName -SwitchName $SwitchName

# Boot from the ISO
Add-VMDvdDrive -VMName $VmName -Path $IsoPath
$dvd = Get-VMDvdDrive -VMName $VmName
Set-VMFirmware -VMName $VmName -FirstBootDevice $dvd

# vTPM + Secure Boot - Windows 11 installer refuses without these
Set-VMKeyProtector -VMName $VmName -NewLocalKeyProtector
Enable-VMTPM -VMName $VmName

Write-Host "Starting VM and opening console..."
Start-VM -Name $VmName
vmconnect.exe localhost $VmName

Write-Host ''
Write-Host 'Next: click inside the VM window, then press a key fast at'
Write-Host '"Press any key to boot from CD or DVD". See Scripts/README.md for post-install steps.'
