# Domain concepts

## Drzewo (folder-tree view)

The 'Drzewo' view is a flat directory `<root>/Drzewo/` containing one `.lnk` shortcut per person in the hourglass selection from a chosen root person. Each shortcut points at that person's folder under `'Lista osób'`.

### Filename convention

```
Gen 0 (root + spouse):  [NN][0][gender] FullName.lnk
Gen ≠ 0:                [NN][display][couple-code][gender] FullName.lnk
```

Field definitions:

| Field | Formula | Example |
|---|---|---|
| `NN` | `gen + 50` (two digits, zero-padded) — physical sort key | `50`, `51`, `49` |
| `display` | `-gen` — sign-flipped human-readable generation number | `0`, `-1`, `1` |
| `couple-code` | Per-generation base-26 letter(s) A…Z (width pre-detected from generation total) | `A`, `B`, `AA` |
| `gender` | `M` (male) or `F` (female); defaults to `M` when unknown | `M`, `F` |
| `FullName` | Computed display name: first + other-first + last + other-last + `zd. maiden` | `Jan Kowalski`, `Anna Nowak zd. Wiśniewska` |

Examples:
- `[50][0][M] Adam Kowalski.lnk` — root (gen 0, male)
- `[50][0][F] Eva Nowakowska.lnk` — root's spouse (gen 0, female)
- `[51][-1][A][M] Piotr Kowalski.lnk` — root's father (gen +1, couple A)
- `[51][-1][B][F] Anna Nowak.lnk` — spouse's mother (gen +1, couple B)
- `[49][1][A][M] Tomek Kowalski.lnk` — root's son (gen −1, couple A)

Windows Explorer sorts by filename. The `NN` prefix places ancestors (higher NN) above root (50) and descendants (lower NN) below, matching the natural family-tree reading direction.

The couple-code width is determined per generation from the total couple count: ≤26 → 1 letter, ≤676 → 2 letters, etc. (base-26 ladder).

## Lista osób (people list)

`<root>/'Lista osób'/` — one subfolder per person, each holding `me.json`. The authoritative people store; the main scan descends only this folder. Example: `<root>/'Lista osób'/Kowalski Jan/me.json`.

## me.json + partial-date format

Per-person record file stored as `me.json` in each person's folder. Dates serialized as `DD|MM|YYYY` with `--` for unknown components; year may be a partial string (`184-` for a decade, `18--` for a century). Example: birth date `12|03|1947`, year-only `--|--|1847`, decade `--|--|184-`.

## Rody (lineage folders)

`<root>/'Rody'/<lineage-surname>/` shortcut folders grouping a contributor's bloodline. Contributor = a parent of the root person OR a parent of the root person's spouse. Membership = the contributor's descendants (subtree) + contributor's spouses + full ancestor bloodline walked upward (no surname gate). Surname-clash: when two contributors share a surname, the folder name escalates to their full display name. Example: paternal grandfather and maternal grandmother both surnamed Kowalski → two full-name folders under `'Rody'/`, not one merged folder.

## Poczekalnia (drafts)

`<root>/'Poczekalnia'/` sibling staging folder for work-in-progress people. Excluded from the main scan by location (not by a name list). Never relationship-synced while parked; no minimum-relationship rule applies. Promotion moves the draft into `'Lista osób'`, runs bidirectional sync, then deletes the draft. Recoverable on failure. Example: a new person created offline sits in `<root>/'Poczekalnia'/Nowak Tomasz/` until approved, then moves to `<root>/'Lista osób'/Nowak Tomasz/`.

## Bidirectional relationship sync invariant

Every relationship is stored on BOTH people's `me.json`. UUID is authoritative; the cached display name is a convenience copy refreshed on rename. Sync is idempotent — re-running de-dupes. Example: adding Anna as Jan's mother also writes Jan into Anna's children list immediately, with no manual step.

