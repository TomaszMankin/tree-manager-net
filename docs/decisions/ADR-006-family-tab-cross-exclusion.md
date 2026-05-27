# ADR-006: Family tab cross-exclusion is per-session, not enforced at filesystem layer

Status: Accepted
Date: 2026-05-28

## Problem

The Family tab presents four relationship pickers (Parents, Children, Spouses, Siblings). A person may not hold multiple roles simultaneously — selecting someone as a parent should prevent selecting them as a spouse or sibling. The question is where this constraint is enforced: at the ViewModel boundary (per-session, UI-only) or at the filesystem layer (persistent, written into me.json).

## Decision

- Cross-exclusion is enforced per-session in `FamilyTabViewModel` only.
- `FamilyTabViewModel` subscribes to each picker's `Selected.CollectionChanged` and recomputes exclusions across all four pickers on every change.
- The loaded person's identifier is excluded from all four picker candidate lists (self-exclusion).
- No cross-role constraint is stored in or validated against `MeFile` — the filesystem layer is write-only at save time and accepts whatever the ViewModel produces.

## Rejected alternatives

- **Filesystem enforcement (validate on read/write)**: Would require reading all four relationship lists and cross-checking IDs on every save — high cost for a constraint that real data may already violate in legacy files. Deferred to a future consistency validator (issue #14).
- **Single unified relationship list**: Collapses role semantics into a flag field; harder to query and render by role type.

## Consequences

+ Exclusion logic is fast and stateless — no I/O on every selection change.
+ Existing me.json files with overlapping roles (legacy data) load without error; the constraint applies only to new edits within a session.
- A person saved in two roles across separate sessions is not prevented; issue #14 (`TreeConsistencyValidator`) is the intended resolution.
