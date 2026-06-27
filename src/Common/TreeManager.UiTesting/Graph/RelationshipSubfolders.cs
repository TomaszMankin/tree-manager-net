using TreeManager.Core.Domain;

namespace TreeManager.UiTesting.Graph;

/// <summary>
/// Re-exports <see cref="RelationshipFolderNames"/> as a convenient set for the verifier
/// and reader. Constants delegate to the single Core source of truth — no literals here.
/// </summary>
public static class RelationshipSubfolders
{
    public const string Parents = RelationshipFolderNames.Parents;
    public const string Children = RelationshipFolderNames.Children;
    public const string Spouses = RelationshipFolderNames.Spouses;
    public const string Siblings = RelationshipFolderNames.Siblings;

    public static readonly string[] All = { Parents, Children, Spouses, Siblings };
}
