namespace TreeManager.Core.Domain.Relationships;

/// <summary>Represents a sibling relationship.</summary>
public readonly record struct SiblingRelationship(Guid SiblingId, SiblingRelationshipType Type);
