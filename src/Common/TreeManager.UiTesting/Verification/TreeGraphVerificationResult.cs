using System.Collections.Generic;
using System.Text;

namespace TreeManager.UiTesting.Verification;

/// <summary>
/// Outcome of comparing an expected graph against the actual on-disk graph. Holds the
/// set differences in both directions for both me.json relationships and .lnk edges.
/// Missing = expected but not found (creation bug); Extra = found but not expected
/// (stale / orphan artifact).
/// </summary>
public sealed class TreeGraphVerificationResult
{
    public TreeGraphVerificationResult(
        IReadOnlyList<RelationshipDiscrepancy> missingRelationships,
        IReadOnlyList<RelationshipDiscrepancy> extraRelationships,
        IReadOnlyList<LinkDiscrepancy> missingLinks,
        IReadOnlyList<LinkDiscrepancy> extraLinks)
    {
        MissingRelationships = missingRelationships;
        ExtraRelationships = extraRelationships;
        MissingLinks = missingLinks;
        ExtraLinks = extraLinks;
    }

    public IReadOnlyList<RelationshipDiscrepancy> MissingRelationships { get; }

    public IReadOnlyList<RelationshipDiscrepancy> ExtraRelationships { get; }

    public IReadOnlyList<LinkDiscrepancy> MissingLinks { get; }

    public IReadOnlyList<LinkDiscrepancy> ExtraLinks { get; }

    public bool IsMatch =>
        MissingRelationships.Count == 0
        && ExtraRelationships.Count == 0
        && MissingLinks.Count == 0
        && ExtraLinks.Count == 0;

    public string Describe()
    {
        if (IsMatch)
        {
            return "Tree graph matches expectation.";
        }

        var builder = new StringBuilder();
        builder.AppendLine("Tree graph does NOT match expectation:");

        AppendRelationshipSection(builder, "Missing me.json relationships (expected, not found)", MissingRelationships);
        AppendRelationshipSection(builder, "Extra me.json relationships (found, not expected)", ExtraRelationships);
        AppendLinkSection(builder, "Missing .lnk edges (expected, not found)", MissingLinks);
        AppendLinkSection(builder, "Extra .lnk edges (found, not expected)", ExtraLinks);

        return builder.ToString();
    }

    private static void AppendRelationshipSection(StringBuilder builder, string title, IReadOnlyList<RelationshipDiscrepancy> items)
    {
        if (items.Count == 0)
        {
            return;
        }

        builder.AppendLine(title + ":");
        foreach (var item in items)
        {
            builder.AppendLine("  - " + item);
        }
    }

    private static void AppendLinkSection(StringBuilder builder, string title, IReadOnlyList<LinkDiscrepancy> items)
    {
        if (items.Count == 0)
        {
            return;
        }

        builder.AppendLine(title + ":");
        foreach (var item in items)
        {
            builder.AppendLine("  - " + item);
        }
    }
}
