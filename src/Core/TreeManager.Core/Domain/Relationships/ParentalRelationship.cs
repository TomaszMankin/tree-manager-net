namespace TreeManager.Core.Domain.Relationships;

/// <summary>Represents a parental relationship.</summary>
public readonly record struct ParentalRelationship(Guid ParentId, ParentalRelationshipType Type);
