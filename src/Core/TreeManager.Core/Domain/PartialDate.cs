namespace TreeManager.Core.Domain;

/// <summary>Partial date. Wire format: "DD|MM|YYYY" with "--" for unknown components. Year stored as raw wire string ("1947", "19--") or null for fully unknown ("----").</summary>
public readonly record struct PartialDate(int? Day, int? Month, string Year);
