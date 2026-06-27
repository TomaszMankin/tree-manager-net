using System;
using System.Collections.Generic;
using TreeManager.UiTesting.Graph;

namespace TreeManager.UiTesting.Verification;

/// <summary>
/// Compares an expected tree graph against the actual on-disk graph with set-equality in
/// both directions, for both me.json relationship references and .lnk edges. Missing
/// catches creation bugs; extra catches stale / orphan artifacts. Links are compared by
/// resolved target folder name.
/// </summary>
public sealed class TreeGraphVerifier
{
    public TreeGraphVerificationResult Verify(ExpectedTreeGraph expected, ActualTreeGraph actual)
    {
        var expectedRelationships = FlattenExpectedRelationships(expected);
        var actualRelationships = FlattenActualRelationships(actual);

        var expectedLinks = FlattenExpectedLinks(expected);
        var actualLinks = FlattenActualLinks(actual);

        return new TreeGraphVerificationResult(
            Difference(expectedRelationships, actualRelationships, ToRelationshipDiscrepancy),
            Difference(actualRelationships, expectedRelationships, ToRelationshipDiscrepancy),
            Difference(expectedLinks, actualLinks, ToLinkDiscrepancy),
            Difference(actualLinks, expectedLinks, ToLinkDiscrepancy));
    }

    private static Dictionary<RelationshipKey, RelationshipRecord> FlattenExpectedRelationships(ExpectedTreeGraph graph)
    {
        var result = new Dictionary<RelationshipKey, RelationshipRecord>();
        foreach (var node in graph.Nodes)
        {
            FlattenNodeRelationships(node, result);
        }
        return result;
    }

    private static Dictionary<RelationshipKey, RelationshipRecord> FlattenActualRelationships(ActualTreeGraph graph)
    {
        var result = new Dictionary<RelationshipKey, RelationshipRecord>();
        foreach (var node in graph.Nodes)
        {
            FlattenNodeRelationships(node, result);
        }
        return result;
    }

    private static void FlattenNodeRelationships(PersonNode node, Dictionary<RelationshipKey, RelationshipRecord> result)
    {
        AddRelationships(node, RelationshipKind.Parent, node.ParentIds, result);
        AddRelationships(node, RelationshipKind.Child, node.ChildIds, result);
        AddRelationships(node, RelationshipKind.Spouse, node.SpouseIds, result);
        AddRelationships(node, RelationshipKind.Sibling, node.SiblingIds, result);
    }

    private static void AddRelationships(
        PersonNode node,
        RelationshipKind kind,
        IEnumerable<Guid> relatedIds,
        Dictionary<RelationshipKey, RelationshipRecord> result)
    {
        foreach (var relatedId in relatedIds)
        {
            var key = new RelationshipKey(node.UniqueIdentifier, kind, relatedId);
            result[key] = new RelationshipRecord(node.FolderName, node.UniqueIdentifier, kind, relatedId);
        }
    }

    private static Dictionary<LinkKey, LinkRecord> FlattenExpectedLinks(ExpectedTreeGraph graph)
    {
        var result = new Dictionary<LinkKey, LinkRecord>();
        foreach (var node in graph.Nodes)
        {
            FlattenNodeLinks(node, result);
        }
        return result;
    }

    private static Dictionary<LinkKey, LinkRecord> FlattenActualLinks(ActualTreeGraph graph)
    {
        var result = new Dictionary<LinkKey, LinkRecord>();
        foreach (var node in graph.Nodes)
        {
            FlattenNodeLinks(node, result);
        }
        return result;
    }

    private static void FlattenNodeLinks(PersonNode node, Dictionary<LinkKey, LinkRecord> result)
    {
        foreach (var edge in node.LinkEdges)
        {
            var key = new LinkKey(node.FolderName, edge.Subfolder, edge.TargetFolderName);
            result[key] = new LinkRecord(node.FolderName, edge.Subfolder, edge.TargetFolderName);
        }
    }

    private static List<TDiscrepancy> Difference<TKey, TValue, TDiscrepancy>(
        Dictionary<TKey, TValue> left,
        Dictionary<TKey, TValue> right,
        Func<TValue, TDiscrepancy> project)
    {
        var result = new List<TDiscrepancy>();
        foreach (var pair in left)
        {
            if (!right.ContainsKey(pair.Key))
            {
                result.Add(project(pair.Value));
            }
        }
        return result;
    }

    private static RelationshipDiscrepancy ToRelationshipDiscrepancy(RelationshipRecord record)
    {
        return new RelationshipDiscrepancy(record.PersonFolderName, record.PersonUid, record.Kind.ToString(), record.RelatedUid);
    }

    private static LinkDiscrepancy ToLinkDiscrepancy(LinkRecord record)
    {
        return new LinkDiscrepancy(record.PersonFolderName, record.Subfolder, record.TargetFolderName);
    }

    private readonly struct RelationshipKey : IEquatable<RelationshipKey>
    {
        private readonly Guid _personUid;
        private readonly RelationshipKind _kind;
        private readonly Guid _relatedUid;

        public RelationshipKey(Guid personUid, RelationshipKind kind, Guid relatedUid)
        {
            _personUid = personUid;
            _kind = kind;
            _relatedUid = relatedUid;
        }

        public bool Equals(RelationshipKey other)
        {
            return _personUid == other._personUid && _kind == other._kind && _relatedUid == other._relatedUid;
        }

        public override bool Equals(object obj)
        {
            return obj is RelationshipKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_personUid, _kind, _relatedUid);
        }
    }

    private readonly struct LinkKey : IEquatable<LinkKey>
    {
        private readonly string _personFolderName;
        private readonly string _subfolder;
        private readonly string _targetFolderName;

        public LinkKey(string personFolderName, string subfolder, string targetFolderName)
        {
            _personFolderName = personFolderName;
            _subfolder = subfolder;
            _targetFolderName = targetFolderName;
        }

        public bool Equals(LinkKey other)
        {
            return string.Equals(_personFolderName, other._personFolderName, StringComparison.OrdinalIgnoreCase)
                && string.Equals(_subfolder, other._subfolder, StringComparison.OrdinalIgnoreCase)
                && string.Equals(_targetFolderName, other._targetFolderName, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj)
        {
            return obj is LinkKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(_personFolderName, StringComparer.OrdinalIgnoreCase);
            hash.Add(_subfolder, StringComparer.OrdinalIgnoreCase);
            hash.Add(_targetFolderName, StringComparer.OrdinalIgnoreCase);
            return hash.ToHashCode();
        }
    }

    private readonly struct RelationshipRecord
    {
        public RelationshipRecord(string personFolderName, Guid personUid, RelationshipKind kind, Guid relatedUid)
        {
            PersonFolderName = personFolderName;
            PersonUid = personUid;
            Kind = kind;
            RelatedUid = relatedUid;
        }

        public string PersonFolderName { get; }

        public Guid PersonUid { get; }

        public RelationshipKind Kind { get; }

        public Guid RelatedUid { get; }
    }

    private readonly struct LinkRecord
    {
        public LinkRecord(string personFolderName, string subfolder, string targetFolderName)
        {
            PersonFolderName = personFolderName;
            Subfolder = subfolder;
            TargetFolderName = targetFolderName;
        }

        public string PersonFolderName { get; }

        public string Subfolder { get; }

        public string TargetFolderName { get; }
    }
}
