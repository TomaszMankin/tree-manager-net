namespace TreeManager.Core.Domain;

/// <summary>
/// Partial date with "XX" wildcard for unknown components.
/// Wire format: "DD|MM|YYYY" (serialized via <see cref="PartialDateExtensions.ToSerializedString"/>).
/// Renamed from familytree's ExtendedDateTime — see ADR-002.
/// </summary>
public readonly record struct PartialDate(int? Day, int? Month, int? Year);
