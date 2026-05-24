using TreeManager.Core.Domain.Relationships;

namespace TreeManager.Core.Domain;

/// <summary>Fluent builders to create typed relationship structs from a person's <see cref="Guid"/> identifier.</summary>
public static class PersonExtensions
{
    /// <summary>Creates a <see cref="SpousalRelationship"/> for this person's ID.</summary>
    public static SpousalRelationship AsSpouse(this Guid id,
        SpousalRelationshipType type = SpousalRelationshipType.Spouse,
        bool isCurrent = true) => new(id, type, isCurrent);

    /// <summary>Creates a <see cref="ParentalRelationship"/> for this person's ID.</summary>
    public static ParentalRelationship AsParent(this Guid id,
        ParentalRelationshipType type = ParentalRelationshipType.Parent) => new(id, type);

    /// <summary>Creates a <see cref="ChildRelationship"/> for this person's ID.</summary>
    public static ChildRelationship AsChild(this Guid id,
        ChildRelationshipType type = ChildRelationshipType.Child) => new(id, type);

    /// <summary>Creates a <see cref="SiblingRelationship"/> for this person's ID.</summary>
    public static SiblingRelationship AsSibling(this Guid id,
        SiblingRelationshipType type = SiblingRelationshipType.Sibling) => new(id, type);
}
