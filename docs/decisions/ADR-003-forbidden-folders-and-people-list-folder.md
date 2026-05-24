# ADR-003: Forbidden folder names hardcoded in ForbiddenFolders

Status: Accepted
Date: 2026-05-24

## Problem

Scan of `<root>/Lista osób/` must skip four non-person folders whose names
are domain conventions: "Pozostałe nieuporządkowane", "Rutowscy - dane ogólne",
"Do ustalenia", "Wspólne". The list is hard-coded in py-tree-manager's
file_service.py. tree-manager-net must match exactly — drift means user-visible
folders go missing or non-person folders pollute the people cache.

Additionally, the parent folder name `Lista osób` appears at multiple call
sites. A single constant source prevents silent breakage if any copy
drifts.

## Decision

- Static `IReadOnlySet<string> ForbiddenFolders.Default` in
  `TreeManager.Core/Domain/Constants/ForbiddenFolders.cs` — four exact
  strings, `StringComparer.Ordinal` (case-sensitive, byte-exact match).
- `PeopleListFolderName = "Lista osób"` sibling constant in the same file —
  both are parity-with-py-tree-manager folder-name tokens, one natural home.
- `MeFileProcessor` primary ctor wires `ForbiddenFolders.Default`; secondary
  ctor accepts an `IReadOnlySet<string>` parameter for test override (e.g.,
  empty set to bypass filtering in folder-depth tests).

## Rejected alternatives

- Read forbidden list from configuration: adds complexity for no use case.
  If the list changes, ship a code change with an ADR update — one-line edit.
- Case-insensitive comparison: would mask real folder-name typos and diverge
  from py-tree-manager (Python `in` on a list is byte-exact).
- Hard-code inside `MeFileProcessor` without a Constants/ file: obscures the
  parity link and prevents reuse from future components that also need to know
  which folders are non-person.
- `FrozenSet<string>`: lookup performance irrelevant for a 4-element set;
  `HashSet<string>` cast to `IReadOnlySet<string>` is simpler.

## Consequences

+ Parity with py-tree-manager guaranteed at the constant-string level.
+ Tests can inject a smaller/larger set when needed.
+ Adding a new forbidden folder = one-line edit + ADR update.
- Hard-coded list — not user-editable at runtime. Acceptable: the four names
  are domain conventions, not user preferences.
