# DES-001 — Windows filesystem as the Drzewo rendering medium

## What
The Drzewo folder represents the family tree as a flat set of Windows shortcut (.lnk) files. Each shortcut points to the corresponding person folder under `Lista osób`.

## Why
Windows Explorer displays .lnk files with their target's folder icon, providing a navigable generational view without any custom UI component. The user can browse generations, identify generational clusters by the encoded sort prefix, and open any person folder with a double-click.

Exporting the tree is a single copy operation — no external viewer required.

## Filename encoding
Each shortcut filename encodes its position in the tree:

```
[NN][display][couple-code][gender] Full Name.lnk
```

| Part | Meaning |
|---|---|
| `NN` | `generation + 50`, zero-padded. Sorts all entries by generation in Explorer. Gen-0 person → `[50]`. |
| `display` | `-(generation)`. Human-readable generation label. Gen+2 ancestor shows as `-2`. |
| `couple-code` | Base-26 letter(s) identifying the couple pair within the generation. Width auto-adjusts to couple count. |
| `gender` | `M` or `F`. For descendant spouses, uses the descendant's gender (rule B). |

Generation-0 (root and spouses) omits the couple code: `[NN][0][gender] Full Name.lnk`.
