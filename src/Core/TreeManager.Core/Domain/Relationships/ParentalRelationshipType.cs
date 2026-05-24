namespace TreeManager.Core.Domain.Relationships;

/// <summary>Type of parental relationship.</summary>
public enum ParentalRelationshipType
{
    Parent,         // Rodzic biologiczny
    StepParent,     // Rodzic przybrany (np. ojczym/macocha)
    AdoptiveParent, // Rodzic adopcyjny
}
