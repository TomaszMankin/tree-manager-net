# ADR-021 — Velopack setup as the installer; no separate Inno Setup script

**Status:** Accepted  
**Date:** 2026-06-07  
**Issue:** #18

---

## Context

The application requires a first-install experience that a non-technical elderly user can run. Requirements from the issue and comments:

- A single executable the user runs to install the application
- Start Menu entry and Desktop shortcut created on install
- Stable, predictable install path that does not change between updates
- In-place updates without creating duplicate Programs entries or moving shortcuts
- Uninstall removes application files but preserves the user's tree data
- Code-signing is explicitly out of scope

Sprint-17 introduced Velopack as the self-update framework. Before implementing a separate Inno Setup script, the issue agent spec required verifying whether Velopack already covers these requirements.

---

## Decision 1 — Velopack's setup executable is the installer; no Inno Setup script is written

Velopack's packaging step (`vpk pack`) produces a one-click setup executable as part of normal release packaging. Per Velopack documentation: "the Windows installer is a one-click setup that automatically installs the application and launches it without user interaction or wizards. It creates shortcuts in the Start Menu and on the Desktop by default."

The setup executable handles the full install lifecycle: file extraction, shortcut creation, Programs registration, and wiring the in-place update engine. Running it again over an existing install performs an in-place update with no duplicate Programs entry.

Writing a separate Inno Setup script alongside Velopack would create two competing lifecycle managers — two Programs entries, two shortcut managers, two uninstall paths — with no user benefit. The Inno Setup scope conditional in the issue spec ("if Velopack produces shortcuts automatically, scope narrows to code-signing wrapper only or ticket is eliminated") applies here. Since code-signing is out of scope, no Inno Setup script is produced.

Rejected: Inno Setup wrapper that bootstraps Velopack's setup executable. Adds a wizard UI around a one-click installer; the resulting experience is more complex, not less. Maintenance burden with no user value for a single-user tool.

Rejected: Inno Setup replacing Velopack for install. Introduces two separate update mechanisms (Inno for first install, Velopack for subsequent updates). The update path becomes inconsistent; shortcuts from the Inno install may not survive Velopack updates cleanly.

Rejected: MSIX packaging. Requires Microsoft Store or sideloading policy configuration; not viable for personal distribution to a single user's machine.

Rejected: WiX Toolset. More complex than the problem warrants; Velopack covers the same scope with no additional tooling.

---

## Decision 2 — %LocalAppData%\TreeManager is accepted as the stable install root

Velopack installs to `%LocalAppData%\{packId}` by default (`%LocalAppData%\TreeManager` for this project). Updates are applied in-place at the same path. The install root never moves between versions. Shortcuts remain valid across all updates because they point into the install root, which does not change.

The alternative of overriding the install path to `%ProgramFiles%\TreeManager` would require administrator elevation on first install and on every subsequent update (the update engine must write to its own directory). Velopack's no-UAC model is a design constraint captured in ADR-020. Forcing a system-wide install path breaks that constraint.

`%LocalAppData%\TreeManager` appears in Windows Apps and Features under the application's registered name, satisfying the requirement for a Programs entry.

---

## Decision 3 — User state relocated to a directory outside the Velopack install root

The component that records the user's chosen tree root path writes its pointer file inside `%LocalAppData%\TreeManager\`. Velopack's uninstall removes the entire `%LocalAppData%\TreeManager\` directory, which would delete this pointer file. After reinstallation the user would need to re-select their tree root (the actual tree data at the user-chosen path is not affected, but the pointer is lost).

To satisfy the requirement that uninstall does not destroy user state, the pointer file is relocated to `%LocalAppData%\PyTreeManager\`, a directory Velopack does not manage and does not remove on uninstall. A one-time migration copies the pointer from the old path on first startup after the change, so existing users are not affected.

---

## Consequences

- The release workflow produces a setup executable alongside the Velopack update assets; this executable is the artifact users download for first install.
- No Inno Setup toolchain is required on the build machine or self-hosted runner.
- Start Menu and Desktop shortcuts are created automatically at install time and removed at uninstall time, with no custom shortcut-creation code in the application.
- The install path (`%LocalAppData%\TreeManager`) is user-space; no administrator elevation is required for install, update, or uninstall.
- Uninstall via Windows Apps and Features removes all application files. The pointer file at `%LocalAppData%\PyTreeManager\last_root.txt` and all tree data at the user-chosen root path are unaffected.
- The application's actual tree data (the family-tree folder the user picked) lives at an arbitrary user-chosen location and is never touched by the install or uninstall process.
- If the repository is ever made private, the update feed mechanism documented in ADR-020 must be revisited; the installer itself is unaffected.
