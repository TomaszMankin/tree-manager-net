using System;
using System.Collections.Generic;

namespace TreeManager.UiTesting.Graph;

/// <summary>
/// One person in a tree graph: a stable identity plus the relationship reference sets
/// recorded in its me.json and the set of .lnk edges that should sit (or do sit) under
/// its relationship subfolders.
///
/// Identity is the person's folder name (the on-disk folder under 'Lista osób').
/// Relationship sets are keyed by UUID — the stable key that survives folder rename /
/// dedup. Link edges use folder NAME for the target (see <see cref="LinkEdge"/>).
/// </summary>
public sealed class PersonNode
{
    public PersonNode(string folderName, Guid uniqueIdentifier)
    {
        FolderName = folderName;
        UniqueIdentifier = uniqueIdentifier;
    }

    public string FolderName { get; }

    public Guid UniqueIdentifier { get; }

    public HashSet<Guid> ParentIds { get; } = new HashSet<Guid>();

    public HashSet<Guid> ChildIds { get; } = new HashSet<Guid>();

    public HashSet<Guid> SpouseIds { get; } = new HashSet<Guid>();

    public HashSet<Guid> SiblingIds { get; } = new HashSet<Guid>();

    public HashSet<LinkEdge> LinkEdges { get; } = new HashSet<LinkEdge>();
}
