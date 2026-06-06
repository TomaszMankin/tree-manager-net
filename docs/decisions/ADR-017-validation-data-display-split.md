# ADR-017: validation-data-display-split

Status: Accepted
Date: 2026-06-06

## Problem

Tree-consistency validation produces structural findings that must be communicated to the user in Polish. Two naive approaches create coupling: putting Polish text directly in the domain core couples Core to a UI language; putting all validation logic in the App layer breaks the onion boundary. Additionally, a separate fail-fast integrity mechanism already exists for generation time (ADR-016); the new diagnostic validator operates under a different contract — collect-all, non-throwing — so neither can replace the other.

## Decision

Core produces findings as language-neutral data: each finding carries its kind (cycle, one-sided relationship, orphan, stale reference) and the identifiers of the people involved. The App layer holds the translation step: it maps each kind to a Polish display string using the person identifiers to look up names. Validation and formatting are separate passes invoked in sequence by the command in the App layer.

## Rejected alternatives

- Core emits Polish strings directly: couples Core to the UI language; Core unit tests must assert on human-readable text rather than on structured data, and adding a language becomes a Core change.
- Single flat string in the finding record: loses the structured data (kind + subject identifiers) that future consumers — logging, export, automated repair — would need to act on the finding rather than just display it.
- Unifying the new validator with the generation-time integrity checks: those checks throw on the first violation before destructive output is written; the diagnostic validator collects all violations and never throws. Bending one contract to serve both purposes would break ADR-016 guarantees.

## Consequences

+ Core validator is unit-tested against data — kind and subject GUIDs — not against Polish strings.
+ Changing or adding a display language requires only an App-layer change; Core is untouched.
+ Two code paths share cycle-detection intent: generation fail-fast (throws, single-root walk) and diagnostic collect-all (returns, all-roots DFS). Duplication is intentional given the contract difference; consolidation is deferred as future work.
- Two-step dispatch (validate → format → display) adds a seam that callers must wire; not self-contained.
