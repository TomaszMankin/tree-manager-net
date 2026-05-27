# ADR-002: me.json schema and PartialDate rename

Status: Accepted
Date: 2026-05-24

## Problem

New repo (tree-manager-net) must read/write py-tree-manager "me.json" files without data loss. Python writes specific snake_case property names + Polish wire values for `sex` + string-encoded partial dates. Drift = corrupted files on round-trip = user loses family data. The familytree reference repo had: a typo (`UniqueIndentifier`), a wrong type (`Notes` as `List<string>` instead of `string`), a non-descriptive name (`ExtendedDateTime`), and missing 10 of the 22 properties. All must be fixed before any callers exist.

## Decision

- **`MeFile` is `public sealed record`** with 22 init-only properties. Every property carries explicit `[JsonPropertyName("snake_case")]`. No global `PropertyNamingPolicy` — drift on any property added without an attribute shows up in serialized output immediately (auditable by inspection).
- **`Sex` enum** uses custom `JsonConverter<Sex>` (`SexJsonConverter`) writing `"Mężczyzna"` / `"Kobieta"` / `"Nieznana"` on serialize. On deserialize accepts both accented `"Mężczyzna"` and legacy unaccented `"Mezczyzna"` — py-tree-manager fixtures use the unaccented form. Tolerates empty string and unknown values by returning `Sex.Unknown` (no throw).
- **`PartialDate`** (renamed from `ExtendedDateTime`) is `readonly record struct (int? Day, int? Month, string Year)`. `Year` is a string to support partial values (e.g. "184-", "18--"). Serializes via `PartialDateExtensions.ToSerializedString()` to `"DD|MM|YYYY"` with `--` wildcards for unknown components.
- **`MeFile.DatesOfBirth` / `DatesOfDeath` / `Notes` are `string`** (not `List<string>`) — py-tree-manager fixture and `PersonDataWrapper.get_notes()` both return plain string; familytree's `List<string>` was a mistake.
- **`MeFile.SerializerOptions`** uses `JavaScriptEncoder.UnsafeRelaxedJsonEscaping` so Polish characters write as raw UTF-8 bytes (not `\uXXXX` escapes), matching py-tree-manager output and keeping diffs human-readable.
- **`IFileSystemFacade`** placed in `Core/Abstractions/IO/` per ADR-001 Onion rule. Writes UTF-8 without BOM (py-tree-manager default). Reads are BOM-tolerant.

## Rejected alternatives

- Global `PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower`: silent drift on any property added without thinking; explicit `[JsonPropertyName]` makes missed cases auditable
- Sex wire `"M"/"K"` (issue #2 spec): contradicts all py-tree-manager fixtures and dropdown labels; would require migration of every existing me.json — rejected via parity rule
- Keep `ExtendedDateTime` name: misleading ("extended" implies more than DateTime; struct actually represents *less*); zero migration cost now; rename clarifies domain concept
- `[JsonStringEnumMemberName("…")]` on Sex: works for write but matches only ONE literal per member on read — cannot accept both `"Mężczyzna"` and `"Mezczyzna"`; custom converter is the only correct path
- `MeFile` as plain class: `record` provides structural equality via compiler synthesis; needed for dirty-tracking comparisons in sprint-08+

## Consequences

+ Roundtrip safety: every property explicitly named; new properties without `[JsonPropertyName]` produce auditable JSON-output drift
+ Sex enum tolerates both accented and unaccented Polish legacy fixtures — backward compatible with all existing py-tree-manager data
+ `PartialDate` name reads naturally in domain code (`person.DatesOfBirth` parsed to `PartialDate` at VM boundary)
+ BOM-tolerant read + BOM-free write matches py-tree-manager behaviour
- `[JsonPropertyName]` on all 22 properties is verbose — intentional cost of explicit mapping
- Custom `SexJsonConverter` is ~35 LOC of bespoke code vs zero for the attribute-only approach — necessary for legacy tolerance
- `DatesOfBirth`/`DatesOfDeath` as `string` in MeFile is a leaky abstraction at the VM boundary (sprint-06 converts string ↔ PartialDate); keeping domain shape identical to py-tree-manager justifies the deferred conversion
