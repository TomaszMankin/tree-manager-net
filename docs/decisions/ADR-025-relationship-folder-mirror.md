---
id: ADR-025
title: Physical relationship subfolders with bidirectional shortcut mirror
status: Accepted
date: 2026-06-09
---

## Context

Each person's data folder in the tree has subfolders named after relationship types. In the Python implementation these folders contain `.lnk` shortcuts pointing at related persons' folders, and every shortcut is created in both directions simultaneously: when A is a parent of B, a shortcut appears both in B's parents folder and in A's children folder. The .NET port initially omitted this physical projection, synchronising only the JSON arrays. Users observed that the filesystem structure did not match expectations and that the folder shortcuts were absent despite the data being correct.

## Decision

Every write operation that creates or modifies a person's relationship arrays also projects those relationships into the physical folder hierarchy as Windows shortcut pairs. The projection mirrors the Python behaviour exactly: each of the four relationship types (parents, children, spouses, siblings) has a dedicated named subfolder, and every relationship is represented by a shortcut from each side to the other's folder. The projection is tied to the persistence layer so it runs automatically on every save without callers needing to know about it.

When a related person's folder cannot be located the shortcut for that link is skipped and an error is logged; the operation continues with the remaining links. The service never throws; a missing folder is a data-quality issue, not a code fault.

## Consequences

- Explorer navigation within the tree now works without launching the application.
- The four relationship subfolders are created on every save, even when empty, so the folder structure is always present.
- Removing a relationship removes both shortcuts; adding one creates both.
- Any corruption of the JSON arrays will be reflected in missing or stale shortcuts, making data problems visible in the filesystem.
