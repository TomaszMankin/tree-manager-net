# ADR-015 — Lineage folder naming: surname-clash full-name escalation

## Context

Issue #13 introduces lineage folders under 'Rody/', one folder per contributor's lineage surname. A contributor is a parent of the root person or a parent of the root person's spouse. The lineage surname for a contributor is ordinarily the maiden name when a maiden name is recorded, and the last name otherwise. Two real-world situations make a simple surname-based folder key insufficient. First, two contributors on opposite sides of the family may carry the same surname — for instance a paternal grandfather and a maternal grandmother who married into the same surname — which would incorrectly collapse their distinct lineages into a single folder. Second, a contributor whose maiden name is the unknown sentinel has no meaningful surname to distinguish from other unknown-maiden-name contributors, which would similarly cause incorrect collisions. Pull request #38 identified both situations through concrete examples and requested a rule that resolves them without requiring manual intervention from the user.

## Decision

Lineage folder names are determined in two passes over the full contributor list. In the first pass each contributor is assigned a candidate folder key: the maiden name when a maiden name is recorded and it is not the unknown sentinel; otherwise the last name; otherwise the contributor's full display name. In the second pass any candidate key that appears for more than one contributor is replaced, for all contributors sharing it, by each contributor's own full display name. This ensures that every lineage folder has a unique key and that no information is silently discarded. The worst-case result is four folders all named by full display name, which is acceptable. The unknown-sentinel maiden name is treated as absent rather than as a valid key, so multiple contributors with unknown maiden names do not share an unknown-sentinel folder.

## Consequences

- Surname collisions between contributors are resolved automatically, at the cost of longer folder names in the collision case.
- The full-display-name escalation is applied symmetrically: if two contributors share a surname, both are escalated, not just the later one. This removes any dependence on contributor ordering for naming.
- The unknown-sentinel maiden name no longer suppresses a contributor from the folder listing; it causes a fall-through to last name or full display name instead.
- Tests for the collision case must assert that both contributors receive full-display-name folders, replacing any earlier test that asserted first-contributor-wins semantics.
- In the common case (no collisions, no unknown maiden names) the algorithm produces exactly the same output as before.

## Status

Accepted · 2026-06-04
