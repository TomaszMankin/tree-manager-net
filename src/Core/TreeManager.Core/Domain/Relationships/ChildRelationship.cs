namespace TreeManager.Core.Domain.Relationships;

/// <summary>Represents a child relationship.</summary>
public readonly record struct ChildRelationship(Guid ChildId, ChildRelationshipType Type);
