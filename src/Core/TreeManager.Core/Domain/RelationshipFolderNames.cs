namespace TreeManager.Core.Domain;

/// <summary>
/// Canonical on-disk subfolder names for the four relationship kinds. Both the
/// relationship mirror (Core.Services) and the UI-testing verifier (UiTesting)
/// read from here so they can never silently diverge.
/// </summary>
public static class RelationshipFolderNames
{
    public const string Parents = "Rodzice";
    public const string Children = "Dzieci";
    public const string Spouses = "Małżonkowie";
    public const string Siblings = "Rodzeństwo";
}
