# ADR-020 — Velopack self-update from GitHub Releases

**Status:** Accepted  
**Date:** 2026-06-06  
**Issue:** #17

---

## Context

The application's target user is an elderly relative who cannot be expected to manually download new versions, run installers, or follow update instructions. Updates must be automatic, low-friction, and require no administrative privileges.

The existing distribution mechanism (a Velopack-produced installer) already installs the application to a per-user location. The update mechanism must be compatible with that install scope. Code-signing is not currently in place, so any UAC elevation prompt would likely cause confusion or refusal.

---

## Decision 1 — Velopack for update delivery

Use Velopack as the update framework. Reasons:

- Actively maintained, MIT-licensed; the Squirrel.Windows fork the installer was originally based on has been abandoned upstream.
- Installs and updates to a per-user location by default — no UAC elevation on update.
- Provides a GitHub Releases feed integration that requires no server infrastructure.
- The framework handles the update bootstrap process (download, verify, apply, relaunch) without custom code.

Rejected: Squirrel.Windows. Abandoned upstream; no new releases.

Rejected: ClickOnce. Poor update UX on recent Windows versions; complex code-signing requirements.

Rejected: MSIX. Requires Microsoft Store or a sideloading policy; heavyweight for a single-user tool; elevated provisioning.

---

## Decision 2 — GitHub Releases as the update feed; no embedded credential

The application's GitHub repository is public. Velopack's GitHub source reads release assets from the public API, which allows up to 60 unauthenticated requests per IP per hour — sufficient for a single-user tool that checks once per launch.

No access token is shipped in the client. An embedded token would create a credential-leak path; it is unnecessary because the repository is and must remain public for this mechanism to function without one.

If the repository is ever made private, this decision must be revisited: either a token delivery strategy is adopted or the update feed URL is changed to a separately hosted location.

---

## Decision 3 — Update check is best-effort and non-blocking

A failed or impossible check — including running the application outside a managed Velopack install (developer workstation, CI, portable launch) — never blocks startup and never surfaces an error to the user.

The update check runs as a fire-and-forget task launched after the main window is visible. Any exception in the check or apply path is logged at error level and discarded; the next launch will attempt the check again.

This is a deliberate trade-off: a missed update check is less harmful than a startup failure.

---

## Decision 4 — Polish-language confirmation dialog

Before downloading and applying an update, a dialog asks the user for confirmation. The dialog uses plain Polish phrasing ("Dostępna aktualizacja — zainstalować?") with "Tak" and "Nie" buttons. The user can decline; declining skips the update and does not re-prompt until the next launch.

The confirmation step also serves as a visible signal that the application is actively maintained.

---

## Consequences

- Updates require no user action beyond clicking "Tak" in the confirmation dialog.
- No administrator credentials are needed for install or update.
- The repository must remain public for the unauthenticated feed to function. Going private breaks the update check silently (the check returns no-update, logged at error).
- Each release must be produced by the dedicated release workflow; packaging is not automatic on push.
- Update correctness at the orchestration level (check → prompt → apply) is verified by unit tests against a mocked update seam. End-to-end upgrade verification is a manual smoke test; no automated CI coverage of the Velopack engine's own download-and-restart path.
