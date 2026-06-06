# ADR-018 — Global exception handling and per-day file logging

**Status**: Accepted
**Date**: 2026-06-06
**Issue**: #15

## Context

The application must record diagnostics when something goes wrong and must never expose a raw technical crash to the user. A WPF desktop application combined with async operations has three distinct channels through which unhandled exceptions can surface: the main UI thread dispatcher, background async tasks, and the process-level AppDomain. Without explicit handling, each channel produces a different outcome — one silently swallows the error, another crashes the process without user feedback, and a third tears down the application before any message is shown.

Additionally, useful diagnostics must be persisted to disk so that problems can be investigated after the fact. The destination of those diagnostics is only knowable after application startup has resolved which data root the user is working with, creating an ordering constraint.

## Decisions

**Decision 1 — three channels observed**: All three unhandled-exception channels are subscribed and routed to a single crash-handling path. The user sees one consistent outcome regardless of which channel triggered the fault.

**Decision 2 — user message is Polish only, no technical detail**: The user sees a simple Polish message. No exception text, no stack trace, and no technical terminology is shown in the user interface. Diagnostic detail goes to the log file only. The user population is non-technical.

**Decision 3 — log location is per-tree runtime-data folder**: One log file is written per calendar day, stored under the active tree's runtime-data folder. Diagnostics travel with the data they describe. For a single-user desktop tool no retention or rotation policy is required.

**Decision 4 — log filename carries an ISO date**: Daily log filenames embed the calendar date in ISO `YYYY-MM-DD` form for human readability and sortability in file explorers. The logging library's built-in rolling filename format produces a non-separated numeric date suffix and cannot produce the required ISO prefix; the date is therefore computed at startup and baked into a fixed path for the session.

**Decision 5 — single logging abstraction throughout**: One logging abstraction is used across the entire application. Introducing a second bridging abstraction would add indirection with no current consumer benefit and would violate the project's anti-overengineering posture.

## Consequences

Startup ordering must establish all three exception channels before the log destination is known, because resolving the log folder path may require prompting the user. A crash occurring before the log file is configured degrades to a best-effort dialog with no file entry. This is acceptable: the primary goal of the log file is post-hoc diagnostics for sessions that successfully start, not crash capture during the startup sequence itself.
