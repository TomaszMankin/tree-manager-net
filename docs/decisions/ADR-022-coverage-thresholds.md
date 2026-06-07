# ADR-022 — Coverage thresholds enforced in CI before cutover

**Status:** Accepted  
**Date:** 2026-06-07  
**Issue:** #20

---

## Context

The application is approaching cutover from incremental feature delivery to maintenance. Tests have been added per sprint as features landed, but no automated floor prevented coverage from eroding as the codebase grew. Before cutover, a coverage gate is wanted so that future changes cannot silently reduce test coverage of the layers that carry business logic.

The codebase is layered: a pure domain-and-services layer with no input/output, an infrastructure layer that owns filesystem access, and an application layer that is dominated by user-interface and operating-system interop wrappers.

---

## Decision 1 — The pure logic layer and the infrastructure layer are gated; the application layer is not

The domain-and-services layer is fully unit-testable in isolation and holds the rules that must not regress, so it carries the highest bar: at least eighty percent of lines and at least seventy percent of branches must be exercised by tests.

The infrastructure layer is testable but partly bound to the real filesystem, so it carries a line floor of seventy percent.

The application layer is excluded from the gate. It is dominated by thin wrappers over the user-interface framework and operating-system components, which the project standards explicitly say not to unit-test (trusting the underlying platform instead). Gating that layer would reward writing low-value tests of pass-through code. The testable behaviour in that layer continues to be covered by existing unit tests without a numeric floor.

Rejected: a single uniform threshold across all layers. It would either be too low to protect the logic layer or force pointless tests in the wrapper-heavy layer.

Rejected: no gate at all, relying on review discipline. Coverage erosion is exactly the kind of slow drift a human reviewer misses; an automated floor is cheap and objective.

---

## Decision 2 — Generated source is excluded from the coverage measurement

Members produced by source generators are excluded from coverage accounting. They are not hand-written logic, and counting them would inflate or distort the percentage without reflecting tested behaviour.

---

## Decision 3 — A label-based escape hatch lets urgent unrelated work bypass the gate

A pull request carrying a designated bypass label skips the coverage gate. This exists so that an urgent fix unrelated to test coverage is not blocked by pre-existing coverage debt elsewhere in the codebase. The hatch is intended for rare use; routine pull requests are expected to meet the thresholds. The label and its sparing-use expectation are documented in the project configuration file so contributors know it exists and when it is appropriate.

---

## Consequences

- Continuous integration fails any pull request that drops the gated layers below their thresholds, unless the bypass label is present.
- The thresholds reflect what the cleaned test suite actually achieves at the time of adoption; they are a floor to defend, not an aspiration the suite does not yet meet.
- Contributors adding logic to the gated layers must add tests or the build fails, making coverage a first-class part of the definition of done.
- The bypass label is a known, auditable mechanism rather than an ad-hoc disabling of the gate; its use is visible on the pull request.
- The application layer remains untested by numeric mandate, consistent with the standard of not testing pass-through wrappers; its meaningful logic is still covered by existing unit tests.
</content>
