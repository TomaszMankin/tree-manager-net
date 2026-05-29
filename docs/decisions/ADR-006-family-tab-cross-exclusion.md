# ADR-006: Family tab cross-exclusion is per-session, not enforced at filesystem layer

Status: Accepted
Date: 2026-05-28

## Problem

The Family tab presents four relationship pickers (Parents, Children, Spouses, Siblings). A person may not hold multiple roles simultaneously — selecting someone as a parent should prevent selecting them as a spouse or sibling. The question is where this constraint is enforced: at the ViewModel boundary (per-session, UI-only) or at the filesystem layer (persistent, written into me.json).

## Decision

- Cross-exclusion enforced in ViewModel layer only; filesystem layer accepts whatever ViewModel produces.
- Exclusion local to current person's four pickers — computed from that person's OWN relationship lists only. Recomputed reactively on any selection change across the four.
- Loaded person excluded from every picker (self-exclusion).
- Opening another person inherits NO exclusions from a prior editing session. Person A selections have zero effect on Person B picker state — exclusion always derives from the person currently loaded, never from who was edited before.
- Bidirectional sync (set someone as child → that someone gets this person as parent) is a SAVE concern, not a picker concern (issue #8). Pickers show only what is already in the loaded person's own persisted lists.
- No cross-role constraint stored in or validated against the persisted file — filesystem layer is write-only at save time.

## Rejected alternatives

- **Filesystem enforcement (validate on read/write)**: Would require reading all four relationship lists and cross-checking IDs on every save — high cost for a constraint that real data may already violate in legacy files. Deferred to a future consistency validator (issue #14).
- **Single unified relationship list**: Collapses role semantics into a flag field; harder to query and render by role type.

## Consequences

+ Exclusion logic is fast and stateless — no I/O on every selection change.
+ Existing me.json files with overlapping roles (legacy data) load without error; the constraint applies only to new edits on the loaded person.
- A person saved in two roles across separate edits is not prevented; issue #14 is the intended resolution.
- Switching person resets state fully — reload entirely from the new person's persisted data, nothing carries over (issue #9). B set as A's child vanishes from A's pickers but stays available for C when C's own lists do not hold B.
- Edge case (shared children across relationships): if A holds C as child and B is A's spouse, editing B does NOT exclude C from B's pickers — B is not assumed a parent. C is excluded from B's pickers only when B's OWN persisted lists already hold C.
