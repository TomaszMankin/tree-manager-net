namespace TreeManager.Core.Domain.Relationships;

/// <summary>Represents a spousal or partner relationship.</summary>
public readonly record struct SpousalRelationship(Guid SpouseId, SpousalRelationshipType Type, bool IsCurrent);
