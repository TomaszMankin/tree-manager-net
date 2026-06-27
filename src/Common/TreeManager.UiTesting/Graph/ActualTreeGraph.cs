using System;
using System.Collections.Generic;

namespace TreeManager.UiTesting.Graph;

/// <summary>
/// In-memory model of the tree as it actually sits on disk, produced by
/// <see cref="DiskTreeGraphReader"/>. Same shape as <see cref="ExpectedTreeGraph"/>
/// (nodes keyed by UUID, each with its me.json reference sets and resolved .lnk edges).
/// </summary>
public sealed class ActualTreeGraph
{
    private readonly Dictionary<Guid, PersonNode> _nodesByUid = new Dictionary<Guid, PersonNode>();

    public IReadOnlyCollection<PersonNode> Nodes => _nodesByUid.Values;

    public PersonNode Add(PersonNode node)
    {
        _nodesByUid[node.UniqueIdentifier] = node;
        return node;
    }

    public bool TryGetByUid(Guid uniqueIdentifier, out PersonNode node)
    {
        return _nodesByUid.TryGetValue(uniqueIdentifier, out node);
    }
}
