namespace TreeManager.Core.Domain.Relationships;

/// <summary>Type of spousal relationship.</summary>
public enum SpousalRelationshipType
{
    Spouse,     // Aktualny małżonek
    ExSpouse,   // Były małżonek (np. po rozwodzie)
    Partner,    // Partner życiowy (bez formalnego małżeństwa)
}
