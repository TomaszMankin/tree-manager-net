# ADR-014 — Shortcut creation via the wide shell-link COM interface

## Context

Issue #12 generates the Drzewo view as a folder of shortcut files, each pointing at a person's folder and named with that person's full name. Person and folder names routinely contain Polish diacritics. The application targets an elderly Polish user, so corrupted names in the generated view are not acceptable. Two ways to create shortcuts on Windows are available: the scripting host's shortcut object, and the wide (Unicode) shell-link COM interface. The choice must guarantee that diacritics in both the shortcut's target path and its display name survive creation intact.

## Decision

Shortcuts are created through the wide, Unicode shell-link COM interface, obtained with a compile-time interop generator, and persisted through the matching Unicode persistence interface. The scripting-host shortcut object is not used. The rationale is correctness of text: the scripting-host path round-trips strings through a non-Unicode layer and corrupts Polish diacritics in shortcut targets and names, whereas the wide COM interface accepts and stores the original Unicode text unchanged. The interop is generated at build time rather than hand-declared so the native signatures stay correct and Windows-only by construction. All native interop is confined to the infrastructure layer behind a single application-owned abstraction, so the rest of the application depends only on the abstraction.

## Consequences

- Diacritics in targets and names are preserved, removing the mojibake class of defect that the scripting-host approach produces.
- A build-time interop generator is added to the infrastructure layer; the build now depends on it, and the layer is effectively Windows-only at runtime, which is already true for the product.
- Shortcut creation runs through COM, requiring apartment initialization; this is handled once at the creation seam and is invisible to callers.
- A regression test resolves a shortcut to a diacritic-bearing target and asserts the path is returned unchanged, locking the behavior against a future regression to the scripting-host approach.

## Status

Accepted · 2026-06-04
</content>
