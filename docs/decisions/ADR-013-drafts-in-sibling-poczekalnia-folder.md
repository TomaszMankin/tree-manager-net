# ADR-013 — Drafts live in a sibling staging folder, isolated until promoted

## Context

Issue #11 needs a staging area where in-progress people can be parked without entering the family tree. Parked work must not appear in the main listing, must not trigger relationship synchronization, and must survive a failed attempt to move it into the tree. The main listing is built by scanning the people folder only; a separate location keeps drafts out of that scan by structure rather than by a maintained exclusion list.

## Decision

Drafts are stored in a staging folder that sits beside the people folder, not inside it. The main scan descends only the people folder, so drafts are excluded by location alone — no exclusion list is maintained. Saved drafts carry a stable identity but are never relationship-synced while parked. Promotion is an explicit user action that writes the person into the people folder, runs the full bidirectional relationship sync used by ordinary creation, and only then removes the draft. If writing or syncing fails, the draft is left intact and any partial entry in the people folder is removed, so the user can retry. Re-running a promotion is safe because relationship sync de-duplicates.

## Consequences

- Drafts are invisible to the main listing for free; widening the scan later would require a deliberate exclusion and is guarded by a test that asserts drafts never surface.
- Promotion reuses the existing creation path, so drafts and ordinary people sync identically.
- Promotion is recoverable rather than strictly all-or-nothing: a crash mid-promote may leave the person in the tree with sync incomplete and the draft still parked, but a repeat promotion completes safely.
- The validation rule that an ordinary save requires at least one relationship is not enforced on drafts, by design — drafts are work-in-progress.

## Status

Accepted · 2026-06-04
</content>
