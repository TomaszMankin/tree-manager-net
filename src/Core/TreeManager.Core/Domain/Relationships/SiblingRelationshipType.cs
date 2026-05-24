namespace TreeManager.Core.Domain.Relationships;

/// <summary>Type of sibling relationship.</summary>
public enum SiblingRelationshipType
{
    Sibling,        // Pełne rodzeństwo
    HalfSibling,    // Rodzeństwo przyrodnie (wspólny jeden rodzic)
    StepSibling,    // Rodzeństwo przybrane (np. dziecko nowego partnera rodzica)
}
