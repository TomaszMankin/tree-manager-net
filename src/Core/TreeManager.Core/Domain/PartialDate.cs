namespace TreeManager.Core.Domain;

/// <summary>Partial date with optional before/approx qualifiers. Wire format: "DD|MM|YYYY" with "--" for unknown components, optionally prefixed with "~" (approx) and/or "&lt;" (before). Year stored as raw wire string ("1947", "19--") or null for fully unknown ("----").</summary>
public readonly record struct PartialDate(int? Day, int? Month, string Year)
{
    public bool IsBefore { get; init; }
    public bool IsApprox { get; init; }
}
