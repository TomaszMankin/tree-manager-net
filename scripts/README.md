# Scripts

Operational helper scripts for tree-manager-net. Not part of the app build.

## Why these exist

The app is WPF (Windows-only, GUI). Unit/integration tests (L0/L1) cover logic and
the on-disk repository, but they never launch the real window. Several user-visible
bugs (false-dirty prompts, Enter-key behaviour, relationship-folder/shortcut graph)
can only be caught by driving the actual UI. That is the L3 tier: a FlaUI suite that
clicks through the app and then verifies the resulting folder/file/shortcut graph on
disk.

FlaUI uses Windows UI Automation, which requires a **real, unlocked, interactive
desktop session**. The existing self-hosted runner runs as `NT AUTHORITY\NETWORK
SERVICE` in session 0 — no desktop — so it cannot run L3. The fix is a dedicated
Hyper-V VM that hosts a second self-hosted runner (label `ui-tests`) in its own
isolated session. Its desktop is separate from yours, so the suite runs without
taking over your mouse/keyboard.

## windows-setup-hyper-v-vm.ps1

Provisions the Hyper-V VM (Gen 2 + vTPM + Secure Boot, required by Windows 11).
Computer-agnostic — all machine-specific values are parameters.

### Prerequisites
- Windows Pro/Enterprise/Home with Hyper-V enabled:
  `Enable-WindowsOptionalFeature -Online -FeatureName Microsoft-Hyper-V -All` (reboot).
- A Windows 11 ISO (any edition; Home is fine).
- Elevated (Administrator) PowerShell.

### Usage
```powershell
# Minimal — ISO path is the only required argument
.\Scripts\windows-setup-hyper-v-vm.ps1 -IsoPath 'D:\iso\Windows11.iso'

# Custom sizing / name
.\Scripts\windows-setup-hyper-v-vm.ps1 -IsoPath 'D:\iso\Win11.iso' `
    -VmName ci-ui -MemoryGB 6 -ProcessorCount 4 -DiskGB 80

# Recreate an existing VM of the same name
.\Scripts\windows-setup-hyper-v-vm.ps1 -IsoPath 'D:\iso\Win11.iso' -Force
```
`Get-Help .\Scripts\windows-setup-hyper-v-vm.ps1 -Full` for all parameters.

Defaults: 2 vCPU, 4 GB RAM, 64 GB disk, VM under the Hyper-V default VHD path,
virtual switch auto-detected (prefers `Default Switch`).

### Windows install notes
- At "Press any key to boot from CD or DVD": click **inside** the VM window first
  (keyboard focus), then mash a key within ~5 seconds. If missed, the VM falls
  through to PXE/disk and shows a boot-failure summary — just restart and retry.
- Skip the product key ("I don't have a product key"); choose any edition. Runs
  unactivated, fine for a test box.
- **Local-account-only bypass** (Win11 OOBE forces a Microsoft account; we need a
  local user for auto-logon). At the "Let's connect you to a network" / sign-in step:
  1. Press **Shift+F10** to open a command prompt.
  2. Run `start ms-cxh:localonly`
     (older builds: run `oobe\bypassnro`, the VM reboots, then choose
     "I don't have internet" -> "Continue with limited setup").
  3. The local-account screen appears -> create user **`tmtest`**. Skip Wi-Fi.
- After install, on the host, detach the ISO so it boots from disk:
  `Remove-VMDvdDrive -VMName tm-ui-tests -ControllerNumber 0 -ControllerLocation 1`
  The ISO is only boot media — once Windows is on the VHDX, moving or deleting the
  ISO does NOT require reinstalling the VM; detach afterwards (the running VM locks it).

### Post-install (inside the VM)
1. **Run `vm-configure-test-session.ps1`** (elevated, as `tmtest`) — one-time. Disables
   UI animations/transparency (FlaUI determinism + speed), lock/screensaver/sleep
   (UIA needs a live desktop), and services unnecessary on a dedicated test VM
   (telemetry, search indexer, SysMain, Xbox, print spooler; Windows Update -> Manual
   to avoid surprise reboots — pass `-KeepWindowsUpdate` to leave it on Automatic).
   Reversible + idempotent. Reboot afterwards.
2. **Auto-logon** — give the account a password first, then run the script:
   ```powershell
   net user <user> *                          # set a password (interactive, no quoting)
   .\Scripts\vm-configure-autologon.ps1 -Username <user>   # prompts for the password
   ```
   This uses Sysinternals Autologon (encrypted LSA secret) — reliable on Win11, unlike
   plain registry `DefaultPassword`. It also sets `DevicePasswordLessBuildVersion=0`
   (stops Win11 forcing Hello-first). Reboot to verify. Do NOT commit the password.

   **!! Enhanced Session gotcha (this cost hours) !!** Auto-logon lands on the VM's
   CONSOLE session. Hyper-V **Enhanced Session** connects over RDP and shows its OWN
   login prompt in a SEPARATE session, hiding the auto-logged-in console — so it looks
   like auto-logon "failed" when it actually worked. To see/use the auto-logon desktop
   (and the session the runner drives), use **BASIC SESSION** in VMConnect
   (View menu -> Enhanced Session = OFF). Use Enhanced Session only when you need
   clipboard during setup, then switch back to Basic.
3. **Run `vm-install-tooling.ps1`** (elevated, needs network) — installs .NET 10 SDK
   (matches `net10.0` / `net10.0-windows`; includes the WPF Desktop runtime) + Git
   via winget. Idempotent.
4. **Register the GitHub Actions runner** with label `ui-tests`. From the repo's
   Settings -> Actions -> Runners -> New self-hosted runner page, run the given
   download + `config.cmd` commands. Answer: labels `ui-tests`; **"Run as service?" =
   No** (a service runs in session 0 = no desktop = no UI automation). Then make it
   auto-start interactively:
   ```powershell
   .\Scripts\vm-runner-autostart.ps1            # creates a Startup-folder shortcut to run.cmd
   ```
5. **Hyper-V Manager -> Checkpoint** the VM as a clean baseline.

## vm-configure-test-session.ps1

One-time in-VM provisioning (animations off, no-lock/sleep, trim services). Run
elevated as `tmtest`. Safe to re-run. `Get-Help .\Scripts\vm-configure-test-session.ps1 -Full`.

Why it matters for tests: animations are the top cause of FlaUI flakiness (control
present but not yet hittable mid-fade); a locked/slept desktop kills UI Automation
outright. None of these change the control tree or AutomationIds — tests only get
faster and more deterministic.

## vm-install-tooling.ps1

Installs .NET 10 SDK + Git via winget (run elevated in the VM, needs network).
The SDK includes the Windows Desktop runtime required to build/run the WPF app.
Idempotent — re-running skips installed packages. Does not register the runner
(that needs a per-repo token; manual, step 4 above).
`Get-Help .\Scripts\vm-install-tooling.ps1 -Full`.

## vm-configure-autologon.ps1

Arms unattended auto-logon via Sysinternals Autologon (encrypted LSA secret) and
sets `DevicePasswordLessBuildVersion=0`. Run elevated in the VM. Prompts for the
password (or pass `-Password`); never commit the password.
`Get-Help .\Scripts\vm-configure-autologon.ps1 -Full`.

**Remember:** verify with VMConnect **Basic Session**, not Enhanced — Enhanced
Session is an RDP login into a separate session and hides the auto-logon console.

## vm-runner-autostart.ps1

Creates a Startup-folder shortcut to the runner's `run.cmd` so it launches in the
interactive session at every auto-logon. Run after the runner is registered
(`config.cmd`, label `ui-tests`, "Run as service? No"). `-RunnerPath` defaults to
`C:\actions-runner`. `Get-Help .\Scripts\vm-runner-autostart.ps1 -Full`.

## Agent-facing readiness gate: `ui-runner-preflight.yml`

The agent that will *use* the VM has access only through GitHub Actions, so the
deterministic "is the VM ready?" check is a workflow, not a manual script. The
workflow `.github/workflows/ui-runner-preflight.yml` runs `runs-on: [self-hosted,
ui-tests]` and executes `vm-verify-setup.ps1` on the runner — failing red if .NET, Git,
auto-logon, animations/lock, services, or the runner setup are wrong. The agent
triggers it and gates on the result:
```bash
gh workflow run ui-runner-preflight.yml --ref <branch>
gh run watch <id>
```
It's also exposed via `workflow_call` so the UI-test workflow can depend on it as a
prerequisite job. The two scripts below are the building blocks / manual equivalents.

## verify-vm-connection.ps1 (run on the HOST)

End-to-end "can I reach and use the VM" check, run on the **host**. Confirms the VM
exists, is running (`-Start` to start it), heartbeat is OK, and — the real proof —
logs in and executes a command **inside** the guest via PowerShell Direct (no network
needed). `-FullCheck` also runs the in-VM `vm-verify-setup.ps1` remotely and relays its
result. Prompts for guest credentials. `Get-Help .\Scripts\verify-vm-connection.ps1 -Full`.
```powershell
.\Scripts\verify-vm-connection.ps1 -Start -FullCheck
```

## vm-verify-setup.ps1 (run INSIDE the VM)

Read-only provisioning checklist — run elevated in the VM to confirm every required
piece is in place (auto-logon, animations/lock off, services disabled, .NET 10 SDK,
Git, runner registered + interactive auto-start + NOT a service). Prints PASS/FAIL and
exits non-zero on any failure. Changes nothing.
`Get-Help .\Scripts\vm-verify-setup.ps1 -Full`.

## End-to-end order (summary)

On the **host** (elevated):
1. `windows-setup-hyper-v-vm.ps1 -IsoPath <iso>` — create VM, install Windows
   (local account bypass, skip key). Detach ISO after.
2. Host Hyper-V Settings -> allow Enhanced Session (for clipboard during setup only).

Inside the **VM** (elevated), then reboot where noted:
3. `vm-configure-test-session.ps1` — animations/lock/services. Reboot.
4. `net user <user> *` then `vm-configure-autologon.ps1 -Username <user>`. Reboot,
   verify in **Basic Session**.
5. `vm-install-tooling.ps1` — .NET 10 SDK + Git.
6. Register the runner (manual, needs token; label `ui-tests`, not-a-service), then
   `vm-runner-autostart.ps1`.
7. `vm-verify-setup.ps1` — confirm everything passes.
8. Hyper-V Manager -> Checkpoint (clean baseline).
