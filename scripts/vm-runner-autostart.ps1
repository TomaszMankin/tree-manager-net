<#
.SYNOPSIS
    Makes an already-configured GitHub Actions runner start automatically at logon,
    in the interactive desktop session (required for UI automation). Run INSIDE the VM.

.DESCRIPTION
    The runner must NOT be installed as a Windows service (a service runs in session 0
    with no desktop, so UI Automation cannot attach). Instead it must run via run.cmd
    inside the logged-on console session. This script creates a Startup-folder shortcut
    to run.cmd so it launches on every auto-logon.

    Prerequisite: the runner is already registered (config.cmd run, label ui-tests,
    answered "No" to run-as-service). See README step 4.

.PARAMETER RunnerPath
    Folder containing run.cmd. Default: C:\actions-runner.

.EXAMPLE
    .\vm-runner-autostart.ps1
.EXAMPLE
    .\vm-runner-autostart.ps1 -RunnerPath 'D:\actions-runner'
#>
[CmdletBinding()]
param(
    [string] $RunnerPath = 'C:\actions-runner'
)

$ErrorActionPreference = 'Stop'

$runCmd = Join-Path $RunnerPath 'run.cmd'
if (-not (Test-Path $runCmd)) {
    throw "run.cmd not found at $runCmd. Register the runner first (see README step 4), or pass -RunnerPath."
}

$startup = [Environment]::GetFolderPath('Startup')   # current user's Startup folder
$lnk = Join-Path $startup 'github-actions-runner.lnk'

$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut($lnk)
$shortcut.TargetPath = $runCmd
$shortcut.WorkingDirectory = $RunnerPath
$shortcut.WindowStyle = 7          # minimized
$shortcut.Description = 'GitHub Actions runner (interactive session)'
$shortcut.Save()

Write-Host "Created startup shortcut: $lnk -> $runCmd"
Write-Host 'The runner will start at next logon. To start it now without rebooting:'
Write-Host "  & '$runCmd'"
