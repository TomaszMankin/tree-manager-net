<#
.SYNOPSIS
    Installs the toolchain the L3 UI-test VM needs: .NET 10 SDK and Git.
    Run INSIDE the VM (elevated) after vm-configure-test-session.ps1. Needs network.

.DESCRIPTION
    The repo targets net10.0 / net10.0-windows (WPF). This installs the matching
    .NET 10 SDK (which includes the Windows Desktop runtime needed to run/build the
    WPF app) and Git, via winget. Idempotent: winget skips already-installed packages.

    Does NOT register the GitHub Actions runner — that needs a one-time token from
    the repo's Settings > Actions > Runners page and is a manual step (see README).

.PARAMETER DotnetPackageId
    winget id for the SDK. Default: Microsoft.DotNet.SDK.10.

.EXAMPLE
    .\vm-install-tooling.ps1
#>
[CmdletBinding()]
param(
    [string] $DotnetPackageId = 'Microsoft.DotNet.SDK.10'
)

$ErrorActionPreference = 'Stop'

function Test-Admin {
    $id = [Security.Principal.WindowsIdentity]::GetCurrent()
    return (New-Object Security.Principal.WindowsPrincipal($id)).IsInRole(
        [Security.Principal.WindowsBuiltInRole]::Administrator)
}
if (-not (Test-Admin)) { throw 'Run elevated (Administrator) inside the VM.' }

# --- winget present? ---------------------------------------------------------

if (-not (Get-Command winget -ErrorAction SilentlyContinue)) {
    throw 'winget not found. Open Microsoft Store, update "App Installer", then re-run. ' +
          '(winget ships with Win11 but needs a first-run/update with network.)'
}

# Network sanity
if (-not (Test-Connection -ComputerName 8.8.8.8 -Count 1 -Quiet)) {
    throw 'No network connectivity. Connect the VM to a network before installing tooling.'
}

function Install-Pkg($id) {
    Write-Host "Installing $id ..."
    winget install --id $id --exact --silent `
        --accept-package-agreements --accept-source-agreements
    if ($LASTEXITCODE -ne 0 -and $LASTEXITCODE -ne -1978335189) {
        # -1978335189 = APPINSTALLER_CLI_ERROR_PACKAGE_ALREADY_INSTALLED (treat as success)
        throw "winget failed for $id (exit $LASTEXITCODE)"
    }
}

# --- Install -----------------------------------------------------------------

Install-Pkg 'Git.Git'
Install-Pkg $DotnetPackageId

# Refresh PATH for this session so the verify step below sees the new tools
$env:Path = [Environment]::GetEnvironmentVariable('Path', 'Machine') + ';' +
            [Environment]::GetEnvironmentVariable('Path', 'User')

# --- Verify ------------------------------------------------------------------

Write-Host ''
Write-Host 'Verifying installs (open a NEW shell if these are not found yet):'
try { Write-Host ('  dotnet: ' + (dotnet --version)) } catch { Write-Host '  dotnet: not on PATH yet - reopen shell' }
try { Write-Host ('  git:    ' + (git --version))    } catch { Write-Host '  git:    not on PATH yet - reopen shell' }

Write-Host ''
Write-Host 'Done. Next: register the GitHub Actions runner (label ui-tests), interactively.'
Write-Host 'See Scripts/README.md > Post-install step 4.'
