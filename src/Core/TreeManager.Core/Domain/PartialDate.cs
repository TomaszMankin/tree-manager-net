namespace TreeManager.Core.Domain;

/// <summary>Partial date. Wire format: "DD|MM|YYYY" with "XX" for unknown components.</summary>
public readonly record struct PartialDate(int? Day, int? Month, int? Year);
