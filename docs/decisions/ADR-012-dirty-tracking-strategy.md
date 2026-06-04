---
id: ADR-012
title: Dirty-tracking strategy — snapshot equality
status: Accepted
date: 2026-06-02
---

# ADR-012 — Dirty-tracking strategy: snapshot equality

## Problem

Editing a person and then navigating away (open another, switch mode) silently discards unsaved edits. Need reliable answer to "are there unsaved changes?" so user can be warned before data is lost.

Two sub-problems:
1. Detecting changes: what counts as "dirty"?
2. Structural equality: domain record compares list members by reference, not content, so two independently assembled records with identical data read as different — dirty detection always fires even with no real change.

## Decision

- Detect unsaved changes by snapshotting persisted content at load and again at save, comparing on demand against the current edited state. Single comparison, not per-field subscription.
- Compare by content including ordered relationship collections. Domain record compares by content equality, not object identity.
- Derived machine-local path excluded from the comparison — it is not user-authored content and would produce spurious diffs.
- Warn user with a Polish discard dialog before navigating away. Two choices: discard (proceed) or cancel (stay).
- On Add path with no snapshot, pristine baseline is the state assembled from default empty inputs — same assembly route, so name-coercion defaults are included.

## Rejected

- Per-field change-event subscription: scattered state across all fields, fragile as fields are added, risk of missed subscriptions diverging from reality. Explicitly rejected in prior load/edit design (issue #10 inherited stance).
- Serialized-text comparison: couples dirty detection to storage format; allocates strings on every check.
- Save button inside the discard dialog: forces the guard to re-enter save logic including validation, error handling, and busy state — fragile coupling. Can be added as a follow-up if user requests it.

## Consequences

+ One place owns "is dirty" for all command paths (open, mode switch, and future window-close).
+ Domain record now compares by content, which also hardens relationship-sync (no spurious updates when data is unmodified).
+ Relationship collections are compared in order — ordering is meaningful (ID and name lists are positionally paired).
- A content snapshot is held in memory for the duration of an edit session.
- Machine-local path is excluded from equality; any future field that is similarly derived at save time must also be explicitly excluded.
