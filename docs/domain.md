# Domain concepts

## Drzewo (folder-tree view)

The Drzewo view is a flat directory `<root>/Drzewo/` containing one `.lnk` shortcut per person in the hourglass selection from a chosen root person. Each shortcut points at that person's folder under `Lista osób/`.

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

> Full domain glossary: issue #19.
