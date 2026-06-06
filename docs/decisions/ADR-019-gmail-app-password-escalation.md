# ADR-019 — Gmail app-password escalation

**Status:** Accepted  
**Date:** 2026-06-06  
**Issue:** #16

---

## Context

On a critical fault the maintainer needs to be notified so they can respond quickly. The application is a single-maintainer Windows desktop tool; the relevant user is an elderly relative who cannot be expected to notice log files or report errors. An automated email notification on crash is the most reliable signal.

The maintainer already has a Gmail account. App-password authentication (a 16-character token separate from the account password) is supported by Gmail's SMTP interface and does not require any OAuth2 consent flow — appropriate for a single-maintainer tool.

Not every installation will have email configured (the elderly user's machine may never have credentials set up). The mechanism must degrade gracefully to a no-op when credentials are absent.

---

## Decision 1 — Gmail app-password over OAuth2

Use a Google App Password for SMTP authentication. Reasons:

- No browser-based consent flow required — suitable for unattended operation.
- The single-maintainer scenario does not justify the complexity of OAuth2 token refresh and redirect handling.
- App passwords are revocable independently of the account password.

Rejected: OAuth2 authorization code flow. It requires a registered OAuth2 client, redirect URI, token storage, and refresh logic — disproportionate for one recipient.

---

## Decision 2 — MailKit over the BCL SmtpClient

Use the MailKit library for SMTP delivery. The BCL `System.Net.Mail.SmtpClient` has been marked deprecated since .NET 5 and lacks async support. MailKit provides a modern async API with explicit TLS options (`SslOnConnect` for port 465, `StartTls` for port 587).

Rejected: `System.Net.Mail.SmtpClient`. Deprecated; no async; blocked in newer .NET versions.

---

## Decision 3 — User-local secrets; never committed

The credentials file (`appsettings.user.json`) is install-scoped, stored in the application base directory, and excluded from version control via the `*.user.json` gitignore pattern. No encryption at rest is applied — the file resides on a single-user machine under the maintainer's own account.

This is a deliberate trade-off: adding encryption would require a key-management strategy that exceeds the scope of a hobby tool. The threat model is a shared machine with a curious co-user, which is not the deployment scenario.

---

## Decision 4 — Crash-handling funnel hook over a custom log sink

Escalation is triggered from the existing crash-handling funnel, not from a custom logging sink. The crash funnel is already the single aggregation point for all three unhandled-exception channels. Attaching escalation there avoids a second observation path and keeps the async fire-and-forget pattern entirely within the application layer, outside the logging pipeline.

Rejected: a custom Serilog log sink. It would duplicate crash observation, run synchronously on the logging thread (requiring an explicit background dispatch anyway), and couple the async escalation concern into the logging configuration.

---

## Decision 5 — Per-install config location

The credentials file lives in the application base directory, not under the per-tree runtime folder. Reasons:

- The maintainer mailbox is one address regardless of which tree is open.
- A crash can occur before any tree root is selected; per-tree settings would be unreachable at that point.

---

## Decision 6 — Fire-and-forget send to avoid UI block

The escalation send is launched asynchronously from the crash handler so it never blocks the UI thread or delays the crash dialog. A failure in the send path falls back to an on-disk offline queue; the queue is drained in the background on a 30-minute timer. This keeps the escalation path out of the synchronous crash-handling stack, preventing a failing SMTP call from masking or delaying the user-visible crash dialog.

---

## Consequences

- A maintainer who has not configured a credentials file sees no email on crash — correct degraded behaviour.
- The offline queue accumulates entries across restarts until the next successful send.
- The credentials file contains a plaintext app-password; it must be excluded from version control and from backups that could be shared.
