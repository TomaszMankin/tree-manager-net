using System;
using System.Collections.Generic;

namespace TreeManager.UiTesting.Graph;

/// <summary>
/// In-memory model of the tree a scenario expects on disk. Built up by recording each
/// person and each relationship. Adding a relationship automatically derives the
/// reciprocal me.json reference and BOTH .lnk edges using the same subfolder mapping
/// the production mirror uses, so callers describe a relationship once and the model
/// stays consistent with what the app writes.
/// </summary>
public sealed class ExpectedTreeGraph
{
    private readonly Dictionary<Guid, PersonNode> _nodesByUid = new Dictionary<Guid, PersonNode>();

    public IReadOnlyCollection<PersonNode> Nodes => _nodesByUid.Values;

    public PersonNode AddPerson(string folderName, Guid uniqueIdentifier)
    {
        if (_nodesByUid.TryGetValue(uniqueIdentifier, out var existing))
        {
            return existing;
        }

        var node = new PersonNode(folderName, uniqueIdentifier);
        _nodesByUid.Add(uniqueIdentifier, node);
        return node;
    }

    public bool TryGetByUid(Guid uniqueIdentifier, out PersonNode node)
    {
        return _nodesByUid.TryGetValue(uniqueIdentifier, out node);
    }

    /// <summary>
    /// Records a relationship from <paramref name="personUid"/> to <paramref name="relatedUid"/>.
    /// Updates both persons' me.json reference sets and derives both reciprocal .lnk edges.
    /// Both persons must already be present.
    /// </summary>
    public void AddRelationship(Guid personUid, Guid relatedUid, RelationshipKind kind)
    {
        var person = Require(personUid);
        var related = Require(relatedUid);

        switch (kind)
        {
            case RelationshipKind.Parent:
                LinkAsymmetric(person, related, RelationshipSubfolders.Parents, RelationshipSubfolders.Children);
                person.ParentIds.Add(relatedUid);
                related.ChildIds.Add(personUid);
                break;

            case RelationshipKind.Child:
                LinkAsymmetric(person, related, RelationshipSubfolders.Children, RelationshipSubfolders.Parents);
                person.ChildIds.Add(relatedUid);
                related.ParentIds.Add(personUid);
                break;

            case RelationshipKind.Spouse:
                LinkSymmetric(person, related, RelationshipSubfolders.Spouses);
                person.SpouseIds.Add(relatedUid);
                related.SpouseIds.Add(personUid);
                break;

            case RelationshipKind.Sibling:
                LinkSymmetric(person, related, RelationshipSubfolders.Siblings);
                person.SiblingIds.Add(relatedUid);
                related.SiblingIds.Add(personUid);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown relationship kind");
        }
    }

    private static void LinkAsymmetric(PersonNode person, PersonNode related, string personSubfolder, string relatedSubfolder)
    {
        person.LinkEdges.Add(new LinkEdge(personSubfolder, related.FolderName));
        related.LinkEdges.Add(new LinkEdge(relatedSubfolder, person.FolderName));
    }

    private static void LinkSymmetric(PersonNode person, PersonNode related, string subfolder)
    {
        person.LinkEdges.Add(new LinkEdge(subfolder, related.FolderName));
        related.LinkEdges.Add(new LinkEdge(subfolder, person.FolderName));
    }

    private PersonNode Require(Guid uid)
    {
        if (!_nodesByUid.TryGetValue(uid, out var node))
        {
            throw new InvalidOperationException("Person " + uid + " must be added before recording a relationship");
        }
        return node;
    }
}
