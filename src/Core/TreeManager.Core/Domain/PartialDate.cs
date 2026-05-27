namespace TreeManager.Core.Domain;

/// <summary>Partial date. Wire format: "DD|MM|YYYY" with "XX" for unknown components. Year stored as raw wire string ("1947", "198X") or null for fully unknown ("XXXX").</summary>
public readonly record struct PartialDate(int? Day, int? Month, string Year);
