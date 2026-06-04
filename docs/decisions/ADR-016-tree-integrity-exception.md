# ADR-016 — Tree integrity: fail-fast on detected corruption

## Context

Issue #13 and pull request #38 identified three situations in which the person tree contains data that is structurally impossible under normal application use and can only arise from manual modification of files on disk. The three situations are: a cycle in the ancestor chain where a person is reachable as their own ancestor; a broken bidirectional reference where one person records another as a parent but the parent does not record the first person as a child; and a person folder that is referenced by a relationship but cannot be read from disk. In all three cases, proceeding with lineage generation would produce silently incorrect output — either an infinite walk, a misattributed ancestor, or a missing branch — without any user-visible indication that the data is corrupt. The tree manager application is responsible for maintaining tree integrity when it writes data, so these situations indicate external tampering.

## Decision

When any of the three integrity violations is detected during lineage generation, the operation is stopped immediately by raising a dedicated tree integrity exception. No recovery logic is attempted. The exception propagates to the command layer, which catches it, logs the detail, and presents a plain error message to the user. Tree integrity is treated as a precondition of the generation operation: if the precondition is not met, the operation does not run and no previously generated lineage output is destroyed. The three violations covered are: a cycle detected during the ancestor walk; a broken bidirectional parent-child reference detected before group building begins; and a referenced person folder that cannot be read during the ancestor walk.

## Consequences

- Any manual corruption of person files that creates one of the three violations will cause lineage generation to stop with an error rather than silently producing incorrect output.
- The existing lineage output from a previous successful run is preserved intact, because the wipe-and-recreate step does not begin until after the integrity check passes.
- No automatic repair is performed. Resolving the underlying data issue is the user's responsibility.
- Tests for the cycle case must be updated: the previous assertion that a cycle was silently tolerated is replaced by an assertion that an exception is raised.
- Future work may add more integrity checks or a dedicated repair command; this decision scopes only the three violations identified in PR #38.

## Status

Accepted · 2026-06-04
