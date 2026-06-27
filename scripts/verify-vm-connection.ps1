<#
.SYNOPSIS
    Run on the HOST. Confirms the L3 UI-test VM exists, is running, and is usable —
    by actually executing a command inside it via PowerShell Direct (no network needed).

.DESCRIPTION
    End-to-end connectivity check for the runner VM:
      1. VM exists.
      2. VM is running (optionally started with -Start).
      3. Integration heartbeat is OK.
      4. PowerShell Direct can log in and run a command inside the guest — the real
         proof you can connect and use it.
      5. (-FullCheck) runs the in-VM Scripts\vm-verify-setup.ps1 remotely and relays
         its PASS/FAIL output.

    PowerShell Direct requires an elevated host session and valid GUEST credentials.

.PARAMETER VmName
    VM name. Default: tm-ui-tests.

.PARAMETER Credential
    Guest credentials (e.g. user 'user'). Prompted if omitted.

.PARAMETER Start
    Start the VM if it is not running.

.PARAMETER FullCheck
    Also run the in-VM provisioning check remotely (expects the script at the path in
    -GuestVerifyScript inside the guest).

.PARAMETER GuestVerifyScript
    Path INSIDE the guest to vm-verify-setup.ps1. Default: C:\Setup\vm-verify-setup.ps1.

.EXAMPLE
    .\verify-vm-connection.ps1
.EXAMPLE
    .\verify-vm-connection.ps1 -Start -FullCheck
#>
[CmdletBinding()]
param(
    [string] $VmName = 'tm-ui-tests',
    [System.Management.Automation.PSCredential] $Credential,
    [switch] $Start,
    [switch] $FullCheck,
    [string] $GuestVerifyScript = 'C:\Setup\vm-verify-setup.ps1'
)

$ErrorActionPreference = 'Stop'

function Test-Admin {
    $id = [Security.Principal.WindowsIdentity]::GetCurrent()
    return (New-Object Security.Principal.WindowsPrincipal($id)).IsInRole(
        [Security.Principal.WindowsBuiltInRole]::Administrator)
}
if (-not (Test-Admin)) { throw 'Run elevated (Administrator) on the HOST.' }

# --- 1. VM exists ------------------------------------------------------------
$vm = Get-VM -Name $VmName -ErrorAction SilentlyContinue
if (-not $vm) { throw "VM '$VmName' not found on this host." }
Write-Host "[PASS] VM exists: $VmName"

# --- 2. Running --------------------------------------------------------------
if ($vm.State -ne 'Running') {
    if ($Start) {
        Write-Host "VM not running - starting..."
        Start-VM -Name $VmName | Out-Null
        do { Start-Sleep 2; $vm = Get-VM -Name $VmName } until ($vm.State -eq 'Running')
    } else {
        throw "VM '$VmName' is '$($vm.State)'. Re-run with -Start, or start it manually."
    }
}
Write-Host "[PASS] VM running"

# --- 3. Heartbeat ------------------------------------------------------------
$hb = Get-VMIntegrationService -VMName $VmName -Name Heartbeat -ErrorAction SilentlyContinue
if ($hb -and $hb.PrimaryStatusDescription -in @('OK','OK (No Application Data)')) {
    Write-Host "[PASS] Heartbeat: $($hb.PrimaryStatusDescription)"
} else {
    Write-Host "[WARN] Heartbeat: $($hb.PrimaryStatusDescription) (guest may still be booting)"
}

# --- 4. PowerShell Direct: actually run something inside ----------------------
if (-not $Credential) { $Credential = Get-Credential -Message "Guest credentials for $VmName (e.g. user 'user')" }

try {
    $info = Invoke-Command -VMName $VmName -Credential $Credential -ScriptBlock {
        [pscustomobject]@{
            Host    = $env:COMPUTERNAME
            User    = whoami
            Session = (Get-Process -Id $PID).SessionId
        }
    }
    Write-Host "[PASS] PowerShell Direct connected and executed inside the guest"
    Write-Host "       guest=$($info.Host) user=$($info.User) sessionId=$($info.Session)"
} catch {
    throw "PowerShell Direct FAILED — cannot use the VM. $($_.Exception.Message)"
}

# --- 5. Optional full in-VM provisioning check -------------------------------
if ($FullCheck) {
    Write-Host ''
    Write-Host "Running in-VM provisioning check ($GuestVerifyScript)..."
    Invoke-Command -VMName $VmName -Credential $Credential -ScriptBlock {
        param($script)
        if (-not (Test-Path $script)) { Write-Host "[WARN] $script not found in guest — skipped"; return }
        & powershell -ExecutionPolicy Bypass -File $script
    } -ArgumentList $GuestVerifyScript
}

Write-Host ''
Write-Host "VM '$VmName' is reachable and usable." -ForegroundColor Green
