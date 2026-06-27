using System;

namespace TreeManager.UiTesting.Verification;

/// <summary>A single me.json relationship reference present on one side of the compare but not the other.</summary>
public sealed class RelationshipDiscrepancy
{
    public RelationshipDiscrepancy(string personFolderName, Guid personUid, string kind, Guid relatedUid)
    {
        PersonFolderName = personFolderName;
        PersonUid = personUid;
        Kind = kind;
        RelatedUid = relatedUid;
    }

    public string PersonFolderName { get; }

    public Guid PersonUid { get; }

    public string Kind { get; }

    public Guid RelatedUid { get; }

    public override string ToString()
    {
        return PersonFolderName + " [" + PersonUid + "] " + Kind + " -> " + RelatedUid;
    }
}
