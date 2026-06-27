using System;

namespace TreeManager.UiTesting.Graph;

/// <summary>
/// One expected or actual .lnk shortcut: a link sitting in <see cref="Subfolder"/>
/// under a person folder, resolving to <see cref="TargetFolderName"/>. Targets are
/// stored as folder NAME only (not full path) so host-path differences never matter;
/// equality is case-insensitive per Windows filesystem semantics.
/// </summary>
public sealed class LinkEdge : IEquatable<LinkEdge>
{
    public LinkEdge(string subfolder, string targetFolderName)
    {
        Subfolder = subfolder;
        TargetFolderName = targetFolderName;
    }

    public string Subfolder { get; }

    public string TargetFolderName { get; }

    public bool Equals(LinkEdge other)
    {
        if (other is null) { return false; }
        if (ReferenceEquals(this, other)) { return true; }

        return string.Equals(Subfolder, other.Subfolder, StringComparison.OrdinalIgnoreCase)
            && string.Equals(TargetFolderName, other.TargetFolderName, StringComparison.OrdinalIgnoreCase);
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as LinkEdge);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Subfolder, StringComparer.OrdinalIgnoreCase);
        hash.Add(TargetFolderName, StringComparer.OrdinalIgnoreCase);
        return hash.ToHashCode();
    }

    public override string ToString()
    {
        return Subfolder + "/" + TargetFolderName + ".lnk";
    }
}
